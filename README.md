# Skoofidon's HR Department

A MelonLoader mod for **Schedule I** that configures employee stats and assignment limits through Mod Manager & Phone App.

**Current version: [1.0.5](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases/tag/v1.0.5), tested in singleplayer.** On 2026-10-30 the user confirmed that more than ten Handler stations can be assigned, scrolling and removal work, and the logs contain no errors. This release includes the used/limit/free route counter, expanded station rows and the parent-lookup compatibility fix. See [the test result](docs/testing/2026-10-30-1.0.5-singleplayer-result.md). Individual stress, multi-selection and save/reload scenarios remain unrecorded.

Supported test environment: **Schedule I 0.4.7f12 (IL2CPP)**, Steam build **25796331**, MelonLoader **0.7.3**, and Phone App **2.2.4**. Version 1.0.1 established this compatibility baseline; 1.0.5 builds on it. [Download releases](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases).

## Features
* **Customizable Employee Stats:** Adjust daily wages, signing fees, and absolute base walk speeds for Handlers, Chemists, Botanists, and Cleaners.
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

## Installation
1. Close the game and move every previous HR DLL, including `.dll.disabled` backups, **outside `Mods`**. Do not keep both `SkoofidonsHRDepartment.dll` and `SkoofidonsHRDepartment.dll.disabled` there: Phone App uses those names to enable/disable the mod, and an existing destination blocks the rename.
2. Copy only `SkoofidonsHRDepartment.dll` to `Mods`. Do not distribute or install game/loader reference DLLs from a build directory.
3. Launch the game. After an update, MelonLoader may first regenerate its IL2CPP assemblies.
4. Confirm **HR 1.0.5** and **Schedule I 0.4.7f12** in the new `MelonLoader/Latest.log`. For station limits, open **Handler Settings → Max Stations** in Phone App, save and reopen the clipboard. See [the station-list checklist](docs/testing/2026-10-30-handler-station-rows.md) and [the route-counter checklist](docs/testing/2026-10-30-handler-route-counter.md).

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

The managed checks exercise the actual mod's clipboard handlers, counter presentation, external API boundaries, station row allocation/rollback, visibility and parent-lookup JIT compilation without starting Unity (22 checks for 1.0.5). The JIT check catches invalid generated helper IL that member-existence checks miss. These checks do not validate rendering, native UI callbacks or Unity cloning, NPC movement, wages, or spawning. Use `--baseline` as a final argument when running the new checks against the legacy clipboard implementation; the candidate-only checks are then skipped.

## Known limitations

* Co-op configuration synchronization is not implemented. This release was tested in singleplayer.
* Botanist action times are game constants and remain unmodified.
* Handler uses typed configuration/UI references for both lists, with fixed viewport heights based on the current game prefab. Other professions still use the older English `Assigned` lookup. Adaptive layout and complete localization remain separate work. The additional word `free` currently follows the mod's English UI.
* Extra station rows are added for one selected Handler, not for other professions or multi-selection. Assignment of more than ten stations, scrolling and removal were user-tested. Stress, multi-selection and save/reload scenarios remain separately unrecorded; extreme assignment counts have not been profiled.
* Fixer preserves the 1.0.0 formula: it uses `LifetimeEmployeesRecruited`, applies the lower rate to loop indices 0–5, then the higher rate, and caps the total. The existing `5+` label does not precisely describe that boundary. Economy behavior has deliberately not been redesigned in this compatibility build.
* Valid gameplay ranges for arbitrary settings are not yet enforced. Begin with positive, moderate speeds and assignment limits.

## Planned work

The [research](docs/superpowers/specs/2026-10-30-unfinished-features-research.md) and [staged plan](docs/superpowers/plans/2026-10-30-unfinished-features.md) cover the route counter (included in 1.0.5), property employee capacities, adaptive clipboard layout, and experimental botanist action timing. Property capacities, adaptive layout and action timing remain planned. Co-op remains deferred.
