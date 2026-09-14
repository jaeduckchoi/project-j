"""Assemble game-owned actors in existing maps and wire the runtime game classes."""
from pathlib import Path
import json
import math

from jonggu.paths import ROOT, LAYOUT_FILE
from jonggu.world.actors import OWN_TAG, ID_TAG, INTERACTION_IDS, _source_snapshot, _upsert
from jonggu.world.config import _ini_values
from jonggu.world.controller import build_controller
from jonggu.world.interaction import build_interactable, configure_interaction_label


def build_world(manager_bp, hud_bp, session_bp):
    """Reapply only the dedicated prototype overlay and its MapsSettings keys."""
    import unreal
    from jonggu.blueprints.graph import B, ASSETS, ensure_bp, compile_bp, save_bp, class_path
    active = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active != ROOT: raise RuntimeError('Unexpected project: ' + str(active))
    layout = json.loads(LAYOUT_FILE.read_text(encoding='utf-8'))
    for bp in (manager_bp, hud_bp, session_bp): compile_bp(bp)
    manager_vars = {str(name) for name in B.list_member_variable_names(manager_bp, False)}
    required = {'is_beach', 'spawn_position', 'customer0', 'customer1', 'clam0', 'clam1', 'clam2'}
    if not required <= manager_vars:
        raise RuntimeError('Manager variables missing: ' + ', '.join(sorted(required - manager_vars)))
    controller_bp = build_controller(manager_bp)
    marker_bp = build_interactable()
    mode_bp = ensure_bp('BP_RestaurantGameMode', unreal.GameModeBase)
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
        manager = _upsert(actors_api, manager_bp.generated_class(), 'manager', [0, 0, 0], actors, map_name)
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
            marker = _upsert(actors_api, marker_bp.generated_class(), key, row['position_cm'], actors, map_name)
            marker.set_editor_property('interaction_id', interaction_id)
            # Assign instance fields before retrieving components: changing a BP
            # property can reconstruct SCS components and invalidate old handles.
            label = marker.get_component_by_class(unreal.TextRenderComponent)
            if label is None: raise RuntimeError('Marker TextRender component missing')
            # Keep the hotspot at its authored floor position. Only its visual
            # label moves toward the source camera to clear the sprite layers.
            configure_interaction_label(label, '1' if interaction_id == 5 else '2' if interaction_id == 6 else 'E',
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
            customer = _upsert(actors_api, unreal.PaperSpriteActor, key, seat['customer_position_cm'], actors, map_name)
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
