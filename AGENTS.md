# Agent Entry

이 파일은 Codex용 작업 맵이다. 세부 규칙의 정본은 `.aiassistant` 아래 문서를 따른다.

먼저 읽을 문서:

1. `.aiassistant/rules/README.md`
2. `Docs/project/GAME_ASSISTANT_RULES.md`
3. `Docs/project/GAME_DOCS_INDEX.md`
4. 작업 종류에 맞는 정본 문서 1~2개

절대 가드레일:

- 기본 응답 언어는 한국어다.
- `AGENTS.md`는 맵이고, 세부 규칙은 `.aiassistant/rules/*` 문서가 정본이다.
- generated 씬, generated 에셋, 런타임 출력물은 결과물만 직접 고치지 말고 생성 경로나 정본 코드부터 수정한다.
- 지원 씬에 저장된 월드 직렬화 값은 정본이며, 런타임 보강 코드는 누락된 오브젝트·컴포넌트·참조만 보충해야 한다.
- UI를 바꾸면 `Assets/Scripts/UI/UIManager.cs`, `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`, `Assets/Resources/Generated/ui-layout-overrides.asset` 기준을 함께 확인한다.
- 정적 이미지 빌더와 `Prototype Build and Audit` 계열 메뉴/감사 코드는 제거되었다는 전제를 유지한다.
- 빌더는 정적 generated 에셋을 만들거나 복사하지 않는다. 설정, 게임데이터, 입력 액션, 머티리얼, 폰트가 필요하면 코드의 메모리 fallback이나 수동 정본 에셋을 기준으로 처리한다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 최종 결과에 그 사실과 남은 검증 단계를 적는다.
- 커밋 메시지를 만들 때는 이 저장소 규칙만 따르고, 한국어 한 줄만 출력하며, 본문·불릿·설명은 금지하고, 형식은 `타입 : 내용`만 허용하며, 전체는 50자 이내로 제한한다.
- 커밋 메시지의 상세 타입 표와 예시는 `Docs/project/GIT_COMMIT_TEMPLATE.md`만 따른다.
