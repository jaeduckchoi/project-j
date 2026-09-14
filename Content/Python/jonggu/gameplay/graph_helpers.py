"""Small shared Blueprint graph expressions for restaurant state and events."""

from jonggu.gameplay.constants import (
    MANAGER_CLASS,
)
from jonggu.gameplay.data import (
    DISH_CLASS, ORDER_CLASS,
)
from jonggu.gameplay.session import (
    SESSION_CLASS,
)


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


def checked_save(g):
    saved=session_call(g,'SaveCheckpoint').find_output_pin('success')
    g.when(neg(g,saved),lambda x:notice(x,'저장하지 못했습니다. 디스크 상태를 확인해 주세요.'))
    return saved
