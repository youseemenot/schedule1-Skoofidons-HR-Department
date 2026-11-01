# Changelog

## 1.3.0-beta.5 — local candidate, 2026-11-01

- Move current-tab reset from the panel header beside Apply changes in the common footer: Close → Reset to defaults → Apply changes.
- Clarify English Apply changes / Reset to defaults and Russian Применить изменения / По умолчанию labels, including the HR reset notice.
- Reserve space for longer labels; place status above the actions on narrow windows. Minimap retains immediate application and shows Close → Reset to defaults.
- Runtime1.4.0 adds an optional BCL reset callback, retaining older providers and enforcing a nondecreasing minimum host version when capabilities are combined.
- Preserve HR draft, Minimap confirmation, native input ownership, original Minimap DLL and saved configuration. Native visual acceptance pending; no stable publication.

## 1.3.0-beta.4 — local candidate, 2026-11-01

- Restore defaults for only the current HR or legacy Minimap tab, using original preference DefaultValue metadata.
- HR resets remain a draft until Apply. Legacy Minimap confirms first, applies immediately and preserves0.5-second save debounce.
- The shared host blocks background controls during confirmation; Escape cancels it before closing F3.
- Preserve unrelated tabs, original Minimap DLL bytes, existing timing/assignment semantics and compatibility with older V1 providers.
- Native beta.4 acceptance pending. No stable publication.

## 1.3.0-beta.3 — local candidate, 2026-11-01

- Configure supported unchanged Minimap 2.2.8 / 2.2.8-dev.3 through shared F3 by updating HR alone.
- Verify exact original hashes and patch only the old F3 read, old settings window and checked preference flush. Map updates and F4 are retained.
- Reuse all 23 original preferences and the approved four-tab live-slider layout; modern Minimap bypasses the adapter.
- Runtime1.2.0 advertises compatibility panels through optional BCL additionalProviders metadata under the supplying host.
- Original DLL bytes remain unchanged. Unknown binaries are not patched. Native gameplay acceptance pending; no stable publication.

## 1.3.0-beta.2 — local candidate, 2026-11-01

- Fix F3 failing before the window appears: reuse the game font, replace unavailable rounded-texture and desktop text-editor calls.
- Preserve the approved layout with cached backgrounds and managed numeric editing, including selection and clipboard operations.
- Audit reachable Unity wrapper bodies for unstripping-failure stubs in addition to compilation/JIT checks.
- Separate shell/provider failures; contain intentional ExitGUI; discard editing state on close.
- Shared settings has its own log prefix and explains automatic coordination. Each mod can coordinate the menu alone.
- User gameplay retest pending. See [the investigation](docs/testing/2026-11-01-f3-unstripping.md).

## 1.3.0-beta.2 — local candidate, 2026-11-01

- Fix F3 failing before the window appears: reuse the game font, replace unavailable rounded-texture and desktop text-editor calls.
- Preserve the approved layout with cached backgrounds and managed numeric editing, including selection and clipboard operations.
- Audit reachable Unity wrapper bodies for unstripping-failure stubs in addition to compilation/JIT checks.
- Separate shell/provider failures; contain intentional ExitGUI; discard editing state on close.
- Shared settings has its own log prefix and explains automatic coordination. Each mod can coordinate the menu alone.
- User gameplay retest pending. See [the investigation](docs/testing/2026-11-01-f3-unstripping.md).

## 1.3.0-beta.1 — local candidate, 2026-10-31

- Standalone F3 settings for all 31 HR preferences; Phone App is optional.
- Shared mod sidebar with Minimap 2.3.0-beta.1, native cursor/control ownership and deferred close.
- RU/EN follows the Steam per-game language; draft validation and explicit Apply preserve existing configuration identifiers.
- No employee, machine or growth-timer behavior changes. Full paired gameplay check pending.

## 1.3.0-dev.1 — Shared settings groundwork, unpublished

- Add a separate temporary F6 input proof, pure V1 registration/election with independent assembly fixtures, and a source vendor-sync helper. Product F3 integration remains gated on gameplay input verification.
- Remove the mandatory Phone App reference from HR core; preserve settings application through a normal service and optional guarded reflection adapter. Existing preference keys, employee/machine logic and stable installed DLLs are retained.
- Record native research,254offline checks and the deferred native gate. This is an internal build, not the complete paired UI candidate.


## 1.2.0 — 2026-10-30

