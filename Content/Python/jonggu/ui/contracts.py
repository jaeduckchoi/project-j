"""Host-safe presentation and pointer contracts shared by authoring and QA.

The manager supplies HUD_FIELDS. Button IDs and rectangles are authored by
layout; these helpers never import Unreal or mutate gameplay state.
"""
from jonggu.assets import asset_path
from jonggu.ui.layout import BUTTONS, GROUPS, resolve_rect, resolve_button_rect

HUD_PATH = asset_path("BP_RestaurantHUD")

HUD_FIELDS = [
    ("screen_id", "int", 0),
    ("header_text", "string", "종구의 식당  ·  1일차  ·  조개 0개"),
    ("phase_text", "string", "준비"),
    ("held_text", "string", "빈손"),
    ("order0_text", "string", "빈 좌석"),
    ("order1_text", "string", "빈 좌석"),
    ("pan_text", "string", "팬  ·  비어 있음"),
    ("pot_text", "string", "냄비  ·  비어 있음"),
    ("pan_progress", "real", 0.0),
    ("pot_progress", "real", 0.0),
    ("help_text", "string", "WASD / 방향키  이동    E  상호작용    Esc  일시정지"),
    ("notice_text", "string", ""),
    ("summary_text", "string", ""),
    ("menu_mask", "int", 1),
    ("guest_limit", "int", 4),
    ("is_service", "bool", False),
    ("closing", "bool", False),
    ("intake_paused", "bool", False),
    ("can_retry", "bool", False),
    ("is_beach", "bool", False),
    ("clams", "int", 0),
    ("pan_busy", "bool", False), ("pot_busy", "bool", False),
    ("pan_remaining", "real", 0.0), ("pot_remaining", "real", 0.0),
    ("pan_portions", "int", 0), ("pot_portions", "int", 0),
    ("can_cook_rice", "bool", False), ("can_cook_pancake", "bool", False),
    ("can_cook_soup", "bool", False), ("can_cook_special", "bool", False),
    ("can_clear_pan", "bool", False), ("can_clear_pot", "bool", False),
    ("reason_rice", "string", ""), ("reason_pancake", "string", ""),
    ("reason_soup", "string", ""), ("reason_special", "string", ""),
    ("summary_served", "int", 0), ("summary_base", "int", 0),
    ("summary_bonus", "int", 0), ("summary_clams_used", "int", 0),
    ("summary_special_served", "int", 0), ("total_revenue", "int", 0),
    ("hovered_action", "int", 0), ("pressed_action", "int", 0),
    ("day", "int", 1), ("elapsed", "real", 0.0),
    ("served_count", "int", 0), ("guest_count", "int", 0),
    ("held_dish_id", "int", -1), ("held_is_special", "bool", False),
    ("pan_dish_id", "int", -1), ("pot_dish_id", "int", -1),
    ("pan_is_special", "bool", False), ("pot_is_special", "bool", False),
    ("order0_active", "bool", False), ("order1_active", "bool", False),
    ("order0_dish_id", "int", -1), ("order1_dish_id", "int", -1),
    ("order0_wait", "real", 0.0), ("order1_wait", "real", 0.0),
    ("interaction_hint", "string", ""), ("display_revenue", "int", 0),
    ("hud_expanded_mask", "int", 0),
    ("pointer_x", "real", -1.0), ("pointer_y", "real", -1.0),
    ("player_actor", "object:/Script/Engine.Actor", None),
    ("pan_anchor", "vector", "(X=0,Y=0,Z=0)"), ("pot_anchor", "vector", "(X=0,Y=0,Z=0)"),
    ("pan_anchor_valid", "bool", False), ("pot_anchor_valid", "bool", False),
]

HUD_RECTS = {group: resolve_rect(group, is_service=True) for group in GROUPS if not group.endswith("_bubble")}

POPUP_PANELS = {
    1: (80, 92, 1120, 536), 2: (80, 92, 1120, 536), 3: (80, 92, 1120, 536),
    4: (300, 164, 680, 392), 5: (220, 110, 840, 500),
}

def visible_button(button, screen_id, is_service=False, is_beach=False, can_retry=False, closing=False, **unused):
    """Pure-Python mirror used by authoring validation and input generation."""
    if button["screen"] != screen_id:
        return False
    condition = button.get("condition")
    if condition == "preparation":
        return not is_service and not is_beach
    if condition == "service":
        return is_service
    if condition == "service_controls":
        return is_service and not closing
    if condition == "can_retry":
        return can_retry
    return True

def enabled_button(button, **state):
    """Shared disabled-control contract; disabled cards still permit hover."""
    field = button.get("enabled_field")
    return bool(state.get(field, False)) if field else True

def hit_test(x, y, screen_id, **state):
    """Reference-space hit test; production input is an equivalent saved graph."""
    for button in BUTTONS:
        if visible_button(button, screen_id, **state):
            bx, by, bw, bh = resolve_button_rect(button, **state)
            if bx <= x < bx + bw and by <= y < by + bh:
                return button["id"] if enabled_button(button, **state) else 0
    return 0

def recipe_button_labels(recipes):
    """Resolve authored data into display text while preserving hit-test IDs."""
    definitions = {recipe["recipe_id"]: recipe for recipe in recipes}
    labels = {}
    for button_id, recipe_id in ((101, 0), (102, 1), (103, 2)):
        recipe = definitions[recipe_id]
        appliance = "팬" if recipe["appliance"] == 0 else "냄비"
        labels[button_id] = f'{recipe["display_name"]}  ·  {appliance} / {recipe["cook_seconds"]:g}초'
    for button_id, recipe_id in ((201, 0), (202, 2), (211, 1), (212, 3)):
        recipe = definitions[recipe_id]
        label = f'{recipe["display_name"]}  ·  {recipe["cook_seconds"]:g}초 / {recipe["portions"]}그릇'
        if recipe["clam_cost"]:
            label += f' / 조개 {recipe["clam_cost"]}개'
        labels[button_id] = label
    return labels
