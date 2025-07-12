using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Seperator : ModSetting
    {
        public ModSetting_Seperator(JObject source, ModSettingContainer parent) : base(source, parent) { }

        public override void Create()
        {
            SetGameObject(Object.Instantiate(ExtraSettingsAPI.seperatorPrefab));
        }

        public override bool SetBackImage(bool state) => false;
    }
}
