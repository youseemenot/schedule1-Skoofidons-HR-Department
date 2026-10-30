using SkoofidonsHRDepartment;

int checks = 0, failures = 0;
void Check(string name, Action test)
{
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
void Near(double expected, double actual, double tolerance = 1e-6) =>
    Expect(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance, $"Expected {expected}, got {actual}");

Check("only positive finite captured rates survive normalization", () =>
{
    foreach (double rate in new[] { .5, 1, 1.5, 7, double.Epsilon, double.MaxValue })
        Expect(MachineTimerPolicy.NormalizeRate(rate) == rate, $"Changed valid rate {rate}");
    foreach (double rate in new[] { 0, -1, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        Near(1, MachineTimerPolicy.NormalizeRate(rate));
});
Check("half speed carries a minute across two native ticks", () =>
{
    Expect(MachineTimerPolicy.TryCredit(1, .5, 0, 10, out int first, out double fraction), "First tick rejected");
    Expect(first == 0, "First half minute rounded up"); Near(.5, fraction);
    Expect(MachineTimerPolicy.TryCredit(1, .5, fraction, 10, out int second, out fraction), "Second tick rejected");
    Expect(second == 1, "Two halves did not credit one minute"); Near(0, fraction);
});
Check("one and a half speed keeps exact fractional credit over uneven ticks", () =>
{
    Expect(MachineTimerPolicy.TryCredit(1, 1.5, 0, 30, out int credit, out double fraction), "First tick rejected");
    Expect(credit == 1, "Wrong initial credit"); Near(.5, fraction);
    Expect(MachineTimerPolicy.TryCredit(3, 1.5, fraction, 29, out credit, out fraction), "Batch rejected");
    Expect(credit == 5, "Batch lost carried fraction"); Near(0, fraction);
});
Check("credit completion clamps a large finite product before integer conversion", () =>
{
    Expect(MachineTimerPolicy.TryCredit(int.MaxValue, 1e100, .75, 3, out int credit, out double fraction), "Finite large product rejected");
    Expect(credit == 3, "Completion overcredited"); Near(0, fraction);
    Expect(MachineTimerPolicy.TryCredit(1, .5, .75, 1, out credit, out fraction), "Completion rejected");
    Expect(credit == 1, "Wrong last minute"); Near(0, fraction);
});
Check("already complete operations do not retain fractional credit", () =>
{
    Expect(MachineTimerPolicy.TryCredit(1, .5, .75, 0, out int credit, out double fraction), "Zero remaining rejected");
    Expect(credit == 0, "Completed operation advanced"); Near(0, fraction);
});
Check("invalid elapsed and arithmetic preserve the native integer path", () =>
{
    foreach (int elapsed in new[] { 0, -1, int.MinValue })
    {
        Expect(!MachineTimerPolicy.TryCredit(elapsed, 2, .25, 10, out int credit, out double fraction), "Invalid elapsed accepted");
        Expect(credit == elapsed, "Native elapsed changed"); Near(.25, fraction);
    }
    foreach (double rate in new[] { 0, -1, double.NaN, double.PositiveInfinity, double.MaxValue })
    {
        Expect(!MachineTimerPolicy.TryCredit(2, rate, .25, 10, out int credit, out double fraction), $"Unsafe rate accepted: {rate}");
        Expect(credit == 2, "Unsafe rate changed native elapsed"); Near(.25, fraction);
    }
    Expect(!MachineTimerPolicy.TryCredit(1, 2, 0, -1, out int native, out _), "Negative remaining accepted");
    Expect(native == 1, "Invalid remaining changed native elapsed");
});
Check("malformed policy fractions cannot enter integer progress", () =>
{
    foreach (double fraction in new[] { -.1, 1, 2, double.NaN, double.PositiveInfinity })
        Expect(!MachineTimerPolicy.TryCredit(1, 2, fraction, 10, out _, out _), $"Accepted fraction {fraction}");
});
Check("nonmutating lookup leaves unknown operations absent and preserves captured state", () =>
{
    var ledger = new MachineTimerLedger();
    Expect(!ledger.TryGet("oven", out var missing) && missing == null, "Unknown lookup created an operation");
    Expect(!ledger.TryGet("oven", out _), "Unknown lookup adopted a native operation");
    ledger.Capture("oven", "batch", .5, .25);
    Expect(ledger.TryGet("oven", out var captured), "Captured operation missing");
    Expect(captured.Identity == "batch", "Lookup changed captured identity");
    Near(.5, captured.CapturedRate); Near(.25, captured.Fraction);
    Expect(ledger.TryAdvance("oven", "batch", 1, 10, out int credit) && credit == 0, "Lookup changed captured speed");
    Expect(ledger.TryGet("oven", out var repeated) && ReferenceEquals(captured, repeated), "Repeated lookup replaced captured state");
    Near(.75, repeated.Fraction);
});
Check("unknown and replacement operations adopt native speed without leaking carry", () =>
{
    var ledger = new MachineTimerLedger();
    var unknown = ledger.GetOrAdopt("oven", "loaded");
    Near(1, unknown.CapturedRate); Near(0, unknown.Fraction);
    ledger.Capture("oven", "new", .5);
    Expect(ledger.TryAdvance("oven", "new", 1, 10, out int credit) && credit == 0, "Captured operation did not slow down");
    var replacement = ledger.GetOrAdopt("oven", "replacement");
    Near(1, replacement.CapturedRate); Near(0, replacement.Fraction);
    Expect(replacement.Identity == "replacement", "Identity was not replaced");
    Expect(!ledger.TryAdvance("oven", "new", 1, 10, out credit) && credit == 1, "Old factor resurrected after replacement");
});
Check("captured rate survives later preference changes and repeated lookup", () =>
{
    var ledger = new MachineTimerLedger(); double preference = .5;
    ledger.Capture("mixer", "batch", preference);
    preference = 7;
    var captured = ledger.GetOrAdopt("mixer", "batch");
    Near(.5, captured.CapturedRate);
    Expect(ReferenceEquals(captured, ledger.GetOrAdopt("mixer", "batch")), "Lookup discarded operation state");
    Expect(ledger.TryAdvance("mixer", "batch", 1, 10, out int credit) && credit == 0, "Preference changed active speed");
    Near(.5, captured.Fraction);
    ledger.Capture("mixer", "next-batch", preference);
    Expect(ledger.TryAdvance("mixer", "next-batch", 1, 10, out credit) && credit == 7, "Next operation missed new setting");
    Near(0, ledger.GetOrAdopt("mixer", "next-batch").Fraction);
});
Check("explicit saved rate and carry restore the next progress tick", () =>
{
    var ledger = new MachineTimerLedger();
    ledger.Capture("cauldron", "saved", 1.5, .75);
    Expect(ledger.TryAdvance("cauldron", "saved", 1, 20, out int credit) && credit == 2, "Restored carry lost");
    Near(.25, ledger.GetOrAdopt("cauldron", "saved").Fraction);
});
Check("invalid capture data normalizes without poisoning later progress", () =>
{
    var ledger = new MachineTimerLedger();
    foreach (double fraction in new[] { -.1, 1, double.NaN, double.PositiveInfinity })
    {
        ledger.Capture("rack", "saved", .5, fraction);
        Expect(ledger.TryAdvance("rack", "saved", 1, 10, out int credit) && credit == 0, "Malformed carry credited work");
        Near(.5, ledger.GetOrAdopt("rack", "saved").Fraction);
    }
    ledger.Capture("rack", "saved", double.NaN, .25);
    Near(1, ledger.GetOrAdopt("rack", "saved").CapturedRate);
    Expect(!ledger.TryAdvance("rack", "saved", 1, 10, out int native) && native == 1, "Invalid saved rate modified native path");
});
Check("native rate and failed ticks leave saved carry unchanged", () =>
{
    var ledger = new MachineTimerLedger();
    ledger.Capture("oven", "neutral", 1, .25);
    Expect(!ledger.TryAdvance("oven", "neutral", 9, 1, out int credit) && credit == 9, "Neutral rate clamped native tick");
    Near(.25, ledger.GetOrAdopt("oven", "neutral").Fraction);
    ledger.Capture("oven", "slow", .5, .25);
    Expect(!ledger.TryAdvance("oven", "slow", -1, 10, out credit) && credit == -1, "Negative tick modified native path");
    Near(.25, ledger.GetOrAdopt("oven", "slow").Fraction);
});
Check("completion and explicit lifecycle cleanup cannot leak operation state", () =>
{
    var ledger = new MachineTimerLedger();
    ledger.Capture("a", "one", 7, .75); ledger.Capture("b", "two", .5, .5);
    Expect(ledger.TryAdvance("a", "one", 1, 1, out int credit) && credit == 1, "Completion not clamped");
    Near(0, ledger.GetOrAdopt("a", "one").Fraction);
    ledger.Forget("a"); Near(1, ledger.GetOrAdopt("a", "one").CapturedRate);
    ledger.Clear(); Near(1, ledger.GetOrAdopt("b", "two").CapturedRate); Near(0, ledger.GetOrAdopt("b", "two").Fraction);
});
Check("separate drying operations on one rack keep independent fractions", () =>
{
    var ledger = new MachineTimerLedger();
    ledger.Capture("rack:slot-a", "a", .5); ledger.Capture("rack:slot-b", "b", 1.5);
    Expect(ledger.TryAdvance("rack:slot-a", "a", 1, 10, out int a) && a == 0, "Slow slot changed");
    Expect(ledger.TryAdvance("rack:slot-b", "b", 1, 10, out int b) && b == 1, "Fast slot changed");
    Expect(ledger.TryAdvance("rack:slot-a", "a", 1, 10, out a) && a == 1, "Other slot consumed slow carry");
    Near(.5, ledger.GetOrAdopt("rack:slot-b", "b").Fraction);
});
Check("growth scales only future positive progress and reaches completion at half speed", () =>
{
    Expect(MachineTimerPolicy.TryScaleGrowth(.25f, .5f, 2, out float growth), "Growth rejected"); Near(.75, growth);
    Expect(MachineTimerPolicy.TryScaleGrowth(.8f, 1.2f, .5, out growth), "Slow final tick rejected"); Near(1, growth);
    Expect(MachineTimerPolicy.TryScaleGrowth(.9f, 1.1f, 7, out growth), "Fast final tick rejected"); Near(1, growth);
});
Check("growth resets and invalid requests stay native", () =>
{
    foreach (float requested in new[] { .5f, .25f, 0f, -1f, float.NaN, float.PositiveInfinity })
    {
        Expect(!MachineTimerPolicy.TryScaleGrowth(.5f, requested, 7, out float result), "Non-growth request scaled");
        Expect(float.IsNaN(requested) ? float.IsNaN(result) : result == requested, "Native reset changed");
    }
    Expect(!MachineTimerPolicy.TryScaleGrowth(float.NaN, .5f, 2, out _), "Invalid previous accepted");
    Expect(!MachineTimerPolicy.TryScaleGrowth(0, 1, double.PositiveInfinity, out float native) && native == 1, "Invalid factor changed native growth");
    Expect(!MachineTimerPolicy.TryScaleGrowth(-float.MaxValue, float.MaxValue, double.MaxValue, out _), "Nonfinite product accepted");
});
Check("drying speeds future warmth-adjusted minutes without rescaling history", () =>
{
    Expect(MachineTimerPolicy.TryDryingAdjustment(10, 2, .5f, 3, out float adjusted), "Drying rejected");
    Near(12, adjusted); Near(13, adjusted + 1);
    Expect(MachineTimerPolicy.TryDryingAdjustment(0, 1, 1, .5, out adjusted), "Slow first tick rejected");
    Near(-.5, adjusted); Near(.5, adjusted + 1);
    Expect(MachineTimerPolicy.TryDryingAdjustment(10, 2, .5f, 1, out adjusted), "Neutral drying rejected"); Near(10, adjusted);
});
Check("drying invalid inputs and nonfinite native final values stay untouched", () =>
{
    foreach (double rate in new[] { 0, -1, double.NaN, double.PositiveInfinity, double.MaxValue })
        Expect(!MachineTimerPolicy.TryDryingAdjustment(10, 2, 1, rate, out float unchanged) && unchanged == 10, "Unsafe drying accepted");
    Expect(!MachineTimerPolicy.TryDryingAdjustment(10, 0, 1, 2, out _), "Zero elapsed accepted");
    Expect(!MachineTimerPolicy.TryDryingAdjustment(10, 1, float.NaN, 2, out _), "Invalid warmth accepted");
    Expect(!MachineTimerPolicy.TryDryingAdjustment(10, 1, -1, 2, out _), "Negative warmth accepted");
    Expect(!MachineTimerPolicy.TryDryingAdjustment(float.MaxValue, 1, float.MaxValue, 1, out float native) && native == float.MaxValue, "Native final float overflow accepted");
    Expect(MachineTimerPolicy.TryDryingAdjustment(10, 1, 0, 7, out native), "Zero warmth rejected"); Near(10, native);
});
Check("ETA uses saved carry and rounds up scaled remaining work", () =>
{
    Expect(MachineTimerPolicy.TryEta(10, .5, 2, out int eta) && eta == 5, "ETA did not ceil");
    Expect(MachineTimerPolicy.TryEta(1, .5, .5, out eta) && eta == 1, "ETA ignored saved half minute");
    Expect(MachineTimerPolicy.TryEta(1, 0, .5, out eta) && eta == 2, "Slow ETA too short");
    Expect(MachineTimerPolicy.TryEta(0, .5, .5, out eta) && eta == 0, "Completed ETA not zero");
    Expect(MachineTimerPolicy.TryEta(double.MaxValue, 0, double.Epsilon, out eta) && eta == int.MaxValue, "ETA overflow did not saturate");
});
Check("ETA rejects malformed work and factors", () =>
{
    foreach (double remaining in new[] { -1, double.NaN, double.PositiveInfinity })
        Expect(!MachineTimerPolicy.TryEta(remaining, 0, 2, out _), "Invalid work accepted");
    foreach (double fraction in new[] { -1, 1, double.NaN })
        Expect(!MachineTimerPolicy.TryEta(10, fraction, 2, out _), "Invalid carry accepted");
    foreach (double rate in new[] { 0, -1, double.NaN, double.PositiveInfinity })
        Expect(!MachineTimerPolicy.TryEta(10, 0, rate, out _), "Invalid ETA factor accepted");
});

object NewQueue(Func<long> clock = null) {
    var type = typeof(MachineTimerPolicy).Assembly.GetType("SkoofidonsHRDepartment.MachineDryingCaptureQueue") ?? throw new Exception("Drying capture queue missing");
    return clock == null ? Activator.CreateInstance(type)! : Activator.CreateInstance(type, clock)!;
}
long Enqueue(object queue, string rack, string fingerprint, double rate, double progress = 0) =>
    (long)queue.GetType().GetMethod("Enqueue")!.Invoke(queue, new object[] { rack, fingerprint, progress, rate })!;
bool Take(object queue, string rack, string fingerprint, out double rate, double progress = 0) {
    object[] args = { rack, fingerprint, progress, 1d };
    bool result = (bool)queue.GetType().GetMethod("TryTake")!.Invoke(queue, args)!;
    rate = (double)args[3]; return result;
}
Check("drying handoff survives a serialized operation identity", () => {
    var queue = NewQueue(); Enqueue(queue, "rack", "batch", 7);
    Expect(Take(queue, "rack", "batch", out double rate) && rate == 7, "Receiver lost captured factor");
    Expect(!Take(queue, "rack", "batch", out _), "Request captured twice");
});
Check("identical drying requests retain ordered captured rates", () => {
    var queue = NewQueue(); Enqueue(queue, "rack", "batch", 7); Enqueue(queue, "rack", "batch", 2);
    Expect(Take(queue, "rack", "batch", out double first) && first == 7, "First factor changed");
    Expect(Take(queue, "rack", "batch", out double second) && second == 2, "Second factor changed");
});
Check("drying requests cannot cross racks or operation fingerprints", () => {
    var queue = NewQueue(); Enqueue(queue, "rack-a", "batch", 7);
    Expect(!Take(queue, "rack-b", "batch", out _), "Other rack consumed request");
    Expect(!Take(queue, "rack-a", "different", out _), "Other operation consumed request");
    Expect(Take(queue, "rack-a", "batch", out _), "Valid request was lost");
});
Check("drying requests validate the native progress snapshot", () => {
    var queue = NewQueue(); Enqueue(queue, "rack", "batch", 7, 5);
    Expect(!Take(queue, "rack", "batch", out _, 0), "Old sync adopted new factor");
    Expect(Take(queue, "rack", "batch", out _, 5), "Matching progress rejected");
});
Check("failed broadcast cancels only its own drying request", () => {
    var queue = NewQueue(); long first = Enqueue(queue, "rack", "batch", 7); Enqueue(queue, "rack", "batch", 2);
    queue.GetType().GetMethod("Cancel")!.Invoke(queue, new object[] { first });
    Expect(Take(queue, "rack", "batch", out double rate) && rate == 2, "Other request cancelled");
});
Check("expired drying broadcasts do not contaminate later synchronization", () => {
    long time = 0; var queue = NewQueue(() => time); Enqueue(queue, "rack", "batch", 7);
    time = 60_001;
    Expect(!Take(queue, "rack", "batch", out _), "Expired rate survived");
});
Check("scene cleanup discards pending drying requests", () => {
    var queue = NewQueue(); Enqueue(queue, "rack", "batch", 7);
    queue.GetType().GetMethod("Clear")!.Invoke(queue, null);
    Expect(!Take(queue, "rack", "batch", out _), "Scene request survived");
});
Check("invalid drying rate falls back to native", () => {
    var queue = NewQueue(); Enqueue(queue, "rack", "batch", double.NaN);
    Expect(Take(queue, "rack", "batch", out double rate) && rate == 1, "Invalid rate propagated");
});

Console.WriteLine($"{checks - failures}/{checks} machine-timer checks passed");
return failures == 0 ? 0 : 1;
