# 프로젝트 구조 가이드

이 문서는 현재 저장소의 실제 구조와 책임 경계를 정리한 정본이다.
코드, 씬, generated 자산, 문서 경로를 설명할 때는 이 문서를 기준으로 맞춘다.

## 1. 최상위 구조

```text
.
├─ .aiassistant
│  ├─ README.md
│  └─ rules
│     ├─ build
│     ├─ gameplay
│     ├─ local
│     ├─ project
│     ├─ scene
├─ Assets
├─ Packages
├─ ProjectSettings
├─ AGENTS.md
└─ CLAUDE.md
```

참고:

- `Library`, `Logs`, `obj`, `.idea`, `.vscode` 같은 로컬/캐시 폴더는 공유 구조의 정본이 아니다.
- `.claude/` 같은 에디터별 로컬 설정 폴더도 규칙 문서 구조의 일부로 취급하지 않는다.
- 인접 API 서버 저장소 `D:\project-j-api`는 이 트리 안에 포함되지 않지만, Unity API 연동 작업에서는 함께 확인해야 하는 외부 결합 저장소다.

## 2. `.aiassistant` 구조

```text
.aiassistant
├─ README.md
└─ rules
   ├─ project
   │  ├─ GAME_ASSISTANT_RULES.md
   │  ├─ GAME_DOCS_INDEX.md
   │  ├─ GAME_PROJECT_STRUCTURE.md
   │  ├─ AGENT_WORKFLOW.md
   │  ├─ SOURCE_OF_TRUTH.md
   │  └─ GIT_COMMIT_TEMPLATE.md
   ├─ gameplay
   ├─ ui
   ├─ scene
   ├─ build
   └─ local
```

## 3. Unity 자산 구조

```text
Assets
├─ Editor
│  └─ UI
├─ Resources
│  └─ Generated
│     ├─ Fonts
│     ├─ GameData
│     │  ├─ Input
│     │  ├─ Recipes
│     │  └─ Resources
│     ├─ Sprites
│     │  ├─ Gather
│     │  ├─ Hub
│     │  ├─ Player
│     │  ├─ UI
│     │  └─ World
│     ├─ generated-game-data-manifest.asset
│     ├─ prototype-generated-asset-settings.asset
│     └─ ui-layout-overrides.asset
├─ Scenes
├─ Scripts
│  ├─ CoreLoop
│  ├─ Exploration
│  ├─ Management
│  ├─ Restaurant
│  ├─ Shared
│  └─ UI
├─ Settings
│  └─ Scenes
├─ TextMesh Pro
├─ UI Toolkit
└─ _Recovery
```

## 4. 책임 경계

- `Assets/Scripts`: 런타임 코드
- `Assets/Editor`: 빌더, 감사, 자동 동기화, 커스텀 에디터
- `Assets/Scenes`: 지원 씬과 플레이 가능 씬
- `Assets/Resources/Generated`: generated 자산과 런타임 로드 경로
- 저장소 안에는 더 이상 `Assets/Design` 원본 저장소를 두지 않는다. 필요하면 `PrototypeGeneratedAssetSettings`에 외부 원본 경로를 선택적으로 연결한다.
- `Packages`, `ProjectSettings`: Unity 패키지와 프로젝트 설정의 정본

## 5. 런타임 코드 기준

- `Assets/Scripts/CoreLoop`: `GameManager`, `DayCycleManager`, `JongguApiSession` 같은 전역 상태, 안내 흐름, 원격 세션 진입점
- `Assets/Scripts/Exploration`: 플레이어 이동, 상호작용, 채집, 포털, 지대, 런타임 보강
- `Assets/Scripts/Management`: 경제, 인벤토리, 창고, 도구, 업그레이드
- `Assets/Scripts/Restaurant`: 메뉴 선택과 영업 로직
- `Assets/Scripts/Shared`: shared 데이터 타입, generated 자산 설정, 로케이터
- `Assets/Scripts/UI`: `UIManager`, 레이아웃, 스킨, 팝업 정지 유틸리티, UI 콘텐츠

