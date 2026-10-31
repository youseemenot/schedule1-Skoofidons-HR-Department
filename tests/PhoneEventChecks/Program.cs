using System.Reflection;

int checks = 0, failed = 0;
void Expect(bool value, string message) { if (!value) throw new Exception(message); }
void Check(string name, Action test)
{
    checks++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + (error.InnerException ?? error).Message); }
}
IDisposable Subscribe(Type type, Action callback, Action<string> warning = null)
{
    var helper = Assembly.GetExecutingAssembly().GetType("SkoofidonsHRDepartment.OptionalSettingsEvents")
        ?? throw new Exception("Optional event binding is not implemented");
    return (IDisposable)helper.GetMethod("TrySubscribe")!.Invoke(null, new object[] { type, callback, warning });
}
Check("both phone and menu save apply exactly once", () => {
    int calls = 0;
    using var binding = Subscribe(typeof(ValidEvents), () => calls++);
    Expect(binding != null, "Valid events were rejected");
    ValidEvents.Phone(); ValidEvents.Menu();
    Expect(calls == 2, "Save source lost or applied twice");
});
Check("disposing removes only our exact handlers and is idempotent", () => {
    int own = 0, other = 0; Action thirdParty = () => other++;
    ValidEvents.OnPhonePreferencesSaved += thirdParty;
    try {
        var binding = Subscribe(typeof(ValidEvents), () => own++);
        ValidEvents.Phone(); binding.Dispose(); binding.Dispose(); ValidEvents.Phone(); ValidEvents.Menu();
        Expect(own == 1 && other == 2, "Dispose leaked our handler or removed another mod's listener");
    } finally { ValidEvents.OnPhonePreferencesSaved -= thirdParty; }
});
Check("absent event type is a safe disconnected adapter", () =>
    Expect(Subscribe(null, () => { }) == null, "Absent app was treated as an error"));
Check("incompatible signatures bind neither save source", () => {
    var warnings = new List<string>();
    Expect(Subscribe(typeof(BadEvents), () => { }, warnings.Add) == null, "Incompatible event accepted");
    Expect(BadEvents.Added == 0 && warnings.Count == 1, "Adapter partially connected to an incompatible app");
});
Check("failed second event registration rolls back the first", () => {
    var warnings = new List<string>();
    Expect(Subscribe(typeof(ThrowingEvents), () => { }, warnings.Add) == null, "Failing adapter was returned");
    Expect(ThrowingEvents.PhoneListeners == 0 && warnings.Count == 1, "Partial subscription leaked");
});
Console.WriteLine($"{checks - failed}/{checks} optional-event checks passed");
return failed == 0 ? 0 : 1;

public static class ValidEvents
{
    public static event Action OnPhonePreferencesSaved;
    public static event Action OnMenuPreferencesSaved;
    public static void Phone() => OnPhonePreferencesSaved?.Invoke();
    public static void Menu() => OnMenuPreferencesSaved?.Invoke();
}
public static class BadEvents
{
    public static int Added;
    public static event Action OnPhonePreferencesSaved { add { Added++; } remove { Added--; } }
    public static event Action<int> OnMenuPreferencesSaved { add { } remove { } }
}
public static class ThrowingEvents
{
    public static int PhoneListeners;
    public static event Action OnPhonePreferencesSaved { add { PhoneListeners++; } remove { PhoneListeners--; } }
    public static event Action OnMenuPreferencesSaved { add { throw new InvalidOperationException("Incompatible addon"); } remove { } }
}
