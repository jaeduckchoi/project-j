"""Read-only gameplay presentation fields copied to the Canvas HUD."""

from jonggu.blueprints.graph import (
    G,
)
from jonggu.gameplay.constants import (
    ACTOR, GS, HUD_CLASS,
)
from jonggu.gameplay.data import (
    RECIPE_CLASS,
)
from jonggu.gameplay.graph_helpers import (
    add, both, dish, dname, eq, ne, neg, order, own, ready, selected, ss, station,
)


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
