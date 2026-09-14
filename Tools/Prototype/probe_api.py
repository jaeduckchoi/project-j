"""Read-only API probe; never saves maps or project assets."""
from pathlib import Path
import json, traceback
import unreal
ROOT=Path(__file__).resolve().parents[2]
out=ROOT/'Saved/PrototypeQA'
out.mkdir(parents=True,exist_ok=True)
report={}
try:
    for name in ['BlueprintGraphPin','K2Node','K2Node_DynamicCast','BlueprintGraphEditor','BlueprintEditorLibrary','HUD']:
        cls=getattr(unreal,name,None)
        report[name]=[n for n in dir(cls) if not n.startswith('_')] if cls else None
    report['world']=str(unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world())
    report['maps']={}
    for map_name in ['L_Hub','L_Beach']:
        unreal.get_editor_subsystem(unreal.LevelEditorSubsystem).load_level('/Game/Jonggu/Maps/'+map_name)
        rows=[]
        for actor in unreal.get_editor_subsystem(unreal.EditorActorSubsystem).get_all_level_actors():
            if actor.get_class().get_name() in ('BP_JongguPlayer_C','CameraActor','WorldSettings'):
                loc=actor.get_actor_location()
                rows.append({'name':actor.get_name(),'class':actor.get_class().get_name(),'pos':[loc.x,loc.y,loc.z]})
        report['maps'][map_name]=rows
    report['success']=True
except Exception:
    report['error']=traceback.format_exc()
finally:
    (out/'api_probe.json').write_text(json.dumps(report,indent=2),encoding='utf8')
