# 프로젝트 작업 하네스

이 문서는 `Docs`의 최상위 진입점이다. 루트 엔트리는 에이전트별 온보딩을 맡고, 실제 규칙과 정본 관계는 `Docs` 아래 문서에서 관리한다.

## 역할

- `AGENTS.md`와 `CLAUDE.md`는 매 세션 처음 읽는 짧은 WHAT/WHY/HOW 맵이다.
- `Docs/README.md`는 작업자가 다음에 어떤 정본 문서를 읽어야 하는지 안내하는 공통 하네스다.
- 세부 규칙은 `Docs/project/*`, 게임 의도는 `Docs/gameplay/*`, UI/씬/빌더 기준은 각 도메인 문서가 정본이다.

## 최소 읽기 순서

1. 사용하는 에이전트의 루트 엔트리(`AGENTS.md` 또는 `CLAUDE.md`)를 읽는다.
2. 이 문서에서 문서 계층과 작업별 진입점을 확인한다.
3. `Docs/project/GAME_ASSISTANT_RULES.md`를 읽어 전역 가드레일과 읽기 제외 범위를 확인한다.
4. `Docs/project/GAME_DOCS_INDEX.md`에서 현재 작업에 필요한 정본 문서 1~2개를 고른다.
5. 코드, 씬, generated 자산을 바꿀 때는 `Docs/project/SOURCE_OF_TRUTH.md`의 정본 관계를 먼저 따른다.

## 문서 계층

- `Docs/project/*`: 전역 규칙, 작업 절차, 프로젝트 구조, 정본 관계, 문서 인덱스
- `Docs/gameplay/*`: 게임 콘셉트, 핵심 루프, 탐험, 식당 운영과 성장 의도
- `Docs/ui/*`: UI 구조, 텍스트, Canvas 그룹, generated UI 기준
- `Docs/scene/*`: 씬 역할, 월드 계층, 직렬화 기준
- `Docs/build/*`: 빌더 흐름, generated 에셋 원칙, 감사 기준

## 작업별 시작점

- 규칙 체계, 문서 정리, 하네스 운영: `Docs/project/GAME_ASSISTANT_RULES.md`, `Docs/project/AGENT_WORKFLOW.md`, `Docs/project/GAME_DOCS_INDEX.md`
- 프로젝트 구조, 경로, 네임스페이스: `Docs/project/GAME_PROJECT_STRUCTURE.md`
- 정본 관계와 함께 수정할 결합 지점: `Docs/project/SOURCE_OF_TRUTH.md`
- UI 변경: `Docs/ui/UI_AND_TEXT_GUIDE.md`, `Docs/ui/UI_GROUPING_RULES.md`
- UI 구조 리팩토링/탐색성 정리: `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/project/AGENT_WORKFLOW.md`, `Docs/ui/UI_AND_TEXT_GUIDE.md`
- 씬/월드 변경: `Docs/scene/GAME_SCENE_AND_SETUP.md`, `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- 빌더/생성 자산 변경: `Docs/build/GAME_BUILD_GUIDE.md`
- Unity API 연동: `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/project/AGENT_WORKFLOW.md`
- 플레이 의도 확인: `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`와 관련 gameplay 문서

## 하네스 운영 원칙

- 엔트리 파일은 백과사전이 아니라 맵이다.
- 새 규칙은 루트 엔트리에 바로 추가하지 말고, 먼저 `Docs` 안의 정본 문서 위치를 정한다.
- 같은 규칙은 한 문서만 정본으로 두고, 다른 문서에는 링크와 짧은 설명만 남긴다.
- 포맷팅과 스타일 검사는 에이전트 지시보다 린터, 훅, 검증 명령을 우선한다.
- generated 결과물보다 생성 경로와 정본 관계를 먼저 수정한다.
- 폴더 리팩토링은 파일 이동만으로 끝내지 말고 asmdef 범위, `.meta`, 직접 경로를 적은 문서까지 같은 변경에서 정리한다.
- 작업이 끝나면 관련 문서와 오래된 경로, 중복 규칙, 드리프트를 함께 정리한다.

## 종료 체크

- 작업과 맞는 정본 문서를 읽었는가?
- 변경한 코드, 씬, generated 경로의 정본 관계가 문서와 맞는가?
- 옛 경로 문자열이나 중복 규칙이 남지 않았는가?
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과에 남은 검증 단계를 적었는가?
