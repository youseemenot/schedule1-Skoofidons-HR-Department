using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using Skoofidons.SharedSettings;
using Skoofidons.SharedSettings.V1;
using UnityEngine;
namespace SkoofidonsHRDepartment
{
    public static class HrSettingsPanel
    {
        private static HrSettingsDraft draft;
        private static int tab;
        private static Vector2 scroll;
        private static string result;
        private static string T(string ru,string en)=>MenuTheme.T(ru,en);
        public static void Initialize()
        {
            EntryPointV1.Initialize("skoofidon.hr","HR Department",new Version(1,3,0),Draw,OnOpened,OnClosed);
            EntryPointV1.ConfigurePanel(MenuTheme.Viewport,Status,()=>draft?.Dirty??false,Apply);
        }
        public static IEnumerable<NumericSetting> CreateSettings()
        {
            NumericSetting F(string id,int page,string ru,string en,MelonPreferences_Entry<float> p,double min=0)=>new(id,page,ru,en,()=>p.Value,v=>p.Value=(float)v,false,min);
            NumericSetting I(string id,int page,string ru,string en,MelonPreferences_Entry<int> p)=>new(id,page,ru,en,()=>p.Value,v=>p.Value=(int)v,true,1);
            var list=new List<NumericSetting>();
            void Employee(int page,string prefix,MelonPreferences_Entry<int> stations,MelonPreferences_Entry<float> move,MelonPreferences_Entry<float> work,MelonPreferences_Entry<float> wage,MelonPreferences_Entry<float> fee,string ru,string en)
            {
                list.Add(I(prefix+"Max",page,ru,en,stations));
                list.Add(F(prefix+"Move",page,"Скорость передвижения ×","Movement speed ×",move,double.Epsilon));
                list.Add(F(prefix+"Work",page,"Скорость работы ×","Work speed ×",work,double.Epsilon));
                list.Add(F(prefix+"Wage",page,"Дневная зарплата $","Daily wage $",wage));
                list.Add(F(prefix+"Fee",page,"Стоимость найма $","Signing fee $",fee));
            }
            Employee(0,"Botanist",Config.BotanistMaxStations,Config.BotanistWalkSpeed,Config.BotanistWorkSpeed,Config.BotanistWage,Config.BotanistFee,"Максимум растений","Maximum plants");
            Employee(1,"Chemist",Config.ChemistMaxStations,Config.ChemistWalkSpeed,Config.ChemistWorkSpeed,Config.ChemistWage,Config.ChemistFee,"Максимум станций","Maximum stations");
            Employee(2,"Cleaner",Config.CleanerMaxBins,Config.CleanerWalkSpeed,Config.CleanerWorkSpeed,Config.CleanerWage,Config.CleanerFee,"Максимум урн","Maximum bins");
            Employee(3,"Handler",Config.PackagerMaxStations,Config.PackagerWalkSpeed,Config.PackagerSpeed,Config.PackagerWage,Config.PackagerFee,"Максимум станций","Maximum stations");
            list.Add(I("HandlerRoutes",3,"Максимум маршрутов","Maximum routes",Config.PackagerMaxRoutes));
            list.Add(F("FixerLess5",4,"Доплата: меньше 5 сотрудников $","Extra fee: fewer than 5 employees $",Config.FixerFeeLess5));
            list.Add(F("Fixer5Plus",4,"Доплата: от 5 сотрудников $","Extra fee: 5 or more employees $",Config.FixerFee5Plus));
            list.Add(F("FixerLimit",4,"Максимальная доплата $","Maximum extra fee $",Config.FixerFeeLimit));
            list.Add(F("Chemistry",5,"Химический стол ×","Chemistry station ×",Config.MachineChemistryRate,double.Epsilon));
            list.Add(F("Oven",5,"Печь ×","Oven ×",Config.MachineOvenRate,double.Epsilon));
            list.Add(F("Mixing",5,"Миксер ×","Mixing station ×",Config.MachineMixingRate,double.Epsilon));
            list.Add(F("Cauldron",5,"Котёл ×","Cauldron ×",Config.MachineCauldronRate,double.Epsilon));
            list.Add(F("Drying",5,"Сушилка ×","Drying rack ×",Config.MachineDryingRate,double.Epsilon));
            list.Add(F("Plants",5,"Растения ×","Plants ×",Config.MachinePlantRate,double.Epsilon));
            list.Add(F("Mushrooms",5,"Грибы ×","Mushrooms ×",Config.MachineShroomRate,double.Epsilon));return list;
        }
        public static void OnOpened(){draft=new HrSettingsDraft(CreateSettings());draft.Read();result=null;scroll=Vector2.zero;}
        public static void OnClosed(){draft?.Discard();draft=null;result=null;}
        private static string Status()=>result??(draft?.Dirty==true?T("Есть изменения. Закрытие отменит неприменённые правки.","Unsaved changes. Closing discards unapplied edits."):T("Настройки сохранены","Settings saved"));
        private static string Apply()
        {
            if(draft==null)return null;
            if(!draft.TryValidate(out var invalid)){var s=draft.Settings.First(x=>x.Id==invalid);tab=s.Tab;return T("Проверьте значение: ","Check value: ")+(MenuTheme.Language=="ru"?s.Ru:s.En);}
            if(!HrSettingsService.TryApply(draft,out var error))return T("Не удалось применить: ","Could not apply: ")+error;
            result=T("Настройки сохранены и применены","Settings saved and applied");return null;
        }
        public static void Draw()
        {
            if(draft==null)return;float w=MenuTheme.Width,h=MenuTheme.Height;
            MenuTheme.Label(new Rect(8,0,w-16,43),"HR Department",30,true);
            MenuTheme.Label(new Rect(8,42,w-16,29),T("Сотрудники и производство","Employees and production"),18,false,MenuTheme.Muted);
            GUI.BeginGroup(new Rect(0,83,w,h-83));float top;
            try{top=MenuTheme.Tabs(new[]{T("Ботаник","Botanist"),T("Химик","Chemist"),T("Уборщик","Cleaner"),T("Грузчик","Handler"),T("Найм","Hiring"),T("Производство","Production")},ref tab,w);}
            finally{GUI.EndGroup();}top+=83;
            var items=draft.Settings.Where(s=>s.Tab==tab).ToArray();float contentH=items.Length*76+250;
            scroll=GUI.BeginScrollView(new Rect(0,top,w,Math.Max(40,h-top)),scroll,new Rect(0,0,w-22,contentH));
            try
            {
                float cw=w-22,y=0;MenuTheme.Box(new Rect(0,0,cw,contentH-8),MenuTheme.Card);
                MenuTheme.Heading(ref y,cw,tab==5?T("Скорость производства","Production speed"):tab==4?T("Условия найма","Hiring fees"):T("Параметры сотрудника","Employee settings"));
                if(tab!=4){MenuTheme.Label(new Rect(20,y,cw-40,56),T("Больше множитель — быстрее. Меньше — медленнее.\n1× — обычно · 2× — вдвое быстрее · 0,5× — вдвое медленнее","Higher multiplier = faster. Lower = slower.\n1× normal · 2× twice as fast · 0.5× half speed"),16,false,MenuTheme.Muted);y+=75;}
                foreach(var s in items)
                {
                    if(s.Id=="Plants"){y+=14;MenuTheme.Heading(ref y,cw,T("Скорость роста","Growth speed"));}
                    bool compact=cw<650;float controls=compact?cw-207:cw-255;
                    MenuTheme.Label(new Rect(20,y,Math.Max(80,controls-30),66),MenuTheme.Language=="ru"?s.Ru:s.En,compact?16:20);
                    string value=draft.Text[s.Id];if(MenuTheme.Button(new Rect(controls,y+13,36,38),"−"))value=Step(s,value,-1);
                    value=MenuTheme.Field(new Rect(controls+42,y+13,compact?105:145,38),value,"hr."+s.Id);
                    if(MenuTheme.Button(new Rect(controls+(compact?153:193),y+13,36,38),"+"))value=Step(s,value,1);
                    if(value!=draft.Text[s.Id]){draft.Text[s.Id]=value;result=null;}
                    if(!s.TryParse(value,out _))MenuTheme.Label(new Rect(controls,y+52,190,18),T("Некорректное число","Invalid number"),12,false,new Color(1,.5f,.4f,1));
                    y+=76;MenuTheme.Divider(y,cw);
                }
            }
            finally{GUI.EndScrollView();}
        }
        private static string Step(NumericSetting s,string text,int direction)
        {
            if(!s.TryParse(text,out double value))return text;
            double min=s.Minimum>0&&!s.Integer?.1:s.Minimum;
            return s.Format(Math.Max(min,value+direction*(s.Integer||s.Minimum==0?1:.1)));
        }
    }
}
