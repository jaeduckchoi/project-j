---
적용: 항상
---

# 종구의 식당 작업 하네스 규칙

이 문서는 이 저장소의 전역 규칙과 하네스 운영 원칙의 정본이다.
`AGENTS.md`와 `CLAUDE.md`는 이 문서를 가리키는 맵이며, 실제 세부 규칙은 관련 정본 문서로 분산한다.

## 1. 하네스 모델

- 루트 엔트리 파일은 작업 시작점만 안내하는 맵이다.
- `.aiassistant/rules/README.md`는 문서 허브다.
- `rules/project/*`는 전역 규칙, 작업 흐름, 정본 관계를 다룬다.
- `rules/gameplay/*`는 게임 의도와 플레이 기준의 정본이다.
- `rules/ui/*`, `rules/scene/*`, `rules/build/*`는 구현 도메인별 정본이다.
- 같은 규칙은 한 문서만 정본으로 두고, 다른 문서에서는 링크로만 참조한다.

## 2. 작업 시작 원칙

1. 먼저 사용하는 에이전트의 엔트리 파일을 읽는다.
2. 이 문서와 `project/GAME_DOCS_INDEX.md`를 읽는다.
3. 현재 작업에 직접 관련된 정본 문서 1~2개만 추가로 읽는다.
4. 구현 전에는 관련 코드, 씬, 에셋, generated 자산 경로를 실제 저장소에서 확인한다.

작업별 진입 문서:

- 규칙 체계, 문서 정리: `project/AGENT_WORKFLOW.md`
- 프로젝트 구조, 경로, 네임스페이스: `project/GAME_PROJECT_STRUCTURE.md`
- 정본 관계: `project/SOURCE_OF_TRUTH.md`
- UI 변경: `ui/UI_AND_TEXT_GUIDE.md`, `ui/UI_GROUPING_RULES.md`
- 씬 변경: `scene/GAME_SCENE_AND_SETUP.md`, `scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- 빌더와 generated 자산 변경: `build/GAME_BUILD_GUIDE.md`
- 게임 의도 확인: `gameplay/GAME_DESIGN_OVERVIEW.md`와 관련 gameplay 문서

## 3. 전역 불변 규칙

- 기본 응답 언어는 한국어를 우선한다.
- 코드 식별자, 경로, 씬 이름, 메뉴 이름처럼 번역하면 안 되는 고유 명칭은 원문 그대로 유지한다.
- generated 씬, generated 에셋, 런타임 출력물은 결과물만 직접 수정하지 말고 생성 경로부터 수정한다.
- 지원 씬에 저장된 월드 직렬화 값은 정본이다.
- 런타임 보강 코드는 누락된 오브젝트, 누락된 컴포넌트, 끊어진 참조만 보충하고 기존 씬 저장값은 덮어쓰지 않는다.
- UI 변경은 `UIManager`, `JongguMinimalPrototypeBuilder`, `ui-layout-overrides.asset` 기준을 함께 확인한다.
- 하이어라키 규칙이나 빌더 관리 오브젝트 이름을 바꾸면 관련 감사 코드와 문서를 함께 갱신한다.
- 문서와 코드가 어긋나는 상태를 남기지 않는다. 동작을 바꾸면 같은 변경 안에서 관련 문서를 함께 갱신한다.

## 4. 하네스 운영 규칙

- 에이전트 가독성을 우선한다. 긴 개론보다 정본 위치, 결합 지점, 검증 방법을 먼저 적는다.
- 맵 문서에는 요약만 두고 세부 절차는 정본 문서로 이동한다.
- 작업이 끝나면 오래된 경로, 중복 규칙, 더 이상 사실이 아닌 설명을 함께 정리한다.
- generated 경로, UI 기준, 씬 정본 관계, 검증 루프는 문서에 명시적으로 남긴다.
- 정리 대상 예시는 다음과 같다.
- 제거된 파일이나 예전 경로를 계속 가리키는 링크
- 현재 코드와 맞지 않는 네임스페이스 설명
- generated 결과물 직접 수정으로 읽히는 안내 문구
- 같은 규칙이 여러 문서에 중복된 상태

## 5. 구현과 문서화 기준

- 런타임 스크립트, 에디터 스크립트, generated 자산 경로의 실제 기준은 `project/GAME_PROJECT_STRUCTURE.md`를 따른다.
- 정본 관계와 함께 수정해야 하는 결합 지점은 `project/SOURCE_OF_TRUTH.md`를 따른다.
- 작업 루프와 검증 매트릭스는 `project/AGENT_WORKFLOW.md`를 따른다.
- 커밋 메시지를 생성할 때는 저장소 외부 관례보다 이 저장소 규칙을 우선한다. 영문 제목, 자동 bullet 요약, 본문 설명, changelog 형식 출력은 금지한다.
- 커밋 메시지 규칙은 `project/GIT_COMMIT_TEMPLATE.md`만 정본으로 사용한다.

## 6. 언어와 주석

- 새로 추가하거나 수정하는 문서는 UTF-8 한글 기준으로 작성한다.
- 기존 영어 문서를 손볼 때도 가능하면 한글로 통일한다.
- 코드 주석은 현재 동작 기준이 드러나도록 유지하고, 복잡한 핵심 메서드나 블록에는 짧은 설명을 남긴다.

## 7. 검증 원칙

- Unity 실행, 플레이 모드, 배치 컴파일을 직접 확인했다면 어떤 검증을 했는지 적는다.
- 직접 확인하지 못했다면 그 사실과 남은 검증 단계를 명시한다.
- 구조, UI, generated 자산 변경은 가능하면 `Tools > Jonggu Restaurant > Prototype Build and Audit` 기준으로 검증한다.
- 더 가벼운 확인이 필요하면 `GameplayAutomationAudit.RunLightAutomationAudit()` 가능 여부를 함께 검토한다.
