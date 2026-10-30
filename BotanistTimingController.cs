using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using HarmonyController = HarmonyLib.Harmony;
using Routine = Il2CppScheduleOne.NPCs.Behaviour.GrowContainerBehaviour.ObjectCompilerGeneratedNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObGrSiSiObObUnique;

namespace SkoofidonsHRDepartment
{
    /// <summary>
    /// Opt-in, manually registered patch of the audited contextual iterator.
    /// No GetActionDuration patch: that native body is shared with unrelated graphics code.
    /// Registration/API checks do not establish gameplay correctness; joint runtime testing is still required.
    /// </summary>
    public static class BotanistTimingController
    {
        private const string HarmonyOwner = "SkoofidonsHRDepartment.BotanistActionTiming";
        private const string ExpectedGameSha512 = "872DFB646C9B866C53DAE12D7A34E1F9DF21A3E16FAC6B7C6CD21BF5FC8E4B3F99932163C5DE7E3798D37641712E82B914D46E1F023D56E77970BE84FB74EC3A";
        private const long MoveNextRva = 0x944380;
        private const int MaximumSamplesPerAction = 8;
        private static readonly Dictionary<TimingAction, int> Samples = new();
        private static HarmonyController _harmony;
        private static MethodInfo _target;
        private static bool _initialized;
        private static bool _active;
        private static bool _registrationErrorLogged;
        private static bool _hookErrorLogged;

        public static void Initialize()
        {
            _initialized = true;
            RefreshRegistration();
        }

