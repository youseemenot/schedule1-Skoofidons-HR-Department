using System.Reflection;
using Skoofidons.SharedSettings.V1;
using UnityEngine;
int checks=0,failed=0;
void Expect(bool value,string why){if(!value)throw new Exception(why);}
void Draw(Action action){GUI.InOnGUI=true;try{action();}finally{GUI.InOnGUI=false;}}
void Release()=> (typeof(MenuTheme).GetMethod("ReleaseResources",BindingFlags.Public|BindingFlags.Static)??throw new Exception("Explicit resource cleanup missing")).Invoke(null,null);
void Box(float radius=7)=>MenuTheme.Box(new Rect(0,0,200,50),Color.white,radius);
void Check(string name,Action action){checks++;try{action();Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+(e.InnerException??e).Message);}}
Check("rounded backgrounds remain visible across first world, menu unload, and subsequent worlds",()=>{
 Draw(()=>Box());var first=GUI.LastBackground;Expect(first!=null,"First world missing backdrop");
 Resources.UnloadUnusedAssets();Draw(()=>Box());Expect(GUI.LastBackground!=null,"Second world lost rounded backdrop");
 Resources.UnloadUnusedAssets();Draw(()=>Box());Expect(GUI.LastBackground!=null,"Third world lost rounded backdrop");
});
Check("same radius reuses one texture and style without per-frame allocation",()=>{
 Draw(()=>Box(14));var texture=GUI.LastBackground;var style=GUI.LastBoxStyle;int count=Texture2D.Generated;
 Draw(()=>{for(int i=0;i<100;i++)Box(14);});
 Expect(ReferenceEquals(texture,GUI.LastBackground)&&ReferenceEquals(style,GUI.LastBoxStyle)&&Texture2D.Generated==count,"Per-frame allocation or unstable cache");
});
Check("explicitly destroyed generated texture is rebuilt before the next draw",()=>{
 Draw(()=>Box(12));var old=GUI.LastBackground;UnityEngine.Object.Destroy(old);Draw(()=>Box(12));
 Expect(GUI.LastBackground!=null&&!ReferenceEquals(old,GUI.LastBackground),"Destroyed native texture stayed cached");
});
Check("a lost style background is rebound from the owned live texture",()=>{
 Draw(()=>Box(10));var old=GUI.LastBackground;GUI.InOnGUI=true;try{GUI.LastBoxStyle.normal.background=null;}finally{GUI.InOnGUI=false;}
 int count=Texture2D.Generated;Draw(()=>Box(10));
 Expect(GUI.LastBackground!=null&&ReferenceEquals(old,GUI.LastBackground)&&count==Texture2D.Generated,"Live owned texture lost or needlessly recreated");
});
Check("cleanup is idempotent outside OnGUI and never destroys borrowed white texture",()=>{
 Draw(()=>{Box(0);Box(17);MenuTheme.Label(new Rect(),"before cleanup");});
 Release();int destroyed=Texture2D.DestroyedGenerated;Release();
 Expect(Texture2D.LiveGenerated==0&&Texture2D.DestroyedGenerated==destroyed&&Texture2D.whiteTexture!=null,"Cleanup leaked resources, repeated destroy, or destroyed builtin");
 Draw(()=>{Box();Box(0);MenuTheme.Label(new Rect(),"after cleanup");});
 Expect(GUI.LastBackground==Texture2D.whiteTexture&&GUI.LastLabelStyle.font!=null,"Clean cache did not rebuild");
});
Check("style fonts refresh from the live skin after game skin replacement",()=>{
 Draw(()=>MenuTheme.Label(new Rect(),"first"));var firstStyle=GUI.LastLabelStyle;var firstFont=firstStyle.font;
 UnityEngine.Object.Destroy(firstFont);GUI.ReplaceSkin();Draw(()=>MenuTheme.Label(new Rect(),"second"));
 Expect(GUI.LastLabelStyle.font!=null&&!ReferenceEquals(firstStyle,GUI.LastLabelStyle)&&ReferenceEquals(GUI.LastLabelStyle.font,GUI.LiveSkin.label.font),"Stale font/style survived reload");
});
Check("destroyed font refreshes even if the skin object itself is retained",()=>{
 Draw(()=>MenuTheme.Label(new Rect(),"first"));UnityEngine.Object.Destroy(GUI.LastLabelStyle.font);
 GUI.InOnGUI=true;try{GUI.LiveSkin.label.font=new Font();}finally{GUI.InOnGUI=false;}
 Draw(()=>MenuTheme.Label(new Rect(),"second"));Expect(GUI.LastLabelStyle.font!=null&&ReferenceEquals(GUI.LastLabelStyle.font,GUI.LiveSkin.label.font),"Same-skin font replacement stayed stale");
});
Check("repeated world loads keep generated textures bounded by the existing radii",()=>{
 Release();for(int load=0;load<100;load++){Draw(()=>{foreach(int radius in new[]{0,2,7,10,12,14,17})Box(radius);});Resources.UnloadUnusedAssets();}
 Expect(Texture2D.LiveGenerated==6,"World loads leaked or lost per-radius textures");Release();
});
Console.WriteLine($"{checks-failed}/{checks} theme-resource checks passed (deterministic Unity model, not native gameplay)");return failed==0?0:1;
