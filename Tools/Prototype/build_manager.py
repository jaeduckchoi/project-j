"""Restaurant rules compiled into saved Blueprint graphs; no Python at runtime."""
from pathlib import Path
import json
import unreal
from bp_helpers import *
from data_types import RECIPE_CLASS, ORDER_CLASS, DISH_CLASS
from build_session import SESSION_CLASS
from build_ui import BUTTONS, HUD_FIELDS
from hud_layout import button_rect_expr
MANAGER_CLASS=PREFIX+'/BP_PrototypeManager.BP_PrototypeManager_C'
HUD_CLASS=PREFIX+'/BP_PrototypeHUD.BP_PrototypeHUD_C'
GS='/Script/Engine.GameplayStatics.'
ACTOR='/Script/Engine.Actor.'
PC='/Script/Engine.PlayerController.'
LAYOUT=json.loads((Path(__file__).parent/'layout.json').read_text(encoding='utf8'))
LABELS=['메뉴판 · 오늘의 메뉴 정하기','팬 · 조리 / 완성품 받기','냄비 · 조리 / 완성품 받기','반납대 · 든 접시 비우기','해변으로 나가기','1번 손님 · 서빙','2번 손님 · 서빙','식당으로 돌아가기','조개 줍기','조개 줍기','조개 줍기']
POSITIONS=[i['position_cm'] for i in LAYOUT['maps']['Hub']['interactions']]+[i['position_cm'] for i in LAYOUT['maps']['Beach']['interactions']]
def eq(g,a,b): return g.math('EqualEqual_IntInt',A=a,B=b)
def ne(g,a,b): return g.math('NotEqual_IntInt',A=a,B=b)
def both(g,*xs):
    out=xs[0]
    for x in xs[1:]: out=g.math('BooleanAND',A=out,B=x)
    return out
def either(g,*xs):
    out=xs[0]
    for x in xs[1:]: out=g.math('BooleanOR',A=out,B=x)
    return out
def neg(g,x): return g.math('Not_PreBool',A=x)
def add(g,a,b): return g.math('Add_IntInt',A=a,B=b)
def sub(g,a,b): return g.math('Subtract_IntInt',A=a,B=b)
def ss(g,name): return g.get(name,obj=g.get('session'),cls=SESSION_CLASS)
def setss(g,name,val): return g.set(name,val,obj=g.get('session'),cls=SESSION_CLASS)
def session_call(g,name,**kw): return g.call(SESSION_CLASS+'.'+name,self=g.get('session'),**kw)
def own(g,name,**kw): return g.call(MANAGER_CLASS+'.'+name,**kw)
def order(g,i,name): return g.get(name,obj=g.get('order'+str(i)),cls=ORDER_CLASS)
def order_set(g,i,name,val): return g.set(name,val,obj=g.get('order'+str(i)),cls=ORDER_CLASS)
def dish(g,name): return g.get(name,obj=g.get('carried'),cls=DISH_CLASS)
def dish_set(g,name,val): return g.set(name,val,obj=g.get('carried'),cls=DISH_CLASS)
def log(g,text):
    details=g.join(text,' elapsed=',g.integer_string(g.math('FFloor',A=g.get('elapsed'))),
        ' intake=',g.math('SelectString',A='paused',B='open',bPickA=g.get('intake_paused')),
        ' closing=',g.math('SelectString',A='true',B='false',bPickA=g.get('closing')),
        ' plate=',g.integer_string(dish(g,'dish_id')),
        ' special=',g.math('SelectString',A='true',B='false',bPickA=dish(g,'is_special')))
    return session_call(g,'LogEvent',event_text=details)
def notice(g,text): g.set('notice_text',text); g.set('notice_remaining',4.0)
def selected(g,dishid):
    bit=g.math('SelectInt',A=1,B=g.math('SelectInt',A=2,B=4,bPickA=eq(g,dishid,1)),bPickA=eq(g,dishid,0))
    return ne(g,g.math('And_IntInt',A=ss(g,'menu_mask'),B=bit),0)
def dname(g,id):
    return g.math('SelectString',A='김치볶음밥',B=g.math('SelectString',A='김치찌개',B='김치전',bPickA=eq(g,id,1)),bPickA=eq(g,id,0))
def station(g,p): return either(g,g.math('Greater_DoubleDouble',A=g.get(p+'_remaining'),B=0.0),g.math('Greater_IntInt',A=g.get(p+'_portions'),B=0))
def ready(g,p): return both(g,g.math('LessEqual_DoubleDouble',A=g.get(p+'_remaining'),B=0.0),g.math('Greater_IntInt',A=g.get(p+'_portions'),B=0))
def button_visible(g, button):
    condition=eq(g,g.get('screen_id'),button['screen'])
    mode=button.get('condition')
    if mode=='service': return both(g,condition,g.get('service'))
    if mode=='service_controls': return both(g,condition,g.get('service'),neg(g,g.get('closing')))
    if mode=='preparation': return both(g,condition,neg(g,g.get('service')),neg(g,g.get('is_beach')))
    if mode=='can_retry': return both(g,condition,g.get('service'))
    return condition

