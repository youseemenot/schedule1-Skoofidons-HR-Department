# Machine and growth speed factors

Current design for local1.2.0-beta.2. The user selected drying and plant/fungal growth as well as chemistry/oven/mixing/cauldron. After architecture review, the user changed growth to affect all living crops. The five machine kinds retain the earlier new-batch policy. Singleplayer only.

Seven independent preferences, default1. Higher is faster: 7 means seven times the affected progress per native tick,0.5 half. Employee work, fixed pauses and global time remain independent.

## Architecture and operation boundaries

Growth is a continuous native progress calculation. Plant.MinPass and ShroomColony.OnMinPass/OnTimeSkipped retain environmental checks and request a positive progress delta through their setters. Scope the current preference to that callback and multiply only this future delta. Existing progress is preserved; loading/resetting/harvesting setters outside growth callbacks remain native. The next callback reads the current preference for every existing/new/loaded crop. No per-crop factor, creation hook, loader marker or extra crop save metadata is needed. Native unclamped requests allow slow growth to reach completion; final clamp remains native.

For machine batches, capture the configured factor at actual operation assignment and retain it through preference changes and save/load. Existing/unrecognized batches without matching metadata use1. This behavior is explicitly requested even though it needs additional state. Reuse the native progress fields and completion logic rather than shared duration getters/recipe objects.

- Chemistry credits CurrentTime then calls original OnTimePass(0) for custom factors; factor1 is strictly native.
- Oven/cauldron/mixing scale credited integer minutes with fractional carry and remaining-work clamp. Oven's distinct minute path redirects once to its equivalent time callback; Mk2 uses the base mixing adapter once.
- Drying adds only future adjustment n×warmth×(capturedFactor−1), preserving native per-call quality transition/reset behavior.
- Capture machine assignments at audited RPC paths. Drying's reached broadcast method serializes new native objects, so a bounded queue matches GUID/fingerprint/progress rather than pointers. Do not rely on the inlined SendOperation method.
- Five parent GetSaveData hooks add versioned raw-string DynamicSaveData with parent GUID, operation fingerprint/progress, factor and fractional credit. Validate matching metadata; malformed/missing/future metadata uses1. Native progress remains in native save fields. Crop metadata from beta.1 is ignored.
- Machine ETA displays use each batch's captured factor. Completion remains quantized by native ticks. Do not replay resource/finalization mutations.

36 unique native methods are pinned to the audited GameAssembly and validated against metadata pointers and conflicting patch owners before registration. Failure removes the machine block's patches while staff features remain. Clear transient state on scene unload/deinitialize. Three guarded managed Phone App sorting selectors provide HR category order without changing IDs or values.

## Decision rule

Follow [project architecture guidance](../../DEVELOPMENT.md): establish native behavior before offering choices, recommend the approach that fits it, and explain additional state/compatibility costs. Earlier new-crop capture was avoidable complexity; beta.2 removes it. Preserve stable DLL/preferences for rollback. Actual detours, UI, output and save I/O require gameplay checks.
