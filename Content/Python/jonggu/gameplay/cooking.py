"""Cooking station, prepared-plate pickup and station disposal graphs."""

from jonggu.blueprints.graph import (
    G,
)
from jonggu.gameplay.data import (
    RECIPE_CLASS,
)
from jonggu.gameplay.graph_helpers import (
    add, both, dish, dish_set, eq, log, neg, notice, own, ready, selected, setss, ss, station, sub,
)


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
