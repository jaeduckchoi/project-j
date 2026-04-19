# UI 및 텍스트 가이드

## 역할

이 문서는 관리 대상 UI 이름, 레이아웃 바인딩, 팝업 연결, 텍스트와 폰트 처리 기준을 다루는 `UI` 도메인의 상위 정본이다.

## 이 문서를 읽는 시점

- HUD, 팝업, 텍스트, 폰트, binding 자산을 수정할 때
- 에디터 프리뷰와 런타임 UI가 같은 기준을 따라야 할 때
- 함께 읽을 문서: [UI_GROUPING_RULES.md](UI_GROUPING_RULES.md), [SOURCE_OF_TRUTH.md](../Project/SOURCE_OF_TRUTH.md)

## 정본 범위

- Canvas 최상위 공용 루트는 `HUDRoot`, `PopupRoot`다.
- 관리 대상 UI 이름 catalog는 `Assets/Code/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`가 소유한다.
- 기본 레이아웃 정본은 `Assets/Code/Scripts/UI/Layout/PrototypeUILayout*.cs` 코드 값이다.
- 에디터에서 연결한 씬 오브젝트 값은 `Assets/Resources/Generated/ui-layout-bindings.asset`에 저장한다.
- `ui-layout-bindings.asset`는 Rect/Image/TMP/Button 표시값뿐 아니라 parent/sibling/initialActive hierarchy contract도 함께 저장한다.
- hierarchy contract의 `initialActive`는 authored 초기 baseline이며, 플레이 중 active 상태의 정답은 runtime state가 가진다.
- runtime-only UI 예외는 `PrototypeUISceneLayoutCatalog`의 명시적 whitelist에 있는 이름만 허용한다.
- 큰 팝업이 어느 월드 오브젝트에서 열리는지는 `Assets/Resources/Generated/popup-interaction-bindings.asset`가 정본이다.
- 팝업 타이틀, 캡션, 콘텐츠는 `PrototypeUIObjectNames.cs`, `PrototypeUIPopupCatalog.cs`, `PrototypeUISkin*.cs`가 소유한다.
- 프로젝트 폰트 정본은 `Assets/TextMesh Pro/Fonts/Galmuri11.ttf`, `Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf`와 대응 TMP Font Asset이다.

관리 대상 이름의 예시는 아래를 기준으로 유지한다.

- HUD: `GuideText`, `RestaurantResultText`, `GuideHelpButton`
- Popup: `PopupTitle`, `PopupLeftCaption`, `PopupRightCaption`, `InventoryText`, `StorageText`, `SelectedRecipeText`, `UpgradeText`
- 탐험 씬 좌측 상단 상태 카드: `TopLeftPanel`, `GoldText`
- Hub 우측 상단 코인 자원 패널: `ResourcePanel`, `ResourceAmountText`

## 함께 수정할 항목

- UI 엔트리와 partial: `Assets/Code/Scripts/UI/UIManager.cs`, `UIManager.*.cs`
- 레이아웃과 binding: `PrototypeUILayout*.cs`, `PrototypeUILayoutBindingSettings.cs`, `Assets/Resources/Generated/ui-layout-bindings.asset`
- 팝업 월드 연결: `PopupInteractionBindingSettings.cs`, `Assets/Resources/Generated/popup-interaction-bindings.asset`
- 에디터 프리뷰: `PrototypeUIDesignController.cs`, `Assets/Code/Editor/UI/UIManagerEditor.cs`, `PrototypeUIDesignControllerEditor.cs`
- Canvas 그룹 구조: [UI_GROUPING_RULES.md](UI_GROUPING_RULES.md)

## 검증

- 에디터에서 UI가 실제로 보이는지 확인한다.
- 관리 대상 이름과 binding 자산이 같은 기준을 바라보는지 확인한다.
- hierarchy contract가 있는 UI 오브젝트는 런타임이 씬 authored parent와 sibling을 유지하는지 확인한다. `PopupCloseButton`의 기준 parent는 `PopupFrame`이다.
- `initialActive` baseline은 초기 hydrate 시점에만 적용되고, 이후 active 변화는 runtime state가 다시 소유하는지 확인한다.
- 팝업 연결이 필요한 station 컴포넌트와 함께 유지되는지 확인한다.
- 씬에 직접 저장된 popup 텍스트, 이미지, 폰트, 배치 값은 명시적 요청 없이는 덮어쓰지 않는다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 결과에 적는다.
