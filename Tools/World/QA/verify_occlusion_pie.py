"""Move the saved Pawn around props and render front/behind evidence in PIE.

Run only in a NEW rendered editor with -JongguOcclusionQAAutomation and
-ExecutePythonScript=<absolute path to this file>. No content or map is saved.
The companion analyze_occlusion_captures.py compares rendered overlap pixels.
"""
from __future__ import annotations

import json
import math
from pathlib import Path
import sys
import time
import traceback
import unreal

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from collision_qa_geometry import clearance, clear_segment, length, normalized, read_shapes, sub, xyz
sys.path.insert(0, str(HERE.parent))
from world_paths import CONTENT_ROOT, DATA_ROOT, PROJECT_ROOT, QA_ROOT, require_qa_launch

SCRIPT = Path(__file__).resolve()
MARKER = "-JongguOcclusionQAAutomation"
OUTPUT = QA_ROOT / "Occlusion"
REPORT_PATH = QA_ROOT / "occlusion_pie_report.json"
LEVELS = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
MANIFEST = json.loads((DATA_ROOT / "render_manifest.json").read_text(encoding="utf-8"))
REPORT = {"schema_version": 1, "success": False, "checks": 0, "failures": [], "cases": [],
          "runtime": "Actual saved Pawn driven through AddMovementInput in Unreal PIE",
          "physical_keyboard_tested": False, "pixel_comparison_required": True}
STATE = {"stage": "loading", "index": 0, "started": time.monotonic(), "authorized": False,
         "finished": False, "last_time": -1.0}
HANDLE = None

CASES = [
    {"name": "menu_board", "scene": "Hub", "target_contains": "/HubTodayMenuBoard/",
     "start": [542, 0, 170], "front": [542, 0, 235],
     "around": [[750, 0, 235], [750, 0, 370]], "behind": [542, 0, 370]},
    {"name": "front_counter", "scene": "Hub", "target_contains": "/HubObjectLayer/PassCounter/",
     "start": [-700, 0, 170], "front": [-700, 0, 235],
     "around": [[-1410, 0, 235], [-1410, 0, 420]], "behind": [-700, 0, 420]},
    {"name": "dining_table", "scene": "Hub", "target_contains": "/TableChair2Top",
     "start": [0, 0, -430], "front": [0, 0, -365],
     "around": [[310, 0, -365], [310, 0, -165]], "behind": [0, 0, -165]},
    {"name": "tree_trunk_canopy", "scene": "Beach", "target_contains": "/BeachTree03",
     "start": [833, 0, 970], "front": [833, 0, 1040],
     "around": [[930, 0, 1040], [930, 0, 1212]], "behind": [833, 0, 1212]},
]


def check(ok, label, **context):
    REPORT["checks"] += 1
    if not ok:
        REPORT["failures"].append({"check": label, **context})


def write_report():
    REPORT_PATH.write_text(json.dumps(REPORT, indent=2), encoding="utf-8")


def finish():
    global HANDLE
    if STATE["finished"]:
        return
    STATE["finished"] = True
    if HANDLE is not None:
        unreal.unregister_slate_post_tick_callback(HANDLE)
        HANDLE = None
    REPORT["success"] = not REPORT["failures"] and len(REPORT["cases"]) == len(CASES)
    REPORT["elapsed_wall_seconds"] = time.monotonic() - STATE["started"]
    write_report()
    unreal.log("JONGGU_OCCLUSION_PIE_RESULT " + json.dumps({"success": REPORT["success"], "report": str(REPORT_PATH)}))
    if STATE["authorized"]:
        if LEVELS.is_in_play_in_editor():
            LEVELS.editor_request_end_play()
        unreal.EditorPythonScripting.set_keep_python_script_alive(False)
        unreal.SystemLibrary.quit_editor()


def target_actors(world, case):
    scene = next(s for s in MANIFEST["scenes"] if s["name"] == case["scene"])
    groups = [g for g in scene["groups"] if case["target_contains"] in g["path"]
              and g["status"] == "visible" and g["instances"]]
    tags = {"JongguMigration:render:" + g["id"] for g in groups}
    actors = [a for a in unreal.GameplayStatics.get_all_actors_of_class(world, unreal.Actor)
              if tags.intersection(str(t) for t in a.tags)]
    if len(actors) != len(groups) or not actors:
        raise RuntimeError("Cannot identify all saved target render actors for " + case["name"])
    return groups, actors


