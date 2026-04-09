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
├─ Art
│  ├─ Buildings
│  ├─ Characters
│  ├─ FX
│  ├─ Props
│  ├─ Tiles
│  └─ UI
├─ Editor
│  ├─ Art
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
│     ├─ Content
│     │  └─ Catalog
│     ├─ Controllers
│     ├─ Layout
│     │  ├─ Catalog
│     │  └─ Definitions
│     ├─ Style
│     │  ├─ Catalog
│     │  └─ Foundation
│     └─ UIManager
└─ Settings
```

## 책임 경계

- `Assets/Scripts`: 런타임 코드
- `Assets/Art`: authored 원본 스프라이트 아트
- `Assets/Scenes`: 실제 씬 직렬화 정본
- `Assets/Resources/Generated`: 런타임이 읽는 generated 리소스
- `Assets/Editor`: 구조 정리, authored art import, 인스펙터 보조 도구

## 어셈블리 경계

- `Assets/Scripts/Jonggu.Gameplay.asmdef`: 런타임 게임플레이 코드. CoreLoop, Exploration, Management, Restaurant 네임스페이스를 포함한다.
- `Assets/Scripts/Shared/Jonggu.Shared.asmdef`: 공용 데이터/유틸. `Shared`, `Shared.Data` 네임스페이스를 포함한다.
- `Assets/Scripts/UI/Jonggu.UI.asmdef`: UI 런타임 코드. `UI`, `UI.Content`, `UI.Controllers`, `UI.Layout`, `UI.Style` 네임스페이스를 포함한다.
- `Assets/Editor/Jonggu.Editor.asmdef`: 에디터 전용 코드. `ProjectEditor`, `ProjectEditor.UI` 네임스페이스를 사용한다.

## 어셈블리별 GlobalSuppressions

각 asmdef는 자체 `GlobalSuppressions.cs`를 두고 `IDE0130` (네임스페이스/폴더 불일치)를 어셈블리 단위로 억제한다.

- `Assets/Scripts/GlobalSuppressions.cs`: Gameplay 어셈블리. `Restaurant` 단일 네임스페이스 예외만 유지.
- `Assets/Scripts/Shared/GlobalSuppressions.cs`: Shared 어셈블리. `Shared.Data` 예외.
- `Assets/Scripts/UI/GlobalSuppressions.cs`: UI 어셈블리. `UI`, `UI.Content`, `UI.Controllers`, `UI.Layout`, `UI.Style` 예외.
- `Assets/Editor/GlobalSuppressions.cs`: Editor 어셈블리. `ProjectEditor`, `ProjectEditor.UI` 예외(`UNITY_EDITOR` 가드 안).

새 네임스페이스는 폴더 구조와 일치시키는 것이 우선이며, 예외 추가는 같은 어셈블리의 GlobalSuppressions에만 등록한다.

## 에디터 코드 기준

- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`: 씬 하이어라키 정리
- `Assets/Editor/ProjectStructureUtility.cs`: 기본 폴더 구조 보조
- `Assets/Editor/Art/ArtSpriteImportPostprocessor.cs`: `Assets/Art/*` 스프라이트 import 정책과 타일셋 자동 슬라이스
- `Assets/Editor/UI/*`: UI 인스펙터와 프리뷰 보조

## generated 경로 기준

- 경로 정본: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- UI 리소스: `Assets/Resources/Generated/Sprites/UI`
- 플레이어 리소스: `Assets/Resources/Generated/Sprites/Player`
  기본 프레임은 `base`, idle 보조 프레임은 `idle/front`, `idle/back`, `idle/side`에 둔다.
- 게임 데이터: `Assets/Resources/Generated/GameData`

## 런타임 기준 파일

- API 세션/원격 동기화: `Assets/Scripts/CoreLoop/Core/JongguApiSession.cs`
- 전역 상태: `Assets/Scripts/CoreLoop/Core/GameManager.cs`
- UI 탐색 폴더: 엔트리/루트 파일은 `Assets/Scripts/UI`, family별 세부 구현은 `UIManager`, `Layout/Catalog`, `Layout/Definitions`, `Style/Catalog`, `Style/Foundation`, `Content/Catalog` 아래에 둔다.
- UI 동작: `Assets/Scripts/UI/UIManager.cs` (엔트리), `Assets/Scripts/UI/UIManager/UIManager.Lifecycle.cs`, `UIManager.EditorPreview.cs`, `UIManager.Bindings.cs`, `UIManager.Input.cs`, `UIManager.Canvas.cs`, `UIManager.Chrome.cs`, `UIManager.HubPopup.cs`, `UIManager.Refresh.cs`
- UI 레이아웃 catalog: `Assets/Scripts/UI/Layout/Catalog/PrototypeUISceneLayoutCatalog.cs` (런타임 read API), `PrototypeUISceneLayoutCatalog.Editor.cs`, `PrototypeUISceneLayoutCatalog.Editor.Capture.cs`
- UI 레이아웃 설정: `Assets/Scripts/UI/Layout/Definitions/PrototypeUISceneLayoutSettings.cs`
- UI 레이아웃 partial: `Assets/Scripts/UI/Layout/Definitions/PrototypeUILayout.cs`, `PrototypeUILayout.UI.cs`, `PrototypeUILayout.Popup.cs`, `PrototypeUIObjectNames.cs`
- UI 스타일 catalog: `Assets/Scripts/UI/Style/Catalog/PrototypeUISkinCatalog.cs`, `PrototypeUISkinCatalog.UI.cs`, `PrototypeUISkinCatalog.Popup.cs`, `Assets/Scripts/UI/Style/Foundation/PrototypeUISkin.cs`, `PrototypeUITheme.cs`
- UI 콘텐츠 catalog: `Assets/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`
- 팝업 일시정지 유틸: `Assets/Scripts/UI/PopupPauseStateUtility.cs`
- TMP 폰트 해석/한글 fallback: `Assets/Scripts/Shared/TmpFontAssetResolver.cs`
- generated 경로/기본값: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`

## 인접 저장소

`D:\project-j-api`는 Unity 트리 밖에 있지만 API 연동 작업에서는 함께 확인해야 하는 결합 저장소입니다.
