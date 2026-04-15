# 씬 하이어라키 그룹 규칙

## 기준

- 씬 루트와 그룹 구조의 정본은 실제 씬 직렬화입니다.
- 정리 도구는 `PrototypeSceneHierarchyOrganizer`를 기준으로 맞춥니다.

## 함께 수정할 항목

- 그룹 이름 변경: `PrototypeSceneHierarchyOrganizer`, 관련 씬 문서, 필요 시 런타임 참조 코드
- CameraBounds authored 경계 정리: `PrototypeSceneHierarchyOrganizer`는 `CameraBounds`의 부모/순서만 맞추고, 위치와 크기는 씬 직렬화 정본을 유지합니다.
- Canvas 루트 변경: `UI_GROUPING_RULES.md`, `UIManager`, `PrototypeUISceneLayoutCatalog`, `PrototypeUISceneLayoutSettings`
- 허브/월드 구조 변경: 관련 `Exploration/World` 코드와 씬 직렬화

## 검증

- 검증은 실제 씬 직렬화, 관련 런타임 코드, 정적 검색 결과를 기준으로 진행합니다.
- Unity에서 직접 확인하지 못한 경우 정적 검색과 코드 검토 기준으로 남은 검증 단계를 기록합니다.
