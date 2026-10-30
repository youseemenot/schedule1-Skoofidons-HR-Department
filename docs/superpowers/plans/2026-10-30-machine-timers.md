> Исторический документ beta.1. Политика роста изменена в [beta.2](../../testing/2026-10-30-machine-timers-beta2.md): текущий множитель действует на все растущие культуры. Для машин сохраняется правило новых партий.

# Machine timer implementation plan

> **For agentic workers:** independent pure policy/ledger task may use superpowers:dispatching-parallel-agents; root owns native integration. Review delegated task and the whole candidate before delivery. Execute continuously under the user's requested scope.

**Goal:** Candidate1.2.0-beta.1 with seven captured, persisted new-operation speed factors.

**Spec:** [design](../specs/2026-10-30-machine-timers-design.md). Base295e1ab, reuse current checkout on codex/machine-timers. C#11/net6.0, Schedule I0.4.7f12 IL2CPP, MelonLoader0.7.3/PhoneApp2.2.4.

## File ownership and interfaces

Pure domain owns MachineTimerPolicy.cs, MachineTimerLedger.cs, tests/MachineTimerChecks. Enum MachineTimerKind {Chemistry,Oven,Mixing,Cauldron,Drying,PlantGrowth,ShroomGrowth}. Policy.NormalizeRate(double): positive finite or1. TryCredit(int elapsed,double rate,double fraction,int remaining,out int credit,out double nextFraction): invalid/nonpositive elapsed returns false/unscaled, precise floor+carry, clamp to remaining/reset remainder on completion. TryScaleGrowth(float previous,float requested,double rate,out float scaled): only positive finite delta, clamp final0..1. TryDryingAdjustment(float previous,int elapsed,float warmth,double rate,out float adjusted): permit transient negative adjustment for slowdown, require finite final progress; no past progress scaling. TryEta(double remaining,double fraction,double rate,out int minutes): ceil remaining work/rate, saturate int max.

Ledger public GetOrAdopt(string key,string identity): returns MachineTimerRecord with immutable Identity/CapturedRate and Fraction (readable, internally mutable). Unknown/replaced identities always rate1/fraction0, never live preferences. Capture(string key,string identity,double rate,double fraction=0), TryAdvance(key,identity,elapsed,remaining,out credit), Forget(key), Clear(). TryAdvance returns false at rate1 (native path) or invalid input. New operation explicitly Capture; saved metadata validated by root before Capture. Pure task must not edit other files or commit.

Root owns Config/Core/ModManagerIntegration, MachineTimerController/Targets/Persistence/Growth/UI adapters, actual-DLL tests, machine module documentation and package/install. Target manifests come from bounded native audits. Static save payload and loader scopes remain outside the pure domain.

## Steps

- [x] Pure tests meaningful RED→GREEN: fractional0.5/1.5, large elapsed/clamping, invalid/overflow, rate change cannot affect current record, unknown/replaced identity1, capture/restore, growth slowdown/completion, drying future-only math/ETA.
- [x] Build exact unique target manifest for time/capture/save/load/UI/growth contexts. Audit all111 assemblies; implement guarded registration and factor1 no-op paths.
- [x] Implement new-only capture and persisted state for all seven kinds, including asynchronous plant loading and multiple drying operations. Native resource/completion paths remain authoritative; growth contexts never alter water.
- [x] Add seven clear default1 settings, lifecycle/save refresh, actual-DLL/schema/JIT/state tests; build and run existing employee/Cleaner/layout/native regression suites plus new checks/API.
- [x] Independent review, fix material issues, document runtime test matrix and actual limitations. Retain precise provenance and save compatibility.
- [x] At closed game, back up stable1.1.0/preferences, package/install candidate, verify hashes, commit feature branch locally. No GitHub publication.

Review focus: double scaling Mk2/oven paths; captured factor across settings/reload; idle or completed state adopting new factor; old/future/corrupt payload fallback; native pause/water/resource isolation; growth contexts restored on exception; async plant restore marker; per-operation dryer identity and quality transitions.

## Execution rulings

- Root delegated pure policy/ledger, persistence and growth-context domains with isolated file ownership, then integrated native timers/guards/displays and reviewed the combined candidate. Added a tested MachineDryingCaptureQueue to bridge actual serialized native operation delivery.
- Drying new capture uses the reached PleaseReceiveOp null-connection broadcast, not inlined/bypassed SendOperation logic. Actual receiver may have another pointer; match the queued native batch snapshot and then capture the receiver identity. Targeted synchronization never captures a new preference.
- Proven new plant/colony callbacks discard stale recycled-pointer state before Capture. Read-only growth/UI lookup must not create an unknown rate1 record before saved-rate restoration.
- Current Property save path includes DynamicSaveData.AdditionalDatas; no sidecar files or rewriting native progress is required. A large native time skip still retains the game's drying quality-step cap.

Outcome: Release clean,180offlinechecks,API272references/13autotargets/0failures,45manualmachine targets unique. Independent review findings resolved and rechecked. Installed1.2.0-beta.1 with game closed, stable/prefs backed up. Hash78A0DC960D3D7F0C514D67AB75E9BDAB8695D1C64C77EFE50281161E398B0C69. Runtime verification pending. [Execution](../../testing/2026-10-30-machine-timers-execution.md).
