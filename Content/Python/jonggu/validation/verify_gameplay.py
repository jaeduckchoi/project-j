"""Run real restaurant Blueprint scenarios on an initialized PIE manager.

Advance(dt) invokes the same function called by Tick, without wall-clock waits.
Fixtures reset independent state, but all cooking/serving/save assertions inspect
actual Blueprint results. The production save and current in-memory play state
are restored in finally. This module does not execute or launch the editor.
"""
from __future__ import annotations

import uuid
import unreal
from jonggu.gameplay.session import PERSISTED_FIELDS, SESSION_ONLY_FIELDS
from jonggu.gameplay.data import ORDER_FIELDS, DISH_FIELDS

MANAGER_FIELDS = [
    "is_beach", "screen_id", "previous_screen", "service", "intake_paused", "closing",
    "elapsed", "next_arrival", "guest_count", "served_count", "base_revenue", "bonus_revenue",
    "clams_used", "special_served", "notice_text", "notice_remaining", "nearest_id",
    "best_distance", "interaction_hint", "temp_int", "temp_dish", "temp_bonus", "chosen_recipe",
] + [p + suffix for p in ("pan", "pot") for suffix in
     ("_remaining", "_duration", "_portions", "_dish", "_special")]


def validate_gameplay(manager):
    """Execute independent scenarios; manager.Setup must already have run in PIE."""
    session = manager.get_editor_property("session")
    orders = [manager.get_editor_property("order0"), manager.get_editor_property("order1")]
    carried = manager.get_editor_property("carried")
    if not session or not all(orders) or not carried:
        raise RuntimeError("Gameplay QA requires manager.Setup and typed runtime objects")
    slot = "JongguGameplayQA_" + uuid.uuid4().hex
    checks = []
    session_fields = [field[0] for field in PERSISTED_FIELDS + SESSION_ONLY_FIELDS]
    original_session = {name: session.get_editor_property(name) for name in session_fields}
    original_manager = {name: manager.get_editor_property(name) for name in MANAGER_FIELDS}
    original_orders = [{name: order.get_editor_property(name) for name, _, _ in ORDER_FIELDS}
                       for order in orders]
    original_carried = {name: carried.get_editor_property(name) for name, _, _ in DISH_FIELDS}

    def m(name): return manager.get_editor_property(name)
    def s(name): return session.get_editor_property(name)
    def d(name): return carried.get_editor_property(name)
    def o(seat, name): return orders[seat].get_editor_property(name)
    def put(obj, name, value): obj.set_editor_property(name, value)
    def call(name, *args): return manager.call_method(name, args)
    def sc(name, *args): return session.call_method(name, args)

    def check(label, passed):
        if not passed:
            state = {name: m(name) for name in
                     ("service", "screen_id", "elapsed", "guest_count", "served_count",
                      "pan_remaining", "pan_portions", "pot_remaining", "pot_portions")}
            raise AssertionError("Gameplay QA: " + label + " | " + str(state))
        checks.append(label)

    def clear_world():
        for name in ("service", "intake_paused", "closing"):
            put(manager, name, False)
        for name in ("elapsed", "next_arrival", "notice_remaining"):
            put(manager, name, 0.0)
        for name in ("guest_count", "served_count", "base_revenue", "bonus_revenue",
                     "clams_used", "special_served", "previous_screen", "temp_int", "temp_dish", "temp_bonus"):
            put(manager, name, 0)
        put(manager, "is_beach", False)
        put(manager, "notice_text", "")
        for prefix in ("pan", "pot"):
            for suffix, value in (("_remaining", 0.0), ("_duration", 1.0),
                                  ("_portions", 0), ("_dish", -1), ("_special", False)):
                put(manager, prefix + suffix, value)
        for order in orders:
            for name, _, value in ORDER_FIELDS:
                put(order, name, value)
        for name, _, value in DISH_FIELDS:
            put(carried, name, value)
        call("SetScreen", 0)

    def reset(menu=1, clams=0, guests=4, money=0):
        clear_world()
        for name, _, value in PERSISTED_FIELDS:
            put(session, name, value)
        for name, value in (("menu_mask", menu), ("clams", clams),
                            ("guest_target", guests), ("total_revenue", money),
                            ("save_slot", slot), ("initialized", True), ("save_ok", True)):
            put(session, name, value)
        call("RefreshHUD")

    def open_service():
        call("OpenService")
        if not m("service"):
            raise AssertionError("Gameplay QA could not open service; save_ok=" + str(s("save_ok")))

    def advance(seconds): call("Advance", float(seconds))
    def first_active(): return next((seat for seat in (0, 1) if o(seat, "active")), None)

    try:
        # Isolation is established before any OpenService/Finish checkpoint call.
        put(session, "save_slot", slot)

        reset(menu=1, clams=0)
        open_service()
        check("opening immediately receives first guest", m("guest_count") == 1 and o(0, "active"))
        iterations = 0
        while m("service") and iterations < 16:
            iterations += 1
            seat = first_active()
            if seat is None:
                advance(max(0.0, m("next_arrival") - m("elapsed")) + 0.01)
                seat = first_active()
            if seat is None:
                break
            call("StartCook", 0)
            advance(m("pan_duration"))
            call("Pickup", 0)
            call("Serve", seat)
        check("four-guest basic service completes without exploration or clams",
              not m("service") and s("checkpoint_phase") == 2 and s("summary_served") == 4
              and s("summary_base") == 4 * m("base_price") and s("clams") == 0
              and s("summary_clams_used") == 0)

        reset(menu=1)
        call("SetScreen", 1)
        call("Action", 101)
        check("menu UI rejects zero dishes", s("menu_mask") == 1)
        call("Action", 102)
        check("menu UI accepts two dishes", s("menu_mask") == 3)
        call("Action", 103)
        check("menu UI rejects three dishes", s("menu_mask") == 3)
        call("Action", 101)
        call("Action", 103)
        open_service()
        old_menu, old_guests = s("menu_mask"), s("guest_target")
        call("Action", 101)
        call("Action", 105)
        check("menu and guest count remain fixed during service",
              s("menu_mask") == old_menu == 6 and s("guest_target") == old_guests)
        call("StartCook", 0)
        check("unselected recipe cannot start cooking", m("pan_remaining") == 0 and m("pan_portions") == 0)
        advance(m("arrival_interval"))
        check("selected menus alternate by dish ID", o(0, "dish_id") == 1 and o(1, "dish_id") == 2)
        for invalid_mask in (0, 7):
            reset(menu=invalid_mask)
            call("OpenService")
            check("opening rejects invalid menu mask " + str(invalid_mask), not m("service"))

        reset(menu=2, clams=1)
        open_service()
        call("StartCook", 3)
        check("one clam starts two-portion special soup",
              s("clams") == 0 and m("clams_used") == 1 and m("pot_portions") == 2
              and m("pot_dish") == 1 and m("pot_special") and m("pot_remaining") > 0)
        before = (m("pot_remaining"), m("pot_portions"), s("clams"), m("clams_used"))
        call("StartCook", 3)
        check("repeated start on occupied pot cannot consume twice",
              before == (m("pot_remaining"), m("pot_portions"), s("clams"), m("clams_used")))
        advance(m("pot_duration"))
        call("Pickup", 1)
        call("Serve", 0)
        check("first special portion satisfies ordinary soup order",
              m("special_served") == 1 and m("served_count") == 1 and m("pot_portions") == 1)
        advance(max(0.0, m("next_arrival") - m("elapsed")) + 0.01)
        call("Pickup", 1)
        call("Serve", first_active())
        check("same clam yields exactly two served special portions",
              m("special_served") == 2 and m("served_count") == 2
              and m("pot_portions") == 0 and s("clams") == 0 and m("clams_used") == 1)
        call("Pickup", 1)
        check("empty pot cannot create a third portion", d("dish_id") == -1 and m("pot_portions") == 0)
        call("StartCook", 3)
        check("zero clams rejects special without changing empty pot", m("pot_portions") == 0 and m("clams_used") == 1)
        call("StartCook", 1)
        check("basic soup remains available after clams run out",
              m("pot_portions") == 1 and m("pot_remaining") > 0 and not m("pot_special") and s("clams") == 0)

        reset(menu=3, clams=1)
        open_service()
        call("StartCook", 3)
        advance(m("pot_duration"))
        call("Pickup", 1)
        call("Serve", 0)
        check("wrong serving preserves dish and outstanding order",
              d("dish_id") == 1 and d("is_special") and o(0, "active")
              and o(0, "dish_id") == 0 and m("served_count") == 0)
        call("Pickup", 1)
        check("carrying one plate blocks second pickup", m("pot_portions") == 1 and d("dish_id") == 1)
        call("UseInteraction", 3)
        check("return counter discards carried plate without refund", d("dish_id") == -1 and not d("is_special") and s("clams") == 0)
        call("EmptyStation", 1)
        check("finished pot can be emptied without clam refund", m("pot_portions") == 0 and s("clams") == 0 and m("clams_used") == 1)
        call("StartCook", 1)
        running = m("pot_remaining")
        call("EmptyStation", 1)
        check("station clearing only removes completed food", m("pot_remaining") == running and m("pot_portions") == 1)
        advance(running)
        call("EmptyStation", 1)
        check("emptying finished station allows another cook", m("pot_portions") == 0)
        call("StartCook", 1)
        check("emptied pot starts a new basic batch", m("pot_remaining") > 0 and m("pot_portions") == 1)

        reset(menu=3, clams=1)
        open_service()
        call("StartCook", 0)
        call("StartCook", 3)
        before = (m("elapsed"), m("pan_remaining"), m("pot_remaining"), m("guest_count"), o(0, "placed_at"))
        call("PauseToggle")
        advance(100)
        check("full pause freezes cooking arrivals and order wait time", m("screen_id") == 4
              and before == (m("elapsed"), m("pan_remaining"), m("pot_remaining"), m("guest_count"), o(0, "placed_at")))
        call("PauseToggle")
        advance(1)
        check("resume advances both appliances", m("screen_id") == 0 and m("elapsed") == 1
              and m("pan_remaining") == before[1] - 1 and m("pot_remaining") == before[2] - 1)
        call("SetScreen", 2)
        advance(1)
        controller = unreal.GameplayStatics.get_player_controller(manager, 0)
        check("cooking selector blocks movement while service time continues",
              m("elapsed") == 2 and controller.is_move_input_ignored())
        call("SetScreen", 0)
        check("closing selector restores movement input", not controller.is_move_input_ignored())

        reset(menu=1)
        open_service()
        call("Action", 20)
        call("StartCook", 0)
        advance(m("pan_duration"))
        call("Pickup", 0)
        call("Serve", 0)
        advance(60)
        check("intake pause permits cooking and serving without auto settlement",
              m("service") and m("intake_paused") and m("served_count") == 1
              and m("guest_count") == 1 and not o(0, "active") and not o(1, "active"))
        call("Action", 20)
        advance(m("arrival_interval") - 1)
        check("resuming intake waits a fresh arrival interval", not m("intake_paused") and m("guest_count") == 1)
        advance(1)
        check("resumed intake admits one guest without burst", m("guest_count") == 2 and sum(bool(o(i, "active")) for i in (0, 1)) == 1)
        call("Action", 21)
        check("early closing waits for accepted order", m("service") and m("closing") and m("intake_paused"))
        call("StartCook", 0)
        advance(m("pan_duration"))
        call("Pickup", 0)
        call("Serve", first_active())
        check("early closing settles after last accepted order", not m("service") and s("checkpoint_phase") == 2 and s("summary_served") == 2)
        settled = (s("total_revenue"), s("summary_served"), s("event_log").count("event=summary "))
        call("TryFinish")
        call("Finish")
        call("Finish")
        check("repeated settlement cannot duplicate revenue or summary event",
              settled == (s("total_revenue"), s("summary_served"), s("event_log").count("event=summary ")))

        reset(menu=1, guests=6)
        open_service()
        advance(m("arrival_interval"))
        advance(60)
        check("two full seats never exceed seating capacity", m("guest_count") == 2 and all(o(i, "active") for i in (0, 1)))
        call("StartCook", 0)
        advance(m("pan_duration"))
        call("Pickup", 0)
        call("Serve", 0)
        advance(0)
        check("freeing a full seat does not release queued arrivals immediately", m("guest_count") == 2)
        advance(max(0.0, m("next_arrival") - m("elapsed")))
        check("next full-seat interval adds only one guest", m("guest_count") == 3 and all(o(i, "active") for i in (0, 1)))

        for wait, expected in ((m("bonus_fast_seconds") - 1, m("bonus_fast")),
                               (m("bonus_fast_seconds"), m("bonus_fast")),
                               (m("bonus_fast_seconds") + 1, m("bonus_slow")),
                               (m("bonus_slow_seconds"), m("bonus_slow")),
                               (m("bonus_slow_seconds") + 1, 0)):
            reset(menu=1)
            open_service()
            call("Action", 20)
            call("StartCook", 0)
            advance(wait)
            call("Pickup", 0)
            call("Serve", 0)
            check("bonus boundary " + str(wait) + " seconds grants " + str(expected),
                  m("served_count") == 1 and m("base_revenue") == m("base_price")
                  and m("bonus_revenue") == expected)

        reset(menu=3, clams=2, guests=4, money=100)
        put(session, "harvested_mask", 3)
        open_service()
        call("StartCook", 3)
        call("StartCook", 0)
        advance(max(m("pan_duration"), m("pot_duration")))
        call("Pickup", 0)
        call("Serve", 0)
        check("live cooking consumes inventory before retry", s("clams") == 1 and m("served_count") == 1 and m("clams_used") == 1)
        # Deliberate fixture edits prove a checkpoint restores the entire durable
        # record, even fields normally fixed by the service UI.
        for name, value in (("menu_mask", 1), ("guest_target", 6), ("total_revenue", 999)):
            put(session, name, value)
        loaded = sc("LoadCheckpoint")
        check("retry restores pre-open inventory money menu guests and harvested state together",
              loaded and s("clams") == 2 and s("total_revenue") == 100
              and s("menu_mask") == 3 and s("guest_target") == 4 and s("harvested_mask") == 3)
        # OpenLevel recreates these map-only objects in the user action. Resetting
        # the same fixture here avoids destroying the PIE actor during this call.
        clear_world()
        open_service()
        check("reopened service starts clean counters and orders after rollback",
              m("served_count") == 0 and m("clams_used") == 0 and m("base_revenue") == 0
              and m("guest_count") == 1 and m("pot_portions") == 0 and d("dish_id") == -1)
        call("StartCook", 3)
        check("retry does not duplicate special ingredients", s("clams") == 1 and m("clams_used") == 1 and m("pot_portions") == 2)
        return {"success": True, "checks": checks, "check_count": len(checks),
                "implementation": "Actual PIE Blueprint functions; Advance is the runtime Tick function",
                "production_checkpoint_untouched": True, "qa_slot": slot,
                "qa_events_recorded": s("event_count") - original_session["event_count"],
                "covered_by_parent_pie_tour": ["actual OpenLevel travel/retry", "visual layout"],
                "remaining_manual_checks": ["physical E and mouse input", "human playtest feedback"]}
    finally:
        # Restore in-memory production state before restoring its slot name.
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
                unreal.log_warning("Unable to remove isolated gameplay QA slot: " + slot)
