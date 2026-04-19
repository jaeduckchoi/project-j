# 씬 및 설정 가이드

## 기본 원칙

- `Assets/Scenes`의 실제 씬 직렬화 값이 정본입니다.
- 런타임 코드는 누락된 오브젝트, 컴포넌트, 참조만 보강해야 합니다.

## 씬 보조 오브젝트와 에디터 프리뷰

- 씬에 저장을 의도한 보조 렌더러, 분할 조각, 동기화용 자식 오브젝트는 `authored helper object`로 보고 씬 정본에 포함합니다.
- 런타임 보강 코드가 임시로 만들었다가 버리는 오브젝트와, 씬에 남겨두는 `authored helper object`는 구분해서 다룹니다.
- scene/prefab이 직접 참조하는 import metadata는 `scene-integrated metadata`로 보고, generated 출력물 본문과 분리해서 씬 직렬화 계약의 일부로 관리합니다.
- 숨김 프리뷰를 쓰는 경우에도 저장 여부와 hierarchy 표시 여부는 분리해서 설계하고, 저장을 의도하지 않은 프리뷰는 씬 파일에 남기지 않습니다.
- `ExecuteAlways` 계열의 씬 동기화 컴포넌트는 저장된 authored 상태에 수렴해야 하며, `editor preview dirty contract`에 따라 씬 로드, 도메인 리로드, `OnValidate`에서 자동으로 scene dirty를 만들지 않습니다.
- 저장 상태를 바꾸는 작업은 명시적 rebuild, clear, 수동 배치처럼 사용자 의도가 드러난 경로에서만 일어나야 합니다.

## 주요 기준

- 씬 구조 정리: `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 월드 레이아웃 상수: 관련 `Exploration/World/*` 코드
- 카메라 월드 경계: `WorldBoundsRoot/CameraBounds`가 MainCamera의 하드 제한 정본입니다. `boundsOverride`는 그 안에서만 추가 clamp를 주고, viewport가 bounds보다 크면 카메라는 해당 bounds에 맞게 자동으로 zoom in 합니다.
- 에디터 정리 도구는 `CameraBounds`의 authored 위치와 크기를 자동으로 덮어쓰지 않습니다.
- 허브 논리 타일 계약: `Assets/Scripts/Exploration/World/HubRoomLayout.cs`
  현재 Hub 씬은 32x18 논리 타일 그리드를 기준으로 하고, 직렬화된 월드 좌표도 1타일 = 1유닛 기준으로 맞춘다.
  `Back Counter`, `Front Counter`, `Mosaic Tile Floor`, `Mosaic Tile Wall` 는 32px 소스 타일 아트라서 씬에서 `scale 3.125` 로 월드 타일 크기를 맞춘다.
- `Beach` 시각 루트는 `WorldVisualRoot/BeachTilemapRoot`, `WorldVisualRoot/BeachDecorRoot`를 기준으로 두고, 장식 오브젝트는 `BeachBoatRoot`, `BeachLandmarkRoot`, `BeachTreeRoot` 아래에 분류합니다.
- UI 구조: `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`, `PrototypeUILayout*.cs`, `PrototypeUIObjectNames.cs`

## 검증

- 현재는 씬 직렬화, 관련 런타임 코드, 정적 검색 결과를 기준으로 검증합니다.
- 씬/에디터 프리뷰 동기화 변경은 씬 재오픈 직후 dirty 없음, 수동 rebuild 전후 결과 일치, 숨김 여부와 저장 여부 일치를 확인합니다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과 보고에 그 사실을 함께 적습니다.
