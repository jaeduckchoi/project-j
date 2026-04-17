# 프로젝트 구조 가이드

## 최상위 구조

```text
.
├─ .aiassistant
├─ Assets
├─ Docs
├─ Packages
├─ ProjectSettings
├─ AGENTS.md
└─ CLAUDE.md
```

## 주요 디렉터리

- `Assets/Scripts`: 런타임 코드
- `Assets/Editor`: 에디터 전용 도구와 인스펙터 보조 코드
- `Assets/Scenes`: 씬 직렬화 정본
- `Assets/Art`: authored 스프라이트 원본
- `Assets/Resources/Generated`: 런타임이 읽는 generated 리소스
- `Docs`: 프로젝트, 게임플레이, UI, 씬, 빌드 문서 정본

## Assets 기준 구조

```text
Assets
├─ Art
├─ Editor
│  ├─ Art
│  ├─ Tests
│  └─ UI
├─ Resources
│  └─ Generated
│     ├─ Fonts
│     ├─ GameData
│     ├─ Sprites
│     └─ ui-layout-overrides.asset
├─ Scenes
├─ Scripts
│  ├─ CoreLoop
│  ├─ Exploration
│  ├─ Management
│  ├─ Restaurant
│  ├─ Shared
│  └─ UI
└─ Settings
```

## 어셈블리 경계

- `Assets/Scripts/Jonggu.Gameplay.asmdef`: CoreLoop, Exploration, Management, Restaurant 런타임 코드
- `Assets/Scripts/Shared/Jonggu.Shared.asmdef`: 공용 데이터와 유틸
- `Assets/Scripts/UI/Jonggu.UI.asmdef`: UI 런타임 코드
- `Assets/Editor/Jonggu.Editor.asmdef`: 에디터 전용 코드
- `Assets/Editor/Tests/Jonggu.Gameplay.EditModeTests.asmdef`: EditMode 테스트

각 asmdef의 `GlobalSuppressions.cs`는 해당 어셈블리의 네임스페이스/폴더 예외만 관리한다. 새 네임스페이스는 폴더 구조와 맞추는 것이 우선이다.

## 런타임 기준 파일

- 전역 상태: `Assets/Scripts/CoreLoop/Core/GameManager.cs`
- generated 경로/기본값: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- GameData fallback: `Assets/Scripts/Shared/Data/GeneratedGameDataLocator.cs`
- UI entry: `Assets/Scripts/UI/UIManager.cs`
- UI partial: `UIManager.Lifecycle.cs`, `UIManager.EditorPreview.cs`, `UIManager.Bindings.cs`, `UIManager.Input.cs`, `UIManager.Canvas.cs`, `UIManager.Chrome.cs`, `UIManager.HubPopup.cs`, `UIManager.Kitchen.cs`, `UIManager.Refresh.cs`
- UI layout: `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`, `PrototypeUISceneLayoutSettings.cs`, `PrototypeUILayout*.cs`, `PrototypeUIObjectNames.cs`
- UI style: `Assets/Scripts/UI/Style/PrototypeUISkin*.cs`, `PrototypeUITheme.cs`
- UI content catalog: `Assets/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`
- UI controller: `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
- 팝업 일시정지 유틸: `Assets/Scripts/UI/PopupPauseStateUtility.cs`

## 에디터 기준 파일

- 씬 계층 정리: `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 기본 폴더 보조: `Assets/Editor/ProjectStructureUtility.cs`
- authored art import: `Assets/Editor/Art/ArtSpriteImportPostprocessor.cs`
- UI 인스펙터와 프리뷰: `Assets/Editor/UI/*`

## generated 경로 기준

- 경로 정본: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- UI 리소스: `Assets/Resources/Generated/Sprites/UI`
- 플레이어 리소스: `Assets/Resources/Generated/Sprites/Player`
- 플레이어 기본 프레임: `base`
- 플레이어 idle 보조 프레임: `idle/front`, `idle/back`, `idle/side`
- 게임 데이터: `Assets/Resources/Generated/GameData`

## TMP 폰트 경로

- 원본 폰트: `Assets/TextMesh Pro/Fonts/Galmuri11.ttf`, `Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf`
- TMP Font Asset: `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11 SDF.asset`, `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11-Bold SDF.asset`

## 외부 네트워크 연동

현재 Unity 클라이언트는 외부 네트워크 연동 없이 로컬 런타임 상태와 generated GameData 기준으로 동작한다.
