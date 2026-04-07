# Claude Entry

이 저장소의 공통 AI 작업 규칙은 아래 문서를 기준으로 한다.

- `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`

작업 전 우선 읽을 문서:

1. `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`
2. `.aiassistant/rules/project/GAME_DOCS_INDEX.md`
3. `.aiassistant/rules/project/GAME_PROJECT_STRUCTURE.md`
4. `.aiassistant/rules/gameplay/GAME_FEATURE_REFERENCE.md`
5. `.aiassistant/rules/ui/UI_AND_TEXT_GUIDE.md`
6. `.aiassistant/rules/scene/GAME_SCENE_AND_SETUP.md`
7. `.aiassistant/rules/build/GAME_BUILD_GUIDE.md`

핵심 규칙 요약:

- 기본 응답 언어는 한국어
- Unity 직렬화 필드명과 씬 참조는 신중하게 수정
- UI 변경 시 `Assets/Scripts/UI/UIManager.cs`와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`를 함께 확인
- 빌더가 생성하는 씬, 프리팹, 생성 에셋은 결과물만 직접 고치지 말고 생성 경로를 먼저 수정
- AI 코드 작업 마무리 응답에서는 필요하면 로컬 메모 후보와 공유 규칙 업데이트 후보를 짧게 제안
- 커밋 메시지는 한국어 `type : subject` 제목을 우선하고, 영문 bullet 요약 본문은 붙이지 않음
- Unity 실행 / 컴파일을 못 했으면 결과에 명시