## 6. 에디터 코드 기준

- `Assets/Editor/JongguMinimalPrototypeBuilder*.cs`: generated 자산, 씬 구성, Build Settings, UI 베이스라인
- `Assets/Editor/PrototypeSceneAudit.cs`: generated 씬과 UI 구조 감사
- `Assets/Editor/GameplayAutomationAudit.cs`: 안내 흐름, popup pause, portal 규칙 경량 감사
- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`: 지원 씬 루트 구조 정리
- `Assets/Editor/ProjectStructureUtility.cs`: 기본 폴더 구조 보장
- `Assets/Editor/UI/*`: UI 프리뷰, Canvas auto-sync, generated 이미지 보조 도구

## 7. generated 자산 구조

### 7-1. generated 자산 루트

- generated 경로의 실제 루트는 `Assets/Resources/Generated/prototype-generated-asset-settings.asset`와 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`가 정본이다.
- 코드에서 경로를 하드코딩하기보다 위 자산과 타입이 제공하는 경로를 우선 사용한다.

### 7-2. GameData

- `Assets/Resources/Generated/GameData/Resources`
- `Assets/Resources/Generated/GameData/Recipes`
- `Assets/Resources/Generated/GameData/Input`

### 7-3. Fonts

- 기본 generated 폰트는 `maplestoryLightSdf.asset`, `maplestoryBoldSdf.asset`
- 예외적으로 generated 폰트와 원본 TTF 파일명은 lower camelCase를 유지한다.

### 7-4. UI 스프라이트

- 저장소 안의 작업 기준 루트는 `Assets/Resources/Generated/Sprites/UI`
- 하위 카테고리는 `Buttons`, `MessageBoxes`, `Panels`
- 선택적 외부 원본 경로는 `Assets/Resources/Generated/prototype-generated-asset-settings.asset`와 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`에 비워 두거나 연결한다.
- 외부 원본이 연결돼 있으면 기존 `Buttons`, `MessageBoxes`, `PanelVariants` 입력 구조를 그대로 쓸 수 있고, output에서는 `Panels`로 정리된다.

### 7-5. 플레이어 스프라이트

- 작업 기준 루트는 `Assets/Resources/Generated/Sprites/Player`다.
- 빌더는 이 폴더의 플레이어 PNG를 재import만 하고, 별도 분해나 합성은 하지 않는다.

## 8. 네임스페이스와 어셈블리 기준

현재 코드 기준 네임스페이스:

- `CoreLoop.*`
- `Exploration.*`
- `Management.*`
- `Restaurant`
- `Shared.*`
- `UI.*`
- `Editor`
- `Editor.UI`

현재 asmdef 기준:

- `Assets/Scripts/Jonggu.Gameplay.asmdef`
- `Assets/Scripts/Shared/Jonggu.Shared.asmdef`
- `Assets/Scripts/UI/Jonggu.UI.asmdef`
- `Assets/Editor/Jonggu.Editor.asmdef`

## 9. 변경 시 함께 맞출 항목

- 프로젝트 구조나 generated 경로 변경:
  `PrototypeGeneratedAssetSettings`, 빌더 코드, 이 문서, `Docs/build/GAME_BUILD_GUIDE.md`
- UI 기준 변경:
  `UIManager`, 빌더 UI 코드, `ui-layout-overrides.asset`, UI 문서
- 씬 루트 구조 변경:
  `PrototypeSceneHierarchyCatalog`, `PrototypeSceneHierarchyOrganizer`, `PrototypeSceneAudit`, 씬 문서

## 10. 이 문서를 갱신해야 하는 경우

- 새 공유 폴더나 문서 루트를 만들었을 때
- generated 자산 루트나 output 폴더 구조가 바뀌었을 때
- asmdef, 네임스페이스, 핵심 책임 경계가 바뀌었을 때
- `AGENTS.md`나 인덱스에서 참조하는 구조 설명이 실제 저장소와 달라졌을 때
