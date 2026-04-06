---
적용: 항상
---

# 종구의 식당 기능 레퍼런스

## 1. 게임 개요

`Jonggu Restaurant`는 2D 탑다운 탐험과 운영을 결합한 프로토타입이다.
플레이어가 탐험에서 재료를 모으고, 허브로 돌아와 간단한 식당 영업을 진행하고, 그 결과를 다음 성장으로 전환하는 하루 루프가 핵심 감각이다.

현재 구현 기준 순서는 다음과 같다.

1. 허브에서 상태 확인
2. 오전 탐험 지역 진입
3. 자원 수집과 인벤토리 관리
4. 허브 복귀
5. 메뉴 선택
6. 영업 진행
7. 골드와 평판 정산
8. 업그레이드 또는 다음 날 진행

## 2. 플레이 가능 씬과 지역 특성

### Hub

- 허브 이름은 `Jonggu Restaurant`이다.
- 메뉴 선택기, 영업대, 창고, 업그레이드 작업대, 지역 포탈이 배치된다.
- 월드 공간의 `Today Menu` 보드는 대표 메뉴 3개를 보여 주며, 향후 일일 선택 로직의 안정적인 기준점 역할을 한다.
- 허브 팝업은 `Cooking Menu`, `Upgrade`, `Materials`, `Storage`에 대해 공용 프레임을 사용한다.

### Beach

- 입문용 탐험 지역이다.
- 주요 자원은 `Fish`, `Shell`, `Seaweed`다.
- 기본 이동과 채집 루프를 익히는 데 초점을 둔다.

### DeepForest

- 중반 탐험 지역이다.
- 주요 자원은 `Mushroom`, `Herb`다.
- `ForestSwampZone` 감속과 가이드 존 요소가 포함된다.

### AbandonedMine

- 후반 탐험 지역이다.
- 주요 자원은 `Glow Moss`다.
- 접근과 이동에 사실상 `Lantern`이 필요하다.
- 핵심 위험은 `MineDarkness`와 좁은 동선에서 나온다.
- 런타임 안전장치가 `MineLooseRubble` 같은 보강 오브젝트를 추가할 수 있다.

### WindHill

- 최종 탐험 지역이다.
- 주요 자원은 `Wind Herb`다.
- `WindLaneZone`의 돌풍 타이밍과 평판 6 지름길을 중심으로 설계되어 있다.

## 3. 핵심 시스템

### 플레이어와 상호작용

- `PlayerController`
  이동 입력과 상호작용 입력을 처리한다.
- `InteractionDetector`
  가장 가까운 상호작용 대상을 고르고 프롬프트를 갱신한다.
- `IInteractable`
  월드 오브젝트 상호작용 계약을 표준화한다.

### 자원과 채집

- `ResourceData`
  자원 이름, 설명, 아이콘, 희귀도, 판매 가치를 저장한다.
- `GatherableResource`
  필요 도구를 확인하고 성공 시 인벤토리에 자원을 추가한다.
- 조건이 막혀 있어도 채집 상호작용은 막힌 이유를 설명해야 한다.

### 도구와 접근 조건

- 시작 시 해금된 도구는 `Rake`, `Fishing Rod`, `Sickle`이다.
- 추가 해금 도구는 `Lantern`이다.
- `ScenePortal`은 오전 전용 이동, 필요 도구, 필요 평판을 함께 확인한다.
- 오후 단계 이후에도 허브 복귀는 허용되어 하루 루프가 자연스럽게 마무리되도록 한다.

### 인벤토리와 창고

- 인벤토리는 재료 전용 슬롯 구조다.
- 슬롯 확장 단계는 `8 -> 12 -> 16`이다.
- 창고는 허브 전용 보관 시스템이다.
- 선택 품목 맡기기/꺼내기와 전체 맡기기/꺼내기를 지원한다.

### 식당 영업과 메뉴

- `RecipeData`
  메뉴 이름, 설명, 판매가, 평판 변화, 필요 재료를 정의한다.
- `RestaurantManager`
  현재 선택 메뉴, 조리 가능 수량, 재료 소모, 결과 정산을 처리한다.
- 기본 일일 영업 상한은 `serviceCapacity = 3`이다.

### 경제와 업그레이드

- `EconomyManager`
  골드와 평판을 관리한다.
- `UpgradeManager`
  인벤토리 확장과 도구 해금을 처리한다.
- 기본 업그레이드 비용은 다음과 같다.
  - 12칸 확장: `Gold 30 + Shell x3`
  - 16칸 확장: `Gold 65 + Herb x4`
  - 랜턴 해금: `Gold 45 + Mushroom x2`

### 하루 흐름

- `DayCycleManager`
  `Morning Explore -> Afternoon Service -> Settlement`를 관리한다.
- 허브 출발과 복귀, 탐험 건너뛰기, 영업 건너뛰기, 다음 날 진행은 모두 같은 매니저 기준을 따른다.
- 일회성 씬 진입 가이드와 임시 안내 문구도 함께 처리한다.

### UI와 텍스트

- 허브 HUD와 팝업의 중심은 `UIManager`다.
- 지원 씬은 `HUDRoot`, `PopupRoot`를 루트로 하는 공용 Canvas 구조를 공유한다.
- 허브 팝업이 열려 있으면 `PopupPauseStateUtility`를 통해 시간이 멈춘다.

### 생성 시스템과 런타임 안전장치

- `GeneratedGameDataLocator`
  생성 데이터 참조가 비어 있을 때 기본 자원과 레시피를 다시 찾는다.
- `GeneratedGameDataManifest`
  빌드 환경에서도 생성 자산 참조를 유지한다.
- `PrototypeSceneRuntimeAugmenter`
  필요 시 허브 패드, 포탈, 지름길, 위험 지대를 런타임에 보강한다.

## 4. 현재 데이터 기준

### 자원

- `Fish`
- `Shell`
- `Seaweed`
- `Mushroom`
- `Herb`
- `Glow Moss`
- `Wind Herb`

### 레시피

- `Fish Platter`
- `Seafood Soup`
- `Herb Fish Soup`
- `Forest Basket`
- `Glow Moss Stew`
- `Wind Herb Salad`

## 5. 에디터 지원 도구

- `Prototype Build and Audit`
  생성 자산과 Build Settings를 다시 맞추고, 누락된 지원 씬을 복구하며 구조 감사를 수행한다.
- `GameplayAutomationAudit`
  내부 경량 감사 코드로 day-loop 흐름, 팝업 일시정지, 포탈 잠금, 누락 씬 안내 회귀를 점검한다.

## 6. 권장 참조 문서

아래 세부 기준 문서는 현재 UTF-8 한국어 본문을 정본으로 유지한다.
후속 요약이나 검토 문서에서는 영어 제목이 아니라 파일 경로와 코드 식별자를 기준으로 참조를 유지한다.

1. `project/GAME_DOCS_INDEX.md`
2. `gameplay/GAMEPLAY_CORE_LOOP.md`
3. `gameplay/GAMEPLAY_EXPLORATION.md`
4. `gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md`
5. `ui/UI_AND_TEXT_GUIDE.md`
6. `scene/GAME_SCENE_AND_SETUP.md`
7. `build/GAME_BUILD_GUIDE.md`

## 7. 검증 메모

- 이 문서는 현재 코드와 생성 데이터 기준에 맞춰 갱신되었다.
- Unity 플레이 모드와 C# 컴파일은 이 작업에서 직접 검증하지 못했으므로, 이후 런타임 검증이 필요하다.
