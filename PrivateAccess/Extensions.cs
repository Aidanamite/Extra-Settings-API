using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace PrivateAccess
{
    public static class Extensions
    {
        public static RectTransform Panel(this Settings settings) => settings.panel;
        public static void Tab(this TabButton button, GameObject newValue) => button.tab = newValue;
        public static ColorBlock ColorBlock(this ControlsSettingsBox controls) => controls.colorblock;
        public static void SetDefault(this KeybindInterface inst,string identifier,KeyCode main, KeyCode alt)
        {
            inst.identifier = identifier;
            inst.mainKeyDefault = main;
            inst.altKeyDefault = alt;
        }
        public static Text TextComponent(this UISlider slider) => slider.sliderTextComponent;
        public static void Refresh(this KeybindInterface ui) => ui.Refresh();
    }
    public static class HiddenStatics
    {
        public static bool SettingsInitialized => Settings.initialized;
    }
}
