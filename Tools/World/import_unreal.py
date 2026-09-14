"""Import the reviewed Unity manifest into isolated, editable Unreal 5.8 levels.

Run via run_migration.ps1. This file executes inside Unreal's Python environment.
All persistent content mutations are confined to the configured project content root.
"""
from __future__ import annotations

import hashlib
import json
import math
import re
import sys
import time
from pathlib import Path
import unreal

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))
from configure_preview_look import ensure_preview_material, configure_preview_camera, validate_preview_camera
from configure_player import (GAME_MODE_PATH, configure_scene_player, ensure_player_assets,
                              player_source, validate_reopened_player, write_player_report)
from world_paths import CONTENT_ROOT, DATA_ROOT, QA_ROOT, require_unreal_project, require_full_import, resolve_source_path
from configure_collision import (prepare_collision, build_scene_collision,
                                 validate_reopened_collision, write_collision_report)
from configure_depth_sorting import prepare_depth_sort, apply_scene_depth_sort, validate_reopened_depth_sort
ROOT = CONTENT_ROOT
TAG = "JongguMigration:"
ASSETS = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
ACTORS = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
LEVELS = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()
REPORT = {"engine": unreal.SystemLibrary.get_engine_version(), "started": time.strftime("%Y-%m-%dT%H:%M:%S"), "assets": {}, "scenes": [], "warnings": []}


def log(message):
    unreal.log("JONGGU: " + str(message))


def asset_name(prefix, label, identity):
    readable = re.sub(r"[^A-Za-z0-9_]", "_", label).strip("_")[:60] or "Asset"
    return prefix + readable + "_" + hashlib.sha256(identity.encode()).hexdigest()[:12]


def owned_path(path):
    if not path.startswith(ROOT + "/"):
        raise ValueError("Refusing to modify an asset outside migration folder: " + path)
    return path


def source_category(asset):
    """Retain the artist's source categories below Textures/Sprites."""
    relative = asset["relative_path"].replace("\\", "/")
    suffix = relative.split("/Generated/Sprites/", 1)[-1]
    folders = suffix.split("/")[:-1]
    return "/".join(re.sub(r"[^A-Za-z0-9_]", "_", x).strip("_") or "Shared" for x in folders) or "Shared"


def relocate_flat_asset(legacy, destination, name):
    """Unreal rename updates loaded references and leaves compatibility redirectors."""
    if legacy == destination or ASSETS.does_asset_exist(destination):
        return
    if ASSETS.does_asset_exist(legacy):
        obj = ASSETS.load_asset(legacy)
        if not TOOLS.rename_assets([unreal.AssetRenameData(obj, destination.rsplit("/", 1)[0], name)]):
            raise RuntimeError("Cannot organize " + legacy)


def metadata(obj, name, value):
    ASSETS.set_metadata_tag(obj, name, str(value))


def save(obj):
    owned_path(obj.get_path_name())
    if not ASSETS.save_loaded_asset(obj, only_if_is_dirty=False):
        raise RuntimeError("Could not save " + obj.get_path_name())


def identity_of(actor):
    return next((str(t)[len(TAG):] for t in actor.tags if str(t).startswith(TAG)), None)


def tag_actor(actor, identity, path, status="visible"):
    actor.tags = [TAG + identity, "UnityPath:" + path, "UnityStatus:" + status]
    actor.set_actor_label(path.rsplit("/", 1)[-1] or identity, mark_dirty=True)
    folder = "/".join(path.split("/")[:-1])
    actor.set_folder_path("Unity/" + folder if folder else "Unity")


def converted_transform(matrix):
    """Map Unity XY into Paper2D XZ; reject shear instead of silently losing it."""
    a, b = matrix[0][0], matrix[0][1]
    c, d = matrix[1][0], matrix[1][1]
    sx = math.hypot(a, c)
    if sx < 1e-10:
        raise ValueError("Zero in-plane X scale")
    sz = (a * d - b * c) / sx
    if abs(sz) < 1e-10:
        raise ValueError("Zero in-plane Y scale")
    if abs(a * b + c * d) > 1e-5 * max(1.0, sx * abs(sz)):
        raise ValueError("Unsupported in-plane shear")
    if any(abs(matrix[i][j]) > 1e-6 for i, j in [(2, 0), (2, 1), (0, 2), (1, 2)]):
        raise ValueError("A world object leaves the source 2D plane")
    angle = math.atan2(c, a)
    result = unreal.Transform()
    result.translation = unreal.Vector(100 * matrix[0][3], -100 * matrix[2][3], 100 * matrix[1][3])
    result.rotation = unreal.Quat(0, -math.sin(angle / 2), 0, math.cos(angle / 2))
    result.scale3d = unreal.Vector(sx, 1, sz)
    return result


