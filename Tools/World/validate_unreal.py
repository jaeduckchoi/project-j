"""Read back saved migration assets/maps without changing or saving content.

Run inside UnrealEditor-Cmd with -run=pythonscript. Only the JSON validation
report is written. Geometry checks use an independent affine basis calculation,
including negative-scale decompositions, rather than importing the writer.
"""
from __future__ import annotations

from collections import Counter
import copy
import hashlib
import json
import math
from pathlib import Path
import sys
import time
import traceback

import unreal


HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))
from world_paths import FIXTURE_ROOT, CONTENT_ROOT, DATA_ROOT, QA_ROOT, require_unreal_project
ROOT = CONTENT_ROOT
PLAYER_BP = ROOT + "/Blueprints/Player/BP_JongguPlayer"
GAME_MODE_BP = ROOT + "/Blueprints/Game/BP_JongguGameMode"
COLLISION_BLUEPRINTS = {
    "box": ROOT + "/Blueprints/Collision/BP_CollisionBox",
    "sphere": ROOT + "/Blueprints/Collision/BP_CollisionSphere",
}
PLAYER_ROOTS = {"Hub": "Hub:457226398", "Beach": "Beach:100040"}
PLAYER_RENDERS = {"Hub": "Hub:655320554", "Beach": "Beach:100512"}
# Independent readback contract; do not import these regions from the writer.
WALK_PART_NAMES = ("head", "body", "left_foot", "right_foot")
WALK_REGIONS = {
    "front": ((0, 0, 61, 56), (0, 54, 61, 22), (18, 74, 12, 8), (30, 74, 12, 8)),
    "back": ((0, 0, 61, 54), (0, 52, 61, 24), (21, 74, 12, 8), (33, 74, 12, 8)),
    "side": ((0, 0, 61, 56), (0, 54, 61, 22), (18, 74, 14, 8), (18, 74, 14, 8)),
}
TAG = "JongguMigration:"
TOLERANCE_CM = 0.01
ASSETS = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
ACTORS = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
LEVELS = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
REPORT = {
    "schema_version": 1,
    "engine": unreal.SystemLibrary.get_engine_version(),
    "started": time.strftime("%Y-%m-%dT%H:%M:%S"),
    "read_only_content": True,
    "tolerances": {"geometry_cm": TOLERANCE_CM, "scale": 0.00001,
                   "canonical_rotation_degrees": 0.001,
                   "sprite_color": 0.000001, "grouped_color": 1 / 255 + 0.000001},
    "checks": 0, "failures": [], "scenes": [], "assets": {},
    "max_errors": {"position_cm": 0.0, "geometry_cm": 0.0,
                   "absolute_axis_scale": 0.0, "canonical_rotation_degrees": 0.0},
}


def check(condition, label, **details):
    REPORT["checks"] += 1
    if not condition:
        REPORT["failures"].append({"check": label, **details})
    return condition


def xyz(value):
    return [float(value.x), float(value.y), float(value.z)]


def rgba(value):
    return [float(value.r), float(value.g), float(value.b), float(value.a)]


def distance(a, b):
    return math.sqrt(sum((x-y)**2 for x, y in zip(a, b)))


def max_delta(a, b):
    return max((abs(x-y) for x, y in zip(a, b)), default=0.0)


def identity(actor):
    return next((str(tag)[len(TAG):] for tag in actor.tags if str(tag).startswith(TAG)), None)


def metadata(obj, key):
    return str(ASSETS.get_metadata_tag(obj, key))


def source_position(matrix, point=(0.0, 0.0, 0.0)):
    p = [sum(matrix[row][col] * point[col] for col in range(3)) + matrix[row][3]
         for row in range(3)]
    return [100 * p[0], -100 * p[2], 100 * p[1]]


def actual_basis(transform):
    """Quaternion rotation columns times scale; q and -q yield the same matrix."""
    q = transform.rotation
    x, y, z, w = float(q.x), float(q.y), float(q.z), float(q.w)
    norm = math.sqrt(x*x+y*y+z*z+w*w)
    x, y, z, w = [v / norm for v in (x, y, z, w)]
    scales = xyz(transform.scale3d)
    columns = [[1-2*(y*y+z*z), 2*(x*y+z*w), 2*(x*z-y*w)],
               [2*(x*y-z*w), 1-2*(x*x+z*z), 2*(y*z+x*w)],
               [2*(x*z+y*w), 2*(y*z-x*w), 1-2*(x*x+y*y)]]
    return [[v*scales[i] for v in column] for i, column in enumerate(columns)]


def transform_point(transform, basis, local_cm):
    origin = xyz(transform.translation)
    return [origin[row] + sum(basis[col][row]*local_cm[col] for col in range(3))
            for row in range(3)]


def compare_transform(context, matrix, transform, sprite=None):
    if transform is None:
        check(False, "missing_world_transform", context=context)
        return
    basis = actual_basis(transform)
    origin_error = distance(xyz(transform.translation), source_position(matrix))
    expected_x = [matrix[0][0], -matrix[2][0], matrix[1][0]]
    expected_z = [matrix[0][1], -matrix[2][1], matrix[1][1]]
    scale_error = max(abs(distance(basis[0], [0, 0, 0])-distance(expected_x, [0, 0, 0])),
                      abs(distance(basis[2], [0, 0, 0])-distance(expected_z, [0, 0, 0])))
    # Canonicalize on the geometric +X direction. FTransform may represent a
    # reflected matrix as either negative X or negative Z scale; both are valid.
    actual_angle = math.atan2(basis[0][2], basis[0][0])
    source_angle = math.atan2(expected_x[2], expected_x[0])
    angle_error = abs(math.degrees(math.atan2(math.sin(actual_angle-source_angle),
                                             math.cos(actual_angle-source_angle))))
    expected_quat = [0.0, -math.sin(source_angle / 2), 0.0, math.cos(source_angle / 2)]
    actual_quat = [0.0, -math.sin(actual_angle / 2), 0.0, math.cos(actual_angle / 2)]
    quat_delta = min(max_delta(expected_quat, actual_quat),
                     max_delta(expected_quat, [-v for v in actual_quat]))
    samples = [(0, 0), (1, 0), (0, 1)]
    if sprite:
        _, _, width, height = sprite["rect"]
        px, py = sprite["pivot"]
        ppu = sprite["ppu"]
        samples.extend((x, y) for x in (-px*width/ppu, (1-px)*width/ppu)
                       for y in (-py*height/ppu, (1-py)*height/ppu))
    geometry_error = max(distance(source_position(matrix, [x, y, 0]),
                                  transform_point(transform, basis, [100*x, 0, 100*y]))
                         for x, y in samples)
    for key, value in (("position_cm", origin_error), ("geometry_cm", geometry_error),
                       ("absolute_axis_scale", scale_error), ("canonical_rotation_degrees", angle_error)):
        REPORT["max_errors"][key] = max(REPORT["max_errors"][key], value)
    check(origin_error <= TOLERANCE_CM, "world_position", context=context, error_cm=origin_error)
    check(geometry_error <= TOLERANCE_CM, "world_affine_geometry", context=context, error_cm=geometry_error)
    check(scale_error <= REPORT["tolerances"]["scale"], "axis_scale", context=context, error=scale_error)
    check(angle_error <= 0.001 and quat_delta <= 0.00001, "rotation_sign_equivalence",
          context=context, error_degrees=angle_error, canonical_quaternion_delta=quat_delta)


def instance_transform(component, index):
    value = component.get_instance_transform(index, True)
    if isinstance(value, (list, tuple)):
        return next((item for item in value if isinstance(item, unreal.Transform)), None)
    return value


