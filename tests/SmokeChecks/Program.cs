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
    int[] ExpandRows(int[] rows, int assigned, Func<int, int, int> create, Action<int> destroy)
    {
        var type = assembly.GetType("SkoofidonsHRDepartment.StationRowExpansion");
        Expect(type != null, "Station row expansion is not implemented.");
        return (int[])type!.GetMethod("Expand")!.MakeGenericMethod(typeof(int))
            .Invoke(null, new object[] { rows, assigned, create, destroy })!;
    }
    Check("fifteen assigned stations retain all ten native rows and create five indexed rows", () => {
        var original = Enumerable.Range(100, 10).ToArray();
        var indexes = new List<int>();
        var rows = ExpandRows(original, 15, (template, index) => {
            Expect(template == 100, "Wrong template row."); indexes.Add(index); return 100 + index;
        }, _ => throw new Exception("Successful expansion destroyed a row."));
        Expect(rows.SequenceEqual(Enumerable.Range(100, 15)), "Station rows were truncated or reordered.");
        Expect(indexes.SequenceEqual(Enumerable.Range(10, 5)), "New rows do not match their actual station indexes.");
        Expect(original.SequenceEqual(Enumerable.Range(100, 10)), "Original array was mutated.");
    });
    Check("lower station occupancy never discards expanded rows", () => {
        var rows = Enumerable.Range(0, 15).ToArray();
        Expect(ReferenceEquals(rows, ExpandRows(rows, 3, (_, _) => throw new Exception("Unexpected allocation."), _ => throw new Exception("Unexpected deletion."))), "Expanded rows were replaced on lower occupancy.");
    });
    Check("repeated station refresh does not allocate duplicate rows", () => {
        var rows = Enumerable.Range(0, 15).ToArray();
        Expect(ReferenceEquals(rows, ExpandRows(rows, 15, (_, _) => throw new Exception("Duplicate row."), _ => { })), "Same occupancy allocated again.");
    });
    Check("failed station expansion rolls back only newly created resources", () => {
        var original = Enumerable.Range(0, 10).ToArray();
        var destroyed = new List<int>();
        bool failed = false;
        try { ExpandRows(original, 13, (_, index) => index == 12 ? throw new InvalidOperationException("clone failed") : index, destroyed.Add); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { failed = true; }
        Expect(failed, "Factory failure was swallowed.");
        Expect(destroyed.OrderBy(i => i).SequenceEqual(new[] { 10, 11 }), "Rollback destroyed original rows or leaked a clone.");
        Expect(original.SequenceEqual(Enumerable.Range(0, 10)), "Failure changed the original rows.");
    });
    Check("missing station template fails before any resource is created", () => {
        bool failed = false;
        try { ExpandRows(Array.Empty<int>(), 1, (_, _) => throw new Exception("Invalid factory call."), _ => { }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { failed = true; }
        Expect(failed, "Missing template did not fail safely.");
    });
    Check("station visibility restores every assigned row and hides unused rows", () => {
        var visibility = assembly.GetType("SkoofidonsHRDepartment.StationRowExpansion")?.GetMethod("SetAssignedVisibility");
        Expect(visibility != null, "Explicit root visibility handling is missing.");
        var rows = Enumerable.Range(0, 19).ToArray();
        var visible = new bool[19]; // Includes inactive clones/native-hidden roots.
        Action<int, bool> setVisible = (index, active) => visible[index] = active;
        visibility!.MakeGenericMethod(typeof(int)).Invoke(null, new object[] { rows, 15, setVisible });
        Expect(visible.SequenceEqual(rows.Select(i => i < 15)), "Assignments above ten stayed hidden or unused rows filled the viewport.");
        visibility.MakeGenericMethod(typeof(int)).Invoke(null, new object[] { rows, 3, setVisible });
        Expect(visible.SequenceEqual(rows.Select(i => i < 3)), "Removing assignments left visible stale rows.");
        Expect(rows.Length == 19, "Visibility discarded allocated rows.");
    });
    Check("station refresh applies the tested row visibility policy", () => {
        var refresh = assembly.GetType("SkoofidonsHRDepartment.HandlerStationController")?.GetMethod("RefreshLayout");
        Expect(refresh != null && CalledMethods(refresh).Any(m => m.Name == "SetAssignedVisibility" && m.DeclaringType?.Name == "StationRowExpansion"), "Unity adapter does not apply the tested visibility policy.");
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
