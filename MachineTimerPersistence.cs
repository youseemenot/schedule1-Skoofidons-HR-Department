using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SkoofidonsHRDepartment
{
    public sealed record MachineTimerSnapshot(MachineTimerKind Kind, string Key, string Identity, string Guid,
        string Fingerprint, double Progress, int Remaining, int Index = 0);

    // Managed save state only; native assignment and save API access belong to adapters.
    public sealed class MachineTimerPersistence
    {
        private const int MaxJsonLength = 1_048_576, MaxEntries = 4096, MaxFingerprintLength = 4096;
        private static readonly JsonDocumentOptions JsonOptions = new() { MaxDepth = 16 };
        private readonly MachineTimerLedger ledger;
        private readonly List<LoadScope> scopes = new();
        private readonly Dictionary<string, Pending> pending = new(StringComparer.Ordinal);
        private readonly Dictionary<string, LoadScope> asyncRestores = new(StringComparer.Ordinal);

        public sealed class LoadScope
        {
            internal MachineTimerPersistence Owner { get; init; }
            internal MachineTimerKind Kind { get; init; }
            internal string ParentKey { get; init; }
            internal LoadScope Parent { get; init; }
        }
        private sealed record Entry(MachineTimerKind Kind, int Index, string Fingerprint, double Progress, double Rate, double Fraction);
        private sealed record SaveEntry(string Kind, int Index, string Fingerprint, double Progress, double Rate, double Fraction);
        private sealed record SavePayload(int Version, string Guid, List<SaveEntry> Entries);
        private sealed class Pending
        {
            internal LoadScope Owner;
            internal readonly Dictionary<int, Entry> Entries = new();
        }

        public MachineTimerPersistence(MachineTimerLedger ledger) => this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

        public MachineTimerRecord Capture(MachineTimerSnapshot snapshot, double configuredRate, bool allowAsyncRestore = false)
        {
            string parentKey = ParentKey(snapshot.Kind, snapshot.Guid);
            string markerKey = MarkerKey(snapshot.Kind, snapshot.Guid);
            bool asynchronous = allowAsyncRestore && asyncRestores.Remove(markerKey);
            if (ledger.TryGet(snapshot.Key, out MachineTimerRecord existing) && existing.Identity == snapshot.Identity)
            {
                DiscardPosition(snapshot, parentKey);
                if (!IsLoading(snapshot.Kind)) asyncRestores.Remove(markerKey);
                return existing;
            }
            bool loading = IsLoading(snapshot.Kind);
            double rate = configuredRate, fraction = 0;
            if (asynchronous || loading)
            {
                rate = 1;
                // Another parent's load must not unlock an unrelated stale save.
                if ((asynchronous || HasActiveParent(parentKey)) && TryConsume(snapshot, parentKey, out Entry restored))
                { rate = restored.Rate; fraction = restored.Fraction; }
                if (asynchronous && parentKey != null && snapshot.Kind != MachineTimerKind.Drying) pending.Remove(parentKey);
            }
            else
            {
                // Explicit creation after loading is new work, even if loading never
                // materialized the operation represented by an old pending entry.
                DiscardPosition(snapshot, parentKey);
                asyncRestores.Remove(markerKey);
            }
            ledger.Capture(snapshot.Key, snapshot.Identity, rate, fraction);
            return ledger.GetOrAdopt(snapshot.Key, snapshot.Identity);
        }

        public MachineTimerRecord GetOrAdopt(MachineTimerSnapshot snapshot)
        {
            string parentKey = ParentKey(snapshot.Kind, snapshot.Guid);
            if (ledger.TryGet(snapshot.Key, out MachineTimerRecord existing) && existing.Identity == snapshot.Identity)
            {
                DiscardPosition(snapshot, parentKey);
                return existing;
            }
            if (TryConsume(snapshot, parentKey, out Entry restored))
            {
                ledger.Capture(snapshot.Key, snapshot.Identity, restored.Rate, restored.Fraction);
                return ledger.GetOrAdopt(snapshot.Key, snapshot.Identity);
            }
            return ledger.GetOrAdopt(snapshot.Key, snapshot.Identity);
        }

        public string Serialize(IEnumerable<MachineTimerSnapshot> snapshots)
        {
            if (snapshots == null) return string.Empty;
            string guid = null;
            var entries = new List<SaveEntry>();
            var slots = new HashSet<(MachineTimerKind, int)>();
            foreach (MachineTimerSnapshot snapshot in snapshots)
            {
                string currentGuid = NormalizeGuid(snapshot.Guid);
                if (entries.Count >= MaxEntries || currentGuid == null || (guid != null && guid != currentGuid) ||
                    !ValidSnapshot(snapshot) || !slots.Add((snapshot.Kind, snapshot.Index))) return string.Empty;
                guid = currentGuid;
                MachineTimerRecord record = GetOrAdopt(snapshot);
                entries.Add(new SaveEntry(snapshot.Kind.ToString(), snapshot.Index, snapshot.Fingerprint, snapshot.Progress,
                    MachineTimerPolicy.NormalizeRate(record.CapturedRate), MachineTimerPolicy.ValidFraction(record.Fraction) ? record.Fraction : 0));
            }
            if (entries.Count == 0) return string.Empty;
            string result = JsonSerializer.Serialize(new SavePayload(1, guid, entries));
            return result.Length <= MaxJsonLength ? result : string.Empty;
        }

        public LoadScope BeginLoad(MachineTimerKind kind, string baseJson, string payload = null)
        {
            string guid = ReadBaseGuid(baseJson);
            string key = ParentKey(kind, guid);
            var token = new LoadScope { Owner = this, Kind = kind, ParentKey = key, Parent = scopes.Count == 0 ? null : scopes[^1] };
            bool preserveOuter = key != null && pending.TryGetValue(key, out Pending previous) && scopes.Contains(previous.Owner);
            List<Entry> parsed = ParsePayload(payload, guid);
            if (key != null)
            {
                if (parsed != null)
                {
                    var staged = new Pending { Owner = token };
                    foreach (Entry entry in parsed) if (entry.Kind == kind) staged.Entries.Add(entry.Index, entry);
                    pending[key] = staged;
                    asyncRestores.Remove(key);
                }
                else if (!preserveOuter)
                {
                    pending.Remove(key);
                    asyncRestores.Remove(key);
                }
            }
            scopes.Add(token);
            return token;
        }

        public void EndLoad(LoadScope token, bool failed)
        {
            if (token == null || token.Owner != this || !scopes.Remove(token)) return;
            if (!failed) return;
            var discard = new List<string>();
            foreach (var pair in pending) if (Within(pair.Value.Owner, token)) discard.Add(pair.Key);
            foreach (string key in discard) pending.Remove(key);
            discard.Clear();
            foreach (var pair in asyncRestores) if (Within(pair.Value, token)) discard.Add(pair.Key);
            foreach (string key in discard) asyncRestores.Remove(key);
        }

        public void MarkAsyncRestore(MachineTimerKind kind, string guid)
        {
            string key = ParentKey(kind, guid);
            LoadScope owner = null;
            for (int i = scopes.Count - 1; i >= 0; i--)
                if (scopes[i].Kind == kind) { owner = scopes[i]; break; }
            // A malformed base GUID cannot authorize a previous load's metadata,
            // but still needs a one-shot legacy marker owned by this failing scope.
            if (key != null && owner != null && owner.ParentKey != key) pending.Remove(key);
            asyncRestores[MarkerKey(kind, guid)] = owner;
        }

        public bool IsLoading(MachineTimerKind kind)
        {
            foreach (LoadScope scope in scopes) if (scope.Kind == kind) return true;
            return false;
        }

        public void Clear()
        {
            scopes.Clear(); pending.Clear(); asyncRestores.Clear(); ledger.Clear();
        }

        private bool HasActiveParent(string key)
        {
            if (key == null) return false;
            foreach (LoadScope scope in scopes) if (scope.ParentKey == key) return true;
            return false;
        }

        private bool TryConsume(MachineTimerSnapshot snapshot, string key, out Entry entry)
        {
            entry = null;
            if (key == null || !pending.TryGetValue(key, out Pending staged) || !staged.Entries.Remove(snapshot.Index, out Entry candidate)) return false;
            if (staged.Entries.Count == 0) pending.Remove(key);
            if (!ValidSnapshot(snapshot) || candidate.Kind != snapshot.Kind || candidate.Fingerprint != snapshot.Fingerprint ||
                !ProgressMatches(snapshot.Kind, candidate.Progress, snapshot.Progress)) return false;
            entry = candidate;
            return true;
        }

        private void DiscardPosition(MachineTimerSnapshot snapshot, string key)
        {
            if (key == null) return;
            if (snapshot.Kind != MachineTimerKind.Drying) pending.Remove(key);
            else if (pending.TryGetValue(key, out Pending staged))
            {
                staged.Entries.Remove(snapshot.Index);
                if (staged.Entries.Count == 0) pending.Remove(key);
            }
        }

        private static bool Within(LoadScope scope, LoadScope ancestor)
        {
            for (; scope != null; scope = scope.Parent) if (scope == ancestor) return true;
            return false;
        }

        private static bool ProgressMatches(MachineTimerKind kind, double saved, double current) =>
            FloatingProgress(kind) ? Math.Abs(saved - current) <= 1e-4 * Math.Max(1, Math.Abs(current)) : saved == current;
        private static bool FloatingProgress(MachineTimerKind kind) =>
            kind == MachineTimerKind.Drying || kind == MachineTimerKind.PlantGrowth || kind == MachineTimerKind.ShroomGrowth;
        private static bool ValidSnapshot(MachineTimerSnapshot snapshot) =>
            snapshot != null && Enum.IsDefined(typeof(MachineTimerKind), snapshot.Kind) && snapshot.Index >= 0 &&
            !string.IsNullOrEmpty(snapshot.Fingerprint) && snapshot.Fingerprint.Length <= MaxFingerprintLength &&
            double.IsFinite(snapshot.Progress) && snapshot.Progress >= 0 &&
            (FloatingProgress(snapshot.Kind) || snapshot.Progress == Math.Truncate(snapshot.Progress));
        private static string NormalizeGuid(string guid) =>
            guid != null && guid.Length <= 64 && System.Guid.TryParse(guid, out System.Guid parsed) && parsed != System.Guid.Empty ? parsed.ToString("D") : null;
        private static string ParentKey(MachineTimerKind kind, string guid)
        {
            string normalized = NormalizeGuid(guid);
            return normalized == null ? null : ((int)kind).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + normalized;
        }
        private static string MarkerKey(MachineTimerKind kind, string guid) => ParentKey(kind, guid) ??
            ((int)kind).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":invalid";

        private static string ReadBaseGuid(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > MaxJsonLength) return null;
            try
            {
                using JsonDocument document = JsonDocument.Parse(json, JsonOptions);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
                string guid = null; bool found = false;
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (property.Name != "GUID") continue;
                    if (found || property.Value.ValueKind != JsonValueKind.String) return null;
                    found = true; guid = NormalizeGuid(property.Value.GetString());
                }
                return guid;
            }
            catch (JsonException) { return null; }
        }

        private static List<Entry> ParsePayload(string json, string expectedGuid)
        {
            if (expectedGuid == null || string.IsNullOrEmpty(json) || json.Length > MaxJsonLength) return null;
            try
            {
                using JsonDocument document = JsonDocument.Parse(json, JsonOptions);
                JsonElement root = document.RootElement;
                if (!UniqueObject(root) || !root.TryGetProperty("Version", out var version) || !version.TryGetInt32(out int number) || number != 1 ||
                    !root.TryGetProperty("Guid", out var parent) || parent.ValueKind != JsonValueKind.String || NormalizeGuid(parent.GetString()) != expectedGuid ||
                    !root.TryGetProperty("Entries", out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > MaxEntries) return null;
                var entries = new List<Entry>();
                var slots = new HashSet<(MachineTimerKind, int)>();
                foreach (JsonElement item in array.EnumerateArray())
                {
                    if (!UniqueObject(item) || !item.TryGetProperty("Kind", out var kindValue) || kindValue.ValueKind != JsonValueKind.String ||
                        !Enum.TryParse(kindValue.GetString(), false, out MachineTimerKind kind) || !Enum.IsDefined(typeof(MachineTimerKind), kind) || kind.ToString() != kindValue.GetString() ||
                        !item.TryGetProperty("Index", out var indexValue) || !indexValue.TryGetInt32(out int index) || index < 0 || !slots.Add((kind, index)) ||
                        !item.TryGetProperty("Fingerprint", out var fingerprint) || fingerprint.ValueKind != JsonValueKind.String ||
                        !item.TryGetProperty("Progress", out var progressValue) || !Number(progressValue, out double progress) ||
                        !item.TryGetProperty("Rate", out var rateValue) || !Number(rateValue, out double rate) || rate <= 0 ||
                        !item.TryGetProperty("Fraction", out var fractionValue) || !Number(fractionValue, out double fraction) || !MachineTimerPolicy.ValidFraction(fraction)) return null;
                    string fp = fingerprint.GetString();
                    if (!ValidSnapshot(new MachineTimerSnapshot(kind, "", "", expectedGuid, fp, progress, 0, index))) return null;
                    entries.Add(new Entry(kind, index, fp, progress, rate, fraction));
                }
                return entries;
            }
            catch (JsonException) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (FormatException) { return null; }
        }

        private static bool UniqueObject(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject()) if (!names.Add(property.Name)) return false;
            return true;
        }
        private static bool Number(JsonElement element, out double value)
        {
            value = 0;
            return element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value) && double.IsFinite(value);
        }
    }
}
