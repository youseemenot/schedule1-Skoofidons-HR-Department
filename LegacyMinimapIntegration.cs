using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using Skoofidons.SharedSettings;
using Skoofidons.SharedSettings.V1;

namespace SkoofidonsHRDepartment
{
    public static class LegacyMinimapIntegration
    {
        private static readonly MelonLogger.Instance Logger=new("Skoofidon Settings");
        private static LegacyMinimapPatches patches;
        private static LegacyMinimapBinding activeBinding;
        private static bool registered;
        public static void Initialize(IEnumerable<MelonBase> melons)
        {
            var matches=melons.Where(m=>m.Info?.Name=="Skoofidon's Minimap").ToArray();
            if(registered&&patches!=null&&matches.Length==1&&ReferenceEquals(matches[0],activeBinding?.Instance))return;
            Shutdown();
            if(matches.Length==0)return;
            if(matches.Length!=1){Logger.Warning("Legacy Minimap adapter inactive: multiple Minimap entries.");return;}
            if(matches[0].GetType().Assembly.GetType(ProviderDiscovery.EndpointName)!=null)return;
            if(!LegacyMinimapBinding.TryBind(matches[0],out var binding,out var reason))
            {Logger.Warning("Legacy Minimap adapter inactive: "+reason);return;}
            if(!binding.IsReady){Logger.Warning("Legacy Minimap adapter inactive: preferences not initialized.");return;}
            try
            {
                patches=LegacyMinimapPatches.Install(binding);
                var panel=new LegacyMinimapPanel(binding);
                var manifest=new Dictionary<string,object>{
                    ["contract"]=ProviderManifest.Contract,["id"]="skoofidon.minimap",["title"]="Minimap",
                    ["providerVersion"]="2.2.8",["hostVersion"]=EntryPointV1.RuntimeVersion,["minimumHostVersion"]="1.3.0",
                    ["draw"]=(Action)panel.Draw,["opened"]=(Action)panel.Opened,["closed"]=(Action)panel.Closed,
                    ["viewport"]=(Action<float,float,string>)MenuTheme.Viewport,["status"]=(Func<string>)panel.Status,
                    ["hasModal"]=(Func<bool>)panel.HasModal,["drawModal"]=(Action<float,float>)panel.DrawModal,["cancelModal"]=(Action)panel.CancelModal
                };
                EntryPointV1.SetAdditionalProviders(new[]{manifest});registered=true;activeBinding=binding;
                Logger.Msg("Legacy Minimap "+binding.Version+" connected to shared F3; original DLL unchanged, F4 and map updates retained.");
            }
            catch(Exception e)
            {
                Shutdown();Logger.Warning("Legacy Minimap adapter inactive; original controls restored: "+e.Message);
            }
        }
        public static void Shutdown()
        {
            try{if(registered)EntryPointV1.SetAdditionalProviders(Array.Empty<IDictionary<string,object>>());}
            finally
            {
                registered=false;
                try{patches?.Dispose();}catch(Exception e){Logger.Warning("Legacy Minimap unpatch: "+e.Message);}
                patches=null;activeBinding=null;
            }
        }
    }
}
