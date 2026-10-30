# Unified speed multipliers and other professions

> **For agentic workers:** dispatch independent bot-policy, employee-speed and Cleaner domains; root integrates Config/Core/docs and reviews combined output.

**Goal:** Candidate1.1.0-beta.2 adds worker rates for other professions and gives every speed control one direction:1=game normal,7=seven times faster,0.5=half speed. User explicitly requests clear effects in each setting's title. Existing costs/limits keep numeric semantics and gain direction labels.

**Base:**7ef9aa4, current checkout. Keep stable1.0.5 and beta.1 backups, no publication. New speed keys avoid interpreting old seconds/raw movement speeds as multipliers. Back up preferences outside Mods for rollback; persistence of unregistered legacy keys is not assumed. New controls default1. The user's confirmed multiplier rule supersedes the earlier absolute-seconds design, including harvest seconds per item.

## Architecture

- Botanist: same guarded contextual coroutine; all five durations become originalWait/multiplier, preserving product quantity and existing work speed. No shared duration hooks, constants or resource changes. New Config fields BotanistSoilSpeed/WaterSpeed/AdditiveSpeed/SeedSpeed/HarvestSpeed, default1; existing BotanistActionTimingEnabled flag preserved.
- Handler/Chemist: native Employee.WorkSpeedController is FloatStack with Multiplicative entries. Use one stable mod-owned label and final order; replacing/removing that entry cannot compound on repeated saves and preserves other entries. Handler Config.PackagerSpeed becomes WorkSpeedMultiplier instead of the old separate PackagingSpeedMultiplier field write; Chemist uses Config.ChemistWorkSpeed. Native coroutine evidence shows both Handler packing/press and four Chemist routines consume CurrentWorkSpeed. No machine recipe duration is changed.
- Movement: Config four WalkSpeed fields become new WalkSpeedMultiplier keys default1. Cache native DefaultSpeed/max cap per live controller, apply baseline*factor without save compounding; scale cap so7x is not silently capped at native7 units. Do not modify employee prefab movement. Restore native values/own work entries on unload/deinit before clearing references.
- Cleaner: five contextual action coroutines do not use CurrentWorkSpeed. Guarded manual MoveNext hooks replace each newly yielded WaitForSeconds with a new instruction of originalSeconds/Config.CleanerWorkSpeed. Never mutate a shared yield object, skip null/frame/other yields, only native Cleaner owner. Defaults1 means no hook. Settings affect future waits; already scheduled waits stay intact.

## Independent domains and interfaces

1. Bot policy owner: BotanistTimingPolicy.cs / BotanistTimingController.cs / tests/TimingChecks. Preserve public method signatures, update multiplier semantics/tests, consume renamed Config fields. TryResolveWait validates speed but does not divide it twice.
2. Employee speed owner: new EmployeeSpeedPolicy.cs / EmployeeSpeedController.cs / tests/EmployeeSpeedChecks. Controller.Apply(Employee,bool isPrefab), RestoreAndClear(). Consumes Config.PackagerSpeed,ChemistWorkSpeed and existing four *WalkSpeed entries. All invalid multipliers preserve/restore normal. Own only these files.
3. Cleaner owner: new CleanerActionSpeedController.cs / CleanerSpeedPolicy.cs / tests/CleanerSpeedChecks. Initialize(),RefreshRegistration(),ClearState(),Deinitialize(). Consumes Config.CleanerWorkSpeed. Pinned game hash, unique target/body audit, owner/conflict guards, no automatic PatchAll. Own only these files. Runtime still untested.
4. Root: Config/EmployeeController/Core/ModManagerIntegration, compiled integration tests, docs, audits, package/install/review. Remove old absolute walk setter and PackagingSpeedMultiplier write to avoid stacking same setting twice.

## Verification and delivery

- [x] Each domain meaningful RED→GREEN multiplier/idempotence/invalid-input tests; verify Cleaner native wait yields and all111assembly target aliases.
- [x] Build mainDLL, old smoke regression plus actualDLL multiplier/config/lifecycle tests, APIs and wrapper JIT. Fresh integrated review; fix important issues.
- [x] Performance titles explicitly state higher=faster/less time; assignment limits state higher=more. Costs remain currency. New keys and default1 documented; no auto reinterpretation of saved seconds. One combined test checklist and candidate1.1.0-beta.2.
- [x] At closed game, back up beta.1 and stable1.0.5, install newDLL, verify hashes, local commit only. User jointlytests runtime; no claims of native success from offline checks.

Delivered: 133 offline checks, zero build warnings/errors, DLL SHA256 DC2377229FAC4CAA796ACBB71CF70A178BCE36397C79830C078DC7555E555601. Installation verified with the game closed on 2026-10-30; beta.1 and preferences backed up outside Mods. See [execution record](../../testing/2026-10-30-unified-speed-execution.md). Gameplay acceptance remains pending; no publication.
