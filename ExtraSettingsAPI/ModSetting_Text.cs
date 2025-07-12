using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Text : ModSetting
    {
        public ModSetting_Text(JObject source, ModSettingContainer parent) : base(source, parent) { }

        public override void Create()
        {
            SetGameObject(Object.Instantiate(ExtraSettingsAPI.textPrefab));
        }
    }
}