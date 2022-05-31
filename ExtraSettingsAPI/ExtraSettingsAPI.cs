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
    public static Dictionary<Mod, modSettingContainer> modSettings;
    public static Dictionary<Mod, EventCaller> mods;
    public static bool init;
    Harmony harmony;
    public static GameObject sliderPrefab;
    public static GameObject checkboxPrefab;
    public static GameObject comboboxPrefab;
    public static GameObject keybindPrefab;
    public static GameObject buttonPrefab;
    public static GameObject textPrefab;
    public static GameObject titlePrefab;
    public static GameObject inputPrefab;
    public static ColorBlock keybindColors;
    public static List<ModData> waitingToLoad;
    public static Traverse self;
    public void Start()
    {
        init = true;
        sliderPrefab = null;
        checkboxPrefab = null;
        comboboxPrefab = null;
        keybindPrefab = null;
        buttonPrefab = null;
        textPrefab = null;
        titlePrefab = null;
        inputPrefab = null;
        modSettings = new Dictionary<Mod, modSettingContainer>();
        mods = new Dictionary<Mod, EventCaller>();
        waitingToLoad = new List<ModData>();
        modInfo = modlistEntry.jsonmodinfo;
        instance = this;
        configPath = Path.Combine(SaveAndLoad.WorldPath, modInfo.name + ".json");
        if (settingsController.IsOpen)
            insertNewSettingsMenu();
        Config = getSaveJson();
        if (RAPI.GetLocalPlayer() != null)
            loadLocal(true);
        self = Traverse.Create(this);
        loadAllSettings();
        keybindColors = new ColorBlock();
        keybindColors.disabledColor = new Color(0.772f, 0.233f, 0.170f, 0.502f);
        keybindColors.highlightedColor = new Color(0.956f, 0.893f, 0.759f, 1.000f);
        keybindColors.normalColor = new Color(0.733f, 0.631f, 0.416f, 1.000f);
        keybindColors.pressedColor = new Color(0.733f, 0.631f, 0.416f, 1.000f);
        keybindColors.selectedColor = new Color(0.956f, 0.893f, 0.759f, 1.000f);
        harmony = new Harmony("com.aidanamite.ExtraSettingsAPI");
        harmony.PatchAll();
        Log("Mod has been loaded!");
    }

    public void Update()
    {
        if (waitingToLoad.Count > 0)
            for (int i = waitingToLoad.Count - 1; i >= 0; i--)
                if (waitingToLoad[i].modinfo.modState == ModInfo.ModStateEnum.running && waitingToLoad[i].modinfo.mainClass != null)
                    tryLoadSettings(waitingToLoad[i]);
                else if (waitingToLoad[i].modinfo.modState == ModInfo.ModStateEnum.errored)
                    waitingToLoad.RemoveAt(i);
    }

    public void OnModUnload()
    {
        harmony.UnpatchAll(harmony.Id);
        if (!init)
            removeNewSettingsMenu();
        Log("Mod has been unloaded!");
    }

    public static void Log(object message)
    {
        Debug.Log("[" + modInfo.name + "]: " + message.ToString());
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

    public static void ErrorLog(object message)
    {
        Debug.LogError("[" + modInfo.name + "]: " + message.ToString());
    }

    public static void ErrorLog(Exception err)
    {
        ErrorLog(err.GetType() + "\n" + err.Message + "\n" + err.StackTrace);
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
            ErrorLog("An error occured while trying to save settings: " + err.Message);
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
        foreach (modSettingContainer container in modSettings.Values)
            store.TrySetField(container.IDName, container.generateSaveJson(path != ""));

        saveJson(data, path);
        CultureInfo.CurrentCulture = current;
    }

    public static void loadLocal(bool loadSave)
    {
        if (loadSave)
            LocalConfig = getSaveJson(true);
        else
            LocalConfig = new JSONObject();
        foreach (modSettingContainer container in modSettings.Values)
            container.loadLocal();
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
        Vector2 newSize = backTransform.anchorMax + new Vector2(0.13f, 0f);
        int newIndex = tabButtons.Value.Length;
        GameObject settingsSet = OptionMenuContainer.transform.FindChildRecursively(sourceName).gameObject;
        newSet = Instantiate(settingsSet);
        GameObject settingsTab = OptionMenuContainer.transform.FindChildRecursively(sourceName + "Tab").gameObject;
        newTab = Instantiate(settingsTab);
        newSet.name = newName;
        newTab.name = newName + "Tab";
        newTab.transform.SetParent(settingsTab.transform.parent, false);
        newSet.transform.SetParent(settingsSet.transform.parent, false);
        newSet.SetActive(false);
        (newTab.transform as RectTransform).pivot = new Vector2(0f, 1f);
        Transform setBox = newSet.transform.FindChildRecursively(sourceName + "SettingsBox");
        if (setBox != null)
        {
            setBox.SetParent(null);
            Destroy(setBox.gameObject);
        }
        ScrollRect scrollRect = newSet.GetComponentInChildren<ScrollRect>();
        Scrollbar scrollbar = newSet.GetComponentInChildren<Scrollbar>();
        scrollRect.verticalScrollbar = scrollbar;
        scrollbar.value = 1;
        VerticalLayoutGroup verticalLayoutGroup = newSet.GetComponentInChildren<VerticalLayoutGroup>(); // This will fetch null if copied tab is "General"
        ContentSizeFitter contentSizeFitter = verticalLayoutGroup.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
        newOptCon = newSet.transform.FindChildRecursively("Content").gameObject;
        foreach (Transform transform in newOptCon.transform)
        {
            if (comboboxPrefab == null && transform.gameObject.GetComponentInChildren<Dropdown>() != null)
            {
                comboboxPrefab = Instantiate(transform.gameObject, null, false);
                comboboxPrefab.name = "DropDownPrefab";
                Dropdown drop = comboboxPrefab.GetComponentInChildren<Dropdown>();
                drop.onValueChanged = new Dropdown.DropdownEvent();
                foreach (LocalizeDropdownSemih localize in drop.gameObject.GetComponentsInChildren<LocalizeDropdownSemih>())
                {
                    localize.enabled = false;
                    DestroyImmediate(localize, true);
                }
                drop.ClearOptions();
                drop.AddOptions(new List<Dropdown.OptionData> { new Dropdown.OptionData("test") });
                drop.itemText.text = drop.options[0].text;
                comboboxPrefab.GetComponentInChildren<Text>().text = "Option Name";
                destroyLocalizations(comboboxPrefab);
            }
            if (sliderPrefab == null && transform.gameObject.GetComponentInChildren<UISlider>() != null)
            {
                sliderPrefab = Instantiate(transform.gameObject, null, false);
                sliderPrefab.name = "SliderPrefab";
                Slider slide = sliderPrefab.GetComponentInChildren<Slider>();
                slide.onValueChanged = new Slider.SliderEvent();
                slide.minValue = 0;
                slide.maxValue = 1;
                slide.wholeNumbers = false;
                slide.value = 0.25f;
                sliderPrefab.GetComponentInChildren<Text>().text = "Option Name";
                destroyLocalizations(sliderPrefab);
            }
            if (checkboxPrefab == null && transform.gameObject.GetComponentInChildren<Toggle>() != null)
            {
                checkboxPrefab = Instantiate(transform.gameObject, null, false);
                checkboxPrefab.name = "CheckboxPrefab";
                Toggle checkbox = checkboxPrefab.GetComponentInChildren<Toggle>();
                checkbox.onValueChanged = new Toggle.ToggleEvent();
                checkbox.isOn = false;
                checkboxPrefab.GetComponentInChildren<Text>().text = "Option Name";
            }
            if (textPrefab == null && transform.gameObject.GetComponentInChildren<Toggle>() != null)
            {
                textPrefab = Instantiate(transform.gameObject, null, false);
                textPrefab.name = "TextPrefab";
                Toggle checkbox2 = textPrefab.GetComponentInChildren<Toggle>();
                checkbox2.onValueChanged = new Toggle.ToggleEvent();
                checkbox2.gameObject.SetActive(false);
                textPrefab.GetComponentInChildren<Text>().text = "Some Text";
            }
            if (titlePrefab == null && transform.gameObject.GetComponentInChildren<Toggle>() != null)
            {
                titlePrefab = Instantiate(transform.gameObject, null, false);
                titlePrefab.name = "TitlePrefab";
                Toggle checkbox3 = titlePrefab.GetComponentInChildren<Toggle>();
                DestroyImmediate(checkbox3.graphic.gameObject);
                //DestroyImmediate(checkbox3.gameObject);
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
                titlePrefab.GetComponentInChildren<Text>().text = "Mod Name";
                titlePrefab.AddImageObject(newSize.x);
            }
            Destroy(transform.gameObject);
        }
        foreach (Transform transform in OptionMenuContainer.transform.FindChildRecursively("Controls").gameObject.transform.FindChildRecursively("Content").gameObject.transform)
        {
            if (keybindPrefab == null && transform.GetComponentInChildren<KeybindInterface>() != null)
            {
                GameObject copiedObj = transform.FindChildRecursively("Sprint").gameObject;
                keybindPrefab = Instantiate(copiedObj, null, false);
                keybindPrefab.name = "KeybindPrefab";
                keybindPrefab.GetComponentInChildren<Text>().text = "Option Name";
                destroyLocalizations(keybindPrefab);
            }
            if (buttonPrefab == null && transform.gameObject.GetComponentInChildren<Button>() != null)
            {
                buttonPrefab = Instantiate(transform.gameObject, null, false);
                buttonPrefab.name = "ButtonPrefab";
                Button button = buttonPrefab.GetComponentInChildren<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.GetComponentInChildren<Text>().text = "Button Name";
                destroyLocalizations(buttonPrefab);
            }
        }
        inputPrefab = Instantiate(textPrefab, null, false);
        inputPrefab.name = "InputPrefab";
        GameObject inputF = Instantiate(comboboxPrefab.transform.Find("Dropdown").gameObject, inputPrefab.transform, false);
        inputF.name = "InputField";
        (inputF.transform as RectTransform).offsetMin *= new Vector2(1.5f, 1);
        DestroyImmediate(inputF.GetComponent<Dropdown>(), true);
        InputField tmp = inputF.AddComponent<InputField>();
        tmp.textComponent = inputF.transform.Find("Label").GetComponent<Text>();
        tmp.textComponent.rectTransform.offsetMax = -tmp.textComponent.rectTransform.offsetMin;
        DestroyImmediate(inputF.transform.Find("Arrow").gameObject, true);
        DestroyImmediate(inputF.transform.Find("Template").gameObject, true);

        newTabBut = newTab.GetComponent<TabButton>();
        destroyLocalizations(newTabBut.gameObject);
        Text newTabTex = newTabBut.GetComponentInChildren<Text>();
        newTabBut.tabIndex = newIndex;
        newTabTex.text = newName;
        newTabBut.name = newTab.name;
        newTabBut.OnPointerExit(true);
        Traverse tabTraverse = Traverse.Create(newTabBut);
        tabTraverse.Field("tabButton").SetValue(newTabBut.GetComponentInChildren<Button>());
        tabTraverse.Field("tab").SetValue(newSet);
        var buttons = tabButtons.Value;
        Add(ref buttons, newTabBut);
        tabButtons.Value = buttons;
        // Adjusts the settings menu size so tab buttons fit nicely
        backTransform.anchorMax = newSize;
        divTransform.anchorMax = newSize;
        contentTransform.anchorMax = newSize;
        closeTransform.anchorMin = newSize;
        closeTransform.anchorMax = newSize;
        tabsTransform.anchorMax = newSize;

        init = false;
    }

    public static void removeNewSettingsMenu()
    {
        init = true;
        if (tabGroup.SelectedTabButton == newTabBut)
            tabGroup.SelectTab(0);
        Vector2 newSize = backTransform.anchorMax - new Vector2(0.13f, 0f);
        backTransform.anchorMax = newSize;
        divTransform.anchorMax = newSize;
        contentTransform.anchorMax = newSize;
        closeTransform.anchorMin = newSize;
        closeTransform.anchorMax = newSize;
        tabsTransform.anchorMax = newSize;
        var buttons = tabButtons.Value;
        Remove(ref buttons, newTabBut);
        tabButtons.Value = buttons;
        Destroy(newTabBut);
        Destroy(newSet);
        Destroy(newTab);
    }

    public static void destroyLocalizations(GameObject gO)
    {
        foreach (Localize localize in gO.GetComponentsInChildren<Localize>())
        {
            localize.enabled = false;
            DestroyImmediate(localize, true);
        }
    }

    public static void generateSettings(Mod mod)
    {
        generateSettings(modSettings[mod]);
    }

    public static void generateSettings(modSettingContainer container)
    {
        container.create();
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

    public static void toggleSettings(bool isOnMainmenu)
    {
        foreach (modSettingContainer container in modSettings.Values)
            container.toggleSettings(isOnMainmenu);
    }

    public static void loadAllSettings()
    {
        foreach (ModData mod in ModManagerPage.modList)
            tryLoadSettings(mod);
    }

    public static void loadSettings(ModData data, JSONObject settings)
    {
        Mod mod = data.modinfo.mainClass;
        if (mod == null)
        {
            waitingToLoad.Add(data);
            return;
        }
        if (mods.ContainsKey(mod))
            return;
        if (waitingToLoad.Contains(data))
            waitingToLoad.Remove(data);
        try
        {
            modSettingContainer newSettings = new modSettingContainer(mod, settings);
            EventCaller caller = new EventCaller(mod);
            modSettings.Add(mod, newSettings);
            mods.Add(mod, caller);
            caller.Call(EventCaller.EventTypes.load);
            if (!init)
                caller.Call(EventCaller.EventTypes.open);
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
                tryLoadSettings(mod);
    }

    public static void tryLoadSettings(Mod mod)
    {
        tryLoadSettings(mod.modlistEntry);
    }

    public static void tryLoadSettings(ModData data)
    {
        var current = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-NZ");
        JSONObject modJson = new JSONObject(Encoding.Default.GetString(data.modinfo.modFiles["modinfo.json"]));
        CultureInfo.CurrentCulture = current;
        if (modJson.HasField("modSettings"))
            loadSettings(data, modJson.GetField("modSettings"));
    }

    public static void updateAllSettingBacks()
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
                foreach (var s in c.Value.settings)
                    if (s != null && s.control && s.control.activeSelf && s.setBackImage(f))
                        f = !f;
            }
    }

    public static void modUnloaded(Mod mod)
    {
        removeSettings(mod);
    }

    public static void removeSettings(Mod mod)
    {
        if (mod == null || !mods.ContainsKey(mod))
            return;
        modSettings[mod].destroy();
        modSettings.Remove(mod);
        mods.Remove(mod);
    }

    public static T getSetting<T>(Mod mod, string settingName) where T : modSetting
    {
        foreach (modSetting setting in modSettings[mod].settings)
            if (setting is T && setting.name == settingName)
                return setting as T;
        foreach (KeyValuePair<modSetting.settingType, Type> pair in modSetting.matches)
            if (pair.Value == typeof(T))
                throw new NullReferenceException("Could not find " + pair.Key.ToString() + " setting " + settingName + " for " + mod.modlistEntry.jsonmodinfo.name);
        throw new NullReferenceException("Could not find " + typeof(T).Name + " setting " + settingName + " for " + mod.modlistEntry.jsonmodinfo.name);
    }

    public static bool getCheckboxState(Mod mod, string settingName)
    {
        return getSetting<modSetting_checkbox>(mod, settingName).value;
    }

    public static int getComboboxSelectedIndex(Mod mod, string settingName)
    {
        return getSetting<modSetting_combobox>(mod, settingName).index;
    }

    public static string getComboboxSelectedItem(Mod mod, string settingName)
    {
        return getSetting<modSetting_combobox>(mod, settingName).value;
    }

    public static string[] getComboboxContent(Mod mod, string settingName)
    {
        return getSetting<modSetting_combobox>(mod, settingName).getContent();
    }

    public static float getSliderValue(Mod mod, string settingName)
    {
        modSetting_slider slider = getSetting<modSetting_slider>(mod, settingName);
        return slider.roundValue;
    }

    public static float getSliderRealValue(Mod mod, string settingName)
    {
        return getSetting<modSetting_slider>(mod, settingName).value;
    }

    public static string getKeybindName(Mod mod, string settingName)
    {
        return getKeybind(mod, settingName).Identifier;
    }

    public static KeyCode getKeybind_main(Mod mod, string settingName)
    {
        return getKeybind(mod, settingName).MainKey;
    }

    public static KeyCode getKeybind_alt(Mod mod, string settingName)
    {
        return getKeybind(mod, settingName).AltKey;
    }

    public static Keybind getKeybind(Mod mod, string settingName)
    {
        return getSetting<modSetting_keybind>(mod, settingName).value;
    }

    public static string getInputValue(Mod mod, string settingName)
    {
        return getSetting<modSetting_input>(mod, settingName).value;
    }

    public static string getDataValue(Mod mod, string settingName, string subname)
    {
        return getSetting<modSetting_data>(mod, settingName).getValue(subname);
    }

    public static string[] getDataNames(Mod mod, string settingName)
    {
        return getSetting<modSetting_data>(mod, settingName).getNames();
    }

    public static string getSettingsText(Mod mod, string settingName)
    {
        return getSetting<modSetting>(mod, settingName).nameText;
    }

    public static void setCheckboxState(Mod mod, string settingName, bool value)
    {
        getSetting<modSetting_checkbox>(mod, settingName).setValue(value);
    }

    public static void setComboboxSelectedIndex(Mod mod, string settingName, int value)
    {
        getSetting<modSetting_combobox>(mod, settingName).setValue(value);
    }

    public static void setComboboxSelectedItem(Mod mod, string settingName, string value)
    {
        getSetting<modSetting_combobox>(mod, settingName).setValue(value);
    }

    public static void setComboboxContent(Mod mod, string settingName, string[] items)
    {
        getSetting<modSetting_combobox>(mod, settingName).setContent(items);
    }

    public static void addComboboxContent(Mod mod, string settingName, string item)
    {
        getSetting<modSetting_combobox>(mod, settingName).addContent(item);
    }

    public static void resetComboboxContent(Mod mod, string settingName)
    {
        getSetting<modSetting_combobox>(mod, settingName).resetContent();
    }

    public static void setSliderValue(Mod mod, string settingName, float value)
    {
        getSetting<modSetting_slider>(mod, settingName).setValue(value);
    }

    public static void setKeybind_main(Mod mod, string settingName, KeyCode value)
    {
        getSetting<modSetting_keybind>(mod, settingName).setValue(value, true);
    }

    public static void setKeybind_alt(Mod mod, string settingName, KeyCode value)
    {
        getSetting<modSetting_keybind>(mod, settingName).setValue(value, false);
    }

    public static void setInputValue(Mod mod, string settingName, string value)
    {
        getSetting<modSetting_input>(mod, settingName).setValue(value);
    }

    public static void setDataValue(Mod mod, string settingName, string subname, string value)
    {
        getSetting<modSetting_data>(mod, settingName).setValue(subname, value);
    }

    public static void setSettingsText(Mod mod, string settingName, string newText)
    {
        getSetting<modSetting>(mod, settingName).setText(newText);
    }

    public static void resetSetting(Mod mod, string settingName)
    {
        getSetting<modSetting>(mod, settingName).resetValue();
    }

    public static void resetSettings(Mod mod)
    {
        foreach (modSetting setting in modSettings[mod].settings)
            setting.resetValue();
    }
}

