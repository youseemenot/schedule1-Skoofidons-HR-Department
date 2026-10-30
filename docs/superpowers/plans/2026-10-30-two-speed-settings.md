# Two employee speed settings implementation plan

> **For agentic workers:** use superpowers:executing-plans inline; one final review of the change.

**Goal:** Every profession exposes the same two speed controls: movement and work. The user's explicit request supplies the design: one work value for all five botanist actions, no opt-in switch or per-action settings.

**Architecture:** Retain existing audited native integrations. Config registers one WorkSpeedMultiplier (default 1) per profession, with the same Work speed label. Botanist registration is automatic only for a positive finite non-neutral value; the callback divides every supported original wait by that one value. The old enable flag and five registered preferences are removed. Keep movement and other profession behavior unchanged.

**Tech stack:** C# 11/net6.0, MelonLoader 0.7.3, Schedule I 0.4.7f12 IL2CPP. Existing checkout at f1860c1; no publication or co-op work.

**Decisions:** New botanist WorkSpeedMultiplier defaults to 1; do not guess how to combine five potentially different prior values. Preserve the other professions' existing WorkSpeedMultiplier keys and saved values. Back up preferences before installation. Passive machine production timers remain outside employee work speed.

## Implementation and evidence

- [x] Update actual-DLL schema check to require exactly four movement and four work preferences, no botanist switch/per-action fields; add neutral/invalid registration policy checks. Observe RED on beta.2.
- [x] Config: replace botanist fields with BotanistWorkSpeed and common Work speed label/helper for all four. BotanistTimingPolicy: RequiresPatch. Controller: consume common factor for registration and every supported action; remove old selector/switch dependencies. Bump candidate to 1.1.0-beta.3.
- [x] Build and run actual-DLL checks plus timing/employee/Cleaner/layout suites and API check. Review the final diff independently. Update README/current testing guide/changelog and historical notices.
- [x] With the game closed, back up beta.2 and preferences outside Mods, package/install beta.3 and verify hashes. Commit locally, no GitHub publication. Runtime acceptance remains pending.

Outcome: Release clean; 135 checks pass, API216/13/0failures. Independent review found no regressions and confirmed from local MelonLoader/PhoneApp IL that stored unregistered old keys do not create Phone App entries. Installed beta.3 with matching SHA256 8AB4D685126CA574BC9AB89BBBAA4E9762B744A903FC2B94BC5454B606F28BE4. See [execution record](../../testing/2026-10-30-two-speed-execution.md).
