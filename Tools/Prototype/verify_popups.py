"""Popup-specific QA against shared geometry and actual compiled PIE functions."""
from __future__ import annotations

import uuid
import build_ui as ui


def button_rect(button, state=None):
    if button.get("hud_group"):
        from hud_layout import resolve_button_rect
        return resolve_button_rect(button, **(state or {}))
    return tuple(button[key] for key in ("x", "y", "w", "h"))


def button_center(button_id, state=None):
    button = next(b for b in ui.BUTTONS if b["id"] == button_id)
    x, y, width, height = button_rect(button, state)
    return float(x + width / 2), float(y + height / 2)


def validate_shared_geometry():
    """Inspect authored bounds/hit definitions without pretending to render."""
    ids = [button["id"] for button in ui.BUTTONS]
    if len(ids) != len(set(ids)):
        raise AssertionError("Popup button IDs must be unique")
    for screen, panel in ui.POPUP_PANELS.items():
        x, y, width, height = panel
        buttons = [button for button in ui.BUTTONS if button["screen"] == screen]
        enabled_state = {button["enabled_field"]: True for button in buttons if button.get("enabled_field")}
        state = dict(enabled_state, is_service=screen in (2, 3, 4), can_retry=True)
        for button in buttons:
            if not (x <= button["x"] and y <= button["y"]
                    and button["x"] + button["w"] <= x + width
                    and button["y"] + button["h"] <= y + height):
                raise AssertionError("Button outside popup: " + str(button["id"]))
            cx, cy = button_center(button["id"])
            if ui.hit_test(cx, cy, screen, **state) != button["id"]:
                raise AssertionError("Shared button center does not hit itself: " + str(button["id"]))
            if button.get("enabled_field"):
                disabled = dict(state, **{button["enabled_field"]: False})
                if ui.hit_test(cx, cy, screen, **disabled) != 0:
                    raise AssertionError("Shared disabled hit produced an action")
        for index, first in enumerate(buttons):
            for second in buttons[index + 1:]:
                overlap = (max(first["x"], second["x"]) < min(first["x"] + first["w"], second["x"] + second["w"])
                           and max(first["y"], second["y"]) < min(first["y"] + first["h"], second["y"] + second["h"]))
                if overlap:
                    raise AssertionError("Overlapping popup hit regions: " + str((first["id"], second["id"])))
    mappings = []
    for width, height in ((1280, 720), (1920, 1080), (1189, 862)):
        scale = min(width / 1280, height / 720)
        offset = ((width - 1280 * scale) / 2, (height - 720 * scale) / 2)
        mappings.append({"viewport": [width, height], "scale": scale, "offset": list(offset)})
    return {"success": True, "button_count": len(ids), "popup_screens": sorted(ui.POPUP_PANELS),
            "all_hit_regions_inside_panels": True, "nonoverlapping": True,
            "shared_enabled_hit_contract": True, "reference_mappings": mappings,
            "evidence": "authored geometry checks; render verification recorded separately"}


