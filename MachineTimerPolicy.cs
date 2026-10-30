using System;

namespace SkoofidonsHRDepartment
{
    public enum MachineTimerKind { Chemistry, Oven, Mixing, Cauldron, Drying, PlantGrowth, ShroomGrowth }

    public static class MachineTimerPolicy
    {
        public static double NormalizeRate(double rate) => PositiveFinite(rate) ? rate : 1;

        // False always leaves the caller's native elapsed value available unchanged.
        public static bool TryCredit(int elapsed, double rate, double fraction, int remaining, out int credit, out double nextFraction)
        {
            credit = elapsed;
            nextFraction = fraction;
            if (elapsed <= 0 || remaining < 0 || !PositiveFinite(rate) || !ValidFraction(fraction)) return false;
            double total = elapsed * rate + fraction;
            if (!double.IsFinite(total)) return false;
            // Compare before converting: a finite product can exceed the entire integer range.
            if (total >= remaining)
            {
                credit = remaining;
                nextFraction = 0;
                return true;
            }
            double whole = Math.Floor(total);
            credit = (int)whole;
            nextFraction = total - whole;
            return true;
        }

        public static bool TryScaleGrowth(float previous, float requested, double rate, out float scaled)
        {
            scaled = requested;
            if (!float.IsFinite(previous) || !float.IsFinite(requested) || !PositiveFinite(rate) || requested <= previous) return false;
            double candidate = previous + ((double)requested - previous) * rate;
            if (!double.IsFinite(candidate)) return false;
            scaled = (float)Math.Clamp(candidate, 0, 1);
            return true;
        }

        // Native code still adds elapsed * warmth after this adjustment. The temporary
        // value may be negative for slowdown; clamping it would grant extra progress.
        public static bool TryDryingAdjustment(float previous, int elapsed, float warmth, double rate, out float adjusted)
        {
            adjusted = previous;
            if (!float.IsFinite(previous) || elapsed <= 0 || !float.IsFinite(warmth) || warmth < 0 || !PositiveFinite(rate)) return false;
            double nativeStep = (double)elapsed * warmth;
            double candidate = previous + nativeStep * (rate - 1);
            float intermediate = (float)candidate;
            // Validate the float addition actually performed by the native callback too.
            float final = intermediate + elapsed * warmth;
            if (!double.IsFinite(candidate) || !float.IsFinite(intermediate) || !float.IsFinite(final)) return false;
            adjusted = intermediate;
            return true;
        }

        public static bool TryEta(double remaining, double fraction, double rate, out int minutes)
        {
            minutes = 0;
            if (!double.IsFinite(remaining) || remaining < 0 || !ValidFraction(fraction) || !PositiveFinite(rate)) return false;
            double estimate = Math.Ceiling(Math.Max(0, remaining - fraction) / rate);
            minutes = estimate >= int.MaxValue ? int.MaxValue : (int)estimate;
            return true;
        }

        private static bool PositiveFinite(double value) => double.IsFinite(value) && value > 0;
        internal static bool ValidFraction(double value) => double.IsFinite(value) && value >= 0 && value < 1;
    }
}
