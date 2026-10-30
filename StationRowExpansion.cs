using System;

namespace SkoofidonsHRDepartment
{
    // Resource allocation is separate from Unity so retaining originals and
    // rolling back partial growth can be checked without running the game.
    public static class StationRowExpansion
    {
        public static void SetAssignedVisibility<T>(T[] entries, int assignedCount, Action<T, bool> setVisible)
        {
            for (int i = 0; i < entries.Length; i++) setVisible(entries[i], i < assignedCount);
        }

        public static T[] Expand<T>(T[] entries, int assignedCount, Func<T, int, T> create, Action<T> destroy)
        {
            if (assignedCount <= entries.Length) return entries;
            if (entries.Length == 0) throw new InvalidOperationException("No station row template is available.");
            var expanded = new T[assignedCount];
            Array.Copy(entries, expanded, entries.Length);
            int created = entries.Length;
            try
            {
                for (; created < expanded.Length; created++)
                    expanded[created] = create(entries[0], created);
                return expanded;
            }
            catch
            {
                for (int i = created - 1; i >= entries.Length; i--) destroy(expanded[i]);
                throw;
            }
        }
    }
}
