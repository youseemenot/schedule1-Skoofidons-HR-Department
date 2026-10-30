using System;
using System.Globalization;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.UI.Stations;

namespace SkoofidonsHRDepartment
{
    public static partial class MachineTimerController
    {
        private static bool Eta(MachineTimerSnapshot snapshot, out int minutes)
        {
            minutes = 0;
            if (!_active || snapshot == null) return false;
            var record = State.GetOrAdopt(snapshot);
            return record.CapturedRate != 1 && MachineTimerPolicy.TryEta(snapshot.Remaining, record.Fraction, record.CapturedRate, out minutes);
        }

        private static void ChemistryClockPostfix(ChemistryStation __instance)
        {
            if (!_active) return;
            try { if (Eta(ChemistrySnapshot(__instance), out int minutes)) __instance.Alarm?.DisplayMinutes(minutes); }
            catch (Exception ex) { Warn("chemistry ETA", ex); }
        }

        private static void CauldronClockPostfix(Cauldron __instance)
        {
            if (!_active) return;
            try { if (Eta(CauldronSnapshot(__instance), out int minutes)) __instance.Alarm?.DisplayMinutes(minutes); }
            catch (Exception ex) { Warn("cauldron ETA", ex); }
        }

        private static void OvenAppearancePostfix(LabOven __instance)
        {
            if (!_active) return;
            try
            {
                var snapshot = OvenSnapshot(__instance);
                if (snapshot?.Remaining > 0 && Eta(snapshot, out int minutes) && __instance.TimerLabel != null)
                    __instance.TimerLabel.text = (minutes / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutes % 60).ToString("00", CultureInfo.InvariantCulture);
            }
            catch (Exception ex) { Warn("oven ETA", ex); }
        }

        private static void MixingClockPostfix(MixingStation __instance)
        {
            if (!_active) return;
            try { if (Eta(MixingSnapshot(__instance), out int minutes)) __instance.Clock?.DisplayMinutes(minutes); }
            catch (Exception ex) { Warn("mixing ETA", ex); }
        }

        private static void MixingScreenPostfix(MixingStationMk2 __instance)
        {
            if (!_active) return;
            try
            {
                if (Eta(MixingSnapshot(__instance), out int minutes) && __instance.ProgressLabel != null)
                    __instance.ProgressLabel.text = minutes.ToString(CultureInfo.InvariantCulture) + " mins remaining";
            }
            catch (Exception ex) { Warn("mixing Mk2 ETA", ex); }
        }

        private readonly record struct DryUiState(bool Changed, float OriginalRate);
        private static void DryingUiPrefix(DryingOperationUI __instance, out DryUiState __state)
        {
            __state = default;
            if (!_active) return;
            try
            {
                var operation = __instance.AssignedOperation;
                // A display may run before restored native operations are adopted.
                // A nonmutating lookup must not block their saved-rate restoration.
                if (operation == null || !Ledger.TryGet(Key(MachineTimerKind.Drying, operation.Pointer), out var record) || record.CapturedRate == 1) return;
                float original = __instance._dryMultiplier;
                float scaled = (float)(original * record.CapturedRate);
                if (!float.IsFinite(scaled) || scaled <= 0) return;
                __state = new(true, original);
                __instance._dryMultiplier = scaled;
            }
            catch (Exception ex) { Warn("drying ETA setup", ex); }
        }

        private static Exception DryingUiFinalizer(DryingOperationUI __instance, DryUiState __state, Exception __exception)
        {
            if (__state.Changed)
            {
                try { __instance._dryMultiplier = __state.OriginalRate; }
                catch (Exception ex) { Warn("drying ETA restore", ex); }
            }
            return __exception;
        }
    }
}
