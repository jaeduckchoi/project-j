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

`HUDStatusGroup` 아래 `TopLeftPanel`과 `GoldText`는 공용 관리 이름을 유지하되, 탐험 씬에서는 좌측 상단 상태 카드, Hub에서는 우측 상단 코인 배지로 재배치될 수 있습니다.

## PopupRoot

- `PopupShellGroup`
- `PopupFrame`
- `PopupFrameHeader`

## 편집 규칙

- `Apply Preview`는 프리뷰 반영이고, 실제 그룹 정리는 `Canvas Grouping` 기준으로 처리합니다.
- `HUDRoot` 또는 `PopupRoot` 하위 이름을 바꾸면 `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/Layout/Definitions/PrototypeUIObjectNames.cs`, 관련 UI 문서를 함께 갱신합니다.
- HUD 그룹 구조를 바꾸면 `Assets/Scripts/UI/Layout/Definitions/PrototypeUILayout.UI.cs`, `Assets/Scripts/UI/Style/Catalog/PrototypeUISkinCatalog.UI.cs`를 함께 확인합니다.
- popup 프레임 구조를 바꾸면 `Assets/Scripts/UI/Layout/Definitions/PrototypeUILayout.Popup.cs`, `Assets/Scripts/UI/Style/Catalog/PrototypeUISkinCatalog.Popup.cs`를 함께 확인합니다.
- 팝업 콘텐츠(타이틀, 캡션, 리스트 항목)는 `Assets/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`를 정본으로 사용합니다.
- 현재 코드 밖의 자동 동기화/감사 흐름은 갱신 대상으로 보지 않습니다.
