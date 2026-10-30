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

[assembly: MelonInfo(typeof(SkoofidonsHRDepartment.Diagnostics.ProbeMod), "HR Botanist Timing Probe", "0.1.0", "Skoofidon")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SkoofidonsHRDepartment.Diagnostics
{
    public sealed class ProbeMod : MelonMod
    {
        private sealed class Observation
        {
            public string Kind;
            public float WaitSeconds;
            public long StartedAt;
        }

        private static ProbeMod Instance;
        private static bool Active;
        private static bool ObservationErrorLogged;
        private static readonly Dictionary<string, int> Samples = new Dictionary<string, int>();
        // Stores managed values only, never retains Unity/IL2CPP action objects.
        private static readonly Dictionary<IntPtr, Observation> Pending = new Dictionary<IntPtr, Observation>();
        private HarmonyController ProbeHarmony;

        public override void OnInitializeMelon()
        {
            Instance = this;
            try
            {
                string gameHash;
                using (var stream = File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll")))
                using (var hash = SHA512.Create()) gameHash = Convert.ToHexString(hash.ComputeHash(stream));
                if (!ProbeProtocol.MatchesGame(gameHash))
                    throw new InvalidOperationException("Game binary differs from the audited 0.4.7f12 build. No diagnostic patch installed.");

                foreach (var name in new[] { "__1__state", "__4__this", "_waitTime_5__2", "_i_5__3" })
                    if (typeof(Routine).GetProperty(name)?.CanRead != true)
                        throw new MissingMemberException("Coroutine field binding missing: " + name);
                var target = typeof(Routine).GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (target == null || target.ReturnType != typeof(bool)) throw new MissingMethodException("Audited MoveNext binding is missing.");
                var info = HarmonyController.GetPatchInfo(target);
                if (info != null && info.Owners.Count > 0) throw new InvalidOperationException("The diagnostic coroutine already has Harmony owners; probe left inactive.");
                IntPtr body = NativeBody(target);
                if (body == IntPtr.Zero) throw new InvalidOperationException("Coroutine has no native body.");

                ProbeHarmony = new HarmonyController("SkoofidonsHRDepartment.BotanistTimingProbe.Observation");
                ProbeHarmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(ProbeMod).GetMethod(nameof(MoveNextPrefix), BindingFlags.NonPublic | BindingFlags.Static)),
                    postfix: new HarmonyMethod(typeof(ProbeMod).GetMethod(nameof(MoveNextPostfix), BindingFlags.NonPublic | BindingFlags.Static)));
                Active = true;
                LoggerInstance.Msg("[BOTANIST_PROBE READY] 0.1.0 observation-only; one contextual MoveNext hook. GetActionDuration is NOT patched. No timing settings or game values changed.");
            }
            catch (Exception ex)
            {
                Active = false;
                ProbeHarmony?.UnpatchSelf();
                LoggerInstance.Warning("[BOTANIST_PROBE INACTIVE] " + ex.Message);
            }
        }

        private static unsafe IntPtr NativeBody(MethodInfo method)
        {
            var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
            if (field == null) return IntPtr.Zero;
            var pointer = (IntPtr)field.GetValue(null);
            if (pointer == IntPtr.Zero) return IntPtr.Zero;
            return UnityVersionHandler.Wrap((Il2CppMethodInfo*)pointer).MethodPointer;
        }

        private static void MoveNextPrefix(Routine __instance, out int __state)
        {
            __state = int.MinValue;
            if (!Active || __instance == null) return;
            try
            {
                __state = __instance.__1__state;
                // A cancelled iterator may be collected and its address reused.
                // Reset stale attribution even when the new kind hit its cap.
                if (__state == 0) Pending.Remove(__instance.Pointer);
            }
            catch (Exception ex) { LogObservationError(ex); }
        }

        private static void MoveNextPostfix(Routine __instance, bool __result, int __state)
        {
            if (!Active || __instance == null || __state == int.MinValue) return;
            try
            {
                if (__state == 0 && __result && ProbeProtocol.IsFirstWait(__state, __instance.__1__state, __result))
                    ObserveStart(__instance);
                else if (!__result && Pending.TryGetValue(__instance.Pointer, out var observation))
                {
                    Pending.Remove(__instance.Pointer);
                    double seconds = (Stopwatch.GetTimestamp() - observation.StartedAt) / (double)Stopwatch.Frequency;
                    Instance.LoggerInstance.Msg(string.Format(CultureInfo.InvariantCulture,
                        "[BOTANIST_PROBE END] kind={0} wait={1:0.###} accumulated={2:0.###} realSeconds={3:0.###}",
                        observation.Kind, observation.WaitSeconds, __instance._i_5__3, seconds));
                }
            }
            catch (Exception ex) { LogObservationError(ex); }
        }

        private static void ObserveStart(Routine routine)
        {
            var owner = routine.__4__this;
            if (owner == null) return;
            string nativeType = owner.GetIl2CppType().FullName;
            string kind = ProbeProtocol.Classify(nativeType);
            Samples.TryGetValue(kind, out int sampled);
            if (!ProbeProtocol.ShouldLog(sampled)) return;
            Samples[kind] = sampled + 1;
            var botanist = owner._botanist;
            float speed = botanist == null ? float.NaN : botanist.CurrentWorkSpeed;
            float wait = routine._waitTime_5__2;
            Pending[routine.Pointer] = new Observation { Kind = kind, WaitSeconds = wait, StartedAt = Stopwatch.GetTimestamp() };
            Instance.LoggerInstance.Msg(string.Format(CultureInfo.InvariantCulture,
                "[BOTANIST_PROBE START] kind={0} nativeType={1} botanist={2} wait={3:0.###} workSpeed={4:0.###} derivedBase={5:0.###} accumulated={6:0.###}",
                kind, nativeType, botanist != null, wait, speed, wait * speed, routine._i_5__3));
        }

        private static void LogObservationError(Exception ex)
        {
            if (ObservationErrorLogged) return;
            ObservationErrorLogged = true;
            Instance?.LoggerInstance.Warning("[BOTANIST_PROBE OBSERVATION ERROR] " + ex);
        }

        private void SummaryAndClear()
        {
            if (Samples.Count > 0)
                LoggerInstance.Msg("[BOTANIST_PROBE SUMMARY] sampled=" + string.Join(",", Samples.Select(p => p.Key + ":" + p.Value)) +
                    "; unfinished=" + Pending.Count + " (may include cancelled actions); maximum 8 samples per action kind per scene.");
            Pending.Clear();
            Samples.Clear();
            ObservationErrorLogged = false;
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main") SummaryAndClear();
        }

        public override void OnDeinitializeMelon()
        {
            Active = false;
            ProbeHarmony?.UnpatchSelf();
            SummaryAndClear();
            Instance = null;
        }
    }
}
