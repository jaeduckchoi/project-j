"""Stage actual Blueprint gameplay states for the main HUD capture matrix."""
from __future__ import annotations

from jonggu.ui.hud import HUD_FIELDS, BUTTONS, visible_button, enabled_button
from jonggu.gameplay.data import ORDER_FIELDS, DISH_FIELDS
from jonggu.validation.verify_popups import button_rect

HUD_CASES = ["00_preparation", "01_service_two_orders", "02_cooking_busy",
             "03_ready_special_and_held_plate", "04_intake_paused", "05_closing",
             "06_menu_overlay", "07_beach"]


def stage_main_hud(fixture, case):
    """Caller must travel to the actual beach before staging its final case."""
    is_beach = fixture.m("is_beach")
    fixture.reset(menu=3, clams=3)
    if case == "07_beach":
        if not is_beach:
            raise AssertionError("Beach HUD capture requires the real beach manager")
        fixture.put(fixture.manager, "is_beach", True)
        fixture.put(fixture.session, "checkpoint_map", "L_Beach")
        fixture.put(fixture.session, "entry_marker", "beach")
        fixture.put(fixture.session, "clams", 0)
        fixture.call("UseInteraction", 8)
        if fixture.s("clams") != 1 or fixture.s("harvested_mask") != 1:
            raise AssertionError("Beach fixture must gather a real clam through Blueprint")
    elif case == "00_preparation":
        pass
    elif case == "06_menu_overlay":
        fixture.click(10)
        if fixture.m("screen_id") != 1:
            raise AssertionError("Preparation menu button did not open its popup")
    else:
        # Real scheduled arrivals give two simultaneous orders for the selected
        # rice/soup menu, with different waiting ages visible in the HUD.
        fixture.call("OpenService")
        fixture.call("Advance", 20.0)
        if not fixture.m("service") or fixture.m("guest_count") != 2:
            raise AssertionError("HUD fixture needs two real scheduled guests")
        if not all(order.get_editor_property("active") for order in fixture.orders):
            raise AssertionError("Both HUD order cards must contain live orders")
        if case in ("02_cooking_busy", "03_ready_special_and_held_plate", "04_intake_paused", "05_closing"):
            fixture.call("StartCook", 0)
            fixture.call("StartCook", 3)
            fixture.call("Advance", 4.0)
            if fixture.m("pan_remaining") != 4.0 or fixture.m("pot_remaining") != 12.0:
                raise AssertionError("HUD cooking state must use real active timers")
        if case == "03_ready_special_and_held_plate":
            fixture.call("Advance", 12.0)
            fixture.call("Pickup", 0)
            fixture.call("Serve", 0)
            fixture.call("Pickup", 1)
            if (fixture.m("pan_portions") != 0 or fixture.m("pot_portions") != 1
                    or fixture.carried.get_editor_property("dish_id") != 1
                    or not fixture.carried.get_editor_property("is_special")):
                raise AssertionError("HUD ready state needs real special plate and leftovers")
        elif case == "04_intake_paused":
            fixture.click(20)
            fixture.call("Advance", 1.0)
            if not fixture.m("intake_paused") or fixture.m("pot_remaining") != 11.0:
                raise AssertionError("Intake pause must leave cooking in progress")
        elif case == "05_closing":
            fixture.click(21)
            if not fixture.m("closing") or not fixture.m("service") or fixture.m("screen_id") != 0:
                raise AssertionError("Closing must retain the two accepted orders on the HUD")
        elif case not in ("01_service_two_orders", "02_cooking_busy"):
            raise ValueError("Unknown main HUD case: " + case)
    fixture.call("FindInteraction")
    fixture.call("UpdatePointer", -1.0, -1.0, False)
    fixture.call("RefreshHUD")
    assert_hud_fields(fixture)


def assert_hud_fields(fixture):
    """Compare rendered data to real objects, including normalized empty states."""
    expected = {"day": fixture.s("day"), "elapsed": fixture.m("elapsed"),
                "served_count": fixture.m("served_count"), "guest_count": fixture.m("guest_count"),
                "interaction_hint": fixture.m("interaction_hint"),
                "display_revenue": fixture.s("total_revenue") +
                    (fixture.m("base_revenue") + fixture.m("bonus_revenue") if fixture.m("service") else 0)}
    dish_id = fixture.carried.get_editor_property("dish_id")
    expected["held_dish_id"] = dish_id
    expected["held_is_special"] = fixture.carried.get_editor_property("is_special") if dish_id >= 0 else False
    for station in ("pan", "pot"):
        occupied = fixture.m(station + "_remaining") > 0 or fixture.m(station + "_portions") > 0
        expected[station + "_dish_id"] = fixture.m(station + "_dish") if occupied else -1
        expected[station + "_is_special"] = fixture.m(station + "_special") if occupied else False
    for index, order in enumerate(fixture.orders):
        active = order.get_editor_property("active")
        prefix = "order" + str(index)
        expected[prefix + "_active"] = active
        expected[prefix + "_dish_id"] = order.get_editor_property("dish_id") if active else -1
        expected[prefix + "_wait"] = max(0.0, fixture.m("elapsed") - order.get_editor_property("placed_at")) if active else 0.0
    for name, expected_value in expected.items():
        actual = fixture.h(name)
        matches = abs(actual - expected_value) < 0.0001 if isinstance(expected_value, float) else actual == expected_value
        if not matches:
            raise AssertionError("Main HUD binding " + name + ": expected " + str(expected_value) + ", got " + str(actual))
    state = {name: fixture.h(name) for name, _, _ in HUD_FIELDS}
    controls = [b for b in BUTTONS if visible_button(b, **state)]
    if fixture.m("screen_id") != 0 and any(b["screen"] == 0 for b in controls):
        raise AssertionError("A normal HUD hit region remained visible behind a popup")
    return list(expected) + ["normal_controls_hidden_behind_popups"]


