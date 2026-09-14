"""Author Canvas-local player occlusion and cooking-bubble projections.

All expressions become saved Blueprint nodes. No Python executes in Play.
"""
from itertools import product
from hud_layout import GROUPS, rect_expr, visible_expr

ENTER_PADDING = 20.0
EXIT_PADDING = 32.0
FADE_SECONDS = 0.15
RESTORE_SECONDS = 0.25
ANCHOR_OFFSET_Z = 240.0
VISIBILITY_FIELDS = [
    ("player_bounds_valid", "bool", False),
    *[("player_bounds_" + side, "real", 0.0) for side in ("left", "top", "right", "bottom")],
    ("hud_delta", "real", 0.0),
]
for prefix in ("pan", "pot"):
    VISIBILITY_FIELDS += [(prefix + "_bubble_x", "real", -10000.0),
                          (prefix + "_bubble_y", "real", -10000.0),
                          (prefix + "_bubble_visible", "bool", False)]
for group in GROUPS:
    VISIBILITY_FIELDS += [("hudalpha_" + group, "real", 0.0),
                          ("hudoccluded_" + group, "bool", False),
                          ("hudhover_" + group, "bool", False)]


def _and(g, *values):
    result = values[0]
    for value in values[1:]:
        result = g.math("BooleanAND", A=result, B=value)
    return result


def _not(g, value):
    return g.math("Not_PreBool", A=value)


def _add(g, a, b):
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return a + b
    return g.math("Add_DoubleDouble", A=a, B=b)


def _sub(g, a, b):
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return a - b
    return g.math("Subtract_DoubleDouble", A=a, B=b)


def _point(g, controller, world):
    # bPlayerViewportRelative already removes ConstrainedViewRect.Min. These
    # local pixels share the HUD Canvas origin, unlike raw mouse/window pixels.
    node = g.fn("/Script/Engine.GameplayStatics.ProjectWorldToScreen",
                Player=controller, WorldPosition=world, bPlayerViewportRelative=True)
    split = g.fn("/Script/Engine.KismetMathLibrary.BreakVector2D",
                 InVec=node.find_output_pin("ScreenPosition"))
    x = g.math("Divide_DoubleDouble", A=split.find_output_pin("X"), B=g.get("ui_scale_x"))
    y = g.math("Divide_DoubleDouble", A=split.find_output_pin("Y"), B=g.get("ui_scale_y"))
    return x, y, node.find_output_pin("ReturnValue")


