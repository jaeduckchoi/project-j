"""Build preparation, cooking, pause and summary popup graphs.

All gameplay and enabled-state decisions arrive through the HUD contract.
Popup drawing is intentionally independent from normal HUD occlusion fading.
"""
from jonggu.ui.contracts import POPUP_PANELS
from jonggu.ui.assets import FRAME_TEXTURE, CARD_TEXTURE


def render_popups(g, display_buttons, recipes_by_id, has_notice):
    special = recipes_by_id[3]
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
