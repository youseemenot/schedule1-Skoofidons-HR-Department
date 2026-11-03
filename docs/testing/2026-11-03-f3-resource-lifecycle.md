# F3 resource lifecycle verification — 3 November 2026

Minimap 2.3.0 and HR Department 1.3.2 include shared runtime 1.4.1. The generated theme textures are explicitly owned, retained across unused-asset unloading and recreated if their native objects are destroyed. Cached styles recover their backgrounds and refresh when the game skin/font changes. Shutdown releases only the generated assets.

The author confirmed that the settings menu retains its graphics after repeated world → main menu → world loads, with Minimap alone and with both updated mods. The paired check included Phone App 2.2.4. The environment was Schedule I 0.4.7f12 IL2CPP, Unity 2022.3.62f2 and MelonLoader 0.7.3.

## Automated verification

- Source-linked theme regression checks: 8/8 in each repository. The earlier resource-lifetime model failed 7 of these 8 checks.
- Coverage includes repeated world loads, texture reuse, destroyed-object recovery, background rebinding, borrowed-texture protection, idempotent release/rebuild, skin/font replacement and bounded live texture count over 100 simulated load cycles.
- Minimap: tracking 18/18; native reset/registration 6/6; session 18/18; reset confirmation 4/4; frozen protocol 22/22; paired protocol 18/18 without Phone and 19/19 with Phone; native-provider bypass 1/1.
- HR: session 18/18; protocol 17/17; draft 8/8; confirmation 4/4; editor 5/5; smoke 26/26; legacy Minimap adapter 15/15.
- Current game API references resolved; reachable Unity wrappers contained no unsupported unstripping stubs. Both stable builds completed without warnings or errors.

The theme tests use deterministic Unity doubles; they do not reproduce native rendering or the deferred destruction queue. The user's visual check establishes the exercised native reload scenario. Co-op, every resolution/language/mod combination and quantitative performance comparisons are outside this acceptance.

## Stable promotion

The stable assemblies were compared with the exact user-tested Minimap 2.3.0-beta.4 and HR 1.3.2-beta.1 assemblies. Metadata and IL match after allowing only the release version and HR startup label. There are no employee, production, growth, tracking or layout changes in the stable promotion.

Run the self-contained theme suite from either checkout:

```powershell
dotnet run --project tests/ThemeResourceChecks -c Release -p:NuGetAudit=false
```

Minimap's `tools/Run-SharedMenuChecks.ps1` also accepts explicit paths to both built mods, the game installation, Phone App and frozen compatibility fixtures. The historical fixture binaries and local verification logs are not distributed with source or releases.
