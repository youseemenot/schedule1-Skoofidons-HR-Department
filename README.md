# Skoofidon's HR Department

> Local test candidate **1.3.1-beta.1** corrects a confirmed Botanist limit defect in1.3.0: changing Maximum plants updated the employee but left an existing assignment selector at8. It now updates both. The existing row expansion/scrolling mechanism also supports single selected Botanist, Chemist and Cleaner lists beyond their native row count, alongside Handler. Offline checks pass; native acceptance is pending. See the [all-profession assignment audit](docs/testing/2026-11-01-employee-assignment-limits.md). Public stable remains1.3.0; Nexus preparation is held for verification.

A MelonLoader mod for **Schedule I** that configures employee stats, assignment limits, machine timers and crop growth through its own **F3 settings menu**. Mod Manager & Phone App integration is optional.

**Current stable version: [1.3.0](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases/tag/v1.3.0).** Adds standalone RU/EN settings, shared F3 with supported unchanged Minimap DLLs, and current-tab defaults beside Apply changes. Opening settings frees the cursor and blocks gameplay controls; closing restores control through the game's UI system. Based on user-tested **1.3.0-beta.5**; stable changes only its version label and startup message. See [release notes](docs/releases/1.3.0.md), [user test result](docs/testing/2026-11-01-beta5-user-result.md) and [footer checklist](docs/testing/2026-11-01-settings-footer.md).

Version [1.2.0](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases/tag/v1.2.0) introduced machine timers and live growth factors; these remain included in1.3.0.

Earlier stable version [1.0.5](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases/tag/v1.0.5) introduced the Handler route counter and more than ten displayed station assignments, with user-tested scrolling/removal. These features remain included in 1.2.0. See [the earlier result](docs/testing/2026-10-30-1.0.5-singleplayer-result.md).

