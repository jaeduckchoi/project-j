# Claude Entry

이 파일은 Claude가 매 세션 처음 받는 짧은 온보딩 맵이다. 모델은 세션 간 프로젝트 상태를 기억하지 않으므로, 여기에는 항상 유효한 WHAT/WHY/HOW만 둔다. 작업 하네스는 [Docs/README.md](Docs/README.md)가 맡고, 세부 규칙은 하위 정본 문서로 점진 공개한다.

## WHAT

- Unity 기반 2D 탑다운 식당 운영 프로토타입이다.
- 핵심 루트는 `Assets`(런타임/에디터/씬/리소스), `Docs`(정본 문서와 작업 하네스), `Skills`(에이전트 워크플로 자산)다.
- 구조와 경로의 정본은 [Docs/Project/GAME_PROJECT_STRUCTURE.md](Docs/Project/GAME_PROJECT_STRUCTURE.md)다.

## WHY

- 플레이어는 탐험으로 재료를 모으고 식당 허브에서 메뉴 선정, 조리, 서빙, 성장 루프를 진행한다.
- 주요 플레이 계약은 `FrontCounter -> BackCounter -> FrontCounter` 조리 흐름과 `Open/Close` 식당 상태다.
- 게임 의도는 [Docs/Gameplay/GAME_DESIGN_OVERVIEW.md](Docs/Gameplay/GAME_DESIGN_OVERVIEW.md)에서 확인한다.

## HOW

1. [Docs/README.md](Docs/README.md)를 읽어 작업 하네스, 문서 계층, 작업별 시작점을 확인한다.
2. [Docs/Project/GAME_ASSISTANT_RULES.md](Docs/Project/GAME_ASSISTANT_RULES.md)를 읽어 전역 가드레일과 읽기 제외 범위를 확인한다.
3. [Docs/Project/GAME_DOCS_INDEX.md](Docs/Project/GAME_DOCS_INDEX.md)에서 현재 작업에 맞는 정본 문서 1~2개만 추가로 읽는다.
4. 코드, 씬, generated 자산을 바꿀 때는 [Docs/Project/SOURCE_OF_TRUTH.md](Docs/Project/SOURCE_OF_TRUTH.md)의 정본 관계를 먼저 따른다.
5. 스킬을 수정하거나 배포 흐름을 바꾸는 작업은 루트 [Skills](Skills)와 문서 하네스 정본을 함께 확인한다.

## Always

- 기본 응답 언어는 한국어다.
- 루트 엔트리에는 상세 규칙을 복제하지 않고 `Docs/README.md`, `GAME_ASSISTANT_RULES.md`, `GAME_DOCS_INDEX.md`, `SOURCE_OF_TRUTH.md`로 연결한다.
- generated 결과물만 직접 고치지 말고 생성 경로 또는 정본 코드부터 수정한다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 최종 결과에 남은 검증 단계를 적는다.
- `Skills/`는 에이전트 실행 보조 자산이며, 게임 규칙과 정본 관계는 `Docs/Project/*` 문서가 소유한다.

## Claude Harness

- Claude 설정은 `.claude/settings.json`(팀 공유)과 `.claude/settings.local.json`(개인 전용)으로 관리한다.
- 읽기 범위와 제외 기준의 단일 정본은 [Docs/Project/GAME_ASSISTANT_RULES.md](Docs/Project/GAME_ASSISTANT_RULES.md)다. 이 목록을 바꾸면 `.claude/settings.json`도 같은 변경에서 맞춘다.
