using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime.Runtime;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;
using MelonLoader.Utils;
using HarmonyController = HarmonyLib.Harmony;

namespace SkoofidonsHRDepartment
{
    public static partial class MachineTimerController
    {
        private const string HarmonyOwner = "SkoofidonsHRDepartment.MachineTimers";
        private const string ExpectedGameSha512 = "872DFB646C9B866C53DAE12D7A34E1F9DF21A3E16FAC6B7C6CD21BF5FC8E4B3F99932163C5DE7E3798D37641712E82B914D46E1F023D56E77970BE84FB74EC3A";
        private sealed record PatchSpec(string Owner, string Name, string[] Parameters, string ReturnType, long Rva, string Prefix, string Postfix, string Finalizer);
        private static HarmonyController _machineHarmony;

        private static MethodInfo Resolve(PatchSpec spec)
        {
            Type type = typeof(ChemistryStation).Assembly.GetType(spec.Owner, throwOnError: true);
            var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == spec.Name && m.GetParameters().Select(p => p.ParameterType.FullName).SequenceEqual(spec.Parameters)).ToArray();
            if (candidates.Length != 1 || candidates[0].ReturnType.FullName != spec.ReturnType)
                throw new MissingMethodException("Machine binding differs: " + spec.Owner + "." + spec.Name);
            return candidates[0];
        }

        private static MethodInfo Callback(string name) => name == null ? null : typeof(MachineTimerController)
            .GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("Machine callback missing: " + name);

        // This check resolves/JIT-compiles managed wrappers only. It never initializes
        // a native class or executes a Unity callback, so the real DLL can be checked offline.
        public static int VerifyManagedBindings()
        {
            foreach (PatchSpec spec in PatchSpecs)
            {
                MethodInfo target = Resolve(spec);
                RuntimeHelpers.PrepareMethod(target.MethodHandle);
                Type state = null;
                foreach (string name in new[] { spec.Prefix, spec.Postfix, spec.Finalizer })
                {
                    MethodInfo callback = Callback(name);
                    if (callback == null) continue;
                    RuntimeHelpers.PrepareMethod(callback.MethodHandle);
                    foreach (var parameter in callback.GetParameters())
                    {
                        Type type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType;
                        if (parameter.Name == "__instance" && !type.IsAssignableFrom(target.DeclaringType)) throw new InvalidOperationException("Incompatible machine instance: " + name);
                        if (parameter.Name == "__result" && !type.IsAssignableFrom(target.ReturnType)) throw new InvalidOperationException("Incompatible machine result: " + name);
                        if (parameter.Name == "__state")
                        {
                            if (state != null && state != type) throw new InvalidOperationException("Incompatible machine state: " + name);
                            state = type;
                        }
                        if (parameter.Name?.Length > 2 && parameter.Name.StartsWith("__") && int.TryParse(parameter.Name.Substring(2), out int index))
                        {
                            var arguments = target.GetParameters();
                            if (index >= arguments.Length || type != arguments[index].ParameterType) throw new InvalidOperationException("Incompatible machine argument: " + name);
                        }
                    }
                }
            }
            return PatchSpecs.Length;
        }

        public static void Initialize()
        {
            try
            {
                VerifyManagedBindings();
                using (var stream = File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll")))
                using (var hash = SHA512.Create())
                    if (!string.Equals(Convert.ToHexString(hash.ComputeHash(stream)), ExpectedGameSha512, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Game binary differs from the audited machine timer build.");
                using var process = Process.GetCurrentProcess();
                var module = process.Modules.Cast<ProcessModule>().FirstOrDefault(m => string.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
                if (module == null) throw new InvalidOperationException("GameAssembly module unavailable.");
                var methods = PatchSpecs.Select(Resolve).ToArray();
                for (int i = 0; i < methods.Length; i++)
                {
                    if (NativeAddress(methods[i]).ToInt64() != module.BaseAddress.ToInt64() + PatchSpecs[i].Rva)
                        throw new InvalidOperationException("Native machine target differs: " + PatchSpecs[i].Owner + "." + PatchSpecs[i].Name);
                    var info = HarmonyController.GetPatchInfo(methods[i]);
                    if (info != null && info.Owners.Any()) throw new InvalidOperationException("Machine target has a conflicting Harmony owner: " + PatchSpecs[i].Name);
                }
                _machineHarmony = new HarmonyController(HarmonyOwner);
                for (int i = 0; i < methods.Length; i++)
                {
                    var spec = PatchSpecs[i];
                    HarmonyMethod Wrap(string name) => name == null ? null : new HarmonyMethod(Callback(name));
                    _machineHarmony.Patch(methods[i], prefix: Wrap(spec.Prefix), postfix: Wrap(spec.Postfix), finalizer: Wrap(spec.Finalizer));
                }
                _active = true;
                MelonLogger.Msg("[HR MACHINE TIMERS READY] " + methods.Length + " audited targets; seven captured new-operation rates. Runtime testing pending.");
            }
            catch (Exception ex)
            {
                Deinitialize();
                MelonLogger.Warning("[HR MACHINE TIMERS INACTIVE] " + ex.Message);
            }
        }

        private static unsafe IntPtr NativeAddress(MethodInfo method)
        {
            var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
            if (field == null) return IntPtr.Zero;
            var pointer = (IntPtr)field.GetValue(null);
            return pointer == IntPtr.Zero ? IntPtr.Zero : UnityVersionHandler.Wrap((Il2CppMethodInfo*)pointer).MethodPointer;
        }

        public static void Deinitialize()
        {
            _active = false;
            try { _machineHarmony?.UnpatchSelf(); }
            catch (Exception ex) { Warn("removing machine patches", ex); }
            _machineHarmony = null;
            ClearState();
        }
    }
}