def button_hit(g, button, mx, my):
    # Draw and input share the same expansion/layout expression. In particular,
    # expanding orders also moves the inventory group's clickable rectangle.
    x,y,w,h=button_rect_expr(g,button,mask=ss(g,'hud_expanded_mask'),is_service=g.get('service'))
    # Promoted math requires a connected typed pin; static edges need no node.
    right=x+w if isinstance(x,(int,float)) and isinstance(w,(int,float)) else g.math('Add_DoubleDouble',A=x,B=w)
    bottom=y+h if isinstance(y,(int,float)) and isinstance(h,(int,float)) else g.math('Add_DoubleDouble',A=y,B=h)
    return both(g,button_visible(g,button),
        g.math('GreaterEqual_DoubleDouble',A=mx,B=x),
        g.math('Less_DoubleDouble',A=mx,B=right),
        g.math('GreaterEqual_DoubleDouble',A=my,B=y),
        g.math('Less_DoubleDouble',A=my,B=bottom))

def checked_save(g):
    saved=session_call(g,'SaveCheckpoint').find_output_pin('success')
    g.when(neg(g,saved),lambda x:notice(x,'저장하지 못했습니다. 디스크 상태를 확인해 주세요.'))
    return saved

def build_manager(typed):
    bp=ensure_bp('BP_PrototypeManager',unreal.Actor)
    specs=[
        ('session','object:'+SESSION_CLASS,None),('order0','object:'+ORDER_CLASS,None),('order1','object:'+ORDER_CLASS,None),
        ('carried','object:'+DISH_CLASS,None),('chosen_recipe','object:'+RECIPE_CLASS,None),
        ('is_beach','bool',False),('spawn_position','vector','(X=-715,Y=0,Z=-415)'),
        ('screen_id','int',0),('previous_screen','int',0),('service','bool',False),('intake_paused','bool',False),('closing','bool',False),
        ('elapsed','real',0.0),('next_arrival','real',0.0),('guest_count','int',0),('served_count','int',0),
        ('base_revenue','int',0),('bonus_revenue','int',0),('clams_used','int',0),('special_served','int',0),
        ('base_price','int',10),('arrival_interval','real',20.0),('bonus_fast_seconds','real',30.0),('bonus_slow_seconds','real',60.0),
        ('bonus_fast','int',2),('bonus_slow','int',1),('nearest_id','int',-1),('best_distance','real',131.0),
        ('notice_text','string','조개 1개로 찌개를 두 그릇씩 만들 수 있어요.'),('notice_remaining','real',7.0),
        ('interaction_hint','string',''),('temp_int','int',0),('temp_dish','int',0),('temp_bonus','int',0)]
    for p in ['pan','pot']:
        specs += [(p+'_remaining','real',0.0),(p+'_duration','real',1.0),(p+'_portions','int',0),(p+'_dish','int',-1),(p+'_special','bool',False)]
    for n in ['rice','soup','pancake','special']: specs.append(('recipe_'+n,'object:'+RECIPE_CLASS,None))
    for n in ['customer0','customer1','clam0','clam1','clam2']: specs.append((n,'object:/Script/Engine.Actor',None))
    specs += [('hovered_action','int',0),('pressed_action','int',0),('pointer_x','real',-1.0),('pointer_y','real',-1.0)]
    # The world builder initializes these from the same authored hotspots.
    for index in (1,2):
        xyz=POSITIONS[index]
        specs.append(('target'+str(index),'vector','(X=%s,Y=%s,Z=%s)' % tuple(xyz)))
    specs += [('can_cook_'+name,'bool',False) for name in ['rice','pancake','soup','special']]
    specs += [('reason_'+name,'string','') for name in ['rice','pancake','soup','special']]
    specs += [('can_clear_pan','bool',False),('can_clear_pot','bool',False)]
    add_vars(bp,specs)
    signatures={'Setup':{},'Advance':{'dt':'real'},'RefreshHUD':{},'SetScreen':{'screen':'int'},'PauseToggle':{},
        'RefreshUIState':{},'UpdatePointer':{'mx':'real','my':'real','pressed':'bool'},
        'Click':{'mx':'real','my':'real'},'Action':{'action':'int'},'Interact':{},'UseInteraction':{'interaction_id':'int'},
        'StartCook':{'recipe_id':'int'},'Pickup':{'station_id':'int'},'EmptyStation':{'station_id':'int'},'Serve':{'seat':'int'},
        'TryArrival':{},'AssignOrder':{'seat':'int'},'TryFinish':{},'OpenService':{},'Finish':{},'Travel':{'to_beach':'bool'},'FindInteraction':{}}
    for name,inputs in signatures.items(): declare_function(bp,name,inputs)
    compile_bp(bp)
    for fn in [setup,advance,refresh_ui_state,refresh_hud,set_screen,pause_toggle,update_pointer,click,action,interact,use_interaction,start_cook,pickup,empty_station,serve,try_arrival,assign_order,try_finish,open_service,finish,travel,find_interaction]:
        fn(bp)
    g=G.event(bp,'ReceiveBeginPlay'); own(g,'Setup')
    g=G.event(bp,'ReceiveTick'); own(g,'Advance',dt=g.param('DeltaSeconds'))
    compile_bp(bp)
    cdo=unreal.get_default_object(bp.generated_class())
    for n,value in zip(['rice','soup','pancake','special'],typed['recipes']): cdo.set_editor_property('recipe_'+n,value)
    save_bp(bp)
    return bp

