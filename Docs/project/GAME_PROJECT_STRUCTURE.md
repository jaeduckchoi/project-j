# 프로젝트 구조 가이드

## 최상위 구조

```text
.
├─ Assets
├─ Docs
├─ Packages
├─ ProjectSettings
├─ AGENTS.md
└─ CLAUDE.md
```

## 주요 디렉터리

- `Assets/Code/Scripts`: 런타임 코드
- `Assets/Code/Editor`: 에디터 전용 도구와 인스펙터 보조 코드
- `Assets/Code/Tests`: EditMode/PlayMode 테스트
- `Assets/Data/GameDataSource`: authored CSV 게임 데이터 원본
- `Assets/Level/Scenes`: 씬 직렬화 정본
- `Assets/Resources/Generated`: 런타임이 읽는 generated 리소스
- `Assets/Settings`: 렌더 파이프라인, 입력, 씬 템플릿 등 설정 자산
- `Assets/Art`: authored 스프라이트 원본 예약 경로
- `Docs`: 프로젝트, 게임플레이, UI, 씬, 빌드 문서 정본

## Assets 기준 구조

```text
Assets
├─ Code
│  ├─ Editor
│  │  ├─ Art
│  │  ├─ GameData
│  │  └─ UI
│  ├─ Scripts
│  │  ├─ CoreLoop
│  │  ├─ Exploration
│  │  ├─ Management
│  │  ├─ Restaurant
│  │  ├─ Shared
│  │  └─ UI
│  └─ Tests
│     ├─ EditMode
│     └─ PlayMode
├─ Data
│  └─ GameDataSource
├─ Level
│  └─ Scenes
├─ Resources
│  └─ Generated
├─ Settings
├─ TextMesh Pro
└─ UI Toolkit
```

## 어셈블리 경계

- `Assets/Code/Scripts/Jonggu.Gameplay.asmdef`: CoreLoop, Exploration, Management, Restaurant 런타임 코드
- `Assets/Code/Scripts/Shared/Jonggu.Shared.asmdef`: 공용 데이터와 유틸
- `Assets/Code/Scripts/UI/Jonggu.UI.asmdef`: UI 런타임 코드
- `Assets/Code/Editor/Jonggu.Editor.asmdef`: 에디터 전용 코드
- `Assets/Code/Tests/EditMode/Jonggu.Gameplay.EditModeTests.asmdef`: EditMode 테스트
- `Assets/Code/Tests/PlayMode/Jonggu.Gameplay.PlayModeTests.asmdef`: PlayMode 테스트

각 asmdef의 `GlobalSuppressions.cs`는 해당 어셈블리의 네임스페이스/폴더 예외만 관리한다. 새 네임스페이스는 폴더 구조와 맞추는 것이 우선이다.

## 런타임 기준 파일

- 전역 상태: `Assets/Code/Scripts/CoreLoop/Core/GameManager.cs`
- 경로 authority: `Assets/Code/Scripts/Shared/ProjectAssetPaths.cs`
- generated 경로/기본값: `Assets/Code/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- GameData fallback: `Assets/Code/Scripts/Shared/Data/GeneratedGameDataLocator.cs`
- UI entry: `Assets/Code/Scripts/UI/UIManager.cs`
- UI partial: `UIManager.Lifecycle.cs`, `UIManager.EditorPreview.cs`, `UIManager.Bindings.cs`, `UIManager.Input.cs`, `UIManager.Canvas.cs`, `UIManager.Chrome.cs`, `UIManager.HubPopup.cs`, `UIManager.Kitchen.cs`, `UIManager.Refresh.cs`
- UI layout: `Assets/Code/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`, `PrototypeUILayout*.cs`, `PrototypeUIObjectNames.cs`, `PrototypeUILayoutBindingSettings.cs`
- UI style: `Assets/Code/Scripts/UI/Style/PrototypeUISkin*.cs`, `PrototypeUITheme.cs`
- UI content catalog: `Assets/Code/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`
- UI controller: `Assets/Code/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
- 팝업 일시정지 유틸: `Assets/Code/Scripts/UI/PopupPauseStateUtility.cs`

## 에디터 기준 파일

- 씬 계층 정리: `Assets/Code/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 기본 폴더 보조: `Assets/Code/Editor/ProjectStructureUtility.cs`
- 게임 데이터 importer: `Assets/Code/Editor/GameData/CsvGameDataImporter.cs`
- authored art import: `Assets/Code/Editor/Art/ArtSpriteImportPostprocessor.cs`
- UI 인스펙터, 프리뷰, 레이아웃 편집기: `Assets/Code/Editor/UI/*`

## 데이터 원본 경로

- authored CSV 원본: `Assets/Data/GameDataSource/*.csv`
- generated 출력 경로: `Assets/Resources/Generated/GameData`

## 설정 자산 경로

- 렌더러/파이프라인: `Assets/Settings/Renderer2D.asset`, `Assets/Settings/UniversalRP.asset`
- 입력/볼륨/URP 전역 설정: `Assets/Settings/InputSystemActions.inputactions`, `Assets/Settings/DefaultVolumeProfile.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- 씬 템플릿: `Assets/Settings/Lit2DSceneTemplate.scenetemplate`, `Assets/Settings/Scenes/URP2DSceneTemplate.unity`

## generated 경로 기준

- 경로 정본: `Assets/Code/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
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
