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
- 게임플레이나 UI를 바꿀 때는 현재 동작 기준이 드러나도록 메서드와 블록 주석을 유지한다.
- 새로 추가하거나 수정하는 주석과 문서는 UTF-8 한글 기준으로 작성한다.
- `Assets/Generated/Fonts` 아래 생성 폰트 에셋과 원본 폰트 파일명은 lower camelCase를 유지한다.
