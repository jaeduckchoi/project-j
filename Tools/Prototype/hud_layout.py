"""Shared HUD layout for saved Blueprint rendering, pointer input and QA.

All coordinates use the existing 1280x720 constrained camera view. This module
has no Unreal dependency; graph helpers only use the supplied get/math/pure API.
"""
REFERENCE_SIZE = (1280, 720)
GROUP_BITS = {"orders": 1, "stations": 2, "inventory": 4}
GROUPS = ("day_phase", "revenue", "orders", "stations", "inventory", "prompt",
          "control_dock", "pause_dock", "pan_bubble", "pot_bubble")
BUBBLE_BODY_SIZE = (72, 40)
BUBBLE_TAIL_HEIGHT = 8
FIXED_RECTS = {
    "day_phase": (20, 20, 340, 44), "revenue": (964, 20, 296, 44),
    "prompt": (336, 600, 624, 40), "control_dock": (336, 652, 624, 56),
    "pause_dock": (984, 652, 276, 56),
}

BUTTONS = [
    dict(screen=0, id=10, key="OpenMenu", x=352, y=660, w=592, h=40, label="오늘의 메뉴 정하기", condition="preparation"),
    dict(screen=0, id=20, key="IntakeToggle", x=352, y=660, w=288, h=40, label="손님 받기 중단", condition="service_controls"),
    dict(screen=0, id=21, key="Close", x=656, y=660, w=288, h=40, label="마감하기", condition="service_controls"),
    dict(screen=0, id=30, key="PauseToggle", x=1000, y=660, w=244, h=40, label="일시정지  Esc"),
    dict(screen=0, id=31, key="ToggleOrders", x=984, y=96, w=276, h=104, label="주문", condition="service", hud_group="orders", expanded_bit=1),
    dict(screen=0, id=32, key="ToggleStations", x=20, y=568, w=284, h=116, label="조리 기구", condition="service", hud_group="stations", expanded_bit=2),
    dict(screen=0, id=33, key="ToggleInventory", x=984, y=96, w=276, h=80, label="조개와 접시", hud_group="inventory", expanded_bit=4),
    dict(screen=1, id=101, key="ToggleRice", x=112, y=200, w=688, h=80, label="김치볶음밥", selected_mask=1, recipe_id=0, presentation="menu_recipe"),
    dict(screen=1, id=102, key="ToggleStew", x=112, y=292, w=688, h=80, label="김치찌개", selected_mask=2, recipe_id=1, presentation="menu_recipe"),
    dict(screen=1, id=103, key="TogglePancake", x=112, y=384, w=688, h=80, label="김치전", selected_mask=4, recipe_id=2, presentation="menu_recipe"),
    dict(screen=1, id=104, key="Guest4", x=848, y=250, w=144, h=56, label="4명", selected_guests=4),
    dict(screen=1, id=105, key="Guest6", x=1008, y=250, w=144, h=56, label="6명", selected_guests=6),
    dict(screen=1, id=106, key="Begin", x=912, y=548, w=256, h=48, label="영업 시작", primary=True),
    dict(screen=1, id=107, key="Cancel", x=1128, y=116, w=40, h=40, label="닫기", presentation="close"),
    dict(screen=2, id=201, key="CookRice", x=112, y=212, w=688, h=112, label="김치볶음밥", recipe_id=0, presentation="cook_recipe", requires_mask=1, enabled_field="can_cook_rice", reason_field="reason_rice"),
    dict(screen=2, id=202, key="CookPancake", x=112, y=344, w=688, h=112, label="김치전", recipe_id=2, presentation="cook_recipe", requires_mask=4, enabled_field="can_cook_pancake", reason_field="reason_pancake"),
    dict(screen=2, id=203, key="ClearPan", x=856, y=392, w=288, h=48, label="완성품 비우기", enabled_field="can_clear_pan", disabled_reason="완성된 음식이 없습니다"),
    dict(screen=2, id=204, key="Cancel", x=1128, y=116, w=40, h=40, label="닫기", presentation="close"),
    dict(screen=3, id=211, key="CookStew", x=112, y=212, w=688, h=112, label="김치찌개", recipe_id=1, presentation="cook_recipe", requires_mask=2, enabled_field="can_cook_soup", reason_field="reason_soup"),
    dict(screen=3, id=212, key="CookSpecial", x=112, y=344, w=688, h=112, label="조개 특선 찌개", recipe_id=3, presentation="cook_recipe", requires_mask=2, enabled_field="can_cook_special", reason_field="reason_special"),
    dict(screen=3, id=213, key="ClearPot", x=856, y=392, w=288, h=48, label="완성품 비우기", enabled_field="can_clear_pot", disabled_reason="완성된 음식이 없습니다"),
    dict(screen=3, id=214, key="Cancel", x=1128, y=116, w=40, h=40, label="닫기", presentation="close"),
    dict(screen=4, id=301, key="Resume", x=332, y=328, w=616, h=52, label="계속하기", primary=True),
    dict(screen=4, id=302, key="Retry", x=332, y=396, w=616, h=52, label="이번 영업 다시 시작", condition="can_retry"),
    dict(screen=4, id=303, key="Resume", x=908, y=188, w=40, h=40, label="닫기", presentation="close"),
    dict(screen=5, id=401, key="NextDay", x=772, y=530, w=256, h=48, label="다음 날 준비하기", primary=True),
]


