"""Portable World authoring paths. Importing this module never writes files."""
from __future__ import annotations

import json
import os
from pathlib import Path

WORLD_ROOT = Path(__file__).resolve().parent
PROJECT_ROOT = next(parent for parent in WORLD_ROOT.parents if (parent / "projectJ.uproject").is_file())
UPROJECT_PATH = PROJECT_ROOT / "projectJ.uproject"
CONFIG_PATH = WORLD_ROOT / "world_config.json"
CONFIG = json.loads(CONFIG_PATH.read_text(encoding="utf-8-sig"))
if CONFIG.get("schema_version") != 1:
    raise ValueError("Unsupported World authoring configuration: " + str(CONFIG_PATH))
DATA_ROOT = WORLD_ROOT / "Data"
FIXTURE_ROOT = WORLD_ROOT / "Fixtures"
CONTENT_ROOT = CONFIG["content_root"].rstrip("/")
QA_ROOT = (WORLD_ROOT / CONFIG["output_root"]).resolve()
if not QA_ROOT.is_relative_to(PROJECT_ROOT / "Saved"):
    raise ValueError("World QA output must remain under this project's Saved directory")
_source = os.environ.get("JONGGU_UNITY_SOURCE") or CONFIG.get("unity_source_root")
SOURCE_ROOT = ((WORLD_ROOT / _source).resolve() if _source else None)


def require_unreal_project():
    import unreal
    active = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active != PROJECT_ROOT:
        raise RuntimeError(f"Expected {UPROJECT_PATH}; active editor project is {active}")
    return PROJECT_ROOT


def resolve_source_path(relative):
    if SOURCE_ROOT is None:
        raise RuntimeError("Full source reimport requires JONGGU_UNITY_SOURCE or unity_source_root in Tools/World/world_config.json")
    result = (SOURCE_ROOT / relative).resolve()
    if not result.is_relative_to(SOURCE_ROOT) or not result.is_file():
        raise RuntimeError("Source asset missing or outside configured Unity source: " + str(result))
    return result


def require_full_import():
    import unreal
    if "-jonggufullworldimport" not in unreal.SystemLibrary.get_command_line().lower():
        raise RuntimeError("Full world import is separate from restaurant authoring; launch explicitly with -JongguFullWorldImport only for a reviewed source reimport")


def require_qa_launch(marker, script_path):
    """Only marked direct scripts or the exact canonical Baseline task may run."""
    import unreal
    require_unreal_project()
    line = unreal.SystemLibrary.get_command_line().replace("\\", "/").lower()
    own = str(script_path).replace("\\", "/").lower()
    bootstrap = str(PROJECT_ROOT / "Tools/Unreal/entry.py").replace("\\", "/").lower()
    direct = marker.lower() in line and "-executepythonscript=" in line and own in line
    baseline = (marker.lower() == "-jonggubaselineqa" and "-jonggubaselineqa" in line
                and "-jonggutask=baseline" in line and "-executepythonscript=" in line and bootstrap in line)
    if not (direct or baseline):
        raise RuntimeError("Run a fresh marked QA editor with the requested script or Tools/Unreal Baseline task")
    QA_ROOT.mkdir(parents=True, exist_ok=True)
