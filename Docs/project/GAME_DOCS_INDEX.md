# 규칙 문서 인덱스

이 문서는 작업별 문서 진입점이다. 새 규칙을 추가하거나 문서를 정리할 때는 먼저 정본 위치를 정하고, 이 문서에는 링크만 남긴다.

## 최소 읽기 순서

1. 루트 엔트리 `AGENTS.md` 또는 `CLAUDE.md`
2. `.aiassistant/rules/README.md`
3. `Docs/project/GAME_ASSISTANT_RULES.md`
4. 현재 작업에 필요한 정본 문서 1~2개

## project 문서

- `GAME_ASSISTANT_RULES.md`: 전역 규칙, 검증 원칙, 읽기 제외 기준
- `GAME_DOCS_INDEX.md`: 작업별 문서 진입점
- `GAME_PROJECT_STRUCTURE.md`: 저장소 구조, 책임 경계, 주요 경로
- `AGENT_WORKFLOW.md`: 작업 루프와 유형별 체크포인트
- `SOURCE_OF_TRUTH.md`: 씬, UI, generated 자산, 로컬 데이터의 정본 관계

## 도메인 문서

- `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`: 게임 콘셉트, 핵심 루프, 장기 의도
- `Docs/gameplay/GAMEPLAY_CORE_LOOP.md`: 자유 이동형 코어 루프와 로컬 게임 데이터 기준
- `Docs/gameplay/GAMEPLAY_EXPLORATION.md`: 지역, 포털, 위험 지대, 탐험 의도
- `Docs/gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md`: 영업, 업그레이드, 자원 소비와 성장 축
- `Docs/ui/UI_AND_TEXT_GUIDE.md`: HUD/팝업 구조, UI 정본 관계, generated UI 경로
- `Docs/ui/UI_GROUPING_RULES.md`: Canvas 그룹 구조와 이름 기준
- `Docs/scene/GAME_SCENE_AND_SETUP.md`: 지원 씬 역할, 주요 직렬화 포인트
- `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`: 월드 계층 구조와 그룹 배치 기준
- `Docs/build/GAME_BUILD_GUIDE.md`: 빌더 흐름, generated 에셋 비생성 원칙, 감사 기준

## 작업별 빠른 진입점

- UI 수정: `AGENT_WORKFLOW.md`, `SOURCE_OF_TRUTH.md`, `Docs/ui/UI_AND_TEXT_GUIDE.md`
- UI 구조/탐색성 리팩토링: `GAME_PROJECT_STRUCTURE.md`, `SOURCE_OF_TRUTH.md`, `Docs/ui/UI_GROUPING_RULES.md`
- 씬 배치나 월드 오브젝트 수정: `SOURCE_OF_TRUTH.md`, `Docs/scene/GAME_SCENE_AND_SETUP.md`
- generated 자산, 폰트, 빌더 경로 수정: `GAME_PROJECT_STRUCTURE.md`, `SOURCE_OF_TRUTH.md`, `Docs/build/GAME_BUILD_GUIDE.md`
- 로컬 데이터와 런타임 상태 변경: `GAME_PROJECT_STRUCTURE.md`, `SOURCE_OF_TRUTH.md`, `AGENT_WORKFLOW.md`
- 문서/규칙 체계 정리: `GAME_ASSISTANT_RULES.md`, `AGENT_WORKFLOW.md`, 이 인덱스
- 루트 엔트리 정리: `.aiassistant/rules/README.md`, `GAME_ASSISTANT_RULES.md`, `GAME_PROJECT_STRUCTURE.md`, `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`
- 주석/코드 가독성 정리: `GAME_ASSISTANT_RULES.md`, `AGENT_WORKFLOW.md`, `GAME_PROJECT_STRUCTURE.md`
- 게임 의도 확인: `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`와 관련 gameplay 문서

## 유지 규칙

- 같은 규칙을 여러 문서에 복제하지 않는다.
- 제거된 파일, 바뀐 경로, 더 이상 사실이 아닌 설명은 이 문서에서도 함께 정리한다.
- 이 문서는 정본 위치만 가리키고 세부 규칙을 길게 설명하지 않는다.