def import_textures(manifest):
    result = {}
    tasks = []
    for a in manifest["assets"]:
        name = asset_name("T_", Path(a["relative_path"]).stem, a["guid"])
        folder = ROOT + "/Textures/" + source_category(a)
        path = folder + "/" + name
        relocate_flat_asset(ROOT + "/Textures/" + name, path, name)
        current = ASSETS.load_asset(path) if ASSETS.does_asset_exist(path) else None
        digest = hashlib.sha256(Path(a["source_path"]).read_bytes()).hexdigest()
        if current is None or ASSETS.get_metadata_tag(current, "UnitySourceHash") != digest:
            task = unreal.AssetImportTask()
            task.filename = a["source_path"]
            task.destination_path = folder
            task.destination_name = name
            task.automated = True
            task.replace_existing = True
            task.replace_existing_settings = True
            task.save = False
            task.factory = unreal.TextureFactory()
            tasks.append(task)
        a["_ue_path"] = path
        a["_source_hash"] = digest
    # TextureFactory avoids a modal Interchange pipeline dialog in unattended jobs.
    if tasks:
        TOOLS.import_asset_tasks(tasks)
    for a in manifest["assets"]:
        tex = ASSETS.load_asset(a["_ue_path"])
        if not isinstance(tex, unreal.Texture2D):
            raise RuntimeError("Texture import failed: " + a["source_path"])
        settings = {
            "compression_settings": unreal.TextureCompressionSettings.TC_EDITOR_ICON,
            "mip_gen_settings": unreal.TextureMipGenSettings.TMGS_NO_MIPMAPS,
            "lod_group": unreal.TextureGroup.TEXTUREGROUP_PIXELS2D,
            "filter": {0: unreal.TextureFilter.TF_NEAREST, 1: unreal.TextureFilter.TF_BILINEAR, 2: unreal.TextureFilter.TF_TRILINEAR}.get(a["filter_mode"], unreal.TextureFilter.TF_DEFAULT),
            "srgb": bool(a.get("srgb", True)), "never_stream": True,
        }
        changed = any(tex.get_editor_property(k) != v for k, v in settings.items()) or ASSETS.get_metadata_tag(tex, "UnitySourceHash") != a["_source_hash"]
        if changed:
            for k, v in settings.items():
                tex.set_editor_property(k, v)
            metadata(tex, "UnityGuid", a["guid"])
            metadata(tex, "UnitySource", a["relative_path"])
            metadata(tex, "UnitySourceHash", a["_source_hash"])
            save(tex)
        result[a["guid"]] = tex
    REPORT["assets"]["textures"] = len(result)
    REPORT["assets"]["textures_reimported"] = len(tasks)
    log("Imported/updated %d textures" % len(result))
    return result


def import_material():
    path = ROOT + "/Materials/MI_Jonggu_TranslucentUnlit"
    created = not ASSETS.does_asset_exist(path)
    mat = ASSETS.load_asset(path) if not created else TOOLS.duplicate_asset("MI_Jonggu_TranslucentUnlit", ROOT + "/Materials", unreal.load_asset("/Paper2D/TranslucentUnlitSpriteMaterial"))
    if mat is None:
        raise RuntimeError("Paper2D unlit material is unavailable")
    override = mat.get_editor_property("base_property_overrides")
    if not (override.get_editor_property("override_two_sided") and override.get_editor_property("two_sided")):
        override.set_editor_property("override_two_sided", True)
        override.set_editor_property("two_sided", True)
        mat.set_editor_property("base_property_overrides", override)
        metadata(mat, "MigrationPurpose", "Unity 2D straight-alpha, unlit, two-sided vertex-colored sprites")
        save(mat)
    elif created:
        save(mat)
    return mat


