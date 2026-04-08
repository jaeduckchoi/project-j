# 식당 영업과 성장 기획

## 1. 허브의 역할

허브는 영업, 정리, 성장, 탐험 지역 결정이 모두 이뤄지는 공간이다.
플레이어는 허브를 직접 걸어 다니며 메뉴를 고르고, 창고를 정리하고, 업그레이드를 진행하고, 새로운 지역으로 이동한다.

## 2. 식당 영업

### 메뉴 데이터

- 서버 bootstrap의 `recipes`가 메뉴 정본이다.
- 각 레시피는 `recipeId`, 표시 이름, 공급처, 난이도, 조리법, 필요 재료, 판매가, 메모를 가진다.
- `recipeId`는 서버 응답 문자열을 그대로 사용하며 행 순서나 숫자로 재계산하지 않는다.
- 필요 재료는 `ingredientId`, `ingredientName`, `quantity` 구조를 사용한다.

### 영업 흐름

1. 메뉴 선택
2. 필요 재료 확인
3. 영업 진행
4. 결과 텍스트 확인
5. 골드와 평판 변화 적용

### 영업 화면이 보여 주는 정보

- 선택 메뉴 설명과 필요 재료
- 보유 수량과 조리 가능 여부
- 영업 결과 텍스트
- 골드 증가량과 평판 변화량

### 영업 규칙

- 기본 영업 상한 : `serviceCapacity = 3`
- 보유 재료와 상한 중 작은 값이 실제 조리 수량이 된다.
- 결과 문자열은 영업 결과 패널과 안내 텍스트에 바로 반영된다.
- 영업과 메뉴 선택은 phase 제한 없이 가능하지만 UX상 Hub에서만 실행한다.

## 3. 인벤토리

| 단계 | 슬롯 수 |
| --- | --- |
| 시작 | 8 |
| 1차 확장 | 12 |
| 2차 확장 | 16 |

- 재료 전용 슬롯 구조이며 도구는 슬롯을 차지하지 않는다.
- 슬롯 압박은 의도된 디자인 요소다.

## 4. 창고

- 허브 전용 보관 시스템이며 `StorageStation`에서 `E` 상호작용으로 연다.
- 지원 동작 : 맡기기, 꺼내기, 선택 품목 맡기기/꺼내기, 모두 맡기기/꺼내기.
- 탐험 후 남은 재료를 정리하는 완충 공간 역할을 한다.

## 5. 경제와 성장

### 골드와 평판

- 골드는 영업 결과와 업그레이드 비용에 사용한다.
- 평판은 편의 해금과 연결된다.
- `WindHillShortcut`은 `Reputation 6`에서 열린다.

### 업그레이드 비용

| 항목 | 비용 |
| --- | --- |
| 인벤토리 12칸 확장 | `Gold 30 + Shell x3` |
| 인벤토리 16칸 확장 | `Gold 65 + Herb x4` |
| 랜턴 해금 | `Gold 45 + Mushroom x2` |

- `UpgradeManager`는 즉시 가능한 항목을 우선해서 보여 준다.

## 6. 허브 상호작용 지점

- 메뉴 선택기 `RecipeSelector`
- 영업대 `ServiceCounter`
- 창고 `StorageStation`
- 업그레이드 작업대 `UpgradeStation`
- 지역 포탈 (`GoToBeach`, `GoToDeepForest`, `GoToAbandonedMine`, `GoToWindHill`)

모든 지점은 `E` 상호작용으로 사용하며, 조건이 맞지 않으면 사유를 표시한다.

## 7. 허브 팝업과 시간 정지

- 팝업 종류 : `Cooking Menu`, `Upgrade`, `Materials`, `Storage`
- 팝업이 열리면 게임 시간이 멈춘다.
- `Esc` 또는 `PopupCloseButton`으로 닫으면 이전 시간 흐름이 복구된다.
- 일시 정지 동작은 `PopupPauseStateUtility`가 관리한다.

## 8. 시스템 연결 지점

- `Assets/Scripts/Restaurant/RestaurantManager.cs`
- `Assets/Scripts/Management/Storage/StorageManager.cs`
- `Assets/Scripts/Management/Storage/StorageStation.cs`
- `Assets/Scripts/Management/Upgrade/UpgradeManager.cs`
- `Assets/Scripts/Management/Economy/EconomyManager.cs`
- `Assets/Scripts/UI/UIManager.cs`
- 레시피 데이터 : 서버 bootstrap `recipes`
- 재료 데이터 : 서버 bootstrap `ingredients`
