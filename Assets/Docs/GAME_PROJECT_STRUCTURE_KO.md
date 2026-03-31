# 종구 식당 프로젝트 구조 가이드

## 1. 목적

이 문서는 `종구 식당` 저장소에서 `Assets` 구조를 어떤 기준으로 유지하는지 정리한 기준 문서다.
이번 기준은 폴더를 과하게 깊게 나누지 않되, generated 스프라이트처럼 역할 구분이 중요한 자산은 한 단계 기능 폴더로 나누어 유지하는 데 맞춘다.

## 2. 현재 `Assets` 구조

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
├─ Docs
├─ Editor
│  └─ UI
├─ Generated
│  ├─ Fonts
│  ├─ GameData
│  └─ Sprites
│     ├─ Gather
│     ├─ Hub
│     ├─ Player
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
└─ TextMesh Pro
```

## 3. 폴더 역할

- `Assets/Scripts`
  런타임 코드 전용 폴더다. 게임플레이, 데이터, UI, 월드 상호작용을 여기서 관리한다.
- `Assets/Editor`
  빌더, 감사 코드, 커스텀 인스펙터처럼 에디터에서만 실행되는 코드를 둔다.
- `Assets/Scenes`
  실제 플레이 가능한 씬을 둔다. 현재 기준 씬은 `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` 이다.
- `Assets/Generated`
  빌더가 생성하거나 갱신하는 원본 산출 경로다.
- `Assets/Resources/Generated`
  런타임에서 `Resources.Load` 로 직접 읽는 generated 리소스 경로다.
- `Assets/Design`
  디자인 원본 보관용 폴더다. 런타임이 직접 읽는 자산은 여기로 연결하지 않는다.
  generated 결과물과 연결되는 디자인 소스는 `GeneratedSources`에 두고, 순수 제작용 원본은 `UIDesign` 아래에서 관리한다.
  현재 `GeneratedSources/UI`에는 버튼, 메시지 박스, 패널 바리에이션처럼 generated UI 작업에 대응되는 원본이 들어 있다.

## 4. generated 자산 구조

### `Assets/Generated/GameData`

- 데이터 파일은 하위 폴더로 쪼개지 않고 한 디렉토리에 모아 둔다.
- 파일명은 kebab-case 로 맞추고, `resource-*`, `recipe-*`, `generated-ui-*` 패턴으로 성격이 드러나게 유지한다.
- `resource-fish.asset`, `resource-shell.asset` 같은 파일은 수집 자원 데이터다.
- `recipe-sushi-set.asset`, `recipe-seafood-soup.asset` 같은 파일은 레시피 데이터다.
- `generated-ui-input-actions.asset` 는 generated 입력 액션 자산이다.

### `Assets/Generated/Sprites`

- 생성 스프라이트는 `Player`, `Gather`, `World`, `Hub`, `UI` 하위 디렉토리로 나누어 둔다.
- `Assets/Design/GeneratedSources/UI` 원본은 빌더가 `Assets/Generated/Sprites/UI` 와 `Assets/Resources/Generated/Sprites/UI` 로 미러링해 사용한다.
- 파일명은 자산 규칙에 맞춰 kebab-case 로 유지하고, 폴더 역할이 드러나도록 `player-front.png`, `gather-fish.png`, `world-floor.png`, `hub-bar.png`처럼 맞춘다.
- 예시:
  `Player/player-front.png`, `World/world-floor.png`, `Gather/gather-fish.png`, `Hub/hub-bar.png`, `UI/Buttons/close-button.png`

### `Assets/Resources/Generated`

- 루트에는 런타임에서 바로 읽는 공용 generated 자산을 둔다.
- `generated-game-data-manifest.asset`
  런타임 데이터 복구와 로더가 참조하는 generated 데이터 매니페스트다.
- `ui-layout-overrides.asset`
  Canvas UI 배치와 표시값 오버라이드를 저장하는 공용 자산이다.

### `Assets/Resources/Generated/Sprites`

- 런타임 폴백 스프라이트는 `Player`, `Gather`, `World`, `Hub`, `UI` 하위 디렉토리로 나누어 둔다.
- 플레이어, 채집물, 월드 공용, 허브 전용, 런타임 전용 UI 스프라이트를 폴더 기준으로 구분한다.
- `UI/Buttons`, `UI/MessageBoxes`, `UI/Panels` 는 `GeneratedSources/UI` 디자인 원본을 반영하는 생성 경로다.
- 예시:
  `Player/player-front.png`, `Hub/hub-today-menu-bg-1.png`, `UI/Buttons/help-button.png`, `UI/MessageBoxes/system-text-box.png`

## 5. 런타임 코드 구조

- `Assets/Scripts/Core`
  `GameManager` 같은 전역 조합 진입점을 둔다.
- `Assets/Scripts/Data`
  `ResourceData`, `RecipeData`, generated 데이터 로케이터와 매니페스트 타입을 둔다.
- `Assets/Scripts/Player`
  입력, 이동, 시각 표현, 방향 스프라이트를 둔다.
- `Assets/Scripts/World`
  스폰, 탐험 보강, 포탈, 월드 공통 로직을 둔다.
- `Assets/Scripts/UI`
  `UIManager`, 레이아웃, 스타일, 콘텐츠 카탈로그를 둔다.

## 6. 배치 규칙

### 새 스크립트 추가

- 런타임 코드는 반드시 `Assets/Scripts` 아래 책임에 맞는 기존 폴더에 둔다.
- 에디터 코드는 반드시 `Assets/Editor` 아래에 둔다.
- 여러 기능이 공유하는 씬 보강, 스폰, 이동 보조 로직은 `Assets/Scripts/World` 를 우선한다.

### 새 데이터 추가

- 데이터 타입 정의는 `Assets/Scripts/Data` 에 둔다.
- 빌더가 생성하는 데이터 자산은 `Assets/Generated/GameData` 한 디렉토리에 두고, kebab-case 파일명으로 `resource-*`, `recipe-*`, `generated-ui-*` 역할이 드러나게 맞춘다.
- 런타임에서 `Resources.Load` 로 찾아야 하는 데이터 참조가 있으면 `Assets/Resources/Generated/generated-game-data-manifest.asset` 경로까지 함께 맞춘다.

### 새 UI 추가

- 화면 상태 갱신과 버튼 연결은 `Assets/Scripts/UI/UIManager.cs` 를 먼저 본다.
- 정적 문구와 텍스트 카탈로그는 `Assets/Scripts/UI/Content` 에 둔다.
- 좌표, 여백, 카드 박스 규칙은 `Assets/Scripts/UI/Layout` 에 둔다.
- 스프라이트 경로와 스킨 적용은 `Assets/Scripts/UI/Style` 에 둔다.
- UI를 바꿨다면 `Assets/Editor/JongguMinimalPrototypeBuilder.cs` 와 함께 확인한다.

## 7. 생성 경로 규칙

- 생성 씬, generated 에셋, 리소스 출력은 결과물만 직접 고치지 말고 먼저 빌더 경로를 수정한다.
- `Assets/Generated` 는 빌더 산출 원본, `Assets/Resources/Generated` 는 런타임 로딩 경로라는 차이를 유지한다.
- 같은 디렉토리에서 파일을 바로 볼 수 있게 유지하되, 파일명은 kebab-case 로 맞추고 폴더와 파일 이름이 함께 용도를 드러내게 유지한다.
- generated 폰트와 소스 폰트 파일명은 `Assets/Generated/Fonts` 아래에서 lower camelCase 를 유지한다.
- 런타임 코드가 `Resources.Load` 를 사용하면 문서에 적힌 경로와 실제 폴더 구조가 반드시 일치해야 한다.

## 8. 작업 체크리스트

### 게임플레이 변경

- 기능 폴더 배치가 맞는가
- `GameManager` 또는 관련 매니저 참조가 필요한가
- 새 데이터가 생기면 `Assets/Generated/GameData` 의 kebab-case 파일명 규칙과 문서도 같이 갱신했는가
- 월드 배치가 바뀌면 `World`, `Scenes`, 빌더 코드를 같이 확인했는가

### UI 변경

- `Assets/Scripts/UI/UIManager.cs` 를 확인했는가
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs` 를 함께 확인했는가
- `Assets/Resources/Generated/ui-layout-overrides.asset` 경로를 기준으로 저장과 로딩이 맞는가
- 팝업이면 시간 정지와 닫기 후 복구 흐름이 유지되는가

