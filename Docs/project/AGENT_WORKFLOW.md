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
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutCatalog.cs` (`.Editor.cs`, `.Editor.Capture.cs` 포함)
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
- `Assets/Resources/Generated/ui-layout-overrides.asset`

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
- Unity 실행/컴파일을 직접 못 했으면 그 사실과 후속 검증 단계를 적는다.
