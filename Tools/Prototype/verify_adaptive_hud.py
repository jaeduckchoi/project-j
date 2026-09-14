"""Adaptive HUD evidence from compiled Blueprint functions and real DrawHUD frames.

This file is editor QA only. It never overrides production projected bounds,
occlusion decisions, alpha values, anchors, or rendered pixels. Player poses are
temporary real Pawn teleports and are restored by the shared fixture.
"""
from __future__ import annotations

from itertools import combinations
import time

from hud_layout import GROUPS, GROUP_BITS, resolve_rect, group_visible
from build_hud_visibility import VISIBILITY_FIELDS
from verify_main_hud import main_hud_metadata, json_presentation
from verify_popups import button_center


ADAPTIVE_CASES = [
    "00_all_compact", "01_all_expanded", "02_mixed", "03_both_cooking",
    "04_pan_ready_pot_cooking", "05_special_two_ready", "06_special_one_left",
    "07_stations_empty", "08_head_overlap", "09_body_overlap", "10_feet_overlap",
    "11_hysteresis_25px", "12_hysteresis_clear_40px", "13_inventory_overlap",
    "14_inventory_hover", "15_inventory_faded_click", "16_pause_freeze", "17_beach",
]


# Additional evidence is separate from the completed 54-image main matrix.
COVERAGE_GROUPS = tuple(group for group in GROUPS if group not in ("day_phase", "inventory"))
COVERAGE_CASES = ["00_all_compact_both_cooking"] + [
    f"{index * 2 + offset:02d}_{group}_{phase}"
    for index, group in enumerate(COVERAGE_GROUPS)
    for offset, phase in ((1, "occluded"), (2, "clear"))
]


def stage_coverage_hud(fixture, case):
    """Cover each remaining group's actual fade and restore in a live Hub view."""
    fixture.adaptive_samples = []
    fixture.adaptive_started = time.monotonic()
    fixture.adaptive_expected = {}
    fixture.adaptive_pose = None
    fixture.call("UpdatePointer", -1.0, -1.0, False)
    if not getattr(fixture, "adaptive_coverage_initialized", False):
        fixture.reset(menu=3, clams=2)
        fixture.call("OpenService")
        fixture.call("Advance", 20.0)
        set_mask(fixture, 0)
        fixture.call("StartCook", 0)
        fixture.call("StartCook", 3)
        fixture.call("Advance", 4.0)
        fixture.call("FindInteraction")
        fixture.call("RefreshHUD")
        fixture.adaptive_coverage_initialized = True
    if case != "00_all_compact_both_cooking":
        if not getattr(fixture, "adaptive_coverage_notice", False):
            # Keep the initial showcase clean. Later, the real rejected duplicate
            # cook supplies prompt text without any synthetic HUD assignment.
            fixture.call("StartCook", 3)
            require(bool(fixture.m("notice_text")), "a real busy-pot action supplies the prompt notice")
            fixture.call("RefreshHUD")
            fixture.adaptive_coverage_notice = True
        group, phase = case[3:].rsplit("_", 1)
        require(group in COVERAGE_GROUPS, "coverage case targets an approved remaining group")
        values = state(fixture)
        require(group_visible(group, **values), group + " is visible before its coverage pose")
        x, y, w, h = resolve_rect(group, **values)
        target = (x + w / 2, y + h / 2) if phase == "occluded" else (640.0, 320.0)
        pose_projected(fixture, "body", target)
        fixture.adaptive_expected = {"group": group, "occluded": phase == "occluded",
                                      "alpha": 1.0 if phase == "occluded" else 0.0}
    fixture.call("RefreshHUD")
    return 0.5


