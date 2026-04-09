---
적용: 항상
---

# 씬 계층 구조 규칙

## 목적

이 문서는 지원 씬의 월드 계층 이름과 부모 규칙만 정의합니다.
정본 판단은 `Docs/project/SOURCE_OF_TRUTH.md`, 작업 절차는 `Docs/project/AGENT_WORKFLOW.md`를 먼저 따릅니다.

## 최상위 루트

```text
Scene
├─ SceneWorldRoot
├─ SceneGameplayRoot
├─ SceneSystemRoot
└─ Canvas
```

- `SceneWorldRoot`: 월드 비주얼과 월드 경계
- `SceneGameplayRoot`: 플레이어, 포털, 상호작용, 채집, 특수 구역
- `SceneSystemRoot`: `GameManager`, `RestaurantManager`, `Main Camera`, `EventSystem`
- `Canvas`: UI 루트. 내부 구조는 `Docs/ui/UI_GROUPING_RULES.md`를 따릅니다.

## 공용 하위 그룹

### `SceneWorldRoot`

```text
SceneWorldRoot
├─ WorldVisualRoot
└─ WorldBoundsRoot
```

- `WorldVisualRoot`: 바닥, 배경, 소품, 월드 타이틀
- `WorldBoundsRoot`: `CameraBounds`, 이동 제한, 월드 벽 콜라이더

### `SceneGameplayRoot`

현재 씬에 필요한 그룹만 만듭니다.

```text
SceneGameplayRoot
├─ PlayerRoot
├─ SpawnRoot
├─ PortalRoot
├─ InteractionRoot
├─ ResourceRoot
└─ ZoneRoot
```

- `PlayerRoot`: `Jonggu`
- `SpawnRoot`: 씬 진입 스폰 지점
- `PortalRoot`: 지역 이동 포털
- `InteractionRoot`: 허브 상호작용 오브젝트
- `ResourceRoot`: 채집 오브젝트
- `ZoneRoot`: 가이드, 감속, 어둠, 돌풍 같은 특수 구역

## 배치 규칙

- 허브 월드 아트는 `WorldVisualRoot/HubArtRoot` 기준으로 둡니다.
- `CameraBounds`, `*MovementBounds`, `*Bounds`는 `WorldBoundsRoot`에 둡니다.
- 포털 패드와 채집 패드는 해당 오브젝트의 자식으로 둡니다.
- 월드 라벨과 타이틀은 대응하는 비주얼 또는 상호작용 앵커와 함께 이동하도록 둡니다.

## 함께 수정할 항목

- 그룹 이름 변경: `PrototypeSceneHierarchyOrganizer`, `PrototypeSceneAudit`, 빌더, 관련 문서
- 허브 아트 루트 변경: `HubRoomLayout`, 빌더, generated 허브 스프라이트 경로
- Canvas 루트 변경: `UI_GROUPING_RULES.md`, `UIManager`, 빌더, UI 오버라이드 자산 처리 코드

## 검증 경로

- 구조와 생성 경로 검증은 `Tools > Jonggu Restaurant > Prototype Build and Audit`
- 플레이 흐름 검증은 `GameplayAutomationAudit`
- Unity에서 직접 저장 결과를 확인하지 못했다면 결과 보고에 미검증 사실을 남깁니다.
