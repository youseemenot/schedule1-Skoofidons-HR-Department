using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using MelonLoader;
using Skoofidons.SharedSettings.V1;

namespace SkoofidonsHRDepartment
{
    public sealed class LegacyMinimapBinding
    {
        private const BindingFlags InstanceFields=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly Dictionary<string,string> Profiles=new(StringComparer.OrdinalIgnoreCase) {
            ["430530DDDA834C580A5C57DD4781BF9540E12E15F5F0C175C4E37A1E6217E450"]="2.2.8",
            ["31AAAD05C1EA75ADB7E785CEED1CECFE21AF9C2CF18EF8BD1242AB2FF2E75993"]="2.2.8-dev.3"
        };
        private static readonly string[] BoolKeys={"minimapEnabled","timeBarEnabled","showOrdersEnabled","showDealersEnabled","showCustomersEnabled","showDeadDropsEnabled","showPropertiesEnabled","showVehiclesEnabled","coopRadarEnabled","policeRadarEnabled","useNavMode"};
        private static readonly string[] FloatKeys={"windowSizeMultiplier","mapZoom","mapOpacity","mapOffsetX","mapOffsetY","timeOffsetX","timeOffsetY","timeScale","arrowSize","arrowR","arrowG","arrowB"};
        private readonly Dictionary<string,FieldInfo> fields=new();
        private readonly FieldInfo category,pending,due,visible;
        private readonly MethodInfo refresh,save;
        public object Instance {get;}
        public string Version {get;}
        public IEnumerable<string> Keys=>fields.Keys;
        public MethodInfo UpdateMethod {get;}
        public MethodInfo GuiMethod {get;}
        public MethodInfo FlushMethod {get;}
        public string SaveError {get;private set;}
        public bool IsReady=>category.GetValue(null) is MelonPreferences_Category;
        private LegacyMinimapBinding(object instance,string version)
        {
            Instance=instance;Version=version;var type=instance.GetType();
            FieldInfo Field(string name,Type expected,BindingFlags flags=InstanceFields)
            {
                var f=type.GetField(name,flags);
                if(f==null||f.FieldType!=expected)throw new InvalidOperationException("Unsupported Minimap field: "+name);
                return f;
            }
            foreach(var key in BoolKeys)fields.Add(key,Field(key,typeof(bool)));
            foreach(var key in FloatKeys)fields.Add(key,Field(key,typeof(float)));
            category=Field("prefCategory",typeof(MelonPreferences_Category),BindingFlags.Public|BindingFlags.Static);
            pending=Field("settingsSavePending",typeof(bool));due=Field("settingsSaveDueAt",typeof(float));visible=Field("guiVisible",typeof(bool));
            MethodInfo Method(string name,bool isPublic=false)
            {
                var m=type.GetMethod(name,BindingFlags.Instance|(isPublic?BindingFlags.Public:BindingFlags.NonPublic),null,Type.EmptyTypes,null);
                if(m==null||m.ReturnType!=typeof(void)||m.DeclaringType!=type)throw new InvalidOperationException("Unsupported Minimap method: "+name);
                return m;
            }
            UpdateMethod=Method("OnUpdate",true);GuiMethod=Method("OnGUI",true);
            refresh=Method("HandleSettingsUpdate");save=Method("SaveSettingsFromF3");FlushMethod=Method("FlushPendingSettings");
        }
        public static bool TryBind(object instance,out LegacyMinimapBinding binding,out string reason)
        {
            binding=null;reason=null;
            try
            {
                if(instance?.GetType().FullName!="MinimapMod.MainMod") {reason="Not the legacy Minimap entry point.";return false;}
                var assembly=instance.GetType().Assembly;
                if(assembly.GetType(ProviderDiscovery.EndpointName)!=null){reason="Minimap already supports shared F3.";return false;}
                using var stream=File.OpenRead(assembly.Location);
                using var sha=SHA256.Create();
                string hash=Convert.ToHexString(sha.ComputeHash(stream));
                if(!Profiles.TryGetValue(hash,out string version)){reason="Unsupported legacy Minimap DLL SHA256="+hash;return false;}
                binding=new LegacyMinimapBinding(instance,version);return true;
            }
            catch(Exception e){reason=e.Message;return false;}
        }
        public T Get<T>(string key)=>(T)fields[key].GetValue(Instance);
        public void SetCached<T>(string key,T value)
        {
            if(fields[key].FieldType!=typeof(T))throw new ArgumentException("Wrong Minimap setting type.");
            fields[key].SetValue(Instance,value);
        }
        public void Set<T>(string key,T value){SetCached(key,value);Invoke(save);}
        public void Refresh()=>Invoke(refresh);
        public void HideOldWindow()=>visible.SetValue(Instance,false);
        private void Invoke(MethodInfo method)
        {
            try{method.Invoke(Instance,null);}
            catch(TargetInvocationException e)when(e.InnerException!=null){ExceptionDispatchInfo.Capture(e.InnerException).Throw();}
        }
        public void Flush(float now,Action<string> warning=null)
        {
            if(!(bool)pending.GetValue(Instance))return;
            try
            {
                var preferences=(MelonPreferences_Category)category.GetValue(null);
                if(preferences==null)throw new IOException("Minimap preferences are not initialized.");
                PreferencePersistence.Save(()=>preferences.SaveToFile(),preferences);
                pending.SetValue(Instance,false);SaveError=null;
            }
            catch(Exception e)
            {
                due.SetValue(Instance,now+5f);
                if(SaveError!=e.Message)warning?.Invoke("Legacy Minimap preferences: "+e.Message);
                SaveError=e.Message;
            }
        }
    }
}
