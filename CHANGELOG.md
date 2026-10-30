# Changelog

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
