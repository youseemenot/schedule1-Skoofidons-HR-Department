using System.Reflection;

// These checks exercise the real pure policy without loading any game assembly.
int passed = 0, failed = 0;
Type Policy() => Assembly.GetExecutingAssembly().GetType("SkoofidonsHRDepartment.BotanistTimingPolicy")
    ?? throw new InvalidOperationException("Timing policy is not implemented.");
object Action(string action) => Enum.Parse(Assembly.GetExecutingAssembly().GetType("SkoofidonsHRDepartment.TimingAction")!, action);
object Call(string method, params object[] args) => Policy().GetMethod(method)!.Invoke(null, args)!;
float Resolve(string action, float original, float multiplier, bool enabled = true) => (float)Call("Resolve", Action(action), original, multiplier, enabled);
void Equal(float actual, float expected) {
    if (float.IsNaN(expected) ? !float.IsNaN(actual) : actual != expected)
        throw new Exception($"Expected {expected}, got {actual}.");
}
void Check(string name, System.Action check) {
    try { check(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + (ex.InnerException ?? ex).Message); }
}
void Expect(bool value) { if (!value) throw new Exception("Guard accepted an unsafe transition or rejected a valid one."); }
void Wait(string action, float wait, float speed, float multiplier, bool enabled, bool expectedSuccess, float expectedWait) {
    object[] args = { Action(action), wait, speed, multiplier, enabled, 0f };
    Expect((bool)Call("TryResolveWait", args) == expectedSuccess);
    Equal((float)args[5], expectedWait);
}

string[] actions = { "Soil", "Water", "Additive", "Seed", "Harvest" };
foreach (string action in actions) {
    Check(action + " multiplier one preserves exact game duration", () => {
        foreach (float duration in new[] { 0f, -0f, float.Epsilon, 15.125f, float.MaxValue })
            Expect(BitConverter.SingleToInt32Bits(Resolve(action, duration, 1f)) == BitConverter.SingleToInt32Bits(duration));
    });
    Check(action + " multiplier seven is seven times faster", () => Equal(Resolve(action, 14f, 7f), 2f));
    Check(action + " multiplier half is half speed", () => Equal(Resolve(action, 15f, .5f), 30f));
}
Check("harvest multiplier preserves quantity relationship", () => {
    Equal(Resolve("Harvest", 14f, 7f), 2f);
    Equal(Resolve("Harvest", 28f, 7f), 4f);
});
Check("empty harvest remains zero", () => Equal(Resolve("Harvest", 0f, .5f), 0f));
Check("small positive multiplier is not clamped", () => {
    Equal(Resolve("Soil", 10f, .01f), 1000f);
    Equal(Resolve("Harvest", 15f, .01f), 1500f);
});
Check("disabled feature preserves game duration", () => Equal(Resolve("Soil", 10f, 7f, false), 10f));
Check("unknown actions preserve game duration", () => { Equal(Resolve("Unknown", 15f, 7f), 15f); Equal(Resolve("999", 15f, 7f), 15f); });
Check("invalid multipliers preserve duration", () => { foreach (float value in new[] { 0f, -3f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Equal(Resolve("Soil", 10f, value), 10f); });
Check("invalid game durations are preserved", () => { foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f }) Equal(Resolve("Harvest", value, .5f), value); });
Check("division overflow preserves original duration", () => Equal(Resolve("Harvest", float.MaxValue, .5f), float.MaxValue));
Check("division underflow preserves a positive game duration", () => Equal(Resolve("Seed", float.Epsilon, 2f), float.Epsilon));
Check("finite subnormal results are allowed", () => Equal(Resolve("Soil", float.Epsilon * 2f, 2f), float.Epsilon));
Check("native kinds match only the five audited actions", () => {
    foreach (var entry in new[] { ("AddSoilToGrowContainerBehaviour", "Soil"), ("WaterPotBehaviour", "Water"), ("ApplyAdditiveToGrowContainerBehaviour", "Additive"), ("SowSeedInPotBehaviour", "Seed"), ("HarvestPotBehaviour", "Harvest") })
        Expect(Call("Classify", "ScheduleOne.NPCs.Behaviour." + entry.Item1).ToString() == entry.Item2);
});
Check("fungal and lookalike actions are excluded", () => {
    foreach (var type in new[] { "ScheduleOne.NPCs.Behaviour.ApplySpawnToMushroomBedBehaviour", "Other.WaterPotBehaviour", "", "ScheduleOne.NPCs.Behaviour.WaterPotBehaviourChild" })
        Expect(Call("Classify", type).ToString() == "Unknown");
});
Check("only the initial successful yield may change wait", () => {
    Expect((bool)Call("IsFirstWait", 0, 1, true));
    foreach (var values in new[] { (0, 1, false), (1, 1, true), (0, 2, true), (-1, 1, true) })
        Expect(!(bool)Call("IsFirstWait", values.Item1, values.Item2, values.Item3));
});
Check("game work speed is already applied and multiplier divides wait once", () => {
    foreach (string action in actions) Wait(action, 7f, 2f, 7f, true, true, 1f);
});
Check("half speed doubles the already scaled wait", () => Wait("Seed", 7.5f, 2f, .5f, true, true, 15f));
Check("normal multiplier preserves exact wait even at extreme valid work speeds", () => {
    foreach (float speed in new[] { float.Epsilon, 2f, float.MaxValue })
        Wait("Soil", float.MaxValue, speed, 1f, true, true, float.MaxValue);
});
Check("valid extreme work speeds need no reconstruction of base duration", () => {
    Wait("Harvest", 14f, float.MaxValue, 7f, true, true, 2f);
    Wait("Soil", float.Epsilon, float.Epsilon, 1f, true, true, float.Epsilon);
});
Check("small positive multiplier slows waits without clamping", () => Wait("Water", 5f, 2f, .01f, true, true, 500f));
Check("zero harvest wait remains zero", () => Wait("Harvest", 0f, 2f, .5f, true, true, 0f));
Check("invalid speeds preserve wait", () => { foreach (float speed in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Wait("Soil", 5f, speed, 1f, true, false, 5f); });
Check("invalid waits preserve wait", () => { foreach (float wait in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Wait("Soil", wait, 2f, 1f, true, false, wait); });
Check("invalid multipliers cannot change waits", () => { foreach (float multiplier in new[] { 0f, -3f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Wait("Soil", 5f, 2f, multiplier, true, false, 5f); });
Check("disabled and unknown waits are unchanged", () => { Wait("Soil", 5f, 2f, 1f, false, false, 5f); Wait("Unknown", 5f, 2f, 1f, true, false, 5f); });
Check("division overflow and underflow cannot corrupt waits", () => {
    Wait("Harvest", float.MaxValue, 2f, .5f, true, false, float.MaxValue);
    Wait("Seed", float.Epsilon, 2f, 2f, true, false, float.Epsilon);
});
Console.WriteLine($"Timing checks: {passed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;
