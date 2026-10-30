# Machine timer module — local 1.2.0-beta.1 candidate

The user resumed this block and selected drying plus plant/fungal growth, applying factors only to new operations. Implementation lives in the root MachineTimer*.cs and MachineGrowth*.cs files. This directory remains an overview; stable1.1.0 does not contain active machine code. The local candidate awaits gameplay verification.

## Audited integration inventory

| Machine | Native clock | Inputs to preserve |
| --- | --- | --- |
| ChemistryStation | StationRecipe.CookTime_Mins, OnTimePass | Recipe identity, elapsed cook progress, temperature/quality, output finalization |
| LabOven | CookableModule.CookTime / OvenCookOperation.GetCookDuration | Ingredient quantity/quality, progress, door/harvest state |
| MixingStation / Mk2 | MixTimePerItem × MixOperation quantity, OnTimePass | Batch quantity, mixer consumption, progress and output |
| Cauldron | CookTime / RemainingCookTime | Operation identity, ingredients and product |

DryingRack changes per-operation float progress while retaining native quality transitions. Plant/fungal growth changes only positive deltas inside native growth callbacks, preserving their environmental checks and water timing. See [the design](../../docs/superpowers/specs/2026-10-30-machine-timers-design.md) and [candidate checklist](../../docs/testing/2026-10-30-machine-timers-candidate.md).

## Operation boundary and saves

Machine Timers has seven independent factors, default1. A batch captures its value at actual start. A preference change affects only subsequent batches. Existing/unrecognized operations and legacy saves without metadata use1. Mixing/Mk2 share a factor; simultaneous drying operations retain separate captures. No extra enable switch is required.

Native progress fields retain their meaning. Versioned additional DynamicSaveData stores captured factors and fractional integer-minute credit; malformed/future/mismatched data falls back to1. Saved asynchronous plant creation has a persistent restore marker; drying matches captured server requests to serialized receiver batches instead of object pointers. Removing the candidate retains native progress; its extra factors are then ignored and may be discarded on a subsequent save.

Preparation/recovery waits, worker Work speed, global time, watering and unrelated equipment remain independent. Passive spawn-station time is not duplicated: its existing stage belongs to employee work. Native per-tick completion and drying quality limits remain. Co-op synchronization is deferred.