def setup(bp):
    g=G(bp,'Setup')
    gi=g.pure(GS+'GetGameInstance')
    g.set('session',g.cast(gi,SESSION_CLASS))
    session_call(g,'InitializeSession')
    level=g.call(GS+'GetCurrentLevelName',bRemovePrefixString=True).find_output_pin('ReturnValue')
    def resume_map(x):
        x.call(GS+'OpenLevel',LevelName=ss(x,'checkpoint_map'),bAbsolute=True,Options='')
        x.ret()
    g.when(g.pure('/Script/Engine.KismetStringLibrary.NotEqual_StrStr',A=level,B=ss(g,'checkpoint_map')),resume_map)
    for i in range(2):
        obj=g.call(GS+'SpawnObject',ObjectClass=ORDER_CLASS).find_output_pin('ReturnValue')
        # ObjectClass pins do not narrow this native function; cast via native Object action.
        g.set('order'+str(i),g.cast(obj,ORDER_CLASS))
    obj=g.call(GS+'SpawnObject',ObjectClass=DISH_CLASS).find_output_pin('ReturnValue')
    g.set('carried',g.cast(obj,DISH_CLASS))
    def returning(x):
        x.set('spawn_position',x.math('MakeVector',X=-715,Y=0,Z=-520))
    g.when(both(g,neg(g,g.get('is_beach')),g.pure('/Script/Engine.KismetStringLibrary.EqualEqual_StrStr',A=ss(g,'entry_marker'),B='hub_return')),returning)
    pawn=g.pure(GS+'GetPlayerPawn',PlayerIndex=0)
    g.call(ACTOR+'K2_SetActorLocation',self=pawn,NewLocation=g.get('spawn_position'),bSweep=False,bTeleport=True)
    for name in ['customer0','customer1']:
        a=g.get(name)
        g.when(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=a),lambda x,a=a:x.call(ACTOR+'SetActorHiddenInGame',self=a,bNewHidden=True))
    def hub(x):
        x.when(eq(x,ss(x,'checkpoint_phase'),2),lambda y:own(y,'SetScreen',screen=5),lambda y:own(y,'SetScreen',screen=0))
    g.when(neg(g,g.get('is_beach')),hub)
    own(g,'FindInteraction'); own(g,'RefreshHUD')

def set_screen(bp):
    g=G(bp,'SetScreen')
    g.set('screen_id',g.param('screen'))
    g.set('hovered_action',0);g.set('pressed_action',0)
    pc=g.pure(GS+'GetPlayerController',PlayerIndex=0)
    g.call('/Script/Engine.Controller.ResetIgnoreMoveInput',self=pc)
    g.call('/Script/Engine.Controller.SetIgnoreMoveInput',self=pc,bNewMoveInput=ne(g,g.get('screen_id'),0))
    pawn=g.pure(GS+'GetPlayerPawn',PlayerIndex=0)
    movement=g.pure('/Script/Engine.Pawn.GetMovementComponent',self=pawn)
    g.call('/Script/Engine.MovementComponent.StopMovementImmediately',self=movement)
    g.call('/Script/Engine.Pawn.ConsumeMovementInputVector',self=pawn)
    g.call('/Script/UMG.WidgetBlueprintLibrary.SetInputMode_GameAndUIEx',PlayerController=pc,InMouseLockMode='DoNotLock',bHideCursorDuringCapture=False,bFlushInput=False)
    g.call(GS+'SetGamePaused',bPaused=eq(g,g.get('screen_id'),4))
    own(g,'RefreshHUD')

def pause_toggle(bp):
    g=G(bp,'PauseToggle')
    def pause(x):
        x.set('previous_screen',x.get('screen_id'))
        own(x,'SetScreen',screen=4)
    g.when(eq(g,g.get('screen_id'),4),lambda x:own(x,'SetScreen',screen=x.get('previous_screen')),pause)

def advance(bp):
    g=G(bp,'Advance')
    # Startup can briefly redirect from the editor map to the saved map.
    # Do not read world objects until Setup has allocated this map's state.
    g.guard(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=g.get('carried')))
    dt=g.param('dt')
    def run(x):
        x.when(x.math('Greater_DoubleDouble',A=x.get('notice_remaining'),B=0.0),
            lambda y:y.set('notice_remaining',y.math('FMax',A=0.0,B=y.math('Subtract_DoubleDouble',A=y.get('notice_remaining'),B=dt))))
        x.when(x.math('LessEqual_DoubleDouble',A=x.get('notice_remaining'),B=0.0),lambda y:y.set('notice_text',''))
        def service(y):
            y.set('elapsed',y.math('Add_DoubleDouble',A=y.get('elapsed'),B=dt))
            for p in ['pan','pot']:
                def cook(z,p=p):
                    z.set(p+'_remaining',z.math('FMax',A=0.0,B=z.math('Subtract_DoubleDouble',A=z.get(p+'_remaining'),B=dt)))
                    def completed(w):
                        notice(w,('팬' if p=='pan' else '냄비')+' · 음식이 완성됐어요.')
                        log(w,p+' completed')
                    z.when(z.math('LessEqual_DoubleDouble',A=z.get(p+'_remaining'),B=0.0),completed)
                y.when(y.math('Greater_DoubleDouble',A=y.get(p+'_remaining'),B=0.0),cook)
            own(y,'TryArrival')
            own(y,'TryFinish')
        x.when(x.get('service'),service)
        own(x,'FindInteraction')
    g.when(ne(g,g.get('screen_id'),4),run)
    own(g,'RefreshHUD')

