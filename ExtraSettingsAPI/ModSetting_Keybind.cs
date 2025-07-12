using HarmonyLib;
using Newtonsoft.Json.Linq;
using PrivateAccess;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static _ExtraSettingsAPI.ModSetting;
using Object = UnityEngine.Object;

namespace _ExtraSettingsAPI
{
    public class ModSetting_Keybind : ModSetting
    {
        public KeybindInterface keybind = null;
        public Keybind instance;
        Value<KeyCode> main;
        Value<KeyCode> alt;
        bool addedKey = false;
        static public Dictionary<string, Keybind> MyKeys = new Dictionary<string, Keybind>();
        public ModSetting_Keybind(JObject source, ModSettingContainer parent) : base(source, parent)
        {
            KeyCode mainDefault = KeyCode.None;
            if (source.TryGetValue<JValue>("mainDefault", out var mainField, JTokenType.String))
                Enum.TryParse(mainField.Value.EnsureType<string>(), true, out mainDefault);
            KeyCode altDefault = KeyCode.None;
            if (source.TryGetValue<JValue>("altDefault", out var altField, JTokenType.String))
                Enum.TryParse(altField.Value.EnsureType<string>(), true, out altDefault);
            main = NewValue(mainDefault);
            alt = NewValue(altDefault);
            instance = new Keybind(parent.IDName + "." + name, main.current, alt.current);
            TrySetupMember(source);
            LoadSettings();
            addKeyBind();
        }

        public void SetValue(KeyCode key, bool isMain, bool local)
        {
            if (isMain)
            {
                main[local] = key;
                instance.MainKey = main.current;
            }
            else
            {
                alt[local] = key;
                instance.AltKey = alt.current;
            }
            if (keybind)
                keybind.Refresh();
        }

        public override bool OnUpdateLate()
        {
            bool changed = false;
            if (instance.MainKey != main.current)
            {
                changed = true;
                main.current = instance.MainKey;
            }
            if (instance.MainKey != main.current)
            {
                changed = true;
                main.current = instance.MainKey;
            }
            return base.OnUpdateLate() || changed;
        }

        public override void SetGameObject(GameObject go)
        {
            base.SetGameObject(go);
            keybind = control.GetComponent<KeybindInterface>();
            keybind.SetDefault(instance.Identifier, main.Default, alt.Default);

            //KeyConnection main = keyTrav.Field("mainKey").GetValue<KeyConnection>();
            //KeyConnection alt = keyTrav.Field("altKey").GetValue<KeyConnection>();
            //main.button = control.transform.FindChildRecursively("MainKey").GetComponent<Button>();
            //alt.button = control.transform.FindChildRecursively("AltKey").GetComponent<Button>();
            //main.text = main.button.GetComponentInChildren<Text>(true);
            //alt.text = alt.button.GetComponentInChildren<Text>(true);
            keybind.Initialize(ExtraSettingsAPI.keybindColors);
            keybind.Set(instance);
        }

        public override void Create()
        {
            SetGameObject(Object.Instantiate(ExtraSettingsAPI.keybindPrefab));
        }

        public override void Destroy()
        {
            removeKeyBind();
            base.Destroy();
            keybind = null;
        }

        protected override bool ShouldTryGenerateSave(bool local) => main.ShouldSave(local) || alt.ShouldSave(local);
        public override JToken GenerateSaveJson(bool local)
        {
            var store = new JObject();
            store["main"] = (int)main[local];
            store["alt"] = (int)alt[local];
            return store;
        }

        public void addKeyBind()
        {
            addedKey = MyKeys.TryAdd(instance.Identifier, instance);
            SetMemberValue(instance.Identifier);
        }

        public void removeKeyBind()
        {
            if (addedKey)
                MyKeys.Remove(instance.Identifier);
            SetMemberValue(null);
        }

        protected override bool IsMemberTypeValid(Type type) => type == typeof(string);

        public override string GetTooltip() => noSave ? base.GetTooltip() : JoinParagraphs(base.GetTooltip(), $"Default Value: {main.Default.ToString().CamelToWords()}, {alt.Default.ToString().CamelToWords()}{(ExtraSettingsAPI.IsInWorld && save.IsSplit() ? $"\nGlobal Value: {main[false].ToString().CamelToWords()}, {alt[false].ToString().CamelToWords()}" : "")}");

        public override void LoadSettings(bool local)
        {
            JToken saved = parent.GetSavedSettings(this, local);
            if (saved is JObject obj)
            {
                if (obj.TryGetValue<JValue>("main", out var mainField))
                {
                    if (mainField.Type == JTokenType.String && Enum.TryParse(mainField.Value.EnsureType<string>(), true, out KeyCode v))
                        main[local] = v;
                    else if (mainField.Type == JTokenType.Integer && mainField.Value.TryConvert(out v))
                        main[local] = v;
                    else
                        main.Reset(local);
                }
                else
                    main.Reset(local);
                if (obj.TryGetValue<JValue>("alt", out var altField))
                {
                    if (altField.Type == JTokenType.String && Enum.TryParse(altField.Value.EnsureType<string>(), true, out KeyCode v))
                        alt[local] = v;
                    else if (altField.Type == JTokenType.Integer && altField.Value.TryConvert(out v))
                        alt[local] = v;
                    else
                        alt.Reset(local);
                }
                else
                    alt.Reset(local);
            }
            else
            {
                main.Reset(local);
                alt.Reset(local);
            }

            instance.MainKey = main.current;
            instance.AltKey = alt.current;
            if (keybind)
                keybind.Refresh();
        }

        public override void ResetValue(bool local)
        {
            main.Reset(local);
            alt.Reset(local);
            instance.MainKey = main.current;
            instance.AltKey = alt.current;
            if (keybind)
                keybind.Refresh();
        }

        public override void OnExitWorld()
        {
            if (!save.IsSplit())
                return;
            instance.MainKey = main.current;
            instance.AltKey = alt.current;
            if (keybind)
                keybind.Refresh();
        }
    }
}