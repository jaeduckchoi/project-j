"""Exercise saved colliders in a NEW, explicitly marked rendered UE editor.

UnrealEditor.exe projectJ.uproject -JongguCollisionQAAutomation
  -ExecutePythonScript=<absolute path to this file> -unattended -nosplash

Uses the actual saved player's AddMovementInput, never a stand-in simulation.
Only the marked process may enter/end PIE or quit. No asset/map is saved.
"""
from __future__ import annotations

import json
import math
from pathlib import Path
import sys
import time
import traceback

import unreal

SCRIPT_PATH = Path(__file__).resolve()
sys.path.insert(0, str(SCRIPT_PATH.parent))
from collision_qa_geometry import (RADIUS, add, clearance, clear_segment, dot,
                                   exposed_face, find_clear_lane, length,
                                   normalized, read_shapes, sub, xyz)

TOOLS = SCRIPT_PATH.parent.parent
sys.path.insert(0, str(TOOLS))
from world_paths import CONTENT_ROOT, DATA_ROOT, PROJECT_ROOT, QA_ROOT, require_qa_launch

REPORT_PATH = QA_ROOT / "collision_pie_report.json"
CAPTURE_ROOT = QA_ROOT / "Collision"
LAUNCH_MARKER = "-JongguCollisionQAAutomation"
LEVELS = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
ACTORS = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
REPORT = {"schema_version": 1, "success": False, "checks": 0, "failures": [],
          "runtime": "Actual saved Pawn in Unreal PIE", "physical_keyboard_tested": False,
          "input_method": "Pawn.AddMovementInput", "scenes": [], "captures": []}
STATE = {"scene_index": 0, "stage": "loading", "started": time.monotonic(),
         "authorized": False, "finished": False, "last_time": -1.0}
HANDLE = None
MANIFEST = json.loads((DATA_ROOT / "render_manifest.json").read_text(encoding="utf-8"))

# Semantic waypoints are manually reviewed against the source artwork, not
# derived from the collision writer. Populated during authoring and checked
# against actual shapes before driving the Pawn through the complete route.
SEMANTIC_ROUTES = {
    "Hub": [
        {"name": "table_gap_and_kitchen_left_passage", "points": [
            [-715, 0, -415], [-400, 0, -415], [-400, 0, 100],
            [-1410, 0, 100], [-1410, 0, 450], [-700, 0, 450]]},
    ],
    "Beach": [
        {"name": "sand_to_connected_dock_and_boat_approach", "points": [
            [1750, 0, 1200], [1750, 0, 500], [-500, 0, 500],
            [-500, 0, 1700], [-2200, 0, 1700]]},
        {"name": "dock_lighthouse_approach", "points": [[-2500, 0, 500], [-2500, 0, 700]]},
        {"name": "walk_beneath_visual_tree_canopy", "points": [[790, 0, 1400], [885, 0, 1400]]},
    ],
}
SEMANTIC_PROBES = {
    "Hub": [
        ("table_gap", [-400, 0, -260], False),
        ("exit_approach", [-1300, 0, -690], False),
        ("outside_hub_floor", [1620, 0, 0], True),
    ],
    "Beach": [
        ("visible_sand", [1750, 0, 500], False),
        ("deep_water", [0, 0, 0], True),
        ("lighthouse_base", [-2500, 0, 925], True),
        ("tree_trunk", [833, 0, 1128], True),
        ("visual_canopy_above_trunk", [833, 0, 1400], False),
        ("outside_beach_floor", [3220, 0, 1200], True),
    ],
}


def check(ok, label, **context):
    REPORT["checks"] += 1
    if not ok:
        REPORT["failures"].append({"check": label, **context})


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
    unreal.log("JONGGU_COLLISION_PIE_RESULT " + json.dumps({
        "success": REPORT["success"], "checks": REPORT["checks"],
        "failures": len(REPORT["failures"]), "report": str(REPORT_PATH)}))
    if STATE["authorized"]:
        if LEVELS.is_in_play_in_editor():
            LEVELS.editor_request_end_play()
        unreal.EditorPythonScripting.set_keep_python_script_alive(False)
        unreal.SystemLibrary.quit_editor()


def source_bounds(name):
    scene = next(s for s in MANIFEST["scenes"] if s["name"] == name)
    bounds = [g["bounds"] for g in scene["groups"] if g.get("bounds") and g["status"] == "visible"]
    return ([100 * min(b["min"][0] for b in bounds), 0.0, 100 * min(b["min"][1] for b in bounds)],
            [100 * max(b["max"][0] for b in bounds), 0.0, 100 * max(b["max"][1] for b in bounds)])


