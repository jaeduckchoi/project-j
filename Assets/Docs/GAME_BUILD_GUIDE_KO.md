# 종구의 식당 빌드 및 생성 가이드

## 1. 에디터에서 다시 생성하는 방법

1. Unity에서 프로젝트를 연다.
2. 상단 메뉴에서 `Tools > Jonggu Restaurant > 프로토타입 빌드 및 감사`를 실행한다.
3. 생성이 끝나면 `Assets/Scenes/Hub.unity` 를 연다.
4. Play를 눌러 현재 루프를 확인한다.

## 2. 생성 또는 갱신되는 항목

- `Hub.unity`
- `Beach.unity`
- `DeepForest.unity`
- `AbandonedMine.unity`
- `WindHill.unity`
- generated 자원 / 레시피 에셋
- generated 플레이스홀더 스프라이트
- generated 한글 TMP 폰트
- Build Settings 씬 목록

## 3. 언제 다시 실행하면 좋은가

- 씬이 누락되었거나 generated 에셋 참조가 비어 있을 때
- generated 폰트나 스프라이트를 다시 만들고 싶을 때
- 초기 씬 배치를 빌더 기준으로 다시 맞추고 싶을 때

## 4. 주의할 점

- 빌더 이름은 예전 메뉴명을 유지하고 있지만, 실제로는 현재 게임 기준 씬과 generated 데이터를 갱신한다.
- 씬을 다시 생성하면 수동으로 배치한 값과 비교 확인이 필요할 수 있다.
- 현재 런타임 안전망도 남아 있어서, 빌더를 다시 돌리지 않아도 일부 누락은 플레이 중 메워진다.

## 5. 텍스트 관련 참고

- generated 한글 폰트는 `MalgunGothic SDF` 기준으로 만들어진다.
- TMP 기본 설정도 이 폰트를 기본값으로 사용하도록 맞춰 두었다.
- 빌더를 다시 돌리면 이후 생성되는 UI와 월드 라벨도 같은 한글 표시 기준을 따른다.