static public class ExtentionMethods
{
    public static int IndexOf<T>(this T[] array, T item)
    {
        return Array.IndexOf(array, item);
    }
    public static GameObject AddImageObject(this GameObject gameObject, float scale)
    {
        GameObject imageContainer = new GameObject();
        imageContainer.transform.SetParent(gameObject.transform, false);
        RectTransform trans = imageContainer.AddComponent<RectTransform>();
        trans.localScale = Vector3.one;
        trans.anchoredPosition = Vector2.zero;
        Image image = imageContainer.AddComponent<Image>();
        image.rectTransform.offsetMin = new Vector2(-370 * scale, -30);
        image.rectTransform.offsetMax = new Vector2(370 * scale, -20f);
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
}

[HarmonyPatch(typeof(Settings), "Open")]
public class Patch_SettingsOpen
{
    static void Postfix()
    {
        ExtraSettingsAPI.insertNewSettingsMenu();
        foreach (EventCaller caller in ExtraSettingsAPI.mods.Values)
            caller.Call(EventCaller.EventTypes.open);
    }
}

[HarmonyPatch(typeof(Settings), "Close")]
public class Patch_SettingsClose
{
    static void Prefix(ref Settings __instance, ref bool __state)
    {
        __state = Traverse.Create(__instance).Field("optionsCanvas").GetValue<GameObject>().activeInHierarchy;
    }
    static void Postfix(ref bool __state)
    {
        if (__state)
        {
            ExtraSettingsAPI.generateSaveJson();

            foreach (EventCaller caller in ExtraSettingsAPI.mods.Values)
                caller.Call(EventCaller.EventTypes.close);
            if (!ExtraSettingsAPI.init)
                ExtraSettingsAPI.removeNewSettingsMenu();
        }
    }
}

[HarmonyPatch(typeof(BaseModHandler), "LoadMod")]
public class Patch_ModLoad
{
    static void Postfix(ref ModData moddata)
    {
        ExtraSettingsAPI.tryLoadSettings(moddata);
    }
}

[HarmonyPatch(typeof(BaseModHandler), "UnloadMod")]
public class Patch_ModUnload
{
    static void Postfix(ref ModData moddata)
    {
        ExtraSettingsAPI.removeSettings(moddata.modinfo.mainClass);
    }
}

[HarmonyPatch(typeof(UISlider), "Update")]
public class Patch_SliderUpdate
{
    static bool Prefix(ref UISlider __instance)
    {
        if (__instance.name.StartsWith("ESAPI_"))
            foreach (modSettingContainer container in ExtraSettingsAPI.modSettings.Values)
                foreach (modSetting setting in container.settings)
                    if (setting is modSetting_slider && (setting as modSetting_slider).UIslider == __instance)
                    {
                        setting.update();
                        return false;
                    }
        return true;
    }
}

[HarmonyPatch(typeof(MyInput), "IdentifierToKeybind")]
public class Patch_KeybindsReset
{
    static bool Prefix(ref string identifier, ref Keybind __result)
    {
        if (modSetting_keybind.MyKeys != null && modSetting_keybind.MyKeys.Count > 0 && modSetting_keybind.MyKeys.ContainsKey(identifier))
        {
            __result = modSetting_keybind.MyKeys[identifier];
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(SaveAndLoad), "Save")]
public class Patch_SaveGame
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
public class Patch_LoadGame
{
    static void Postfix()
    {
        ExtraSettingsAPI.loadLocal(true);
    }
}

[HarmonyPatch(typeof(NewGameBox), "Button_CreateNewGame")]
public class Patch_NewGame
{
    static void Postfix()
    {
        ExtraSettingsAPI.loadLocal(false);
    }
}

[HarmonyPatch(typeof(LoadSceneManager), "LoadScene")]
public class Patch_UnloadWorld
{
    static void Postfix(ref string sceneName)
    {
        if (sceneName == Semih_Network.MenuSceneName)
            ExtraSettingsAPI.LocalConfig = null;
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

public class EventCaller
{
    public Mod parent { get; }
    Traverse modTraverse;
    Dictionary<EventTypes, Traverse> settingsEvents = new Dictionary<EventTypes, Traverse>();
    Traverse<bool> APIBool;
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
                modTraverse = Traverse.Create(settingsField.GetValue());
        }
        foreach (KeyValuePair<EventTypes, string> pair in EventNames)
            if (pair.Key != EventTypes.button)
                settingsEvents.Add(pair.Key, modTraverse.Method(pair.Value, new Type[] { }, new object[] { }));
        APIBool = modTraverse.Field<bool>("ExtraSettingsAPI_Loaded");
        modTraverse.Field<Traverse>("ExtraSettingsAPI_Traverse").Value = ExtraSettingsAPI.self;
    }

