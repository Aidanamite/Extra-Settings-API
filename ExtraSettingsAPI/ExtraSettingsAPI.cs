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
    public static Settings settingsController = ComponentManager<Settings>.Value;
    static Traverse settingsTraverse = Traverse.Create(settingsController);
    static GameObject OptionMenuContainer = settingsTraverse.Field("optionsCanvas").GetValue<GameObject>().transform.FindChildRecursively("OptionMenuParent").gameObject;
    static TabGroup tabGroup = OptionMenuContainer.GetComponentInChildren<TabGroup>();
    static Traverse<TabButton[]> tabButtons = Traverse.Create(tabGroup).Field<TabButton[]>("tabButtons");
    static GameObject newSet;
    static GameObject newTab;
    static TabButton newTabBut;
    public static GameObject newOptCon;
    static string newName = "Mods";
    static string sourceName = "Graphics";
    static RectTransform backTransform = OptionMenuContainer.transform.FindChildRecursively("BrownBackground").transform as RectTransform;
    static RectTransform divTransform = OptionMenuContainer.transform.FindChildRecursively("Divider").transform as RectTransform;
    static RectTransform contentTransform = OptionMenuContainer.transform.FindChildRecursively("TabContent").transform as RectTransform;
    static RectTransform closeTransform = OptionMenuContainer.transform.FindChildRecursively("CloseButton").transform as RectTransform;
    static RectTransform tabsTransform = OptionMenuContainer.transform.FindChildRecursively("TabContainer").transform as RectTransform;
    public static Sprite dividerSprite = divTransform.GetComponent<Image>().sprite;
    public static Dictionary<Mod, ModSettingContainer> modSettings;
    public static Dictionary<Mod, EventCaller> mods;
    public static bool init;
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
        init = true;
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
        keybindColors = new ColorBlock();
        keybindColors.disabledColor = new Color(0.772f, 0.233f, 0.170f, 0.502f);
        keybindColors.highlightedColor = new Color(0.956f, 0.893f, 0.759f, 1.000f);
        keybindColors.normalColor = new Color(0.733f, 0.631f, 0.416f, 1.000f);
        keybindColors.pressedColor = new Color(0.733f, 0.631f, 0.416f, 1.000f);
        keybindColors.selectedColor = new Color(0.956f, 0.893f, 0.759f, 1.000f);
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
        if (!init)
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
        OptionMenuContainer = settingsTraverse.Field("optionsCanvas").GetValue<GameObject>().transform.FindChildRecursively("OptionMenuParent").gameObject;
        tabGroup = OptionMenuContainer.GetComponentInChildren<TabGroup>();
        tabButtons = Traverse.Create(tabGroup).Field<TabButton[]>("tabButtons");
        backTransform = OptionMenuContainer.transform.FindChildRecursively("BrownBackground").transform as RectTransform;
        divTransform = OptionMenuContainer.transform.FindChildRecursively("Divider").transform as RectTransform;
        contentTransform = OptionMenuContainer.transform.FindChildRecursively("TabContent").transform as RectTransform;
        closeTransform = OptionMenuContainer.transform.FindChildRecursively("CloseButton").transform as RectTransform;
        tabsTransform = OptionMenuContainer.transform.FindChildRecursively("TabContainer").transform as RectTransform;
        Vector2 newSize = new Vector2(tabsTransform.rect.width * tabButtons.Value.Length.StepUp(), 0);
        backTransform.offsetMax += newSize;
        divTransform.offsetMax += newSize;
        contentTransform.offsetMax += newSize;
        closeTransform.offsetMin += newSize;
        closeTransform.offsetMax += newSize;
        tabsTransform.offsetMax += newSize;
        int newIndex = tabButtons.Value.Length;
        GameObject settingsSet = OptionMenuContainer.transform.FindChildRecursively(sourceName).gameObject;
        newSet = Instantiate(settingsSet, settingsSet.transform.parent, false);
        GameObject settingsTab = OptionMenuContainer.transform.FindChildRecursively(sourceName + "Tab").gameObject;
        newTab = Instantiate(settingsTab, settingsTab.transform.parent, false);
        newSet.name = newName;
        newTab.name = newName + "Tab";
        newSet.SetActive(false);
        newTabBut = newTab.GetComponent<TabButton>();
        DestroyLocalizations(newTabBut.gameObject);
        Text newTabTex = newTabBut.GetComponentInChildren<Text>(true);
        newTabBut.tabIndex = newIndex;
        newTabTex.text = newName;
        newTabBut.name = newTab.name;
        newTabBut.OnPointerExit(true);
        Traverse tabTraverse = Traverse.Create(newTabBut);
        tabTraverse.Field("tabButton").SetValue(newTabBut.GetComponentInChildren<Button>(true));
        tabTraverse.Field("tab").SetValue(newSet);
        var buttons = tabButtons.Value;
        Add(ref buttons, newTabBut);
        tabButtons.Value = buttons;
        (newTab.transform as RectTransform).pivot = new Vector2(0f, 1f);
        DestroyImmediate(newSet.GetComponent<GraphicsSettingsBox>());
        newOptCon = newSet.transform.FindChildRecursively("Content").gameObject;
        foreach (Transform transform in newOptCon.transform)
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
                Destroy(inputF.transform.Find("Arrow").gameObject);
                Destroy(inputF.transform.Find("Template").gameObject);
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
                Destroy(checkbox3.graphic.gameObject);
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
        foreach (Transform transform in OptionMenuContainer.transform.FindChildRecursively("Controls").gameObject.transform.FindChildRecursively("Content").gameObject.transform)
        {
            if (!keybindPrefab && transform.GetComponentInChildren<KeybindInterface>(true))
            {
                GameObject copiedObj = transform.FindChildRecursively("Sprint").gameObject;
                keybindPrefab = Instantiate(copiedObj, prefabParent, false);
                keybindPrefab.name = "Keybind Setting";
                keybindPrefab.GetComponentInChildren<Text>(true).text = "Option Name";
                DestroyLocalizations(keybindPrefab);
            }
            if (!buttonPrefab && transform.gameObject.GetComponentInChildren<Button>(true))
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

        //ScrollRect scrollRect = newSet.GetComponentInChildren<ScrollRect>(true);
        Scrollbar scrollbar = newSet.GetComponentInChildren<Scrollbar>(true);
        //scrollRect.verticalScrollbar = scrollbar;
        scrollbar.value = 1;
        VerticalLayoutGroup verticalLayoutGroup = newSet.GetComponentInChildren<VerticalLayoutGroup>(true); // This will fetch null if copied tab is "General"
        ContentSizeFitter contentSizeFitter = verticalLayoutGroup.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
        init = false;
    }

    public static void removeNewSettingsMenu()
    {
        init = true;
        if (tabGroup.SelectedTabButton == newTabBut)
            tabGroup.SelectTab(0);
        var buttons = tabButtons.Value;
        Vector2 newSize = new Vector2(tabsTransform.rect.width * buttons.Length.StepDown(), 0f);
        backTransform.offsetMax -= newSize;
        divTransform.offsetMax -= newSize;
        contentTransform.offsetMax -= newSize;
        closeTransform.offsetMin -= newSize;
        closeTransform.offsetMax -= newSize;
        tabsTransform.offsetMax -= newSize;
        Remove(ref buttons, newTabBut);
        tabButtons.Value = buttons;
        Destroy(newTabBut);
        Destroy(newSet);
        Destroy(newTab);
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
            if (!init)
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
}

