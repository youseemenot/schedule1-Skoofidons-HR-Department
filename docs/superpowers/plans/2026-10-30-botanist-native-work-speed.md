# Botanist native work speed implementation plan

> **For agentic workers:** use superpowers:executing-plans inline, followed by one independent final review.

**Goal:** Finish employee Work speed: use one botanist factor for pot/fungal actions, drying-rack loading and spawn-station work. Preserve all fixed preparation/recovery/synchronization waits. Prepare a separate inactive machine-timer module outline.

**Authority/design:** The user explicitly selected this scope after the native process audit. Movement/work controls stay unchanged. Machine cooking/baking/mixing remain a separate deferred block. No new UI switches or per-action sliders.

**Architecture:** Add Botanist to EmployeeSpeedController's existing owned multiplicative FloatStack entry. Remove the obsolete five-action BotanistTimingController/Policy and their lifecycle registration to avoid M². Native GrowContainer.PerformAction, StartDryingRack and UseSpawnStation already divide by CurrentWorkSpeed. Existing Handler/Chemist and Cleaner integrations stay unchanged. Fixed waits, navigation, scheduler and machine clocks are not patched.

**Ruling:** StopDryingRack contains a single fixed 0.5s wait after the animation trigger, then bulk output transfer, rather than an independently scaled work timer. Keep it, as requested; do not promise full dryer unloading scales with M. Preserve the two fixed 0.5s supply pickup waits too. Old report's wording 'per step' is corrected by examining the full coroutine; no per-item unload loop exists there.

**Ruling:** The machine scaffold is an isolated future/MachineTimers/README.md contract with audited clocks and implementation gates, excluded from current compile/runtime/UI. No dummy controls or active overrides.

**Tech stack/base:** C#11/net6.0, installed Schedule I0.4.7f12 IL2CPP / MelonLoader0.7.3 / Phone App2.2.4; base73488aa, reuse current checkout. Candidate1.1.0-beta.4; no publication/co-op work.

## Steps

- [x] Actual-DLL regression checks: prove native BotanistWorkSpeed routing, absence of old timing hook/double division, lifecycle/cleanup and no machine settings. Observe RED on beta.3.
- [x] Implement the native work-stack route, delete obsolete hook/policy/legacy pure timing suite, update Core/save integration/version.
- [x] Add reproducible native coverage checks for all eight GrowContainer actions (five pot + three fungal), shared getter/division, dryer loading/spawn station and preserved fixed unload/supply waits. Run actual-DLL, employee, Cleaner, layout and API checks.
- [x] Update current README/test guide/coverage report, add machine module scaffold and execution ledger. Independent review; address material findings.
- [x] With game closed, back up beta.3/preferences, package/install beta.4, verify hashes, commit locally. Leave runtime acceptance to the user's combined singleplayer test.

Outcome: 109 checks pass, Release0warnings/errors, API209refs/13auto targets/0failures. Fresh independent review found no Critical/Important/Minor findings and verified native dataflow/absence of action overrides. Installed beta.4 at closed game; backups/hash recorded in artifacts/hr-botanist-work-1.1.0-beta.4/installation.json. No publication. See [execution ledger](../../testing/2026-10-30-botanist-work-execution.md).
