using System.Reflection;
using Skoofidons.SharedSettings.V1;

// Reflection during RED allows an absent implementation to be a test failure, not a compiler error.
int checks = 0, failures = 0;
void Expect(bool x, string why) { if (!x) throw new Exception(why); }
void Check(string name, Action test) { checks++; try { test(); Console.WriteLine("PASS " + name); } catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e).Message); } }
Type SessionType() => typeof(Program).Assembly.GetType("Skoofidons.SharedSettings.V1.MenuSession") ?? throw new Exception("Menu session missing");

if (typeof(Program).Assembly.GetType("Skoofidons.SharedSettings.V1.MenuSession") == null)
{
    Check("one physical toggle cannot open and close in the same frame", () => SessionType());
    Check("provider callbacks survive tab switches exactly once", () => SessionType());
    Check("held input and foreign top defer native release", () => SessionType());
    Check("render failure releases native ownership and isolates provider", () => SessionType());
}
#if SESSION_IMPLEMENTED
ProviderManifest Provider(string id, Action open = null, Action close = null, Action draw = null) => ProviderManifest.Parse(new Dictionary<string, object> {
    ["contract"]=ProviderManifest.Contract,["id"]=id,["title"]=id,["providerVersion"]="1.0.0",["hostVersion"]="1.0.0",["minimumHostVersion"]="1.0.0",
    ["opened"]=open ?? (()=>{}),["closed"]=close ?? (()=>{}),["draw"]=draw ?? (()=>{})
},typeof(Program).Assembly);
Check("one physical toggle cannot open and close in the same frame", () => {
    var lease=new Lease();var s=new MenuSession(new[]{Provider("a")},()=>lease);
    s.Toggle(1);s.Toggle(1);Expect(s.Visible && !s.Closing,"Same-frame duplicate closed the menu");
    s.Toggle(2);s.Tick(2,false,true);Expect(lease.Releases==0,"Closing frame released input");
    s.Tick(3,true,true);Expect(lease.Releases==0,"Held input escaped");
    s.Tick(4,false,true);Expect(!s.HasLease && lease.Releases==1,"Released input did not close");
});
Check("provider callbacks survive tab switches exactly once", () => {
    int a=0,b=0,ac=0,bc=0;var s=new MenuSession(new[]{Provider("a",()=>a++,()=>ac++),Provider("b",()=>b++,()=>bc++)},()=>new Lease());
    s.Toggle(1);s.Select("b",2);s.Select("a",3);s.Select("b",4);
    Expect(a==1&&b==1&&ac==0&&bc==0,"Switching tabs reset a provider's draft");
    s.RequestClose(5);s.Tick(6,false,true);s.Abort(7);
    Expect(ac==1&&bc==1,"Visited provider did not close exactly once");
});
Check("foreign top defers normal close but emergency removes only owned lease", () => {
    var lease=new Lease();var s=new MenuSession(new[]{Provider("a")},()=>lease);s.Toggle(1);lease.Top=false;
    Expect(!s.Visible,"Covered menu still receives input");s.RequestClose(2);s.Tick(3,false,true);
    Expect(lease.Releases==0,"Normal close popped a foreign top");
    s.Abort(4);Expect(lease.Releases==1&&!s.HasLease,"Scene teardown left an owned lease");
});
Check("failed acquisition calls no provider", () => {
    int calls=0;var s=new MenuSession(new[]{Provider("a",()=>calls++)},()=>throw new Exception("No native state"));
    s.Toggle(1);Expect(!s.HasLease&&calls==0&&s.Error!=null,"Unprotected panel opened");
});
Check("render failure releases native ownership and isolates provider", () => {
    var lease=new Lease();var s=new MenuSession(new[]{Provider("a",draw:()=>throw new Exception("Bad layout")),Provider("b")},()=>lease);
    s.Toggle(1);var result=s.GetType().GetMethod("Draw").Invoke(s,new object[]{2});
    Expect(result is bool ok&&!ok,"Host cannot stop damaged GUI event");
    s.Tick(3,false,true);Expect(!s.HasLease,"Faulty layout retained ownership");
    s.Toggle(4);Expect(s.Selected.Id=="b","Broken provider was reopened");
});
Check("reopening remembers selected provider",()=>{
    var s=new MenuSession(new[]{Provider("a"),Provider("b")},()=>new Lease());
    s.Toggle(1);s.Select("b",2);s.RequestClose(3);s.Tick(4,false,true);s.Toggle(5);
    Expect(s.Selected.Id=="b","Reopening reset selected mod");
});
Check("shell initialization failure does not disable healthy providers",()=>{
    var s=new MenuSession(new[]{Provider("a"),Provider("b")},()=>new Lease());s.Toggle(1);
    var fail=s.GetType().GetMethod("FailShell")??throw new Exception("Shell failures are attributed to active provider");
    fail.Invoke(s,new object[]{new NotSupportedException("Font unavailable"),2});
    s.Tick(3,true,true);Expect(s.HasLease,"Shell failure bypassed held-input barrier");
    s.Tick(4,false,true);s.Toggle(5);Expect(s.Selected.Id=="a"&&s.Visible,"Healthy provider disabled by shell failure");
});
Check("native ExitGUI is contained at managed Melon event boundary",()=>{
    var type=typeof(Program).Assembly.GetType("Skoofidons.SharedSettings.V1.GuiEventBoundary")??throw new Exception("Native ExitGUI leaks into Melon error log");
    var end=type.GetMethod("End");
    end.Invoke(null,new object[]{(Action)(()=>throw new ExitGUIException())});
    try{end.Invoke(null,new object[]{(Action)(()=>throw new InvalidOperationException("Other failure"))});throw new Exception("Unexpected failure swallowed");}
    catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){}
});
Check("non-draw provider callback failure still isolates that provider",()=>{
    var s=new MenuSession(new[]{Provider("a"),Provider("b")},()=>new Lease());s.Toggle(1);
    var method=s.GetType().GetMethod("TryProviderCall")??throw new Exception("Footer and viewport faults are treated as shell failures");
    var arguments=new object[]{(Func<string>)(()=>throw new Exception("Bad footer")),2,null};
    Expect(!(bool)method.MakeGenericMethod(typeof(string)).Invoke(s,arguments),"Callback failure ignored");
    s.Tick(3,false,true);s.Toggle(4);Expect(s.Selected.Id=="b","Broken callback blocks healthy panel");
});
Check("ordinary ExitGUI abort is not a provider fault",()=>{
    var s=new MenuSession(new[]{Provider("a",draw:()=>throw new ExitGUIException())},()=>new Lease());s.Toggle(1);
    try{s.Draw(2);throw new Exception("ExitGUI swallowed");}catch(ExitGUIException){}
    Expect(s.Visible&&!s.Closing,"Normal GUI exit disabled provider");
});
Check("closed callback failure cannot suppress lease release", () => {
    var lease=new Lease();var s=new MenuSession(new[]{Provider("a",close:()=>throw new Exception("Save failed"))},()=>lease);
    s.Toggle(1);s.RequestClose(2);s.Tick(3,false,true);Expect(!s.HasLease&&lease.Releases==1,"Close callback stranded input");
});
Check("cleanup failure retries without repeating provider close", () => {
    int closes=0;var lease=new Lease{FailOnce=true};var s=new MenuSession(new[]{Provider("a",close:()=>closes++)},()=>lease);
    s.Toggle(1);s.RequestClose(2);s.Tick(3,false,true);Expect(s.HasLease,"Failed cleanup was forgotten");
    s.Tick(4,false,true);Expect(!s.HasLease&&closes==1&&lease.Releases==2,"Retry duplicated close or stopped");
});
Check("owner destroyed and focus loss are both recoverable", () => {
    var lease=new Lease();var s=new MenuSession(new[]{Provider("a")},()=>lease);s.Toggle(1);s.Tick(2,true,false);s.Tick(3,true,false);
    Expect(!s.HasLease,"Focus loss waits indefinitely for key up");
    var dead=new Lease();s=new MenuSession(new[]{Provider("a")},()=>dead);s.Toggle(4);dead.Alive=false;s.Tick(5,false,true);
    Expect(!s.HasLease,"Destroyed scene remained in session");
});
dynamic HostLifecycle()=>Activator.CreateInstance(typeof(Program).Assembly.GetType("Skoofidons.SharedSettings.V1.MenuHostLifecycle")??throw new Exception("Restartable host lifecycle missing"));
Check("initialize shutdown initialize creates a live new host",()=>{
 dynamic host=HostLifecycle();Expect(host.Prepare(1),"Initial preparation failed");
 var first=new MenuSession(new[]{Provider("a")},()=>new Lease());host.Activate(first);first.Toggle(1);
 Expect(!host.Prepare(2),"Live initialization was duplicated");
 host.Stop(3);Expect(!host.IsHost&&host.Session==null,"Stopped host retained dead session");
 Expect(host.Prepare(4),"Restart preparation skipped");var second=new MenuSession(new[]{Provider("a")},()=>new Lease());host.Activate(second);second.Toggle(4);
 Expect(host.IsHost&&second.Visible,"Restarted host stays disabled");
});
Check("restart retains failed cleanup and refuses to forget ownership",()=>{
 dynamic host=HostLifecycle();host.Prepare(1);var lease=new Lease{Failures=2};var s=new MenuSession(new[]{Provider("a")},()=>lease);host.Activate(s);s.Toggle(1);
 host.Stop(2);Expect(!host.IsHost&&ReferenceEquals((object)host.Session,s)&&s.HasLease,"Pending lease discarded");
 try{host.Prepare(3);throw new Exception("Unfinished cleanup allowed a new host");}catch(InvalidOperationException){}
 Expect(ReferenceEquals((object)host.Session,s),"Failed restart lost original lease");Expect(host.Prepare(4)&&host.Session==null&&!s.HasLease,"Cleanup did not permit subsequent restart");
});
Check("failure after session publication can stop and retry",()=>{
 dynamic host=HostLifecycle();host.Prepare(1);host.Activate(new MenuSession(new[]{Provider("a")},()=>new Lease()));
 host.Stop(2);Expect(host.Prepare(3),"Partial bootstrap failure cannot retry");host.Activate(new MenuSession(new[]{Provider("a")},()=>new Lease()));Expect(host.IsHost,"Retry not activated");
});
#endif
Console.WriteLine($"{checks-failures}/{checks} menu-session checks passed");
return failures==0?0:1;
#if SESSION_IMPLEMENTED
sealed class Lease : IMenuInputLease
{
    public bool Alive=true,Top=true,FailOnce;public int Releases,Failures;
    public bool IsAlive=>Alive; public bool IsTop=>Top;
    public bool TryRelease(out string error){Releases++;if(FailOnce||Failures>0){FailOnce=false;Failures=Math.Max(0,Failures-1);error="Transient";return false;}error=null;return true;}
}
sealed class ExitGUIException : Exception {}
#endif
