using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Tools;

namespace SkoofidonsHRDepartment
{
    public static class EmployeeSpeedController
    {
        private const string WorkSpeedLabel = "SkoofidonsHRDepartment.EmployeeWorkSpeedMultiplier";
        private static readonly EmployeeMovementSpeedState<NPCSpeedController> Movement = new();
        private static readonly Dictionary<nint, FloatStack> WorkStacks = new();

        public static void Apply(Employee employee, bool isPrefab)
        {
            // Prefab mutations would become the apparent baseline of newly spawned employees.
            if (isPrefab || employee == null) return;
            string typeName;
            try { typeName = employee.GetIl2CppType().Name; }
            catch (Exception) { return; }

            try
            {
                float movement = typeName switch
                {
                    "Packager" => Config.PackagerWalkSpeed.Value,
                    "Chemist" => Config.ChemistWalkSpeed.Value,
                    "Botanist" => Config.BotanistWalkSpeed.Value,
                    "Cleaner" => Config.CleanerWalkSpeed.Value,
                    _ => 1f
                };
                var speed = employee.Movement?.SpeedController;
                if (speed != null)
                    Movement.Apply(speed.Pointer, speed, speed.DefaultSpeed, speed._maxSpeed, movement, WriteMovement);
            }
            catch (Exception) { /* Employee or its native movement binding was destroyed. */ }

            try
            {
                if (typeName == "Packager") ApplyWorkSpeed(employee.WorkSpeedController, Config.PackagerSpeed.Value);
                else if (typeName == "Chemist") ApplyWorkSpeed(employee.WorkSpeedController, Config.ChemistWorkSpeed.Value);
                else if (typeName == "Botanist") ApplyWorkSpeed(employee.WorkSpeedController, Config.BotanistWorkSpeed.Value);
            }
            catch (Exception) { /* Work-stack cleanup remains tracked if a native update failed. */ }
        }

        private static void WriteMovement(NPCSpeedController controller, float speed, float cap)
        {
            // The game clamps effective speed to _maxSpeed (normally 7), so both
            // the base speed and cap must scale for a true 7x movement setting.
            // Both setters store their field before recalculating. Even if a
            // teardown callback throws, attempt the paired default restoration.
            try { controller.SetMaxSpeed(cap); }
            finally { controller.SetDefaultSpeed(speed); }
        }

        private static void ApplyWorkSpeed(FloatStack stack, float multiplier)
        {
            if (stack == null || stack.Pointer == IntPtr.Zero) return;
            nint identity = stack.Pointer;
            bool tracked = WorkStacks.ContainsKey(identity);
            if (!tracked && (!float.IsFinite(multiplier) || multiplier <= 0f || multiplier == 1f)) return;

            // Never divide a previously modified Value to infer a baseline: native
            // buffs may have changed. Removing only our label exposes the live rate.
            stack.Remove(WorkSpeedLabel);
            multiplier = EmployeeSpeedPolicy.ResolveWorkMultiplier(stack.Value, multiplier);
            if (multiplier == 1f)
            {
                WorkStacks.Remove(identity);
                return;
            }

            WorkStacks[identity] = stack;
            // Audited FloatStack comparer sorts by first.Order.CompareTo(second.Order).
            // A final multiplicative entry scales the native additive/override result.
            stack.Add(new FloatStack.StackEntry(WorkSpeedLabel, multiplier, FloatStack.EStackMode.Multiplicative, int.MaxValue));
        }

        public static void RestoreAndClear()
        {
            Movement.RestoreAndClear(WriteMovement);
            var stacks = new List<FloatStack>(WorkStacks.Values);
            WorkStacks.Clear();
            foreach (var stack in stacks)
            {
                try { stack.Remove(WorkSpeedLabel); }
                catch (Exception) { /* Scene teardown may already have destroyed the native stack. */ }
            }
        }
    }
}
