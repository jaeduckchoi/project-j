"""Serialize asset-driven static collision without changing imported visuals."""
from __future__ import annotations

import hashlib
import json
import math
import unreal

from collision_geometry import load_rules, build_collision_specs
from world_paths import CONTENT_ROOT, DATA_ROOT, QA_ROOT

PROFILE = "JongguObstacle"
VERSION = "ground-footprints-1"
ASSETS = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()
REPORT = {"schema_version": 1, "success": False, "scenes": []}


def xyz(v):
    return [float(v.x), float(v.y), float(v.z)]


def identity(actor):
    return next((str(t)[len("JongguMigration:"):] for t in actor.tags
                 if str(t).startswith("JongguMigration:")), None) if actor else None


def configure_shape(component):
    component.set_mobility(unreal.ComponentMobility.MOVABLE)
    component.set_collision_profile_name(PROFILE)
    component.set_editor_property("generate_overlap_events", False)
    component.set_simulate_physics(False)
    component.set_hidden_in_game(True)
    component.set_editor_property("can_ever_affect_navigation", False)
    component.set_editor_property("shape_color", unreal.Color(70, 240, 115, 255))
    if component.get_collision_enabled() != unreal.CollisionEnabled.QUERY_ONLY:
        raise RuntimeError("JongguObstacle profile is missing; restart the editor after config update")
    if component.get_collision_response_to_channel(unreal.CollisionChannel.ECC_PAWN) != unreal.CollisionResponseType.ECR_BLOCK:
        raise RuntimeError("JongguObstacle must block Pawn")


def ensure_blueprint(shape):
    name = "BP_Collision" + shape.title()
    folder = CONTENT_ROOT + "/Blueprints/Collision"
    path = folder + "/" + name
    bp = ASSETS.load_asset(path) if ASSETS.does_asset_exist(path) else None
    if bp is None:
        factory = unreal.BlueprintFactory()
        factory.set_editor_property("parent_class", unreal.Actor)
        bp = TOOLS.create_asset(name, folder, unreal.Blueprint, factory)
    if not isinstance(bp, unreal.Blueprint) or unreal.BlueprintEditorLibrary.get_blueprint_parent_class(bp) != unreal.Actor.static_class():
        raise RuntimeError("Unexpected collision Blueprint type: " + path)
    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    library = unreal.SubobjectDataBlueprintFunctionLibrary
    handles = subsystem.k2_gather_subobject_data_for_blueprint(bp)
    component_class = unreal.BoxComponent if shape == "box" else unreal.SphereComponent
    found = [(h, library.get_object_for_blueprint(library.get_data(h), bp)) for h in handles]
    found = [(h, obj) for h, obj in found if isinstance(obj, component_class)]
    if len(found) > 1:
        raise RuntimeError("Duplicate collision root: " + path)
    if not found:
        handle, reason = subsystem.add_new_subobject(unreal.AddNewSubobjectParams(
            parent_handle=handles[0], new_class=component_class, blueprint_context=bp))
        if str(reason):
            raise RuntimeError("Cannot add collision shape: " + str(reason))
        subsystem.rename_subobject_member_variable(bp, handle, "collision_shape")
        if not subsystem.make_new_scene_root(handles[0], handle, bp):
            raise RuntimeError("Cannot assign collision shape as root")
        component = library.get_object_for_blueprint(library.get_data(handle), bp)
    else:
        handle, component = found[0]
    configure_shape(component)
    if shape == "box":
        component.set_box_extent(unreal.Vector(50, 100, 50), False)
    else:
        component.set_sphere_radius(24, False)
    if not unreal.BlueprintEditorLibrary.compile_blueprint(bp):
        raise RuntimeError("Cannot compile " + path)
    ASSETS.set_metadata_tag(bp, "JongguCollisionBuilderVersion", VERSION)
    ASSETS.set_metadata_tag(bp, "JongguCollisionPurpose", "Static asset footprint; collision_rules.json is authoritative")
    if not ASSETS.save_loaded_asset(bp, only_if_is_dirty=False):
        raise RuntimeError("Cannot save " + path)
    return bp.generated_class()


def prepare_collision(manifest):
    rules = load_rules(DATA_ROOT)
    digest = hashlib.sha256((DATA_ROOT / "collision_rules.json").read_bytes()).hexdigest()
    REPORT["rules_sha256"] = digest
    REPORT["scenes"] = []
    specs = {scene["name"]: build_collision_specs(scene, manifest, rules) for scene in manifest["scenes"]}
    for name, rows in specs.items():
        ids = [row["id"] for row in rows]
        if len(ids) != len(set(ids)) or any(not x.startswith("collision:") for x in ids):
            raise RuntimeError("Invalid generated collision IDs in " + name)
    return {"classes": {shape: ensure_blueprint(shape) for shape in ("box", "sphere")},
            "rules_sha256": digest, "specs": specs}