def kind(shape):
    tag = next((t.split(":", 1)[1] for t in shape["tags"] if t.startswith("JongguCollisionKind:")), None)
    if tag:
        return tag
    if "boundary" in shape["id"].lower():
        return "boundary"
    if "terrain" in shape["id"].lower() or "water" in shape["id"].lower():
        return "terrain"
    return "object"


def capture_collision_view(name):
    """Actual editor SceneCapture render with world debug geometry; never save."""
    world = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()
    shapes = read_shapes(world, unreal)
    source = next(a for a in ACTORS.get_all_level_actors() if isinstance(a, unreal.CameraActor)
                  and any(str(t).startswith("JongguMigration:camera:") for t in a.tags))
    CAPTURE_ROOT.mkdir(parents=True, exist_ok=True)
    for shape in shapes:
        color = unreal.LinearColor(0.1, 1.0, 0.2, 1.0) if kind(shape) == "object" else unreal.LinearColor(1.0, 0.2, 0.05, 1.0)
        if shape["kind"] == "box":
            unreal.SystemLibrary.draw_debug_box(world, unreal.Vector(*shape["center"]),
                unreal.Vector(*shape["extents"]), color,
                unreal.Rotator(pitch=shape["rotation"][0], yaw=shape["rotation"][1], roll=shape["rotation"][2]), 180.0, 3.0)
        else:
            unreal.SystemLibrary.draw_debug_sphere(world, unreal.Vector(*shape["center"]),
                shape["radius"], 24, color, 180.0, 3.0)
    for overview in ([False, True] if name == "Beach" else [False]):
        location = source.get_actor_location()
        width = float(source.camera_component.get_editor_property("ortho_width"))
        if overview:
            lo, hi = source_bounds(name)
            location = unreal.Vector((lo[0] + hi[0]) / 2, location.y, (lo[2] + hi[2]) / 2)
            width = max(hi[0] - lo[0], (hi[2] - lo[2]) * 16 / 9) + 300
        cap = ACTORS.spawn_actor_from_class(unreal.SceneCapture2D, location,
            source.get_actor_rotation(), transient=True)
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
        for flag_name, enabled in (("AntiAliasing", False), ("TemporalAA", False),
                                   ("Bloom", False), ("MotionBlur", False), ("CompositeEditorPrimitives", True)):
            flag = unreal.EngineShowFlagsSetting()
            flag.show_flag_name, flag.enabled = flag_name, enabled
            flags.append(flag)
        comp.set_editor_property("show_flag_settings", flags)
        target = unreal.RenderingLibrary.create_render_target2d(world, 1920, 1080,
            unreal.TextureRenderTargetFormat.RTF_RGBA8, unreal.LinearColor(0, 0, 0, 1), False)
        target.set_editor_property("target_gamma", 2.2)
        comp.set_editor_property("texture_target", target)
        unreal.SystemLibrary.execute_console_command(world, "Editor.AsyncAssetCompilationFinishAll")
        for _ in range(3):
            comp.capture_scene()
        filename = name + ("_Collision_Overview.png" if overview else "_Collision.png")
        unreal.RenderingLibrary.export_render_target(world, target, str(CAPTURE_ROOT), filename)
        ACTORS.destroy_actor(cap)
        check((CAPTURE_ROOT / filename).is_file(), "actual_editor_collision_capture", scene=name, file=filename)
        REPORT["captures"].append({"scene": name, "file": str(CAPTURE_ROOT / filename),
                                  "source": "Actual Unreal SceneCapture2D with DrawDebugBox/Sphere",
                                  "overview": overview, "ortho_width": width,
                                  "source_camera_modified": False})


