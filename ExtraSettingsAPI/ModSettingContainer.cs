using HarmonyLib;
using HMLLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static Mono.Security.X509.X520;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSettingContainer
    {
        public readonly string ModName;
        public readonly string IDName;
        public readonly Mod parent;
        public readonly object Target;
        public readonly Type TargetType;
        public JArray settingsJson;
        public Dictionary<string, ModSetting> settings = new Dictionary<string, ModSetting>();
        public List<ModSetting> allSettings = new List<ModSetting>();
        public GameObject title = null;
        public bool open = false;
        public ModSettingContainer(Mod mod, JArray settings, object target, Type targetType)
        {
            parent = mod;
            ModName = parent.modlistEntry.jsonmodinfo.name;
            IDName = parent.GetType().FullName;
            Target = target;
            TargetType = targetType;
            settingsJson = settings;
            var settingsToCreate = (IList<JToken>)settingsJson;
            try
            {
                var dyn = new Traverse(Target ?? TargetType).Method("ExtraSettingsAPI_CreateDynamicSettings").GetValue();
                if (dyn != null)
                {
                    IEnumerable<JObject> jsons = null;
                    if (dyn is IEnumerable<JObject> js)
                        jsons = js;
                    else if (dyn is IEnumerable<string> strs)
                        jsons = strs.Select(x => JObject.Parse(x));
                    else if (dyn is IEnumerable<JSONObject> ojs)
                        jsons = ojs.Select(x => JObject.Parse(x.ToString()));
                    if (jsons != null)
                        foreach (var j in jsons)
                            settingsToCreate.Add(j);
                    else
                        ExtraSettingsAPI.LogWarning($"The dynamic settings of {parent.modlistEntry.jsonmodinfo.name} are an unrecorgnised type. They should be given as an IEnumerable<> of strings, JObjects or JSONObjects. Got {dyn.GetType().FullName}");
                }
            }
            catch (Exception err)
            {
                ExtraSettingsAPI.LogError($"An error occuring while parsing the dynamic settings of {parent.modlistEntry.jsonmodinfo.name}\n{err.CleanInvoke()}");
            }
            foreach (JObject settingEntry in settingsToCreate)
                try
                {
                    var n = ModSetting.CreateSetting(settingEntry, this);
                    this.settings.TryAdd(n.name, n);
                    allSettings.Add(n);
                }
                catch (Exception err)
                {
                    ExtraSettingsAPI.LogError(err);
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

        public JToken GetSavedSettings(ModSetting setting, bool local)
        {
            if (setting.noSave)
                return null;
            JToken data;
            if (local && setting.save != ModSetting.SaveType.Global && TryGetSavedSettings(setting.name, true, out data))
                return data;
            if (!local && setting.save != ModSetting.SaveType.World && TryGetSavedSettings(setting.name, false, out data))
                return data;
            return null;
        }
        bool TryGetSavedSettings(string setting, bool local, out JToken result)
        {
            result = null;
            var dataStore = local ? ExtraSettingsAPI.LocalConfig : ExtraSettingsAPI.Config;
            if (dataStore == null || !dataStore.TryGetValue<JObject>("savedSettings", out var set) || !set.TryGetValue(IDName, out set) || !set.TryGetValue(setting, out var val))
                return false;
            result = val;
            return true;
        }

        public JObject GenerateSaveJson(bool isLocal = false)
        {
            var store = new JObject();
            foreach (var setting in allSettings)
            {
                var dat = setting.MaybeGenerateSave(isLocal);
                if (dat != null)
                    store[setting.name] = dat;
            }
            if (store.Count == 0)
                return null;
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
            foreach (var setting in allSettings)
                if (setting.control)
                    setting.control.SetActiveSafe(on && setting.ShouldShow(!ExtraSettingsAPI.IsInWorld));
            ExtraSettingsAPI.UpdateAllSettingBacks();
        }

        public void LoadLocal()
        {
            foreach (var setting in allSettings)
            {
                if (setting.save != ModSetting.SaveType.Global)
                    setting.LoadSettings(true);
                if (setting.resetSplitButton && setting.splitButton)
                    setting.splitButton.gameObject.SetActive(true);
            }
            ExtraSettingsAPI.GetCallerFromMod(parent).Call(EventCaller.EventTypes.WorldLoad);
        }

        public bool DoUpdateLate()
        {
            bool result = false;
            foreach (var s in allSettings)
                if ((ExtraSettingsAPI.IsInWorld || s.save != ModSetting.SaveType.World) && s.OnUpdateLate())
                    result = true;
            return result;
        }

        public void OnExitWorld()
        {
            foreach (var s in allSettings)
            {
                if (s.resetSplitButton && s.splitButton)
                    s.splitButton.gameObject.SetActive(false);
                s.OnExitWorld();
            }
        }
    }
}