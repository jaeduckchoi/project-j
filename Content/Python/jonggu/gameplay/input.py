"""Menu, keyboard interaction and shared HUD pointer/action routing."""

from jonggu.blueprints.graph import (
    G,
)
from jonggu.gameplay.constants import (
    ACTOR, GS, LABELS, POSITIONS,
)
from jonggu.gameplay.graph_helpers import (
    both, dish, dish_set, eq, log, ne, neg, notice, own, ready, session_call, setss, ss,
)
from jonggu.ui.layout import (
    BUTTONS, button_rect_expr,
)


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
