---
적용: 항상
---

# 종구의 식당 프로젝트 구조 가이드

## 1. 목적

이 문서는 `Jonggu Restaurant` 저장소의 실제 폴더 구조와 책임 경계를 정의한다.
기본 원칙은 작업 기준 문서는 `.aiassistant/rules` 아래에 두고, 실제 Unity 에셋은 `Assets` 아래에 유지하는 것이다.

## 2. 현재 최상위 구조

```text
.
├─ .aiassistant
│  └─ rules
│     ├─ build
│     ├─ gameplay
│     ├─ local
│     ├─ project
│     ├─ scene
│     └─ ui
├─ Assets
│  ├─ Design
│  ├─ Editor
│  │  └─ UI
│  ├─ Generated
│  ├─ Resources
│  │  └─ Generated
│  ├─ Scenes
│  ├─ Scripts
│  ├─ Settings
│  ├─ TextMesh Pro
│  ├─ UI Toolkit
│  └─ _Recovery
└─ Tools
```

## 3. `.aiassistant/rules`의 역할

- `project`
  공용 작업 규칙, 문서 인덱스, 저장소 구조 기준을 둔다.
- `gameplay`
  코어 루프, 탐험, 식당/성장, 통합 게임플레이 기준을 둔다.
- `ui`
  HUD/팝업 구조, 텍스트, Canvas 그룹 기준을 둔다.
- `scene`
  씬 구성, 계층 그룹, 씬 점검 문서를 둔다.
- `build`
  빌더 흐름, 생성 자산 복구, 감사 흐름 문서를 둔다.
- `local`
  개인 PC 메모나 로컬 실행 규칙을 둔다.

## 4. 현재 `Assets` 구조

```text
Assets
├─ Design
│  ├─ Archive
│  ├─ GeneratedSources
│  │  ├─ Data
│  │  ├─ Fonts
│  │  ├─ Sprites
│  │  └─ UI
│  ├─ References
│  └─ UIDesign
│     ├─ Exports
│     ├─ Mockups
│     └─ Vector
├─ Editor
│  └─ UI
├─ Generated
│  ├─ Fonts
│  ├─ GameData
│  └─ Sprites
│     ├─ Gather
│     ├─ Hub
│     ├─ Player
│     ├─ UI
│     └─ World
├─ Resources
│  └─ Generated
│     └─ Sprites
│        ├─ Gather
│        ├─ Hub
│        ├─ Player
│        ├─ UI
│        └─ World
├─ Scenes
├─ Scripts
│  ├─ Camera
│  ├─ Core
│  ├─ Data
│  ├─ Economy
│  ├─ Flow
│  ├─ Gathering
│  ├─ Interaction
│  ├─ Inventory
│  ├─ Player
│  ├─ Restaurant
│  ├─ Storage
│  ├─ Tools
│  ├─ UI
│  ├─ Upgrade
│  └─ World
├─ Settings
│  └─ Scenes
├─ TextMesh Pro
├─ UI Toolkit
└─ _Recovery
```

## 5. 폴더별 책임

- `Assets/Scripts`
  런타임 코드 전용이다. `CoreLoop`, `Exploration`, `Management`, `Restaurant`, `UI`, `Shared` 기능 루트를 사용해 관련 시스템을 가깝게 유지한다.
- `Assets/Editor`
  빌더, 감사, Canvas 자동 동기화, 커스텀 인스펙터 같은 에디터 전용 코드를 둔다.
- `Assets/Scenes`
  플레이 가능한 씬을 둔다. 지원 씬은 `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill`이다.
- `Assets/Resources/Generated`
  생성 자산과 런타임 `Resources.Load` 경로를 함께 관리하는 출력물을 둔다.
- `Assets/Resources/Generated`
  런타임에서 `Resources.Load`로 직접 읽는 생성 자산을 둔다.
- `Assets/Design`
  디자인 원본과 검토 자료 전용이다. 런타임이 이 경로를 직접 참조하면 안 된다.
- `Assets/Settings/Scenes`
  Build Settings와 씬 설정 관련 프로젝트 설정을 둔다.
