# Agent Entry

이 저장소의 공통 작업 기준 문서:

- `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`

작업 전에 우선 확인할 문서:

1. `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`
2. `Assets/Docs/GAME_FEATURE_REFERENCE_KO.md`
3. `Assets/Docs/UI_AND_TEXT_GUIDE_KO.md`
4. `Assets/Docs/GAME_SCENE_AND_SETUP_KO.md`

응답 및 작업 원칙:

- 기본 응답 언어는 한국어를 우선한다.
- Unity 직렬화 파일과 에셋 참조는 영향 범위가 크므로 참조 경로까지 함께 확인한다.
- UI를 바꾸면 `Assets/Scripts/UI/UIManager.cs`와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 확인한다.
- Unity 실행 또는 컴파일을 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 명시한다.

프로젝트 추가 규칙:

- 플레이어 비주얼은 맵 스케일에 맞게 유지하고, 필요하면 물리 루트와 비주얼 루트를 분리한다.
- 허브 팝업 UI(`요리 메뉴`, `업그레이드`, `재료`, `창고`)가 열리면 게임 진행을 일시 정지하고, 닫히면 원래 시간 흐름을 복구한다.
- 레거시 버튼, 오래된 도크, 사용하지 않는 카드처럼 중복 UI 경로가 남지 않게 정리한다.
- 새 런타임/에디터 스크립트는 폴더 경로 기준 네임스페이스를 맞추고, `Camera`나 `Editor`처럼 Unity/.NET 주요 타입과 충돌하는 폴더는 `GameCamera`, `ProjectEditor`처럼 충돌 없는 예외 네임스페이스를 사용한다.
- 기존 `MonoBehaviour`, `ScriptableObject`, 직렬화 가능한 타입을 네임스페이스로 이동할 때는 `UnityEngine.Scripting.APIUpdating.MovedFrom`으로 직렬화 경로를 보존한다.
- 네임스페이스나 생성 구조를 바꾸면 관련 `using`, `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, 자동 감사 코드, 배치 컴파일 결과를 함께 확인한다.
- private 필드 네이밍은 `[SerializeField] private`는 lower camelCase, 일반 `private`/`private static`은 `_camelCase`, `private static readonly`는 PascalCase를 기본으로 유지한다.
- 게임플레이나 UI를 바꿀 때는 현재 동작 기준이 드러나도록 메서드와 블록 주석을 유지하고, 관련 파일에 무주석 핵심 메서드나 블록이 있으면 함께 보강한다.
- 새로 추가하거나 수정하는 코드의 주석과 문서는 UTF-8 한글 기준으로 작성하고, 기존 영어 주석을 손볼 때도 한글로 통일한다.
- Git 커밋 메시지는 한글로 작성하고 `type : subject` 형식을 따른다.
- Git 커밋 메시지 상세 규칙은 `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`의 Git 섹션을 기준으로 유지하고, 규칙을 바꾸면 `AGENTS.md`와 project rules 문서를 함께 갱신한다.
- Git 커밋 템플릿 경로는 `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE_KO.md`를 기준으로 사용하고, 규칙을 바꾸면 템플릿과 문서를 함께 갱신한다.
- 커밋 제목은 50자 이내로 작성하고, 제목 끝에 마침표를 붙이지 않는다. 제목만으로 충분하면 본문은 생략한다.
- 영어 diff 요약, PR 제목, 자동 생성 커밋 초안이 들어와도 최종 커밋 메시지는 자연스러운 한글로 다시 작성한다.
- 파일 경로, 코드 식별자, 브랜치명처럼 번역하면 안 되는 고유 명칭을 제외하면 영문 문장을 제목이나 본문에 그대로 쓰지 않는다.
- `type`은 정의된 소문자 목록만 사용하고, 본문은 왜 바꿨는지와 무엇을 바꿨는지를 짧고 구체적으로 적는다. footer는 이슈 번호, 후속 작업, 브레이킹 변경이 있을 때만 적는다.
- squash merge 커밋 메시지는 `[squash] 브랜치명` 형식을 따른다.
- `Assets/Generated/Fonts` 아래 생성 폰트 에셋과 원본 폰트 파일명은 lower camelCase를 유지한다.
- 허브 팝업에서 씬에 직접 지정한 `Image.sprite`, `PopupTitle`, `PopupLeftCaption`의 폰트/배치 값은 명시적 요청 없이는 초기화하거나 다른 기준으로 덮어쓰지 않는다.
