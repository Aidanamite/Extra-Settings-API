using System.Collections.Generic;

public class ModSetting_Data : ModSetting
{
    Dictionary<string, string> value;
    JSONObject defaultValue;
    public ModSetting_Data(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
        if (source == null || source.IsNull || !source.HasField("default"))
            defaultValue = new JSONObject(new Dictionary<string,string>());
        else
            defaultValue = source.GetField("default").Copy();
        if (access.NotWorldSave())
            LoadSettings();
        else
            ResetValue();
    }

    public void SetValue(string name, string newValue)
    {
        value[name] = newValue;
        if (access.NotWorldSave())
            ExtraSettingsAPI.generateSaveJson();
    }

    public void SetValues(Dictionary<string,string> values)
    {
	    value.Clear();
        foreach (var i in values)
	    value[i.Key] = i.Value;
        if (access.NotWorldSave())
            ExtraSettingsAPI.generateSaveJson();
    }

    public string getValue(string name)
    {
        if (value.ContainsKey(name))
            return value[name];
        return "";
    }

    public string[] getNames()
    {
        string[] names = new string[value.Keys.Count];
        value.Keys.CopyTo(names, 0);
        return names;
    }

    public override void LoadSettings()
    {
        JSONObject saved = parent.GetSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.ToDictionary();
        else
            ResetValue();
    }
    public override void Destroy() { }
    public override JSONObject GenerateSaveJson()
    {
        return new JSONObject(value);
    }
    public override void ResetValue()
    {
        value = defaultValue.ToDictionary();
    }
    public override void Create() { }
    public override void SetText(string newText) { }
}
