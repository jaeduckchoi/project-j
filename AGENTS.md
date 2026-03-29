# Agent Entry

Common working rules for this repository:

- `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`

Documents to review first:

1. `.aiassistant/rules/project/GAME_ASSISTANT_RULES_KO.md`
2. `Assets/Docs/GAME_FEATURE_REFERENCE_KO.md`
3. `Assets/Docs/UI_AND_TEXT_GUIDE_KO.md`
4. `Assets/Docs/GAME_SCENE_AND_SETUP_KO.md`

Response and working principles:

- Prefer Korean for user-facing responses.
- Unity serialized files and asset references have wide impact, so always verify the reference path together with the change.
- When changing UI, also check `Assets/Scripts/UI/UIManager.cs` and `Assets/Editor/JongguMinimalPrototypeBuilder.cs`.
- If Unity execution or compilation was not directly verified, state that clearly and mention the remaining verification steps.

Additional project rules:

- Keep player visuals aligned with map scale, and split physics and visual roots when needed.
- When hub popup UI (`요리 메뉴`, `업그레이드`, `재료`, `창고`) opens, pause gameplay; when it closes, restore the previous time flow.
- Remove duplicate UI paths such as legacy buttons, outdated docks, or unused cards instead of leaving them behind.
- New runtime and editor scripts must follow folder-based namespaces. When a folder name such as `Camera` or `Editor` conflicts with a major Unity/.NET type, use a conflict-free exception namespace such as `GameCamera` or `ProjectEditor`.
- Keep partial helper files in the parent folder when they must share the same namespace as their partial type. Do not split one partial type across different folder namespaces.
- When moving an existing `MonoBehaviour`, `ScriptableObject`, or other serializable type into a namespace, preserve the serialized path with `UnityEngine.Scripting.APIUpdating.MovedFrom`.
- When changing namespaces or generated structure, also verify related `using` directives, `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, automated audit code, and batch compilation results.
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
