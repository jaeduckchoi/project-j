"""Read-only native verification of package moves and pre-rename SaveGame data."""
from __future__ import annotations

from datetime import datetime, timezone
import hashlib
import json
import traceback

from jonggu.assets import ASSETS, LEGACY_ASSET_PATHS, asset_path, object_path, generated_class_path
from jonggu.paths import ROOT, LAYOUT_FILE, require_active_project

FIXTURE_SLOT = "JongguRefactorLegacyFixture"


def _fixture_input(name):
    versioned = ROOT / "Tools/Unreal/Fixtures" / name
    return versioned if versioned.is_file() else ROOT / "Saved/RefactorQA" / name


def _stage_legacy_fixture(destination, source, expected_hash):
    """Copy original bytes only when this exact QA slot does not already exist."""
    created = False
    if source.is_file():
        original = source.read_bytes()
        if hashlib.sha256(original).hexdigest() != expected_hash:
            raise RuntimeError("Versioned legacy fixture hash does not match its metadata")
    elif not destination.is_file():
        raise RuntimeError("Legacy fixture source is missing: " + str(source))
    if not destination.exists():
        destination.parent.mkdir(parents=True, exist_ok=True)
        try:
            with destination.open("xb") as stream:
                stream.write(original)
            created = True
        except FileExistsError:
            # Another process may have created the slot after the existence
            # check. Never overwrite or claim ownership of that file.
            pass
    return created, hashlib.sha256(destination.read_bytes()).hexdigest()


