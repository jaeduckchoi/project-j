"""Saved local player input controller with aspect-fitted Canvas pointer coordinates."""


def _function_node(g, path, **inputs):
    """UE exposes some getters as impure; execute those without assuming flags."""
    node = g.fn(path, **inputs)
    if node.find_execute_pin().is_valid():
        g.emit(node)
    return node


def build_controller(manager_bp):
    import unreal
    from jonggu.blueprints.graph import G, ensure_bp, add_vars, compile_bp, save_bp, class_path
    bp = ensure_bp('BP_RestaurantPlayerController', unreal.PlayerController)
    add_vars(bp, [('pointer_x', 'real', -1.0), ('pointer_y', 'real', -1.0)])
    compile_bp(bp)
    manager_class = class_path(manager_bp)

    begin = G.event(bp, 'ReceiveBeginPlay')
    begin.call('/Script/Engine.Actor.SetTickableWhenPaused', bTickableWhenPaused=True)
    begin.call('/Script/Engine.Actor.SetActorTickEnabled', bEnabled=True)
    begin.guard(begin.pure('/Script/Engine.Controller.IsLocalController'))
    player = _function_node(begin, '/Script/Engine.GameplayStatics.GetPlayerController', PlayerIndex=0).find_output_pin('ReturnValue')
    begin.call('/Script/UMG.WidgetBlueprintLibrary.SetInputMode_GameAndUIEx',
               PlayerController=player, InWidgetToFocus=None, InMouseLockMode='DoNotLock',
               bHideCursorDuringCapture=False, bFlushInput=True)

    tick = G.event(bp, 'ReceiveTick')
    tick.guard(tick.pure('/Script/Engine.Controller.IsLocalController'))
    manager = _function_node(tick, '/Script/Engine.GameplayStatics.GetActorOfClass',
                             ActorClass=manager_class).find_output_pin('ReturnValue')
    tick.guard(tick.pure('/Script/Engine.KismetSystemLibrary.IsValid', Object=manager))

    def pressed(g, key):
        return _function_node(g, '/Script/Engine.PlayerController.WasInputKeyJustPressed', Key=key).find_output_pin('ReturnValue')

    # Sample independently from action priority. Missing cursor/black bars clear
    # pointer state without placing a guard on the Escape/E execution path.
    tick.set('pointer_x', -1.0)
    tick.set('pointer_y', -1.0)
    cursor = _function_node(tick, '/Script/Engine.PlayerController.GetMousePosition')
    viewport = _function_node(tick, '/Script/Engine.PlayerController.GetViewportSize')
    width, height = viewport.find_output_pin('SizeX'), viewport.find_output_pin('SizeY')
    x, y = cursor.find_output_pin('LocationX'), cursor.find_output_pin('LocationY')
    valid_size = tick.math('BooleanAND', A=tick.math('Greater_IntInt', A=width, B=0),
                           B=tick.math('Greater_IntInt', A=height, B=0))
    valid_cursor = tick.math('BooleanAND', A=cursor.find_output_pin('ReturnValue'), B=valid_size)

    def normalize_pointer(g):
        # SourceCamera constrains its ViewRect to 16:9. Canvas HUD coordinates
        # belong to that fitted rectangle; native mouse getters use the window.
        full_w = g.math('Conv_IntToDouble', InInt=width)
        full_h = g.math('Conv_IntToDouble', InInt=height)
        scale = g.math('FMin', A=g.math('Divide_DoubleDouble', A=full_w, B=1280.0),
                        B=g.math('Divide_DoubleDouble', A=full_h, B=720.0))
        offset_x = g.math('Multiply_DoubleDouble',
                          A=g.math('Subtract_DoubleDouble', A=full_w,
                                   B=g.math('Multiply_DoubleDouble', A=1280.0, B=scale)), B=0.5)
        offset_y = g.math('Multiply_DoubleDouble',
                          A=g.math('Subtract_DoubleDouble', A=full_h,
                                   B=g.math('Multiply_DoubleDouble', A=720.0, B=scale)), B=0.5)
        mx = g.math('Divide_DoubleDouble', A=g.math('Subtract_DoubleDouble', A=x, B=offset_x), B=scale)
        my = g.math('Divide_DoubleDouble', A=g.math('Subtract_DoubleDouble', A=y, B=offset_y), B=scale)
        inside_x = g.math('BooleanAND', A=g.math('GreaterEqual_DoubleDouble', A=mx, B=0.0),
                          B=g.math('Less_DoubleDouble', A=mx, B=1280.0))
        inside_y = g.math('BooleanAND', A=g.math('GreaterEqual_DoubleDouble', A=my, B=0.0),
                          B=g.math('Less_DoubleDouble', A=my, B=720.0))
        g.when(g.math('BooleanAND', A=inside_x, B=inside_y),
               lambda h: (h.set('pointer_x', mx), h.set('pointer_y', my)))

    tick.when(valid_cursor, normalize_pointer)
    down = _function_node(tick, '/Script/Engine.PlayerController.IsInputKeyDown', Key='LeftMouseButton').find_output_pin('ReturnValue')
    tick.call(manager_class + '.UpdatePointer', self=manager,
              mx=tick.get('pointer_x'), my=tick.get('pointer_y'), pressed=down)

    def click(g):
        g.call(manager_class + '.Click', self=manager, mx=g.get('pointer_x'), my=g.get('pointer_y'))

    # Exclusive priority prevents one key opening a modal and a second key
    # activating it in the same frame. No gameplay action runs without a manager.
    pause_pressed = tick.math('BooleanOR', A=pressed(tick, 'Escape'), B=pressed(tick, 'F10'))
    tick.when(pause_pressed,
              lambda g: g.call(manager_class + '.PauseToggle', self=manager),
              lambda g: g.when(pressed(g, 'E'),
                  lambda h: h.call(manager_class + '.Interact', self=manager),
                  lambda h: h.when(pressed(h, 'LeftMouseButton'), click)))
    compile_bp(bp)
    defaults = unreal.get_default_object(bp.generated_class())
    defaults.set_editor_property('show_mouse_cursor', True)
    defaults.set_editor_property('enable_click_events', False)
    defaults.set_editor_property('enable_mouse_over_events', False)
    defaults.set_editor_property('should_perform_full_tick_when_paused', True)
    save_bp(bp)
    return bp
