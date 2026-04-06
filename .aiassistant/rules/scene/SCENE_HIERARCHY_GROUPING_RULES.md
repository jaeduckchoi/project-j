---
적용: 항상
---

# 종구의 식당 씬 계층 그룹 규칙

## 1. 목적

이 문서는 지원 씬(`Hub`, `Beach`, `DeepForest`, `AbandonedMine`, `WindHill`)이 공유하는 월드 계층 그룹 기준을 정의한다.
빌더, 현재 씬 정리기, 생성 씬 감사는 모두 같은 부모 구조와 그룹 이름을 따라야 한다.

## 2. 최상위 루트

지원 씬은 다음 최상위 순서에 맞춘다.

```text
Scene
├─ SceneWorldRoot
├─ SceneGameplayRoot
├─ SceneSystemRoot
└─ Canvas
```

- `SceneWorldRoot`
  월드 비주얼과 월드 경계 오브젝트를 그룹화한다.
- `SceneGameplayRoot`
  플레이어, 스폰 지점, 포탈, 상호작용 오브젝트, 채집 오브젝트, 특수 존을 그룹화한다.
- `SceneSystemRoot`
  `GameManager`, `RestaurantManager`, `Main Camera`, `EventSystem` 같은 시스템 오브젝트를 그룹화한다.
- `Canvas`
  UI는 루트 레벨에 유지하고, 내부는 `HUDRoot`, `PopupRoot`로 구성한다.

## 3. 공용 자식 그룹

### `SceneWorldRoot`

```text
SceneWorldRoot
├─ WorldVisualRoot
└─ WorldBoundsRoot
```

- `WorldVisualRoot`
  바닥, 배경, 소품, 월드 제목 비주얼을 둔다.
- `WorldBoundsRoot`
  `CameraBounds`, 이동 한계, 맵 벽 콜라이더를 둔다.

### `SceneGameplayRoot`

현재 씬에 필요한 그룹만 만든다.

```text
SceneGameplayRoot
├─ PlayerRoot
├─ SpawnRoot
├─ PortalRoot
├─ InteractionRoot   (hub)
├─ ResourceRoot      (exploration scenes)
└─ ZoneRoot          (scenes with special zones)
```

- `PlayerRoot`
  `Jonggu`를 둔다.
- `SpawnRoot`
  씬 진입 스폰 지점을 둔다.
- `PortalRoot`
  씬 이동 포탈을 둔다.
- `InteractionRoot`
  `RecipeSelector`, `ServiceCounter`, `StorageStation`, `UpgradeStation` 같은 허브 상호작용 오브젝트를 둔다.
- `ResourceRoot`
  채집 오브젝트를 둔다.
- `ZoneRoot`
  가이드 트리거, 늪지, 어둠 지대, 돌풍 지대 같은 특수 구역을 둔다.

## 4. 오브젝트 배치 규칙

- 허브 아트는 `HubArtRoot`를 통해 `WorldVisualRoot` 아래에 둔다.
- `BeachPortalPad`, `ForestPortalPad`, `MinePortalPad`, `WindPortalPad` 같은 포탈 패드는 대응되는 포탈 오브젝트의 자식으로 유지한다.
- 탐험 씬의 `*_Pad` 채집 패드는 대응되는 채집 오브젝트의 자식으로 유지한다.
- `*_Title` 같은 월드 라벨은 대응되는 월드 비주얼 또는 상호작용 앵커에 맞춰 정렬한다.
- `CameraBounds`, `*MovementBounds`, `*Bounds` 같은 이동 제한 오브젝트는 `WorldBoundsRoot` 아래에 둔다.

## 5. 씬별 배치 예시

### Hub

- `HubArtRoot` -> `WorldVisualRoot`
- `HubMovementBounds`, `CameraBounds` -> `WorldBoundsRoot`
- `HubEntry` -> `SpawnRoot`
- `GoToBeach`, `GoToDeepForest`, `GoToAbandonedMine`, `GoToWindHill` -> `PortalRoot`
- `RecipeSelector`, `ServiceCounter`, `StorageStation`, `UpgradeStation` -> `InteractionRoot`
- `GameManager`, `RestaurantManager`, `Main Camera`, `EventSystem` -> `SceneSystemRoot`

### 공용 탐험 기준

- 바닥, 소품, 월드 타이틀 -> `WorldVisualRoot`
- `CameraBounds`, 이동 한계, 맵 벽 -> `WorldBoundsRoot`
- 진입 스폰과 복귀 포탈 -> `SpawnRoot`, `PortalRoot`
- 채집 오브젝트 -> `ResourceRoot`
- 가이드 또는 특수 존 -> `ZoneRoot`

## 6. `ZoneRoot` 예시

- `DeepForest`
  `ForestGuide`, `ForestSwampZone`
- `AbandonedMine`
  `MineGuide`, `MineDarkness`
- `WindHill`
  `WindGuide`, `WindLaneZone`

추가 런타임 안전장치 오브젝트가 생기더라도 가능하면 같은 `ZoneRoot` 기준을 따른다.

## 7. 작업 원칙

- 결과 씬만 직접 수정하지 않는다. 빌더와 정리기가 같은 그룹 규칙을 따르도록 함께 갱신한다.
- 그룹 이름이 바뀌면 생성 씬 감사와 관련 문서도 함께 갱신한다.
- UI 그룹 규칙은 `ui/UI_GROUPING_RULES.md`에서 별도로 정의한다.
- Unity에서 최종 씬 저장을 직접 검증하지 못했다면 `Tools > Jonggu Restaurant > Prototype Build and Audit` 또는 `Organize Active Scene Hierarchy`를 통해 최종 상태를 확인한다.
