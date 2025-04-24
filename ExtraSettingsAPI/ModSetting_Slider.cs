using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ModSetting_Slider : ModSetting
{
    public Slider slider = null;
    public UISlider UIslider = null;
    public Text sliderText = null;
    public float defaultValue;
    public SliderType valueType;
    public float minValue;
    public float maxValue;
    public int rounding;
    public float value;
    public float roundValue
    {
        get
        {
            return (float)Math.Round(value, rounding + (int)valueType * 2);
        }
    }
    public ModSetting_Slider(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
        if (source.HasField("default"))
            defaultValue = source.GetField("default").n;
        else
            defaultValue = 0;
        valueType = SliderType.Number;
        minValue = 0;
        maxValue = 100;
        rounding = 0;
        if (source.HasField("range"))
        {
            JSONObject range = source.GetField("range");
            if (range.HasField("type"))
                if (Enum.TryParse(range.GetField("type").str, true, out valueType))
                    maxValue = 1;
                else
                    throw new InvalidCastException("Failed to parse slider type for " + name + " in " + parent.ModName);
            if (range.HasField("min"))
                minValue = range.GetField("min").n;
            if (range.HasField("max"))
                maxValue = range.GetField("max").n;
            if (range.HasField("decimals"))
                rounding = (int)range.GetField("decimals").n;
        }
        if (access.NotWorldSave())
            LoadSettings();
        else
            value = defaultValue;
    }

    override public void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        slider = control.GetComponentInChildren<Slider>(true);
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        SetValue(value);
        UIslider = control.GetComponentInChildren<UISlider>(true);
        UIslider.SliderEvent.RemoveAllListeners();
        slider.onValueChanged.RemoveAllListeners();
        sliderText = Traverse.Create(UIslider).Field("sliderTextComponent").GetValue<Text>();
        UIslider.name = "ESAPI_" + control.name + "_UISlider";
        slider.onValueChanged.AddListener(delegate { value = slider.value; });
    }

    public void SetValue(float newValue)
    {
        if (!slider)
        {
            if (newValue < minValue)
                value = minValue;
            else if (newValue > maxValue)
                value = maxValue;
            else
                value = newValue;
        }
        else
        {
            if (newValue < minValue)
                slider.value = minValue;
            else if (newValue > maxValue)
                slider.value = maxValue;
            else
                slider.value = newValue;
        }
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
    }
    public override void Update()
    {
        if (!UIslider.gameObject.activeInHierarchy)
            UIslider.enabled = false;
        sliderText.text = GetText();
    }
    public string GetText()
    {
        switch (valueType)
        {
            case SliderType.Percent:
                return roundValue * 100 + "%";
            case SliderType.Custom:
                return ExtraSettingsAPI.mods[parent.parent].GetSliderText(this);
            default:
                return roundValue.ToString();
        }
    }

    public override JSONObject GenerateSaveJson()
    {
        return new JSONObject(value);
    }
    public override void LoadSettings()
    {
        JSONObject saved = parent.GetSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.IsNumber ? saved.n : saved.IsString ? float.TryParse(saved.str,out var v) ? v : defaultValue : saved.IsBool ? saved.b ? 1 : 0 : defaultValue;
        else
            value = defaultValue;
    }

    public override void ResetValue()
    {
        SetValue(defaultValue);
    }

    public override string CurrentValue() => GetText();
    public override bool TrySetValue(string str)
    {
        if (!float.TryParse(str, out var v))
            return false;
        SetValue(v);
        return true;
    }
    public override string[] PossibleValues() => new[] { $"any decimal value from {minValue} to {maxValue}" };
}
