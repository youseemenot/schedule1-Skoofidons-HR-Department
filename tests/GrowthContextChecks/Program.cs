using SkoofidonsHRDepartment;

int checks = 0, failures = 0;
void Expect(bool value, string message) { if (!value) throw new Exception(message); }
void Near(float expected, float actual) => Expect(Math.Abs(expected - actual) < 1e-6f, $"Expected {expected}, got {actual}");
void Check(string name, Action test)
{
    checks++;
    MachineGrowthContext.Clear();
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    finally { MachineGrowthContext.Clear(); }
}
bool Scale(MachineTimerKind kind, nint pointer, float previous, float requested, out float scaled) =>
    MachineGrowthContext.TryScale(kind, pointer, previous, requested, out scaled);

Check("manual and load setters outside growth callbacks preserve their requested progress", () =>
{
    Expect(!Scale(MachineTimerKind.PlantGrowth, 11, .2f, .5f, out float result), "Unscoped setter was scaled");
    Near(.5f, result);
});
Check("growth scaling belongs only to the current exact kind and native pointer", () =>
{
    var scope = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 11, 2);
    Expect(Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out float scaled), "Matching callback was not scaled");
    Near(.4f, scaled);
    Expect(!Scale(MachineTimerKind.PlantGrowth, 12, .2f, .3f, out scaled), "Different plant was scaled");
    Near(.3f, scaled);
    Expect(!Scale(MachineTimerKind.ShroomGrowth, 11, .2f, .3f, out scaled), "Different kind was scaled");
    Near(.3f, scaled);
    MachineGrowthContext.Exit(scope);
    Expect(!Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out _), "Finished context leaked");
});
Check("nested callbacks shadow outer contexts and restore the outer captured factor", () =>
{
    var outer = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 11, 2);
    var inner = MachineGrowthContext.Enter(MachineTimerKind.ShroomGrowth, 22, .5);
    Expect(!Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out _), "Outer context leaked into nested callback");
    Expect(Scale(MachineTimerKind.ShroomGrowth, 22, .2f, .4f, out float scaled), "Inner callback was not scaled");
    Near(.3f, scaled);
    MachineGrowthContext.Exit(inner);
    Expect(Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out scaled), "Outer context was lost");
    Near(.4f, scaled);
    MachineGrowthContext.Exit(outer);
});
Check("a nested native-rate callback prevents accidentally reusing its outer factor", () =>
{
    var outer = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 11, 7);
    var inner = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 11, 1);
    Expect(!Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out float scaled), "Native factor used an outer multiplier");
    Near(.3f, scaled);
    MachineGrowthContext.Exit(inner);
    Expect(Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out scaled), "Outer multiplier was not restored");
    Near(.9f, scaled);
    MachineGrowthContext.Exit(outer);
});
Check("exception unwinding removes nested contexts back to the callback's prior depth", () =>
{
    var outer = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 11, 2);
    MachineGrowthContext.Scope abandoned = default;
    try
    {
        abandoned = MachineGrowthContext.Enter(MachineTimerKind.ShroomGrowth, 22, 7);
        throw new InvalidOperationException("native growth failure");
    }
    catch (InvalidOperationException) { }
    finally { MachineGrowthContext.Exit(outer); }
    Expect(!Scale(MachineTimerKind.ShroomGrowth, 22, .1f, .2f, out _), "Exception leaked an inner context");
    var fresh = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 33, .5);
    MachineGrowthContext.Exit(abandoned);
    MachineGrowthContext.Exit(outer);
    Expect(Scale(MachineTimerKind.PlantGrowth, 33, .2f, .4f, out float scaled), "Stale finalizer removed new work");
    Near(.3f, scaled);
    MachineGrowthContext.Exit(fresh);
});
Check("scene clear makes stale finalizers harmless to fresh contexts", () =>
{
    var stale = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 11, 7);
    MachineGrowthContext.Clear();
    var fresh = MachineGrowthContext.Enter(MachineTimerKind.ShroomGrowth, 22, .5);
    MachineGrowthContext.Exit(stale);
    Expect(Scale(MachineTimerKind.ShroomGrowth, 22, .2f, .4f, out float scaled), "Stale scene finalizer removed fresh context");
    Near(.3f, scaled);
    MachineGrowthContext.Exit(fresh);
});
Check("growth contexts cannot cross threads", () =>
{
    var outer = MachineGrowthContext.Enter(MachineTimerKind.PlantGrowth, 11, 2);
    Exception error = null;
    var thread = new Thread(() =>
    {
        try
        {
            Expect(!Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out _), "Another thread inherited a growth scope");
            var local = MachineGrowthContext.Enter(MachineTimerKind.ShroomGrowth, 22, .5);
            MachineGrowthContext.Exit(outer);
            Expect(Scale(MachineTimerKind.ShroomGrowth, 22, .2f, .4f, out float scaled), "Foreign finalizer removed local scope");
            Near(.3f, scaled);
            MachineGrowthContext.Exit(local);
        }
        catch (Exception ex) { error = ex; }
    });
    thread.Start(); thread.Join();
    if (error != null) throw error;
    Expect(Scale(MachineTimerKind.PlantGrowth, 11, .2f, .3f, out float result), "Other thread changed caller context");
    Near(.4f, result);
    MachineGrowthContext.Exit(outer);
});
Check("slow growth reaches completion from an unclamped request and preserves zero or backward changes", () =>
{
    var scope = MachineGrowthContext.Enter(MachineTimerKind.ShroomGrowth, 22, .5);
    Expect(Scale(MachineTimerKind.ShroomGrowth, 22, .9f, 1.3f, out float scaled), "Unclamped future delta rejected");
    Near(1f, scaled);
    Expect(!Scale(MachineTimerKind.ShroomGrowth, 22, .9f, .9f, out scaled), "Blocked progress was scaled"); Near(.9f, scaled);
    Expect(!Scale(MachineTimerKind.ShroomGrowth, 22, .9f, .2f, out scaled), "Backward change was scaled"); Near(.2f, scaled);
    MachineGrowthContext.Exit(scope);
});

Console.WriteLine($"{checks - failures}/{checks} growth context checks passed");
return failures == 0 ? 0 : 1;