static public class ExtentionMethods
{
    public static int IndexOf<T>(this T[] array, T item)
    {
        return Array.IndexOf(array, item);
    }
    public static GameObject AddImageObject(this GameObject gameObject, float thickness)
    {
        GameObject imageContainer = new GameObject("Divider");
        RectTransform trans = imageContainer.AddComponent<RectTransform>();
        imageContainer.transform.SetParent(gameObject.transform, false);
        Image image = imageContainer.AddComponent<Image>();
        trans.anchorMin = new Vector2(0, 0);
        trans.anchorMax = new Vector2(1, 0);
        trans.offsetMin = new Vector2(10, 0);
        trans.offsetMax = new Vector2(-10, thickness);
        image.sprite = ExtraSettingsAPI.dividerSprite;
        return imageContainer;
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, bool data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, float data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, int data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, JSONObject data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, string data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, JSONObject.CreateStringObject(data));
        else
            jsonObj.AddField(fieldName, JSONObject.CreateStringObject(data));
    }

    public static bool NotWorldSave(this ModSetting.MenuType type) => type != ModSetting.MenuType.World && type != ModSetting.MenuType.WorldCustom;

    public static string[] ToStringArray(this JSONObject obj)
    {
        var a = new string[obj.Count];
        for (int i = 0; i < a.Length; i++)
            a[i] = obj[i].str;
        return a;
    }

    public static float StepUp(this int v) => 1f / v;
    public static float StepDown(this int v) => 1 - ((v - 1f) / v);

    public static void PrintAllFields(this object obj)
    {
        var s = new StringBuilder(obj.ToString());
        var t = obj.GetType();
        while (t != typeof(object))
        {
            foreach (var f in t.GetFields(~BindingFlags.Default))
                if (!f.IsStatic)
                {
                    s.Append("\n - ");
                    s.Append(f.FieldType.FullName);
                    s.Append(" ");
                    s.Append(f.DeclaringType.FullName);
                    s.Append(".");
                    s.Append(f.Name);
                    s.Append(" = ");
                    s.Append(f.GetValue(obj));
                }
            foreach (var p in t.GetProperties(~BindingFlags.Default))
                if (p.GetGetMethod() != null && !p.GetGetMethod().IsStatic && p.GetGetMethod().GetParameters().Length == 0)
                {
                    s.Append("\n - ");
                    s.Append(p.GetGetMethod().ReturnType.FullName);
                    s.Append(" ");
                    s.Append(p.DeclaringType.FullName);
                    s.Append(".");
                    s.Append(p.Name);
                    s.Append(" = ");
                    s.Append(p.GetValue(obj));
                }
            t = t.BaseType;
        }
        Debug.Log(s.ToString());
    }
}

