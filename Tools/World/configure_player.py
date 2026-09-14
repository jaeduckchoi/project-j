"""Author the Blueprint Jonggu player without changing source migration manifests.

Imported by import_unreal.py. Executing this file in Unreal runs that same
idempotent import, including the player integration and saved-level validation.
"""
from __future__ import annotations

import copy
import json
from pathlib import Path
import sys

import unreal

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))
from world_paths import CONTENT_ROOT, QA_ROOT
from build_player_blueprint import ensure_player_blueprint
from configure_player_walk import ensure_walk_parts, apply_walk_defaults

PLAYER_PATH = CONTENT_ROOT + "/Blueprints/Player/BP_JongguPlayer"
GAME_MODE_PATH = CONTENT_ROOT + "/Blueprints/Game/BP_JongguGameMode"
PARENT_PLAYER_PATH = "/Script/Engine.Pawn"
RUNTIME_ROOT = CONTENT_ROOT + "/Sprites/Player/Runtime"
ASSETS = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()


def runtime_sprite_specs(manifest):
    """Use the six source frames, including the PPU-32 front idle exception."""
    assets = {a["relative_path"].replace("\\", "/"): a for a in manifest["assets"]}
    prefix = "Assets/Resources/Generated/Sprites/Player/"
    result = []
    for direction in ("front", "back", "side"):
        for frame, suffix in enumerate(("base/" + direction + ".png", "idle/" + direction + "/frame_2.png")):
            asset = assets[prefix + suffix]
            if (asset["width"], asset["height"]) != (61, 82) or len(asset["sprites"]) != 1:
                raise ValueError("Unexpected player frame dimensions or sprite split: " + suffix)
            result.append({"direction": direction, "frame": frame, "texture_guid": asset["guid"],
                           "source_sprite_key": asset["sprites"][0], "source_path": asset["relative_path"],
                           "source_ppu": asset["ppu"], "rect": [0, 0, 61, 82],
                           "ppu": 80.0, "pivot": [0.5, 0.08]})
    return result


def player_source(scene):
    roots = [o for o in scene["objects"] if o["path"] == "SceneGameplayRoot/PlayerRoot"]
    groups = [g for g in scene["groups"] if g["path"] == "SceneGameplayRoot/PlayerRoot/Jonggu/PlayerVisual"]
    characters = [o for o in scene["objects"] if o["path"] == "SceneGameplayRoot/PlayerRoot/Jonggu"]
    if len(roots) != 1 or len(groups) != 1 or len(characters) != 1 or len(groups[0]["instances"]) != 1:
        raise ValueError("Expected one PlayerRoot, Jonggu and single player sprite in " + scene["name"])
    root, group, character = roots[0], groups[0], characters[0]
    target_matrix = copy.deepcopy(root["world_matrix"])
    if scene["name"] == "Beach":
        # The source root is at (0,0), while the actual Jonggu child is (17.5,12).
        # Place the collision/movement origin at the character, then attach every
        # existing source child with KEEP_WORLD so the static source view stays exact.
        for row in range(3):
            target_matrix[row][3] = character["world_matrix"][row][3]
    return {"root": root, "group": group, "target_matrix": target_matrix,
            "root_rebased": target_matrix != root["world_matrix"]}


def set_metadata(obj, key, value):
    ASSETS.set_metadata_tag(obj, key, str(value))


def save_asset(obj):
    if not obj.get_path_name().startswith(CONTENT_ROOT + "/"):
        raise RuntimeError("Player authoring cannot save outside its project content")
    if not ASSETS.save_loaded_asset(obj, only_if_is_dirty=False):
        raise RuntimeError("Cannot save " + obj.get_path_name())