def render_variants(world, pawn, case, view):
    """Four actual renders distinguish player and prop contributions per pixel."""
    groups, targets = target_actors(world, case)
    controller = unreal.GameplayStatics.get_player_controller(world, 0)
    source = controller.get_view_target()
    bs = [g["bounds"] for g in groups]
    lo = [100 * min(b["min"][i] for b in bs) for i in (0, 1)]
    hi = [100 * max(b["max"][i] for b in bs) for i in (0, 1)]
    width = max(hi[0] - lo[0] + 250, (hi[1] - lo[1] + 220) * 16 / 9)
    loc = unreal.Vector((lo[0] + hi[0]) / 2, source.get_actor_location().y, (lo[1] + hi[1]) / 2)
    # A dedicated unsaved editor QA actor is duplicated into PIE with the map.
    # Blueprint-internal runtime spawning functions are intentionally not
    # exposed by Unreal Python; the duplicated actor captures the actual world.
    cap = next(a for a in unreal.GameplayStatics.get_all_actors_of_class(world, unreal.SceneCapture2D)
               if "JongguOcclusionCapture" in [str(t) for t in a.tags])
    cap.set_actor_location(loc, False, True)
    cap.set_actor_rotation(source.get_actor_rotation(), True)
    comp = cap.capture_component2d
    comp.set_editor_property("projection_type", unreal.CameraProjectionMode.ORTHOGRAPHIC)
    comp.set_editor_property("ortho_width", width)
    comp.set_editor_property("auto_calculate_ortho_planes", False)
    comp.set_editor_property("capture_every_frame", False)
    comp.set_editor_property("capture_on_movement", False)
    comp.set_editor_property("always_persist_rendering_state", True)
    comp.set_editor_property("capture_source", unreal.SceneCaptureSource.SCS_FINAL_COLOR_LDR)
    comp.set_editor_property("post_process_settings", source.camera_component.get_editor_property("post_process_settings"))
    comp.set_editor_property("post_process_blend_weight", 1.0)
    flags = []
    for name in ("AntiAliasing", "TemporalAA", "Bloom", "MotionBlur", "LensFlares"):
        flag = unreal.EngineShowFlagsSetting()
        flag.show_flag_name, flag.enabled = name, False
        flags.append(flag)
    comp.set_editor_property("show_flag_settings", flags)
    target = unreal.RenderingLibrary.create_render_target2d(world, 1920, 1080,
        unreal.TextureRenderTargetFormat.RTF_RGBA8, unreal.LinearColor(0, 0, 0, 1), False)
    target.set_editor_property("target_gamma", 2.2)
    comp.set_editor_property("texture_target", target)
    player_priority = int(pawn.get_editor_property("player_visual").get_editor_property("translucency_sort_priority"))
    target_priorities = []
    for actor in targets:
        component = actor.get_component_by_class(unreal.PaperSpriteComponent) or actor.get_component_by_class(unreal.PaperGroupedSpriteComponent)
        target_priorities.append(int(component.get_editor_property("translucency_sort_priority")))
    check(all(player_priority > p if view == "front" else player_priority < p for p in target_priorities),
          "feet_depth_places_player_on_correct_side", case=case["name"], view=view,
          player_priority=player_priority, target_priorities=target_priorities)
    variants = {}
    try:
        for label, hide_player, hide_target in (("actual", False, False), ("world_only", True, False),
                                               ("player_without_target", False, True), ("background", True, True)):
            pawn.set_actor_hidden_in_game(hide_player)
            for actor in targets:
                actor.set_actor_hidden_in_game(hide_target)
            for _ in range(3):
                comp.capture_scene()
            filename = case["name"] + "_" + view + "_" + label + ".png"
            unreal.RenderingLibrary.export_render_target(world, target, str(OUTPUT), filename)
            variants[label] = str(OUTPUT / filename)
            check((OUTPUT / filename).is_file(), "actual_runtime_occlusion_render_written", file=filename)
    finally:
        pawn.set_actor_hidden_in_game(False)
        for actor in targets:
            actor.set_actor_hidden_in_game(False)
    return {"view": view, "position": xyz(pawn.get_actor_location()), "player_priority": player_priority,
            "target_priorities": target_priorities, "variants": variants,
            "capture_ortho_width": width, "capture_location": xyz(loc), "source_camera_modified": False}


def inspect_frame(pawn):
    position = xyz(pawn.get_actor_location())
    expected = -math.floor(position[2] + 0.5)
    priorities = [int(pawn.get_editor_property(n).get_editor_property("translucency_sort_priority"))
                  for n in ("player_visual", "walk_head", "walk_body", "walk_left_foot", "walk_right_foot")]
    return {"position": position, "expected_priority": expected, "priorities": priorities,
            "clearance": clearance(position, STATE["shapes"]), "speed": length(xyz(pawn.get_velocity()))}


def start_case(world, pawn, now):
    case = CASES[STATE["index"]]
    STATE["case"] = {"name": case["name"], "scene": case["scene"], "frames": [], "captures": []}
    STATE["shapes"] = read_shapes(world, unreal)
    route = [case["start"], case["front"]] + case["around"] + [case["behind"]]
    check(all(clear_segment(a, b, STATE["shapes"], margin=0.5) for a, b in zip(route, route[1:])),
          "front_to_behind_route_clear_for_saved_root", case=case["name"])
    pawn.get_component_by_class(unreal.FloatingPawnMovement).stop_movement_immediately()
    pawn.consume_movement_input_vector()
    pawn.set_actor_location(unreal.Vector(*case["start"]), False, True)
    STATE.update(stage="moving_front", destination=case["front"], case_start=now, last_time=-1.0,
                 around_index=0, last_position=case["start"], travel_cm=0.0)


