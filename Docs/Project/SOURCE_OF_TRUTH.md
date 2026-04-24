# 정본 관계 가이드

이 문서는 어떤 대상의 정본이 어디에 있는지와, 함께 맞춰야 하는 결합 지점을 빠르게 찾기 위한 기준 문서다.

## 문서 정본

| 대상 | 정본 | 함께 맞출 항목 | 예외 |
| --- | --- | --- | --- |
| 작업 진입 | [Docs/README.md](../README.md) | [AGENTS.md](../../AGENTS.md), [CLAUDE.md](../../CLAUDE.md), [GAME_DOCS_INDEX.md](GAME_DOCS_INDEX.md) | 루트 엔트리는 맵만 유지한다. |
| 전역 규칙 | [GAME_ASSISTANT_RULES.md](GAME_ASSISTANT_RULES.md) | `.claude/settings.json`, `.codex/config.toml` | 읽기 제외 기준이 바뀔 때만 설정을 같이 수정한다. |
| 저장소 구조와 대표 경로 | [GAME_PROJECT_STRUCTURE.md](GAME_PROJECT_STRUCTURE.md) | 코드 주석, 루트 README, 관련 도메인 문서 링크 | 정책 설명은 이 문서가 아니라 각 도메인 문서가 소유한다. |
| 작업 매핑 | [GAME_DOCS_INDEX.md](GAME_DOCS_INDEX.md) | [Docs/README.md](../README.md) | 이 문서는 링크 허브만 맡고 상세 규칙은 소유하지 않는다. |
| 에이전트 스킬 자산 | `Skills/*` | [Docs/README.md](../README.md), [GAME_PROJECT_STRUCTURE.md](GAME_PROJECT_STRUCTURE.md) | 스킬은 실행 보조 자산이며 게임 규칙과 정본 관계를 소유하지 않는다. |

## 코드와 자산 정본