def coverage_metadata(fixture, case):
    data = adaptive_metadata(fixture, case)
    values = state(fixture)
    checks = data["adaptive_checks"]
    checks.append(require(fixture.s("hud_expanded_mask") == 0, "coverage uses compact HUD"))
    checks.append(require(fixture.m("pan_remaining") == 4.0 and fixture.m("pot_remaining") == 12.0,
                          "both real cooking timers stay at the staged progress"))
    checks.append(require(values["pan_bubble_visible"] and values["pot_bubble_visible"],
                          "both actual projected cooking bubbles stay visible"))
    if case == "00_all_compact_both_cooking":
        checks.append(require(not fixture.m("intake_paused") and not fixture.m("notice_text")
                              and not fixture.m("interaction_hint"),
                              "showcase keeps normal intake and clear notice/prompt at the default spawn"))
    else:
        checks.append(require(bool(fixture.m("notice_text")) and group_visible("prompt", **values),
                              "prompt remains visible from a real gameplay notice"))
    group = fixture.adaptive_expected.get("group")
    if group:
        checks.append(require(group_visible(group, **values), group + " stays visible during fade/restore"))
    data["coverage_scope"] = "Additional actual fade/restore coverage for the eight groups absent from the main matrix"
    return data


def state(fixture):
    result = fixture.presentation()
    result.update({name: fixture.h(name) for name, _, _ in VISIBILITY_FIELDS})
    return result


def require(passed, label):
    if not passed:
        raise AssertionError("Adaptive HUD QA: " + label)
    return label


def set_mask(fixture, target):
    """Use actual pointer Click hit testing at the currently resolved geometry."""
    for bit, action in ((1, 31), (2, 32), (4, 33)):
        if (fixture.s("hud_expanded_mask") ^ target) & bit:
            fixture.click(action)
    fixture.call("RefreshHUD")
    require(fixture.s("hud_expanded_mask") == target and fixture.h("hud_expanded_mask") == target,
            "dynamic button clicks resolve to fold mask " + str(target))


def validate_adaptive_runtime(fixture):
    """Synchronous behavior checks; geometry/fade results use later real frames."""
    checks = []
    fixture.reset(menu=3, clams=2)
    fixture.put(fixture.session, "hud_expanded_mask", 0)
    fixture.call("OpenService")
    before = fixture.game_snapshot()
    for mask in (1, 3, 7, 5, 0):
        set_mask(fixture, mask)
        checks.append(require(fixture.game_snapshot() == before,
                              "fold mask " + str(mask) + " changes no gameplay state"))
    set_mask(fixture, 7)
    # Expanded group contents are display-only; only its 40px header toggles.
    for group in GROUP_BITS:
        x, y, w, h = resolve_rect(group, **fixture.presentation())
        fixture.call("Click", x + w / 2.0, y + h - 8.0)
        checks.append(require(fixture.s("hud_expanded_mask") == 7,
                              "expanded " + group + " body is not an invisible toggle"))
    fixture.session.call_method("SaveCheckpoint")
    require(fixture.s("save_ok"), "isolated checkpoint save succeeds")
    fixture.put(fixture.session, "clams", 99)
    fixture.put(fixture.session, "total_revenue", 999)
    set_mask(fixture, 2)
    fixture.session.call_method("LoadCheckpoint")
    checks.append(require(fixture.s("save_ok") and fixture.s("clams") == 2
                          and fixture.s("total_revenue") == 0
                          and fixture.s("hud_expanded_mask") == 2,
                          "checkpoint restores stock and income while retaining session folds"))
    fixture.call("RefreshHUD")
    checks.append(require(fixture.h("hud_expanded_mask") == 2,
                          "checkpoint reload presents current folds"))
    fixture.call("StartCook", 1)
    fixture.call("Advance", 4.0)
    fixture.click(30)
    before = fixture.game_snapshot()
    fixture.call("Advance", 10.0)
    checks.append(require(fixture.game_snapshot() == before
                          and fixture.unreal.GameplayStatics.is_game_paused(fixture.manager),
                          "full pause freezes explicit gameplay advance"))
    fixture.click(303)
    checks.append(require(fixture.s("hud_expanded_mask") == 2 and not fixture.pc.is_move_input_ignored(),
                          "pause close restores movement and fold selection"))
    fixture.reset()
    return {"success": True, "checks": checks, "check_count": len(checks),
            "evidence": "Actual compiled Blueprint Click, SaveCheckpoint, LoadCheckpoint, Advance and PauseToggle",
            "real_map_retry_next_day_and_new_pie_checks": "verify_pie.py adaptive_checks"}


def bounds(fixture):
    require(fixture.h("player_bounds_valid"), "actual player projected bounds are valid")
    return tuple(float(fixture.h("player_bounds_" + side)) for side in ("left", "top", "right", "bottom"))