class PopupFixture:
    """Reusable real-PIE fixture for behavioral checks and screenshot staging."""
    def __init__(self, manager):
        import unreal
        from build_session import PERSISTED_FIELDS, SESSION_ONLY_FIELDS
        from data_types import ORDER_FIELDS, DISH_FIELDS
        from verify_gameplay import MANAGER_FIELDS
        self.unreal = unreal
        self.manager = manager
        self.session = manager.get_editor_property("session")
        self.orders = [manager.get_editor_property("order0"), manager.get_editor_property("order1")]
        self.carried = manager.get_editor_property("carried")
        self.pc = unreal.GameplayStatics.get_player_controller(manager, 0)
        self.hud = self.pc.get_hud()
        self.persisted = PERSISTED_FIELDS
        self.order_fields, self.dish_fields = ORDER_FIELDS, DISH_FIELDS
        self.slot = "JongguPopupQA_" + uuid.uuid4().hex
        fields = list(dict.fromkeys(MANAGER_FIELDS + ["hovered_action", "pressed_action"]
                     + [name for name, _, _ in ui.HUD_FIELDS if name in ("pointer_x", "pointer_y")]))
        self.original_manager = {name: self.m(name) for name in fields}
        self.original_session = {name: self.s(name) for name, _, _ in PERSISTED_FIELDS + SESSION_ONLY_FIELDS}
        self.original_orders = [{name: obj.get_editor_property(name) for name, _, _ in ORDER_FIELDS} for obj in self.orders]
        self.original_carried = {name: self.carried.get_editor_property(name) for name, _, _ in DISH_FIELDS}
        self.put(self.session, "save_slot", self.slot)

    def m(self, name): return self.manager.get_editor_property(name)
    def s(self, name): return self.session.get_editor_property(name)
    def h(self, name): return self.hud.get_editor_property(name)
    def put(self, obj, name, value): obj.set_editor_property(name, value)
    def call(self, name, *args): return self.manager.call_method(name, args)
    def presentation(self): return {name: self.h(name) for name, _, _ in ui.HUD_FIELDS}
    def click(self, button_id):
        self.call("RefreshHUD")
        return self.call("Click", *button_center(button_id, self.presentation()))

    def reset(self, menu=3, clams=1):
        for name in ("service", "intake_paused", "closing", "is_beach"):
            self.put(self.manager, name, False)
        for name in ("elapsed", "next_arrival", "notice_remaining"):
            self.put(self.manager, name, 0.0)
        self.put(self.manager, "notice_text", "")
        for name in ("guest_count", "served_count", "base_revenue", "bonus_revenue", "clams_used", "special_served", "previous_screen"):
            self.put(self.manager, name, 0)
        for prefix in ("pan", "pot"):
            for suffix, value in (("_remaining", 0.0), ("_duration", 1.0), ("_portions", 0), ("_dish", -1), ("_special", False)):
                self.put(self.manager, prefix + suffix, value)
        for order in self.orders:
            for name, _, value in self.order_fields: self.put(order, name, value)
        for name, _, value in self.dish_fields: self.put(self.carried, name, value)
        for name, _, value in self.persisted: self.put(self.session, name, value)
        for name, value in (("menu_mask", menu), ("clams", clams), ("save_slot", self.slot), ("initialized", True)):
            self.put(self.session, name, value)
        self.call("SetScreen", 0)

    def open(self):
        self.call("OpenService")
        if not self.m("service"):
            raise AssertionError("Popup fixture could not open its isolated service")
        self.call("Action", 20)

    def game_snapshot(self):
        return tuple(self.m(name) for name in ("service", "elapsed", "served_count", "clams_used", "base_revenue", "bonus_revenue",
                    "pan_remaining", "pan_portions", "pan_dish", "pot_remaining", "pot_portions", "pot_dish")) + (
                    self.s("clams"), self.s("menu_mask"), self.s("total_revenue"), self.carried.get_editor_property("dish_id"))

    def stage(self, screen):
        """Representative popup state created with real cooking and settlement."""
        self.reset(menu=3, clams=2)
        if screen == 1:
            self.click(10)
        elif screen in (2, 3):
            self.open()
            self.call("SetScreen", screen)
        elif screen == 4:
            self.open()
            self.call("StartCook", 1)
            self.call("Advance", 4.0)
            self.call("SetScreen", 3)
            self.call("PauseToggle")
        elif screen == 5:
            self.put(self.session, "menu_mask", 2)
            self.open()
            self.call("StartCook", 3)
            self.call("Advance", 16.0)
            self.call("Pickup", 1)
            self.call("Serve", 0)
            self.call("Action", 21)
        else:
            raise ValueError("Unknown popup screen")
        if self.m("screen_id") != screen:
            raise AssertionError("Could not stage popup " + str(screen))
        self.call("UpdatePointer", -1.0, -1.0, False)
        self.call("RefreshHUD")

    def restore(self):
        if hasattr(self, "adaptive_pawn_position"):
            pawn = self.unreal.GameplayStatics.get_player_pawn(self.manager, 0)
            pawn.get_movement_component().stop_movement_immediately()
            pawn.set_actor_location(self.adaptive_pawn_position, False, True)
            pawn.set_actor_tick_enabled(self.adaptive_pawn_tick)
            pawn.get_movement_component().set_component_tick_enabled(self.adaptive_movement_tick)
        for name, value in self.original_session.items():
            if name != "save_slot": self.put(self.session, name, value)
        for name, value in self.original_manager.items(): self.put(self.manager, name, value)
        for order, values in zip(self.orders, self.original_orders):
            for name, value in values.items(): self.put(order, name, value)
        for name, value in self.original_carried.items(): self.put(self.carried, name, value)
        self.put(self.session, "save_slot", self.original_session["save_slot"])
        self.call("SetScreen", self.original_manager["screen_id"])
        for name in ("hovered_action", "pressed_action"):
            self.put(self.manager, name, self.original_manager[name])
        self.call("RefreshHUD")
        if self.unreal.GameplayStatics.does_save_game_exist(self.slot, 0):
            if not self.unreal.GameplayStatics.delete_game_in_slot(self.slot, 0):
                self.unreal.log_warning("Unable to remove popup QA slot: " + self.slot)


