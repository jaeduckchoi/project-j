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

- `Hub` 씬 Canvas 값을 공용 오버라이드 자산으로 먼저 동기화한다.
- `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` UI를 위 오버라이드 기준으로 다시 생성한다.
- generated 데이터 자산은 `Assets/Generated/GameData` 한 디렉토리에서 kebab-case 파일명 기준으로 다시 맞춘다.
- 런타임 데이터 매니페스트는 `Assets/Resources/Generated/generated-game-data-manifest.asset` 기준으로 유지한다.
- generated 스프라이트는 `Assets/Generated/Sprites` 와 `Assets/Resources/Generated/Sprites` 아래 역할 폴더를 유지하고, 파일명은 kebab-case 기준으로 다시 맞춘다.
- `PrototypeSceneAudit`를 호출해 생성된 씬 구조와 레이아웃을 점검한다.

## 4. 메뉴별 역할

- `프로토타입 빌드 및 감사`
  생성 자산 준비, 기본 씬 다시 만들기, 생성 씬 감사를 한 번에 실행하는 기본 메뉴다.
- `생성 자산 및 씬 다시 만들기`
  감사를 제외하고 생성 단계만 다시 실행한다. 생성 결과를 먼저 맞춘 뒤 나중에 감사를 따로 돌리고 싶을 때 쓴다.
- `생성 씬 감사만 실행`
  현재 저장된 `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill` 구조를 다시 점검한다.

## 5. 언제 다시 실행하면 좋은가

- 씬이 손상됐거나 generated 에셋 참조가 비었을 때
- generated 폰트나 스프라이트를 다시 만들어야 할 때
- 빌더 기준 레이아웃과 UI 구조를 다시 맞추고 싶을 때
- `Hub`에서 같은 이름 UI 값을 바꾼 뒤 다른 탐험 씬에도 반영하고 싶을 때
  `Hub` 저장만 하면 공용 UI 오버라이드와 탐험 씬 HUD 기준이 자동 갱신되므로, 별도 수동 저장 메뉴는 없다.

## 6. 주의사항

- 빌더 결과물만 직접 고치지 말고, 기준이 되는 빌더 코드와 레이아웃 상수를 먼저 수정한다.
- 지원하는 Canvas 씬을 저장하면 Canvas 아래 UI의 `RectTransform`, `Image`, `TextMeshProUGUI`, `Button` 표시값이 공용 자산에 자동 저장된다.
- 위 공용 UI 오버라이드 자산 경로는 `Assets/Resources/Generated/ui-layout-overrides.asset` 이다.
- `프로토타입 빌드 및 감사`를 실행하면 기본 레이아웃은 `Hub` 기준으로 다시 읽고, 현재 열려 있는 씬의 `Canvas` UI 값은 마지막에 다시 덮어쓴다.
- 팝업 스프라이트시트처럼 빌더가 만들지 않는 런타임 전용 리소스는 `Assets/Resources/Generated/Sprites/UI` 아래에서 관리한다.
- 생성 씬 감사가 실패하면 빌드 흐름도 실패로 보고 원인을 먼저 정리한다.

## 7. 텍스트 관련 참고

- generated TMP 폰트는 프로젝트 기준 기본 폰트를 따라 다시 만들어진다.
- 빌드 후 생성되는 UI와 월드 텍스트는 현재 TMP 설정과 빌더 기준값을 따른다.
