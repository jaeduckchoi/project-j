# UI 및 텍스트 기준

## 역할

이 문서는 현재 프로젝트의 Canvas UI 정본 관계와 관리 대상 구조만 다룹니다.
작업 절차와 검증 순서는 `project/AGENT_WORKFLOW.md`, 전체 정본 관계는 `project/SOURCE_OF_TRUTH.md`를 먼저 확인합니다.

## 현재 UI 기준

- Canvas 최상위 공용 루트는 `HUDRoot`, `PopupRoot`입니다.
- `Assets/Scripts/UI/UIManager.cs`가 런타임 UI 동작의 중심입니다.
- 현재 관리 대상 HUD 텍스트와 버튼 기준은 `guideText`, `resultText`, `guideHelpButton`입니다.
- `GuideText`, `RestaurantResultText`, `GuideHelpButton`은 `UIManager`, `JongguMinimalPrototypeBuilder`, `PrototypeSceneAudit`가 같은 이름 기준으로 함께 사용합니다.
- 허브 팝업(`요리 메뉴`, `업그레이드`, `재료`, `창고`)이 열리면 `PopupPauseStateUtility`를 통해 게임 진행을 일시 정지하고, 닫히면 이전 시간 흐름을 복구합니다.
- 허브 팝업 내부에서 씬에 직접 저장한 `PopupTitle`, `PopupLeftCaption`, `Image.sprite`, 폰트, 배치 값은 명시적 요청 없이는 초기화하거나 덮어쓰지 않습니다.

## UI 정본 관계

- 지원 씬의 Canvas 관리 대상 값은 `Assets/Resources/Generated/ui-layout-overrides.asset`이 공용 정본입니다.
- 위 자산은 `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`가 읽고 씁니다.
- `Assets/Editor/UI/PrototypeUICanvasAutoSync.cs`는 지원하는 Canvas 씬 저장 시 관리 대상 값을 공용 자산으로 동기화합니다.
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`와 `Assets/Scripts/UI/UIManager.cs`는 같은 오버라이드 자산 기준으로 HUD와 팝업 구조를 맞춥니다.
- 지원 씬에 이미 저장된 월드 직렬화 값은 씬이 정본이며, UI 오버라이드는 Canvas 관리 대상 값에만 적용됩니다.

## generated UI 경로

- 디자인 원본 루트: `Assets/Design/GeneratedSources/UI`
- 입력 분류: `Buttons`, `MessageBoxes`, `PanelVariants`
- 런타임 출력 루트: `Assets/Resources/Generated/Sprites/UI`
- 출력 분류: `Buttons`, `MessageBoxes`, `Panels`
- 위 경로의 실제 기준은 `Assets/Resources/Generated/prototype-generated-asset-settings.asset`과 `Assets/Scripts/Shared/PrototypeGeneratedAssetSettings.cs`입니다.
- `Assets/Design`는 디자인 원본 보관소이며, 런타임 코드가 직접 참조하지 않습니다.

## 폰트와 텍스트

- 기본 본문 TMP 폰트는 `Assets/Resources/Generated/Fonts/maplestoryLightSdf.asset`입니다.
- 기본 제목 TMP 폰트는 `Assets/Resources/Generated/Fonts/maplestoryBoldSdf.asset`입니다.
- 새 주석과 문서, 직접 관리하는 UI 텍스트 설명은 UTF-8 한글 기준으로 유지합니다.
- 파일 경로, 코드 식별자, 리소스 이름처럼 번역하면 안 되는 명칭은 원문 그대로 둡니다.

## 함께 확인할 코드

- 런타임 동작: `Assets/Scripts/UI/UIManager.cs`
- 팝업 일시 정지: `Assets/Scripts/UI/PopupPauseStateUtility.cs`
- UI 오버라이드 자산: `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
- 저장 시 자동 동기화: `Assets/Editor/UI/PrototypeUICanvasAutoSync.cs`
- 기본 구조 생성: `Assets/Editor/JongguMinimalPrototypeBuilder.UI.cs`
- 구조 감사: `Assets/Editor/PrototypeSceneAudit.cs`

## 변경 시 바로 따라올 항목

- HUD 또는 팝업 오브젝트 이름 변경: `UIManager`, 빌더, 감사 코드, `UI_GROUPING_RULES.md`
- generated UI 경로 변경: `PrototypeGeneratedAssetSettings`, 빌더, 스킨 카탈로그, 빌드 문서
- 공용 Canvas 관리 범위 변경: `PrototypeUISceneLayoutSettings`, 자동 동기화, `SOURCE_OF_TRUTH.md`