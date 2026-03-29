# 종구의 식당 씬 및 세팅 가이드

## 1. 씬 구성

### Hub

- 허브 메인 씬이다.
- 메뉴 선택대, 영업대, 창고, 작업대, 지역 포탈이 들어간다.
- 창고 선택 패드와 폐광산 포탈이 직렬화되어 있다.

### Beach

- 입문 탐험 지역이다.
- 기본 채집 루프와 허브 복귀 흐름을 확인한다.

### DeepForest

- 중반 탐험 지역이다.
- 버섯 / 약초 수집과 감속 지형이 들어간다.

### AbandonedMine

- 후반 탐험 지역이다.
- 랜턴 요구 조건, `GlowMoss`, 어둠 구간, 잔해 감속이 들어간다.

### WindHill

- 최종 탐험 지역이다.
- 강풍 존과 평판 기반 숏컷을 확인한다.

## 2. 공통 런타임 구성

### GameManager

아래 참조가 핵심이다.

- `InventoryManager`
- `StorageManager`
- `EconomyManager`
- `ToolManager`
- `DayCycleManager`
- `UpgradeManager`

참조가 비어 있어도 런타임 보정이 일부 들어가 있지만, 씬에 직접 연결되어 있으면 확인과 유지보수가 쉽다.

### UIManager

주요 연결 대상은 아래와 같다.

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

최근 UI 구조는 아래 기준으로 동작한다.

- 좌측 상단에는 `코인 / 평판`만 표시한다.
- 허브에서는 하단 중앙 버튼으로 `요리 메뉴`, `업그레이드`, `재료` 팝업을 연다.
- 허브 팝업이 열리면 게임 진행이 일시정지되고 `Esc` 로 닫으면 원래 시간 흐름으로 복구된다.
- 허브 팝업 우측 상단에는 `PopupCloseButton` 이 따로 있고 같은 닫기 동작을 수행한다.
- 창고는 근접 자동 노출이 아니라 `StorageStation` 앞에서 `E` 상호작용 시 팝업 형태로 열린다.
- 허브 씬의 창고 월드 오브젝트는 `StorageArea` 아래에 `StorageWall`, `StorageSign`, `StorageStation` 순으로 묶어 관리한다.
- 탐험 지역에서는 우측 상단 카드로 `재료 / 가방`을 확인한다.
- 주요 패널 / 버튼 이미지는 `Assets/Resources/Generated/UI/Vector` 아래 SVG 리소스를 공용으로 사용한다.

### PrototypeUIDesignController

- 경로: `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`

- `uiManager`
- `showEditorPreview`
- `editorPreviewPanel`

편집 모드 프리뷰와 SVG 경로 확인은 이 컴포넌트에서 따로 다룬다.

- 일반 HUD 레이아웃/스킨은 `Assets/Scripts/UI/Layout/PrototypeUILayout.UI.cs`, `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.UI.cs`에서 관리한다.
- 팝업 레이아웃/스킨은 `Assets/Scripts/UI/Layout/PrototypeUILayout.Popup.cs`, `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.Popup.cs`에서 관리한다.
- `Canvas` 내부 오브젝트는 `HUDRoot`, `PopupRoot` 기준으로 묶고, 런타임과 빌더도 같은 구조를 기준으로 찾는다.

- `Apply Preview` 버튼으로 Play 모드 없이 팝업 스킨 배치를 바로 확인할 수 있다.
- `Canvas Grouping` 버튼으로 기존 씬의 평면 Canvas 자식도 같은 그룹 구조로 맞출 수 있다.
- `Refresh SVG Cache` 버튼으로 `PrototypeUISkin` 임시 스프라이트를 다시 렌더링할 수 있다.
- `Sync Canvas UI Layouts` 버튼으로 현재 씬 Canvas 아래 UI 배치와 `Image.sprite/type/color/preserveAspect` 값을 공용 자산에 저장하면 빌더와 런타임이 같은 값을 사용한다.
- 공용 자산 경로는 `Assets/Resources/Generated/UI/uiLayoutOverrides.asset` 이고, 첫 동기화 시 자동 생성된다.

## 3. 허브 체크 포인트

- `RestaurantManager.availableRecipes`
- `StorageStation.stationAction`
- `UpgradeStation`
- `ScenePortal.targetSceneName`
- `ScenePortal.targetSpawnPointId`

허브에서는 아래 흐름이 끊기지 않는지 보면 된다.

1. 메뉴 선택
2. 창고 품목 선택
3. 맡기기 / 꺼내기
4. 업그레이드 확인
5. 지역 이동
6. 장사 실행

## 4. 탐험 지역 체크 포인트

- `GatherableResource.resourceData`
- `GatherableResource.requiredToolType`
- 귀환용 `ScenePortal`
- `MovementModifierZone`
- `DarknessZone`
- `WindGustZone`

핵심은 `상호작용 가능 여부`, `막힌 이유 안내`, `귀환 포탈 동작`, `위험 구간 체감` 이다.

## 5. 플레이 테스트 권장 순서

1. `Hub` 에서 텍스트 가독성, 창고 `E` 팝업, 메뉴 선택 UI 확인
2. `Beach` 에서 기본 채집 후 허브 복귀 확인
3. `DeepForest` 에서 버섯 / 약초 수집과 감속 구간 확인
4. 허브 작업대에서 인벤토리 확장 또는 랜턴 해금 비용 확인
5. `AbandonedMine` 진입 후 `GlowMoss`, 어둠, 잔해 구간 확인
6. 평판을 올린 뒤 `WindHillShortcut` 사용 가능 여부 확인
7. 허브에서 메뉴 선택, 장사, 정산, 다음 날 전환 확인

## 6. 현재 리스크

- Unity 실행과 C# 컴파일 검증은 이 환경에서 직접 하지 못했다.
- 밸런스 수치는 실제 플레이 후 재조정이 필요할 수 있다.
- `PrototypeSceneRuntimeAugmenter` 안전망은 남겨둔 상태라, 씬 직렬화가 안정되면 의존을 더 줄여도 된다.

