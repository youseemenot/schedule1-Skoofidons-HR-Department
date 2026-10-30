using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppScheduleOne.NPCs.Behaviour;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using HarmonyController = HarmonyLib.Harmony;
using PickUp = Il2CppScheduleOne.NPCs.Behaviour.PickUpTrashBehaviour.ObjectCompilerGeneratedNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObPiObObUnique;
using BagCan = Il2CppScheduleOne.NPCs.Behaviour.BagTrashCanBehaviour.ObjectCompilerGeneratedNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObBaObObUnique;
using EmptyGrabber = Il2CppScheduleOne.NPCs.Behaviour.EmptyTrashGrabberBehaviour.ObjectCompilerGeneratedNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObEmObObUnique;
using GrabBag = Il2CppScheduleOne.NPCs.Behaviour.DisposeTrashBagBehaviour.ObjectCompilerGeneratedNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObDiVoObMoInVoBoOb1;
using DropBag = Il2CppScheduleOne.NPCs.Behaviour.DisposeTrashBagBehaviour.ObjectCompilerGeneratedNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObDiVoObMoInVoBoOb0;

namespace SkoofidonsHRDepartment
{
    /// <summary>
    /// Opt-in contextual wait replacement. The five native bodies were unique in all
    /// 111 Cpp2IL assemblies of the pinned build. Offline guards do not prove gameplay correctness.
    /// </summary>
    public static class CleanerActionSpeedController
    {
        private const string HarmonyOwner = "SkoofidonsHRDepartment.CleanerActionSpeed";
        private const string ExpectedGameSha512 = "872DFB646C9B866C53DAE12D7A34E1F9DF21A3E16FAC6B7C6CD21BF5FC8E4B3F99932163C5DE7E3798D37641712E82B914D46E1F023D56E77970BE84FB74EC3A";
        private const int MaximumSamplesPerAction = 8;
        private static readonly Target[] Targets =
        {
            new("PickUpTrash", typeof(PickUp), typeof(PickUpTrashBehaviour), 0x944030, nameof(PickUpPostfix)),
            new("BagTrashCan", typeof(BagCan), typeof(BagTrashCanBehaviour), 0x92ED80, nameof(BagCanPostfix)),
            new("EmptyTrashGrabber", typeof(EmptyGrabber), typeof(EmptyTrashGrabberBehaviour), 0x92E7F0, nameof(EmptyGrabberPostfix)),
            new("GrabTrashBag", typeof(GrabBag), typeof(DisposeTrashBagBehaviour), 0x92E3F0, nameof(GrabBagPostfix)),
            new("DropTrashBag", typeof(DropBag), typeof(DisposeTrashBagBehaviour), 0x92DF60, nameof(DropBagPostfix))
        };
        private static HarmonyController _harmony;
        private static bool _initialized;
        private static bool _active;
        private static bool _registrationErrorLogged;
        private static bool _hookErrorLogged;

        private sealed class Target
        {
            internal readonly string Name;
            internal readonly Type RoutineType, OwnerType;
            internal readonly long Rva;
            internal readonly string Callback;
            internal MethodInfo Method;
            internal PropertyInfo Owner, Current;
            internal int Samples;
            internal Target(string name, Type routine, Type owner, long rva, string callback)
            { Name = name; RoutineType = routine; OwnerType = owner; Rva = rva; Callback = callback; }
        }

        public static void Initialize()
        {
            _initialized = true;
            RefreshRegistration();
        }

