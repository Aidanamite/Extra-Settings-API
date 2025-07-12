using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_MultiButton : ModSetting
    {
        public List<Button> buttons = new List<Button>();
        public string[] names;
        public string[] defaultNames;
        public Transform container;
        public ModSetting_MultiButton(JObject source, ModSettingContainer parent) : base(source, parent)
        {
            if (source.TryGetValue<JArray>("buttons", out var buttonsField))
                defaultNames = buttonsField.Select(x => (x as JValue)?.Value?.EnsureType<string>()).ToArray();
            else
                defaultNames = new string[0];
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
            buttons.Clear();
        }

        public override void ResetValue(bool local)
        {
            SetValue(defaultNames);
        }

        public override void SetText(string newText) { }

        public void SetValue(string[] newText)
        {
            names = newText ?? new string[0];
            if (container)
            {
                var c = Math.Max(buttons.Count, names.Length);
                for (int i = 0; i < buttons.Count || i < names.Length; i++)
                    if (i >= names.Length)
                    {
                        Object.Destroy(buttons[i]);
                        buttons.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        var button = i < buttons.Count ? buttons[i] : null;
                        if (!button)
                        {
                            buttons.Insert(i, button = Object.Instantiate(ExtraSettingsAPI.multibuttonChildPrefab, container));
                            var j = i;
                            button.onClick.AddListener(() => ExtraSettingsAPI.mods[parent.parent].ButtonPress(this, j));
                        }
                        var t = button.GetComponentInChildren<Text>(true);
                        t.text = names[i];
                        var r = button.transform as RectTransform;
                        r.sizeDelta += new Vector2(t.preferredWidth - t.preferredHeight + r.rect.height - r.rect.width, 0);
                        foreach (var hover in button.GetComponentsInChildren<SettingHoverDetector>(true))
                            hover.owner = this;
                    }
            }
        }
        public string[] GetValue()
        {
            if (names != null)
                return names;
            return null;
        }
    }
}