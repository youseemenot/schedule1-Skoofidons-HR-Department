# Legacy Minimap F3 adapter implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: use superpowers:executing-plans; implement inline with one final independent review.

**Goal:** Update HR alone so its shared F3 can configure an unchanged supported Minimap2.2.8 DLL.

**Architecture / approved specification:** Optional HR adapter identifies registered legacy Minimap by exact known DLL digest and validated members. Harmony rewrites only its F3 key-read; suppresses its old OnGUI and routes pending preference flushes through checked persistence. Its original map update/F4/settings application remain. HR renders the previously approved four-tab panel against reflected original settings. Native menu ownership stays in the shared runtime. Additional V1 provider manifests advertise this panel from its actual owner HR assembly, allowing future compatible hosts to discover it. Modern Minimap providers bypass the adapter.

**Tech stack:** C#11/net6, MelonLoader0.7.3, Harmony, reflection, existing shared menu/Theme.

**Spec:** This document records the user's approved approach from the preceding turn and this request; original shared-menu spec remains applicable to UI/input/persistence.

## Constraints

- No changes to either legacy DLL on disk or current Minimap source/DLL for this feature. HR-only candidate1.3.0-beta.3; stable releases unchanged.
- Support exact known published2.2.8 and existing2.2.8-dev.3 binaries; reject unknown shapes/digests without patching. No loading unregistered DLLs from Mods.
- No global key suppression, no skipping whole OnUpdate. F4 and map calculations preserved. Patch installation is atomic with rollback and owned unpatching.
- Reuse approved RU/EN four-tab Minimap layout and native input lease. All23existing preferences, live changes and0.5s debounce retained. Surface persistence errors.
- No mandatory Minimap/Phone reference; HR alone and HR+modern Minimap remain usable.

## Review focus

1. Failed partial patch/publication: old Minimap must retain its own controls after rollback.
2. Additional provider belongs to supplying HR runtime, not a legacy assembly incapable of hosting; deterministic election and dedup remain.
3. Only F3 input call is changed; all other instructions and F4 survive. OnGUI suppression never affects HUD/map update.
4. Close/save/scene flush retains pending/error behavior and does not disable map update on IO failure.
5. Modern, absent, unsupported, duplicate candidates and repeated init/shutdown cause no accidental patch or double provider.

## Task1 — Verified binding and patches

**Files:** LegacyMinimapBinding.cs, LegacyMinimapPatches.cs, tests/LegacyMinimapChecks.
**Interfaces:** TryBind(object instance,out binding,out reason); Get<T>/Set<T>, Refresh, Flush, Status; Install/Dispose owned patches.

- [x] Write/run RED tests against unchanged original DLLs: profiles/members/all23prefs, modern/unknown rejection, F3-only rewrite/F4 retained, malformed IL rejected unchanged, OnGUI suppression, rollback/unpatch. Hash originals before/after.
- [x] Implement exact binding and scoped Harmony patches, retaining original save/apply path with checked flush.
- [x] Run GREEN real-DLL metadata/JIT/patch checks and controlled managed input fixture; no native Unity calls in tests.

## Task2 — Register and draw legacy panel

**Files:** LegacyMinimapIntegration.cs, LegacyMinimapPanel.cs, Core.cs, shared EntryPointV1/ProviderDiscovery; shared checks.
**Interfaces:** Initialize(IEnumerable<MelonBase>), Shutdown(); additionalProviders BCL collection, same per-provider dictionary contract.

- [x] RED discovery checks for additional panels/election/isolation; binding changes update original fields and invoke original application/save callbacks via test seams.
- [x] Implement optional registration after all mods initialized, rollback if registration fails, lifecycle cleanup; same approved four-tab controls read/write binding.
- [x] GREEN HR alone, HR+modern, HR+legacy, no mandatory reference, source/body audit and existing regressions.

## Task3 — Review and candidate

- [x] One fresh-context read-only review, fix material issues with focused RED/GREEN.
- [x] Record supported original hashes, HR candidate hash, exact offline/native limits and test guide. Prepare HR-only ZIP.
- [x] Install only when game closed, backup current pair/config outside Mods. For actual legacy test restore unchanged known legacy DLL byte-for-byte, retain current Minimap beta2 backup. Verify all hashes. No publication.
- [x] Follow-up commercial value: user-friendly compatibility is this release's concrete value; the next update should follow actual test feedback/compatibility needs, preserving timely fixes.
