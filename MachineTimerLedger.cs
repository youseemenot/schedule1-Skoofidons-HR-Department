using System;
using System.Collections.Generic;

namespace SkoofidonsHRDepartment
{
    public sealed record MachineTimerRecord
    {
        public string Identity { get; }
        public double CapturedRate { get; }
        public double Fraction { get; internal set; }
        internal MachineTimerRecord(string identity, double rate, double fraction)
        { Identity = identity; CapturedRate = rate; Fraction = fraction; }
    }

    public sealed class MachineTimerLedger
    {
        private readonly Dictionary<string, MachineTimerRecord> operations = new(StringComparer.Ordinal);

        public bool TryGet(string key, out MachineTimerRecord record) => operations.TryGetValue(key, out record);

        // Lookup must never capture a current preference: pre-existing and replacement
        // operations stay native unless a creation or validated restoration calls Capture.
        public MachineTimerRecord GetOrAdopt(string key, string identity)
        {
            if (operations.TryGetValue(key, out MachineTimerRecord record) && record.Identity == identity) return record;
            record = new MachineTimerRecord(identity, 1, 0);
            operations[key] = record;
            return record;
        }

        public void Capture(string key, string identity, double rate, double fraction = 0) =>
            operations[key] = new MachineTimerRecord(identity, MachineTimerPolicy.NormalizeRate(rate),
                MachineTimerPolicy.ValidFraction(fraction) ? fraction : 0);

        public bool TryAdvance(string key, string identity, int elapsed, int remaining, out int credit)
        {
            credit = elapsed;
            MachineTimerRecord record = GetOrAdopt(key, identity);
            if (record.CapturedRate == 1) return false;
            if (!MachineTimerPolicy.TryCredit(elapsed, record.CapturedRate, record.Fraction, remaining,
                out credit, out double nextFraction)) return false;
            record.Fraction = nextFraction;
            return true;
        }

        public void Forget(string key) => operations.Remove(key);
        public void Clear() => operations.Clear();
    }
}
