# AI Assistant Rules

이 프로젝트의 공통 AI 작업 규칙은 아래 경로를 기준으로 관리한다.

- 공유 규칙: `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`
- 로컬 전용 규칙: `.aiassistant/rules/local/`

에이전트별 진입 파일은 루트에 둔다.

- Codex: `AGENTS.md`
- Claude: `CLAUDE.md`

권장 동작 순서:

1. 루트 진입 파일을 먼저 읽는다.
2. 그 안에서 안내하는 `.aiassistant/rules/project` 문서를 읽는다.
3. 필요하면 `.aiassistant/rules/local`의 로컬 규칙도 함께 읽는다.

추가 기준:

- `.aiassistant/rules/gameplay`, `.aiassistant/rules/ui`, `.aiassistant/rules/scene`, `.aiassistant/rules/build` 아래 기준 문서는 UTF-8 한국어 본문을 정본으로 유지한다.
- 후속 인덱스, 요약, 검토 문서는 영어 제목이 아니라 파일 경로와 코드 식별자를 기준으로 연결을 유지한다.
- 코드 식별자, 에셋 경로, 씬 이름, 메뉴 이름처럼 번역하면 안 되는 고유 명칭은 인라인 코드로 영문 표기를 유지한다.
- 커밋 메시지, 커밋 초안 정리, squash merge 제목 작성은 `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`의 Git 섹션을 우선 기준으로 따른다.
- 영문 diff 요약이나 자동 생성된 영문 커밋 초안이 있어도 최종 커밋 메시지는 한글로 다시 작성한다.
- 제목만으로 충분하면 한 줄 제목만 남기고, 영문 bullet 요약 본문은 추가하지 않는다.
- Git 커밋 템플릿은 `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE.md`를 기준으로 사용한다.
