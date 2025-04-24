using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ModSetting_Input : ModSetting
{
    public InputField input = null;
    public string defaultValue;
    public string value;
    public int maxLength;
    public InputField.ContentType contentType;
    public ModSetting_Input(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
        if (source.HasField("default"))
            defaultValue = source.GetField("default").str;
        else
            defaultValue = "";
        if (source.HasField("max"))
            maxLength = (int)source.GetField("max").n;
        else
            maxLength = 0;
        if (source.HasField("mode"))
            Enum.TryParse(source.GetField("mode").str, true, out contentType);
        if (access.NotWorldSave())
            LoadSettings();
        else
            value = defaultValue;
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        input = control.GetComponentInChildren<InputField>(true);
        input.characterLimit = maxLength > 0 ? maxLength : int.MaxValue;
        input.contentType = contentType;
        input.onEndEdit.AddListener((t) => { value = t; });
        SetValue(value);
    }

    public void SetValue(string newValue)
    {
        if (input)
            input.text = newValue;
        value = newValue;
    }

    public override void Create()
    {
        SetGameObject(Object.Instantiate(ExtraSettingsAPI.inputPrefab));
    }

    public override void Destroy()
    {
        base.Destroy();
        input = null;
    }

    public override JSONObject GenerateSaveJson()
    {
        return JSONObject.StringObject(value);
    }
    public override void LoadSettings()
    {
        JSONObject saved = parent.GetSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.IsNumber ? saved.n.ToString() : saved.IsString ? saved.str : saved.IsBool ? saved.b.ToString() : defaultValue;
        else
            value = defaultValue;
    }

    public override void ResetValue()
    {
        SetValue(defaultValue);
    }

    public override string CurrentValue() => value;
    public override bool TrySetValue(string str)
    {
        if (str.Length > maxLength)
            return false;
        if (contentType == InputField.ContentType.Alphanumeric)
        {
            foreach (var c in str)
                if (!char.IsLetterOrDigit(c))
                    return false;
        }
        else if (contentType == InputField.ContentType.DecimalNumber)
        {
            var dec = false;
            var exp = false;
            var min = false;
            var num = false;
            foreach (var c in str)
                if (char.IsDigit(c))
                {
                    num = true;
                    min = true;
                }
                else if (!exp && !dec && c == '.')
                {
                    dec = true;
                    min = true;
                }
                else if (!exp && num && c == 'e')
                {
                    exp = true;
                    min = false;
                    num = false;
                }
                else if (!min && c == '-')
                    min = true;
                else
                    return false;
            return num;
        }
        else if (contentType == InputField.ContentType.IntegerNumber)
        {
            foreach (var c in str)
                if (!char.IsDigit(c))
                    return false;
        }
        else if (contentType == InputField.ContentType.EmailAddress)
        {
            var oth = false;
            var at = false;
            foreach (var c in str)
                if ("!#$%&'*+-/=?^_`{|}~ \n".IndexOf(c) != -1)
                    return false;
                else if (c == '.')
                {
                    if (!oth)
                        return false;
                    oth = false;
                }
                else if (c == '@')
                {
                    if (!oth || at)
                        return false;
                    oth = false;
                    at = true;
                }
                else
                    oth = true;
            return oth;
        }
        else if (contentType == InputField.ContentType.Name)
        {
            var spa = true;
            foreach (var c in str)
                if (!spa && c == ' ')
                    spa = true;
                else if (!char.IsLetter(c) && c != '\'' && c != '-')
                    return false;
                else
                    spa = false;
        }
        return true;
    }
    public override string[] PossibleValues() => new[] { $"any {contentType.ToString().CamelToWords().ToLowerInvariant()} text" };
    public override string DisplayType() => contentType.ToString().CamelToWords() + " " + base.DisplayType();
}
