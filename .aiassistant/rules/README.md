---
적용: 항상
---

# AI Assistant Rules

`.aiassistant`는 이 저장소의 작업 하네스다. 엔트리 파일은 짧은 맵 역할만 하고, 실제 규칙과 정본 관계는 `.aiassistant/rules` 아래 문서에 기록한다.

## 1. 문서 계층

- 루트 엔트리: `AGENTS.md`, `CLAUDE.md`
- 전역 규칙과 작업 흐름: `.aiassistant/rules/project/*`
- 게임 의도와 플레이 기준: `.aiassistant/rules/gameplay/*`
- UI, 씬, 빌더 정본: `.aiassistant/rules/ui/*`, `.aiassistant/rules/scene/*`, `.aiassistant/rules/build/*`

## 2. 최소 읽기 순서

1. 사용하는 에이전트의 루트 엔트리 파일을 읽는다.
2. `.aiassistant/rules/project/GAME_ASSISTANT_RULES.md`를 읽는다.
3. `.aiassistant/rules/project/GAME_DOCS_INDEX.md`에서 현재 작업에 맞는 정본 문서를 찾는다.
4. 필요한 경우에만 도메인 문서를 추가로 읽는다.

## 3. 작업별 시작점

- 규칙 체계, 문서 정리, 하네스 운영: `rules/project/GAME_ASSISTANT_RULES.md`, `rules/project/AGENT_WORKFLOW.md`
- 프로젝트 구조, 경로, 네임스페이스: `rules/project/GAME_PROJECT_STRUCTURE.md`
- 정본 관계와 함께 수정할 결합 지점: `rules/project/SOURCE_OF_TRUTH.md`
- UI 변경: `rules/ui/UI_AND_TEXT_GUIDE.md`, `rules/ui/UI_GROUPING_RULES.md`
- 씬/월드 변경: `rules/scene/GAME_SCENE_AND_SETUP.md`, `rules/scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- 빌더/생성 자산 변경: `rules/build/GAME_BUILD_GUIDE.md`
- 플레이 의도 확인: `rules/gameplay/GAME_DESIGN_OVERVIEW.md`와 관련 gameplay 문서

## 4. 하네스 운영 원칙

- 엔트리 파일은 백과사전이 아니라 맵이다.
- 규칙은 한 문서만 정본으로 두고 나머지는 링크로 연결한다.
- generated 결과물보다 생성 경로와 정본 관계를 먼저 수정한다.
- 작업이 끝나면 관련 문서와 오래된 경로, 중복 규칙, 드리프트를 함께 정리한다.
- Unity 실행이나 컴파일을 확인하지 못했다면 그 사실을 결과에 명시한다.

## 5. 언어 정책

- `AGENTS.md`, `CLAUDE.md`, `.aiassistant/rules/README.md`, `rules/project/*`, `rules/gameplay/*`는 한국어를 기본으로 유지한다.
- 코드 식별자, 경로, 씬 이름, 메뉴 이름처럼 번역하면 안 되는 고유 명칭은 원문 그대로 적는다.
- 커밋 메시지 규칙은 `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE.md`만 정본으로 사용한다.
