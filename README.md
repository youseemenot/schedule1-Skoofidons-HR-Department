# Skoofidon's HR Department

A MelonLoader mod for **Schedule I** that configures employee stats and assignment limits through Mod Manager & Phone App.

**Working branch: 1.0.3 Handler test build.** The released baseline remains 1.0.1. This candidate adds `Routes (2/5; free 3)`: actual route entries / configured limit, plus remaining slots. Version 1.0.2's user screenshots confirmed changing counts but exposed a Hint overlap and a large gap above stations; 1.0.3 corrects these layouts and synchronizes the existing station-limit setting with the live selection field. See [the focused test checklist](docs/testing/2026-10-30-handler-route-counter.md). Gameplay validation of these fixes is pending.

**1.0.1 is the singleplayer compatibility update for Schedule I 0.4.7f12 (IL2CPP)**, Steam build **25796331**, MelonLoader **0.7.3**, and Phone App **2.2.4**. Compilation passed, and on 2026-10-30 the user reported a successful gameplay test. The supplied log confirms startup, Phone App integration and preference saving without errors; individual checklist scenarios have not all been documented separately. See [the test result](docs/testing/2026-10-30-singleplayer-result.md) and [download releases](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases).

## Features
* **Customizable Employee Stats:** Adjust daily wages, signing fees, and absolute base walk speeds for Handlers, Chemists, Botanists, and Cleaners.
* **Fixer:** Adjust two incremental hiring-commission rates and a cap. Fixer has no wage or movement settings.
* **Expanded Work Limits:** Override the maximum number of assigned routes, stations, pots, and trash bins.
* **Mod Manager Integration:** Apply settings after saving from the Phone App or its menu.
* **UI Improvements:** Scrollable assignment areas. The candidate shows used/limit/free Handler routes and grows visual route rows only when existing route data needs more rows. Lowering a limit preserves existing routes and blocks new additions while full.
* **Handler stations:** `Handler Settings → Max Stations` (`MaxStations`, default 3) controls `Assigned (used/limit)` for packaging stations and brick presses. It is independent of `Max Routes`. Existing employee selection fields receive the limit after preferences are saved; reopen the clipboard to refresh its station heading.

## Requirements
* Schedule I **0.4.7f12, IL2CPP branch**. Mono builds are not supported by this project.
* MelonLoader **0.7.3** and its .NET 6 runtime.
* Mod Manager & Phone App **2.2.4** by Prowiler (required).
* Other game/loader/Phone App combinations need a separate compatibility check.

## Installation
1. Close the game and move every previous HR DLL, including `.dll.disabled` backups, **outside `Mods`**. Do not keep both `SkoofidonsHRDepartment.dll` and `SkoofidonsHRDepartment.dll.disabled` there: Phone App uses those names to enable/disable the mod, and an existing destination blocks the rename.
2. Copy only `SkoofidonsHRDepartment.dll` to `Mods`. Do not distribute or install game/loader reference DLLs from a build directory.
3. Launch the game. After an update, MelonLoader may first regenerate its IL2CPP assemblies.
4. Confirm the expected HR version (**1.0.3** for this candidate, **1.0.1** for the published release) and **Schedule I 0.4.7f12** in the new `MelonLoader/Latest.log`. Follow the candidate checklist above or [the baseline singleplayer checklist](docs/SINGLEPLAYER_TESTING.md).

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

The managed checks exercise the actual mod's clipboard open/scroll/close handlers, route-counter presentation rules, button-state and live station-limit API boundaries without starting Unity (14 checks for 1.0.3). They do not validate rendering, native UI callbacks, row cloning, NPC movement, wages, or spawning. Use `--baseline` as a final argument when running the new checks against the legacy clipboard implementation; the candidate-only checks are then skipped.

## Known limitations

* Co-op configuration synchronization is not implemented or changed by this build. Use this candidate for singleplayer testing.
* Botanist action times are game constants and remain unmodified.
* Handler uses typed configuration/UI references for both lists, with fixed viewport heights based on the current game prefab. Other professions still use the older English `Assigned` lookup. Adaptive layout and complete localization remain separate work. The additional word `free` currently follows the mod's English UI.
* Native station UI has 10 visual entries; this candidate does not expand that array. Test `Max Stations` with moderate limits (for example 5); values above 10 need separate UI work and gameplay validation.
* Fixer preserves the 1.0.0 formula: it uses `LifetimeEmployeesRecruited`, applies the lower rate to loop indices 0–5, then the higher rate, and caps the total. The existing `5+` label does not precisely describe that boundary. Economy behavior has deliberately not been redesigned in this compatibility build.
* Valid gameplay ranges for arbitrary settings are not yet enforced. Begin with positive, moderate speeds and assignment limits.

## Planned work

The [research](docs/superpowers/specs/2026-10-30-unfinished-features-research.md) and [staged plan](docs/superpowers/plans/2026-10-30-unfinished-features.md) cover the route counter (now a 1.0.3 test candidate), property employee capacities, adaptive clipboard layout, and experimental botanist action timing. None of these additions are in the published 1.0.1. Co-op remains deferred.
