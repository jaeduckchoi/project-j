"""Thin Unreal entrypoint. Select a task with -JongguTask=<name>."""
from pathlib import Path
import json
import re
import runpy
import sys
import unreal

ROOT = Path(__file__).resolve().parents[2]
PYTHON_ROOT = ROOT / "Content/Python"
if str(PYTHON_ROOT) not in sys.path:
    sys.path.insert(0, str(PYTHON_ROOT))
# Repeated editor invocations must see edited authoring modules.
for name in list(sys.modules):
    if name == "jonggu" or name.startswith("jonggu."):
        del sys.modules[name]
from jonggu.paths import require_active_project
require_active_project()
match = re.search(r"-JongguTask=(\w+)", unreal.SystemLibrary.get_command_line(), re.IGNORECASE)
TASK = match.group(1).lower() if match else "build"
if TASK == "build":
    from jonggu.build import build_project
    build_project()
elif TASK in ("verify", "capture", "baseline"):
    module = {"verify": "verify_pie", "capture": "verify_popup_pie", "baseline": "verify_baseline"}[TASK]
    runpy.run_module("jonggu.validation." + module, run_name="__main__")
elif TASK == "refactor":
    from jonggu.maintenance.reorganize import reorganize_assets
    reorganize_assets()
elif TASK == "structure":
    from jonggu.validation.verify_refactor import validate_refactor
    result = validate_refactor()
    output = ROOT / "Saved/RefactorQA/structure_report.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    if not result.get("success", False):
        raise RuntimeError("Structure validation failed: " + str(output))
else:
    raise ValueError("Unknown Jonggu task: " + TASK)
