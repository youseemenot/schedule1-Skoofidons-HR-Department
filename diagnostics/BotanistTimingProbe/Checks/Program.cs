using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 2) throw new ArgumentException("Usage: ProbeChecks <probe.dll> <game directory>");
string[] paths = { Path.Combine(args[1], "MelonLoader", "net6"), Path.Combine(args[1], "MelonLoader", "Il2CppAssemblies") };
AssemblyLoadContext.Default.Resolving += (context, name) => {
    string file = paths.Select(p => Path.Combine(p, name.Name + ".dll")).FirstOrDefault(File.Exists);
    return file == null ? null : context.LoadFromAssemblyPath(file);
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
int checks = 0, failures = 0;
void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
void Check(string name, Action action) {
    checks++;
    try { action(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.GetBaseException().Message); }
}
object Call(string name, params object[] values) {
    var protocol = assembly.GetType("SkoofidonsHRDepartment.Diagnostics.ProbeProtocol");
    Expect(protocol != null, "Diagnostic protocol is missing.");
    return protocol!.GetMethod(name)!.Invoke(null, values)!;
}
Check("soil classified from native type", () => Expect((string)Call("Classify", "ScheduleOne.NPCs.Behaviour.AddSoilToGrowContainerBehaviour") == "soil", "Wrong action."));
Check("water classified independently of shared wrapper", () => Expect((string)Call("Classify", "ScheduleOne.NPCs.Behaviour.WaterPotBehaviour") == "water", "Wrong action."));
Check("additive classified independently of shared wrapper", () => Expect((string)Call("Classify", "ScheduleOne.NPCs.Behaviour.ApplyAdditiveToGrowContainerBehaviour") == "additive", "Wrong action."));
Check("fungal action is outside phase one", () => Expect((string)Call("Classify", "ScheduleOne.NPCs.Behaviour.ApplySpawnToMushroomBedBehaviour") == "other", "Fungal action included."));
Check("unknown action is outside phase one", () => Expect((string)Call("Classify", "Unknown") == "other", "Unknown action included."));
Check("seed is identified inside the contextual coroutine", () => Expect((string)Call("Classify", "ScheduleOne.NPCs.Behaviour.SowSeedInPotBehaviour") == "seed", "Wrong action."));
Check("harvest is identified inside the contextual coroutine", () => Expect((string)Call("Classify", "ScheduleOne.NPCs.Behaviour.HarvestPotBehaviour") == "harvest", "Wrong action."));
Check("only first yielding step starts an observation", () => Expect((bool)Call("IsFirstWait", 0, 1, true), "Initial wait missed."));
Check("subsequent frames do not duplicate start samples", () => Expect(!(bool)Call("IsFirstWait", 1, 1, true), "Observation repeated every frame."));
Check("a routine completed without yielding is not a waiting action", () => Expect(!(bool)Call("IsFirstWait", 0, -1, false), "Completed action treated as waiting."));
Check("a different game binary prevents activation", () => Expect(!(bool)Call("MatchesGame", new string('0', 128)), "Unknown game binary accepted."));
Check("audited game hash permits activation case-insensitively", () => Expect((bool)Call("MatchesGame", "872dfb646c9b866c53dae12d7a34e1f9df21a3e16fac6b7c6cd21bf5fc8e4b3f99932163c5de7e3798d37641712e82b914d46e1f023d56e77970be84fb74ec3a"), "Audited binary rejected."));
Check("diagnostic samples are bounded", () => Expect((bool)Call("ShouldLog", 0) && (bool)Call("ShouldLog", 7) && !(bool)Call("ShouldLog", 8) && !(bool)Call("ShouldLog", 100000), "Logging limit incorrect."));
Check("coroutine observer cannot replace or mutate the return value", () => {
    var observer = assembly.GetType("SkoofidonsHRDepartment.Diagnostics.ProbeMod")?.GetMethod("MoveNextPostfix", BindingFlags.NonPublic | BindingFlags.Static);
    Expect(observer != null, "Coroutine observer is missing.");
    Expect(observer!.ReturnType == typeof(void), "Observer replaces the original result.");
    var result = observer.GetParameters().Single(p => p.Name == "__result");
    Expect(result.ParameterType == typeof(bool) && !result.IsOut, "MoveNext result is writable by reference.");
    Expect(observer.GetParameters().Single(p => p.Name == "__instance").ParameterType.GetProperty("__4__this")?.PropertyType.Name == "GrowContainerBehaviour", "Observer does not capture the contextual coroutine.");
});
Check("probe has no automatic PatchAll declarations", () => {
    Expect(assembly.GetType("SkoofidonsHRDepartment.Diagnostics.ProbeMod") != null, "Probe is missing.");
    foreach (var type in assembly.GetTypes()) {
        Expect(!type.CustomAttributes.Any(a => a.AttributeType.Name == "HarmonyPatch"), "Automatic type patch found.");
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Expect(!method.CustomAttributes.Any(a => a.AttributeType.Name == "HarmonyPatch"), "Automatic method patch found.");
    }
});
Check("contextual MoveNext wrapper can be JIT-compiled offline", () => {
    var observer = assembly.GetType("SkoofidonsHRDepartment.Diagnostics.ProbeMod")?.GetMethod("MoveNextPostfix", BindingFlags.NonPublic | BindingFlags.Static);
    Expect(observer != null, "Coroutine observer is missing.");
    var routine = observer!.GetParameters().Single(p => p.Name == "__instance").ParameterType;
    var target = routine.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    Expect(target != null && target.ReturnType == typeof(bool), "Native target signature changed.");
    System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(target!.MethodHandle);
});
Console.WriteLine($"{checks-failures}/{checks} probe checks passed. Unity and native dispatch were not executed.");
return failures == 0 ? 0 : 1;
