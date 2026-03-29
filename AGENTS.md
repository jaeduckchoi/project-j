# Agent Entry

Common working rules for this repository:

- `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`

Documents to review first:

1. `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`
2. `Assets/Docs/GAME_PROJECT_STRUCTURE_KO.md`
3. `Assets/Docs/GAME_FEATURE_REFERENCE_KO.md`
4. `Assets/Docs/UI_AND_TEXT_GUIDE_KO.md`
5. `Assets/Docs/GAME_SCENE_AND_SETUP_KO.md`

Response and working principles:

- Prefer Korean for user-facing responses.
- Unity serialized files and asset references have wide impact, so always verify the reference path together with the change.
- When changing UI, also check `Assets/Scripts/UI/UIManager.cs` and `Assets/Editor/JongguMinimalPrototypeBuilder.cs`.
- If Unity execution or compilation was not directly verified, state that clearly and mention the remaining verification steps.

Additional project rules:

- Keep player visuals aligned with map scale, and split physics and visual roots when needed.
- `Assets/Docs/GAME_PROJECT_STRUCTURE_KO.md` 기준으로 폴더 역할을 유지한다. `Assets/Scripts` 는 런타임, `Assets/Editor` 는 에디터 전용, `Assets/Generated` 는 빌더가 관리하는 생성 자산, `Assets/Resources/Generated` 는 `Resources.Load` 대상 런타임 자산, `Assets/Scenes` 는 플레이 가능한 씬, `Assets/Docs` 는 기준 문서다.
- 새 기능은 역할에 맞는 기존 폴더에 배치하고, 포탈/스폰/위험 구역/런타임 씬 보강처럼 여러 기능이 공유하는 씬 로직은 `Assets/Scripts/World` 에 둔다. 기능 전용 상호작용 스테이션은 `Storage`, `Restaurant`, `Upgrade` 처럼 해당 기능 폴더에 둔다.
- UI 코드는 `UIManager` 진입점, `UI/Controllers` 편집 프리뷰, `UI/Content` 정적 문구, `UI/Layout` 배치 상수, `UI/Style` 스킨 매핑으로 분리한다.
- When hub popup UI (`요리 메뉴`, `업그레이드`, `재료`, `창고`) opens, pause gameplay; when it closes, restore the previous time flow.
- Remove duplicate UI paths such as legacy buttons, outdated docks, or unused cards instead of leaving them behind.
- New runtime and editor scripts must follow folder-based namespaces. When a folder name such as `Camera` or `Editor` conflicts with a major Unity/.NET type, use a conflict-free exception namespace such as `GameCamera` or `ProjectEditor`.
- Keep partial helper files in the parent folder when they must share the same namespace as their partial type. Do not split one partial type across different folder namespaces.
- When moving an existing `MonoBehaviour`, `ScriptableObject`, or other serializable type into a namespace, preserve the serialized path with `UnityEngine.Scripting.APIUpdating.MovedFrom`.
- When changing namespaces or generated structure, also verify related `using` directives, `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, automated audit code, and batch compilation results.
- 빌더가 생성하는 씬/에셋 구조를 바꿀 때는 결과물만 직접 고치지 말고 `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, `Assets/Editor/PrototypeSceneAudit.cs`, 관련 문서와 리소스 경로를 함께 맞춘다.
- Private field naming defaults are: lower camelCase for `[SerializeField] private`, `_camelCase` for regular `private` and `private static`, and PascalCase for `private static readonly` and `private const`.
- Keep the Rider/Unity naming rules in `.editorconfig` so that `Unity serialized field` is applied before the general `Instance fields (private)` rule.
- When changing gameplay or UI, keep method and block comments that explain the current behavior, and add missing comments if a touched file has important unannotated blocks or methods.
- New or updated code comments and documents must use UTF-8 Korean by default, and when touching existing English comments, rewrite them in Korean while preserving the meaning.
- Git commit messages must be written in Korean and follow the `type : subject` format.
- Detailed Git commit message rules must stay aligned with the Git section of `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`, and if the rule changes, update both `AGENTS.md` and the project rules document.
- The Git commit template path is `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE_KO.md`. If the rule changes, update the template and the related documents together.
- Keep commit titles within 50 characters and do not end the title with a period. Omit the body if the title is already sufficient.
- Even if an English diff summary, PR title, or auto-generated commit draft is provided, rewrite the final commit message in natural Korean.
- Do not leave English sentences as-is in the title or body unless they are untranslatable identifiers such as file paths, code identifiers, or branch names.
- Use only the predefined lowercase `type` values, and keep the body concise and specific about why and what changed. Use a footer only for issue numbers, follow-up work, or breaking changes.
- Squash merge commit messages must use the format `[squash] branch-name`.
- Generated font assets and source font filenames under `Assets/Generated/Fonts` must stay in lower camelCase.
- Do not reset or overwrite scene-assigned `Image.sprite`, `PopupTitle`, or `PopupLeftCaption` font/layout values in hub popups unless explicitly requested.
