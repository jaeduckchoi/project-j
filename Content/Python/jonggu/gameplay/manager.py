"""Compose the restaurant manager Blueprint from its behavior builders."""
import unreal

from jonggu.blueprints.graph import (
    G, add_vars, compile_bp, declare_function, ensure_bp, save_bp,
)
from jonggu.gameplay.constants import (
    POSITIONS,
)
from jonggu.gameplay.cooking import (
    empty_station, pickup, start_cook,
)
from jonggu.gameplay.data import (
    DISH_CLASS, ORDER_CLASS, RECIPE_CLASS,
)
from jonggu.gameplay.graph_helpers import (
    own,
)
from jonggu.gameplay.input import (
    action, click, find_interaction, interact, pause_toggle, set_screen, update_pointer, use_interaction,
)
from jonggu.gameplay.presentation import (
    refresh_hud, refresh_ui_state,
)
from jonggu.gameplay.service import (
    advance, assign_order, finish, open_service, serve, setup, travel, try_arrival, try_finish,
)
from jonggu.gameplay.session import (
    SESSION_CLASS,
)


def build_manager(typed):
    bp=ensure_bp('BP_RestaurantManager',unreal.Actor)
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
