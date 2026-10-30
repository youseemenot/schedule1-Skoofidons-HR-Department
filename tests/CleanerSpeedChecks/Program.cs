using SkoofidonsHRDepartment;

int checks = 0, failures = 0;
void Check(string name, Action test) {
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
void Scales(float original, float multiplier, float expected) {
    Expect(CleanerSpeedPolicy.TryScaleDuration(original, multiplier, out float actual), "Rejected valid wait");
    Expect(actual == expected, $"Expected {expected}, got {actual}");
}
Check("seven times faster divides a fourteen second action to two seconds", () => Scales(14f, 7f, 2f));
Check("half speed doubles an action duration", () => Scales(14f, .5f, 28f));
Check("normal preserves exact duration bits", () => {
    float original = BitConverter.Int32BitsToSingle(0x3f812345);
    Expect(CleanerSpeedPolicy.TryScaleDuration(original, 1f, out float result), "Rejected normal");
    Expect(BitConverter.SingleToInt32Bits(result) == BitConverter.SingleToInt32Bits(original), "Changed normal duration");
});
Check("invalid multiplier preserves the original duration", () => {
    foreach (float multiplier in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
        Expect(!CleanerSpeedPolicy.TryScaleDuration(14f, multiplier, out float result), $"Accepted {multiplier}");
        Expect(result == 14f, "Lost original wait");
    }
});
Check("overflow and underflow cannot turn a positive wait into infinity or zero", () => {
    Expect(!CleanerSpeedPolicy.TryScaleDuration(float.MaxValue, .5f, out float overflow) && overflow == float.MaxValue, "Accepted overflow");
    Expect(!CleanerSpeedPolicy.TryScaleDuration(float.Epsilon, 7f, out float underflow) && underflow == float.Epsilon, "Accepted underflow");
});
Check("invalid native duration is preserved and zero remains zero", () => {
    foreach (float original in new[] { -1f, float.NaN, float.PositiveInfinity }) {
        Expect(!CleanerSpeedPolicy.TryScaleDuration(original, 7f, out float result), "Accepted invalid native wait");
        Expect(result.Equals(original), "Lost invalid native wait");
    }
    Scales(0f, 7f, 0f);
});
Check("only active waits from native Cleaner owners are eligible", () => {
    Expect(CleanerSpeedPolicy.IsEligibleYield(true, "ScheduleOne.Employees.Cleaner", true), "Skipped Cleaner wait");
    Expect(!CleanerSpeedPolicy.IsEligibleYield(false, "ScheduleOne.Employees.Cleaner", true), "Changed completed routine");
    Expect(!CleanerSpeedPolicy.IsEligibleYield(true, "ScheduleOne.Employees.Cleaner", false), "Changed frame or other instruction");
    foreach (string owner in new[] { null, "ScheduleOne.Employees.Botanist", "Cleaner", "ScheduleOne.Employees.CleanerSubclass" })
        Expect(!CleanerSpeedPolicy.IsEligibleYield(true, owner, true), "Changed another owner");
});
Check("normal and invalid settings require no coroutine patches", () => {
    foreach (float multiplier in new[] { 1f, 0f, -1f, float.NaN, float.PositiveInfinity })
        Expect(!CleanerSpeedPolicy.RequiresPatch(multiplier), "Registered inactive setting");
    Expect(CleanerSpeedPolicy.RequiresPatch(7f) && CleanerSpeedPolicy.RequiresPatch(.5f), "Skipped valid non-normal setting");
});
Console.WriteLine($"{checks - failures}/{checks} Cleaner-speed checks passed");
return failures == 0 ? 0 : 1;