def import_sprites(manifest, textures, material):
    result = {}
    dims = {a["guid"]: (a["width"], a["height"]) for a in manifest["assets"]}
    categories = {a["guid"]: source_category(a) for a in manifest["assets"]}
    for key, s in manifest["sprites"].items():
        if s["texture_guid"] not in textures:
            raise ValueError("Unknown texture for sprite " + key)
        name = asset_name("S_", s["name"], key)
        folder = ROOT + "/Sprites/" + categories[s["texture_guid"]]
        path = folder + "/" + name
        relocate_flat_asset(ROOT + "/Sprites/" + name, path, name)
        sprite = ASSETS.load_asset(path) if ASSETS.does_asset_exist(path) else TOOLS.create_asset(name, folder, unreal.PaperSprite, unreal.PaperSpriteFactory())
        x, y, w, h = s["rect"]
        if any(abs(v - round(v)) > 1e-5 for v in [x, y, w, h]):
            raise ValueError("Fractional sprite region requires explicit UV handling: " + key)
        ue_y = dims[s["texture_guid"]][1] - y - h
        contract = json.dumps({k: s[k] for k in ["rect", "pivot", "ppu"]})
        current_uv = sprite.get_editor_property("source_uv")
        current_dim = sprite.get_editor_property("source_dimension")
        current_pivot = sprite.get_editor_property("custom_pivot_point")
        expected_pivot = (x + s["pivot"][0] * w, ue_y + (1 - s["pivot"][1]) * h)
        unchanged = (
            ASSETS.get_metadata_tag(sprite, "UnitySpriteContract") == contract
            and sprite.get_editor_property("source_texture") == textures[s["texture_guid"]]
            and abs(sprite.get_editor_property("pixels_per_unreal_unit") - s["ppu"] / 100) < 1e-6
            and abs(current_uv.x - x) < 1e-4 and abs(current_uv.y - ue_y) < 1e-4
            and abs(current_dim.x - w) < 1e-4 and abs(current_dim.y - h) < 1e-4
            and abs(current_pivot.x - expected_pivot[0]) < 1e-4 and abs(current_pivot.y - expected_pivot[1]) < 1e-4
            and sprite.get_editor_property("pivot_mode") == unreal.SpritePivotMode.CUSTOM
            and not sprite.get_editor_property("snap_pivot_to_pixel_grid")
            and sprite.get_editor_property("sprite_collision_domain") == unreal.SpriteCollisionMode.NONE
            and sprite.get_editor_property("render_geometry").get_editor_property("geometry_type") == unreal.SpritePolygonMode.SOURCE_BOUNDING_BOX
            and sprite.get_editor_property("default_material") == material
        )
        if unchanged:
            result[key] = sprite
            continue
        # Changing these editor properties regenerates Paper2D's baked vertex data.
        sprite.set_editor_property("source_texture", textures[s["texture_guid"]])
        sprite.set_editor_property("source_uv", unreal.Vector2D(x, ue_y))
        sprite.set_editor_property("source_dimension", unreal.Vector2D(w, h))
        sprite.set_editor_property("pixels_per_unreal_unit", s["ppu"] / 100.0)
        sprite.set_editor_property("snap_pivot_to_pixel_grid", False)
        sprite.set_editor_property("pivot_mode", unreal.SpritePivotMode.CUSTOM)
        sprite.set_editor_property("custom_pivot_point", unreal.Vector2D(x + s["pivot"][0] * w, ue_y + (1 - s["pivot"][1]) * h))
        sprite.set_editor_property("sprite_collision_domain", unreal.SpriteCollisionMode.NONE)
        geometry = sprite.get_editor_property("render_geometry")
        geometry.set_editor_property("geometry_type", unreal.SpritePolygonMode.SOURCE_BOUNDING_BOX)
        sprite.set_editor_property("render_geometry", geometry)
        sprite.set_editor_property("default_material", material)
        metadata(sprite, "UnitySpriteKey", key)
        metadata(sprite, "UnitySourceSpriteKey", s.get("source_sprite_key", key))
        metadata(sprite, "UnitySpriteContract", contract)
        save(sprite)
        result[key] = sprite
    REPORT["assets"]["sprites"] = len(result)
    log("Created/updated %d sprites" % len(result))
    return result


