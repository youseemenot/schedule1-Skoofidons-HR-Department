# Changelog

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
