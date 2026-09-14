"""Render final ordering and the original fixed ordering in the same saved scene.

This detects static art lost through render-side priority clamping. No assets or
maps are saved. Run in a rendered UnrealEditor-Cmd Python commandlet (no NullRHI).
"""
from pathlib import Path
import json
import sys
import unreal

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from world_paths import DATA_ROOT, CONTENT_ROOT, QA_ROOT, require_unreal_project

require_unreal_project()
OUTPUT = QA_ROOT / "StaticDepth"
OUTPUT.mkdir(parents=True, exist_ok=True)
manifest = json.loads((DATA_ROOT / "render_manifest.json").read_text(encoding="utf-8"))
rules = json.loads((DATA_ROOT / "collision_rules.json").read_text(encoding="utf-8"))
levels = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
editor = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem)
report = {"success": False, "saved_content_modified": False, "scenes": []}


def identity(actor):
    return next((str(t).removeprefix("JongguMigration:") for t in actor.tags
                 if str(t).startswith("JongguMigration:")), None)


def capture(scene):
    assert levels.load_level(CONTENT_ROOT + "/Maps/L_" + scene["name"])
    world = editor.get_editor_world()
    owned = {identity(a): a for a in actors.get_all_level_actors() if identity(a)}
    source = next(a for a in owned.values() if isinstance(a, unreal.CameraActor))
    camera = source.camera_component
    cap = actors.spawn_actor_from_class(unreal.SceneCapture2D, source.get_actor_location(),
                                       source.get_actor_rotation(), transient=True)
    component = cap.capture_component2d
    component.set_editor_property("projection_type", unreal.CameraProjectionMode.ORTHOGRAPHIC)
    component.set_editor_property("ortho_width", camera.get_editor_property("ortho_width"))
    component.set_editor_property("auto_calculate_ortho_planes", False)
    component.set_editor_property("capture_every_frame", False)
    component.set_editor_property("capture_on_movement", False)
    component.set_editor_property("always_persist_rendering_state", True)
    component.set_editor_property("capture_source", unreal.SceneCaptureSource.SCS_FINAL_COLOR_LDR)
    component.set_editor_property("post_process_settings", camera.get_editor_property("post_process_settings"))
    component.set_editor_property("post_process_blend_weight", 1.0)
    flags = []
    for name in ("AntiAliasing", "TemporalAA", "Bloom", "MotionBlur", "LensFlares"):
        flag = unreal.EngineShowFlagsSetting()
        flag.show_flag_name, flag.enabled = name, False
        flags.append(flag)
    component.set_editor_property("show_flag_settings", flags)
    target = unreal.RenderingLibrary.create_render_target2d(world, 1920, 1080,
        unreal.TextureRenderTargetFormat.RTF_RGBA8, unreal.LinearColor(0, 0, 0, 1), False)
    target.set_editor_property("target_gamma", 2.2)
    component.set_editor_property("texture_target", target)
    unreal.SystemLibrary.execute_console_command(world, "Editor.AsyncAssetCompilationFinishAll")
    for material in ("MI_Jonggu_TranslucentUnlit", "M_Jonggu_PreviewDisplay"):
        unreal.MaterialEditingLibrary.get_statistics(unreal.load_asset(CONTENT_ROOT + "/Materials/" + material))
    groups = {g["id"]: g for g in scene["groups"]}
    # All current render groups use source sorting layer zero. Refuse to infer
    # another layer mapping if future source art expands the contract.
    assert all(g["sorting_layer"] == 0 for g in groups.values())
    old = []
    for group in groups.values():
        actor = owned["render:" + group["id"]]
        sprite = actor.get_component_by_class(unreal.PaperSpriteComponent) or actor.get_component_by_class(unreal.PaperGroupedSpriteComponent)
        old.append((sprite, int(sprite.get_editor_property("translucency_sort_priority")), group["sorting_order"]))
    player_group = next(gid for gid, value in rules["depth_sorting"]["scenes"][scene["name"]]["groups"].items()
                        if value["category"] == "player")
    pawn = next(a for a in owned.values() if isinstance(a, unreal.Pawn))
    for sprite in pawn.get_components_by_class(unreal.PaperSpriteComponent):
        old.append((sprite, int(sprite.get_editor_property("translucency_sort_priority")), groups[player_group]["sorting_order"]))
    row = {"name": scene["name"], "files": {}, "source_camera_modified": False}
    try:
        for variant in ("current", "source_order"):
            if variant == "source_order":
                for sprite, saved, original in old:
                    sprite.set_translucent_sort_priority(original)
            for _ in range(3):
                component.capture_scene()
            filename = scene["name"] + "_" + variant + ".png"
            unreal.RenderingLibrary.export_render_target(world, target, str(OUTPUT), filename)
            assert (OUTPUT / filename).is_file()
            row["files"][variant] = str(OUTPUT / filename)
    finally:
        for sprite, saved, original in old:
            sprite.set_translucent_sort_priority(saved)
        actors.destroy_actor(cap)
    report["scenes"].append(row)


try:
    for scene in manifest["scenes"]:
        capture(scene)
    report["success"] = True
finally:
    (QA_ROOT / "static_depth_render_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