    public void Call(EventTypes eventType)
    {
        if (eventType == EventTypes.button)
            return;
        if (eventType == EventTypes.open)
        {
            ExtraSettingsAPI.generateSettings(parent);
            Call(EventTypes.create);
        }
        if (eventType == EventTypes.load)
            APIBool.Value = true;
        if (eventType == EventTypes.unload)
            APIBool.Value = false;
        if (settingsEvents[eventType].MethodExists())
            try
            {
                settingsEvents[eventType].GetValue();
            }
            catch (Exception e)
            {
                ExtraSettingsAPI.ErrorLog($"An exception occured in the setting {eventType} event of the {parent.modlistEntry.jsonmodinfo.name} mod\n{e.InnerException}");
            }
    }

    public void ButtonPress(modSetting_button button)
    {
        modTraverse.Method(EventNames[EventTypes.button], new Type[] { typeof(string) }, new object[] { button.name }).GetValue();
    }

    public bool Equals(Mod obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        return parent == obj;
    }

    public enum EventTypes
    {
        open,
        close,
        load,
        unload,
        button,
        create
    }
    public static Dictionary<EventTypes, string> EventNames = new Dictionary<EventTypes, string>
    {
        { EventTypes.open, "ExtraSettingsAPI_SettingsOpen" },
        { EventTypes.close, "ExtraSettingsAPI_SettingsClose" },
        { EventTypes.load, "ExtraSettingsAPI_Load" },
        { EventTypes.unload, "ExtraSettingsAPI_Unload" },
        { EventTypes.button, "ExtraSettingsAPI_ButtonPress" },
        { EventTypes.create, "ExtraSettingsAPI_SettingsCreate" }
    };
}

public class modSetting
{
    public static bool useAlt = false;
    public string name { get; }
    public string nameText;
    public modSettingContainer parent;
    public Text text = null;
    public menuType access;
    public GameObject control { get; private set; } = null;
    Image backImage;

