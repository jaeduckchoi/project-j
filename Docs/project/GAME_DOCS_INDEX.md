# 규칙 문서 인덱스

이 문서는 `.aiassistant` 문서의 작업 기반 인덱스다.
새 규칙을 추가하거나 기존 문서를 정리할 때는 먼저 이 문서에서 정본 위치를 찾는다.

## 1. 최소 읽기 순서

1. 루트 엔트리 파일 `AGENTS.md` 또는 `CLAUDE.md`
2. `.aiassistant/rules/README.md`
3. `Docs/project/GAME_ASSISTANT_RULES.md`
4. 현재 작업에 맞는 정본 문서 1~2개

## 2. `project` 문서

- `GAME_ASSISTANT_RULES.md`: 전역 규칙과 하네스 운영 원칙
- `GAME_DOCS_INDEX.md`: 이 인덱스와 작업별 문서 진입점
- `GAME_PROJECT_STRUCTURE.md`: 실제 저장소 구조, 책임 경계, generated 경로
- `AGENT_WORKFLOW.md`: 기본 작업 루프, 검증 매트릭스, 드리프트 정리 기준
- `SOURCE_OF_TRUTH.md`: 씬 직렬화, builder, generated 자산 경로, 인접 API 계약의 정본 관계
- `GIT_COMMIT_TEMPLATE.md`: 커밋 메시지 규칙의 유일한 정본

## 3. `gameplay` 문서

- `GAME_DESIGN_OVERVIEW.md`: 게임 콘셉트, 핵심 루프, 장기 의도
- `GAMEPLAY_CORE_LOOP.md`: 자유 이동형 코어 루프와 서버 데이터 계약
- `GAMEPLAY_EXPLORATION.md`: 지역, 포털, 위험 지대, 탐험 의도
- `GAMEPLAY_RESTAURANT_AND_GROWTH.md`: 영업, 업그레이드, 자원 소비와 성장 축

## 4. `ui`, `scene`, `build` 문서

- `UI_AND_TEXT_GUIDE.md`: 현재 HUD/팝업 구조, UI 정본 관계, generated UI 경로
- `UI_GROUPING_RULES.md`: Canvas 그룹 구조와 이름 기준
- `GAME_SCENE_AND_SETUP.md`: 지원 씬 역할, 주요 직렬화 포인트, 고위험 체크포인트
- `SCENE_HIERARCHY_GROUPING_RULES.md`: 월드 계층 구조와 그룹 배치 기준
- `GAME_BUILD_GUIDE.md`: 빌더 흐름, 정적 generated 에셋 비생성 원칙, 감사 기준

## 5. 작업별 빠른 진입점

- UI 수정:
  `Docs/project/AGENT_WORKFLOW.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/ui/UI_AND_TEXT_GUIDE.md`, `Docs/ui/UI_GROUPING_RULES.md`
- UI 구조/탐색성 리팩토링:
  `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/project/AGENT_WORKFLOW.md`, `Docs/ui/UI_AND_TEXT_GUIDE.md`
- 씬 배치나 월드 오브젝트 수정:
  `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/scene/GAME_SCENE_AND_SETUP.md`, `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- generated 자산, 폰트, 빌더 경로 수정:
  `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/build/GAME_BUILD_GUIDE.md`
- Unity API 연동:
  `Docs/project/GAME_PROJECT_STRUCTURE.md`, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/project/AGENT_WORKFLOW.md`
- 문서/규칙 체계 정리:
  `Docs/project/GAME_ASSISTANT_RULES.md`, `Docs/project/AGENT_WORKFLOW.md`, 이 인덱스
- 주석/코드 가독성 정리:
  `Docs/project/GAME_ASSISTANT_RULES.md`, `Docs/project/AGENT_WORKFLOW.md`, `Docs/project/GAME_PROJECT_STRUCTURE.md`
- 게임 의도 확인:
  `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`와 관련 gameplay 문서

## 6. 인덱스 유지 규칙

- 새 규칙이 생기면 먼저 정본 문서를 정하고, 그 다음 이 문서에 링크를 추가한다.
- 제거된 파일, 바뀐 경로, 더 이상 사실이 아닌 설명은 이 문서에서도 함께 정리한다.
- 같은 규칙을 여러 문서에 복제하지 않는다. 이 문서는 언제나 정본 위치만 가리킨다.