def find_interaction(bp):
    g=G(bp,'FindInteraction')
    g.set('nearest_id',-1);g.set('best_distance',131.0);g.set('interaction_hint','')
    player=g.pure(GS+'GetPlayerPawn',PlayerIndex=0)
    pos=g.pure(ACTOR+'K2_GetActorLocation',self=player)
    for idx,xyz in enumerate(POSITIONS):
        allowed=g.get('is_beach') if idx>=7 else neg(g,g.get('is_beach'))
        if idx>=8:
            allowed=both(g,allowed,eq(g,g.math('And_IntInt',A=ss(g,'harvested_mask'),B=1<<(idx-8)),0))
        def check(x,idx=idx,xyz=xyz):
            vec=x.math('MakeVector',X=xyz[0],Y=xyz[1],Z=xyz[2])
            dist=x.math('Vector_Distance',V1=pos,V2=vec)
            def choose(y):
                y.set('nearest_id',idx);y.set('best_distance',dist);y.set('interaction_hint','E  '+LABELS[idx])
            x.when(x.math('Less_DoubleDouble',A=dist,B=x.get('best_distance')),choose)
        g.when(allowed,check)

def interact(bp):
    g=G(bp,'Interact');g.guard(eq(g,g.get('screen_id'),0))
    own(g,'FindInteraction')
    own(g,'UseInteraction',interaction_id=g.get('nearest_id'))

def use_interaction(bp):
    g=G(bp,'UseInteraction');idx=g.param('interaction_id')
    for i in range(11):
        def do(x,i=i):
            if i==0:
                x.when(neg(x,x.get('service')),lambda y:own(y,'SetScreen',screen=1))
            elif i in (1,2):
                p='pan' if i==1 else 'pot'
                def appliance(y):
                    # Always show selector when hands are occupied, allowing complete-pot clearing.
                    y.when(both(y,ready(y,p),eq(y,dish(y,'dish_id'),-1)),
                        lambda z:own(z,'Pickup',station_id=i-1),lambda z:own(z,'SetScreen',screen=i+1))
                x.when(x.get('service'),appliance,lambda y:notice(y,'메뉴를 고르고 영업을 시작해 주세요.'))
            elif i==3:
                dish_set(x,'dish_id',-1);dish_set(x,'is_special',False);notice(x,'접시를 비웠습니다.');log(x,'discard plate')
            elif i in (4,7):
                own(x,'Travel',to_beach=i==4)
            elif i in (5,6): own(x,'Serve',seat=i-5)
            else:
                def collect(y):
                    result=session_call(y,'CollectClam',point_bit=1<<(i-8)).find_output_pin('success')
                    y.when(result,lambda z:notice(z,'조개를 찾았어요. 1개로 찌개 2그릇!'))
                x.when(both(x,x.get('is_beach'),neg(x,x.get('service'))),collect)
        g.when(eq(g,idx,i),do)

def refresh_ui_state(bp):
    g=G(bp,'RefreshUIState')
    g.guard(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=g.get('session')))
    for name,p in [('rice','pan'),('pancake','pan'),('soup','pot'),('special','pot')]:
        recipe=g.get('recipe_'+name)
        listed=selected(g,g.get('dish_id',obj=recipe,cls=RECIPE_CLASS))
        enough=g.math('GreaterEqual_IntInt',A=ss(g,'clams'),B=g.get('clam_cost',obj=recipe,cls=RECIPE_CLASS))
        occupied=station(g,p)
        g.set('can_cook_'+name,both(g,g.get('service'),listed,neg(g,occupied),enough))
        busy_reason=g.math('SelectString',A='조리 중이에요.',B='완성품을 받거나 비워 주세요.',
            bPickA=g.math('Greater_DoubleDouble',A=g.get(p+'_remaining'),B=0.0))
        reason=g.math('SelectString',A='영업을 시작해 주세요.',
            B=g.math('SelectString',A='오늘 메뉴에 없어요.',
                B=g.math('SelectString',A=busy_reason,
                    B=g.math('SelectString',A='',B='조개가 부족해요.',bPickA=enough),bPickA=occupied),
                bPickA=neg(g,listed)),bPickA=neg(g,g.get('service')))
        g.set('reason_'+name,reason)
    for p in ['pan','pot']: g.set('can_clear_'+p,both(g,g.get('service'),ready(g,p)))


def update_pointer(bp):
    g=G(bp,'UpdatePointer')
    g.set('pointer_x',g.param('mx'));g.set('pointer_y',g.param('my'))
    g.set('hovered_action',0);g.set('pressed_action',0)
    g.guard(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=g.get('carried')))
    own(g,'RefreshUIState')
    mx,my=g.param('mx'),g.param('my')
    for button in BUTTONS:
        def hover(x,button=button):
            x.set('hovered_action',button['id'])
            enabled=button.get('enabled_field')
            down=both(x,x.param('pressed'),x.get(enabled)) if enabled else x.param('pressed')
            x.when(down,lambda y:y.set('pressed_action',button['id']))
            own(x,'RefreshHUD');x.ret()
        g.when(button_hit(g,button,mx,my),hover)
    own(g,'RefreshHUD')


def click(bp):
    g=G(bp,'Click')
    g.guard(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=g.get('carried')))
    own(g,'RefreshUIState')
    mx,my=g.param('mx'),g.param('my')
    # Disabled hits are consumed without an action. Every handled click returns
    # immediately, so a newly opened screen cannot receive this same click.
    for button in BUTTONS:
        def clicked(x,button=button):
            enabled=button.get('enabled_field')
            if enabled: x.when(x.get(enabled),lambda y:own(y,'Action',action=button['id']))
            else: own(x,'Action',action=button['id'])
            own(x,'RefreshHUD');x.ret()
        g.when(button_hit(g,button,mx,my),clicked)


