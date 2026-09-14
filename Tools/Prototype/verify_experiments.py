"""Small operational comparisons using the real PIE Blueprint cooking graphs.

The same one-second Advance calls drive every scenario. Plate pickup/discard is
instantaneous in this measurement; walking, table service and human enjoyment
are deliberately not inferred from these results.
"""
from __future__ import annotations

import uuid
import unreal
from build_session import PERSISTED_FIELDS, SESSION_ONLY_FIELDS
from data_types import ORDER_FIELDS, DISH_FIELDS
from verify_gameplay import MANAGER_FIELDS


def validate_experiments(manager):
    """Measure station throughput; restore current play and its production slot."""
    session = manager.get_editor_property("session")
    orders = [manager.get_editor_property("order0"), manager.get_editor_property("order1")]
    carried = manager.get_editor_property("carried")
    if not session or not all(orders) or not carried:
        raise RuntimeError("Operational comparisons require an initialized PIE manager")
    slot = "JongguExperimentQA_" + uuid.uuid4().hex
    original_session = {name: session.get_editor_property(name)
                        for name, _, _ in PERSISTED_FIELDS + SESSION_ONLY_FIELDS}
    original_manager = {name: manager.get_editor_property(name) for name in MANAGER_FIELDS}
    original_orders = [{name: order.get_editor_property(name) for name, _, _ in ORDER_FIELDS}
                       for order in orders]
    original_carried = {name: carried.get_editor_property(name) for name, _, _ in DISH_FIELDS}

    def m(name): return manager.get_editor_property(name)
    def s(name): return session.get_editor_property(name)
    def put(obj, name, value): obj.set_editor_property(name, value)
    def call(name, *args): return manager.call_method(name, args)

    def prepare(menu):
        for name in ("service", "intake_paused", "closing", "is_beach"):
            put(manager, name, False)
        for name in ("elapsed", "next_arrival", "notice_remaining"):
            put(manager, name, 0.0)
        for name in ("guest_count", "served_count", "base_revenue", "bonus_revenue",
                     "clams_used", "special_served", "previous_screen"):
            put(manager, name, 0)
        for prefix in ("pan", "pot"):
            for suffix, value in (("_remaining", 0.0), ("_duration", 1.0),
                                  ("_portions", 0), ("_dish", -1), ("_special", False)):
                put(manager, prefix + suffix, value)
        for order in orders:
            for name, _, value in ORDER_FIELDS:
                put(order, name, value)
        for name, _, value in DISH_FIELDS:
            put(carried, name, value)
        for name, _, value in PERSISTED_FIELDS:
            put(session, name, value)
        for name, value in (("menu_mask", menu), ("clams", 1), ("save_slot", slot),
                            ("initialized", True), ("save_ok", True)):
            put(session, name, value)
        call("SetScreen", 0)
        call("OpenService")
        if not m("service"):
            raise RuntimeError("Operational comparison could not save/open its isolated fixture")
        call("Action", 20)  # Preserve the first order; block unrelated arrivals.

    def collect(station):
        call("Pickup", station)
        dish_id = carried.get_editor_property("dish_id")
        if dish_id < 0:
            raise RuntimeError("Ready station did not yield a real Blueprint dish")
        plate = {"dish_id": dish_id, "special": carried.get_editor_property("is_special"),
                 "completed_plate_at_seconds": m("elapsed")}
        call("UseInteraction", 3)
        if carried.get_editor_property("dish_id") != -1:
            raise RuntimeError("Experiment could not clear its carried plate")
        return plate

    def paired(second_recipe, menu):
        prepare(menu)
        call("StartCook", 0)
        call("StartCook", second_recipe)
        second_station = "pan" if second_recipe == 2 else "pot"
        second_dish = 2 if second_recipe == 2 else 1
        initial_second_accepted = m(second_station + "_dish") == second_dish
        first_accepted = m("pan_dish") == 0 and m("pan_remaining") > 0
        if not first_accepted:
            raise RuntimeError("Rice could not start in paired station comparison")
        starts = [{"recipe_id": 0, "at_seconds": 0.0}]
        attempts = [{"recipe_id": 0, "at_seconds": 0.0, "accepted": True},
                    {"recipe_id": second_recipe, "at_seconds": 0.0,
                     "accepted": initial_second_accepted}]
        if initial_second_accepted:
            starts.append({"recipe_id": second_recipe, "at_seconds": 0.0})
        plates = []
        for _ in range(120):
            call("Advance", 1.0)
            for station_id, prefix in enumerate(("pan", "pot")):
                if m(prefix + "_remaining") <= 0 and m(prefix + "_portions") > 0:
                    plates.append(collect(station_id))
                    if not initial_second_accepted and len(starts) == 1:
                        call("StartCook", second_recipe)
                        accepted = m("pan_dish") == second_dish and m("pan_remaining") > 0
                        attempts.append({"recipe_id": second_recipe,
                                         "at_seconds": m("elapsed"), "accepted": accepted})
                        if not accepted:
                            raise RuntimeError("Released shared pan could not start its waiting recipe")
                        starts.append({"recipe_id": second_recipe, "at_seconds": m("elapsed")})
            if len(plates) == 2:
                break
        if len(plates) != 2:
            raise RuntimeError("Paired station experiment did not finish within 120 simulated seconds")
        return {"menu_mask": menu, "initial_simultaneous_accepted": 1 + int(initial_second_accepted),
                "initial_second_recipe_blocked": not initial_second_accepted,
                "cooking_starts": starts, "start_attempts": attempts, "plates": plates,
                "two_plates_at_seconds": m("elapsed"),
                "second_recipe_waited_for_appliance_seconds": starts[1]["at_seconds"],
                "clams_used": m("clams_used")}

    def soup_batch(special):
        prepare(2)
        recipe_id = 3 if special else 1
        starts = []
        plates = []

        def start():
            call("StartCook", recipe_id)
            if m("pot_remaining") <= 0:
                raise RuntimeError("Soup recipe did not start in comparison")
            starts.append({"recipe_id": recipe_id, "at_seconds": m("elapsed")})

        start()
        for _ in range(120):
            call("Advance", 1.0)
            if m("pot_remaining") <= 0 and m("pot_portions") > 0:
                while m("pot_portions") > 0 and len(plates) < 2:
                    plates.append(collect(1))
                if len(plates) == 1:
                    start()
            if len(plates) == 2:
                break
        if len(plates) != 2:
            raise RuntimeError("Soup comparison did not finish within 120 simulated seconds")
        return {"recipe_id": recipe_id, "cooking_start_count": len(starts),
                "cooking_starts": starts, "plates": plates,
                "two_plates_at_seconds": m("elapsed"), "clams_used": m("clams_used"),
                "remaining_clams": s("clams")}

    try:
        put(session, "save_slot", slot)
        shared = paired(2, 5)
        split = paired(1, 3)
        basic = soup_batch(False)
        special = soup_batch(True)
        return {
            "success": True,
            "evidence_type": "scripted operational observation, human reasons untested",
            "execution": "Actual compiled Blueprint OpenService/StartCook/Advance/Pickup/UseInteraction",
            "step_seconds": 1.0,
            "controls": {"initial_clams_each_scenario": 1, "intake_paused": True,
                         "plate_handling_seconds": 0,
                         "comparison_includes": "appliance scheduling and production only"},
            "shared_pan_rice_pancake": shared,
            "split_pan_pot_rice_soup": split,
            "two_basic_soups": basic,
            "one_special_soup_batch": special,
            "measured_differences": {
                "paired_shared_minus_split_completion_seconds":
                    shared["two_plates_at_seconds"] - split["two_plates_at_seconds"],
                "basic_minus_special_completion_seconds":
                    basic["two_plates_at_seconds"] - special["two_plates_at_seconds"],
                "basic_minus_special_cook_starts": basic["cooking_start_count"] - special["cooking_start_count"],
            },
            "limitations": [
                "Menu comparison uses different recipes and their configured durations; not an isolated causal estimate of appliance count.",
                "Plate pickup and discard consume no simulated time; walking and customer service are excluded.",
                "No human playtest was performed; enjoyment, menu-choice reasons and exploration motivation remain untested.",
            ],
            "production_checkpoint_untouched": True,
        }
    finally:
        for name, value in original_session.items():
            if name != "save_slot": put(session, name, value)
        for name, value in original_manager.items(): put(manager, name, value)
        for order, values in zip(orders, original_orders):
            for name, value in values.items(): put(order, name, value)
        for name, value in original_carried.items(): put(carried, name, value)
        put(session, "save_slot", original_session["save_slot"])
        call("SetScreen", original_manager["screen_id"])
        call("RefreshHUD")
        if unreal.GameplayStatics.does_save_game_exist(slot, 0):
            if not unreal.GameplayStatics.delete_game_in_slot(slot, 0):
                unreal.log_warning("Unable to remove isolated experiment slot: " + slot)

