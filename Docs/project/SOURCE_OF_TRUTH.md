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

### Canvas 관리 UI

- `Assets/Resources/Generated/ui-layout-overrides.asset`가 관리 대상 Canvas 레이아웃의 정본입니다.
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`와 `Assets/Scripts/UI/UIManager.cs`가 이를 읽는 런타임 기준입니다.
- `GuideText`, `RestaurantResultText`, `GuideHelpButton`, `PopupTitle`, `PopupLeftCaption` 같은 managed UI 이름은 런타임 UI 코드 기준으로 유지합니다.

### generated 경로

- generated 자산 루트와 리소스 로드 경로의 정본은 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- generated UI 리소스는 `Assets/Resources/Generated/Sprites/UI`
- generated 플레이어 리소스는 `Assets/Resources/Generated/Sprites/Player`
- generated 게임 데이터는 `Assets/Resources/Generated/GameData`

### 에디터 코드

- 현재 에디터 정본은 구조 정리와 인스펙터 보조 도구입니다.
- 삭제된 `JongguMinimalPrototypeBuilder`, `PrototypeSceneAudit`, `GameplayAutomationAudit`는 더 이상 정본이 아닙니다.

### 인접 API 계약

- Unity API 연동의 외부 정본은 `D:\project-j-api\docs\rules\API.md`, 관련 controller/dto, `src/main/resources/db/migration/V2__seed_catalog.sql`
- Unity 쪽 구현 정본은 `Assets/Scripts/CoreLoop/Core/JongguApiSession.cs`와 `Assets/Scripts/CoreLoop/Core/GameManager.cs`

## 정본이 아닌 것

- generated PNG, generated 출력물, 임시 복구 메모
- 삭제된 빌더/감사 메뉴나 그에 대한 과거 문서 설명

## 함께 맞춰야 하는 결합 지점

### UI 구조 변경

- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
- `Assets/Resources/Generated/ui-layout-overrides.asset`
- `Docs/ui/UI_AND_TEXT_GUIDE.md`
- `Docs/ui/UI_GROUPING_RULES.md`

### 씬 하이어라키 변경

- `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs`
- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- `Docs/scene/GAME_SCENE_AND_SETUP.md`
- `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`

### generated 경로 변경

- `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- `Docs/project/GAME_PROJECT_STRUCTURE.md`
- `Docs/build/GAME_BUILD_GUIDE.md`

### Unity API 연동 변경

- `Assets/Scripts/CoreLoop/Core/GameManager.cs`
- `Assets/Scripts/CoreLoop/Core/JongguApiSession.cs`
- `D:\project-j-api\docs\rules\API.md`와 관련 DTO/controller/seed SQL
