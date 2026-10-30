using System;
using System.Text.Json;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Persistence.Datas;

namespace SkoofidonsHRDepartment
{
    public static partial class MachineTimerController
    {
        public readonly struct GrowthScope
        {
            internal readonly MachineGrowthContext.Scope Context;
            internal GrowthScope(MachineGrowthContext.Scope context) => Context = context;
        }

        private static void ClearGrowthContexts() => MachineGrowthContext.Clear();

        public static void PotSeedPrefix(Pot __instance, out nint __state)
        {
            __state = -1;
            if (!_active || __instance == null) return;
            try { __state = __instance.Plant?.Pointer ?? IntPtr.Zero; }
            catch (Exception exception) { Warn("plant creation prefix", exception); }
        }

        public static void PotSeedPostfix(Pot __instance, nint __state)
        {
            if (!_active || __instance == null || __state == -1) return;
            try
            {
                Plant plant = __instance.Plant;
                if (plant == null || plant.Pointer == __state) return;
                MachineTimerSnapshot snapshot = PlantSnapshot(__instance);
                if (snapshot != null)
                {
                    Ledger.Forget(snapshot.Key);
                    Capture(snapshot, ConfiguredRate(MachineTimerKind.PlantGrowth), asyncRestore: true);
                }
            }
            catch (Exception exception) { Warn("plant creation capture", exception); }
        }

        public static void PotLoadPlantPrefix(Pot __instance, PlantData __0)
        {
            if (!_active || __instance == null || __0 == null) return;
            try
            {
                // LoadPlant schedules seed creation after the loader returns. The
                // marker therefore lives in persistence, beyond the load scope.
                if (!string.IsNullOrEmpty(__0.SeedID)) State.MarkAsyncRestore(MachineTimerKind.PlantGrowth, GuidOf(__instance));
            }
            catch (Exception exception) { Warn("plant asynchronous load", exception); }
        }

        public static void ColonyCreatePrefix(MushroomBed __instance, out nint __state)
        {
            __state = -1;
            if (!_active || __instance == null) return;
            try { __state = __instance.CurrentColony?.Pointer ?? IntPtr.Zero; }
            catch (Exception exception) { Warn("colony creation prefix", exception); }
        }

        public static void ColonyCreatePostfix(MushroomBed __instance, nint __state)
        {
            if (!_active || __instance == null || __state == -1) return;
            try
            {
                // Bed.Load applies saved progress only after colony creation.
                // Defer restoration until its postfix sees the final snapshot.
                if (State.IsLoading(MachineTimerKind.ShroomGrowth)) return;
                ShroomColony colony = __instance.CurrentColony;
                if (colony == null || colony.Pointer == __state) return;
                MachineTimerSnapshot snapshot = ColonySnapshot(__instance);
                if (snapshot != null)
                {
                    Ledger.Forget(snapshot.Key);
                    Capture(snapshot, ConfiguredRate(MachineTimerKind.ShroomGrowth));
                }
            }
            catch (Exception exception) { Warn("colony creation capture", exception); }
        }

        public static void BedLoadPrefix(MushroomBed __instance, out MachineTimerPersistence.LoadScope __state)
        {
            __state = null;
            if (!_active || __instance == null) return;
            try
            {
                // The enclosing DynamicSaveData loader owns the staged payload;
                // this nested scope preserves it while identifying the live bed.
                string baseJson = JsonSerializer.Serialize(new { GUID = GuidOf(__instance) });
                __state = State.BeginLoad(MachineTimerKind.ShroomGrowth, baseJson, null);
            }
            catch (Exception exception) { Warn("colony load prefix", exception); }
        }

        public static void BedLoadPostfix(MushroomBed __instance)
        {
            if (!_active || __instance == null) return;
            try
            {
                if (!State.IsLoading(MachineTimerKind.ShroomGrowth)) return;
                MachineTimerSnapshot snapshot = ColonySnapshot(__instance);
                if (snapshot != null) Capture(snapshot, ConfiguredRate(MachineTimerKind.ShroomGrowth));
            }
            catch (Exception exception) { Warn("colony load capture", exception); }
        }

        public static void PlantGrowPrefix(Plant __instance, out GrowthScope __state)
        {
            __state = default;
            if (!_active || __instance == null) return;
            try
            {
                nint pointer = __instance.Pointer;
                __state = new GrowthScope(MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, pointer,
                    GrowthRate(MachineTimerKind.PlantGrowth, pointer)));
            }
            catch (Exception exception) { Warn("plant growth prefix", exception); }
        }

        public static Exception PlantGrowFinalizer(Exception __exception, GrowthScope __state)
        {
            try { MachineGrowthContext.Exit(__state.Context); }
            catch (Exception exception) { Warn("plant growth cleanup", exception); }
            return __exception;
        }

        public static void PlantProgressPrefix(Plant __instance, ref float __0)
        {
            if (!_active || __instance == null) return;
            try
            {
                if (MachineGrowthContext.TryScale(MachineTimerKind.PlantGrowth, __instance.Pointer,
                    __instance.NormalizedGrowthProgress, __0, out float scaled)) __0 = scaled;
            }
            catch (Exception exception) { Warn("plant growth progress", exception); }
        }

        public static void ShroomGrowPrefix(ShroomColony __instance, out GrowthScope __state)
        {
            __state = default;
            if (!_active || __instance == null) return;
            try
            {
                nint pointer = __instance.Pointer;
                __state = new GrowthScope(MachineGrowthContext.Enter(MachineTimerKind.ShroomGrowth, pointer,
                    GrowthRate(MachineTimerKind.ShroomGrowth, pointer)));
            }
            catch (Exception exception) { Warn("colony growth prefix", exception); }
        }

        public static Exception ShroomGrowFinalizer(Exception __exception, GrowthScope __state)
        {
            try { MachineGrowthContext.Exit(__state.Context); }
            catch (Exception exception) { Warn("colony growth cleanup", exception); }
            return __exception;
        }

        public static void ShroomProgressPrefix(ShroomColony __instance, ref float __0)
        {
            if (!_active || __instance == null) return;
            try
            {
                if (MachineGrowthContext.TryScale(MachineTimerKind.ShroomGrowth, __instance.Pointer,
                    __instance.GrowthProgress, __0, out float scaled)) __0 = scaled;
            }
            catch (Exception exception) { Warn("colony growth progress", exception); }
        }
    }
}