def update_visibility(g):
    """Called once by DrawHUD, after actual Canvas SizeX/Y and scale assignment."""
    normal = g.math("EqualEqual_IntInt", A=g.get("screen_id"), B=0)
    valid_size = _and(g, g.math("Greater_IntInt", A=g.get("draw_size_x"), B=0),
                        g.math("Greater_IntInt", A=g.get("draw_size_y"), B=0))
    controller = g.pure("/Script/Engine.GameplayStatics.GetPlayerController", PlayerIndex=0)
    valid_controller = g.pure("/Script/Engine.KismetSystemLibrary.IsValid", Object=controller)
    valid_player = g.pure("/Script/Engine.KismetSystemLibrary.IsValid", Object=g.get("player_actor"))
    g.set("player_bounds_valid", False)
    for side in ("left", "top", "right", "bottom"):
        g.set("player_bounds_" + side, 0.0)
    g.set("hud_delta", g.pure("/Script/Engine.GameplayStatics.GetWorldDeltaSeconds"))

    def project_player():
        bounds = g.fn("/Script/Engine.Actor.GetActorBounds",
                      bOnlyCollidingComponents=False, bIncludeFromChildActors=False)
        g.connect(g.get("player_actor"), bounds.find_input_pin("self"))
        if bounds.find_execute_pin().is_valid():
            g.emit(bounds)
        origin = g.fn("/Script/Engine.KismetMathLibrary.BreakVector", InVec=bounds.find_output_pin("Origin"))
        extent = g.fn("/Script/Engine.KismetMathLibrary.BreakVector", InVec=bounds.find_output_pin("BoxExtent"))
        g.set("player_bounds_valid", True)
        for side in ("left", "top"):
            g.set("player_bounds_" + side, 1000000.0)
        for side in ("right", "bottom"):
            g.set("player_bounds_" + side, -1000000.0)
        # The Pawn has separate head/body/feet walking sprites. Actor bounds
        # include their full visual extent, not just the small collision sphere.
        for signs in product((-1.0, 1.0), repeat=3):
            components = {}
            for axis, sign in zip(("X", "Y", "Z"), signs):
                components[axis] = _add(g, origin.find_output_pin(axis),
                    g.math("Multiply_DoubleDouble", A=extent.find_output_pin(axis), B=sign))
            world = g.math("MakeVector", **components)
            x, y, valid = _point(g, controller, world)
            g.set("player_bounds_valid", _and(g, g.get("player_bounds_valid"), valid))
            for side, value, function in (("left", x, "FMin"), ("top", y, "FMin"),
                                           ("right", x, "FMax"), ("bottom", y, "FMax")):
                g.set("player_bounds_" + side, g.math(function, A=g.get("player_bounds_" + side), B=value))
    g.when(_and(g, valid_size, valid_controller, valid_player), project_player)

    for prefix in ("pan", "pot"):
        g.set(prefix + "_bubble_visible", False)
        g.set(prefix + "_bubble_x", -10000.0)
        g.set(prefix + "_bubble_y", -10000.0)
        def project_bubble(prefix=prefix):
            x, y, valid = _point(g, controller, g.get(prefix + "_anchor"))
            g.set(prefix + "_bubble_x", _sub(g, x, 36.0))
            g.set(prefix + "_bubble_y", _sub(g, y, 48.0))
            # Offscreen anchors must not create a floating edge indicator.
            inside = _and(g, valid,
                g.math("GreaterEqual_DoubleDouble", A=x, B=36.0),
                g.math("LessEqual_DoubleDouble", A=x, B=1244.0),
                g.math("GreaterEqual_DoubleDouble", A=y, B=48.0),
                g.math("LessEqual_DoubleDouble", A=y, B=720.0))
            g.set(prefix + "_bubble_visible", inside)
        occupied = g.math("BooleanOR",
            A=g.math("Greater_DoubleDouble", A=g.get(prefix + "_remaining"), B=0.0),
            B=g.math("Greater_IntInt", A=g.get(prefix + "_portions"), B=0))
        g.when(_and(g, valid_size, valid_controller, g.get(prefix + "_anchor_valid"),
            g.get("is_service"), _not(g, g.get("is_beach")), occupied,
            g.math("NotEqual_IntInt", A=g.get("screen_id"), B=5)), project_bubble)

    for group in GROUPS:
        x, y, w, h = rect_expr(g, group)
        visible = visible_expr(g, group)
        pad = g.math("SelectFloat", A=EXIT_PADDING, B=ENTER_PADDING,
                     bPickA=g.get("hudoccluded_" + group))
        overlap = _and(g,
            g.math("Greater_DoubleDouble", A=_add(g, g.get("player_bounds_right"), pad), B=x),
            g.math("Less_DoubleDouble", A=_sub(g, g.get("player_bounds_left"), pad), B=_add(g, x, w)),
            g.math("Greater_DoubleDouble", A=_add(g, g.get("player_bounds_bottom"), pad), B=y),
            g.math("Less_DoubleDouble", A=_sub(g, g.get("player_bounds_top"), pad), B=_add(g, y, h)))
        hover = _and(g, visible, normal,
            g.math("GreaterEqual_DoubleDouble", A=g.get("pointer_x"), B=x),
            g.math("Less_DoubleDouble", A=g.get("pointer_x"), B=_add(g, x, w)),
            g.math("GreaterEqual_DoubleDouble", A=g.get("pointer_y"), B=y),
            g.math("Less_DoubleDouble", A=g.get("pointer_y"), B=_add(g, y, h)))
        can_fade = _and(g, normal, visible, g.get("player_bounds_valid"))
        g.set("hudoccluded_" + group, _and(g, can_fade, overlap))
        g.set("hudhover_" + group, hover)
        faded = _and(g, g.get("hudoccluded_" + group), _not(g, hover))
        target = g.math("SelectFloat", A=1.0, B=0.0, bPickA=faded)
        speed = g.math("SelectFloat", A=1.0 / FADE_SECONDS, B=1.0 / RESTORE_SECONDS, bPickA=faded)
        interpolated = g.math("FInterpTo_Constant", Current=g.get("hudalpha_" + group),
            Target=target, DeltaTime=g.get("hud_delta"), InterpSpeed=speed)
        g.set("hudalpha_" + group,
              g.math("SelectFloat", A=interpolated, B=0.0, bPickA=can_fade))
