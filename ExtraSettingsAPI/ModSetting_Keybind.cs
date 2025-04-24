using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ModSetting_Keybind : ModSetting
{
    public KeybindInterface keybind = null;
    public Keybind defaultValue;
    Keybind _v;
    bool addedKey = false;
    static public Dictionary<string, Keybind> MyKeys = new Dictionary<string, Keybind>();
    public Keybind value
    {
        get
        {
            if (!keybind)
                return _v;
            return keybind.Keybind;
        }
        set
        {
            _v = value;
            if (keybind)
                keybind.Set(value);
        }
    }
    public ModSetting_Keybind(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
        KeyCode newKey = KeyCode.None;
        if (source.HasField("mainDefault"))
            Enum.TryParse(source.GetField("mainDefault").str, true, out newKey);
        KeyCode newKey2 = KeyCode.None;
        if (source.HasField("altDefault"))
            Enum.TryParse(source.GetField("altDefault").str, true, out newKey2);
        defaultValue = new Keybind(parent.IDName + "." + name, newKey, newKey2);
        if (access.NotWorldSave())
            LoadSettings();
        else
            value = new Keybind(defaultValue);
        addKeyBind();
    }

    public void SetValue(KeyCode key, bool main = true)
    {
        if (main)
            value.MainKey = key;
        else
            value.AltKey = key;
        value = value;
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        keybind = control.GetComponent<KeybindInterface>();
        Traverse keyTrav = Traverse.Create(keybind);
        keyTrav.Field("idenifier").SetValue(defaultValue.Identifier);
        keyTrav.Field("mainKeyDefault").SetValue(defaultValue.MainKey);
        keyTrav.Field("altKeyDefault").SetValue(defaultValue.AltKey);
        KeyConnection main = keyTrav.Field("mainKey").GetValue<KeyConnection>();
        KeyConnection alt = keyTrav.Field("altKey").GetValue<KeyConnection>();
        main.button = control.transform.FindChildRecursively("MainKey").GetComponent<Button>();
        alt.button = control.transform.FindChildRecursively("AltKey").GetComponent<Button>();
        main.text = main.button.GetComponentInChildren<Text>(true);
        alt.text = alt.button.GetComponentInChildren<Text>(true);
        keybind.Initialize(ExtraSettingsAPI.keybindColors);
        value = _v;
    }

    public override void Create()
    {
        SetGameObject(Object.Instantiate(ExtraSettingsAPI.keybindPrefab));
    }

    public override void Destroy()
    {
        removeKeyBind();
        base.Destroy();
        keybind = null;
    }

    public override JSONObject GenerateSaveJson()
    {
        JSONObject store = new JSONObject();
        store.AddField("main", (int)value.MainKey);
        store.AddField("alt", (int)value.AltKey);
        return store;
    }

    public void addKeyBind()
    {
        addedKey = MyKeys.TryAdd(value.Identifier, value);
    }

    public void removeKeyBind()
    {
        if (addedKey)
        {
            MyKeys.Remove(value.Identifier);
        }
    }

    public override void LoadSettings()
    {
        JSONObject saved = parent.GetSavedSettings(this);
        if (saved != null && !saved.IsNull)
        {
            KeyCode newKey = defaultValue.MainKey;
            if (saved.HasField("main"))
                newKey = saved.GetField("main").IsString ? (Enum.TryParse(saved.GetField("main").str, true, out KeyCode v) ? v : defaultValue.MainKey) : saved.GetField("main").IsNumber ? (KeyCode)(int)saved.GetField("main").n : defaultValue.MainKey;
            KeyCode newKey2 = defaultValue.AltKey;
            if (saved.HasField("alt"))
                newKey2 = saved.GetField("alt").IsString ? (Enum.TryParse(saved.GetField("alt").str, true, out KeyCode v) ? v : defaultValue.AltKey) : saved.GetField("alt").IsNumber ? (KeyCode)(int)saved.GetField("alt").n : defaultValue.AltKey;
            value = new Keybind(defaultValue.Identifier, newKey, newKey2);
        }
        else
            value = new Keybind(defaultValue);
    }

    public override void ResetValue()
    {
        SetValue(defaultValue.MainKey);
        SetValue(defaultValue.AltKey, false);
    }
}
