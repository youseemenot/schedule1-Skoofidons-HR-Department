# Skoofidon's HR Department

A MelonLoader mod for **Schedule I** that configures employee stats and assignment limits through Mod Manager & Phone App.

**Local candidate: 1.2.0-beta.1.** Adds seven machine/growth factors for chemistry, oven, mixing, cauldron, drying, plants and mushrooms. Factors are captured by **new operations only** and preserved in save metadata; running/legacy operations keep their factor (legacy defaults to 1). Higher means less time. See [the candidate checklist](docs/testing/2026-10-30-machine-timers-candidate.md). This candidate is not published and awaits gameplay testing; the public stable release remains 1.1.0.

**Current stable version: [1.1.0](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases/tag/v1.1.0).** Based on beta.4, user-tested in singleplayer on 2026-10-30 with no problems reported. Stable changes only the version metadata and startup message; executable behavior is compared against the tested binary before publication. All four professions have movement/work multipliers, and clipboard layout adapts to the available space. See [release notes](docs/releases/1.1.0.md), [test result](docs/testing/2026-10-30-beta.4-singleplayer-result.md) and [gameplay checklist](docs/testing/2026-10-30-botanist-work-candidate.md).

Previous stable version [1.0.5](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases/tag/v1.0.5) introduced the Handler route counter and more than ten displayed station assignments, with user-tested scrolling/removal. These features remain included in 1.1.0. See [the earlier result](docs/testing/2026-10-30-1.0.5-singleplayer-result.md).

Supported test environment: **Schedule I 0.4.7f12 (IL2CPP)**, Steam build **25796331**, MelonLoader **0.7.3**, and Phone App **2.2.4**. [Download releases](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases).

## Features
* **Customizable Employee Stats:** Adjust daily wages, signing fees, and movement multipliers for Handlers, Chemists, Botanists, and Cleaners.
* **Fixer:** Adjust two incremental hiring-commission rates and a cap. Fixer has no wage or movement settings.
* **Expanded Work Limits:** Override the maximum number of assigned routes, stations, pots, and trash bins.
* **Mod Manager Integration:** Apply settings after saving from the Phone App or its menu.
* **UI Improvements:** Scrollable assignment areas. Handler routes show used/limit/free counts and grow visual rows only when existing route data needs more rows. Lowering a limit preserves existing routes and blocks new additions while full.
* **Handler stations:** `Handler Settings → Max Stations` (`MaxStations`, default 3) controls `Assigned (used/limit)` for packaging stations and brick presses. It is independent of `Max Routes`. Existing employee selection fields receive the limit after preferences are saved; reopen the clipboard to refresh its station heading. For a single Handler, the scrollable list grows beyond ten rows to display actual assignments, keeping native row labels/icons and indexed removal. Lowering the limit preserves and displays existing assignments.

## Requirements
* Schedule I **0.4.7f12, IL2CPP branch**. Mono builds are not supported by this project.
* MelonLoader **0.7.3** and its .NET 6 runtime.
* Mod Manager & Phone App **2.2.4** by Prowiler (required).
* Other game/loader/Phone App combinations need a separate compatibility check.

## Performance settings

Employee settings below belong to stable 1.1.0. The local machine candidate adds a separate **Machine Timers** category: Chemistry / Oven / Mixing / Cauldron / Drying / Plant growth / Mushroom growth, each with `x (higher = less time)` in its title and default1. A new batch captures the current factor, so later preference changes affect subsequent batches only. Mixing covers both station versions; growth does not accelerate watering or employee actions. Read [the module overview](future/MachineTimers/README.md) for save and scope details.

All performance values default to **1**. Movement uses original speed × multiplier; action waits use normal wait ÷ multiplier. Thus **7** means seven times faster movement or one seventh of the action wait, and **0.5** means half speed or twice the wait. Titles state `higher = faster` or `higher = less time`. Limits remain counts, and wages/fees remain currency amounts.

| Setting | Default | Meaning |
| --- | --- | --- |
| Move speed x — all four professions | 1 | Scale original movement speed and its cap |
| Work speed x — Handler | 1 | Packaging and brick presses |
| Work speed x — Chemist | 1 | Manual loading, preparation, stirring and harvesting stages |
| Work speed x — Cleaner | 1 | Pickup, bagging, grabber emptying and bag disposal waits |
| Work speed x — Botanist | 1 | Pot and fungal actions, drying-rack loading and spawn-station work; preserve quantities and native work effects |

Save preferences to apply. Botanist changes affect the next action; Cleaner changes affect future yielded waits. Already scheduled waits retain their duration. Passive machine recipe timers are unchanged. Invalid/nonpositive multipliers retain or restore normal behavior. Repeated saves do not compound movement/work rates; native work buffs remain in effect.

The keys are `WalkSpeedMultiplier` and `WorkSpeedMultiplier` in each profession's category; all beta.3 values retain their keys. The five earlier botanist action values and enable flag are no longer used, and old absolute speeds/seconds are not reinterpreted as factors. Keep a preferences backup for rollback. Botanist shares the native work-stack integration with Handler/Chemist, with no extra duration hook; only Cleaner needs the audited contextual hooks. Keep the research BotanistProbe DLL outside Mods. Returning work speed to 1 removes the mod's work entry for future actions.