[HarmonyPatch(typeof(Settings), "Open")]
static class Patch_SettingsOpen
{
    static void Postfix()
    {
        ExtraSettingsAPI.insertNewSettingsMenu();
        foreach (EventCaller caller in ExtraSettingsAPI.mods.Values)
            caller.Call(EventCaller.EventTypes.Open);
    }
}

[HarmonyPatch(typeof(Settings), "Close")]
static class Patch_SettingsClose
{
    static void Prefix(ref Settings __instance, ref bool __state) => __state = Traverse.Create(__instance).Field("optionsCanvas").GetValue<GameObject>().activeInHierarchy;
    static void Postfix(ref bool __state)
    {
        if (__state)
        {
            ExtraSettingsAPI.generateSaveJson();
            foreach (EventCaller caller in ExtraSettingsAPI.mods.Values)
                caller.Call(EventCaller.EventTypes.Close);
            if (!ExtraSettingsAPI.init)
                ExtraSettingsAPI.removeNewSettingsMenu();
        }
    }
}

/*[HarmonyPatch(typeof(Transform), "parent", MethodType.Setter)]
static class Patch_ModLoad
{
    static void Postfix(Transform __instance, Transform __0)
    {
        if (__0 == ModManagerPage.ModsGameObjectParent.transform)
            __instance.gameObject.AddComponent<WaitForFirstUpdate>().onFirstUpdate = () => ExtraSettingsAPI.tryLoadSettings(__instance.GetComponent<Mod>());
    }
}

[HarmonyPatch(typeof(BaseModHandler), "UnloadMod")]
static class Patch_ModUnload
{
    static void Postfix(ref ModData moddata) => ExtraSettingsAPI.modUnloaded(moddata.modinfo.mainClass);
}*/

[HarmonyPatch(typeof(UISlider), "Update")]
static class Patch_SliderUpdate
{
    static bool Prefix(ref UISlider __instance)
    {
        if (__instance.name.StartsWith("ESAPI_"))
            foreach (ModSettingContainer container in ExtraSettingsAPI.modSettings.Values)
                foreach (var setting in container.allSettings)
                    if (setting is ModSetting_Slider s && s.UIslider == __instance)
                    {
                        setting.Update();
                        return false;
                    }
        return true;
    }
}

[HarmonyPatch(typeof(MyInput), "IdentifierToKeybind")]
static class Patch_KeybindsReset
{
    static bool Prefix(ref string identifier, ref Keybind __result)
    {
        if (ModSetting_Keybind.MyKeys != null && ModSetting_Keybind.MyKeys.Count > 0 && ModSetting_Keybind.MyKeys.ContainsKey(identifier))
        {
            __result = ModSetting_Keybind.MyKeys[identifier];
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(KeybindInterface))]
static class Patch_EnterExitKeybind
{
    public static (KeybindInterface,bool) lastEntered;
    [HarmonyPatch("PointerEnter")]
    [HarmonyPrefix]
    static void Enter(KeybindInterface __instance, KeyConnection key, KeyConnection ___mainKey)
    {
        lastEntered = (__instance, key == ___mainKey);
    }
    [HarmonyPatch("PointerExit")]
    [HarmonyPrefix]
    static void Exit()
    {
        lastEntered = default;
    }
}

[HarmonyPatch(typeof(SaveAndLoad), "Save")]
static class Patch_SaveGame
{
    static void Postfix(string filename)
    {
        string[] path = filename.Split(new char[] { '\\', '/' });
        filename = "";
        for (int i = 0; i < path.Length - 1; i++)
            filename += path[i] + "\\";
        if (filename.EndsWith(SaveAndLoad.latestStringNameEnding + "\\"))
            ExtraSettingsAPI.generateSaveJson(filename + ExtraSettingsAPI.modInfo.name + ".json");
    }
}

[HarmonyPatch(typeof(LoadGameBox), "Button_LoadGame")]
static class Patch_LoadGame
{
    static void Postfix() => ExtraSettingsAPI.loadLocal(true);
}

[HarmonyPatch(typeof(NewGameBox), "Button_CreateNewGame")]
static class Patch_NewGame
{
    static void Postfix() => ExtraSettingsAPI.loadLocal(false);
}

[HarmonyPatch(typeof(LoadSceneManager), "LoadScene")]
static class Patch_UnloadWorld
{
    static void Postfix(ref string sceneName)
    {
        if (sceneName == Raft_Network.MenuSceneName)
            ExtraSettingsAPI.LocalConfig = null;
    }
}

static class Patch_ReplaceAPICalls
{
    public static HashSet<MethodInfo> methodsToLookFor;
    public static IEnumerable<MethodBase> TargetMethods(Assembly assembly)
    {
        var l = new List<MethodBase>();
        foreach (var t in assembly.GetTypes())
            foreach (var m in t.GetMethods(~BindingFlags.Default))
                try
                {
                    foreach (var i in PatchProcessor.GetCurrentInstructions(m, out var iL))
                        if (i.opcode == OpCodes.Call && i.operand is MethodInfo method && methodsToLookFor.Contains(method))
                        {
                            l.Add(m);
                            break;
                        }
                } catch { }
        return l;
    }
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        foreach (var i in code)
            if (i.opcode == OpCodes.Call && i.operand is MethodInfo method && methodsToLookFor.Contains(method) && !method.IsStatic)
                i.opcode = OpCodes.Callvirt;
        return code;
    }
}

