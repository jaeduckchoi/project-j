# 씬 및 설정 가이드

## 기본 원칙

- `Assets/Scenes`의 실제 씬 직렬화 값이 정본입니다.
- 런타임 코드는 누락된 오브젝트, 컴포넌트, 참조만 보강해야 합니다.

## 주요 기준

- 씬 구조 정리: `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 월드 레이아웃 상수: 관련 `Exploration/World/*` 코드
- 허브 논리 타일 계약: `Assets/Scripts/Exploration/World/HubRoomLayout.cs`
  현재 Hub 씬은 32x18 논리 타일 그리드를 기준으로 하고, 직렬화된 월드 좌표도 1타일 = 1유닛 기준으로 맞춘다.
  `Back Counter`, `Front Counter`, `Mosaic Tile Floor`, `Mosaic Tile Wall` 는 32px 소스 타일 아트라서 씬에서 `scale 3.125` 로 월드 타일 크기를 맞춘다.
- UI 구조: `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/Layout/Catalog/PrototypeUISceneLayoutCatalog.cs`, `Assets/Scripts/UI/Layout/Definitions/PrototypeUISceneLayoutSettings.cs`, `Assets/Resources/Generated/ui-layout-overrides.asset`

## 검증

- 현재는 씬 직렬화, 관련 런타임 코드, 정적 검색 결과를 기준으로 검증합니다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과 보고에 그 사실을 함께 적습니다.
