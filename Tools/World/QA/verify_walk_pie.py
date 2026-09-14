"""Verify the saved eight-pose part walk and normalized eight-way movement in PIE.

Run in a NEW automation editor, never the user's existing interactive editor:
  UnrealEditor.exe projectJ.uproject -JongguWalkQAAutomation \
    -ExecutePythonScript=<absolute path to this file> -unattended -nosplash

The launch marker and script path are mandatory. Only that marked process is
allowed to start/end PIE or quit. No assets or maps are saved by this script.
Input is injected at Pawn.AddMovementInput, the keyboard graph's runtime entry;
physical keyboard events are not exercised. Use a rendered editor at >=30 FPS
to observe every pose in the 20-pose-per-second walk cycle.
"""
from __future__ import annotations

import json
import math
from pathlib import Path
import time
import traceback
import sys

import unreal

SCRIPT_PATH = Path(__file__).resolve()
sys.path.insert(0, str(SCRIPT_PATH.parent.parent))
sys.path.insert(0, str(SCRIPT_PATH.parent))
from world_paths import PROJECT_ROOT, DATA_ROOT, QA_ROOT, require_qa_launch
from collision_qa_geometry import clearance, find_clear_lane, read_shapes
REPORT_PATH = QA_ROOT / "walk_pie_report.json"
CONTENT_ROOT = "/Game/Jonggu"
PLAYER_PATH = CONTENT_ROOT + "/Blueprints/Player/BP_JongguPlayer"
LAUNCH_MARKER = "-JongguWalkQAAutomation"
SETTLE_SECONDS = 0.3
WALK_SECONDS = 1.8  # 1.5 s after settling, more than three full 160 cm cycles.
IDLE_SECONDS = 1.1
LEVELS = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
REPORT = {
    "schema_version": 3,
    "success": False,
    "runtime": "Unreal PIE",
    "input_method": "Pawn.AddMovementInput normalized XZ vectors",
    "physical_keyboard_tested": False,
    "collision_safe_phase_reset": "Each moving phase begins on a verified clear swept-sphere lane in the saved map; facing and animation state remain live.",
    "settle_seconds": SETTLE_SECONDS,
    "report_path": str(REPORT_PATH),
    "checks": 0,
    "failures": [],
    "scenes": [],
}

# Enum: front=0, back=1, left=2, right=3. At a 45-degree diagonal the
# previous compatible facing axis remains; an incompatible facing picks side.
PHASES = [
    ("idle_front", IDLE_SECONDS, (0, 0, 0), 0),
    ("front", WALK_SECONDS, (0, 0, -1), 0),
    ("stop_front", IDLE_SECONDS, (0, 0, 0), 0),
    ("southeast_keeps_front", WALK_SECONDS, (1, 0, -1), 0),
    ("right", WALK_SECONDS, (1, 0, 0), 3),
    ("northeast_keeps_right", WALK_SECONDS, (1, 0, 1), 3),
    ("back", WALK_SECONDS, (0, 0, 1), 1),
    ("northwest_keeps_back", WALK_SECONDS, (-1, 0, 1), 1),
    ("left", WALK_SECONDS, (-1, 0, 0), 2),
    ("southwest_keeps_left", WALK_SECONDS, (-1, 0, -1), 2),
    ("stop_left", IDLE_SECONDS, (0, 0, 0), 2),
    ("northeast_incompatible_facing_chooses_right", WALK_SECONDS, (1, 0, 1), 3),
    ("stop_northeast", IDLE_SECONDS, (0, 0, 0), 3),
    ("restart_northeast", WALK_SECONDS, (1, 0, 1), 3),
    ("front_again", WALK_SECONDS, (0, 0, -1), 0),
    ("stop_final", IDLE_SECONDS, (0, 0, 0), 0),
]
STATE = {
    "scene_index": 0,
    "stage": "loading",
    "started": time.monotonic(),
    "phase": 0,
    "phase_start": None,
    "samples": [],
    "last_sample": -1.0,
    "authorized_process": False,
    "finished": False,
}
HANDLE = None


def check(ok, label, **data):
    REPORT["checks"] += 1
    if not ok:
        REPORT["failures"].append({"check": label, **data})


def xyz(vector):
    return [float(vector.x), float(vector.y), float(vector.z)]


def magnitude(vector):
    return math.sqrt(sum(value * value for value in vector))


def asset_path(asset):
    return asset.get_path_name() if asset is not None else None


def write_report():
    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(REPORT, indent=2), encoding="utf-8")


