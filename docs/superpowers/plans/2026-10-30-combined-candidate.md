# Combined botanist timings and adaptive clipboard candidate

> **For agentic workers:** execute the two independent domains with superpowers:dispatching-parallel-agents, then integrate and review the combined diff.

**Goal:** Prepare one installed candidate containing timing settings and adaptive clipboard layout for the user's combined gameplay test.

**Authority:** On 30 October the user explicitly requested proceeding with the following stages immediately and testing them together. This overrides the previous roadmap's sequential gameplay gate. Runtime behavior remains unverified until that test; settings default to disabled. Stable 1.0.5 is preserved and no GitHub publication is requested.

**Base:** 33a1a0f, branch derived from codex/botanist-timing-probe. Game0.4.7f12 IL2CPP / MelonLoader0.7.3 / PhoneApp2.2.4.

## Task 1: Botanist timing domain

Own new BotanistTimingPolicy.cs, BotanistTimingController.cs and tests/TimingChecks only. Root integrator owns Config.cs/Core.cs/ModManagerIntegration.cs.

Interfaces: TimingAction enum (Unknown,Soil,Water,Additive,Seed,Harvest); BotanistTimingPolicy.Resolve(TimingAction,float originalDuration,float configuredSeconds,bool enabled), Classify(string nativeType). Public static controller Initialize(), RefreshRegistration(), ClearState(), Deinitialize(). Controller consumes Config.BotanistActionTimingEnabled, BotanistSoilTime, BotanistWaterTime, BotanistAdditiveTime, BotanistSeedTime, BotanistHarvestSecondsPerItem (MelonPreferences entries).

- [x] RED→GREEN meaningful pure policy checks: disabled/unknown/invalid preserve original, soil10→5,seed15→7.5,harvest15*0.5→7.5,zero harvest stays0,positive config<0.1clamped (nonpositive preserved), invalid speed/wait does not corrupt game.
- [x] Reuse unique contextual coroutine MoveNext. Never patch shared GetActionDuration or write constants. Pin audited game hash; validate generated fields and target; reject conflicting probe/owners. Manual patch registration only while enabled. On first0→1yield compute base=wait*workSpeed, desiredBase via policy, then set wait=desiredBase/workSpeed ONCE. Filter known action and Botanist. Unknown/fungal actions unchanged, exceptions contained. Add bounded useful logs. Settings affect next action.
- [x] Build and report tests plus wiring requirements. No game install, no git commit, no edits outside owned files.

## Task 2: Adaptive clipboard domain

Own ClipboardLayoutPolicy.cs, ClipboardLayoutController.cs, PackagerUIFix.cs and tests/LayoutChecks only. Root integrator owns Core.cs, other controllers and final docs.

Interfaces: ClipboardLayoutController.Apply(ConfigPanel), RefreshTracked(), Clear(). Confirm native ConfigPanel namespace and Bind signature from local metadata. Preserve public PackagerUIFix APIs called by existing code. Use typed ObjectListFieldUI/RouteListFieldUI descendants for actual panels, including inactive prefabs; no English heading lookups or generic GetComponentInParent<T>(bool) broken helper.

- [x] RED→GREEN pure budget policy tests for one/two lists, tiny panel fallback, no negative geometry and bounded list heights.
- [x] Compute available rect height after static fields and headings. One list uses remaining space; Handler two lists share remainder. Put station rows directly below its heading; route rows below From/To. Keep Hint/edit controls accessible and preserve native count/list behavior.
- [x] If minimum readable viewports do not fit, use one outer scrolling content region, disabling inner scroll axes/raycast interception. No competing nested ScrollRect. Re-evaluate on Bind/rect size or screen size change through tracked controller, no per-frame Resources scan. Idempotent, destroyed views pruned, refs cleared on unload. Preserve expanded station/route row arrays and scroll state during same selection.
- [x] Build and report tests plus integration hook requirements. No game install, no git commit, no edits outside owned files.

## Task 3: Integrate and deliver

- [x] Add disabled-by-default timings with defaults10/10/10/15/1sec peritem, preserve existing keys except ambiguous old HarvestTime is not migrated. Wire manual registration/preference updates and layout size tracking. Candidate1.1.0-beta.1.
- [x] Build main DLL, run existing22checks plus both feature suites and API/JIT checks. Fresh-context review of combined candidate, fix important findings and recheck.
- [x] Update docs and combined gameplay checklist, back up stable1.0.5, move separate probe out of Mods, install only when game closed, verify hashes and local commit. No publication. Record runtime limitations without stopping for individual stage approvals.
