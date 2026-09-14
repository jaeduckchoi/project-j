"""Build the normal HUD groups in their established draw order."""
from jonggu.ui.layout import rect_expr, expanded_expr
from jonggu.ui.assets import HUD_COIN_TEXTURE, HUD_PROMPT_TEXTURE


def render_hud(g, display_buttons, has_notice):
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