def import_preview_game_mode():
    path = GAME_MODE_PATH
    folder, name = path.rsplit("/", 1)
    bp = ASSETS.load_asset(path) if ASSETS.does_asset_exist(path) else None
    if bp is None:
        factory = unreal.BlueprintFactory()
        factory.set_editor_property("parent_class", unreal.GameModeBase)
        bp = TOOLS.create_asset(name, folder, unreal.Blueprint, factory)
    else:
        defaults = unreal.get_default_object(bp.generated_class())
        if defaults.get_editor_property("default_pawn_class") is None and defaults.get_editor_property("hud_class") is None:
            return bp.generated_class()
    unreal.BlueprintEditorLibrary.compile_blueprint(bp)
    defaults = unreal.get_default_object(bp.generated_class())
    defaults.set_editor_property("default_pawn_class", None)
    defaults.set_editor_property("hud_class", None)
    save(bp)
    return bp.generated_class()


def ensure_actor(existing, identity, cls, path, matrix=None, status="visible"):
    actor = existing.get(identity)
    # Blueprint generated classes are UClass objects, not Python type objects.
    try:
        same_class = actor is not None and isinstance(actor, cls)
    except TypeError:
        same_class = actor is not None and actor.get_class() == cls
    if actor and not same_class:
        ACTORS.destroy_actor(actor)
        actor = None
    if actor is None:
        actor = ACTORS.spawn_actor_from_class(cls, unreal.Vector())
    if actor is None:
        raise RuntimeError("Failed to create actor " + identity)
    # Detach first; otherwise editing parent transforms during a reimport moves children twice.
    actor.detach_from_actor(unreal.DetachmentRule.KEEP_WORLD, unreal.DetachmentRule.KEEP_WORLD, unreal.DetachmentRule.KEEP_WORLD)
    if matrix is not None:
        actor.set_actor_transform(converted_transform(matrix), sweep=False, teleport=True)
    tag_actor(actor, identity, path, status)
    hidden = status != "visible"
    actor.set_actor_hidden_in_game(hidden)
    actor.set_is_temporarily_hidden_in_editor(hidden)
    existing[identity] = actor
    return actor


def attach(actor, parent):
    actor.attach_to_actor(parent, "", unreal.AttachmentRule.KEEP_WORLD, unreal.AttachmentRule.KEEP_WORLD, unreal.AttachmentRule.KEEP_WORLD, weld_simulated_bodies=False)


def configure_render_component(comp, group, layer_order):
    comp.set_mobility(unreal.ComponentMobility.MOVABLE)
    comp.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
    comp.set_editor_property("cast_shadow", False)
    comp.set_translucent_sort_priority(layer_order[group.get("sorting_layer_id", 0)] * 100000 + group["sorting_order"])
    comp.set_visibility(group["status"] == "visible", True)
    comp.set_hidden_in_game(group["status"] != "visible", True)