def create_runtime_sprite(spec, texture, material):
    folder = RUNTIME_ROOT + "/" + spec["direction"].title()
    name = "S_Player_" + spec["direction"].title() + "_" + str(spec["frame"])
    path = folder + "/" + name
    sprite = ASSETS.load_asset(path) if ASSETS.does_asset_exist(path) else None
    if sprite is None:
        sprite = TOOLS.create_asset(name, folder, unreal.PaperSprite, unreal.PaperSpriteFactory())
    if not isinstance(sprite, unreal.PaperSprite):
        raise RuntimeError("Runtime sprite path is not a PaperSprite: " + path)
    sprite.set_editor_property("source_texture", texture)
    sprite.set_editor_property("source_uv", unreal.Vector2D(0, 0))
    sprite.set_editor_property("source_dimension", unreal.Vector2D(61, 82))
    sprite.set_editor_property("pixels_per_unreal_unit", 0.8)
    sprite.set_editor_property("snap_pivot_to_pixel_grid", False)
    sprite.set_editor_property("pivot_mode", unreal.SpritePivotMode.CUSTOM)
    sprite.set_editor_property("custom_pivot_point", unreal.Vector2D(30.5, 75.44))
    sprite.set_editor_property("sprite_collision_domain", unreal.SpriteCollisionMode.NONE)
    geometry = sprite.get_editor_property("render_geometry")
    geometry.set_editor_property("geometry_type", unreal.SpritePolygonMode.SOURCE_BOUNDING_BOX)
    sprite.set_editor_property("render_geometry", geometry)
    sprite.set_editor_property("default_material", material)
    set_metadata(sprite, "PlayerRuntimeSprite", "True")
    set_metadata(sprite, "PlayerRuntimeSourceGuid", spec["texture_guid"])
    set_metadata(sprite, "PlayerRuntimeSourceSpriteKey", spec["source_sprite_key"])
    set_metadata(sprite, "PlayerRuntimeContract", json.dumps(spec, sort_keys=True))
    save_asset(sprite)
    return sprite


def apply_player_defaults(defaults, frames):
    for direction in ("front", "back", "side"):
        defaults.set_editor_property(direction + "_frames", frames[direction])
    defaults.set_editor_property("idle_frames_per_second", 1.0 / 0.3)
    defaults.set_editor_property("side_sprite_faces_left", True)
    defaults.set_editor_property("move_speed", 400.0)
    defaults.set_editor_property("auto_possess_player", unreal.AutoReceiveInput.PLAYER0)
    defaults.set_editor_property("auto_possess_ai", unreal.AutoPossessAI.DISABLED)
    apply_walk_defaults(defaults)


def ensure_player_assets(manifest, textures, material):
    frames = {direction: [] for direction in ("front", "back", "side")}
    sprite_rows = []
    for spec in runtime_sprite_specs(manifest):
        sprite = create_runtime_sprite(spec, textures[spec["texture_guid"]], material)
        frames[spec["direction"]].append(sprite)
        sprite_rows.append(dict(spec, unreal_path=sprite.get_path_name()))
    bp = ensure_player_blueprint()
    ensure_walk_parts(frames, material)
    if not isinstance(bp, unreal.Blueprint) or unreal.BlueprintEditorLibrary.get_blueprint_parent_class(bp) != unreal.Pawn.static_class():
        raise RuntimeError("Player Blueprint has an unexpected parent: " + PLAYER_PATH)
    unreal.BlueprintEditorLibrary.compile_blueprint(bp)
    defaults = unreal.get_default_object(bp.generated_class())
    apply_player_defaults(defaults, frames)
    unreal.BlueprintEditorLibrary.compile_blueprint(bp)
    # Blueprint CDO properties must be serialized even if compilation reinstanced it.
    apply_player_defaults(unreal.get_default_object(bp.generated_class()), frames)
    # SCS component templates are separate from the Blueprint's CDO; initialize
    # the template as well so a newly placed player is visible before Play.
    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    library = unreal.SubobjectDataBlueprintFunctionLibrary
    for handle in subsystem.k2_gather_subobject_data_for_blueprint(bp):
        template = library.get_object_for_blueprint(library.get_data(handle), bp)
        if isinstance(template, unreal.PaperSpriteComponent) and template.get_name().startswith("player_visual"):
            template.set_sprite(frames["front"][0])
            template.set_material(0, material)
    if not unreal.BlueprintEditorLibrary.compile_blueprint(bp):
        raise RuntimeError("Cannot compile player with its frame assets")
    apply_player_defaults(unreal.get_default_object(bp.generated_class()), frames)
    set_metadata(bp, "PlayerAuthoringVersion", "blueprint-walk-2")
    save_asset(bp)
    return {"blueprint": bp, "class": bp.generated_class(), "frames": frames, "material": material,
            "sprites": sprite_rows, "scenes": []}


def transform_record(transform):
    return {"translation": [float(transform.translation.x), float(transform.translation.y), float(transform.translation.z)],
            "rotation": [float(transform.rotation.x), float(transform.rotation.y), float(transform.rotation.z), float(transform.rotation.w)],
            "scale": [float(transform.scale3d.x), float(transform.scale3d.y), float(transform.scale3d.z)]}


