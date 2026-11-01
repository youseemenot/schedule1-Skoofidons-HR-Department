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
        private readonly ResetConfirmation confirmation=new();
        public LegacyMinimapPanel(LegacyMinimapBinding binding){this.binding=binding;}
        public void Opened(){confirmation.Cancel();binding.Refresh();settingsScroll=Vector2.zero;}
        public void Closed(){confirmation.Cancel();binding.Flush(Time.realtimeSinceStartup,MelonLoader.MelonLogger.Warning);}
        public bool HasModal()=>confirmation.Pending;
        public void CancelModal()=>confirmation.Cancel();
        private static string[] TabNames()=>new[]{T("Общее","General"),T("Маркеры","Markers"),T("Положение","Position"),T("Стрелка","Arrow")};
        public string Status()=>binding.SaveError==null?T("Изменения применяются сразу","Changes apply immediately"):T("Не удалось сохранить: ","Could not save: ")+binding.SaveError;
        public void Draw()
        {
            float w=MenuTheme.Width,h=MenuTheme.Height;
            MenuTheme.DefaultsHeader("Minimap",T("Карта и маркеры","Map and markers"),()=>confirmation.Request(settingsTab));
            float top;int previousTab=settingsTab;
            GUI.BeginGroup(new Rect(0,83,w,h-83));
            try{top=MenuTheme.Tabs(TabNames(),ref settingsTab,w);}
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
        public void DrawModal(float width,float height)
        {
            if(!confirmation.Pending)return;
            MenuTheme.Box(new Rect(0,0,width,height),new Color(0,0,0,.6f),0);
            float w=Math.Min(620,width-32),h=confirmation.Error==null?310:370;
            var box=new Rect((width-w)*.5f,(height-h)*.5f,w,h);
            MenuTheme.Box(box,MenuTheme.Line);MenuTheme.Box(new Rect(box.x+1,box.y+1,w-2,h-2),MenuTheme.Card);
            GUI.BeginGroup(box);
            try
            {
                string name=TabNames()[confirmation.Tab];
                MenuTheme.Label(new Rect(24,16,w-48,64),T("Сбросить вкладку «"+name+"»?","Reset the "+name+" tab?"),24,true);
                MenuTheme.Label(new Rect(24,87,w-48,64),T("Стандартные значения будут восстановлены только на этой вкладке.","Default values will be restored only on this tab."),19);
                MenuTheme.Label(new Rect(24,157,w-48,48),T("Остальные настройки Minimap сохранятся.","Other Minimap settings will be kept."),18,false,MenuTheme.Muted);
                if(confirmation.Error!=null)MenuTheme.Label(new Rect(24,209,w-48,55),T("Не удалось сбросить: ","Could not reset: ")+confirmation.Error,15,false,new Color(1,.5f,.4f,1));
                MenuTheme.Divider(h-82,w);
                if(MenuTheme.Button(new Rect(w-310,h-62,132,43),T("Отмена","Cancel"))){confirmation.Cancel();return;}
                if(MenuTheme.Button(new Rect(w-162,h-62,138,43),T("Сбросить","Reset"),true))
                    confirmation.Confirm(tab=>binding.ResetTab(tab,Time.realtimeSinceStartup));
            }
            finally{GUI.EndGroup();}
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
