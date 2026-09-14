"""Project-relative authoring paths; independent of drive and launch directory."""
from pathlib import Path

ROOT = next(parent for parent in Path(__file__).resolve().parents if (parent / "projectJ.uproject").is_file())
PYTHON_ROOT = ROOT / "Content/Python"
LAYOUT_FILE = ROOT / "SourceData/Restaurant/layout.json"
FONT_SOURCE_DIR = ROOT / "SourceAssets/Fonts"
QA_ROOT = ROOT / "Saved/RefactorQA"


def require_active_project():
    import unreal
    active = Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active != ROOT:
        raise RuntimeError(f"Expected {ROOT}, active Unreal project is {active}")
    return ROOT
