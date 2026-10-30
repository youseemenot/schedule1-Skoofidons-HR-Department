using SkoofidonsHRDepartment;

int checks = 0, failures = 0;
void Check(string name, Action test) {
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
void Near(float expected, float actual) => Expect(Math.Abs(expected - actual) < .0001f, $"Expected {expected}, got {actual}");

Check("7x scales movement and native cap", () => {
    Expect(EmployeeSpeedPolicy.TryScaleMovement(1.8f, 7f, 7f, out var speed, out var cap), "Rejected multiplier");
    Near(12.6f, speed); Near(49f, cap);
});
Check("half movement scales both values", () => {
    Expect(EmployeeSpeedPolicy.TryScaleMovement(1.8f, 7f, .5f, out var speed, out var cap), "Rejected multiplier");
    Near(.9f, speed); Near(3.5f, cap);
});
Check("normal retains the supplied native baseline", () => {
    Expect(EmployeeSpeedPolicy.TryScaleMovement(2.5f, 9f, 1f, out var speed, out var cap), "Rejected normal");
    Near(2.5f, speed); Near(9f, cap);
});
Check("invalid and overflowing movement cannot enter native setters", () => {
    foreach (var invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue })
        Expect(!EmployeeSpeedPolicy.TryScaleMovement(1.8f, 7f, invalid, out _, out _), $"Accepted {invalid}");
    Expect(!EmployeeSpeedPolicy.TryScaleMovement(float.Epsilon, 7f, .5f, out _, out _), "Accepted underflow");
    Expect(!EmployeeSpeedPolicy.TryScaleMovement(float.NaN, 7f, 2f, out _, out _), "Accepted invalid baseline");
});
Check("work factor multiplies the native rate without erasing buffs", () => {
    Near(7f, EmployeeSpeedPolicy.ResolveWorkMultiplier(1.5f, 7f));
    Near(.5f, EmployeeSpeedPolicy.ResolveWorkMultiplier(1.5f, .5f));
    Near(1f, EmployeeSpeedPolicy.ResolveWorkMultiplier(1.5f, 1f));
});
Check("invalid work factors and products remove the mod factor", () => {
    foreach (var invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.MaxValue })
        Near(1f, EmployeeSpeedPolicy.ResolveWorkMultiplier(2f, invalid));
    Near(1f, EmployeeSpeedPolicy.ResolveWorkMultiplier(float.Epsilon, .5f));
    Near(1f, EmployeeSpeedPolicy.ResolveWorkMultiplier(float.NaN, 7f));
});
Check("repeated saves retain first baseline across native wrapper instances", () => {
    var state = new EmployeeMovementSpeedState<object>();
    object first = new(), second = new();
    float speed = 1.8f, cap = 7f;
    void Write(object _, float s, float c) { speed = s; cap = c; }
    state.Apply((nint)1, first, speed, cap, 7f, Write);
    state.Apply((nint)1, second, speed, cap, 7f, Write);
    Near(12.6f, speed); Near(49f, cap);
    state.Apply((nint)1, second, speed, cap, .5f, Write);
    Near(.9f, speed); Near(3.5f, cap);
});
Check("normal restores and releases baseline so later changes can be recaptured", () => {
    var state = new EmployeeMovementSpeedState<object>();
    object owner = new(); float speed = 1.8f, cap = 7f;
    void Write(object _, float s, float c) { speed = s; cap = c; }
    state.Apply((nint)1, owner, speed, cap, 7f, Write);
    state.Apply((nint)1, owner, speed, cap, 1f, Write);
    Near(1.8f, speed); Near(7f, cap);
    state.Apply((nint)1, owner, 2f, 8f, 7f, Write);
    Near(14f, speed); Near(56f, cap);
});
Check("invalid changed setting restores an already affected controller", () => {
    var state = new EmployeeMovementSpeedState<object>();
    object owner = new(); float speed = 1.8f, cap = 7f;
    void Write(object _, float s, float c) { speed = s; cap = c; }
    state.Apply((nint)1, owner, speed, cap, 7f, Write);
    state.Apply((nint)1, owner, speed, cap, float.NaN, Write);
    Near(1.8f, speed); Near(7f, cap);
});
Check("normal and invalid settings never capture or write untouched controllers", () => {
    var state = new EmployeeMovementSpeedState<object>();
    foreach (float factor in new[] { 1f, 0f, float.NaN, float.MaxValue })
        state.Apply((nint)1, new(), 1.8f, 7f, factor, (_, _, _) => throw new Exception("Unexpected native write"));
    state.RestoreAndClear((_, _, _) => throw new Exception("Untouched controller retained"));
});
Check("cleanup restores each native baseline and releases all references", () => {
    var state = new EmployeeMovementSpeedState<object>();
    object a = new(), b = new(); var restored = new Dictionary<object, (float, float)>();
    state.Apply((nint)1, a, 1.8f, 7f, 7f, (_, _, _) => { });
    state.Apply((nint)2, b, 2f, 8f, .5f, (_, _, _) => { });
    state.RestoreAndClear((owner, speed, cap) => restored.Add(owner, (speed, cap)));
    Expect(restored.Count == 2, "Missed controller");
    Expect(restored[a] == (1.8f, 7f) && restored[b] == (2f, 8f), "Lost baselines");
    state.RestoreAndClear((_, _, _) => throw new Exception("Cleanup retained a reference"));
});
Check("destroyed binding during cleanup cannot strand remaining references", () => {
    var state = new EmployeeMovementSpeedState<object>();
    object a = new(), b = new(); int attempted = 0;
    state.Apply((nint)1, a, 1.8f, 7f, 7f, (_, _, _) => { });
    state.Apply((nint)2, b, 2f, 8f, .5f, (_, _, _) => { });
    state.RestoreAndClear((owner, _, _) => { attempted++; if (owner == a) throw new InvalidOperationException("Destroyed native binding"); });
    Expect(attempted == 2, "Cleanup stopped at first stale binding");
    state.RestoreAndClear((_, _, _) => throw new Exception("Cleanup retained stale binding"));
});
Console.WriteLine($"{checks - failures}/{checks} employee-speed checks passed");
return failures == 0 ? 0 : 1;
