"""Move authored assets with Unreal's rename API, retaining save compatibility."""
from datetime import datetime, timezone
import hashlib
import json
import traceback
from pathlib import Path
from jonggu.assets import LEGACY_ASSET_PATHS
from jonggu.paths import ROOT, require_active_project


def reorganize_assets():
    import unreal
    require_active_project()
    qa = ROOT / "Saved/RefactorQA"
    qa.mkdir(parents=True, exist_ok=True)
    report = {"success": False, "generated_at_utc": datetime.now(timezone.utc).isoformat(), "moves": []}
    assets = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    try:
        if not (qa / "BeforeRefactor.zip").is_file():
            raise RuntimeError("Create the source/content backup before reorganizing assets")
        saves = ROOT / "Saved/SaveGames"
        report["production_saves"] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in saves.glob("*.sav") if not p.name.startswith("JongguRefactor")}
        old_save = "/Game/Jonggu/Prototype/BP_PrototypeSave.BP_PrototypeSave_C"
        legacy_meta = qa / "legacy_save.json"
        if not legacy_meta.exists():
            cls = unreal.load_class(None, old_save)
            if cls is None:
                raise RuntimeError("Legacy SaveGame class must exist before the first migration")
            expected = {"day": 7, "clams": 5, "harvested_mask": 3, "menu_mask": 5,
                        "guest_target": 6, "total_revenue": 123, "checkpoint_phase": 0,
                        "checkpoint_map": "L_Hub", "entry_marker": "hub",
                        "summary_served": 4, "summary_base": 40, "summary_bonus": 7,
                        "summary_clams_used": 1, "summary_special_served": 2}
            instance = unreal.GameplayStatics.create_save_game_object(cls)
            for field, value in expected.items():
                instance.set_editor_property(field, value)
            instance.set_editor_property("schema_version", 1)
            slot = "JongguRefactorLegacyFixture"
            if not unreal.GameplayStatics.save_game_to_slot(instance, slot, 0):
                raise RuntimeError("Cannot create isolated legacy save fixture")
            file = saves / (slot + ".sav")
            legacy_meta.write_text(json.dumps({"slot": slot, "old_class_path": old_save,
                "expected": expected, "file_sha256": hashlib.sha256(file.read_bytes()).hexdigest()}, indent=2), encoding="utf-8")
        # Load referencers before renaming so Unreal updates live hard references.
        original_maps = {}
        levels = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
        actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
        for name in ("L_Hub", "L_Beach"):
            if not levels.load_level("/Game/Jonggu/Maps/" + name):
                raise RuntimeError("Cannot open " + name)
            original_maps[name] = [{"object_name": a.get_name(), "label": a.get_actor_label(),
                "class": a.get_class().get_path_name(), "tags": [str(t) for t in a.tags],
                "location": [a.get_actor_location().x, a.get_actor_location().y, a.get_actor_location().z]}
                for a in actors.get_all_level_actors()]
        (qa / "actors_before.json").write_text(json.dumps(original_maps, ensure_ascii=False, indent=2), encoding="utf-8")
        plans = []
        for old, new in LEGACY_ASSET_PATHS.items():
            if assets.does_asset_exist(new):
                report["moves"].append({"old": old, "new": new, "already_moved": True})
                continue
            asset = assets.load_asset(old)
            if asset is None:
                raise RuntimeError("Missing migration input: " + old)
            folder, name = new.rsplit("/", 1)
            plans.append(unreal.AssetRenameData(asset, folder, name))
            report["moves"].append({"old": old, "new": new})
        if plans and not unreal.AssetToolsHelpers.get_asset_tools().rename_assets(plans):
            raise RuntimeError("Unreal asset rename did not complete")
        for new in LEGACY_ASSET_PATHS.values():
            asset = assets.load_asset(new)
            if asset is None or asset.get_path_name().split(".")[0] != new:
                raise RuntimeError("Wrong rename destination: " + new)
            if not assets.save_loaded_asset(asset, only_if_is_dirty=False):
                raise RuntimeError("Cannot save moved asset: " + new)
        for name in ("L_Hub", "L_Beach"):
            levels.load_level("/Game/Jonggu/Maps/" + name)
            if not levels.save_current_level():
                raise RuntimeError("Cannot save map references: " + name)
        # Preserve class path strings serialized in pre-refactor SaveGame files.
        ini = ROOT / "Config/DefaultEngine.ini"
        text = ini.read_text(encoding="utf-8-sig")
        for old, new in LEGACY_ASSET_PATHS.items():
            text = text.replace(old + "." + old.rsplit("/", 1)[1], new + "." + new.rsplit("/", 1)[1])
        if "[CoreRedirects]" not in text:
            text += "\n[CoreRedirects]\n"
        for old, new in LEGACY_ASSET_PATHS.items():
            row = f'+PackageRedirects=(OldName="{old}",NewName="{new}")'
            if row not in text: text += row + "\n"
            if old.rsplit("/", 1)[1].startswith("BP_"):
                before = old + "." + old.rsplit("/", 1)[1] + "_C"
                after = new + "." + new.rsplit("/", 1)[1] + "_C"
                row = f'+ClassRedirects=(OldName="{before}",NewName="{after}")'
                if row not in text: text += row + "\n"
        ini.write_text(text, encoding="utf-8")
        unused = "/Game/Jonggu/Prototype/F_PrototypeUI"
        if assets.does_asset_exist(unused):
            refs = assets.find_package_referencers_for_asset(unused, load_assets_to_confirm=True)
            if refs: raise RuntimeError("Unused fallback font still referenced: " + str(refs))
            if not assets.delete_asset(unused): raise RuntimeError("Cannot remove unused fallback font")
            report["removed_unused"] = [unused]
        report["success"] = True
    except Exception:
        report["error"] = traceback.format_exc()
        unreal.log_error(report["error"])
    finally:
        (qa / "asset_migration_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    if not report["success"]:
        raise RuntimeError(report["error"])
    return report
