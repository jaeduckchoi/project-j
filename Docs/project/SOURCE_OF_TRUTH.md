# 정본 관계 가이드

이 문서는 현재 프로젝트에서 무엇을 정본으로 삼아야 하는지, 그리고 어떤 파일들을 함께 수정해야 하는지를 설명하는 정본이다.

## 1. 문서 정본

- 작업 시작 맵: `AGENTS.md`, `CLAUDE.md`
- 문서 허브: `.aiassistant/rules/README.md`
- 전역 규칙: `Docs/project/GAME_ASSISTANT_RULES.md`
- 작업 흐름: `Docs/project/AGENT_WORKFLOW.md`
- 구조와 경로: `Docs/project/GAME_PROJECT_STRUCTURE.md`
- 커밋 메시지 규칙: `Docs/project/GIT_COMMIT_TEMPLATE.md`
- 게임 의도: `Docs/gameplay/*`

## 2. 코드와 자산의 정본

### 지원 씬 직렬화

- `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` 씬에 직접 저장된 월드 `Transform`, 주요 컴포넌트 직렬화 값, `SpriteRenderer`, 월드 `TextMeshPro` 값은 런타임 정본이다.
- 이미 씬에 저장된 값은 런타임 보강 코드가 기본적으로 덮어쓰지 않는다.

### Canvas 관리 UI

- 지원 씬 Canvas 아래의 관리 대상 UI 기준은 `Assets/Resources/Generated/ui-layout-overrides.asset`가 정본이다.
- `PrototypeUISceneLayoutSettings`, `UIManager`, `PrototypeUICanvasAutoSync`, 빌더가 같은 기준을 공유한다.
- managed UI에는 `GuideText`, `RestaurantResultText`, `GuideHelpButton`, `PopupTitle`, `PopupLeftCaption`, HUD/Popup 그룹 구조가 포함된다.

### generated 자산 경로

- generated 자산 루트와 design source 루트의 정본은 `Assets/Resources/Generated/prototype-generated-asset-settings.asset`와 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`다.
- UI source는 `Assets/Design/GeneratedSources/UI`이고 output은 `Assets/Resources/Generated/Sprites/UI`다.
- source의 `PanelVariants`는 output에서 `Panels`로 정리된다.

### 빌더

- `JongguMinimalPrototypeBuilder`는 generated 자산, Build Settings, Canvas 동기화, 누락된 지원 씬 복구, generated scene audit 흐름의 정본 구현이다.
- existing 지원 씬을 강제로 재생성하는 도구가 아니라, sync와 missing-scene recovery 중심으로 동작해야 한다.

### 런타임 보강

- `PrototypeSceneRuntimeAugmenter`는 누락된 오브젝트, 누락된 참조, 런타임 생성이 필요한 오브젝트만 보강한다.
- 이미 저장된 오브젝트와 값은 그대로 유지하는 것이 기본 원칙이다.

### 감사 코드

- `PrototypeSceneAudit`는 generated 씬 구조, UI 그룹, 레이아웃 기준의 정본 검증기다.
- `GameplayAutomationAudit`는 day cycle, popup pause, portal 규칙 같은 회귀 위험 로직의 정본 검증기다.

### 인접 API 계약

- Unity API 연동 계약의 외부 정본은 `D:\project-j-api\docs\rules\API.md`, 관련 controller/dto, `src/main/resources/db/migration/V2__seed_catalog.sql`이다.
- Unity 쪽 연결 구현의 정본은 `Assets/Scripts/CoreLoop/Core/JongguApiSession.cs`와 `Assets/Scripts/CoreLoop/Core/GameManager.cs`다.
- 씬 이름, resource/recipe/tool/upgrade code는 Unity와 API가 같은 문자열 계약을 유지해야 한다.

## 3. 정본이 아닌 것

- generated PNG, generated 씬 출력물, 런타임 출력은 정본이 아니다.
- 예전 커밋 시점의 임시 복구 메모나 수동 패치 절차도 정본이 아니다.
- 엔트리 파일 안의 요약 문장은 세부 규칙의 정본이 아니다.

## 4. 함께 수정해야 하는 결합 지점

### UI 구조 변경

- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
- `Assets/Resources/Generated/ui-layout-overrides.asset`
- `Docs/ui/UI_AND_TEXT_GUIDE.md`, 필요 시 `Docs/ui/UI_GROUPING_RULES.md`

### 씬 루트 구조 변경

- `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs`
- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- `Assets/Editor/PrototypeSceneAudit.cs`
- `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`, `Docs/scene/GAME_SCENE_AND_SETUP.md`

### generated 자산 경로 변경

- `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- `Assets/Resources/Generated/prototype-generated-asset-settings.asset`
- `Assets/Editor/JongguMinimalPrototypeBuilder*.cs`
- `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/build/GAME_BUILD_GUIDE.md`

### Unity API 연동 변경

- `Assets/Scripts/CoreLoop/Core/GameManager.cs`
- `Assets/Scripts/CoreLoop/Core/JongguApiSession.cs`
- 원격 스냅샷을 적용하는 각 매니저의 `ApplyRemoteState` 계열 메서드
- `D:\project-j-api\docs\rules\API.md`, 관련 DTO/controller, `src/main/resources/db/migration/V2__seed_catalog.sql`

### 허브 월드 아트 변경

- `Assets/Design` 원본
- `Assets/Resources/Generated/Sprites/Hub`
- `Assets/Scripts/Exploration/World/HubRoomLayout.cs`
- `Assets/Scripts/Exploration/World/PrototypeSceneRuntimeAugmenter.cs`
- `Assets/Editor/JongguMinimalPrototypeBuilder*.cs`

## 5. 정본 판단 순서

1. 이 값이 지원 씬에 직접 저장된 값인가?
2. 관리 대상 Canvas 값이라면 `ui-layout-overrides.asset` 기준인가?
3. generated 경로나 design source 경로라면 `PrototypeGeneratedAssetSettings` 기준인가?
4. 값이 실제로 missing recovery용인가, 아니면 기존 씬 값을 덮어쓰는가?
5. 인접 API 계약까지 포함해 관련 코드와 문서가 같은 기준을 설명하고 있는가?

이 질문에 답하지 못하면 구현 전에 관련 코드와 문서를 다시 확인한다.
