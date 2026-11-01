# Settings Footer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the approved footer wording and grouping in HR beta.5 for testing.
**Architecture:** Optional reset action on the existing provider contract, rendered by the elected host; existing reset methods retain their semantics.
**Tech Stack:** C#11/net6, Unity IMGUI, MelonLoader, BCL provider manifests.
**Spec:** `docs/superpowers/specs/2026-11-01-settings-footer-design.md`

## Global Constraints

- Existing branch/checkouts reused; user approved UI and requested execution. Minimap DLL and cfg remain unchanged.
- Only current tab resets. HR draft + Apply; Minimap confirmation + live apply.
- RU/EN follow game language; native input lease and modal isolation preserved.
- No publication. HR version 1.3.0-beta.5; embedded capability 1.4.0.

## Review Focus

- Older provider with no reset: no phantom action, same lifecycle.
- Host capability ordering: reset providers require host1.4.0.
- Narrow windows/Russian labels: footer buttons and status cannot overlap.
- Modal opened by footer reset: background blocked and dialog drawn last in same event.
- Provider exception during reset: existing session isolation closes safely.

### Task 1: Footer integration and candidate

**Files:** SharedSettings/V1/{EntryPointV1,ProviderManifest,MenuFooterLayout,SharedMenuRuntime,MenuTheme}.cs; HrSettingsPanel.cs; LegacyMinimap{Panel,Integration}.cs; Core.cs; tests/SharedSettingsChecks; README.md; CHANGELOG.md; docs/testing/2026-11-01-settings-footer.md.
**Interfaces:** ConfigureDefaults(Action reset) publishes optional `reset` BCL Action; ProviderManifest.Reset; MenuFooterLayout derives footer/button/status bounds from width, language and action availability without Unity dependencies.

- [x] Add and run failing shared contract checks for optional/invalid reset, retained callbacks and host floor. Add meaningful layout invariants at wide/narrow widths for all action combinations.
- [x] Implement reset capability, host footer, header cleanup and wording; bump candidate metadata.
- [x] Build with existing Build.ps1; run shared/session/draft/confirmation/numeric/legacy/paired/smoke checks and Unity stub audit. Inspect final diff and obtain independent review.
- [ ] Document results, package HR DLL + build-info + testing guide, verify game closed, back up and replace only HR DLL; verify Minimap/cfg hashes unchanged.
- [ ] Record hashes and pending native checks, commit local changes; leave branch unpublished.