- `Assets/TextMesh Pro`
  TMP 설정과 기본 TMP 리소스를 둔다.
- `Assets/UI Toolkit`
  Unity UI Toolkit 기본 리소스를 둔다.
- `Assets/_Recovery`
  복구 또는 임시 저장 경로이며 정식 런타임 기준은 아니다.

## 6. 생성 자산 구조

### `Assets/Resources/Generated/GameData`

- 생성 데이터는 `Resources`, `Recipes`, `Input` 역할 기준으로 묶는다.
- 파일명은 `resource-*`, `recipe-*`, `generated-ui-*` 같은 kebab-case 패턴을 사용한다.
- 핵심 자원은 `Assets/Resources/Generated/GameData/Resources`, 레시피는 `Assets/Resources/Generated/GameData/Recipes`, 생성 입력 자산은 `Assets/Resources/Generated/GameData/Input` 아래에 둔다.

### `Assets/Resources/Generated/Fonts`

- 본문 폰트 기준은 `maplestoryLightSdf.asset`, 제목 폰트 기준은 `maplestoryBoldSdf.asset`를 유지한다.
- 원본 TTF는 `maplestoryLight.ttf`, `maplestoryBold.ttf`이며, 생성 폰트 파일명은 lower camelCase를 유지한다.
- `malgunGothicSdf.asset`는 폴백 또는 레거시 자산으로 남아 있을 수 있지만, 현재 빌더 기본값은 메이플스토리 계열이다.

### `Assets/Resources/Generated/Sprites`

- 생성 스프라이트는 `Player`, `Gather`, `World`, `Hub`, `UI` 아래로 구분한다.
- `Assets/Design/GeneratedSources/UI`는 빌더를 통해 `Assets/Resources/Generated/Sprites/UI`에 반영된다.
- UI 원본은 `Buttons`, `MessageBoxes`, `Panels` 같은 카테고리 기준으로 정리한다.

### `Assets/Resources/Generated`

- 런타임이 직접 읽는 공용 생성 자산을 둔다.
- `generated-game-data-manifest.asset`
  생성 데이터 복구와 런타임 로딩에 쓰는 매니페스트다.
- `ui-layout-overrides.asset`
  지원 씬 공용 Canvas 레이아웃과 표시값 오버라이드 자산이다.

## 7. 런타임 코드 구조

- Assets/Scripts/CoreLoop
  GameManager, DayCycleManager 같은 전역 조합과 하루 루프 진입점을 둔다.
- Assets/Scripts/Exploration
  플레이어 이동, 카메라, 채집, 상호작용, 포탈, 위험 지대, 씬 보강을 둔다.
- Assets/Scripts/Management
  경제, 인벤토리, 창고, 도구, 업그레이드 진행을 둔다.
- Assets/Scripts/Restaurant
  메뉴 선택, 영업 실행, 허브 상호작용 로직을 둔다.
- Assets/Scripts/Shared
  ResourceData, RecipeData, 생성 데이터 로케이터, 매니페스트 타입 같은 공용 정의를 둔다.
- Assets/Scripts/UI
  UIManager, 팝업 일시정지 로직, 레이아웃, 스타일, 콘텐츠 카탈로그를 둔다.

## 8. 에디터 코드 구조

- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
  생성 자산, 기본 씬, 기본 UI 배치를 재구성한다.
- `Assets/Editor/PrototypeSceneAudit.cs`
  생성 씬 구조와 UI 기준을 감사한다.
- `Assets/Editor/GameplayAutomationAudit.cs`
  day-loop 흐름, 팝업 일시정지, 포탈 잠금, 누락 씬 안내를 점검한다.
- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
  지원 씬 계층을 공용 루트 구조로 다시 정리한다.
- `Assets/Editor/UI/*`
  Canvas 자동 동기화, UI 프리뷰 도구, 생성 스프라이트 보조 에디터를 둔다.

## 9. 배치 규칙

### 새 스크립트 추가

