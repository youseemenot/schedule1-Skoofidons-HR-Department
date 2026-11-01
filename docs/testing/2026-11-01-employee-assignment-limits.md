# Employee assignment limits —1.3.1-beta.1

The user found Botanist Assigned8/8 despite Maximum plants11 and requested checks of all professions. Active preference category is `Skoofidon's HR Department_Botanist`, MaxStations11; older unused category with8is not the active source. The supplied116-line log (`cf50e0f4-0a4b-407f-bf24-dd91990e91e3`, SHA256 `ACB3F33677926031C471D1AAF51D48DADE89B8DEBBC99B2E7D5F925717567CE2`) confirms stable1.3.0, settings saves, no errors and the known Il2CppInterop startup warning.

## Audit

| Profession | Native assignment field |1.3.0 live limit application | Candidate |
| --- | --- | --- | --- |
| Botanist | configuration.Assigns.MaxItems | Missing: only employee.MaxAssignedPots updated | Update both employee and live selector |
| Chemist | configuration.Stations.MaxItems | Already updated | Retain behavior in an explicit adapter |
| Cleaner | configuration.Bins.MaxItems | Already updated; user screenshot shows3/6matching configured6 | Retain behavior in an explicit adapter |
| Handler | configuration.Stations.MaxItems, configuration.Routes.MaxRoutes | Already updated; user screenshot shows1/22and3/22 | Retain behavior |

Native BotanistConfiguration constructor copies employee.MaxAssignedPots(+0x388) into Assigns.MaxItems(+0x20) once. Audited0.4.7f12 instructions:0x5EC2CCload,0x5EC2D2store. Later changing only the employee field cannot update an already-created selection field. ObjectListFieldUI.Clicked reads live MaxItems before opening the native selector; its station/object validation remains native. Disassembly artifacts are in parent workspace `artifacts/botanist-limit-2026-11-01`.

Separate display gap:1.3.0's row cloning was restricted to exact Handler Stations fields. The candidate accepts the exact Stations/Assigns/Bins field belonging to any of the four employee configurations, verified by type and pointer identity. It retains original rows, clones only for actual occupancy, wires indexed deletion, restores assigned root visibility and reuses the existing adaptive scrolling. Unrelated fields and multi-selection remain native. Existing assignments are never removed when a limit decreases; adding more is governed by the new live limit. No assignments/preferences are reset on installation.

This is code/API/offline verification, not a claim that all four employee types have already passed new native gameplay tests. Chemist/Cleaner original assignment constants remain untouched; their existing live ObjectListField limits continue to be used.

## Check in singleplayer

1. Close the game before replacing HR DLL; confirm1.3.1-beta.1in the next log. Keep existing Minimap and cfg. Load the same save with the existing Botanist.
2. F3→Botanist→Maximum plants11(or a suitable higher value), Apply changes. Close settings and reopen the Botanist clipboard: expect Assigned8/11for the reported save. Existing eight objects remain assigned. No re-hiring required.
3. Assign ninth, tenth and eleventh objects. Reopen clipboard, scroll to the last row and remove it once: exactly that object is removed, remaining labels/icons and counts agree. Reopen/repeat to catch duplicate removal listeners.
4. Repeat limit application, assignments beyond the original row count, scroll and one-click removal separately for Chemist Stations and Cleaner Bins. Use suitable available valid objects on the test property. Do not interpret a native ownership/type/destination rejection as a count failure.
5. Handler: verify Assigned1/22and Routes3/22(or actual save counts), independent station/route caps, scrolling and indexed removal remain correct.
6. Lower a limit below current occupancy. All existing assignments remain visible; further additions are blocked while full. Raise it again and verify additions resume. Save/reload and check the last row/removal. Multi-select employees should retain native behavior. F3 Apply and optional Phone App save should both refresh live limits; reopen clipboard after applying.

Co-op is outside this check. Send Latest.log and indicate which profession/step was tested. Screenshots intended for Nexus should wait until this correction is accepted.

## Offline verification

The actual-DLL boundary checks first failed39/42against1.3.0 (missing Botanist/live adapter and all-profession row recognition), then passed42/42with the candidate, with and without Phone App. The existing allocator's8→11case preserves original rows and creates indexed rows8/9/10; reducing occupancy retains rows. These checks validate managed allocation and external API wiring, not native Unity execution.

Candidate SHA256: `7D5EE7605ED74C63E00F1EFC20AF6425CEBACD7EFBDBF998EEF1A3B6F82E7F37`. Build0warnings/0errors. Unity transitive body audit192methods/0unsupported stubs; external API audit passes. Other regression results and independent review are recorded before candidate installation. Stable1.3.0 public assets/tag are not overwritten; this is an unpublished test candidate.

Final offline results: smoke42/42with and42/42without Phone; shared22/22with frozen fixtures; session18/18; draft8/8; confirmation4/4; editor5/5; employee12/12; Cleaner8/8; growth9/9; layout43/43; machine save23/23; machine policy29/29; events5/5; lifetime4/4. Legacy Minimap15/15on each original DLL; modern pair18/18. API404references/13automatic Harmony targets,0failures. Independent review found no Critical/Important/Minor issues; its additional default-scope smoke runs passed26/26with and26/26without Phone. Native assignment/click/scroll/save checks above remain pending. No new settings, saved-item mutation or multiplayer claims.

Installed after closed-game checks; candidate hash verified. Code commit `d5354550b88033a792c83749a03f32309a7c0d1c`. Backup in parent workspace `artifacts/botanist-limit-2026-11-01/backups/20261101-033516`; package `artifacts/botanist-limit-2026-11-01/HR-Department-1.3.1-beta.1-employee-limits.zip`. Original Minimap SHA256 remains `430530DDDA834C580A5C57DD4781BF9540E12E15F5F0C175C4E37A1E6217E450`; preference file hash remained `8B9E724461B6C95E301B1CFC491D527C7D9EF9601A5E56E0FD4202109E18B699` during installation. Local branch retained; no push or release publication. No game save was modified by installation.
