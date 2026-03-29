# AI Assistant Rules

이 프로젝트의 공통 AI 작업 규칙은 아래 경로를 기준으로 관리한다.

- 공유 규칙: `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`
- 로컬 전용 규칙: `.aiassistant/rules/local/`

에이전트별 진입 파일은 루트에 둔다.

- Codex: `AGENTS.md`
- Claude: `CLAUDE.md`

권장 동작 순서:

1. 루트 진입 파일을 먼저 읽는다.
2. 그 안에서 안내하는 `.aiassistant/rules/project` 문서를 읽는다.
3. 필요하면 `.aiassistant/rules/local` 의 로컬 규칙도 함께 읽는다.

추가 기준:

- 커밋 메시지, 커밋 초안 정리, squash merge 제목 작성은 `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`의 Git 섹션을 우선 기준으로 따른다.
- 영문 diff 요약이나 자동 생성된 영문 커밋 초안이 있어도 최종 커밋 메시지는 한글로 다시 작성한다.
- Git 커밋 템플릿은 `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE_KO.md`를 기준으로 사용한다.