# HR1.3.0-beta.5 — user test result

On2026-11-01 the user reported «вроде все ок» after the footer test build and supplied a log. Record this as no problems reported in the tested functionality, without inferring completion of every manual checklist case.

## Evidence

- Source attachment: `ee08b260-5c49-4399-bcb1-2e23fb767529/Вставленный текст.txt`; SHA256 `90545F7F2E1C43589E905B1F9C534A82FD3340830D89DF8645EC2D1F640136DA`.
-160lines, time range02:46:58.313–02:53:25.949. Schedule I0.4.7f12, MelonLoader0.7.3, Phone App2.2.4.
- HR1.3.0-beta.5 DLL SHA256 `B47E63CF6AC7B1CEBE774D3828D26A90A41D6CAE66C16116723708E8291704F3` matches the installed candidate. Unchanged Minimap2.2.8 SHA256 `430530DDDA834C580A5C57DD4781BF9540E12E15F5F0C175C4E37A1E6217E450` also matches.
- Legacy Minimap adapter connected; shared F3 runtime1.4.0 lists HR Department and Minimap.
-0ERROR/exception/unstripping-failure entries;0deprecated notices;1Il2CppInterop warning: `Class::Init signatures have been exhausted, using a substitute!`. This is the previously observed loader/interop startup warning, not an HR failure entry.
-5HR `Preferences Saved!` entries, including startup, and15`MelonPreferences Saved to` entries on the legacy settings persistence path. Phone App menu saves/resets also occur. These messages establish save calls, not independent verification of all saved values after restart.
- Cleaner PickUpTrash/EmptyTrashGrabber samples show work factor122. This records the exercised setting, not a recommendation or proof that extreme values preserve efficient game AI. Existing moderation guidance is unchanged.

## Scope

The user's result supports the local candidate for the functionality they checked. The log does not directly instrument footer placement, reset scope, cancellation, language changes, narrow-window geometry or cursor ownership; no scenario-by-scenario report was supplied. No new defect is identified, so no code change/rebuild is needed. Keep the tested DLL unchanged. No push, merge or release publication performed; stable remains1.2.0.
