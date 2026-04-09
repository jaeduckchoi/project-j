# 에이전트 작업 흐름

이 문서는 이 저장소에서 에이전트가 따라야 하는 기본 작업 루프와 검증 매트릭스의 정본이다.

## 1. 기본 작업 루프

1. 맵 읽기
- `AGENTS.md` 또는 `CLAUDE.md`
- `.aiassistant/rules/README.md`

2. 정본 문서 찾기
- `GAME_DOCS_INDEX.md`에서 현재 작업에 맞는 문서를 고른다.
- 한 번에 필요한 정본 문서만 읽는다.

3. 실제 상태 확인
- 코드, 씬, generated 자산, 직렬화 값을 저장소에서 직접 확인한다.
- Unity API 연동 작업이면 `D:\project-j-api`의 DTO, controller, seed SQL까지 함께 확인한다.
- 문서에 적힌 설명보다 현재 저장소 사실을 우선 확인한다.

4. 정본 경계 결정
- 무엇이 정본인지 `SOURCE_OF_TRUTH.md`에서 먼저 판단한다.
- scene serialization, builder, generated asset 중 어디를 먼저 수정해야 하는지 결정한다.
- 정적 generated 에셋 이슈라면 결과물 파일을 다시 만들지 말고 메모리 fallback, 수동 정본 에셋, 또는 씬 직렬화 중 어디가 정본인지 먼저 결정한다.

5. 구현
- 결과물만 직접 수정하지 않고, 정본 경계에 맞는 파일부터 수정한다.
- 같은 규칙을 여러 문서에 복제하지 않는다.

### 대용량 신규 파일 생성

- Windows에서 패치 길이 제한이 걸릴 수 있으므로 큰 신규 파일은 한 번에 추가하지 않는다.
- 먼저 `using`, namespace, 타입 선언, 필수 필드처럼 컴파일 가능한 최소 뼈대만 만든다.
- 이후 메서드, DTO, 상수, 문자열 블록을 1개 주제씩 여러 번의 작은 `apply_patch`로 이어 붙인다.
- Unity 스크립트라면 `.meta` 파일도 별도 작은 패치로 만든다.
- 각 조각을 넣을 때마다 파일을 다시 읽어 위치와 중괄호 균형을 확인한 뒤 다음 조각을 붙인다.

6. 검증
- 가능한 경우 `Prototype Build and Audit` 또는 관련 감사 기준으로 확인한다.
- 직접 실행하지 못하면 어떤 검증이 남았는지 명시한다.

7. 문서 동기화
- 동작 기준이 바뀌면 관련 정본 문서도 같은 변경 안에서 갱신한다.
- 인덱스와 엔트리 맵이 새 정본을 제대로 가리키는지 확인한다.

8. 드리프트 정리
- 오래된 경로, 중복 규칙, 현재 코드와 어긋난 설명을 함께 제거한다.

## 2. 변경 유형별 검증 매트릭스

### 문서만 수정한 경우

- 링크가 실제 파일을 가리키는지 확인한다.
- stale 경로와 중복 규칙이 제거됐는지 `rg`로 확인한다.
- Unity 실행은 필수는 아니지만 미실행 사실을 결과에 적는다.

### UI 변경

- `UIManager`, 빌더 UI 코드, `ui-layout-overrides.asset` 기준을 함께 확인한다.
- `HUDRoot`, `PopupRoot`, `GuideText`, `RestaurantResultText`, `GuideHelpButton` 같은 managed UI가 현재 구조와 맞는지 본다.
- 가능하면 `Prototype Build and Audit` 기준으로 확인한다.

### 씬/월드 변경

- 지원 씬의 저장값이 정본인지 먼저 판단한다.
- 플레이 중 필요한 오브젝트와 참조가 씬 직렬화에 직접 저장돼 있는지 확인한다.
- 하이어라키가 바뀌면 `PrototypeSceneAudit`와 계층 문서까지 함께 갱신한다.

### generated 자산/빌더 변경

- `PrototypeGeneratedAssetSettings.cs`의 코드 기본값과 실제 런타임 로드 경로가 맞는지 확인한다.
- 빌더가 정적 generated 에셋을 다시 만들지 않도록 확인하고, 필요한 값은 메모리 fallback 또는 수동 정본 에셋으로 옮긴다.
- 빌더 코드, generated 경로, 관련 문서를 함께 맞춘다.
- 가능하면 `Prototype Build and Audit` 기준으로 확인한다.

### 게임플레이 규칙 변경

- gameplay 문서와 실제 코드가 같은 의도를 설명하는지 확인한다.
- 안내 흐름, portal, popup pause 같은 회귀 위험 항목은 `GameplayAutomationAudit` 연관성을 점검한다.

### Unity API 연동 변경

- `JongguApiSession`, `GameManager`, 원격 스냅샷 적용 매니저가 같은 계약을 쓰는지 확인한다.
- 씬 이름, resource/recipe/tool/upgrade code가 API와 같은 문자열인지 확인한다.
- 가능하면 `D:\project-j-api` 서버를 실행한 뒤 세션 생성, 이동, 채집, 창고, 메뉴 선택, 영업, 업그레이드를 순서대로 확인한다.
- Unity나 API 서버를 직접 띄우지 못했으면 어떤 계약 문서를 기준으로 연결했는지와 남은 통합 검증 단계를 결과에 적는다.

## 3. 드리프트 정리 기준

정리 대상:

- 존재하지 않는 파일이나 예전 파일명을 계속 가리키는 링크
- 실제 코드와 맞지 않는 네임스페이스 설명
- generated 결과물을 직접 수정하라고 읽히는 안내 문구
- 한 규칙이 여러 문서에 중복된 상태
- 더 이상 사용하지 않는 복구 절차나 일회성 메모

정리 원칙:

- 같은 의미의 규칙은 정본 하나만 남긴다.
- 엔트리 파일에는 요약만 남기고 세부 규칙은 정본 문서로 이동한다.
- 규칙을 줄이되, 정본 위치와 결합 지점은 더 명확하게 남긴다.

## 4. 문서 동기화 기준

- 프로젝트 구조 변경: `GAME_PROJECT_STRUCTURE.md`, `GAME_DOCS_INDEX.md`
- 정본 관계 변경: `SOURCE_OF_TRUTH.md`, 관련 도메인 문서
- Unity API 연동 변경: `GAME_PROJECT_STRUCTURE.md`, `SOURCE_OF_TRUTH.md`, 필요 시 관련 외부 API 규칙 문서
- UI 기준 변경: UI 문서, 필요 시 씬/빌드 문서
- 씬 계층 변경: 씬 문서, 빌드 문서, 정본 관계 문서
- 커밋 규칙 변경: `GIT_COMMIT_TEMPLATE.md`만 수정하고 다른 문서는 링크만 유지

## 5. 결과 보고 기준

- 무엇을 바꿨는지
- 어떤 문서를 정본으로 정리했는지
- 어떤 검증을 했는지, 못 했다면 왜 못 했는지
- 남은 드리프트 후보나 후속 정리 후보가 있으면 짧게 제안하기