def validate_camera_preview(component, context):
    """Independently check the saved camera look; never import its mutator."""
    material_path = ROOT + "/Materials/M_Jonggu_PreviewDisplay"
    material = ASSETS.load_asset(material_path)
    settings = component.get_editor_property("post_process_settings")
    weighted = settings.get_editor_property("weighted_blendables").get_editor_property("array")
    matching = [entry for entry in weighted if entry.get_editor_property("object") == material and material is not None]
    blend_weight = float(component.get_editor_property("post_process_blend_weight"))
    check(abs(blend_weight-1) <= 0.000001, "camera_post_process_weight", actor=context, actual=blend_weight)
    check(len(weighted) == 1 and len(matching) == 1, "camera_single_preview_blendable", actor=context,
          total=len(weighted), matching=len(matching))
    for entry in matching:
        weight = float(entry.get_editor_property("weight"))
        check(abs(weight-1) <= 0.000001, "camera_preview_material_weight", actor=context, actual=weight)
    check(isinstance(material, unreal.Material), "camera_preview_material_exists", actor=context)
    if material:
        check(material.get_editor_property("material_domain") == unreal.MaterialDomain.MD_POST_PROCESS,
              "preview_material_post_process_domain", actor=context)
        check(material.get_editor_property("blendable_location") == unreal.BlendableLocation.BL_REPLACING_TONEMAPPER,
              "preview_material_replaces_tonemapper", actor=context)
    expected_fields = {
        "auto_exposure_method": unreal.AutoExposureMethod.AEM_MANUAL,
        "auto_exposure_bias": 0.0,
        "auto_exposure_apply_physical_camera_exposure": False,
        "auto_exposure_bias_curve": None,
        "vignette_intensity": 0.0,
        "bloom_intensity": 0.0,
        "motion_blur_amount": 0.0,
        "lens_flare_intensity": 0.0,
        "scene_fringe_intensity": 0.0,
        "film_grain_intensity": 0.0,
        "sharpen": 0.0,
        "local_exposure_highlight_contrast_scale": 1.0,
        "local_exposure_shadow_contrast_scale": 1.0,
    }
    fields = {}
    for name, expected in expected_fields.items():
        actual = settings.get_editor_property(name)
        overridden = bool(settings.get_editor_property("override_" + name))
        equal = abs(float(actual)-expected) <= 0.000001 if isinstance(expected, float) else actual == expected
        check(equal, "camera_preview_"+name, actor=context, actual=str(actual), expected=str(expected))
        check(overridden, "camera_preview_override_"+name, actor=context)
        fields[name] = {"value": actual if actual is None or isinstance(actual, (bool, int, float, str)) else str(actual),
                        "override": overridden}
    return {"blend_weight": blend_weight, "material": material.get_path_name() if material else None,
            "weighted_blendables": len(weighted), "matching_blendables": len(matching), "fields": fields}


def validate_assets(manifest):
    sprites, textures, runtime_sprites, walk_sprites = {}, {}, {}, {}
    aliases = []
    for path in ASSETS.list_assets(ROOT + "/Sprites", recursive=True, include_folder=False):
        obj = ASSETS.load_asset(path)
        if isinstance(obj, unreal.PaperSprite):
            if path.split(".", 1)[0] != obj.get_path_name().split(".", 1)[0]:
                aliases.append({"from": path, "resolved": obj.get_path_name()})
                continue
            if metadata(obj, "PlayerRuntimeSprite") == "True":
                runtime_sprites[path.split(".", 1)[0]] = obj
                continue
            if metadata(obj, "PlayerWalkPart"):
                walk_sprites[path.split(".", 1)[0]] = obj
                continue
            key = metadata(obj, "UnitySpriteKey")
            check(key not in sprites, "unique_sprite_key", key=key)
            sprites[key] = obj
    for path in ASSETS.list_assets(ROOT + "/Textures", recursive=True, include_folder=False):
        obj = ASSETS.load_asset(path)
        if isinstance(obj, unreal.Texture2D):
            if path.split(".", 1)[0] != obj.get_path_name().split(".", 1)[0]:
                aliases.append({"from": path, "resolved": obj.get_path_name()})
                continue
            guid = metadata(obj, "UnityGuid")
            check(guid not in textures, "unique_texture_guid", guid=guid)
            textures[guid] = obj
    check(set(sprites) == set(manifest["sprites"]), "sprite_asset_keys",
          missing=sorted(set(manifest["sprites"])-set(sprites)),
          unexpected=sorted(set(sprites)-set(manifest["sprites"])))
    assets = {a["guid"]: a for a in manifest["assets"]}
    check(set(textures) == set(assets), "texture_asset_guids")
    for guid, expected in assets.items():
        if guid not in textures:
            continue
        texture = textures[guid]
        expected_filter = {0: unreal.TextureFilter.TF_NEAREST, 1: unreal.TextureFilter.TF_BILINEAR,
                           2: unreal.TextureFilter.TF_TRILINEAR}[expected["filter_mode"]]
        check(texture.get_editor_property("filter") == expected_filter, "texture_filter", guid=guid)
        check(bool(texture.get_editor_property("srgb")) == expected["srgb"], "texture_srgb", guid=guid)
        check(metadata(texture, "UnitySourceHash") == expected["source_sha256"], "texture_source_hash", guid=guid)
    for key, expected in manifest["sprites"].items():
        if key not in sprites:
            continue
        sprite = sprites[key]
        x, y, width, height = expected["rect"]
        texture_height = assets[expected["texture_guid"]]["height"]
        expected_uv = [x, texture_height-y-height]
        uv = sprite.get_editor_property("source_uv")
        size = sprite.get_editor_property("source_dimension")
        pivot = sprite.get_editor_property("custom_pivot_point")
        ppu = float(sprite.get_editor_property("pixels_per_unreal_unit"))
        expected_pivot = [x + expected["pivot"][0]*width,
                          expected_uv[1] + (1-expected["pivot"][1])*height]
        check(max_delta([uv.x, uv.y], expected_uv) <= 0.0001, "sprite_uv", key=key)
        check(max_delta([size.x, size.y], [width, height]) <= 0.0001, "sprite_dimensions", key=key)
        check(max_delta([pivot.x, pivot.y], expected_pivot) <= 0.0001, "sprite_pivot", key=key)
        check(abs(ppu-expected["ppu"]/100) <= 0.000001, "sprite_ppu", key=key)
        check(sprite.get_editor_property("pivot_mode") == unreal.SpritePivotMode.CUSTOM, "sprite_custom_pivot", key=key)
        check(not sprite.get_editor_property("snap_pivot_to_pixel_grid"), "sprite_pivot_precision", key=key)
        check(sprite.get_editor_property("source_texture") == textures.get(expected["texture_guid"]), "sprite_texture_reference", key=key)
        check(sprite.get_editor_property("sprite_collision_domain") == unreal.SpriteCollisionMode.NONE, "sprite_no_collision", key=key)
        material = sprite.get_editor_property("default_material")
        check(material is not None and material.get_path_name().startswith(ROOT + "/Materials/"), "sprite_material", key=key)
    REPORT["assets"] = {"textures_checked": len(textures), "sprites_checked": len(sprites),
                        "compatibility_redirectors_skipped": len(aliases)}
    player = validate_player_assets(manifest, textures, runtime_sprites)
    player["walk_parts"] = validate_walk_assets(manifest, textures, walk_sprites)
    return sprites, player


