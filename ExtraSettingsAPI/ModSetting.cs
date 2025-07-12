using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public abstract class ModSetting
    {
        public static bool useAlt = false;
        public readonly string name;
        public string nameText;
        public ModSettingContainer parent;
        public Text text = null;
        public readonly MenuType access;
        public readonly SaveType save;
        public GameObject control { get; private set; } = null;
        public string section = null;
        public MemberRef target = null;
        public bool hasTargetFallback = false;
        public object targetFallback;
        public bool realTimeTarget = false;
        public bool noSave = false;
        public string tooltip;
        public bool resetSplitButton = false;
        public bool resetButton = false;
        public Button splitButton;
        Image backImage;

        public ModSetting(JObject source, ModSettingContainer parent)
        {
            name = source.GetValue<JValue>("name", JTokenType.String)?.Value?.EnsureType<string>() ?? "";
            if (source.TryGetValue<JValue>("text", out var textField, JTokenType.String))
                nameText = textField.Value.EnsureType<string>();
            else
                nameText = name;
            access = MenuType.Both;
            if (source.TryGetValue<JValue>("access", out var accessField, JTokenType.String))
                Enum.TryParse(accessField.Value.EnsureType<string>(), true, out access);
            save = access.GetSaveType();
            if (source.TryGetValue<JValue>("section", out var sectionField, JTokenType.String))
                section = sectionField.Value.EnsureType<string>();
            this.parent = parent;
            if (source.TryGetValue<JValue>("dontSave", out var saveField, JTokenType.Boolean))
                noSave = saveField.Value.EnsureType<bool>();
            if (source.TryGetValue("tooltip", out var tipField))
            {
                if (tipField.Type == JTokenType.String)
                    tooltip = (tipField as JValue).Value.EnsureType<string>();
                else if (tipField.Type == JTokenType.Array)
                    tooltip = (tipField as JArray).Join(x => x is JValue val && val.Value != null ? val.Value.ToString() : "", "\n");
            }
            if (save.IsSplit() && source.TryGetValue<JValue>("splitResetButton", out var splitResetField, JTokenType.Boolean))
                resetSplitButton = splitResetField.Value.EnsureType<bool>();
            if (source.TryGetValue<JValue>("resetButton", out var resetField, JTokenType.Boolean))
                resetButton = resetField.Value.EnsureType<bool>();
        }
        protected void TrySetupMember(JObject source)
        {
            if (!source.TryGetValue<JValue>("member", out var memberField, JTokenType.String) || string.IsNullOrWhiteSpace(memberField.Value.EnsureType<string>()))
                return;
            MemberRef member;
            try
            {
                member = parent.Target != null ? MemberRef.MakeRef(parent.Target, memberField.Value.EnsureType<string>()) : MemberRef.MakeRef(parent.TargetType, memberField.Value.EnsureType<string>());
            }
            catch (Exception e)
            {
                ExtraSettingsAPI.LogError($"An error occured trying setup member access for [{parent.ModName}] {name}\n{e.CleanInvoke()}");
                return;
            }
            if (!member.CanSet)
            {
                ExtraSettingsAPI.LogError($"Member target for [{parent.ModName}] {name} cannot be set");
                return;
            }
            if (!IsMemberTypeValid(member.MemberType))
            {
                ExtraSettingsAPI.LogError("Member type " + member.MemberType.FullName + " cannot be used for setting " + name);
                return;
            }
            target = member;
            if (source.TryGetValue<JValue>("memberRealtime", out var realtimeField, JTokenType.Boolean) && realtimeField.Value.EnsureType<bool>())
            {
                if (target.CanGet)
                    realTimeTarget = true;
                else
                    ExtraSettingsAPI.LogWarning($"Cannot enable realtime member access for [{parent.ModName}] {name}. Cannot get value of member");
            }
            if (source.TryGetValue("memberFallback", out var fallbackField))
            {
                object fValue = fallbackField;
                if (fallbackField.Type == JTokenType.String || fallbackField.Type == JTokenType.Boolean || fallbackField.Type == JTokenType.Integer || fallbackField.Type == JTokenType.Float)
                    fValue = (fallbackField as JValue).Value;
                else if (fallbackField.Type == JTokenType.Null)
                    fValue = null;
                if (fValue == null)
                {
                    if (target.MemberType.IsValueType)
                        fValue = Activator.CreateInstance(target.MemberType);
                }
                else if (!target.MemberType.IsInstanceOfType(fValue))
                {
                    if (ConvertValueForMember(fValue, target.MemberType, out var cValue))
                        fValue = cValue;
                    else
                    {
                        ExtraSettingsAPI.LogError($"A member fallback value was specified for [{parent.ModName}] {name} but the control did not know how convert the provided value to the member type");
                        goto skipFallback;
                    }
                }
                targetFallback = fValue;
                hasTargetFallback = true;
            skipFallback:;
            }
        }
        public void SetMemberValue(object value)
        {
            if (target == null || SkipMemberSet())
                return;
            if (!TryEnsureValueForMember(ref value, out var error))
            {
                ExtraSettingsAPI.LogWarning($"Could not convert value {(value != null ? "[" + value.ToString() + "] (" + value.GetType().FullName + ")" : "null")} for member of [{parent.ModName}] {name}{(error != null ? "\n" + error : "")}");
                return;
            }
            try
            {
                target.IndirectValue = value;
            }
            catch (Exception e)
            {
                ExtraSettingsAPI.LogError($"An error occured attempting to set the value for [{parent.ModName}] {name}\n{e}");
            }
        }
        public bool TryEnsureValueForMember(ref object value, out Exception error) => TryEnsureValueForType(ref value, target.MemberType, out error);
        public bool TryEnsureValueForType(ref object value, Type targetType, out Exception error)
        {
            if (!_EnsureMemberConvert(ref value, targetType, out error))
            {
                if (!hasTargetFallback)
                    return false;
                value = targetFallback;
            }
            return true;
        }
        bool _EnsureMemberConvert(ref object value, Type targetType, out Exception error)
        {
            error = null;
            if (value.IsAssignableTo(targetType))
                return true;
            object nValue;
            try
            {
                if (!ConvertValueForMember(value, targetType, out nValue))
                    return false;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
            value = nValue;
            return true;
        }
        protected virtual bool ConvertValueForMember(object value, Type targetType, out object result) => value.TryConvert(targetType, out result);
        public bool TryGetMemberValue(out object value)
        {
            if (target == null || !target.CanGet)
            {
                value = null;
                return false;
            }
            try
            {
                value = target.IndirectValue;
                return true;
            }
            catch (Exception e)
            {
                ExtraSettingsAPI.LogError($"An error occured attempting to get the value for [{parent.ModName}] {name}\n{e}");
                value = null;
                return false;
            }
        }

        int RealtimeCheck = 0;
        public bool SkipMemberSet() => RealtimeCheck > 0;
        public virtual bool OnUpdateLate()
        {
            try
            {
                RealtimeCheck++;
                if (realTimeTarget)
                    return DoRealtimeMemberCheck() && ShouldSaveOnSet();
            }
            finally
            {
                RealtimeCheck--;
            }
            return false;
        }
        protected virtual bool DoRealtimeMemberCheck() => false;

        public bool ShouldSaveOnSet() => !noSave && (save == SaveType.Global || (save.IsSplit() && !ExtraSettingsAPI.IsInWorld));

        protected virtual bool IsMemberTypeValid(Type type) => throw new NotSupportedException("Member refernces not supported by " + DisplayType());

        public virtual string GetTooltip() => tooltip;
        protected static string JoinParagraphs(params string[] parts) => parts.Join(delimeter: "\n\n", filter: x => !string.IsNullOrWhiteSpace(x));

        public enum SettingType
        {
            Checkbox,
            Slider,
            Combobox,
            Keybind,
            Button,
            Text,
            Data,
            Input,
            MultiButton,
            Section,
            Seperator
        }

        public enum MenuType
        {
            Both,
            MainMenu,
            World,
            GlobalWorld,
            WorldCustom,
            GlobalCustom,
            Split,
            SplitCustom,
            SplitStrong,
            SplitStrongCustom
        }

        public enum SaveType
        {
            Global,
            World,
            Split,
            SplitStrong
        }
        [Flags]
        public enum SetFlags
        {
            None,
            Storage = 1,
            Control = 2,
            Member = 4,
            All = Storage | Control | Member
        }

        public static Dictionary<SettingType, Type> matches = new Dictionary<SettingType, Type>
        {
            { SettingType.Checkbox, typeof(ModSetting_Checkbox) },
            { SettingType.Slider, typeof(ModSetting_Slider) },
            { SettingType.Combobox, typeof(ModSetting_Combobox) },
            { SettingType.Keybind, typeof(ModSetting_Keybind) },
            { SettingType.Button, typeof(ModSetting_Button) },
            { SettingType.Text, typeof(ModSetting_Text) },
            { SettingType.Data, typeof(ModSetting_Data) },
            { SettingType.Input, typeof(ModSetting_Input) },
            { SettingType.MultiButton, typeof(ModSetting_MultiButton) },
            { SettingType.Section, typeof(ModSetting_Section) },
            { SettingType.Seperator, typeof(ModSetting_Seperator) }
        };

        public static ModSetting CreateSetting(JObject source, ModSettingContainer parent)
        {
            SettingType type;
            try
            {
                type = (SettingType)Enum.Parse(typeof(SettingType), source.GetValue<JValue>("type", JTokenType.String).Value.EnsureType<string>(), true);
            }
            catch (Exception err)
            {
                if (err is ArgumentException)
                    throw new FormatException("Provided type string is not valid");
                if (err is NullReferenceException)
                    throw new FormatException("Failed to get type string of setting");
                throw err;
            }
            try
            {
                return (ModSetting)matches[type].GetConstructor(new Type[] { typeof(JObject), typeof(ModSettingContainer) }).Invoke(new object[] { source, parent });
            }
            catch (Exception e)
            {
                throw new InvalidDataException("Failed to initialize a " + type + " for the " + parent.ModName + " mod", e.CleanInvoke());
            }
        }

        virtual public void SetGameObject(GameObject go)
        {
            control = go;
            control.name = parent.ModName + "." + name;
            control.transform.SetParent(ExtraSettingsAPI.newTabContent.transform, false);
            foreach (var hover in control.GetComponentsInChildren<SettingHoverDetector>(true))
                hover.owner = this;
            text = control.GetComponentInChildren<Text>(true);
            if (text)
            {
                text.text = nameText;
                if (!(this is ModSetting_Button))
                {
                    text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
                }
            }
            backImage = control.GetComponent<Image>() ?? control.GetComponentInChildren<Image>(true);
            if (resetSplitButton)
            {
                (splitButton = Object.Instantiate(ExtraSettingsAPI.resetSplitButtonPrefab, control.transform)).onClick.AddListener(() => ResetValue(true));
                if (ExtraSettingsAPI.IsInWorld)
                    splitButton.gameObject.SetActive(true);
            }
            if (resetButton)
                Object.Instantiate(ExtraSettingsAPI.resetButtonPrefab, control.transform).onClick.AddListener(() => { ResetValue(false); ResetValue(true); });
        }

        public virtual bool SetBackImage(bool state)
        {
            if (!backImage)
                return false;
            backImage.enabled = state;
            return true;
        }

        virtual public void SetText(string newText)
        {
            nameText = newText;
            if (text)
            {
                text.text = newText;
                if (!(this is ModSetting_Button))
                {
                    text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
                }
            }
        }

        virtual public void Create() { }
        virtual public void Destroy()
        {
            Object.Destroy(control);
            control = null;
        }
        virtual public void Update() { }
        public void LoadSettings()
        {
            if (save != SaveType.World)
                LoadSettings(false);
            if (save != SaveType.Global)
                LoadSettings(true);
        }
        virtual public void LoadSettings(bool local) { }
        virtual public void ResetValue(bool local) { }
        public void ResetValue()
        {
            if (save.IsSplit())
            {
                ResetValue(false);
                ResetValue(true);
            }
            else
                ResetValue(save == SaveType.World);
        }
        protected virtual bool ShouldTryGenerateSave(bool local) => save != (local ? SaveType.Global : SaveType.World);
        public JToken MaybeGenerateSave(bool local)
        {
            if (noSave)
                return null;
            if (ShouldTryGenerateSave(local))
                return GenerateSaveJson(local);
            return null;
        }
        virtual public JToken GenerateSaveJson(bool local) => null;

        public bool ShouldShow(bool isOnMainMenu)
        {
            bool res;
            if (access == MenuType.Both || access == MenuType.Split)
                res = true;
            else if (access == MenuType.GlobalCustom || access == MenuType.SplitCustom)
                res = ExtraSettingsAPI.mods[parent.parent].GetSettingVisible(this);
            else if (isOnMainMenu)
                res = access == MenuType.MainMenu;
            else if (access == MenuType.WorldCustom && ExtraSettingsAPI.mods[parent.parent].GetSettingVisible(this))
                res = true;
            else
                res = access == MenuType.GlobalWorld || access == MenuType.World;
            if (res)
            {
                var s = section;
                while (s != null && parent.settings.TryGetValue(s, out var p))
                {
                    if (p is ModSetting_Section m)
                    {
                        if (!m.open)
                            return false;
                        s = p.section;
                        continue;
                    }
                    foreach (var i in parent.allSettings)
                        if (i.name == section && i is ModSetting_Section m2)
                        {
                            if (!m2.open)
                                return false;
                            s = i.section;
                            continue;
                        }
                    break;
                }
            }
            return res;
        }

        public virtual bool TrySetValue(string str) => false;
        public virtual string[] PossibleValues() => new string[0];
        public virtual string CurrentValue() => "";
        public virtual string DisplayType() => GetType().Name.Remove(0, 11);


        protected Value<T> NewValue<T>(T Default = default) => new Value<T>(Default, save);
        public struct Value<T>
        {
            SaveType save;
            T defaultValue;
            T value;
            T splitValue;
            bool hasSplit;

            public Value(T Default, SaveType Save)
            {
                save = Save;
                defaultValue = Default;
                value = Clone(defaultValue);
                splitValue = save.IsSplit() ? Clone(defaultValue) : default;
                hasSplit = save == SaveType.SplitStrong;
            }
            public bool HasSplit => hasSplit;
            public bool ShouldSave(bool local) => save == SaveType.World ? local : (!local || save != SaveType.Split || hasSplit);
            public T current { get => this[ExtraSettingsAPI.IsInWorld]; set => this[ExtraSettingsAPI.IsInWorld] = value; }
            public T this[bool local]
            {
                get => hasSplit && local ? splitValue : value;
                set
                {
                    if (save.IsSplit() && local)
                    {
                        hasSplit = true;
                        splitValue = value;
                    }
                    else
                    {
                        this.value = value;
                        if (save.IsSplit() && !hasSplit)
                            splitValue = Clone(this.value);
                    }
                }
            }
            public void Reset(bool local)
            {
                if (save.IsSplit() && local)
                {
                    splitValue = Clone(value);
                    hasSplit = save == SaveType.SplitStrong;
                }
                else
                {
                    value = Clone(defaultValue);
                    if (save.IsSplit() && !hasSplit)
                        splitValue = Clone(value);
                }
            }
            static T Clone(T value)
            {
                if (value != null && value.GetType().IsByRef)
                {
                    if (value is ICloneable clonable)
                        return (T)clonable.Clone();
                    if (value is ISerializable)
                    {
                        var formatter = new BinaryFormatter();
                        var stream = new MemoryStream();
                        formatter.Serialize(stream, value);
                        stream.Seek(0, SeekOrigin.Begin);
                        return (T)formatter.Deserialize(stream);
                    }
                    throw new NotSupportedException();
                }
                return value;
            }

            public T Default => Clone(defaultValue);
            public T Dereferenced(bool local) => Clone(this[local]);
        }

        protected SetFlags FilterFlags(SetFlags flags, bool local)
        {
            if (save.IsSplit() && !local && ExtraSettingsAPI.IsInWorld)
                return flags & SetFlags.Storage;
            return flags;
        }

        public virtual void OnExitWorld() { }
    }
}