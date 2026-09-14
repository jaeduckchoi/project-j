"""Day lifecycle, arrivals, orders, service accounting and map travel."""

from jonggu.blueprints.graph import (
    G,
)
from jonggu.gameplay.constants import (
    ACTOR, GS,
)
from jonggu.gameplay.data import (
    DISH_CLASS, ORDER_CLASS,
)
from jonggu.gameplay.graph_helpers import (
    add, both, checked_save, dish, dish_set, either, eq, log, ne, neg, notice, order, order_set, own, session_call, setss, ss,
)
from jonggu.gameplay.session import (
    SESSION_CLASS,
)


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
