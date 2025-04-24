using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

[HarmonyPatch(typeof(Settings), "Open")]
static class Patch_SettingsOpen
{
    static void Postfix(Settings __instance)
    {
        ExtraSettingsAPI.insertNewSettingsMenu();
        foreach (EventCaller caller in ExtraSettingsAPI.mods.Values)
            caller.Call(EventCaller.EventTypes.Open);
        ExtraSettingsAPI.MaybeReselectModTab();
    }
}

[HarmonyPatch(typeof(Settings), "Close")]
static class Patch_SettingsClose
{
    static void Prefix(Settings __instance, ref bool __state) => __state = Traverse.Create(__instance).Field("optionsCanvas").GetValue<GameObject>().activeInHierarchy;
    static void Postfix(ref bool __state)
    {
        if (__state)
        {
            ExtraSettingsAPI.generateSaveJson();
            foreach (EventCaller caller in ExtraSettingsAPI.mods.Values)
                caller.Call(EventCaller.EventTypes.Close);
            if (!ExtraSettingsAPI.needsToCreateSettings)
                ExtraSettingsAPI.removeNewSettingsMenu();
        }
    }
}

[HarmonyPatch(typeof(UISlider), "Update")]
static class Patch_SliderUpdate
{
    static bool Prefix(UISlider __instance)
    {
        if (__instance.name.StartsWith("ESAPI_"))
            foreach (ModSettingContainer container in ExtraSettingsAPI.modSettings.Values)
                foreach (var setting in container.allSettings)
                    if (setting is ModSetting_Slider s && s.UIslider == __instance)
                    {
                        setting.Update();
                        return false;
                    }
        return true;
    }
}

[HarmonyPatch(typeof(MyInput), "IdentifierToKeybind")]
static class Patch_KeybindsReset
{
    static bool Prefix(ref string identifier, ref Keybind __result)
    {
        if (ModSetting_Keybind.MyKeys != null && ModSetting_Keybind.MyKeys.Count > 0 && ModSetting_Keybind.MyKeys.ContainsKey(identifier))
        {
            __result = ModSetting_Keybind.MyKeys[identifier];
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(KeybindInterface))]
static class Patch_EnterExitKeybind
{
    public static (KeybindInterface, bool) lastEntered;
    [HarmonyPatch("PointerEnter")]
    [HarmonyPrefix]
    static void Enter(KeybindInterface __instance, KeyConnection key, KeyConnection ___mainKey)
    {
        lastEntered = (__instance, key == ___mainKey);
    }
    [HarmonyPatch("PointerExit")]
    [HarmonyPrefix]
    static void Exit()
    {
        lastEntered = default;
    }
}

[HarmonyPatch(typeof(SaveAndLoad), "Save")]
static class Patch_SaveGame
{
    static void Postfix(string filename)
    {
        string[] path = filename.Split(new char[] { '\\', '/' });
        filename = "";
        for (int i = 0; i < path.Length - 1; i++)
            filename += path[i] + "\\";
        if (filename.EndsWith(SaveAndLoad.latestStringNameEnding + "\\"))
            ExtraSettingsAPI.generateSaveJson(filename + ExtraSettingsAPI.modInfo.name + ".json");
    }
}

[HarmonyPatch(typeof(LoadGameBox), "Button_LoadGame")]
static class Patch_LoadGame
{
    static void Postfix() => ExtraSettingsAPI.loadLocal(true);
}

[HarmonyPatch(typeof(NewGameBox), "Button_CreateNewGame")]
static class Patch_NewGame
{
    static void Postfix() => ExtraSettingsAPI.loadLocal(false);
}

[HarmonyPatch(typeof(LoadSceneManager), "LoadScene")]
static class Patch_UnloadWorld
{
    static void Postfix(ref string sceneName)
    {
        if (sceneName == Raft_Network.MenuSceneName)
            ExtraSettingsAPI.LocalConfig = null;
    }
}

static class Patch_ReplaceAPICalls
{
    public static HashSet<MethodInfo> methodsToLookFor;
    public static IEnumerable<MethodBase> TargetMethods(Assembly assembly)
    {
        var l = new List<MethodBase>();
        foreach (var t in assembly.GetTypes())
            foreach (var m in t.GetMethods(~BindingFlags.Default))
                try
                {
                    foreach (var i in PatchProcessor.GetCurrentInstructions(m, out var iL))
                        if (i.opcode == OpCodes.Call && i.operand is MethodInfo method && methodsToLookFor.Contains(method))
                        {
                            l.Add(m);
                            break;
                        }
                }
                catch { }
        return l;
    }
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        foreach (var i in code)
            if (i.opcode == OpCodes.Call && i.operand is MethodInfo method && methodsToLookFor.Contains(method) && !method.IsStatic)
                i.opcode = OpCodes.Callvirt;
        return code;
    }
}