def validate_walk_assets(manifest, textures, walk_sprites):
    """Check the twelve original-texture regions without changing source counts."""
    by_source = {a["relative_path"].replace("\\", "/"): a for a in manifest["assets"]}
    material = ASSETS.load_asset(ROOT + "/Materials/MI_Jonggu_TranslucentUnlit")
    expected_paths, parts = set(), {}
    for direction, regions in WALK_REGIONS.items():
        parts[direction] = []
        source = by_source["Assets/Resources/Generated/Sprites/Player/base/" + direction + ".png"]
        source_frame_path = ROOT + "/Sprites/Player/Runtime/" + direction.title() + "/S_Player_" + direction.title() + "_0"
        source_frame = ASSETS.load_asset(source_frame_path)
        folder = ROOT + "/Sprites/Player/WalkParts/" + direction.title() + "/"
        direction_paths = set()
        for name, (x, y, width, height) in zip(WALK_PART_NAMES, regions):
            path = folder + "S_Walk_" + direction.title() + "_" + name
            direction_paths.add(path)
            expected_paths.add(path)
            sprite = walk_sprites.get(path)
            parts[direction].append(sprite)
            if not check(sprite is not None, "player_walk_part_exists", path=path):
                continue
            check(metadata(sprite, "PlayerWalkPart") == name, "player_walk_part_identity", path=path)
            check(source_frame is not None and metadata(sprite, "PlayerWalkSource") == source_frame.get_path_name(),
                  "player_walk_source_frame", path=path)
            check((source["width"], source["height"]) == (61, 82), "player_walk_source_size", path=path)
            check(textures.get(source["guid"]) is not None
                  and sprite.get_editor_property("source_texture") == textures[source["guid"]],
                  "player_walk_source_texture", path=path)
            uv, size, pivot = [sprite.get_editor_property(p) for p in ("source_uv", "source_dimension", "custom_pivot_point")]
            check(max_delta([uv.x, uv.y], [x, y]) < 0.0001, "player_walk_region_uv", path=path)
            check(max_delta([size.x, size.y], [width, height]) < 0.0001, "player_walk_region_size", path=path)
            check(max_delta([pivot.x, pivot.y], [30.5, 75.44]) < 0.0001, "player_walk_absolute_foot_pivot", path=path)
            check(abs(float(sprite.get_editor_property("pixels_per_unreal_unit"))-0.8) < 0.000001,
                  "player_walk_ppu_80", path=path)
            check(sprite.get_editor_property("pivot_mode") == unreal.SpritePivotMode.CUSTOM
                  and not sprite.get_editor_property("snap_pivot_to_pixel_grid"), "player_walk_exact_pivot", path=path)
            check(sprite.get_editor_property("sprite_collision_domain") == unreal.SpriteCollisionMode.NONE,
                  "player_walk_no_collision", path=path)
            check(sprite.get_editor_property("render_geometry").get_editor_property("geometry_type") == unreal.SpritePolygonMode.SOURCE_BOUNDING_BOX,
                  "player_walk_rectangular_geometry", path=path)
            check(material is not None and sprite.get_editor_property("default_material") == material,
                  "player_walk_unlit_material", path=path)
        actual_direction_paths = {path for path in walk_sprites if path.startswith(folder)}
        check(actual_direction_paths == direction_paths and len(actual_direction_paths) == 4,
              "player_walk_four_parts_per_direction", direction=direction,
              missing=sorted(direction_paths-actual_direction_paths), unexpected=sorted(actual_direction_paths-direction_paths))
    check(set(walk_sprites) == expected_paths and len(walk_sprites) == 12, "player_walk_exactly_twelve_parts",
          missing=sorted(expected_paths-set(walk_sprites)), unexpected=sorted(set(walk_sprites)-expected_paths))
    REPORT["assets"]["player_walk_parts_checked"] = len(walk_sprites)
    return parts


def validate_player_assets(manifest, textures, runtime_sprites):
    """Check the requested six runtime frames independently of the authoring code."""
    by_source = {a["relative_path"].replace("\\", "/"): a for a in manifest["assets"]}
    expected_paths, frames = set(), {}
    for direction in ("front", "back", "side"):
        frames[direction] = []
        for frame in (0, 1):
            source_relative = "Assets/Resources/Generated/Sprites/Player/" + (
                "base/" + direction + ".png" if frame == 0 else "idle/" + direction + "/frame_2.png")
            source = by_source[source_relative]
            path = ROOT + "/Sprites/Player/Runtime/" + direction.title() + "/S_Player_" + direction.title() + "_" + str(frame)
            expected_paths.add(path)
            sprite = runtime_sprites.get(path)
            frames[direction].append(sprite)
            if not check(sprite is not None, "runtime_player_frame_exists", path=path):
                continue
            check((source["width"], source["height"]) == (61, 82), "runtime_player_source_size", path=path)
            check(sprite.get_editor_property("source_texture") == textures.get(source["guid"]),
                  "runtime_player_frame_texture", path=path)
            uv, size, pivot = [sprite.get_editor_property(p) for p in ("source_uv", "source_dimension", "custom_pivot_point")]
            check(max_delta([uv.x, uv.y], [0, 0]) < 0.0001, "runtime_player_uv", path=path)
            check(max_delta([size.x, size.y], [61, 82]) < 0.0001, "runtime_player_frame_size", path=path)
            check(max_delta([pivot.x, pivot.y], [30.5, 75.44]) < 0.0001, "runtime_player_foot_pivot", path=path)
            check(abs(float(sprite.get_editor_property("pixels_per_unreal_unit"))-0.8) < 0.000001,
                  "runtime_player_ppu_80", path=path)
            check(sprite.get_editor_property("pivot_mode") == unreal.SpritePivotMode.CUSTOM
                  and not sprite.get_editor_property("snap_pivot_to_pixel_grid"), "runtime_player_exact_pivot", path=path)
            check(sprite.get_editor_property("sprite_collision_domain") == unreal.SpriteCollisionMode.NONE,
                  "runtime_player_sprite_no_collision", path=path)
            check(sprite.get_editor_property("render_geometry").get_editor_property("geometry_type") == unreal.SpritePolygonMode.SOURCE_BOUNDING_BOX,
                  "runtime_player_rectangular_geometry", path=path)
            check(metadata(sprite, "PlayerRuntimeSourceGuid") == source["guid"], "runtime_player_source_guid", path=path)
            check(metadata(sprite, "PlayerRuntimeSourceSpriteKey") == source["sprites"][0], "runtime_player_source_sprite_key", path=path)
            check(sprite.get_editor_property("default_material") == ASSETS.load_asset(ROOT + "/Materials/MI_Jonggu_TranslucentUnlit"),
                  "runtime_player_unlit_material", path=path)
    check(set(runtime_sprites) == expected_paths and len(runtime_sprites) == 6, "runtime_player_exactly_six_frames",
          missing=sorted(expected_paths-set(runtime_sprites)), unexpected=sorted(set(runtime_sprites)-expected_paths))
    REPORT["assets"]["runtime_player_sprites_checked"] = len(runtime_sprites)
    bp = ASSETS.load_asset(PLAYER_BP)
    generated = None
    if check(isinstance(bp, unreal.Blueprint), "player_blueprint_exists"):
        check(unreal.BlueprintEditorLibrary.get_blueprint_parent_class(bp) == unreal.Pawn.static_class(),
              "player_uses_engine_pawn_parent")
        generated = bp.generated_class()
        check(generated is not None, "player_blueprint_generated_class")
        graphs = unreal.BlueprintEditorLibrary.list_graphs(bp)
        names = {g.get_name() for g in graphs}
        check({"EventGraph", "RefreshPlayerVisual"}.issubset(names), "player_movement_visual_graphs")
        for graph in graphs:
            editor = unreal.BlueprintGraphEditor.get_graph_editor(graph)
            check(not editor.list_nodes_with_errors(), "player_graph_has_no_compile_errors", graph=graph.get_name())
    return {"frames": frames, "class": generated}


def player_target_matrix(scene):
    """The Beach movement origin is explicitly placed at its existing Jonggu child."""
    root = next(o for o in scene["objects"] if o["id"] == PLAYER_ROOTS[scene["name"]])
    matrix = copy.deepcopy(root["world_matrix"])
    if scene["name"] == "Beach":
        character = next(o for o in scene["objects"] if o["path"] == "SceneGameplayRoot/PlayerRoot/Jonggu")
        for row in range(3):
            matrix[row][3] = character["world_matrix"][row][3]
    return matrix


def player_feet_sort_priority(world_z_cm):
    """Independent requested feet-depth contract, including negative half values."""
    return -math.floor(float(world_z_cm) + 0.5)


