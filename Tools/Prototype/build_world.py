"""Author the prototype controller, reusable markers and additive map overlay.

Editor-only Python: the resulting controller/game mode/interaction getter are
saved Blueprint graphs. Source player, camera, art and collision are untouched.
"""
from pathlib import Path
import json
import math
import re

ROOT = Path(__file__).resolve().parents[2]
OWN_TAG = 'JongguPrototype'
ID_TAG = 'JongguPrototypeId:'
INTERACTION_IDS = {
    'hub_menu': 0, 'hub_pan': 1, 'hub_pot': 2, 'hub_discard': 3,
    'hub_beach_portal': 4, 'hub_seat_1': 5, 'hub_seat_2': 6,
    'beach_hub_portal': 7, 'beach_clam_1': 8, 'beach_clam_2': 9,
    'beach_clam_3': 10,
}


def _ini_values(text, values):
    """Change only named MapsSettings values, retaining unrelated bytes/newlines."""
    header = '[/Script/EngineSettings.GameMapsSettings]'
    match = re.search(r'^' + re.escape(header) + r'[ \t]*(?:\r?\n|$)', text, re.M)
    newline = '\r\n' if '\r\n' in text else '\n'
    if match is None:
        prefix = text + ('' if not text or text.endswith(('\n', '\r')) else newline)
        return prefix + header + newline + ''.join(k + '=' + v + newline for k, v in values.items())
    following = re.search(r'^\[', text[match.end():], re.M)
    end = match.end() + following.start() if following else len(text)
    body = text[match.end():end]
    for key, value in values.items():
        pattern = r'^([ \t]*' + re.escape(key) + r'[ \t]*=)[^\r\n]*'
        if re.search(pattern, body, re.M):
            body = re.sub(pattern, lambda m: m.group(1) + value, body, flags=re.M)
        else:
            body += ('' if not body or body.endswith(('\n', '\r')) else newline) + key + '=' + value + newline
    return text[:match.end()] + body + text[end:]


def _function_node(g, path, **inputs):
    """UE exposes some getters as impure; execute those without assuming flags."""
    node = g.fn(path, **inputs)
    if node.find_execute_pin().is_valid():
        g.emit(node)
    return node


def _controller(manager_bp):
    import unreal
    from bp_helpers import G, ensure_bp, add_vars, compile_bp, save_bp, class_path
    bp = ensure_bp('BP_PrototypeController', unreal.PlayerController)
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


def _marker_text_material():
    import unreal
    from bp_helpers import ASSETS, PREFIX
    path = PREFIX + '/M_PrototypeInteractionText'
    version = '1_unlit_vertex_color'
    material = ASSETS.load_asset(path) if ASSETS.does_asset_exist(path) else None
    if material is not None and ASSETS.get_metadata_tag(material, 'PrototypeTextVersion') == version:
        return material
    if material is None:
        material = ASSETS.duplicate_asset('/Engine/EngineMaterials/DefaultTextMaterialTranslucent', path)
    if not isinstance(material, unreal.Material):
        raise RuntimeError('Cannot create project-owned marker text material')
    edit = unreal.MaterialEditingLibrary
    base_color = unreal.MaterialProperty.MP_BASE_COLOR
    color_node = edit.get_material_property_input_node(material, base_color)
    if color_node is None: raise RuntimeError('Text material base-color input is missing')
    output_name = edit.get_material_property_input_node_output_name(material, base_color)
    if not edit.connect_material_property(color_node, output_name, unreal.MaterialProperty.MP_EMISSIVE_COLOR):
        raise RuntimeError('Cannot connect marker text color to emissive')
    material.set_editor_property('shading_model', unreal.MaterialShadingModel.MSM_UNLIT)
    errors = edit.recompile_material(material)
    if errors: raise RuntimeError('Marker text material compile failed: ' + '; '.join(str(e) for e in errors))
    ASSETS.set_metadata_tag(material, 'PrototypeTextVersion', version)
    if not ASSETS.save_loaded_asset(material, only_if_is_dirty=False):
        raise RuntimeError('Cannot save marker text material')
    return material


