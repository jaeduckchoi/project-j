# 씬 및 설정 가이드

## 역할

이 문서는 씬 직렬화, `authored helper object`, editor preview dirty contract를 다룰 때 읽는 `Scene` 도메인의 상위 정본이다.

## 이 문서를 읽는 시점

- 씬, prefab, 월드 배치, 카메라 경계를 수정할 때
- 씬 저장 상태와 에디터 프리뷰 동기화 방식을 검토할 때
- 함께 읽을 문서: [SOURCE_OF_TRUTH.md](../Project/SOURCE_OF_TRUTH.md), [SCENE_HIERARCHY_GROUPING_RULES.md](SCENE_HIERARCHY_GROUPING_RULES.md), [HUB_WHITEBOX.md](HUB_WHITEBOX.md), [BEACH_WHITEBOX.md](BEACH_WHITEBOX.md)

## 정본 범위

- `Assets/Level/Scenes`의 실제 씬 직렬화 값이 정본이다.
- 씬에 저장을 의도한 보조 렌더러, 분할 조각, 동기화용 자식 오브젝트는 `authored helper object`로 보고 씬 정본에 포함한다.
- catalog 관리 대상과 `SceneAuthoredHelperContractMarker`가 붙은 helper 오브젝트의 parent/sibling/initialActive baseline 값은 `Assets/Resources/Generated/scene-hierarchy-contracts.asset`로 함께 동기화한다.
- scene, prefab이 직접 참조하는 import metadata는 `scene-integrated metadata`로 보고 generated 출력물 본문과 분리해서 다룬다.
- `ExecuteAlways` 계열의 씬 동기화 컴포넌트는 저장된 authored 상태에 수렴해야 하며, 씬 로드, 도메인 리로드, `OnValidate`에서 자동으로 scene dirty를 만들지 않는다.
- `WorldBoundsRoot/CameraBounds`가 MainCamera의 하드 제한 정본이다.
- 허브 논리 타일 계약은 `Assets/Code/Scripts/Exploration/World/HubRoomLayout.cs`가 소유한다.
- `Beach` 시각 루트와 장식 루트 분류는 실제 씬 직렬화와 관련 월드 코드가 같이 유지한다.

## 함께 수정할 항목

- 씬 구조 정리 도구: `Assets/Code/Editor/PrototypeSceneHierarchyOrganizer.cs`
- 씬 hierarchy contract 동기화: `Assets/Code/Editor/PrototypeSceneHierarchyContractSyncUtility.cs`, `SceneHierarchyContractSettings.cs`, `SceneAuthoredHelperContractMarker.cs`
- 월드 레이아웃 상수와 관련 런타임 코드: `Assets/Code/Scripts/Exploration/World/*`
- 화이트박스 기준: [HUB_WHITEBOX.md](HUB_WHITEBOX.md), [BEACH_WHITEBOX.md](BEACH_WHITEBOX.md)
- 씬과 UI가 만나는 지점: [UI_AND_TEXT_GUIDE.md](../UI/UI_AND_TEXT_GUIDE.md)
- generated 경로나 metadata 판단이 얽히면 [GAME_BUILD_GUIDE.md](../Build/GAME_BUILD_GUIDE.md)를 같이 본다.

## 검증

- 씬 재오픈 직후 dirty 없음
- 수동 rebuild 전후 결과 일치
- 숨김 프리뷰와 저장 대상 오브젝트가 뒤섞이지 않음
- 카메라 bounds, authored helper object, marker가 붙은 helper contract, 씬 참조가 유지됨
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 결과에 적는다.
