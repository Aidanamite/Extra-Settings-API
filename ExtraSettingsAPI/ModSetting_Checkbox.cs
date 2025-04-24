using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ModSetting_Checkbox : ModSetting
{
    public Toggle checkbox = null;
    public bool defaultValue;
    public bool value;
    public ModSetting_Checkbox(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
        defaultValue = source.GetField("default").b;
        if (access.NotWorldSave())
            LoadSettings();
        else
            value = defaultValue;
    }

    public void SetValue(bool newValue)
    {
        if (!checkbox)
            value = newValue;
        else
            checkbox.isOn = newValue;
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        checkbox = control.GetComponentInChildren<Toggle>(true);
        SetValue(value);
        checkbox.onValueChanged.AddListener(delegate { value = checkbox.isOn; });
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

    public override JSONObject GenerateSaveJson()
    {
        return new JSONObject(value);
    }

    public override void LoadSettings()
    {
        JSONObject saved = parent.GetSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.IsBool ? saved.b : saved.IsNumber ? saved.n != 0 : saved.IsString ? bool.TryParse(saved.str, out var v) ? v : defaultValue : defaultValue;
        else
            value = defaultValue;
    }

    public override void ResetValue()
    {
        SetValue(defaultValue);
    }

    public override string CurrentValue() => value.ToString();
    public override bool TrySetValue(string str)
    {
        if (bool.TryParse(str, out var v))
            SetValue(v);
        else if (str == "0")
            SetValue(false);
        else if (str == "1")
            SetValue(true);
        else
            return false;
        return true;
    }
    public override string[] PossibleValues() => new[] { "true", "false" };
}
