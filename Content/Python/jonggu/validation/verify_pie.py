"""Run a marked editor's actual PIE, isolated save, map travel and rendered UI QA."""
from pathlib import Path
import sys,time,json,traceback,uuid
from datetime import datetime,timezone
import unreal
from jonggu.paths import ROOT, require_active_project
from jonggu.assets import asset_path, generated_class_path
require_active_project()
from jonggu.ui.hud import BUTTONS, HUD_FIELDS
from jonggu.validation.verify_popups import button_center
QA=ROOT/'Saved/PrototypeQA';RUN_ID=datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S_%fZ');CAP=QA/'PopupRegression'/RUN_ID/'Screenshots';CAP.mkdir(parents=True,exist_ok=True)
LEVELS=unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
REPORT={'generated_at_utc':datetime.now(timezone.utc).isoformat(),'success':False,'checks':[],'screenshots':[],'physical_keyboard_tested':False}
STATE={'stage':'start','start':time.monotonic(),'wait':0}
HANDLE=None
SLOT='JongguPIEQA_'+uuid.uuid4().hex
SESSION_BP=unreal.load_asset(asset_path('BP_RestaurantGameInstance'))
CDO=unreal.get_default_object(SESSION_BP.generated_class())
OLD_SLOT=CDO.get_editor_property('save_slot')

def write(): (QA/'pie_report.json').write_text(json.dumps(REPORT,ensure_ascii=False,indent=2),encoding='utf8')
def check(label,passed):
    if not passed: raise AssertionError(label)
    if label not in REPORT['checks']:REPORT['checks'].append(label)
def adaptive_check(label,passed):
    if not passed: raise AssertionError('Adaptive HUD: '+label)
    checks = REPORT.setdefault('adaptive_checks', [])
    if label not in checks: checks.append(label)
def has_folds(): return any(name=='hud_expanded_mask' for name,_,_ in HUD_FIELDS)
def set_folds(m,value):
    put(get(m,'session'),'hud_expanded_mask',value);call(m,'RefreshHUD')
def getworld(): return unreal.EditorLevelLibrary.get_game_world()
def current():
    w=getworld()
    m=unreal.GameplayStatics.get_actor_of_class(w,unreal.load_class(None,generated_class_path('BP_RestaurantManager'))) if w else None
    return w,m
def call(m,fn,*args): return m.call_method(fn,args)
def click_button(m,button_id):
    call(m,'RefreshHUD')
    hud=unreal.GameplayStatics.get_player_controller(m,0).get_hud()
    return call(m,'Click',*button_center(button_id,{name:hud.get_editor_property(name) for name,_,_ in HUD_FIELDS}))
def get(m,field): return m.get_editor_property(field)
def put(m,field,value): m.set_editor_property(field,value)
def pose(w,xyz):
    p=unreal.GameplayStatics.get_player_pawn(w,0)
    p.get_movement_component().stop_movement_immediately()
    p.consume_movement_input_vector()
    p.set_actor_location(unreal.Vector(*xyz),False,True)
def screenshot(w,name):
    file=CAP/(name+'.png')
    if STATE.get('capture_name')!=name:
        STATE['capture_name']=name;STATE['capture_started']=time.monotonic()
        if file.exists():file.unlink()
        unreal.SystemLibrary.execute_console_command(w,'Shot -nosuffix filename="'+str(file).replace('\\','/')+'"',unreal.GameplayStatics.get_player_controller(w,0))
        return False
    if not file.exists() or time.monotonic()-STATE['capture_started']<0.1:return False
    REPORT['screenshots'].append(str(file));STATE['capture_name']=None
    return True
def stage(name,delay=0.3):
    STATE['stage']=name;STATE['wait']=time.monotonic()+delay;write()
def finish(error=None):
    global HANDLE
    if STATE.get('finished'):return
    STATE['finished']=True
    if error:REPORT['error']=error
    REPORT['success']=error is None
    REPORT['elapsed_seconds']=time.monotonic()-STATE['start']
    CDO.set_editor_property('save_slot',OLD_SLOT)
    try:
        unreal.GameplayStatics.set_game_paused(getworld(),False)
        if LEVELS.is_in_play_in_editor():LEVELS.editor_request_end_play()
    except Exception:pass
    if HANDLE:unreal.unregister_slate_post_tick_callback(HANDLE);HANDLE=None
    if unreal.GameplayStatics.does_save_game_exist(SLOT,0):unreal.GameplayStatics.delete_game_in_slot(SLOT,0)
    write()
    unreal.SystemLibrary.quit_editor()

