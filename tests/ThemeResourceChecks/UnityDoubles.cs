// Deterministic resource-lifetime model only. The source under test is the real MenuTheme.cs.
namespace UnityEngine
{
    [System.Flags] public enum HideFlags { None=0,HideAndDontSave=61 }
    public class Object
    {
        internal bool Destroyed;
        public HideFlags hideFlags;
        public static bool operator ==(Object a,Object b){bool an=ReferenceEquals(a,null)||a.Destroyed,bn=ReferenceEquals(b,null)||b.Destroyed;return an||bn?an==bn:ReferenceEquals(a,b);}
        public static bool operator !=(Object a,Object b)=>!(a==b);
        public override bool Equals(object o)=>ReferenceEquals(this,o);
        public override int GetHashCode()=>base.GetHashCode();
        public static void Destroy(Object value){if(ReferenceEquals(value,null)||value.Destroyed)return;value.Destroyed=true;if(value is Texture2D t&&!t.Builtin)Texture2D.DestroyedGenerated++;}
    }
    public sealed class Texture2D:Object
    {
        public static int Generated,DestroyedGenerated;
        internal bool Builtin;
        internal static readonly List<Texture2D> All=new();
        public static readonly Texture2D whiteTexture=new Texture2D {Builtin=true};
        private Texture2D(){}
        public Texture2D(int w,int h,TextureFormat f,bool mipmap){GUI.RequireOnGUI();Generated++;All.Add(this);}
        public static int LiveGenerated=>All.Count(t=>t!=null);
        public void SetPixels32(Color32[] values){GUI.RequireOnGUI();}
        public void Apply(bool updateMipmaps,bool makeNoLongerReadable){GUI.RequireOnGUI();}
    }
    public static class Resources
    {
        public static void UnloadUnusedAssets(){foreach(var t in Texture2D.All)if((t.hideFlags&HideFlags.HideAndDontSave)!=HideFlags.HideAndDontSave)Object.Destroy(t);}
    }
    public sealed class Font:Object{}
    public sealed class GUISkin:Object {public GUIStyle label;public Font font;}
    public sealed class GUIStyleState {private Texture2D texture;public Texture2D background {get{GUI.RequireOnGUI();return texture;}set{GUI.RequireOnGUI();texture=value;}}public Color textColor;}
    public sealed class GUIStyle
    {
        private GUIStyleState normalState=new(),focusState=new();
        public GUIStyleState normal{get{GUI.RequireOnGUI();return normalState;}}
        public GUIStyleState focused{get{GUI.RequireOnGUI();return focusState;}}
        public int fontSize;public bool wordWrap;public TextAnchor alignment;public FontStyle fontStyle;public RectOffset padding,border;public float fixedHeight,fixedWidth;public Font font;
        public GUIStyle(){GUI.RequireOnGUI();}
        public GUIStyle(GUIStyle other):this(){if(other==null)return;font=other.font;fontSize=other.fontSize;normalState.textColor=other.normalState.textColor;normalState.background=other.normalState.background;}
        public Vector2 CalcSize(GUIContent c)=>new Vector2(c.text.Length*10,20);
    }
    public static class GUI
    {
        public static bool InOnGUI,enabled=true;
        public static Color color;
        public static GUISkin LiveSkin;
        public static GUIStyle LastBoxStyle,LastLabelStyle;
        public static Texture2D LastBackground;
        static GUI(){ReplaceSkin();}
        public static void ReplaceSkin(){bool was=InOnGUI;InOnGUI=true;try{LiveSkin=new GUISkin{label=new GUIStyle{font=new Font()}};}finally{InOnGUI=was;}}
        public static void RequireOnGUI(){if(!InOnGUI)throw new System.InvalidOperationException("GUI/style access outside OnGUI");}
        public static GUISkin skin{get{RequireOnGUI();return LiveSkin;}}
        public static void Box(Rect r,GUIContent c,GUIStyle style){RequireOnGUI();LastBoxStyle=style;LastBackground=style.normal.background;}
        public static void Label(Rect r,string text,GUIStyle style){RequireOnGUI();LastLabelStyle=style;}
        public static bool Button(Rect r,string text,GUIStyle style){RequireOnGUI();return false;}
        public static float HorizontalSlider(Rect r,float v,float min,float max,GUIStyle rail,GUIStyle thumb){RequireOnGUI();return v;}
        public static void BeginGroup(Rect r){RequireOnGUI();}public static void EndGroup(){RequireOnGUI();}public static void FocusControl(string name){RequireOnGUI();}
    }
    public struct Color {public Color(float r,float g,float b,float a){}public static Color white=>new(1,1,1,1);}
    public struct Color32 {public Color32(byte r,byte g,byte b,byte a){}}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Rect {public float x,y,width,height;public float yMax=>y+height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public bool Contains(Vector2 p)=>false;}
    public sealed class RectOffset {public RectOffset(int a,int b,int c,int d){}}
    public sealed class GUIContent {public string text;public GUIContent(string text){this.text=text;}public static GUIContent none=new("");}
    public enum TextureFormat {RGBA32}public enum TextAnchor {MiddleLeft,MiddleCenter,MiddleRight}public enum FontStyle {Bold}public enum FocusType {Keyboard}
    public enum EventType {MouseDown,MouseDrag,MouseUp,KeyDown}public enum KeyCode {A,C,X,V,LeftArrow,RightArrow,Home,End,Backspace,Delete,Return,KeypadEnter,Tab}
    public sealed class Event {public static Event current=new();public Vector2 mousePosition;public EventType type;public int button;public bool shift,control,command;public KeyCode keyCode;public char character;public void Use(){}}
    public static class GUIUtility {public static int keyboardControl,hotControl;public static string systemCopyBuffer;public static int GetControlID(int hash,FocusType type,Rect rect)=>hash;}
    public static class Time {public static float realtimeSinceStartup;}
    public static class Mathf {public static float InverseLerp(float min,float max,float value)=>(value-min)/(max-min);}
}
namespace Skoofidons.SharedSettings.V1 {public static class GameLanguageSource {public static string Normalize(string language)=>language=="ru"?"ru":"en";}}
