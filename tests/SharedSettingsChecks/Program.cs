#nullable enable
using System.Reflection;
using System.Collections;
using System.Security.Cryptography;
using System.Runtime.Loader;

int checks = 0, failed = 0;
void Check(string name, Action test)
{
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + (error.InnerException ?? error).Message); }
}
void Expect(bool value, string why) { if (!value) throw new Exception(why); }
object Barrier() => Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType("Skoofidons.SharedSettings.V1.ReleaseBarrier")
    ?? throw new Exception("Release barrier is not implemented"))!;
object Call(object instance, string name, params object[] values) => instance.GetType().GetMethod(name)!.Invoke(instance, values)!;
bool Ready(object b, int frame, bool held = false, bool top = true, bool focused = true) =>
    (bool)Call(b, "Ready", frame, held, top, focused);

Check("closing never releases gameplay in the same input frame", () => {
    var b = Barrier(); Call(b, "Begin", 10);
    Expect(!Ready(b, 10), "Closing click can reach gameplay in the closing frame");
    Expect(Ready(b, 11), "Released input must allow cleanup on the next frame");
});
Check("held mouse or keyboard keeps the native lease until released", () => {
    var b = Barrier(); Call(b, "Begin", 10);
    Expect(!Ready(b, 11, held: true) && !Ready(b, 100, held: true), "Held input escaped the lease");
    Expect(Ready(b, 101), "Released input did not unlock cleanup");
});
Check("foreign top screen cannot be popped by deferred close", () => {
    var b = Barrier(); Call(b, "Begin", 1);
    Expect(!Ready(b, 2, top: false), "Foreign screen would be popped");
    Expect(Ready(b, 3), "Own returned screen cannot finish closing");
});
Check("focus loss does not wait forever for a missed key up", () => {
    var b = Barrier(); Call(b, "Begin", 1);
    Expect(Ready(b, 2, held: true, focused: false), "Lost focus left an invisible key barrier");
    Expect(!Ready(b, 2, held: true, top: false, focused: false), "Focus loss permits foreign pop");
});
Check("reset and repeated close are idempotent", () => {
    var b = Barrier();
    Expect(!Ready(b, 1), "An unopened session is releasable");
    Call(b, "Begin", 1); Call(b, "Begin", 2);
    Expect(Ready(b, 2), "Repeated close moved the barrier forward");
    Call(b, "Reset"); Call(b, "Reset");
    Expect(!Ready(b, 3), "Reset left pending native cleanup");
});

Type SharedType(string name) => Assembly.GetExecutingAssembly().GetType("Skoofidons.SharedSettings.V1." + name)
    ?? throw new Exception("V1 protocol is not implemented: " + name);
object Static(string type, string method, params object?[] values) => SharedType(type).GetMethod(method)!.Invoke(null, values)!;
Dictionary<string, object> Manifest(string id = "skoofidon.hr", string host = "1.0.0") => new() {
    ["contract"] = "Skoofidons.SettingsMenu/1", ["id"] = id, ["title"] = "Title " + id,
    ["providerVersion"] = "1.0.0", ["hostVersion"] = host, ["minimumHostVersion"] = "1.0.0",
    ["draw"] = (Action)(() => { }), ["opened"] = (Action)(() => { }), ["closed"] = (Action)(() => { })
};
object Parse(IDictionary<string, object> values) => Static("ProviderManifest", "Parse", values, Assembly.GetExecutingAssembly());
string Id(object provider) => (string)provider.GetType().GetProperty("Id")!.GetValue(provider)!;
object Elect(params object[] providers)
{
    var array = Array.CreateInstance(SharedType("ProviderManifest"), providers.Length);
    for (int i = 0; i < providers.Length; i++) array.SetValue(providers[i], i);
    return Static("HostElection", "Elect", (object)array);
}
void Reject(IDictionary<string, object> values)
{
    try { Parse(values); throw new Exception("Invalid provider was accepted"); }
    catch (TargetInvocationException error) when (error.InnerException is FormatException) { }
}
Check("standalone provider elects itself", () => Expect(Id(Elect(Parse(Manifest()))) == "skoofidon.hr", "Standalone mod has no host"));
Check("load order never changes host election", () => {
    var hr = Parse(Manifest()); var minimap = Parse(Manifest("skoofidon.minimap"));
    Expect(Id(Elect(hr, minimap)) == "skoofidon.hr" && Id(Elect(minimap, hr)) == "skoofidon.hr", "First updater won the election");
});
Check("highest compatible host serves old providers", () => {
    var hr = Parse(Manifest()); var third = Parse(Manifest("example.third", "1.1.0"));
    Expect(Id(Elect(hr, third)) == "example.third" && Id(Elect(third, hr)) == "example.third", "Old runtime ignored the newer V1 host");
});
Check("unknown fields are forward compatible", () => {
    var values = Manifest(); values["futureCapability"] = new object();
    Expect(Id(Parse(values)) == "skoofidon.hr", "Unknown metadata disabled an old provider");
});
Check("invalid contracts IDs versions and CLR callbacks are rejected", () => {
    foreach (var (key, value) in new (string, object)[] {
        ("contract", "Other/2"), ("id", ""), ("id", "../other"), ("title", " "),
        ("hostVersion", "NaN"), ("hostVersion", 1), ("minimumHostVersion", "2.0.0"),
        ("draw", "not an Action"), ("closed", (Func<bool>)(() => true))
    }) { var values = Manifest(); values[key] = value; Reject(values); }
    foreach (string key in new[] { "contract", "id", "title", "providerVersion", "hostVersion", "minimumHostVersion", "draw", "opened", "closed" })
    { var values = Manifest(); values.Remove(key); Reject(values); }
});
Check("local endpoint exposes callable CLR callbacks after initialization", () => {
    var endpoint = Assembly.GetExecutingAssembly().GetType("Skoofidons.SharedSettings.EntryPointV1")
        ?? throw new Exception("V1 endpoint is not implemented");
    int drawn = 0, opened = 0, closed = 0;
    endpoint.GetMethod("Initialize")!.Invoke(null, new object[] { "local.mod", "Local", new Version(1, 0, 0),
        (Action)(() => drawn++), (Action)(() => opened++), (Action)(() => closed++) });
    var values = (IDictionary<string, object>)endpoint.GetMethod("GetManifestV1")!.Invoke(null, null)!;
    ((Action)values["opened"])(); ((Action)values["draw"])(); ((Action)values["closed"])();
    Expect(drawn == 1 && opened == 1 && closed == 1, "Endpoint callbacks were lost");
});
Check("V1.1 viewport footer and language use only shared CLR delegates", () => {
    var endpoint = Assembly.GetExecutingAssembly().GetType("Skoofidons.SharedSettings.EntryPointV1")!;
    int calls=0;
    endpoint.GetMethod("ConfigurePanel")!.Invoke(null,new object[]{
        (Action<float,float,string>)((w,h,locale)=>{Expect(w==800&&h==600&&locale=="ru","Viewport or game language lost");calls++;}),
        (Func<string>)(()=>"status"),(Func<bool>)(()=>true),(Func<string>)(()=>null!)});
    var values=(IDictionary<string,object>)endpoint.GetMethod("GetManifestV1")!.Invoke(null,null)!;
    var p=Parse(values);
    ((Action<float,float,string>)p.GetType().GetProperty("Viewport")!.GetValue(p)!)(800,600,"ru");
    Expect(calls==1&&values["minimumHostVersion"].ToString()=="1.1.0","Incapable old host can own the panel");
    try {values["id"]="changed";throw new Exception("Manifest mutable");}catch(NotSupportedException){}
    var bad=Manifest();bad["viewport"]=(Action)(()=>{});Reject(bad);
});

