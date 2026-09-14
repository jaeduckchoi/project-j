"""Reproduce the recorded pre-prototype Beach corner case without saving assets."""
from pathlib import Path
import importlib.util,sys
ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'Saved/CollisionWork/QA/verify_collision_pie.py'
spec=importlib.util.spec_from_file_location('jonggu_baseline_collision',source)
m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
m.PROJECT_ROOT=ROOT
m.SCRIPT_PATH=Path(__file__).resolve()
m.REPORT_PATH=ROOT/'Saved/PrototypeQA/baseline_collision.json'
m.CAPTURE_ROOT=ROOT/'Saved/PrototypeQA/BaselineCaptures'
m.STATE['scene_index']=1
original=m.build_cases
m.build_cases=lambda:[c for c in original() if c['name']=='left_then_up_only_corner']
m.main()
