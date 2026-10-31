using System;
using System.Linq;
using System.Reflection;

namespace Skoofidons.SharedSettings.V1
{
    public static class GameLanguageSource
    {
        public static string Normalize(string language)
        {
            var value = (language ?? "").Trim().ToLowerInvariant();
            return value == "russian" || value == "ru" || value.StartsWith("ru-") ? "ru" : "en";
        }
        public static string Read()
        {
            try
            {
                const string name = "Il2Cppcom.rlabrecque.steamworks.net";
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name)
                    ?? Assembly.Load(name);
                var method = assembly.GetType("Il2CppSteamworks.SteamApps")?.GetMethod("GetCurrentGameLanguage", BindingFlags.Public | BindingFlags.Static);
                return Normalize(method?.Invoke(null, null) as string);
            }
            catch { return "en"; }
        }
    }
}