def action(bp):
    g=G(bp,'Action');a=g.param('action')
    for id in [10,20,21,30,31,32,33,101,102,103,104,105,106,107,201,202,203,204,211,212,213,214,301,302,303,401]:
        def run(x,id=id):
            if id==10:
                x.guard(both(x,neg(x,x.get('service')),neg(x,x.get('is_beach'))));own(x,'SetScreen',screen=1)
            elif id==20:
                x.guard(both(x,x.get('service'),neg(x,x.get('closing'))))
                x.set('intake_paused',neg(x,x.get('intake_paused')))
                # Resuming starts a fresh interval and cannot release accumulated arrivals.
                x.set('next_arrival',x.math('Add_DoubleDouble',A=x.get('elapsed'),B=x.get('arrival_interval')))
                log(x,'toggle intake')
            elif id==21:
                x.guard(x.get('service'));x.set('closing',True);x.set('intake_paused',True)
                notice(x,'접수한 주문을 마치면 오늘 영업이 끝납니다.');log(x,'close requested');own(x,'TryFinish')
            elif id==30: own(x,'PauseToggle')
            elif id in (31,32,33):
                x.guard(eq(x,x.get('screen_id'),0))
                x.guard(x.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=x.get('session')))
                if id in (31,32): x.guard(x.get('service'))
                setss(x,'hud_expanded_mask',x.math('Xor_IntInt',A=ss(x,'hud_expanded_mask'),B=1<<(id-31)))
                x.set('hovered_action',0);x.set('pressed_action',0)
                own(x,'RefreshHUD')
            elif id in (101,102,103):
                x.guard(both(x,neg(x,x.get('service')),eq(x,x.get('screen_id'),1)))
                bit=1<<(id-101)
                x.set('temp_int',x.math('Xor_IntInt',A=ss(x,'menu_mask'),B=bit))
                valid=both(x,ne(x,x.get('temp_int'),0),ne(x,x.get('temp_int'),7))
                x.when(valid,lambda y:(setss(y,'menu_mask',y.get('temp_int')),log(y,'menu changed')),
                    lambda y:notice(y,'메뉴는 1종 또는 2종을 골라 주세요.'))
            elif id in (104,105):
                x.guard(neg(x,x.get('service')));setss(x,'guest_target',4 if id==104 else 6)
            elif id==106: own(x,'OpenService')
            elif id in (107,204,214): own(x,'SetScreen',screen=0)
            elif id in (201,202,211,212): own(x,'StartCook',recipe_id={201:0,202:2,211:1,212:3}[id])
            elif id in (203,213):
                own(x,'EmptyStation',station_id=0 if id==203 else 1);own(x,'SetScreen',screen=0)
            elif id in (301,303): own(x,'SetScreen',screen=x.get('previous_screen'))
            elif id==302:
                x.guard(x.get('service'))
                loaded=session_call(x,'LoadCheckpoint').find_output_pin('success')
                def retry(y):
                    log(y,'retry service');own(y,'SetScreen',screen=0)
                    y.call(GS+'OpenLevel',LevelName='L_Hub',bAbsolute=True,Options='')
                x.when(loaded,retry,lambda y:notice(y,'준비 기록을 읽지 못해 재시도를 중단했습니다.'))
            elif id==401:
                x.guard(eq(x,ss(x,'checkpoint_phase'),2))
                session_call(x,'BeginNextDay');own(x,'SetScreen',screen=0)
                x.call(GS+'OpenLevel',LevelName='L_Hub',bAbsolute=True,Options='')
            x.ret()
        g.when(eq(g,a,id),run)

def open_service(bp):
    g=G(bp,'OpenService')
    g.guard(both(g,neg(g,g.get('is_beach')),neg(g,g.get('service')),ne(g,ss(g,'checkpoint_phase'),2)))
    g.guard(both(g,ne(g,ss(g,'menu_mask'),0),ne(g,ss(g,'menu_mask'),7)))
    setss(g,'checkpoint_phase',0);setss(g,'checkpoint_map','L_Hub');setss(g,'entry_marker','hub')
    saved=checked_save(g)
    g.guard(saved)
    for n in ['served_count','guest_count','base_revenue','bonus_revenue','clams_used','special_served']:g.set(n,0)
    g.set('elapsed',0.0);g.set('next_arrival',0.0);g.set('closing',False);g.set('intake_paused',False);g.set('service',True)
    log(g,'open service');own(g,'SetScreen',screen=0);own(g,'TryArrival')