- 런타임 코드는 `Assets/Scripts` 아래 올바른 기능 루트에 배치해야 한다.
- 에디터 코드는 `Assets/Editor` 아래에 배치해야 한다.
- 씬 보강, 스폰 로직, 이동 보조 로직은 가능하면 `Assets/Scripts/Exploration/World`를 사용한다.

### 새 데이터 추가

- 데이터 타입 정의는 `Assets/Scripts/Shared/Data` 아래에 둔다.
- 빌더가 생성하는 데이터 자산은 `Assets/Resources/Generated/GameData`의 대응 하위 폴더에 두고, 역할이 드러나는 kebab-case 파일명을 유지한다.
- 런타임이 `Resources.Load`로 데이터를 읽는다면 `Assets/Resources/Generated/generated-game-data-manifest.asset`와 관련 경로를 함께 맞춘다.

### 새 UI 추가

- 먼저 `Assets/Scripts/UI/UIManager.cs`에서 상태 갱신과 버튼 연결 방식을 확인한다.
- 정적 문자열과 텍스트 카탈로그는 `Assets/Scripts/UI/Content` 아래에 둔다.
- 간격, 위치, 그룹 기준은 `Assets/Scripts/UI/Layout` 아래에 둔다.
- 스프라이트 경로와 스킨 적용 로직은 `Assets/Scripts/UI/Style` 아래에 둔다.
- UI를 바꿀 때는 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`와 `Assets/Editor/UI/PrototypeUIDesignControllerEditor.cs`도 함께 검토한다.

## 10. 생성 경로 규칙

- 생성 씬, 생성 자산, 런타임 출력물만 직접 수정하지 않는다. 먼저 빌더 경로를 수정한다.
- 생성 자산 경로는 `Assets/Resources/Generated` 하나를 기준으로 유지하고, 런타임 로딩 경로와 실제 폴더 구조를 일치시킨다.
- 런타임 코드가 `Resources.Load`를 쓰면 문서 경로와 실제 폴더 구조가 정확히 일치해야 한다.
- 지원 씬 Canvas 레이아웃은 `ui-layout-overrides.asset`를 기준으로 따라야 하며, 씬 저장 시 트리거되는 자동 동기화 흐름이 유지되어야 한다.

## 11. 작업 체크리스트

### 게임플레이 변경

- 새 코드가 올바른 책임 폴더 아래에 배치되었는가?
- 관련 `GameManager` 또는 매니저 참조가 필요한가?
- 새 데이터를 추가했다면 `Assets/Resources/Generated/GameData` 규칙과 문서도 함께 갱신했는가?
- 월드 레이아웃을 바꿨다면 `Assets/Scripts/Exploration/World`, `Scenes`, 빌더 코드, 계층 규칙 문서를 함께 검토했는가?

### UI 변경

- `Assets/Scripts/UI/UIManager.cs`를 검토했는가?
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 검토했는가?
- 저장/로드 경로가 `Assets/Resources/Generated/ui-layout-overrides.asset`와 맞는가?
- 팝업 관련 변경이라면 일시정지와 복구 동작이 여전히 정상인가?

### 생성 구조 변경

- 결과 씬만이 아니라 빌더 코드부터 변경했는가?
- `Assets/Editor/PrototypeSceneAudit.cs`도 기준에 맞게 갱신했는가?
- `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs`와 정리기 기준이 서로 맞는가?
- 네임스페이스, 직렬화 경로, 리소스 경로가 모두 일관된가?

## 12. 현재 기준 메모

- 공용 작업 기준 문서는 `.aiassistant/rules` 아래에 있다.
- 생성 데이터는 `Assets/Resources/Generated/GameData/Resources`, `Assets/Resources/Generated/GameData/Recipes`, `Assets/Resources/Generated/GameData/Input` 아래에 묶여 있다.
- 생성 폰트 기본값은 `maplestoryLightSdf`와 `maplestoryBoldSdf`다.
- Canvas UI 오버라이드는 `Assets/Resources/Generated/ui-layout-overrides.asset`에 저장된다.
- 이 작업에서는 Unity 실행과 컴파일을 직접 검증하지 못했으므로, 이후 빌더/감사/플레이 모드 검증이 추가로 필요하다.
