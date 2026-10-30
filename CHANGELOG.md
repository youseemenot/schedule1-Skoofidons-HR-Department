# Changelog

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