## Installation
1. Close the game and move every previous HR DLL, including `.dll.disabled` backups, **outside `Mods`**. Do not keep both `SkoofidonsHRDepartment.dll` and `SkoofidonsHRDepartment.dll.disabled` there: Phone App uses those names to enable/disable the mod, and an existing destination blocks the rename.
2. Copy only `SkoofidonsHRDepartment.dll` to `Mods`. Do not distribute or install game/loader reference DLLs from a build directory.
3. Launch the game. After an update, MelonLoader may first regenerate its IL2CPP assemblies.
4. Confirm **HR 1.1.0** (published stable) or **1.2.0-beta.1** (local candidate), plus **Schedule I 0.4.7f12** in the new `MelonLoader/Latest.log`. For station limits, open **Handler Settings → Max stations** in Phone App, save and reopen the clipboard. See [the station-list checklist](docs/testing/2026-10-30-handler-station-rows.md) and [the route-counter checklist](docs/testing/2026-10-30-handler-route-counter.md).

## Build from source

Use a .NET SDK supporting C# 11 (SDK 10.0.204 was used here), with the .NET 6 reference pack available through NuGet. Keep the target framework at `net6.0` for this loader.

First start the updated game once so MelonLoader generates matching references. From PowerShell:

```powershell
.\scripts\Build.ps1 -GameDirectory 'S:\SteamLibrary\steamapps\common\Schedule I'
```

The script checks `GameAssembly.dll` against the generator's recorded SHA-512 and rejects stale references. For an independently generated set, supply `-InteropDirectory`; that directory must contain `game-reference.json` whose `GameAssemblySha512` records the binary used for generation. Optional `-PackageCache` and `-RestoreSource` support an existing/offline NuGet cache.

Output: `bin/Release/net6.0/SkoofidonsHRDepartment.dll` and `build-info.json`. Dependency paths are configurable; dependency DLLs are not copied into the output. Direct MSBuild use supports `-p:GameDirectory=...` (or `SCHEDULE_I_GAME_DIR`) and `-p:InteropDirectory=...`; use the script when freshness validation is required.

## Automated checks

```powershell
dotnet run --project tests/SmokeChecks -c Release -- `
  'bin/Release/net6.0/SkoofidonsHRDepartment.dll' `
  'S:\SteamLibrary\steamapps\common\Schedule I' `
  'S:\SteamLibrary\steamapps\common\Schedule I\MelonLoader\Il2CppAssemblies'
```

The local candidate has 180 offline checks: actual DLL38 (`--combined --unified`), machine policy/queue29, save state28, growth context8, employee rates12, Cleaner8, layout43 and Botanist native coverage14. The published1.1.0 had109 checks. Native machine targets are manually registered; the actual-DLL suite resolves/JIT-compiles all45 audited targets and callbacks but does not execute Unity, native detours, save I/O or rendering. Use `--baseline` only for the legacy clipboard implementation.

## Known limitations

* The [employee process audit](docs/research/2026-10-30-employee-process-coverage.md) distinguishes Work speed from fixed setup/recovery, navigation and machine clocks. Per user instruction, fixed post-transfer, door, press and botanist supply/unload waits remain native. Dryer unload retains its single 0.5s animation wait; drying itself is a machine/world process. Cleaner retains its existing five action hooks. A factor of 7 does not promise a sevenfold reduction of the whole duty cycle.

* Co-op configuration synchronization is not implemented. This release was tested in singleplayer.
* Employee work factors preserve native quantities and resource operations; game constants/shared GetActionDuration bodies remain untouched. The user reported no problems in the beta.4 test; detailed timing, resource/cancellation measurements remain unrecorded. Machine clocks are unchanged in stable1.1.0; the local1.2 candidate activates the [separate machine module](future/MachineTimers/README.md).
* Machine/growth factors are captured once. Existing operations and saves without metadata default to1; changing settings does not retroactively alter them. Progress remains native; additional versioned save data stores captured factors/fractional credit. Completion remains limited by native minute/frame ticks, and drying retains the native one-quality-step-per-call limit on large skips. Actual capture, ETA, resources and save roundtrip await gameplay testing.
* Clipboard layout uses typed components for all four professions and actual panel geometry. Handler shares space between two lists; very short panels use one outer scroll area. Individual results for resizing, wheel routing, multi-selection and other UI scales/languages remain unrecorded. The word `free` remains English.
* Extra station rows are added for one selected Handler, not for other professions or multi-selection. Assignment of more than ten stations, scrolling and removal were user-tested. Stress, multi-selection and save/reload scenarios remain separately unrecorded; extreme assignment counts have not been profiled.
* Fixer preserves the 1.0.0 formula: it uses `LifetimeEmployeesRecruited`, applies the lower rate to loop indices 0–5, then the higher rate, and caps the total. The existing `5+` label does not precisely describe that boundary. Economy behavior has deliberately not been redesigned in this compatibility build.
* Valid gameplay ranges for arbitrary settings are not yet enforced. Begin with positive, moderate speeds and assignment limits.

## Planned work

The [research](docs/superpowers/specs/2026-10-30-unfinished-features-research.md) and [staged plan](docs/superpowers/plans/2026-10-30-unfinished-features.md) record development history. Employee work rates and adaptive layout are in1.1.0; machine/growth timers are in the local1.2 candidate. Property capacities and co-op remain deferred.

The separate [Botanist Timing Probe](docs/testing/botanist-duration-probe.md) is a historical observation-only research module, outside the main HR DLL and release assets. It is not needed for 1.1.0; the final Botanist implementation uses native employee work speed instead of a duration hook.
