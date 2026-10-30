# Handler station rows Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow existing Max Stations settings above 10 to display every Handler station with working removal and scrolling.

**Architecture:** Extend the native Entries array before ObjectListFieldUI.Refresh. Keep native row population, selection and removal; own callbacks on cloned rows and normalize them after native Bind. Use a small managed expansion operation with rollback so offline checks exercise the actual allocation policy.

**Tech Stack:** C# 11, net6.0, MelonLoader 0.7.3, Harmony, IL2CPP Unity UI.

**Spec:** ../specs/2026-10-30-handler-station-rows-design.md

## Global Constraints

- Singleplayer, installed Schedule I 0.4.7f12 IL2CPP references; no co-op changes.
- Reuse the current checkout and feature branch; no publication requested.
- Preserve station data when lowering limits; do not preallocate to an arbitrary configured maximum.

## Review Focus

- Bind calls Refresh before registering deletion callbacks: prevent duplicate or inherited callbacks.
- Other professions and multi-selection keep native behavior.
- Removing rows near bottom must leave reachable content, with no blank out-of-bounds scroll position.
- Scene changes and destroyed panels must not retain UI references.
- Factory failure rolls back new rows without changing assignments.

### Task 1: Extend Handler station rows and prepare candidate

**Files:** Create StationRowExpansion.cs and HandlerStationController.cs; modify PackagerUIFix.cs, Core.cs, tests/SmokeChecks/Program.cs, README.md, CHANGELOG.md and focused testing documentation.

**Interfaces:** StationRowExpansion.Expand<T>(T[] entries, int assignedCount, Func<T,int,T> create, Action<T> destroy) returns same or fully expanded array, rolls back created resources on failure. HandlerStationController hooks Bind/Refresh and clears tracked references on unload. Existing PackagerUIFix supplies the viewport.

- [x] Add failing managed checks: 10→15 rows retaining originals and correct factory indexes; a smaller count never shrinks; repeated count does not allocate; factory failure rolls back; no-template nonempty input fails safely.
- [x] Implement expansion and Handler-only native adapter. Verify Remove button child and native callback ordering against local prefab/disassembly.
- [x] Update layout after Refresh, preserve scroll during same selection, reset on Bind. Update candidate version and docs.
- [x] Build Release; run SmokeChecks and external API/Harmony validation; fresh-context review of the final diff, fix important findings.
- [x] Package and install only with game closed, back up previous DLL outside Mods, record hashes and gameplay limitations, commit locally.
