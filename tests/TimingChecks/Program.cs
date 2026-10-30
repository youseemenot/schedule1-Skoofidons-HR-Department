using System.Reflection;

// Each check catches a wrong branch or invalid value reaching the coroutine wait.
// Reflection allows the initial RED run to report a missing policy as a failed
// behavior check rather than a compiler error; no game assembly is loaded here.
int passed = 0, failed = 0;
Type Policy() => Assembly.GetExecutingAssembly().GetType("SkoofidonsHRDepartment.BotanistTimingPolicy")
    ?? throw new InvalidOperationException("Timing policy is not implemented.");
object Action(string action) => Enum.Parse(Assembly.GetExecutingAssembly().GetType("SkoofidonsHRDepartment.TimingAction")!, action);
object Call(string method, params object[] args) => Policy().GetMethod(method)!.Invoke(null, args)!;
float Resolve(string action, float original, float configured, bool enabled = true) => (float)Call("Resolve", Action(action), original, configured, enabled);
void Equal(float actual, float expected) {
    if (float.IsNaN(expected) ? !float.IsNaN(actual) : actual != expected)
        throw new Exception($"Expected {expected}, got {actual}.");
}
void Check(string name, System.Action check) {
    try { check(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + (ex.InnerException ?? ex).Message); }
}
void Expect(bool value) { if (!value) throw new Exception("Guard accepted an unsafe transition or rejected a valid one."); }
void Wait(string action, float wait, float speed, float setting, bool enabled, bool expectedSuccess, float expectedWait) {
    object[] args = { Action(action), wait, speed, setting, enabled, 0f };
    Expect((bool)Call("TryResolveWait", args) == expectedSuccess);
    Equal((float)args[5], expectedWait);
}

Check("disabled feature preserves game duration", () => Equal(Resolve("Soil", 10f, 5f, false), 10f));
Check("unknown actions preserve game duration", () => Equal(Resolve("Unknown", 15f, 5f), 15f));
Check("soil uses configured seconds", () => Equal(Resolve("Soil", 10f, 5f), 5f));
Check("water uses configured seconds", () => Equal(Resolve("Water", 10f, 6f), 6f));
Check("additive uses configured seconds", () => Equal(Resolve("Additive", 10f, 3f), 3f));
Check("seed uses configured seconds", () => Equal(Resolve("Seed", 15f, 7.5f), 7.5f));
Check("harvest preserves item count through seconds per item", () => Equal(Resolve("Harvest", 15f, .5f), 7.5f));
Check("empty harvest remains zero", () => Equal(Resolve("Harvest", 0f, .5f), 0f));
Check("too-small positive duration is clamped", () => Equal(Resolve("Soil", 10f, .01f), .1f));
Check("too-small positive per-item duration is clamped before scaling", () => Equal(Resolve("Harvest", 15f, .01f), 1.5f));
Check("invalid settings preserve duration", () => { foreach (float value in new[] { 0f, -3f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Equal(Resolve("Soil", 10f, value), 10f); });
Check("invalid game durations are preserved", () => { foreach (float value in new[] { float.NaN, float.PositiveInfinity, -1f }) Equal(Resolve("Harvest", value, .5f), value); });
Check("overflow preserves original duration", () => Equal(Resolve("Harvest", float.MaxValue, float.MaxValue), float.MaxValue));
Check("harvest underflow preserves a positive game duration", () => Equal(Resolve("Harvest", float.Epsilon, .1f), float.Epsilon));
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
Check("work speed applies once to configured absolute seconds", () => Wait("Soil", 5f, 2f, 5f, true, true, 2.5f));
Check("harvest reconstructs base item duration before speed scaling", () => Wait("Harvest", 7.5f, 2f, .5f, true, true, 3.75f));
Check("zero harvest wait remains zero", () => Wait("Harvest", 0f, 2f, .5f, true, true, 0f));
Check("invalid speeds preserve wait", () => { foreach (float speed in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) Wait("Soil", 5f, speed, 1f, true, false, 5f); });
Check("invalid waits preserve wait", () => { foreach (float wait in new[] { -1f, float.NaN, float.PositiveInfinity }) Wait("Soil", wait, 2f, 1f, true, false, wait); });
Check("invalid settings cannot change waits", () => { foreach (float setting in new[] { 0f, -3f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Wait("Soil", 5f, 2f, setting, true, false, 5f); });
Check("disabled and unknown waits are unchanged", () => { Wait("Soil", 5f, 2f, 1f, false, false, 5f); Wait("Unknown", 5f, 2f, 1f, true, false, 5f); });
Check("overflow and underflow cannot corrupt waits", () => {
    Wait("Harvest", float.MaxValue, 2f, .5f, true, false, float.MaxValue);
    Wait("Soil", 1f, float.Epsilon, float.MaxValue, true, false, 1f);
});
Console.WriteLine($"Timing checks: {passed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;
