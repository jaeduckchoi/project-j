---
적용: 항상
---

# 종구의 식당 규칙 문서 인덱스

## 1. 목적

이 문서는 `.aiassistant/rules` 아래 카테고리별 폴더로 정리된 기준 문서들의 인덱스다.
목적은 런타임 에셋과 작업 기준 문서를 분리해, 사람 협업자와 AI 도우미가 필요한 규칙 문서를 빠르게 찾을 수 있도록 하는 것이다.
현재 세부 기준 문서는 UTF-8 한국어 본문을 정본으로 유지하며, 파일 경로와 코드 식별자를 기준으로 후속 문서 연결을 유지한다.

## 2. 현재 문서 분류

### `project`

- `GAME_ASSISTANT_RULES.md`
  공용 작업 규칙, 문서 정책, UI/빌더/감사 기준, Git 규칙을 다룬다.
- `GAME_DOCS_INDEX.md`
  전체 문서 지도와 권장 읽기 순서를 제공한다.
- `GAME_PROJECT_STRUCTURE.md`
  저장소 구조, `.aiassistant/rules`, `Assets`, 생성 자산, 에디터 코드 배치 규칙을 다룬다.

### `gameplay`

- 이 카테고리의 세부 기준 문서는 현재 한국어 정본 기준으로 유지한다.
- `GAME_FEATURE_REFERENCE.md`
  게임 시스템, 지역, 데이터, 성장 축을 통합해서 요약한다.
- `GAMEPLAY_CORE_LOOP.md`
  하루 루프와 상태 전이 기준을 정리하는 한국어 정본 문서다.
- `GAMEPLAY_EXPLORATION.md`
  탐험 지역, 도구, 위험 지대, 포탈 잠금 규칙을 다루는 한국어 정본 문서다.
- `GAMEPLAY_RESTAURANT_AND_GROWTH.md`
  메뉴 흐름, 식당 영업, 창고, 업그레이드, 평판 흐름을 다루는 한국어 정본 문서다.

### `ui`

- `UI_AND_TEXT_GUIDE.md`
  HUD/팝업 구조, TMP 폰트, 생성 UI 스프라이트, 에디터 프리뷰 기준을 다룬다.
- `UI_GROUPING_RULES.md`
  `HUDRoot`, `PopupRoot`를 중심으로 한 Canvas 그룹 규칙을 다루는 한국어 정본 문서다.

### `scene`

- `GAME_SCENE_AND_SETUP.md`
  지원 씬 구조, 인스펙터 점검 지점, 권장 테스트 순서를 다룬다.
- `SCENE_HIERARCHY_GROUPING_RULES.md`
  `SceneWorldRoot`, `SceneGameplayRoot`, `SceneSystemRoot`, `Canvas` 기준의 월드 계층 규칙을 다룬다.

### `build`

- `GAME_BUILD_GUIDE.md`
  `Tools > Jonggu Restaurant > Prototype Build and Audit` 메인 메뉴와 생성 자산/감사 흐름을 다룬다.

## 3. 권장 읽기 순서

1. `project/GAME_ASSISTANT_RULES.md`
2. `project/GAME_DOCS_INDEX.md`
3. `project/GAME_PROJECT_STRUCTURE.md`
4. `gameplay/GAME_FEATURE_REFERENCE.md`
5. `gameplay/GAMEPLAY_CORE_LOOP.md`
6. `ui/UI_AND_TEXT_GUIDE.md`
7. `scene/GAME_SCENE_AND_SETUP.md`
8. `build/GAME_BUILD_GUIDE.md`

## 4. 작업 유형별 빠른 이동

- 구조 변경
  `project/GAME_PROJECT_STRUCTURE.md`, `scene/SCENE_HIERARCHY_GROUPING_RULES.md`, `build/GAME_BUILD_GUIDE.md`
- 게임플레이 변경
  `gameplay/GAME_FEATURE_REFERENCE.md`, `gameplay/GAMEPLAY_CORE_LOOP.md`, `gameplay/GAMEPLAY_EXPLORATION.md`, `gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md`
- UI 변경
  `ui/UI_AND_TEXT_GUIDE.md`, `ui/UI_GROUPING_RULES.md`, `scene/GAME_SCENE_AND_SETUP.md`
- 빌더 또는 감사 변경
  `build/GAME_BUILD_GUIDE.md`, `scene/SCENE_HIERARCHY_GROUPING_RULES.md`, `project/GAME_ASSISTANT_RULES.md`
- AI 워크플로우 또는 커밋 규칙 변경
  `project/GAME_ASSISTANT_RULES.md`, `project/GIT_COMMIT_TEMPLATE.md`, `build/GAME_BUILD_GUIDE.md`

## 5. 현재 프로젝트 요약

- 지원하는 플레이 가능 씬은 `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill`이다.
- 핵심 루프는 `허브 준비 -> 오전 탐험 -> 허브 복귀 -> 오후 영업 -> 정산 -> 다음 날`이다.
- 주요 성장 축은 인벤토리 확장, 랜턴 해금, 평판 6 지름길 해금이다.
- 공용 UI 기준은 `Assets/Resources/Generated/ui-layout-overrides.asset`에 저장되며, 지원 씬 저장 시 자동 동기화된다.
- 씬 구조 규칙은 `PrototypeSceneHierarchyCatalog`를 통해, UI 구조 규칙은 `PrototypeUISceneLayoutSettings`와 `UIManager`를 통해 공유된다.

## 6. 검증 메모

- 이 인덱스는 현재 저장소 구조와 코드 기준으로 갱신되었다.
- Unity 플레이 모드와 배치 컴파일은 이 작업에서 직접 검증하지 못했으므로, 변경 적용 후 빌드와 감사 메뉴 검증이 추가로 필요하다.
