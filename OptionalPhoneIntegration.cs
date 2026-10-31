using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;

namespace SkoofidonsHRDepartment
{
    public static class OptionalPhoneIntegration
    {
        private static OptionalSettingsEvents connection;

        public static void Initialize(IEnumerable<MelonBase> melons)
        {
            Shutdown();
            try
            {
                var phone = melons.FirstOrDefault(m => m?.Info?.Name == "Mod Manager & Phone App");
                if (phone == null) return;
                var assembly = phone.GetType().Assembly;
                var events = assembly.GetType("ModManagerPhoneApp.ModSettingsEvents", throwOnError: false);
                connection = OptionalSettingsEvents.TrySubscribe(events, HrSettingsService.ApplyFromPreferences, MelonLogger.Warning);
                PreferenceCategoryOrderIntegration.Initialize(assembly);
                if (connection != null) MelonLogger.Msg("Optional Phone App settings integration active.");
            }
            catch (Exception error)
            {
                Shutdown();
                MelonLogger.Warning("Optional Phone App integration inactive: " + error.Message);
            }
        }

        public static void Shutdown()
        {
            try { connection?.Dispose(); }
            finally { connection = null; PreferenceCategoryOrderIntegration.Deinitialize(); }
        }
    }
}