    public modSetting(JSONObject source, modSettingContainer parent)
    {
        name = source.GetField("name").str;
        if (source.HasField("text"))
            nameText = source.GetField("text").str;
        else
            nameText = name;
        access = menuType.both;
        if (source.HasField("access"))
            Enum.TryParse(source.GetField("access").str, true, out access);
        this.parent = parent;
    }

    public enum settingType
    {
        checkbox,
        slider,
        combobox,
        keybind,
        button,
        text,
        data,
        input
    }

    public enum menuType
    {
        both,
        mainmenu,
        world,
        globalworld
    }

    public static Dictionary<settingType, Type> matches = new Dictionary<settingType, Type>
    {
        {settingType.checkbox, typeof(modSetting_checkbox) },
        {settingType.slider, typeof(modSetting_slider) },
        {settingType.combobox, typeof(modSetting_combobox) },
        {settingType.keybind, typeof(modSetting_keybind) },
        {settingType.button, typeof(modSetting_button) },
        {settingType.text, typeof(modSetting_text) },
        {settingType.data, typeof(modSetting_data) },
        {settingType.input, typeof(modSetting_input) }
    };

    public static modSetting createSetting(JSONObject source, modSettingContainer parent)
    {
        settingType type;
        try
        {
            type = (settingType)Enum.Parse(typeof(settingType), source.GetField("type").str);
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
            return (modSetting)matches[type].GetConstructor(new Type[] { typeof(JSONObject), typeof(modSettingContainer) }).Invoke(new object[] { source, parent });
        }
        catch
        {
            throw new InvalidDataException("Failed to initialize a " + type + " for the " + parent.ModName + " mod");
        }
    }

