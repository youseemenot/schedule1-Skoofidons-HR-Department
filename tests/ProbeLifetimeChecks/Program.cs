using System.Reflection;

int tests = 0, failed = 0;
void Expect(bool value, string why) { if (!value) throw new Exception(why); }
void Check(string name, Action test)
{
    tests++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + (error.InnerException ?? error).Message); }
}
object Cleanup() => Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType("Skoofidons.SettingsProbe.ProbeCleanupCoordinator")
    ?? throw new Exception("Owned cleanup coordinator is not implemented"))!;
bool Run(object cleanup, bool ownerAlive, Action remove, Action exit, Action destroy) =>
    (bool)cleanup.GetType().GetMethod("TryComplete")!.Invoke(cleanup, new object[] { ownerAlive, remove, exit, destroy, null })!;
bool Pending(object cleanup) => (bool)cleanup.GetType().GetProperty("Pending")!.GetValue(cleanup)!;
Check("live owner is removed before Escape and object release", () => {
    var c = Cleanup(); var steps = new List<string>();
    Expect(Run(c, true, () => steps.Add("state"), () => steps.Add("exit"), () => steps.Add("object")), "Cleanup failed");
    Expect(steps.SequenceEqual(new[] { "state", "exit", "object" }), "Escape or native object released before owned stack removal");
});
Check("destroyed owning scene skips native stack access", () => {
    var c = Cleanup(); var steps = new List<string>();
    Expect(Run(c, false, () => throw new Exception("Dead owner touched"), () => steps.Add("exit"), () => steps.Add("object")), "Scene teardown failed");
    Expect(steps.SequenceEqual(new[] { "exit", "object" }), "Dead owner had an unsafe cleanup path");
});
Check("failed removal retains Escape and schedules a complete retry", () => {
    var c = Cleanup(); int removes = 0, exits = 0, destroys = 0;
    Expect(!Run(c, true, () => { removes++; throw new Exception("Native removal failed"); }, () => exits++, () => destroys++), "Failed remove reported success");
    Expect(Pending(c) && exits == 0 && destroys == 0, "Failure abandoned retry or removed Escape");
    Expect(Run(c, true, () => removes++, () => exits++, () => destroys++), "Retry never completed");
    Expect(!Pending(c) && removes == 2 && exits == 1 && destroys == 1, "Retry cleanup sequence differs");
});
Check("failed Exit release retries without popping the state twice", () => {
    var c = Cleanup(); int removes = 0, exits = 0, destroys = 0;
    Expect(!Run(c, true, () => removes++, () => { exits++; throw new Exception("Exit release failed"); }, () => destroys++), "Failed Exit release reported success");
    Expect(Pending(c) && removes == 1 && destroys == 0, "Failure destroyed native object early");
    Expect(Run(c, true, () => removes++, () => exits++, () => destroys++), "Exit cleanup did not recover");
    Expect(removes == 1 && exits == 2 && destroys == 1, "Retry repeated native stack removal");
    Run(c, true, () => removes++, () => exits++, () => destroys++);
    Expect(removes == 1 && exits == 2 && destroys == 1, "Repeated completion was not idempotent");
});
Console.WriteLine($"{tests - failed}/{tests} probe-lifetime checks passed");
return failed == 0 ? 0 : 1;