def _xy(value):
    if hasattr(value, "x") and hasattr(value, "y"):
        return float(value.x), float(value.y)
    if isinstance(value, (tuple, list)):
        for item in value:
            if hasattr(item, "x") and hasattr(item, "y"):
                return float(item.x), float(item.y)
    raise AssertionError("Native world projection failed: " + str(value))


def pose_projected(fixture, part, target):
    """Translate actual Pawn using the fixed camera's projected world basis."""
    pawn = fixture.unreal.GameplayStatics.get_player_pawn(fixture.manager, 0)
    movement = pawn.get_movement_component()
    if not hasattr(fixture, "adaptive_pawn_position"):
        fixture.adaptive_pawn_position = pawn.get_actor_location()
        fixture.adaptive_pawn_tick = pawn.is_actor_tick_enabled()
        fixture.adaptive_movement_tick = movement.is_component_tick_enabled()
    movement.stop_movement_immediately()
    pawn.consume_movement_input_vector()
    pawn.set_actor_tick_enabled(False)
    movement.set_component_tick_enabled(False)
    left, top, right, bottom = bounds(fixture)
    points = {"head": ((left + right) / 2, top), "body": ((left + right) / 2, (top + bottom) / 2),
              "feet": ((left + right) / 2, bottom), "left": (left, (top + bottom) / 2)}
    current = points[part]
    original = pawn.get_actor_location()
    coords = [original.x, original.y, original.z]

    def project(values):
        position = fixture.unreal.Vector(*values)
        point = fixture.unreal.GameplayStatics.project_world_to_screen(fixture.pc, position, True)
        x, y = _xy(point)
        return x / fixture.h("ui_scale_x"), y / fixture.h("ui_scale_y")

    origin = project(coords)
    basis = []
    for axis in range(3):
        shifted = list(coords)
        shifted[axis] += 100.0
        point = project(shifted)
        basis.append(((point[0] - origin[0]) / 100.0, (point[1] - origin[1]) / 100.0))
    axis0, axis1 = max(combinations(range(3), 2),
                       key=lambda pair: abs(basis[pair[0]][0] * basis[pair[1]][1]
                                            - basis[pair[1]][0] * basis[pair[0]][1]))
    a, b = basis[axis0], basis[axis1]
    determinant = a[0] * b[1] - b[0] * a[1]
    require(abs(determinant) > 0.000001, "camera projection provides two independent movement axes")
    dx, dy = target[0] - current[0], target[1] - current[1]
    coords[axis0] += (dx * b[1] - dy * b[0]) / determinant
    coords[axis1] += (a[0] * dy - a[1] * dx) / determinant
    pawn.set_actor_location(fixture.unreal.Vector(*coords), False, True)
    fixture.adaptive_pose = {"part": part, "target_reference": list(target),
                             "prior_bounds": [left, top, right, bottom], "world_axes": [axis0, axis1]}


def sample_frame(fixture):
    """Sample observed DrawHUD state; the driver calls this on separate frames."""
    if not hasattr(fixture, "adaptive_samples"):
        return
    sample = {"wall_seconds": round(time.monotonic() - fixture.adaptive_started, 5),
              "draw_count": fixture.h("draw_count"), "world_delta": fixture.h("hud_delta"),
              "alpha": {group: fixture.h("hudalpha_" + group) for group in GROUPS}}
    if not fixture.adaptive_samples or sample["draw_count"] != fixture.adaptive_samples[-1]["draw_count"]:
        fixture.adaptive_samples.append(sample)
        if len(fixture.adaptive_samples) > 120:
            fixture.adaptive_samples.pop(1)