def inspect_start(world, pawn, controller):
    scene = STATE["scene"]
    context = {"scene": scene["name"]}
    root = pawn.root_component
    movement = pawn.get_component_by_class(unreal.FloatingPawnMovement)
    check(isinstance(root, unreal.SphereComponent), "actual_saved_player_sphere_root", **context)
    check(movement is not None and movement.get_editor_property("updated_component") == root,
          "floating_movement_sweeps_actual_root", **context)
    if not isinstance(root, unreal.SphereComponent) or movement is None:
        raise RuntimeError("Saved player collision/movement contract is missing")
    check(abs(root.get_scaled_sphere_radius() - RADIUS) < 0.01, "actual_player_world_radius_24", **context)
    check(abs(float(movement.get_editor_property("max_speed")) - 400) < 0.01,
          "actual_player_maximum_speed_400", **context)
    check(movement.get_editor_property("constrain_to_plane"), "actual_player_plane_constraint", **context)
    scene["spawn"] = xyz(pawn.get_actor_location())
    scene["pawn_path"] = pawn.get_path_name()
    scene["view_target"] = controller.get_view_target().get_path_name()
    shapes = read_shapes(world, unreal)
    STATE["shapes"] = shapes
    scene["actual_shapes"] = shapes
    check(bool(shapes), "saved_colliders_exist", **context)
    check(len(shapes) == len({s["id"] for s in shapes}), "unique_runtime_collision_ids", **context)
    for shape in shapes:
        check(shape["profile"] == "JongguObstacle", "actual_obstacle_profile", collider=shape["id"], **context)
        check("QUERY_ONLY" in shape["enabled"], "actual_obstacle_query_only", collider=shape["id"], **context)
        check("BLOCK" in shape["pawn_response"], "actual_obstacle_blocks_pawn", collider=shape["id"], **context)
        check(abs(shape["center"][1] - scene["spawn"][1]) < 0.01,
              "obstacle_aligned_to_player_y_plane", collider=shape["id"], **context)
    spawn_clearance = clearance(scene["spawn"], shapes)
    scene["spawn_clearance_cm"] = spawn_clearance
    check(spawn_clearance >= 0.0, "original_saved_spawn_is_clear", clearance_cm=spawn_clearance, **context)
    scene["semantic_probes"] = []
    for label, point, expected_blocked in SEMANTIC_PROBES[scene["name"]]:
        gap = clearance(point, shapes)
        check((gap < 0) == expected_blocked, "independent_semantic_point_" + label,
              point=point, expected_blocked=expected_blocked, clearance_cm=gap, **context)
        scene["semantic_probes"].append({"name": label, "point": point,
                                         "expected_blocked": expected_blocked, "clearance_cm": gap})
    for actor in unreal.GameplayStatics.get_all_actors_of_class(world, unreal.Actor):
        if any(str(t).startswith("JongguMigration:render:") for t in actor.tags):
            for cls in (unreal.PaperSpriteComponent, unreal.PaperGroupedSpriteComponent):
                component = actor.get_component_by_class(cls)
                if component:
                    check(component.get_collision_enabled() == unreal.CollisionEnabled.NO_COLLISION,
                          "render_art_does_not_add_hidden_collision", actor=actor.get_path_name(), **context)