    virtual public void setGameObject(GameObject go)
    {
        control = go;
        control.name = parent.ModName + "." + name;
        control.transform.SetParent(ExtraSettingsAPI.newOptCon.transform, false);
        text = control.GetComponentInChildren<Text>();
        text.text = nameText;
        if (!(this is modSetting_button))
        {
            text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
        }
        backImage = control.GetComponent<Image>() ?? control.GetComponentInChildren<Image>();
    }

    public bool setBackImage(bool state)
    {
        if (!backImage)
            return false;
        backImage.enabled = state;
        return true;
    }

    virtual public void setText(string newText)
    {
        nameText = newText;
        if (text != null)
        {
            text.text = newText;
            if (!(this is modSetting_button))
            {
                text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
            }
        }
    }

    virtual public void create() { }
    virtual public void destroy()
    {
        Object.Destroy(control);
        control = null;
    }
    virtual public void update() { }
    virtual public void loadSettings() { }
    virtual public void resetValue() { }
    virtual public JSONObject generateSaveJson() { return new JSONObject(); }
}

public class modSetting_checkbox : modSetting
{
    public Toggle checkbox = null;
    public bool defaultValue;
    public bool value;
    public modSetting_checkbox(JSONObject source, modSettingContainer parent) : base(source, parent)
    {
        defaultValue = source.GetField("default").b;
        if (access != menuType.world)
            loadSettings();
        else
            value = defaultValue;
    }

