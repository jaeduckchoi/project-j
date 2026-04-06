---
적용: 항상
---

# 종구의 식당 씬 구성 및 설정 가이드

## 1. 지원 씬 구성

### Hub

- 메인 허브 씬이다.
- 월드 아트 레이어, 상호작용 지점, HUD/팝업 UI를 포함한 `16:9` 고정 카메라 기준을 사용한다.
- 허브 바닥은 반복 타일 스프라이트 구조를 사용하며, 바닥 타일 밀도는 기본적으로 `1 월드 유닛 = 32 px` 기준을 유지한다.
- 허브 카운터 비주얼은 `HubBar` 루트 아래 `HubBarLeftVisual`, `HubBarRightVisual` 두 파츠로 나누어 관리하고, 오른쪽 파츠는 별도 스프라이트와 `spriteBorder`를 가질 수 있다.
- 메뉴 선택기, 영업대, 창고, 업그레이드 작업대, 지역 포탈이 포함된다.

### Beach

- 입문용 탐험 씬이다.
- 기본 채집 루프와 허브 복귀 흐름 검증에 사용한다.

### DeepForest

- 중반 탐험 씬이다.
- 버섯/약초 채집과 감속 지대 검증에 사용한다.

### AbandonedMine

- 후반 탐험 씬이다.
- 랜턴 조건, `Glow Moss`, 어둠 흐름 검증에 사용한다.

### WindHill

- 최종 탐험 씬이다.
- 돌풍 지대와 평판 기반 지름길 검증에 사용한다.

## 2. 공용 런타임 설정

### GameManager

핵심 연결 참조는 다음과 같다.

- `InventoryManager`
- `StorageManager`
- `EconomyManager`
- `ToolManager`
- `DayCycleManager`
- `UpgradeManager`

일부 런타임 보강이 누락된 참조를 보완할 수는 있지만, 직접 씬에 연결해 두는 편이 유지보수와 검증에 더 유리하다.

### UIManager

주요 연결 필드는 다음과 같다.

- `interactionPromptText`
- `inventoryText`
- `storageText`
- `upgradeText`
- `goldText`
- `selectedRecipeText`
- `dayPhaseText`
- `bodyFontAsset`
- `headingFontAsset`
- `skipExplorationButton`
- `skipServiceButton`
- `nextDayButton`
- `recipePanelButton`
- `upgradePanelButton`
- `materialPanelButton`
- `popupCloseButton`

현재 UI 기준 동작은 다음과 같다.

- 허브에서 하단 버튼은 `Cooking Menu`, `Upgrade`, `Materials` 팝업을 연다.
- 허브 팝업이 열리면 게임 진행이 멈춘다. `Esc` 또는 `PopupCloseButton`으로 닫으면 원래 시간 흐름을 복구한다.
- 창고는 자동 근접 UI가 아니라 `StorageStation`에서 `E` 상호작용으로 열린다.
- 주요 패널과 버튼 이미지는 `PrototypeUISkinCatalog`가 정의한 생성 UI 리소스 경로를 공유한다.

### PrototypeUIDesignController

- 경로: `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
- `Apply Preview`, `Canvas Grouping`, `Open Scene Builder Preview`, `Refresh SVG Cache`, `Reset Canvas UI Layouts` 같은 에디터 보조 기능을 지원한다.
- Canvas 오브젝트를 `HUDRoot`, `PopupRoot` 아래에 그룹화하며, 빌더와 런타임도 같은 구조를 따른다.

## 3. 월드 계층 기준

지원 씬은 다음 최상위 구조에 맞춰 정렬한다.

```text
Scene
├─ SceneWorldRoot
├─ SceneGameplayRoot
├─ SceneSystemRoot
└─ Canvas
```

- `SceneWorldRoot`
  월드 비주얼과 경계 오브젝트를 그룹화한다.
- `SceneGameplayRoot`
  플레이어, 스폰 지점, 포탈, 상호작용 오브젝트, 채집 오브젝트, 위험 지대를 그룹화한다.
- `SceneSystemRoot`
  `GameManager`, `RestaurantManager`, `Main Camera`, `EventSystem` 같은 시스템 오브젝트를 그룹화한다.
- `Canvas`
  UI 루트이며 내부는 `HUDRoot`, `PopupRoot` 기준으로 구성한다.

## 4. 허브 체크포인트

- `RecipeSelector`
- `ServiceCounter`
- `StorageStation`
- `UpgradeStation`
- `GoToBeach`
- `GoToDeepForest`
- `GoToAbandonedMine`
- `GoToWindHill`
- `HubArtRoot`
- `HubBar`
- `HubBarLeftVisual`
- `HubBarRightVisual`
- `HubTodayMenuBoard`
- `CameraBounds`

기대하는 허브 흐름은 다음과 같다.

1. 메뉴 선택
2. 창고 품목 선택
3. 맡기기 또는 꺼내기
4. 업그레이드 확인
5. 지역 이동
6. 영업 진행

## 5. 탐험 씬 체크포인트

- `GatherableResource.resourceData`
- `GatherableResource.requiredToolType`
- 복귀용 `ScenePortal`
- `MovementModifierZone`
- `DarknessZone`
- `WindGustZone`

핵심은 상호작용 가능 여부, 차단 이유 안내, 복귀 포탈 동작, 위험 지대 체감이 제대로 동작하는지 확인하는 것이다.

## 6. 권장 플레이테스트 순서

1. `Hub`에서 텍스트 가독성, 창고 `E` 팝업 동작, 메뉴 선택 UI를 확인한다.
2. `Beach`에서 기본 자원을 채집하고 허브로 돌아온다.
3. `DeepForest`에서 버섯/약초 채집과 감속 지대를 확인한다.
4. 허브 작업대에서 인벤토리 확장이나 랜턴 해금 비용을 확인한다.
5. `AbandonedMine`에서 `Glow Moss`와 어둠 이동을 확인한다.
6. 평판을 올리고 `WindHillShortcut`을 확인한다.
7. 허브에서 메뉴 선택, 영업, 정산, 다음 날 흐름을 확인한다.

## 7. 관련 에디터 메뉴

- `Prototype Build and Audit`
- `Rebuild Generated Assets and Scenes`
- `Run Generated Scene Audit Only`
- `Organize Active Scene Hierarchy`
- `Light Automation Audit`

## 8. 현재 위험 요소

- 이 환경에서는 Unity 실행과 C# 컴파일을 직접 검증하지 못했다.
- 최종 밸런스 수치는 실제 플레이테스트 이후 추가 조정이 필요할 수 있다.
- `PrototypeSceneRuntimeAugmenter` 안전장치는 아직 남아 있으며, 씬 직렬화가 더 안정되면 축소할 수 있다.
