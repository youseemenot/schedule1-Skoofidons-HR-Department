#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Skoofidons.SharedSettings.V1
{
    public static class ProviderDiscovery
    {
        public const string EndpointName = "Skoofidons.SharedSettings.EntryPointV1";

        public static IReadOnlyList<ProviderManifest> Discover(IEnumerable<Assembly> assemblies, Action<string>? warning = null)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
            var providers = new List<ProviderManifest>();
            foreach (Assembly assembly in assemblies.Distinct())
            {
                try
                {
                    var endpoint = assembly.GetType(EndpointName, throwOnError: false);
                    if (endpoint == null) continue;
                    var method = endpoint.GetMethod("GetManifestV1", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                        binder: null, types: Type.EmptyTypes, modifiers: null);
                    if (method == null || method.ReturnType != typeof(IDictionary<string, object>))
                        throw new FormatException("Invalid V1 manifest method.");
                    var values = method.Invoke(null, null) as IDictionary<string, object>;
                    if (values == null) throw new FormatException("Settings manifest returned null.");
                    var owner=ProviderManifest.Parse(values, assembly);
                    providers.Add(owner);
                    if(values.TryGetValue("additionalProviders",out var extras))
                    {
                        try
                        {
                            if(extras is not IEnumerable<IDictionary<string,object>> items)throw new FormatException("Invalid additional provider collection.");
                            var children=items.Take(33).ToArray();
                            if(children.Length>32)throw new FormatException("Too many additional settings providers.");
                            foreach(var child in children)
                                try
                                {
                                    var parsed=ProviderManifest.Parse(child,assembly);
                                    if(parsed.HostVersion!=owner.HostVersion||child.ContainsKey("additionalProviders"))throw new FormatException("Invalid supplying host for additional panel.");
                                    providers.Add(parsed);
                                }
                                catch(Exception e){warning?.Invoke("Additional settings provider skipped in "+assembly.GetName().Name+": "+e.Message);}
                        }
                        catch(Exception e){warning?.Invoke("Additional settings providers skipped in "+assembly.GetName().Name+": "+e.Message);}
                    }
                }
                catch (Exception error)
                {
                    warning?.Invoke("Settings provider skipped in " + assembly.GetName().Name + ": " +
                        (error.InnerException ?? error).Message);
                }
            }
            var selected = new List<ProviderManifest>();
            foreach (var group in providers.GroupBy(p => p.Id, StringComparer.Ordinal))
            {
                var ordered = group.OrderByDescending(p => p.ProviderVersion)
                    .ThenBy(p => p.Assembly.FullName, StringComparer.Ordinal)
                    .ThenBy(p => p.Assembly.ManifestModule.ModuleVersionId).ToArray();
                selected.Add(ordered[0]);
                if (ordered.Length > 1) warning?.Invoke("Duplicate settings provider ID: " + group.Key + "; using " + ordered[0].Assembly.GetName().Name);
            }
            return Array.AsReadOnly(selected.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Id, StringComparer.Ordinal).ToArray());
        }
    }
}
