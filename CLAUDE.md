# Claude Entry

이 파일은 Claude가 매 세션 처음 받는 짧은 온보딩 맵이다. 모델은 세션 간 프로젝트 상태를 기억하지 않으므로, 여기에는 항상 유효한 WHAT/WHY/HOW만 둔다. 세부 규칙은 아래 문서로 점진 공개한다.

## WHAT

- Unity 기반 2D 탑다운 식당 운영 프로토타입이다.
- 핵심 루트는 `Assets`(런타임/에디터/씬/리소스), `Docs`(정본 문서), `.aiassistant`(에이전트 규칙 허브)다.
- 구조와 경로의 정본은 `Docs/project/GAME_PROJECT_STRUCTURE.md`다.

## WHY

- 플레이어는 탐험으로 재료를 모으고 식당 허브에서 메뉴 선정, 조리, 서빙, 성장 루프를 진행한다.
- 주요 플레이 계약은 `FrontCounter -> BackCounter -> FrontCounter` 조리 흐름과 `Open/Close` 식당 상태다.
- 게임 의도는 `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`에서 확인한다.

## HOW

1. `.aiassistant/rules/README.md`를 읽어 규칙 허브와 작업별 진입점을 확인한다.
2. `Docs/project/GAME_ASSISTANT_RULES.md`를 읽어 전역 가드레일과 읽기 제외 범위를 확인한다.
3. `Docs/project/GAME_DOCS_INDEX.md`에서 현재 작업에 맞는 정본 문서 1~2개만 추가로 읽는다.
4. 코드, 씬, generated 자산을 바꿀 때는 `Docs/project/SOURCE_OF_TRUTH.md`의 정본 관계를 먼저 따른다.

## Always

- 기본 응답 언어는 한국어다.
- generated 결과물만 직접 고치지 말고 생성 경로 또는 정본 코드부터 수정한다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 최종 결과에 남은 검증 단계를 적는다.
- 커밋 메시지 규칙은 `Docs/project/GIT_COMMIT_TEMPLATE.md`만 따른다.

## Claude Harness

- Claude 설정은 `.claude/settings.json`(팀 공유)과 `.claude/settings.local.json`(개인 전용)으로 관리한다.
- 읽기 범위와 제외 기준의 단일 정본은 `Docs/project/GAME_ASSISTANT_RULES.md`다. 이 목록을 바꾸면 `.claude/settings.json`도 같은 변경에서 맞춘다.
