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

## Unity 자산 구조

```text
Assets
├─ Editor
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

## 책임 경계

- `Assets/Scripts`: 런타임 코드
- `Assets/Scenes`: 실제 씬 직렬화 정본
- `Assets/Resources/Generated`: 런타임이 읽는 generated 리소스
- `Assets/Editor`: 구조 정리와 인스펙터 보조 도구

## 에디터 코드 기준

- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`: 씬 하이어라키 정리
- `Assets/Editor/ProjectStructureUtility.cs`: 기본 폴더 구조 보조
- `Assets/Editor/UI/*`: UI 인스펙터와 프리뷰 보조

삭제된 항목:

- `Assets/Editor/JongguMinimalPrototypeBuilder*.cs`
- `Assets/Editor/PrototypeSceneAudit.cs`
- `Assets/Editor/GameplayAutomationAudit.cs`
- `Assets/Editor/UI/PrototypeUICanvasAutoSync.cs`

## generated 경로 기준

- 경로 정본: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- UI 리소스: `Assets/Resources/Generated/Sprites/UI`
- 플레이어 리소스: `Assets/Resources/Generated/Sprites/Player`
- 게임 데이터: `Assets/Resources/Generated/GameData`

## 런타임 기준 파일

- API 세션/원격 동기화: `Assets/Scripts/CoreLoop/Core/JongguApiSession.cs`
- 전역 상태: `Assets/Scripts/CoreLoop/Core/GameManager.cs`
- UI 동작: `Assets/Scripts/UI/UIManager.cs`
- UI 레이아웃 설정: `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
- generated 경로/기본값: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`

## 인접 저장소

`D:\project-j-api`는 Unity 트리 밖에 있지만 API 연동 작업에서는 함께 확인해야 하는 결합 저장소입니다.
