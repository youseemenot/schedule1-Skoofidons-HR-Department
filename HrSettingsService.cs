namespace SkoofidonsHRDepartment
{
    public static class HrSettingsService
    {
        public static bool TryApply(HrSettingsDraft draft, out string error) =>
            draft.TryApply(() => Skoofidons.SharedSettings.V1.PreferencePersistence.Save(
                () => MelonLoader.MelonPreferences.Save(), Config.BotanistCat, Config.ChemistCat,
                Config.CleanerCat, Config.PackagerCat, Config.FixerCat, Config.MachinesCat), ApplyFromPreferences, out error);
        public static void ApplyFromPreferences()
        {
            CleanerActionSpeedController.RefreshRegistration();
            EmployeeController.ApplyToPrefabs();
            EmployeeController.ApplyToSpawnedEmployees();
            PackagerUIFix.UpdateRouteCount();
        }
    }
}
