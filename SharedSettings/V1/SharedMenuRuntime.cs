using System;
using System.Linq;
using MelonLoader;
using UnityEngine;

namespace Skoofidons.SharedSettings.V1
{
    public static class SharedMenuRuntime
    {
        private static MenuSession session;
        private static System.Collections.Generic.IReadOnlyList<ProviderManifest> providers;
        private static Il2CppSystem.Action<bool> focusChanged;
        private static Vector2 sidebarScroll;
        private static string language="en", loggedError, applyError;
        private static bool host;
        public static void LateInitialize()
        {
            if(session!=null)return;
            providers=ProviderDiscovery.Discover(MelonBase.RegisteredMelons.Select(m=>m.MelonAssembly.Assembly),MelonLogger.Warning);
            var winner=HostElection.Elect(providers);
            host=winner!=null&&winner.Assembly==typeof(SharedMenuRuntime).Assembly;
            if(!host)return;
            session=new MenuSession(providers,()=>new GameUiInputLease(()=>session.RequestClose(Time.frameCount)));
            focusChanged=(Action<bool>)(focused=>{if(!focused)session.RequestClose(Time.frameCount);});
            Application.focusChanged+=focusChanged;
            MelonLogger.Msg("[Shared F3] Host="+winner.Id+"; runtime="+EntryPointV1.RuntimeVersion+"; providers="+providers.Count);
        }
        public static void Tick()
        {
            if(!host||session==null)return;
            try
            {
                if(Input.GetKeyDown(KeyCode.F3))
                {
                    if(!session.HasLease) {language=GameLanguageSource.Read();applyError=null;loggedError=null;}
                    session.Toggle(Time.frameCount);
                }
                session.Tick(Time.frameCount,Input.anyKey,Application.isFocused);
                if(session.Error!=null&&loggedError!=session.Error)
                {loggedError=session.Error;MelonLogger.Warning("[Shared F3] "+loggedError);}
            }
            catch(Exception e){session.Abort(Time.frameCount);Log(e);}
        }
        private static void Log(Exception e)
        {if(loggedError==e.Message)return;loggedError=e.Message;MelonLogger.Warning("[Shared F3] "+e);}
        public static void Draw()
        {
            if(!host||session==null||!session.Visible)return;
            var matrix=GUI.matrix;var color=GUI.color;var background=GUI.backgroundColor;var content=GUI.contentColor;bool enabled=GUI.enabled;
            try {GUI.color=GUI.backgroundColor=GUI.contentColor=Color.white;GUI.enabled=true;DrawWindow();}
            catch(Exception e){session.FailProvider(e,Time.frameCount);Log(e);}
            finally {GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=background;GUI.contentColor=content;GUI.enabled=enabled;}
        }
        private static void DrawWindow()
        {
            MenuTheme.Ensure();
            float w=Math.Min(1420,Screen.width-24),h=Math.Min(960,Screen.height-24),side=w<850?154:Math.Min(330,w*.27f);
            var window=new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);
            MenuTheme.Box(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.35f),0);
            MenuTheme.Box(window,MenuTheme.Background);
            GUI.BeginGroup(window);
            try
            {
                MenuTheme.Viewport(w,h,language);
                MenuTheme.Label(new Rect(22,12,w-90,48),MenuTheme.T("Настройки модов","Mod settings"),w<850?24:30,true);
                if(MenuTheme.Button(new Rect(w-58,16,40,40),"×"))session.RequestClose(Time.frameCount);
                MenuTheme.Box(new Rect(0,72,w,1),MenuTheme.Line,0);
                MenuTheme.Box(new Rect(side,73,1,h-153),MenuTheme.Line,0);
                float bodyH=h-160;
                sidebarScroll=GUI.BeginScrollView(new Rect(8,90,side-16,bodyH-20),sidebarScroll,new Rect(0,0,side-34,Math.Max(bodyH-22,providers.Count*88+35)));
                try
                {
                    MenuTheme.Label(new Rect(12,0,side-48,26),MenuTheme.T("МОДЫ","MODS"),14,false,MenuTheme.Muted);
                    for(int i=0;i<providers.Count;i++)
                    {
                        var p=providers[i];bool active=p.Id==session.Selected?.Id;
                        var r=new Rect(0,34+i*88,side-36,80);
                        if(active){MenuTheme.Box(r,new Color(.025f,.2f,.25f,1));MenuTheme.Box(new Rect(0,r.y,4,r.height),MenuTheme.Accent,0);}
                        string title=p.Id=="skoofidon.minimap"?"Minimap":p.Id=="skoofidon.hr"?"HR Department":p.Title;
                        MenuTheme.Label(new Rect(14,r.y+5,r.width-24,36),title,side<200?17:21,true);
                        string subtitle=p.Id=="skoofidon.minimap"?MenuTheme.T("Карта и маркеры","Map and markers"):
                            p.Id=="skoofidon.hr"?MenuTheme.T("Сотрудники и производство","Employees and production"):p.ProviderVersion.ToString();
                        MenuTheme.Label(new Rect(14,r.y+40,r.width-24,34),subtitle,side<200?12:16,false,MenuTheme.Muted);
                        if(GUI.Button(r,"",new GUIStyle())){session.Select(p.Id,Time.frameCount);applyError=null;}
                    }
                }
                finally{GUI.EndScrollView();}
                var panel=new Rect(side+20,90,w-side-40,bodyH-15);
                var selected=session.Selected;
                if(selected!=null&&session.Visible)
                {
                    selected.Viewport?.Invoke(panel.width,panel.height,language);
                    GUI.BeginGroup(panel);
                    try{session.Draw(Time.frameCount);}finally{GUI.EndGroup();}
                }
                MenuTheme.Viewport(w,h,language);
                MenuTheme.Box(new Rect(0,h-80,w,1),MenuTheme.Line,0);
                bool canApply=selected?.Apply!=null;
                float closeX=w-(canApply?290:155);
                string status=applyError??selected?.Status?.Invoke()??"";
                MenuTheme.Label(new Rect(20,h-69,Math.Max(50,closeX-35),58),status,w<850?13:17,false,applyError==null?MenuTheme.Muted:new Color(1,.5f,.4f,1));
                if(MenuTheme.Button(new Rect(closeX,h-61,125,43),MenuTheme.T("Закрыть","Close"))) session.RequestClose(Time.frameCount);
                if(canApply)
                {
                    bool dirty=selected.Dirty?.Invoke()??true;
                    GUI.enabled=dirty;
                    if(MenuTheme.Button(new Rect(w-151,h-61,133,43),MenuTheme.T("Применить","Apply"),true))
                        try{applyError=selected.Apply();GUI.FocusControl(null);}catch(Exception e){applyError=e.Message;}
                    GUI.enabled=true;
                }
            }
            finally{GUI.EndGroup();}
        }
        public static void SceneChanged()=>session?.Tick(Time.frameCount,Input.anyKey,Application.isFocused);
        public static void Shutdown()
        {
            try{session?.Abort(Time.frameCount);}
            finally{if(focusChanged!=null)Application.focusChanged-=focusChanged;focusChanged=null;}
        }
    }
}
