"""Apply authored ground-anchor ordering to static art and the initial player."""
import json
import unreal

from collision_geometry import load_rules
from depth_sorting import build_depth_sort_specs, player_sort_priority
from world_paths import DATA_ROOT

ASSETS = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)


def prepare_depth_sort(manifest):
    rules = load_rules(DATA_ROOT)
    return {scene["name"]: build_depth_sort_specs(scene, manifest, rules) for scene in manifest["scenes"]}


def apply_scene_depth_sort(scene, render_actors, nodes, player_report, specs):
    rows = specs[scene["name"]]
    expected = {g["id"] for g in scene["groups"]}
    if {row["group_id"] for row in rows} != expected:
        raise RuntimeError("Depth sorting must explicitly classify every source render group")
    for row in rows:
        actor = render_actors[row["group_id"]]
        component = actor.get_component_by_class(unreal.PaperSpriteComponent)
        if component is None:
            component = actor.get_component_by_class(unreal.PaperGroupedSpriteComponent)
        component.set_translucent_sort_priority(int(row["priority"]))
        ASSETS.set_metadata_tag(actor, "JongguDepthSort", json.dumps(row, sort_keys=True))
    pawn = nodes[player_report["source_root_id"]]
    priority = player_sort_priority(pawn.get_actor_location().z)
    for component in pawn.get_components_by_class(unreal.PaperSpriteComponent):
        component.set_translucent_sort_priority(priority)
    player_report["sort_priority"] = priority
    player_report["sort_mode"] = "ground-anchor:-floor(feet_z_cm+0.5)"
    return {"specs": rows, "initial_player_priority": priority, "reopen_verified": False}


def validate_reopened_depth_sort(actors, row):
    for spec in row["specs"]:
        actor = actors["render:" + spec["group_id"]]
        component = actor.get_component_by_class(unreal.PaperSpriteComponent)
        if component is None:
            component = actor.get_component_by_class(unreal.PaperGroupedSpriteComponent)
        if component.get_editor_property("translucency_sort_priority") != spec["priority"]:
            raise RuntimeError("Saved ground sort priority changed: " + spec["group_id"])
        if json.loads(ASSETS.get_metadata_tag(actor, "JongguDepthSort")) != spec:
            raise RuntimeError("Saved ground sort metadata changed: " + spec["group_id"])
    row["reopen_verified"] = True
