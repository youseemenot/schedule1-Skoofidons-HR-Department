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
    Check("UI parent lookup wrappers can be JIT-compiled without constraint violations", () => {
        foreach (var type in assembly.GetTypes())
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetMethodBody() == null) continue;
                foreach (var target in CalledMethods(method))
                    if (target.DeclaringType?.Namespace == "UnityEngine" && target.Name == "GetComponentInParent")
                        // Compile only: no Unity method or type initializer is
                        // executed. Exposes invalid generated Cast<T> IL that
                        // member-existence API checks cannot detect.
                        System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(target.MethodHandle);
            }
    });
}
if (args.Contains("--combined"))
{
    Check("all professions expose exactly movement and work speed preferences", () => {
        var config = assembly.GetType("SkoofidonsHRDepartment.Config")!;
        var expected = new[] { "PackagerWalkSpeed", "PackagerSpeed", "ChemistWalkSpeed", "ChemistWorkSpeed", "BotanistWalkSpeed", "BotanistWorkSpeed", "CleanerWalkSpeed", "CleanerWorkSpeed" };
        foreach (var name in expected)
            Expect(config.GetField(name, BindingFlags.Public | BindingFlags.Static) != null, "Speed preference missing: " + name);
        var actual = config.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.Name.Contains("Speed") || f.Name.Contains("Timing")).Select(f => f.Name).OrderBy(n => n);
        Expect(actual.SequenceEqual(expected.OrderBy(n => n)), "Extra speed controls or botanist enable switch remain.");
    });
    Check("compiled Botanist work rate uses the native employee work stack", () => {
        var controller = assembly.GetType("SkoofidonsHRDepartment.EmployeeSpeedController")!;
        var apply = controller.GetMethod("Apply")!;
        var fields = ReferencedMembers(apply).OfType<FieldInfo>().ToArray();
        Expect(fields.Any(f => f.DeclaringType?.Name == "Config" && f.Name == "BotanistWorkSpeed"), "Botanist work rate is not routed to native employee application.");
        Expect(CalledMethods(apply).Any(m => m.Name == "ApplyWorkSpeed"), "Native work stack application missing.");
    });
    Check("compiled Botanist multiplier has no second duration hook", () => {
        Expect(assembly.GetType("SkoofidonsHRDepartment.BotanistTimingController") == null, "Old contextual timing hook would divide an already scaled wait again.");
        Expect(assembly.GetType("SkoofidonsHRDepartment.BotanistTimingPolicy") == null, "Obsolete per-action duration path remains compiled.");
    });
    Check("compiled lifecycle wires work-rate changes and layout size tracking", () => {
        var core = assembly.GetType("SkoofidonsHRDepartment.Core")!;
        Expect(CalledMethods(core.GetMethod("OnUpdate")!).Any(m => m.DeclaringType?.Name == "ClipboardLayoutController" && m.Name == "RefreshTracked"), "Layout size tracking not wired.");
        var save = assembly.GetType("SkoofidonsHRDepartment.ModManagerIntegration")!.GetMethod("OnSettingsChanged", BindingFlags.NonPublic | BindingFlags.Static)!;
        Expect(CalledMethods(save).Any(m => m.DeclaringType?.Name == "EmployeeController" && m.Name == "ApplyToSpawnedEmployees"), "Saved work rates are not applied to live employees.");
        Expect(CalledMethods(core.GetMethod("OnDeinitializeMelon")!).Any(m => m.DeclaringType?.Name == "EmployeeSpeedController" && m.Name == "RestoreAndClear"), "Owned work multiplier cleanup is missing.");
        Expect(!CalledMethods(save).Any(m => m.DeclaringType?.Name == "BotanistTimingController"), "Save still registers old timing hook.");
    });
    Check("compiled layout budgets fit normal panels and scroll tiny panels", () => {
        var type = assembly.GetType("SkoofidonsHRDepartment.ClipboardLayoutPolicy");
        Expect(type != null, "Layout policy missing from main DLL.");
        var method = type!.GetMethod("Calculate")!;
        object Layout(float height) => method.Invoke(null, new object[] { height, 60f, new[] { 44f, 80f }, new[] { 500f, 500f } })!;
        var normal = Layout(480f);
        Expect(!(bool)normal.GetType().GetField("OuterScroll")!.GetValue(normal)!, "Normal panel unnecessarily uses outer scroll.");
        var sections = (Array)normal.GetType().GetField("Sections")!.GetValue(normal)!;
        var last = sections.GetValue(sections.Length - 1)!;
        float end = (float)last.GetType().GetField("ViewportTop")!.GetValue(last)! + (float)last.GetType().GetField("Height")!.GetValue(last)!;
        Expect(end <= 480f, "Last viewport extends beyond its panel.");
        var tiny = Layout(80f);
        Expect((bool)tiny.GetType().GetField("OuterScroll")!.GetValue(tiny)! && (float)tiny.GetType().GetField("ContentHeight")!.GetValue(tiny)! > 80f, "Tiny panel does not expose scrollable content.");
    });
}
if (args.Contains("--unified"))
{
    Check("machine and growth timers expose seven separate rate preferences", () => {
        var config = assembly.GetType("SkoofidonsHRDepartment.Config")!;
        var fields = config.GetFields(BindingFlags.Public | BindingFlags.Static);
        Expect(fields.Count(f => f.FieldType.Name == "MelonPreferences_Category") == 6, "Machine Timers category missing or duplicated.");
        foreach (var name in new[] { "MachineChemistryRate", "MachineOvenRate", "MachineMixingRate", "MachineCauldronRate", "MachineDryingRate", "MachinePlantRate", "MachineShroomRate" })
            Expect(fields.Any(f => f.Name == name && f.FieldType.IsGenericType && f.FieldType.GenericTypeArguments.Single() == typeof(float)), "Machine preference missing: " + name);
    });
    Check("machine timer lifecycle and settings preserve the new-operation boundary", () => {
        var controller = assembly.GetType("SkoofidonsHRDepartment.MachineTimerController");
        Expect(controller != null, "Machine timer controller missing.");
        var core = assembly.GetType("SkoofidonsHRDepartment.Core")!;
        Expect(CalledMethods(core.GetMethod("OnInitializeMelon")!).Any(m => m.DeclaringType == controller && m.Name == "Initialize"), "Machine initialization missing.");
        Expect(CalledMethods(core.GetMethod("OnDeinitializeMelon")!).Any(m => m.DeclaringType == controller && m.Name == "Deinitialize"), "Machine cleanup missing.");
        var save = assembly.GetType("SkoofidonsHRDepartment.ModManagerIntegration")!.GetMethod("OnSettingsChanged", BindingFlags.NonPublic | BindingFlags.Static)!;
        Expect(!CalledMethods(save).Any(m => m.DeclaringType == controller && m.Name == "ClearState"), "Preference save discards rates captured by running operations.");
    });
    Check("all guarded machine targets and injected callbacks resolve and JIT", () => {
        var controller = assembly.GetType("SkoofidonsHRDepartment.MachineTimerController");
        Expect(controller != null, "Machine timer controller missing.");
        Expect((int)controller!.GetMethod("VerifyManagedBindings")!.Invoke(null, null)! == 45, "Audited target set changed.");
    });
    Check("actual DLL preserves captured factors through save and load", () => {
        var ledgerType = assembly.GetType("SkoofidonsHRDepartment.MachineTimerLedger")!;
        var stateType = assembly.GetType("SkoofidonsHRDepartment.MachineTimerPersistence")!;
        var snapshotType = assembly.GetType("SkoofidonsHRDepartment.MachineTimerSnapshot")!;
        var kind = Enum.Parse(assembly.GetType("SkoofidonsHRDepartment.MachineTimerKind")!, "Oven");
        object Snapshot(string identity) => Activator.CreateInstance(snapshotType, new object[] { kind, "oven", identity, "aa63bc20-e2e3-44e9-b808-50cf94c270cd", "batch", 10d, 20, 0 })!;
        object State() => Activator.CreateInstance(stateType, Activator.CreateInstance(ledgerType))!;
        double Rate(object record) => (double)record.GetType().GetProperty("CapturedRate")!.GetValue(record)!;
        var state = State(); var snapshot = Snapshot("original");
        stateType.GetMethod("Capture")!.Invoke(state, new[] { snapshot, (object)7d, false });
        Expect(Rate(stateType.GetMethod("Capture")!.Invoke(state, new[] { snapshot, (object)2d, false })!) == 7d, "Existing factor was replaced by a preference change.");
        var snapshots = Array.CreateInstance(snapshotType, 1); snapshots.SetValue(snapshot, 0);
        string json = (string)stateType.GetMethod("Serialize")!.Invoke(state, new object[] { snapshots })!;
        var restored = State();
        object scope = stateType.GetMethod("BeginLoad")!.Invoke(restored, new object[] { kind, "{\"GUID\":\"aa63bc20-e2e3-44e9-b808-50cf94c270cd\"}", json })!;
        object record = stateType.GetMethod("Capture")!.Invoke(restored, new[] { Snapshot("restored"), (object)2d, false })!;
        Expect(Rate(record) == 7d, "Saved factor was not restored.");
        stateType.GetMethod("EndLoad")!.Invoke(restored, new[] { scope, (object)false });
    });
    Check("growth rate lookup cannot preempt asynchronous saved-rate restoration", () => {
        var controller = assembly.GetType("SkoofidonsHRDepartment.MachineTimerController")!;
        controller.GetMethod("ClearState")!.Invoke(null, null);
        var kind = Enum.Parse(assembly.GetType("SkoofidonsHRDepartment.MachineTimerKind")!, "PlantGrowth");
        var lookup = controller.GetMethod("GrowthRate", BindingFlags.NonPublic | BindingFlags.Static)!;
        Expect((double)lookup.Invoke(null, new object[] { kind, new IntPtr(0x12345) })! == 1d, "Unknown growth must stay native.");
        var ledger = controller.GetField("Ledger", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        object[] input = { "PlantGrowth:12345", null! };
        Expect(!(bool)ledger.GetType().GetMethod("TryGet")!.Invoke(ledger, input)!, "A read-only growth lookup adopted rate1 before async restore.");
    });
    Check("drying capture targets the reached broadcast path across serialized identities", () => {
        var controller = assembly.GetType("SkoofidonsHRDepartment.MachineTimerController")!;
        var specs = (Array)controller.GetField("PatchSpecs", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        string Name(object spec) => (string)spec.GetType().GetProperty("Name")!.GetValue(spec)!;
        Expect(specs.Cast<object>().Any(s => Name(s) == "PleaseReceiveOp"), "Reached server broadcast method is not hooked.");
        Expect(!specs.Cast<object>().Any(s => Name(s) == "RpcLogic___SendOperation_1307702229"), "Drying capture still relies on the inlined/bypassed send logic.");
        Expect(assembly.GetType("SkoofidonsHRDepartment.MachineDryingCaptureQueue") != null, "Serialized receiver cannot recover its captured factor.");
    });
    Check("proven new plants and colonies discard recycled pointer records", () => {
        var controller = assembly.GetType("SkoofidonsHRDepartment.MachineTimerController")!;
        foreach (var name in new[] { "PotSeedPostfix", "ColonyCreatePostfix" }) {
            var calls = CalledMethods(controller.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!).ToList();
            int forget = calls.FindIndex(m => m.DeclaringType?.Name == "MachineTimerLedger" && m.Name == "Forget");
            int capture = calls.FindIndex(m => m.Name == "Capture");
            Expect(forget >= 0 && capture > forget, "New crop can retain the previous occupant's factor: " + name);
        }
    });
    Check("compiled movement multiplier scales speed and cap sevenfold", () => {
        var policy = assembly.GetType("SkoofidonsHRDepartment.EmployeeSpeedPolicy");
        Expect(policy != null, "Normalized employee speed policy missing.");
        var values = new object[] { 1.8f, 7f, 7f, 0f, 0f };
        Expect((bool)policy!.GetMethod("TryScaleMovement")!.Invoke(null, values)!, "Sevenfold movement rejected.");
        Expect(Math.Abs((float)values[3] - 12.6f) < .0001f && (float)values[4] == 49f, "Movement remained raw/capped at7 instead of sevenfold.");
    });
    Check("compiled movement state never compounds on preference saves", () => {
        var definition = assembly.GetType("SkoofidonsHRDepartment.EmployeeMovementSpeedState`1");
        Expect(definition != null, "Movement baseline state missing.");
        var type = definition!.MakeGenericType(typeof(object));
        var state = Activator.CreateInstance(type)!;
        var controller = new object();
        float speed = 1.8f, cap = 7f;
        Action<object, float, float> write = (_, s, c) => { speed = s; cap = c; };
        void Apply(float factor) => type.GetMethod("Apply")!.Invoke(state, new object[] { new IntPtr(1), controller, speed, cap, factor, write });
        Apply(7f); Apply(7f);
        Expect(Math.Abs(speed - 12.6f) < .0001f && cap == 49f, "Repeated save multiplied an already modified speed.");
        Apply(1f);
        Expect(speed == 1.8f && cap == 7f, "One did not restore native movement values.");
    });
    Check("compiled Cleaner policy shortens only Cleaner waits", () => {
        var type = assembly.GetType("SkoofidonsHRDepartment.CleanerSpeedPolicy");
        Expect(type != null, "Cleaner action speed policy missing.");
        var values = new object[] { 14f, 7f, 0f };
        Expect((bool)type!.GetMethod("TryScaleDuration")!.Invoke(null, values)! && (float)values[2] == 2f, "Larger Cleaner factor does not shorten wait.");
        Expect(!(bool)type.GetMethod("IsEligibleYield")!.Invoke(null, new object[] { true, "ScheduleOne.Employees.Botanist", true })!, "Cleaner multiplier applies to another profession.");
    });
    Check("compiled Cleaner wrappers JIT and lifecycle integration are present", () => {
        var type = assembly.GetType("SkoofidonsHRDepartment.CleanerActionSpeedController");
        Expect(type != null, "Cleaner action controller missing.");
        var callbacks = type!.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name.EndsWith("Postfix")).ToArray();
        Expect(callbacks.Length == 5, "Cleaner must cover the five audited contextual routines.");
        foreach (var callback in callbacks) {
            var routine = callback.GetParameters().Single(p => p.Name == "__instance").ParameterType;
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(routine.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.MethodHandle);
        }
        var core = assembly.GetType("SkoofidonsHRDepartment.Core")!;
        Expect(CalledMethods(core.GetMethod("OnInitializeMelon")!).Any(m => m.DeclaringType?.Name == "CleanerActionSpeedController"), "Cleaner initialization missing.");
        var save = assembly.GetType("SkoofidonsHRDepartment.ModManagerIntegration")!.GetMethod("OnSettingsChanged", BindingFlags.NonPublic | BindingFlags.Static)!;
        Expect(CalledMethods(save).Any(m => m.DeclaringType?.Name == "CleanerActionSpeedController"), "Cleaner setting save missing.");
        var apply = assembly.GetType("SkoofidonsHRDepartment.EmployeeController")!.GetMethod("ApplyStats")!;
        Expect(CalledMethods(apply).Any(m => m.DeclaringType?.Name == "EmployeeSpeedController" && m.Name == "Apply"), "Employee work/movement multiplier wiring missing.");
        Expect(!CalledMethods(apply).Any(m => m.Name == "set_PackagingSpeedMultiplier"), "Handler multiplier is applied twice through both fields and work stack.");
    });
}
Console.WriteLine($"{checks - failures}/{checks} checks passed. These checks do not execute Unity or native employee code.");
return failures == 0 ? 0 : 1;

static IEnumerable<MethodBase> CalledMethods(MethodInfo method) => ReferencedMembers(method).OfType<MethodBase>();

static IEnumerable<MemberInfo> ReferencedMembers(MethodInfo method)
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
            yield return method.Module.ResolveMethod(BitConverter.ToInt32(il, offset),
                method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments())!;
        else if (operand == OperandType.InlineField)
            yield return method.Module.ResolveField(BitConverter.ToInt32(il, offset),
                method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments())!;
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
