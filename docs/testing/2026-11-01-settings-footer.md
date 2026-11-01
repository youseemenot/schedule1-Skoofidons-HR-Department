# HR1.3.0-beta.5 — shared footer

Graphical prototype approved in chat on2026-11-01: `artifacts/settings-footer-2026-11-01/design/approved-footer-en.png` in the parent workspace. This build changes action placement/wording, preserving the beta.4 [current-tab reset behavior](2026-11-01-current-tab-defaults.md).

- HR footer: **Close → Reset to defaults → Apply changes**, RU **Закрыть → По умолчанию → Применить изменения**. Current tab only / Только текущая вкладка caption appears over reset. The panel header has no reset action.
- HR reset restores the current draft tab. Apply changes saves and applies the draft; closing without applying discards it. Other tabs are preserved. The reset notice names the updated Apply changes action.
- Legacy Minimap footer: **Close → Reset to defaults**, because changes apply immediately. Reset still asks confirmation for the selected tab. Escape cancels confirmation first, leaving F3 open; background controls are blocked.
- Longer actions have reserved widths; status uses its own row below1100logical pixels. The common host reserves matching content height. Language still follows the game.
- Optional provider `reset` uses a BCL Action, with host/minimum capability1.4.0. Old providers without reset remain compatible; callbacks use the existing provider exception boundary. Configuring another capability afterwards cannot lower the host requirement.
- Only HR DLL is updated. Published original Minimap2.2.8 and configuration values are unchanged. Phone App remains optional. No stable release/publication.

## Check in singleplayer

1. Confirm HR1.3.0-beta.5 in the log. F3→HR: header has no reset; bottom shows Close, Reset to defaults, Apply changes, with no clipped/overlapping labels.
2. Change a value on Cleaner and another HR tab. Reset Cleaner: only its draft changes. Apply changes saves; close/reopen confirms. Separately test Close without applying.
3. Minimap→General: reset is next to Close. Cancel preserves values; confirm restores only General. Escape cancels the confirmation and keeps F3 open. Try sidebar/buttons behind it: blocked. F4/map updates still work.
4. Check RU/EN and a smaller supported resolution: captions/status/buttons remain readable and the lists scroll above the footer. Check free cursor, camera/control blocking and return of control on close.

Offline checks validate provider compatibility, geometry, draft/confirmation/session behavior and old DLL integration. They do not render Unity or replace these native checks. Send Latest.log and a screenshot of any visual issue.

## Offline validation

Build:0warnings/0errors. Shared protocol/layout22/22 with frozen old-provider fixtures (three new checks first failed against beta.4, then passed); session18/18; draft8/8; confirmation4/4; editor5/5. Both original legacy Minimap DLLs15/15 each; modern pair18/18; smoke38/38 with Phone and38/38 without it. Existing employee/cleaner/growth/layout/machine/event/lifetime suites pass. Unity transitive body audit192reachable methods/0unsupported stubs; external API397references/13Harmony targets/0failures. Native rendering/label fit/input acceptance remains pending.

Optional Phone checks7/7 without and8/8 with Phone; Botanist native coverage audit14/14. Independent code review found no Critical/Important/Minor defects and independently rebuilt/passed shared22/22. It does not establish native visual acceptance. Candidate DLL SHA256: `B47E63CF6AC7B1CEBE774D3828D26A90A41D6CAE66C16116723708E8291704F3`.

Player value: clearer reset/save actions beside each other. This local UI refinement is part of the shared-settings release; it is not a standalone published update. Further useful catalog updates should follow actual feedback and compatibility needs.