def build_scene(scene, sprites, preview_game_mode, preview_material, player_assets, collision_assets, depth_specs):
    path = owned_path(ROOT + "/Maps/L_" + scene["name"])
    if ASSETS.does_asset_exist(path):
        if not LEVELS.load_level(path):
            raise RuntimeError("Cannot load " + path)
    elif not LEVELS.new_level(path):
        raise RuntimeError("Cannot create " + path)
    existing = {}
    for actor in ACTORS.get_all_level_actors():
        identity = identity_of(actor)
        if identity:
            if identity in existing:
                raise ValueError("Duplicate migration actor ID: " + identity)
            existing[identity] = actor
            actor.detach_from_actor(unreal.DetachmentRule.KEEP_WORLD, unreal.DetachmentRule.KEEP_WORLD, unreal.DetachmentRule.KEEP_WORLD)
    expected = set()
    nodes = {}
    player = player_source(scene)
    # Keep every source node ID. PlayerRoot becomes a placed native player Pawn;
    # other nodes remain mesh-less hierarchy Actors.
    for obj in scene["objects"]:
        key = "node:" + obj["id"]
        state = "visible" if obj["active_in_hierarchy"] else "inactive"
        is_player_root = obj["id"] == player["root"]["id"]
        node_class = player_assets["class"] if is_player_root else unreal.StaticMeshActor
        matrix = player["target_matrix"] if is_player_root else obj["world_matrix"]
        node = ensure_actor(existing, key, node_class, obj["path"], matrix, state)
        if not is_player_root:
            node.static_mesh_component.set_mobility(unreal.ComponentMobility.MOVABLE)
            node.static_mesh_component.set_static_mesh(None)
            node.static_mesh_component.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
        nodes[obj["id"]] = node
        expected.add(key)
    for obj in scene["objects"]:
        if obj.get("parent_id") in nodes:
            attach(nodes[obj["id"]], nodes[obj["parent_id"]])
    layer_order = {layer: i for i, layer in enumerate(sorted({g.get("sorting_layer_id", 0) for g in scene["groups"]}))}
    groups_report = []
    render_actors = {}
    for group in scene["groups"]:
        key = "render:" + group["id"]
        instances = group["instances"]
        single = group["kind"] == "sprite" and len(instances) == 1
        cls = unreal.PaperSpriteActor if single else unreal.PaperGroupedSpriteActor
        actor = ensure_actor(existing, key, cls, group["path"] + "/Visual", group["world_matrix"], group["status"])
        if single:
            instance = instances[0]
            actor.set_actor_transform(converted_transform(instance["world_matrix"]), sweep=False, teleport=True)
            comp = actor.get_component_by_class(unreal.PaperSpriteComponent)
            comp.set_mobility(unreal.ComponentMobility.MOVABLE)
            if not comp.set_sprite(sprites[instance["sprite_key"]]):
                # set_sprite also returns false when an existing asset was already assigned.
                if comp.get_sprite() != sprites[instance["sprite_key"]]:
                    raise RuntimeError("Sprite assignment failed for " + key)
            comp.set_sprite_color(unreal.LinearColor(*instance["color"]))
        else:
            comp = actor.get_component_by_class(unreal.PaperGroupedSpriteComponent)
            comp.clear_instances()
            comp.set_mobility(unreal.ComponentMobility.MOVABLE)
            for instance in instances:
                idx = comp.add_instance(converted_transform(instance["world_matrix"]), sprites[instance["sprite_key"]], True, unreal.LinearColor(*instance["color"]))
                if idx < 0:
                    raise RuntimeError("Could not place " + instance["id"])
            if comp.get_instance_count() != len(instances):
                raise RuntimeError("Grouped sprite count mismatch for " + key)
        configure_render_component(comp, group, layer_order)
        if group["object_id"] in nodes:
            attach(actor, nodes[group["object_id"]])
        metadata(actor, "UnityInstanceIds", json.dumps([i["id"] for i in instances]))
        groups_report.append({"source_id": group["id"], "actor_id": key, "status": group["status"], "kind": group["kind"], "instances": len(instances)})
        render_actors[group["id"]] = actor
        expected.add(key)
    player_report = configure_scene_player(scene, player, nodes, render_actors, player_assets,
                                           converted_transform, configure_render_component, layer_order)
    cameras_report = []
    for i, camera in enumerate(scene["cameras"]):
        if not camera.get("orthographic", True):
            raise ValueError("Perspective source camera is outside this migration scope")
        key = "camera:" + camera["object_id"]
        actor = ensure_actor(existing, key, unreal.CameraActor, "Camera/SourceCamera", camera["world_matrix"])
        actor.set_actor_rotation(unreal.Rotator(pitch=0, yaw=-90, roll=0), teleport_physics=True)
        comp = actor.camera_component
        comp.set_projection_mode(unreal.CameraProjectionMode.ORTHOGRAPHIC)
        comp.set_ortho_width(camera["orthographic_size"] * 2 * 100 * 16 / 9)
        comp.set_aspect_ratio(16 / 9)
        comp.set_editor_property("constrain_aspect_ratio", True)
        comp.set_editor_property("auto_calculate_ortho_planes", False)
        comp.set_editor_property("ortho_near_clip_plane", 0.1)
        comp.set_editor_property("ortho_far_clip_plane", 100000)
        preview_look = configure_preview_camera(comp, preview_material)
        actor.set_editor_property("auto_activate_for_player", unreal.AutoReceiveInput.PLAYER0 if i == 0 else unreal.AutoReceiveInput.DISABLED)
        metadata(actor, "UnityCamera", json.dumps(camera))
        expected.add(key)
        cameras_report.append({"actor_id": key, "ortho_width": comp.get_editor_property("ortho_width"), "preview_look": preview_look})
    collision_report = build_scene_collision(scene, nodes, existing, expected, collision_assets, ensure_actor, attach)
    depth_report = apply_scene_depth_sort(scene, render_actors, nodes, player_report, depth_specs)
    # Delete only obsolete actors bearing this migration's ownership tag, retaining user additions.
    removed = 0
    for key, actor in existing.items():
        if key not in expected:
            ACTORS.destroy_actor(actor)
            removed += 1
    # The placed Pawn auto-possesses player 0. DefaultPawnClass remains None so
    # GameMode never creates a second pawn at a PlayerStart or the world origin.
    world = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()
    world.get_world_settings().set_editor_property("default_game_mode", preview_game_mode)
    metadata(world, "UnitySourceScene", scene["name"])
    metadata(world, "MigrationManifestHash", REPORT["manifest_sha256"])
    if not LEVELS.save_current_level():
        raise RuntimeError("Cannot save " + path)
    actual = [identity_of(a) for a in ACTORS.get_all_level_actors() if identity_of(a)]
    if len(actual) != len(set(actual)) or set(actual) != expected:
        raise RuntimeError("Actor identity mismatch after saving " + path)
    result = {"name": scene["name"], "path": path, "owned_actors": len(actual), "source_actors": len(actual) - len(collision_report["specs"]), "collision_actors": len(collision_report["specs"]), "obsolete_removed": removed, "groups": groups_report, "cameras": cameras_report, "player": player_report, "collision": collision_report, "expected_actor_ids": sorted(expected)}
    result["depth_sorting"] = depth_report
    log("Saved %s: %d owned actors" % (path, len(actual)))
    return result