def expected_depth_sorting(scene, rules):
    """Compute expected static ordering from authored anchors, never import the writer."""
    config = rules["depth_sorting"]
    authored = config["scenes"][scene["name"]]["groups"]
    groups = {group["id"]: group for group in scene["groups"]}
    objects = {obj["id"]: obj for obj in scene["objects"]}
    layers = {value: i for i, value in enumerate(sorted({g.get("sorting_layer_id", 0) for g in scene["groups"]}))}
    check(config.get("schema_version") == 1 and config.get("rounding") == "floor_plus_half",
          "feet_depth_rules_schema", scene=scene["name"])
    check(config["gameplay_base"] == 0 and config["units_per_cm"] == 1,
          "feet_depth_rules_match_player_blueprint", scene=scene["name"])
    check(config["background_base"] == -30000
          and config.get("engine_priority_min") == -32768 and config.get("engine_priority_max") == 32767,
          "feet_depth_rules_match_renderer_range", scene=scene["name"])
    check(set(authored).issubset(groups), "feet_depth_rule_group_ids", scene=scene["name"],
          unexpected=sorted(set(authored)-set(groups)))
    expected = {}
    for key, group in groups.items():
        entry = authored.get(key, {"category": "source"})
        category = entry["category"]
        source_priority = layers[group.get("sorting_layer_id", 0)]*100000 + group["sorting_order"]
        anchor_z_cm = None
        if category in ("depth", "player"):
            check(bool(entry.get("anchor_object_id")) != bool(entry.get("anchor_group_id")),
                  "feet_depth_single_authored_anchor", group=key)
            check(abs(entry.get("suborder", 0)) <= 4, "feet_depth_suborder_tie_band", group=key)
            if entry.get("anchor_object_id"):
                matrix = objects[entry["anchor_object_id"]]["world_matrix"]
            else:
                matrix = groups[entry["anchor_group_id"]]["world_matrix"]
            x, y = entry["anchor_local_xy"]
            anchor_z_cm = 100*(matrix[1][0]*x + matrix[1][1]*y + matrix[1][3])
            priority = config["gameplay_base"] - math.floor(anchor_z_cm*config["units_per_cm"] + 0.5) + entry.get("suborder", 0)
        elif category in ("background", "source"):
            priority = config["background_base"] + source_priority
        else:
            raise ValueError("Unknown feet depth category: " + category)
        # FPrimitiveSceneProxy clamps this int32 property to int16 in the renderer.
        # Raw serialized equality alone can otherwise pass while draw order collapses.
        check(-32768 <= priority <= 32767, "feet_depth_expected_renderer_range", group=key, priority=priority)
        check(group["status"] != "visible" or key in authored,
              "visible_render_has_explicit_depth_category", group=key)
        expected[key] = {"priority": priority, "category": category, "anchor_z_cm": anchor_z_cm,
                         "suborder": entry.get("suborder", 0),
                         "anchor_group_id": entry.get("anchor_group_id"),
                         "anchor_object_id": entry.get("anchor_object_id"),
                         "anchor_local_xy": entry.get("anchor_local_xy")}
    return expected


def validate_scene_player(scene, actors, manifest, layers, player):
    """Read the actual placed Pawn, including its visible sprite's world geometry."""
    context = "node:" + PLAYER_ROOTS[scene["name"]]
    pawn = actors[context]
    group = next(g for g in scene["groups"] if g["id"] == PLAYER_RENDERS[scene["name"]])
    source_instance = group["instances"][0]
    check(isinstance(pawn, unreal.Pawn) and pawn.get_class() == player["class"], "placed_player_class", actor=context)
    check(pawn.get_editor_property("auto_possess_player") == unreal.AutoReceiveInput.PLAYER0,
          "player_auto_possess_player_0", actor=context)
    check(pawn.get_editor_property("auto_possess_ai") == unreal.AutoPossessAI.DISABLED, "player_no_ai_possession", actor=context)
    check(all(not pawn.get_editor_property("use_controller_rotation_" + axis) for axis in ("pitch", "yaw", "roll")),
          "player_sprite_rotation_independent_of_controller", actor=context)
    check(metadata(pawn, "PlayerSourceRootId") == PLAYER_ROOTS[scene["name"]]
          and metadata(pawn, "PlayerSourceVisualId") == PLAYER_RENDERS[scene["name"]],
          "player_source_identity_metadata", actor=context)
    for name, expected in (("move_speed", 400.0), ("idle_frames_per_second", 1.0/0.3)):
        check(abs(float(pawn.get_editor_property(name))-expected) < 0.0001, "player_setting", actor=context, property=name)
    for name, expected in (("facing_direction", 0), ("current_frame_index", 0), ("is_moving", False),
                           ("side_sprite_faces_left", True)):
        check(pawn.get_editor_property(name) == expected, "player_initial_state", actor=context, property=name)
    for direction, expected in player["frames"].items():
        check(list(pawn.get_editor_property(direction + "_frames")) == expected, "player_ordered_frames", actor=context, direction=direction)
    visual = pawn.get_editor_property("player_visual")
    for direction in ("front", "back", "side"):
        parts = pawn.get_editor_property(direction + "_walk_parts")
        check(len(parts) == 4 and all(part is not None for part in parts),
              "player_original_texture_walk_parts", actor=context, direction=direction)
        check(list(parts) == player["walk_parts"][direction],
              "player_ordered_walk_parts", actor=context, direction=direction)
    collision = pawn.get_component_by_class(unreal.SphereComponent)
    movement = pawn.get_component_by_class(unreal.FloatingPawnMovement)
    check(collision is not None and pawn.root_component == collision, "player_sphere_root", actor=context)
    if collision:
        check(abs(collision.get_unscaled_sphere_radius()-24) < 0.0001, "player_collision_radius", actor=context)
        check(str(collision.get_collision_profile_name()) == "Pawn", "player_collision_profile", actor=context)
    check(movement is not None, "player_floating_movement_component", actor=context)
    if movement:
        # UpdatedComponent is transient: it is assigned on registration/BeginPlay,
        # so persisted editor readback checks the setting and runtime QA checks identity.
        check(bool(movement.get_editor_property("auto_register_updated_component")),
              "player_movement_auto_registers_collision_root", actor=context)
        check(abs(float(movement.get_editor_property("max_speed"))-400) < 0.0001, "player_max_speed", actor=context)
        check(movement.get_editor_property("constrain_to_plane"), "player_plane_constraint", actor=context)
        check(max_delta(xyz(movement.get_editor_property("plane_constraint_normal")), [0, 1, 0]) < 0.000001,
              "player_movement_xz_plane", actor=context)
    if check(visual is not None, "player_visual_component", actor=context):
        check(visual.get_owner() == pawn and visual.get_attach_parent() == collision, "player_visual_parent", actor=context)
        check(visual.get_collision_enabled() == unreal.CollisionEnabled.NO_COLLISION,
              "player_visual_has_no_collision", actor=context)
        check(visual.get_sprite() == player["frames"]["front"][0], "player_front_initial_frame", actor=context)
        compare_transform(context+":visible_player", source_instance["world_matrix"], visual.get_world_transform(),
                          {"rect": [0, 0, 61, 82], "ppu": 80, "pivot": [0.5, 0.08]})
        check(bool(visual.get_editor_property("visible")) and not visual.get_editor_property("hidden_in_game"),
              "player_visual_visible", actor=context)
        check(max_delta(rgba(visual.get_editor_property("sprite_color")), source_instance["color"]) < 0.000001,
              "player_visual_color", actor=context)
        priority = player_feet_sort_priority(source_position(player_target_matrix(scene))[2])
        actual_priority = int(visual.get_editor_property("translucency_sort_priority"))
        check(-32768 <= actual_priority <= 32767, "player_visual_renderer_sort_range", actor=context, actual=actual_priority)
        check(-32768 <= priority <= 32767, "player_expected_renderer_sort_range", actor=context, expected=priority)
        check(visual.get_editor_property("translucency_sort_priority") == priority,
              "player_visual_sort_from_feet", actor=context, expected=priority)
        part_names = ("walk_head", "walk_body", "walk_left_foot", "walk_right_foot")
        for name in part_names:
            part = pawn.get_editor_property(name)
            check(part is not None and part.get_editor_property("translucency_sort_priority") == priority,
                  "player_walk_part_sort_from_feet", actor=context, component=name, expected=priority)
            if part is not None:
                actual_part_priority = int(part.get_editor_property("translucency_sort_priority"))
                check(-32768 <= actual_part_priority <= 32767, "player_walk_part_renderer_sort_range",
                      actor=context, component=name, actual=actual_part_priority)
        check(visual.get_material(0) == ASSETS.load_asset(ROOT + "/Materials/MI_Jonggu_TranslucentUnlit"),
              "player_visual_material", actor=context)
        check(max_delta(xyz(pawn.get_editor_property("visual_scale")), xyz(visual.get_editor_property("relative_scale3d"))) < 0.000001,
              "player_saved_visual_scale", actor=context)
    return {"actor_id": context, "blueprint": PLAYER_BP, "source_visual_id": group["id"],
            "root_rebased": scene["name"] == "Beach", "target_world_matrix": player_target_matrix(scene),
            "initial_visual_checked": visual is not None, "runtime_execution_tested": False}


