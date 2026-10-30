using System.Linq;
using MelonLoader;
using ModManagerPhoneApp; // Требуется референс в проекте, но можно сделать опциональным через Reflection

namespace SkoofidonsHRDepartment
{
    public static class ModManagerIntegration
    {
        private static bool _modManagerFound;

        public static void Initialize()
        {
            _modManagerFound = MelonBase.RegisteredMelons.Any(m => m?.Info?.Name == "Mod Manager & Phone App");

            if (_modManagerFound)
            {
                PreferenceCategoryOrderIntegration.Initialize();
                ModSettingsEvents.OnPhonePreferencesSaved += OnSettingsChanged;
                ModSettingsEvents.OnMenuPreferencesSaved += OnSettingsChanged;
                MelonLogger.Msg("Mod Manager found! Phone integration active.");
            }
        }

        public static void Deinitialize()
        {
            PreferenceCategoryOrderIntegration.Deinitialize();
            if (_modManagerFound)
            {
                ModSettingsEvents.OnPhonePreferencesSaved -= OnSettingsChanged;
                ModSettingsEvents.OnMenuPreferencesSaved -= OnSettingsChanged;
            }
        }

        private static void OnSettingsChanged()
        {
            CleanerActionSpeedController.RefreshRegistration();
            // Настройки в MelonPreferences уже обновились автоматически.
            // Нам нужно лишь раскидать их по работникам и перерисовать UI.
            EmployeeController.ApplyToPrefabs();
            EmployeeController.ApplyToSpawnedEmployees();
            PackagerUIFix.UpdateRouteCount();
        }
    }
}
