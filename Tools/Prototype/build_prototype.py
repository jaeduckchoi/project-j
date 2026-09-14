"""Build only dedicated prototype assets; existing migration assets stay intact."""
from pathlib import Path
import sys,json,traceback,shutil,hashlib
from datetime import datetime,timezone
import unreal
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
sys.path.insert(0,str(HERE))
QA=ROOT/'Saved/PrototypeQA'
QA.mkdir(parents=True,exist_ok=True)
report={'success':False,'generated_at_utc':datetime.now(timezone.utc).isoformat()}
(QA/'build_report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
try:
    active=Path(unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())).resolve()
    if active!=ROOT: raise RuntimeError('Unexpected project: '+str(active))
    if '-run=' not in unreal.SystemLibrary.get_command_line().lower() and unreal.get_editor_subsystem(unreal.LevelEditorSubsystem).is_in_play_in_editor():
        raise RuntimeError('End Play before rebuilding prototype assets')
    # A second invocation in the same editor must use changed authoring source.
    for name in ('bp_helpers','build_session','hud_layout','build_hud_visibility','build_ui', 'build_popup_assets','data_types','build_manager','verify_session','build_world'):
        module=sys.modules.get(name)
        if module and Path(getattr(module,'__file__','')).resolve().parent==HERE:
            sys.modules.pop(name)
    baseline=QA/'Baseline'
    if not (baseline/'hashes.json').exists():
        hashes={}
        for rel in ['Content/Jonggu/Maps/L_Hub.umap','Content/Jonggu/Maps/L_Beach.umap','Content/Jonggu/Blueprints/Player/BP_JongguPlayer.uasset','Content/Jonggu/Blueprints/Game/BP_JongguGameMode.uasset','Config/DefaultEngine.ini','README.md']:
            src=ROOT/rel; dst=baseline/rel
            dst.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(src,dst)
            hashes[rel]=hashlib.sha256(src.read_bytes()).hexdigest()
        (baseline/'hashes.json').write_text(json.dumps(hashes,indent=2),encoding='utf8')
    from build_session import build_session
    report['stage']='session'
    session,save=build_session()
    from build_ui import build_ui
    report['stage']='ui'
    hud=build_ui()
    from data_types import build_data_types
    report['stage']='data'
    typed=build_data_types()
    from build_manager import build_manager
    report['stage']='manager'
    manager=build_manager(typed)
    from verify_session import validate_session_runtime
    report['stage']='session_test'
    report['session_tests']=validate_session_runtime(session)
    from build_world import build_world
    report['stage']='world'
    report['world']=build_world(manager,hud,session)
    report['success']=True
except Exception:
    report['error']=traceback.format_exc()
    unreal.log_error(report['error'])
finally:
    (QA/'build_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