public class ToggleImage : MonoBehaviour
{
    Toggle obj;
    bool last;
    public Sprite on;
    public Sprite off;
    void Awake()
    {
        obj = GetComponent<Toggle>();
        last = !obj.isOn;
    }
    void Update()
    {
        if (last != obj.isOn && obj.image)
        {
            last = obj.isOn;
            obj.image.sprite = last ? on : off;
        }
    }
}

public class WaitForFirstUpdate : MonoBehaviour
{
    public Action onFirstUpdate;
    void Update()
    {
        onFirstUpdate?.Invoke();
        DestroyImmediate(this);
    }
}

public class EventCaller
{
    public Mod parent { get; }
    Traverse modTraverse;
    Dictionary<EventTypes, Traverse> settingsEvents = new Dictionary<EventTypes, Traverse>();
    public Traverse<bool> APIBool;
    public EventCaller(Mod mod)
    {
        parent = mod;
        modTraverse = Traverse.Create(parent);
        var settingsField = modTraverse.Field("ExtraSettingsAPI_Settings");
        if (settingsField.FieldExists())
        {
            if (settingsField.GetValue() == null)
                try
                {
                    var fType = settingsField.GetValueType();
                    if (fType == typeof(Type))
                        throw new InvalidOperationException("Cannot create instance of class " + fType.FullName);
                    if (fType.IsAbstract)
                        throw new InvalidOperationException("Cannot create instance of abstract class " + fType.FullName);
                    if (fType.IsInterface)
                        throw new InvalidOperationException("Cannot create instance of interface class " + fType.FullName);
                    var c = fType.GetConstructors((BindingFlags)(-1)).FirstOrDefault(x => x.GetParameters().Length == 0);
                    if (c == null)
                        throw new MissingMethodException("No parameterless constructor found for class " + fType.FullName);
                    else
                        settingsField.SetValue(c.Invoke(new object[0]));
                } catch (Exception e)
                {
                    ExtraSettingsAPI.Log($"Found settings field of mod {parent.modlistEntry.jsonmodinfo.name}'s main class but failed to create an instance for it. You may need to create the class instance yourself.\n{e}");
                }
            if (settingsField.GetValue() != null)
            {
                if (settingsField.GetValue() is Type)
                    modTraverse = Traverse.Create((Type)settingsField.GetValue());
                else
                    modTraverse = Traverse.Create(settingsField.GetValue());
            }
        }
        foreach (KeyValuePair<EventTypes, string> pair in EventNames)
            if (pair.Key != EventTypes.Button)
                settingsEvents.Add(pair.Key, modTraverse.Method(pair.Value, new Type[] { }, new object[] { }));
        APIBool = modTraverse.Field<bool>("ExtraSettingsAPI_Loaded");
        modTraverse.Field<Traverse>("ExtraSettingsAPI_Traverse").Value = ExtraSettingsAPI.self;
        var patchedMethods = new HashSet<MethodInfo>();
        foreach(var modMethod in (modTraverse.GetValue() as Type ?? modTraverse.GetValue().GetType()).GetMethods(~BindingFlags.Default))
            if (modMethod.Name.StartsWith("ExtraSettingsAPI_") && !EventNames.ContainsValue(modMethod.Name))
            {
                var matches = new List<MethodInfo>();
                MethodInfo m1 = null;
                var s = -1;
                var pars = default(List<int>);
                foreach (var m in typeof(ExtraSettingsAPI).GetMethods(~BindingFlags.Default))
                    if (m.Name.Equals(modMethod.Name.Remove(0, "ExtraSettingsAPI_".Length),StringComparison.InvariantCultureIgnoreCase))
                    {
                        matches.Add(m);
                        if (Transpiler.CheckPatchParameters(modMethod, m, out var l, out var skip) && (s == -1 || skip < s))
                        {
                            s = skip;
                            m1 = m;
                            pars = l;
                        }
                    }
                if (matches.Count == 0)
                    ExtraSettingsAPI.LogWarning($"{parent.modlistEntry.jsonmodinfo.name} >> Could not find any methods matching the name of method {modMethod.DeclaringType.FullName}::{modMethod}. You may have misspelled the method name or not meant to implement the ExtraSettingsAPI here");
                else if (m1 == null)
                    ExtraSettingsAPI.LogWarning($"{parent.modlistEntry.jsonmodinfo.name} >> Could not find suitable implementation for method {modMethod.DeclaringType.FullName}::{modMethod}. You may have misspelled the method name, not meant to implement the ExtraSettingsAPI here or used the wrong parameters. The following methods were found with the same name:" + matches.Join(y => "\n" + y.ReturnType?.FullName + " " + modMethod.Name + "(" + y.GetParameters().Skip(1).Join(x => x.ParameterType.FullName) + ")",""));
                else
                {
                    try
                    {
                        Transpiler.newMethod = m1;
                        Transpiler.modClass = parent.GetType();
                        Transpiler.argumentPairs = pars;
                        ExtraSettingsAPI.instance.harmony.Patch(modMethod, transpiler: new HarmonyMethod(typeof(Transpiler), nameof(Transpiler.Transpile)));
                        patchedMethods.Add(modMethod);
                    } catch (Exception e)
                    {
                        ExtraSettingsAPI.LogError($"An error occured while trying to implement the {modMethod.Name} method for the {parent.modlistEntry.jsonmodinfo.name} mod\n{e}");
                    }
                }
            }
        Patch_ReplaceAPICalls.methodsToLookFor = patchedMethods;
        foreach (var m in Patch_ReplaceAPICalls.TargetMethods(parent.GetType().Assembly))
            ExtraSettingsAPI.instance.harmony.Patch(m, transpiler: new HarmonyMethod(typeof(Patch_ReplaceAPICalls), nameof(Patch_ReplaceAPICalls.Transpiler)));
    }

