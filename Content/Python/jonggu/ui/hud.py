"""Build the saved, Blueprint-only restaurant HUD.

Importing this module is host-safe. Explicit build_ui() owns editor mutations;
Canvas primitives, normal HUD and popup panels have separate source modules.
Public contracts are re-exported for existing manager and QA callers.
"""
from jonggu.ui.layout import (
    REFERENCE_SIZE, BUTTONS, GROUPS, GROUP_BITS, resolve_rect, resolve_button_rect,
    rect_expr, visible_expr, expanded_expr, button_rect_expr,
)
from jonggu.ui.contracts import (
    HUD_PATH, HUD_FIELDS, HUD_RECTS, POPUP_PANELS,
    visible_button, enabled_button, hit_test, recipe_button_labels,
)
from jonggu.ui.theme import popup_color, nine_slice_patches


def build_ui():
    import unreal
    from jonggu.blueprints.graph import ensure_bp, add_vars, compile_bp, save_bp
    from jonggu.gameplay.data import RECIPES
    from jonggu.ui.visibility import VISIBILITY_FIELDS, update_visibility
    from jonggu.ui.assets import build_popup_assets
    from jonggu.ui.primitives import DrawGraph
    from jonggu.ui.renderer import render_hud
    from jonggu.ui.panels import render_popups

    build_popup_assets()
    recipes_by_id = {recipe["recipe_id"]: recipe for recipe in RECIPES}
    recipe_labels = recipe_button_labels(RECIPES)
    display_buttons = [dict(button, label=recipe_labels.get(button["id"], button["label"])) for button in BUTTONS]

    B = unreal.BlueprintEditorLibrary
    bp = ensure_bp("BP_RestaurantHUD", unreal.HUD)
    add_vars(bp, HUD_FIELDS + [
        ("ui_scale_x", "real", 1.0), ("ui_scale_y", "real", 1.0),
        ("draw_count", "int", 0), ("draw_size_x", "int", 0), ("draw_size_y", "int", 0),
    ] + VISIBILITY_FIELDS)
    editor = unreal.BlueprintGraphEditor.get_graph_editor(B.find_event_graph(bp))
    editor.remove_nodes(editor.list_all_nodes())
    compile_bp(bp)

    g = DrawGraph(unreal, editor, recipes_by_id)
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
    render_hud(g, display_buttons, has_notice)
    render_popups(g, display_buttons, recipes_by_id, has_notice)

    compile_bp(bp)
    defaults = unreal.get_default_object(bp.generated_class())
    defaults.set_editor_property("show_hud", True)
    save_bp(bp)
    return bp


if __name__ == "__main__":
    build_ui()
