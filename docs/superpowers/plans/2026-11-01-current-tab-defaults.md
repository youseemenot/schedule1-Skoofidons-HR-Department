# Current-tab defaults reset implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: use superpowers:executing-plans inline, with one final independent review.

**Goal:** Implement the approved reset prototypes for HR and the legacy Minimap panel, resetting only the current tab.

**Architecture / approved spec:** The user approved current-tab scope after the two in-chat images in artifacts/settings-reset-2026-11-01/design. HR resets draft text only; Apply persists through the existing service. Legacy Minimap requests confirmation; confirming resets only the tab's original preferences to each entry's DefaultValue and invokes original live application, retaining0.5s pending save. The shared host draws a modal over the entire window and disables underlying controls; Escape cancels that modal first, F3/close/focus loss discards pending confirmation. Optional BCL modal callbacks keep provider boundaries independent of assembly identity.

**Tech:** C#11/net6, existing MelonPreferences/default metadata, shared Theme/input lease, pure managed checks.

**Constraints:** Approved upper-right defaults button/caption, RU/EN, responsive header; no whole-mod reset, per-field reset or limits/default changes. Only HR candidate1.3.0-beta.4 installed; unchanged legacy Minimap2.2.8 remains. Native modern Minimap panes stay on their existing version; their own reset support can ship with that mod's later update. Shared runtime1.3.0 adds optional hasModal/drawModal/cancelModal callbacks. No source/DLL update to Minimap in this task. Stable versions/publication unchanged.

## Review focus

1. Reset leaves all other tabs' data, including incomplete HR draft edits, unchanged.
2. Cancel/Escape/F3/focus loss/reopen never applies a pending Minimap reset; controls behind modal cannot act.
3. Defaults come from the original entries, including bool/float values, counts and currencies; batch failure restores data and pending flags.
4. Dialog/renderer exceptions remain isolated, closeable and compatible with native input ownership; clean Begin/End and GUI globals.
5. Legacy SHA/profile/patch/F4/debounce behavior and HR alone/modern provider compatibility are retained.

## Task1 — Reset semantics and defaults

- [x] RED HR tests: selected tab only, draft-only until Apply, discard, invalid other-tab text retained, use explicit default metadata.
- [x] RED managed legacy fixture tests: all23entry mappings across4tabs, read DefaultValue rather than current values, cancel/no-op versus confirmation, only selected prefs written, original application once,0.5s queue, rollback on failure.
- [x] Implement NumericSetting default metadata/HrSettingsDraft.ResetTab and legacy tab schema/reset transaction. GREEN tests against both original DLL profiles; no native callbacks outside game.

## Task2 — Confirmation and approved UI

- [x] RED pure confirmation/session tests: captured tab, cancellation, single confirm, failure retention, Escape first cancels, no host lease release; optional modal contract validation.
- [x] Implement host modal callback boundary/background disabling, reset header/button, HR status, legacy confirmation overlay and lifecycle cancellation. Match approved images, wrap at small widths.
- [x] GREEN JIT/body-audit + regressions; no Unity execution claim.

## Task3 — Review and candidate

- [x] One read-only fresh-context review, one focused fix pass for material issues, RED/GREEN and full related suites.
- [x] Document beta.4 behavior/test scope, HR-only package, current and previous SHA/backups. Install only with game closed; do not replace old Minimap, change user values or publish.
- [x] Ask user to test HR reset/cancel/Apply and Minimap cancel/confirm/restart, other tabs preserved, Escape/control ownership; native matrix remains explicit.

Commercial value: recover from experimental values quickly while preserving unrelated settings; subsequent useful release follows actual feedback/compatibility needs.
