#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skoofidons.SharedSettings.V1
{
    public static class HostElection
    {
        public static ProviderManifest? Elect(IEnumerable<ProviderManifest> providers) =>
            providers.OrderByDescending(p => p.HostVersion)
                .ThenBy(p => p.Id, StringComparer.Ordinal)
                .ThenBy(p => p.Assembly.FullName, StringComparer.Ordinal)
                .ThenBy(p => p.Assembly.ManifestModule.ModuleVersionId)
                .FirstOrDefault();
    }
}