def finish():
    global HANDLE
    if STATE["finished"]:
        return
    STATE["finished"] = True
    if HANDLE is not None:
        unreal.unregister_slate_post_tick_callback(HANDLE)
        HANDLE = None
    REPORT["success"] = not REPORT["failures"] and len(REPORT["scenes"]) == 2
    REPORT["elapsed_wall_seconds"] = time.monotonic() - STATE["started"]
    write_report()
    unreal.log("JONGGU_WALK_PIE_RESULT " + json.dumps({
        "success": REPORT["success"], "checks": REPORT["checks"],
        "failures": len(REPORT["failures"]), "report": str(REPORT_PATH),
    }))
    if STATE["authorized_process"]:
        if LEVELS.is_in_play_in_editor():
            LEVELS.editor_request_end_play()
        unreal.EditorPythonScripting.set_keep_python_script_alive(False)
        unreal.SystemLibrary.quit_editor()


def summarize_phase():
    name, duration, direction, expected_facing = PHASES[STATE["phase"]]
    samples = STATE["samples"]
    steady = [sample for sample in samples if sample["phase_time"] >= SETTLE_SECONDS]
    context = {"scene": STATE["scene"]["name"], "phase": name}
    moving = any(direction)
    check(len(steady) >= 8, "runtime_phase_samples", count=len(steady), **context)
    if not steady:
        STATE["scene"]["phases"].append({"name": name, "samples": samples})
        return

    frames_seen = sorted({sample["frame"] for sample in steady})
    speed_min = min(sample["speed"] for sample in steady)
    speed_max = max(sample["speed"] for sample in steady)
    check(all(sample["moving"] == moving for sample in steady), "runtime_moving_state", **context)
    check(all(sample["facing"] == expected_facing for sample in steady),
          "stable_facing_with_diagonal_hysteresis", expected=expected_facing,
          observed=sorted({sample["facing"] for sample in steady}), **context)
    if moving:
        check(all(part["sort_priority"] == sample["sort_priority"] for sample in steady for part in sample["parts"]),
              "walking_preserves_world_occlusion_priority", **context)
        check(all(sample["sprite"] is None for sample in steady),
              "walking_hides_whole_idle_sprite", **context)
        check(all(part["sprite"] is not None and part["sprite"] == part["expected_sprite"]
                  and part["visible"] for sample in steady for part in sample["parts"]),
              "walking_parts_sprite_selection_and_visibility", **context)
    else:
        check(all(sample["sprite"] is not None and sample["sprite"] == sample["expected_sprite"]
                  for sample in steady), "stopped_restores_original_idle_sprite", **context)
        check(all(not part["visible"] for sample in steady for part in sample["parts"]),
              "stopped_hides_all_walking_parts", **context)
    check(all((sample["scale"][0] < 0) == sample["expected_flip"] for sample in steady),
          "runtime_side_flip", **context)
    check(all(abs(sample["position"][1] - STATE["scene"]["initial_position"][1]) < 0.01
              for sample in samples), "runtime_y_plane_constant", **context)
    check(all(sample["view_target"] == STATE["scene"]["view_target"] for sample in samples),
          "camera_preserved_through_movement", **context)
    check(all(sample["pawn_path"] == STATE["scene"]["pawn_path"] for sample in samples),
          "possession_preserved_through_movement", **context)
    check(all(0.0 <= sample["walk_phase"] < 1.000001 for sample in samples),
          "walk_phase_normalized", **context)
    check(all(-0.001 <= sample["walk_blend"] <= 1.001 for sample in samples),
          "walk_blend_normalized", **context)
    check(all(abs(sample["visual_location"][0]) < 0.01 and
              abs(sample["visual_location"][1]) < 0.01 and
              abs(sample["visual_location"][2]) <= 4.0 for sample in samples),
          "visual_bob_small_and_vertical_only", **context)

    phase_summary = {
        "name": name, "direction": list(direction), "expected_facing": expected_facing,
        "walk_pose_indexes_observed" if moving else "idle_frames_observed": frames_seen,
        "steady_speed_range": [speed_min, speed_max], "samples": samples,
    }
    if moving:
        check(frames_seen == list(range(8)), "walk_observes_all_eight_pose_indexes",
              frames=frames_seen, settled_duration_seconds=duration-SETTLE_SECONDS, **context)
        for part_index, foot in ((2, "left_foot"), (3, "right_foot")):
            transforms = {tuple(round(value, 3) for value in
                                sample["parts"][part_index]["location"] + sample["parts"][part_index]["rotation"])
                          for sample in steady}
            check(len(transforms) >= 4, "walking_foot_transform_changes", foot=foot,
                  distinct_transforms=len(transforms), **context)
        check(all(390.0 <= sample["speed"] <= 400.1 for sample in steady),
              "cardinal_and_diagonal_speed_normalized_to_400", minimum=speed_min, maximum=speed_max, **context)
        check(all(sample["walk_blend"] > 0.85 for sample in steady),
              "walk_blend_settles_while_moving", **context)
        length = magnitude(direction)
        normalized_direction = [value / length for value in direction]
        check(all(sum(sample["velocity"][i] * normalized_direction[i] for i in range(3)) > 390.0
                  for sample in steady), "runtime_movement_direction", **context)
        elapsed = steady[-1]["phase_time"] - steady[0]["phase_time"]
        projected = sum((steady[-1]["position"][i] - steady[0]["position"][i]) * normalized_direction[i]
                        for i in range(3))
        check(abs(projected - 400.0 * elapsed) <= 12.0,
              "runtime_actual_translation", distance_cm=projected, elapsed=elapsed, **context)
        # Compare complete accumulated cycles with actual travelled distance,
        # avoiding any assumption about the initial contact pose after restart.
        phase_distance = 0.0
        sampled_distance = 0.0
        usable_pairs = 0
        for previous, current in zip(steady, steady[1:]):
            dt = current["phase_time"] - previous["phase_time"]
            if not 0 < dt < 0.15:
                continue
            phase_distance += ((current["walk_phase"] - previous["walk_phase"]) % 1.0) * 160.0
            sampled_distance += magnitude([current["position"][i] - previous["position"][i] for i in range(3)])
            usable_pairs += 1
        check(usable_pairs >= 8 and abs(phase_distance - sampled_distance) <= max(20.0, sampled_distance * 0.10),
              "walk_cycle_tracks_actual_distance", phase_distance_cm=phase_distance,
              actual_distance_cm=sampled_distance, pairs=usable_pairs, **context)
        phase_summary["phase_distance_cm"] = phase_distance
        phase_summary["actual_distance_cm"] = sampled_distance
    else:
        check(frames_seen == [0, 1], "idle_plays_original_two_frames", frames=frames_seen, **context)
        check(speed_max < 0.1, "idle_stops_movement", maximum=speed_max, **context)
        check(all(sample["walk_blend"] < 0.15 for sample in steady), "walk_blend_fades_after_stop", **context)
        check(all(abs(sample["visual_location"][2]) < 0.3 for sample in steady),
              "visual_bob_settles_after_stop", **context)
        check(max(sample["walk_phase"] for sample in steady) - min(sample["walk_phase"] for sample in steady) < 0.001,
              "idle_does_not_advance_walk_distance", **context)
    STATE["scene"]["phases"].append(phase_summary)


