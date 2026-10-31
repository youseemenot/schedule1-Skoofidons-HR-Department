namespace SkoofidonsHRDepartment
{
    public static class HrSettingsService
    {
        public static void ApplyFromPreferences()
        {
            CleanerActionSpeedController.RefreshRegistration();
            EmployeeController.ApplyToPrefabs();
            EmployeeController.ApplyToSpawnedEmployees();
            PackagerUIFix.UpdateRouteCount();
        }
    }
}
