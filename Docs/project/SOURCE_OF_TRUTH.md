# 정본 관계 가이드

## 문서 정본

- 작업 시작 안내: `AGENTS.md`, `CLAUDE.md`
- 작업 하네스: `Docs/README.md`
- 프로젝트 규칙: `Docs/project/GAME_ASSISTANT_RULES.md`
- 작업 절차: `Docs/project/AGENT_WORKFLOW.md`
- 구조와 경로: `Docs/project/GAME_PROJECT_STRUCTURE.md`

## 코드와 자산 정본

### 씬 직렬화

- `Assets/Scenes` 아래 실제 씬 직렬화 값이 월드 구조의 정본이다.
- 런타임 보강 코드는 누락된 오브젝트, 컴포넌트, 참조만 보충한다.
- `SceneWorldRoot`는 월드 계층 정리용 루트다.
- MainCamera 하드 경계 정본은 `WorldBoundsRoot/CameraBounds`다.
- `HubRoomLayout.cs`는 허브의 32x18 논리 타일 계약과 1타일 = 1유닛 기준을 담는다.
- 최종 허브 배치 정본은 `Assets/Scenes/Hub.unity` 직렬화 값이다.

### Canvas 관리 UI

- 관리 대상 Canvas 레이아웃 정본은 `Assets/Resources/Generated/ui-layout-overrides.asset`다.
- managed UI 이름과 런타임 read API 정본은 `PrototypeUISceneLayoutCatalog`다.
- 에디터 sync/overlay/capture 흐름은 `PrototypeUISceneLayoutCatalog.Editor*.cs`가 맡는다.
- 에디터 프리뷰와 저장 흐름은 `PrototypeUIDesignController`와 `UIManager.EditorPreview.cs`가 맡는다.
- UI entry와 partial은 `UIManager.cs`와 `UIManager.*.cs` family를 기준으로 유지한다.
- 레이아웃, 스타일, 팝업 콘텐츠는 각각 `PrototypeUILayout*.cs`, `PrototypeUISkin*.cs`, `PrototypeUIPopupCatalog.cs`를 기준으로 유지한다.
- 팝업 일시정지 계산은 `PopupPauseStateUtility.cs`가 정본이고, `UIManager`는 결과만 반영한다.

### TMP 폰트 자산

- 원본 폰트는 `Assets/TextMesh Pro/Fonts/Galmuri11.ttf`, `Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf`다.
- TMP Font Asset은 `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11 SDF.asset`, `Assets/TextMesh Pro/Resources/Fonts & Materials/Galmuri11-Bold SDF.asset`다.
- UI/월드 텍스트는 씬에 저장된 폰트 참조와 TMP 기본 폰트 fallback을 기준으로 처리한다.

### generated 경로

- generated 자산 루트와 리소스 로드 경로의 정본은 `PrototypeGeneratedAssetSettings.cs`다.
- UI 리소스는 `Assets/Resources/Generated/Sprites/UI`에 둔다.
- 플레이어 리소스는 `Assets/Resources/Generated/Sprites/Player`에 둔다.
- generated 게임 데이터는 `Assets/Resources/Generated/GameData`에 둔다.

### authored 아트 import

- authored 스프라이트 원본은 `Assets/Art/*`다.
- import 정책 정본은 `Assets/Editor/Art/ArtSpriteImportPostprocessor.cs`다.
- `Assets/Resources/Generated/*`는 generated 출력물이므로 authored import 규칙의 정본이 아니다.

### 로컬 게임 데이터

- Unity 런타임은 외부 네트워크 연동 없이 씬 직렬화 값, generated GameData, 메모리 fallback 데이터를 기준으로 상태를 구성한다.
- 레시피와 자원 코드는 generated GameData와 `GeneratedGameDataLocator` fallback 정의를 기준으로 유지한다.
- fallback 자원 코드는 `Fish`, `Shell`, `Seaweed`, `Herb`, `Mushroom`, `GlowMoss`, `WindHerb` 형식을 사용한다.

## 정본이 아닌 것

- generated PNG와 generated 출력물 자체
- 임시 복구 메모
- 과거 자동 생성·감사 흐름이나 그에 대한 문서 설명

## 함께 맞춰야 하는 결합 지점

- UI 구조 변경: `UIManager.*.cs`, `PrototypeUISceneLayoutCatalog*.cs`, `PrototypeUISceneLayoutSettings.cs`, `PrototypeUILayout*.cs`, `PrototypeUISkin*.cs`, `PrototypeUIPopupCatalog.cs`, `PopupPauseStateUtility.cs`, `ui-layout-overrides.asset`, `Docs/ui/*`
- TMP 폰트 변경: `UIManager.cs`, `PrototypeUISkin.cs`, `GAME_PROJECT_STRUCTURE.md`, `Docs/ui/UI_AND_TEXT_GUIDE.md`
- 씬 하이어라키 변경: `PrototypeSceneHierarchyCatalog.cs`, `PrototypeSceneHierarchyOrganizer.cs`, `Docs/scene/*`
- generated 경로 변경: `PrototypeGeneratedAssetSettings.cs`, `GAME_PROJECT_STRUCTURE.md`, `Docs/build/GAME_BUILD_GUIDE.md`
- 로컬 데이터와 런타임 상태 변경: `GameManager.cs`, `GeneratedGameDataLocator.cs`, 관련 manager 초기화와 자원 흐름
