"""Host-only source/layout verification; never imports Unreal or edits game assets.

Run with --compare-before to additionally compare the optional refactor archive.
That historical comparison is opt-in so later intentional UI changes remain valid.
"""
from __future__ import annotations

import sys
sys.dont_write_bytecode = True

import argparse
import ast
from datetime import datetime, timezone
import importlib
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import tempfile
import traceback
import zipfile

ROOT = Path(__file__).resolve().parents[2]
PYTHON_ROOT = ROOT / "Content/Python"
if str(PYTHON_ROOT) not in sys.path:
    sys.path.insert(0, str(PYTHON_ROOT))

PURE_MODULES = (
    "jonggu", "jonggu.paths", "jonggu.assets", "jonggu.build",
    "jonggu.ui.hud", "jonggu.ui.layout", "jonggu.ui.contracts",
    "jonggu.ui.theme", "jonggu.ui.assets", "jonggu.ui.primitives",
    "jonggu.ui.renderer", "jonggu.ui.panels", "jonggu.ui.visibility",
    "jonggu.gameplay.data", "jonggu.gameplay.constants",
    "jonggu.world.actors", "jonggu.world.config", "jonggu.world.build",
    "jonggu.validation.verify_popups", "jonggu.validation.verify_main_hud",
    "jonggu.validation.verify_adaptive_hud",
)
# A future compatibility exception must name one source file and explain why.
LEGACY_SOURCE_ALLOWLIST = {}


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def import_probe():
    for name in PURE_MODULES:
        module = importlib.import_module(name)
        require(Path(module.__file__).resolve().is_relative_to(PYTHON_ROOT),
                "Imported outside the project package: " + name)
    require("unreal" not in sys.modules, "Host contracts unexpectedly imported Unreal")
    from jonggu.paths import ROOT as package_root
    require(package_root == ROOT, "Package root depends on the working directory")
    return {"modules": len(PURE_MODULES), "cwd": str(Path.cwd()), "unreal_imported": False}


def parse_sources():
    parsed = {}
    stale = []
    retired = [re.compile(re.escape(first) + r"[/\\]+" + re.escape(second), re.I)
               for first, second in (("Tools", "Prototype"), ("Saved", "CollisionWork"))]
    for folder in (ROOT / "Content/Python", ROOT / "Tools"):
        for path in sorted(folder.rglob("*.py")):
            relative = path.relative_to(ROOT).as_posix()
            text = path.read_text(encoding="utf-8-sig")
            parsed[relative] = ast.parse(text, filename=relative)
            if any(pattern.search(text) for pattern in retired):
                if relative not in LEGACY_SOURCE_ALLOWLIST:
                    stale.append(relative)
    require(not stale, "Retired source paths in active scripts: " + ", ".join(stale))
    internal_imports = 0
    for relative, tree in parsed.items():
        for node in ast.walk(tree):
            modules = ([node.module] if isinstance(node, ast.ImportFrom) and node.module else
                       [alias.name for alias in node.names] if isinstance(node, ast.Import) else [])
            for module in modules:
                if module == "jonggu" or module.startswith("jonggu."):
                    target = PYTHON_ROOT.joinpath(*module.split("."))
                    require(target.with_suffix(".py").is_file() or (target / "__init__.py").is_file(),
                            relative + " imports a missing module: " + module)
                    internal_imports += 1
    return parsed, {"python_files": len(parsed), "internal_imports": internal_imports,
                    "retired_path_references": 0}


def validate_registry(parsed):
    from jonggu.assets import ASSETS, asset_path, object_path, generated_class_path
    paths = list(ASSETS.values())
    require(len(paths) == len({path.casefold() for path in paths}), "Duplicate canonical asset paths")
    for name, path in ASSETS.items():
        require(path.startswith("/Game/Jonggu/") and "/Prototype/" not in path,
                "Noncanonical asset destination: " + path)
        require(PurePosixPath(path).name == name and "." not in path and ".." not in path,
                "Registry name and package basename differ: " + name)
        require(asset_path(name) == path and object_path(name) == path + "." + name
                and generated_class_path(name) == path + "." + name + "_C",
                "Registry resolver mismatch: " + name)
    calls = 0
    for relative, tree in parsed.items():
        resolver_names = {alias.asname or alias.name for node in ast.walk(tree)
                          if isinstance(node, ast.ImportFrom) and node.module == "jonggu.assets"
                          for alias in node.names
                          if alias.name in ("asset_path", "object_path", "generated_class_path")}
        for node in ast.walk(tree):
            if (isinstance(node, ast.Call) and isinstance(node.func, ast.Name)
                    and node.func.id in resolver_names and node.args
                    and isinstance(node.args[0], ast.Constant) and isinstance(node.args[0].value, str)):
                require(node.args[0].value in ASSETS,
                        relative + " references an unknown asset: " + node.args[0].value)
                calls += 1
    return {"canonical_assets": len(ASSETS), "literal_registry_references": calls}