    public void setValue(bool newValue)
    {
        if (checkbox == null)
            value = newValue;
        else
            checkbox.isOn = newValue;
    }

    public override void setGameObject(GameObject go)
    {
        base.setGameObject(go);
        checkbox = control.GetComponentInChildren<Toggle>();
        setValue(value);
        checkbox.onValueChanged.AddListener(delegate { value = checkbox.isOn; });
    }

    public override void create()
    {
        setGameObject(Object.Instantiate(ExtraSettingsAPI.checkboxPrefab));
    }

    public override void destroy()
    {
        base.destroy();
        checkbox = null;
    }

    public override JSONObject generateSaveJson()
    {
        return new JSONObject(value);
    }

    public override void loadSettings()
    {
        JSONObject saved = parent.getSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.b;
        else
            value = defaultValue;
    }

    public override void resetValue()
    {
        setValue(defaultValue);
    }
}

public class modSetting_combobox : modSetting
{
    public Dropdown combobox = null;
    public string defaultValue;
    string[] values;
    string[] defaultValues;
    public string value;
    public int index;
    bool contentHasChanged;
    public modSetting_combobox(JSONObject source, modSettingContainer parent) : base(source, parent)
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
        if (access != menuType.world)
            loadSettings();
        else
            value = defaultValue;
        index = values.IndexOf(value);
    }

    public override void setGameObject(GameObject go)
    {
        base.setGameObject(go);
        combobox = control.GetComponentInChildren<Dropdown>();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        foreach (string item in values)
            options.Add(new Dropdown.OptionData(item));
        combobox.ClearOptions();
        combobox.AddOptions(options);
        setValue(value);
        index = combobox.value;
        combobox.onValueChanged.AddListener(delegate { index = combobox.value; value = values[index]; });
    }

    public void setValue(int newValue)
    {
        if (combobox == null)
        {
            index = newValue;
            value = values[index];
        }
        else
            combobox.value = newValue;
    }

    public void setValue(string newValue)
    {
        if (combobox == null)
        {
            index = Math.Max(values.IndexOf(newValue), 0);
            value = values[index];
        }
        else
            combobox.value = Math.Max(values.IndexOf(newValue), 0);
    }

    public override void create()
    {
        setGameObject(Object.Instantiate(ExtraSettingsAPI.comboboxPrefab));
    }

    public override void destroy()
    {
        base.destroy();
        combobox = null;
    }

    public override JSONObject generateSaveJson()
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

    public override void loadSettings()
    {
        JSONObject saved = parent.getSavedSettings(this);
        if (saved != null && !saved.IsNull)
        {
            if (saved.IsString)
            {
                resetContent();
                setValue(saved.str);
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
                    setValue(saved.GetField("value").str);
                else
                    setValue(defaultValue);
            }
        }
        else
        {
            resetContent();
            setValue(defaultValue);
        }
        index = values.IndexOf(value);
    }

    public void setContent(string[] items)
    {
        contentHasChanged = true;
        values = items;
        if (combobox != null)
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
        if (combobox != null)
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
        if (combobox != null)
        {
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (string item in values)
                options.Add(new Dropdown.OptionData(item));
            combobox.options = options;
        }
    }

    public override void resetValue()
    {
        resetContent();
        setValue(defaultValue);
    }
}

public class modSetting_slider : modSetting
{
    public Slider slider = null;
    public UISlider UIslider = null;
    public Text sliderText = null;
    public float defaultValue;
    public sliderType valueType;
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
    public modSetting_slider(JSONObject source, modSettingContainer parent) : base(source, parent)
    {
        if (source.HasField("default"))
            defaultValue = source.GetField("default").n;
        else
            defaultValue = 0;
        valueType = sliderType.number;
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
        if (access != menuType.world)
            loadSettings();
        else
            value = defaultValue;
    }

    override public void setGameObject(GameObject go)
    {
        base.setGameObject(go);
        slider = control.GetComponentInChildren<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        setValue(value);
        UIslider = control.GetComponentInChildren<UISlider>();
        UIslider.SliderEvent.RemoveAllListeners();
        slider.onValueChanged.RemoveAllListeners();
        sliderText = Traverse.Create(UIslider).Field("sliderTextComponent").GetValue<Text>();
        UIslider.name = "ESAPI_" + control.name + "_UISlider";
        slider.onValueChanged.AddListener(delegate { value = slider.value; });
    }

