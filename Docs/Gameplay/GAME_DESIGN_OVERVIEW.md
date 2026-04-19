# 종구의 식당 게임 기획 개요

이 문서는 현재 프로토타입의 상위 게임 의도만 요약하는 개요 문서다. 세부 규칙과 수치는 하위 정본 문서에서만 관리한다.

## 게임 개요

종구의 식당은 작은 섬을 배경으로 탐험과 식당 운영을 오가는 2D 탑다운 운영형 게임이다.

- 플레이어는 번아웃을 겪은 황종구가 되어 외딴 섬에서 자신만의 식당을 연다.
- 탐험 루프에서 식재료를 모으고, 운영 루프에서 오늘의 메뉴를 정하고 손님 주문을 조리하고 서빙한다.
- 재료 수급과 영업 보상은 다시 탐험 효율과 성장 축을 강화하는 구조로 이어진다.

## 상위 루프 요약

1. 허브에서 보유 재료와 레시피를 확인한다.
2. 오늘의 메뉴를 정하고, 부족한 재료가 있으면 탐험으로 나간다.
3. 허브로 돌아와 영업을 열고 주문에 맞춰 조리와 서빙을 반복한다.
4. 영업 보상으로 성장 축을 열고 다음 탐험과 운영 준비로 돌아간다.

현재 허브 코어의 조리 계약은 `냉장고 확인 -> CookingUtensils에서 재료 선택 -> 조리 시작 -> 결과물 회수 -> PassCounter 적재 또는 회수 -> 서빙`을 기준으로 잡는다. 정확한 운영 규칙은 [GAMEPLAY_CORE_LOOP.md](GAMEPLAY_CORE_LOOP.md)를 따른다.

## 씬 축

- `scene:hub`: 식당 허브. 준비, 영업, 조리, 서빙이 만나는 중심 공간이다.
- `scene:room`: 도구 작업대와 성장 확인을 위한 보조 공간이다.
- `scene:beach`: 첫 탐험 허브. 도구 입문, 갯바위 채집, 후속 지역 암시를 담당한다.
- `scene:sea`: `Beach`에서 이어지는 독립 채집 루프 목표다.
- 후속 지역: `DeepForest`, `WindHill`, `Shortcut`, `AbandonedMine`은 중장기 탐험 확장 축이다.

공간 크기와 화면 기준은 [HUB_WHITEBOX.md](../Scene/HUB_WHITEBOX.md), [BEACH_WHITEBOX.md](../Scene/BEACH_WHITEBOX.md) 같은 `Scene` 문서가 정본이다.

## 세부 정본 문서

- 허브 코어 루프와 운영 수치: [GAMEPLAY_CORE_LOOP.md](GAMEPLAY_CORE_LOOP.md)
- 허브 공간 의미와 성장 축: [GAMEPLAY_RESTAURANT_AND_GROWTH.md](GAMEPLAY_RESTAURANT_AND_GROWTH.md)
- 탐험 목적과 지역 진행: [GAMEPLAY_EXPLORATION.md](GAMEPLAY_EXPLORATION.md)
- 씬 직렬화와 코드 결합 지점: [SOURCE_OF_TRUTH.md](../Project/SOURCE_OF_TRUTH.md)
