using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ObjectScripts;
using MelonLoader;

namespace SkoofidonsHRDepartment
{
    public static partial class MachineTimerController
    {
        private static readonly MachineTimerLedger Ledger = new();
        private static readonly MachineTimerPersistence State = new(Ledger);
        private static readonly MachineDryingCaptureQueue DryingCaptures = new();
        private static readonly Dictionary<nint, long> CauldronEpochs = new();
        private static readonly HashSet<string> Warnings = new();
        private static readonly Dictionary<MachineTimerKind, int> CaptureSamples = new();
        private static bool _active;

        private static double ConfiguredRate(MachineTimerKind kind) => MachineTimerPolicy.NormalizeRate(kind switch
        {
            MachineTimerKind.Chemistry => Config.MachineChemistryRate?.Value ?? 1f,
            MachineTimerKind.Oven => Config.MachineOvenRate?.Value ?? 1f,
            MachineTimerKind.Mixing => Config.MachineMixingRate?.Value ?? 1f,
            MachineTimerKind.Cauldron => Config.MachineCauldronRate?.Value ?? 1f,
            MachineTimerKind.Drying => Config.MachineDryingRate?.Value ?? 1f,
            MachineTimerKind.PlantGrowth => Config.MachinePlantRate?.Value ?? 1f,
            MachineTimerKind.ShroomGrowth => Config.MachineShroomRate?.Value ?? 1f,
            _ => 1f
        });

        private static string PointerId(nint pointer) => pointer.ToString("X");
        private static string Key(MachineTimerKind kind, nint pointer) => kind + ":" + PointerId(pointer);
        private static string GuidOf(BuildableItem owner) => owner.GUID.ToString();
        private static string Fingerprint(params object[] values) => JsonSerializer.Serialize(values);
        private static MachineTimerRecord Capture(MachineTimerSnapshot snapshot, double rate, bool asyncRestore = false)
        {
            var record = State.Capture(snapshot, rate, asyncRestore);
            CaptureSamples.TryGetValue(snapshot.Kind, out int count);
            if (count < 8)
            {
                CaptureSamples[snapshot.Kind] = count + 1;
                MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                    "[HR MACHINE CAPTURE] kind={0} rate={1:0.###}; new/restored operation; sample={2}/8",
                    snapshot.Kind, record.CapturedRate, count + 1));
            }
            return record;
        }

        private static void Warn(string scope, Exception exception)
        {
            if (Warnings.Add(scope)) MelonLogger.Warning("[HR MACHINE TIMER] " + scope + ": " + exception.Message);
        }

        private static MachineTimerSnapshot ChemistrySnapshot(ChemistryStation owner, ChemistryCookOperation operation = null)
        {
            operation ??= owner.CurrentCookOperation;
            if (operation == null || operation.Recipe == null) return null;
            int total = operation.Recipe.CookTime_Mins;
            int remaining = (int)Math.Clamp((long)total - operation.CurrentTime, 0, int.MaxValue);
            return new(MachineTimerKind.Chemistry, Key(MachineTimerKind.Chemistry, owner.Pointer), PointerId(operation.Pointer),
                GuidOf(owner), Fingerprint(operation.RecipeID, (int)operation.ProductQuality), operation.CurrentTime, remaining);
        }

        private static MachineTimerSnapshot OvenSnapshot(LabOven owner, OvenCookOperation operation = null)
        {
            operation ??= owner.CurrentOperation;
            if (operation == null) return null;
            int remaining = (int)Math.Clamp((long)operation.GetCookDuration() - operation.CookProgress, 0, int.MaxValue);
            return new(MachineTimerKind.Oven, Key(MachineTimerKind.Oven, owner.Pointer), PointerId(operation.Pointer), GuidOf(owner),
                Fingerprint(operation.IngredientID, operation.ProductID, operation.IngredientQuantity, (int)operation.IngredientQuality), operation.CookProgress, remaining);
        }

        private static MachineTimerSnapshot MixingSnapshot(MixingStation owner, MixOperation operation = null, int? progress = null)
        {
            operation ??= owner.CurrentMixOperation;
            if (operation == null) return null;
            int elapsed = progress ?? owner.CurrentMixTime;
            long total = (long)operation.Quantity * owner.MixTimePerItem;
            if (total < 0 || total > int.MaxValue) return null;
            return new(MachineTimerKind.Mixing, Key(MachineTimerKind.Mixing, owner.Pointer), PointerId(operation.Pointer), GuidOf(owner),
                Fingerprint(operation.ProductID, operation.IngredientID, operation.Quantity, (int)operation.ProductQuality), elapsed,
                (int)Math.Clamp(total - elapsed, 0, int.MaxValue));
        }

        private static MachineTimerSnapshot CauldronSnapshot(Cauldron owner, int? remaining = null, int? quality = null)
        {
            int left = remaining ?? owner.RemainingCookTime;
            if (left <= 0) return null;
            CauldronEpochs.TryGetValue(owner.Pointer, out long epoch);
            return new(MachineTimerKind.Cauldron, Key(MachineTimerKind.Cauldron, owner.Pointer), epoch.ToString(CultureInfo.InvariantCulture), GuidOf(owner),
                Fingerprint(owner.CookTime, quality ?? (int)owner.InputQuality), left, left);
        }

        private static MachineTimerSnapshot DryingSnapshot(DryingRack owner, DryingOperation operation, int index)
        {
            if (operation == null) return null;
            return new(MachineTimerKind.Drying, Key(MachineTimerKind.Drying, operation.Pointer), PointerId(operation.Pointer), GuidOf(owner),
                Fingerprint(operation.ItemID, operation.Quantity, (int)operation.StartQuality), operation.Time, int.MaxValue, index);
        }

        private static MachineTimerSnapshot PlantSnapshot(Pot owner)
        {
            Plant plant = owner.Plant;
            if (plant == null) return null;
            return new(MachineTimerKind.PlantGrowth, Key(MachineTimerKind.PlantGrowth, plant.Pointer), PointerId(plant.Pointer), GuidOf(owner),
                Fingerprint(plant.SeedDefinition?.ID), plant.NormalizedGrowthProgress, int.MaxValue);
        }

        private static MachineTimerSnapshot ColonySnapshot(MushroomBed owner)
        {
            ShroomColony colony = owner.CurrentColony;
            if (colony == null) return null;
            return new(MachineTimerKind.ShroomGrowth, Key(MachineTimerKind.ShroomGrowth, colony.Pointer), PointerId(colony.Pointer), GuidOf(owner),
                Fingerprint(colony._spawnDefinition?.ID), colony.GrowthProgress, int.MaxValue);
        }

        private static double GrowthRate(MachineTimerKind kind, nint pointer) =>
            Ledger.TryGet(Key(kind, pointer), out var record) && record.Identity == PointerId(pointer) ? record.CapturedRate : 1;

        public static void ClearState()
        {
            State.Clear(); Ledger.Clear(); DryingCaptures.Clear(); CauldronEpochs.Clear(); CaptureSamples.Clear(); Warnings.Clear();
            ClearGrowthContexts();
        }
    }
}
