# 프로젝트 구조 가이드

이 문서는 저장소 구조와 대표 경로의 정본이다.

## 최상위 구조

```text
.
├─ Assets
├─ Docs
├─ Packages
├─ ProjectSettings
├─ Skills
├─ AGENTS.md
└─ CLAUDE.md
```

## 주요 디렉터리

- `Assets/Code/Scripts`: 런타임 코드
- `Assets/Code/Editor`: 에디터 도구와 인스펙터 코드
- `Assets/Code/Tests`: EditMode, PlayMode 테스트
- `Assets/Data/GameDataSource`: authored CSV 게임 데이터 원본
- `Assets/Level/Scenes`: 씬 직렬화 정본
- `Assets/Resources/Generated`: 런타임이 읽는 generated 리소스
- `Assets/Settings`: 렌더 파이프라인, 입력, 씬 템플릿 등 설정 자산
- `Docs`: Project, Gameplay, Scene, UI, Build 문서 정본
- `Skills`: Codex, Claude 등 에이전트가 사용하는 워크플로 자산. 게임 규칙과 정본 관계는 `Docs/Project` 문서가 소유한다.

## Assets 구조

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
├─ Data
├─ Level
├─ Resources
├─ Settings
└─ TextMesh Pro
```

## 대표 어셈블리와 경로

- 런타임 게임플레이: `Assets/Code/Scripts/Jonggu.Gameplay.asmdef`
- 공유 데이터와 유틸: `Assets/Code/Scripts/Shared/Jonggu.Shared.asmdef`
- UI 런타임: `Assets/Code/Scripts/UI/Jonggu.UI.asmdef`
- 에디터 코드: `Assets/Code/Editor/Jonggu.Editor.asmdef`
- 테스트: `Assets/Code/Tests/EditMode/Jonggu.Gameplay.EditModeTests.asmdef`, `Assets/Code/Tests/PlayMode/Jonggu.Gameplay.PlayModeTests.asmdef`

## 대표 런타임 파일

- 전역 상태: `Assets/Code/Scripts/CoreLoop/Core/GameManager.cs`
- generated 경로와 기본값: `Assets/Code/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- GameData fallback: `Assets/Code/Scripts/Shared/Data/GeneratedGameDataLocator.cs`
- UI 엔트리: `Assets/Code/Scripts/UI/UIManager.cs`
- UI 레이아웃 catalog: `Assets/Code/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`
- UI 레이아웃 정의: `Assets/Code/Scripts/UI/Layout/PrototypeUILayout.cs`, `PrototypeUILayout.UI.cs`, `PrototypeUILayout.Popup.cs`
- UI 스타일 정의: `Assets/Code/Scripts/UI/Style/PrototypeUISkin.cs`, `PrototypeUISkinCatalog.cs`, `PrototypeUISkinCatalog.UI.cs`, `PrototypeUISkinCatalog.Popup.cs`
- 팝업 콘텐츠 catalog: `Assets/Code/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`
- 에디터 프리뷰 컨트롤러: `Assets/Code/Scripts/UI/Controllers/PrototypeUIDesignController.cs`

## 대표 에디터 파일

- 씬 계층 정리: `Assets/Code/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 기본 폴더 보조: `Assets/Code/Editor/ProjectStructureUtility.cs`
- CSV importer: `Assets/Code/Editor/GameData/CsvGameDataImporter.cs`
- authored art import: `Assets/Code/Editor/Art/ArtSpriteImportPostprocessor.cs`
- UI 에디터 도구: `Assets/Code/Editor/UI/UIManagerEditor.cs`, `PrototypeUIDesignControllerEditor.cs`, `PrototypeUILayoutBindingSyncUtility.cs`

## 데이터와 설정 경로

- authored CSV 원본: `Assets/Data/GameDataSource/*.csv`
- generated GameData: `Assets/Resources/Generated/GameData`
- generated UI 리소스: `Assets/Resources/Generated/Sprites/UI`
- generated 플레이어 리소스: `Assets/Resources/Generated/Sprites/Player`
- 렌더러와 파이프라인 설정: `Assets/Settings/Renderer2D.asset`, `Assets/Settings/UniversalRP.asset`
- 입력과 전역 설정: `Assets/Settings/InputSystemActions.inputactions`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- TMP 폰트 원본: `Assets/TextMesh Pro/Fonts/Galmuri11.ttf`, `Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf`
- TMP Font Asset: `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11 SDF.asset`, `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11-Bold SDF.asset`
