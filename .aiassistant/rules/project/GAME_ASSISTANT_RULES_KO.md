# 종구의 식당 AI 작업 규칙

## 1. 프로젝트 개요

- 이 저장소는 Unity 기반 2D 탑다운 탐험 + 식당 운영 게임 프로젝트다.
- 핵심 루프는 `탐험 -> 재료 수집 -> 허브 복귀 -> 메뉴 선택 -> 영업 -> 성장` 이다.
- 기본 작업 언어는 한국어로 둔다. 사용자가 다른 언어를 요청하면 그 요청을 따른다.

## 2. 우선 확인 파일

- 게임 기능 요약: `Assets/Docs/GAME_FEATURE_REFERENCE_KO.md`
- 핵심 루프: `Assets/Docs/GAMEPLAY_CORE_LOOP_KO.md`
- 탐험 시스템: `Assets/Docs/GAMEPLAY_EXPLORATION_KO.md`
- 장사 / 성장 시스템: `Assets/Docs/GAMEPLAY_RESTAURANT_AND_GROWTH_KO.md`
- UI / 텍스트 기준: `Assets/Docs/UI_AND_TEXT_GUIDE_KO.md`
- 씬 / 세팅 체크: `Assets/Docs/GAME_SCENE_AND_SETUP_KO.md`

## 3. 코드 작업 규칙

- Unity 직렬화 필드 이름은 함부로 바꾸지 않는다.
- 씬 YAML, generated 데이터, 빌더 코드가 함께 엮인 경우 한쪽만 수정하고 끝내지 않는다.
- UI를 바꾸면 `Assets/Scripts/UI/UIManager.cs` 와 `Assets/Editor/JongguMinimalPrototypeBuilder.cs` 를 같이 확인한다.
- generated 데이터가 비거나 씬이 덜 만들어진 상황을 고려해 런타임 안전망 코드를 존중한다.
- 사용자가 만든 기존 변경사항은 되돌리지 않는다.

## 4. 주석과 문서 규칙

- 메서드 선언 위 설명은 블록 주석으로 작성한다.
- 그 외 설명은 라인 주석으로 작성한다.
- 기능을 바꾸면 관련 문서도 같이 갱신한다.
- 문서는 `프로토타입` 용어보다 실제 게임 기능 기준으로 유지한다.

## 5. UI / 텍스트 규칙

- TextMesh Pro 와 한글 폰트 설정을 유지한다.
- 한글 표시가 깨질 수 있는 폰트 교체는 피한다.
- UI는 카드형 구역, 액션 독, 프롬프트 가독성을 유지하는 방향으로 수정한다.
- 해상도 대응과 런타임 보정 여부를 같이 확인한다.

## 6. 검증 규칙

- Unity 플레이 테스트나 컴파일을 실제로 못 했으면 답변에 그 사실을 명시한다.
- 실행 검증이 불가능할 때는 어떤 파일과 어떤 직렬화 값을 확인했는지 구체적으로 적는다.
- 버그 수정 후에는 관련 씬, 빌더, 문서까지 영향 범위를 확인한다.

## 7. 권장 응답 방식

- 먼저 바뀐 결과를 짧게 요약한다.
- 이어서 핵심 파일 경로를 명시한다.
- 남은 리스크나 직접 확인이 필요한 포인트가 있으면 마지막에 분리해서 적는다.
