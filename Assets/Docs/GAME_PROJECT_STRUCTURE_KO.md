# 종구의 식당 프로젝트 구조 가이드

## 1. 목적

이 문서는 `종구의 식당` 저장소에서 폴더별 역할과 파일 배치 기준을 정리한다.
새 기능을 추가하거나 폴더를 옮길 때, 어떤 경로가 소스 오브 트루스인지 빠르게 판단할 수 있게 하는 것이 목적이다.

## 2. 최상위 구조

현재 기준 `Assets` 핵심 구조는 아래와 같다.

```text
Assets
├─ Design
├─ Docs
├─ Editor
│  └─ UI
├─ Generated
│  ├─ Fonts
│  ├─ GameData
│  └─ Sprites
├─ Resources
│  └─ Generated
│     └─ Sprites
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
  런타임 코드 전용 폴더다. 게임플레이, 데이터 타입, UI 동작, 월드 상호작용을 여기서 관리한다.
- `Assets/Editor`
  빌더, 감사 코드, 커스텀 인스펙터처럼 에디터에서만 쓰는 코드를 둔다.
- `Assets/Scenes`
  실제 플레이 가능한 씬 파일을 둔다. 현재 기준 `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` 씬이 여기에 있다.
- `Assets/Generated`
  빌더가 생성하거나 갱신하는 에셋의 기본 산출 경로다.
  `Fonts` 는 TMP 폰트 소스와 생성 결과, `GameData` 는 ScriptableObject 데이터, `Sprites` 는 생성 스프라이트를 둔다.
- `Assets/Resources/Generated`
  `Resources.Load` 로 직접 불러오는 런타임 리소스를 둔다.
  현재는 `Sprites` 와 `GeneratedGameDataManifest.asset` 이 여기에 있다.
- `Assets/Docs`
  구현 기준 문서와 작업 규칙 문서를 둔다.
- `Assets/Settings`
  URP 설정과 씬 템플릿처럼 프로젝트 공용 설정 자산을 둔다.
- `Assets/TextMesh Pro`
  TMP 전역 설정과 패키지 자산을 둔다.
- `Assets/Design`
  런타임이 직접 참조하지 않는 디자인 원본 보관용 예약 폴더로 본다.
  현재 저장소 기준으로는 적극적으로 사용되고 있지 않으므로, 새 런타임 의존성은 여기로 연결하지 않는다.

## 4. 런타임 스크립트 구조

### 공통 시스템

- `Assets/Scripts/Core`
  `GameManager` 같은 전역 조합 루트를 둔다.
- `Assets/Scripts/Flow`
  날짜, 단계, 가이드 문구처럼 하루 흐름을 관리한다.
- `Assets/Scripts/Interaction`
  상호작용 인터페이스와 감지기를 둔다.
- `Assets/Scripts/World`
  포탈, 스폰 포인트, 위험 구역, 런타임 씬 보강처럼 씬 횡단 성격의 월드 로직을 둔다.

### 플레이어와 카메라

- `Assets/Scripts/Player`
  입력, 이동, 시각 표현, 이동 제한을 둔다.
  플레이어 시각 루트와 물리 루트는 필요할 때 분리한다.
- `Assets/Scripts/Camera`
  카메라 추적만 둔다.
  폴더 이름 충돌 때문에 네임스페이스는 `GameCamera` 를 사용한다.

### 기능별 시스템

- `Assets/Scripts/Data`
  `ResourceData`, `RecipeData`, 생성 데이터 로케이터와 매니페스트처럼 데이터 정의와 데이터 접근 보조 타입을 둔다.
- `Assets/Scripts/Inventory`
  인벤토리 런타임 로직을 둔다.
- `Assets/Scripts/Storage`
  창고 매니저와 창고 스테이션을 둔다.
- `Assets/Scripts/Restaurant`
  레시피 선택, 영업 실행, 영업대 상호작용을 둔다.
- `Assets/Scripts/Upgrade`
  업그레이드 비용, 해금, 작업대 상호작용을 둔다.
- `Assets/Scripts/Tools`
  도구 타입과 도구 해금 상태를 둔다.
- `Assets/Scripts/Gathering`
  채집 오브젝트를 둔다.
- `Assets/Scripts/Economy`
  골드와 평판을 둔다.

### UI

- `Assets/Scripts/UI/UIManager.cs`
  런타임 UI 진입점이다.
- `Assets/Scripts/UI/Controllers`
  편집 모드 프리뷰를 제어하는 보조 컨트롤러를 둔다.
- `Assets/Scripts/UI/Content`
  팝업 제목, 캡션, 예시 문구처럼 정적 UI 문구 카탈로그를 둔다.
- `Assets/Scripts/UI/Layout`
  공용 레이아웃 상수와 `PrototypeUILayout` partial 파일들을 둔다.
- `Assets/Scripts/UI/Style`
  테마, 리소스 경로 매핑, 스킨 적용 로직을 둔다.

## 5. 에디터 코드 구조

- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
  생성 에셋과 씬을 만드는 기본 빌더다.
- `Assets/Editor/PrototypeSceneAudit.cs`
  생성된 씬 구조가 기준과 맞는지 검사한다.
- `Assets/Editor/UI`
  `UIManager`, `PrototypeUIDesignController` 전용 커스텀 인스펙터를 둔다.

에디터 코드는 `ProjectEditor` 또는 `ProjectEditor.UI` 네임스페이스를 유지한다.

## 6. 배치 규칙

### 새 스크립트 추가

- 런타임 스크립트는 반드시 `Assets/Scripts` 아래 역할에 맞는 기존 폴더에 둔다.
- 에디터 전용 스크립트는 반드시 `Assets/Editor` 아래에 둔다.
- 특정 기능의 월드 상호작용 오브젝트는 해당 기능 폴더를 우선 사용한다.
  예: 창고 스테이션은 `Storage`, 레시피 선택대는 `Restaurant`, 작업대는 `Upgrade`.
- 여러 기능이 공유하는 포탈, 스폰, 위험 구역, 런타임 씬 보강은 `World` 에 둔다.

### 새 데이터 추가

- 데이터 타입 정의는 `Assets/Scripts/Data` 에 둔다.
- 빌더가 생성하는 데이터 자산은 `Assets/Generated/GameData` 를 기본 경로로 둔다.
- 런타임에서 `Resources.Load` 로 찾아야 하는 데이터 참조가 있으면 `Assets/Resources/Generated` 와 매니페스트 경로도 함께 맞춘다.

### 새 UI 추가

- 화면 전체 상태 갱신과 버튼 연결은 `UIManager` 에서 조율한다.
- 정적 문구 카탈로그는 `UI/Content` 로 보낸다.
- 좌표, 앵커, 카드 박스 규칙은 `UI/Layout` 로 보낸다.
- 스프라이트 사양, 리소스 경로, 스킨 적용은 `UI/Style` 로 보낸다.
- 편집 모드 프리뷰와 인스펙터 도구는 `Assets/Editor/UI` 에 둔다.

## 7. 네임스페이스 규칙

- 기본 규칙은 폴더 기반 네임스페이스다.
  예: `Assets/Scripts/Inventory` 는 `Inventory`, `Assets/Scripts/UI/Style` 은 `UI.Style`.
- 충돌 예외는 명시적으로 유지한다.
  `Assets/Scripts/Camera` 는 `GameCamera`, `Assets/Editor` 는 `ProjectEditor`.
- partial 타입은 같은 부모 폴더와 같은 네임스페이스를 유지한다.
  현재 `PrototypeUILayout`, `PrototypeUISkinCatalog` 가 이 규칙을 따른다.
- 기존 `MonoBehaviour`, `ScriptableObject`, 직렬화 타입을 네임스페이스로 옮길 때는 `MovedFrom` 을 유지한다.

## 8. 생성 경로 규칙

- 빌더가 만드는 씬이나 에셋은 결과물만 직접 수정하지 않는다.
  먼저 `Assets/Editor/JongguMinimalPrototypeBuilder.cs` 와 관련 감사 코드, 참조 경로를 함께 수정한다.
- `Assets/Generated` 는 생성 원본 자산 경로, `Assets/Resources/Generated` 는 런타임 로딩 경로라는 차이를 유지한다.
- 생성 폰트와 소스 폰트 파일명은 `Assets/Generated/Fonts` 아래에서 lower camelCase 를 유지한다.
- 런타임 코드가 `Resources.Load` 를 사용하면, 문서에 적힌 경로와 실제 폴더가 일치하는지 같이 확인한다.
- 현재 UI 스킨 코드는 `Generated/UI/Vector` 리소스 경로를 기대한다.
  이 경로를 다시 사용할 때는 실제 폴더 생성, 빌더/리소스 배치, 관련 문서를 한 번에 맞춘다.

## 9. 작업 체크리스트

### 게임플레이 기능 변경

- 기능 폴더 배치가 맞는가
- `GameManager` 또는 관련 매니저 참조가 필요한가
- 데이터 자산이 필요하면 `Assets/Generated/GameData` 와 문서도 같이 갱신했는가
- 씬 배치까지 바뀌면 `World`, `Scenes`, 빌더 코드를 같이 확인했는가

### UI 변경

- `Assets/Scripts/UI/UIManager.cs` 를 확인했는가
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs` 를 같이 확인했는가
- 필요하면 `Assets/Scripts/UI/Layout`, `Assets/Scripts/UI/Style`, `Assets/Docs/UI_GROUPING_RULES_KO.md` 를 같이 갱신했는가
- 허브 팝업이면 열릴 때 일시정지, 닫힐 때 시간 복구가 유지되는가

### 씬/생성 구조 변경

- `Assets/Scenes` 결과만 고치지 않고 빌더를 같이 수정했는가
- `Assets/Editor/PrototypeSceneAudit.cs` 기준도 같이 맞췄는가
- 직렬화 경로, `using`, 네임스페이스, 리소스 경로가 모두 같은 기준으로 맞는가

## 10. 현재 기준 메모

- 현재 저장소에서 `Assets/Resources/Generated` 는 `Sprites` 와 `GeneratedGameDataManifest.asset` 중심으로 쓰고 있다.
- UI 문서 일부는 `Assets/Resources/Generated/UI/Vector` 경로를 기준으로 설명하지만, 현재 저장소에서는 실제 폴더 존재 여부를 먼저 확인해야 한다.
- Unity 실행과 컴파일 검증은 이 작업에서 직접 확인하지 못했으므로, 실제 적용 후에는 씬 열기와 플레이 모드 기준 검증이 필요하다.
