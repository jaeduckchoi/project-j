---
적용: 항상
---

# 종구의 식당 작업 규칙

## 1. 프로젝트 개요

- 이 저장소는 2D 탑다운 탐험과 식당 운영을 결합한 Unity 프로토타입이다.
- 핵심 루프는 `허브 상태 확인 -> 탐험 -> 재료 수집 -> 허브 복귀 -> 메뉴 선택 -> 간단한 영업 진행 -> 결과 정산 -> 성장 -> 다음 날 진행`이다.
- 전체적인 체감 비중은 `탐험 8 : 식당 2`에 가깝게 유지한다.
- 명시적 요청이 없는 한 사용자 대상 응답과 문서는 기본적으로 한국어로 작성한다.

## 2. 작업 전에 확인할 문서

- `project/GAME_DOCS_INDEX.md`
- `project/GAME_PROJECT_STRUCTURE.md`
- `gameplay/GAME_FEATURE_REFERENCE.md`
- `gameplay/GAMEPLAY_CORE_LOOP.md`
- `gameplay/GAMEPLAY_EXPLORATION.md`
- `gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md`
- `ui/UI_AND_TEXT_GUIDE.md`
- `ui/UI_GROUPING_RULES.md`
- `scene/GAME_SCENE_AND_SETUP.md`
- `scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- `build/GAME_BUILD_GUIDE.md`

## 3. 프로토타입 범위

- 기본 플레이어 조작은 `WASD` 또는 방향키 이동과 `E` 상호작용이다.
- 클릭 이동, 장비 교체 UI, 버리기 시스템, 복잡한 실시간 식당 운영은 기본 프로토타입 범위에서 제외한다.
- 인벤토리는 재료 전용 슬롯 구조를 유지하고 업그레이드를 통해 `8 -> 12 -> 16`칸으로 확장한다.
- 도구는 인벤토리 슬롯을 차지하지 않는 영구 해금 아이템으로 유지한다.
- 창고는 허브 전용의 단순한 보관 시스템으로 유지한다.
- 업그레이드는 데이터 중심 구조를 유지하고 `골드 + 특정 재료`를 함께 소모한다.
- 기본 탐험 지역 순서는 `Beach -> DeepForest -> AbandonedMine -> WindHill`이다.

## 4. 구현 구조 규칙

- 시스템은 책임 기준으로 분리한다. 예시 타입은 `PlayerController`, `InteractionDetector`, `IInteractable`, `InventoryManager`, `StorageManager`, `RestaurantManager`, `EconomyManager`, `UpgradeManager`, `ScenePortal`, `DayCycleManager`, `UIManager`가 있다.
- 공용 작업 기준은 `.aiassistant/rules/{project|gameplay|ui|scene|build}` 아래에 두고 런타임 에셋 경로와 분리한다.
- 런타임 코드는 `Assets/Scripts`, 에디터 전용 코드는 `Assets/Editor`, 플레이 가능한 씬은 `Assets/Scenes`, 생성 자산은 `Assets/Resources/Generated`를 기준으로 관리하고 `Resources.Load` 경로와 실제 폴더 구조를 일치시킨다.
- 런타임 코드는 기능 기준으로 `Assets/Scripts/CoreLoop`, `Assets/Scripts/Exploration`, `Assets/Scripts/Management`, `Assets/Scripts/Restaurant`, `Assets/Scripts/UI`, `Assets/Scripts/Shared` 아래에 배치한다.
- 지역 공통 로직이나 런타임 보강 로직은 `Assets/Scripts/Exploration/World` 아래에 둔다.
- 생성 게임 데이터는 `Assets/Resources/Generated/GameData/Resources`, `Assets/Resources/Generated/GameData/Recipes`, `Assets/Resources/Generated/GameData/Input` 아래에 역할별로 유지한다.
- UI 역할 분리는 `UIManager`, `UI/Controllers`, `UI/Content`, `UI/Layout`, `UI/Style` 기준으로 유지한다.
- 가능하면 `ScriptableObject` 같은 데이터 우선 구조를 선호한다.
- Unity 직렬화 파일과 에셋 참조는 영향 범위가 크므로 경로와 참조 링크를 함께 확인한다.
- 생성된 씬 YAML, 생성 자산, 런타임 출력물만 직접 고치지 않는다. 먼저 생성 경로를 수정한다.
- 생성 구조를 바꿀 때는 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, `Assets/Editor/PrototypeSceneAudit.cs`, 관련 문서, 생성 리소스 경로를 함께 맞춘다.
- 허브 월드 아트를 교체할 때는 `Assets/Design` 원본, `Assets/Resources/Generated/Sprites/Hub`, `Assets/Scripts/Exploration/World/HubRoomLayout.cs`, `Assets/Scripts/Exploration/World/PrototypeSceneRuntimeAugmenter.cs`, `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, 지원 씬 직렬화를 같은 기준으로 갱신한다.
- `HubWallBackground`와 `HubFrontOutline`는 별도 생성 자산이므로 PNG만 직접 수정하기보다 `JongguMinimalPrototypeBuilder`의 생성 규칙과 리소스 연결 기준을 먼저 확인한다.
- UI를 바꿀 때는 `Assets/Scripts/UI/UIManager.cs`와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 검토한다.