def start_cook(bp):
    g=G(bp,'StartCook')
    g.guard(g.get('service'))
    rid=g.param('recipe_id')
    g.guard(both(g,g.math('GreaterEqual_IntInt',A=rid,B=0),g.math('LessEqual_IntInt',A=rid,B=3)))
    for i,name in enumerate(['rice','soup','pancake','special']):
        g.when(eq(g,rid,i),lambda x,name=name:x.set('chosen_recipe',x.get('recipe_'+name)))
    r=g.get('chosen_recipe')
    value=lambda field:g.get(field,obj=r,cls=RECIPE_CLASS)
    def execute(x):
        for p,appliance in [('pan',0),('pot',1)]:
            def begin(y,p=p):
                def free(z):
                    enough=z.math('GreaterEqual_IntInt',A=ss(z,'clams'),B=value('clam_cost'))
                    def start(w):
                        setss(w,'clams',sub(w,ss(w,'clams'),value('clam_cost')))
                        w.set('clams_used',add(w,w.get('clams_used'),value('clam_cost')))
                        w.set(p+'_remaining',value('cook_seconds'));w.set(p+'_duration',value('cook_seconds'))
                        w.set(p+'_portions',value('portions'));w.set(p+'_dish',value('dish_id'));w.set(p+'_special',eq(w,rid,3))
                        log(w,w.join('cook ',value('display_name')));own(w,'SetScreen',screen=0)
                    z.when(enough,start,lambda w:notice(w,'조개가 부족해요. 기본 찌개는 계속 만들 수 있습니다.'))
                y.when(neg(y,station(y,p)),free,lambda z:notice(z,'기구를 사용 중입니다. 완성품을 받거나 비워 주세요.'))
            x.when(eq(x,value('appliance'),appliance),begin)
    g.when(selected(g,value('dish_id')),execute,lambda x:notice(x,'오늘 메뉴에 없는 음식입니다.'))

def pickup(bp):
    g=G(bp,'Pickup')
    g.guard(both(g,g.get('service'),eq(g,dish(g,'dish_id'),-1)))
    for i,p in enumerate(['pan','pot']):
        def take(x,p=p):
            x.guard(ready(x,p))
            dish_set(x,'dish_id',x.get(p+'_dish'));dish_set(x,'is_special',x.get(p+'_special'))
            x.set(p+'_portions',sub(x,x.get(p+'_portions'),1))
            log(x,'pickup '+p)
        g.when(eq(g,g.param('station_id'),i),take)

def empty_station(bp):
    g=G(bp,'EmptyStation');g.guard(g.get('service'))
    for i,p in enumerate(['pan','pot']):
        def clear(x,p=p):
            x.guard(ready(x,p));x.set(p+'_portions',0);notice(x,'남은 완성품을 비웠습니다.');log(x,'discard '+p)
        g.when(eq(g,g.param('station_id'),i),clear)

def serve(bp):
    g=G(bp,'Serve');g.guard(g.get('service'))
    for i in range(2):
        def table(x,i=i):
            good=both(x,order(x,i,'active'),eq(x,order(x,i,'dish_id'),dish(x,'dish_id')))
            def deliver(y):
                wait=y.math('Subtract_DoubleDouble',A=y.get('elapsed'),B=order(y,i,'placed_at'))
                bonus=y.math('SelectInt',A=y.get('bonus_fast'),B=y.math('SelectInt',A=y.get('bonus_slow'),B=0,bPickA=y.math('LessEqual_DoubleDouble',A=wait,B=y.get('bonus_slow_seconds'))),bPickA=y.math('LessEqual_DoubleDouble',A=wait,B=y.get('bonus_fast_seconds')))
                y.set('temp_bonus',bonus)
                y.set('served_count',add(y,y.get('served_count'),1));y.set('base_revenue',add(y,y.get('base_revenue'),y.get('base_price')))
                y.set('bonus_revenue',add(y,y.get('bonus_revenue'),y.get('temp_bonus')))
                y.when(dish(y,'is_special'),lambda z:z.set('special_served',add(z,z.get('special_served'),1)))
                notice(y,y.math('SelectString',A='조개가 듬뿍이네요. 잘 먹었습니다!',B='따뜻한 한 끼, 잘 먹었습니다!',bPickA=dish(y,'is_special')))
                log(y,y.join('serve seat ',str(i+1),' dish ',y.integer_string(dish(y,'dish_id')),' bonus ',y.integer_string(y.get('temp_bonus'))))
                dish_set(y,'dish_id',-1);dish_set(y,'is_special',False);order_set(y,i,'active',False)
                own(y,'TryFinish')
            x.when(good,deliver,lambda y:notice(y,'손님의 주문과 같은 음식을 들고 와 주세요.'))
        g.when(eq(g,g.param('seat'),i),table)

def try_arrival(bp):
    g=G(bp,'TryArrival')
    g.guard(both(g,g.get('service'),neg(g,g.get('closing')),neg(g,g.get('intake_paused')),
        g.math('Less_IntInt',A=g.get('guest_count'),B=ss(g,'guest_target')),
        g.math('GreaterEqual_DoubleDouble',A=g.get('elapsed'),B=g.get('next_arrival'))))
    def first(x): own(x,'AssignOrder',seat=0)
    def second(x): x.when(neg(x,order(x,1,'active')),lambda y:own(y,'AssignOrder',seat=1))
    g.when(neg(g,order(g,0,'active')),first,second)
    # When both seats are full, start a fresh interval. Never queue a burst.
    g.set('next_arrival',g.math('Add_DoubleDouble',A=g.get('elapsed'),B=g.get('arrival_interval')))

def assign_order(bp):
    g=G(bp,'AssignOrder')
    # Enumerate selected dish IDs into two slots using mask values; never order an unavailable variant.
    mask=ss(g,'menu_mask')
    first=g.math('SelectInt',A=0,B=g.math('SelectInt',A=1,B=2,bPickA=ne(g,g.math('And_IntInt',A=mask,B=2),0)),bPickA=ne(g,g.math('And_IntInt',A=mask,B=1),0))
    second=g.math('SelectInt',A=2,B=g.math('SelectInt',A=1,B=first,bPickA=eq(g,mask,3)),bPickA=either(g,eq(g,mask,5),eq(g,mask,6)))
    g.set('temp_dish',g.math('SelectInt',A=first,B=second,bPickA=eq(g,g.math('Percent_IntInt',A=g.get('guest_count'),B=2),0)))
    for i in range(2):
        def put(x,i=i):
            order_set(x,i,'dish_id',x.get('temp_dish'));order_set(x,i,'placed_at',x.get('elapsed'))
            order_set(x,i,'customer_index',add(x,x.get('guest_count'),1));order_set(x,i,'active',True)
        g.when(eq(g,g.param('seat'),i),put)
    g.set('guest_count',add(g,g.get('guest_count'),1));log(g,g.join('order ',g.integer_string(g.get('temp_dish'))))