def validate_reopened(scene_report, player_assets):
    if not LEVELS.load_level(scene_report["path"]):
        raise RuntimeError("Reopen failed: " + scene_report["path"])
    actors = {identity_of(a): a for a in ACTORS.get_all_level_actors() if identity_of(a)}
    if sorted(actors) != scene_report["expected_actor_ids"]:
        raise RuntimeError("Reopened actor IDs differ")
    for g in scene_report["groups"]:
        actor = actors[g["actor_id"]]
        grouped = actor.get_component_by_class(unreal.PaperGroupedSpriteComponent)
        if grouped and grouped.get_instance_count() != g["instances"]:
            raise RuntimeError("Reopened grouped sprite count differs")
        sprite = actor.get_component_by_class(unreal.PaperSpriteComponent)
        if sprite and sprite.get_sprite() is None:
            raise RuntimeError("Reopened sprite reference is missing")
    for camera in scene_report["cameras"]:
        validate_preview_camera(actors[camera["actor_id"]].camera_component)
    validate_reopened_player(actors, scene_report["player"], player_assets)
    validate_reopened_collision(actors, scene_report["collision"])
    validate_reopened_depth_sort(actors, scene_report["depth_sorting"])
    world = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()
    game_class = world.get_world_settings().get_editor_property("default_game_mode")
    defaults = unreal.get_default_object(game_class)
    if defaults.get_editor_property("default_pawn_class") is not None or defaults.get_editor_property("hud_class") is not None:
        raise RuntimeError("Reopened game mode would spawn a second Pawn or template HUD")
    scene_report["reopen_verified"] = True