def configure_scene_player(scene, source, nodes, render_actors, assets, converted_transform, configure_render_component, layer_order):
    pawn = nodes[source["root"]["id"]]
    group = source["group"]
    placeholder = render_actors[group["id"]]
    apply_player_defaults(pawn, assets["frames"])
    expected_world = converted_transform(group["instances"][0]["world_matrix"])
    root_scale = pawn.get_actor_scale3d()
    pawn.set_editor_property("visual_scale", unreal.Vector(
        expected_world.scale3d.x / root_scale.x,
        expected_world.scale3d.y / root_scale.y,
        expected_world.scale3d.z / root_scale.z))
    pawn.set_editor_property("facing_direction", 0)
    pawn.set_editor_property("is_moving", False)
    pawn.set_editor_property("current_frame_index", 0)
    # Exposed actor property edits rerun SCS. Fetch the final live component only
    # after these edits, otherwise a replaced component can silently lose writes.
    comp = pawn.get_editor_property("player_visual")
    if comp is None:
        raise RuntimeError("Player Blueprint has no PaperSpriteComponent")
    # Native setters avoid PostEditChange rerunning SCS halfway through component
    # authoring. The general imported-actor helper uses set_editor_property.
    comp.set_mobility(unreal.ComponentMobility.MOVABLE)
    comp.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
    comp.set_cast_shadow(False)
    comp.set_translucent_sort_priority(layer_order[group.get("sorting_layer_id", 0)] * 100000 + group["sorting_order"])
    comp.set_visibility(group["status"] == "visible", True)
    comp.set_hidden_in_game(group["status"] != "visible", True)
    comp.set_material(0, assets["material"])
    comp.set_sprite_color(unreal.LinearColor(*group["instances"][0]["color"]))
    # Its sprite component is already attached to the Pawn's root; Unreal derives
    # the relative transform from the original world transform without moving it.
    comp.set_world_transform(expected_world, False, True)
    # Construction remains a static first-frame preview; the saved Blueprint
    # function animates only in game. This also avoids game-time reads in editor.
    comp.set_sprite(assets["frames"]["front"][0])
    placeholder_comp = placeholder.get_component_by_class(unreal.PaperSpriteComponent)
    placeholder_comp.set_visibility(False, True)
    placeholder_comp.set_hidden_in_game(True, True)
    placeholder.set_actor_hidden_in_game(True)
    placeholder.set_is_temporarily_hidden_in_editor(True)
    set_metadata(placeholder, "PlayerTargetOverride", "Source visual retained as hidden placeholder; Pawn.PlayerVisual renders it")
    set_metadata(pawn, "PlayerSourceRootId", source["root"]["id"])
    set_metadata(pawn, "PlayerSourceVisualId", group["id"])
    set_metadata(pawn, "PlayerSourceWorldMatrix", json.dumps(source["root"]["world_matrix"]))
    set_metadata(pawn, "PlayerTargetWorldMatrix", json.dumps(source["target_matrix"]))
    row = {"scene": scene["name"], "pawn_actor_id": "node:" + source["root"]["id"],
           "placeholder_actor_id": "render:" + group["id"], "source_root_id": source["root"]["id"],
           "source_visual_id": group["id"], "source_root_world_matrix": source["root"]["world_matrix"],
           "target_root_world_matrix": source["target_matrix"], "root_rebased": source["root_rebased"],
           "target_root_world_transform": transform_record(converted_transform(source["target_matrix"])),
           "source_visual_world_transform": transform_record(expected_world),
           "initial_visual_world_transform": transform_record(comp.get_world_transform()),
           "child_attachment_rule": "KEEP_WORLD", "placeholder_hidden": True,
           "frame_assets": {d: [s.get_path_name() for s in assets["frames"][d]] for d in assets["frames"]},
           "auto_possess_player": "PLAYER0", "move_speed_cm_per_second": 400.0,
           "sprite_color": group["instances"][0]["color"],
           "material": assets["material"].get_path_name(),
           "sort_priority": layer_order[group.get("sorting_layer_id", 0)] * 100000 + group["sorting_order"],
           "idle_frame_seconds": 0.3, "walking_poses": 8,
           "walk_cycle_distance_cm": 160.0, "walk_parts_from_original_texture": True,
           "source_manifest_modified": False, "saved_level_reopen_verified": False}
    assets["scenes"].append(row)
    return row


