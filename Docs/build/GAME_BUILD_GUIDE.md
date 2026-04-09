# 빌드 및 생성 가이드

## 현재 기준

이 저장소에는 더 이상 `Tools > Jonggu Restaurant > Prototype Build and Audit` 같은 통합 빌더/감사 메뉴가 없습니다.
정적 이미지 에셋 복사, 허브 아트 생성, Canvas 자동 동기화, Build Settings 자동 정리, generated scene audit 흐름도 함께 제거되었습니다.

## 유지되는 기준

- generated 경로의 정본은 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- UI generated 리소스 경로는 `Assets/Resources/Generated/Sprites/UI/{Buttons,MessageBoxes,Panels}`
- 플레이어 generated 리소스 경로는 `Assets/Resources/Generated/Sprites/Player`
- 게임 데이터 generated 경로는 `Assets/Resources/Generated/GameData`

## 작업 원칙

- generated 결과물만 직접 수정하지 말고, 실제 정본 코드나 수동 정본 에셋을 먼저 수정합니다.
- 빌더가 대신 생성해 주던 정적 허브 이미지나 씬 구조를 기대하지 않습니다.
- Build Settings, 씬 저장, 하이어라키 정리는 필요 시 Unity 에디터에서 직접 확인합니다.
- 검증은 삭제된 빌더 메뉴가 아니라 현재 남아 있는 런타임 코드와 씬 직렬화 상태를 기준으로 진행합니다.

## 검증

- UI 변경: `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`, `Assets/Resources/Generated/ui-layout-overrides.asset`
- generated 경로 변경: `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`
- API 연동 변경: `Assets/Scripts/CoreLoop/Core/JongguApiSession.cs`, `Assets/Scripts/CoreLoop/Core/GameManager.cs`, `D:\project-j-api` 계약 문서

Unity 실행이나 컴파일을 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 결과에 함께 적습니다.