| 대상 | 정본 | 함께 맞출 항목 | 예외 |
| --- | --- | --- | --- |
| 씬 직렬화 | `Assets/Level/Scenes/*` | `PrototypeSceneHierarchyOrganizer.cs`, [GAME_SCENE_AND_SETUP.md](../Scene/GAME_SCENE_AND_SETUP.md), [SCENE_HIERARCHY_GROUPING_RULES.md](../Scene/SCENE_HIERARCHY_GROUPING_RULES.md) | 런타임 코드는 누락된 오브젝트, 컴포넌트, 참조만 최소 범위로 보강한다. |
| `authored helper object` | 저장 의도를 가진 씬 오브젝트 자체와 `Assets/Resources/Generated/scene-hierarchy-contracts.asset` | 관련 씬 직렬화 값, `SceneAuthoredHelperContractMarker`, 동기화 컴포넌트, [GAME_SCENE_AND_SETUP.md](../Scene/GAME_SCENE_AND_SETUP.md) | 임시 프리뷰 오브젝트와 혼동하지 않는다. marker가 없는 helper는 자동 contract 대상이 아니다. |
| 허브 타일과 카메라 계약 | `Assets/Code/Scripts/Exploration/World/HubRoomLayout.cs`, `WorldBoundsRoot/CameraBounds` | `Assets/Level/Scenes/Hub.unity`, [HUB_WHITEBOX.md](../Scene/HUB_WHITEBOX.md) | 허브 최종 배치는 씬 직렬화가 정본이다. |
| Canvas 관리 UI | `PrototypeUISceneLayoutCatalog.cs`, `PrototypeUILayout*.cs`, `Assets/Resources/Generated/ui-layout-bindings.asset` | `UIManager.cs`, `UIManager.*.cs`, `PrototypeUIDesignController.cs`, [UI_AND_TEXT_GUIDE.md](../UI/UI_AND_TEXT_GUIDE.md), [UI_GROUPING_RULES.md](../UI/UI_GROUPING_RULES.md) | binding 자산의 hierarchy contract가 있으면 parent/sibling과 `initialActive` baseline을 정본으로 취급한다. 런타임은 구조를 임의로 바꾸지 않고, 플레이 중 active 상태는 runtime state가 소유하며, 계약 누락 시에만 fallback grouping을 사용한다. |
| 팝업 월드 연결 | `Assets/Resources/Generated/popup-interaction-bindings.asset` | `PopupInteractionBindingSettings.cs`, 관련 station 컴포넌트, [UI_AND_TEXT_GUIDE.md](../UI/UI_AND_TEXT_GUIDE.md) | 편집기는 binding 자산과 씬 station 연결을 같이 맞춘다. |
| UI 텍스트와 스타일 | `PrototypeUIPopupCatalog.cs`, `PrototypeUISkin*.cs`, `PrototypeUIObjectNames.cs` | `UIManager.cs`, 관련 popup UI 컴포넌트, [UI_AND_TEXT_GUIDE.md](../UI/UI_AND_TEXT_GUIDE.md) | 씬에 직접 저장된 popup 텍스트와 폰트는 명시적 요청 없이는 덮어쓰지 않는다. |
| TMP 폰트 자산 | `Assets/TextMesh Pro/Fonts/Galmuri11.ttf`, `Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf`, 대응 TMP Font Asset | UI 씬 직렬화 값, `PrototypeUISkin.cs`, [UI_AND_TEXT_GUIDE.md](../UI/UI_AND_TEXT_GUIDE.md) | 씬 저장 폰트 참조와 TMP fallback을 같이 본다. |
| generated 경로 | `Assets/Code/Scripts/Shared/PrototypeGeneratedAssetSettings.cs` | `Assets/Resources/Generated/*`, [GAME_PROJECT_STRUCTURE.md](GAME_PROJECT_STRUCTURE.md), [GAME_BUILD_GUIDE.md](../Build/GAME_BUILD_GUIDE.md) | generated 출력물 본문은 정본이 아니다. |
| authored CSV 게임 데이터 | `Assets/Data/GameDataSource/*.csv`, `CsvGameDataImporter.cs` | generated GameData, `GeneratedGameDataLocator.cs`, [GAME_BUILD_GUIDE.md](../Build/GAME_BUILD_GUIDE.md) | CSV 원본만 바꾸고 generated, fallback 검증을 생략하지 않는다. |
| `scene-integrated metadata` | scene, prefab 직렬화가 직접 기대는 import metadata | 관련 `.meta`, scene, prefab 참조 값, [GAME_SCENE_AND_SETUP.md](../Scene/GAME_SCENE_AND_SETUP.md), [GAME_BUILD_GUIDE.md](../Build/GAME_BUILD_GUIDE.md) | generated 출력물 본문과 같은 것으로 취급하지 않는다. |
| authored 아트 import | `Assets/Code/Editor/Art/ArtSpriteImportPostprocessor.cs` | `Assets/Art/*`, 관련 `.meta`, 씬 참조 값 | `Assets/Resources/Generated/*`는 authored import 규칙의 정본이 아니다. |
| 로컬 게임 데이터와 fallback | generated GameData, `GeneratedGameDataLocator.cs`, `GameManager.cs` | 인벤토리, 창고, 경제, 도구, 업그레이드 흐름 | 외부 네트워크 상태를 정본으로 두지 않는다. |

## 정본이 아닌 것

- generated PNG 본문과 generated 출력물 자체
- 임시 복구 메모와 과거 감사 로그
- 더 이상 사용하지 않는 자동 생성, 자동 감사 흐름에 대한 설명
- `Skills/*`에 복제된 안내 문구. 상세 규칙은 `Docs/Project/*` 정본 문서가 소유한다.

단, `scene-integrated metadata`처럼 scene serialization contract를 직접 성립시키는 Unity 메타데이터는 예외로 두고 이 문서와 관련 씬 문서 기준으로 함께 관리한다.
