"""Explicit Canvas Blueprint authoring primitives.

The constructor receives the Unreal API and target graph editor. Keeping those
editor dependencies out of module import allows host-only layout validation.
Graph construction order and typed-pin-first wiring match the original HUD.
"""
from jonggu.ui.layout import GROUP_BITS, visible_expr, expanded_expr, button_rect_expr
from jonggu.ui.theme import popup_color, nine_slice_patches
from jonggu.ui.assets import (
    CARD_TEXTURE, CLOSE_TEXTURE, FOOD_TEXTURES, POPUP_FONT_REGULAR,
    POPUP_FONT_BOLD, HUD_CARD_TEXTURE, HUD_FRAME_TEXTURE,
)


class DrawGraph:
    def __init__(self, unreal_api, editor, recipes_by_id):
        self.unreal = unreal_api
        self.editor = editor
        self.recipes_by_id = recipes_by_id
        self.nodes = []
        self.tails = []
        self.active_hud_group = None


    def place(self, node):
        if node is None:
            raise RuntimeError("Cannot create HUD Blueprint node")
        index = len(self.nodes)
        node.set_node_pos(self.unreal.IntPoint((index % 10) * 280, (index // 10) * 210))
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
        node = self.place(self.editor.add_call_function_node(path))
        # Connect typed sources first to specialize promoted math pins.
        for name, value in sorted(inputs.items(), key=lambda item: not isinstance(item[1], self.unreal.BlueprintGraphPin)):
            pin = node.find_input_pin(name)
            if isinstance(value, self.unreal.BlueprintGraphPin):
                self.connect(value, pin)
            else:
                self.literal(pin, value)
        return node

    def pure(self, path, **inputs):
        return self.fn(path, **inputs).find_output_pin("ReturnValue")

    def math(self, name, **inputs):
        return self.pure("/Script/Engine.KismetMathLibrary." + name, **inputs)

    def get(self, name):
        return self.place(self.editor.add_get_member_variable_node(name)).find_output_pin(name)

    def emit(self, node):
        for tail in self.tails:
            self.connect(tail, node.find_execute_pin())
        self.tails = [node.find_then_pin()]
        return node

    def set(self, name, value):
        node = self.place(self.editor.add_set_member_variable_node(name))
        if isinstance(value, self.unreal.BlueprintGraphPin):
            self.connect(value, node.find_input_pin(name))
        else:
            self.literal(node.find_input_pin(name), value)
        return self.emit(node)

    def when(self, condition, draw):
        branch = self.place(self.editor.add_branch_node())
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
            recipe = self.recipes_by_id[button["recipe_id"]]
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
        if isinstance(value, self.unreal.BlueprintGraphPin) or isinstance(delta, self.unreal.BlueprintGraphPin):
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
        name = self.math("SelectString", A=self.recipes_by_id[2]["display_name"], B=self.recipes_by_id[0]["display_name"],
                         bPickA=self.math("EqualEqual_IntInt", A=dish, B=2))
        name = self.math("SelectString", A=self.recipes_by_id[1]["display_name"], B=name,
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

    def screen(self, number, draw):
        self.when(self.math("EqualEqual_IntInt", A=self.get("screen_id"), B=number), draw)