- Promote user-tested1.2.0-beta.2 to stable. Runtime differences are limited to version metadata and the startup label; executable parity is checked before publication.
- Add seven speed factors: chemistry, oven, mixing/Mk2, cauldron, drying, plant growth and mushroom growth. Higher values shorten affected time;1 is normal,7 is sevenfold progress,0.5 half speed.
- Five machines capture the factor for new batches and retain it across save/load. Current plant/mushroom factors affect all growing cultures without rewriting accumulated progress or tracking per-crop factors.
- Group Phone App settings Botanist, Chemist, Cleaner, Handler, Fixer, Machine Timers while preserving saved values. Retain all employee and clipboard features from1.1.0.
- User confirmed correct ordering, growth changes on existing plants/mushrooms, no progress jumps and no errors. Broader beta.1 checks also reported no problems.176 offline checks and API/native-target verification cover the promoted build; exhaustive gameplay scenarios remain unrecorded.

See [release notes](docs/releases/1.2.0.md) and [beta.2 result](docs/testing/2026-10-30-machine-beta2-result.md). Co-op remains deferred.

## 1.2.0-beta.2 — Live crop growth and settings order, unpublished

- Apply current plant/mushroom factors to future growth of all growing cultures, including existing and loaded crops. Preserve accumulated progress and native conditions.
- Remove per-crop capture/save/load and asynchronous restore markers. Keep new-batch capture and save behavior for the five machines; reduce guarded native targets from45 to36.
- Group settings Botanist, Chemist, Cleaner, Handler, Fixer, Machine Timers without changing preference identifiers or values.
- User subsequently confirmed correct order, live growth factors, no progress jumps and no errors; promoted to stable1.2.0 above.

## 1.2.0-beta.1 — Machine and growth timer candidate, unpublished

- Add seven default1 factors in Machine Timers: chemistry, oven, mixing/Mk2, cauldron, drying, plant growth and mushroom growth. Higher values shorten affected time; 0.5 slows it down.
- Capture the factor only when a new operation/crop starts. Preserve it across preference changes and native DynamicSaveData roundtrips, including fractional minute credit. Existing/legacy operations use1.
- Keep native completion, product quantities, quality, temperature, lighting and moisture conditions. Change growth deltas only; do not accelerate water consumption, employee actions, global time or fixed pauses.
- Handle the oven's separate minute path, Mk2's base call, per-operation drying factors, serialized drying delivery and asynchronous plant restoration. Use captured factors for machine ETA displays.
- Register45 unique, pinned-binary targets manually after API/native-owner validation; remove the machine block's patches on registration failure. Singleplayer runtime testing is pending. Stable GitHub release remains1.1.0.

## 1.1.0 — 2026-10-30

- Promote user-tested beta.4 to stable. Runtime changes from beta.4 are limited to version metadata and the initialization log message; release validation compares behavior against the tested DLL.
- Give Handler, Chemist, Botanist and Cleaner the same movement/work speed settings: 1 = normal; higher movement values mean faster movement, higher work values mean shorter work stages.
- Cover Handler packaging/pressing and item transfer, Chemist station/oven/mixer/cauldron work, Botanist pot/fungal actions/drying-rack loading/spawn station, and five Cleaner action routines. Preserve fixed preparation/recovery pauses, quantities and native work effects.
- Add adaptive clipboard layout for all four professions, retaining the Handler route counter and expanded station list from 1.0.5.
- Use new multiplier preference keys rather than reinterpreting old absolute movement speeds/seconds. Defaults are 1; fees, wages and assignment limits retain their numeric meanings.
- Keep automatic machine timers as a separate inactive scaffold. Co-op and property capacity changes remain deferred.

Validation: beta.4 passed the user's singleplayer check with no problems reported; supplied log confirms its exact hash and all five Cleaner callbacks at factor 22. Stable is rebuilt for its version stamp and receives the same 109 offline checks plus executable-behavior comparison. Detailed individual gameplay measurements remain unrecorded. See [release notes](docs/releases/1.1.0.md).

## 1.1.0-beta.4 — Expanded Botanist native work speed, unpublished

- Apply Botanist Work speed through the native Employee.WorkSpeedController, like Handler/Chemist. Cover pot/fungal GrowContainer actions, drying-rack loading and spawn-station work with one existing preference.
- Remove the five-action duration hook, its registration lifecycle, obsolete policy and policy tests. Avoid applying the factor twice; preserve native work effects, quantities and resources.
- Preserve fixed pauses as requested, including dryer unload's single 0.5s animation wait and both supply-pickup waits. Retain existing Handler/Chemist/Cleaner behavior and machine clocks.
- Add 14 reproducible native coverage checks and update actual-DLL regression checks. Prepare an isolated inactive machine-timer module outline with no active settings or patches. User test result is recorded below; no publication or co-op work.

