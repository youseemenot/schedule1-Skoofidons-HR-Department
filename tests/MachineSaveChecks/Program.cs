using System.Text.Json;
using SkoofidonsHRDepartment;

int checks = 0, failures = 0;
const string guid = "15aa6d36-3a0b-49b7-8b8a-683da85b1b2c";
const string otherGuid = "25aa6d36-3a0b-49b7-8b8a-683da85b1b2c";
MachineTimerSnapshot S(MachineTimerKind kind = MachineTimerKind.Chemistry, string identity = "live", double progress = 10, int index = 0,
    string parent = guid, string fingerprint = "recipe:product:5") => new(kind, $"{kind}:{parent}:{index}", identity, parent, fingerprint, progress, 90, index);
string Base(string parent = guid) => JsonSerializer.Serialize(new { GUID = parent });
string Payload(MachineTimerKind kind = MachineTimerKind.Chemistry, double rate = 2, double fraction = .25, double progress = 10,
    int index = 0, string parent = guid, string fingerprint = "recipe:product:5") =>
    JsonSerializer.Serialize(new { Version = 1, Guid = parent, Entries = new[] { new { Kind = kind.ToString(), Index = index, Fingerprint = fingerprint, Progress = progress, Rate = rate, Fraction = fraction } } });
void Check(string name, Action test)
{
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
}
void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
void Near(double expected, double actual) => Expect(double.IsFinite(actual) && Math.Abs(expected - actual) < 1e-9, $"Expected {expected}, got {actual}");
MachineTimerPersistence New() => new(new MachineTimerLedger());
MachineTimerRecord Restore(string payload, MachineTimerSnapshot snapshot = null, string baseJson = null)
{
    var p = New(); var s = snapshot ?? S(); var scope = p.BeginLoad(s.Kind, baseJson ?? Base(s.Guid), payload);
    try { return p.Capture(s, 7); } finally { p.EndLoad(scope, false); }
}

