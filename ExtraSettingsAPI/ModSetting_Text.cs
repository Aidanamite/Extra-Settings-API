using UnityEngine;
using Object = UnityEngine.Object;

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
