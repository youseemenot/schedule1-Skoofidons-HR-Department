using System.Reflection;
using System.Reflection.Emit;
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
if (!args.Contains("--baseline"))
{
    object Snapshot(int fields, int used, int limit)
    {
        var type = assembly.GetType("SkoofidonsHRDepartment.RouteCounterSnapshot");
        Expect(type != null, "Route counter is not implemented.");
        return type!.GetMethod("ForSelection")!.Invoke(null, new object[] { fields, used, limit })!;
    }
    int Number(object value, string property) => (int)value.GetType().GetProperty(property)!.GetValue(value)!;
    bool CanAdd(object value) => (bool)value.GetType().GetProperty("CanAdd")!.GetValue(value)!;
    string Label(object value, string caption) => (string)value.GetType().GetMethod("FormatLabel")!.Invoke(value, new object[] { caption })!;

    Check("empty Handler shows all route slots available", () => {
        var count = Snapshot(1, 0, 5);
        Expect(Number(count, "Available") == 5 && CanAdd(count), "Empty list must allow five slots.");
        Expect(Label(count, "Routes") == "Routes (0/5; free 5)", "Empty counter is incorrect.");
    });
    Check("partly used Handler reports remaining slots", () => {
        var count = Snapshot(1, 2, 5);
        Expect(Number(count, "Available") == 3 && CanAdd(count), "Two routes must leave three free slots.");
        Expect(Label(count, "Routes") == "Routes (2/5; free 3)", "Partial counter is incorrect.");
    });
    Check("full route list cannot add another route", () => {
        var count = Snapshot(1, 5, 5);
        Expect(Number(count, "Available") == 0 && !CanAdd(count), "Full list must reject additions.");
    });
    Check("reducing limit preserves over-limit route count", () => {
        var count = Snapshot(1, 6, 3);
        Expect(Number(count, "Used") == 6 && Number(count, "Available") == 0 && !CanAdd(count), "Existing routes must not be hidden or create negative availability.");
        Expect(Label(count, "Routes") == "Routes (6/3; free 0)", "Over-limit label must preserve actual count.");
    });
    Check("multiple employees do not inherit the first employee counter", () => Expect(Snapshot(2, 2, 5) == null, "Multi-selection must keep the native display."));
    Check("unbound UI has no employee counter", () => Expect(Snapshot(0, 2, 5) == null, "Unbound UI must keep the native display."));
    Check("invalid negative limit cannot create available slots", () => {
        var count = Snapshot(1, 2, -1);
        Expect(Number(count, "Limit") == 0 && Number(count, "Available") == 0 && !CanAdd(count), "Negative limit must not allow additions.");
    });
    Check("counter preserves the supplied game heading", () => Expect(Label(Snapshot(1, 2, 5), "Маршруты") == "Маршруты (2/5; free 3)", "Game heading was replaced with an English lookup string."));
    Check("route counter leaves button interactivity to the native UI", () => {
        var refresh = assembly.GetType("SkoofidonsHRDepartment.RouteCounterController")!.GetMethod("Refresh")!;
        // Native Refresh hides/shows AddButton.gameObject but never restores
        // Selectable.interactable. A controller write would remain sticky when
        // a route is removed. Check the real adapter's external API boundary.
        foreach (var target in CalledMethods(refresh))
            Expect(!(target.Name == "set_interactable" && target.DeclaringType?.Namespace == "UnityEngine.UI"), "Counter writes a button state the game's visibility refresh cannot restore.");
    });
    Check("Handler limits update the live station selection field", () => {
        var apply = assembly.GetType("SkoofidonsHRDepartment.EmployeeController")!.GetMethod("ApplyHandlerLimits");
        Expect(apply != null, "Handler limit adapter is missing.");
        var calls = CalledMethods(apply!).ToArray();
        Expect(calls.Any(m => m.Name == "get_Stations" && m.DeclaringType?.Name == "PackagerConfiguration"), "Live Handler Stations field is not accessed.");
        Expect(calls.Any(m => m.Name == "set_MaxItems" && m.DeclaringType?.Name == "ObjectListField"), "Live station limit is not updated.");
    });
}
Console.WriteLine($"{checks - failures}/{checks} checks passed. These checks do not execute Unity or native employee code.");
return failures == 0 ? 0 : 1;

static IEnumerable<MethodBase> CalledMethods(MethodInfo method)
{
    var codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(code => unchecked((ushort)code.Value));
    var il = method.GetMethodBody()!.GetILAsByteArray()!;
    for (int offset = 0; offset < il.Length;)
    {
        ushort value = il[offset++];
        if (value == 0xfe) value = (ushort)(0xfe00 | il[offset++]);
        var operand = codes[value].OperandType;
        if (operand == OperandType.InlineMethod)
            yield return method.Module.ResolveMethod(BitConverter.ToInt32(il, offset))!;
        offset += operand switch {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
            _ => 4
        };
    }
}
