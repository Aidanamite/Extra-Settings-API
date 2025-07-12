using MeshCombineStudio;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Checkbox : ModSetting
    {
        public Toggle checkbox = null;
        public Value<bool> value;
        public ModSetting_Checkbox(JObject source, ModSettingContainer parent) : base(source, parent)
        {
            TrySetupMember(source);
            value = NewValue(source.TryGetValue("default", out var defaultField) ? defaultField.Type == JTokenType.Boolean && (defaultField as JValue).Value.EnsureType<bool>() : TryGetMemberValue(out var memValue) && memValue.TryConvert(out bool mem) ? mem : default);
            LoadSettings();
        }

        public void SetValue(bool newValue, bool local, SetFlags flags = SetFlags.All)
        {
            flags = FilterFlags(flags, local);
            if (flags.HasFlag(SetFlags.Storage))
                value[local] = newValue;
            if (flags.HasFlag(SetFlags.Control) && checkbox)
                checkbox.SetIsOnWithoutNotify(newValue);
            if (flags.HasFlag(SetFlags.Member))
                SetMemberValue(newValue);
        }

        protected override bool IsMemberTypeValid(Type type) => typeof(IConvertible).IsAssignableFrom(type);

        public override string GetTooltip() => noSave ? base.GetTooltip() : JoinParagraphs(base.GetTooltip(), $"Default Value: {value.Default}{(ExtraSettingsAPI.IsInWorld && save.IsSplit() ? $"\nGlobal Value: {value[false]}" : "")}");

        protected override bool DoRealtimeMemberCheck()
        {
            if (!TryGetMemberValue(out var mem))
                return false;
            if (!mem.TryConvert(out bool val) || value.current == val)
                return false;
            SetValue(val, ExtraSettingsAPI.IsInWorld, SetFlags.All ^ SetFlags.Member);
            return true;
        }

        public override void SetGameObject(GameObject go)
        {
            base.SetGameObject(go);
            checkbox = control.GetComponentInChildren<Toggle>(true);
            checkbox.isOn = value.current;
            checkbox.onValueChanged.AddListener(x => SetValue(x, ExtraSettingsAPI.IsInWorld, SetFlags.All ^ SetFlags.Control));
        }

        public override void Create()
        {
            SetGameObject(Object.Instantiate(ExtraSettingsAPI.checkboxPrefab));
        }

        public override void Destroy()
        {
            base.Destroy();
            checkbox = null;
        }

        protected override bool ShouldTryGenerateSave(bool local) => value.ShouldSave(local);
        public override JToken GenerateSaveJson(bool local) => value[local];

        public override void LoadSettings(bool local)
        {
            JToken saved = parent.GetSavedSettings(this, local);
            if (saved is JValue jval && jval.Value.TryConvert<bool>(out var v))
                SetValue(v, local);
            else
                ResetValue(local);
        }

        public override void ResetValue(bool local)
        {
            value.Reset(local);
            SetValue(value[local], local, SetFlags.All ^ SetFlags.Storage);
        }

        public override string CurrentValue() => value.ToString();
        public override bool TrySetValue(string str)
        {
            if (bool.TryParse(str, out var v))
                SetValue(v, ExtraSettingsAPI.IsInWorld);
            else if (str == "0")
                SetValue(false, ExtraSettingsAPI.IsInWorld);
            else if (str == "1")
                SetValue(true, ExtraSettingsAPI.IsInWorld);
            else
                return false;
            return true;
        }
        public override string[] PossibleValues() => new[] { "true", "false" };

        public override void OnExitWorld()
        {
            if (save.IsSplit())
                SetValue(value.current, false, SetFlags.All ^ SetFlags.Storage);
        }
    }
}