def validate_obstacle_component(component, context):
    """Read the serialized primitive settings, independently of the authoring tool."""
    profile = str(component.get_collision_profile_name())
    enabled = component.get_collision_enabled()
    object_type = component.get_collision_object_type()
    pawn_response = component.get_collision_response_to_channel(unreal.CollisionChannel.ECC_PAWN)
    overlap = bool(component.get_editor_property("generate_overlap_events"))
    simulate = bool(component.get_editor_property("body_instance").get_editor_property("simulate_physics"))
    check(profile == "JongguObstacle", "obstacle_profile", actor=context, actual=profile)
    check(enabled == unreal.CollisionEnabled.QUERY_ONLY, "obstacle_query_only", actor=context, actual=str(enabled))
    check(object_type == unreal.CollisionChannel.ECC_WORLD_STATIC, "obstacle_world_static", actor=context, actual=str(object_type))
    check(pawn_response == unreal.CollisionResponseType.ECR_BLOCK, "obstacle_blocks_pawn", actor=context, actual=str(pawn_response))
    check(not overlap, "obstacle_no_overlap_events", actor=context)
    check(not simulate, "obstacle_no_simulation", actor=context)
    check(bool(component.get_editor_property("hidden_in_game")), "obstacle_hidden_in_game", actor=context)
    check(bool(component.get_editor_property("visible")), "obstacle_editor_shape_visible", actor=context)
    return {"profile": profile, "enabled": str(enabled), "object_type": str(object_type),
            "pawn_response": str(pawn_response), "overlap": overlap, "simulate_physics": simulate}


def validate_collision_blueprints():
    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    library = unreal.SubobjectDataBlueprintFunctionLibrary
    generated = {}
    for shape, path in COLLISION_BLUEPRINTS.items():
        blueprint = ASSETS.load_asset(path)
        if not check(isinstance(blueprint, unreal.Blueprint), "collision_blueprint_exists", path=path):
            continue
        generated[shape] = blueprint.generated_class()
        check(generated[shape] is not None, "collision_blueprint_generated_class", path=path)
        check(unreal.BlueprintEditorLibrary.get_blueprint_parent_class(blueprint) == unreal.Actor.static_class(),
              "collision_blueprint_actor_parent", path=path)
        roots, primitives = [], []
        for handle in subsystem.k2_gather_subobject_data_for_blueprint(blueprint):
            data = library.get_data(handle)
            template = library.get_object_for_blueprint(data, blueprint)
            if isinstance(template, unreal.PrimitiveComponent):
                primitives.append(template)
            if isinstance(template, unreal.SceneComponent) and library.is_root_component(data):
                roots.append(template)
        expected_type = unreal.BoxComponent if shape == "box" else unreal.SphereComponent
        check(len(roots) == 1 and isinstance(roots[0], expected_type),
              "collision_blueprint_primitive_root", path=path, roots=[r.get_name() for r in roots])
        check(len(primitives) == 1, "collision_blueprint_single_primitive", path=path, actual=len(primitives))
        for root in roots:
            check(root.get_name().startswith("collision_shape"), "collision_blueprint_root_member", path=path)
            validate_obstacle_component(root, path)
        for graph in unreal.BlueprintEditorLibrary.list_graphs(blueprint):
            check(not unreal.BlueprintGraphEditor.get_graph_editor(graph).list_nodes_with_errors(),
                  "collision_blueprint_no_compile_errors", path=path, graph=graph.get_name())
    REPORT["assets"]["collision_blueprints_checked"] = len(generated)
    return generated


def read_collision_contract():
    """The authored rules hash prevents stale expected specs from blessing stale maps."""
    raw = DATA_ROOT.joinpath("collision_rules.json").read_bytes()
    rules = json.loads(raw)
    authored_path = QA_ROOT / "collision_authoring_report.json"
    if not authored_path.is_file(): authored_path = FIXTURE_ROOT / "collision_authoring_report.json"
    authored = json.loads(authored_path.read_text(encoding="utf-8"))
    rules_hash = hashlib.sha256(raw).hexdigest()
    check(rules.get("schema_version") == 1, "collision_rules_schema")
    check(authored.get("schema_version") == 1, "collision_authoring_schema")
    check(authored.get("success") is True, "collision_authoring_success")
    check(authored.get("rules_sha256") == rules_hash, "collision_authored_rules_hash")
    REPORT["collision_rules_sha256"] = rules_hash
    return {"rules": rules, "rules_sha256": rules_hash,
            "scenes": {row["name"]: row for row in authored["scenes"]}}


def collision_source_footprints(scene, rules):
    """Independent affine footprint expectation; no importer/generator code is used."""
    objects = {obj["id"]: obj for obj in scene["objects"]}
    config = rules["scenes"][scene["name"]]
    inputs = []
    for object_id in config.get("object_rule_ids", []):
        obj = objects[object_id]
        if obj.get("active_in_hierarchy", True):
            inputs.append((object_id, object_id, obj["world_matrix"], rules["object_rules"][object_id]))
    for group in scene["groups"]:
        if group.get("kind") != "sprite" or group.get("status") not in (None, "visible"):
            continue
        key = group.get("source_sprite_key", group.get("sprite_key"))
        rule = rules.get("sprite_rules", {}).get(key)
        if (rule and scene["name"] in rule.get("scenes", [scene["name"]])
                and objects[group["object_id"]].get("active_in_hierarchy", True)):
            inputs.append((group["id"], group["object_id"], group["world_matrix"], rule))
    expected = {}
    for source_id, parent, matrix, rule in inputs:
        sx = math.sqrt(matrix[0][0]**2 + matrix[1][0]**2)
        sy = math.sqrt(matrix[0][1]**2 + matrix[1][1]**2)
        for index, footprint in enumerate(rule.get("footprints", [])):
            x, z = footprint.get("center", [0, 0])
            key = "collision:" + source_id + ":" + str(index)
            row = {"parent_id": parent, "shape": footprint["shape"], "category": rule["category"],
                   "center_cm": [100*(matrix[0][0]*x + matrix[0][1]*z + matrix[0][3]), 0,
                                 100*(matrix[1][0]*x + matrix[1][1]*z + matrix[1][3])],
                   "rotation_degrees": math.degrees(math.atan2(matrix[1][0], matrix[0][0]))}
            if footprint["shape"] == "box":
                width, height = footprint["size"]
                row["extent_cm"] = [50*width*sx, rules.get("default_depth_half_extent_cm", 100), 50*height*sy]
            else:
                row["radius_cm"] = 100*footprint["radius"]*sx
                row["rotation_degrees"] = 0.0
            expected[key] = row
    return expected


