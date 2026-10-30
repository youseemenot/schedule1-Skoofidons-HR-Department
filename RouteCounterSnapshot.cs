using System;

namespace SkoofidonsHRDepartment
{
    // Pure presentation rules; no Unity objects or native state are changed here.
    public sealed class RouteCounterSnapshot
    {
        public int Used { get; }
        public int Limit { get; }
        public int Available => Math.Max(0, Limit - Used);
        public bool CanAdd => Used < Limit;

        private RouteCounterSnapshot(int used, int limit)
        {
            Used = Math.Max(0, used);
            Limit = Math.Max(0, limit);
        }

        public static RouteCounterSnapshot ForSelection(int fieldCount, int used, int limit) =>
            fieldCount == 1 ? new RouteCounterSnapshot(used, limit) : null;

        public string FormatLabel(string caption) => $"{caption} ({Used}/{Limit}; free {Available})";
    }
}
