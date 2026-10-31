using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

if (args.Length < 2) { Console.WriteLine("Usage: OptionalPhoneChecks <mod.dll> <game directory> [--with-phone]"); return 2; }
bool withPhone = args.Contains("--with-phone");
var paths = new[] { Path.Combine(args[1], "MelonLoader", "net6"), Path.Combine(args[1], "MelonLoader", "Il2CppAssemblies") };
int phoneRequests = 0;
AssemblyLoadContext.Default.Resolving += (context, name) => {
    if (!withPhone && name.Name!.Contains("Phone", StringComparison.OrdinalIgnoreCase)) { phoneRequests++; return null; }
    string file = paths.Select(p => Path.Combine(p, name.Name + ".dll")).FirstOrDefault(File.Exists);
    return file == null ? null : context.LoadFromAssemblyPath(Path.GetFullPath(file));
};
Assembly phone = withPhone ? AssemblyLoadContext.Default.LoadFromAssemblyPath(
    Path.GetFullPath(Path.Combine(args[1], "Mods", "ModManager&PhoneApp.dll"))) : null;
var mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
int checks = 0, failures = 0;
void Check(string name, Action test)
{
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failures++; Console.WriteLine("FAIL " + name + ": " + (error.InnerException ?? error).Message); }
}
void Expect(bool condition, string why) { if (!condition) throw new Exception(why); }
Check("HR metadata has no mandatory Phone App assembly reference", () =>
    Expect(!mod.GetReferencedAssemblies().Any(a => a.Name!.Contains("Phone", StringComparison.OrdinalIgnoreCase)), "Phone App is still referenced"));
Check("every HR type resolves with Phone App unavailable", () => Expect(mod.GetTypes().Length > 0, "No HR types"));
Check("main initialization path JIT-compiles without optional assembly resolution", () =>
    RuntimeHelpers.PrepareMethod(mod.GetType("SkoofidonsHRDepartment.Core")!.GetMethod("OnInitializeMelon")!.MethodHandle));
Check("integration can initialize and clean up with Phone App unavailable", () => {
    var integration = mod.GetType("SkoofidonsHRDepartment.ModManagerIntegration")!;
    integration.GetMethod("Initialize")!.Invoke(null, null);
    integration.GetMethod("Initialize")!.Invoke(null, null);
    integration.GetMethod("Deinitialize")!.Invoke(null, null);
    integration.GetMethod("Deinitialize")!.Invoke(null, null);
});
Check("ordinary settings service resolves without optional UI", () => {
    var service = mod.GetType("SkoofidonsHRDepartment.HrSettingsService") ?? throw new Exception("Settings application still belongs to Phone App");
    RuntimeHelpers.PrepareMethod(service.GetMethod("ApplyFromPreferences")!.MethodHandle);
});
Check("category ordering checks only the available optional selectors", () => {
    var integration = mod.GetType("SkoofidonsHRDepartment.PreferenceCategoryOrderIntegration")!;
    Expect((int)integration.GetMethod("VerifyManagedBindings")!.Invoke(null, null)! == (withPhone ? 3 : 0), "Optional selector availability differs");
});
Check("no integration requested the absent Phone App DLL", () => Expect(phoneRequests == 0, "CLR tried to resolve Phone App"));
if (withPhone) Check("actual Phone App 2.2.4 save events bind and unsubscribe", () => {
    int calls = 0;
    var type = phone.GetType("ModManagerPhoneApp.ModSettingsEvents")!;
    var helper = mod.GetType("SkoofidonsHRDepartment.OptionalSettingsEvents")!;
    var binding = (IDisposable)helper.GetMethod("TrySubscribe")!.Invoke(null, new object[] { type, (Action)(() => calls++), null })!;
    Expect(binding != null, "Current Phone App events were rejected");
    foreach (string name in new[] { "OnPhonePreferencesSaved", "OnMenuPreferencesSaved" })
        ((Action)type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Invoke();
    binding.Dispose(); binding.Dispose();
    Expect(calls == 2, "Phone/menu saves were duplicated or lost");
    Expect(new[] { "OnPhonePreferencesSaved", "OnMenuPreferencesSaved" }.All(name =>
        type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null) == null), "Real event subscription leaked");
});
Console.WriteLine($"{checks - failures}/{checks} optional-phone checks passed ({(withPhone ? "with" : "without")} Phone App)");
return failures == 0 ? 0 : 1;
