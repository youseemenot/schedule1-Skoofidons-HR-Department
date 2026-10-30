namespace SkoofidonsHRDepartment
{
    public static class CleanerSpeedPolicy
    {
        public static bool TryScaleDuration(float original, float multiplier, out float duration)
        {
            duration = original;
            if (!float.IsFinite(original) || original < 0f || !PositiveFinite(multiplier)) return false;
            if (multiplier == 1f) return true;
            float candidate = original / multiplier;
            if (!float.IsFinite(candidate) || candidate < 0f || (original > 0f && candidate == 0f)) return false;
            duration = candidate;
            return true;
        }
        public static bool IsEligibleYield(bool running, string nativeOwnerType, bool isWaitForSeconds) =>
            running && isWaitForSeconds && nativeOwnerType == "ScheduleOne.Employees.Cleaner";
        public static bool RequiresPatch(float multiplier) => PositiveFinite(multiplier) && multiplier != 1f;
        private static bool PositiveFinite(float value) => float.IsFinite(value) && value > 0f;
    }
}