def validate_reopened_player(actors, row, assets):
    pawn = actors[row["pawn_actor_id"]]
    if pawn.get_class() != assets["class"]:
        raise RuntimeError("Reopened PlayerRoot is not BP_JongguPlayer")
    if pawn.get_editor_property("auto_possess_player") != unreal.AutoReceiveInput.PLAYER0:
        raise RuntimeError("Reopened PlayerRoot does not auto-possess player 0")
    for prop, expected_value in (("idle_frames_per_second", 1.0 / 0.3), ("move_speed", 400.0)):
        if abs(float(pawn.get_editor_property(prop)) - expected_value) > 0.0001:
            raise RuntimeError("Reopened player setting differs: " + prop)
    if not pawn.get_editor_property("side_sprite_faces_left"):
        raise RuntimeError("Reopened side-frame source orientation differs")
    actual_root = transform_record(pawn.get_actor_transform())
    for key in ("translation", "rotation", "scale"):
        if max(abs(a - b) for a, b in zip(actual_root[key], row["target_root_world_transform"][key])) > 0.0001:
            raise RuntimeError("Reopened PlayerRoot transform differs: " + key)
    comp = pawn.get_editor_property("player_visual")
    if comp.get_sprite() != assets["frames"]["front"][0]:
        raise RuntimeError("Reopened player does not show the first front frame")
    for direction in ("front", "back", "side"):
        parts = pawn.get_editor_property(direction + "_walk_parts")
        if len(parts) != 4 or any(part is None for part in parts):
            raise RuntimeError("Reopened player has incomplete walk parts: " + direction)
    for direction, expected in row["frame_assets"].items():
        if [s.get_path_name() for s in pawn.get_editor_property(direction + "_frames")] != expected:
            raise RuntimeError("Reopened player animation frames differ: " + direction)
    actual = transform_record(comp.get_world_transform())
    expected = row["source_visual_world_transform"]
    for key in ("translation", "rotation", "scale"):
        if max(abs(a - b) for a, b in zip(actual[key], expected[key])) > 0.0001:
            raise RuntimeError("Reopened player changed initial visual " + key)
    if not comp.get_editor_property("visible") or comp.get_editor_property("hidden_in_game"):
        raise RuntimeError("Reopened player visual is hidden")
    color = comp.get_editor_property("sprite_color")
    if max(abs(a - b) for a, b in zip((color.r, color.g, color.b, color.a), row["sprite_color"])) > 0.000001:
        raise RuntimeError("Reopened player tint differs")
    if comp.get_material(0).get_path_name() != row["material"] or comp.get_editor_property("translucency_sort_priority") != row["sort_priority"]:
        raise RuntimeError("Reopened player material or sorting differs")
    placeholder = actors[row["placeholder_actor_id"]].get_component_by_class(unreal.PaperSpriteComponent)
    if placeholder.get_sprite() is None or placeholder.get_editor_property("visible") or not placeholder.get_editor_property("hidden_in_game"):
        raise RuntimeError("Reopened source player placeholder is not preserved and hidden")
    row["saved_level_reopen_verified"] = True
    row["reopened_visual_world_transform"] = actual


def write_player_report(assets, manifest_sha256):
    report = {"schema_version": 1, "success": all(s["saved_level_reopen_verified"] for s in assets["scenes"]),
              "parent_class": PARENT_PLAYER_PATH, "player_blueprint": PLAYER_PATH,
              "runtime_implementation": "Blueprint + Engine/Paper2D components",
              "project_native_module": False, "runtime_python": False,
              "game_mode_blueprint": GAME_MODE_PATH, "source_manifest_sha256": manifest_sha256,
              "source_manifest_modified": False, "runtime_sprite_count": len(assets["sprites"]),
              "walk_part_sprite_count": 12, "walking_poses": 8,
              "walk_art_source": "Existing front/back/side textures; no raster edits",
              "runtime_sprites": assets["sprites"], "scenes": assets["scenes"],
              "game_mode_default_pawn": None, "game_mode_hud": None,
              "runtime_execution_tested": False,
              "note": "Authoring and saved-level checks only. Runtime movement/animation must be tested separately."}
    QA_ROOT.mkdir(parents=True, exist_ok=True)
    (QA_ROOT / "player_authoring_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    import runpy
    runpy.run_path(str(HERE / "import_unreal.py"), run_name="__main__")
