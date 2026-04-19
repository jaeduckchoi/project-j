# 빌드 및 생성 가이드

## 현재 기준

이 저장소는 통합 빌더/감사 메뉴 없이 운영합니다.
정적 이미지 에셋 복사, 허브 아트 생성, Canvas 자동 동기화, Build Settings 자동 정리 같은 자동화 흐름을 전제로 두지 않습니다.

## 유지되는 기준

- generated 경로의 정본은 `Assets/Code/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- UI generated 리소스 경로는 `Assets/Resources/Generated/Sprites/UI/{Buttons,MessageBoxes,Panels}`
- 플레이어 generated 리소스 경로는 `Assets/Resources/Generated/Sprites/Player`
- 게임 데이터 generated 경로는 `Assets/Resources/Generated/GameData`

## 작업 원칙

- generated 결과물 본문만 직접 수정하지 말고, 실제 정본 코드나 수동 정본 에셋을 먼저 수정합니다.
- scene/prefab 직렬화가 직접 기대는 Unity import metadata는 `scene-integrated metadata`로 구분합니다. 생성 경로나 import 정책을 먼저 확인하되, 직렬화 계약을 유지하려면 관련 `.meta`와 scene reference를 같은 변경에서 맞춥니다.
- 정적 허브 이미지나 씬 구조를 자동 생성 흐름에 기대하지 않습니다.
- 씬에 저장할 helper object 여부와 에디터 프리뷰의 dirty 계약은 build 규칙이 아니라 `SOURCE_OF_TRUTH.md`, `Docs/scene/*` 기준으로 관리합니다.
- Build Settings, 씬 저장, 하이어라키 정리는 필요 시 Unity 에디터에서 직접 확인합니다.
- 검증은 현재 남아 있는 런타임 코드와 씬 직렬화 상태를 기준으로 진행합니다.

## 검증

- UI 변경: `Assets/Code/Scripts/UI/UIManager.cs`, `Assets/Code/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs`, `PrototypeUILayout*.cs`, `PrototypeUIObjectNames.cs`, `PrototypeUILayoutBindingSettings.cs`, `Assets/Code/Scripts/Shared/PopupInteractionBindingSettings.cs`, `Assets/Resources/Generated/ui-layout-bindings.asset`, `Assets/Resources/Generated/popup-interaction-bindings.asset`
- generated 경로 변경: `Assets/Code/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- `scene-integrated metadata` 변경: 관련 import metadata, scene/prefab 직렬화 값, `Docs/project/SOURCE_OF_TRUTH.md`, `Docs/scene/GAME_SCENE_AND_SETUP.md`
- 로컬 상태/게임 데이터 변경: `Assets/Code/Scripts/CoreLoop/Core/GameManager.cs`, `Assets/Code/Scripts/Shared/Data/GeneratedGameDataLocator.cs`, 관련 runtime manager

Unity 실행이나 컴파일을 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 결과에 함께 적습니다.