def try_finish(bp):
    g=G(bp,'TryFinish')
    complete=either(g,g.get('closing'),g.math('GreaterEqual_IntInt',A=g.get('guest_count'),B=ss(g,'guest_target')))
    g.when(both(g,g.get('service'),complete,neg(g,order(g,0,'active')),neg(g,order(g,1,'active'))),lambda x:own(x,'Finish'))

def finish(bp):
    g=G(bp,'Finish');g.guard(g.get('service'))
    g.set('service',False);g.set('closing',True)
    for p in ['pan','pot']:g.set(p+'_portions',0);g.set(p+'_remaining',0.0)
    dish_set(g,'dish_id',-1);dish_set(g,'is_special',False)
    for sessionname,name in [('summary_served','served_count'),('summary_base','base_revenue'),('summary_bonus','bonus_revenue'),('summary_clams_used','clams_used'),('summary_special_served','special_served')]:setss(g,sessionname,g.get(name))
    setss(g,'total_revenue',add(g,ss(g,'total_revenue'),add(g,g.get('base_revenue'),g.get('bonus_revenue'))))
    setss(g,'checkpoint_phase',2);setss(g,'checkpoint_map','L_Hub');setss(g,'entry_marker','hub')
    log(g,'summary');checked_save(g);own(g,'SetScreen',screen=5)

def travel(bp):
    g=G(bp,'Travel');g.guard(both(g,neg(g,g.get('service')),ne(g,ss(g,'checkpoint_phase'),2)))
    target=g.param('to_beach')
    g.guard(g.math('NotEqual_BoolBool',A=target,B=g.get('is_beach')))
    setss(g,'checkpoint_map',g.math('SelectString',A='L_Beach',B='L_Hub',bPickA=target))
    setss(g,'entry_marker',g.math('SelectString',A='beach',B='hub_return',bPickA=target))
    saved=checked_save(g)
    g.guard(saved);log(g,'travel')
    own(g,'SetScreen',screen=0)
    g.call(GS+'OpenLevel',LevelName=g.math('SelectString',A='L_Beach',B='L_Hub',bPickA=target),bAbsolute=True,Options='')

