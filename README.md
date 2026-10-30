# Skoofidon's HR Department

A MelonLoader mod for **Schedule I** that configures employee stats and assignment limits through Mod Manager & Phone App.

**Local candidate: 1.2.0-beta.2.** Five machine factors apply to new batches and survive saving/loading. Plant and mushroom factors apply to the future growth of **all growing cultures**, including existing and loaded crops; accumulated progress is preserved. Settings now group Botanist, Chemist, Cleaner, Handler, then Fixer and Machine Timers. See [the candidate checklist](docs/testing/2026-10-30-machine-timers-beta2.md). The user reported no problems in the checks performed on beta.1 ([result](docs/testing/2026-10-30-machine-beta1-result.md)); beta.2 still needs gameplay verification. Neither candidate is published; public stable remains 1.1.0.* Five machine factors are captured once per new batch. Existing batches keep their factor (legacy defaults to1); versioned save data stores factors/fractional credit. Plant/mushroom growth instead reads the current preference each native growth callback, including for old saves; no per-crop factor metadata is needed. Completion stays limited by native ticks; drying retains its one-quality-step-per-call limit on large skips. Full gameplay coverage remains unrecorded.

A MelonLoader mod for **Schedule I** that configures employee stats and assignment limits through Mod Manager & Phone App.

**Local candidate: 1.2.0-beta.2.** Five machine factors apply to new batches and survive saving/loading. Plant and mushroom factors apply to the future growth of **all growing cultures**, including existing and loaded crops; accumulated progress is preserved. Settings now group Botanist, Chemist, Cleaner, Handler, then Fixer and Machine Timers. See [the candidate checklist](docs/testing/2026-10-30-machine-timers-beta2.md). The user reported no problems in the checks performed on beta.1 ([result](docs/testing/2026-10-30-machine-beta1-result.md)); beta.2 still needs gameplay verification. Neither candidate is published; public stable remains 1.1.0.

Development decisions follow the [architecture-first project guidance](docs/DEVELOPMENT.md).
