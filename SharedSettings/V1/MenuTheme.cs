using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skoofidons.SharedSettings.V1
{
    public static class MenuTheme
    {
        public static readonly Color Background = new(.065f,.09f,.12f,.99f), Card = new(.09f,.125f,.16f,1),
            Line = new(.18f,.24f,.29f,1), Accent = new(0,.8f,.85f,1), Muted = new(.66f,.76f,.84f,1);
        private static GUIStyle text, bold, button, field, rail, thumb;
        private sealed class BackgroundResource
        {
            public GUIStyle Style;
            // Null for the borrowed built-in white texture; only generated textures are owned.
            public Texture2D Texture;
        }
        private static readonly Dictionary<int,BackgroundResource> backgrounds=new();
        private static GUISkin sourceSkin;
        private static Font sourceFont;
        private static bool hadSourceFont;
        private static readonly NumericEditor editor=new();
        private static string activeField;
        private static bool inputResetPending;
        private static int ownedKeyboard,ownedHot;
        public static void ResetInput()
        {
            activeField=null;editor.Reset("");inputResetPending=true;
        }
        // Called from each embedded runtime's OnGUI even when it is not coordinator.
        // Close callbacks can run outside OnGUI, so native GUI globals are released here.
        public static void FlushInputReset()
        {
            if(!inputResetPending)return;
            if(ownedKeyboard!=0&&GUIUtility.keyboardControl==ownedKeyboard)GUIUtility.keyboardControl=0;
            if(ownedHot!=0&&GUIUtility.hotControl==ownedHot)GUIUtility.hotControl=0;
            ownedKeyboard=ownedHot=0;inputResetPending=false;
        }
        public static string Language { get; private set; } = "en";
        public static float Width { get; private set; }
        public static float Height { get; private set; }
        public static string T(string ru, string en) => Language == "ru" ? ru : en;
        public static void Viewport(float width, float height, string language)
        { Width=width; Height=height; Language=GameLanguageSource.Normalize(language); }
        public static void Ensure()
        {
            var skin=GUI.skin;
            var font=skin.label.font;
            if(font==null)font=skin.font;
            if(text!=null&&sourceSkin==skin&&sourceFont==font&&(!hadSourceFont||sourceFont!=null))return;
            // Reuse the live game skin. OS-font creation is an unstripping-failure stub here.
            text = new GUIStyle(skin.label) { font=font,fontSize=20,wordWrap=true,alignment=TextAnchor.MiddleLeft };
            text.normal.textColor=Color.white;
            bold = new GUIStyle(text) { fontStyle=FontStyle.Bold };
            button = new GUIStyle(text) { alignment=TextAnchor.MiddleCenter };
            field = new GUIStyle(text) { alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(6,6,0,0) };
            field.normal.background=null; field.focused.background=null;
            field.normal.textColor=Color.white; field.focused.textColor=Color.white;
            rail = new GUIStyle { fixedHeight=24 };
            thumb = new GUIStyle { fixedWidth=24,fixedHeight=24 };
            sourceSkin=skin;sourceFont=font;hadSourceFont=font!=null;
        }
        // May run outside OnGUI. Do not read GUI globals or native GUIStyle state here.
        public static void ReleaseResources()
        {
            foreach(var resource in backgrounds.Values)
                if(resource.Texture!=null)UnityEngine.Object.Destroy(resource.Texture);
            backgrounds.Clear();
            text=bold=button=field=rail=thumb=null;
            sourceSkin=null;sourceFont=null;hadSourceFont=false;
        }
        public static void Box(Rect r, Color color, float radius=7)
        {
            int corner=(int)Math.Max(0,radius);
            if(!backgrounds.TryGetValue(corner,out var resource))
            {
                resource=new BackgroundResource {Style=new GUIStyle {border=new RectOffset(corner,corner,corner,corner)}};
                backgrounds.Add(corner,resource);
            }
            Texture2D texture=corner>0?resource.Texture:Texture2D.whiteTexture;
            // Unity's fake-null detects a native texture destroyed while its managed style survives.
            if(corner>0&&texture==null)
                {
                    int size=corner*2+2;var pixels=new Color32[size*size];
                    for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                    {
                        float dx=Math.Max(corner-(x+.5f),x+.5f-(size-corner));
                        float dy=Math.Max(corner-(y+.5f),y+.5f-(size-corner));
                        float distance=(float)Math.Sqrt(Math.Max(0,dx)*Math.Max(0,dx)+Math.Max(0,dy)*Math.Max(0,dy));
                        byte alpha=(byte)(Math.Clamp(corner+.5f-distance,0,1)*255);
                        pixels[y*size+x]=new Color32(255,255,255,alpha);
                    }
                    texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
                    resource.Texture=texture;
                    texture.hideFlags=HideFlags.HideAndDontSave;
                    texture.SetPixels32(pixels);texture.Apply(false,true);
                }
            var style=resource.Style;
            if(style.normal.background!=texture)style.normal.background=texture;
            var old=GUI.color;
            try{GUI.color=color;GUI.Box(r,GUIContent.none,style);}finally{GUI.color=old;}
        }
        public static void Label(Rect r,string value,int size=20,bool strong=false,Color? color=null)
        {
            Ensure(); var style=strong?bold:text; int old=style.fontSize; var oldColor=style.normal.textColor;
            style.fontSize=size; style.normal.textColor=color??Color.white;
            GUI.Label(r,value,style); style.fontSize=old; style.normal.textColor=oldColor;
        }
        public static bool Button(Rect r,string value,bool primary=false,int fontSize=20)
        {
            Ensure(); bool hover=r.Contains(Event.current.mousePosition);
            Box(r,primary?Accent:hover?new Color(.2f,.28f,.34f,1):new Color(.14f,.19f,.25f,1));
            button.normal.textColor=primary?Background:Color.white;
            int oldSize=button.fontSize;bool clicked;
            try{button.fontSize=fontSize;clicked=GUI.Button(r,value,button);}finally{button.fontSize=oldSize;}
            if(clicked){GUIUtility.keyboardControl=0;activeField=null;ownedKeyboard=0;}
            return clicked;
        }
        public static string Field(Rect r,string value,string name)
        {
            Ensure(); Box(r,new Color(.055f,.08f,.11f,1));
            if(!GUI.enabled){GUI.Label(r,value,field);return value;}
            int id=GUIUtility.GetControlID(name.GetHashCode(),FocusType.Keyboard,r);
            var e=Event.current;
            bool focused=activeField==name&&GUIUtility.keyboardControl==id;
            if(e.type==EventType.MouseDown&&e.button==0&&r.Contains(e.mousePosition))
            {
                if(!focused){activeField=name;editor.Reset(value);}
                GUIUtility.keyboardControl=id;GUIUtility.hotControl=id;ownedKeyboard=ownedHot=id;
                editor.Place(CharacterAt(r,editor.Text,e.mousePosition.x),e.shift);e.Use();focused=true;
            }
            if(focused&&e.type==EventType.MouseDrag&&GUIUtility.hotControl==id)
            {editor.Place(CharacterAt(r,editor.Text,e.mousePosition.x),true);e.Use();}
            if(e.type==EventType.MouseUp&&GUIUtility.hotControl==id){GUIUtility.hotControl=0;ownedHot=0;e.Use();}
            if(focused&&e.type==EventType.KeyDown)
            {
                bool used=true,command=e.control||e.command;
                if(command&&e.keyCode==KeyCode.A)editor.SelectAll();
                else if(command&&e.keyCode==KeyCode.C)GUIUtility.systemCopyBuffer=editor.SelectedText;
                else if(command&&e.keyCode==KeyCode.X){GUIUtility.systemCopyBuffer=editor.SelectedText;editor.Insert("");}
                else if(command&&e.keyCode==KeyCode.V)editor.Insert(GUIUtility.systemCopyBuffer);
                else if(e.keyCode==KeyCode.LeftArrow)editor.Move(-1,e.shift);
                else if(e.keyCode==KeyCode.RightArrow)editor.Move(1,e.shift);
                else if(e.keyCode==KeyCode.Home)editor.Home(e.shift);
                else if(e.keyCode==KeyCode.End)editor.End(e.shift);
                else if(e.keyCode==KeyCode.Backspace)editor.Backspace();
                else if(e.keyCode==KeyCode.Delete)editor.Delete();
                else if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter||e.keyCode==KeyCode.Tab){GUIUtility.keyboardControl=0;activeField=null;}
                else if(!command&&!char.IsControl(e.character))editor.Insert(e.character.ToString());
                else used=false;
                if(used)e.Use();
            }
            string display=focused?editor.Text:value;
            float left=(r.width-Measure(display))*.5f;
            GUI.BeginGroup(r);
            try
            {
                if(focused&&editor.Length>0)
                    Box(new Rect(left+Measure(display.Substring(0,editor.Start)),7,Measure(editor.SelectedText),r.height-14),new Color(0,.45f,.5f,.75f),0);
                GUI.Label(new Rect(0,0,r.width,r.height),display,field);
                if(focused&&Time.realtimeSinceStartup%1<.55f)
                    Box(new Rect(left+Measure(display.Substring(0,editor.Caret)),8,1,r.height-16),Color.white,0);
            }
            finally{GUI.EndGroup();}
            return display;
        }
        private static float Measure(string value)=>field.CalcSize(new GUIContent(value)).x-12;
        private static int CharacterAt(Rect r,string value,float x)
        {
            float left=r.x+(r.width-Measure(value))*.5f;
            for(int i=0;i<value.Length;i++)if(x<left+(Measure(value.Substring(0,i))+Measure(value.Substring(0,i+1)))*.5f)return i;
            return value.Length;
        }
        public static void Divider(float y,float width) => Box(new Rect(16,y,width-32,1),Line,0);
        public static void Heading(ref float y,float width,string value)
        { Label(new Rect(20,y,width-40,38),value,22,true); y+=43; Divider(y,width); y+=12; }
        public static float PanelHeader(string title,string subtitle)
        {
            float w=Width;bool small=w<650;
            Label(new Rect(8,0,w-16,43),title,small?24:30,true);
            Label(new Rect(8,42,w-16,29),subtitle,small?14:18,false,Muted);
            return 83;
        }
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