def build_cases():
    scene, shapes = STATE["scene"], STATE["shapes"]
    name, origin = scene["name"], scene["spawn"]
    bounds = source_bounds(name)
    cases = []
    directions = [("east", (1, 0, 0)), ("west", (-1, 0, 0)), ("north", (0, 0, 1)),
                  ("south", (0, 0, -1)), ("northeast", (1, 0, 1)),
                  ("northwest", (-1, 0, 1)), ("southeast", (1, 0, -1)), ("southwest", (-1, 0, -1))]
    for label, direction in directions:
        start, end = find_clear_lane(origin, direction, 500.0, shapes, bounds)
        cases.append({"name": "free_speed_" + label, "mode": "free", "start": start,
                      "direction": normalized(direction), "duration": 1.05, "lane_end": end})
    # Cover every approach axis using different saved obstacles where possible.
    used = set()
    objects = [s for s in shapes if kind(s) == "object"]
    for label, normal in directions[:4]:
        selected = None
        for shape in sorted(objects, key=lambda s: (s["id"] in used, length(sub(s["center"], origin)))):
            face = exposed_face(shape, shapes, normal)
            if face:
                selected = face
                break
        check(selected is not None, "available_real_object_approach", scene=name, direction=label)
        if selected:
            used.add(selected["shape_id"])
            cases.append(dict(selected, name="object_block_" + label, mode="block", duration=0.9))
    for category in ("boundary", "terrain"):
        if category == "terrain" and name == "Hub":
            continue
        selected = None
        for shape in [s for s in shapes if kind(s) == category]:
            for _, normal in directions[:4]:
                face = exposed_face(shape, shapes, normal, approach=100.0)
                if face:
                    selected = face
                    break
            if selected:
                break
        check(selected is not None, "available_" + category + "_approach", scene=name)
        if selected:
            cases.append(dict(selected, name=category + "_blocks_player", mode="block", duration=0.9))
    # A tangent slide into a sufficiently long real box face, and a diagonal
    # approach toward a box corner. Start points must already be clear.
    for mode in ("slide", "corner"):
        selected = None
        # Beach's accessible props are round trunks; its large boxes are boats
        # surrounded by blocked water. Use an actual shoreline edge/corner
        # there when no object box has a physically reachable test trajectory.
        geometric_targets = sorted(objects, key=lambda s: s["id"]) + [s for s in shapes if kind(s) == "terrain"]
        for shape in geometric_targets:
            if shape["kind"] != "box":
                continue
            for axis in (0, 2):
                tangent_axis = 2 if axis == 0 else 0
                if mode == "slide" and shape["extents"][tangent_axis] < 90:
                    continue
                for sign in (-1.0, 1.0):
                    normal = [sign * n for n in shape["axes"][axis]]
                    tangent = shape["axes"][tangent_axis]
                    face = exposed_face(shape, shapes, normal, approach=40.0)
                    if not face:
                        continue
                    if mode == "slide":
                        face["direction"] = normalized(add(face["direction"], tangent))
                        face["tangent"] = tangent
                        # Keep the intended slide beside the face until the end.
                        if clear_segment(face["start"], add(face["start"], tangent, 110), shapes, margin=1):
                            selected = dict(face, mode=mode, name="tangent_slide", duration=0.55)
                    else:
                        corner = add(add(shape["center"], normal, shape["extents"][axis]),
                                     tangent, shape["extents"][tangent_axis])
                        outward = normalized(add(normal, tangent))
                        start = add(corner, outward, RADIUS + 100)
                        near = add(corner, outward, RADIUS + 3)
                        if clear_segment(start, near, shapes, margin=1):
                            selected = {"name": "diagonal_corner_block", "mode": "corner", "start": start,
                                        "direction": [-v for v in outward], "normal": outward,
                                        "contact": add(corner, outward, RADIUS), "shape_id": shape["id"], "duration": 0.9}
                    if selected:
                        break
                if selected:
                    break
            if selected:
                break
        check(selected is not None, "available_real_" + mode + "_trajectory", scene=name)
        if selected:
            cases.append(selected)
    routes = SEMANTIC_ROUTES[name]
    check(bool(routes), "independent_semantic_routes_authored", scene=name)
    for route in routes:
        points = route["points"]
        check(all(clear_segment(a, b, shapes, margin=0.5) for a, b in zip(points, points[1:])),
              "semantic_route_is_open_for_player_radius", scene=name, route=route["name"])
        cases.append({"name": route["name"], "mode": "route", "start": points[0],
                      "points": points[1:], "duration": sum(length(sub(b, a)) for a, b in zip(points, points[1:])) / 180 + 5})
    # User regression: horizontal travel, then UP alone at a blocking face or
    # rounded collision corner. Facing must follow UP even when real velocity
    # becomes zero or slides sideways. Releasing input must retain that facing.
    for side in (-1, 1):
        for situation in ("blocked", "corner"):
            chosen = None
            candidates = sorted(objects, key=lambda s: s["id"]) + [s for s in shapes if kind(s) == "terrain"]
            for shape in candidates:
                if shape["kind"] != "box" or shape["extents"][0] < 100:
                    continue
                if abs(abs(shape["axes"][0][0]) - 1.0) > 0.001:
                    continue
                low_z = shape["center"][2] - shape["extents"][2]
                if situation == "blocked":
                    end_x = shape["center"][0]
                    start = [end_x - side * 47, origin[1], low_z - RADIUS - 30]
                else:
                    # Native turning inertia carries the Pawn about 25 cm
                    # farther sideways after input switches. Begin just inside
                    # the corner so UP genuinely hits its rounded sphere edge.
                    end_x = shape["center"][0] + side * (shape["extents"][0] - 8)
                    start = [end_x - side * 47, origin[1], low_z - RADIUS - 30]
                horizontal_end = [end_x, origin[1], start[2]]
                approach_end = [end_x, origin[1], low_z - RADIUS - 3]
                if clear_segment(start, horizontal_end, shapes, margin=1) and clear_segment(horizontal_end, approach_end, shapes, margin=1):
                    chosen = {"name": ("left" if side < 0 else "right") + "_then_up_only_" + situation,
                              "mode": "intent", "situation": situation, "start": start,
                              "first_direction": [side, 0, 0], "switch_at": 0.18, "release_at": 1.0,
                              "duration": 1.45, "shape_id": shape["id"]}
                    break
            check(chosen is not None, "available_input_facing_regression_trajectory", scene=name, side=side, situation=situation)
            if chosen:
                cases.append(chosen)
    return cases


