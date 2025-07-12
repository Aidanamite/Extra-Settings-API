using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Data : ModSetting
    {
        Value<Dictionary<string, string>> value;
        public ModSetting_Data(JObject source, ModSettingContainer parent) : base(source, parent)
        {
            TrySetupMember(source);
            var defaultValue
                = TryGetMemberValue(out var member) && member != null
                ? new Dictionary<string, string>((IDictionary<string, string>)member)
                : source.TryGetValue<JObject>("default", out var defaultField)
                ? GetSimple(defaultField)
                : null;
            value = NewValue(defaultValue ?? new Dictionary<string, string>());
            LoadSettings();
        }

        static Dictionary<string, string> GetSimple(IEnumerable<KeyValuePair<string, JToken>> obj) => obj.ToDictionary(x => x.Key, x => (x.Value as JValue)?.Value?.EnsureType<string>());

        protected override bool IsMemberTypeValid(Type type) => typeof(IDictionary<string, string>).IsAssignableFrom(type);
        protected override bool ConvertValueForMember(object value, Type targetType, out object result)
        {
            if (value is JObject j)
            {
                result = GetSimple(j);
                return TryEnsureValueForType(ref result, targetType, out _);
            }
            if (value is Dictionary<string, string> d)
            {
                result = CreateNewMemberValue(targetType, d);
                return true;
            }
            return base.ConvertValueForMember(value, targetType, out result);
        }

        static IDictionary<string, string> CreateNewMemberValue(Type targetType, IDictionary<string, string> values = null)
        {
            if (targetType.IsAbstract || targetType.IsInterface)
                throw new ArgumentException("Cannot construct instance of abstract or interface type \"" + targetType.FullName + "\"", nameof(targetType));
            var ctors = targetType.GetConstructors(~System.Reflection.BindingFlags.Default);
            ConstructorInfo best = null;
            bool bestHasDict = false;
            var want = values?.GetType();
            foreach (var ctor in ctors)
            {
                bool success = true;
                bool hasDict = false;
                foreach (var p in ctor.GetParameters())
                    if (want != null && p.ParameterType.IsAssignableFrom(want))
                    {
                        if (hasDict)
                        {
                            success = false;
                            break;
                        }
                        hasDict = true;
                    }
                    else if (!p.IsOptional)
                    {
                        success = false;
                        break;
                    }
                if (success && (!bestHasDict || hasDict) && (best == null || (hasDict && !bestHasDict) || ctor.GetParameters().Length < best.GetParameters().Length))
                {
                    best = ctor;
                    bestHasDict = hasDict;
                }
            }
            if (best == null)
                throw new MissingMethodException("No suitable constructor found for type \"" + targetType.FullName + "\". Type must have a constructor that requires 0 parameters or a single Dictionary<string,string> parameter");
            return (IDictionary<string, string>)best.Invoke(best.GetParameters().Select(x => x.ParameterType.IsAssignableFrom(want) ? values : x.DefaultValue).ToArray());
        }

        public void SetValue(string name, string newValue, bool local, SetFlags flags = SetFlags.All)
        {
            flags = FilterFlags(flags, local);
            if (flags.HasFlag(SetFlags.Storage))
                value[local][name] = newValue;
            if (flags.HasFlag(SetFlags.Member) && target != null)
            {
                if (SkipMemberSet())
                    return;
                if (TryGetMemberValue(out var mem))
                {
                    var d = mem as IDictionary<string, string>;
                    if (d == null)
                        SetMemberValue(d = CreateNewMemberValue(target.MemberType));
                    d[name] = newValue;
                }
                else
                    SetMemberValue(CreateNewMemberValue(target.MemberType, value[local]));
            }
        }

        public void SetValues(Dictionary<string, string> values, bool local, SetFlags flags = SetFlags.All)
        {
            flags = FilterFlags(flags, local);
            if (flags.HasFlag(SetFlags.Storage))
                value[local].CopyFrom(values);
            if (flags.HasFlag(SetFlags.Member) && target != null)
            {
                if (SkipMemberSet())
                    return;
                if (TryGetMemberValue(out var mem))
                {
                    var d = mem as IDictionary<string, string>;
                    if (d == null)
                        SetMemberValue(d = CreateNewMemberValue(target.MemberType));
                    d.CopyFrom(values);
                }
                else
                    SetMemberValue(CreateNewMemberValue(target.MemberType, values));
            }
        }

        protected override bool DoRealtimeMemberCheck()
        {
            if (!TryGetMemberValue(out var mem))
                return false;
            if (mem == null)
            {
                SetValues(null, ExtraSettingsAPI.IsInWorld, SetFlags.All ^ SetFlags.Member);
                return true;
            }
            return value.current.CopyFrom(mem as IDictionary<string, string>);
        }

        public string getValue(string name)
        {
            if (value.current.TryGetValue(name, out var v))
                return v;
            return "";
        }

        public string[] getNames()
        {
            return value.current.Keys.ToArray();
        }

        public override void LoadSettings(bool local)
        {
            JToken saved = parent.GetSavedSettings(this, local);
            if (saved is JObject obj)
                SetValues(GetSimple(obj), local, SetFlags.All ^ SetFlags.Control);
            else
                ResetValue(local);
        }
        public override void Destroy() { }
        protected override bool ShouldTryGenerateSave(bool local) => value.ShouldSave(local);
        public override JToken GenerateSaveJson(bool local)
        {
            var obj = new JObject();
            foreach (var p in value[local])
                obj[p.Key] = p.Value;
            return obj;
        }
        public override void ResetValue(bool local)
        {
            value.Reset(local);
            SetValues(value[local], local, SetFlags.All ^ SetFlags.Storage);
        }
        public override void Create() { }
        public override void SetText(string newText) { }

        public override void OnExitWorld()
        {
            if (save.IsSplit())
                SetValues(value.current, false, SetFlags.All ^ SetFlags.Storage);
        }
    }
}