def compare_before():
    archive = ROOT / "Saved/RefactorQA/BeforeRefactor.zip"
    if not archive.is_file():
        return {"status": "skipped", "reason": "Optional historical archive is absent"}
    from jonggu.ui import layout, contracts, theme
    signature = lambda node: ast.dump(node, include_attributes=False)
    def declarations(tree):
        result = {}
        for node in tree.body:
            if isinstance(node, ast.FunctionDef):
                result[node.name] = node
            elif isinstance(node, ast.Assign):
                for target in node.targets:
                    if isinstance(target, ast.Name):
                        result[target.id] = node
        return result
    with zipfile.ZipFile(archive) as source:
        legacy_folder = "/".join(("Tools", "Prototype"))
        old_layout = declarations(ast.parse(source.read(legacy_folder + "/hud_layout.py")))
        old_ui = declarations(ast.parse(source.read(legacy_folder + "/build_ui.py")))
    layout_checks = 0
    for name, node in declarations(ast.parse(Path(layout.__file__).read_text(encoding="utf-8"))).items():
        require(name in old_layout and signature(node) == signature(old_layout[name]),
                "Historical layout declaration changed: " + name)
        layout_checks += 1
    contract_checks = 0
    for module in (contracts, theme):
        for name, node in declarations(ast.parse(Path(module.__file__).read_text(encoding="utf-8"))).items():
            if name == "HUD_PATH":
                continue  # Intentional registry migration, unrelated to drawing/input.
            require(name in old_ui and signature(node) == signature(old_ui[name]),
                    "Historical UI contract changed: " + name)
            contract_checks += 1
    return {"status": "passed", "layout_declarations": layout_checks,
            "ui_contract_declarations": contract_checks,
            "evidence": "AST comparison against the actual optional pre-refactor source archive"}


def check_sources(compare_historical=False):
    report = {"success": False, "generated_at_utc": datetime.now(timezone.utc).isoformat(),
              "evidence": "Host source and authored geometry checks; no engine or render validation"}
    try:
        parsed, report["sources"] = parse_sources()
        report["registry"] = validate_registry(parsed)
        # A separate interpreter prevents imports cached by this checker from
        # hiding dependence on the project working directory or Unreal runtime.
        probe = subprocess.run([sys.executable, "-B", str(Path(__file__).resolve()), "--probe-imports"],
                               cwd=tempfile.gettempdir(), capture_output=True, text=True, timeout=60,
                               encoding="utf-8")
        require(probe.returncode == 0, "Foreign-CWD import failed: " + probe.stderr.strip())
        report["foreign_cwd_import"] = json.loads(probe.stdout)
        from jonggu.validation.verify_popups import validate_shared_geometry
        report["geometry"] = validate_shared_geometry()
        require(report["geometry"].get("success"), "Shared UI geometry failed")
        report["historical_comparison"] = compare_before() if compare_historical else {"status": "not_requested"}
        report["success"] = True
    except Exception:
        report["error"] = traceback.format_exc()
    output = ROOT / "Saved/RefactorQA/source_report.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    return report, output


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--probe-imports", action="store_true", help=argparse.SUPPRESS)
    parser.add_argument("--compare-before", action="store_true", help="Compare optional historical UI source archive")
    arguments = parser.parse_args()
    if arguments.probe_imports:
        print(json.dumps(import_probe(), ensure_ascii=True))
    else:
        result, output = check_sources(arguments.compare_before)
        print(output)
        if not result["success"]:
            print(result["error"], file=sys.stderr)
            raise SystemExit(1)
