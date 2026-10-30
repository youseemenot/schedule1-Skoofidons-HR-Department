using System;

namespace SkoofidonsHRDepartment
{
    public enum TimingAction { Unknown, Soil, Water, Additive, Seed, Harvest }

    /// <summary>Pure timing rules; positive multipliers divide each game's original duration.</summary>
    public static class BotanistTimingPolicy
    {
        public static TimingAction Classify(string nativeType) => nativeType switch
        {
            "ScheduleOne.NPCs.Behaviour.AddSoilToGrowContainerBehaviour" => TimingAction.Soil,
            "ScheduleOne.NPCs.Behaviour.WaterPotBehaviour" => TimingAction.Water,
            "ScheduleOne.NPCs.Behaviour.ApplyAdditiveToGrowContainerBehaviour" => TimingAction.Additive,
            "ScheduleOne.NPCs.Behaviour.SowSeedInPotBehaviour" => TimingAction.Seed,
            "ScheduleOne.NPCs.Behaviour.HarvestPotBehaviour" => TimingAction.Harvest,
            _ => TimingAction.Unknown
        };

        public static bool IsFirstWait(int before, int after, bool running) => before == 0 && after == 1 && running;

        public static float Resolve(TimingAction action, float originalDuration, float configuredMultiplier, bool enabled)
        {
            if (!enabled || !Known(action) || !NonNegativeFinite(originalDuration) || !PositiveFinite(configuredMultiplier))
                return originalDuration;
            return TryScale(originalDuration, configuredMultiplier, out float desired) ? desired : originalDuration;
        }

        public static bool TryResolveWait(TimingAction action, float originalWait, float workSpeed,
            float configuredMultiplier, bool enabled, out float resolvedWait)
        {
            resolvedWait = originalWait;
            if (!enabled || !Known(action) || !NonNegativeFinite(originalWait) ||
                !PositiveFinite(workSpeed) || !PositiveFinite(configuredMultiplier)) return false;
            // The native coroutine already divided its duration by CurrentWorkSpeed.
            // Preserve that result, including harvest's item count, and apply only our multiplier.
            if (!TryScale(originalWait, configuredMultiplier, out float candidate)) return false;
            resolvedWait = candidate;
            return true;
        }

        private static bool TryScale(float original, float multiplier, out float scaled)
        {
            scaled = original;
            if (multiplier == 1f) return true;
            float candidate = original / multiplier;
            if (!NonNegativeFinite(candidate) || (original > 0f && candidate == 0f)) return false;
            scaled = candidate;
            return true;
        }

        private static bool Known(TimingAction action) => action >= TimingAction.Soil && action <= TimingAction.Harvest;
        private static bool PositiveFinite(float value) => float.IsFinite(value) && value > 0f;
        private static bool NonNegativeFinite(float value) => float.IsFinite(value) && value >= 0f;
    }
}