def validate_main_hud_runtime(fixture):
    """Exercise pause and live/settled income once in this capture-only fixture."""
    checks = []
    def check(label, passed):
        if not passed: raise AssertionError("Main HUD QA: " + label)
        checks.append(label)
    fixture.reset(clams=1)
    fixture.click(30)
    check("main HUD pause button opens full pause", fixture.m("screen_id") == 4
          and fixture.unreal.GameplayStatics.is_game_paused(fixture.manager))
    fixture.click(303)
    check("pause X restores normal HUD movement", fixture.m("screen_id") == 0
          and not fixture.unreal.GameplayStatics.is_game_paused(fixture.manager)
          and not fixture.pc.is_move_input_ignored())
    fixture.click(10)
    fixture.call("RefreshHUD")
    assert_hud_fields(fixture)
    state = {name: fixture.h(name) for name, _, _ in HUD_FIELDS}
    check("menu overlay hides pause and all normal HUD controls",
          not any(b["screen"] == 0 and visible_button(b, **state) for b in BUTTONS))
    fixture.click(107)
    fixture.put(fixture.session, "total_revenue", 137)
    fixture.call("OpenService")
    fixture.call("Advance", 20.0)
    fixture.call("StartCook", 0)
    fixture.call("StartCook", 3)
    fixture.call("Advance", 16.0)
    fixture.call("Pickup", 0)
    fixture.call("Serve", 0)
    fixture.call("RefreshHUD")
    assert_hud_fields(fixture)
    check("one serving appears once in live income", fixture.m("served_count") == 1
          and fixture.s("total_revenue") == 137 and fixture.h("display_revenue") > 137)
    check("served order and emptied station have no stale dish icons", not fixture.h("order0_active")
          and fixture.h("order0_dish_id") == -1 and fixture.h("order0_wait") == 0.0
          and fixture.h("pan_dish_id") == -1 and fixture.h("held_dish_id") == -1)
    fixture.call("Pickup", 1)
    fixture.click(21)
    fixture.call("Serve", 1)
    fixture.call("RefreshHUD")
    assert_hud_fields(fixture)
    earned = fixture.m("base_revenue") + fixture.m("bonus_revenue")
    check("summary counts settled income exactly once", fixture.m("screen_id") == 5
          and not fixture.m("service") and earned > 0 and fixture.s("total_revenue") == 137 + earned
          and fixture.h("display_revenue") == fixture.s("total_revenue"))
    check("summary cleanup clears held and station dish icons", fixture.h("held_dish_id") == -1
          and fixture.h("pan_dish_id") == -1 and fixture.h("pot_dish_id") == -1)
    return {"success": True, "checks": checks, "check_count": len(checks),
            "implementation": "Actual compiled Blueprint Click/Advance/Serve/Finish and current HUD fields"}


def json_presentation(value):
    """Preserve native object/vector fields in report-safe observable form."""
    if value is None or isinstance(value, (str, bool, int, float)):
        return value
    if hasattr(value, "x") and hasattr(value, "y"):
        return [float(getattr(value, axis)) for axis in ("x", "y", "z") if hasattr(value, axis)]
    if hasattr(value, "get_path_name"):
        return value.get_path_name()
    if isinstance(value, dict):
        return {str(key): json_presentation(item) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [json_presentation(item) for item in value]
    return str(value)


def main_hud_metadata(fixture):
    """Snapshot current public presentation and typed gameplay, never a mirror."""
    presentation = {name: fixture.h(name) for name, _, _ in HUD_FIELDS}
    return {
        "presentation": json_presentation(presentation),
        "presentation_checks": assert_hud_fields(fixture),
        "visible_controls": [{"id": button["id"], "screen": button["screen"], "label": button["label"],
                              "bounds": list(button_rect(button, presentation)),
                              "enabled": enabled_button(button, **presentation)}
                             for button in BUTTONS if visible_button(button, **presentation)],
        "orders": [{name: order.get_editor_property(name) for name, _, _ in ORDER_FIELDS}
                   for order in fixture.orders],
        "held_dish": {name: fixture.carried.get_editor_property(name) for name, _, _ in DISH_FIELDS},
        "service": {name: fixture.m(name) for name in ("service", "intake_paused", "closing", "elapsed",
                    "guest_count", "served_count", "base_revenue", "bonus_revenue", "clams_used", "special_served")},
        "session": {name: fixture.s(name) for name in ("day", "clams", "harvested_mask", "menu_mask",
                    "guest_target", "total_revenue", "checkpoint_map", "entry_marker")},
        "is_beach": fixture.m("is_beach"),
        "world_name": fixture.manager.get_world().get_name(),
        "world_object_path": fixture.manager.get_path_name(),
    }