Supported test environment: **Schedule I 0.4.7f12 (IL2CPP)**, Steam build **25796331**, MelonLoader **0.7.3**, and Phone App **2.2.4**. [Download releases](https://github.com/youseemenot/schedule1-Skoofidons-HR-Department/releases).

## Features

* **Customizable Employee Stats:** Adjust daily wages, signing fees, movement and work multipliers for Handlers, Chemists, Botanists, and Cleaners.
* **Machine and growth speed:** Separate factors for chemistry, oven, mixing/Mk2, cauldron, drying, plants and mushrooms. Five machines capture their factor per new batch; all growing plants/mushrooms use current growth settings.
* **Settings order:** Botanist → Chemist → Cleaner → Handler → Fixer → Machine Timers, preserving identifiers and stored values.
* **Fixer:** Adjust two incremental hiring-commission rates and a cap. Fixer has no wage or movement settings.
* **Expanded Work Limits:** Override the maximum number of assigned routes, stations, pots, and trash bins.
* **Standalone F3 settings:** HR settings work without Phone App, Minimap or a separate shared-menu DLL. Russian/English follows the game's selected language. Draft edits are saved with **Apply changes**; closing discards unapplied edits.
* **Current-tab defaults:** **Reset to defaults** restores the selected HR tab's draft, preserving other tabs. Click **Apply changes** to save.
* **Shared settings with Minimap:** Supported original Minimap2.2.8/2.2.8-dev.3 DLLs can use the same F3 menu through the HR adapter, without a Minimap update. Minimap keeps live sliders, save debounce and F4 diagnostics; its reset asks confirmation. Unknown Minimap binaries are not patched.
* **Optional Mod Manager integration:** Save from Phone App or its main menu to apply settings when it is installed.
* **UI Improvements:** Scrollable assignment areas. Handler routes show used/limit/free counts and grow visual rows only when existing route data needs more rows. Lowering a limit preserves existing routes and blocks new additions while full.
* **Handler stations:** `Handler Settings → Max Stations` (`MaxStations`, default 3) controls `Assigned (used/limit)` for packaging stations and brick presses. It is independent of `Max Routes`. Existing employee selection fields receive the limit after preferences are saved; reopen the clipboard to refresh its station heading. For a single Handler, the scrollable list grows beyond ten rows to display actual assignments, keeping native row labels/icons and indexed removal. Lowering the limit preserves and displays existing assignments.

## Requirements

* Schedule I **0.4.7f12, IL2CPP branch**. Mono builds are not supported by this project.
* MelonLoader **0.7.3** and its .NET 6 runtime.
* Mod Manager & Phone App **2.2.4** by Prowiler is optional; F3 works without it.
* Minimap is optional. The legacy adapter supports exact original2.2.8/2.2.8-dev.3 builds; [compatibility details](docs/testing/2026-11-01-legacy-minimap.md). Compatible modern shared-menu providers are discovered automatically; unknown old Minimap builds may retain their own F3 behavior.
* Other game/loader combinations need a separate compatibility check. Co-op configuration synchronization is not included.

## Settings

In a loaded singleplayer game, press **F3** to open settings. Choose HR Department, then Botanist, Chemist, Cleaner, Handler, Hiring or Production. Use **Apply changes** to save and apply HR edits. **Reset to defaults** affects only the open tab and stays in the draft until applied. Close or Escape exits; unapplied HR edits are discarded.

When supported Minimap is installed, select it in the same sidebar. Its sliders and toggles apply immediately. Reset asks confirmation for the current tab; Escape cancels that confirmation first. F4 remains Minimap's diagnostic key. Language follows the game's selected Russian/English interface. No Phone App or Minimap dependency is needed to configure HR itself.

## Performance settings

Machine Timers has seven independent factors, default1: Chemistry / Oven / Mixing / Cauldron / Drying / Plant growth / Mushroom growth. Titles show `x (higher = less time)`. Mixing covers both mixer versions. Growth affects future native progress, preserving accumulated progress, environmental conditions and water timing; employee work remains separate. Read [the module overview](future/MachineTimers/README.md) for save and scope details.

| Process | When a changed factor applies |
| --- | --- |
| Chemistry, Oven, Mixing/Mk2, Cauldron, Drying | New batches only; captured factor survives save/load. Existing batches keep their factor, with legacy/unrecognized batches defaulting to1. |
| Plant and mushroom growth | Next native growth calculation for all growing cultures, including already planted and loaded crops. Past progress is unchanged; no per-crop factor is saved. |

All performance values default to **1**. Movement uses original speed × multiplier; action waits use normal wait ÷ multiplier. Thus **7** means seven times faster movement or one seventh of the action wait, and **0.5** means half speed or twice the wait. Titles state `higher = faster` or `higher = less time`. Limits remain counts, and wages/fees remain currency amounts.

**Use speed multipliers in moderation.** Start at1× and increase gradually. Around **7×** has already provided substantial acceleration in the author's single-player testing; this is an empirical reference and results vary by task and save. Very high values can produce erratic movement, inefficient work or other unexpected results. A larger number does not always improve actual output. Reduce the relevant multiplier if behavior becomes unreliable.

| Setting | Default | Meaning |
| --- | --- | --- |
| Move speed x — all four professions | 1 | Scale original movement speed and its cap |
| Work speed x — Handler | 1 | Packaging and brick presses |
| Work speed x — Chemist | 1 | Manual loading, preparation, stirring and harvesting stages |
| Work speed x — Cleaner | 1 | Pickup, bagging, grabber emptying and bag disposal waits |
| Work speed x — Botanist | 1 | Pot and fungal actions, drying-rack loading and spawn-station work; preserve quantities and native work effects |

Use F3 **Apply changes** (or save in the optional Phone App) to apply HR preferences. Botanist changes affect the next action; Cleaner changes affect future yielded waits. Already scheduled waits retain their duration. Machine timers use the separate settings described above. Invalid/nonpositive multipliers retain or restore normal behavior. Repeated saves do not compound movement/work rates; native work buffs remain in effect.

The keys are `WalkSpeedMultiplier` and `WorkSpeedMultiplier` in each profession's category; all beta.3 values retain their keys. The five earlier botanist action values and enable flag are no longer used, and old absolute speeds/seconds are not reinterpreted as factors. Keep a preferences backup for rollback. Botanist shares the native work-stack integration with Handler/Chemist, with no extra duration hook; only Cleaner needs the audited contextual hooks. Keep the research BotanistProbe DLL outside Mods. Returning work speed to 1 removes the mod's work entry for future actions.

## Installation

1. Close the game and move every previous HR DLL, including `.dll.disabled` backups, **outside `Mods`**. Do not keep both `SkoofidonsHRDepartment.dll` and `SkoofidonsHRDepartment.dll.disabled` there: Phone App uses those names to enable/disable the mod, and an existing destination blocks the rename.
2. Copy only `SkoofidonsHRDepartment.dll` to `Mods`. Do not distribute or install game/loader reference DLLs from a build directory.
3. Launch the game. After an update, MelonLoader may first regenerate its IL2CPP assemblies.
4. Confirm **HR 1.3.0**, plus **Schedule I 0.4.7f12** in the new `MelonLoader/Latest.log`. In a loaded game press **F3**. For station limits, choose **HR Department → Handler → Maximum stations**, click **Apply changes** and reopen the clipboard. Existing saved values are retained. See [the station-list checklist](docs/testing/2026-10-30-handler-station-rows.md) and [the route-counter checklist](docs/testing/2026-10-30-handler-route-counter.md).

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

Release validation covers the actual DLL with and without Phone App, shared providers and frozen older DLLs, menu lifecycle, draft/reset/confirmation, numeric editing, legacy Minimap integration and the previous employee/machine/save/layout suites. The Unity body audit rejects unsupported unstripping stubs; external API checks cover397references and13automatic Harmony targets. Stable metadata/IL is compared with the exact user-tested beta.5, allowing only the version label and initialization message. These checks do not execute Unity or establish exhaustive native input/rendering/gameplay behavior; [user evidence](docs/testing/2026-11-01-beta5-user-result.md) and [release validation](docs/testing/2026-11-01-1.3.0-release.md) are recorded separately.

## Known limitations

* The [employee process audit](docs/research/2026-10-30-employee-process-coverage.md) distinguishes Work speed from fixed setup/recovery, navigation and machine clocks. Per user instruction, fixed post-transfer, door, press and botanist supply/unload waits remain native. Dryer unload retains its single 0.5s animation wait; drying itself is a machine/world process. Cleaner retains its existing five action hooks. A factor of 7 does not promise a sevenfold reduction of the whole duty cycle.

* Co-op configuration synchronization is not implemented. This release was tested in singleplayer.
* Employee work factors preserve native quantities and resource operations; game constants/shared GetActionDuration bodies remain untouched. Detailed timing/resource/cancellation matrices remain unrecorded. The [machine module](future/MachineTimers/README.md) remains separate from employee work.
* Five machine factors are captured per new batch, with versioned save metadata for factors/fractional credit. Plant/mushroom growth reads the current preference every native growth callback, including for old saves; no per-crop capture/save/load layer is used. Completion remains limited by native ticks; drying retains its one-quality-step-per-call limit on large skips. User testing confirmed live crop changes and no progress jumps; exhaustive resource/save scenarios remain unrecorded.
* Clipboard layout uses typed components for all four professions and actual panel geometry. Handler shares space between two lists; very short panels use one outer scroll area. Individual results for resizing, wheel routing, multi-selection and other UI scales/languages remain unrecorded. The word `free` remains English.
* Extra station rows are added for one selected Handler, not for other professions or multi-selection. Assignment of more than ten stations, scrolling and removal were user-tested. Stress, multi-selection and save/reload scenarios remain separately unrecorded; extreme assignment counts have not been profiled.
* Fixer preserves the 1.0.0 formula: it uses `LifetimeEmployeesRecruited`, applies the lower rate to loop indices 0–5, then the higher rate, and caps the total. The existing `5+` label does not precisely describe that boundary. Economy behavior has deliberately not been redesigned in this compatibility build.
* Valid gameplay ranges for arbitrary settings are not yet enforced. Begin with positive, moderate speeds and assignment limits.
* The shared F3 flow was user-tested with original Minimap2.2.8 and Phone App2.2.4. Offline checks also cover operation without Phone App and supported2.2.8-dev.3. Every resolution, language/input transition and every mod combination has not been separately confirmed in game. Old unsupported Minimap DLLs may still respond to F3 independently.

## Planned work

The [research](docs/superpowers/specs/2026-10-30-unfinished-features-research.md) and [staged plan](docs/superpowers/plans/2026-10-30-unfinished-features.md) record development history. Employee work rates/adaptive layout are in1.1.0; machine/growth timers in1.2.0; standalone/shared F3 and current-tab defaults in1.3.0. See the [next release priorities](docs/releases/ROADMAP.md). Property capacities and co-op remain deferred.

The separate [Botanist Timing Probe](docs/testing/botanist-duration-probe.md) is a historical observation-only research module, outside the main HR DLL and release assets. It is not needed for 1.1.0; the final Botanist implementation uses native employee work speed instead of a duration hook.

Development decisions follow the [architecture-first project guidance](docs/DEVELOPMENT.md).
