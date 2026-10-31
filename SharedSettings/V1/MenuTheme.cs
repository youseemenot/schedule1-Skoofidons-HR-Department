using System;
using UnityEngine;

namespace Skoofidons.SharedSettings.V1
{
    public static class MenuTheme
    {
        public static readonly Color Background = new(.065f,.09f,.12f,.99f), Card = new(.09f,.125f,.16f,1),
            Line = new(.18f,.24f,.29f,1), Accent = new(0,.8f,.85f,1), Muted = new(.66f,.76f,.84f,1);
        private static GUIStyle text, bold, button, field, rail, thumb;
        public static string Language { get; private set; } = "en";
        public static float Width { get; private set; }
        public static float Height { get; private set; }
        public static string T(string ru, string en) => Language == "ru" ? ru : en;
        public static void Viewport(float width, float height, string language)
        { Width=width; Height=height; Language=GameLanguageSource.Normalize(language); }
        public static void Ensure()
        {
            if (text != null) return;
            var font = Font.CreateDynamicFontFromOSFont("Segoe UI", 20);
            text = new GUIStyle(GUI.skin.label) { font=font,fontSize=20,wordWrap=true,alignment=TextAnchor.MiddleLeft };
            text.normal.textColor=Color.white;
            bold = new GUIStyle(text) { fontStyle=FontStyle.Bold };
            button = new GUIStyle(text) { alignment=TextAnchor.MiddleCenter };
            field = new GUIStyle(text) { alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(6,6,0,0) };
            field.normal.background=null; field.focused.background=null;
            field.normal.textColor=Color.white; field.focused.textColor=Color.white;
            rail = new GUIStyle { fixedHeight=24 };
            thumb = new GUIStyle { fixedWidth=24,fixedHeight=24 };
        }
        public static void Box(Rect r, Color color, float radius=7) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, radius);
        public static void Label(Rect r,string value,int size=20,bool strong=false,Color? color=null)
        {
            Ensure(); var style=strong?bold:text; int old=style.fontSize; var oldColor=style.normal.textColor;
            style.fontSize=size; style.normal.textColor=color??Color.white;
            GUI.Label(r,value,style); style.fontSize=old; style.normal.textColor=oldColor;
        }
        public static bool Button(Rect r,string value,bool primary=false)
        {
            Ensure(); bool hover=r.Contains(Event.current.mousePosition);
            Box(r,primary?Accent:hover?new Color(.2f,.28f,.34f,1):new Color(.14f,.19f,.25f,1));
            button.normal.textColor=primary?Background:Color.white;
            return GUI.Button(r,value,button);
        }
        public static string Field(Rect r,string value,string name)
        {
            Ensure(); Box(r,new Color(.055f,.08f,.11f,1));
            GUI.SetNextControlName(name); return GUI.TextField(r,value,field);
        }
        public static void Divider(float y,float width) => Box(new Rect(16,y,width-32,1),Line,0);
        public static void Heading(ref float y,float width,string value)
        { Label(new Rect(20,y,width-40,38),value,22,true); y+=43; Divider(y,width); y+=12; }
        public static bool Toggle(ref float y,float width,string label,bool value)
        {
            Ensure(); var r=new Rect(width-82,y+11,64,34);
            Label(new Rect(20,y,Math.Max(80,width-118),56),label,19);
            Box(r,value?Accent:Line,17);
            Box(new Rect(value?r.x+33:r.x+3,r.y+3,28,28),Color.white,14);
            if (GUI.Button(new Rect(12,y,width-24,56),"",new GUIStyle())) value=!value;
            y+=58; Divider(y,width); return value;
        }
        public static float Slider(ref float y,float width,string label,float value,float min,float max,
            string display,string left,string right,Color? tint=null)
        {
            Ensure(); bool compact=width<650; float start=compact?20:width*.46f;
            Label(new Rect(20,y,width-40,32),label,19);
            Label(new Rect(width-120,y,100,32),display,18,false,Muted);
            float sy=y+42,sw=width-start-26;
            var r=new Rect(start,sy,sw,24);
            float next=GUI.HorizontalSlider(r,value,min,max,rail,thumb);
            float p=Mathf.InverseLerp(min,max,next),travel=sw-24;
            Box(new Rect(start+12,sy+10,travel,5),Line,2);
            if(p>0) Box(new Rect(start+12,sy+10,travel*p,5),tint??Accent,2);
            Box(new Rect(start+travel*p,sy,24,24),tint??Accent,12);
            Box(new Rect(start+travel*p+2,sy+2,20,20),Color.white,10);
            Label(new Rect(start,sy+26,100,22),left,14,false,Muted);
            var style=new GUIStyle(text){alignment=TextAnchor.MiddleRight,fontSize=14}; style.normal.textColor=Muted;
            GUI.Label(new Rect(width-126,sy+26,100,22),right,style);
            y+=100; Divider(y,width); return next;
        }
        public static float Tabs(string[] labels,ref int selected,float width)
        {
            int cols=Math.Max(1,Math.Min(labels.Length,(int)(width/125))); int rows=(labels.Length+cols-1)/cols;
            for(int i=0;i<labels.Length;i++)
            {
                var r=new Rect(i%cols*width/cols,i/cols*48,width/cols,46);
                if(GUI.Button(r,labels[i],new GUIStyle(button){fontSize=17})) {selected=i;GUI.FocusControl(null);}
                if(i==selected) Box(new Rect(r.x,r.yMax-2,r.width,3),Accent,0);
            }
            return rows*48+18;
        }
    }
}
