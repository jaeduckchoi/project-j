---
적용: 항상
---

# 종구의 식당 탐험 시스템

## 1. 탐험 개요

탐험은 게임에서 가장 큰 비중을 차지한다.
플레이어는 지역을 직접 돌아다니며 자원을 모으고, 도구 요구 조건과 환경 위험을 확인하며, 언제 돌아갈지 판단한다.

## 2. 공용 탐험 요소

### 이동과 상호작용

- 이동은 `WASD` 또는 방향키를 사용한다.
- 상호작용은 `E`를 사용한다.
- `InteractionDetector`가 가장 가까운 유효 상호작용 대상을 선택한다.

### 채집

- 채집 오브젝트는 `GatherableResource`를 사용한다.
- 필요 도구가 없으면 채집이 막히고, 이유를 함께 표시한다.
- 성공하면 채집한 재료가 인벤토리에 추가된다.

### 도구

- 시작 시 해금된 도구: `Rake`, `Fishing Rod`, `Sickle`
- 추가 해금 도구: `Lantern`

도구는 인벤토리 슬롯을 차지하지 않으며, 한 번 획득하면 영구 해금 상태를 유지한다.

### 포탈 잠금 규칙

- 기본적으로 탐험 지역 이동 포탈은 `Morning Explore` 단계에서만 열린다.
- 허브 복귀 포탈은 오후가 시작된 뒤에도 열려 있어 하루 루프가 자연스럽게 끝나도록 한다.
- 포탈은 필요 도구와 필요 평판도 함께 확인한다.
- 조건이 부족하면 일반 행동 프롬프트 대신 차단 이유를 보여 준다.

## 3. 지역별 특성

### Beach

- 입문 지역
- 자원: `Fish`, `Shell`, `Seaweed`
- 목적: 기본 이동과 채집 루프를 익히게 한다.
- 특징: 위험 요소가 거의 없다.

### DeepForest

- 중반 지역
- 자원: `Mushroom`, `Herb`
- 목적: 탐험과 업그레이드 재료 흐름을 연결한다.
- 특징: `ForestGuide`, `ForestSwampZone`, 경로 선택 압박이 있다.

### AbandonedMine

- 후반 지역
- 자원: `Glow Moss`
- 목적: 랜턴 해금 후 진입하는 고위험 지역이다.
- 특징: `MineGuide`, `MineDarkness`, 좁은 동선이 핵심이다.
- 메모: 런타임 보강이 `MineLooseRubble` 같은 보조 위험 오브젝트를 추가할 수 있다.

### WindHill

- 최종 지역
- 자원: `Wind Herb`
- 목적: 반복 파밍과 평판 기반 편의 성장 요소를 제공한다.
- 특징: `WindGuide`, `WindLaneZone`, `WindHillShortcut`이 핵심이다.

## 4. 환경 위험 요소

### 감속 지대

- `MovementModifierZone`으로 구현한다.
- 늪지나 잔해처럼 지나가기 어렵게 느껴져야 하는 지형에 사용한다.

### 어둠 지대

- `DarknessZone`으로 구현한다.
- 랜턴이 없으면 이동과 접근이 크게 불편해지도록 의도한다.

### 돌풍 지대

- `WindGustZone`으로 구현한다.
- 일정 주기로 플레이어를 한 방향으로 밀어낸다.

## 5. 탐험에서 확인할 것

- 자원이 의도한 도구 요구 조건에 따라 올바르게 막히는가?
- 차단 이유가 명확하게 전달되는가?
- 복귀 포탈이 허브 복귀 흐름을 유지하는가?
- 위험 지대가 시각적으로 구분되는가?
- 인벤토리 압박이 의미 있는 수집 판단을 만드는가?
- `WindHillShortcut`이 평판 6 이후 열리는가?

## 6. 관련 코드와 에셋

- `Assets/Scripts/Exploration/Gathering/GatherableResource.cs`
- `Assets/Scripts/Exploration/World/ScenePortal.cs`
- `Assets/Scripts/Exploration/World/DarknessZone.cs`
- `Assets/Scripts/Exploration/World/MovementModifierZone.cs`
- `Assets/Scripts/Exploration/World/WindGustZone.cs`
- `Assets/Scripts/Exploration/World/PrototypeSceneRuntimeAugmenter.cs`
- `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs`

## 7. 현재 구현 상태

- 네 개의 탐험 지역 `Beach`, `DeepForest`, `AbandonedMine`, `WindHill`이 연결되어 있다.
- 랜턴 조건, 돌풍, 감속, 어둠, 지름길 흐름이 코드와 씬 데이터로 연결되어 있다.
- `GameplayAutomationAudit` 경량 감사 코드는 포탈 잠금 규칙과 누락 씬 안내 동작도 함께 점검한다.
- 남은 작업은 주로 실제 플레이테스트를 바탕으로 한 경로와 밸런스 조정이다.
