# 씬 및 설정 가이드

## 기본 원칙

- `Assets/Scenes`의 실제 씬 직렬화 값이 정본입니다.
- 런타임 코드는 누락된 오브젝트, 컴포넌트, 참조만 보강해야 합니다.

## 주요 기준

- 씬 구조 정리: `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 월드 레이아웃 상수: 관련 `Exploration/World/*` 코드
- 카메라 월드 경계: `WorldBoundsRoot/CameraBounds`가 MainCamera의 하드 제한 정본입니다. `boundsOverride`는 그 안에서만 추가 clamp를 주고, viewport가 bounds보다 크면 카메라는 해당 bounds에 맞게 자동으로 zoom in 합니다.
- 에디터 정리 도구는 `CameraBounds`의 authored 위치와 크기를 자동으로 덮어쓰지 않습니다.
- 허브 논리 타일 계약: `Assets/Scripts/Exploration/World/HubRoomLayout.cs`
  현재 Hub 씬은 32x18 논리 타일 그리드를 기준으로 하고, 직렬화된 월드 좌표도 1타일 = 1유닛 기준으로 맞춘다.
  `Back Counter`, `Front Counter`, `Mosaic Tile Floor`, `Mosaic Tile Wall` 는 32px 소스 타일 아트라서 씬에서 `scale 3.125` 로 월드 타일 크기를 맞춘다.
- `Beach` 시각 루트는 `WorldVisualRoot/BeachTilemapRoot`, `WorldVisualRoot/BeachDecorRoot`를 기준으로 두고, 장식 오브젝트는 `BeachBoatRoot`, `BeachLandmarkRoot`, `BeachTreeRoot` 아래에 분류합니다.
- UI 구조: `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`, `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`, `Assets/Resources/Generated/ui-layout-overrides.asset`

## 검증

- 현재는 씬 직렬화, 관련 런타임 코드, 정적 검색 결과를 기준으로 검증합니다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과 보고에 그 사실을 함께 적습니다.