        public static void RefreshRegistration()
        {
            try
            {
                if (!_initialized || Config.BotanistActionTimingEnabled?.Value != true)
                {
                    RemovePatch();
                    _registrationErrorLogged = false;
                    return;
                }
                if (_active)
                {
                    RejectOtherOwners(_target);
                    RejectProbeAssembly();
                    return;
                }

                // Resolve the generated method; the audited native address is only
                // a validation guard, never a raw-address detour target.
                RejectProbeAssembly();
                ValidateGameHash();
                ValidateProperty("__1__state", typeof(int), false);
                ValidateProperty("__4__this", typeof(Il2CppScheduleOne.NPCs.Behaviour.GrowContainerBehaviour), false);
                ValidateProperty("_waitTime_5__2", typeof(float), true);
                ValidateProperty("_i_5__3", typeof(float), false);
                var target = typeof(Routine).GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null, Type.EmptyTypes, null);
                if (target == null || target.ReturnType != typeof(bool))
                    throw new MissingMethodException("Audited coroutine MoveNext binding is missing.");
                RejectOtherOwners(target);
                IntPtr nativeBody = NativeBody(target);
                using (var process = Process.GetCurrentProcess())
                {
                    var module = process.Modules.Cast<ProcessModule>().FirstOrDefault(m =>
                        string.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
                    if (module == null || nativeBody == IntPtr.Zero || nativeBody.ToInt64() != module.BaseAddress.ToInt64() + MoveNextRva)
                        throw new InvalidOperationException("Coroutine native pointer differs from the audited contextual target.");
                }

                _target = target;
                _harmony = new HarmonyController(HarmonyOwner);
                _harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(BotanistTimingController).GetMethod(nameof(MoveNextPrefix), BindingFlags.NonPublic | BindingFlags.Static)),
                    postfix: new HarmonyMethod(typeof(BotanistTimingController).GetMethod(nameof(MoveNextPostfix), BindingFlags.NonPublic | BindingFlags.Static)));
                _active = true;
                _registrationErrorLogged = false;
                MelonLogger.Msg("[HR BOTANIST TIMING READY] Audited contextual coroutine registered; settings affect next actions. Runtime gameplay validation is pending.");
            }
            catch (Exception ex)
            {
                RemovePatch();
                if (!_registrationErrorLogged)
                {
                    _registrationErrorLogged = true;
                    MelonLogger.Warning("[HR BOTANIST TIMING INACTIVE] " + ex.Message);
                }
            }
        }

        private static void ValidateGameHash()
        {
            using var stream = File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll"));
            using var hash = SHA512.Create();
            if (!string.Equals(Convert.ToHexString(hash.ComputeHash(stream)), ExpectedGameSha512, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Game binary differs from the audited 0.4.7f12 build.");
        }

        private static void ValidateProperty(string name, Type type, bool writable)
        {
            var property = typeof(Routine).GetProperty(name);
            if (property == null || !property.CanRead || property.PropertyType != type || (writable && !property.CanWrite))
                throw new MissingMemberException("Coroutine field binding differs from audit: " + name);
        }

        private static void RejectProbeAssembly()
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a =>
                string.Equals(a.GetName().Name, "SkoofidonsHRBotanistProbe", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Separate botanist probe is loaded; remove it before enabling action timings.");
        }

        private static void RejectOtherOwners(MethodInfo target)
        {
            var info = HarmonyController.GetPatchInfo(target);
            // An owner left behind after an unpatch failure is also a conflict:
            // re-registering it could run the harvest postfix twice.
            if (info != null && info.Owners.Any(owner => owner != HarmonyOwner || !_active))
                throw new InvalidOperationException("Contextual coroutine already has another Harmony owner; timings left inactive.");
        }

        private static unsafe IntPtr NativeBody(MethodInfo method)
        {
            var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
            if (field == null) return IntPtr.Zero;
            var pointer = (IntPtr)field.GetValue(null);
            return pointer == IntPtr.Zero ? IntPtr.Zero : UnityVersionHandler.Wrap((Il2CppMethodInfo*)pointer).MethodPointer;
        }

        private static void MoveNextPrefix(Routine __instance, out int __state)
        {
            __state = int.MinValue;
            if (!_active || Config.BotanistActionTimingEnabled?.Value != true || __instance == null) return;
            try { __state = __instance.__1__state; }
            catch (Exception ex) { LogHookError(ex); }
        }

        private static void MoveNextPostfix(Routine __instance, bool __result, int __state)
        {
            if (!_active || Config.BotanistActionTimingEnabled?.Value != true || __instance == null || __state != 0) return;
            try
            {
                if (!BotanistTimingPolicy.IsFirstWait(__state, __instance.__1__state, __result) || __instance._i_5__3 != 0f) return;
                var owner = __instance.__4__this;
                if (owner == null) return;
                var kind = BotanistTimingPolicy.Classify(owner.GetIl2CppType().FullName);
                if (kind == TimingAction.Unknown) return;
                var botanist = owner._botanist;
                var npc = owner.Npc;
                if (botanist == null || npc == null || npc.Pointer != botanist.Pointer ||
                    npc.GetIl2CppType().FullName != "ScheduleOne.Employees.Botanist") return;
                float originalWait = __instance._waitTime_5__2;
                float speed = botanist.CurrentWorkSpeed;
                float multiplier = ConfiguredMultiplier(kind);
                if (!BotanistTimingPolicy.TryResolveWait(kind, originalWait, speed, multiplier, true, out float desiredWait))
                {
                    LogSample(kind, "SKIPPED", originalWait, originalWait, speed, multiplier);
                    return;
                }
                // Initial 0 -> 1 yield only; later resumes cannot scale the wait again.
                if (desiredWait != originalWait) __instance._waitTime_5__2 = desiredWait;
                LogSample(kind, desiredWait == originalWait ? "UNCHANGED" : "APPLIED", originalWait, desiredWait, speed, multiplier);
            }
            catch (Exception ex) { LogHookError(ex); }
        }

        private static float ConfiguredMultiplier(TimingAction kind) => kind switch
        {
            TimingAction.Soil => Config.BotanistSoilSpeed.Value,
            TimingAction.Water => Config.BotanistWaterSpeed.Value,
            TimingAction.Additive => Config.BotanistAdditiveSpeed.Value,
            TimingAction.Seed => Config.BotanistSeedSpeed.Value,
            TimingAction.Harvest => Config.BotanistHarvestSpeed.Value,
            _ => float.NaN
        };

        private static void LogSample(TimingAction kind, string status, float original, float desired, float speed, float multiplier)
        {
            Samples.TryGetValue(kind, out int count);
            if (count >= MaximumSamplesPerAction) return;
            Samples[kind] = count + 1;
            MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                "[HR BOTANIST TIMING {0}] action={1} originalWait={2:0.###} desiredWait={3:0.###} workSpeed={4:0.###} multiplier={5:0.###}; sample={6}/8",
                status, kind, original, desired, speed, multiplier, count + 1));
        }

        private static void LogHookError(Exception ex)
        {
            if (_hookErrorLogged) return;
            _hookErrorLogged = true;
            MelonLogger.Warning("[HR BOTANIST TIMING ACTION SKIPPED] " + ex.Message);
        }

        private static void RemovePatch()
        {
            bool wasActive = _active;
            _active = false;
            try
            {
                _harmony?.UnpatchSelf();
                _harmony = null;
                _target = null;
                if (wasActive) MelonLogger.Msg("[HR BOTANIST TIMING DISABLED] Coroutine patch removed; ongoing actions keep their existing wait.");
            }
            catch (Exception ex) { LogHookError(ex); }
        }

        public static void ClearState()
        {
            Samples.Clear();
            _hookErrorLogged = false;
        }

        public static void Deinitialize()
        {
            _initialized = false;
            RemovePatch();
            ClearState();
        }
    }
}
