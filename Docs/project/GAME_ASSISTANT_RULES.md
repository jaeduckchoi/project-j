# 게임 프로젝트 작업 규칙

## 기본 규칙

- 기본 응답 언어는 한국어다.
- 루트 엔트리 파일(`AGENTS.md`, `CLAUDE.md`)은 맵이고, 세부 규칙 허브는 `.aiassistant/rules/README.md`, 실제 정본 문서는 `Docs/*`다.
- generated 결과물만 직접 고치지 말고 생성 경로 또는 정본 코드부터 수정한다.
- 씬에 저장된 월드 직렬화 값은 정본이며 런타임 보강 코드는 누락분만 보충한다.
- UI 변경은 `UIManager`, `PrototypeUISceneLayoutCatalog`, `PrototypeUISceneLayoutSettings`, `ui-layout-overrides.asset`를 함께 확인한다.
- 정적 generated 에셋 생성이나 복구를 빌더에 기대하지 않는다.
- `D:\project-j-api`와 맞물리는 code를 바꾸면 API 계약, Unity 코드, 문서를 같은 변경에서 함께 맞춘다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과에 그 사실과 남은 검증 단계를 적는다.

## 구현 규칙

- Windows에서는 새 파일을 길게 한 번에 붙이지 말고 안전하게 작성한다.
- 수동 편집은 `apply_patch`를 우선 사용한다.
- 문서와 코드가 어긋나면 같은 변경 안에서 문서도 갱신한다.
- 과거 빌더/감사 흐름을 현재 검증 기준처럼 문서화하지 않는다.

## 검증 규칙

- 구조, UI, generated 경로 변경은 정적 검색과 관련 런타임 코드 기준으로 검증한다.
- API 연동 변경은 가능하면 `D:\project-j-api`와 함께 계약/동작을 확인한다.
- Unity에서 직접 실행하지 못했으면 미검증 사실을 명시한다.
