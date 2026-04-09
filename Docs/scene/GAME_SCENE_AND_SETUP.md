---
적용: 항상
---

# 씬 설정 기준

## 지원 씬

| 씬 | 역할 |
| --- | --- |
| `Hub` | 허브, 메뉴 선택, 영업 결과, 창고, 업그레이드, 지역 이동 |
| `Beach` | 기본 채집과 복귀 흐름 검증 |
| `DeepForest` | 버섯/허브 채집과 이동 감속 구역 검증 |
| `AbandonedMine` | 등불 요구, 어둠 구역, `Glow Moss` 검증 |
| `WindHill` | 바람 구역과 평판 기반 지름길 검증 |

게임플레이 의도와 밸런스 맥락은 `Docs/gameplay/GAME_DESIGN_OVERVIEW.md`, 탐험 세부 내용은 `Docs/gameplay/GAMEPLAY_EXPLORATION.md`를 봅니다.

## 씬 정본 규칙

- 지원 씬에 직접 저장된 `Transform`, 컴포넌트 직렬화 값, `SpriteRenderer`, 월드 `TextMeshPro` 값은 기본 정본입니다.
- 플레이 중 필요한 월드 오브젝트와 참조는 지원 씬 직렬화에 직접 저장해 유지합니다.
- 빌더는 기존 지원 씬을 재생성하거나 기존 월드 직렬화 값을 강제로 덮어쓰는 용도로 쓰지 않습니다.
- 지원 Canvas 씬의 관리 대상 UI 값은 `Assets/Resources/Generated/ui-layout-overrides.asset`을 통해 공용 기준으로 유지됩니다.

## 현재 점검할 핵심 연결

### `GameManager`

- `InventoryManager`
- `StorageManager`
- `EconomyManager`
- `ToolManager`
- `DayCycleManager`
- `UpgradeManager`

### `UIManager`

현재 문서와 함께 맞춰야 하는 핵심 직렬화 필드는 아래입니다.

- `guideText`
- `resultText`
- `guideHelpButton`
- `popupCloseButton`
- 허브 하단 패널 버튼

현재 UI 기준 동작은 아래와 같습니다.

- 허브 팝업은 열릴 때 게임 시간을 멈추고 닫힐 때 복구합니다.
- `StorageStation`은 허브 상호작용으로 창고를 엽니다.
- Canvas 공용 루트 이름은 `HUDRoot`, `PopupRoot`를 유지합니다.

## 씬 계층 기준

월드 계층의 정본 이름과 부모 규칙은 `Docs/scene/SCENE_HIERARCHY_GROUPING_RULES.md`를 따릅니다.
Canvas 내부 계층은 `Docs/ui/UI_GROUPING_RULES.md`를 따릅니다.

## 씬별 고위험 지점

### `Hub`

- `HubArtRoot`, `HubBar`, `HubBarLeftVisual`, `HubBarRightVisual`
- `RecipeSelector`, `ServiceCounter`, `StorageStation`, `UpgradeStation`
- `GoToBeach`, `GoToDeepForest`, `GoToAbandonedMine`, `GoToWindHill`
- `CameraBounds`

허브 아트나 카운터 구조를 바꾸면 generated 허브 스프라이트, `HubRoomLayout`, 빌더, 지원 씬 직렬화를 함께 확인합니다.

### 탐험 씬 공통

- `GatherableResource.resourceData`
- `GatherableResource.requiredToolType`
- 복귀 `ScenePortal`
- `MovementModifierZone`
- `DarknessZone`
- `WindGustZone`

## 확인용 도구

- 구조 감사: `Assets/Editor/PrototypeSceneAudit.cs`
- 경량 게임플레이 감사: `Assets/Editor/GameplayAutomationAudit.cs`
- 통합 메뉴: `Tools > Jonggu Restaurant > Prototype Build and Audit`

Unity 재생이나 컴파일을 직접 확인하지 못한 작업이라면 결과 보고에 그 사실을 함께 남깁니다.
