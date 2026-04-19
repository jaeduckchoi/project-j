# 빌드 및 생성 가이드

## 역할

이 문서는 generated 경로, 생성 흐름 전제, `scene-integrated metadata` 처리 기준을 정리하는 `Build` 도메인의 상위 정본이다.

## 이 문서를 읽는 시점

- generated 자산 경로를 바꾸거나 생성 결과를 검토할 때
- import metadata와 씬 직렬화 계약이 함께 얽힐 때
- 함께 읽을 문서: [SOURCE_OF_TRUTH.md](../Project/SOURCE_OF_TRUTH.md), [GAME_SCENE_AND_SETUP.md](../Scene/GAME_SCENE_AND_SETUP.md)

## 정본 범위

- generated 경로의 정본은 `Assets/Code/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`다.
- UI generated 리소스 경로는 `Assets/Resources/Generated/Sprites/UI`다.
- 플레이어 generated 리소스 경로는 `Assets/Resources/Generated/Sprites/Player`다.
- 게임 데이터 generated 경로는 `Assets/Resources/Generated/GameData`다.
- 이 저장소는 통합 빌더, 감사 메뉴, 자동 정리 흐름을 전제로 두지 않는다.
- `scene-integrated metadata`는 generated 출력물 본문과 분리해서, 씬 직렬화 계약의 일부로 관리한다.

## 함께 수정할 항목

- generated 경로: `PrototypeGeneratedAssetSettings.cs`, [GAME_PROJECT_STRUCTURE.md](../Project/GAME_PROJECT_STRUCTURE.md)
- 게임 데이터 생성: `CsvGameDataImporter.cs`, `GeneratedGameDataLocator.cs`
- UI binding 자산: `Assets/Resources/Generated/ui-layout-bindings.asset`, `Assets/Resources/Generated/popup-interaction-bindings.asset`
- import metadata와 씬 참조: 관련 `.meta`, scene, prefab, [GAME_SCENE_AND_SETUP.md](../Scene/GAME_SCENE_AND_SETUP.md)

## 검증

- generated 결과물 본문이 아니라 정본 코드나 수동 정본 자산을 수정했는지 확인한다.
- generated 경로 변경 뒤 관련 리소스 로드 경로와 문서 경로가 같이 맞는지 확인한다.
- `scene-integrated metadata` 변경이면 관련 scene, prefab 참조 값이 유지되는지 확인한다.
- Build Settings, 씬 저장, 하이어라키 정리는 필요 시 Unity 에디터에서 직접 확인한다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 결과에 적는다.