def start_case(pawn, now):
    case = STATE["cases"][STATE["case_index"]]
    movement = pawn.get_component_by_class(unreal.FloatingPawnMovement)
    movement.stop_movement_immediately()
    pawn.consume_movement_input_vector()
    pawn.set_actor_location(unreal.Vector(*case["start"]), False, True)
    check(clearance(case["start"], STATE["shapes"]) >= -0.1,
          "case_start_has_no_penetration", scene=STATE["scene"]["name"], case=case["name"])
    STATE.update(case_start=now, samples=[], route_index=0, last_time=-1.0)


def summarize_case(completed):
    case = STATE["cases"][STATE["case_index"]]
    samples = STATE["samples"]
    context = {"scene": STATE["scene"]["name"], "case": case["name"]}
    check(len(samples) >= 3, "actual_runtime_case_sampled", count=len(samples), **context)
    if not samples:
        return
    worst = min(s["clearance"] for s in samples)
    check(worst >= -0.5, "actual_player_never_penetrates_obstacles", worst_clearance_cm=worst, **context)
    check(all(abs(s["position"][1] - case["start"][1]) < 0.01 for s in samples),
          "actual_player_remains_in_y_plane", **context)
    check(all(s["speed"] <= 400.2 for s in samples), "movement_never_exceeds_maximum_speed", **context)
    steady = [s for s in samples if s["time"] > 0.3]
    if case["mode"] == "free":
        check(steady and all(390 <= s["speed"] <= 400.2 for s in steady),
              "cardinal_diagonal_free_speed_400", speed_range=[min(s["speed"] for s in steady),
              max(s["speed"] for s in steady)] if steady else [], **context)
        if len(steady) > 1:
            elapsed = steady[-1]["time"] - steady[0]["time"]
            actual = dot(sub(steady[-1]["position"], steady[0]["position"]), case["direction"])
            check(abs(actual - 400 * elapsed) <= 15, "actual_free_displacement_matches_speed", cm=actual, **context)
    elif case["mode"] in ("block", "corner"):
        displacement = dot(sub(samples[-1]["position"], case["start"]), case["direction"])
        target = next(s for s in STATE["shapes"] if s["id"] == case["shape_id"])
        from collision_qa_geometry import surface_distance
        gap = surface_distance(samples[-1]["position"], target) - RADIUS
        check(20 <= displacement < 125, "player_reaches_obstacle_and_is_blocked", movement_cm=displacement, **context)
        check(-0.5 <= gap <= 3.0, "player_stops_at_actual_footprint_surface", gap_cm=gap, **context)
        check(samples[-1]["speed"] < 1.0, "sustained_input_cannot_pass_obstacle", **context)
    elif case["mode"] == "slide":
        tangent = dot(sub(samples[-1]["position"], case["start"]), case["tangent"])
        check(tangent > 60, "diagonal_input_slides_along_real_obstacle", tangential_cm=tangent, **context)
        from collision_qa_geometry import surface_distance
        target = next(s for s in STATE["shapes"] if s["id"] == case["shape_id"])
        minimum_gap = min(surface_distance(s["position"], target) - RADIUS for s in samples)
        check(-0.5 <= minimum_gap <= 3, "sliding_actually_contacts_the_obstacle", gap_cm=minimum_gap, **context)
    elif case["mode"] == "route":
        check(completed, "actual_player_completes_semantic_route", reached=STATE["route_index"], **context)
    elif case["mode"] == "intent":
        horizontal = [s for s in samples if 0.08 < s["time"] < case["switch_at"]]
        upward = [s for s in samples if case["switch_at"] + 0.08 < s["time"] < case["release_at"]]
        released = [s for s in samples if s["time"] > case["release_at"] + 0.2]
        expected_side = 2 if case["first_direction"][0] < 0 else 3
        check(horizontal and all(s["facing"] == expected_side for s in horizontal),
              "horizontal_intent_sets_expected_initial_facing", **context)
        check(upward and all(s["facing"] == 1 for s in upward),
              "up_only_intent_faces_back_despite_collision_velocity", **context)
        check(released and all(s["facing"] == 1 for s in released),
              "releasing_input_preserves_last_requested_facing", **context)
        if case["situation"] == "blocked":
            blocked = [s for s in upward if s["time"] > 0.7]
            check(blocked and all(s["speed"] < 1 and not s["moving"] for s in blocked),
                  "blocked_up_input_turns_idle_player_without_fake_walking", **context)
        else:
            tangent = [s for s in upward if s["clearance"] <= 3 and abs(s["velocity"][0]) > 1]
            check(bool(tangent), "up_only_corner_case_exercises_sideways_collision_slide", **context)
    STATE["scene"]["cases"].append({"case": case, "samples": samples})


