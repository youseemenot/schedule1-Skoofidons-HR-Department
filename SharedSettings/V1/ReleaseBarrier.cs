namespace Skoofidons.SharedSettings.V1
{
    public sealed class ReleaseBarrier
    {
        private bool pending;
        private int requestedFrame;

        public void Begin(int frame)
        {
            if (pending) return;
            pending = true;
            requestedFrame = frame;
        }

        public bool Ready(int frame, bool anyInputHeld, bool ownsTop, bool focused) =>
            pending && frame > requestedFrame && ownsTop && (!focused || !anyInputHeld);

        public void Reset() => pending = false;
    }
}
