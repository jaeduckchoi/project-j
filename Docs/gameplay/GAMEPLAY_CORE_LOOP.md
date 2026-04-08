# 코어 루프 기획

## 1. 기본 흐름

서버 계약에서 하루, 단계, settlement 루틴은 제거되었다. 플레이어는 phase 제한 없이 허브와 탐험 지역을 오가며 재료 수집, 메뉴 선택, 영업, 창고 정리, 업그레이드를 반복한다.

1. **허브 확인** : 골드, 평판, 인벤토리, 창고, 선택 메뉴를 확인한다.
2. **지역 이동** : 포탈 규칙에 맞는 탐험 지역으로 이동한다.
3. **재료 수집** : 지역 자원을 모으고 인벤토리 한도 안에서 무엇을 챙길지 판단한다.
4. **허브 복귀** : 언제든 허브로 돌아와 메뉴, 창고, 업그레이드를 정리한다.
5. **메뉴 선택** : 서버 `recipeId` 기준으로 원하는 메뉴를 선택한다.
6. **영업 실행** : 서버 영업 결과를 받아 골드, 평판, 재료 상태를 갱신한다.

## 2. 제한 규칙

- 탐험, 레시피 선택, 영업은 day/phase 제한 없이 호출 가능하다.
- 메뉴 선택, 영업, 창고 사용은 UX상 Hub에서만 가능하게 유지한다.
- 지역 이동 제약은 portal rule의 필요 도구와 필요 평판만 따른다.
- 탐험 건너뛰기, 영업 건너뛰기, 다음 진행 플로우는 사용하지 않는다.

## 3. 의도하는 감각

- 어디로 갈지 정할 때의 기대감
- 인벤토리 안에 무엇을 담을지 결정하는 압박
- 깊이 들어갈지 돌아갈지 판단하는 탐험 긴장
- 모은 재료가 메뉴, 골드, 평판으로 변환되는 만족감
- 업그레이드가 다음 탐험을 여는 성장감

## 4. 데이터 계약

- 레시피 정본은 서버 bootstrap의 `recipes`이며, 식별자는 `recipeId` 문자열이다.
- `recipeId`는 행 순서나 숫자로 재계산하지 않고 서버 응답 값을 그대로 사용한다.
- 플레이어 스냅샷의 선택 메뉴는 `selectedRecipeId`로 유지한다.
- 레시피 재료는 `ingredientId`, `ingredientName`, `quantity` 구조를 사용한다.
- 재료 카탈로그는 bootstrap의 `ingredients`이며, `ingredientId` 문자열을 그대로 사용한다.
- 레시피 이미지는 `Resources/Generated/Sprites/Recipes/{recipeId}`를 우선 사용한다.

## 5. 시스템 연결 지점

- 안내 관리 : `DayCycleManager`
- 씬 이동 안내 : `DayCycleManager.HandleSceneTravel`
- 레시피/재료 카탈로그 반영 : `JongguApiSession` → `RestaurantManager.ApplyRemoteCatalog`
- 영업 결과 반영 : `JongguApiSession` → `RestaurantManager.ApplyRemoteState`
- 임시 가이드 : `ShowTemporaryGuide`, `ShowHintOnce`
- 자동 회귀 점검 : `GameplayAutomationAudit` (가이드, 포탈 잠금, 팝업 일시정지)
