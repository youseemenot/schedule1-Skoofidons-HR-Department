using System;
using Il2CppScheduleOne.Growing;

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

        public static void PlantGrowPrefix(Plant __instance, out GrowthScope __state)
        {
            __state = default;
            if (!_active || __instance == null) return;
            try
            {
                nint pointer = __instance.Pointer;
                __state = new GrowthScope(MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, pointer,
                    ConfiguredRate(MachineTimerKind.PlantGrowth)));
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
                    ConfiguredRate(MachineTimerKind.ShroomGrowth)));
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
