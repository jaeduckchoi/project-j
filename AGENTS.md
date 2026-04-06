# Agent Entry

이 저장소의 공통 작업 기준 문서:

- `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`

작업 전에 우선 확인할 문서:

1. `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`
2. `.aiassistant/rules/project/GAME_DOCS_INDEX.md`
3. `.aiassistant/rules/project/GAME_PROJECT_STRUCTURE.md`
4. `.aiassistant/rules/gameplay/GAME_FEATURE_REFERENCE.md`
5. `.aiassistant/rules/ui/UI_AND_TEXT_GUIDE.md`
6. `.aiassistant/rules/scene/GAME_SCENE_AND_SETUP.md`
7. `.aiassistant/rules/build/GAME_BUILD_GUIDE.md`

응답 및 작업 원칙:

- 기본 응답 언어는 한국어를 우선한다.
- Unity 직렬화 파일과 에셋 참조는 영향 범위가 크므로 참조 경로까지 함께 확인한다.
- UI를 바꾸면 `Assets/Scripts/UI/UIManager.cs`와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 확인한다.
- 빌더가 생성하는 씬, 프리팹, 생성 에셋을 바꿀 때는 결과물만 직접 고치지 말고 생성 경로를 먼저 고친다.
- Unity 실행 또는 컴파일을 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 명시한다.

프로젝트 추가 규칙:

- 플레이어 비주얼은 맵 스케일에 맞게 유지하고, 필요하면 물리 루트와 비주얼 루트를 분리한다.
- 런타임 코드는 Assets/Scripts/CoreLoop, Assets/Scripts/Exploration, Assets/Scripts/Management, Assets/Scripts/Restaurant, Assets/Scripts/UI, Assets/Scripts/Shared 기능 루트를 기준으로 정리하고, 생성 데이터는 Assets/Generated/GameData/{Resources,Recipes,Input} 기준으로 배치한다.
- 런타임 스크립트와 에디터 스크립트는 폴더 기준 네임스페이스를 따른다.
- `Camera`, `Editor`처럼 Unity 또는 .NET 주요 타입과 충돌하는 폴더명은 `GameCamera`, `ProjectEditor` 같은 예외 네임스페이스를 사용한다.
- partial 타입 보조 파일은 부모 타입과 같은 네임스페이스를 유지하는 폴더에 둔다.
- 기존 `MonoBehaviour`, `ScriptableObject`, 직렬화 타입을 네임스페이스로 옮길 때는 `UnityEngine.Scripting.APIUpdating.MovedFrom`으로 직렬화 경로를 보존한다.
- private 필드 네이밍은 `[SerializeField] private`는 lower camelCase, 일반 `private`와 `private static`은 `_camelCase`, `private static readonly`와 `private const`는 PascalCase를 기본으로 한다.
- `.editorconfig`의 Rider/Unity 네이밍 규칙은 `Unity serialized field`가 일반 `Instance fields (private)`보다 먼저 적용되게 유지한다.
- 허브 팝업 UI(`요리 메뉴`, `업그레이드`, `재료`, `창고`)가 열리면 게임 진행을 일시 정지하고, 닫히면 원래 시간 흐름을 복구한다.
- 레거시 버튼, 오래된 도크, 사용하지 않는 카드처럼 중복 UI 경로가 남지 않게 정리한다.
- 허브 팝업에서 씬에 직접 지정한 `Image.sprite`, `PopupTitle`, `PopupLeftCaption`의 폰트와 배치 값은 명시적 요청 없이는 초기화하거나 덮어쓰지 않는다.
- Canvas 내부 공용 루트 이름은 `HUDRoot`, `PopupRoot`를 기준으로 유지한다.
- 지원하는 Canvas 씬 공용 HUD 기준은 `Assets/Resources/Generated/ui-layout-overrides.asset`에 저장한 관리 대상 UI 오버라이드 값이다. 지원 씬 중 하나를 저장하면 같은 관리 대상 Canvas 변경이 `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` 씬 Canvas에도 함께 반영된다.
- 지원하는 Canvas 씬을 저장하면 현재 씬 Canvas 아래 UI의 `RectTransform`, 부모 그룹/형제 순서, 삭제 상태, `Image.sprite/type/color/preserveAspect`, `TextMeshProUGUI`, `Button` 표시 값이 `Assets/Resources/Generated/ui-layout-overrides.asset`에 자동 저장된다.
- `프로토타입 빌드 및 감사`는 레이아웃과 표시 값은 `Hub` 기준을 우선 사용하고, `HUDActionGroup`, `HUDPanelButtonGroup` 이름과 해당 그룹 하위 UI 값은 현재 열려 있는 씬 기준으로 마지막에 다시 동기화한다.
- 빌더, 런타임 `UIManager`, 자동 감사 코드는 위 오버라이드 자산을 같은 기준으로 사용해야 한다.
- 지원 씬에서 빌더가 관리하는 오브젝트 값을 직접 조정한 경우 빌더는 같은 오브젝트 이름 기준으로 `Transform`, 활성 상태, `SpriteRenderer`, 월드 `TextMeshPro`, `Collider2D`, `Camera`, 포털·지대·채집·스테이션·매니저의 안전한 직렬화 값만 다시 적용하고, 씬 오브젝트 참조는 빌더가 재연결해야 한다.
- 위 오브젝트 값 동기화는 이름 기준이므로 지원 씬의 빌더 관리 오브젝트 이름을 바꾸면 빌더 코드와 감사 규칙도 함께 갱신한다.
- 생성 구조, UI 기준, 네임스페이스를 바꿀 때는 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, `Assets/Editor/PrototypeSceneAudit.cs`, 관련 문서, 배치 컴파일 결과를 함께 맞춘다.
- `Tools > Jonggu Restaurant` 아래 새 메뉴를 추가하거나 바꿀 때는 한국어 표시를 기본으로 하고, 반복 실행이 잦은 빌드 기능보다 유지보수 도구가 아래에 오도록 `MenuItem` priority를 함께 조정한다.
- 생성 씬 감사는 별도 수동 흐름보다 `프로토타입 빌드 및 감사` 안에서 자동으로 수행하는 기본 흐름을 우선한다.
- 게임플레이나 UI를 바꿀 때는 현재 동작 기준이 드러나도록 메서드와 블록 주석을 유지하고, 관련 파일에 무주석 핵심 메서드나 블록이 있으면 함께 보강한다.
- 새로 추가하거나 수정하는 코드 주석과 문서는 UTF-8 한글 기준으로 작성하고, 기존 영어 주석을 손볼 때도 한글로 통일한다.
- Git 커밋 메시지는 한국어로 작성하고 `type : subject` 형식을 따른다.
- 제목은 50자 이내로 작성하고, 끝에 마침표를 붙이지 않는다.
- 제목은 변경 대상과 결과가 드러나게 쓰고, 제목만으로 충분하면 본문은 생략한다.
- 본문은 필요할 때만 변경 이유와 핵심 내용을 짧게 적고, footer는 이슈 번호, 후속 작업, 브레이킹 변경이 있을 때만 사용한다.
- `type` 선택 기준과 템플릿은 `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`, `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE.md`를 따른다.
- 규칙을 바꾸면 `AGENTS.md`, project rules 문서, 템플릿을 함께 갱신한다.
- 영어 diff 요약, PR 제목, 자동 생성 커밋 초안이 들어와도 최종 커밋 메시지는 자연스러운 한글로 다시 작성한다.
- 파일 경로, 코드 식별자, 브랜치명처럼 번역하면 안 되는 고유 명칭을 제외하면 영문 문장을 제목이나 본문에 그대로 쓰지 않는다.
- squash merge 커밋 메시지는 `[squash] 브랜치명` 형식을 따른다.
- `Assets` 아래 자산 파일명은 기본적으로 kebab-case를 사용한다. 단 `Assets/Generated/Fonts` 아래 생성 폰트 에셋과 원본 폰트 파일명은 기존 규칙대로 lower camelCase를 유지한다.
- Canvas UI를 과거 커밋 기준으로 복구할 때는 전체 씬을 되돌리지 말고 `Assets/Resources/Generated/ui-layout-overrides.asset`, 필요한 경우 `Assets/Generated/Fonts/maplestoryLightSdf.asset`, `Assets/Generated/Fonts/maplestoryBoldSdf.asset`, 지원 씬의 `Canvas` 하위와 `UIManager` 직렬화만 기준 커밋에 맞춘다.
- `GuideHelpButton`, `ActionAccent`처럼 시점에 따라 있고 없을 수 있는 관리 대상 UI는 코드 삭제보다 `ui-layout-overrides.asset`의 `removedObjectNames`, 씬 `Canvas` 직렬화, `UIManager` 직렬화를 함께 맞춰 복구하고, 빌더와 `UIManager`가 같은 제거 기준을 쓰는지 확인한다.
- Unity 씬 YAML을 직접 다룰 때는 `%YAML 1.1`, `%TAG !u! tag:unity3d.com,2011:` 헤더를 반드시 보존하고, 복구 후에는 대상 씬에서 `Canvas` 바깥 직렬화가 바뀌지 않았는지 확인한다.
