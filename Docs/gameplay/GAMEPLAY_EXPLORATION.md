# 탐험 시스템 기획

## 1. 탐험의 비중

탐험은 플레이 흐름에서 가장 큰 시간 비중을 차지한다.
플레이어는 지역을 직접 돌아다니며 자원을 모으고, 도구 요구 조건과 환경 위험을 확인하고, 언제 돌아갈지 판단한다.

## 2. 공용 조작과 상호작용

- 이동 : `WASD` 또는 방향키
- 상호작용 : `E`
- 채집 가능 거리에 들어가면 가장 가까운 유효 대상이 자동으로 선택된다.
- 채집 대상이 도구 부족으로 막혀 있어도 차단 사유는 항상 표시한다.

## 3. 도구 해금

| 도구 | 해금 시점 | 역할 |
| --- | --- | --- |
| `Rake` | 시작부터 | 지면 채집 |
| `Fishing Rod` | 시작부터 | 바다 채집 |
| `Sickle` | 시작부터 | 풀/식물 채집 |
| `Lantern` | 업그레이드 해금 | 어둠 지대 진입 |

- 도구는 인벤토리 슬롯을 차지하지 않는 영구 해금 아이템이다.
- 한 번 해금하면 이후 모든 탐험에서 사용할 수 있다.

## 4. 탐험 지역

### Beach (입문)

- 자원 : `Fish`, `Shell`, `Seaweed`
- 목적 : 기본 이동, 채집, 복귀, 영업의 한 사이클을 익히게 한다.
- 위험 : 거의 없음. 안전한 입문 환경.

### DeepForest (중반)

- 자원 : `Mushroom`, `Herb`
- 목적 : 재료 다양화와 인벤토리 확장 동기 부여.
- 위험 : `ForestSwampZone` 감속, 가이드 존, 경로 선택 압박.

### AbandonedMine (후반)

- 자원 : `Glow Moss`
- 목적 : `Lantern` 해금 후 진입 가능한 고위험 지역.
- 위험 : `MineDarkness`, 좁은 동선.
- 메모 : 런타임 안전장치가 `MineLooseRubble` 같은 보조 위험을 추가할 수 있다.

### WindHill (최종)

- 자원 : `Wind Herb`
- 목적 : 반복 파밍과 평판 기반 편의 해금.
- 위험 : `WindLaneZone` 돌풍, 평판 6 `WindHillShortcut`.

## 5. 환경 위험 종류

- **감속 지대** (`MovementModifierZone`)
  늪지나 잔해처럼 지나가기 어려운 지형에 사용한다.
- **어둠 지대** (`DarknessZone`)
  랜턴이 없으면 이동과 시야가 크게 불편해지도록 의도한다.
- **돌풍 지대** (`WindGustZone`)
  일정 주기로 플레이어를 한 방향으로 밀어낸다.

## 6. 포탈 잠금 규칙

- 탐험 지역 진입 포탈은 day/phase 제한을 두지 않는다.
- 허브 복귀 포탈은 언제든 허브로 돌아갈 수 있게 유지한다.
- 포탈은 portal rule 기준의 필요 도구와 필요 평판만 확인한다.
- 조건이 부족하면 일반 행동 프롬프트 대신 차단 사유를 보여 준다.

## 7. 플레이테스트 확인 항목

- 자원이 의도한 도구 요구 조건에 따라 올바르게 막히는가?
- 차단 사유가 명확하게 전달되는가?
- 복귀 포탈이 허브 복귀 흐름을 유지하는가?
- 위험 지대가 시각적으로 구분되는가?
- 인벤토리 압박이 의미 있는 수집 판단을 만드는가?
- `WindHillShortcut`이 평판 6 이후 열리는가?

## 8. 시스템 연결 지점

- 채집 : `Assets/Scripts/Exploration/Gathering/GatherableResource.cs`
- 포탈 : `Assets/Scripts/Exploration/World/ScenePortal.cs`
- 위험 지대 : `DarknessZone.cs`, `MovementModifierZone.cs`, `WindGustZone.cs`
- 런타임 보강 : `Assets/Scripts/Exploration/World/PrototypeSceneRuntimeAugmenter.cs`
- 계층 카탈로그 : `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs`
