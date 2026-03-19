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
