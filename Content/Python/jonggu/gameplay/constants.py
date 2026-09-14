"""Stable native/class identifiers and authored interaction layout for graph builders."""
import json

from jonggu.assets import generated_class_path
from jonggu.paths import LAYOUT_FILE

MANAGER_CLASS = generated_class_path('BP_RestaurantManager')
HUD_CLASS = generated_class_path('BP_RestaurantHUD')
GS = '/Script/Engine.GameplayStatics.'
ACTOR = '/Script/Engine.Actor.'
PC = '/Script/Engine.PlayerController.'
LAYOUT = json.loads(LAYOUT_FILE.read_text(encoding='utf-8'))
LABELS=['메뉴판 · 오늘의 메뉴 정하기','팬 · 조리 / 완성품 받기','냄비 · 조리 / 완성품 받기','반납대 · 든 접시 비우기','해변으로 나가기','1번 손님 · 서빙','2번 손님 · 서빙','식당으로 돌아가기','조개 줍기','조개 줍기','조개 줍기']
POSITIONS=[i['position_cm'] for i in LAYOUT['maps']['Hub']['interactions']]+[i['position_cm'] for i in LAYOUT['maps']['Beach']['interactions']]