def collision_dry_ground_mask(scene, manifest, rules):
    """Independently rasterize authored polygons using scanline intersections.

    This intentionally does not use the writer's point-in-convex-polygon test or
    rectangle merge. It checks every grid cell in the world-space land/dock union.
    """
    terrain = rules["scenes"][scene["name"]].get("terrain")
    if not terrain:
        return None
    xmin, zmin, xmax, zmax = terrain["bounds"]
    step = terrain["grid_step"]
    width, height = round((xmax-xmin)/step), round((zmax-zmin)/step)
    mask = [bytearray(width) for _ in range(height)]
    for group in scene["groups"]:
        if group["id"] not in terrain["group_ids"]:
            continue
        for instance in group["instances"]:
            sprite = manifest["sprites"][instance["sprite_key"]]
            sw, sh = sprite["rect"][2:]
            px, pz = sprite["pivot"]
            matrix = instance["world_matrix"]
            for left, bottom, right, top in rules["terrain_sprite_rules"][instance["sprite_key"]].get("walkable_uv", []):
                polygon = []
                for u, v in ((left, bottom), (right, bottom), (right, top), (left, top)):
                    x, z = (u-px)*sw/sprite["ppu"], (v-pz)*sh/sprite["ppu"]
                    polygon.append((matrix[0][0]*x + matrix[0][1]*z + matrix[0][3],
                                    matrix[1][0]*x + matrix[1][1]*z + matrix[1][3]))
                first = max(0, math.ceil((min(p[1] for p in polygon)-zmin)/step - 0.5 - 1e-8))
                last = min(height-1, math.floor((max(p[1] for p in polygon)-zmin)/step - 0.5 + 1e-8))
                for iz in range(first, last+1):
                    z = zmin+(iz+0.5)*step
                    crossings = []
                    for edge, (ax, az) in enumerate(polygon):
                        bx, bz = polygon[(edge+1) % 4]
                        if abs(az-bz) < 1e-10:
                            if abs(z-az) < 1e-8:
                                crossings.extend((ax, bx))
                        elif min(az, bz)-1e-8 <= z <= max(az, bz)+1e-8:
                            crossings.append(ax+(z-az)*(bx-ax)/(bz-az))
                    if crossings:
                        start = max(0, math.ceil((min(crossings)-xmin)/step - 0.5 - 1e-8))
                        stop = min(width-1, math.floor((max(crossings)-xmin)/step - 0.5 + 1e-8))
                        if start <= stop:
                            mask[iz][start:stop+1] = b"\x01"*(stop-start+1)
    return mask, terrain


def validate_collision_source_spec(scene, manifest, rules, expected):
    """Require generated expectation to cover authored footprints and all water cells."""
    footprint_expected = collision_source_footprints(scene, rules)
    actual_footprints = {key: spec for key, spec in expected.items() if spec["category"] not in ("boundary", "water")}
    check(set(actual_footprints) == set(footprint_expected), "collision_source_footprint_ids", scene=scene["name"],
          missing=sorted(set(footprint_expected)-set(actual_footprints)),
          unexpected=sorted(set(actual_footprints)-set(footprint_expected)))
    for key, row in footprint_expected.items():
        if key not in actual_footprints:
            continue
        spec = actual_footprints[key]
        for field, value in row.items():
            actual = spec.get(field)
            match = (max_delta(actual, value) <= TOLERANCE_CM if isinstance(value, list)
                     else abs(actual-value) < 0.00001 if isinstance(value, float)
                     else actual == value)
            check(match, "collision_source_footprint_" + field, actor=key, expected=value, actual=actual)
    config = rules["scenes"][scene["name"]]
    xmin, zmin, xmax, zmax = config["bounds"]
    thickness = config.get("boundary_thickness", 0.5)
    boundary = {key: spec for key, spec in expected.items() if spec["category"] == "boundary"}
    check(len(boundary) == 4, "collision_four_outer_boundaries", scene=scene["name"])
    for edge, center, size in (
            ("left", [xmin-thickness/2, (zmin+zmax)/2], [thickness, zmax-zmin+2*thickness]),
            ("right", [xmax+thickness/2, (zmin+zmax)/2], [thickness, zmax-zmin+2*thickness]),
            ("bottom", [(xmin+xmax)/2, zmin-thickness/2], [xmax-xmin, thickness]),
            ("top", [(xmin+xmax)/2, zmax+thickness/2], [xmax-xmin, thickness])):
        key = "collision:" + scene["name"] + ":boundary:" + edge
        spec = boundary.get(key)
        if not check(spec is not None, "collision_boundary_present", actor=key):
            continue
        check(spec["parent_id"] == config["bounds_parent_id"] and spec["shape"] == "box"
              and abs(spec.get("rotation_degrees", 0)) < 0.00001
              and max_delta(spec["center_cm"], [100*center[0], 0, 100*center[1]]) <= TOLERANCE_CM
              and max_delta(spec["extent_cm"], [50*size[0], rules.get("default_depth_half_extent_cm", 100), 50*size[1]]) <= TOLERANCE_CM,
              "collision_authored_outer_boundary", actor=key)
    water = [spec for spec in expected.values() if spec["category"] == "water"]
    ground = collision_dry_ground_mask(scene, manifest, rules)
    if not ground:
        check(not water, "collision_no_unrequested_water", scene=scene["name"])
        return {"source_footprints_checked": len(footprint_expected), "terrain_cells_checked": 0}
    dry, terrain = ground
    xmin, zmin, xmax, zmax = terrain["bounds"]
    step = terrain["grid_step"]
    height, width = len(dry), len(dry[0])
    blocked = [bytearray(width) for _ in range(height)]
    overlaps = 0
    for spec in water:
        x, _, z = spec["center_cm"]
        ex, ey, ez = spec["extent_cm"]
        edges = [((x-ex)/100-xmin)/step, ((z-ez)/100-zmin)/step,
                 ((x+ex)/100-xmin)/step, ((z+ez)/100-zmin)/step]
        ix0, iz0, ix1, iz1 = [round(value) for value in edges]
        valid = (spec["shape"] == "box" and spec["parent_id"] == terrain["parent_id"]
                 and abs(spec.get("rotation_degrees", 0)) < 0.00001
                 and abs(ey-rules.get("default_depth_half_extent_cm", 100)) < 0.00001
                 and max_delta(edges, [ix0, iz0, ix1, iz1]) < 0.00001
                 and 0 <= ix0 < ix1 <= width and 0 <= iz0 < iz1 <= height)
        if not check(valid, "collision_water_grid_aligned", actor=spec["id"]):
            continue
        for iz in range(iz0, iz1):
            overlaps += sum(blocked[iz][ix0:ix1])
            blocked[iz][ix0:ix1] = b"\x01"*(ix1-ix0)
    mismatches = sum(sum(1 for a, b in zip(drow, brow) if a == b) for drow, brow in zip(dry, blocked))
    check(overlaps == 0, "collision_water_rectangles_disjoint", scene=scene["name"], overlapping_cells=overlaps)
    check(mismatches == 0, "collision_water_is_world_dry_union_complement", scene=scene["name"], mismatched_cells=mismatches)
    return {"source_footprints_checked": len(footprint_expected), "terrain_cells_checked": width*height,
            "dry_cells": sum(sum(row) for row in dry), "water_cells": sum(sum(row) for row in blocked)}


