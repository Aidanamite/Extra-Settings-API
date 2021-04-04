using UnityEngine;

public class SettingsTemplate : Mod
{
    string myNewKey;
    public void Start()
    {
        Debug.Log("Settings Template mod has been loaded!");
    }

    public void Update()
    {
        if (ExtraSettingsAPI_Loaded && MyInput.GetButton(myNewKey))
            Debug.Log("Key is pressed");
    }

    public void OnModUnload()
    {
        Debug.Log("Settings Template mod has been unloaded!");
    }

    public void ExtraSettingsAPI_Load() // Occurs when the API loads the mod's settings
    {
        myNewKey = ExtraSettingsAPI_GetKeybindName("keybindDisplay");
    }

    public void ExtraSettingsAPI_Unload() // Occurs when the API unloads
    {
        
    }

    public void ExtraSettingsAPI_SettingsOpen() // Occurs when user opens the settings menu
    {

    }

    public void ExtraSettingsAPI_SettingsClose() // Occurs when user closes the settings menu
    {
        
    }

    public void ExtraSettingsAPI_SettingsCreate() // Occurs when API creates the controls for the settings of this mod. This event still occurs but is now obsolete, use the SettingsOpen event instead
    {

    }

    public void ExtraSettingsAPI_ButtonPress(string name) // Occurs when a settings button is clicked. "name" is set the the button's name
    {
        if (name == "Button Display")
            Debug.Log("I've been clicked");
    }


    // ------------------------ DO NOT CHANGE unless you know what you are doing ------------------------------------------------

    // These variables are automatically assigned and changed. You MUST have these to use any of the methods
    static HarmonyLib.Traverse ExtraSettingsAPI_Traverse;
    static bool ExtraSettingsAPI_Loaded = false; // Are this mod's settings are currently loaded?


    /*
     =================================================
     | Use the following methods to get/set the values of
     | the settings. These functions will throw Exceptions
     | if either the specified name is wrong or the
     | setting does not exist. These functions are setup
     | to only allow you to get the settings that belong
     | to your mod. This means you don't need to worry
     | about mod conflicts in the settings.
     =================================================
    */

    // Use to get the selected index from a Combobox type setting
    public int ExtraSettingsAPI_GetComboboxSelectedIndex(string SettingName) 
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getComboboxSelectedIndex", new object[] { this, SettingName }).GetValue<int>();
        return -1;
    }

    // Use to get the selected item name from a Combobox type setting
    public string ExtraSettingsAPI_GetComboboxSelectedItem(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getComboboxSelectedItem", new object[] { this, SettingName }).GetValue<string>();
        return "";
    }

    // Use to get the list of items in a Combobox type setting
    public string[] ExtraSettingsAPI_GetComboboxContent(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getComboboxContent", new object[] { this, SettingName }).GetValue<string[]>();
        return new string[0];
    }

    // Use to get the current state of a Checkbox type setting
    public bool ExtraSettingsAPI_GetCheckboxState(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getCheckboxState", new object[] { this, SettingName }).GetValue<bool>();
        return false;
    }

    // Use to get the current value from a Slider type setting
    // This method returns the value of the slider rounded according to the mod's setting configuration
    public float ExtraSettingsAPI_GetSliderValue(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getSliderValue", new object[] { this, SettingName }).GetValue<float>();
        return 0;
    }

    // Use to get the current value from a Slider type setting
    // This method returns the non-rounded value of the slider
    public float ExtraSettingsAPI_GetSliderRealValue(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getSliderRealValue", new object[] { this, SettingName }).GetValue<float>();
        return 0;
    }

    // Use to get the keybind name for a Keybind type setting
    // The returned name can be used with the MyInput functions to detect keypresses
    public string ExtraSettingsAPI_GetKeybindName(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getKeybindName", new object[] { this, SettingName }).GetValue<string>();
        return "";
    }

    // Use to get the raw keybind for a Keybind type setting
    public Keybind ExtraSettingsAPI_GetKeybind(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getKeybind", new object[] { this, SettingName }).GetValue<Keybind>();
        return null;
    }

    // Use to get the main key for a Keybind type setting
    public KeyCode ExtraSettingsAPI_GetKeybindMain(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getKeybind_main", new object[] { this, SettingName }).GetValue<KeyCode>();
        return KeyCode.None;
    }

    // Use to get the alternate key for a Keybind type setting
    public KeyCode ExtraSettingsAPI_GetKeybindAlt(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getKeybind_alt", new object[] { this, SettingName }).GetValue<KeyCode>();
        return KeyCode.None;
    }

    // Use to get the text label of a setting
    public string ExtraSettingsAPI_GetSettingText(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getSettingText", new object[] { this, SettingName }).GetValue<string>();
        return "";
    }

    // Use to set the selected index in a Combobox type setting
    public void ExtraSettingsAPI_SetComboboxSelectedIndex(string SettingName, int value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setComboboxSelectedIndex", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to set the selected item in a Combobox type setting
    public void ExtraSettingsAPI_SetComboboxSelectedItem(string SettingName, string value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setComboboxSelectedItem", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to set the items listed in a Combobox type setting
    public void ExtraSettingsAPI_SetComboboxContent(string SettingName, string[] value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setComboboxContent", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to add an item to the items listed in a Combobox type setting
    public void ExtraSettingsAPI_AddComboboxContent(string SettingName, string value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("addComboboxContent", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to reset items listed in a Combobox type setting to the list set in the mod json
    public void ExtraSettingsAPI_ResetComboboxContent(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("resetComboboxContent", new object[] { this, SettingName }).GetValue();
    }

    // Use to set the current state of a Checkbox type setting
    public void ExtraSettingsAPI_SetCheckboxState(string SettingName, bool value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setCheckboxState", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to set the value of a Slider type setting
    public void ExtraSettingsAPI_SetSliderValue(string SettingName, float value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setSliderValue", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to set the current main keybinding for a Keybind type setting
    public void ExtraSettingsAPI_SetKeybindMain(string SettingName, KeyCode value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setKeybind_main", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to set the current alternative keybinding for a Keybind type setting
    public void ExtraSettingsAPI_SetKeybindAlt(string SettingName, KeyCode value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setKeybind_alt", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to set the text label of a setting
    // This change is not stored in the save data
    public void ExtraSettingsAPI_SetText(string SettingName, string value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setSettingsText", new object[] { this, SettingName, value }).GetValue();
    }

    // Use to reset a setting to its default value
    public void ExtraSettingsAPI_ResetSetting(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("resetSetting", new object[] { this, SettingName }).GetValue();
    }

    // Use to reset all settings to their default values
    public void ExtraSettingsAPI_ResetAllSettings()
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("resetSettings", new object[] { this }).GetValue();
    }
}