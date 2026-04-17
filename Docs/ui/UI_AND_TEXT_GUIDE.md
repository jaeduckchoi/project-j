# UI 및 텍스트 가이드

## 현재 UI 기준

- Canvas 최상위 공용 루트는 `HUDRoot`, `PopupRoot`
- UI 코드는 엔트리/루트 파일은 `Assets/Scripts/UI`, family별 세부 구현은 `Assets/Scripts/UI`, `Assets/Scripts/UI/Layout`, `Assets/Scripts/UI/Style`, `Assets/Scripts/UI/Content/Catalog` 아래에 둔다.
- 런타임 UI 동작의 중심은 `Assets/Scripts/UI/UIManager.cs`(엔트리)와 `Assets/Scripts/UI/UIManager.Lifecycle.cs`, `UIManager.EditorPreview.cs`, `UIManager.Bindings.cs`, `UIManager.Input.cs`, `UIManager.Canvas.cs`, `UIManager.Chrome.cs`, `UIManager.HubPopup.cs`, `UIManager.Refresh.cs`
- 에디터 UI 프리뷰/설정은 `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`, `Assets/Scripts/UI/UIManager.EditorPreview.cs`, `Assets/Editor/UI/*`에서 관리하며, 빈 Canvas도 에디터에서 관리 UI를 생성해 조정할 수 있어야 한다.
- 레이아웃 catalog 정본은 `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`이며, 관리 대상 Canvas 이름 목록과 명시적 레이아웃 binding 적용 경로를 유지한다.
- 기본 레이아웃 정본은 `Assets/Scripts/UI/Layout/PrototypeUILayout*.cs` 코드 값이고, 에디터에서 연결한 씬 오브젝트 값은 `Assets/Resources/Generated/ui-layout-bindings.asset`에 저장한다.
- 레이아웃 partial: `Assets/Scripts/UI/Layout/PrototypeUILayout.cs`(엔트리), `PrototypeUILayout.UI.cs`, `PrototypeUILayout.Popup.cs`
- 관리 대상 오브젝트 이름 catalog: `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs` (`GetManagedCanvasObjectNames`, `EnumerateHudCanvasObjectNames`, `EnumeratePopupCanvasObjectNames`)
- 팝업 타이틀/캡션 공용 상수: `Assets/Scripts/UI/Layout/PrototypeUIObjectNames.cs`
- 스타일 catalog: `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.cs`(엔트리), `PrototypeUISkinCatalog.UI.cs`, `PrototypeUISkinCatalog.Popup.cs`, `Assets/Scripts/UI/Style/PrototypeUISkin.cs`, `PrototypeUITheme.cs`
- 팝업 콘텐츠 catalog: `Assets/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`
- 프로젝트 원본 폰트 소스: 본문 `Assets/TextMesh Pro/Fonts/Galmuri11.ttf`, 제목 `Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf`
- 프로젝트 TMP Font Asset: 본문 `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11 SDF.asset`, 제목 `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11-Bold SDF.asset`

## 관리 대상 이름

- HUD: `GuideText`, `RestaurantResultText`, `GuideHelpButton`
- Popup: `PopupTitle`, `PopupLeftCaption`, `PopupRightCaption`, `InventoryText`, `StorageText`, `SelectedRecipeText`, `UpgradeText`

탐험 씬의 좌측 상단 상태 카드는 `TopLeftPanel`과 `GoldText`를 유지합니다.
Hub의 우측 상단 코인 자원 패널은 `ResourcePanel`과 `ResourceAmountText`를 사용합니다.

이 이름들은 `PrototypeUISceneLayoutCatalog` 기준으로 함께 유지합니다.

## generated UI 경로

- 루트: `Assets/Resources/Generated/Sprites/UI`
- 하위: `Buttons`, `MessageBoxes`, `Panels`
- 경로 정본: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`

## 편집 규칙

- 씬에 직접 저장된 popup 텍스트, 이미지, 폰트, 배치 값은 명시적 요청 없이는 덮어쓰지 않습니다.
- Canvas 그룹 재정리는 `UIManager`와 `PrototypeUIDesignController` 기반 편집 도구를 사용합니다.
- 에디터에서 UI가 보이지 않는 변경은 완료 상태로 보지 않습니다. `PrototypeUIDesignController`의 `Apply Preview`로 관리 UI를 수동 생성·갱신하고, UI 레이아웃 편집기에서는 선택한 관리 ID에 메모와 스프라이트 override를 저장합니다. 상세 오브젝트는 현재 씬에서 같은 이름의 UI 오브젝트를 찾아 Image 컴포넌트가 있을 때만 스프라이트를 연결합니다. 편집기 좌측 트리는 HUD/큰 팝업 구조와 함께 창고/요리/업그레이드/재료/프론트 카운터/냉장고 같은 팝업 유형별 그룹으로 정리합니다. 큰 팝업 연결은 `StorageStation`, `RefrigeratorStation`, `FrontCounterStation` 같은 월드 상호작용 컴포넌트 기준으로 확인합니다.
- `Canvas 그룹 정리`와 에디터 프리뷰 계층 생성은 `PopupSharedLeftGroup`, `PopupSharedRightGroup`, `PopupStorageRightGroup`, `PopupUpgradeRightGroup`, `PopupRefrigeratorGroup` 같은 에디터용 그룹을 만들어 팝업 관련 오브젝트를 유형별로 묶습니다. 이 그룹은 에디터 정리용이며, 런타임 오브젝트 이름 정본은 그대로 유지합니다.
- 현재 코드 밖의 별도 자동 동기화/감사 흐름을 전제로 작업하지 않습니다.

## 함께 확인할 코드

- `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/UIManager.Lifecycle.cs`, `UIManager.EditorPreview.cs`, `UIManager.Bindings.cs`, `UIManager.Input.cs`, `UIManager.Canvas.cs`, `UIManager.Chrome.cs`, `UIManager.HubPopup.cs`, `UIManager.Refresh.cs`
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`, `PrototypeUILayoutBindingSettings.cs`, `Assets/Resources/Generated/ui-layout-bindings.asset`
- `Assets/Scripts/UI/PopupPauseStateUtility.cs`
- `Assets/Scripts/UI/Layout/PrototypeUILayout.cs` 및 `.UI.cs`, `.Popup.cs`, `PrototypeUIObjectNames.cs`
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.cs` 및 `.UI.cs`, `.Popup.cs`, `Assets/Scripts/UI/Style/PrototypeUISkin.cs`, `PrototypeUITheme.cs`
- `Assets/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`
- `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
- `Assets/Editor/UI/UIManagerEditor.cs`
- `Assets/Editor/UI/PrototypeUIDesignControllerEditor.cs`
- `Assets/Editor/UI/UIManagerEditor.cs` (UI 레이아웃 편집기 포함)
