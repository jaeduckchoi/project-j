# 종구의 식당 문서 인덱스

## 1. 문서 목적

이 폴더 문서는 `종구의 식당` 현재 구현 상태를 게임 기준으로 정리하기 위한 문서다.
이전처럼 `최소 프로토타입` 중심으로 나누지 않고, 실제 게임 루프와 기능 기준으로 읽을 수 있게 다시 정리한다.

## 2. 게임 한눈에 보기

- 장르: 2D 탑다운 탐험-운영 게임
- 핵심 루프: `허브 준비 -> 오전 탐험 -> 허브 복귀 -> 메뉴 선택 -> 장사 -> 정산 -> 다음 날`
- 핵심 지역: `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill`
- 핵심 성장 축: 인벤토리 확장, 랜턴 해금, 평판 기반 숏컷
- 핵심 목표: 탐험에서 얻은 재료를 장사와 성장으로 자연스럽게 환원하는 구조

## 3. 현재 문서 구성

- `GAME_FEATURE_REFERENCE_KO.md`
  게임 특징, 지역별 특징, 시스템별 기능을 기능 단위로 정리한 문서다.
- `GAMEPLAY_CORE_LOOP_KO.md`
  하루 단위 핵심 루프와 플레이 감각을 정리한 문서다.
- `GAMEPLAY_EXPLORATION_KO.md`
  탐험 지역, 도구, 채집, 환경 위험 요소를 정리한 문서다.
- `GAMEPLAY_RESTAURANT_AND_GROWTH_KO.md`
  장사, 창고, 경제, 업그레이드 기능을 정리한 문서다.
- `UI_AND_TEXT_GUIDE_KO.md`
  UI 구성, 가독성 보강, 한글 폰트 기준을 정리한 문서다.
- `GAME_SCENE_AND_SETUP_KO.md`
  씬 구성, 인스펙터 체크 포인트, 플레이 테스트 순서를 정리한 문서다.
- `GAME_BUILD_GUIDE_KO.md`
  에디터에서 씬과 generated 데이터를 다시 만드는 방법을 정리한 문서다.

## 4. 권장 읽기 순서

1. `GAME_DOCS_INDEX_KO.md`
2. `GAME_FEATURE_REFERENCE_KO.md`
3. `GAMEPLAY_CORE_LOOP_KO.md`
4. `GAMEPLAY_EXPLORATION_KO.md`
5. `GAMEPLAY_RESTAURANT_AND_GROWTH_KO.md`
6. `UI_AND_TEXT_GUIDE_KO.md`
7. `GAME_SCENE_AND_SETUP_KO.md`
8. `GAME_BUILD_GUIDE_KO.md`

## 5. 빠른 확인 순서

1. `Hub` 에서 UI, 창고, 작업대, 포탈 확인
2. `Beach` 에서 기본 채집 확인
3. `DeepForest` 에서 버섯 / 약초와 감속 구간 확인
4. `AbandonedMine` 에서 랜턴 조건, 어둠, `GlowMoss` 확인
5. `WindHill` 에서 강풍과 평판 숏컷 확인
6. 허브에서 메뉴 선택, 장사, 정산, 다음 날 흐름 확인

## 6. 현재 상태 메모

- 코드와 씬 데이터 기준 주요 루프는 연결되어 있다.
- 에디터 플레이 기준 텍스트 가독성과 한글 폰트 세팅도 보강된 상태다.
- Unity 실제 플레이 / 컴파일 검증은 이 환경에서 직접 하지 못했다.
