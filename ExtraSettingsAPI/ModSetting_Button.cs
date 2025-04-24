using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