def validate_scene_collision(scene, manifest, actors, source_actors, contract, classes):
    authored = contract["scenes"][scene["name"]]
    expected = {spec["id"]: spec for spec in authored["specs"]}
    check(len(expected) == len(authored["specs"]), "collision_expected_unique_ids", scene=scene["name"])
    check(set(actors) == set(expected), "collision_actor_id_contract", scene=scene["name"],
          missing=sorted(set(expected)-set(actors)), unexpected=sorted(set(actors)-set(expected)))
    check(authored.get("reopen_verified") is True, "collision_authoring_reopen_verified", scene=scene["name"])
    source_readback = validate_collision_source_spec(scene, manifest, contract["rules"], expected)
    for key, actor in source_actors.items():
        for component in actor.get_components_by_class(unreal.PrimitiveComponent):
            if key == "node:" + PLAYER_ROOTS[scene["name"]] and component == actor.root_component:
                continue
            check(component.get_collision_enabled() == unreal.CollisionEnabled.NO_COLLISION,
                  "source_visual_or_placeholder_has_no_collision", actor=key, component=component.get_name())
    rows, categories = [], Counter()
    for key, spec in expected.items():
        if key not in actors:
            continue
        actor = actors[key]
        root = actor.root_component
        shape = spec["shape"]
        cls = unreal.BoxComponent if shape == "box" else unreal.SphereComponent
        if not check(isinstance(root, cls), "collision_actor_root_type", actor=key, shape=shape):
            continue
        check(actor.get_class() == classes.get(shape), "collision_actor_blueprint_class", actor=key)
        check(len(actor.get_components_by_class(unreal.PrimitiveComponent)) == 1,
              "collision_actor_single_primitive", actor=key)
        parent = actor.get_attach_parent_actor()
        parent_id = "node:" + spec["parent_id"] if spec.get("parent_id") else None
        check((identity(parent) if parent else None) == parent_id,
              "collision_authored_parent", actor=key, expected=parent_id)
        check(parent_id is None or parent_id in source_actors, "collision_parent_is_source_node", actor=key)
        check(not actor.get_editor_property("hidden"), "collision_actor_active", actor=key)
        check(metadata(actor, "JongguCollisionRulesHash") == contract["rules_sha256"],
              "collision_actor_rules_hash", actor=key)
        try:
            stored_spec = json.loads(metadata(actor, "JongguCollisionSpec"))
        except (TypeError, ValueError):
            stored_spec = None
        check(stored_spec == spec, "collision_actor_spec_metadata", actor=key)
        state = validate_obstacle_component(root, key)
        transform = root.get_world_transform()
        center = xyz(transform.translation)
        basis = actual_basis(transform)
        check(max_delta(center, spec["center_cm"]) <= TOLERANCE_CM,
              "collision_world_center", actor=key, actual=center, expected=spec["center_cm"])
        check(abs(center[1]) <= TOLERANCE_CM, "collision_xz_plane", actor=key)
        check(max_delta(xyz(transform.scale3d), [1, 1, 1]) < 0.00001,
              "collision_world_unit_scale", actor=key, actual=xyz(transform.scale3d))
        angle = math.radians(float(spec.get("rotation_degrees", 0)))
        expected_basis = [[math.cos(angle), 0, math.sin(angle)], [0, 1, 0],
                          [-math.sin(angle), 0, math.cos(angle)]]
        check(max(max_delta(a, b) for a, b in zip(basis, expected_basis)) < 0.00001,
              "collision_world_basis", actor=key, expected_degrees=spec.get("rotation_degrees", 0))
        row = dict(state, id=key, parent_id=spec.get("parent_id"), shape=shape,
                   center_cm=center, rotation_quat=[float(getattr(transform.rotation, c)) for c in "xyzw"],
                   scale=xyz(transform.scale3d))
        if shape == "box":
            extents = xyz(root.get_unscaled_box_extent())
            check(max_delta(extents, spec["extent_cm"]) <= TOLERANCE_CM,
                  "collision_box_half_extents", actor=key, actual=extents, expected=spec["extent_cm"])
            check(extents[1] >= 24, "collision_box_covers_player_plane", actor=key)
            # Compare all eight world corners: parent rotation/scale errors cannot hide in AABBs.
            for sx in (-1, 1):
                for sy in (-1, 1):
                    for sz in (-1, 1):
                        local = [sx*extents[0], sy*extents[1], sz*extents[2]]
                        expected_local = [sx*spec["extent_cm"][0], sy*spec["extent_cm"][1], sz*spec["extent_cm"][2]]
                        expected_world = [spec["center_cm"][r] + sum(expected_basis[c][r]*expected_local[c] for c in range(3)) for r in range(3)]
                        check(distance(transform_point(transform, basis, local), expected_world) <= TOLERANCE_CM,
                              "collision_box_world_corner", actor=key)
            row["extent_cm"] = extents
        else:
            radius = float(root.get_unscaled_sphere_radius())
            check(abs(radius-float(spec["radius_cm"])) <= TOLERANCE_CM,
                  "collision_sphere_radius", actor=key, actual=radius, expected=spec["radius_cm"])
            row["radius_cm"] = radius
        categories[spec["category"]] += 1
        rows.append(row)
    fingerprint = hashlib.sha256(json.dumps(rows, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    return {"actors_checked": len(rows), "category_counts": dict(categories), "source_readback": source_readback,
            "snapshot_sha256": fingerprint, "snapshot": rows}


def validate_scene(scene, baseline, manifest, sprites, player, collision_contract, collision_classes):
    if not LEVELS.load_level(baseline["path"]):
        raise RuntimeError("Cannot load saved map: " + baseline["path"])
    all_actors = [a for a in ACTORS.get_all_level_actors() if identity(a)]
    managed = {identity(a): a for a in all_actors}
    check(len(managed) == len(all_actors), "unique_actor_ids", scene=scene["name"])
    collision_actors = {key: actor for key, actor in managed.items() if key.startswith("collision:")}
    actors = {key: actor for key, actor in managed.items() if not key.startswith("collision:")}
    expected_ids = {"node:" + obj["id"] for obj in scene["objects"]}
    expected_ids.update("render:" + group["id"] for group in scene["groups"])
    expected_ids.update("camera:" + camera["object_id"] for camera in scene["cameras"])
    baseline_source_ids = {key for key in baseline["expected_actor_ids"] if not key.startswith("collision:")}
    check(set(actors) == expected_ids == baseline_source_ids, "actor_id_contract", scene=scene["name"])
    check(set(managed) == set(baseline["expected_actor_ids"]), "managed_actor_id_contract", scene=scene["name"])
    check(len(actors) == {"Hub": 99, "Beach": 56}[scene["name"]], "planned_actor_count", scene=scene["name"], actual=len(actors))
    for obj in scene["objects"]:
        key = "node:" + obj["id"]
        actor = actors[key]
        expected_matrix = player_target_matrix(scene) if obj["id"] == PLAYER_ROOTS[scene["name"]] else obj["world_matrix"]
        compare_transform(key, expected_matrix, actor.get_actor_transform())
        expected_parent = "node:" + obj["parent_id"] if obj.get("parent_id") else None
        parent = actor.get_attach_parent_actor()
        check((identity(parent) if parent else None) == expected_parent, "authored_parent", actor=key, expected_parent=expected_parent)
        check(bool(actor.get_editor_property("hidden")) == (not obj["active_in_hierarchy"]), "node_hidden_state", actor=key)
    layers = {value: i for i, value in enumerate(sorted({g.get("sorting_layer_id", 0) for g in scene["groups"]}))}
    depth_expected = expected_depth_sorting(scene, collision_contract["rules"])
    groups = []
    status_counts = Counter()
    for group in scene["groups"]:
        key = "render:" + group["id"]
        actor = actors[key]
        status_counts[group["status"]] += 1
        instances = group["instances"]
        single = group["kind"] == "sprite" and len(instances) == 1
        comp = actor.get_component_by_class(unreal.PaperSpriteComponent if single else unreal.PaperGroupedSpriteComponent)
        if not check(comp is not None, "render_component_type", actor=key):
            continue
        check(comp.get_collision_enabled() == unreal.CollisionEnabled.NO_COLLISION,
              "render_component_has_no_collision", actor=key)
        is_player_placeholder = group["id"] == PLAYER_RENDERS[scene["name"]]
        expected_visible = group["status"] == "visible" and not is_player_placeholder
        check(bool(comp.get_editor_property("visible")) == expected_visible, "component_visibility", actor=key)
        check(bool(comp.get_editor_property("hidden_in_game")) == (not expected_visible), "component_hidden_in_game", actor=key)
        check(bool(actor.get_editor_property("hidden")) == (not expected_visible), "render_actor_hidden", actor=key)
        priority = depth_expected[group["id"]]["priority"]
        actual_priority = int(comp.get_editor_property("translucency_sort_priority"))
        check(-32768 <= actual_priority <= 32767, "render_component_renderer_sort_range", actor=key, actual=actual_priority)
        check(comp.get_editor_property("translucency_sort_priority") == priority,
              "authored_feet_sorting_priority", actor=key, expected=priority,
              category=depth_expected[group["id"]]["category"])
        parent = actor.get_attach_parent_actor()
        check((identity(parent) if parent else None) == "node:"+group["object_id"], "render_parent", actor=key)
        check(json.loads(metadata(actor, "UnityInstanceIds")) == [item["id"] for item in instances], "ordered_instance_ids", actor=key)
        instance_data = None
        if not single:
            check(comp.get_instance_count() == len(instances), "grouped_instance_count", actor=key)
            instance_data = comp.get_editor_property("per_instance_sprite_data")
            check(len(instance_data) == len(instances), "serialized_instance_array_count", actor=key)
        for index, instance in enumerate(instances):
            expected_sprite = manifest["sprites"][instance["sprite_key"]]
            if single:
                actual_transform = actor.get_actor_transform()
                actual_sprite = comp.get_sprite()
                actual_color = rgba(comp.get_editor_property("sprite_color"))
                color_tolerance = REPORT["tolerances"]["sprite_color"]
            else:
                actual_transform = instance_transform(comp, index)
                data = instance_data[index]
                actual_sprite = data.get_editor_property("source_sprite")
                actual_color = [v / 255 for v in rgba(data.get_editor_property("vertex_color"))]
                color_tolerance = REPORT["tolerances"]["grouped_color"]
            compare_transform(instance["id"], instance["world_matrix"], actual_transform, expected_sprite)
            check(actual_sprite == sprites[instance["sprite_key"]] and metadata(actual_sprite, "UnitySpriteKey") == instance["sprite_key"], "instance_sprite_key", instance=instance["id"])
            check(max_delta(actual_color, instance["color"]) <= color_tolerance, "instance_color", instance=instance["id"], actual=actual_color, expected=instance["color"])
        if group["status"] == "missing_sprite":
            check(group["name"] == "Shadow" and len(instances) == 0, "known_shadow_placeholder", actor=key)
        if group["status"] in ("disabled", "suppressed_split_source"):
            check(len(instances) > 0 and not expected_visible, "hidden_sprite_preserved", actor=key)
        if group["name"] == "PlayerVisual":
            check(len(instances) == 1 and expected_sprite["ppu"] == 80 and expected_sprite["pivot"] == [0.5, 0.08], "player_runtime_pivot_ppu", actor=key)
        groups.append({"id": group["id"], "kind": group["kind"], "status": group["status"], "instances_checked": len(instances),
                       "target_visible": expected_visible, "player_visual_replaced_by_pawn": is_player_placeholder,
                       "feet_depth_readback": depth_expected[group["id"]]})
    player_readback = validate_scene_player(scene, actors, manifest, layers, player)
    cameras_readback = []
    for camera in scene["cameras"]:
        key = "camera:"+camera["object_id"]
        actor = actors[key]
        comp = actor.camera_component
        source_matrix = camera["world_matrix"]
        # Unity camera axes are right=+X, up=+Y, forward=+Z. The migration
        # remap sends these to UE +X, +Z, -Y respectively for these two cameras.
        def remapped_axis(column):
            axis = [source_matrix[0][column], -source_matrix[2][column], source_matrix[1][column]]
            length = distance(axis, [0, 0, 0])
            return [v/length for v in axis]
        expected_forward, expected_right, expected_up = [remapped_axis(c) for c in (2, 0, 1)]
        actual_forward = xyz(actor.get_actor_forward_vector())
        actual_right = xyz(actor.get_actor_right_vector())
        actual_up = xyz(actor.get_actor_up_vector())
        rotation = actor.get_actor_rotation()
        transform = actor.get_actor_transform()
        actual_rotation = {name: float(getattr(rotation, name)) for name in ("pitch", "yaw", "roll")}
        camera_diagnostics = {
            "actor_id": key, "location_cm": xyz(actor.get_actor_location()),
            "rotation_degrees": actual_rotation,
            "quaternion_xyzw": [float(getattr(transform.rotation, name)) for name in "xyzw"],
            "forward": actual_forward, "right": actual_right, "up": actual_up,
            "expected_forward": expected_forward, "expected_right": expected_right, "expected_up": expected_up,
            "ortho_width_cm": float(comp.get_editor_property("ortho_width")),
            "preview_look": validate_camera_preview(comp, key),
        }
        cameras_readback.append(camera_diagnostics)
        check(distance(xyz(actor.get_actor_location()), source_position(camera["world_matrix"])) <= TOLERANCE_CM, "camera_position", actor=key)
        check(distance(actual_forward, expected_forward) < 0.00001, "camera_direction", actor=key,
              actual=actual_forward, expected=expected_forward, rotation_degrees=actual_rotation)
        check(distance(actual_right, expected_right) < 0.00001, "camera_right", actor=key,
              actual=actual_right, expected=expected_right, rotation_degrees=actual_rotation)
        check(distance(actual_up, expected_up) < 0.00001, "camera_up", actor=key,
              actual=actual_up, expected=expected_up, rotation_degrees=actual_rotation)
        check(comp.get_editor_property("projection_mode") == unreal.CameraProjectionMode.ORTHOGRAPHIC, "camera_projection", actor=key)
        check(abs(comp.get_editor_property("ortho_width")-3200) <= TOLERANCE_CM, "camera_width_3200cm", actor=key)
        check(abs(comp.get_editor_property("aspect_ratio")-16/9) < 0.000001, "camera_aspect_16_9", actor=key)
    world = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()
    mode = world.get_world_settings().get_editor_property("default_game_mode")
    expected_game_mode = ASSETS.load_asset(GAME_MODE_BP)
    check(isinstance(expected_game_mode, unreal.Blueprint)
          and mode == expected_game_mode.generated_class(), "jonggu_game_mode", scene=scene["name"])
    if mode:
        defaults = unreal.get_default_object(mode)
        check(defaults.get_editor_property("default_pawn_class") is None, "no_duplicate_default_pawn", scene=scene["name"])
        check(defaults.get_editor_property("hud_class") is None, "preview_no_hud", scene=scene["name"])
    collision_readback = validate_scene_collision(scene, manifest, collision_actors, actors, collision_contract, collision_classes)
    REPORT["scenes"].append({"name": scene["name"], "map": baseline["path"], "actors_checked": len(actors),
                             "managed_actors_checked": len(managed), "collision_readback": collision_readback,
                             "objects_checked": len(scene["objects"]), "groups": groups,
                             "cameras_readback": cameras_readback,
                             "player_readback": player_readback,
                             "group_status_counts": dict(status_counts),
                             "instances_checked": sum(len(g["instances"]) for g in scene["groups"]),
                             "tile_instances_checked": sum(len(g["instances"]) for g in scene["groups"] if g["kind"] == "tilemap")})


def main():
    require_unreal_project()
    unreal.AssetRegistryHelpers.get_asset_registry().scan_paths_synchronous([ROOT], force_rescan=True)
    source = DATA_ROOT.joinpath("render_manifest.json").read_bytes()
    manifest = json.loads(source)
    baseline_path = QA_ROOT / "import_report.json"
    if not baseline_path.is_file(): baseline_path = FIXTURE_ROOT / "import_report.json"
    baseline = json.loads(baseline_path.read_text(encoding="utf-8"))
    REPORT["manifest_sha256"] = hashlib.sha256(source).hexdigest()
    check(baseline.get("success") is True, "import_success")
    check(baseline.get("idempotency_verified") is True, "import_idempotency")
    check(baseline.get("manifest_sha256") == REPORT["manifest_sha256"], "import_manifest_hash")
    sprites, player = validate_assets(manifest)
    collision_contract = read_collision_contract()
    collision_classes = validate_collision_blueprints()
    baseline_scenes = {s["name"]: s for s in baseline["scenes"]}
    for scene in manifest["scenes"]:
        validate_scene(scene, baseline_scenes[scene["name"]], manifest, sprites, player, collision_contract, collision_classes)
    REPORT["total_instances_checked"] = sum(s["instances_checked"] for s in REPORT["scenes"])
    REPORT["tile_instances_checked"] = sum(s["tile_instances_checked"] for s in REPORT["scenes"])
    check(REPORT["total_instances_checked"] == 2609, "planned_render_instances_2609")
    check(REPORT["tile_instances_checked"] == 2504, "planned_tiles_2504")
    REPORT["success"] = not REPORT["failures"]


if __name__ == "__main__":
    try:
        main()
    except Exception:
        REPORT["success"] = False
        REPORT["exception"] = traceback.format_exc()
    finally:
        REPORT["finished"] = time.strftime("%Y-%m-%dT%H:%M:%S")
        QA_ROOT.mkdir(parents=True, exist_ok=True)
        QA_ROOT.joinpath("unreal_readback.json").write_text(json.dumps(REPORT, ensure_ascii=False, indent=2), encoding="utf-8")
    if not REPORT.get("success"):
        raise RuntimeError("JONGGU_READBACK_FAILED: " + json.dumps({"failures": REPORT["failures"][:10], "exception": REPORT.get("exception")}, ensure_ascii=False))
    unreal.log("JONGGU_READBACK_SUCCESS: %d instances, %d checks, max geometry error %.8f cm" %
               (REPORT["total_instances_checked"], REPORT["checks"], REPORT["max_errors"]["geometry_cm"]))
