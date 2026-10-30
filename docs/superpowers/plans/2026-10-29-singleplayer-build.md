# Current-game singleplayer build implementation plan

> **For agentic workers:** Execute inline using superpowers:executing-plans. The user has requested implementation of the reviewed build repairs.

**Goal:** Build HR Department against the installed Schedule I 0.4.7f12 IL2CPP game and prepare a clearly identified singleplayer test DLL.

**Architecture:** Preserve existing preference keys and employee settings. Generate fresh reference assemblies separately from the game installation, use configurable dependency paths, and repair compatibility and singleplayer lifecycle issues without adding networking.

**Tech stack:** C# 11, .NET 6, MelonLoader 0.7.3, Harmony, Mod Manager & Phone App 2.2.4.

**Spec:** User request in this chat: restore the current-game build, fix errors, update documentation, prepare for singleplayer testing; leave co-op untouched.

## Constraints and review focus

- The installed game is 0.4.7f12, Steam build 25796331. August logs and interop assemblies describe the older 0.4.6f13 installation.
- Do not edit savegames, start a campaign, or publish a release. Preserve the existing disabled DLL.
- Retain preference keys and absolute walk-speed meaning; do not silently turn stored values into multipliers.
- Use the existing clean checkout; no extra worktree is necessary for this isolated mod repository.
- Check repeated settings application, newly hired employees, unloading/reloading, UI closure, and route-label refresh.
- Compilation and automated checks do not establish that gameplay has passed.

## Tasks

### 1. Current references and failing baseline

- [x] Extract current metadata and regenerate IL2CPP interop assemblies under workspace artifacts using installed tools.
- [x] Inspect the current movement API and reproduce compilation failures against these references.
- [x] Record the game version and binary hash with generated references.

### 2. Compatibility and singleplayer repairs

- [x] Add configurable game/interop paths and clear missing-reference errors to the project; keep .NET 6 and prevent dependency DLLs being packaged.
- [x] Repair walk-speed application using the current game's API without cumulative changes.
- [x] Add meaningful regression checks for any nontrivial conversion/state handling, observe failure first, then verify the fix.
- [x] Repair employee initialization, clipboard closing, and live route-title refresh where current API confirms the issue.
- [x] Build Release against current references and review the entire diff.

### 3. Documentation and test handoff

- [x] Update README, changelog, and a singleplayer checklist with exact versions, limitations, build instructions, and rollback.
- [x] Correct the workspace summary's stale checkout, game-version, build, and botanist-timing statements.
- [x] Stage a versioned DLL for testing, verify its hash and packaged contents, and preserve the old DLL.
- [x] Run build and regression checks, obtain a final code review, and report what remains for in-game testing.

## Verification record

- Original source against fresh 0.4.7f12 references: four CS1061 errors for removed WalkSpeed.
- Current Release: zero errors and warnings; references generated from the installed binary and stamped with its SHA-512.
- Managed clipboard regression: legacy disabled DLL vetoed Close; candidate passes 4/4.
- Stale-reference and missing-dependency guards reject invalid inputs; PowerShell parser and git diff --check pass.
- Independent review: no Critical/Important findings. Plan-checkbox polish addressed here.
- Test DLL installed with matching SHA-256; previous disabled DLL preserved. Savegames, preferences, and co-op code unchanged.
- 2026-10-30 installation correction: moved the legacy `.dll.disabled` outside `Mods` into workspace artifacts/hr-backups/2026-10-30. Keeping both toggle destination names occupied caused Phone App rename collisions. Both DLL hashes verified; installation instructions corrected.
- Native initialization, temporary speed modifiers, hiring/payment, actual assignment execution, Unity layout, reload behavior, and other-mod compatibility remain pending in-game checks.
