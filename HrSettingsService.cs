namespace SkoofidonsHRDepartment
{
    public static class HrSettingsService
    {
        public static bool TryApply(HrSettingsDraft draft, out string error) =>
            draft.TryApply(() => MelonLoader.MelonPreferences.Save(), ApplyFromPreferences, out error);
        public static void ApplyFromPreferences()
        {
            CleanerActionSpeedController.RefreshRegistration();
            EmployeeController.ApplyToPrefabs();
            EmployeeController.ApplyToSpawnedEmployees();
            PackagerUIFix.UpdateRouteCount();
        }
    }
}