## 5. 네임스페이스와 네이밍 규칙

- 런타임 스크립트와 에디터 스크립트는 폴더 기준 네임스페이스를 따른다.
- `Camera`, `Editor`처럼 Unity 또는 .NET 타입과 충돌하는 폴더명은 `GameCamera`, `ProjectEditor` 같은 예외 네임스페이스를 사용한다.
- partial 타입 보조 파일은 부모 타입과 같은 네임스페이스를 유지하는 폴더에 둔다.
- 기존 `MonoBehaviour`, `ScriptableObject`, 직렬화 타입을 네임스페이스로 옮길 때는 `UnityEngine.Scripting.APIUpdating.MovedFrom`으로 직렬화 경로를 보존한다.
- 기본 private 필드 네이밍 규칙은 다음과 같다.
  - `[SerializeField] private` : lower camelCase
  - 일반 `private`, `private static` : `_camelCase`
  - `private static readonly`, `private const` : PascalCase
- `.editorconfig`에서는 `Unity serialized field` 규칙이 일반 `Instance fields (private)` 규칙보다 먼저 적용되도록 유지한다.

## 6. UI, 빌더, 감사 규칙

- 허브 팝업 UI(`Cooking Menu`, `Upgrade`, `Materials`, `Storage`)가 열리면 게임 진행을 멈추고, 닫히면 이전 시간 흐름을 복구해야 한다.
- 팝업 일시정지 규칙은 `PopupPauseStateUtility`와 `UIManager`가 함께 공유하므로 한쪽만 바꾸지 않는다.
- 레거시 버튼, 오래된 도크, 사용하지 않는 카드처럼 중복 UI 경로는 제거한다.
- 명시적 요청이 없는 한 허브 팝업에서 씬에 직접 지정한 `Image.sprite`, `PopupTitle`, `PopupLeftCaption`의 폰트와 배치 값은 초기화하거나 덮어쓰지 않는다.
- 공용 Canvas 루트 이름은 `HUDRoot`와 `PopupRoot`를 유지한다.
- 지원하는 Canvas 씬의 공용 HUD 기준은 `Assets/Resources/Generated/ui-layout-overrides.asset`에 저장한다. 지원 씬 하나를 저장하면 같은 관리 대상 Canvas 변경이 다른 지원 씬 Canvas에도 전파되어야 한다.
- 지원 씬에 직접 저장한 월드 Transform, SpriteRenderer, 월드 TextMeshPro, 주요 게임플레이 컴포넌트 직렬화 값은 게임 런타임의 기본 정본으로 유지한다.
- 런타임 보강 코드는 누락된 오브젝트, 누락된 컴포넌트, 끊어진 참조만 보충하고, 씬에 이미 저장된 값은 기본적으로 덮어쓰지 않는다.
- `Tools > Jonggu Restaurant > Prototype Build and Audit`는 생성 자산, Build Settings, Canvas 동기화, 누락된 지원 씬 복구, 생성 씬 감사를 한 흐름으로 수행해야 한다.
- 별도 수동 씬 감사보다 빌드 흐름 안의 자동 감사를 우선한다.
- 지원하는 Canvas 씬 저장 시 `RectTransform`, 부모 그룹과 형제 순서, 삭제 상태, `Image.sprite/type/color/preserveAspect`, `TextMeshProUGUI`, `Button` 표시 값, `HUDActionGroup` 또는 `HUDPanelButtonGroup` 이름 오버라이드가 `Assets/Resources/Generated/ui-layout-overrides.asset`에 자동 저장되어야 한다.
- 빌더, 런타임 `UIManager`, 자동 감사 코드는 모두 같은 오버라이드 자산 기준을 사용해야 한다.
- 메인 빌드는 기존 지원 씬을 재생성하지 않으므로, 지원 씬에 직접 저장한 정적 값은 씬 직렬화를 정본으로 유지한다.
- 누락된 지원 씬을 복구할 때만 빌더가 같은 오브젝트 이름 기준의 안전한 직렬화 값과 씬 오브젝트 참조 재연결 규칙을 사용한다.
- 위 복구 규칙은 이름 기준이므로 지원 씬의 빌더 관리 오브젝트 이름이 바뀌면 빌더 코드와 감사 규칙도 함께 갱신한다.
- 지원 씬의 월드 계층은 `SceneWorldRoot`, `SceneGameplayRoot`, `SceneSystemRoot`, `Canvas` 구조에 맞춘다. 이 구조가 바뀌면 `PrototypeSceneHierarchyCatalog`, 정리기, 감사 로직을 함께 갱신한다.
- `Tools > Jonggu Restaurant` 메뉴는 기본적으로 `프로토타입 빌드 및 감사` 하나만 노출하고, 보조 유지보수 경로는 메뉴 대신 코드 내부 호출로 유지한다.
- 자주 깨지는 핵심 규칙은 `GameplayAutomationAudit` 경량 감사 코드가 다루므로, day-cycle 흐름, 포탈 잠금, 팝업 일시정지 규칙을 바꾸면 해당 감사도 함께 갱신한다.