def validate_refactor():
    import unreal
    from jonggu.gameplay.session import PERSISTED_FIELDS
    from jonggu.world.actors import OWN_TAG, ID_TAG, actor_presentation

    require_active_project()
    report = {"success": False, "generated_at_utc": datetime.now(timezone.utc).isoformat(),
              "checks": [], "failures": [], "assets": [], "maps": [],
              "method": "Actual AssetRegistry, saved maps, native legacy SaveGame and compiled LoadCheckpoint",
              "content_saved": False, "legacy_fixture_slot": FIXTURE_SLOT}
    levels = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    actor_api = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    editor = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem)
    original_world = editor.get_editor_world()
    original_map = original_world.get_path_name().split(".")[0] if original_world else None
    save_dir = ROOT / "Saved/SaveGames"
    fixture_file = save_dir / (FIXTURE_SLOT + ".sav")
    fixture_created = False
    before_hash = None
    production_before = {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                         for p in save_dir.glob("*.sav") if p.stem != FIXTURE_SLOT}

    def check(label, passed, **detail):
        if passed:
            report["checks"].append(label)
        else:
            report["failures"].append({"check": label, **detail})
        return bool(passed)

    commandlet = "-run=" in unreal.SystemLibrary.get_command_line().lower()
    report["commandlet"] = commandlet
    try:
        # IsInPlayInEditor dereferences interactive LevelEditor state; no Slate
        # level editor exists in -run=pythonscript commandlets.
        if not commandlet and levels.is_in_play_in_editor():
            raise RuntimeError("End Play before running the read-only Structure task")
        metadata_path = _fixture_input("legacy_save.json")
        fixture_meta = json.loads(metadata_path.read_text(encoding="utf-8-sig"))
        report["legacy_metadata_file"] = str(metadata_path)
        if fixture_meta.get("slot") != FIXTURE_SLOT:
            raise RuntimeError("Refusing a legacy checkpoint slot outside the exact isolated QA fixture")
        expected = fixture_meta["expected"]
        check("legacy fixture contains every persisted field", set(expected) == {name for name, _, _ in PERSISTED_FIELDS})
        check("legacy fixture records the actual pre-rename class",
              fixture_meta.get("old_class_path") in {old + "." + old.rsplit("/", 1)[1] + "_C"
                  for old, new in LEGACY_ASSET_PATHS.items() if new == asset_path("BP_PrototypeSave")})
        fixture_source = _fixture_input("LegacyCheckpoint.sav")
        fixture_created, before_hash = _stage_legacy_fixture(fixture_file, fixture_source, fixture_meta["file_sha256"])
        report["legacy_fixture_source"] = str(fixture_source)
        report["legacy_slot_created_for_this_run"] = fixture_created
        if not check("pre-rename checkpoint bytes match recorded fixture", before_hash == fixture_meta["file_sha256"], actual=before_hash):
            raise RuntimeError("Legacy fixture changed before validation; newly saved files cannot prove migration")
        loaded = unreal.GameplayStatics.load_game_from_slot(FIXTURE_SLOT, 0)
        if not check("native loader opens pre-rename SaveGame", loaded is not None):
            raise RuntimeError("Legacy SaveGame did not deserialize through the compatibility redirect")
        check("legacy SaveGame resolves to canonical renamed class",
              loaded.get_class().get_path_name() == generated_class_path("BP_PrototypeSave"),
              actual=loaded.get_class().get_path_name())
        check("legacy schema version remains supported", loaded.get_editor_property("schema_version") == 1)
        for field, value in expected.items():
            actual = loaded.get_editor_property(field)
            check("legacy native field " + field, actual == value, expected=value, actual=actual)
        session_bp = unreal.load_asset(asset_path("BP_RestaurantGameInstance"))
        session = unreal.new_object(session_bp.generated_class())
        session.set_editor_property("save_slot", FIXTURE_SLOT)
        check("new session starts with compact HUD", session.get_editor_property("hud_expanded_mask") == 0)
        session.call_method("LoadCheckpoint")
        check("compiled session accepts the legacy checkpoint", session.get_editor_property("save_ok"))
        for field, value in expected.items():
            actual = session.get_editor_property(field)
            check("legacy compiled-session field " + field, actual == value, expected=value, actual=actual)
        check("legacy load does not restore session-only folds", session.get_editor_property("hud_expanded_mask") == 0)
        check("read-only legacy verification leaves fixture bytes unchanged",
              hashlib.sha256(fixture_file.read_bytes()).hexdigest() == before_hash)
        report["legacy_fixture_sha256"] = before_hash

        baseline_hashes = ROOT / "Tools/Unreal/Fixtures/source_hashes.json"
        if baseline_hashes.is_file():
            for relative, expected_hash in json.loads(baseline_hashes.read_text(encoding="utf-8-sig")).items():
                if relative.endswith(".uasset"):
                    original = ROOT / relative
                    check("original asset bytes " + relative,
                          original.is_file() and hashlib.sha256(original.read_bytes()).hexdigest() == expected_hash)

        registry = unreal.AssetRegistryHelpers.get_asset_registry()
        registry.scan_paths_synchronous(["/Game/Jonggu"], force_rescan=True)
        for name, package in ASSETS.items():
            asset = unreal.load_asset(asset_path(name))
            valid = asset is not None and asset.get_path_name() == object_path(name)
            check("canonical asset " + name, valid, expected=object_path(name),
                  actual=asset.get_path_name() if asset else None)
            if name.startswith("BP_"):
                cls = unreal.load_class(None, generated_class_path(name))
                check("canonical generated class " + name,
                      cls is not None and cls.get_path_name() == generated_class_path(name))
            report["assets"].append({"name": name, "package": package, "loaded": valid})
        all_assets = list(registry.get_assets_by_path("/Game/Jonggu", recursive=True))
        old_prefix = "/Game/Jonggu/Prototype/"
        remaining = [str(item.package_name) for item in all_assets
                     if str(item.package_name).startswith(old_prefix) and not item.is_redirector()]
        check("legacy folder has no remaining authored assets", not remaining, packages=remaining)
        fallback_font = ROOT / "Content/Jonggu/Prototype/F_PrototypeUI.uasset"
        check("unused legacy fallback font file is removed", not fallback_font.exists(),
              file=str(fallback_font))
        report["compatibility_redirectors"] = sorted(str(item.package_name) for item in all_assets
                                                      if str(item.package_name).startswith(old_prefix) and item.is_redirector())
        options = unreal.AssetRegistryDependencyOptions(
            include_soft_package_references=True, include_hard_package_references=True,
            include_searchable_names=True, include_soft_management_references=True,
            include_hard_management_references=True)
        old_edges = []
        for package in sorted({str(item.package_name) for item in all_assets if not item.is_redirector()}):
            for dependency in registry.get_dependencies(package, options) or []:
                if str(dependency).startswith(old_prefix):
                    old_edges.append({"from": package, "to": str(dependency)})
        check("live authored packages have no dependencies on legacy packages", not old_edges, edges=old_edges)
        report["legacy_dependency_edges"] = old_edges

        actor_evidence = _fixture_input("actors_before.json")
        before = json.loads(actor_evidence.read_text(encoding="utf-8-sig")) if actor_evidence.is_file() else {}
        report["migration_actor_evidence"] = {"available": bool(before), "file": str(actor_evidence) if before else None}
        layout = json.loads(LAYOUT_FILE.read_text(encoding="utf-8-sig"))
        for map_name, scene in layout["maps"].items():
            if not check("map reopens " + map_name, levels.load_level(scene["package"])):
                continue
            actors = list(actor_api.get_all_level_actors())
            source_rows = before.get(scene["package"].rsplit("/", 1)[1])
            source = {next(tag for tag in item["tags"] if tag.startswith("JongguMigration:")): item
                      for item in (source_rows or [])
                      if any(tag.startswith("JongguMigration:") for tag in item["tags"])}
            actual_source = {}
            owned = {}
            for actor in actors:
                tags = [str(tag) for tag in actor.tags]
                source_id = next((tag for tag in tags if tag.startswith("JongguMigration:")), None)
                if source_id: actual_source[source_id] = actor
                if OWN_TAG in tags:
                    identity = next((tag[len(ID_TAG):] for tag in tags if tag.startswith(ID_TAG)), None)
                    owned.setdefault(identity, []).append(actor)
            if source_rows is not None:
                check("source identities survive " + map_name, set(source) == set(actual_source))
            for identity in sorted(set(source) & set(actual_source)):
                old, actor = source[identity], actual_source[identity]
                location = actor.get_actor_location()
                match = (old["class"] == actor.get_class().get_path_name()
                         and old["label"] == actor.get_actor_label()
                         and old["tags"] == [str(tag) for tag in actor.tags]
                         and max(abs(old["location"][i] - getattr(location, axis)) for i, axis in enumerate(("x", "y", "z"))) < 0.001)
                check("source actor preserved " + identity, match)
            expected_ids = {"manager"} | {"marker:" + row["id"] for row in scene["interactions"]}
            expected_ids |= {"customer:" + row["id"] for row in scene.get("seats", [])}
            check("overlay identities remain exact " + map_name, set(owned) == expected_ids)
            labels = []
            for identity, matching in owned.items():
                if not check("one overlay instance " + map_name + ":" + str(identity), len(matching) == 1):
                    continue
                actor = matching[0]
                if identity not in expected_ids: continue
                label, folder = actor_presentation(identity, map_name)
                check("readable overlay label " + map_name + ":" + identity,
                      actor.get_actor_label() == label and str(actor.get_folder_path()) == folder,
                      expected=[label, folder], actual=[actor.get_actor_label(), str(actor.get_folder_path())])
                labels.append({"identity": identity, "label": actor.get_actor_label(), "folder": str(actor.get_folder_path())})
            mode = editor.get_editor_world().get_world_settings().get_editor_property("default_game_mode")
            check("map uses renamed GameMode " + map_name,
                  mode is not None and mode.get_path_name() == generated_class_path("BP_RestaurantGameMode"))
            if mode:
                defaults = unreal.get_default_object(mode)
                check("map keeps placed Pawn policy " + map_name, defaults.get_editor_property("default_pawn_class") is None)
                check("map uses renamed HUD " + map_name,
                      defaults.get_editor_property("hud_class").get_path_name() == generated_class_path("BP_RestaurantHUD"))
                check("map uses renamed controller " + map_name,
                      defaults.get_editor_property("player_controller_class").get_path_name() == generated_class_path("BP_RestaurantPlayerController"))
            report["maps"].append({"name": map_name, "source_count": len(actual_source), "overlay": labels})
    except Exception:
        report["failures"].append({"exception": traceback.format_exc()})
    finally:
        if fixture_created:
            # Delete only the file this invocation exclusively created and only
            # while its bytes still match the immutable legacy input.
            try:
                unchanged = fixture_file.is_file() and hashlib.sha256(fixture_file.read_bytes()).hexdigest() == before_hash
                if check("owned temporary legacy slot remains unchanged before cleanup", unchanged):
                    fixture_file.unlink()
                report["temporary_legacy_slot_removed"] = not fixture_file.exists()
            except Exception:
                report["failures"].append({"legacy_fixture_cleanup_exception": traceback.format_exc()})
        if original_map and original_map.startswith("/Game/"):
            try:
                check("original editor map restored", levels.load_level(original_map))
            except Exception:
                report["failures"].append({"map_restore_exception": traceback.format_exc()})
        production_after = {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                            for p in save_dir.glob("*.sav") if p.stem != FIXTURE_SLOT}
        check("all non-fixture saves remain byte-identical", production_before == production_after)
    report["success"] = not report["failures"]
    report["check_count"] = len(report["checks"])
    return report