    static class Transpiler
    {
        public static Type modClass;
        public static MethodInfo newMethod;
        public static List<int> argumentPairs;
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var bArgs = GetArguments(method);
            var nArgs = GetArguments(newMethod);
            CodeInstruction GetArg(int index) => (index >= 0 && index <= 3) ? new CodeInstruction(new[] { OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3 }[index]) : new CodeInstruction(OpCodes.Ldarg_S, index);
            var code = new List<CodeInstruction>();
            for (int i = 0; i < argumentPairs.Count; i++)
            {
                if (argumentPairs[i] == -1)
                {
                    code.AddRange(new[]
                        {
                            new CodeInstruction(OpCodes.Ldtoken,modClass),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type),"GetTypeFromHandle")),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtraSettingsAPI),nameof(ExtraSettingsAPI.GetMod)))
                        });
                    if (CanCastTo(nArgs[i], typeof(EventCaller)))
                        code.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtraSettingsAPI), nameof(ExtraSettingsAPI.GetCallerFromMod))));
                }
                else if (argumentPairs[i] != -1 && CanCastTo(bArgs[argumentPairs[i]], nArgs[i]))
                    code.Add(GetArg(argumentPairs[i]));
                else if (CanCastTo(nArgs[i], typeof(Mod)))
                        code.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtraSettingsAPI), nameof(ExtraSettingsAPI.GetCallerFromMod))));
                    else
                        code.AddRange(new[]
                        {
                            GetArg(argumentPairs[i]),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EventCaller),nameof(parent)))
                        });
            }
            code.AddRange(new[]
            {
                    new CodeInstruction(OpCodes.Call, newMethod),
                    new CodeInstruction(OpCodes.Ret)
                });
            return code;
        }

        public static bool CheckPatchParameters(MethodInfo caller, MethodInfo target,out List<int> patchParams, out int skipped)
        {
            var callerParams = GetArguments(caller);
            var targetParams = GetArguments(target);
            patchParams = null;
            skipped = 0;
            if (CanCastTo(target.ReturnType,caller.ReturnType))
            {
                var l = new List<int>();
                int i = 0;
                while (i < callerParams.Count || l.Count < targetParams.Count)
                    if (l.Count >= targetParams.Count)
                    {
                        skipped += callerParams.Count - i - 1;
                        break;
                    }
                    else if (i < callerParams.Count && CanCastTo(callerParams[i], targetParams[l.Count], true))
                    {
                        l.Add(i);
                        i++;
                    }
                    else
                    {
                        skipped++;
                        if (CanCastTo(targetParams[l.Count], typeof(Mod), true))
                            l.Add(-1);
                        else
                            i++;
                    }
                if (l.Count >= targetParams.Count)
                {
                    patchParams = l;
                    return true;
                }
                //Debug.LogWarning($"Argument mismatch fail\nCaller arguments: {callerParams.Join(x => x.FullName)}\nTarget arguments: {targetParams.Join(x => x.FullName)}\nSkipped: {skipped}\nArgument connections: {l.Join()}");
            }
            //else
                //Debug.LogWarning($"Return type fail");
            return false;
        }
        static bool CanCastTo(Type objType, Type targetType, bool includeCustomCast = false)
        {
            if (targetType.IsAssignableFrom(objType))
                return true;
            if (includeCustomCast)
            {
                var f1 = CanCastTo(objType, typeof(EventCaller)) || CanCastTo(objType, typeof(Mod));
                var f2 = CanCastTo(targetType, typeof(EventCaller)) || CanCastTo(targetType, typeof(Mod));
                if (f1 && f2)
                    return true;
            }
            return false;
        }

        static List<Type> GetArguments(MethodBase method)
        {
            var l = new List<Type>();
            if (!method.IsStatic)
                l.Add(method.DeclaringType);
            foreach (var p in method.GetParameters())
                l.Add(p.ParameterType);
            return l;
        }
    }

    public void Call(EventTypes eventType)
    {
        if (eventType == EventTypes.Button)
            return;
        if (eventType == EventTypes.Open)
        {
            ExtraSettingsAPI.generateSettings(parent);
            Call(EventTypes.Create);
        }
        if (eventType == EventTypes.Load)
            APIBool.Value = true;
        if (eventType == EventTypes.Unload)
            APIBool.Value = false;
        if (settingsEvents[eventType].MethodExists())
            try
            {
                settingsEvents[eventType].GetValue();
            }
            catch (Exception e)
            {
                ExtraSettingsAPI.LogError($"An exception occured in the setting {eventType} event of the {parent.modlistEntry.jsonmodinfo.name} mod\n{e.InnerException??e}");
            }
    }

    public void ButtonPress(ModSetting_Button button)
    {
        modTraverse.Method(EventNames[EventTypes.Button], new Type[] { typeof(string) }, new object[] { button.name }).GetValue();
    }
    public void ButtonPress(ModSetting_MultiButton button,int index)
    {
        modTraverse.Method(EventNames[EventTypes.Button], new Type[] { typeof(string), typeof(int) }, new object[] { button.name, index }).GetValue();
    }

    public string GetSliderText(ModSetting_Slider slider)
    {
        try
        {
            var t = modTraverse.Method(EventNames[EventTypes.Slider], slider.name, slider.value);
            if (!t.MethodExists())
            {
                ExtraSettingsAPI.LogWarning($"{parent.name} does not contain an appropriate definition for {EventNames[EventTypes.Slider]}. Setting {slider.nameText} requires this because its display mode is {slider.valueType}");
                return "{null}";
            }
            var r = t.GetValue();
            if (r is string s)
                return s;
            if (r != null)
                return r.ToString();
        } catch (Exception e)
        {
            Debug.LogError(e);
        }
        return "{null}";
    }

    public bool GetSettingVisible(ModSetting setting)
    {
        try
        {
            var t = modTraverse.Method(EventNames[EventTypes.Access], setting.name);
            if (!t.MethodExists())
            {
                ExtraSettingsAPI.LogWarning($"{parent.name} does not contain an appropriate definition for {EventNames[EventTypes.Access]}. Setting {setting.nameText} requires this because its access mode is {setting.access}");
                return false;
            }
            var r = t.GetValue();
            if (r is bool b)
                return b;
            ExtraSettingsAPI.LogWarning($"Return value of {EventNames[EventTypes.Access]} must be a bool. Mod {parent.name} returned {r?.GetType().ToString() ?? "null"}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return false;
    }

    public bool Equals(Mod obj)
    {
        if (!obj || GetType() != obj.GetType())
        {
            return false;
        }
        return parent == obj;
    }

    public enum EventTypes
    {
        Open,
        Close,
        Load,
        Unload,
        Button,
        Create,
        Slider,
        Access
    }
    public static Dictionary<EventTypes, string> EventNames = new Dictionary<EventTypes, string>
    {
        { EventTypes.Open, "ExtraSettingsAPI_SettingsOpen" },
        { EventTypes.Close, "ExtraSettingsAPI_SettingsClose" },
        { EventTypes.Load, "ExtraSettingsAPI_Load" },
        { EventTypes.Unload, "ExtraSettingsAPI_Unload" },
        { EventTypes.Button, "ExtraSettingsAPI_ButtonPress" },
        { EventTypes.Create, "ExtraSettingsAPI_SettingsCreate" },
        { EventTypes.Slider, "ExtraSettingsAPI_HandleSliderText" },
        { EventTypes.Access, "ExtraSettingsAPI_HandleSettingVisible" }
    };
}

public class ModSetting
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
        control.transform.SetParent(ExtraSettingsAPI.newOptCon.transform, false);
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
}

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
}