        public static void RefreshRegistration()
        {
            try
            {
                if (!_initialized || !CleanerSpeedPolicy.RequiresPatch(Config.CleanerWorkSpeed?.Value ?? 1f))
                {
                    RemovePatches();
                    _registrationErrorLogged = false;
                    return;
                }
                if (_active)
                {
                    foreach (var target in Targets) RejectOtherOwners(target.Method);
                    return;
                }

                ValidateGameHash();
                using (var process = Process.GetCurrentProcess())
                {
                    var module = process.Modules.Cast<ProcessModule>().FirstOrDefault(m =>
                        string.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
                    if (module == null) throw new InvalidOperationException("GameAssembly module is unavailable.");
                    foreach (var target in Targets) ValidateTarget(target, module.BaseAddress);
                }

                _harmony ??= new HarmonyController(HarmonyOwner);
                foreach (var target in Targets)
                    _harmony.Patch(target.Method, postfix: new HarmonyMethod(typeof(CleanerActionSpeedController)
                        .GetMethod(target.Callback, BindingFlags.NonPublic | BindingFlags.Static)));
                _active = true;
                _registrationErrorLogged = false;
                MelonLogger.Msg("[HR CLEANER SPEED READY] Five audited contextual coroutines registered; future waits use the multiplier. Runtime gameplay validation is pending.");
            }
            catch (Exception ex)
            {
                RemovePatches();
                if (_registrationErrorLogged) return;
                _registrationErrorLogged = true;
                MelonLogger.Warning("[HR CLEANER SPEED INACTIVE] " + ex.Message);
            }
        }

        private static void ValidateGameHash()
        {
            using var stream = File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll"));
            using var hash = SHA512.Create();
            if (!string.Equals(Convert.ToHexString(hash.ComputeHash(stream)), ExpectedGameSha512, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Game binary differs from the audited 0.4.7f12 build.");
        }

        private static void ValidateTarget(Target target, IntPtr moduleBase)
        {
            ValidateProperty(target.RoutineType, "__1__state", typeof(int), false);
            target.Owner = ValidateProperty(target.RoutineType, "__4__this", target.OwnerType, false);
            target.Current = ValidateProperty(target.RoutineType, "__2__current", typeof(Il2CppSystem.Object), true);
            target.Method = target.RoutineType.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, Type.EmptyTypes, null);
            if (target.Method == null || target.Method.ReturnType != typeof(bool))
                throw new MissingMethodException("Audited Cleaner coroutine MoveNext binding is missing: " + target.Name);
            RejectOtherOwners(target.Method);
            // Resolve and patch the generated MethodInfo only. The native RVA is a guard,
            // never a detour target, and no shared WaitForSeconds constructor is patched.
            IntPtr body = NativeBody(target.Method);
            if (body == IntPtr.Zero || body.ToInt64() != moduleBase.ToInt64() + target.Rva)
                throw new InvalidOperationException("Cleaner coroutine native body differs from audit: " + target.Name);
        }

        private static PropertyInfo ValidateProperty(Type routine, string name, Type expected, bool writable)
        {
            var property = routine.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (property == null || property.PropertyType != expected || property.GetMethod == null ||
                property.GetMethod.IsStatic || property.GetIndexParameters().Length != 0 ||
                (writable && (property.SetMethod == null || property.SetMethod.IsStatic)))
                throw new MissingMemberException("Cleaner coroutine field binding differs from audit: " + routine.Name + "." + name);
            return property;
        }

        private static void RejectOtherOwners(MethodInfo target)
        {
            var patches = HarmonyController.GetPatchInfo(target);
            // A stale own patch after failed cleanup is also a conflict; never scale twice.
            if (patches != null && patches.Owners.Any(owner => owner != HarmonyOwner || !_active))
                throw new InvalidOperationException("Cleaner coroutine already has a conflicting Harmony owner.");
        }

        private static unsafe IntPtr NativeBody(MethodInfo method)
        {
            var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
            if (field == null) return IntPtr.Zero;
            var pointer = (IntPtr)field.GetValue(null);
            return pointer == IntPtr.Zero ? IntPtr.Zero : UnityVersionHandler.Wrap((Il2CppMethodInfo*)pointer).MethodPointer;
        }

        // Concrete wrapper types keep Harmony's instance injection and CLR type constraints explicit.
        private static void PickUpPostfix(PickUp __instance, bool __result) => ReplaceWait(Targets[0], __instance, __result);
        private static void BagCanPostfix(BagCan __instance, bool __result) => ReplaceWait(Targets[1], __instance, __result);
        private static void EmptyGrabberPostfix(EmptyGrabber __instance, bool __result) => ReplaceWait(Targets[2], __instance, __result);
        private static void GrabBagPostfix(GrabBag __instance, bool __result) => ReplaceWait(Targets[3], __instance, __result);
        private static void DropBagPostfix(DropBag __instance, bool __result) => ReplaceWait(Targets[4], __instance, __result);

        private static void ReplaceWait(Target target, Il2CppObjectBase routine, bool running)
        {
            if (!_active || !running || routine == null) return;
            try
            {
                float multiplier = Config.CleanerWorkSpeed?.Value ?? 1f;
                if (!CleanerSpeedPolicy.RequiresPatch(multiplier)) return;
                var owner = target.Owner.GetValue(routine) as Il2CppScheduleOne.NPCs.Behaviour.Behaviour;
                var npc = owner?.Npc;
                if (npc == null) return;
                var current = target.Current.GetValue(routine) as Il2CppSystem.Object;
                var wait = current?.TryCast<WaitForSeconds>();
                if (!CleanerSpeedPolicy.IsEligibleYield(running, npc.GetIl2CppType().FullName, wait != null)) return;
                float original = wait.m_Seconds;
                if (!CleanerSpeedPolicy.TryScaleDuration(original, multiplier, out float desired) || desired == original) return;

                // Each true MoveNext yield is observed once. Replace the instruction rather
                // than mutating a potentially shared wait; an already scheduled wait stays intact.
                target.Current.SetValue(routine, new WaitForSeconds(desired));
                if (target.Samples >= MaximumSamplesPerAction) return;
                target.Samples++;
                MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                    "[HR CLEANER SPEED APPLIED] action={0} originalWait={1:0.###} desiredWait={2:0.###} multiplier={3:0.###}; sample={4}/8",
                    target.Name, original, desired, multiplier, target.Samples));
            }
            catch (Exception ex) { LogHookError(ex); }
        }

        private static void RemovePatches()
        {
            bool wasActive = _active;
            _active = false;
            try
            {
                _harmony?.UnpatchSelf();
                _harmony = null;
                foreach (var target in Targets) { target.Method = null; target.Owner = null; target.Current = null; }
                if (wasActive) MelonLogger.Msg("[HR CLEANER SPEED DISABLED] Contextual patches removed; already scheduled waits keep their duration.");
            }
            catch (Exception ex) { LogHookError(ex); }
        }

        private static void LogHookError(Exception ex)
        {
            if (_hookErrorLogged) return;
            _hookErrorLogged = true;
            MelonLogger.Warning("[HR CLEANER SPEED ACTION SKIPPED] " + ex.Message);
        }

        public static void ClearState()
        {
            foreach (var target in Targets) target.Samples = 0;
            _hookErrorLogged = false;
            _registrationErrorLogged = false;
        }

        public static void Deinitialize()
        {
            _initialized = false;
            RemovePatches();
            ClearState();
        }
    }
}
