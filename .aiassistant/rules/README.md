---
적용: 항상
---

# AI Assistant Rules

`.aiassistant`는 이 저장소의 작업 하네스다. 엔트리 파일은 짧은 맵 역할만 하고, 실제 규칙과 정본 관계는 **프로젝트 최상위 `Docs` 디렉터리** 아래 문서에 기록한다.

## 1. 문서 계층

- 루트 엔트리: `AGENTS.md`, `CLAUDE.md`
- 전역 규칙과 작업 흐름: `Docs/project/*`
- 게임 의도와 플레이 기준: `Docs/gameplay/*`
- UI, 씬, 빌더 정본: `Docs/ui/*`, `Docs/scene/*`, `Docs/build/*`

## 2. 최소 읽기 순서

1. 사용하는 에이전트의 루트 엔트리 파일을 읽는다.
2. `Docs/project/GAME_ASSISTANT_RULES.md`를 읽는다.
3. `Docs/project/GAME_DOCS_INDEX.md`에서 현재 작업에 맞는 정본 문서를 찾는다.
4. 필요한 경우에만 도메인 문서를 추가로 읽는다.

## 3. 작업별 시작점

- 규칙 체계, 문서 정리, 하네스 운영: `Docs/project/GAME_ASSISTANT_RULES.md`, `Docs/project/AGENT_WORKFLOW.md`
- 프로젝트 구조, 경로, 네임스페이스: `Docs/project/GAME_PROJECT_STRUCTURE.md`
- 정본 관계와 함께 수정할 결합 지점: `Docs/project/SOURCE_OF_TRUTH.md`
- UI 변경: `Docs/ui/UI_AND_TEXT_GUIDE.md`, `Docs/ui/UI_GROUPING_RULES.md`
- UI 구조 리팩토링/탐색성 정리: `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/project/AGENT_WORKFLOW.md`, `Docs/ui/UI_AND_TEXT_GUIDE.md`
- 씬/월드 변경: `Docs/scene/GAME_SCENE_AND_SETUP.md`, `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- 빌더/생성 자산 변경: `Docs/build/GAME_BUILD_GUIDE.md`
- Unity API 연동: `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/project/AGENT_WORKFLOW.md`
- 플레이 의도 확인: `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`와 관련 gameplay 문서

## 4. 하네스 운영 원칙

- 엔트리 파일은 백과사전이 아니라 맵이다.
- 규칙은 한 문서만 정본으로 두고 나머지는 링크로 연결한다.
- generated 결과물보다 생성 경로와 정본 관계를 먼저 수정한다.
- 폴더 리팩토링은 파일 이동만으로 끝내지 말고 asmdef 범위, `.meta`, 직접 경로를 적은 문서까지 같은 변경에서 정리한다.
- partial family는 엔트리/상태 파일과 역할별 세부 파일을 구분해 배치하고, public 타입/네임스페이스 호환은 우선 유지한다.
- Windows에서 큰 신규 파일을 만들 때는 한 번의 긴 패치 대신, 파일 뼈대를 먼저 만든 뒤 여러 번의 작은 패치로 나눠 추가한다.
- 작업이 끝나면 관련 문서와 오래된 경로, 중복 규칙, 드리프트를 함께 정리한다.
- Unity 실행이나 컴파일을 확인하지 못했다면 그 사실을 결과에 명시한다.

## 5. 주석 기준

- 주석은 폴더/partial 경계, 책임 분리 이유, 정본 관계처럼 탐색 비용을 줄이는 곳에만 추가한다.
- 변수 대입이나 조건문 자체를 설명하는 주석은 피하고, "왜 이 파일/블록이 존재하는가"를 먼저 적는다.
- family로 나뉜 코드에서는 엔트리 파일이나 각 family의 진입 파일에 역할 주석을 두고, 반복되는 구현 세부에는 같은 설명을 복제하지 않는다.

## 6. 언어 정책

- `AGENTS.md`, `CLAUDE.md`, `.aiassistant/rules/README.md`, `Docs/project/*`, `Docs/gameplay/*`는 한국어를 기본으로 유지한다.
- 코드 식별자, 경로, 씬 이름, 메뉴 이름처럼 번역하면 안 되는 고유 명칭은 원문 그대로 적는다.
- 커밋 메시지 규칙은 `Docs/project/GIT_COMMIT_TEMPLATE.md`만 정본으로 사용한다.