public class ModSetting_Combobox : ModSetting
{
    public Dropdown combobox = null;
    public string defaultValue;
    string[] values;
    string[] defaultValues;
    public string value;
    public int index;
    bool contentHasChanged;
    public ModSetting_Combobox(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
        contentHasChanged = false;
        defaultValue = source.GetField("default").str;
        if (source.HasField("values"))
        {
            List<JSONObject> items = source.GetField("values").list;
            defaultValues = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                defaultValues[i] = items[i].str;
            }
        }
        else
        {
            defaultValues = new string[0];
        }
        values = defaultValues;
        if (access.NotWorldSave())
            LoadSettings();
        else
            value = defaultValue;
        index = values.IndexOf(value);
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        combobox = control.GetComponentInChildren<Dropdown>(true);
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        foreach (string item in values)
            options.Add(new Dropdown.OptionData(item));
        combobox.ClearOptions();
        combobox.AddOptions(options);
        SetValue(value);
        index = combobox.value;
        combobox.onValueChanged.AddListener(delegate { index = combobox.value; value = values[index]; });
    }

    public void SetValue(int newValue)
    {
        if (!combobox)
        {
            index = newValue;
            value = values[index];
        }
        else
            combobox.value = newValue;
    }

    public void SetValue(string newValue)
    {
        if (!combobox)
        {
            index = Math.Max(values.IndexOf(newValue), 0);
            value = values[index];
        }
        else
            combobox.value = Math.Max(values.IndexOf(newValue), 0);
    }

    public override void Create()
    {
        SetGameObject(Object.Instantiate(ExtraSettingsAPI.comboboxPrefab));
    }

    public override void Destroy()
    {
        base.Destroy();
        combobox = null;
    }

    public override JSONObject GenerateSaveJson()
    {
        JSONObject store = new JSONObject();
        store.AddField("value", value);
        if (contentHasChanged)
        {
            JSONObject store2 = new JSONObject();
            foreach (string item in values)
                store2.Add(item);
            store.AddField("values", store2);
        }
        return store;
    }

    public override void LoadSettings()
    {
        JSONObject saved = parent.GetSavedSettings(this);
        if (saved != null && !saved.IsNull)
        {
            if (saved.IsString)
            {
                resetContent();
                SetValue(saved.str);
            }
            else
            {
                if (saved.HasField("values") && saved.GetField("values").IsArray)
                {
                    setContent(new string[0]);
                    foreach (JSONObject JSON in saved.GetField("values").list)
                        addContent(JSON.str);
                }
                else
                    resetContent();
                if (saved.HasField("value"))
                {
                    if (saved.GetField("value").IsNumber)
                        SetValue((int)saved.GetField("value").n);
                    else
                        SetValue(saved.GetField("value").IsString ? saved.GetField("value").str : defaultValue);
                }
                else
                    SetValue(defaultValue);
            }
        }
        else
        {
            resetContent();
            SetValue(defaultValue);
        }
        index = values.IndexOf(value);
    }

    public void setContent(string[] items)
    {
        contentHasChanged = true;
        values = items;
        if (combobox)
        {
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (string item in items)
                options.Add(new Dropdown.OptionData(item));
            combobox.options = options;
        }
    }

    public void addContent(string item)
    {
        contentHasChanged = true;
        Array.Resize(ref values, values.Length + 1);
        values[values.Length - 1] = item;
        if (combobox)
            combobox.options.Add(new Dropdown.OptionData(item));
    }

    public string[] getContent()
    {
        return values;
    }

    public void resetContent()
    {
        contentHasChanged = false;
        values = defaultValues;
        if (combobox)
        {
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (string item in values)
                options.Add(new Dropdown.OptionData(item));
            combobox.options = options;
        }
    }

    public override void ResetValue()
    {
        resetContent();
        SetValue(defaultValue);
    }
}

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
        {
            UIslider.enabled = false;
        }
        switch (valueType)
        {
            case SliderType.Percent:
                sliderText.text = Math.Round(slider.value * 100, rounding) + "%";
                break;
            case SliderType.Custom:
                sliderText.text = ExtraSettingsAPI.mods[parent.parent].GetSliderText(this);
                break;
            default:
                sliderText.text = Math.Round(slider.value, rounding).ToString();
                break;
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
}

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

