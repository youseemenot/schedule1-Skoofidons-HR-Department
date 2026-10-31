#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skoofidons.SharedSettings.V1
{
    public sealed class ProviderManifest
    {
        public const string Contract = "Skoofidons.SettingsMenu/1";
        public string Id { get; }
        public string Title { get; }
        public Version ProviderVersion { get; }
        public Version HostVersion { get; }
        public Version MinimumHostVersion { get; }
        public Assembly Assembly { get; }
        public Action Draw { get; }
        public Action Opened { get; }
        public Action Closed { get; }

        private ProviderManifest(IDictionary<string, object> values, Assembly assembly)
        {
            if (Read<string>(values, "contract") != Contract) throw new FormatException("Unsupported settings contract.");
            Id = Read<string>(values, "id");
            if (Id.Length == 0 || Id.Length > 128) throw new FormatException("Invalid settings provider ID.");
            foreach (char c in Id)
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '.' && c != '-' && c != '_')
                    throw new FormatException("Invalid settings provider ID.");
            Title = Read<string>(values, "title");
            if (string.IsNullOrWhiteSpace(Title) || Title.Length > 128) throw new FormatException("Invalid settings provider title.");
            ProviderVersion = ReadVersion(values, "providerVersion");
            HostVersion = ReadVersion(values, "hostVersion");
            MinimumHostVersion = ReadVersion(values, "minimumHostVersion");
            if (HostVersion < MinimumHostVersion) throw new FormatException("Provider requires a newer runtime than it supplies.");
            Draw = Read<Action>(values, "draw");
            Opened = Read<Action>(values, "opened");
            Closed = Read<Action>(values, "closed");
            Assembly = assembly;
        }

        public static ProviderManifest Parse(IDictionary<string, object> values, Assembly assembly)
        {
            if (values == null || assembly == null) throw new FormatException("Missing settings manifest or owning assembly.");
            return new ProviderManifest(values, assembly);
        }

        private static T Read<T>(IDictionary<string, object> values, string key)
        {
            if (!values.TryGetValue(key, out object? value) || value is not T typed)
                throw new FormatException("Invalid settings manifest field: " + key);
            return typed;
        }

        private static Version ReadVersion(IDictionary<string, object> values, string key)
        {
            if (!Version.TryParse(Read<string>(values, key), out Version? version))
                throw new FormatException("Invalid settings version: " + key);
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
        }
    }
}
