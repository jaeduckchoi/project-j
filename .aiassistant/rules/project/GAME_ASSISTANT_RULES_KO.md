---
적용: 항상
---

# Jonggu Restaurant 작업 규칙

## 1. 프로젝트 개요

- 이 저장소는 2D 탑다운 탐험과 식당 운영을 결합한 Unity 프로토타입이다.
- 핵심 루프는 `허브 상태 확인 -> 탐험 -> 재료 수집 -> 허브 복귀 -> 메뉴 선택 -> 간단한 서비스 -> 정산 -> 성장 -> 다음 날 진행`이다.
- 전체 체감 비중은 `탐험 8 : 식당 2`를 기본으로 유지한다.
- 사용자 응답과 문서화는 한국어를 기본으로 한다.

## 2. 작업 전 확인 문서

- `Assets/Docs/GAME_PROJECT_STRUCTURE_KO.md`
- `Assets/Docs/GAME_FEATURE_REFERENCE_KO.md`
- `Assets/Docs/GAMEPLAY_CORE_LOOP_KO.md`
- `Assets/Docs/GAMEPLAY_EXPLORATION_KO.md`
- `Assets/Docs/GAMEPLAY_RESTAURANT_AND_GROWTH_KO.md`
- `Assets/Docs/UI_AND_TEXT_GUIDE_KO.md`
- `Assets/Docs/GAME_SCENE_AND_SETUP_KO.md`
- `Assets/Docs/GAME_BUILD_GUIDE_KO.md`

## 3. 프로토타입 기본 범위

- 플레이어 조작은 `WASD` 또는 방향키 이동 + `E` 상호작용을 기본으로 한다.
- 클릭 이동, 장비 교체 UI, 버리기 시스템, 복잡한 실시간 식당 운영은 기본 범위에서 제외한다.
- 인벤토리는 재료 전용 슬롯 구조를 유지하고, 업그레이드로 `8 -> 12 -> 16` 슬롯까지 확장한다.
- 도구는 인벤토리를 차지하지 않는 영구 해금 구조를 유지한다.
- 창고는 허브 전용 단순 보관 구조를 유지한다.
- 업그레이드는 `gold + 특정 재료`를 함께 소비하는 데이터 기반 구조를 유지한다.
- 탐험 지역 기본 순서는 `Beach -> DeepForest -> AbandonedMine -> WindHill`이다.

## 4. 구현 구조 규칙

- 역할별 시스템 분리를 유지한다. 예시는 `PlayerController`, `InteractionDetector`, `IInteractable`, `InventoryManager`, `StorageManager`, `RestaurantManager`, `EconomyManager`, `UpgradeManager`, `ScenePortal`, `DayCycleManager`, `UIManager`다.
- `Assets/Scripts`는 런타임 코드, `Assets/Editor`는 에디터 전용 코드, `Assets/Scenes`는 플레이 가능한 씬, `Assets/Generated`는 빌더가 관리하는 생성 에셋, `Assets/Resources/Generated`는 `Resources.Load` 대상 생성 에셋, `Assets/Docs`는 기준 문서를 둔다.
- 새 기능은 해당 책임에 맞는 기존 폴더에 넣고, 여러 기능이 공유하는 지역 로직이나 보강 로직은 `Assets/Scripts/World`에 둔다.
- UI 코드는 `UIManager`, `UI/Controllers`, `UI/Content`, `UI/Layout`, `UI/Style` 역할 분리를 유지한다.
- ScriptableObject 등 데이터 우선 구조를 가능한 한 우선한다.
- Unity 직렬화 파일과 에셋 참조는 영향 범위가 크므로 경로와 참조 연결을 함께 확인한다.
- 빌더가 생성하는 씬 YAML, generated 에셋, 리소스 출력은 결과물만 직접 고치지 말고 생성 경로를 먼저 고친다.
- 생성 구조를 바꿀 때는 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, `Assets/Editor/PrototypeSceneAudit.cs`, 관련 문서, 생성 리소스 경로를 함께 갱신한다.
- UI 변경 시 `Assets/Scripts/UI/UIManager.cs`와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 확인한다.

## 5. 네임스페이스와 네이밍 규칙

- 런타임 스크립트와 에디터 스크립트는 폴더 기준 네임스페이스를 따른다.
- `Camera`, `Editor`처럼 Unity 또는 .NET 타입과 충돌하는 폴더명은 `GameCamera`, `ProjectEditor` 같은 예외 네임스페이스를 사용한다.
- partial 타입 보조 파일은 부모 타입과 같은 네임스페이스를 유지하는 폴더에 둔다.
- 기존 `MonoBehaviour`, `ScriptableObject`, 직렬화 타입을 네임스페이스로 옮길 때는 `UnityEngine.Scripting.APIUpdating.MovedFrom`을 사용한다.
- private 필드 네이밍 기본값은 다음과 같다.
  - `[SerializeField] private` : lower camelCase
  - 일반 `private`, `private static` : `_camelCase`
  - `private static readonly`, `private const` : PascalCase
- `.editorconfig`에서는 `Unity serialized field` 규칙이 일반 `Instance fields (private)`보다 먼저 적용되게 유지한다.

## 6. UI, 빌더, 감사 규칙

