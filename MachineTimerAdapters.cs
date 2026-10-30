using System;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;

namespace SkoofidonsHRDepartment
{
    public static partial class MachineTimerController
    {
        private static bool Credit(MachineTimerSnapshot snapshot, ref int elapsed)
        {
            if (!_active || snapshot == null || elapsed <= 0 || snapshot.Remaining <= 0) return false;
            State.GetOrAdopt(snapshot);
            if (!Ledger.TryAdvance(snapshot.Key, snapshot.Identity, elapsed, snapshot.Remaining, out int credit)) return false;
            elapsed = credit;
            return true;
        }

        private static void ChemistryTimePrefix(ChemistryStation __instance, ref int __0)
        {
            if (!_active) return;
            try
            {
                var snapshot = ChemistrySnapshot(__instance);
                int credit = __0;
                if (Credit(snapshot, ref credit))
                {
                    // This native method checks completion BEFORE adding minutes.
                    // Credit once before that check, then retain the original finish path.
                    __instance.CurrentCookOperation.CurrentTime += credit;
                    __0 = 0;
                }
            }
            catch (Exception ex) { Warn("chemistry time", ex); }
        }

        private static void OvenTimePrefix(LabOven __instance, ref int __0)
        {
            if (!_active) return;
            try { Credit(OvenSnapshot(__instance), ref __0); }
            catch (Exception ex) { Warn("oven time", ex); }
        }

        private static bool OvenMinutePrefix(LabOven __instance)
        {
            if (!_active) return true;
            try
            {
                var snapshot = OvenSnapshot(__instance);
                if (snapshot == null || State.GetOrAdopt(snapshot).CapturedRate == 1) return true;
                // The two audited native bodies differ only by inc versus add minutes.
                // Route the normal tick through the same hooked path as time skips once.
                __instance.OnTimePass(1);
                return false;
            }
            catch (Exception ex) { Warn("oven minute", ex); return true; }
        }

        private static void MixingTimePrefix(MixingStation __instance, ref int __0)
        {
            if (!_active) return;
            try { Credit(MixingSnapshot(__instance), ref __0); }
            catch (Exception ex) { Warn("mixing time", ex); }
        }

        private static void CauldronTimePrefix(Cauldron __instance, ref int __0)
        {
            if (!_active) return;
            try { Credit(CauldronSnapshot(__instance), ref __0); }
            catch (Exception ex) { Warn("cauldron time", ex); }
        }

        private static void DryingTimePrefix(DryingRack __instance, int __0)
        {
            if (!_active || __0 <= 0) return;
            try
            {
                float warmth = __instance.GetDryMultiplier(); // Read only; this shared getter is never patched.
                var operations = __instance.DryingOperations;
                for (int i = 0; i < operations.Count; i++)
                {
                    var operation = operations[i];
                    var snapshot = DryingSnapshot(__instance, operation, i);
                    if (snapshot == null) continue;
                    double rate = State.GetOrAdopt(snapshot).CapturedRate;
                    if (rate != 1 && MachineTimerPolicy.TryDryingAdjustment(operation.Time, __0, warmth, rate, out float adjusted))
                        operation.Time = adjusted;
                }
            }
            catch (Exception ex) { Warn("drying time", ex); }
        }

        private static void ChemistryCapturePrefix(ChemistryStation __instance, ChemistryCookOperation __1)
        {
            if (!_active) return;
            try
            {
                if (__1 == null) { Ledger.Forget(Key(MachineTimerKind.Chemistry, __instance.Pointer)); return; }
                var snapshot = ChemistrySnapshot(__instance, __1);
                if (snapshot != null) Capture(snapshot, ConfiguredRate(snapshot.Kind));
            }
            catch (Exception ex) { Warn("chemistry capture", ex); }
        }

        private static void OvenCapturePrefix(LabOven __instance, OvenCookOperation __1)
        {
            if (!_active) return;
            try
            {
                if (__1 == null) { Ledger.Forget(Key(MachineTimerKind.Oven, __instance.Pointer)); return; }
                var snapshot = OvenSnapshot(__instance, __1);
                if (snapshot != null) Capture(snapshot, ConfiguredRate(snapshot.Kind));
            }
            catch (Exception ex) { Warn("oven capture", ex); }
        }

        private static void MixingCapturePrefix(MixingStation __instance, MixOperation __1, int __2)
        {
            if (!_active) return;
            try
            {
                if (__1 == null) { Ledger.Forget(Key(MachineTimerKind.Mixing, __instance.Pointer)); return; }
                var snapshot = MixingSnapshot(__instance, __1, __2);
                if (snapshot != null) Capture(snapshot, ConfiguredRate(snapshot.Kind));
            }
            catch (Exception ex) { Warn("mixing capture", ex); }
        }

        private static void CauldronCapturePrefix(Cauldron __instance, int __1, EQuality __2)
        {
            if (!_active) return;
            try
            {
                CauldronEpochs.TryGetValue(__instance.Pointer, out long epoch);
                CauldronEpochs[__instance.Pointer] = epoch + 1;
                Ledger.Forget(Key(MachineTimerKind.Cauldron, __instance.Pointer));
                var snapshot = CauldronSnapshot(__instance, __1, (int)__2);
                if (snapshot != null) Capture(snapshot, ConfiguredRate(snapshot.Kind));
            }
            catch (Exception ex) { Warn("cauldron capture", ex); }
        }

        private static void DryingPreparePrefix(DryingRack __instance, Il2CppFishNet.Connection.NetworkConnection __0, DryingOperation __1, out long __state)
        {
            __state = 0;
            if (!_active || __1 == null || (__0 != null && __0.Pointer != IntPtr.Zero)) return;
            try
            {
                if (__1.Quantity <= 0) return;
                var snapshot = DryingSnapshot(__instance, __1, __instance.DryingOperations.Count);
                __state = DryingCaptures.Enqueue(snapshot.Guid, snapshot.Fingerprint, snapshot.Progress, ConfiguredRate(MachineTimerKind.Drying));
            }
            catch (Exception ex) { Warn("drying capture", ex); }
        }

        private static Exception DryingPrepareFinalizer(long __state, Exception __exception)
        {
            if (__exception != null) DryingCaptures.Cancel(__state);
            return __exception;
        }

        private static void DryingReceivePrefix(DryingRack __instance, DryingOperation __1)
        {
            if (!_active || __1 == null) return;
            try
            {
                var snapshot = DryingSnapshot(__instance, __1, __instance.DryingOperations.Count);
                if (DryingCaptures.TryTake(snapshot.Guid, snapshot.Fingerprint, snapshot.Progress, out double capturedRate))
                {
                    // A new receiver may reuse a previously collected native pointer.
                    Ledger.Forget(snapshot.Key);
                    Capture(snapshot, capturedRate);
                }
                else State.GetOrAdopt(snapshot);
            }
            catch (Exception ex) { Warn("drying adoption", ex); }
        }
    }
}
