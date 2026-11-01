# Employee assignment limit correction

> **For agentic workers:** Use superpowers:executing-plans, systematic-debugging and verification-before-completion.

**Goal:** Correct live Botanist limits and audit all four employee assignment lists before continuing Nexus materials.
**Spec:** User reports Botanist remains Assigned8/8 despite configured11, then requests checks of other professions. Active1.3.0 config records11; supplied logcf50e0f4... has116lines, no errors. Native constructor0x5EC2CC reads Botanist+0x388(MaxAssignedPots) and0x5EC2D2 writes ObjectListField+0x20(MaxItems) via configuration.Assigns. Existing ApplyStats updates only employee.MaxAssignedPots. Handler already updates both fields; Chemist/Collector(Cleaner) update their live Stations/Bins.MaxItems. Row cloning currently accepts Handler only.
**Architecture:** Reuse typed native assignment fields and existing demand-based row cloning/scrolling. Update Botanist.configuration.Assigns.MaxItems alongside MaxAssignedPots. Apply existing row allocation/visibility/deletion to exact Stations/Assigns/Bins fields of all four configurations. Recognize fields by configuration type and pointer identity, leave unrelated lists and multi-selection native. No new settings/layout, prototype or speed changes. Existing approved clipboard design applies.
**Scope:** Local1.3.1-beta.1 candidate, preserve1.3.0 public assets/tag; no new publication. User-reported issue takes priority over Nexus artwork. Never replace DLL while game runs; do not change prefs or assignments.

## Review focus

- Existing employee configuration uses new limit after save; prefab may not yet have config.
- All four live selection fields receive their own correct preference.
- Single-employee lists expand only to actual occupancy, preserving native rows and indexed removal.
- Reducing limits never deletes assignments; repeated binds/refresh do not duplicate listeners/rows; multi-selection remains native.
- Same scroll/layout mechanism and Supplies/Bed space remain intact for Botanist; Handler routes unchanged.

- [x] Add failing actual-DLL API-boundary checks for live Botanist limit and all four employee adapters/list field recognition. Exercise8→11row allocation and unchanged/lowered occupancy with existing allocator.
- [x] Correct typed limit propagation and generalize current list adapter; bump local candidate version.
- [x] Build; run smoke/assignment, shared settings and previous suites/API/Unity-body audits. Independent review before installation.
- [ ] Update docs with defect/scope/checks. Prepare candidate package and request closed game only once ready, back up and replace HR if possible. Keep Minimap/cfg unchanged; native acceptance pending.
