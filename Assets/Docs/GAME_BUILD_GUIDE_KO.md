# Jonggu Restaurant 빌드 및 생성 가이드

## 1. 에디터에서 다시 생성하는 방법

1. Unity에서 프로젝트를 연다.
2. 상단 메뉴에서 `Tools > Jonggu Restaurant > 프로토타입 빌드 및 감사`를 실행한다.
3. 빌드가 끝나면 생성 씬 감사가 자동으로 이어서 실행된다.
4. 빌드와 감사가 모두 끝나면 `Assets/Scenes/Hub.unity`를 연다.
5. Play를 눌러 현재 루프를 확인한다.

## 2. 생성 또는 갱신 대상

- `Hub.unity`
- `Beach.unity`
- `DeepForest.unity`
- `AbandonedMine.unity`
- `WindHill.unity`
- generated 자원 및 임시 에셋
- generated 스프라이트
- generated TMP 폰트
- Build Settings 목록

## 3. 빌드 시 함께 수행되는 일

- `WindHill` 씬의 `HUDRoot`를 탐험 씬 공용 기준으로 저장 상태 기준 동기화한다.
- `Beach`, `DeepForest`, `AbandonedMine`의 `HUDRoot`를 위 기준으로 다시 맞춘다.
- `PrototypeSceneAudit`를 호출해 생성된 씬 구조와 레이아웃을 점검한다.

## 4. 언제 다시 실행하면 좋은가

- 씬이 손상됐거나 generated 에셋 참조가 비었을 때
- generated 폰트나 스프라이트를 다시 만들어야 할 때
- 빌더 기준 레이아웃과 UI 구조를 다시 맞추고 싶을 때
- `WindHill` 기준 `HUDRoot` 변경을 다른 탐험 씬에도 반영하고 싶을 때

## 5. 주의사항

- 빌더 결과물만 직접 고치지 말고, 기준이 되는 빌더 코드와 레이아웃 상수를 먼저 수정한다.
- `Sync Canvas UI Layouts`는 Canvas 아래 UI의 `RectTransform`과 `Image` 표시값을 공용 오버라이드 자산에 저장하는 용도다.
- 생성 씬 감사가 실패하면 빌드 흐름도 실패로 보고 원인을 먼저 정리한다.

## 6. 텍스트 관련 참고

- generated TMP 폰트는 프로젝트 기준 기본 폰트를 따라 다시 만들어진다.
- 빌드 후 생성되는 UI와 월드 텍스트는 현재 TMP 설정과 빌더 기준값을 따른다.
