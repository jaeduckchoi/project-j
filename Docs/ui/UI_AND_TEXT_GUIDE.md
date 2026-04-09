# UI 및 텍스트 가이드

## 현재 UI 기준

- Canvas 최상위 공용 루트는 `HUDRoot`, `PopupRoot`
- 런타임 UI 동작의 중심은 `Assets/Scripts/UI/UIManager.cs`
- 레이아웃 정본은 `Assets/Resources/Generated/ui-layout-overrides.asset`
- 레이아웃 설정 타입은 `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`

## 관리 대상 이름

- HUD: `GuideText`, `RestaurantResultText`, `GuideHelpButton`
- Popup: `PopupTitle`, `PopupLeftCaption`, `PopupRightCaption`, `InventoryText`, `StorageText`, `SelectedRecipeText`, `UpgradeText`

이 이름들은 현재 런타임 UI 코드 기준으로 함께 유지합니다.

## generated UI 경로

- 루트: `Assets/Resources/Generated/Sprites/UI`
- 하위: `Buttons`, `MessageBoxes`, `Panels`
- 경로 정본: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`

## 편집 규칙

- 씬에 직접 저장된 popup 텍스트, 이미지, 폰트, 배치 값은 명시적 요청 없이는 덮어쓰지 않습니다.
- Canvas 그룹 재정리는 `UIManager`와 `PrototypeUIDesignController` 기반 편집 도구를 사용합니다.
- 삭제된 빌더, 자동 동기화, 구조 감사 파일을 전제로 작업하지 않습니다.

## 함께 확인할 코드

- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/PopupPauseStateUtility.cs`
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
- `Assets/Editor/UI/UIManagerEditor.cs`
- `Assets/Editor/UI/PrototypeUIDesignControllerEditor.cs`