### Canvas UI 복구 원칙

- Canvas UI를 과거 커밋 기준으로 복구할 때는 전체 씬을 통째로 되돌리지 말고 `Assets/Resources/Generated/ui-layout-overrides.asset`, 필요한 TMP 폰트 자산, 지원 씬의 `Canvas` 하위와 `UIManager` 직렬화만 기준 커밋에 맞춰 복구한다.
- `GuideHelpButton`, `ActionAccent`처럼 후속 커밋에서 추가되거나 제거된 관리 대상 UI는 코드 삭제보다 `ui-layout-overrides.asset`의 `removedObjectNames`, 씬 `Canvas` 직렬화, `UIManager` 직렬화를 함께 맞춰 복구하고, 빌더와 `UIManager`가 같은 제거 기준을 쓰는지 확인한다.
- Unity 씬 YAML을 수동으로 편집할 때는 `%YAML 1.1`, `%TAG !u! tag:unity3d.com,2011:` 헤더를 보존해야 하며, 복구 후에는 `Canvas` 바깥 직렬화가 바뀌지 않았는지 확인한다.

## 7. 주석과 문서 규칙

- 복잡한 로직의 현재 의도를 드러내는 데 도움이 되는 메서드 요약과 블록 주석은 유지한다.
- 새로 작성하거나 수정하는 코드 주석과 문서는 명시적 요청이 없는 한 UTF-8 한국어로 작성한다.
- 기존 영어 주석이나 문서를 수정할 때는 의미를 보존하되 현재 작업의 언어 정책에 맞춰 정리한다.
- 자명한 대입문이나 설명이 필요 없는 로직에는 장황한 주석을 추가하지 않는다.
- 중요한 메서드나 블록에 주석이 없다면 같은 작업 안에서 보강한다.
- 동작이 바뀌면 관련 문서도 함께 갱신한다.
- 새 기준이 규칙이 되면 `AGENTS.md`와 이 문서에 함께 반영한다.
- AI 작업 마무리 응답에서는 이번 변경에서 반복될 만한 훅 후보, 로컬 메모 후보, 공유 규칙 승격 후보가 있으면 짧게 제안한다.

## 8. 폰트와 에셋 규칙

- `Assets` 아래 에셋 파일명은 기본적으로 kebab-case를 사용한다. 단 `Assets/Resources/Generated/Fonts` 아래 생성 폰트 에셋과 원본 폰트 파일은 기존 lower camelCase 규칙을 유지한다.
- 기본 TMP 본문 폰트는 `Assets/Resources/Generated/Fonts/maplestoryLightSdf.asset`, 제목 폰트는 `Assets/Resources/Generated/Fonts/maplestoryBoldSdf.asset`를 유지하고, 빌더가 재생성할 수 있도록 원본 TTF 경로도 함께 맞춘다.
- `Assets/Design`는 디자인 원본 저장소 전용이며, 게임 런타임 리소스는 `Assets/Resources` 또는 생성 경로를 통해 참조해야 한다.
- 허브 월드 아트 원본은 `Assets/Design`에서 관리하고, 빌더가 `Assets/Resources/Generated/Sprites/Hub`를 기준으로 갱신하도록 유지한다.
- `HubWallBackground`와 `HubFrontOutline`는 `Assets/Resources/Generated/Sprites/Hub`에서 관리하는 생성 자산으로 유지한다.
- 허브 벽 아트 정렬이 어긋나면 결과 PNG만 덧그리지 말고 빌더의 타일 배치 값, 배경/전경 위치, 리소스 경로를 함께 조정한다.
- 허브 바닥 타일은 기본적으로 `1 월드 유닛 = 32 px` 밀도를 기준으로 맞추고, 타일 교체 시 `Pixels Per Unit`, `Transform.localScale`, `SpriteRenderer.size`를 같은 단위 기준으로 검토한다.
- 허브 카운터는 `HubBar` 루트 아래 `HubBarLeftVisual`, `HubBarRightVisual` 분리 비주얼 구조를 기준으로 관리하고, 파츠 교체 시 각 스프라이트의 소스 비율과 `spriteBorder`를 함께 조정한다.
- 파일명이 바뀌면 빌더 코드, 문서, TMP 참조 경로를 함께 갱신한다.
- 가능하면 에셋 네이밍 규칙은 빌더가 생성하는 이름 규칙과 일치시킨다.

