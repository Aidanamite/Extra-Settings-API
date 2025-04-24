using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public abstract class ModSetting
{
    public static bool useAlt = false;
    public string name { get; }
    public string nameText;
    public ModSettingContainer parent;
    public Text text = null;
    public MenuType access;
    public GameObject control { get; private set; } = null;
    public string section = null;
    Image backImage;

    public ModSetting(JSONObject source, ModSettingContainer parent)
    {
        name = source.GetField("name").str;
        if (source.HasField("text"))
            nameText = source.GetField("text").str;
        else
            nameText = name;
        access = MenuType.Both;
        if (source.HasField("access"))
            Enum.TryParse(source.GetField("access").str, true, out access);
        if (source.HasField("section"))
            section = source.GetField("section").str;
        this.parent = parent;
    }

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
        Section
    }

    public enum MenuType
    {
        Both,
        MainMenu,
        World,
        GlobalWorld,
        WorldCustom,
        GlobalCustom
    }

    public static Dictionary<SettingType, Type> matches = new Dictionary<SettingType, Type>
    {
        {SettingType.Checkbox, typeof(ModSetting_Checkbox) },
        {SettingType.Slider, typeof(ModSetting_Slider) },
        {SettingType.Combobox, typeof(ModSetting_Combobox) },
        {SettingType.Keybind, typeof(ModSetting_Keybind) },
        {SettingType.Button, typeof(ModSetting_Button) },
        {SettingType.Text, typeof(ModSetting_Text) },
        {SettingType.Data, typeof(ModSetting_Data) },
        {SettingType.Input, typeof(ModSetting_Input) },
        {SettingType.MultiButton, typeof(ModSetting_MultiButton) },
        {SettingType.Section, typeof(ModSetting_Section) }
    };

    public static ModSetting CreateSetting(JSONObject source, ModSettingContainer parent)
    {
        SettingType type;
        try
        {
            type = (SettingType)Enum.Parse(typeof(SettingType), source.GetField("type").str, true);
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
            return (ModSetting)matches[type].GetConstructor(new Type[] { typeof(JSONObject), typeof(ModSettingContainer) }).Invoke(new object[] { source, parent });
        }
        catch
        {
            throw new InvalidDataException("Failed to initialize a " + type + " for the " + parent.ModName + " mod");
        }
    }

    virtual public void SetGameObject(GameObject go)
    {
        control = go;
        control.name = parent.ModName + "." + name;
        control.transform.SetParent(ExtraSettingsAPI.newTabContent.transform, false);
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
    }

    public bool SetBackImage(bool state)
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
    virtual public void LoadSettings() { }
    virtual public void ResetValue() { }
    virtual public JSONObject GenerateSaveJson() { return new JSONObject(); }

    public bool ShouldShow(bool isOnMainMenu)
    {
        bool res;
        if (access == MenuType.Both)
            res = true;
        else if (isOnMainMenu)
        {
            if (access == MenuType.MainMenu)
                res = true;
            else
                res = access == MenuType.GlobalCustom && ExtraSettingsAPI.mods[parent.parent].GetSettingVisible(this);
        }
        else if (access == MenuType.WorldCustom && ExtraSettingsAPI.mods[parent.parent].GetSettingVisible(this))
            res = true;
        else
            res = access == MenuType.GlobalWorld || access == MenuType.World;
        if (res)
        {
            var s = section;
            while (s != null && parent.settings.TryGetValue(s,out var p))
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
}