def inspect_start(controller, pawn):
    scene = STATE["scene"]
    context = {"scene": scene["name"]}
    check(pawn.get_class().get_path_name().startswith(PLAYER_PATH + "."),
          "player0_possesses_saved_pawn", actual=pawn.get_class().get_path_name(), **context)
    view = controller.get_view_target()
    check(isinstance(view, unreal.CameraActor) and
          any(str(tag).startswith("JongguMigration:camera:") for tag in view.tags),
          "original_camera_is_view_target", **context)
    scene["initial_position"] = xyz(pawn.get_actor_location())
    scene["pawn_path"] = pawn.get_path_name()
    scene["view_target"] = asset_path(view)
    world = unreal.EditorLevelLibrary.get_game_world()
    STATE["collision_shapes"] = read_shapes(world, unreal)
    manifest = json.loads((DATA_ROOT / "render_manifest.json").read_text(encoding="utf-8"))
    source = next(s for s in manifest["scenes"] if s["name"] == scene["name"])
    bounds = [g["bounds"] for g in source["groups"] if g.get("bounds") and g["status"] == "visible"]
    STATE["lane_bounds"] = (
        [100 * min(b["min"][0] for b in bounds), 0, 100 * min(b["min"][1] for b in bounds)],
        [100 * max(b["max"][0] for b in bounds), 0, 100 * max(b["max"][1] for b in bounds)])
    STATE["lane_cache"] = {}
    check(clearance(scene["initial_position"], STATE["collision_shapes"]) >= -0.1,
          "saved_spawn_clear_of_actual_obstacles", **context)
    root = pawn.root_component
    movement = pawn.get_component_by_class(unreal.FloatingPawnMovement)
    check(isinstance(root, unreal.SphereComponent), "original_sphere_root_preserved", **context)
    if isinstance(root, unreal.SphereComponent):
        check(abs(float(root.get_unscaled_sphere_radius()) - 24.0) < 0.01,
              "original_root_collision_radius_preserved", **context)
    check(movement is not None and movement.get_editor_property("updated_component") == root,
          "movement_updates_pawn_root", **context)
    visual = pawn.get_editor_property("player_visual")
    check(visual.get_attach_parent() == root, "original_visual_remains_under_root", **context)
    for part_name in ("walk_head", "walk_body", "walk_left_foot", "walk_right_foot"):
        part = pawn.get_editor_property(part_name)
        check(isinstance(part, unreal.PaperSpriteComponent) and part.get_attach_parent() == visual,
              "walk_part_attached_beneath_original_visual", component=part_name, **context)
    if movement is not None:
        for prop, expected in (("max_speed", 400.0), ("acceleration", 3200.0),
                               ("deceleration", 4800.0), ("turning_boost", 12.0)):
            actual = float(movement.get_editor_property(prop))
            check(abs(actual - expected) < 0.01, "movement_configuration_" + prop,
                  expected=expected, actual=actual, **context)
    distance = float(pawn.get_editor_property("walk_cycle_distance"))
    check(abs(distance - 160.0) < 0.01, "walk_cycle_distance_160cm", actual=distance, **context)
    scene["animation_assets"] = {}
    for direction in ("front", "back", "side"):
        for suffix, length in (("_frames", 2), ("_walk_parts", 4)):
            name = direction + suffix
            frames = list(pawn.get_editor_property(name))
            paths = [asset_path(frame) for frame in frames]
            check(len(paths) == length and all(paths) and len(set(paths)) == length,
                  "populated_distinct_animation_frames", variable=name, frames=paths, **context)
            scene["animation_assets"][name] = paths