    public void setValue(float newValue)
    {
        if (slider == null)
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

    public enum sliderType
    {
        number,
        percent
    }
    public override void create()
    {
        setGameObject(Object.Instantiate(ExtraSettingsAPI.sliderPrefab));
    }
    public override void destroy()
    {
        base.destroy();
        slider = null;
    }
    public override void update()
    {
        if (!UIslider.gameObject.activeInHierarchy)
        {
            UIslider.enabled = false;
        }
        sliderText.text = (valueType == sliderType.percent) ? Math.Round(slider.value * 100, rounding) + "%" : Math.Round(slider.value, rounding).ToString();
    }

    public override JSONObject generateSaveJson()
    {
        return new JSONObject(value);
    }
    public override void loadSettings()
    {
        JSONObject saved = parent.getSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.n;
        else
            value = defaultValue;
    }

    public override void resetValue()
    {
        setValue(defaultValue);
    }
}

public class modSetting_keybind : modSetting
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
            if (keybind == null)
                return _v;
            return keybind.Keybind;
        }
        set
        {
            _v = value;
            if (keybind != null)
                keybind.Set(value);
        }
    }
    public modSetting_keybind(JSONObject source, modSettingContainer parent) : base(source, parent)
    {
        KeyCode newKey = KeyCode.None;
        if (source.HasField("mainDefault"))
            Enum.TryParse(source.GetField("mainDefault").str, true, out newKey);
        KeyCode newKey2 = KeyCode.None;
        if (source.HasField("altDefault"))
            Enum.TryParse(source.GetField("altDefault").str, true, out newKey2);
        defaultValue = new Keybind(parent.IDName + "." + name, newKey, newKey2);
        if (access != menuType.world)
            loadSettings();
        else
            value = new Keybind(defaultValue);
        addKeyBind();
    }

    public void setValue(KeyCode key, bool main = true)
    {
        if (main)
            value.MainKey = key;
        else
            value.AltKey = key;
        value = value;
    }

    public override void setGameObject(GameObject go)
    {
        base.setGameObject(go);
        keybind = control.GetComponent<KeybindInterface>();
        Traverse keyTrav = Traverse.Create(keybind);
        keyTrav.Field("idenifier").SetValue(defaultValue.Identifier);
        keyTrav.Field("mainKeyDefault").SetValue(defaultValue.MainKey);
        keyTrav.Field("altKeyDefault").SetValue(defaultValue.AltKey);
        KeyConnection main = keyTrav.Field("mainKey").GetValue<KeyConnection>();
        KeyConnection alt = keyTrav.Field("altKey").GetValue<KeyConnection>();
        main.button = control.transform.FindChildRecursively("MainKey").GetComponent<Button>();
        alt.button = control.transform.FindChildRecursively("AltKey").GetComponent<Button>();
        main.text = main.button.GetComponentInChildren<Text>();
        alt.text = alt.button.GetComponentInChildren<Text>();
        keybind.Initialize(ExtraSettingsAPI.keybindColors);
        value = _v;
    }

    public override void create()
    {
        setGameObject(Object.Instantiate(ExtraSettingsAPI.keybindPrefab));
    }

    public override void destroy()
    {
        removeKeyBind();
        base.destroy();
        keybind = null;
    }

    public override JSONObject generateSaveJson()
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

    public override void loadSettings()
    {
        JSONObject saved = parent.getSavedSettings(this);
        if (saved != null && !saved.IsNull)
        {
            KeyCode newKey = defaultValue.MainKey;
            if (saved.HasField("main"))
                newKey = (KeyCode)(int)saved.GetField("main").n;
            KeyCode newKey2 = defaultValue.AltKey;
            if (saved.HasField("alt"))
                newKey2 = (KeyCode)(int)saved.GetField("alt").n;
            value = new Keybind(defaultValue.Identifier, newKey, newKey2);
        }
        else
            value = new Keybind(defaultValue);
    }

    public override void resetValue()
    {
        setValue(defaultValue.MainKey);
        setValue(defaultValue.AltKey, false);
    }
}

public class modSetting_button : modSetting
{
    public Button button = null;
    public modSetting_button(JSONObject source, modSettingContainer parent) : base(source, parent)
    {
    }

    public override void setGameObject(GameObject go)
    {
        base.setGameObject(go);
        button = control.GetComponentInChildren<Button>();
        Vector2 sizeDif = new Vector2(text.preferredWidth + text.preferredHeight - (button.transform as RectTransform).offsetMax.x, 0);
        (button.transform as RectTransform).offsetMax += sizeDif;
        button.onClick.AddListener(delegate { ExtraSettingsAPI.mods[parent.parent].ButtonPress(this); });
    }

    public override void create()
    {
        setGameObject(Object.Instantiate(ExtraSettingsAPI.buttonPrefab));
    }

    public override void destroy()
    {
        base.destroy();
        button = null;
    }

    public override void setText(string newText)
    {
        base.setText(newText);
        if (button != null)
        {
            Vector2 sizeDif = new Vector2(text.preferredWidth + text.preferredHeight - (button.transform as RectTransform).offsetMax.x, 0);
            (button.transform as RectTransform).offsetMax += sizeDif;
        }

    }
}