def tick(delta):
    try:
        if time.monotonic() - STATE["started"] > 420:
            raise RuntimeError("Collision PIE timed out in " + STATE["stage"])
        stage = STATE["stage"]
        if stage == "loading":
            name = ("Hub", "Beach")[STATE["scene_index"]]
            if not LEVELS.load_level(CONTENT_ROOT + "/Maps/L_" + name):
                raise RuntimeError("Cannot load saved map " + name)
            STATE["scene"] = {"name": name, "cases": []}
            capture_collision_view(name)
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
        controller = unreal.GameplayStatics.get_player_controller(world, 0) if world else None
        pawn = unreal.GameplayStatics.get_player_pawn(world, 0) if controller else None
        if pawn is None:
            return
        now = unreal.GameplayStatics.get_time_seconds(world)
        if stage == "starting":
            inspect_start(world, pawn, controller)
            STATE["cases"] = build_cases()
            STATE["case_index"] = 0
            start_case(pawn, now)
            STATE["stage"] = "running"
        if now <= STATE["last_time"]:
            return
        STATE["last_time"] = now
        case = STATE["cases"][STATE["case_index"]]
        elapsed = now - STATE["case_start"]
        position, velocity = xyz(pawn.get_actor_location()), xyz(pawn.get_velocity())
        STATE["samples"].append({"time": elapsed, "position": position, "velocity": velocity,
                                 "speed": length(velocity), "clearance": clearance(position, STATE["shapes"]),
                                 "facing": int(pawn.get_editor_property("facing_direction")),
                                 "moving": bool(pawn.get_editor_property("is_moving"))})
        complete = False
        direction = case.get("direction", [0, 0, 0])
        if case["mode"] == "intent":
            direction = case["first_direction"] if elapsed < case["switch_at"] else [0, 0, 1] if elapsed < case["release_at"] else [0, 0, 0]
        if case["mode"] == "route":
            destination = case["points"][STATE["route_index"]]
            if length(sub(destination, position)) <= 16:
                STATE["route_index"] += 1
                complete = STATE["route_index"] == len(case["points"])
                if not complete:
                    destination = case["points"][STATE["route_index"]]
            if not complete:
                direction = normalized(sub(destination, position))
        if complete or elapsed >= case["duration"]:
            summarize_case(complete)
            STATE["case_index"] += 1
            if STATE["case_index"] == len(STATE["cases"]):
                REPORT["scenes"].append(STATE["scene"])
                write_report()
                STATE["stage"] = "ending"
                LEVELS.editor_request_end_play()
                return
            start_case(pawn, now)
            return
        pawn.add_movement_input(unreal.Vector(*direction), 1.0, False)
        check(pawn.get_path_name() == STATE["scene"]["pawn_path"], "same_saved_pawn_throughout_runtime")
        check(controller.get_view_target().get_path_name() == STATE["scene"]["view_target"], "saved_source_camera_preserved")
    except Exception:
        REPORT["failures"].append({"exception": traceback.format_exc(), "stage": STATE["stage"]})
        REPORT["incomplete_scene"] = STATE.get("scene")
        REPORT["incomplete_samples"] = STATE.get("samples")
        finish()


def main():
    global HANDLE
    require_qa_launch(LAUNCH_MARKER, SCRIPT_PATH)
    active = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active != PROJECT_ROOT or not (PROJECT_ROOT / "projectJ.uproject").is_file():
        raise RuntimeError("Unexpected project for collision QA: " + str(active))
    if LEVELS.is_in_play_in_editor():
        raise RuntimeError("Refusing to take over an existing PIE session")
    STATE["authorized"] = True
    unreal.EditorPythonScripting.set_keep_python_script_alive(True)
    write_report()
    HANDLE = unreal.register_slate_post_tick_callback(tick)
    unreal.log("JONGGU_COLLISION_PIE_AUTOMATION_READY")


if __name__ == "__main__":
    main()