### 생성 구조 변경

- `Assets/Scenes` 결과물만 고치지 않고 빌더도 같이 수정했는가
- `Assets/Editor/PrototypeSceneAudit.cs` 기준도 함께 맞췄는가
- 직렬화 경로, `using`, 네임스페이스, 리소스 경로가 같은 기준으로 맞는가

## 9. 현재 기준 메모

- generated 데이터는 `Assets/Generated/GameData` 한 디렉토리에 두고 `resource-*`, `recipe-*`, `generated-ui-*` kebab-case 파일명 기준으로 구분한다.
- generated 스프라이트는 `Assets/Generated/Sprites` 아래 `Player`, `Gather`, `World`, `Hub` 하위 디렉토리 기준으로 관리한다.
- 런타임 폴백 스프라이트는 `Assets/Resources/Generated/Sprites` 아래 `Player`, `Gather`, `World`, `Hub`, `UI` 하위 디렉토리 기준으로 관리한다.
- 런타임 데이터 매니페스트는 `Assets/Resources/Generated/generated-game-data-manifest.asset` 기준으로 관리한다.
- Canvas UI 오버라이드는 `Assets/Resources/Generated/ui-layout-overrides.asset` 에 저장한다.
- Unity 실행과 컴파일 검증은 이 작업에서 직접 확인하지 못했으므로, 실제 적용 후에는 씬 열기와 플레이 모드 기준 검증이 필요하다.