def snapshot(actor):
    root = actor.root_component
    box = isinstance(root, unreal.BoxComponent)
    if not box and not isinstance(root, unreal.SphereComponent):
        raise RuntimeError("Collision actor does not have a primitive root: " + actor.get_path_name())
    transform = root.get_world_transform()
    parent = identity(actor.get_attach_parent_actor())
    row = {"id": identity(actor), "parent_id": parent[len("node:"):] if parent and parent.startswith("node:") else parent,
           "shape": "box" if box else "sphere", "center_cm": xyz(transform.translation),
           "rotation_quat": [float(getattr(transform.rotation, v)) for v in ("x", "y", "z", "w")],
           "scale": xyz(transform.scale3d), "profile": str(root.get_collision_profile_name()),
           "enabled": str(root.get_collision_enabled()), "object_type": str(root.get_collision_object_type()),
           "pawn_response": str(root.get_collision_response_to_channel(unreal.CollisionChannel.ECC_PAWN)),
           "overlap": bool(root.get_editor_property("generate_overlap_events")), "simulate_physics": bool(root.is_simulating_physics())}
    if box:
        row["extent_cm"] = xyz(root.get_scaled_box_extent())
    else:
        row["radius_cm"] = float(root.get_scaled_sphere_radius())
    return row


def build_scene_collision(scene, nodes, existing, expected, prepared, ensure_actor, attach):
    rows = prepared["specs"][scene["name"]]
    actual = []
    for spec in rows:
        actor = ensure_actor(existing, spec["id"], prepared["classes"][spec["shape"]],
                             "Collision/" + spec["category"] + "/" + spec["label"])
        if spec["parent_id"] not in nodes:
            raise RuntimeError("Unknown collision source parent: " + spec["parent_id"])
        angle = math.radians(spec.get("rotation_degrees", 0))
        transform = unreal.Transform()
        transform.translation = unreal.Vector(*spec["center_cm"])
        transform.rotation = unreal.Quat(0, -math.sin(angle / 2), 0, math.cos(angle / 2))
        transform.scale3d = unreal.Vector(1, 1, 1)
        actor.set_actor_transform(transform, sweep=False, teleport=True)
        attach(actor, nodes[spec["parent_id"]])
        # Set dimensions after all instance edits that could rerun Blueprint SCS.
        root = actor.root_component
        configure_shape(root)
        if spec["shape"] == "box":
            root.set_box_extent(unreal.Vector(*spec["extent_cm"]), False)
        else:
            root.set_sphere_radius(float(spec["radius_cm"]), False)
        ASSETS.set_metadata_tag(actor, "JongguCollisionSpec", json.dumps(spec, sort_keys=True))
        ASSETS.set_metadata_tag(actor, "JongguCollisionRulesHash", prepared["rules_sha256"])
        kind = "boundary" if "bound" in spec["category"].lower() else "terrain" if any(t in spec["category"].lower() for t in ("water", "terrain", "shore")) else "object"
        actor.tags = list(actor.tags) + ["JongguCollisionKind:" + kind, "JongguCollisionCategory:" + spec["category"]]
        expected.add(spec["id"])
        row = snapshot(actor)
        if max(abs(a-b) for a, b in zip(row["center_cm"], spec["center_cm"])) > 0.01:
            raise RuntimeError("Collision placement differs: " + spec["id"])
        if max(abs(v-1) for v in row["scale"]) > 1e-5:
            raise RuntimeError("Collision geometry must have unit world scale")
        actual.append(row)
    result = {"name": scene["name"], "specs": rows, "snapshot": actual, "reopen_verified": False}
    REPORT["scenes"].append(result)
    return result


def validate_reopened_collision(actors, row):
    for before in row["snapshot"]:
        actor = actors.get(before["id"])
        if actor is None:
            raise RuntimeError("Missing saved collider: " + before["id"])
        after = snapshot(actor)
        for key in ("id", "parent_id", "shape", "profile", "enabled", "object_type", "pawn_response", "overlap", "simulate_physics"):
            if before[key] != after[key]:
                raise RuntimeError("Saved collision changed %s: %s" % (key, before["id"]))
        for key in ("center_cm", "rotation_quat", "scale", "extent_cm", "radius_cm"):
            if key not in before:
                continue
            a = before[key] if isinstance(before[key], list) else [before[key]]
            b = after[key] if isinstance(after[key], list) else [after[key]]
            if max(abs(x-y) for x, y in zip(a, b)) > 0.01:
                raise RuntimeError("Saved collision geometry changed %s: %s" % (key, before["id"]))
    row["reopen_verified"] = True


def write_collision_report():
    REPORT["success"] = bool(REPORT["scenes"]) and all(row["reopen_verified"] for row in REPORT["scenes"])
    QA_ROOT.mkdir(parents=True, exist_ok=True)
    (QA_ROOT / "collision_authoring_report.json").write_text(json.dumps(REPORT, ensure_ascii=False, indent=2), encoding="utf-8")
