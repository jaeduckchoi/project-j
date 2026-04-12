# 에이전트 작업 절차

## 기본 루프

1. 루트 엔트리 파일(`AGENTS.md` 또는 `CLAUDE.md`)과 `.aiassistant/rules/README.md`를 읽는다.
2. `Docs/project/GAME_ASSISTANT_RULES.md`를 읽는다.
3. `Docs/project/GAME_DOCS_INDEX.md`에서 현재 작업에 맞는 문서를 찾는다.
4. 코드, 씬, generated 자산, 인접 API 저장소 상태를 실제로 확인한다.
5. 정본 경계를 먼저 결정한 뒤 구현한다.
6. 문서와 검증까지 같은 변경에서 마무리한다.

## 유형별 확인

### UI 변경

- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/UIManager.Lifecycle.cs` 및 같은 family partial
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs` (`.Editor.cs`, `.Editor.Capture.cs` 포함)
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
- `Assets/Resources/Generated/ui-layout-overrides.asset`
- 관리 대상 이름이나 그룹 루트를 바꾸면 `PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames`, `EnumerateHudCanvasObjectNames`, `EnumeratePopupCanvasObjectNames` 기준이 함께 맞는지 확인한다.
- 에디터에서 Canvas UI가 비어 보이면 런타임 생성만 믿지 말고 `PrototypeUIDesignController`의 `Apply Preview` 수동 프리뷰와 `UIManager.EditorPreview` 경로가 관리 UI를 만들고 저장할 수 있는지 확인한다. 프리뷰는 기존 씬 `RectTransform` 배치를 덮어쓰지 않아야 한다.

### 구조 리팩토링/경로 정리

- 파일 이동 전 asmdef 범위, namespace 유지 여부, `.meta` 보존 여부를 먼저 확인한다.
- partial family는 엔트리 파일, read API, catalog, definitions처럼 역할별 폴더 경계를 먼저 정한 뒤 이동한다.
- UI managed object 이름은 `PrototypeUISceneLayoutCatalog`를 단일 정본으로 유지하고, runtime/editor 양쪽에 별도 문자열 목록을 만들지 않는다.
- 이동 후 `Docs/project/*`, `Docs/ui/*`, `Docs/build/*`, `Docs/scene/*`에 직접 적힌 경로를 함께 갱신한다.

### 주석 정리

- 폴더/partial 책임을 처음 읽는 사람이 바로 이해해야 하는 진입 파일에만 주석을 추가한다.
- 주석은 "왜 여기 있나"를 설명하고, 코드 한 줄 해설처럼 반복되는 설명은 넣지 않는다.

### 씬/하이어라키 변경

- 씬 직렬화 값이 정본인지 먼저 확인한다.
- `PrototypeSceneHierarchyOrganizer`와 씬 문서를 함께 맞춘다.

### generated 경로 변경

- `PrototypeGeneratedAssetSettings.cs`를 기준으로 경로를 맞춘다.
- generated 자산 복구를 별도 빌더에 기대하지 않는다.

### Unity API 연동 변경

- `JongguApiSession`, `GameManager`, 관련 runtime manager
- `D:\project-j-api`의 API 문서, DTO, controller, seed SQL

## 문서와 검증

- 동작 기준이 바뀌면 관련 문서를 바로 갱신한다.
- 과거 빌더/감사 흐름을 검증 기준으로 남기지 않는다.
- 구조 리팩토링 뒤에는 옛 경로 문자열과 stale 문서 경로를 정적 검색으로 확인한다.
- 가능하면 관련 csproj를 빌드해 파일 이동이나 namespace 유지가 깨지지 않았는지 먼저 확인한다.
- Unity 실행/컴파일을 직접 못 했으면 그 사실과 후속 검증 단계를 적는다.
