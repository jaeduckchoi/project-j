# UI 그룹 규칙

## 기본 구조

- `Canvas`
- `HUDRoot`
- `PopupRoot`

## HUDRoot

- `HUDStatusGroup`
- `HUDActionGroup`
- `HUDBottomGroup`
- `HUDOverlayGroup`
- 필요 시 `HUDPanelButtonGroup`

`HUDStatusGroup` 아래 탐험 씬은 `TopLeftPanel`과 `GoldText`를 좌측 상단 상태 카드로 유지합니다.
Hub는 `ResourcePanel`과 `ResourceAmountText`를 우측 상단 코인 자원 패널로 사용합니다.

## PopupRoot

- `PopupShellGroup`
- `PopupFrame`
- `PopupFrameHeader`

## 편집 규칙

- `Apply Preview`는 관리 UI를 수동으로 생성·프리뷰하는 흐름이며, 기존 씬 `RectTransform` 배치는 덮어쓰지 않습니다.
- 실제 그룹 정리는 `Canvas Grouping` 기준으로 처리하되, 에디터에서 조정한 위치 값은 유지합니다.
- 빈 Canvas에서는 `PrototypeUIDesignController` 수동 프리뷰가 먼저 관리 UI를 생성할 수 있어야 하며, 런타임에서만 UI가 생기는 상태로 두지 않습니다.
- `HUDRoot` 또는 `PopupRoot` 하위 이름을 바꾸면 `Assets/Code/Scripts/UI/UIManager.cs`, `Assets/Code/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`, 관련 UI 문서를 함께 갱신합니다. 팝업 타이틀/캡션 공용 이름이면 `PrototypeUIObjectNames.cs`도 함께 맞춥니다.
- HUD 그룹 구조를 바꾸면 `Assets/Code/Scripts/UI/Layout/PrototypeUILayout.UI.cs`, `Assets/Code/Scripts/UI/Style/PrototypeUISkinCatalog.UI.cs`를 함께 확인합니다.
- popup 프레임 구조를 바꾸면 `Assets/Code/Scripts/UI/Layout/PrototypeUILayout.Popup.cs`, `Assets/Code/Scripts/UI/Style/PrototypeUISkinCatalog.Popup.cs`를 함께 확인합니다.
- 팝업 콘텐츠(타이틀, 캡션, 리스트 항목)는 `Assets/Code/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`를 정본으로 사용합니다.
- 현재 코드 밖의 자동 동기화/감사 흐름은 갱신 대상으로 보지 않습니다.
