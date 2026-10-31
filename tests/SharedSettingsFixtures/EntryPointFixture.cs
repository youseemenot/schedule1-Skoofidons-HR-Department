using System.Reflection;

namespace Skoofidons.SharedSettings
{
    // Deliberately the same full name in several independent fixture assemblies.
    public static class EntryPointV1
    {
        public static int DrawCalls;
        public static IDictionary<string, object> GetManifestV1()
        {
            var metadata = typeof(EntryPointV1).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .ToDictionary(a => a.Key, a => a.Value);
            var id = metadata["ProviderId"]!;
            return new Dictionary<string, object> {
                ["contract"] = id == "broken" ? "Unknown/7" : "Skoofidons.SettingsMenu/1",
                ["id"] = id, ["title"] = id, ["providerVersion"] = metadata["ProviderVersion"]!,
                ["hostVersion"] = metadata["HostVersion"]!, ["minimumHostVersion"] = "1.0.0",
                ["draw"] = (Action)(() => DrawCalls++), ["opened"] = (Action)(() => { }), ["closed"] = (Action)(() => { })
            };
        }
    }
}
