using System.Reflection;
using System.Runtime.Loader;

// Exercise the real mod's managed clipboard handlers without starting Unity.
// Native employee behavior still requires the singleplayer checklist.
if (args.Length < 3) throw new ArgumentException("Usage: SmokeChecks <mod.dll> <game directory> <interop directory>");
string[] search = { Path.Combine(args[1], "MelonLoader", "net6"), args[2], Path.Combine(args[1], "Mods") };
AssemblyLoadContext.Default.Resolving += (context, name) => {
    string file = search.Select(dir => Path.Combine(dir, name.Name + ".dll")).FirstOrDefault(File.Exists);
    return file == null ? null : context.LoadFromAssemblyPath(Path.GetFullPath(file));
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
var patches = assembly.GetType("SkoofidonsHRDepartment.GamePatches", throwOnError: true)!;
object Call(string name) {
    var method = patches.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;
    return method.Invoke(null, new object[method.GetParameters().Length])!;
}
void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
int failures = 0, checks = 0;
void Check(string name, Action test) {
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}

Call("MenuClosePostfix");
Check("gameplay scroll works with clipboard closed", () => Expect((bool)Call("DisableScrollWheel"), "Scroll blocked while closed."));
Call("MenuOpenPostfix");
// The legacy build reads Unity's InputValue while scrolling. Baseline mode
// isolates the Close regression without fabricating a Unity input context.
if (!args.Contains("--baseline"))
    Check("clipboard consumes gameplay scroll", () => Expect(!(bool)Call("DisableScrollWheel"), "Gameplay still receives clipboard scrolling."));
Check("opening clipboard does not veto its first close", () => {
    foreach (var method in patches.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
        bool prefix = method.CustomAttributes.Any(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPrefix");
        bool closes = method.CustomAttributes.Any(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch" && a.ConstructorArguments.Any(v => v.Value is string s && s == "Close"));
        if (prefix && closes && method.ReturnType == typeof(bool))
            Expect((bool)method.Invoke(null, null)!, "The mod vetoed Close immediately after Open.");
    }
});
Call("MenuClosePostfix");
Check("closing clipboard restores gameplay scroll", () => Expect((bool)Call("DisableScrollWheel"), "Scroll remained blocked after Close."));
Console.WriteLine($"{checks - failures}/{checks} checks passed. These checks do not execute Unity or native employee code.");
return failures == 0 ? 0 : 1;
