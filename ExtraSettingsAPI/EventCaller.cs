using HarmonyLib;
using HMLLibrary;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Debug = UnityEngine.Debug;
using System.Runtime.CompilerServices;

namespace _ExtraSettingsAPI
{
    public class EventCaller
    {
        public Harmony harmony;
        public Mod parent { get; }
        public Traverse modTraverse;
        IEvents settingsEvents;
        public Traverse<bool> APIBool;
        public EventCaller(Mod mod)
        {
            parent = mod;
            modTraverse = Traverse.Create(parent);
            var settingsField = modTraverse.Field("ExtraSettingsAPI_Settings");
            if (settingsField.FieldExists())
            {
                if (settingsField.GetValue() == null)
                    try
                    {
                        var fType = settingsField.GetValueType();
                        if (fType == typeof(Type))
                            throw new InvalidOperationException("Cannot create instance of class " + fType.FullName);
                        if (fType.IsAbstract)
                            throw new InvalidOperationException("Cannot create instance of abstract class " + fType.FullName);
                        if (fType.IsInterface)
                            throw new InvalidOperationException("Cannot create instance of interface class " + fType.FullName);
                        var c = fType.GetConstructors((BindingFlags)(-1)).FirstOrDefault(x => x.GetParameters().Length == 0);
                        if (c == null)
                            throw new MissingMethodException("No parameterless constructor found for class " + fType.FullName);
                        else
                            settingsField.SetValue(c.Invoke(new object[0]));
                    }
                    catch (Exception e)
                    {
                        ExtraSettingsAPI.Log($"Found settings field of mod {parent.modlistEntry.jsonmodinfo.name}'s main class but failed to create an instance for it. You may need to create the class instance yourself.\n{e}");
                    }
                if (settingsField.GetValue() != null)
                {
                    if (settingsField.GetValue() is Type t)
                        modTraverse = Traverse.Create(t);
                    else
                        modTraverse = Traverse.Create(settingsField.GetValue());
                }
            }
            settingsEvents = CreateIEvents(modTraverse.GetValue());
            APIBool = modTraverse.Field<bool>("ExtraSettingsAPI_Loaded");
            modTraverse.Field<Traverse>("ExtraSettingsAPI_Traverse").Value = ExtraSettingsAPI.self;
            var patchedMethods = new HashSet<MethodInfo>();
            var loadModInstructions = (typeof(Mod), new[]
            {
                new CodeInstruction(OpCodes.Ldtoken, parent.GetType()),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Type),"GetTypeFromHandle")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtraSettingsAPI),nameof(ExtraSettingsAPI.GetMod)))
            });
            var events = typeof(IEvents).GetMethods().Select(x => "ExtraSettingsAPI_" + x.Name).ToHashSet();
            foreach (var modMethod in (modTraverse.GetValue() as Type ?? modTraverse.GetValue().GetType()).GetMethods(~BindingFlags.Default))
                if (modMethod.Name.StartsWith("ExtraSettingsAPI_") && !events.Contains(modMethod.Name))
                {
                    var matches = new List<MethodInfo>();
                    MethodInfo m1 = null;
                    var s = -1;
                    List<CodeInstruction> pars = null;

                    foreach (var m in typeof(ExtraSettingsAPI).GetMethods(~BindingFlags.Default))
                        if (m.Name.Equals(modMethod.Name.Remove(0, "ExtraSettingsAPI_".Length), StringComparison.InvariantCultureIgnoreCase))
                        {
                            matches.Add(m);
                            if (Transpiler.TryGenerateInstructions(modMethod, m, Transpiler.ReturnHandling.FlexibleType, out var l, out var skip, loadModInstructions) && (s == -1 || skip < s))
                            {
                                s = skip;
                                m1 = m;
                                pars = l;
                            }
                        }
                    if (matches.Count == 0)
                        ExtraSettingsAPI.LogWarning($"{parent.modlistEntry.jsonmodinfo.name} >> Could not find any methods matching the name of method {modMethod.DeclaringType.FullName}::{modMethod}. You may have misspelled the method name or not meant to implement the ExtraSettingsAPI here");
                    else if (m1 == null)
                        ExtraSettingsAPI.LogWarning($"{parent.modlistEntry.jsonmodinfo.name} >> Could not find suitable implementation for method {modMethod.DeclaringType.FullName}::{modMethod}. You may have misspelled the method name, not meant to implement the ExtraSettingsAPI here or used the wrong parameters. The following methods were found with the same name:" + matches.Join(y => "\n" + y.ReturnType?.FullName + " " + modMethod.Name + "(" + y.GetParameters().Skip(1).Join(x => x.ParameterType.FullName) + ")", ""));
                    else
                    {
                        try
                        {
                            Transpiler.newMethod = m1;
                            Transpiler.argumentPairs = pars;
                            GetHarmony().Patch(modMethod, transpiler: new HarmonyMethod(typeof(Transpiler), nameof(Transpiler.Transpile)));
                            patchedMethods.Add(modMethod);
                        }
                        catch (Exception e)
                        {
                            ExtraSettingsAPI.LogError($"An error occured while trying to implement the {modMethod.Name} method for the {parent.modlistEntry.jsonmodinfo.name} mod\n{e}");
                        }
                    }
                }
            Patch_ReplaceAPICalls.methodsToLookFor = patchedMethods; // Used to avoid api call issues caused by inlining. This is a workaround for mods that don't use the NoInlining method implementation
            foreach (var m in Patch_ReplaceAPICalls.TargetMethods(parent.GetType().Assembly))
                GetHarmony().Patch(m, transpiler: new HarmonyMethod(typeof(Patch_ReplaceAPICalls), nameof(Patch_ReplaceAPICalls.Transpiler)));


        }

        Harmony GetHarmony() => harmony == null ? harmony = new Harmony("com.aidanamite.ExtraSettingsAPI-" + parent.GetType().FullName + "-" + DateTime.UtcNow.Ticks) : harmony;

        static class Transpiler
        {
            public static Dictionary<Type,CodeInstruction[]> specialArgs = new Dictionary<Type, CodeInstruction[]>();
            public static MethodInfo newMethod;
            public static List<CodeInstruction> argumentPairs;
            public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator iL)
            {
                foreach (var i in argumentPairs)
                    iL.ParseDeclares(i);
                return argumentPairs;
            }

            public static IEnumerable<CodeInstruction> HandleReturnTypes(Type targetRet, Type callerRet, ILGenerator iL)
            {
                if (CanCastTo(targetRet, callerRet, out var cast, true))
                {
                    foreach (var i in cast)
                        yield return i;
                }
                else
                {
                    if (targetRet != typeof(void))
                        yield return new CodeInstruction(OpCodes.Pop);
                    if (callerRet != typeof(void))
                    {
                        if (callerRet.IsPrimitive)
                            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        else if (!callerRet.IsValueType)
                            yield return new CodeInstruction(OpCodes.Ldnull);
                        else
                        {
                            var loc = iL.DeclareLocal(callerRet);
                            yield return new CodeInstruction(OpCodes.Ldloca_S, loc);
                            yield return new CodeInstruction(OpCodes.Initobj, callerRet);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, loc);
                        }
                    }
                }
                yield break;
            }

            [Flags]
            public enum ReturnHandling
            {
                Strict = 0,
                FlexibleType = 1,
                AllowIgnoreReturn = 2,
                AllowMissingReturn = 4
            }
            public static bool TryGenerateInstructions(MethodInfo caller, MethodInfo target, ReturnHandling returnHandling, out List<CodeInstruction> instructions, out int skipped, params (Type, CodeInstruction[])[] specialParams)
            {
                var callerParams = GetArguments(caller, out _);
                var targetParams = GetArguments(target, out var optional);
                instructions = null;
                skipped = 0;
                CodeInstruction[] returnCast = null;
                if (!CanSendAs(target.ReturnType, caller.ReturnType)) 
                {
                    if (returnHandling.HasFlag(ReturnHandling.FlexibleType) && CanCastTo(target.ReturnType, caller.ReturnType, out var cast, true))
                        returnCast = cast;
                    else if (returnHandling.HasFlag(ReturnHandling.AllowIgnoreReturn) && caller.ReturnType == typeof(void))
                        returnCast = new CodeInstruction[] { new CodeInstruction(OpCodes.Pop) };
                    else if (returnHandling.HasFlag(ReturnHandling.AllowMissingReturn) && (target.ReturnType == typeof(void) || returnHandling.HasFlag(ReturnHandling.AllowIgnoreReturn)))
                    {
                        var rl = new List<CodeInstruction>();
                        if (target.ReturnType != typeof(void))
                            rl.Add(new CodeInstruction(OpCodes.Pop));
                        if (caller.ReturnType.IsPrimitive || caller.ReturnType.IsEnum)
                            rl.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                        else if (!caller.ReturnType.IsValueType)
                            rl.Add(new CodeInstruction(OpCodes.Ldnull));
                        else
                        {
                            var loc = new DeclareLocal(caller.ReturnType);
                            rl.Add(new CodeInstruction(OpCodes.Ldloca_S, loc));
                            rl.Add(new CodeInstruction(OpCodes.Initobj, caller.ReturnType));
                            rl.Add(new CodeInstruction(OpCodes.Ldloc_S, loc));
                        }
                        returnCast = rl.ToArray();
                    }
                    else
                        //Debug.LogWarning($"Return type fail");
                        return false;
                }
                var l = new List<CodeInstruction>();
                int callerArg = 0;
                for (int targetArg = 0; targetArg < targetParams.Count; targetArg++)
                {
                    var targetParam = targetParams[targetArg];
                    CodeInstruction[] cast = null;
                    if (callerArg < callerParams.Count && CanCastTo(callerParams[callerArg], targetParam, out cast, true))
                    {
                        l.Add(GetArg(callerArg));
                        if (cast != null)
                            l.AddRange(cast);
                        callerArg++;
                    }
                    else if (specialParams.Contains(x => CanSendAs(x.Item1, targetParam), out var special))
                    {
                        l.AddRange(special.Item2);
                    }
                    else if (specialParams.Contains(x => CanCastTo(x.Item1, targetParam, out cast, true), out special))
                    {
                        if (cast != null)
                            l.AddRange(cast);
                        l.AddRange(special.Item2);
                    }
                    else if (optional[targetArg] != null)
                    {
                        var valueType = optional[targetArg].ParameterType;
                        if (valueType.IsValueType && !valueType.IsPrimitive)
                        {
                            var loc = new DeclareLocal(valueType);
                            l.Add(new CodeInstruction(OpCodes.Ldloca_S, loc));
                            l.Add(new CodeInstruction(OpCodes.Initobj, valueType));
                            l.Add(new CodeInstruction(OpCodes.Ldloc_S, loc));
                        }
                        else
                        {
                            var value = optional[targetArg].DefaultValue;
                            if (value == null)
                                l.Add(new CodeInstruction(OpCodes.Ldnull));
                            else if (valueType.IsPrimitive || valueType.IsEnum)
                            {
                                if (value is Enum e)
                                    e.TryConvert(valueType.GetElementType(), out value);
                                if (value is float)
                                    l.Add(new CodeInstruction(OpCodes.Ldc_R4, value));
                                else if (value is double)
                                    l.Add(new CodeInstruction(OpCodes.Ldc_R8, value));
                                else if ((value is long _long && (_long < -1 || _long > int.MaxValue)) || (value is ulong _ulong && _ulong > int.MaxValue))
                                    l.Add(new CodeInstruction(OpCodes.Ldc_I8, value));
                                else
                                {
                                    value.TryConvert<long>(out var lvalue); // This should never fail
                                    if (lvalue >= -1 && lvalue <= 8)
                                        l.Add(new CodeInstruction(
                                            lvalue == 0 ? OpCodes.Ldc_I4_0
                                            : lvalue == 1 ? OpCodes.Ldc_I4_1
                                            : lvalue == 2 ? OpCodes.Ldc_I4_2
                                            : lvalue == 3 ? OpCodes.Ldc_I4_3
                                            : lvalue == 4 ? OpCodes.Ldc_I4_4
                                            : lvalue == 5 ? OpCodes.Ldc_I4_5
                                            : lvalue == 6 ? OpCodes.Ldc_I4_6
                                            : lvalue == 7 ? OpCodes.Ldc_I4_7
                                            : lvalue == 8 ? OpCodes.Ldc_I4_8
                                            : OpCodes.Ldc_I4_M1));
                                    if (value is sbyte || (lvalue > 0 && lvalue <= 255))
                                        l.Add(new CodeInstruction(OpCodes.Ldc_I4_S, value));
                                    else
                                        l.Add(new CodeInstruction(OpCodes.Ldc_I4, value));
                                }
                            }
                            else
                                throw new NotSupportedException("I DON'T KNOW OF ANY CASE WHERE THIS WILL BE HIT");
                        }
                    }
                    else if (callerArg + 1 < callerParams.Count)
                    {
                        callerArg++;
                        skipped++;
                        while (true)
                        {
                            if (CanCastTo(callerParams[callerArg], targetParam, out cast, true))
                            {
                                l.Add(GetArg(callerArg));
                                if (cast != null)
                                    l.AddRange(cast);
                                callerArg++;
                                break;
                            }
                            callerArg++;
                            skipped++;
                            if (callerArg >= callerParams.Count)
                                return false;
                        }
                    }
                    else
                        return false;
                }
                skipped += callerParams.Count - callerArg - 1;
                l.Add(new CodeInstruction(target.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, target));
                if (returnCast != null)
                    l.AddRange(returnCast);
                l.Add(new CodeInstruction(OpCodes.Ret));
                instructions = l;
                return true;
                //Debug.LogWarning($"Argument mismatch fail\nCaller arguments: {callerParams.Join(x => x.FullName)}\nTarget arguments: {targetParams.Join(x => x.FullName)}\nSkipped: {skipped}\nArgument connections: {l.Join()}");

            }
            public static CodeInstruction GetArg(int index) {
                if (index <= 3)
                    return new CodeInstruction(
                        index == 0
                        ? OpCodes.Ldarg_0
                        : index == 1
                        ? OpCodes.Ldarg_1
                        : index == 2
                        ? OpCodes.Ldarg_2
                        : OpCodes.Ldarg_3);
                if (index <= 255)
                    return new CodeInstruction(OpCodes.Ldarg_S, (byte)index);
                return new CodeInstruction(OpCodes.Ldarg, index);
            }
            static bool CanSendAs(Type objType, Type targetType)
            {
                return (objType.IsValueType == targetType.IsValueType) && targetType.IsAssignableFrom(objType);
            }
            static bool CanCastTo(Type objType, Type targetType, out CodeInstruction[] cast, bool includeCustomCasts = false)
            {
                cast = Array.Empty<CodeInstruction>();
                if (CanSendAs(targetType, objType))
                    return true;
                if (includeCustomCasts)
                {
                    if (CanSendAs(objType, typeof(EventCaller)) && targetType == typeof(Mod))
                    {
                        cast = new[] {
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(EventCaller),nameof(parent)))
                        };
                        return true;
                    }
                    if (CanSendAs(objType, typeof(Mod)) && targetType == typeof(EventCaller))
                    {
                        cast = new[] {
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtraSettingsAPI), nameof(ExtraSettingsAPI.GetCallerFromMod)))
                        };
                        return true;
                    }
                }
                if (objType.IsValueType && objType != typeof(void) && targetType == typeof(object))
                {
                    cast = new[] {
                            new CodeInstruction(OpCodes.Box, objType)
                        };
                    return true;
                }
                if (objType.IsByRef && CanCastTo(objType.GetElementType(), targetType, out var innerCast))
                {
                    var elem = objType.GetElementType();
                    cast = new[] {
                            elem.IsPrimitive
                            ? new CodeInstruction (
                                elem == typeof(byte) || elem == typeof(bool)
                                ? OpCodes.Ldind_U1
                                : elem == typeof(sbyte)
                                ? OpCodes.Ldind_I1
                                : elem == typeof(short)
                                ? OpCodes.Ldind_I2
                                : elem == typeof(ushort) || elem == typeof(char)
                                ? OpCodes.Ldind_U2
                                : elem == typeof(int)
                                ? OpCodes.Ldind_I4
                                : elem == typeof(uint)
                                ? OpCodes.Ldind_U4
                                : elem == typeof(long) || elem == typeof(ulong)
                                ? OpCodes.Ldind_I8
                                : elem == typeof(float)
                                ? OpCodes.Ldind_R4
                                : elem == typeof(double)
                                ? OpCodes.Ldind_R8
                                : throw new NotSupportedException("THIS SHOULD NEVER HAPPEN")
                            )
                            : elem.IsValueType
                            ? new CodeInstruction(OpCodes.Ldobj, elem)
                            : new CodeInstruction(OpCodes.Ldind_Ref)
                        }.AddRangeToArray(innerCast);
                    return true;
                }
                return false;
            }

            public static List<Type> GetArguments(MethodBase method, out List<ParameterInfo> optionals)
            {
                var l = new List<Type>();
                optionals = new List<ParameterInfo>();
                if (!method.IsStatic)
                {
                    l.Add(method.DeclaringType);
                    optionals.Add(null);
                }
                foreach (var p in method.GetParameters())
                {
                    l.Add(p.ParameterType);
                    optionals.Add(p.IsOptional ? p : null);
                }
                return l;
            }
        }

        public void Call(SimpleEventTypes eventType)
        {
            try
            {
                switch (eventType)
                {
                    case SimpleEventTypes.Create:
                        ExtraSettingsAPI.generateSettings(parent);
                        settingsEvents.SettingsCreate();
                        break;
                    case SimpleEventTypes.Open:
                        ExtraSettingsAPI.checkSettingVisibility(parent);
                        settingsEvents.SettingsOpen();
                        break;
                    case SimpleEventTypes.Load:
                        APIBool.Value = true;
                        settingsEvents.Load();
                        break;
                    case SimpleEventTypes.Unload:
                        APIBool.Value = false;
                        settingsEvents.Unload();
                        break;
                    case SimpleEventTypes.Close:
                        settingsEvents.SettingsClose();
                        break;
                    case SimpleEventTypes.WorldLoad:
                        settingsEvents.WorldLoad();
                        break;
                    case SimpleEventTypes.WorldExit:
                        settingsEvents.WorldUnload();
                        break;
                }
            }
            catch (Exception e)
            {
                ExtraSettingsAPI.LogError($"An exception occured in the setting {eventType} event of the {parent.modlistEntry.jsonmodinfo.name} mod\n{e.CleanInvoke()}");
            }
        }

        public void ButtonPress(ModSetting_Button button) => settingsEvents.ButtonPress(button.name);
        public void ButtonPress(ModSetting_MultiButton button, int index) => settingsEvents.ButtonPress(button.name,index);
        public char InputValidation(ModSetting_Input input, string current, int position, char newChar) => settingsEvents.HandleInputValidation(input.name, current, position, newChar);
        public string InputChanged(ModSetting_Input input, string text, ref int caretPosition, ref int selectionPosition)
        {
            settingsEvents.InputChanged(input.name, ref text, ref caretPosition, ref selectionPosition);
            return text;
        }
        public int InputCaretClamp(ModSetting_Input input, string current, int position) => settingsEvents.InputCaretClamp(input.name, current, position);

        public string GetSliderText(ModSetting_Slider slider, float value)
        {
            try
            {
                var r = settingsEvents.HandleSliderText(slider.name,value);
                if (r is string s)
                    return s;
                if (r != null)
                    return r.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return "{null}";
        }

        public bool GetSettingVisible(ModSetting setting)
        {
            try
            {
                return settingsEvents.HandleSettingVisible(setting.name, ExtraSettingsAPI.IsInWorld);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return false;
        }

        public bool GetSettingEnabled(ModSetting setting)
        {
            try
            {
                return settingsEvents.HandleSettingEnabled(setting.name, ExtraSettingsAPI.IsInWorld);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return true;
        }

        public bool Equals(Mod obj)
        {
            if (!obj || GetType() != obj.GetType())
            {
                return false;
            }
            return parent == obj;
        }

        public enum SimpleEventTypes
        {
            Open,
            Close,
            Load,
            Unload,
            Create,
            WorldLoad,
            WorldExit
        }
        public abstract class IEvents
        {
            public virtual void SettingsOpen() { }
            public virtual void SettingsClose() { }
            public virtual void Load() { }
            public virtual void Unload() { }
            public virtual void ButtonPress(string SettingName, int ButtonIndex) { }
            public virtual void ButtonPress(string SettingName) { }
            public virtual char HandleInputValidation(string SettingName, string Before, int CharPosition, char NewChar) => NewChar;
            public virtual int InputCaretClamp(string SettingName, string Text, int Position) => Position;
            public virtual void SettingsCreate() { }
            public virtual object HandleSliderText(string SettingName, float Value)
            {
                ExtraSettingsAPI.LogWarning("You have a custom text slider but there is no suitable declaration of [object ExtraSettingsAPI_HandleSliderText(string, float)] to handle the display");
                return null;
            }
            public virtual bool HandleSettingVisible(string SettingName, bool InWorld)
            {
                ExtraSettingsAPI.LogWarning("You have a setting with custom visibility but there is no suitable declaration of [bool ExtraSettingsAPI_HandleSettingVisible(string, bool)] to control it");
                return false;
            }
            public virtual bool HandleSettingEnabled(string SettingName, bool InWorld) => true;
            public virtual void InputChanged(string SettingName, ref string Value, ref int caretPosition, ref int selectionPosition) { }
            public virtual void WorldLoad() { }
            public virtual void WorldUnload() { }
        }

        static AssemblyBuilder assembly;
        static ModuleBuilder module;
        static ConditionalWeakTable<object, List<MethodInfo>> noCollect = new ConditionalWeakTable<object, List<MethodInfo>>();
        public static void ReleaseModule()
        {
            assembly = null;
            module = null;
            noCollect = new ConditionalWeakTable<object, List<MethodInfo>>();
        }
        static IEvents CreateIEvents(object target)
        {
            if (module == null)
            {
                assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ExtraSettingsAPI-" + DateTime.UtcNow.Ticks), AssemblyBuilderAccess.RunAndCollect);
                module = assembly.DefineDynamicModule(assembly.GetName().Name + ".dll");
            }
            bool inst;
            Type targetType;
            if (target is Type t)
            {
                targetType = t;
                inst = false;
            }
            else
            {
                targetType = target.GetType();
                inst = true;
            }
            // TODO: Figure out how to get the methods to be able to call other class's private methods. Current assumption is that the IgnoresAccessChecksTo attribute is not being given the required assembly name
            /*ExtraSettingsAPI.Log($"Adding access attribute for " + targetType.Assembly.GetName().Name + " (" + targetType.Module.Name + " >> "+targetType.Assembly.Location+") ? ");
            assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(IgnoresAccessChecksToAttribute).GetConstructors()[0], new object[] { targetType.Assembly.GetName().Name }));*/
            var interfaceType = typeof(IEvents);
            var newType = module.DefineType("_ExtraSettingsAPI.IEvents."+targetType.FullName + DateTime.UtcNow.Ticks, TypeAttributes.Public, interfaceType);
            var fields = new Dictionary<FieldBuilder, object>();
            FieldBuilder instField = null;
            if (inst)
                fields[instField = newType.DefineField("instance", targetType, FieldAttributes.Public)] = target;
            //newType.AddInterfaceImplementation(interfaceType); // Note: I'm now using an abstract type as the base instead of an interface so this call is no longer needed for my use-case, but would be needed for potential other cases
            newType.DefineDefaultConstructor(MethodAttributes.Public);
            var loadTargetInstructions = (targetType, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld,instField)
            });
            int privateInd = 0;
            foreach (var baseMethod in interfaceType.GetMethods(~BindingFlags.Default))
                if (baseMethod.IsVirtual || baseMethod.IsAbstract)
                {
                    MethodInfo m1 = null;
                    var s = -1;
                    List<CodeInstruction> instructions = null;
                    foreach (var m in targetType.GetMethods(~BindingFlags.Default))
                        if ((inst || m.IsStatic)
                            && m.Name.Equals("ExtraSettingsAPI_" + baseMethod.Name, StringComparison.InvariantCultureIgnoreCase)
                            && Transpiler.TryGenerateInstructions(baseMethod, m, Transpiler.ReturnHandling.FlexibleType | Transpiler.ReturnHandling.AllowIgnoreReturn, out var l, out var skip, loadTargetInstructions)
                            && (s == -1 || skip < s))
                        {
                            s = skip;
                            m1 = m;
                            instructions = l;
                        }
                    if (m1 == null)
                    {
                        if (baseMethod.IsAbstract)
                            throw new MissingMethodException($"No suitable definition of {baseMethod.ReturnType.ToDisplayName()} ExtraSettingsAPI_{baseMethod.Name}({baseMethod.GetParameters().Select(x => x.ParameterType.ToDisplayName() + " " + x.Name).Join()}) was found");
                        continue;
                    }
                    var newMethod = newType.DefineMethod(baseMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, baseMethod.ReturnType, baseMethod.GetParameters().Select(x => x.ParameterType).ToArray());
                    var il = newMethod.GetILGenerator();
                    foreach (var i in instructions)
                    {
                        // Workaround for being unable to directly call private methods, stores an unsafe delegate pointer for the method and invokes that instead
                        if (i.operand is MethodInfo m && !m.IsPublic)
                        {
                            il.Emit(i, labelsOnly: true);
                            var f = newType.DefineField($"_PrivateMethod{privateInd++}", typeof(IntPtr), FieldAttributes.Public);
                            fields[f] = m.GetPointer();
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, f);
                            il.EmitCalli(OpCodes.Calli, default, m.ReturnType, Transpiler.GetArguments(m, out _).ToArray());
                            continue;
                        }


                        il.Emit(i);
                    }
                    newType.DefineMethodOverride(newMethod, baseMethod);
                }
            var createdType = newType.CreateType();
            var o = (IEvents)Activator.CreateInstance(createdType);
            foreach (var p in fields)
                createdType.GetField(p.Key.Name, ~BindingFlags.Default).SetValue(o, p.Value);
            return o;
        }
    }
}

/*namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class IgnoresAccessChecksToAttribute : Attribute
    {
        public string AssemblyName { get; }

        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }
    }
}*/