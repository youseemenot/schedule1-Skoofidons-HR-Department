# Skoofidon's HR Department

A MelonLoader mod for **Schedule I** that configures employee stats and assignment limits through Mod Manager & Phone App.

**Local test candidate: 1.1.0-beta.2.** Adds work rates for Handler, Chemist and Cleaner, five botanist action rates, movement multipliers and adaptive clipboard layout. Every performance multiplier uses 1 = normal and 7 = seven times faster. Prepared for the user's combined gameplay test; runtime acceptance is pending. The public release remains 1.0.5. See [the current checklist](docs/testing/2026-10-30-unified-speed-candidate.md).

**Current version: [1.0.5](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases/tag/v1.0.5), tested in singleplayer.** On 2026-10-30 the user confirmed that more than ten Handler stations can be assigned, scrolling and removal work, and the logs contain no errors. This release includes the used/limit/free route counter, expanded station rows and the parent-lookup compatibility fix. See [the test result](docs/testing/2026-10-30-1.0.5-singleplayer-result.md). Individual stress, multi-selection and save/reload scenarios remain unrecorded.

Supported test environment: **Schedule I 0.4.7f12 (IL2CPP)**, Steam build **25796331**, MelonLoader **0.7.3**, and Phone App **2.2.4**. Version 1.0.1 established this compatibility baseline; 1.0.5 builds on it. [Download releases](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases).

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

## Candidate performance settings

All performance values default to **1**. Movement uses original speed × multiplier; action waits use normal wait ÷ multiplier. Thus **7** means seven times faster movement or one seventh of the action wait, and **0.5** means half speed or twice the wait. Titles state `higher = faster` or `higher = less time`. Limits remain counts, and wages/fees remain currency amounts.

| Setting | Default | Meaning |
| --- | --- | --- |
| Move speed x — all four professions | 1 | Scale original movement speed and its cap |
| Packing and pressing x — Handler | 1 | Native work-speed multiplier for packaging and brick presses |
| Station work x — Chemist | 1 | Manual loading, preparation, stirring and harvesting stages |
| Cleaning actions x — Cleaner | 1 | Pickup, bagging, grabber emptying and bag disposal waits |
| Custom action rates — Botanist | Off | Enable the five action multipliers below |
| Soil filling / Watering / Additives / Planting / Harvesting x | 1 each | Shorten the original contextual wait; preserve harvest quantity and native work-speed effects |

Save preferences to apply. Botanist changes affect the next action; Cleaner changes affect future yielded waits. Already scheduled waits retain their duration. Passive machine recipe timers are unchanged. Invalid/nonpositive multipliers retain or restore normal behavior. Repeated saves do not compound movement/work rates; native work buffs remain in effect.

New `WalkSpeedMultiplier`, `WorkSpeedMultiplier` and five botanist `*SpeedMultiplier` keys prevent old absolute speeds or seconds from being reinterpreted as factors. Set desired values again after upgrading; the botanist enable flag is retained. Keep a preferences backup for rollback. Contextual timing hooks are restricted to the audited game binary and reject conflicts. Keep the separate SkoofidonsHRBotanistProbe DLL outside Mods.

## Installation
1. Close the game and move every previous HR DLL, including `.dll.disabled` backups, **outside `Mods`**. Do not keep both `SkoofidonsHRDepartment.dll` and `SkoofidonsHRDepartment.dll.disabled` there: Phone App uses those names to enable/disable the mod, and an existing destination blocks the rename.
2. Copy only `SkoofidonsHRDepartment.dll` to `Mods`. Do not distribute or install game/loader reference DLLs from a build directory.
3. Launch the game. After an update, MelonLoader may first regenerate its IL2CPP assemblies.
4. Confirm the installed HR version (**1.0.5** stable or **1.1.0-beta.2** local candidate) and **Schedule I 0.4.7f12** in the new `MelonLoader/Latest.log`. For station limits, open **Handler Settings → Max stations** in Phone App, save and reopen the clipboard. See [the station-list checklist](docs/testing/2026-10-30-handler-station-rows.md) and [the route-counter checklist](docs/testing/2026-10-30-handler-route-counter.md).

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

The stable managed suite has 22 checks. Add `--combined --unified` for 31 checks on this candidate's actual DLL, including timing/lifecycle/layout boundaries. Pure suites cover botanist timing (39), employee rates (12), Cleaner waits (8) and layout budgets (43): 133 checks in total. These do not execute Unity or native employee callbacks, rendering, cloning or resource handling. The JIT check catches invalid generated helper IL that member-existence checks miss. Use `--baseline` for the legacy clipboard implementation.

## Known limitations

* Co-op configuration synchronization is not implemented. This release was tested in singleplayer.
* Candidate timing overrides change each eligible action's contextual wait; game constants and shared GetActionDuration bodies remain untouched. Fungal/unknown actions and other NPC types retain their durations. Actual gameplay timing/resource/cancellation behavior awaits the joint test.
* Candidate clipboard layout uses typed components for all four professions and actual panel geometry. Handler shares space between two lists; very short panels use one outer scroll area. Resizing, wheel routing, multi-selection and other UI scales/languages still need gameplay validation. The word `free` remains English.
* Extra station rows are added for one selected Handler, not for other professions or multi-selection. Assignment of more than ten stations, scrolling and removal were user-tested. Stress, multi-selection and save/reload scenarios remain separately unrecorded; extreme assignment counts have not been profiled.
* Fixer preserves the 1.0.0 formula: it uses `LifetimeEmployeesRecruited`, applies the lower rate to loop indices 0–5, then the higher rate, and caps the total. The existing `5+` label does not precisely describe that boundary. Economy behavior has deliberately not been redesigned in this compatibility build.
* Valid gameplay ranges for arbitrary settings are not yet enforced. Begin with positive, moderate speeds and assignment limits.

## Planned work

The [research](docs/superpowers/specs/2026-10-30-unfinished-features-research.md) and [staged plan](docs/superpowers/plans/2026-10-30-unfinished-features.md) cover the route counter (released in 1.0.5), experimental action timing and adaptive layout (this candidate), plus property employee capacities (backlog). Co-op remains deferred.

The separate [Botanist Timing Probe](docs/testing/botanist-duration-probe.md) is an observation-only research module, outside the main HR DLL and public 1.0.5 assets. It observes a contextual action coroutine; direct duration hooks were rejected after finding shared native bodies with graphics methods. The user authorized combined candidate testing; runtime acceptance remains pending.
