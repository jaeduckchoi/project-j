# Prototype placement baseline

Recorded 2026-09-13T23:56:12+09:00 before this subtask changes any Unreal asset. This subtask writes only layout.json and this baseline.

- Exact packages: `/Game/Jonggu/Maps/L_Hub` and `/Game/Jonggu/Maps/L_Beach`.
- Current collision primitives: Hub 16; Beach 74. Rebuilt rule/manifest IDs, centers and sizes match prior actual PIE readback within 0.02cm.
- Placement validation passed: 2496 path samples at at most 5cm spacing, all hotspots/spawns/customer anchors, player radius 24cm plus a required 2cm margin. Independent signed-distance checks against actual PIE primitives and 26cm circle/OBB checks against regenerated specs agree. Detailed per-point/path clearance and exact source SHA256 hashes are embedded in layout.json.
- Existing GPU images `Saved/CollisionWork/QA/StaticDepth/Hub_current_RGB.png` and `Beach_current_RGB.png` were visually inspected. All prototype positions fit the unchanged 3200x1800cm camera frame. Beach spawn and three clam nodes occupy the visible right-side sand; source player at (1750,0,1200) remains an off-screen original placement until runtime entry positioning.
- Hub uses the original pan/pot art, menu board, and left/middle tables. Enter the kitchen through X=0 then Z=470. Hotspots are floor positions; never move the original art or generated collision to these coordinates. Customer anchors are nonblocking free ground behind tables; existing chair sprites are unchanged.
- Hub return (-715,0,-520) is 180cm from the portal (-715,0,-700), outside the 130cm interaction radius. Beach spawn (550,0,400) is 200cm from its return portal (350,0,400). Portals remain explicitly activated, never overlap-triggered.

## Existing test failure, not introduced here

`Saved/CollisionWork/QA/collision_pie_report.json` already reports `success=false`. Its only failure is `up_only_corner_case_exercises_sideways_collision_slide`, scene `Beach`, case `left_then_up_only_corner`. The recorded test starts at [-345.0, 0.0, 536.0], moves left until 0.18s, switches to up, releases at 1.0s, and ends at 1.45s. Target: `collision:Beach:water:280:234:312:354`. This checks that the scenario exercises sideways collision sliding; it is not by itself proof of an input-facing gameplay defect. No movement code or test coordinates were changed here.

The current player asset/build report version is `blueprint-player-walk-9-input-facing`. Older outside-Migration v5 and stage report prose v8 success are not current all-pass evidence. New PIE verification must report this prior failure separately from prototype regressions.

## Remaining verification and constraints

- This report proves authored geometry feasibility, not new PIE behavior, physical keyboard input, UI focus, customer animation, or asset save/reload. Root owns those checks.
- Preserve source SourceCamera, player art, collisions, and Unity marker positions. Prototype actor IDs must use separate ownership; no `JongguMigration:` tags.
- Existing source migration target paths still require root reconciliation before any regeneration. This subtask neither reruns the importer nor promotes staged files.
- Existing stage cleanup report has `maps_reopen_verified=false`; stage independent `unreal_readback.json`, `walk_pie_report.json`, and `cleanup_reference_readback.json` were absent in the earlier baseline. Stage import_report separately records two successful imports and map reopens; these scopes must not be conflated.

## Implementation verification follow-up

The original scenario was rerun before prototype changes. `Saved/PrototypeQA/baseline_collision.json` records 594 checks and three failures in `left_then_up_only_corner`: initial facing, up-only facing, and actual sideways-slide exercise. The first moving sample is at 0.4s, after the configured 0.18s direction switch. This timing limit and the prior failure remain separate from prototype regression claims. Final prototype verification is recorded in [VALIDATION.md](D:/laeti-dev/ProjectJ/Tools/Prototype/VALIDATION.md).
