using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
