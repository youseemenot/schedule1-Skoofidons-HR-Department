using System;
using System.Collections.Generic;
using System.Linq;
namespace SkoofidonsHRDepartment
{
    public sealed class MinimapTabSetting
    {
        public string Key {get;}public string PreferenceField {get;}public int Tab {get;}
        public MinimapTabSetting(string key,string preferenceField,int tab){Key=key;PreferenceField=preferenceField;Tab=tab;}
    }
    public static class MinimapTabSchema
    {
        public static readonly IReadOnlyList<MinimapTabSetting> Settings=Array.AsReadOnly(new[]{
            new MinimapTabSetting("minimapEnabled","prefEnabled",0),new("timeBarEnabled","prefTime",0),new("useNavMode","prefNavMode",0),
            new("windowSizeMultiplier","prefWindowSize",0),new("mapZoom","prefZoom",0),new("mapOpacity","prefOpacity",0),
            new("showOrdersEnabled","prefOrders",1),new("showDealersEnabled","prefDealers",1),new("showCustomersEnabled","prefCustomers",1),new("showDeadDropsEnabled","prefDeadDrops",1),
            new("showPropertiesEnabled","prefProperties",1),new("showVehiclesEnabled","prefVehicles",1),new("coopRadarEnabled","prefCoop",1),new("policeRadarEnabled","prefPolice",1),
            new("mapOffsetX","prefMapOffsetX",2),new("mapOffsetY","prefMapOffsetY",2),new("timeOffsetX","prefTimeOffsetX",2),new("timeOffsetY","prefTimeOffsetY",2),new("timeScale","prefTimeScale",2),
            new("arrowSize","prefArrowSize",3),new("arrowR","prefArrowR",3),new("arrowG","prefArrowG",3),new("arrowB","prefArrowB",3)
        });
        public static MinimapTabSetting[] ForTab(int tab)
        {
            if(tab<0||tab>3)throw new ArgumentOutOfRangeException(nameof(tab));
            return Settings.Where(s=>s.Tab==tab).ToArray();
        }
    }
}
