using Newtonsoft.Json.Linq;
using ShellLink.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Combobox : ModSetting
    {
        public Dropdown combobox = null;
        Value<string[]> values;
        public Value<int> value;
        Value<bool> modifiedContent;
        public ModSetting_Combobox(JObject source, ModSettingContainer parent) : base(source, parent)
        {
            TrySetupMember(source);
            modifiedContent = NewValue(false);
            if (source.TryGetValue<JArray>("values", out var valuesField))
            {
                var defaultValues = new string[valuesField.Count];
                for (int i = 0; i < valuesField.Count; i++)
                    defaultValues[i] = (valuesField[i] as JValue).Value.ToString();
                values = NewValue(defaultValues);
            }
            else
                values = NewValue(new string[0]);
            var index = 0;
            if (source.TryGetValue<JValue>("default", out var defaultField))
            {
                if (defaultField.Type == JTokenType.Integer)
                    index = defaultField.Value.EnsureType<int>();
                else if (defaultField.Type == JTokenType.String)
                    index = Array.IndexOf(values.current, defaultField.Value.EnsureType<string>());
            }
            else if (TryConvertMember(out var mem))
                index = mem;
            if (index < 0 || index >= values.current.Length)
                index = 0;
            value = NewValue(index);
            LoadSettings();
        }

        protected override bool IsMemberTypeValid(Type type) => typeof(string) == type || type.IsEnum || type.IsNumber(false);

        protected override bool DoRealtimeMemberCheck()
        {
            if (!TryConvertMember(out var mem) || mem == value.current)
                return false;
            SetValue(mem, ExtraSettingsAPI.IsInWorld, SetFlags.All ^ SetFlags.Member);
            return true;
        }

        public override void SetGameObject(GameObject go)
        {
            base.SetGameObject(go);
            combobox = control.GetComponentInChildren<Dropdown>(true);
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (string item in values.current)
                options.Add(new Dropdown.OptionData(item));
            combobox.ClearOptions();
            combobox.AddOptions(options);
            if (options.Count > 0)
                combobox.value = value.current;
            combobox.onValueChanged.AddListener(x => SetValue(x, ExtraSettingsAPI.IsInWorld, SetFlags.All ^ SetFlags.Control));
        }

        bool TryConvertMember(out int result)
        {
            result = 0;
            if (!TryGetMemberValue(out var obj) || obj == null)
                return false;
            var curVals = values.current;
            if (obj is string str)
            {
                result = curVals.IndexOf(str);
                return result >= 0;
            }
            if (obj.TryConvert(out int ind) && ind >= 0 && ind < curVals.Length)
            {
                result = ind;
                return true;
            }
            str = obj.ToString();
            ind = curVals.IndexOf(str);
            if (ind < 0)
                return false;
            result = ind;
            return true;
        }

        protected override bool ConvertValueForMember(object value, Type targetType, out object result)
        {
            return base.ConvertValueForMember(value, targetType, out result);
        }

        public override string GetTooltip() => noSave || modifiedContent.current || values.current.Length == 0 ? base.GetTooltip() : JoinParagraphs(base.GetTooltip(), $"Default Value: {values.Default[value.Default]}{(ExtraSettingsAPI.IsInWorld && save.IsSplit() ? $"\nGlobal Value: {values[false][value[false]]}" : "")}");

        void StoreToMember()
        {
            if (target == null || SkipMemberSet())
                return;
            if (target.MemberType == typeof(string))
                SetMemberValue(CurrentValue());
            else
                SetMemberValue(value.current);
        }

        public void SetValue(int newValue, bool local, SetFlags flags = SetFlags.All, bool force = false)
        {
            if (newValue < 0 || newValue > values[local].Length)
            {
                if (force)
                    newValue = 0;
                else
                    return;
            }
            flags = FilterFlags(flags, local);
            if (flags.HasFlag(SetFlags.Storage))
                value[local] = newValue;
            if (flags.HasFlag(SetFlags.Control) && combobox)
                combobox.SetValueWithoutNotify(newValue);
            if (flags.HasFlag(SetFlags.Member))
                StoreToMember();
        }

        public void SetValue(string newValue, bool local, SetFlags flags = SetFlags.All, bool force = false)
        {
            SetValue(values[local].IndexOf(newValue), local, flags, force);
        }

        public override void Create()
        {
            SetGameObject(Object.Instantiate(ExtraSettingsAPI.comboboxPrefab));
        }

        public override void Destroy()
        {
            base.Destroy();
            combobox = null;
        }

        protected override bool ShouldTryGenerateSave(bool local) => value.ShouldSave(local) || values.ShouldSave(local);
        public override JToken GenerateSaveJson(bool local)
        {
            var store = new JObject();
            store["value"] = value[local];
            if (modifiedContent[local])
            {
                JArray store2 = new JArray();
                foreach (string item in values[local])
                    store2.Add(item);
                store["values"] = store2;
            }
            return store;
        }

        public override void LoadSettings(bool local)
        {
            var saved = parent.GetSavedSettings(this, local);
            if (saved != null)
            {
                if (saved.Type == JTokenType.String)
                {
                    _resetContent(local);
                    SetValue((saved as JValue).Value.EnsureType<string>(), local);
                }
                else if (saved is JObject obj)
                {
                    if (obj.TryGetValue<JArray>("values", out var data))
                        setContent(data.Select(x => (x as JValue)?.Value?.EnsureType<string>() ?? "").ToArray(), local);
                    else
                        _resetContent(local);
                    value.Reset(local);
                    if (obj.TryGetValue<JValue>("value", out var datum))
                    {
                        if (datum.Type == JTokenType.Integer)
                            SetValue(datum.Value.EnsureType<int>(), local);
                        else if (datum.Type == JTokenType.String)
                            SetValue(datum.Value.EnsureType<string>(), local);
                    }
                }
            }
            else
                ResetValue(local);
            //index = values.IndexOf(value);
        }

        void setContent(string[] items, bool local)
        {
            modifiedContent[local] = true;
            values[local] = items;
            if (combobox && (!save.IsSplit() || local))
                combobox.options = items.Select(x => new Dropdown.OptionData(x)).ToList();
        }
        public void setContentKeepItem(string[] items, bool local)
        {
            var item = values[local].SafeGet(value[local]);
            setContent(items, local);
            SetValue(item, local, force: true);
        }
        public void setContentKeepIndex(string[] items, bool local)
        {
            var item = value[local];
            setContent(items, local);
            SetValue(item, local, force: true);
        }
        public void setContentAndItem(string[] items, string item, bool local)
        {
            setContent(items, local);
            SetValue(item, local, force: true);
        }
        public void setContentAndIndex(string[] items, int index, bool local)
        {
            setContent(items, local);
            SetValue(index, local, force: true);
        }

        public void addContent(string item, bool local)
        {
            modifiedContent[local] = true;
            var nValues = values.current;
            Array.Resize(ref nValues, nValues.Length + 1);
            nValues[nValues.Length - 1] = item;
            values[local] = nValues;
            if (combobox && (!save.IsSplit() || local))
                combobox.options.Add(new Dropdown.OptionData(item));
        }

        public string[] getContent(bool local)
        {
            return values.Dereferenced(local);
        }

        void _resetContent(bool local)
        {
            modifiedContent.Reset(local);
            values.Reset(local);
            if (combobox && (!save.IsSplit() || local))
                combobox.options = values[local].Select(x => new Dropdown.OptionData(x)).ToList();
        }

        public void resetContentKeepItem(bool local)
        {
            var item = values[local].SafeGet(value[local]);
            _resetContent(local);
            SetValue(item, local, force: true);
        }

        public void resetContentKeepIndex(bool local)
        {
            var item = value[local];
            _resetContent(local);
            SetValue(item, local, force: true);
        }

        public override void ResetValue(bool local)
        {
            _resetContent(local);
            value.Reset(local);
            SetValue(value[local], local, SetFlags.All ^ SetFlags.Storage);
        }

        public override string CurrentValue() => values.current.SafeGet(value.current, true, "");
        public override bool TrySetValue(string str)
        {
            var ind = values.current.IndexOf(str);
            if (ind == -1 && int.TryParse(str, out var r) && 0 <= r && r < values.current.Length)
                ind = (int)r;
            if (ind == -1)
                return false;
            SetValue(ind, ExtraSettingsAPI.IsInWorld);
            return true;
        }
        public override string[] PossibleValues() => values.current;

        public override void OnExitWorld()
        {
            if (!save.IsSplit())
                return;
            if (combobox)
                combobox.options = values.current.Select(x => new Dropdown.OptionData(x)).ToList();
            SetValue(value.current, false, SetFlags.All ^ SetFlags.Storage);
        }
    }
}