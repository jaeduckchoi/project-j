# 정본 관계 가이드

## 문서 정본

- 작업 시작 안내: `AGENTS.md`, `CLAUDE.md`
- 규칙 허브: `.aiassistant/rules/README.md`
- 프로젝트 규칙: `Docs/project/GAME_ASSISTANT_RULES.md`
- 작업 절차: `Docs/project/AGENT_WORKFLOW.md`
- 구조와 경로: `Docs/project/GAME_PROJECT_STRUCTURE.md`
- 커밋 메시지 규칙: `Docs/project/GIT_COMMIT_TEMPLATE.md`

## 코드와 자산 정본

### 씬 직렬화

- `Assets/Scenes` 아래 실제 씬 직렬화 값이 월드 구조의 정본입니다.
- 런타임 보강 코드는 누락된 오브젝트, 컴포넌트, 참조만 보충해야 합니다.
- `SceneWorldRoot`는 월드 계층 정리용 루트이고, MainCamera의 하드 경계 정본은 `WorldBoundsRoot/CameraBounds`입니다.
- `Assets/Scripts/Exploration/World/HubRoomLayout.cs` 는 허브의 32x18 논리 타일 계약과 1타일 = 1유닛 기준을 담고, 최종 배치 정본은 여전히 `Assets/Scenes/Hub.unity` 직렬화 값입니다.
- 허브의 `Back Counter`, `Front Counter`, `Mosaic Tile Floor`, `Mosaic Tile Wall` 는 `Design/Object/Main Hub/*` 32px 소스 아트를 기준으로 하며, 현재는 `Assets/Scenes/Hub.unity` 의 `scale 3.125` 직렬화 값으로 월드 타일 크기를 맞춥니다.

### Canvas 관리 UI

- `Assets/Resources/Generated/ui-layout-overrides.asset`가 관리 대상 Canvas 레이아웃의 정본입니다.
- UI 코드는 엔트리/루트 파일은 `Assets/Scripts/UI`, family별 세부 구현은 `Assets/Scripts/UI`, `Assets/Scripts/UI/Layout`, `Assets/Scripts/UI/Style`, `Assets/Scripts/UI/Content/Catalog` 아래에 둡니다.
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`가 런타임 read API와 managed object 이름 기준의 정본이며, `GetManagedCanvasObjectNames`, `EnumerateHudCanvasObjectNames`, `EnumeratePopupCanvasObjectNames`를 통해 runtime/editor가 같은 목록을 공유합니다. `PrototypeUISceneLayoutCatalog.Editor.cs`, `PrototypeUISceneLayoutCatalog.Editor.Capture.cs`는 에디터 sync/overlay/capture 흐름을 맡습니다.
- `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`와 `Assets/Scripts/UI/UIManager.EditorPreview.cs`는 런타임 생성 UI를 에디터에서도 생성·프리뷰·저장 가능하게 유지하는 정본 흐름입니다.
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`와 `Assets/Scripts/UI/UIManager.cs`(엔트리), `Assets/Scripts/UI/UIManager.Lifecycle.cs`, `UIManager.EditorPreview.cs`, `UIManager.Bindings.cs`, `UIManager.Input.cs`, `UIManager.Canvas.cs`, `UIManager.Chrome.cs`, `UIManager.HubPopup.cs`, `UIManager.Refresh.cs`가 이를 읽는 런타임 기준입니다.
- 레이아웃 partial 정본: `Assets/Scripts/UI/Layout/PrototypeUILayout.cs`(엔트리), `PrototypeUILayout.UI.cs`, `PrototypeUILayout.Popup.cs`, `PrototypeUIObjectNames.cs`(PopupTitle/Caption 공용 상수).
- 스타일 partial 정본: `Assets/Scripts/UI/Style/PrototypeUISkin.cs`, `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.cs`(엔트리), `PrototypeUISkinCatalog.UI.cs`, `PrototypeUISkinCatalog.Popup.cs`, `Assets/Scripts/UI/Style/PrototypeUITheme.cs`.
- 팝업 콘텐츠 catalog 정본: `Assets/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`.
- 팝업 일시정지 계산 정본: `Assets/Scripts/UI/PopupPauseStateUtility.cs` (`UIManager`는 결과만 받아 `Time.timeScale`에 반영).
- `GuideText`, `RestaurantResultText`, `GuideHelpButton`, `PopupTitle`, `PopupLeftCaption` 같은 managed UI 이름은 `PrototypeUISceneLayoutCatalog` 기준으로 유지합니다.

### TMP 폰트 자산