if (args.Length > 0)
{
    Assembly Load(string name) => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(args[0], name, "Fixture." + name + ".dll")));
    var hr = Load("OldHr"); var minimap = Load("OldMinimap"); var third = Load("ThirdProvider"); var newer = Load("NewHost"); var broken = Load("Broken");
    object Discover(Assembly owner, Assembly[] participants, Action<string>? warning = null) =>
        (owner.GetType("Skoofidons.SharedSettings.V1.ProviderDiscovery") ?? throw new Exception("Fixture V1 discovery missing"))
            .GetMethod("Discover")!.Invoke(null, new object?[] { participants, warning })!;
    object Winner(Assembly owner, Assembly[] participants) =>
        owner.GetType("Skoofidons.SharedSettings.V1.HostElection")!.GetMethod("Elect")!.Invoke(null, new[] { Discover(owner, participants) })!;
    Check("two separate DLLs expose both same-named endpoints and invoke their own draw", () => {
        var providers = ((IEnumerable)Discover(hr, new[] { hr, minimap })).Cast<object>().ToArray();
        Expect(providers.Length == 2, "Type identity collapsed providers from separate DLLs");
        foreach (var provider in providers) ((Action)provider.GetType().GetProperty("Draw")!.GetValue(provider)!)();
        Expect((int)hr.GetType("Skoofidons.SharedSettings.EntryPointV1")!.GetField("DrawCalls")!.GetValue(null)! == 1 &&
            (int)minimap.GetType("Skoofidons.SharedSettings.EntryPointV1")!.GetField("DrawCalls")!.GetValue(null)! == 1, "Wrong assembly callback invoked");
    });
    Check("third DLL appears without rebuilding either old participant", () => {
        byte[] before = SHA256.HashData(File.ReadAllBytes(hr.Location));
        var list = ((IEnumerable)Discover(hr, new[] { hr, minimap, third })).Cast<object>().ToArray();
        Expect(list.Length == 3 && list.Any(p => Id(p) == "example.third"), "Third mod requires an old DLL update");
        Expect(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(hr.Location))), "Old DLL changed");
    });
    Check("all old runtimes independently elect the new compatible host", () => {
        foreach (var owner in new[] { hr, minimap, third })
            foreach (var participants in new[] { new[] { hr, minimap, newer }, new[] { newer, minimap, hr } })
                Expect(Id(Winner(owner, participants)) == "example.newhost", "An old runtime created its own window");
    });
    Check("duplicate IDs and invalid providers do not create duplicate panels", () => {
        var warnings = new List<string>();
        var list = ((IEnumerable)Discover(hr, new[] { hr, hr, minimap, broken }, warnings.Add)).Cast<object>().ToArray();
        Expect(list.Length == 2 && warnings.Count == 1, "Discovery failed to isolate broken/duplicate participants");
    });
    if (File.Exists(Path.Combine(args[0], "DuplicateHr", "Fixture.DuplicateHr.dll")))
    {
        var duplicate = Load("DuplicateHr");
        Check("different assemblies with the same provider ID select the newer provider once", () => {
            foreach (var participants in new[] { new[] { hr, duplicate }, new[] { duplicate, hr } })
            {
                var list = ((IEnumerable)Discover(hr, participants)).Cast<object>().ToArray();
                Expect(list.Length == 1 && (Assembly)list[0].GetType().GetProperty("Assembly")!.GetValue(list[0])! == duplicate,
                    "Duplicate ID silently selected a different panel depending on load order");
            }
        });
    }
}

Console.WriteLine($"{checks - failed}/{checks} shared-settings checks passed");
return failed == 0 ? 0 : 1;
