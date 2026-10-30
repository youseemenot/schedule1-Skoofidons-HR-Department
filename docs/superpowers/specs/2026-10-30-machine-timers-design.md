# Machine and growth speed factors

User request: resume the reserved machine-timer block. A larger multiplier shortens time, as with employee work speed. The user explicitly selected **drying plus plant/fungal growth**, and **new operations only**. This supersedes the preliminary remaining-work assumption.

Seven independent controls in Machine Timers: chemistry table, lab oven, mixing stations including Mk2, cauldron, drying rack, plant growth, mushroom growth. Default1; 2 means twice the affected progress rate, 7 seven times, 0.5 half. The active operation captures its factor once and keeps it across preference changes and save/load. Existing/unrecognized operations and saves without HR metadata use1. Employee speed, preparation/recovery pauses, water consumption and native resource/product quantities remain independent. Singleplayer only; no publication before user testing.

## Approach and evidence

Duration-getter overrides were rejected: native readiness inlines recipe/quantity/duration, shared bodies exist, and recipe objects are shared. Use scoped elapsed-progress adapters while preserving original completion logic. Audit evidence: artifacts/machine-timers-research-2026-10-30, exact current GameAssembly SHA512 recorded there.

- Chemistry: custom active factor credits native CurrentTime then calls original OnTimePass(0) to preserve its threshold/finalization path; neutral1 is strictly native. Cauldron/oven/mixing scale credited integer minutes, clamp to remaining and carry fractional credit. Oven normal minute callback redirects once to its equivalent OnTimePass(1), only for custom active factors. Mk2 inherits the single base mixing adapter.
- Drying stores float elapsed natively: apply only the future delta adjustment n×warmth×(capturedFactor−1), then native handles quality/output. Preserve vanilla per-call quality-transition cap.
- Plant/fungal growth setters scale only positive growth deltas while inside the audited growth callback context. Vanilla checks, clamps and completion remain. No upstream clamp exists; slowdown reaches completion. Do not scale whole Pot/GrowContainer or water consumption.
- Capture at actual RPC operation assignment/new object creation, not constructors or first time tick. For plants, Pot.LoadPlant schedules asynchronous creation: a persistent per-pot restore marker survives the coroutine. Mushroom colonies are destroyed on full harvest; partial harvesting retains the existing factor. Drying needs one captured factor per simultaneous operation.
- Parent DynamicSaveData gets a versioned raw-string JSON payload with captured factor, native snapshot/fingerprint and integer fractional credit. Match parent GUID and operation when restoring; invalid/absent metadata is1. Existing game save fields retain native progress. Load scope and persistent plant/drying restore markers prevent treating restored operations as new.
- ETA displays use captured factors; progress bars retain native progress. Machine/growth completion stays quantized by native ticks. Invalid factors fail back to1. Never replay completion/resource mutations or patch shared constructors/getters.

Register only audited unique methods manually after binary/pointer/API/conflicting-owner validation. Registration failure removes the whole machine block's patches and logs inactivity; staff features remain available. Clean runtime state on scene unload/deinitialize. Preserve the stable1.1.0 DLL/preferences for rollback.

## Scope limits

No additional passive spawn-station timer exists; its action is already employee work and must not be multiplied again. Global time, animation, scheduler, watering and unrelated equipment are not accelerated. Co-op/network synchronization is deferred. Save roundtrip, real native detours, new-only capture, completion/output and visual ETA require the combined gameplay test.
