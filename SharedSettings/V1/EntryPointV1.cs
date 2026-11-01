#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Skoofidons.SharedSettings.V1;

namespace Skoofidons.SharedSettings
{
    public static class EntryPointV1
    {
        public const string RuntimeVersion = "1.3.0";
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

        // Optional V1.1 capabilities use only BCL types across independently compiled mods.
        // Configure once, during provider initialization, before discovery.
        public static void ConfigurePanel(Action<float, float, string> viewport, Func<string> status,
            Func<bool>? dirty = null, Func<string?>? apply = null)
        {
            var values = new Dictionary<string, object>(GetManifestV1()) {
                ["minimumHostVersion"] = "1.1.0", ["viewport"] = viewport, ["status"] = status
            };
            if (dirty != null) values["dirty"] = dirty;
            if (apply != null) values["apply"] = apply;
            ProviderManifest.Parse(values, typeof(EntryPointV1).Assembly);
            manifest = new ReadOnlyDictionary<string, object>(values);
        }
        public static void SetAdditionalProviders(IEnumerable<IDictionary<string,object>> providers)
        {
            var values=new Dictionary<string,object>(GetManifestV1());
            var owner=ProviderManifest.Parse(values,typeof(EntryPointV1).Assembly);
            var snapshots=providers.Take(33).Select(p=>(IDictionary<string,object>)new ReadOnlyDictionary<string,object>(new Dictionary<string,object>(p))).ToArray();
            if(snapshots.Length>32)throw new ArgumentException("Too many additional settings providers.");
            var ids=new HashSet<string>{owner.Id};
            foreach(var item in snapshots)
            {
                var child=ProviderManifest.Parse(item,typeof(EntryPointV1).Assembly);
                if(!ids.Add(child.Id)||child.HostVersion!=owner.HostVersion||item.ContainsKey("additionalProviders"))
                    throw new ArgumentException("Invalid additional settings provider.");
            }
            if(snapshots.Length==0)values.Remove("additionalProviders");
            else values["additionalProviders"]=Array.AsReadOnly(snapshots);
            manifest=new ReadOnlyDictionary<string,object>(values);
        }
        public static void ConfigureModal(Func<bool> hasModal,Action<float,float> drawModal,Action cancelModal)
        {
            var values=new Dictionary<string,object>(GetManifestV1()){
                ["minimumHostVersion"]="1.3.0",["hasModal"]=hasModal,["drawModal"]=drawModal,["cancelModal"]=cancelModal
            };
            ProviderManifest.Parse(values,typeof(EntryPointV1).Assembly);
            manifest=new ReadOnlyDictionary<string,object>(values);
        }
    }
}
