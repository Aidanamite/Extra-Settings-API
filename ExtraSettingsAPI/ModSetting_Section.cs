using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ModSetting_Section : ModSetting
{
    public bool open = false;
    public ModSetting_Section(JSONObject source, ModSettingContainer parent) : base(source, parent)
    {
    }

    public override void SetGameObject(GameObject go)
    {
        base.SetGameObject(go);
        control.GetComponentInChildren<Toggle>(true).isOn = open;
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