def main():
    require_unreal_project()
    require_full_import()
    unreal.AssetRegistryHelpers.get_asset_registry().scan_paths_synchronous([ROOT, "/Paper2D"], force_rescan=True)
    data = DATA_ROOT.joinpath("render_manifest.json").read_bytes()
    manifest = json.loads(data)
    if manifest["schema_version"] != 1:
        raise ValueError("Unsupported manifest version")
    REPORT["manifest_sha256"] = hashlib.sha256(data).hexdigest()
    REPORT["warnings"] = manifest.get("warnings", [])
    for source_asset in manifest["assets"]:
        source_asset["source_path"] = str(resolve_source_path(source_asset["relative_path"]))
    textures = import_textures(manifest)
    material = import_material()
    sprites = import_sprites(manifest, textures, material)
    player_assets = ensure_player_assets(manifest, textures, material)
    REPORT["assets"]["player_runtime_sprites"] = len(player_assets["sprites"])
    preview_game_mode = import_preview_game_mode()
    preview_material = ensure_preview_material()
    collision_assets = prepare_collision(manifest)
    depth_specs = prepare_depth_sort(manifest)
    REPORT["collision_rules_sha256"] = collision_assets["rules_sha256"]
    for scene in manifest["scenes"]:
        REPORT["scenes"].append(build_scene(scene, sprites, preview_game_mode, preview_material, player_assets, collision_assets, depth_specs))
    # Force an actual map transition before validation to exercise deserialization.
    for result in REPORT["scenes"]:
        validate_reopened(result, player_assets)
    previous_path = QA_ROOT / "import_report.json"
    previous = json.loads(previous_path.read_text(encoding="utf-8")) if previous_path.exists() else None
    if previous and previous.get("manifest_sha256") == REPORT["manifest_sha256"] and previous.get("collision_rules_sha256") == REPORT["collision_rules_sha256"] and previous.get("success"):
        old = {s["name"]: s for s in previous["scenes"]}
        for current in REPORT["scenes"]:
            baseline = old[current["name"]]
            if current["expected_actor_ids"] != baseline["expected_actor_ids"] or current["groups"] != baseline["groups"]:
                raise RuntimeError("Reimport changed scene identity/count contract")
            if current["depth_sorting"] != baseline["depth_sorting"]:
                raise RuntimeError("Reimport changed ground-anchor sorting")
            # Parent-relative serialization can change the last float bits; compare
            # independent saved shape snapshots with the same geometric tolerance.
            before = baseline["collision"]["snapshot"]
            after = current["collision"]["snapshot"]
            if len(before) != len(after):
                raise RuntimeError("Reimport changed collision count")
            for a, b in zip(before, after):
                for key in a:
                    if isinstance(a[key], list):
                        same = len(a[key]) == len(b[key]) and max((abs(x-y) for x, y in zip(a[key], b[key])), default=0) <= 0.01
                    elif isinstance(a[key], float):
                        same = abs(a[key] - b[key]) <= 0.01
                    else:
                        same = a[key] == b[key]
                    if not same:
                        raise RuntimeError("Reimport changed collision %s: %s" % (key, a["id"]))
        REPORT["idempotency_verified"] = True
    else:
        REPORT["idempotency_verified"] = False
    REPORT["success"] = True
    write_player_report(player_assets, REPORT["manifest_sha256"])
    write_collision_report()
    REPORT["finished"] = time.strftime("%Y-%m-%dT%H:%M:%S")
    QA_ROOT.mkdir(parents=True, exist_ok=True)
    previous_path.write_text(json.dumps(REPORT, ensure_ascii=False, indent=2), encoding="utf-8")
    # A recovered run must not leave a stale failure looking like the current result.
    QA_ROOT.joinpath("import_error.json").unlink(missing_ok=True)
    if "-run=" not in unreal.SystemLibrary.get_command_line().lower():
        LEVELS.load_level(ROOT + "/Maps/L_Hub")
        camera = next(a for a in ACTORS.get_all_level_actors() if isinstance(a, unreal.CameraActor) and identity_of(a))
        LEVELS.pilot_level_actor(camera)
        LEVELS.set_exact_camera_view(True)
        LEVELS.editor_set_game_view(True)
    log("IMPORT_SUCCESS")


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        REPORT["success"] = False
        REPORT["error"] = traceback.format_exc()
        QA_ROOT.mkdir(parents=True, exist_ok=True)
        QA_ROOT.joinpath("import_error.json").write_text(json.dumps(REPORT, ensure_ascii=False, indent=2), encoding="utf-8")
        raise
