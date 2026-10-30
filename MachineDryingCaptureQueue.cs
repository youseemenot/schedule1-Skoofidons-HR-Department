using System;
using System.Collections.Generic;

namespace SkoofidonsHRDepartment
{
    // The native broadcast serializes operations, so receiver object pointers cannot
    // identify the server request. Associate only reached new broadcasts with their
    // ordered matching receiver snapshots; targeted synchronization never enqueues.
    public sealed class MachineDryingCaptureQueue
    {
        private sealed record Request(long Ticket, string Fingerprint, double Progress, double Rate, long Created);
        private readonly Dictionary<string, List<Request>> pending = new(StringComparer.OrdinalIgnoreCase);
        private readonly Func<long> milliseconds;
        private long nextTicket;
        private const long LifetimeMilliseconds = 30_000;
        private const int MaximumPendingPerRack = 512;
        public MachineDryingCaptureQueue() : this(() => Environment.TickCount64) { }
        public MachineDryingCaptureQueue(Func<long> milliseconds) => this.milliseconds = milliseconds ?? throw new ArgumentNullException(nameof(milliseconds));

        public long Enqueue(string rack, string fingerprint, double progress, double rate)
        {
            if (string.IsNullOrEmpty(rack) || fingerprint == null || !double.IsFinite(progress)) return 0;
            if (!pending.TryGetValue(rack, out var requests)) pending[rack] = requests = new();
            Prune(requests);
            if (requests.Count >= MaximumPendingPerRack) requests.RemoveAt(0);
            long ticket = ++nextTicket;
            requests.Add(new(ticket, fingerprint, progress, MachineTimerPolicy.NormalizeRate(rate), milliseconds()));
            return ticket;
        }

        public bool TryTake(string rack, string fingerprint, double progress, out double rate)
        {
            rate = 1;
            if (rack == null || !double.IsFinite(progress) || !pending.TryGetValue(rack, out var requests)) return false;
            Prune(requests);
            for (int i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                if (request.Fingerprint != fingerprint || Math.Abs(request.Progress - progress) > 0.0001 * Math.Max(1, Math.Abs(progress))) continue;
                rate = request.Rate;
                requests.RemoveAt(i);
                if (requests.Count == 0) pending.Remove(rack);
                return true;
            }
            if (requests.Count == 0) pending.Remove(rack);
            return false;
        }

        public void Cancel(long ticket)
        {
            if (ticket == 0) return;
            var empty = new List<string>();
            foreach (var pair in pending)
            {
                pair.Value.RemoveAll(request => request.Ticket == ticket);
                if (pair.Value.Count == 0) empty.Add(pair.Key);
            }
            foreach (string key in empty) pending.Remove(key);
        }
        public void Clear() => pending.Clear();
        private void Prune(List<Request> requests)
        {
            long now = milliseconds();
            requests.RemoveAll(request => now < request.Created || now - request.Created >= LifetimeMilliseconds);
        }
    }
}
