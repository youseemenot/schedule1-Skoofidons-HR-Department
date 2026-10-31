#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Skoofidons.SharedSettings.V1;

namespace Skoofidons.SharedSettings
{
    public static class EntryPointV1
    {
        public const string RuntimeVersion = "1.0.0";
        private static IDictionary<string, object>? manifest;
        public static void Initialize(string id, string title, Version providerVersion, Action draw, Action opened, Action closed)
        {
            var values = new Dictionary<string, object> {
                ["contract"] = ProviderManifest.Contract, ["id"] = id, ["title"] = title,
                ["providerVersion"] = providerVersion.ToString(), ["hostVersion"] = RuntimeVersion, ["minimumHostVersion"] = "1.0.0",
                ["draw"] = draw, ["opened"] = opened, ["closed"] = closed
            };
            ProviderManifest.Parse(values, typeof(EntryPointV1).Assembly);
            if (manifest != null) throw new InvalidOperationException("Settings provider is already initialized.");
            manifest = new ReadOnlyDictionary<string, object>(values);
        }

        public static IDictionary<string, object> GetManifestV1() =>
            manifest ?? throw new InvalidOperationException("Settings provider has not initialized yet.");
    }
}