- 프로젝트 원본 폰트 소스는 `Assets/TextMesh Pro/Fonts/Galmuri11.ttf`, `Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf`입니다.
- 프로젝트 TMP Font Asset 경로는 `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11 SDF.asset`, `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11-Bold SDF.asset`입니다.
- UI/월드 텍스트는 씬에 저장된 폰트 참조와 TMP 기본 폰트 fallback을 기준으로 처리합니다.

### generated 경로

- generated 자산 루트와 리소스 로드 경로의 정본은 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- generated UI 리소스는 `Assets/Resources/Generated/Sprites/UI`
- generated 플레이어 리소스는 `Assets/Resources/Generated/Sprites/Player`
  기본 프레임 정본은 `base`, idle 보조 프레임 정본은 `idle/<direction>` 구조를 따른다.
- generated 게임 데이터는 `Assets/Resources/Generated/GameData`

### authored 아트 import 규칙

- authored 스프라이트 아트의 원본 경로는 `Assets/Art/*` 입니다.
- `Assets/Art/*` import 정책의 정본은 `Assets/Editor/Art/ArtSpriteImportPostprocessor.cs` 입니다.
- 타일/월드/캐릭터/FX/UI 경로별 PPU, 필터, 슬라이스 규칙은 이 Editor postprocessor를 기준으로 유지합니다.
- `Assets/Resources/Generated/*` 는 generated 출력물이므로 같은 import 규칙의 정본이 아닙니다.

### 에디터 코드

- 현재 에디터 정본은 구조 정리와 인스펙터 보조 도구입니다.
- `Assets/Art/*` authored 스프라이트 import 자동화도 에디터 정본 범위에 포함됩니다.
- 과거 자동 생성·감사 흐름은 더 이상 정본이 아닙니다.

### 로컬 게임 데이터 계약

- Unity 런타임은 외부 네트워크 연동 없이 씬 직렬화 값, generated GameData, 메모리 fallback 데이터를 기준으로 상태를 구성합니다.
- 레시피와 자원 코드는 generated GameData와 `GeneratedGameDataLocator`의 fallback 정의를 기준으로 유지합니다.
- fallback 자원 코드는 `Fish`, `Shell`, `Seaweed`, `Herb`, `Mushroom`, `GlowMoss`, `WindHerb` 형식을 사용합니다.

## 정본이 아닌 것

- generated PNG, generated 출력물, 임시 복구 메모
- 과거 자동 생성·감사 흐름이나 그에 대한 문서 설명

## 함께 맞춰야 하는 결합 지점

### UI 구조 변경

- `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/UIManager.Lifecycle.cs`, `UIManager.EditorPreview.cs`, `UIManager.Bindings.cs`, `UIManager.Input.cs`, `UIManager.Canvas.cs`, `UIManager.Chrome.cs`, `UIManager.HubPopup.cs`, `UIManager.Refresh.cs`
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`(`.Editor.cs`, `.Editor.Capture.cs` 포함)
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
- `Assets/Scripts/UI/Layout/PrototypeUILayout.cs`(`.UI.cs`, `.Popup.cs` 포함)
- `Assets/Scripts/UI/Layout/PrototypeUIObjectNames.cs` (PopupTitle/Caption 같은 팝업 공용 이름을 건드릴 때)
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.cs`(`.UI.cs`, `.Popup.cs` 포함), `Assets/Scripts/UI/Style/PrototypeUISkin.cs`, `PrototypeUITheme.cs`
- `Assets/Scripts/UI/Content/Catalog/PrototypeUIPopupCatalog.cs`
- `Assets/Scripts/UI/PopupPauseStateUtility.cs`
- `Assets/Resources/Generated/ui-layout-overrides.asset`
- `Docs/ui/UI_AND_TEXT_GUIDE.md`
- `Docs/ui/UI_GROUPING_RULES.md`

### TMP 폰트 자산 변경

- `Assets/Scripts/UI/UIManager.cs` (폰트 적용 호출부)
- `Assets/Scripts/UI/Style/PrototypeUISkin.cs` (텍스트 스타일에서 폰트 사용 시)
- `Docs/project/GAME_PROJECT_STRUCTURE.md`
- `Docs/ui/UI_AND_TEXT_GUIDE.md`

### 씬 하이어라키 변경

- `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs`
- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- `Docs/scene/GAME_SCENE_AND_SETUP.md`
- `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`

### generated 경로 변경

- `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- `Docs/project/GAME_PROJECT_STRUCTURE.md`
- `Docs/build/GAME_BUILD_GUIDE.md`

### 로컬 데이터와 런타임 상태 변경

- `Assets/Scripts/CoreLoop/Core/GameManager.cs`
- `Assets/Scripts/Shared/Data/GeneratedGameDataLocator.cs`
- 관련 manager의 초기화, 인벤토리, 창고, 경제, 도구, 업그레이드 흐름
