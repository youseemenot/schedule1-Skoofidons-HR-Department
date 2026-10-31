#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skoofidons.SharedSettings.V1
{
    public interface IMenuInputLease
    {
        bool IsAlive { get; }
        bool IsTop { get; }
        bool TryRelease(out string? error);
    }

    // Pure session policy: native ownership and rendering live outside this class.
    public sealed class MenuSession
    {
        private readonly IReadOnlyList<ProviderManifest> providers;
        private readonly Func<IMenuInputLease> acquire;
        private readonly HashSet<string> visited = new(), disabled = new();
        private readonly ReleaseBarrier barrier = new();
        private IMenuInputLease? lease;
        private int lastToggle = -1;
        private bool closedCallbacks, emergency;
        public ProviderManifest? Selected { get; private set; }
        public bool HasLease => lease != null;
        public bool Closing { get; private set; }
        public bool Visible => lease != null && lease.IsAlive && lease.IsTop && !Closing;
        public string? Error { get; private set; }
        public MenuSession(IReadOnlyList<ProviderManifest> providers, Func<IMenuInputLease> acquire)
        { this.providers = providers; this.acquire = acquire; }

        public void Toggle(int frame)
        {
            if (lastToggle == frame) return;
            lastToggle = frame;
            if (HasLease) { RequestClose(frame); return; }
            Selected = providers.FirstOrDefault(p => !disabled.Contains(p.Id));
            if (Selected == null) return;
            Error = null; visited.Clear(); closedCallbacks = false; emergency = false;
            Closing = false; barrier.Reset();
            try
            {
                lease = acquire();
                if (!lease.IsAlive) { Error = "Input state acquisition failed."; Abort(frame); return; }
                Visit(Selected, frame);
            }
            catch (Exception e) { Error = e.Message; if (lease != null) Abort(frame); }
        }
        public void Select(string id, int frame)
        {
            if (!Visible || disabled.Contains(id)) return;
            var next = providers.FirstOrDefault(p => p.Id == id);
            if (next == null) return;
            Selected = next; Visit(next, frame);
        }
        private void Visit(ProviderManifest p, int frame)
        {
            if (!visited.Add(p.Id)) return;
            try { p.Opened(); }
            catch (Exception e) { FailProvider(e, frame); }
        }
        public void RequestClose(int frame)
        {
            if (lease == null || Closing) return;
            Closing = true; barrier.Begin(frame);
        }
        public void Tick(int frame, bool held, bool focused)
        {
            if (lease == null) return;
            if (!lease.IsAlive) { Abort(frame); return; }
            if (!focused) RequestClose(frame);
            if (emergency || Closing && barrier.Ready(frame, held, lease.IsTop, focused)) Release();
        }
        public void Draw(int frame)
        {
            if (!Visible || Selected == null) return;
            try { Selected.Draw(); }
            catch (Exception e) { FailProvider(e, frame); }
        }
        public void FailProvider(Exception e, int frame)
        {
            Error = e.Message;
            if (Selected != null) disabled.Add(Selected.Id);
            RequestClose(frame);
        }
        public void Abort(int frame)
        {
            if (lease == null) return;
            emergency = true; RequestClose(frame); Release();
        }
        private void Release()
        {
            if (!closedCallbacks)
            {
                closedCallbacks = true;
                foreach (var p in providers.Where(p => visited.Contains(p.Id)))
                    try { p.Closed(); } catch (Exception e) { Error = e.Message; }
            }
            try
            {
                if (!lease!.TryRelease(out var error)) { Error = error; emergency = true; return; }
                lease = null; Closing = false; emergency = false; barrier.Reset();
            }
            catch (Exception e) { Error = e.Message; emergency = true; }
        }
    }
}
