# UI 그룹 구조 규칙

## 목적

이 문서는 `Canvas` 하위 구조의 정본 이름과 부모 규칙만 정의합니다.
실제 정본 관계는 `project/SOURCE_OF_TRUTH.md`, 작업 절차는 `project/AGENT_WORKFLOW.md`를 따릅니다.

## 최상위 루트

`Canvas` 바로 아래에는 관리 대상 공용 루트 `HUDRoot`, `PopupRoot`만 둡니다.

```text
Canvas
├─ HUDRoot
└─ PopupRoot
```

## `HUDRoot` 기준 구조

- `HUDStatusGroup`
- `HUDActionGroup`
- `HUDBottomGroup`
- `HUDPanelButtonGroup`
- `InteractionPromptBackdrop`
- `InteractionPromptText`
- `HUDOverlayGroup`

`HUDOverlayGroup` 안에는 아래 관리 대상이 들어갑니다.

- `GuideBackdrop`
- `GuideText`
- `GuideHelpButton`
- `ResultBackdrop`
- `RestaurantResultText`

## `PopupRoot` 기준 구조

- `PopupShellGroup`
- `PopupFrame`
- `PopupFrameHeader`

`PopupShellGroup` 아래에는 `PopupOverlay`를 둡니다.
`PopupFrame` 아래에는 다음 핵심 구조를 유지합니다.

- `PopupTitle`
- `PopupCloseButton`
- `PopupFrameLeft`
- `PopupFrameRight`

좌우 프레임 안에는 다음 이름 규칙을 유지합니다.

- 왼쪽: `PopupLeftCaption`, `PopupLeftBody`, `PopupLeftItemBox01`~`04`, `PopupLeftItemText01`~`04`, `InventoryText`
- 오른쪽: `PopupRightCaption`, `PopupRightBody`, `PopupRightItemBox01`~`04`, `PopupRightItemText01`~`04`, `StorageText`, `SelectedRecipeText`, `UpgradeText`

## 편집 규칙

- `Apply Preview`는 미리보기 반영용이며, 실제 부모 재구성은 `Canvas Grouping` 기준으로 봅니다.
- `HUDRoot` 또는 `PopupRoot` 하위 이름을 바꾸면 `UIManager`, `JongguMinimalPrototypeBuilder`, `PrototypeSceneAudit`를 함께 갱신합니다.
- 팝업 프레임 구조를 바꾸면 `PrototypeUILayout.Popup.cs`, `PrototypeUISkinCatalog.Popup.cs`도 함께 확인합니다.
- 지원 Canvas 씬 저장 시 관리 대상 값이 `ui-layout-overrides.asset`으로 동기화되므로, 수동 재구성 뒤에는 저장 결과를 다시 확인합니다.