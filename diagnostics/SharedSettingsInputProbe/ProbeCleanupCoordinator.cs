using System;

namespace Skoofidons.SettingsProbe
{
    // Owns cleanup ordering and retry progress; native operations stay in the probe.
    public sealed class ProbeCleanupCoordinator
    {
        private bool stateRemoved, exitReleased, completed;
        public bool Pending { get; private set; }

        public bool TryComplete(bool ownerAlive, Action removeOwned, Action releaseExit, Action destroyOwned, out Exception error)
        {
            error = null;
            if (completed) return true;
            Pending = true;
            try
            {
                if (!stateRemoved)
                {
                    if (ownerAlive) removeOwned();
                    stateRemoved = true;
                }
                if (!exitReleased)
                {
                    releaseExit();
                    exitReleased = true;
                }
                destroyOwned();
                completed = true;
                Pending = false;
                return true;
            }
            catch (Exception exception) { error = exception; return false; }
        }

        public void Reset()
        {
            stateRemoved = false; exitReleased = false; completed = false; Pending = false;
        }
    }
}