def is_expanded(group, hud_expanded_mask=0):
    return bool(int(hud_expanded_mask) & GROUP_BITS.get(group, 0))


def resolve_rect(group, hud_expanded_mask=0, is_service=False, **state):
    if group in FIXED_RECTS:
        return FIXED_RECTS[group]
    expanded = is_expanded(group, hud_expanded_mask)
    if group == "orders":
        return (984, 96, 276, 284 if expanded else 104)
    if group == "stations":
        height = 260 if expanded else 116
        return (20, 684 - height, 284, height)
    if group == "inventory":
        order_height = 284 if is_expanded("orders", hud_expanded_mask) else 104
        return (984, 96 + order_height + 12 if is_service else 96, 276, 164 if expanded else 80)
    if group in ("pan_bubble", "pot_bubble"):
        return (state.get(group + "_x", 0.0), state.get(group + "_y", 0.0), 72, 48)
    raise KeyError(group)


def group_visible(group, screen_id=0, is_service=False, is_beach=False, closing=False,
                  notice_text="", interaction_hint="", **state):
    if group in ("orders", "stations"):
        return bool(is_service)
    if group in ("pan_bubble", "pot_bubble"):
        return bool(state.get(group + "_visible", False))
    if group == "control_dock":
        return not is_beach and not closing
    if group == "prompt":
        return bool(interaction_hint or (screen_id == 0 and notice_text))
    return True


def resolve_button_rect(button, **state):
    group = button.get("hud_group")
    if group:
        x, y, w, h = resolve_rect(group, **state)
        return (x, y, w, 40 if is_expanded(group, state.get("hud_expanded_mask", 0)) else h)
    return tuple(button[key] for key in ("x", "y", "w", "h"))


def expanded_expr(g, group, mask=None):
    mask = g.get("hud_expanded_mask") if mask is None else mask
    return g.math("NotEqual_IntInt", A=g.math("And_IntInt", A=mask, B=GROUP_BITS[group]), B=0)


def rect_expr(g, group, mask=None, is_service=None):
    if group in FIXED_RECTS:
        return FIXED_RECTS[group]
    if group in ("pan_bubble", "pot_bubble"):
        return (g.get(group + "_x"), g.get(group + "_y"), 72, 48)
    expanded = expanded_expr(g, group, mask)
    if group == "orders":
        return (984, 96, 276, g.math("SelectFloat", A=284.0, B=104.0, bPickA=expanded))
    if group == "stations":
        h = g.math("SelectFloat", A=260.0, B=116.0, bPickA=expanded)
        return (20, g.math("Subtract_DoubleDouble", A=684.0, B=h), 284, h)
    if group == "inventory":
        service = g.get("is_service") if is_service is None else is_service
        orders_h = g.math("SelectFloat", A=284.0, B=104.0, bPickA=expanded_expr(g, "orders", mask))
        y = g.math("SelectFloat", A=g.math("Add_DoubleDouble", A=108.0, B=orders_h), B=96.0, bPickA=service)
        h = g.math("SelectFloat", A=164.0, B=80.0, bPickA=expanded)
        return (984, y, 276, h)
    raise KeyError(group)


def visible_expr(g, group):
    if group in ("orders", "stations"):
        return g.get("is_service")
    if group in ("pan_bubble", "pot_bubble"):
        return g.get(group + "_visible")
    if group == "control_dock":
        return g.math("BooleanAND", A=g.math("Not_PreBool", A=g.get("is_beach")), B=g.math("Not_PreBool", A=g.get("closing")))
    if group == "prompt":
        hint = g.math("Greater_IntInt", A=g.pure("/Script/Engine.KismetStringLibrary.Len", S=g.get("interaction_hint")), B=0)
        notice = g.math("Greater_IntInt", A=g.pure("/Script/Engine.KismetStringLibrary.Len", S=g.get("notice_text")), B=0)
        normal = g.math("EqualEqual_IntInt", A=g.get("screen_id"), B=0)
        return g.math("BooleanOR", A=hint, B=g.math("BooleanAND", A=normal, B=notice))
    return g.math("EqualEqual_IntInt", A=g.get("screen_id"), B=g.get("screen_id"))


def button_rect_expr(g, button, mask=None, is_service=None):
    group = button.get("hud_group")
    if group:
        x, y, w, h = rect_expr(g, group, mask, is_service)
        return (x, y, w, g.math("SelectFloat", A=40.0, B=h, bPickA=expanded_expr(g, group, mask)))
    return tuple(button[key] for key in ("x", "y", "w", "h"))
