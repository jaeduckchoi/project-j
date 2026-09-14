"""Author the restaurant prototype's saved, Blueprint-only Canvas HUD.

No Python runs during play. Coordinates and action IDs are shared with the
PlayerController through BUTTONS. The manager owns gameplay and copies the
public HUD_FIELDS presentation state; the HUD never mutates gameplay state.
"""
from __future__ import annotations

from hud_layout import (
    REFERENCE_SIZE, BUTTONS, GROUPS, GROUP_BITS, resolve_rect, resolve_button_rect,
    rect_expr, visible_expr, expanded_expr, button_rect_expr,
)
HUD_PATH = "/Game/Jonggu/Prototype/BP_PrototypeHUD"
# Project-owned runtime composite font, with a fixed reference size. CoreStyle
# supplies NanumGothic for Korean in the editor; PIE is this prototype's target.
FONT_ASSET_PATH = "/Game/Jonggu/Prototype/F_PrototypeUI"
FONT_PATH = FONT_ASSET_PATH + ".F_PrototypeUI"

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

# Defaults are a compact service snapshot for documentation. Runtime rendering
# and pointer input always use hud_layout's state-dependent rectangle resolver.
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


def popup_color(hex_color, alpha=1.0):
    """Convert authored sRGB popup palette to linear Canvas vertex colors."""
    channels = [int(hex_color.lstrip("#")[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    linear = [value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4 for value in channels]
    return "(R=%.8f,G=%.8f,B=%.8f,A=%.8f)" % (*linear, alpha)


def nine_slice_patches(x, y, width, height, source_size=32, border=8):
    """Nine destination rectangles and normalized UVs; corners never stretch."""
    if width < border * 2 or height < border * 2 or source_size <= border * 2:
        raise ValueError("Nine-slice dimensions must leave a positive centre")
    positions_x, positions_y = (x, x + border, x + width - border), (y, y + border, y + height - border)
    sizes_x, sizes_y = (border, width - border * 2, border), (border, height - border * 2, border)
    uv = (0.0, border / source_size, 1.0 - border / source_size)
    uv_sizes = (border / source_size, 1.0 - 2.0 * border / source_size, border / source_size)
    return [(positions_x[col], positions_y[row], sizes_x[col], sizes_y[row], uv[col], uv[row], uv_sizes[col], uv_sizes[row])
            for row in range(3) for col in range(3)]


def build_ui():
    import unreal
    from bp_helpers import ensure_bp, add_vars, compile_bp, save_bp
    from data_types import RECIPES
    from build_hud_visibility import VISIBILITY_FIELDS, update_visibility
    from build_popup_assets import (
        build_popup_assets, FRAME_TEXTURE, CARD_TEXTURE, CLOSE_TEXTURE,
        FOOD_TEXTURES, POPUP_FONT_REGULAR, POPUP_FONT_BOLD,
        HUD_FRAME_TEXTURE, HUD_CARD_TEXTURE, HUD_PROMPT_TEXTURE, HUD_COIN_TEXTURE,
    )

    build_popup_assets()
    recipes_by_id = {recipe["recipe_id"]: recipe for recipe in RECIPES}
    recipe_labels = recipe_button_labels(RECIPES)
    display_buttons = [dict(button, label=recipe_labels.get(button["id"], button["label"])) for button in BUTTONS]
    special = next(recipe for recipe in RECIPES if recipe["recipe_id"] == 3)
    special_hint = f'조개 {special["clam_cost"]}개로 찌개 {special["portions"]}그릇! 해변은 방문하지 않아도 됩니다.'

    assets = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    font = assets.load_asset(FONT_ASSET_PATH) if assets.does_asset_exist(FONT_ASSET_PATH) else None
    if font is None:
        font = assets.duplicate_asset("/Engine/EngineFonts/Roboto", FONT_ASSET_PATH)
    if font is None:
        raise RuntimeError("Cannot create prototype UI font")
    font.set_editor_property("font_cache_type", unreal.FontCacheType.RUNTIME)
    font.set_editor_property("runtime_font_source", unreal.RuntimeFontSource.CORE_STYLE_DEFAULT)
    font.set_editor_property("legacy_font_size", 26)
    if not assets.save_loaded_asset(font, only_if_is_dirty=False):
        raise RuntimeError("Cannot save prototype UI font")

    B = unreal.BlueprintEditorLibrary
    bp = ensure_bp("BP_PrototypeHUD", unreal.HUD)
    add_vars(bp, HUD_FIELDS + [
        ("ui_scale_x", "real", 1.0), ("ui_scale_y", "real", 1.0),
        ("draw_count", "int", 0), ("draw_size_x", "int", 0), ("draw_size_y", "int", 0),
    ] + VISIBILITY_FIELDS)
    editor = unreal.BlueprintGraphEditor.get_graph_editor(B.find_event_graph(bp))
    editor.remove_nodes(editor.list_all_nodes())
    compile_bp(bp)

    class DrawGraph:
        def __init__(self):
            self.nodes = []
            self.tails = []
            self.active_hud_group = None

        def place(self, node):
            if node is None:
                raise RuntimeError("Cannot create HUD Blueprint node")
            index = len(self.nodes)
            node.set_node_pos(unreal.IntPoint((index % 10) * 280, (index // 10) * 210))
            self.nodes.append(node)
            return node

        def connect(self, source, target):
            if not source.is_valid() or not target.is_valid() or not source.try_create_connection(target):
                raise RuntimeError("HUD pin connection failed: %s -> %s" % (source, target))

        def literal(self, pin, value):
            value = str(value).lower() if isinstance(value, bool) else str(value)
            if not pin.is_valid() or not pin.set_pin_value(value):
                raise RuntimeError("HUD literal failed: %s = %s" % (pin, value))

        def fn(self, path, **inputs):
            node = self.place(editor.add_call_function_node(path))
            # Connect typed sources first to specialize promoted math pins.
            for name, value in sorted(inputs.items(), key=lambda item: not isinstance(item[1], unreal.BlueprintGraphPin)):
                pin = node.find_input_pin(name)
                if isinstance(value, unreal.BlueprintGraphPin):
                    self.connect(value, pin)
                else:
                    self.literal(pin, value)
            return node

        def pure(self, path, **inputs):
            return self.fn(path, **inputs).find_output_pin("ReturnValue")

        def math(self, name, **inputs):
            return self.pure("/Script/Engine.KismetMathLibrary." + name, **inputs)

        def get(self, name):
            return self.place(editor.add_get_member_variable_node(name)).find_output_pin(name)

        def emit(self, node):
            for tail in self.tails:
                self.connect(tail, node.find_execute_pin())
            self.tails = [node.find_then_pin()]
            return node

        def set(self, name, value):
            node = self.place(editor.add_set_member_variable_node(name))
            if isinstance(value, unreal.BlueprintGraphPin):
                self.connect(value, node.find_input_pin(name))
            else:
                self.literal(node.find_input_pin(name), value)
            return self.emit(node)

        def when(self, condition, draw):
            branch = self.place(editor.add_branch_node())
            self.connect(condition, branch.find_input_pin("Condition"))
            self.emit(branch)
            self.tails = [branch.find_output_pin("then")]
            draw()
            self.tails = self.tails + [branch.find_output_pin("else")]

        def rect(self, x, y, w, h, color):
            return self.emit(self.fn("/Script/Engine.HUD.DrawRect", RectColor=color,
                ScreenX=self.math("Multiply_DoubleDouble", A=x, B=self.get("ui_scale_x")),
                ScreenY=self.math("Multiply_DoubleDouble", A=y, B=self.get("ui_scale_y")),
                ScreenW=self.math("Multiply_DoubleDouble", A=w, B=self.get("ui_scale_x")),
                ScreenH=self.math("Multiply_DoubleDouble", A=h, B=self.get("ui_scale_y"))))

        def text(self, value, x, y, size=0.7, color="(R=0.94,G=0.95,B=0.92,A=1)"):
            return self.emit(self.fn("/Script/Engine.HUD.DrawText", Text=value, TextColor=color,
                ScreenX=self.math("Multiply_DoubleDouble", A=x, B=self.get("ui_scale_x")),
                ScreenY=self.math("Multiply_DoubleDouble", A=y, B=self.get("ui_scale_y")),
                Font=FONT_PATH,
                Scale=self.math("Multiply_DoubleDouble", A=size, B=self.math("FMin", A=self.get("ui_scale_x"), B=self.get("ui_scale_y"))),
                bScalePosition=False))

        def join(self, *parts):
            value = parts[0] if parts else ""
            for part in parts[1:]:
                value = self.pure("/Script/Engine.KismetStringLibrary.Concat_StrStr", A=value, B=part)
            return value

        def int_text(self, value):
            return self.pure("/Script/Engine.KismetStringLibrary.Conv_IntToString", InInt=value)

        def texture(self, texture, x, y, w, h, u=0.0, v=0.0, uw=1.0, vh=1.0, alpha=1.0):
            tint = self.math("MakeColor", R=1.0, G=1.0, B=1.0, A=alpha)
            return self.emit(self.fn("/Script/Engine.HUD.DrawTexture", Texture=texture,
                ScreenX=self.math("Multiply_DoubleDouble", A=x, B=self.get("ui_scale_x")),
                ScreenY=self.math("Multiply_DoubleDouble", A=y, B=self.get("ui_scale_y")),
                ScreenW=self.math("Multiply_DoubleDouble", A=w, B=self.get("ui_scale_x")),
                ScreenH=self.math("Multiply_DoubleDouble", A=h, B=self.get("ui_scale_y")),
                TextureU=u, TextureV=v, TextureUWidth=uw, TextureVHeight=vh,
                TintColor=tint, Scale=1.0, bScalePosition=False, Rotation=0.0))

        def nine_slice(self, texture, x, y, w, h):
            for patch in nine_slice_patches(x, y, w, h):
                self.texture(texture, *patch)

        def popup_rect(self, x, y, w, h, color, alpha=1.0):
            self.rect(x, y, w, h, popup_color(color, alpha))

        def popup_text(self, value, x, y, px=22, bold=False, color="#1F1F1F"):
            if isinstance(color, str) and color.startswith("#"):
                color = popup_color(color)
            self.emit(self.fn("/Script/Engine.HUD.DrawText", Text=value, TextColor=color,
                ScreenX=self.math("Multiply_DoubleDouble", A=x, B=self.get("ui_scale_x")),
                ScreenY=self.math("Multiply_DoubleDouble", A=y, B=self.get("ui_scale_y")),
                Font=POPUP_FONT_BOLD if bold else POPUP_FONT_REGULAR,
                Scale=self.math("Multiply_DoubleDouble", A=px / 32.0,
                                B=self.math("FMin", A=self.get("ui_scale_x"), B=self.get("ui_scale_y"))),
                bScalePosition=False))

        def popup_centered(self, value, x, y, w, h, px=22, bold=False, color="#1F1F1F"):
            measure = self.fn("/Script/Engine.HUD.GetTextSize", Text=value,
                              Font=POPUP_FONT_BOLD if bold else POPUP_FONT_REGULAR, Scale=px / 32.0)
            if measure.find_execute_pin().is_valid():
                self.emit(measure)
            left = self.math("Add_DoubleDouble", A=x, B=self.math("Divide_DoubleDouble",
                A=self.math("Subtract_DoubleDouble", A=w, B=measure.find_output_pin("OutWidth")), B=2.0))
            top = self.math("Add_DoubleDouble", A=y, B=self.math("Divide_DoubleDouble",
                A=self.math("Subtract_DoubleDouble", A=h, B=measure.find_output_pin("OutHeight")), B=2.0))
            self.popup_text(value, left, top, px, bold, color)

        def popup_outline(self, x, y, w, h, color, thickness=2):
            self.popup_rect(x, y, w, thickness, color)
            self.popup_rect(x, y + h - thickness, w, thickness, color)
            self.popup_rect(x, y + thickness, thickness, h - thickness * 2, color)
            self.popup_rect(x + w - thickness, y + thickness, thickness, h - thickness * 2, color)

        def popup_button(self, button):
            x, y, w, h = (button[key] for key in ("x", "y", "w", "h"))
            enabled = self.get(button["enabled_field"]) if button.get("enabled_field") else True
            hover = self.math("EqualEqual_IntInt", A=self.get("hovered_action"), B=button["id"])
            pressed = self.math("EqualEqual_IntInt", A=self.get("pressed_action"), B=button["id"])
            active_hover = self.math("BooleanAND", A=hover, B=enabled)
            active_press = self.math("BooleanAND", A=pressed, B=enabled)
            if button.get("presentation") == "close":
                self.when(active_hover, lambda: self.popup_rect(x - 2, y - 2, w + 4, h + 4, "#F3ECDB"))
                self.when(active_press, lambda: self.popup_rect(x - 2, y - 2, w + 4, h + 4, "#B9AD8F"))
                # The original 32px sprite has a 7px transparent margin.
                # Crop its occupied 18px square to show a 32px control glyph.
                self.texture(CLOSE_TEXTURE, x + 4, y + 4, w - 8, h - 8,
                             u=7.0 / 32.0, v=7.0 / 32.0, uw=18.0 / 32.0, vh=18.0 / 32.0)
                return

            self.nine_slice(CARD_TEXTURE, x, y, w, h)
            fill = "#37583E" if button.get("primary") else "#F3ECDB"
            self.popup_rect(x + 8, y + 8, w - 16, h - 16, fill)
            selected = None
            if "selected_mask" in button:
                selected = self.selected(button["selected_mask"])
            elif "selected_guests" in button:
                selected = self.math("EqualEqual_IntInt", A=self.get("guest_limit"), B=button["selected_guests"])
            if selected is not None:
                self.when(selected, lambda: self.popup_rect(x + 8, y + 8, w - 16, h - 16, "#E5E9CE"))
                self.when(selected, lambda: self.popup_rect(x + 8, y + 8, 4, h - 16, "#37583E"))
            self.when(active_hover, lambda: self.popup_rect(x + 8, y + 8, w - 16, h - 16,
                "#486B50" if button.get("primary") else "#FBF7EC"))
            if selected is not None:
                self.when(self.math("BooleanAND", A=active_hover, B=selected),
                          lambda: self.popup_rect(x + 8, y + 8, w - 16, h - 16, "#EDF0DB"))
            self.when(active_hover, lambda: self.popup_outline(x + 3, y + 3, w - 6, h - 6, "#AE8642", 2))
            self.when(active_press, lambda: self.popup_rect(x + 8, y + 8, w - 16, h - 16,
                "#284331" if button.get("primary") else "#DFD1AF"))
            self.when(self.math("Not_PreBool", A=enabled),
                      lambda: self.popup_rect(x + 8, y + 8, w - 16, h - 16, "#D5CEBD"))
            color = self.math("SelectColor", A=popup_color("#FAF6EB" if button.get("primary") else "#1F1F1F"),
                              B=popup_color("#817767"), bPickA=enabled)
            if "recipe_id" in button:
                recipe = recipes_by_id[button["recipe_id"]]
                is_cooking = button.get("presentation") == "cook_recipe"
                alpha = self.math("SelectFloat", A=1.0, B=0.5, bPickA=enabled)
                self.texture(FOOD_TEXTURES[recipe["dish_id"]], x + 16, y + (24 if is_cooking else 8), 64, 64, alpha=alpha)
                self.popup_text(recipe["display_name"], x + 96, y + (18 if is_cooking else 14), 22, True, color)
                appliance = "팬" if recipe["appliance"] == 0 else "냄비"
                detail = f'{appliance}  ·  {recipe["cook_seconds"]:g}초  ·  {recipe["portions"]}그릇'
                if recipe["clam_cost"]:
                    detail += f'  ·  조개 {recipe["clam_cost"]}개'
                self.popup_text(detail, x + 96, y + (49 if is_cooking else 46), 18, color="#665B47")
                if recipe["recipe_id"] == 3:
                    self.popup_rect(x + w - 82, y + 16, 62, 26, "#E6CB87")
                    self.popup_centered("특선", x + w - 82, y + 16, 62, 26, 18, True, "#624A1D")
                if is_cooking:
                    state_text = self.math("SelectString", A="클릭하여 조리 시작", B=self.get(button["reason_field"]), bPickA=enabled)
                    self.popup_text(state_text, x + 96, y + 78, 18, color="#665B47")
                elif selected is not None:
                    def selected_mark():
                        # Pixel strokes remain legible without relying on a font glyph.
                        for dx, dy in ((0, 8), (4, 12), (8, 8), (12, 4), (16, 0)):
                            self.popup_rect(x + w - 116 + dx, y + 29 + dy, 4, 4, "#37583E")
                        self.popup_text("선택됨", x + w - 92, y + 30, 18, True, "#37583E")
                    self.when(selected, selected_mark)
            else:
                self.popup_centered(button["label"], x + 12, y, w - 24, h, 22, bool(button.get("primary")), color)

        def offset(self, value, delta):
            if isinstance(value, unreal.BlueprintGraphPin) or isinstance(delta, unreal.BlueprintGraphPin):
                return self.math("Add_DoubleDouble", A=value, B=delta)
            return value + delta

        def hud_alpha(self, foreground=False, base=1.0):
            if self.active_hud_group is None:
                return base
            amount = self.get("hudalpha_" + self.active_hud_group)
            factor = self.math("Subtract_DoubleDouble", A=1.0,
                B=self.math("Multiply_DoubleDouble", A=amount, B=0.35 if foreground else 0.8))
            return self.math("Multiply_DoubleDouble", A=factor, B=base)

        def hud_color(self, color, foreground=False, base=1.0):
            channels = [int(color.lstrip("#")[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
            r, gg, b = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in channels]
            return self.math("MakeColor", R=r, G=gg, B=b, A=self.hud_alpha(foreground, base))

        def hud_rect(self, x, y, w, h, color, foreground=False, base=1.0):
            self.rect(x, y, w, h, self.hud_color(color, foreground, base))

        def hud_nine_slice(self, texture, x, y, w, h, source_size=32):
            xx = (x, self.offset(x, 8), self.offset(x, self.offset(w, -8)))
            yy = (y, self.offset(y, 8), self.offset(y, self.offset(h, -8)))
            ww, hh = (8, self.offset(w, -16), 8), (8, self.offset(h, -16), 8)
            uv = (0.0, 8.0 / source_size, 1.0 - 8.0 / source_size)
            uv_size = (8.0 / source_size, 1.0 - 16.0 / source_size, 8.0 / source_size)
            alpha = self.hud_alpha()
            for row in range(3):
                for col in range(3):
                    self.texture(texture, xx[col], yy[row], ww[col], hh[row],
                                 uv[col], uv[row], uv_size[col], uv_size[row], alpha)

        def hud_group(self, name, draw):
            # Authoring-only context: alpha pins are scoped to this group.
            # Restoring it ensures popup nodes never inherit HUD fading.
            previous = self.active_hud_group
            self.active_hud_group = name
            self.when(visible_expr(self, name), draw)
            self.active_hud_group = previous

        def hud_panel(self, x, y, w, h, fill="#3D260D", card=False):
            self.hud_rect(self.offset(x, 3), self.offset(y, 3), self.offset(w, -6), self.offset(h, -6), fill, base=0.96)
            if self.active_hud_group in GROUP_BITS:
                action = {"orders": 31, "stations": 32, "inventory": 33}[self.active_hud_group]
                self.when(self.math("EqualEqual_IntInt", A=self.get("hovered_action"), B=action),
                    lambda: self.hud_rect(self.offset(x, 3), self.offset(y, 3), self.offset(w, -6), self.offset(h, -6), "#634A2B", base=0.96))
                self.when(self.math("EqualEqual_IntInt", A=self.get("pressed_action"), B=action),
                    lambda: self.hud_rect(self.offset(x, 3), self.offset(y, 3), self.offset(w, -6), self.offset(h, -6), "#281908", base=0.96))
            self.hud_nine_slice(HUD_CARD_TEXTURE if card else HUD_FRAME_TEXTURE, x, y, w, h)

        def hud_text(self, value, x, y, px=18, bold=False, color="#F3ECDB"):
            self.popup_text(value, x, y, px, bold, self.hud_color(color, True))

        def hud_centered(self, value, x, y, w, h, px=18, bold=False, color="#F3ECDB"):
            self.popup_centered(value, x, y, w, h, px, bold, self.hud_color(color, True))

        def hud_fold_mark(self, group, x, y, w):
            xx, yy = self.offset(x, self.offset(w, -28)), self.offset(y, 17)
            def arrow(up):
                for dx, dy in ((0, 0), (2, 2), (4, 4), (6, 4), (8, 2), (10, 0)):
                    self.hud_rect(self.offset(xx, dx), self.offset(yy, 4 - dy if up else dy),
                                  2, 2, "#FFEDA1", True)
            expanded = expanded_expr(self, group)
            self.when(expanded, lambda: arrow(True))
            self.when(self.math("Not_PreBool", A=expanded), lambda: arrow(False))

        def dish_name(self, dish):
            name = self.math("SelectString", A=recipes_by_id[2]["display_name"], B=recipes_by_id[0]["display_name"],
                             bPickA=self.math("EqualEqual_IntInt", A=dish, B=2))
            name = self.math("SelectString", A=recipes_by_id[1]["display_name"], B=name,
                             bPickA=self.math("EqualEqual_IntInt", A=dish, B=1))
            return self.math("SelectString", A=name, B="", bPickA=self.math("GreaterEqual_IntInt", A=dish, B=0))

        def dish_icon(self, dish, x, y, size=40):
            for dish_id in (0, 1, 2):
                self.when(self.math("EqualEqual_IntInt", A=dish, B=dish_id),
                          lambda dish_id=dish_id: self.texture(FOOD_TEXTURES[dish_id], x, y, size, size, alpha=self.hud_alpha(True)))

        def hud_badge(self, x, y):
            self.hud_rect(x, y, 52, 24, "#E6CB87")
            self.hud_centered("특선", x, y, 52, 24, 18, True, "#3D260D")

        def hud_button(self, button):
            x, y, w, h = button_rect_expr(self, button)
            palette = {
                10: ("#F0BD1A", "#FFD75B", "#C69415"),
                20: ("#8ACC40", "#B2DF70", "#689D2B"),
                21: ("#C4AF86", "#E6CB87", "#A28A61"),
                30: ("#3D260D", "#634A2B", "#281908"),
            }
            base, hover, pressed = palette[button["id"]]
            self.hud_panel(x, y, w, h, base, True)
            self.when(self.math("EqualEqual_IntInt", A=self.get("hovered_action"), B=button["id"]),
                      lambda: self.hud_rect(x + 4, y + 4, w - 8, h - 8, hover))
            self.when(self.math("EqualEqual_IntInt", A=self.get("pressed_action"), B=button["id"]),
                      lambda: self.hud_rect(x + 4, y + 4, w - 8, h - 8, pressed))
            label = button["label"]
            if button["id"] == 20:
                label = self.math("SelectString", A="손님 받기 재개", B="손님 받기 중단", bPickA=self.get("intake_paused"))
            self.hud_centered(label, x + 8, y, w - 16, h, 20, True,
                              "#F3ECDB" if button["id"] == 30 else "#1F1F1F")

        def selected(self, mask):
            return self.math("NotEqual_IntInt", A=self.math("And_IntInt", A=self.get("menu_mask"), B=mask), B=0)

        def button(self, button):
            x, y, w, h = (button[k] for k in ("x", "y", "w", "h"))
            base = "(R=0.17,G=0.23,B=0.24,A=1)"
            accent = "(R=0.13,G=0.39,B=0.31,A=1)"
            if button.get("primary"):
                base = accent
            self.rect(x, y, w, h, base)
            selected = None
            if "selected_mask" in button:
                selected = self.selected(button["selected_mask"])
            elif "selected_guests" in button:
                selected = self.math("EqualEqual_IntInt", A=self.get("guest_limit"), B=button["selected_guests"])
            if selected is not None:
                self.when(selected, lambda: self.rect(x, y, w, h, accent))
                self.when(selected, lambda: self.rect(x, y, 4, h, "(R=0.88,G=0.73,B=0.36,A=1)"))
            label = button["label"]
            if button["id"] == 20:
                label = self.pure("/Script/Engine.KismetMathLibrary.SelectString", A="손님 받기 재개", B="손님 받기 중단", bPickA=self.get("intake_paused"))
            text_color = "(R=0.94,G=0.95,B=0.92,A=1)"
            if "requires_mask" in button:
                # Keep controls visible to explain unavailable choices. Gameplay
                # rejects unselected recipes with a notice and no resource cost.
                self.when(self.math("Not_PreBool", A=self.selected(button["requires_mask"])),
                          lambda: self.rect(x, y, w, h, "(R=0.105,G=0.13,B=0.14,A=1)"))
            self.text(label, x + 16, y + 10, 0.66, text_color)

        def screen(self, number, draw):
            self.when(self.math("EqualEqual_IntInt", A=self.get("screen_id"), B=number), draw)

    g = DrawGraph()
    event = B.add_event_override(bp, "ReceiveDrawHUD", unreal.IntPoint(-800, 0))
    g.tails = [event.find_then_pin()]
    g.set("draw_count", g.math("Add_IntInt", A=g.get("draw_count"), B=1))
    g.set("draw_size_x", event.find_output_pin("SizeX"))
    g.set("draw_size_y", event.find_output_pin("SizeY"))
    # Promoted arithmetic nodes specialize from connected input pins. Feeding
    # SizeX/SizeY directly makes integer division (1189 / 1280 = 0), hiding the
    # entire HUD in smaller windows. Explicitly convert before division.
    width = g.math("Conv_IntToDouble", InInt=event.find_output_pin("SizeX"))
    height = g.math("Conv_IntToDouble", InInt=event.find_output_pin("SizeY"))
    g.set("ui_scale_x", g.math("Divide_DoubleDouble", A=width, B=1280.0))
    g.set("ui_scale_y", g.math("Divide_DoubleDouble", A=height, B=720.0))
    update_visibility(g)

    # Shared layout drives both rendering and interaction. Folded groups retain
    # their essential information; details use the session-only expansion mask.
    has_notice = g.math("Greater_IntInt", A=g.pure("/Script/Engine.KismetStringLibrary.Len", S=g.get("notice_text")), B=0)
    O = g.offset

    def day_phase():
        g.hud_panel(*rect_expr(g, "day_phase"))
        phase = g.math("SelectString", A="마감 정리", B=g.math("SelectString", A="손님 받기 중단", B="영업 중", bPickA=g.get("intake_paused")), bPickA=g.get("closing"))
        phase = g.math("SelectString", A=phase, B=g.math("SelectString", A="해변", B="영업 준비", bPickA=g.get("is_beach")), bPickA=g.get("is_service"))
        g.hud_text(g.join(g.int_text(g.get("day")), "일차 · ", phase), 36, 30, 22, True, "#FFEDA1")
    g.hud_group("day_phase", day_phase)

    def revenue():
        g.hud_panel(*rect_expr(g, "revenue"))
        g.texture(HUD_COIN_TEXTURE, 980, 28, 28, 28, alpha=g.hud_alpha(True))
        g.hud_text("누적 매출", 1022, 32, 18, color="#FFEDA1")
        g.hud_text(g.int_text(g.get("display_revenue")), 1126, 29, 22, True, "#FFEDA1")
    g.hud_group("revenue", revenue)

    def orders():
        x, y, w, h = rect_expr(g, "orders")
        g.hud_panel(x, y, w, h, card=True)
        title = g.join("주문 ", g.int_text(g.get("guest_count")), "/", g.int_text(g.get("guest_limit")), " · 서빙 ", g.int_text(g.get("served_count")))
        g.hud_text(title, x + 16, y + 10, 18, True, "#FFEDA1")
        g.hud_fold_mark("orders", x, y, w)
        expanded = expanded_expr(g, "orders")
        def compact():
            for index in (0, 1):
                prefix, yy = "order" + str(index), y + 38 + index * 31
                g.dish_icon(g.get(prefix + "_dish_id"), x + 16, yy, 28)
                state = g.math("SelectString", A=g.join(g.int_text(g.math("FCeil", A=g.get(prefix + "_wait"))), "초"), B="빈 좌석", bPickA=g.get(prefix + "_active"))
                g.hud_text(g.join(str(index + 1), "번 · ", state), x + 56, yy + 4, 18)
        def detailed():
            for index in (0, 1):
                prefix, yy = "order" + str(index), y + 48 + index * 114
                g.hud_text(str(index + 1) + "번 좌석", x + 16, yy, 18, True, "#FFEDA1")
                g.dish_icon(g.get(prefix + "_dish_id"), x + 16, yy + 28, 40)
                name = g.math("SelectString", A=g.dish_name(g.get(prefix + "_dish_id")), B="빈 좌석", bPickA=g.get(prefix + "_active"))
                g.hud_text(name, x + 68, yy + 24, 18, True)
                g.when(g.get(prefix + "_active"), lambda prefix=prefix, yy=yy:
                    g.hud_text(g.join("기다린 시간 ", g.int_text(g.math("FCeil", A=g.get(prefix + "_wait"))), "초"), x + 68, yy + 56))
            g.hud_rect(x + 16, y + 150, w - 32, 1, "#746042")
        g.when(expanded, detailed)
        g.when(g.math("Not_PreBool", A=expanded), compact)
    g.hud_group("orders", orders)

    def stations():
        x, y, w, h = rect_expr(g, "stations")
        g.hud_panel(x, y, w, h, card=True)
        g.hud_text("조리 기구", x + 16, O(y, 10), 18, True, "#FFEDA1")
        g.hud_fold_mark("stations", x, y, w)
        expanded = expanded_expr(g, "stations")
        for index, (prefix, title) in enumerate((("pan", "팬"), ("pot", "냄비"))):
            remaining, portions, busy = g.get(prefix + "_remaining"), g.get(prefix + "_portions"), g.get(prefix + "_busy")
            ready = g.math("BooleanAND", A=g.math("LessEqual_DoubleDouble", A=remaining, B=0.0), B=g.math("Greater_IntInt", A=portions, B=0))
            def compact(prefix=prefix, title=title, index=index, busy=busy, ready=ready, portions=portions):
                yy = O(y, 38 + index * 38)
                g.dish_icon(g.get(prefix + "_dish_id"), x + 16, yy, 28)
                g.hud_text(title, x + 56, O(yy, 4), 18, True, "#FFEDA1")
                state = g.math("SelectString", A=g.join(g.int_text(portions), "그릇 완료"), B="조리 중", bPickA=ready)
                state = g.math("SelectString", A=state, B="비어 있음", bPickA=busy)
                g.hud_text(state, x + 96, O(yy, 4), 18)
                g.when(g.get(prefix + "_is_special"), lambda: g.hud_badge(x + w - 68, O(yy, 1)))
            def detailed(prefix=prefix, title=title, index=index, busy=busy, ready=ready, portions=portions, remaining=remaining):
                yy = O(y, 44 + index * 112)
                g.hud_text(title, x + 16, O(yy, 4), 22, True, "#FFEDA1")
                g.dish_icon(g.get(prefix + "_dish_id"), x + 16, O(yy, 30), 36)
                name = g.math("SelectString", A=g.dish_name(g.get(prefix + "_dish_id")), B="비어 있음", bPickA=busy)
                g.hud_text(name, x + 64, O(yy, 29), 18, True)
                state = g.math("SelectString", A=g.join(g.int_text(portions), "그릇 · E로 수령"), B=g.join(g.int_text(g.math("FCeil", A=remaining)), "초 남음"), bPickA=ready)
                state = g.math("SelectString", A=state, B="미리 조리 가능", bPickA=busy)
                g.hud_text(state, x + 64, O(yy, 55), 18)
                g.when(g.get(prefix + "_is_special"), lambda: g.hud_badge(x + w - 68, O(yy, 4)))
                g.hud_rect(x + 16, O(yy, 84), w - 32, 5, "#746042")
                progress = g.math("FClamp", Value=g.get(prefix + "_progress"), Min=0.0, Max=1.0)
                g.hud_rect(x + 16, O(yy, 84), g.math("Multiply_DoubleDouble", A=progress, B=float(w - 32)), 5, "#E6CB87", True)
            g.when(expanded, detailed)
            g.when(g.math("Not_PreBool", A=expanded), compact)
    g.hud_group("stations", stations)

    def inventory():
        x, y, w, h = rect_expr(g, "inventory")
        g.hud_panel(x, y, w, h)
        expanded = expanded_expr(g, "inventory")
        g.hud_fold_mark("inventory", x, y, w)
        holding = g.math("GreaterEqual_IntInt", A=g.get("held_dish_id"), B=0)
        held_name = g.math("SelectString", A=g.dish_name(g.get("held_dish_id")), B="빈손", bPickA=holding)
        clams = g.join("조개 ", g.int_text(g.get("clams")), "개")
        def compact():
            g.hud_text(clams, x + 16, O(y, 10), 22, True, "#FFEDA1")
            g.dish_icon(g.get("held_dish_id"), x + 16, O(y, 42), 28)
            g.hud_text(held_name, x + 56, O(y, 46), 18, True)
            g.when(g.get("held_is_special"), lambda: g.hud_badge(x + w - 68, O(y, 42)))
        def detailed():
            g.hud_text("조개와 접시", x + 16, O(y, 10), 18, True, "#FFEDA1")
            g.hud_text(clams, x + 16, O(y, 52), 22, True, "#FFEDA1")
            g.hud_rect(x + 16, O(y, 88), w - 32, 1, "#B9AD8F")
            g.hud_text("들고 있는 음식", x + 16, O(y, 98), 18, color="#C4AF86")
            g.dish_icon(g.get("held_dish_id"), x + 16, O(y, 116), 40)
            g.hud_text(held_name, x + 68, O(y, 122), 18, True)
            g.when(g.get("held_is_special"), lambda: g.hud_badge(x + w - 68, O(y, 52)))
        g.when(expanded, detailed)
        g.when(g.math("Not_PreBool", A=expanded), compact)
    g.hud_group("inventory", inventory)

    def prompt():
        x, y, w, h = rect_expr(g, "prompt")
        g.hud_nine_slice(HUD_PROMPT_TEXTURE, x, y, w, h, source_size=34)
        normal_notice = g.math("BooleanAND", A=has_notice, B=g.math("EqualEqual_IntInt", A=g.get("screen_id"), B=0))
        text = g.math("SelectString", A=g.get("notice_text"), B=g.get("interaction_hint"), bPickA=normal_notice)
        g.hud_centered(text, x + 16, y + 2, w - 32, h - 4, 18, False, "#3B4557")
    g.hud_group("prompt", prompt)

    def controls():
        g.hud_panel(*rect_expr(g, "control_dock"), card=True)
        for button in display_buttons:
            if button["id"] not in (10, 20, 21):
                continue
            def draw(button=button):
                condition = g.math("BooleanAND", A=g.math("Not_PreBool", A=g.get("is_service")), B=g.math("Not_PreBool", A=g.get("is_beach"))) if button["id"] == 10 else g.math("BooleanAND", A=g.get("is_service"), B=g.math("Not_PreBool", A=g.get("closing")))
                g.when(condition, lambda: g.hud_button(button))
            g.screen(0, draw)
    g.hud_group("control_dock", controls)

    def pause_dock():
        g.hud_panel(*rect_expr(g, "pause_dock"), card=True)
        g.screen(0, lambda: g.hud_button(next(button for button in display_buttons if button["id"] == 30)))
    g.hud_group("pause_dock", pause_dock)

    def cooking_bubble(prefix):
        x, y, w, _ = rect_expr(g, prefix + "_bubble")
        # A small pixel tail points at the projected world anchor. This is a
        # display-only cooking state; it does not create a new interaction.
        for offset, tail_w in ((0, 12), (2, 8), (4, 4), (6, 2)):
            g.hud_rect(O(x, 36 - tail_w / 2), O(y, 40 + offset), tail_w, 2, "#1F1F1F")
            if tail_w > 4:
                g.hud_rect(O(x, 38 - tail_w / 2), O(y, 40 + offset), tail_w - 4, 2, "#F3ECDB")
        g.hud_nine_slice(HUD_PROMPT_TEXTURE, x, y, 72, 40, source_size=34)
        g.hud_rect(O(x, 32), O(y, 38), 8, 3, "#F3ECDB")
        remaining = g.get(prefix + "_remaining")
        cooking_now = g.math("Greater_DoubleDouble", A=remaining, B=0.0)
        text = g.math("SelectString", A=g.join(g.int_text(g.math("FCeil", A=remaining)), "초"), B=g.join(g.int_text(g.get(prefix + "_portions")), "그릇"), bPickA=cooking_now)
        g.hud_centered(text, O(x, 8), O(y, 4), 56, 20, 18, True, "#3B4557")
        g.hud_rect(O(x, 8), O(y, 28), 56, 6, "#B9AD8F")
        progress = g.math("FClamp", Value=g.get(prefix + "_progress"), Min=0.0, Max=1.0)
        bar_color = g.math("SelectColor", A=g.hud_color("#E6CB87", True), B=g.hud_color("#8ACC40", True), bPickA=cooking_now)
        g.rect(O(x, 8), O(y, 28), g.math("Multiply_DoubleDouble", A=progress, B=56.0), 6, bar_color)
    for prefix in ("pan", "pot"):
        g.hud_group(prefix + "_bubble", lambda prefix=prefix: cooking_bubble(prefix))
    g.hud_text("WASD 이동 · E 상호작용", 20, 693, 18)

    # Modal dimming covers the whole game view, including the normal HUD.
    g.when(g.math("Greater_IntInt", A=g.get("screen_id"), B=0),
           lambda: g.popup_rect(0, 0, 1280, 720, "#000000", 0.52))

    def popup_frame(screen, title, subtitle):
        x, y, w, h = POPUP_PANELS[screen]
        g.popup_rect(x + 4, y + 6, w, h, "#000000", 0.18)
        g.nine_slice(FRAME_TEXTURE, x, y, w, h)
        g.popup_text(title, x + 32, y + 28, 32, True)
        g.popup_text(subtitle, x + 32, y + 67, 18, color="#665B47")

    def popup_buttons(screen):
        for button in display_buttons:
            if button["screen"] != screen:
                continue
            if button.get("condition") == "can_retry":
                g.when(g.get("can_retry"), lambda button=button: g.popup_button(button))
            else:
                g.popup_button(button)

    def popup_notice(x, y, w, value=None):
        def draw():
            g.popup_rect(x, y, w, 28, "#F5E4B8")
            g.popup_rect(x, y, 3, 28, "#AE8642")
            g.popup_text(g.get("notice_text") if value is None else value, x + 12, y + 4, 18, color="#624A1D")
        g.when(has_notice, draw)

    def paper_panel(x, y, w, h):
        g.nine_slice(CARD_TEXTURE, x, y, w, h)
        g.popup_rect(x + 8, y + 8, w - 16, h - 16, "#F3ECDB")

    def prep():
        popup_frame(1, "오늘의 메뉴", "먹고 싶은 한 끼를 골라요. 메뉴는 1~2종까지 선택할 수 있어요.")
        paper_panel(832, 200, 336, 264)
        g.popup_text("오늘의 손님", 848, 214, 22, True)
        g.popup_text("선택한 메뉴", 848, 330, 22, True)
        rice_selected, soup_selected, pancake_selected = g.selected(1), g.selected(2), g.selected(4)
        rice_name = g.math("SelectString", A=recipes_by_id[0]["display_name"], B="", bPickA=rice_selected)
        soup_name = g.math("SelectString", A=g.join(g.math("SelectString", A=" · ", B="", bPickA=rice_selected), recipes_by_id[1]["display_name"]), B="", bPickA=soup_selected)
        previous = g.math("BooleanOR", A=rice_selected, B=soup_selected)
        pancake_name = g.math("SelectString", A=g.join(g.math("SelectString", A=" · ", B="", bPickA=previous), recipes_by_id[2]["display_name"]), B="", bPickA=pancake_selected)
        g.popup_text(g.join(rice_name, soup_name, pancake_name), 848, 360, 18, color="#665B47")
        pan_used = g.math("BooleanOR", A=rice_selected, B=pancake_selected)
        shared_pan = g.math("BooleanAND", A=rice_selected, B=pancake_selected)
        appliance_text = g.math("SelectString", A="팬 공유 · 2종을 차례로 조리", B=g.math("SelectString", A="팬 + 냄비 · 함께 조리 가능", B=g.math("SelectString", A="냄비 사용", B="팬 사용", bPickA=soup_selected), bPickA=g.math("BooleanAND", A=pan_used, B=soup_selected)), bPickA=shared_pan)
        g.popup_text(appliance_text, 848, 393, 18, color="#665B47")
        g.popup_text(g.join("조개 ", g.int_text(g.get("clams")), f'개 보유 · 특선 {special["portions"]}그릇'), 848, 426, 18, color="#665B47")
        g.popup_rect(112, 524, 1056, 1, "#9C8769", 0.55)
        g.popup_text("선택한 메뉴로 손님을 맞이합니다.", 112, 562, 18, color="#665B47")
        popup_buttons(1)
        popup_notice(112, 484, 1056)
    g.screen(1, prep)

    def cooking(screen, prefix, title):
        popup_frame(screen, title, "음식을 누르면 조리가 시작됩니다. 선택창을 열어도 영업 시간은 흐릅니다.")
        paper_panel(832, 200, 336, 276)
        remaining = g.get(prefix + "_remaining")
        portions = g.get(prefix + "_portions")
        cooking_now = g.math("Greater_DoubleDouble", A=remaining, B=0.0)
        has_food = g.math("Greater_IntInt", A=portions, B=0)
        status = g.math("SelectString", A="조리 중", B=g.math("SelectString", A="수령 가능", B="비어 있음", bPickA=has_food), bPickA=cooking_now)
        value = g.math("SelectString", A=g.join(g.int_text(g.math("FCeil", A=remaining)), "초 남음"),
                       B=g.math("SelectString", A=g.join(g.int_text(portions), "그릇 완성"), B="사용 가능", bPickA=has_food), bPickA=cooking_now)
        g.popup_text("현재 " + ("팬" if prefix == "pan" else "냄비"), 856, 218, 22, True)
        g.popup_text(status, 856, 258, 18, color="#665B47")
        g.popup_text(value, 856, 291, 32, True)
        g.popup_rect(856, 340, 288, 8, "#D5CAB3")
        progress = g.math("FClamp", Value=g.get(prefix + "_progress"), Min=0.0, Max=1.0)
        g.popup_rect(856, 340, g.math("Multiply_DoubleDouble", A=progress, B=288.0), 8, "#37583E")
        g.popup_text(g.join("조개 ", g.int_text(g.get("clams")), "개 보유"), 856, 363, 18, color="#665B47")
        g.when(g.math("Not_PreBool", A=g.get("can_clear_" + prefix)),
               lambda: g.popup_text("완성된 음식이 없습니다", 856, 444, 18, color="#817767"))
        g.when(g.get("can_clear_" + prefix),
               lambda: g.popup_text("비운 음식은 돌아오지 않아요" if prefix == "pan" else "사용한 조개는 돌려받지 못해요", 856, 444, 18, color="#665B47"))
        g.popup_rect(112, 524, 1056, 1, "#9C8769", 0.55)
        g.popup_text("완성품은 E로 한 그릇씩 수령해요. 타거나 상하지 않아요.", 112, 562, 18, color="#665B47")
        popup_buttons(screen)
        popup_notice(112, 484, 1056)
    g.screen(2, lambda: cooking(2, "pan", "팬에서 조리하기"))
    g.screen(3, lambda: cooking(3, "pot", "냄비에서 조리하기"))

    def pause():
        popup_frame(4, "잠시 쉬어가기", "조리와 손님 도착, 대기 시간이 모두 멈췄습니다.")
        g.popup_text("Esc 또는 F10으로도 계속할 수 있어요.", 332, 280, 18, color="#665B47")
        popup_buttons(4)
        g.when(g.get("can_retry"), lambda: g.popup_text("다시 시작하면 개점 직전 메뉴·재고·매출로 돌아갑니다.", 332, 464, 18, color="#665B47"))
        popup_notice(332, 492, 616)
    g.screen(4, pause)

    def stat(label, value, x, y, w, unit=""):
        paper_panel(x, y, w, 88)
        g.popup_text(label, x + 16, y + 12, 18, color="#665B47")
        g.popup_text(g.join(g.int_text(value), unit), x + 16, y + 32, 32, True)

    def summary():
        popup_frame(5, "오늘의 영업 결산", "수고했어요. 오늘 대접한 따뜻한 한 끼를 돌아봐요.")
        stat("대접한 식사", g.get("summary_served"), 252, 224, 232, "그릇")
        paper_panel(500, 224, 528, 88)
        g.popup_text("오늘의 매출", 516, 236, 18, color="#665B47")
        daily = g.math("Add_IntInt", A=g.get("summary_base"), B=g.get("summary_bonus"))
        g.popup_text(g.int_text(daily), 516, 256, 32, True)
        g.popup_text(g.join("기본 ", g.int_text(g.get("summary_base")), " + 추가 ", g.int_text(g.get("summary_bonus"))), 704, 269, 18, color="#665B47")
        stat("사용한 조개", g.get("summary_clams_used"), 252, 328, 232, "개")
        stat("특선으로 대접", g.get("summary_special_served"), 516, 328, 232, "그릇")
        stat("누적 매출", g.get("total_revenue"), 780, 328, 248)
        g.popup_text("남은 조개는 보관하고, 해변의 조개는 내일 다시 생겨요.", 252, 446, 18, color="#665B47")
        g.popup_rect(252, 518, 776, 1, "#9C8769", 0.55)
        popup_buttons(5)
        summary_notice = g.math("SelectString", A="오늘의 영업을 마쳤습니다.", B=g.get("notice_text"),
            bPickA=g.pure("/Script/Engine.KismetStringLibrary.EqualEqual_StrStr", A=g.get("notice_text"), B="접수한 주문을 마치면 오늘 영업이 끝납니다."))
        popup_notice(252, 482, 776, summary_notice)
    g.screen(5, summary)

    compile_bp(bp)
    defaults = unreal.get_default_object(bp.generated_class())
    defaults.set_editor_property("show_hud", True)
    save_bp(bp)
    return bp


if __name__ == "__main__":
    build_ui()
