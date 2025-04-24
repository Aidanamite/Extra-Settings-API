using HMLLibrary;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ModSettingContainer
{
    public string ModName { get; }
    public string IDName { get; }
    public Mod parent { get; }
    public JSONObject settingsJson;
    public Dictionary<string, ModSetting> settings = new Dictionary<string, ModSetting>();
    public List<ModSetting> allSettings = new List<ModSetting>();
    public GameObject title = null;
    public bool open = false;
    public ModSettingContainer(Mod mod, JSONObject settings)
    {
        parent = mod;
        ModName = parent.modlistEntry.jsonmodinfo.name;
        IDName = parent.GetType().FullName;
        settingsJson = settings;
        if (!settingsJson.IsArray)
            throw new FormatException("Mod settings in " + ModName + " are not formatted correctly");
        foreach (JSONObject settingEntry in settingsJson.list)
            try
            {
                var n = ModSetting.CreateSetting(settingEntry, this);
                this.settings.TryAdd(n.name, n);
                allSettings.Add(n);
            }
            catch (Exception err)
            {
                ExtraSettingsAPI.Log(err);
            }
    }

    public void Create()
    {
        title = Object.Instantiate(ExtraSettingsAPI.titlePrefab);
        title.name = IDName + "Title";
        title.transform.SetParent(ExtraSettingsAPI.newTabContent.transform, false);
        Text text = title.GetComponentInChildren<Text>(true);
        text.text = "-------- " + ModName;
        text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
        foreach (var setting in allSettings)
            setting.Create();
        title.GetComponentInChildren<Toggle>(true).isOn = open;
        ToggleSettings();
        title.GetComponentInChildren<Toggle>(true).onValueChanged.AddListener(ToggleSettings);
    }

    public void Destroy()
    {
        if (title)
        {
            Object.Destroy(title);
            title = null;
        }
        foreach (var setting in allSettings)
            setting.Destroy();
    }

    public JSONObject GetSavedSettings(ModSetting setting)
    {
        JSONObject dataStore = setting.access.NotWorldSave() ? ExtraSettingsAPI.Config : ExtraSettingsAPI.LocalConfig;
        if (dataStore == null || dataStore.IsNull)
            return null;
        JSONObject set = dataStore.GetField("savedSettings");
        if (set == null || set.IsNull)
            return null;
        set = set.GetField(IDName);
        if (set == null || set.IsNull)
            return null;
        return set.GetField(setting.name);
    }

    public JSONObject GenerateSaveJson(bool isLocal = false)
    {
        JSONObject store = new JSONObject();
        foreach (var setting in allSettings)
            if (setting.access.NotWorldSave() != isLocal)
            {
                JSONObject dat = setting.GenerateSaveJson();
                if (dat != null)
                    store.AddField(setting.name, dat);
            }
        return store;
    }

    public void ToggleSettings()
    {
        if (title)
            ToggleSettings(title.GetComponentInChildren<Toggle>(true).isOn);
    }

    public void ToggleSettings(bool on)
    {
        open = on;
        var isOnMainmenu = !RAPI.GetLocalPlayer();
        foreach (var setting in allSettings)
            if (setting.control)
                setting.control.SetActive(on && setting.ShouldShow(isOnMainmenu));
        ExtraSettingsAPI.UpdateAllSettingBacks();
    }

    public void LoadLocal()
    {
        foreach (var setting in allSettings)
            if (!setting.access.NotWorldSave())
                setting.LoadSettings();
    }
}