def tick(dt):
    if STATE.get('busy') or STATE.get('finished'):return
    STATE['busy']=True
    try:
        if time.monotonic()-STATE['start']>180:raise TimeoutError('PIE stage '+STATE['stage'])
        if time.monotonic()<STATE['wait']:return
        s=STATE['stage']
        if s=='start':
            if not LEVELS.load_level('/Game/Jonggu/Maps/L_Hub'):raise RuntimeError('Hub load failed')
            CDO.set_editor_property('save_slot',SLOT)
            LEVELS.editor_request_begin_play()
            stage('ready',1.0);return
        if s=='restart':
            if LEVELS.is_in_play_in_editor():return
            LEVELS.editor_request_begin_play();stage(STATE.get('restart_target','restarted'),1.0);return
        if not LEVELS.is_in_play_in_editor():return
        w,m=current()
        if m is None or get(m,'session') is None or get(m,'carried') is None:return
        session=get(m,'session')
        if s=='ready':
            if has_folds(): adaptive_check('new initial PIE starts with compact HUD',get(session,'hud_expanded_mask')==0)
            unreal.SystemLibrary.execute_console_command(w,'Slate.Commands.ListBound',unreal.GameplayStatics.get_player_controller(w,0))
            hud=unreal.GameplayStatics.get_player_controller(w,0).get_hud()
            REPORT['hud']={'class':hud.get_class().get_name(),'show_hud':hud.get_editor_property('show_hud'),'viewport_size':list(unreal.GameplayStatics.get_player_controller(w,0).get_viewport_size()),'scale_x':get(hud,'ui_scale_x'),'scale_y':get(hud,'ui_scale_y'),'draw_count':get(hud,'draw_count')}
            check('dedicated controller is active',unreal.GameplayStatics.get_player_controller(w,0).get_class().get_path_name()==generated_class_path('BP_RestaurantPlayerController'))
            check('existing Pawn is possessed',unreal.GameplayStatics.get_player_pawn(w,0).get_class().get_name()=='BP_JongguPlayer_C')
            check('source camera is view target',unreal.GameplayStatics.get_player_controller(w,0).get_view_target().get_class().get_name()=='CameraActor')
            check('PIE uses isolated save',get(session,'save_slot')==SLOT)
            from jonggu.validation.verify_gameplay import validate_gameplay
            REPORT['gameplay']=validate_gameplay(m)
            from jonggu.validation.verify_popups import validate_popups
            REPORT['popups']=validate_popups(m)
            from jonggu.validation.verify_experiments import validate_experiments
            REPORT['experiments']=validate_experiments(m)
            call(m,'SetScreen',0);stage('capture_hub');return
        if s=='capture_hub':
            if not screenshot(w,'01_hub_preparation'):return
            click_button(m,10)
            check('mouse menu click opens preparation screen',get(m,'screen_id')==1)
            stage('capture_menu');return
        if s=='capture_menu':
            if not screenshot(w,'02_menu'):return
            click_button(m,107);check('mouse cancel restores movement',get(m,'screen_id')==0)
            if has_folds(): set_folds(m,5)
            pose(w,[-715,0,-700]);call(m,'Interact')
            stage('beach',0.6);return
        if s=='beach':
            if not get(m,'is_beach'):return
            if has_folds(): adaptive_check('fold selection survives actual Hub to Beach travel',get(session,'hud_expanded_mask')==5)
            p=unreal.GameplayStatics.get_player_pawn(w,0).get_actor_location()
            check('Beach spawns on configured sand',abs(p.x-550)<2 and abs(p.z-400)<2)
            if not screenshot(w,'03_beach'):return
            for xyz in [[250,0,150],[800,0,-50],[1350,0,200]]:
                pose(w,xyz);call(m,'Interact');call(m,'Interact')
            check('three unique clam nodes grant exactly three',get(session,'clams')==3 and get(session,'harvested_mask')==7)
            pose(w,[350,0,400]);call(m,'Interact');stage('returned',0.6);return
        if s=='returned':
            if get(m,'is_beach'):return
            if has_folds(): adaptive_check('fold selection survives actual Beach return',get(session,'hud_expanded_mask')==5)
            check('round trip preserves inventory and depleted points',get(session,'clams')==3 and get(session,'harvested_mask')==7)
            pose(w,[-715,0,-700]);call(m,'Interact');stage('beach_again',0.6);return
        if s=='beach_again':
            if not get(m,'is_beach'):return
            pose(w,[250,0,150]);call(m,'Interact')
            check('same-day revisit cannot regenerate clams',get(session,'clams')==3)
            check('harvested marker hidden',get(m,'clam0').get_editor_property('hidden'))
            pose(w,[350,0,400]);call(m,'Interact');stage('start_special',0.6);return
        if s=='start_special':
            if get(m,'is_beach'):return
            call(m,'Action',10)
            click_button(m,101) # default rice+soup -> soup only
            check('menu mouse toggle leaves soup only',get(session,'menu_mask')==2)
            click_button(m,106)
            check('mouse begin opens service',get(m,'service'))
            pose(w,[-578,0,480]);call(m,'Interact')
            check('pot interaction opens selector',get(m,'screen_id')==3)
            stage('capture_cooking');return
        if s=='capture_cooking':
            if not screenshot(w,'04_cooking_choice'):return
            click_button(m,212)
            check('special start deducts one clam',get(session,'clams')==2 and get(m,'pot_portions')==2)
            STATE['paused_snapshot']=[get(m,'elapsed'),get(m,'pot_remaining'),get(m,'guest_count'),unreal.GameplayStatics.get_time_seconds(w)]
            call(m,'PauseToggle');stage('paused_capture',0.5);return
        if s=='paused_capture':
            if not screenshot(w,'07_pause'):return
            check('actual engine pause freezes clock cooking and arrivals',STATE['paused_snapshot']==[get(m,'elapsed'),get(m,'pot_remaining'),get(m,'guest_count'),unreal.GameplayStatics.get_time_seconds(w)])
            call(m,'PauseToggle');check('pause resume restores game time and movement',get(m,'screen_id')==0 and not unreal.GameplayStatics.is_game_paused(w) and not unreal.GameplayStatics.get_player_controller(w,0).is_move_input_ignored())
            call(m,'Advance',16.0);call(m,'Interact')
            pose(w,[-880,0,-380]);call(m,'Interact')
            check('first actual table interaction serves special',get(m,'served_count')==1)
            call(m,'Advance',21.0)
            stage('capture_service');return
        if s=='capture_service':
            if not screenshot(w,'05_service'):return
            pose(w,[-578,0,480]);call(m,'Interact')
            order_index=0 if get(get(m,'order0'),'active') else 1
            pose(w,[-880 if order_index==0 else 0,0,-380]);call(m,'Interact')
            check('one batch serves two soup orders',get(m,'special_served')==2 and get(m,'clams_used')==1)
            call(m,'Action',21)
            check('early close reaches summary',get(m,'screen_id')==5 and not get(m,'service'))
            stage('capture_summary');return
        if s=='capture_summary':
            if not screenshot(w,'06_summary'):return
            click_button(m,401);stage('next_day',0.6);return
        if s=='next_day':
            if has_folds(): adaptive_check('next-day preparation retains session fold selection',get(session,'hud_expanded_mask')==5)
            check('next day retains unused clams and renews harvest',get(session,'day')==2 and get(session,'clams')==2 and get(session,'harvested_mask')==0)
            # Real injected movement verifies the original pawn and new input gate together.
            pose(w,[-715,0,-415])
            STATE['move_start']=unreal.GameplayStatics.get_player_pawn(w,0).get_actor_location()
            STATE['move_frames']=0;STATE['move_time']=unreal.GameplayStatics.get_time_seconds(w);STATE['last_move_time']=-1;REPORT['movement_samples']=[];stage('movement',0);return
        if s=='movement':
            pawn=unreal.GameplayStatics.get_player_pawn(w,0)
            t=unreal.GameplayStatics.get_time_seconds(w)
            if t==STATE['last_move_time']:return
            STATE['last_move_time']=t
            pc=unreal.GameplayStatics.get_player_controller(w,0)
            loc=pawn.get_actor_location();vel=pawn.get_velocity()
            REPORT['movement_samples'].append({'time':t,'position':[loc.x,loc.y,loc.z],'velocity':[vel.x,vel.y,vel.z],'ignored':pc.is_move_input_ignored(),'screen':get(m,'screen_id'),'paused':unreal.GameplayStatics.is_game_paused(w),'max_speed':pawn.get_movement_component().get_editor_property('max_speed')})
            pawn.add_movement_input(unreal.Vector(1,0,0),1.0,False)
            STATE['move_frames']+=1
            if t-STATE['move_time']>=0.5:
                check('existing Pawn can move after UI and map travel',pawn.get_actor_location().x-STATE['move_start'].x>3)
                call(m,'SetScreen',1);STATE['blocked_pos']=pawn.get_actor_location();stage('blocked',0.2)
            return
        if s=='blocked':
            pawn=unreal.GameplayStatics.get_player_pawn(w,0)
            pawn.add_movement_input(unreal.Vector(1,0,0),1.0,False)
            check('modal UI stops actual Pawn motion',pawn.get_actor_location().distance(STATE['blocked_pos'])<1)
            call(m,'SetScreen',0)
            # Exercise the actual UI retry and resulting OpenLevel, after spending
            # stock and recording revenue that must not survive retry.
            STATE['retry_stock']=get(session,'clams');STATE['retry_revenue']=get(session,'total_revenue')
            put(session,'menu_mask',2);call(m,'OpenService');call(m,'StartCook',3)
            call(m,'Advance',17.0);call(m,'Pickup',1);call(m,'Serve',0)
            check('retry fixture consumed stock and served',get(session,'clams')==STATE['retry_stock']-1 and get(m,'served_count')==1)
            if has_folds(): set_folds(m,6)
            call(m,'PauseToggle');click_button(m,302);stage('retried',0.8);return
        if s=='retried':
            if get(m,'service'):return
            if has_folds(): adaptive_check('actual service retry preserves current session folds',get(session,'hud_expanded_mask')==6)
            check('real retry restores preopen stock revenue and menu',get(session,'clams')==STATE['retry_stock'] and get(session,'total_revenue')==STATE['retry_revenue'] and get(session,'menu_mask')==2)
            check('real retry clears orders cooking plate and pause',not get(get(m,'order0'),'active') and not get(get(m,'order1'),'active') and get(m,'pot_portions')==0 and get(get(m,'carried'),'dish_id')==-1 and not unreal.GameplayStatics.is_game_paused(w))
            STATE['restart_day']=get(session,'day')
            LEVELS.editor_request_end_play();stage('restart',0.6);return
        if s=='restarted':
            if has_folds(): adaptive_check('new PIE resets session folds despite saved checkpoint',get(session,'hud_expanded_mask')==0)
            check('new PIE session reloads checkpoint once',get(session,'day')==STATE['restart_day'] and get(session,'clams')==STATE['retry_stock'] and get(session,'total_revenue')==STATE['retry_revenue'] and get(session,'menu_mask')==2)
            check('new PIE session begins outside service',not get(m,'service') and get(m,'screen_id')==0)
            pose(w,[-715,0,-700]);call(m,'Interact');stage('restart_beach_fixture',0.6);return
        if s=='restart_beach_fixture':
            if not get(m,'is_beach'):return
            pose(w,[250,0,150]);call(m,'Interact')
            pose(w,[350,0,400]);call(m,'Interact');stage('restart_beach_departure',0.6);return
        if s=='restart_beach_departure':
            if get(m,'is_beach'):return
            STATE['beach_stock']=get(session,'clams');STATE['beach_mask']=get(session,'harvested_mask')
            pose(w,[-715,0,-700]);call(m,'Interact');stage('restart_beach_stop',0.6);return
        if s=='restart_beach_stop':
            if not get(m,'is_beach'):return
            STATE['restart_target']='beach_restarted'
            LEVELS.editor_request_end_play();stage('restart',0.6);return
        if s=='beach_restarted':
            if not get(m,'is_beach'):return
            check('new PIE redirects editor Hub to saved Beach',get(session,'checkpoint_map')=='L_Beach')
            check('Beach restart preserves stock and harvest without duplication',get(session,'clams')==STATE['beach_stock'] and get(session,'harvested_mask')==STATE['beach_mask'] and get(m,'clam0').get_editor_property('hidden'))
            pose(w,[250,0,150]);call(m,'Interact')
            check('restored harvested point cannot grant another clam',get(session,'clams')==STATE['beach_stock'])
            finish();return
    except Exception: finish(traceback.format_exc())
    finally: STATE['busy']=False

line=unreal.SystemLibrary.get_command_line()
if '-JongguPrototypeQA' not in line:raise RuntimeError('Run only a new marked QA editor')
unreal.EditorPythonScripting.set_keep_python_script_alive(True)
HANDLE=unreal.register_slate_post_tick_callback(tick)
write()



