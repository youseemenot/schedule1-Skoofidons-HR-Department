using System;

namespace SkoofidonsHRDepartment
{
    public enum TimingAction { Unknown, Soil, Water, Additive, Seed, Harvest }

    /// <summary>Pure timing rules; inputs and results are seconds before work-speed scaling.</summary>
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

        public static float Resolve(TimingAction action, float originalDuration, float configuredSeconds, bool enabled)
        {
            if (!enabled || !Known(action) || !NonNegativeFinite(originalDuration) || !float.IsFinite(configuredSeconds) || configuredSeconds <= 0f)
                return originalDuration;
            float setting = Math.Max(.1f, configuredSeconds);
            // The audited harvest base is one second per item, so its base duration
            // carries the item count. Other actions use an absolute base duration.
            float desired = action == TimingAction.Harvest ? originalDuration * setting : setting;
            return NonNegativeFinite(desired) && !(originalDuration > 0f && desired == 0f) ? desired : originalDuration;
        }

        public static bool TryResolveWait(TimingAction action, float originalWait, float workSpeed,
            float configuredSeconds, bool enabled, out float resolvedWait)
        {
            resolvedWait = originalWait;
            if (!enabled || !Known(action) || !NonNegativeFinite(originalWait) ||
                !float.IsFinite(workSpeed) || workSpeed <= 0f || !float.IsFinite(configuredSeconds) || configuredSeconds <= 0f) return false;
            float originalBase = originalWait * workSpeed;
            if (!NonNegativeFinite(originalBase) || (originalWait > 0f && originalBase == 0f)) return false;
            float desiredBase = Resolve(action, originalBase, configuredSeconds, true);
            float candidate = desiredBase / workSpeed;
            if (!NonNegativeFinite(candidate) || (desiredBase > 0f && candidate == 0f)) return false;
            resolvedWait = candidate;
            return true;
        }

        private static bool Known(TimingAction action) => action >= TimingAction.Soil && action <= TimingAction.Harvest;
        private static bool NonNegativeFinite(float value) => float.IsFinite(value) && value >= 0f;
    }
}
