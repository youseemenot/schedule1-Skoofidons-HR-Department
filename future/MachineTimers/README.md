# Machine timer module — stable1.2.0

The user resumed this block and selected drying plus plant/fungal growth, applying machine factors to new batches and current growth factors to all growing crops. Implementation lives in the root MachineTimer*.cs and MachineGrowth*.cs files. This directory remains an overview; stable1.1.0 does not contain active machine code. Stable1.2.0 promotes user-tested beta.2; see the test result and release notes in docs.

## Audited integration inventory

| Machine | Native clock | Inputs to preserve |
| --- | --- | --- |
| ChemistryStation | StationRecipe.CookTime_Mins, OnTimePass | Recipe identity, elapsed cook progress, temperature/quality, output finalization |
| LabOven | CookableModule.CookTime / OvenCookOperation.GetCookDuration | Ingredient quantity/quality, progress, door/harvest state |
| MixingStation / Mk2 | MixTimePerItem × MixOperation quantity, OnTimePass | Batch quantity, mixer consumption, progress and output |
| Cauldron | CookTime / RemainingCookTime | Operation identity, ingredients and product |

DryingRack changes per-operation float progress while retaining native quality transitions. Plant/fungal growth changes only positive deltas inside native growth callbacks, preserving their environmental checks and water timing. See [the design](../../docs/superpowers/specs/2026-10-30-machine-timers-design.md) and [candidate checklist](../../docs/testing/2026-10-30-machine-timers-beta2.md).

## Operation boundary and saves

Machine Timers has seven independent factors, default1. The five machine kinds capture their value at actual batch start. A preference change affects only subsequent batches. Existing/unrecognized operations and legacy saves without metadata use1. Mixing/Mk2 share a factor; simultaneous drying operations retain separate captures. Plant and mushroom growth instead reads the current factor each native growth callback for all growing cultures, preserving past progress. No extra enable switch is required.

Native progress fields retain their meaning. Versioned additional DynamicSaveData stores captured factors and fractional integer-minute credit; malformed/future/mismatched data falls back to1. Crop factors need no capture/save/load machinery. Drying matches captured server requests to serialized receiver batches instead of object pointers. Removing the mod retains native progress; its extra factors are then ignored and may be discarded on a subsequent save.

Preparation/recovery waits, worker Work speed, global time, watering and unrelated equipment remain independent. Passive spawn-station time is not duplicated: its existing stage belongs to employee work. Native per-tick completion and drying quality limits remain. Co-op synchronization is deferred.
