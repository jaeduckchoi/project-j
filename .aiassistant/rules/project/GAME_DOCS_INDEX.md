---
적용: 항상
---

# 규칙 문서 인덱스

이 문서는 `.aiassistant` 문서의 작업 기반 인덱스다.
새 규칙을 추가하거나 기존 문서를 정리할 때는 먼저 이 문서에서 정본 위치를 찾는다.

## 1. 최소 읽기 순서

1. 루트 엔트리 파일 `AGENTS.md` 또는 `CLAUDE.md`
2. `.aiassistant/rules/README.md`
3. `project/GAME_ASSISTANT_RULES.md`
4. 현재 작업에 맞는 정본 문서 1~2개

## 2. `project` 문서

- `GAME_ASSISTANT_RULES.md`: 전역 규칙과 하네스 운영 원칙
- `GAME_DOCS_INDEX.md`: 이 인덱스와 작업별 문서 진입점
- `GAME_PROJECT_STRUCTURE.md`: 실제 저장소 구조, 책임 경계, generated 경로
- `AGENT_WORKFLOW.md`: 기본 작업 루프, 검증 매트릭스, 드리프트 정리 기준
- `SOURCE_OF_TRUTH.md`: 씬 직렬화, runtime augmenter, builder, generated 자산의 정본 관계
- `GIT_COMMIT_TEMPLATE.md`: 커밋 메시지 규칙의 유일한 정본

## 3. `gameplay` 문서

- `GAME_DESIGN_OVERVIEW.md`: 게임 콘셉트, 핵심 루프, 장기 의도
- `GAMEPLAY_CORE_LOOP.md`: 일과 진행 단계와 전환 기준
- `GAMEPLAY_EXPLORATION.md`: 지역, 포털, 위험 지대, 탐험 의도
- `GAMEPLAY_RESTAURANT_AND_GROWTH.md`: 영업, 업그레이드, 자원 소비와 성장 축

## 4. `ui`, `scene`, `build` 문서

- `UI_AND_TEXT_GUIDE.md`: 현재 HUD/팝업 구조, UI 정본 관계, generated UI 경로
- `UI_GROUPING_RULES.md`: Canvas 그룹 구조와 이름 기준
- `GAME_SCENE_AND_SETUP.md`: 지원 씬 역할, 주요 직렬화 포인트, 고위험 체크포인트
- `SCENE_HIERARCHY_GROUPING_RULES.md`: 월드 계층 구조와 그룹 배치 기준
- `GAME_BUILD_GUIDE.md`: 빌더 흐름, generated 자산 경로, 감사 기준

## 5. 작업별 빠른 진입점

- UI 수정:
  `project/AGENT_WORKFLOW.md`, `project/SOURCE_OF_TRUTH.md`, `ui/UI_AND_TEXT_GUIDE.md`, `ui/UI_GROUPING_RULES.md`
- 씬 배치나 월드 오브젝트 수정:
  `project/SOURCE_OF_TRUTH.md`, `scene/GAME_SCENE_AND_SETUP.md`, `scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- generated 자산, 폰트, 빌더 경로 수정:
  `project/GAME_PROJECT_STRUCTURE.md`, `project/SOURCE_OF_TRUTH.md`, `build/GAME_BUILD_GUIDE.md`
- 문서/규칙 체계 정리:
  `project/GAME_ASSISTANT_RULES.md`, `project/AGENT_WORKFLOW.md`, 이 인덱스
- 게임 의도 확인:
  `gameplay/GAME_DESIGN_OVERVIEW.md`와 관련 gameplay 문서

## 6. 인덱스 유지 규칙

- 새 규칙이 생기면 먼저 정본 문서를 정하고, 그 다음 이 문서에 링크를 추가한다.
- 제거된 파일, 바뀐 경로, 더 이상 사실이 아닌 설명은 이 문서에서도 함께 정리한다.
- 같은 규칙을 여러 문서에 복제하지 않는다. 이 문서는 언제나 정본 위치만 가리킨다.
