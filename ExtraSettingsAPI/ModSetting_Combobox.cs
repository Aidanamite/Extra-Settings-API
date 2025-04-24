using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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

    public override string CurrentValue() => value;
    public override bool TrySetValue(string str)
    {
        var ind = Array.IndexOf(values, str);
        if (ind == -1 && uint.TryParse(str, out var r) && r < values.Length)
            ind = (int)r;
        if (ind == -1)
            return false;
        SetValue(ind);
        return true;
    }
    public override string[] PossibleValues() => values;
}