Check("new operations capture once and replacement operations capture a fresh preference", () =>
{
    var p = New(); Near(.5, p.Capture(S(), .5).CapturedRate);
    Near(.5, p.Capture(S(), 7).CapturedRate);
    Near(7, p.Capture(S(identity: "next"), 7).CapturedRate);
});
Check("unknown and replaced operations never adopt current preferences", () =>
{
    var p = New(); Near(1, p.GetOrAdopt(S()).CapturedRate);
    Near(1, p.Capture(S(), 7).CapturedRate);
    Near(1, p.GetOrAdopt(S(identity: "unknown-replacement")).CapturedRate);
});
Check("all five machine kinds retain their captured factor and fraction across a JSON roundtrip", () =>
{
    foreach (MachineTimerKind kind in new[] { MachineTimerKind.Chemistry, MachineTimerKind.Oven, MachineTimerKind.Mixing, MachineTimerKind.Cauldron, MachineTimerKind.Drying })
    {
        var ledger = new MachineTimerLedger(); var p = new MachineTimerPersistence(ledger); var s = S(kind);
        p.Capture(s, 1.5); ledger.TryAdvance(s.Key, s.Identity, 1, 90, out _);
        string payload = p.Serialize(new[] { s });
        var restored = Restore(payload, s with { Identity = "recreated" });
        Near(1.5, restored.CapturedRate); Near(.5, restored.Fraction);
    }
});
Check("neutral factors and zero fractions remain explicit save entries", () =>
{
    var p = New(); p.Capture(S(), 1);
    using var json = JsonDocument.Parse(p.Serialize(new[] { S() }));
    var entry = json.RootElement.GetProperty("Entries")[0];
    Near(1, entry.GetProperty("Rate").GetDouble()); Near(0, entry.GetProperty("Fraction").GetDouble());
    Expect(json.RootElement.GetProperty("Version").GetInt32() == 1, "Missing schema version");
    Expect(json.RootElement.GetProperty("Guid").GetString() == guid, "Missing parent GUID");
});
Check("missing malformed and future metadata force legacy rate during load", () =>
{
    foreach (string payload in new[] { null, "", "{", "null", "[]", "{}", Payload().Replace("\"Version\":1", "\"Version\":2") })
    { var record = Restore(payload); Near(1, record.CapturedRate); Near(0, record.Fraction); }
});
Check("GUID matching accepts canonical equivalents but rejects a different parent", () =>
{
    Near(2, Restore(Payload(parent: guid.ToUpperInvariant()), S(parent: "{" + guid + "}"), Base(guid)).CapturedRate);
    Near(1, Restore(Payload(parent: otherGuid)).CapturedRate);
    Near(1, Restore(Payload(), S(parent: otherGuid), Base(otherGuid)).CapturedRate);
});
Check("base save only reads its exact top level GUID property", () =>
{
    foreach (string json in new[] { "{}", "{\"guid\":\"" + guid + "\"}", "{\"nested\":{\"GUID\":\"" + guid + "\"}}", "{\"GUID\":\"invalid\"}", "{\"GUID\":\"" + guid + "\",\"GUID\":\"" + guid + "\"}" })
        Near(1, Restore(Payload(), baseJson: json).CapturedRate);
});
Check("kind index fingerprint and integer native progress must match", () =>
{
    Near(1, Restore(Payload(MachineTimerKind.Oven)).CapturedRate);
    Near(1, Restore(Payload(index: 1)).CapturedRate);
    Near(1, Restore(Payload(fingerprint: "changed")).CapturedRate);
    Near(1, Restore(Payload(progress: 11)).CapturedRate);
    Near(1, Restore(Payload(progress: 10.00001)).CapturedRate);
});
Check("float timers accept bounded native serialization rounding only", () =>
{
    foreach (var kind in new[] { MachineTimerKind.Drying })
    {
        Near(2, Restore(Payload(kind, progress: .5), S(kind, progress: .50009)).CapturedRate);
        Near(1, Restore(Payload(kind, progress: .5), S(kind, progress: .5002)).CapturedRate);
        Near(2, Restore(Payload(kind, progress: 1000), S(kind, progress: 1000.09)).CapturedRate);
    }
});
Check("an invalid entry rejects the whole payload", () =>
{
    string valid = Payload();
    foreach (string bad in new[] { "0", "-2", "1e999", "\"NaN\"" })
        Near(1, Restore(valid.Replace("\"Rate\":2", "\"Rate\":" + bad)).CapturedRate);
    foreach (string bad in new[] { "-0.1", "1", "1e999", "\"NaN\"" })
        Near(1, Restore(valid.Replace("\"Fraction\":0.25", "\"Fraction\":" + bad)).CapturedRate);
    string secondInvalid = valid.Replace("\"Entries\":[", "\"Entries\":[{\"Kind\":\"Oven\",\"Index\":0,\"Fingerprint\":\"other\",\"Progress\":2,\"Rate\":0,\"Fraction\":0},");
    Near(1, Restore(secondInvalid).CapturedRate);
});
Check("duplicate ambiguous slots and fields reject the whole payload", () =>
{
    string payload = Payload(); using var json = JsonDocument.Parse(payload);
    string entry = json.RootElement.GetProperty("Entries")[0].GetRawText();
    Near(1, Restore(payload.Replace(entry, entry + "," + entry)).CapturedRate);
    Near(1, Restore(payload.Replace("\"Rate\":2", "\"Rate\":2,\"Rate\":7")).CapturedRate);
    Near(1, Restore(payload.Replace("\"Version\":1", "\"Version\":1,\"Version\":1")).CapturedRate);
});
Check("nested legacy loader preserves its outer valid staged restore", () =>
{
    var p = New(); var outer = p.BeginLoad(MachineTimerKind.Chemistry, Base(), Payload());
    var inner = p.BeginLoad(MachineTimerKind.Chemistry, Base());
    Near(2, p.Capture(S(), 7).CapturedRate); p.EndLoad(inner, false);
    Expect(p.IsLoading(MachineTimerKind.Chemistry), "Inner scope popped outer scope");
    p.EndLoad(outer, false); Expect(!p.IsLoading(MachineTimerKind.Chemistry), "Outer scope leaked");
});
Check("failed scope discards pending data and removes load inhibition", () =>
{
    var p = New(); var scope = p.BeginLoad(MachineTimerKind.Chemistry, Base(), Payload());
    p.EndLoad(scope, true); p.EndLoad(scope, true);
    Expect(!p.IsLoading(MachineTimerKind.Chemistry), "Failed scope still loading");
    Near(1, p.GetOrAdopt(S()).CapturedRate); Near(7, p.Capture(S(identity: "new"), 7).CapturedRate);
});
Check("ending an outer scope cannot pop an unrelated nested scope", () =>
{
    var p = New(); var a = p.BeginLoad(MachineTimerKind.Chemistry, Base(), Payload());
    var b = p.BeginLoad(MachineTimerKind.Oven, Base()); p.EndLoad(a, true);
    Expect(p.IsLoading(MachineTimerKind.Oven), "Wrong scope popped");
    Near(1, p.Capture(S(MachineTimerKind.Oven), 7).CapturedRate); p.EndLoad(b, false);
    Near(7, p.Capture(S(MachineTimerKind.Oven, identity: "new"), 7).CapturedRate);
});
Check("new single operation discards old pending restore after load completes", () =>
{
    var p = New(); var scope = p.BeginLoad(MachineTimerKind.Chemistry, Base(), Payload()); p.EndLoad(scope, false);
    Near(7, p.Capture(S(identity: "new"), 7).CapturedRate);
    Near(1, p.GetOrAdopt(S(identity: "unrecognized")).CapturedRate);
});
Check("drying positions with identical fingerprints restore independent captured rates", () =>
{
    var source = New(); var a = S(MachineTimerKind.Drying, progress: 5); var b = S(MachineTimerKind.Drying, progress: 5, index: 1);
    source.Capture(a, .5); source.Capture(b, 7); string payload = source.Serialize(new[] { a, b });
    var p = New(); var scope = p.BeginLoad(MachineTimerKind.Drying, Base(), payload); p.EndLoad(scope, false);
    Near(7, p.GetOrAdopt(b with { Identity = "loaded-b" }).CapturedRate);
    Near(.5, p.GetOrAdopt(a with { Identity = "loaded-a" }).CapturedRate);
    Near(.5, p.GetOrAdopt(a with { Identity = "loaded-a", Progress = 6 }).CapturedRate);
    Near(1, p.GetOrAdopt(a with { Identity = "replacement" }).CapturedRate);
});
Check("explicit new drying item leaves other positions pending", () =>
{
    var source = New(); var a = S(MachineTimerKind.Drying); var b = S(MachineTimerKind.Drying, index: 1);
    source.Capture(a, 2); source.Capture(b, .5);
    var p = New(); var scope = p.BeginLoad(MachineTimerKind.Drying, Base(), source.Serialize(new[] { a, b })); p.EndLoad(scope, false);
    Near(7, p.Capture(a with { Identity = "new" }, 7).CapturedRate);
    Near(.5, p.GetOrAdopt(b with { Identity = "restored" }).CapturedRate);
});
Check("adoption consumes mismatched pending position rather than resurrecting it later", () =>
{
    var p = New(); var scope = p.BeginLoad(MachineTimerKind.Drying, Base(), Payload(MachineTimerKind.Drying)); p.EndLoad(scope, false);
    Near(1, p.GetOrAdopt(S(MachineTimerKind.Drying, progress: 11)).CapturedRate);
    Near(1, p.GetOrAdopt(S(MachineTimerKind.Drying, identity: "later", progress: 10)).CapturedRate);
});
Check("clear removes ledger pending restores and load scopes", () =>
{
    var p = New(); p.Capture(S(), 2);
    var scope = p.BeginLoad(MachineTimerKind.Drying, Base(), Payload(MachineTimerKind.Drying));
    p.Clear(); p.EndLoad(scope, false);
    Near(1, p.GetOrAdopt(S()).CapturedRate); Near(1, p.GetOrAdopt(S(MachineTimerKind.Drying)).CapturedRate);
    Expect(!p.IsLoading(MachineTimerKind.Drying), "Clear left a load scope");
});
Check("growth no longer writes per-crop captured-rate metadata", () =>
{
    foreach (var kind in new[] { MachineTimerKind.PlantGrowth, MachineTimerKind.ShroomGrowth })
        Expect(New().Serialize(new[] { S(kind) }) == "", "Crop wrote obsolete captured-rate metadata");
});
Check("oversized deeply nested and oversized entry arrays safely use legacy rate", () =>
{
    Near(1, Restore(new string(' ', 1_048_577)).CapturedRate);
    Near(1, Restore(new string('[', 40) + "0" + new string(']', 40)).CapturedRate);
    string entries = string.Join(",", Enumerable.Range(0, 4097).Select(i => $"{{\"Kind\":\"Drying\",\"Index\":{i},\"Fingerprint\":\"x\",\"Progress\":0,\"Rate\":2,\"Fraction\":0}}"));
    Near(1, Restore($"{{\"Version\":1,\"Guid\":\"{guid}\",\"Entries\":[{entries}]}}").CapturedRate);
});
Check("invalid source metadata cannot write transferable save state", () =>
{
    var p = New(); Expect(p.Serialize(new[] { S(parent: "bad") }) == "", "Invalid parent saved");
    Expect(p.Serialize(new[] { S(), S(index: 1, parent: otherGuid) }) == "", "Mixed parent payload saved");
    Expect(p.Serialize(Array.Empty<MachineTimerSnapshot>()) == "", "Empty operation set wrote a payload");
});

Check("known ledger identities cannot leave pending save data for later replacements", () =>
{
    foreach (bool capture in new[] { false, true })
    {
        var ledger = new MachineTimerLedger(); var p = new MachineTimerPersistence(ledger);
        ledger.Capture(S().Key, S().Identity, .5);
        var scope = p.BeginLoad(MachineTimerKind.Chemistry, Base(), Payload()); p.EndLoad(scope, false);
        Near(.5, (capture ? p.Capture(S(), 7) : p.GetOrAdopt(S())).CapturedRate);
        Near(1, p.GetOrAdopt(S(identity: "replacement")).CapturedRate);
    }
});

Console.WriteLine($"{checks - failures}/{checks} machine-save checks passed");
return failures == 0 ? 0 : 1;
