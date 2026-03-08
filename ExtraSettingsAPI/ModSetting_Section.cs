using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Section : ModSetting
    {
        public bool open = false;
        Toggle button;
        public ModSetting_Section(JObject source, ModSettingContainer parent) : base(source, parent)
        {
        }

        public override void SetGameObject(GameObject go)
        {
            base.SetGameObject(go);
            button = control.GetComponentInChildren<Toggle>(true);
            button.isOn = open;
            button.onValueChanged.AddListener(x =>
            {
                open = x;
                parent.ToggleSettings();
                ExtraSettingsAPI.UpdateAllSettingBacks();
            });
        }

        protected override void SetInteractable(bool state)
        {
            base.SetInteractable(state);
            if (!state && button.isOn)
                button.isOn = false;
            SimpleSetInteractable(button, state);
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
}