## 9. 검증 규칙

- Unity 플레이 모드나 컴파일을 직접 검증하지 못했다면 그 사실을 명시한다.
- 자동 감사나 배치 컴파일이 있으면 결과를 함께 확인하고 보고한다.
- 생성 구조나 UI 기준을 바꿀 때는 가능하면 `Tools > Jonggu Restaurant > Prototype Build and Audit`로 먼저 검증하고, 필요하면 `GameplayAutomationAudit.RunLightAutomationAudit()` 같은 내부 자동 감사 경로도 함께 확인한다.
- 런타임 검증이 불가능하다면 어떤 파일, 좌표, 참조를 확인했는지 구체적으로 남긴다.
- 생성 구조, 네임스페이스, UI 기준을 바꿀 때는 저장된 씬, 관련 `using` 지시문, 빌더 코드, 감사 코드, 배치 컴파일 결과가 모두 서로 맞는지 확인한다.
- `commit-msg`는 커밋 형식 검증, `pre-commit`은 경고성 경로 연계 검사, `pre-push`는 Unity 경량 감사 같은 무거운 검증에 배치한다.

## 10. Git 커밋 메시지 규칙

### 기본 원칙

- 모든 Git 커밋 메시지는 한국어로 작성한다.
- 기본 형식은 `type : subject`이다.
- 영어 diff 요약, PR 제목, 자동 생성 초안을 받더라도 최종 커밋 메시지는 자연스러운 한국어로 다시 작성한다.
- 파일 경로, 코드 식별자, 브랜치명처럼 번역하면 안 되는 고유 명칭을 제외하면 영어 문장을 그대로 남기지 않는다.
- 제목만으로 충분하면 AI가 자동으로 붙인 영문 bullet 요약이나 changelog 본문은 삭제한다.
- `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE.md`를 로컬 `commit.template` 기준으로 사용하고, 규칙이 바뀌면 템플릿과 `AGENTS.md`, project rules 문서를 함께 갱신한다.

### 제목

- 제목은 50자 이내로 작성하고, 끝에 마침표를 붙이지 않는다.
- 제목은 변경 대상과 결과가 드러나게 작성한다.
- `수정`, `작업`, `정리`처럼 범위가 넓은 표현만 단독으로 쓰지 않는다.
- 한 커밋에는 가능하면 한 가지 의도를 담고, 제목도 그 의도를 기준으로 작성한다.

### 본문과 footer

- 제목만으로 충분하면 본문과 footer는 생략할 수 있다.
- 본문은 필요할 때만 작성하고, 제목 다음 빈 줄 아래에 한국어 짧은 문장 1~2줄로만 적는다.
- 본문에 bullet 목록이나 영어 요약 문장을 붙이지 않는다.
- footer는 이슈 번호, 후속 작업, 브레이킹 변경이 있을 때만 사용한다.

### `type` 선택 기준

| type | 의미 |
| --- | --- |
| `feat` | 새로운 기능 추가 |
| `update` | 기존 기능 조정 |
| `fix` | 버그 수정 |
| `docs` | 문서 또는 주석 수정 |
| `design` | UI 또는 시각 표현 변경 |
| `style` | 동작 변화 없는 형식 수정 |
| `rename` | 파일 또는 식별자 이름 변경 |
| `delete` | 불필요한 파일 또는 코드 삭제 |
| `refactor` | 동작 유지 전제의 구조 개선 |
| `test` | 테스트 추가 또는 개선 |
| `chore` | 빌드, 설정, import 정리 등 유지보수 작업 |

### 형식

```text
type : subject

본문

footer
```

### squash merge

- `[squash] branch-name`


