# 에이전트 작업 절차

## 기본 루프

1. 루트 엔트리와 `Docs/README.md`를 읽는다.
2. `GAME_ASSISTANT_RULES.md`와 `GAME_DOCS_INDEX.md`에서 필요한 정본 문서만 고른다.
3. 실제 코드, Scenes, generated 경로 상태를 확인한다.
4. 정본 경계를 먼저 정한 뒤 구현한다.
5. 관련 문서와 검증 결과까지 같은 변경에서 마무리한다.

## 작업별 체크포인트

### UI 변경

- 결합 지점 정본은 `SOURCE_OF_TRUTH.md`의 `Canvas 관리 UI`와 `UI 구조 변경` 섹션을 따른다.
- 관리 대상 Canvas 이름은 `PrototypeUISceneLayoutCatalog` 한 곳에서만 유지한다.
- 에디터에서 보이고 저장 가능한 UI는 `PrototypeUIDesignController`와 `UIManager.EditorPreview` 경로까지 확인한다.
- `UIManager` partial을 바꾸면 관련 partial과 팝업 일시정지 흐름이 함께 맞는지 확인한다.

### 구조 리팩토링

- 파일 이동 전 asmdef 범위, namespace, `.meta` 보존 여부를 확인한다.
- partial family는 엔트리 파일과 역할별 하위 파일 경계를 먼저 정한다.
- 이동 후 옛 경로 문자열이 코드와 문서에 남지 않았는지 정적 검색한다.
- class, struct, interface, enum, public/internal 메서드를 새로 만들거나 의미를 바꾸면 XML 문서 주석도 함께 갱신한다.
- 핵심 상태 전이, 정본 보강, 데이터 소모/복구 순서는 짧은 라인 주석으로 의도를 남기되, 단순 구현 설명은 추가하지 않는다.

### 씬/하이어라키 변경

- `Assets/Level/Scenes` 직렬화 값을 정본으로 본다.
- 런타임 보강 코드는 누락된 오브젝트, 컴포넌트, 참조만 보충한다.
- 씬에 저장된 `authored helper object`가 있으면 hierarchy, 이름, 참조도 scene contract로 취급한다.
- 씬용 에디터 프리뷰/동기화 컴포넌트는 로드, 도메인 리로드, `OnValidate`에서 scene dirty를 만들지 않게 유지한다.
- 씬 계층을 바꾸면 `PrototypeSceneHierarchyOrganizer`와 `Docs/scene/*`를 함께 확인한다.

### generated 경로 변경

- 경로 정본은 `PrototypeGeneratedAssetSettings.cs`다.
- generated 결과물 본문을 직접 고치지 말고 생성 경로나 정본 코드를 수정한다.
- scene/prefab 직렬화가 직접 기대는 import metadata는 `scene-integrated metadata`로 구분하고, 관련 scene 문서와 함께 확인한다.
- 정적 generated 에셋 복구를 별도 빌더에 기대하지 않는다.

### 로컬 데이터와 런타임 상태 변경

- `GameManager`, `GeneratedGameDataLocator`, 관련 manager 초기화 흐름을 확인한다.
- 인벤토리, 창고, 경제, 도구, 업그레이드가 generated GameData와 fallback 기준에 맞는지 확인한다.

## 문서와 검증

- 동작 기준이 바뀌면 관련 문서를 같은 변경에서 갱신한다.
- 과거 빌더/감사 흐름을 현재 검증 기준처럼 남기지 않는다.
- 씬/에디터 프리뷰 동기화 변경은 재오픈 시 dirty 없음, 수동 rebuild 전후 일치, 숨김 여부와 저장 여부 일치를 검증 항목에 포함한다.
- 가능하면 관련 csproj나 Unity 검증으로 경로 이동 회귀를 확인한다.
- Unity 실행/컴파일을 직접 못 했으면 결과에 남은 검증 단계를 적는다.
