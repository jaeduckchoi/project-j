# 프로젝트 문서 진입점

이 문서는 처음 읽는 사람과 에이전트를 위한 `Docs` 랜딩 문서다. 세부 규칙과 도메인 기준은 하위 정본 문서에서만 관리한다.

## 최소 읽기 순서

1. [AGENTS.md](../AGENTS.md) 또는 [CLAUDE.md](../CLAUDE.md)
2. [GAME_ASSISTANT_RULES.md](Project/GAME_ASSISTANT_RULES.md)
3. [GAME_DOCS_INDEX.md](Project/GAME_DOCS_INDEX.md)
4. 현재 작업의 1차 정본 문서 1개
5. 코드, 씬, generated 자산, Unity 메타데이터를 건드리면 [SOURCE_OF_TRUTH.md](Project/SOURCE_OF_TRUTH.md)

## 문서 계층

- [Project](Project/GAME_DOCS_INDEX.md): 전역 규칙, 작업 절차, 저장소 구조, 정본 관계, 문서 템플릿
- [Gameplay](Gameplay/GAME_DESIGN_OVERVIEW.md): 게임 개요, 허브 코어 루프, 탐험, 식당 운영과 성장 의도
- [Scene](Scene/GAME_SCENE_AND_SETUP.md): 씬 직렬화, 화이트박스 기준, 하이어라키 규칙
- [UI](UI/UI_AND_TEXT_GUIDE.md): 관리 UI, 텍스트, Canvas 그룹, 바인딩 자산 기준
- [Build](Build/GAME_BUILD_GUIDE.md): generated 경로, 빌더 전제, `scene-integrated metadata` 처리 기준

## 작업별 시작점

- 규칙 체계와 문서 하네스 정리: [GAME_DOCS_INDEX.md](Project/GAME_DOCS_INDEX.md), [GAME_ASSISTANT_RULES.md](Project/GAME_ASSISTANT_RULES.md)
- 프로젝트 구조와 대표 경로 확인: [GAME_PROJECT_STRUCTURE.md](Project/GAME_PROJECT_STRUCTURE.md), [SOURCE_OF_TRUTH.md](Project/SOURCE_OF_TRUTH.md)
- 플레이 의도 확인: [GAME_DESIGN_OVERVIEW.md](Gameplay/GAME_DESIGN_OVERVIEW.md), [GAMEPLAY_CORE_LOOP.md](Gameplay/GAMEPLAY_CORE_LOOP.md)
- 식당 운영과 성장 정리: [GAMEPLAY_CORE_LOOP.md](Gameplay/GAMEPLAY_CORE_LOOP.md), [GAMEPLAY_RESTAURANT_AND_GROWTH.md](Gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md)
- 탐험과 지역 진행 정리: [GAMEPLAY_EXPLORATION.md](Gameplay/GAMEPLAY_EXPLORATION.md), [BEACH_WHITEBOX.md](Scene/BEACH_WHITEBOX.md)
- 씬 구조와 직렬화 변경: [GAME_SCENE_AND_SETUP.md](Scene/GAME_SCENE_AND_SETUP.md), [SCENE_HIERARCHY_GROUPING_RULES.md](Scene/SCENE_HIERARCHY_GROUPING_RULES.md)
- UI 구조와 텍스트 변경: [UI_AND_TEXT_GUIDE.md](UI/UI_AND_TEXT_GUIDE.md), [UI_GROUPING_RULES.md](UI/UI_GROUPING_RULES.md)
- generated 자산과 import metadata 변경: [GAME_BUILD_GUIDE.md](Build/GAME_BUILD_GUIDE.md), [SOURCE_OF_TRUTH.md](Project/SOURCE_OF_TRUTH.md)
