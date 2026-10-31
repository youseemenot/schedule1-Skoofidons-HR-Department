using System;
using Skoofidons.SharedSettings;
using Skoofidons.SharedSettings.V1;
using UnityEngine;

namespace SkoofidonsHRDepartment
{
    public sealed class LegacyMinimapPanel
    {
        private int settingsTab;
        private Vector2 settingsScroll;
        private static string T(string ru,string en)=>MenuTheme.T(ru,en);
        private readonly LegacyMinimapBinding binding;
        public LegacyMinimapPanel(LegacyMinimapBinding binding){this.binding=binding;}
        public void Opened(){binding.Refresh();settingsScroll=Vector2.zero;}
        public void Closed()=>binding.Flush(Time.realtimeSinceStartup,MelonLoader.MelonLogger.Warning);
        public string Status()=>binding.SaveError==null?T("Изменения применяются сразу","Changes apply immediately"):T("Не удалось сохранить: ","Could not save: ")+binding.SaveError;
        public void Draw()
        {
            float w=MenuTheme.Width,h=MenuTheme.Height;
            MenuTheme.Label(new Rect(8,0,w-16,43),"Minimap",30,true);
            MenuTheme.Label(new Rect(8,42,w-16,29),T("Карта и маркеры","Map and markers"),18,false,MenuTheme.Muted);
            float top;int previousTab=settingsTab;
            GUI.BeginGroup(new Rect(0,83,w,h-83));
            try{top=MenuTheme.Tabs(new[]{T("Общее","General"),T("Маркеры","Markers"),T("Положение","Position"),T("Стрелка","Arrow")},ref settingsTab,w);}
            finally{GUI.EndGroup();}
            top+=83;
            if(settingsTab!=previousTab)settingsScroll=Vector2.zero;
            float contentH=settingsTab==0?650:settingsTab==1?680:settingsTab==2?700:660;
            settingsScroll=GUI.BeginScrollView(new Rect(0,top,w,Math.Max(40,h-top)),settingsScroll,new Rect(0,0,w-22,contentH));
            try
            {
                float cw=w-22,y=0;
                void Section(string title,float height){MenuTheme.Box(new Rect(0,y,cw,height),MenuTheme.Card);MenuTheme.Heading(ref y,cw,title);}
                void Gap(){y+=24;}
                if(settingsTab==0)
                {
                    Section(T("Отображение","Display"),246);
                    Toggle(ref y,cw,T("Мини-карта","Minimap"),"minimapEnabled");
                    Toggle(ref y,cw,T("Часы","Clock"),"timeBarEnabled");
                    Toggle(ref y,cw,T("Режим навигации","Navigation mode"),"useNavMode");
                    Gap();Section(T("Размер и масштаб","Size and scale"),365);
                    Slider(ref y,cw,T("Размер окна","Window size"),"windowSizeMultiplier",1,3);
                    Slider(ref y,cw,T("Масштаб карты","Map zoom"),"mapZoom",.5f,3);
                    Slider(ref y,cw,T("Непрозрачность","Opacity"),"mapOpacity",.2f,1,true);
                }
                else if(settingsTab==1)
                {
                    Section(T("На мини-карте","On the minimap"),420);
                    Toggle(ref y,cw,T("Активные заказы","Active orders"),"showOrdersEnabled");
                    Toggle(ref y,cw,T("Дилеры","Dealers"),"showDealersEnabled");
                    Toggle(ref y,cw,T("Покупатели","Customers"),"showCustomersEnabled");
                    Toggle(ref y,cw,T("Тайники","Dead drops"),"showDeadDropsEnabled");
                    Toggle(ref y,cw,T("Недвижимость","Properties"),"showPropertiesEnabled");
                    Toggle(ref y,cw,T("Транспорт","Vehicles"),"showVehiclesEnabled");
                    Gap();Section(T("В игровом мире","In the game world"),188);
                    Toggle(ref y,cw,T("Игроки кооператива","Co-op players"),"coopRadarEnabled");
                    Toggle(ref y,cw,T("Полиция","Police"),"policeRadarEnabled");
                }
                else if(settingsTab==2)
                {
                    Section(T("Мини-карта","Minimap"),265);
                    Offset(ref y,cw,T("По горизонтали","Horizontal"),"mapOffsetX",Screen.width,T("Влево","Left"),T("Вправо","Right"));
                    Offset(ref y,cw,T("По вертикали","Vertical"),"mapOffsetY",Screen.height,T("Вниз","Down"),T("Вверх","Up"));
                    Gap();Section(T("Часы","Clock"),365);
                    Offset(ref y,cw,T("По горизонтали","Horizontal"),"timeOffsetX",Screen.width,T("Влево","Left"),T("Вправо","Right"));
                    Offset(ref y,cw,T("По вертикали","Vertical"),"timeOffsetY",Screen.height,T("Вниз","Down"),T("Вверх","Up"));
                    Slider(ref y,cw,T("Масштаб часов","Clock scale"),"timeScale",.5f,3);
                }
                else
                {
                    Section(T("Размер стрелки","Arrow size"),166);
                    Slider(ref y,cw,T("Размер","Size"),"arrowSize",.5f,3);
                    Gap();float sectionY=y;Section(T("Цвет стрелки","Arrow colour"),365);
                    MenuTheme.Label(new Rect(cw-70,sectionY+4,45,40),"➤",32,true,new Color(binding.Get<float>("arrowR"),binding.Get<float>("arrowG"),binding.Get<float>("arrowB"),1));
                    Slider(ref y,cw,T("Красный (R)","Red (R)"),"arrowR",0,1,true,new Color(1,.25f,.35f,1));
                    Slider(ref y,cw,T("Зелёный (G)","Green (G)"),"arrowG",0,1,true,new Color(.1f,.9f,.3f,1));
                    Slider(ref y,cw,T("Синий (B)","Blue (B)"),"arrowB",0,1,true,new Color(.1f,.5f,1,1));
                }
            }
            finally{GUI.EndScrollView();}
        }
        private void Toggle(ref float y,float w,string label,string key)
        {bool value=binding.Get<bool>(key);bool next=MenuTheme.Toggle(ref y,w,label,value);if(next!=value)binding.Set(key,next);}
        private static string Number(float value)=>value.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture).Replace('.',MenuTheme.Language=="ru"?',':'.');
        private void Slider(ref float y,float w,string label,string key,float min,float max,bool percent=false,Color? tint=null)
        {
            float value=binding.Get<float>(key);
            string Display(float v)=>percent?Mathf.RoundToInt(v*100)+" %":Number(v)+" ×";
            float next=MenuTheme.Slider(ref y,w,label,value,min,max,Display(value),Display(min),Display(max),tint);
            if(!Mathf.Approximately(next,value))binding.Set(key,next);
        }
        private void Offset(ref float y,float w,string label,string key,float bound,string left,string right)
        {
            float value=binding.Get<float>(key);
            float next=MenuTheme.Slider(ref y,w,label,value,-bound,bound,Mathf.RoundToInt(value)+" px",left,right);
            if(!Mathf.Approximately(next,value))binding.Set(key,next);
        }
    }
}