Validation, 2026-10-30: the user reported no problems during singleplayer testing. Supplied log confirms the exact DLL, initialization/settings saves and all five Cleaner callbacks at factor 22, with no errors. One Il2CppInterop fallback warning and Phone App deprecated-event notifications are recorded separately. See [test result](docs/testing/2026-10-30-beta.4-singleplayer-result.md); individual scenario measurements were not supplied. DLL SHA256 remains E99020D98FFCDBC38C4DF874FC47F4F33ED3C7D7A45D51357B58AC2077898AAA. No publication performed.

## 1.1.0-beta.3 — Two speed settings per profession, unpublished

- Use the same Move speed and Work speed labels for all four professions. Both default to 1; increasing work speed shortens action waits.
- Replace five botanist action multipliers and Custom action rates switch with one WorkSpeedMultiplier for soil, water, additives, planting and harvesting. Non-neutral valid values apply automatically after saving; 1 restores normal behavior for future actions.
- Start the new botanist work key at 1 rather than guessing how to merge different prior action values. Retain existing movement and other professions' work keys and values. Keep native timing guards, resources, yield lifecycle and adaptive clipboard unchanged.
- Candidate prepared for a combined singleplayer test; no GitHub publication or co-op change.

## 1.1.0-beta.2 — Unified performance multipliers, unpublished

- Make every performance multiplier use 1 = normal, 7 = seven times faster, 0.5 = half speed. Labels explain that higher movement values mean faster movement and higher action values mean less time.
- Replace absolute botanist seconds with five multipliers dividing the original contextual wait; preserve harvest quantity and native employee work speed. Retain the disabled-by-default enable flag.
- Add Handler packing/pressing and Chemist manual station work multipliers through the native work-speed stack. Preserve other modifiers and avoid compounding or applying the Handler multiplier twice. Passive machine recipe timers are unchanged.
- Add guarded Cleaner hooks for five contextual action coroutines, replacing each eligible new wait rather than mutating shared yield objects.
- Scale live employee movement from cached original speed and cap; repeated saves are idempotent and 1 restores the original values. Use new preference keys defaulting to 1 instead of reinterpreting saved absolute speeds/seconds.
- Retain beta.1 adaptive clipboard changes. Release build has zero warnings/errors; 133 offline checks pass. Native gameplay, UI labels and timing acceptance await the user's combined singleplayer test. Public stable release remains 1.0.5; co-op is deferred.

## 1.1.0-beta.1 — Combined singleplayer test candidate, unpublished

- Add disabled-by-default Custom Action Times for botanist soil/water/additive/seed durations and harvest seconds per unit. Preserve employee work-speed scaling once; ignore invalid settings, unknown/fungal actions and non-botanist owners. Apply only to newly started actions.
- Register the unique contextual coroutine hook manually while enabled, with pinned game/API/native-body and conflicting-owner checks. Never hook shared GetActionDuration bodies or write game constants. Preserve other features if timing registration fails.
- Replace fixed/English-scanned list layout with typed adaptive panels for Handler, Botanist, Chemist and Cleaner. Share Handler's available list space, reserve static fields/headings, and use one outer scroll region when inner viewports cannot fit.
- Track size/content changes without repeated Resources scans, preserve scroll positions, recover generated wrappers after reference cleanup, hide unused row roots and keep native multi-selection blockers above generated controls.
- Prepared for the user's requested joint test. The read-only probe is a separate earlier experiment and must not remain loaded alongside this timing hook. Public stable version remains 1.0.5; gameplay acceptance is pending.

## 1.0.5 — 2026-10-30

Changes since the previous public release, 1.0.1:

- Show used/limit/free Handler routes, with updates on binding, route changes and preference saves.
- Allow the existing Max Stations setting to exceed ten visible Handler assignments; grow rows as needed, with scrolling and indexed removal.
- Update live station limits for existing employees and fix Handler list positions, heading/Hint overlap and scroll bounds. Preserve existing assignments after lowering a limit.
- Include the development changes from 1.0.2–1.0.4 below, plus the parent-lookup fix:

- Replace generated `Component.GetComponentInParent<T>(bool)` calls with the native overload taking an explicit IL2CPP type and a checked cast. Preserve lookup through inactive parents. The generic wrapper throws VerificationException in the installed interop build, interrupting scene UI initialization and Handler station layout updates.
- Add a compile-only JIT regression check for called parent-lookup wrappers. Reproduces the supplied log's exception on 1.0.4 without running Unity; 1.0.5 passes all 22 checks. Rendering and assignment behavior are unchanged by this fix.