public class modSetting_input : modSetting
{
    public InputField input = null;
    public string defaultValue;
    public string value;
    public int maxLength;
    public InputField.ContentType contentType;
    public modSetting_input(JSONObject source, modSettingContainer parent) : base(source, parent)
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
            Enum.TryParse(source.GetField("mode").str, out contentType);
        if (access != menuType.world)
            loadSettings();
        else
            value = defaultValue;
    }

    public override void setGameObject(GameObject go)
    {
        base.setGameObject(go);
        input = control.GetComponentInChildren<InputField>();
        if (maxLength > 0)
            input.onValueChanged.AddListener((t) => { if (t.Length > maxLength) input.text = value; else value = t; });
        else
            input.onValueChanged.AddListener((t) => { value = t; });
        input.contentType = contentType;
        setValue(value);
    }

    public void setValue(string newValue)
    {
        if (input == null)
            value = newValue;
        else
            input.text = newValue;
    }

    public override void create()
    {
        setGameObject(Object.Instantiate(ExtraSettingsAPI.inputPrefab));
    }

    public override void destroy()
    {
        base.destroy();
        input = null;
    }

    public override JSONObject generateSaveJson()
    {
        return JSONObject.StringObject(value);
    }
    public override void loadSettings()
    {
        JSONObject saved = parent.getSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.str;
        else
            value = defaultValue;
    }

    public override void resetValue()
    {
        setValue(defaultValue);
    }
}

public class modSetting_text : modSetting
{
    public modSetting_text(JSONObject source, modSettingContainer parent) : base(source, parent)
    {
    }

    public override void setGameObject(GameObject go)
    {
        base.setGameObject(go);
    }

    public override void create()
    {
        setGameObject(Object.Instantiate(ExtraSettingsAPI.textPrefab));
    }
}

public class modSetting_data : modSetting
{
    Dictionary<string, string> value;
    JSONObject defaultValue;
    public modSetting_data(JSONObject source, modSettingContainer parent) : base(source, parent)
    {
        if (source == null || source.IsNull || !source.HasField("default"))
            defaultValue = new JSONObject();
        else
            defaultValue = source.GetField("default").Copy();
        if (access != menuType.world)
            loadSettings();
        else
            resetValue();
    }

    public void setValue(string name, string newValue)
    {
        if (value.ContainsKey(name))
            value[name] = newValue;
        else
            value.Add(name, newValue);
        if (access != menuType.world)
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

    public override void loadSettings()
    {
        JSONObject saved = parent.getSavedSettings(this);
        if (saved != null && !saved.IsNull)
            value = saved.ToDictionary();
        else
            resetValue();
    }
    public override void destroy() { }
    public override JSONObject generateSaveJson()
    {
        return new JSONObject(value);
    }
    public override void resetValue()
    {
        value = defaultValue.ToDictionary();
    }
    public override void create() { }
    public override void setText(string newText) { }
}

public class modSettingContainer
{
    public string ModName { get; }
    public string IDName { get; }
    public Mod parent { get; }
    public JSONObject settingsJson;
    public List<modSetting> settings = new List<modSetting>();
    public GameObject title = null;
    public modSettingContainer(Mod mod, JSONObject settings)
    {
        parent = mod;
        ModName = parent.modlistEntry.jsonmodinfo.name;
        IDName = parent.GetType().Name;
        settingsJson = settings;
        if (!settingsJson.IsArray)
            throw new FormatException("Mod settings in " + ModName + " are not formatted correctly");
        foreach (JSONObject settingEntry in settingsJson.list)
            try
            {
                this.settings.Add(modSetting.createSetting(settingEntry, this));
            }
            catch (Exception err)
            {
                ExtraSettingsAPI.Log(err);
            }
    }

    public void create()
    {
        title = Object.Instantiate(ExtraSettingsAPI.titlePrefab);
        title.name = IDName + "Title";
        title.transform.SetParent(ExtraSettingsAPI.newOptCon.transform, false);
        Text text = title.GetComponentInChildren<Text>();
        text.text = "-------- " + ModName;
        text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
        foreach (modSetting setting in settings)
            setting.create();
        toggleSettings(RAPI.GetLocalPlayer() == null);
        title.GetComponentInChildren<Toggle>().onValueChanged.AddListener(x => {
            toggleSettings(RAPI.GetLocalPlayer() == null, x);
            ExtraSettingsAPI.updateAllSettingBacks();
        });
    }

    public void destroy()
    {
        if (title != null)
        {
            Object.Destroy(title);
            title = null;
        }
        foreach (modSetting setting in settings)
            setting.destroy();
    }

    public JSONObject getSavedSettings(modSetting setting)
    {
        JSONObject dataStore = (setting.access == modSetting.menuType.world) ? ExtraSettingsAPI.LocalConfig : ExtraSettingsAPI.Config;
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

    public JSONObject generateSaveJson(bool isLocal = false)
    {
        JSONObject store = new JSONObject();
        foreach (modSetting setting in settings)
        {
            if (setting.access == modSetting.menuType.world != isLocal)
                continue;
            JSONObject dat = setting.generateSaveJson();
            if (dat != null)
                store.AddField(setting.name, dat);
        }
        return store;
    }

    public void toggleSettings(bool isOnMainmenu) => toggleSettings(isOnMainmenu, title.GetComponentInChildren<Toggle>().isOn);

    public void toggleSettings(bool isOnMainmenu, bool on)
    {
        foreach (modSetting setting in settings)
            if (setting.control != null)
                setting.control.SetActive(on && (isOnMainmenu ? setting.access <= modSetting.menuType.mainmenu : (setting.access != modSetting.menuType.mainmenu)));
        ExtraSettingsAPI.updateAllSettingBacks();
    }

    public void loadLocal()
    {
        foreach (modSetting setting in settings)
            if (setting.access == modSetting.menuType.world)
                setting.loadSettings();
    }
}