def reset_moving_phase_to_clear_lane(pawn):
    name, duration, direction, _ = PHASES[STATE["phase"]]
    if not any(direction):
        return
    key = tuple(direction)
    if key not in STATE["lane_cache"]:
        STATE["lane_cache"][key] = find_clear_lane(STATE["scene"]["initial_position"], direction,
            duration * 400.0 + 120.0, STATE["collision_shapes"], STATE["lane_bounds"])
    start, end = STATE["lane_cache"][key]
    pawn.get_component_by_class(unreal.FloatingPawnMovement).stop_movement_immediately()
    pawn.consume_movement_input_vector()
    pawn.set_actor_location(unreal.Vector(*start), False, True)
    STATE["scene"].setdefault("verified_phase_lanes", []).append({
        "phase": name, "start": start, "end": end, "pawn_radius_cm": 24.0})


def tick(delta):
    try:
        if time.monotonic() - STATE["started"] > 240:
            raise RuntimeError("PIE automation timed out in " + STATE["stage"])
        stage = STATE["stage"]
        if stage == "loading":
            name = ("Hub", "Beach")[STATE["scene_index"]]
            if not LEVELS.load_level(CONTENT_ROOT + "/Maps/L_" + name):
                raise RuntimeError("Cannot load " + name)
            STATE["scene"] = {"name": name, "phases": []}
            STATE["stage"] = "starting"
            LEVELS.editor_request_begin_play()
            return
        if stage == "ending":
            if not LEVELS.is_in_play_in_editor():
                STATE["scene_index"] += 1
                if STATE["scene_index"] == 2:
                    finish()
                else:
                    STATE["stage"] = "loading"
            return
        if not LEVELS.is_in_play_in_editor():
            return
        world = unreal.EditorLevelLibrary.get_game_world()
        if world is None:
            return
        controller = unreal.GameplayStatics.get_player_controller(world, 0)
        pawn = unreal.GameplayStatics.get_player_pawn(world, 0) if controller else None
        if pawn is None:
            return
        now = unreal.GameplayStatics.get_time_seconds(world)
        if stage == "starting":
            inspect_start(controller, pawn)
            STATE["phase"] = 0
            STATE["phase_start"] = now
            STATE["samples"] = []
            STATE["last_sample"] = -1.0
            STATE["stage"] = "running"
            reset_moving_phase_to_clear_lane(pawn)
        name, duration, direction, expected_facing = PHASES[STATE["phase"]]
        phase_time = now - STATE["phase_start"]
        if phase_time >= duration:
            summarize_phase()
            STATE["phase"] += 1
            if STATE["phase"] == len(PHASES):
                REPORT["scenes"].append(STATE["scene"])
                write_report()
                STATE["stage"] = "ending"
                LEVELS.editor_request_end_play()
                return
            STATE["phase_start"] = now
            STATE["samples"] = []
            STATE["last_sample"] = -1.0
            name, duration, direction, expected_facing = PHASES[STATE["phase"]]
            phase_time = 0.0
            reset_moving_phase_to_clear_lane(pawn)
        # A Slate callback can repeat without a simulation frame. Add input
        # exactly once per new game time to avoid skewing acceleration.
        if now <= STATE["last_sample"]:
            return
        STATE["last_sample"] = now
        if any(direction):
            length = magnitude(direction)
            pawn.add_movement_input(unreal.Vector(*(value / length for value in direction)), 1.0, False)
        visual = pawn.get_editor_property("player_visual")
        velocity = xyz(pawn.get_velocity())
        facing = int(pawn.get_editor_property("facing_direction"))
        frame = int(pawn.get_editor_property("current_frame_index"))
        moving = bool(pawn.get_editor_property("is_moving"))
        direction_name = "front" if facing == 0 else "back" if facing == 1 else "side"
        frames = pawn.get_editor_property(direction_name + "_frames")
        expected_sprite = frames[frame] if not moving and 0 <= frame < len(frames) else None
        walk_parts = pawn.get_editor_property(direction_name + "_walk_parts")
        parts = []
        for index, part_name in enumerate(("walk_head", "walk_body", "walk_left_foot", "walk_right_foot")):
            part = pawn.get_editor_property(part_name)
            rotation = part.get_editor_property("relative_rotation")
            parts.append({
                "name": part_name,
                "sprite": asset_path(part.get_sprite()),
                "expected_sprite": asset_path(walk_parts[index]) if index < len(walk_parts) else None,
                "visible": bool(part.is_visible()) and not bool(part.get_editor_property("hidden_in_game")),
                "location": xyz(part.get_editor_property("relative_location")),
                "rotation": [float(rotation.pitch), float(rotation.yaw), float(rotation.roll)],
                "sort_priority": int(part.get_editor_property("translucency_sort_priority")),
            })
        side_faces_left = bool(pawn.get_editor_property("side_sprite_faces_left"))
        STATE["samples"].append({
            "phase_time": phase_time,
            "position": xyz(pawn.get_actor_location()),
            "velocity": velocity,
            "speed": magnitude(velocity),
            "moving": moving,
            "facing": facing,
            "frame": frame,
            "walk_phase": float(pawn.get_editor_property("walk_phase")),
            "walk_blend": float(pawn.get_editor_property("walk_blend")),
            "sprite": asset_path(visual.get_sprite()),
            "expected_sprite": asset_path(expected_sprite),
            "parts": parts,
            "expected_flip": (facing == 3 and side_faces_left) or (facing == 2 and not side_faces_left),
            "scale": xyz(visual.get_editor_property("relative_scale3d")),
            "visual_location": xyz(visual.get_editor_property("relative_location")),
            "sort_priority": int(visual.get_editor_property("translucency_sort_priority")),
            "view_target": asset_path(controller.get_view_target()),
            "pawn_path": pawn.get_path_name(),
        })
        check(clearance(xyz(pawn.get_actor_location()), STATE["collision_shapes"]) >= -0.5,
              "walk_phase_never_penetrates_saved_obstacles", scene=STATE["scene"]["name"], phase=name)
    except Exception:
        REPORT["failures"].append({"exception": traceback.format_exc(), "stage": STATE["stage"],
                                   "phase_index": STATE["phase"]})
        if "scene" in STATE:
            REPORT["incomplete_scene"] = STATE["scene"]
            REPORT["incomplete_phase_samples"] = STATE["samples"]
        finish()


def main():
    global HANDLE
    require_qa_launch(LAUNCH_MARKER, SCRIPT_PATH)
    active = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active != PROJECT_ROOT or not (PROJECT_ROOT / "projectJ.uproject").is_file():
        raise RuntimeError("Expected project " + str(PROJECT_ROOT) + "; active project is " + str(active))
    if LEVELS.is_in_play_in_editor():
        raise RuntimeError("Refusing to take over an existing PIE session")
    STATE["authorized_process"] = True
    unreal.EditorPythonScripting.set_keep_python_script_alive(True)
    write_report()
    HANDLE = unreal.register_slate_post_tick_callback(tick)
    unreal.log("JONGGU_WALK_PIE_AUTOMATION_READY")


if __name__ == "__main__":
    main()