- 허브 팝업 UI(`요리 메뉴`, `업그레이드`, `재료`, `창고`)가 열리면 게임 진행을 일시 정지하고, 닫히면 원래 시간 흐름을 복구한다.
- 레거시 버튼, 오래된 도크, 사용하지 않는 카드처럼 중복 UI 경로를 남기지 않는다.
- 허브 팝업에서 씬에 직접 지정한 `Image.sprite`, `PopupTitle`, `PopupLeftCaption`의 폰트와 배치 값은 명시적 요청 없이는 초기화하거나 덮어쓰지 않는다.
- Canvas 공용 루트 이름은 `HUDRoot`, `PopupRoot`를 기준으로 유지한다.
- 탐험 씬 공용 HUD 기준은 `Hub` 씬 Canvas에서 같은 이름으로 저장한 UI 오버라이드 값이다.
- `Tools > Jonggu Restaurant > 프로토타입 빌드 및 감사`를 실행하면 다음을 한 번에 수행한다.
  - generated 에셋과 기본 씬 빌드
  - `Hub` 씬 Canvas 오버라이드를 먼저 동기화한 뒤 `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` UI를 다시 생성
  - 자동 생성 씬 감사 실행
- 생성 씬 감사는 별도 수동 루틴보다 빌드 흐름 안의 자동 감사 기준을 우선한다.
- 지원하는 Canvas 씬을 저장하면 현재 씬 Canvas 아래 UI의 `RectTransform`, `Image.sprite/type/color/preserveAspect`, `TextMeshProUGUI`, `Button` 표시 값과 `HUDActionGroup`, `HUDPanelButtonGroup` 이름 오버라이드가 `Assets/Resources/Generated/ui-layout-overrides.asset`에 자동 저장된다.
- 빌더, 런타임 `UIManager`, 자동 감사 코드는 위 오버라이드 자산을 같은 기준으로 사용해야 한다.
- 메뉴 추가 또는 변경 시 `Tools > Jonggu Restaurant` 아래 표시는 한국어를 기본으로 하고, 유지보수 도구는 빌드 도구보다 아래에 오도록 `MenuItem` priority를 조정한다.

## 7. 주석과 문서 규칙

- 복잡한 로직 앞에는 현재 동작 기준이 드러나는 메서드 설명과 블록 주석을 유지한다.
- 새로 추가하거나 수정하는 코드 주석과 문서는 UTF-8 한글 기준으로 작성한다.
- 기존 영어 주석이나 문서를 수정할 때는 의미를 유지한 채 한글로 정리한다.
- 명백한 대입이나 자명한 동작에는 장황한 주석을 달지 않는다.
- 중요한 무주석 메서드나 블록을 건드렸다면 같은 작업에서 함께 보강한다.
- 동작 기준이 바뀌면 관련 문서를 함께 갱신한다.
- 새 기준을 규칙으로 추가했다면 `AGENTS.md`와 이 문서에 모두 반영한다.

## 8. 폰트와 에셋 규칙

- `Assets` 아래 자산 파일명은 기본적으로 kebab-case를 사용한다. 단 `Assets/Generated/Fonts` 아래 생성 폰트 에셋과 원본 폰트 파일명은 기존 규칙대로 lower camelCase를 유지한다.
- `Assets/Design`는 디자인 원본 보관용이고, 실제 게임이 직접 참조하는 리소스는 `Assets/Resources` 또는 generated 경로로 연결한다.
- 파일명이 바뀌면 빌더 코드, 문서, TMP 참조 경로를 함께 갱신한다.
- 파일명 규칙은 가능하면 빌더가 생성하는 에셋 네이밍 규칙과 맞춘다.

## 9. 검증 규칙

- Unity 플레이 테스트나 컴파일을 직접 확인하지 못했으면 반드시 명시한다.
- 자동 감사나 배치 컴파일이 있으면 결과를 함께 확인하고 보고한다.
- 런타임 검증이 불가능하면 어떤 파일, 좌표, 참조를 기준으로 검토했는지 구체적으로 남긴다.
- 생성 구조, 네임스페이스, UI 기준을 바꿀 때는 저장된 씬, 관련 `using`, 빌더 코드, 감사 코드, 배치 컴파일 결과를 함께 맞춘다.

## 10. Git 커밋 메시지 규칙

- 모든 Git 커밋 메시지는 한글로 작성한다.
- 기본 형식은 `type : subject`다.
- 제목은 50자 이내로 작성하고 마침표로 끝내지 않는다.
- 제목만으로 충분하면 본문과 footer는 생략할 수 있다.
- 본문은 한 줄 공백 뒤에 짧고 구체적으로 작성한다.
- 영어 diff 요약, PR 제목, 자동 생성 초안이 있어도 최종 커밋 메시지는 자연스러운 한글로 다시 작성한다.
- 파일 경로, 코드 식별자, 브랜치명처럼 번역하면 안 되는 고유 명칭을 제외하면 영문 문장을 그대로 두지 않는다.
- footer는 이슈 번호, 후속 작업, 브레이킹 변경이 있을 때만 사용한다.
- squash merge 커밋 메시지는 `[squash] 브랜치명` 형식을 따른다.
- `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE_KO.md`를 로컬 `commit.template` 기준으로 사용하고, 규칙이 바뀌면 템플릿과 문서를 함께 갱신한다.

### 허용되는 `type`

- `feat` : 새 기능 추가
- `update` : 기존 기능 수정
- `fix` : 버그 수정
- `docs` : 문서 또는 주석 수정
- `design` : UI 디자인, CSS 성격 변경
- `style` : 동작 변화 없는 오탈자, 포맷, 세미콜론, 공백 정리
- `rename` : 파일 또는 식별자 이름 변경
- `delete` : 불필요한 파일 제거
- `refactor` : 구조 개선 중심 리팩터링
- `test` : 테스트 추가 또는 테스트 코드 보강
- `chore` : 빌드 설정, 프로젝트 설정, import 변경, 함수명 정리 같은 유지보수

### 형식

```text
type : subject

body

footer
```










