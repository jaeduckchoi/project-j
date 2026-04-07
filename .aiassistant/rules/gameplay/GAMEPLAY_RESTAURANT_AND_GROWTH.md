---
적용: 항상
---

# 종구의 식당 영업 및 성장 시스템

## 1. 허브 운영 개요

허브는 탐험 후 정리, 성장, 식당 영업이 모두 이뤄지는 핵심 공간이다.
플레이어는 허브를 직접 걸어 다니며 메뉴를 고르고, 창고를 정리하고, 업그레이드를 진행하고, 새로운 지역으로 이동한다.

## 2. 식당 영업

### 메뉴 데이터

- 메뉴는 `RecipeData`로 관리한다.
- 각 레시피는 표시 이름, 설명, 필요 재료, 판매가, 평판 변화를 정의한다.
- 현재 생성된 레시피는 다음과 같다.
  - `Sushi Set`
  - `Seafood Soup`
  - `Herb Fish Soup`
  - `Forest Basket`
  - `Glow Moss Stew`
  - `Wind Herb Salad`

### 영업 흐름

1. 메뉴 선택
2. 필요 재료 확인
3. 영업 진행
4. 결과 텍스트 확인
5. 골드와 평판 증가 적용

### 현재 표시하는 정보

- 선택 메뉴 설명
- 필요 재료
- 현재 보유 수량
- 조리 가능 여부
- 영업 결과
- 골드 증가량
- 평판 변화량

### 영업 계산 기준

- `RestaurantManager`는 현재 선택한 레시피를 기준으로 조리 가능 수량을 계산한다.
- 기본 일일 영업 상한은 `serviceCapacity = 3`이다.
- 결과 문자열은 `오늘의 영업 결과` 성격의 요약으로 만들어져 정산 단계에 전달된다.

## 3. 인벤토리와 창고

### 인벤토리

- 재료 전용 슬롯 구조
- 시작은 `8칸`
- 중간 확장은 `12칸`
- 최종 확장은 `16칸`

### 창고

- 허브 전용 접근
- 맡기기
- 꺼내기
- 선택 품목 맡기기
- 선택 품목 꺼내기
- 모두 맡기기 또는 모두 꺼내기

창고는 탐험 후 남은 재료를 정리할 수 있게 해 허브 루프를 매끄럽게 만든다.

## 4. 경제와 성장

### 골드와 평판

- 골드는 영업 결과와 업그레이드 비용에 사용한다.
- 평판은 편의성 해금과 연결된다.
- `WindHillShortcut`은 `Reputation 6`에서 열린다.

### 업그레이드

현재 업그레이드 중심축은 다음과 같다.

- 인벤토리 확장
- 랜턴 해금

기본 비용은 다음과 같다.

- 12칸 확장: `Gold 30 + Shell x3`
- 16칸 확장: `Gold 65 + Herb x4`
- 랜턴 해금: `Gold 45 + Mushroom x2`

`UpgradeManager`는 어떤 항목을 먼저 보여 줄지 결정할 때 현재 즉시 가능한 행동을 우선한다.

## 5. 허브 상호작용 지점

- 메뉴 선택기 `RecipeSelector`
- 영업대 `ServiceCounter`
- 창고 `StorageStation`
- 업그레이드 지점 `UpgradeStation`
- 지역 포탈

이들은 모두 `E` 상호작용을 사용하며, 조건이 맞지 않으면 이유를 설명한다.

## 6. 허브 팝업과 시간 정지

- `Cooking Menu`, `Upgrade`, `Materials`, `Storage` 팝업은 모두 허브 씬의 공용 팝업 프레임을 사용한다.
- 팝업이 열리면 `PopupPauseStateUtility`를 통해 시간이 멈춘다.
- `Esc` 또는 `PopupCloseButton`으로 닫으면 이전 시간 흐름이 복구된다.

## 7. 관련 코드와 에셋

- `Assets/Scripts/Restaurant/RestaurantManager.cs`
- `Assets/Scripts/Management/Storage/StorageManager.cs`
- `Assets/Scripts/Management/Storage/StorageStation.cs`
- `Assets/Scripts/Management/Upgrade/UpgradeManager.cs`
- `Assets/Scripts/Management/Economy/EconomyManager.cs`
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Resources/Generated/GameData/Recipes/recipe-*.asset`

## 8. 현재 구현 상태

- 메뉴 선택, 영업 실행, 정산이 처음부터 끝까지 연결되어 있다.
- 창고는 선택 품목/전체 맡기기와 꺼내기를 모두 지원한다.
- 업그레이드는 현재 바로 실행 가능한 항목이 먼저 보이도록 정리되어 있다.
- 남은 작업은 주로 영업 수치와 업그레이드 비용 밸런스 조정이다.
