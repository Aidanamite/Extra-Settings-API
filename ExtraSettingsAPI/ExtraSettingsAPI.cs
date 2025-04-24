using HarmonyLib;
using HMLLibrary;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using RaftModLoader;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

public class ExtraSettingsAPI : Mod
{
    static public JsonModInfo modInfo;
    static public ExtraSettingsAPI instance;
    static string configPath;
    public static string worldConfigPath
    {
        get
        {
            string date = new DateTime(SaveAndLoad.WorldToLoad.lastPlayedDateTicks).ToString(SaveAndLoad.dateTimeFormattingSaveFile);
            string text = Path.Combine(SaveAndLoad.WorldPath, SaveAndLoad.WorldToLoad.name, date);
            if (Directory.Exists(text))
                return Path.Combine(text, modInfo.name + ".json");
            return Path.Combine(SaveAndLoad.WorldPath, SaveAndLoad.WorldToLoad.name, date + SaveAndLoad.latestStringNameEnding, modInfo.name + ".json");
        }
    }
    public static JSONObject Config;
    public static JSONObject LocalConfig;
    public static Settings settingsController => ComponentManager<Settings>.Value;
    //static Traverse settingsTraverse = Traverse.Create(settingsController);
    //static GameObject OptionMenuContainer = settingsTraverse.Field("optionsCanvas").GetValue<GameObject>().transform.FindChildRecursively("OptionMenuParent").gameObject;
    //static TabGroup tabGroup = OptionMenuContainer.GetComponentInChildren<TabGroup>();
    static GameObject newTabBody;
    static GameObject newTabButtonObj;
    static TabButton newTabButton;
    public static RectTransform newTabContent;
    static string newName = "Mods";
    public static Sprite dividerSprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == "Divider");
    public static Dictionary<Mod, ModSettingContainer> modSettings;
    public static Dictionary<Mod, EventCaller> mods;
    public static bool needsToCreateSettings;
    public Harmony harmony;
    public static RectTransform prefabParent;
    public static GameObject sliderPrefab;
    public static GameObject checkboxPrefab;
    public static GameObject comboboxPrefab;
    public static GameObject keybindPrefab;
    public static GameObject buttonPrefab;
    public static GameObject textPrefab;
    public static GameObject titlePrefab;
    public static GameObject sectionPrefab;
    public static GameObject inputPrefab;
    public static GameObject multibuttonPrefab;
    public static Button multibuttonChildPrefab;
    public static ColorBlock keybindColors;
    public static List<ModData> waitingToLoad;
    public static Traverse self;
    public void Awake()
    {
        needsToCreateSettings = true;
        prefabParent = new GameObject("PrefabParent").AddComponent<RectTransform>();
        prefabParent.gameObject.SetActive(false);
        DontDestroyOnLoad(prefabParent.gameObject);

        sliderPrefab = null;
        checkboxPrefab = null;
        comboboxPrefab = null;
        keybindPrefab = null;
        buttonPrefab = null;
        textPrefab = null;
        titlePrefab = null;
        sectionPrefab = null;
        inputPrefab = null;
        modSettings = new Dictionary<Mod, ModSettingContainer>();
        mods = new Dictionary<Mod, EventCaller>();
        waitingToLoad = new List<ModData>();
        modInfo = modlistEntry.jsonmodinfo;
        instance = this;
        configPath = Path.Combine(SaveAndLoad.WorldPath, modInfo.name + ".json");
        if (settingsController.IsOpen)
            insertNewSettingsMenu();
        Config = getSaveJson();
        if (RAPI.GetLocalPlayer())
            loadLocal(true);
        self = Traverse.Create(this);
        //loadAllSettings();
        (harmony = new Harmony("com.aidanamite.ExtraSettingsAPI")).PatchAll();
        Log("Mod has been loaded!");
    }

    public void Update()
    {
        if (waitingToLoad.Count > 0)
            for (int i = waitingToLoad.Count - 1; i >= 0; i--)
                if (waitingToLoad[i].modinfo?.mainClass && mods.ContainsKey(waitingToLoad[i].modinfo.mainClass))
                    waitingToLoad.RemoveAt(i);
                else if (waitingToLoad[i].modinfo.modState == ModInfo.ModStateEnum.running && waitingToLoad[i].modinfo.mainClass)
                {
                    var m = waitingToLoad[i];
                    waitingToLoad.RemoveAt(i);
                    waitingToLoad[i].modinfo.mainClass.gameObject.AddComponent<WaitForFirstUpdate>().onFirstUpdate = delegate
                    {
                        TryLoadSettings(m);
                    };
                }
                else if (waitingToLoad[i].modinfo.modState == ModInfo.ModStateEnum.errored)
                    waitingToLoad.RemoveAt(i);
        if (Patch_EnterExitKeybind.lastEntered.Item1 && Input.GetKeyDown(KeyCode.Mouse1)) {
            if (Patch_EnterExitKeybind.lastEntered.Item2)
                Patch_EnterExitKeybind.lastEntered.Item1.Keybind.MainKey = KeyCode.None;
            else
                Patch_EnterExitKeybind.lastEntered.Item1.Keybind.AltKey = KeyCode.None;
            Patch_EnterExitKeybind.lastEntered.Item1.Set(Patch_EnterExitKeybind.lastEntered.Item1.Keybind);
        }
    }

    public void OnModUnload()
    {
        harmony?.UnpatchAll(harmony.Id);
        if (!needsToCreateSettings)
            removeNewSettingsMenu();
        if (prefabParent)
            Destroy(prefabParent.gameObject);
        Log("Mod has been unloaded!");
    }

    public static void Log(object message)
    {
        Debug.Log("[" + modInfo.name + "]: " + message.ToString());
    }

    public static void LogWarning(object message)
    {
        Debug.LogWarning("[" + modInfo.name + "]: " + message.ToString());
    }

    public static void LogTree(Transform transform)
    {
        Debug.Log(GetLogTree(transform));
    }

    public static string GetLogTree(Transform transform, string prefix = " -")
    {
        string str = "\n" + prefix + transform.name;
        foreach (Behaviour obj in transform.GetComponents<Behaviour>())
            str += ": " + obj.GetType().Name;
        foreach (Transform sub in transform)
            str += GetLogTree(sub, prefix + "--");
        return str;
    }

    public static void LogError(object message)
    {
        Debug.LogError("[" + modInfo.name + "]: " + message.ToString());
    }

    public static void ErrorLog(Exception err)
    {
        LogError(err);
    }

    public static JSONObject getSaveJson(bool isLocal = false)
    {
        JSONObject data;
        var current = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-NZ");
        try
        {
            if (File.Exists(isLocal ? worldConfigPath : configPath))
                data = new JSONObject(File.ReadAllText(isLocal ? worldConfigPath : configPath));
            else
                data = new JSONObject();
        }
        catch
        {
            data = new JSONObject();
        }
        CultureInfo.CurrentCulture = current;
        return data;
    }

    private static void saveJson(JSONObject data, string path = "")
    {
        try
        {
            File.WriteAllText((path == "") ? configPath : path, data.ToString());
        }
        catch (Exception err)
        {
            LogError("An error occured while trying to save settings: " + err.Message);
        }
    }

    public static void generateSaveJson(string path = "")
    {
        JSONObject data = (path == "") ? Config : LocalConfig;
        var current = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-NZ");
        if (data == null)
            data = new JSONObject();
        if (data.IsNull || !data.HasField("savedSettings"))
            data.AddField("savedSettings", new JSONObject());
        JSONObject store = data.GetField("savedSettings");
        foreach (ModSettingContainer container in modSettings.Values)
            store.TrySetField(container.IDName, container.GenerateSaveJson(path != ""));

        saveJson(data, path);
        CultureInfo.CurrentCulture = current;
    }

    public static void saveSettings() => generateSaveJson();

    public static void loadLocal(bool loadSave)
    {
        if (loadSave)
            LocalConfig = getSaveJson(true);
        else
            LocalConfig = new JSONObject();
        foreach (ModSettingContainer container in modSettings.Values)
            container.LoadLocal();
    }

    public static void insertNewSettingsMenu()
    {
        var MenuPanel = Traverse.Create(settingsController).Field("panel").GetValue<RectTransform>();
        var TabGroup = MenuPanel.GetComponentInChildren<TabGroup>();
        MenuPanel.offsetMax += new Vector2((TabGroup.tabButtons[1].transform.localPosition - TabGroup.tabButtons[0].transform.localPosition).x, 0);
        int newIndex = TabGroup.tabButtons.Length;
        var settingsBody = ComponentManager<Settings>.Value.graphicsBox.gameObject;
        var settingsTab = TabGroup.tabButtons.First(x => x.Tab == settingsBody);
        newTabBody = Instantiate(settingsBody, settingsBody.transform.parent, false);
        newTabBody.name = newName;
        newTabBody.SetActive(false);
        newTabButtonObj = (newTabButton = Instantiate(settingsTab, settingsTab.transform.parent, false)).gameObject;
        newTabButtonObj.name = newName + "Tab";
        DestroyLocalizations(newTabButtonObj);
        Text newTabTex = newTabButton.GetComponentInChildren<Text>(true);
        newTabButton.tabIndex = newIndex;
        newTabTex.text = newName;
        newTabButton.OnPointerExit(true);
        Traverse tabTraverse = Traverse.Create(newTabButton);
        tabTraverse.Field("tabButton").SetValue(newTabButton.GetComponentInChildren<Button>(true));
        tabTraverse.Field("tab").SetValue(newTabBody);
        Add(ref TabGroup.tabButtons, newTabButton);
        //(newTabButtonObj.transform as RectTransform).pivot = new Vector2(0f, 1f);
        DestroyImmediate(newTabBody.GetComponent<GraphicsSettingsBox>());
        newTabContent = newTabBody.GetComponentInChildren<ScrollRect>().content;
        foreach (Transform transform in newTabContent)
        {
            if (!comboboxPrefab && transform.gameObject.GetComponentInChildren<Dropdown>(true))
            {
                comboboxPrefab = Instantiate(transform.gameObject, prefabParent, false);
                comboboxPrefab.name = "Combobox Setting";
                Dropdown drop = comboboxPrefab.GetComponentInChildren<Dropdown>(true);
                drop.onValueChanged = new Dropdown.DropdownEvent();
                foreach (LocalizeDropdownSemih localize in drop.GetComponentsInChildren<LocalizeDropdownSemih>(true))
                {
                    localize.enabled = false;
                    DestroyImmediate(localize);
                }
                drop.ClearOptions();
                drop.AddOptions(new List<Dropdown.OptionData> { new Dropdown.OptionData("test") });
                drop.itemText.text = drop.options[0].text;
                comboboxPrefab.GetComponentInChildren<Text>(true).text = "Option Name";
                DestroyLocalizations(comboboxPrefab);

                inputPrefab = Instantiate(comboboxPrefab, prefabParent, false);
                inputPrefab.name = "Input Setting";
                GameObject inputF = inputPrefab.transform.Find("Dropdown").gameObject;
                inputF.name = "InputField";
                (inputF.transform as RectTransform).offsetMin *= new Vector2(1.5f, 1);
                DestroyImmediate(inputF.GetComponent<Dropdown>());
                InputField tmp = inputF.AddComponent<InputField>();
                tmp.textComponent = inputF.transform.Find("Label").GetComponent<Text>();
                tmp.textComponent.rectTransform.offsetMax = -tmp.textComponent.rectTransform.offsetMin;
                DestroyImmediate(inputF.transform.Find("Arrow").gameObject);
                DestroyImmediate(inputF.transform.Find("Template").gameObject);
            }
            if (!sliderPrefab && transform.gameObject.GetComponentInChildren<UISlider>(true))
            {
                sliderPrefab = Instantiate(transform.gameObject, prefabParent, false);
                sliderPrefab.name = "Slider Setting";
                Slider slide = sliderPrefab.GetComponentInChildren<Slider>(true);
                slide.onValueChanged = new Slider.SliderEvent();
                slide.minValue = 0;
                slide.maxValue = 1;
                slide.wholeNumbers = false;
                slide.value = 0.25f;
                sliderPrefab.GetComponentInChildren<Text>(true).text = "Option Name";
                DestroyLocalizations(sliderPrefab);
            }
            if (!checkboxPrefab && transform.gameObject.GetComponentInChildren<Toggle>(true) && transform.gameObject.GetComponentInChildren<Dropdown>(true) == null)
            {
                checkboxPrefab = Instantiate(transform.gameObject, prefabParent, false);
                checkboxPrefab.name = "Checkbox Setting";
                Toggle checkbox = checkboxPrefab.GetComponentInChildren<Toggle>(true);
                checkbox.onValueChanged = new Toggle.ToggleEvent();
                checkbox.isOn = false;
                checkboxPrefab.GetComponentInChildren<Text>(true).text = "Option Name";
                DestroyLocalizations(checkboxPrefab);

                titlePrefab = Instantiate(transform.gameObject, prefabParent, false);
                titlePrefab.name = "Title";
                Toggle checkbox3 = titlePrefab.GetComponentInChildren<Toggle>(true);
                DestroyImmediate(checkbox3.graphic.gameObject);
                //Destroy(checkbox3.gameObject);
                checkbox3.onValueChanged = new Toggle.ToggleEvent();
                checkbox3.isOn = false;
                var checkboxImage = checkbox3.gameObject.AddComponent<ToggleImage>();
                var tex = new Texture2D(1, 1);
                tex.LoadImage(instance.GetEmbeddedFileBytes("down.png"));
                tex.Apply();
                checkboxImage.on = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                tex = new Texture2D(1, 1);
                tex.LoadImage(instance.GetEmbeddedFileBytes("left.png"));
                tex.Apply();
                checkboxImage.off = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                titlePrefab.GetComponentInChildren<Text>(true).text = "Mod Name";
                DestroyLocalizations(titlePrefab);

                sectionPrefab = Instantiate(titlePrefab, prefabParent, false);
                sectionPrefab.name = "Section Setting";
                titlePrefab.AddImageObject(10);
                sectionPrefab.AddImageObject(5);

                textPrefab = Instantiate(transform.gameObject, prefabParent, false);
                textPrefab.name = "Text Setting";
                Toggle checkbox2 = textPrefab.GetComponentInChildren<Toggle>(true);
                checkbox2.onValueChanged = new Toggle.ToggleEvent();
                checkbox2.gameObject.SetActive(false);
                textPrefab.GetComponentInChildren<Text>(true).text = "Some Text";
                DestroyLocalizations(textPrefab);
            }
            Destroy(transform.gameObject);
        }
        keybindColors = Traverse.Create(ComponentManager<Settings>.Value.controls).Field("colorblock").GetValue<ColorBlock>();
        foreach (Transform transform in ComponentManager<Settings>.Value.controls.GetComponentInChildren<ScrollRect>().content)
        {
            if (!keybindPrefab && transform.GetComponentInChildren<KeybindInterface>(true))
            {
                GameObject copiedObj = transform.Find("Sprint").gameObject;
                keybindPrefab = Instantiate(copiedObj, prefabParent, false);
                keybindPrefab.name = "Keybind Setting";
                keybindPrefab.GetComponentInChildren<Text>(true).text = "Option Name";
                DestroyLocalizations(keybindPrefab);
            }
            if (!buttonPrefab && transform.GetComponentInChildren<Button>(true))
            {
                buttonPrefab = Instantiate(transform.gameObject, prefabParent, false);
                buttonPrefab.name = "Button Setting";
                Button button = buttonPrefab.GetComponentInChildren<Button>(true);
                button.onClick = new Button.ButtonClickedEvent();
                button.GetComponentInChildren<Text>(true).text = "Button Name";
                DestroyLocalizations(buttonPrefab);

                multibuttonPrefab = Instantiate(transform.gameObject, prefabParent, false);
                multibuttonPrefab.name = "MultiButton Setting";
                var b = multibuttonPrefab.GetComponentInChildren<Button>(true).GetComponent<RectTransform>();
                var p = b.parent as RectTransform;
                var s = (b.rect.height - p.rect.height) / 2;
                Destroy(b.gameObject);
                var layout = new GameObject("Layout");
                layout.transform.SetParent(p, false);
                var group = layout.AddComponent<HorizontalLayoutGroup>();
                group.spacing = 2;
                group.childAlignment = TextAnchor.MiddleLeft;
                group.childScaleWidth =true;
                group.childScaleHeight = true;
                group.childControlWidth = false;
                group.childControlHeight = false;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
                var fitter = layout.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                var r = layout.GetComponent<RectTransform>();
                r.anchorMin = Vector2.zero;
                r.anchorMax = new Vector2(0,1);
                r.pivot = new Vector2(0, 0.5f);
                r.offsetMin = Vector2.one * s;
                r.offsetMax = Vector2.one * -s;
                DestroyLocalizations(multibuttonPrefab);

                multibuttonChildPrefab = Instantiate(button, prefabParent, false);
                multibuttonChildPrefab.name = "MultiButton Setting Button";
                DestroyImmediate( multibuttonChildPrefab.GetComponent<LayoutElement>());
            }
        }

        var scrollRect = newTabBody.GetComponentInChildren<ScrollRect>();
        var scrollbar = newTabBody.GetComponentInChildren<Scrollbar>();
        scrollRect.verticalScrollbar = scrollbar;
        scrollbar.value = 1;
        var verticalLayoutGroup = scrollRect.content.GetOrAddComponent<VerticalLayoutGroup>();
        var contentSizeFitter = verticalLayoutGroup.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
        needsToCreateSettings = false;
    }


    public static void MaybeReselectModTab()
    {
        if (scrollPos != null)
            newTabBody.GetComponentInChildren<ScrollRect>().content.anchoredPosition = scrollPos.Value;
        if (!modsTabWasSelected)
            return;
        newTabButton.OnPointerClick();
    }
    static bool modsTabWasSelected = false;
    static Vector2? scrollPos;
    public static void removeNewSettingsMenu()
    {
        needsToCreateSettings = true;
        var MenuPanel = Traverse.Create(settingsController).Field("panel").GetValue<RectTransform>();
        var TabGroup = MenuPanel.GetComponentInChildren<TabGroup>();
        scrollPos = newTabBody.GetComponentInChildren<ScrollRect>().content.anchoredPosition;
        if (modsTabWasSelected = (TabGroup.SelectedTabButton == newTabButton))
            TabGroup.SelectTab(0);
        MenuPanel.offsetMax -= new Vector2((TabGroup.tabButtons[1].transform.localPosition - TabGroup.tabButtons[0].transform.localPosition).x, 0f);
        Remove(ref TabGroup.tabButtons, newTabButton);
        Destroy(newTabBody);
        Destroy(newTabButtonObj);
    }

    public static Mod GetMod(Type type)
    {
        foreach (var m in mods.Keys)
            if (m.GetType() == type)
                return m;
        return null;
    }
    public static EventCaller GetCallerFromMod(Mod mod)
    {
        if (mods.TryGetValue(mod,out var caller))
            return caller;
        return null;
    }

    public static void DestroyLocalizations(GameObject gO)
    {
        foreach (var localize in gO.GetComponentsInChildren<Localize>(true))
            DestroyImmediate(localize, true);
    }

    public static void generateSettings(Mod mod)
    {
        generateSettings(modSettings[mod]);
    }

    public static void generateSettings(ModSettingContainer container)
    {
        container.Create();
    }

    public static void Add<T>(ref T[] array, T item)
    {
        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = item;
    }
    public static void RemoveAt<T>(ref T[] array, int index)
    {
        for (int i = index + 1; i < array.Length; i++)
            array[i - 1] = array[i];
        Array.Resize(ref array, array.Length - 1);
    }
    public static void Remove<T>(ref T[] array, T item)
    {
        RemoveAt(ref array, array.IndexOf(item));
    }

    public static void toggleSettings()
    {
        foreach (ModSettingContainer container in modSettings.Values)
            container.ToggleSettings();
    }

    /*public static void loadAllSettings()
    {
        foreach (ModData mod in ModManagerPage.modList)
            if (mod.modinfo.goInstance)
                mod.modinfo.goInstance.AddComponent<WaitForFirstUpdate>().onFirstUpdate = delegate { tryLoadSettings(mod); };
    }*/

    public static void loadSettings(ModData data, JSONObject settings)
    {
        Mod mod = data.modinfo.mainClass;
        if (!mod)
        {
            waitingToLoad.Add(data);
            return;
        }
        if (mods.ContainsKey(mod))
            return;

            waitingToLoad.RemoveAll( x=> x == data);
        try
        {
            ModSettingContainer newSettings = new ModSettingContainer(mod, settings);
            EventCaller caller = new EventCaller(mod);
            modSettings.Add(mod, newSettings);
            mods.Add(mod, caller);
            caller.Call(EventCaller.EventTypes.Load);
            if (!needsToCreateSettings)
                caller.Call(EventCaller.EventTypes.Open);
        }
        catch (Exception err)
        {
            ErrorLog(err);
        }
    }

    public static void tryLoadSettings(string modName)
    {
        foreach (ModData mod in ModManagerPage.modList)
            if (modName == mod.jsonmodinfo.name)
                TryLoadSettings(mod);
    }

    public static void tryLoadSettings(Mod mod)
    {
        TryLoadSettings(mod.modlistEntry);
    }

    public static void TryLoadSettings(ModData data)
    {
        var current = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-NZ");
        JSONObject modJson = null;
        foreach (var f in data.modinfo.modFiles)
            if (f.Key.ToLower().EndsWith("modinfo.json"))
            {
                try
                {
                    Assembly.GetExecutingAssembly().GetType("F");
                    modJson = new JSONObject(Encoding.Default.GetString(f.Value));
                    break;
                } catch { }
            }
        CultureInfo.CurrentCulture = current;
        if (modJson == null)
            LogWarning($"Failed to find/read modjson file for {data.jsonmodinfo.name}");
        else if (modJson.HasField("modSettings"))
            loadSettings(data, modJson.GetField("modSettings"));
    }

    public static void UpdateAllSettingBacks()
    {
        var f = false;
        foreach (var c in modSettings)
            if (c.Value != null && c.Key)
            {
                if (c.Value.title && c.Value.title.GetComponent<Image>())
                {
                    c.Value.title.GetComponent<Image>().enabled = f;
                    f = !f;
                }
                foreach (var s in c.Value.allSettings)
                    if (s != null && s.control && s.control.activeSelf && s.SetBackImage(f))
                        f = !f;
            }
    }

    public override void ModEvent_OnModUnloaded(Mod mod)
    {
        removeSettings(mod);
        base.ModEvent_OnModUnloaded(mod);
    }
    public override void ModEvent_OnModLoaded(Mod mod)
    {
        if (mod)
            mod.gameObject.AddComponent<WaitForFirstUpdate>().onFirstUpdate = () => tryLoadSettings(mod);
        base.ModEvent_OnModUnloaded(mod);
    }

    public static void removeSettings(Mod mod)
    {
        if (!mod || !mods.ContainsKey(mod))
            return;
        modSettings[mod].Destroy();
        modSettings.Remove(mod);
        mods[mod].APIBool.Value = false;
        mods.Remove(mod);
    }

    public static T getSetting<T>(Mod mod, string settingName) where T : ModSetting
    {
        if (modSettings[mod].settings.TryGetValue(settingName, out var lookup))
        {
            if (lookup is T o)
                return o;
            foreach (var setting in modSettings[mod].allSettings)
                if (setting is T o2 && setting.name == settingName)
                    return o2;
        }
        foreach (KeyValuePair<ModSetting.SettingType, Type> pair in ModSetting.matches)
            if (pair.Value == typeof(T))
                throw new NullReferenceException("Could not find " + pair.Key.ToString() + " setting " + settingName + " for " + mod.modlistEntry.jsonmodinfo.name);
        throw new NullReferenceException("Could not find " + typeof(T).Name + " setting " + settingName + " for " + mod.modlistEntry.jsonmodinfo.name);
    }

    public static bool getCheckboxState(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Checkbox>(mod, settingName).value;
        return default;
    }

    public static int getComboboxSelectedIndex(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Combobox>(mod, settingName).index;
        return default;
    }

    public static string getComboboxSelectedItem(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Combobox>(mod, settingName).value;
        return default;
    }

    public static string[] getComboboxContent(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Combobox>(mod, settingName).getContent();
        return default;
    }

    public static float getSliderValue(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Slider>(mod, settingName).roundValue;
        return default;
    }

    public static float getSliderRealValue(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Slider>(mod, settingName).value;
        return default;
    }

    public static string getKeybindName(Mod mod, string settingName)
    {
        if (mod)
            return getKeybind(mod, settingName).Identifier;
        return default;
    }

    public static KeyCode getKeybind_main(Mod mod, string settingName)
    {
        if (mod)
            return getKeybind(mod, settingName).MainKey;
        return default;
    }

    public static KeyCode getKeybind_alt(Mod mod, string settingName)
    {
        if (mod)
            return getKeybind(mod, settingName).AltKey;
        return default;
    }
    public static KeyCode getKeybindMain(Mod mod, string settingName) => getKeybind_main(mod, settingName);
    public static KeyCode getKeybindAlt(Mod mod, string settingName) => getKeybind_alt(mod, settingName);

    public static Keybind getKeybind(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Keybind>(mod, settingName).value;
        return default;
    }

    public static string getInputValue(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Input>(mod, settingName).value;
        return default;
    }

    public static string getDataValue(Mod mod, string settingName, string subname)
    {
        if (mod)
            return getSetting<ModSetting_Data>(mod, settingName).getValue(subname);
        return default;
    }

    public static string[] getDataNames(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_Data>(mod, settingName).getNames();
        return default;
    }

    public static string getSettingsText(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting>(mod, settingName).nameText;
        return default;
    }

    public static string getSettingText(Mod mod, string settingName) => getSettingsText(mod, settingName);
    public static string getText(Mod mod, string settingName) => getSettingsText(mod, settingName);

    public static string[] getButtons(Mod mod, string settingName)
    {
        if (mod)
            return getSetting<ModSetting_MultiButton>(mod, settingName).GetValue();
        return default;
    }

    public static void setCheckboxState(Mod mod, string settingName, bool value)
    {
        if (mod)
            getSetting<ModSetting_Checkbox>(mod, settingName).SetValue(value);
        generateSaveJson();
    }

    public static void setComboboxSelectedIndex(Mod mod, string settingName, int value)
    {
        if (mod)
            getSetting<ModSetting_Combobox>(mod, settingName).SetValue(value);
        generateSaveJson();
    }

    public static void setComboboxSelectedItem(Mod mod, string settingName, string value)
    {
        if (mod)
            getSetting<ModSetting_Combobox>(mod, settingName).SetValue(value);
        generateSaveJson();
    }

    public static void setComboboxContent(Mod mod, string settingName, string[] items)
    {
        if (mod)
            getSetting<ModSetting_Combobox>(mod, settingName).setContent(items);
        generateSaveJson();
    }

    public static void addComboboxContent(Mod mod, string settingName, string item)
    {
        if (mod)
            getSetting<ModSetting_Combobox>(mod, settingName).addContent(item);
        generateSaveJson();
    }

    public static void resetComboboxContent(Mod mod, string settingName)
    {
        if (mod)
            getSetting<ModSetting_Combobox>(mod, settingName).resetContent();
        generateSaveJson();
    }

    public static void setSliderValue(Mod mod, string settingName, float value)
    {
        if (mod)
            getSetting<ModSetting_Slider>(mod, settingName).SetValue(value);
        generateSaveJson();
    }

    public static void setKeybind_main(Mod mod, string settingName, KeyCode value)
    {
        if (mod)
            getSetting<ModSetting_Keybind>(mod, settingName).SetValue(value, true);
        generateSaveJson();
    }

    public static void setKeybind_alt(Mod mod, string settingName, KeyCode value)
    {
        if (mod)
            getSetting<ModSetting_Keybind>(mod, settingName).SetValue(value, false);
        generateSaveJson();
    }

    public static void setKeybindMain(Mod mod, string settingName, KeyCode value) => setKeybind_main(mod, settingName, value);

    public static void setKeybindAlt(Mod mod, string settingName, KeyCode value) => setKeybind_alt(mod, settingName, value);

    public static void setInputValue(Mod mod, string settingName, string value)
    {
        if (mod)
            getSetting<ModSetting_Input>(mod, settingName).SetValue(value);
        generateSaveJson();
    }

    public static void setDataValue(Mod mod, string settingName, string subname, string value)
    {
        if (mod)
            getSetting<ModSetting_Data>(mod, settingName).SetValue(subname, value);
    }

    public static void setDataValues(Mod mod, string settingName, Dictionary<string, string> value)
    {
        if (mod)
            getSetting<ModSetting_Data>(mod, settingName).SetValues(value);
    }

    public static void setSettingsText(Mod mod, string settingName, string newText)
    {
        if (mod)
            getSetting<ModSetting>(mod, settingName).SetText(newText);
    }

    public static void setText(Mod mod, string settingName, string newText) => setSettingsText(mod, settingName, newText);
    public static void setSettingText(Mod mod, string settingName, string newText) => setSettingsText(mod, settingName, newText);

    public static void setButtons(Mod mod, string settingName, string[] newButtons)
    {
        if (mod)
            getSetting<ModSetting_MultiButton>(mod, settingName).SetValue(newButtons);
    }

    public static void resetSetting(Mod mod, string settingName)
    {
        if (mod)
            getSetting<ModSetting>(mod, settingName).ResetValue();
        generateSaveJson();
    }

    public static void resetSettings(Mod mod)
    {
        if (mod)
            foreach (var setting in modSettings[mod].allSettings)
                setting.ResetValue();
        generateSaveJson();
    }

    public static void resetAllSettings(Mod mod) => resetSettings(mod);

    public static Mod getModFromType(Type type)
    {
        foreach (var m in mods)
            if (m.Key.GetType() == type)
                return m.Key;
        return null;
    }

    public static Mod getModFromAssembly(Type type)
    {
        foreach (var m in mods)
            if (m.Key.GetType().Assembly == type.Assembly)
                return m.Key;
        return null;
    }

    public static void checkSettingVisibility(Mod mod)
    {
        if (mod)
            modSettings[mod].ToggleSettings();
    }


    static bool ResolveSetting(string[] args, out Mod mod, out ModSetting setting)
    {
        mod = null;
        setting = null;
        if (args?.Length >= 1)
        {
            foreach (var s in modSettings)
                if (s.Value.IDName == args[0])
                    mod = s.Key;
            if (!mod)
            {
                Debug.LogWarning($"Mod \"{args[0]}\" not found");
                return false;
            }
        }
        if (args?.Length >= 2)
        {
            var i = 0;
            foreach (var s in modSettings[mod].allSettings)
                if (i++.ToString() == args[1] || s.name == args[1])
                    setting = s;
            if (setting != null)
            {
                Debug.LogWarning($"Setting \"{args[1]}\" not found for mod {mod.name}");
                return false;
            }
        }
        if (args == null || args.Length <= 1)
        {
            if (args?.Length == 1)
            {
                var i = 0;
                Debug.Log($"Settings found for {mod.name}:{modSettings[mod].allSettings.Join(x => $"\n{i++}|{x.name} => {x.GetType().Name.Remove(0, 11)} {x.nameText}", "")}");
            }
            else
                Debug.Log($"Mods with settings:{modSettings.Join(x => $"\n{x.Value.IDName} => {x.Key.name}", "")}");
            return false;
        }
        return true;
    }
    [ConsoleCommand("modsettings", "[RDS ONLY] Syntax: modsettings <mod> <setting|index> [set|reset|click] [new value]")]
    public static void SetSetting(string[] args)
    {
        if (!RAPI.IsDedicatedServer())
        {
            Debug.LogError("This command can only be used on RDS");
            return;
        }
        if (!ResolveSetting(args, out var m, out var s))
            return;
        if (args.Length < 3)
        {
            Debug.LogWarning("Not enough arguments; Missing mode argument");
            return;
        }
        if (args[2].ToLowerInvariant() == "reset")
        {
            GetCallerFromMod(m).Call(EventCaller.EventTypes.Open);
            s.ResetValue();
            GetCallerFromMod(m).Call(EventCaller.EventTypes.Close);
            Debug.Log("Value reset to " + s.CurrentValue());
            return;
        }
        if (args[2].ToLowerInvariant() == "click")
        {
            if (s is ModSetting_Button b)
            {
                GetCallerFromMod(m).Call(EventCaller.EventTypes.Open);
                GetCallerFromMod(m).ButtonPress(b);
                GetCallerFromMod(m).Call(EventCaller.EventTypes.Close);
                Debug.Log(s.nameText + " button clicked");
            }
            else if (s is ModSetting_MultiButton mb)
            {
                if (args.Length < 4)
                {
                    var i = 0;
                    Debug.Log($"Name: {s.nameText}\nButtons:{mb.names.Join(x => $"\n{i++} | {x}","")}");
                    return;
                }
                var sb2 = new StringBuilder();
                for (int i = 3; i < args.Length; i++)
                {
                    if (sb2.Length != 0)
                        sb2.Append(' ');
                    sb2.Append(args[i]);
                }
                var v2 = sb2.ToString();
                var ind = Array.IndexOf(mb.names, v2);
                if (ind == -1 && uint.TryParse(v2, out var ui) && ui < mb.names.Length)
                    ind = (int)ui;
                if (ind == -1)
                    Debug.LogWarning($"MultiButton {s.nameText} does not have the button \"{v2}\"");
                else
                {
                    GetCallerFromMod(m).Call(EventCaller.EventTypes.Open);
                    GetCallerFromMod(m).ButtonPress(mb,ind);
                    GetCallerFromMod(m).Call(EventCaller.EventTypes.Close);
                }
            }
            else
                Debug.LogWarning($"{s.nameText} is a {s.DisplayType().ToLowerInvariant()} not a button");
            return;
        }
        if (args[3].ToLowerInvariant() != "set")
        {
            Debug.LogWarning("Mode argument must be either \"set\", \"reset\" or \"click\"");
            return;
        }
        var sb = new StringBuilder();
        for (int i = 3; i < args.Length; i++)
        {
            if (sb.Length != 0)
                sb.Append(' ');
            sb.Append(args[i]);
        }
        var v = sb.ToString();
        GetCallerFromMod(m).Call(EventCaller.EventTypes.Open);
        if (v == "")
        {
            Debug.Log($"Name: {s.nameText}\nSetting Type: {s.DisplayType()}\nCurrent: {s.CurrentValue()}\nCan be: [{s.PossibleValues().Join()}]");
            GetCallerFromMod(m).Call(EventCaller.EventTypes.Close);
            return;
        }
        var flag = s.TrySetValue(v);
        GetCallerFromMod(m).Call(EventCaller.EventTypes.Close);
        if (flag)
            Debug.Log("Value changed to " + s.CurrentValue());
        else
            Debug.Log($"Value \"{v}\" is not a valid option for the {s.DisplayType().ToLowerInvariant()} \"{s.nameText}\"");
    }
}