def stage_adaptive_hud(fixture, case):
    """Sequential capture states intentionally preserve live cooked portions."""
    fixture.adaptive_samples = []
    fixture.adaptive_started = time.monotonic()
    fixture.adaptive_expected = {}
    fixture.adaptive_pose = None
    fixture.call("UpdatePointer", -1.0, -1.0, False)
    if case == "00_all_compact":
        fixture.reset(menu=3, clams=2)
        fixture.put(fixture.session, "hud_expanded_mask", 0)
        fixture.call("OpenService")
        fixture.call("Advance", 20.0)
        fixture.click(20)
        set_mask(fixture, 0)
    elif case == "01_all_expanded": set_mask(fixture, 7)
    elif case == "02_mixed": set_mask(fixture, 5)
    elif case == "03_both_cooking":
        fixture.call("StartCook", 0)
        fixture.call("StartCook", 3)
        fixture.call("Advance", 4.0)
    elif case == "04_pan_ready_pot_cooking": fixture.call("Advance", 4.0)
    elif case == "05_special_two_ready": fixture.call("Advance", 8.0)
    elif case == "06_special_one_left": fixture.call("Pickup", 1)
    elif case == "07_stations_empty":
        fixture.call("UseInteraction", 3)
        fixture.call("Pickup", 1)
        fixture.call("UseInteraction", 3)
        fixture.call("EmptyStation", 0)
    elif case in ("08_head_overlap", "09_body_overlap", "10_feet_overlap",
                  "11_hysteresis_25px", "12_hysteresis_clear_40px"):
        x, y, w, h = resolve_rect("day_phase", **state(fixture))
        part, target = {
            "08_head_overlap": ("head", (x + w / 2, y + h + 10)),
            "09_body_overlap": ("body", (x + w / 2, y + h / 2)),
            "10_feet_overlap": ("feet", (x + w / 2, y - 10)),
            "11_hysteresis_25px": ("left", (x + w + 25, y + h / 2)),
            "12_hysteresis_clear_40px": ("left", (x + w + 40, y + h / 2)),
        }[case]
        pose_projected(fixture, part, target)
        fixture.adaptive_expected = {"group": "day_phase", "occluded": case != "12_hysteresis_clear_40px",
                                      "alpha": 0.0 if case == "12_hysteresis_clear_40px" else 1.0}
    elif case == "13_inventory_overlap":
        x, y, w, h = resolve_rect("inventory", **state(fixture))
        pose_projected(fixture, "body", (x + w / 2, y + h / 2))
        fixture.adaptive_expected = {"group": "inventory", "occluded": True, "alpha": 1.0}
    elif case == "14_inventory_hover":
        fixture.call("UpdatePointer", *button_center(33, fixture.presentation()), False)
        fixture.adaptive_expected = {"group": "inventory", "occluded": True, "alpha": 0.0, "hover": True}
    elif case == "15_inventory_faded_click":
        # The driver waits for actual alpha=1 before dispatching the same shared
        # pointer click. Fading must never shrink or disable the input region.
        fixture.adaptive_expected = {"group": "inventory", "occluded": True, "alpha": 1.0}
    elif case == "16_pause_freeze":
        fixture.call("StartCook", 1)
        fixture.call("Advance", 4.0)
        fixture.click(30)
        fixture.adaptive_pause_snapshot = fixture.game_snapshot()
        fixture.adaptive_pause_time = fixture.unreal.GameplayStatics.get_time_seconds(fixture.manager)
    elif case == "17_beach":
        require(fixture.m("is_beach"), "beach case uses the actual traveled manager")
        require(fixture.s("hud_expanded_mask") == 5, "real beach travel retained mask 5")
        fixture.call("UseInteraction", 8)
        require(fixture.s("clams") == 1, "real beach gathering increments the isolated stock")
    else: raise ValueError(case)
    fixture.call("FindInteraction")
    fixture.call("RefreshHUD")
    # Both alpha transitions require multiple real DrawHUD frames. No sleep or
    # simulation-mirror calculation is used to manufacture their final state.
    return 0.5


def click_faded_inventory(fixture):
    before_alpha = fixture.h("hudalpha_inventory")
    require(before_alpha > 0.95, "inventory reached real faded state before pointer click")
    before_mask = fixture.s("hud_expanded_mask")
    fixture.click(33)
    fixture.call("UpdatePointer", *button_center(33, fixture.presentation()), False)
    fixture.call("RefreshHUD")
    require(fixture.s("hud_expanded_mask") == (before_mask ^ 4), "faded inventory remains clickable")
    fixture.adaptive_faded_click = {"alpha_before_click": before_alpha, "mask_before": before_mask,
                                     "mask_after": fixture.s("hud_expanded_mask")}
    # Collapsing can move the character outside the new shorter rectangle, so
    # the final invariant is normal alpha under the actual pointer.
    fixture.adaptive_expected = {"group": "inventory", "alpha": 0.0, "hover": True}