public class ModSetting_Button : ModSetting
{
    public Button button = null;
    public ModSetting_Button(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        button = control.GetComponentInChildren<Button>(true);
        Vector2 sizeDif = new Vector2(text.preferredWidth + text.preferredHeight - (button.transform as RectTransform).offsetMax.x, 0);
        (button.transform as RectTransform).offsetMax += sizeDif;
        button.onClick.AddListener(() => ExtraSettingsAPI.mods[parent.parent].ButtonPress(this));
    }

    public override void Create()
    {
        SetGameObject(Object.Instantiate(ExtraSettingsAPI.buttonPrefab));
    }

    public override void Destroy()
    {
        base.Destroy();
        button = null;
    }

    public override void SetText(string newText)
    {
        base.SetText(newText);
        if (button)
        {
            Vector2 sizeDif = new Vector2(text.preferredWidth + text.preferredHeight - (button.transform as RectTransform).offsetMax.x, 0);
            (button.transform as RectTransform).offsetMax += sizeDif;
        }

    }
}

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
}

public class ModSetting_Text : ModSetting
{
    public ModSetting_Text(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
    }

    public override void Create()
    {
        SetGameObject(Object.Instantiate(ExtraSettingsAPI.textPrefab));
    }
}

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

public class ModSetting_MultiButton : ModSetting
{
    public Button[] buttons;
    public string[] names;
    public string[] defaultNames;
    public Transform container;
    public ModSetting_MultiButton(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
        buttons = new Button[0];
        if (source == null || source.IsNull || !source.HasField("buttons") || !source.GetField("buttons").IsArray)
            defaultNames = new string[0];
        else
            defaultNames = source.GetField("buttons").ToStringArray();
        SetValue(defaultNames);
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        container = control.GetComponentInChildren<HorizontalLayoutGroup>(true).transform;
        SetValue(names);
    }

