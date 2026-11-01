using System;
using System.Linq;
using MelonLoader;
using UnityEngine;

namespace Skoofidons.SharedSettings.V1
{
    public static class SharedMenuRuntime
    {
        private static readonly MelonLogger.Instance Logger=new("Skoofidon Settings");
        private static readonly MenuHostLifecycle lifecycle=new();
        private static MenuSession session=>lifecycle.Session;
        private static System.Collections.Generic.IReadOnlyList<ProviderManifest> providers;
        private static Il2CppSystem.Action<bool> focusChanged;
        private static Vector2 sidebarScroll;
        private static string language="en", loggedError, applyError;
        private static bool host=>lifecycle.IsHost;
        public static void LateInitialize()
        {
            if(!lifecycle.Prepare(Time.frameCount))return;
            providers=ProviderDiscovery.Discover(MelonBase.RegisteredMelons.Select(m=>m.MelonAssembly.Assembly),Logger.Warning);
            var winner=HostElection.Elect(providers);
            if(winner==null||winner.Assembly!=typeof(SharedMenuRuntime).Assembly)return;
            lifecycle.Activate(new MenuSession(providers,()=>new GameUiInputLease(()=>session?.RequestExit(Time.frameCount))));
            focusChanged=(Action<bool>)(focused=>{if(!focused)session?.RequestClose(Time.frameCount);});
            Application.focusChanged+=focusChanged;
            Logger.Msg("F3 ready; runtime="+EntryPointV1.RuntimeVersion+"; active panels="+string.Join(", ",providers.Select(p=>p.Title))+
                "; automatic coordinator="+winner.Id+". Any participating mod can provide this menu on its own.");
        }
        public static void Tick()
        {
            if(!host||session==null)return;
            try
            {
                if(Input.GetKeyDown(KeyCode.F3))
                {
                    if(!session.HasLease&&!NativeInputGuard.CanOpen())return;
                    if(!session.HasLease) {language=GameLanguageSource.Read();applyError=null;loggedError=null;}
                    session.Toggle(Time.frameCount);
                }
                session.Tick(Time.frameCount,Input.anyKey,Application.isFocused);
                if(session.Error!=null&&loggedError!=session.Error)
                {loggedError=session.Error;Logger.Warning(loggedError);}
            }
            catch(Exception e){session.Abort(Time.frameCount);Log(e);}
        }
        private static void Log(Exception e)
        {if(loggedError==e.Message)return;loggedError=e.Message;Logger.Warning(e.ToString());}
        public static void Draw()
        {
            MenuTheme.FlushInputReset();
            if(!host||session==null||!session.Visible)return;
            bool abortEvent=false;
            using(var saved=new GuiStateScope())
            {
                try {GUI.color=GUI.backgroundColor=GUI.contentColor=Color.white;GUI.enabled=true;abortEvent=!DrawWindow();}
                catch(Exception e){if(!MenuSession.IsGuiExit(e)){session.FailShell(e,Time.frameCount);Log(e);}abortEvent=true;}
            }
            // Outside the catch: terminate Unity's GUI event; never draw a footer into damaged groups.
            if(abortEvent)GuiEventBoundary.End(GUIUtility.ExitGUI);
        }
        private static bool DrawWindow()
        {
            MenuTheme.Ensure();
            float w=Math.Min(1420,Screen.width-24),h=Math.Min(960,Screen.height-24),side=w<850?154:Math.Min(330,w*.27f);
            var window=new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);
            MenuTheme.Box(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.35f),0);
            MenuTheme.Box(window,MenuTheme.Background);
            GUI.BeginGroup(window);
            try
            {
                var selected=session.Selected;
                bool modal=false;
                if(selected?.HasModal!=null)
                    using(var state=new GuiStateScope())
                        if(!session.TryProviderCall(selected.HasModal,Time.frameCount,out modal))return false;
                bool backgroundEnabled=!modal;
                GUI.enabled=backgroundEnabled;
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
                selected=session.Selected;
                if(selected!=null&&session.Visible)
                {
                    using(var state=new GuiStateScope())
                        if(!session.TryProviderCall(()=>{selected.Viewport?.Invoke(panel.width,panel.height,language);return true;},Time.frameCount,out _))return false;
                    GUI.BeginGroup(panel);
                    bool drawn;
                    try{using(var state=new GuiStateScope())drawn=session.Draw(Time.frameCount);}finally{GUI.EndGroup();}
                    if(!drawn)return false;
                }
                MenuTheme.Viewport(w,h,language);
                MenuTheme.Box(new Rect(0,h-80,w,1),MenuTheme.Line,0);
                bool canApply=selected?.Apply!=null;
                float closeX=w-(canApply?290:155);
                string status;
                using(var state=new GuiStateScope())
                    if(!session.TryProviderCall(()=>applyError??selected?.Status?.Invoke()??"",Time.frameCount,out status))return false;
                MenuTheme.Label(new Rect(20,h-69,Math.Max(50,closeX-35),58),status,w<850?13:17,false,applyError==null?MenuTheme.Muted:new Color(1,.5f,.4f,1));
                if(MenuTheme.Button(new Rect(closeX,h-61,125,43),MenuTheme.T("Закрыть","Close"))) session.RequestClose(Time.frameCount);
                if(canApply)
                {
                    bool dirty;
                    using(var state=new GuiStateScope())
                        if(!session.TryProviderCall(()=>selected.Dirty?.Invoke()??true,Time.frameCount,out dirty))return false;
                    GUI.enabled=backgroundEnabled&&dirty;
                    if(MenuTheme.Button(new Rect(w-151,h-61,133,43),MenuTheme.T("Применить","Apply"),true))
                    {
                        using(var state=new GuiStateScope())
                            if(!session.TryProviderCall(selected.Apply,Time.frameCount,out applyError))return false;
                        GUI.FocusControl(null);
                    }
                    GUI.enabled=backgroundEnabled;
                }
                GUI.enabled=true;
                if(selected?.HasModal!=null&&session.Visible)
                {
                    using(var state=new GuiStateScope())
                        if(!session.TryProviderCall(selected.HasModal,Time.frameCount,out modal))return false;
                    if(modal)
                        using(var state=new GuiStateScope())
                            if(!session.TryProviderCall(()=>{selected.DrawModal!(w,h);return true;},Time.frameCount,out _))return false;
                }
                return true;
            }
            finally{GUI.EndGroup();}
        }
        public static void SceneChanged()=>session?.Tick(Time.frameCount,Input.anyKey,Application.isFocused);
        public static void Shutdown()
        {
            try{lifecycle.Stop(0);}
            finally
            {
                try{if(focusChanged!=null)Application.focusChanged-=focusChanged;}
                catch(Exception e){Log(e);}
                finally{focusChanged=null;}
            }
        }
    }
}