Validation, 2026-10-30: the user tested 1.0.5 in singleplayer and confirmed assignment of more than ten stations, working scrolling/removal and no log errors. Release assets retain the exact tested DLL, SHA256 `6D293EE64D743B688F4CD0DB3C9BED52CD6A4A9F7FFA2D84C10B010949CC1101`. Co-op remains outside the tested scope.

## 1.0.4 — Expanded Handler station-list test candidate, unreleased

- Extend native Handler station rows beyond ten according to actual assignments, using the existing Max Stations preference. Keep native text/icon population and indexed removal.
- Clear inherited delete events on clones; account for native Bind wiring listeners after Refresh, avoiding duplicate clone callbacks on rebinding.
- Explicitly activate assigned row roots after Refresh/Bind and hide unused roots. Preserve visible over-limit assignments after lowering the limit; native Refresh alone only toggles row children.
- Rebuild station content height, clamp scroll after removal, and reset to the top when binding a selection. Fit longer Assigned counts beside the Hint and edit controls.
- Roll back partial row creation on failure, preserve original rows/data, clear tracked UI on unload. Add managed allocation, rollback, visibility and adapter-boundary checks. Gameplay acceptance remains pending.

## 1.0.3 — Handler layout and station-limit test candidate, unreleased

- Move the route Hint to the right edge and reserve label space; keep the tooltip available while fitting the counter on one line.
- Normalize Handler list contents to their viewport top instead of retaining the prefab's old station offset. Leave room above routes for From/To headings. Initialize typed Handler panels including inactive prefabs and after binding.
- Apply the existing Handler Settings / Max Stations preference to both the employee and live Stations.MaxItems field. Existing stations are not deleted when lowering the limit.
- Add a managed external API regression check for live station limits. Visual and station assignment checks require singleplayer validation; station UI expansion beyond its native 10 entries is outside this candidate.

## 1.0.2 — Handler route-counter test candidate, unreleased

- Show actual used/limit/free Handler routes, including incomplete route entries, from the bound RouteListField.
- Refresh the heading after UI Start/Bind/Refresh and HR preference saves; stop overwriting it with the old `Max` title.
- Keep native multi-selection behavior and block additional routes at/over the current limit. Existing assignments survive a limit decrease.
- Grow native route UI entries only when existing data exceeds the current visual array. Preserve native deletion handling, accounting for entries created before versus after UI Start.
- Clear tracked UI state on scene unload and discard destroyed views. Keep property capacity, adaptive layout and botanist timers outside this candidate.
- Add eight managed presentation checks and a button-state API boundary check to the four existing clipboard checks. Native UI event wiring, row expansion/deletion and rendering still require the focused gameplay checklist.

## 1.0.1 — 2026-10-30

- Target installed Schedule I 0.4.7f12 IL2CPP, MelonLoader 0.7.3, and Phone App 2.2.4.
- Replace removed `NPCMovement.WalkSpeed` writes with `NPCSpeedController.SetDefaultSpeed`, preserving absolute speed values and existing preference keys.
- Apply employee settings after `Employee.Start`, including new hires after loading a save; retain the load-complete pass.
- Remove the prefix that vetoed clipboard closing immediately after opening. Reset scroll state when leaving the main scene.
- Refresh cached Handler route-limit labels after preference saves and clear those references on scene unload.
- Add configurable dependency paths, missing-reference diagnostics, and a build script that rejects stale IL2CPP references by game-binary hash. Keep game/loader DLLs out of build output.
- Correct compatibility and feature descriptions, identify botanist timings as constants, and add a singleplayer test checklist.
- Correct installation guidance: store old DLLs outside `Mods`, avoiding Phone App toggle failures when `.dll` and `.dll.disabled` names are both occupied.

Validation: Release build against references freshly generated from the installed 0.4.7f12 binary, zero errors/warnings; four managed clipboard checks pass. On 2026-10-30 the user reported a successful gameplay test; the supplied log confirms initialization and preference saving without errors. Detailed scenario results remain to be recorded. Phone App's DEPRECATED OnPreferencesSaved message is emitted by its compatibility bridge; HR subscribes to the current events. Co-op and the Fixer economy formula are unchanged.

## 1.0.0 — 2026-05-07

Initial published version: 21 preferences, employee stats and assignment limits, Phone App integration, and scrollable employee lists.