    public override void Create()
    {
        SetGameObject(Object.Instantiate(ExtraSettingsAPI.multibuttonPrefab));
    }

    public override void Destroy()
    {
        base.Destroy();
        buttons = new Button[0];
    }

    public override void ResetValue()
    {
        base.ResetValue();
        SetValue(defaultNames);
    }

    public override void SetText(string newText) { }

    public void SetValue(string[] newText)
    {
        names = newText ?? new string[0];
        if (container)
        {
            var c = Math.Max(buttons.Length, names.Length);
            for (int i = 0; i < c; i++)
                if (i >= names.Length)
                    Object.Destroy(buttons[i]);
                else {
                    var button = i < buttons.Length ? buttons[i] : null;
                    if (!button)
                    {
                        button = Object.Instantiate(ExtraSettingsAPI.multibuttonChildPrefab, container);
                        var j = i;
                        button.onClick.AddListener(() => ExtraSettingsAPI.mods[parent.parent].ButtonPress(this, j));
                    }
                    var t = button.GetComponentInChildren<Text>(true);
                    t.text = names[i];
                    var r = button.transform as RectTransform;
                    r.sizeDelta += new Vector2(t.preferredWidth - t.preferredHeight + r.rect.height - r.rect.width, 0);
                }
            buttons = container.GetComponentsInChildren<Button>(true);
        }
    }
    public string[] GetValue()
    {
        if (names != null)
            return names;
        return null;
    }
}

public class ModSetting_Section : ModSetting
{
    public bool open = false;
    public ModSetting_Section(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        control.GetComponentInChildren<Toggle>(true).onValueChanged.AddListener(x => {
            open = x;
            parent.ToggleSettings();
            ExtraSettingsAPI.UpdateAllSettingBacks();
        });
    }

    public override void Create()
    {
        SetGameObject(Object.Instantiate(ExtraSettingsAPI.sectionPrefab));
    }

    public override void Destroy()
    {
        base.Destroy();
    }
}

public class ModSettingContainer
{
    public string ModName { get; }
    public string IDName { get; }
    public Mod parent { get; }
    public JSONObject settingsJson;
    public Dictionary<string, ModSetting> settings = new Dictionary<string, ModSetting>();
    public List<ModSetting> allSettings = new List<ModSetting>();
    public GameObject title = null;
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
        title.transform.SetParent(ExtraSettingsAPI.newOptCon.transform, false);
        Text text = title.GetComponentInChildren<Text>(true);
        text.text = "-------- " + ModName;
        text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
        foreach (var setting in allSettings)
            setting.Create();
        ToggleSettings();
        title.GetComponentInChildren<Toggle>(true).onValueChanged.AddListener(x => {
            ToggleSettings(x);
            ExtraSettingsAPI.UpdateAllSettingBacks();
        });
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