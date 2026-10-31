using System;
using System.Collections.Generic;
using System.Reflection;

namespace SkoofidonsHRDepartment
{
    // Pure CLR boundary: loading HR never resolves the optional Phone App assembly.
    public sealed class OptionalSettingsEvents : IDisposable
    {
        private readonly List<(EventInfo Event, Delegate Handler)> bindings = new();
        private readonly Action<string> warning;
        private OptionalSettingsEvents(Action<string> warning) => this.warning = warning;

        public static OptionalSettingsEvents TrySubscribe(Type eventType, Action onSaved, Action<string> warning = null)
        {
            if (eventType == null) return null;
            if (onSaved == null) throw new ArgumentNullException(nameof(onSaved));
            var connection = new OptionalSettingsEvents(warning);
            try
            {
                var events = new EventInfo[2];
                string[] names = { "OnPhonePreferencesSaved", "OnMenuPreferencesSaved" };
                for (int i = 0; i < names.Length; i++)
                {
                    var item = eventType.GetEvent(names[i], BindingFlags.Public | BindingFlags.Static);
                    if (item?.EventHandlerType != typeof(Action) || item.AddMethod?.IsStatic != true || item.RemoveMethod?.IsStatic != true)
                        throw new InvalidOperationException("Unsupported Phone App event: " + names[i]);
                    events[i] = item;
                }
                foreach (var item in events)
                {
                    connection.bindings.Add((item, onSaved));
                    item.AddEventHandler(null, onSaved);
                }
                return connection;
            }
            catch (Exception error)
            {
                connection.Dispose();
                warning?.Invoke("Optional Phone App events inactive: " + (error.InnerException ?? error).Message);
                return null;
            }
        }

        public void Dispose()
        {
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                try { bindings[i].Event.RemoveEventHandler(null, bindings[i].Handler); }
                catch (Exception error) { warning?.Invoke("Optional Phone App event cleanup: " + (error.InnerException ?? error).Message); }
            }
            bindings.Clear();
        }
    }
}