def refresh_hud(bp):
    g=G(bp,'RefreshHUD')
    g.guard(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=g.get('carried')))
    own(g,'RefreshUIState')
    hud=g.call(GS+'GetActorOfClass',ActorClass=HUD_CLASS).find_output_pin('ReturnValue')
    g.guard(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=hud))
    put=lambda key,val:g.set(key,val,obj=hud,cls=HUD_CLASS)
    for key,val in [('screen_id',g.get('screen_id')),('menu_mask',ss(g,'menu_mask')),('guest_limit',ss(g,'guest_target')),
        ('is_service',g.get('service')),('closing',g.get('closing')),('intake_paused',g.get('intake_paused')),('is_beach',g.get('is_beach')),('can_retry',g.get('service')),
        ('notice_text',g.get('notice_text'))]:put(key,val)
    put('clams',ss(g,'clams'))
    put('hud_expanded_mask',ss(g,'hud_expanded_mask'))
    put('pointer_x',g.get('pointer_x'));put('pointer_y',g.get('pointer_y'))
    player=g.pure(GS+'GetPlayerPawn',PlayerIndex=0)
    valid_player=g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=player)
    put('player_actor',None)
    g.when(valid_player,lambda x:x.set('player_actor',player,obj=hud,cls=HUD_CLASS))
    anchor_valid=neg(g,g.get('is_beach'))
    for prefix,index in [('pan',1),('pot',2)]:
        put(prefix+'_anchor_valid',anchor_valid)
        put(prefix+'_anchor',g.math('MakeVector',X=0.0,Y=0.0,Z=0.0))
        def assign_anchor(x,prefix=prefix,index=index):
            anchor=x.math('Add_VectorVector',A=x.get('target'+str(index)),B=x.math('MakeVector',X=0.0,Y=0.0,Z=240.0))
            x.set(prefix+'_anchor',anchor,obj=hud,cls=HUD_CLASS)
        g.when(anchor_valid,assign_anchor)
    for key in ['hovered_action','pressed_action','can_clear_pan','can_clear_pot']:
        put(key,g.get(key))
    for name in ['rice','pancake','soup','special']:
        put('can_cook_'+name,g.get('can_cook_'+name));put('reason_'+name,g.get('reason_'+name))
    for p in ['pan','pot']:
        put(p+'_busy',station(g,p));put(p+'_remaining',g.get(p+'_remaining'));put(p+'_portions',g.get(p+'_portions'))
    for key in ['summary_served','summary_base','summary_bonus','summary_clams_used','summary_special_served']:
        put(key,ss(g,key))
    put('total_revenue',ss(g,'total_revenue'))
    put('day',ss(g,'day'));put('elapsed',g.get('elapsed'))
    put('served_count',g.get('served_count'));put('guest_count',g.get('guest_count'))
    live_receipts=add(g,g.get('base_revenue'),g.get('bonus_revenue'))
    put('display_revenue',add(g,ss(g,'total_revenue'),g.math('SelectInt',A=live_receipts,B=0,bPickA=g.get('service'))))
    put('interaction_hint',g.get('interaction_hint'))
    put('header_text',g.join('종구의 식당  ·  ',g.integer_string(ss(g,'day')),'일차  ·  조개 ',g.integer_string(ss(g,'clams')),'개'))
    phase=g.math('SelectString',A=g.math('SelectString',A='마감 중 · 받은 주문을 마무리해 주세요',B=g.math('SelectString',A='손님 받기 중단',B='영업 중',bPickA=g.get('intake_paused')),bPickA=g.get('closing')),B=g.math('SelectString',A='해변 나들이 · 준비 시간은 자유롭게',B='준비 · 기본 재료는 항상 충분해요',bPickA=g.get('is_beach')),bPickA=g.get('service'))
    put('phase_text',g.math('SelectString',A='오늘의 결산',B=phase,bPickA=eq(g,g.get('screen_id'),5)))
    held=g.math('SelectString',A='빈손',B=g.join(g.math('SelectString',A='조개 특선 ',B='',bPickA=dish(g,'is_special')),dname(g,dish(g,'dish_id')),' 1그릇'),bPickA=eq(g,dish(g,'dish_id'),-1))
    put('held_text',held)
    holding=ne(g,dish(g,'dish_id'),-1)
    put('held_dish_id',g.math('SelectInt',A=dish(g,'dish_id'),B=-1,bPickA=holding))
    put('held_is_special',both(g,holding,dish(g,'is_special')))
    for i in range(2):
        active=order(g,i,'active')
        put('order'+str(i)+'_active',active)
        put('order'+str(i)+'_dish_id',g.math('SelectInt',A=order(g,i,'dish_id'),B=-1,bPickA=active))
        wait_seconds=g.math('FMax',A=0.0,B=g.math('Subtract_DoubleDouble',A=g.get('elapsed'),B=order(g,i,'placed_at')))
        put('order'+str(i)+'_wait',g.math('SelectFloat',A=wait_seconds,B=0.0,bPickA=active))
        elapsed=g.math('FFloor',A=g.math('FMax',A=0.0,B=g.math('Subtract_DoubleDouble',A=g.get('elapsed'),B=order(g,i,'placed_at'))))
        label=g.join(str(i+1)+'번 손님 · ',dname(g,order(g,i,'dish_id')),' · ',g.integer_string(elapsed),'초')
        put('order'+str(i)+'_text',g.math('SelectString',A=label,B=str(i+1)+'번 좌석 · 비어 있음',bPickA=order(g,i,'active')))
        a=g.get('customer'+str(i))
        g.when(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=a),lambda x,a=a,i=i:x.call(ACTOR+'SetActorHiddenInGame',self=a,bNewHidden=neg(x,order(x,i,'active'))))
    for p,label in [('pan','팬'),('pot','냄비')]:
        occupied=station(g,p)
        put(p+'_dish_id',g.math('SelectInt',A=g.get(p+'_dish'),B=-1,bPickA=occupied))
        put(p+'_is_special',both(g,occupied,g.get(p+'_special')))
        seconds=g.math('FCeil',A=g.get(p+'_remaining'))
        cooking=g.join(label+' · ',dname(g,g.get(p+'_dish')),' · ',g.integer_string(seconds),'초 남음')
        done=g.join(label+' · 완성 ',g.integer_string(g.get(p+'_portions')),'그릇')
        put(p+'_text',g.math('SelectString',A=cooking,B=g.math('SelectString',A=done,B=label+' · 비어 있음',bPickA=ready(g,p)),bPickA=g.math('Greater_DoubleDouble',A=g.get(p+'_remaining'),B=0.0)))
        fraction=g.math('FClamp',Value=g.math('Subtract_DoubleDouble',A=1.0,B=g.math('Divide_DoubleDouble',A=g.get(p+'_remaining'),B=g.math('FMax',A=0.001,B=g.get(p+'_duration')))),Min=0.0,Max=1.0)
        put(p+'_progress',g.math('SelectFloat',A=fraction,B=0.0,bPickA=station(g,p)))
    put('help_text',g.join('WASD 이동  ·  Esc 일시정지    ',g.get('interaction_hint')))
    put('summary_text',g.join('오늘 대접한 식사  ',g.integer_string(ss(g,'summary_served')),'그릇\n기본 매출  ',g.integer_string(ss(g,'summary_base')),'  +  서비스 보너스  ',g.integer_string(ss(g,'summary_bonus')),
        '\n조개 사용  ',g.integer_string(ss(g,'summary_clams_used')),'개  ·  특선 제공  ',g.integer_string(ss(g,'summary_special_served')),'그릇\n누적 매출  ',g.integer_string(ss(g,'total_revenue'))))
    for i in range(3):
        a=g.get('clam'+str(i))
        harvested=ne(g,g.math('And_IntInt',A=ss(g,'harvested_mask'),B=1<<i),0)
        g.when(g.pure('/Script/Engine.KismetSystemLibrary.IsValid',Object=a),lambda x,a=a,harvested=harvested:x.call(ACTOR+'SetActorHiddenInGame',self=a,bNewHidden=harvested))

