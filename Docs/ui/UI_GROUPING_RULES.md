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

## PopupRoot

- `PopupShellGroup`
- `PopupFrame`
- `PopupFrameHeader`

## 편집 규칙

- `Apply Preview`는 프리뷰 반영이고, 실제 그룹 정리는 `Canvas Grouping` 기준으로 처리합니다.
- `HUDRoot` 또는 `PopupRoot` 하위 이름을 바꾸면 `UIManager`와 관련 UI 문서를 함께 갱신합니다.
- popup 프레임 구조를 바꾸면 `PrototypeUILayout.Popup.cs`, `PrototypeUISkinCatalog.Popup.cs`를 함께 확인합니다.
- 삭제된 빌더/감사/자동 동기화 코드는 더 이상 갱신 대상이 아닙니다.