def validate_popups(manager):
    fixture = PopupFixture(manager)
    checks = []
    def check(label, passed):
        if not passed: raise AssertionError("Popup QA: " + label)
        checks.append(label)
    def disabled(button_id, reason=True):
        button = next(b for b in ui.BUTTONS if b["id"] == button_id)
        fixture.call("RefreshHUD")
        field = button["enabled_field"]
        check("disabled state matches rendered field " + str(button_id), not fixture.m(field) and not fixture.h(field))
        if reason and button.get("reason_field"):
            check("disabled reason is visible-state data " + str(button_id), bool(fixture.h(button["reason_field"])))
        before, screen = fixture.game_snapshot(), fixture.m("screen_id")
        fixture.call("UpdatePointer", *button_center(button_id), True)
        check("disabled hover never becomes pressed " + str(button_id), fixture.m("hovered_action") == button_id and fixture.m("pressed_action") == 0)
        fixture.click(button_id); fixture.click(button_id)
        check("disabled duplicate click changes no game resource " + str(button_id), before == fixture.game_snapshot() and fixture.m("screen_id") == screen)
    try:
        geometry = validate_shared_geometry()
        fixture.reset(menu=1, clams=1); fixture.open(); fixture.call("SetScreen", 2)
        disabled(202)
        disabled(203, False)
        fixture.call("UpdatePointer", *button_center(201), True)
        check("enabled recipe supports hover and pressed feedback", fixture.m("hovered_action") == 201 and fixture.m("pressed_action") == 201)
        fixture.click(201)
        before = fixture.game_snapshot(); fixture.click(201)
        check("successful recipe click cannot pass through to new screen", fixture.m("screen_id") == 0 and before == fixture.game_snapshot())
        fixture.call("SetScreen", 2); disabled(201)
        fixture.click(204)
        check("pan X restores movement and keeps service", fixture.m("screen_id") == 0 and fixture.m("service") and not fixture.pc.is_move_input_ignored())

        fixture.reset(menu=2, clams=0); fixture.open(); fixture.call("SetScreen", 3)
        disabled(212)
        check("basic soup remains enabled without clams", fixture.h("can_cook_soup"))
        fixture.click(211); fixture.call("SetScreen", 3)
        disabled(211); disabled(213, False)
        fixture.call("Advance", 16.0); fixture.call("RefreshHUD")
        check("finished food keeps recipe disabled and enables clearing", not fixture.h("can_cook_soup") and fixture.h("can_clear_pot"))
        fixture.click(213)
        check("enabled clear removes portions and exits popup", fixture.m("pot_portions") == 0 and fixture.m("screen_id") == 0)

        fixture.reset(menu=2, clams=1); fixture.open(); fixture.call("SetScreen", 3)
        fixture.click(212); after_start = fixture.game_snapshot(); fixture.click(212)
        check("special double click spends one clam for two portions", fixture.s("clams") == 0 and fixture.m("clams_used") == 1
              and fixture.m("pot_portions") == 2 and after_start == fixture.game_snapshot())
        fixture.call("SetScreen", 3); disabled(212)
        fixture.click(214)
        check("pot X restores movement", fixture.m("screen_id") == 0 and not fixture.pc.is_move_input_ignored())

        fixture.reset(); fixture.click(10); fixture.click(107)
        check("menu X restores preparation movement", fixture.m("screen_id") == 0 and not fixture.pc.is_move_input_ignored())
        fixture.open()
        for screen, close_id in ((2, 204), (3, 214)):
            fixture.call("SetScreen", screen); fixture.call("PauseToggle")
            before = fixture.game_snapshot(); fixture.click(303)
            check("pause X returns to cooking popup " + str(screen), fixture.m("screen_id") == screen
                  and not fixture.unreal.GameplayStatics.is_game_paused(manager) and fixture.pc.is_move_input_ignored()
                  and before == fixture.game_snapshot())
            fixture.click(close_id)
            check("cooking X completes focus restoration " + str(screen), fixture.m("screen_id") == 0
                  and not fixture.pc.is_move_input_ignored() and fixture.m("hovered_action") == 0 and fixture.m("pressed_action") == 0)
        fixture.call("PauseToggle"); fixture.click(303)
        check("pause X returns to gameplay", fixture.m("screen_id") == 0 and not fixture.pc.is_move_input_ignored()
              and not fixture.unreal.GameplayStatics.is_game_paused(manager))

        fixture.stage(5)
        check("summary exposes next day and no X", [b["id"] for b in ui.BUTTONS if b["screen"] == 5] == [401])
        x, y, width, _ = ui.POPUP_PANELS[5]
        before = fixture.game_snapshot(); fixture.call("Click", float(x + width - 30), float(y + 30))
        check("summary corner cannot dismiss or duplicate settlement", fixture.m("screen_id") == 5 and before == fixture.game_snapshot())
        fixture.call("PauseToggle"); fixture.click(303)
        check("pause X restores summary without advancing day", fixture.m("screen_id") == 5 and fixture.s("day") == 1)
        return {"success": True, "checks": checks, "check_count": len(checks), "shared_geometry": geometry,
                "implementation": "Actual compiled manager Click/UpdatePointer and HUD presentation fields",
                "production_checkpoint_untouched": True, "visual_qa": "separate actual-PIE captures required"}
    finally:
        fixture.restore()
