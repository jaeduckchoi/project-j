# 에이전트 작업 절차

## 기본 루프

1. [AGENTS.md](../../AGENTS.md) 또는 [CLAUDE.md](../../CLAUDE.md), [Docs/README.md](../README.md)를 읽는다.
2. [GAME_ASSISTANT_RULES.md](GAME_ASSISTANT_RULES.md)와 [GAME_DOCS_INDEX.md](GAME_DOCS_INDEX.md)에서 현재 작업의 정본 문서를 고른다.
3. 실제 코드, 씬, generated 경로 상태를 확인한다.
4. [SOURCE_OF_TRUTH.md](SOURCE_OF_TRUTH.md)로 정본 경계를 먼저 정한 뒤 구현한다.
5. 관련 문서와 검증 결과를 같은 변경에서 마무리한다.

`Skills/`는 루트의 에이전트 워크플로 자산이다. 스킬을 수정할 때도 게임 규칙과 정본 관계는 `Docs/Project/*` 문서를 먼저 따른다.

## 작업별 체크포인트

### 구조 리팩토링

- 파일 이동 전 asmdef 범위, namespace, `.meta` 보존 여부를 확인한다.
- 이동 후 옛 경로 문자열이 코드와 문서에 남지 않았는지 정적 검색한다.
- public 타입이나 공개 시그니처 의미가 바뀌면 XML 문서 주석도 같이 갱신한다.

### 씬, 하이어라키 변경

- `Assets/Level/Scenes` 직렬화 값을 정본으로 보고, 런타임 코드는 누락된 참조만 보강한다.
- `authored helper object`, `scene-integrated metadata`, editor preview dirty contract가 함께 유지되는지 확인한다.
- 씬 계층을 바꾸면 `PrototypeSceneHierarchyOrganizer`와 [SCENE_HIERARCHY_GROUPING_RULES.md](../Scene/SCENE_HIERARCHY_GROUPING_RULES.md)를 같이 본다.

### UI 변경

- 관리 대상 이름과 binding 자산 정본은 `PrototypeUISceneLayoutCatalog`와 관련 generated asset에서 확인한다.
- 에디터에서 보이는 프리뷰 경로는 `PrototypeUIDesignController`와 `UIManager.EditorPreview.cs`까지 함께 확인한다.
- UI 구조 변경이면 [UI_AND_TEXT_GUIDE.md](../UI/UI_AND_TEXT_GUIDE.md)와 [UI_GROUPING_RULES.md](../UI/UI_GROUPING_RULES.md)를 같이 갱신한다.

### generated 경로, 데이터 변경

- 경로 정본은 `PrototypeGeneratedAssetSettings.cs`다.
- generated 결과물 본문만 직접 고치지 말고 생성 경로나 정본 코드를 수정한다.
- CSV 원본, generated GameData, fallback 데이터가 같이 맞는지 확인한다.

## 검증 체크포인트

- 구조, 경로, 문서 리팩토링 뒤에는 옛 경로 문자열이 남지 않았는지 검색한다.
- 씬, 에디터 프리뷰 동기화 변경은 씬 재오픈 시 dirty 없음, 수동 rebuild 전후 결과 일치를 기준으로 본다.
- UI 변경은 관리 이름, binding 자산, editor preview 경로가 같이 맞는지 확인한다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과에 남은 검증 단계를 적는다.