def tick(delta):
    try:
        if time.monotonic() - STATE["started"] > 360:
            raise RuntimeError("Occlusion PIE timeout in " + STATE["stage"])
        case = CASES[STATE["index"]]
        if STATE["stage"] == "loading":
            if not LEVELS.load_level(CONTENT_ROOT + "/Maps/L_" + case["scene"]):
                raise RuntimeError("Cannot load " + case["scene"])
            capture_actor = unreal.get_editor_subsystem(unreal.EditorActorSubsystem).spawn_actor_from_class(
                unreal.SceneCapture2D, unreal.Vector(), unreal.Rotator(), transient=False)
            capture_actor.tags = ["JongguOcclusionCapture"]
            capture_actor.capture_component2d.set_editor_property("capture_every_frame", False)
            capture_actor.capture_component2d.set_editor_property("capture_on_movement", False)
            STATE["stage"] = "starting"
            LEVELS.editor_request_begin_play()
            return
        if STATE["stage"] == "ending":
            if not LEVELS.is_in_play_in_editor():
                STATE["stage"] = "loading"
            return
        if not LEVELS.is_in_play_in_editor():
            return
        world = unreal.EditorLevelLibrary.get_game_world()
        pawn = unreal.GameplayStatics.get_player_pawn(world, 0) if world else None
        if pawn is None:
            return
        now = unreal.GameplayStatics.get_time_seconds(world)
        if STATE["stage"] == "starting":
            start_case(world, pawn, now)
        if now <= STATE["last_time"]:
            return
        STATE["last_time"] = now
        if now - STATE["case_start"] > 35:
            raise RuntimeError("Cannot complete around-prop route: " + case["name"])
        frame = inspect_frame(pawn)
        STATE["case"]["frames"].append(frame)
        STATE["travel_cm"] += length(sub(frame["position"], STATE["last_position"]))
        STATE["last_position"] = frame["position"]
        if STATE["stage"].startswith("moving"):
            delta_position = sub(STATE["destination"], frame["position"])
            if length(delta_position) <= 8:
                pawn.get_component_by_class(unreal.FloatingPawnMovement).stop_movement_immediately()
                pawn.consume_movement_input_vector()
                if STATE["stage"] == "moving_around":
                    STATE["around_index"] += 1
                    if STATE["around_index"] < len(case["around"]):
                        STATE["destination"] = case["around"][STATE["around_index"]]
                    else:
                        STATE.update(stage="moving_behind", destination=case["behind"])
                else:
                    STATE.update(stage="settling_front" if STATE["stage"] == "moving_front" else "settling_behind", settled_at=now)
                return
            # Reduce input close to waypoints to arrive accurately without
            # teleporting; this remains the player's native movement component.
            pawn.add_movement_input(unreal.Vector(*normalized(delta_position)), min(1.0, length(delta_position) / 80), False)
            return
        if now - STATE["settled_at"] < 0.65:
            return
        view = "front" if STATE["stage"] == "settling_front" else "behind"
        STATE["case"]["captures"].append(render_variants(world, pawn, case, view))
        if view == "front":
            STATE.update(stage="moving_around", destination=case["around"][0], around_index=0)
            return
        frames = STATE["case"]["frames"]
        # Ignore only the first frame after the test's initial relocation; all
        # actual traversed positions must update every visible part together.
        stable = frames[1:]
        check(stable and all(all(p == f["expected_priority"] for p in f["priorities"]) for f in stable),
              "all_player_parts_follow_feet_priority_every_frame", case=case["name"])
        check(all(f["clearance"] >= -0.5 for f in frames), "around_prop_motion_never_penetrates", case=case["name"])
        check(len({f["priorities"][0] for f in stable}) >= 5,
              "actual_motion_changes_depth_priority", case=case["name"])
        STATE["case"]["actual_travel_cm"] = STATE["travel_cm"]
        REPORT["cases"].append(STATE["case"])
        write_report()
        STATE["index"] += 1
        if STATE["index"] == len(CASES):
            finish()
        elif CASES[STATE["index"]]["scene"] != case["scene"]:
            STATE["stage"] = "ending"
            LEVELS.editor_request_end_play()
        else:
            STATE["stage"] = "starting"
    except Exception:
        REPORT["failures"].append({"exception": traceback.format_exc(), "stage": STATE["stage"]})
        REPORT["incomplete_case"] = STATE.get("case")
        finish()


def main():
    global HANDLE
    require_qa_launch(MARKER, SCRIPT)
    active = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active != PROJECT_ROOT or LEVELS.is_in_play_in_editor():
        raise RuntimeError("Unexpected project or existing PIE session")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    STATE["authorized"] = True
    unreal.EditorPythonScripting.set_keep_python_script_alive(True)
    write_report()
    HANDLE = unreal.register_slate_post_tick_callback(tick)


if __name__ == "__main__":
    main()