def adaptive_metadata(fixture, case):
    data = main_hud_metadata(fixture)
    values = state(fixture)
    checks = []
    expected = fixture.adaptive_expected
    group = expected.get("group")
    if group:
        if "occluded" in expected:
            checks.append(require(values["hudoccluded_" + group] == expected["occluded"],
                                  group + " projected overlap/hysteresis decision"))
        checks.append(require(abs(values["hudalpha_" + group] - expected["alpha"]) < 0.05,
                              group + " completed real-frame alpha transition"))
        if "hover" in expected:
            checks.append(require(values["hudhover_" + group] == expected["hover"], group + " actual hover geometry"))
    if fixture.adaptive_pose:
        left, top, right, bottom = bounds(fixture)
        part = fixture.adaptive_pose["part"]
        point = {"head": ((left + right) / 2, top), "body": ((left + right) / 2, (top + bottom) / 2),
                 "feet": ((left + right) / 2, bottom), "left": (left, (top + bottom) / 2)}[part]
        error = max(abs(point[i] - fixture.adaptive_pose["target_reference"][i]) for i in (0, 1))
        checks.append(require(error < 2.0, "actual " + part + " position matches projected target within 2px"))
        fixture.adaptive_pose["measured_reference"] = list(point)
        fixture.adaptive_pose["error_reference_pixels"] = error
    bubble_cases = {
        "03_both_cooking": (4.0, 12.0, 1, 2), "04_pan_ready_pot_cooking": (0.0, 8.0, 1, 2),
        "05_special_two_ready": (0.0, 0.0, 1, 2), "06_special_one_left": (0.0, 0.0, 1, 1),
        "07_stations_empty": (0.0, 0.0, 0, 0),
    }
    if case in bubble_cases:
        observed = tuple(fixture.m(name) for name in ("pan_remaining", "pot_remaining", "pan_portions", "pot_portions"))
        checks.append(require(observed == bubble_cases[case], "actual sequential cooking/portion state " + case))
        for prefix in ("pan", "pot"):
            occupied = fixture.m(prefix + "_remaining") > 0 or fixture.m(prefix + "_portions") > 0
            checks.append(require(values[prefix + "_bubble_visible"] == occupied,
                                  prefix + " bubble is visible exactly while occupied in the Hub view"))
    if case == "16_pause_freeze":
        checks.append(require(fixture.game_snapshot() == fixture.adaptive_pause_snapshot
                              and fixture.unreal.GameplayStatics.get_time_seconds(fixture.manager) == fixture.adaptive_pause_time,
                              "full pause freezes cooking, arrivals, orders and native game time across real frames"))
        checks.append(require(all(values["hudalpha_" + item] == 0.0 for item in GROUPS),
                              "modal popup immediately normalizes every HUD group opacity"))
    if case == "17_beach":
        checks.append(require(not values["pan_bubble_visible"] and not values["pot_bubble_visible"],
                              "real beach view has no cooking bubbles"))
    data.update({"adaptive_checks": checks, "hud_expanded_mask": fixture.s("hud_expanded_mask"),
        "visibility": json_presentation({name: values[name] for name, _, _ in VISIBILITY_FIELDS}),
        "groups": {item: {"bounds": list(resolve_rect(item, **values)), "visible": group_visible(item, **values),
                            "occluded": values["hudoccluded_" + item], "alpha": values["hudalpha_" + item],
                            "background_opacity": 1.0 - 0.8 * values["hudalpha_" + item],
                            "foreground_opacity": 1.0 - 0.35 * values["hudalpha_" + item]} for item in GROUPS},
        "actual_pawn_world_position": json_presentation(fixture.unreal.GameplayStatics.get_player_pawn(fixture.manager, 0).get_actor_location()),
        "pose": fixture.adaptive_pose, "real_frame_samples": fixture.adaptive_samples,
        "faded_click": getattr(fixture, "adaptive_faded_click", None) if case == "15_inventory_faded_click" else None,
        "manual_keyboard_mouse_playtest": False})
    return data
