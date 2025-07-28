using HarmonyLib;
using Newtonsoft.Json.Linq;
using PrivateAccess;
using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Slider : ModSetting
    {
        public Slider slider = null;
        public UISlider UIslider = null;
        public Text sliderText = null;
        public SliderType valueType = SliderType.Number;
        public float minValue = 0;
        public float maxValue = 100;
        public int rounding = 0;
        public Rounding roundMode = Rounding.Nearest;
        string formatter;
        public Value<float> value;
        public bool roundMember;
        public float roundValue
        {
            get
            {
                return DoRound(value.current);
            }
        }
        double roundingFactor;
        float DoRound(float value) => (float)((value * roundingFactor).Round(roundMode) / roundingFactor);
        public ModSetting_Slider(JObject source, ModSettingContainer parent) : base(source, parent)
        {
            if (source.TryGetValue<JObject>("range", out var range))
            {
                if (range.TryGetValue<JValue>("type", out var typeField, JTokenType.String))
                    if (Enum.TryParse(typeField.Value?.EnsureType<string>(), true, out valueType))
                        maxValue = 1;
                    else
                        throw new InvalidCastException("Failed to parse slider type for " + name + " in " + parent.ModName);
                if (range.TryGetValue<JValue>("min", out var field) && field.Value.TryConvert(out float v))
                    minValue = v;
                if (range.TryGetValue("max", out field) && field.Value.TryConvert(out v))
                    maxValue = v;
                if (range.TryGetValue("decimals", out field) && field.Value.TryConvert(out int v2))
                    rounding = v2;
                if (range.TryGetValue("roundMode", out field, JTokenType.String))
                    if (Enum.TryParse(field.Value?.EnsureType<string>(), true, out Rounding roundParse))
                        roundMode = roundParse;
                    else
                        ExtraSettingsAPI.LogError("Failed to parse rounding mode for " + name + " in " + parent.ModName);
                if (range.TryGetValue("format", out field, JTokenType.String))
                    formatter = field.Value?.EnsureType<string>();
            }
            if (formatter == null)
            {
                formatter = "#,0";
                if (rounding > 0)
                    formatter += "." + new string('0',rounding);
                if (valueType == SliderType.Percent)
                    formatter += "%";
            }
            roundingFactor = Math.Pow(10, rounding + (valueType == SliderType.Percent ? 2 : 0));
            TrySetupMember(source);
            if (target != null && source.TryGetValue<JValue>("memberRound", out var roundField, JTokenType.Boolean))
                roundMember = roundField.Value.EnsureType<bool>();
            var defaultValue = 0f;
            if (source.TryGetValue<JValue>("default", out var defaultField) && defaultField.Value.TryConvert(out float dv))
                defaultValue = dv;
            else if (TryGetMemberValue(out var mem) && mem.TryConvert(out float memValue))
                defaultValue = memValue;
            if (defaultValue < minValue)
                defaultValue = minValue;
            else if (defaultValue > maxValue)
                defaultValue = maxValue;
            value = NewValue(defaultValue);
            LoadSettings();
        }

        protected override bool IsMemberTypeValid(Type type) => type.IsNumber();

        public override string GetTooltip() => noSave ? base.GetTooltip() : JoinParagraphs(base.GetTooltip(), $"Default Value: {GetText(value.Default)}{(ExtraSettingsAPI.IsInWorld && save.IsSplit() ? $"\nGlobal Value: {value[false]}" : "")}{(ExtraSettingsAPI.IsInWorld && save.IsSplit() ? $"\nGlobal Value: {GetText(value[false])}" : "")}");

        protected override bool DoRealtimeMemberCheck()
        {
            if (!TryGetMemberValue(out var mem))
                return false;
            object curr = roundMember ? roundValue : value.current;
            if (TryEnsureValueForMember(ref curr, out _) && !Equals(mem, curr) && mem.TryConvert(out float memValue))
            {
                SetValue(memValue, ExtraSettingsAPI.IsInWorld, SetFlags.All ^ SetFlags.Member);
                return true;
            }
            return false;
        }

        override public void SetGameObject(GameObject go)
        {
            base.SetGameObject(go);
            slider = control.GetComponentInChildren<Slider>(true);
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value.current;
            UIslider = control.GetComponentInChildren<UISlider>(true);
            UIslider.gameObject.AddComponent<Component>().owner = this;
            UIslider.SliderEvent.RemoveAllListeners();
            slider.onValueChanged.RemoveAllListeners();
            sliderText = UIslider.TextComponent();
            UIslider.name = "ESAPI_" + control.name + "_UISlider";
            UIslider.enabled = false;
            slider.onValueChanged.AddListener(x => SetValue(x, ExtraSettingsAPI.IsInWorld, SetFlags.All ^ SetFlags.Control));
        }

        public void SetValue(float newValue, bool local, SetFlags flags = SetFlags.All)
        {
            flags = FilterFlags(flags, local);
            if (newValue < minValue)
                newValue = minValue;
            else if (newValue > maxValue)
                newValue = maxValue;
            if (flags.HasFlag(SetFlags.Storage))
                value[local] = newValue;
            if (flags.HasFlag(SetFlags.Control) && slider)
                slider.SetValueWithoutNotify(newValue);
            if (flags.HasFlag(SetFlags.Member))
                SetMemberValue(roundMember ? DoRound(newValue) : newValue);
        }

        public enum SliderType
        {
            Number,
            Percent,
            Custom
        }
        public override void Create()
        {
            SetGameObject(Object.Instantiate(ExtraSettingsAPI.sliderPrefab));
        }
        public override void Destroy()
        {
            base.Destroy();
            slider = null;
            last = float.NaN;
        }
        float last = float.NaN;
        public override void Update()
        {
            if (UIslider.Value != last)
            {
                last = UIslider.Value;
                sliderText.text = GetText();
            }
        }
        public string GetText() => GetText(value.current);
        public string GetText(float value) => valueType == SliderType.Custom ? ExtraSettingsAPI.mods[parent.parent].GetSliderText(this,value) : value.ToString(formatter);

        protected override bool ShouldTryGenerateSave(bool local) => value.ShouldSave(local);
        public override JToken GenerateSaveJson(bool local) => value[local];

        public override void LoadSettings(bool local)
        {
            JToken saved = parent.GetSavedSettings(this, local);
            if (saved is JValue val && val.TryConvert(out float v))
                SetValue(v, local);
            else
                ResetValue(local);
        }

        public override void ResetValue(bool local)
        {
            value.Reset(local);
            SetValue(value[local], local, SetFlags.All ^ SetFlags.Storage);
        }

        public override string CurrentValue() => GetText();
        public override bool TrySetValue(string str)
        {
            if (!float.TryParse(str, out var v))
                return false;
            SetValue(v, ExtraSettingsAPI.IsInWorld);
            return true;
        }
        public override string[] PossibleValues() => new[] { $"any decimal value from {minValue} to {maxValue}" };

        public class Component : MonoBehaviour
        {
            public ModSetting_Slider owner;
            public void Update()
            {
                owner.Update();
            }
        }

        public override void OnExitWorld()
        {
            if (save.IsSplit())
                SetValue(value.current, false, SetFlags.All ^ SetFlags.Storage);
        }
    }
}