def _configure_label(label, text='E', gather=False, relative_y=8.0):
    import unreal
    # TextRender requires an OFFLINE font atlas. Its native opaque material
    # ignores translucent sort priority and can be covered by the source sprites.
    font = unreal.load_asset('/Engine/EngineFonts/RobotoDistanceField')
    material = _marker_text_material()
    if font is None or material is None: raise RuntimeError('Marker font/material is missing')
    label.set_mobility(unreal.ComponentMobility.MOVABLE)
    label.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
    label.set_cast_shadow(False)
    label.set_visibility(True, True)
    label.set_hidden_in_game(False, True)
    label.set_font(font)
    label.set_text_material(material)
    label.set_text(text)
    label.set_world_size(80.0)
    label.set_horizontal_alignment(unreal.HorizTextAligment.EHTA_CENTER)
    label.set_vertical_alignment(unreal.VerticalTextAligment.EVRTA_TEXT_CENTER)
    # Native TextRender local +X normal faces camera +Y after +90 yaw.
    label.set_relative_location(unreal.Vector(0.0, relative_y, 28.0), False, False)
    label.set_relative_rotation(unreal.Rotator(pitch=0.0, yaw=90.0, roll=0.0), False, False)
    label.set_text_render_color(unreal.Color(166, 239, 225, 255) if gather else unreal.Color(255, 226, 160, 255))
    label.set_translucent_sort_priority(20000)


def _marker_blueprint():
    import unreal
    from bp_helpers import G, B, ensure_bp, add_vars, declare_function, compile_bp, save_bp
    # Shared Blueprint base with a public interaction contract. UE5.8 Python
    # does not expose a supported API for editing implemented BPI interfaces.
    bp = ensure_bp('BP_PrototypeInteractable', unreal.Actor)
    add_vars(bp, [('interaction_id', 'int', -1)])
    B.set_blueprint_variable_instance_editable(bp, 'interaction_id', True)
    B.set_blueprint_variable_category(bp, 'interaction_id', 'Prototype')
    declare_function(bp, 'GetInteractionId', outputs={'id': 'int'}, pure=True)
    compile_bp(bp)
    getter = G(bp, 'GetInteractionId')
    getter.ret(id=getter.get('interaction_id'))

    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    library = unreal.SubobjectDataBlueprintFunctionLibrary
    handles = subsystem.k2_gather_subobject_data_for_blueprint(bp)
    context = handles[0]
    found = {}
    for handle in handles:
        obj = library.get_object_for_blueprint(library.get_data(handle), bp)
        if obj:
            for name in ('prototype_root', 'prototype_label'):
                if obj.get_name().startswith(name): found[name] = (handle, obj)

    def component(name, cls, parent):
        if name in found: return found[name]
        handle, reason = subsystem.add_new_subobject(unreal.AddNewSubobjectParams(
            parent_handle=parent, new_class=cls, blueprint_context=bp))
        if str(reason): raise RuntimeError('Cannot add marker component: ' + str(reason))
        subsystem.rename_subobject_member_variable(bp, handle, name)
        obj = library.get_object_for_blueprint(library.get_data(handle), bp)
        return handle, obj

    root_handle, root = component('prototype_root', unreal.SceneComponent, context)
    if 'prototype_root' not in found and not subsystem.make_new_scene_root(context, root_handle, bp):
        raise RuntimeError('Cannot set marker scene root')
    _, label = component('prototype_label', unreal.TextRenderComponent, root_handle)
    root.set_mobility(unreal.ComponentMobility.MOVABLE)
    _configure_label(label)
    save_bp(bp)
    return bp


def _source_snapshot(actors):
    result = {}
    for actor in actors:
        tags = [str(t) for t in actor.tags]
        identity = next((t for t in tags if t.startswith('JongguMigration:')), None)
        if identity is None: continue
        transform = actor.get_actor_transform()
        result[identity] = (
            actor.get_class().get_path_name(), tuple(tags),
            tuple(float(getattr(transform.translation, k)) for k in ('x', 'y', 'z')),
            tuple(float(getattr(transform.rotation, k)) for k in ('x', 'y', 'z', 'w')),
            tuple(float(getattr(transform.scale3d, k)) for k in ('x', 'y', 'z')),
            bool(actor.get_editor_property('hidden')))
    return result


def _upsert(actors_api, cls, key, position, actors):
    import unreal
    uclass = cls.static_class() if hasattr(cls, 'static_class') and not isinstance(cls, unreal.Class) else cls
    matches = [a for a in actors if OWN_TAG in [str(t) for t in a.tags] and ID_TAG + key in [str(t) for t in a.tags]]
    if key == 'manager' and not matches:
        matches = [a for a in actors if a.get_class() == uclass and not any(str(t).startswith('JongguMigration:') for t in a.tags)]
    actor = matches[0] if matches else None
    for duplicate in matches[1:]:
        if not actors_api.destroy_actor(duplicate): raise RuntimeError('Cannot remove duplicate prototype ' + key)
    if actor is not None and actor.get_class() != uclass:
        if not actors_api.destroy_actor(actor): raise RuntimeError('Cannot replace prototype ' + key)
        actor = None
    if actor is None:
        actor = actors_api.spawn_actor_from_class(cls, unreal.Vector(*position))
        if actor is None: raise RuntimeError('Cannot spawn prototype ' + key)
    actor.tags = [OWN_TAG, ID_TAG + key]
    actor.set_actor_label('Prototype_' + key.replace(':', '_'), mark_dirty=True)
    actor.set_folder_path('Prototype')
    actor.set_actor_location(unreal.Vector(*position), False, True)
    actor.set_actor_enable_collision(False)
    actor.set_actor_hidden_in_game(False)
    return actor


