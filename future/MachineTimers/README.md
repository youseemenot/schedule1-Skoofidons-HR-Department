# Machine timer module — inactive scaffold

Status: deferred by the user on 2026-10-30. This directory is excluded from the current top-level C# compile. It registers no settings, patches, save fields or runtime callbacks. Employee Work speed must not consume these machine clocks.

## Audited integration inventory

| Machine | Native clock | Inputs to preserve |
| --- | --- | --- |
| ChemistryStation | StationRecipe.CookTime_Mins, OnTimePass | Recipe identity, elapsed cook progress, temperature/quality, output finalization |
| LabOven | CookableModule.CookTime / OvenCookOperation.GetCookDuration | Ingredient quantity/quality, progress, door/harvest state |
| MixingStation / Mk2 | MixTimePerItem × MixOperation quantity, OnTimePass | Batch quantity, mixer consumption, progress and output |
| Cauldron | CookTime / RemainingCookTime | Operation identity, ingredients and product |

See [the native process audit](../../docs/research/2026-10-30-employee-process-coverage.md) for addresses and evidence. Drying/world growth clocks require a separate audit before adding them to this inventory.

## Reserved boundary

Future configuration belongs in its own Machine Timers category, independent of employee type and presence. Defaults must preserve native clocks. Do not add entries or compile an override until this block is explicitly resumed.

Before implementation, decide whether changes affect only new operations or also running ones, how to retain fractional progress when native timers are integer game minutes, and how to distinguish player-started operations. Then audit save/reload, recipe completion/resource quantities, Mk2 behavior, cancellation and any shared native bodies. Co-op needs its own design; it is not implicitly enabled by this outline.
