"""Build authored game assets without changing source art or player graphs."""
from datetime import datetime, timezone
import hashlib
import json
import shutil
import traceback

from jonggu.paths import ROOT, require_active_project


def build_project():
    import unreal
    require_active_project()
    if "-run=" not in unreal.SystemLibrary.get_command_line().lower():
        if unreal.get_editor_subsystem(unreal.LevelEditorSubsystem).is_in_play_in_editor():
            raise RuntimeError("End Play before rebuilding game assets")
    qa = ROOT / "Saved/PrototypeQA"
    qa.mkdir(parents=True, exist_ok=True)
    report = {"success": False, "generated_at_utc": datetime.now(timezone.utc).isoformat()}
    try:
        baseline = qa / "Baseline"
        if not (baseline / "hashes.json").exists():
            hashes = {}
            for relative in ("Content/Jonggu/Maps/L_Hub.umap", "Content/Jonggu/Maps/L_Beach.umap",
                             "Content/Jonggu/Blueprints/Player/BP_JongguPlayer.uasset",
                             "Content/Jonggu/Blueprints/Game/BP_JongguGameMode.uasset",
                             "Config/DefaultEngine.ini", "README.md"):
                source, target = ROOT / relative, baseline / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
                hashes[relative] = hashlib.sha256(source.read_bytes()).hexdigest()
            (baseline / "hashes.json").write_text(json.dumps(hashes, indent=2), encoding="utf-8")
        from jonggu.gameplay.session import build_session
        from jonggu.ui.hud import build_ui
        from jonggu.gameplay.data import build_data_types
        from jonggu.gameplay.manager import build_manager
        from jonggu.world.build import build_world
        from jonggu.validation.verify_session import validate_session_runtime
        report["stage"] = "session"
        session, save = build_session()
        report["stage"] = "ui"
        hud = build_ui()
        report["stage"] = "data"
        typed = build_data_types()
        report["stage"] = "manager"
        manager = build_manager(typed)
        report["stage"] = "session_test"
        report["session_tests"] = validate_session_runtime(session)
        report["stage"] = "world"
        report["world"] = build_world(manager, hud, session)
        report["success"] = True
    except Exception:
        report["error"] = traceback.format_exc()
        unreal.log_error(report["error"])
    finally:
        (qa / "build_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    if not report["success"]:
        raise RuntimeError(report["error"])
    return report