def build_world(manager_bp, hud_bp, session_bp):
    """Reapply only the dedicated prototype overlay and its MapsSettings keys."""
    import unreal
    from bp_helpers import B, ASSETS, ensure_bp, compile_bp, save_bp, class_path
    active = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active != ROOT: raise RuntimeError('Unexpected project: ' + str(active))
    layout = json.loads((ROOT / 'Tools/Prototype/layout.json').read_text(encoding='utf-8'))
    for bp in (manager_bp, hud_bp, session_bp): compile_bp(bp)
    manager_vars = {str(name) for name in B.list_member_variable_names(manager_bp, False)}
    required = {'is_beach', 'spawn_position', 'customer0', 'customer1', 'clam0', 'clam1', 'clam2'}
    if not required <= manager_vars:
        raise RuntimeError('Manager variables missing: ' + ', '.join(sorted(required - manager_vars)))
    controller_bp = _controller(manager_bp)
    marker_bp = _marker_blueprint()
    mode_bp = ensure_bp('BP_PrototypeGameMode', unreal.GameModeBase)
    compile_bp(mode_bp)
    mode = unreal.get_default_object(mode_bp.generated_class())
    mode.set_editor_property('default_pawn_class', None)
    mode.set_editor_property('hud_class', hud_bp.generated_class())
    mode.set_editor_property('player_controller_class', controller_bp.generated_class())
    save_bp(mode_bp)

    player_bp = ASSETS.load_asset('/Game/Jonggu/Blueprints/Player/BP_JongguPlayer')
    if player_bp is None: raise RuntimeError('Original player Blueprint is missing')
    front_frames = unreal.get_default_object(player_bp.generated_class()).get_editor_property('front_frames')
    if not front_frames or front_frames[0] is None: raise RuntimeError('Original front frame missing')
    front = front_frames[0]
    material = ASSETS.load_asset('/Game/Jonggu/Materials/MI_Jonggu_TranslucentUnlit')
    if material is None: raise RuntimeError('Original unlit sprite material missing')
    levels = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    actors_api = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    editor_world = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem)
    report = {'success': False, 'controller': class_path(controller_bp), 'game_mode': class_path(mode_bp),
              'interactable': class_path(marker_bp), 'interaction_contract': 'Shared Blueprint base: GetInteractionId', 'maps': []}

    for map_name, scene in layout['maps'].items():
        if not levels.load_level(scene['package']): raise RuntimeError('Cannot load ' + scene['package'])
        actors = list(actors_api.get_all_level_actors())
        before = _source_snapshot(actors)
        pawns = [a for a in actors if a.get_class() == player_bp.generated_class()]
        if len(pawns) != 1: raise RuntimeError('Expected one original placed player in ' + map_name)
        cameras = [a for a in actors if isinstance(a, unreal.CameraActor) and any(str(t).startswith('JongguMigration:') for t in a.tags)]
        if len(cameras) != 1: raise RuntimeError('Expected one original source camera in ' + map_name)
        camera_y = float(cameras[0].get_actor_location().y)
        visual = pawns[0].get_editor_property('player_visual')
        visual_scale = visual.get_world_scale()
        visual_rotation = visual.get_world_rotation()
        manager = _upsert(actors_api, manager_bp.generated_class(), 'manager', [0, 0, 0], actors)
        manager.set_editor_property('is_beach', map_name == 'Beach')
        spawn = scene['player_return_cm'] if map_name == 'Hub' else scene['player_spawn_cm']
        manager.set_editor_property('spawn_position', unreal.Vector(*spawn))
        for field in ('customer0', 'customer1', 'clam0', 'clam1', 'clam2'):
            manager.set_editor_property(field, None)
        expected = {'manager'}
        assigned_targets = []
        marker_rendering = []
        for row in scene['interactions']:
            interaction_id = INTERACTION_IDS[row['id']]
            key = 'marker:' + row['id']
            expected.add(key)
            marker = _upsert(actors_api, marker_bp.generated_class(), key, row['position_cm'], actors)
            marker.set_editor_property('interaction_id', interaction_id)
            # Assign instance fields before retrieving components: changing a BP
            # property can reconstruct SCS components and invalidate old handles.
            label = marker.get_component_by_class(unreal.TextRenderComponent)
            if label is None: raise RuntimeError('Marker TextRender component missing')
            # Keep the hotspot at its authored floor position. Only its visual
            # label moves toward the source camera to clear the sprite layers.
            _configure_label(label, '1' if interaction_id == 5 else '2' if interaction_id == 6 else 'E',
                             row['kind'] == 'gather', camera_y - 50.0 - float(row['position_cm'][1]))
            location = label.get_world_location()
            rotation = label.get_world_rotation()
            marker_rendering.append({'id': row['id'], 'text': str(label.get_editor_property('text')),
                'world_location': [float(getattr(location, axis)) for axis in ('x', 'y', 'z')],
                'world_rotation': [float(getattr(rotation, axis)) for axis in ('pitch', 'yaw', 'roll')],
                'font': label.get_editor_property('font').get_path_name(),
                'material': label.get_editor_property('text_material').get_path_name()})
            if map_name == 'Beach' and row['kind'] == 'gather':
                manager.set_editor_property('clam' + str(interaction_id - 8), marker)
            # Manager generators may bake the same layout constants into their
            # graphs. When explicit targetN members exist, initialize them too.
            field = 'target' + str(interaction_id)
            if field in manager_vars:
                manager.set_editor_property(field, unreal.Vector(*row['position_cm']))
                assigned_targets.append(field)

        for index, seat in enumerate(scene.get('seats', [])):
            key = 'customer:' + seat['id']
            expected.add(key)
            customer = _upsert(actors_api, unreal.PaperSpriteActor, key, seat['customer_position_cm'], actors)
            comp = customer.get_component_by_class(unreal.PaperSpriteComponent)
            comp.set_mobility(unreal.ComponentMobility.MOVABLE)
            comp.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
            comp.set_cast_shadow(False)
            comp.set_sprite(front)
            comp.set_material(0, material)
            comp.set_sprite_color(unreal.LinearColor(0.66, 0.84, 1.0, 1.0) if index == 0 else unreal.LinearColor(1.0, 0.72, 0.78, 1.0))
            customer.set_actor_scale3d(visual_scale)
            customer.set_actor_rotation(visual_rotation, True)
            comp.set_translucent_sort_priority(-math.floor(seat['customer_position_cm'][2] + 0.5))
            customer.set_actor_hidden_in_game(True)
            manager.set_editor_property('customer' + str(index), customer)

        # Only this generator's named overlay is reconciled. Other root-owned
        # prototype helpers and every source-tagged actor are retained.
        for actor in list(actors_api.get_all_level_actors()):
            tags = [str(t) for t in actor.tags]
            key = next((t[len(ID_TAG):] for t in tags if t.startswith(ID_TAG)), None)
            if OWN_TAG in tags and key and (key.startswith(('marker:', 'customer:')) or key == 'manager') and key not in expected:
                if not actors_api.destroy_actor(actor): raise RuntimeError('Cannot remove obsolete prototype ' + key)
        world = editor_world.get_editor_world()
        world.get_world_settings().set_editor_property('default_game_mode', mode_bp.generated_class())
        after = _source_snapshot(actors_api.get_all_level_actors())
        if before != after: raise RuntimeError('Source actor state changed in ' + map_name)
        if not levels.save_current_level(): raise RuntimeError('Cannot save prototype overlay in ' + map_name)
        report['maps'].append({'name': map_name, 'package': scene['package'],
                               'prototype_ids': sorted(expected), 'source_actors_preserved': len(before),
                               'spawn_position_cm': spawn, 'assigned_target_members': assigned_targets,
                               'marker_rendering': marker_rendering})

    # Update live config for this editor and the precise file keys for later
    # editor/PIE sessions. Never call SaveConfig, which would rewrite other keys.
    settings = unreal.GameMapsSettings.get_game_maps_settings()
    settings.set_editor_property('game_instance_class', unreal.SoftClassPath(class_path(session_bp)))
    settings.set_editor_property('global_default_game_mode', unreal.SoftClassPath(class_path(mode_bp)))
    config_path = ROOT / 'Config/DefaultEngine.ini'
    original = config_path.read_bytes()
    bom = original.startswith(b'\xef\xbb\xbf')
    config = original.decode('utf-8-sig')
    updated = _ini_values(config, {'GameInstanceClass': class_path(session_bp),
                                   'GlobalDefaultGameMode': class_path(mode_bp)})
    encoded = (b'\xef\xbb\xbf' if bom else b'') + updated.encode('utf-8')
    if encoded != original: config_path.write_bytes(encoded)
    if not levels.load_level(layout['maps']['Hub']['package']): raise RuntimeError('Cannot reopen Hub')
    report['success'] = True
    return report
