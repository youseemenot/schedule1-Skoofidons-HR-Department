using System;
using System.Collections.Generic;

namespace SkoofidonsHRDepartment
{
    /// <summary>Multiplier validation independent of Unity and native bindings.</summary>
    public static class EmployeeSpeedPolicy
    {
        public static bool TryScaleMovement(float baseline, float baselineCap, float multiplier, out float speed, out float cap)
        {
            speed = baseline;
            cap = baselineCap;
            if (!PositiveFinite(baseline) || !PositiveFinite(baselineCap) || !PositiveFinite(multiplier)) return false;
            float scaledSpeed = baseline * multiplier;
            float scaledCap = baselineCap * multiplier;
            if (!PositiveFinite(scaledSpeed) || !PositiveFinite(scaledCap)) return false;
            speed = scaledSpeed;
            cap = scaledCap;
            return true;
        }

        // A neutral result means remove our entry, preserving every native entry.
        public static float ResolveWorkMultiplier(float nativeRate, float multiplier) =>
            PositiveFinite(nativeRate) && PositiveFinite(multiplier) && PositiveFinite(nativeRate * multiplier)
                ? multiplier : 1f;

        private static bool PositiveFinite(float value) => float.IsFinite(value) && value > 0f;
    }

    /// <summary>Owns original movement values only while our multiplier affects a native controller.</summary>
    public sealed class EmployeeMovementSpeedState<T> where T : class
    {
        private sealed record Baseline(T Controller, float Speed, float Cap);
        private readonly Dictionary<nint, Baseline> baselines = new();

        public void Apply(nint identity, T controller, float currentDefault, float currentMax, float multiplier,
            Action<T, float, float> write)
        {
            if (identity == 0 || controller == null) return;
            bool tracked = baselines.TryGetValue(identity, out var baseline);
            float originalSpeed = tracked ? baseline.Speed : currentDefault;
            float originalCap = tracked ? baseline.Cap : currentMax;
            bool valid = EmployeeSpeedPolicy.TryScaleMovement(originalSpeed, originalCap, multiplier, out float speed, out float cap);
            if (!valid || multiplier == 1f)
            {
                if (tracked)
                {
                    write(controller, originalSpeed, originalCap);
                    baselines.Remove(identity);
                }
                return;
            }

            // Save before native writes: a partially failed setter still needs cleanup.
            // A fresh managed wrapper for the same pointer must never recapture modified values.
            baselines[identity] = new Baseline(controller, originalSpeed, originalCap);
            write(controller, speed, cap);
        }

        public void RestoreAndClear(Action<T, float, float> restore)
        {
            var snapshot = new List<Baseline>(baselines.Values);
            baselines.Clear();
            foreach (var baseline in snapshot)
            {
                // Destroyed IL2CPP bindings must not stop restoration of other employees.
                try { restore(baseline.Controller, baseline.Speed, baseline.Cap); }
                catch (Exception) { }
            }
        }
    }
}
