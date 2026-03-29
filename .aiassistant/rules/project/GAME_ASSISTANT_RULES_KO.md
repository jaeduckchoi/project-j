---
적용: 항상
---

# Jonggu Restaurant AI Working Rules

## 1. Project Overview

- This repository is a Unity-based 2D top-down exploration plus restaurant-management prototype.
- The project name is `종구의 식당`.
- The core loop is `check hub status -> morning exploration -> gather ingredients -> return to hub -> choose menu -> run simple service -> settle results -> grow -> advance to the next day`.
- The intended gameplay ratio is roughly `exploration 8 : restaurant service 2`, and restaurant service should stay outcome-driven rather than becoming a complex real-time management simulation.
- Prefer Korean for default user-facing responses.

## 2. Documents To Review First

- Feature overview: `Assets/Docs/GAME_FEATURE_REFERENCE_KO.md`
- Daily loop: `Assets/Docs/GAMEPLAY_CORE_LOOP_KO.md`
- Exploration systems: `Assets/Docs/GAMEPLAY_EXPLORATION_KO.md`
- Restaurant and growth systems: `Assets/Docs/GAMEPLAY_RESTAURANT_AND_GROWTH_KO.md`
- UI and text guide: `Assets/Docs/UI_AND_TEXT_GUIDE_KO.md`
- Scene and setup guide: `Assets/Docs/GAME_SCENE_AND_SETUP_KO.md`
- Build and generated-data guide: `Assets/Docs/GAME_BUILD_GUIDE_KO.md`

## 3. Prototype Scope Baseline

- Player control is based on `WASD` or arrow-key movement plus `E` interaction. Click-to-move is out of scope.
- A minimap is out of scope by default. Use region names, short guide text, and minimal directional guidance instead.
- Prefer short contextual guidance at hub entry, region entry, first gathering, and first service instead of a long standalone tutorial stage.
- Exploration skip and service skip stay available as prototype iteration features.
- The hub is not a static menu screen. It remains a safe walkable space where the player interacts with `주방`, `장부/게시판`, `창고`, `작업대`, and `출입문`.

## 4. Game Design Baseline

- Inventory remains a slot-based structure dedicated to gathered resources, expanding from `8 slots` to `12` and `16` through upgrades.
- Tools remain permanently unlocked items that do not occupy inventory space. The baseline set is `갈퀴`, `낚시대`, `낫`, and `랜턴`.
- Tool slots, equipment swapping during exploration, and discard systems are out of default scope unless the scope is explicitly expanded.
- Storage stays a simple hub-only container, and its core functions are `store` and `take out`.
- Upgrades remain data-driven and consume `gold + specific materials` together.
- Restaurant service remains simple and centered on menu selection, ingredient checks, result settlement, and gold/reputation updates.
- Exploration regions follow the expansion order `바닷가 -> 깊은 숲 -> 폐광산 -> 바람 언덕` by default.
- `폐광산` keeps entry conditions such as lantern or visibility constraints, and `바람 언덕` keeps region-specific gimmicks such as strong winds and shortcuts.

## 5. Implementation Structure Baseline

- Core systems should stay separated by role, with structures such as `PlayerController`, `InteractionDetector`, `IInteractable`, `InventoryManager`, `StorageManager`, `RestaurantManager`, `EconomyManager`, `UpgradeManager`, `ScenePortal`, `DayCycleManager/FlowManager`, and `UIManager`.
- Resources, recipes, upgrades, and regional unlock conditions should be managed with ScriptableObject or other data-first structures when possible.
- Reduce hardcoding and keep variable names, method names, and null checks easy to read for junior developers.
- Unity serialized files and asset references have wide impact, so update reference paths together with the change.
- If generated YAML or scene data is the output of builder code, do not patch only the output. Clean up the generation path first.
- When changing UI, also check `Assets/Scripts/UI/UIManager.cs` and `Assets/Editor/JongguMinimalPrototypeBuilder.cs`.
- New runtime and editor scripts must follow folder-based namespaces.
- If a folder name such as `Camera` or `Editor` conflicts with a major Unity/.NET type, use a conflict-free exception namespace such as `GameCamera` or `ProjectEditor`, and reflect repeated exceptions in the rules document.
- Keep partial helper files in the parent folder when they must share the same namespace as their partial type. Do not split one partial type across different folder namespaces.
- When moving an existing `MonoBehaviour`, `ScriptableObject`, or other serializable type into a namespace, preserve the previous serialized path with `UnityEngine.Scripting.APIUpdating.MovedFrom`.
- When changing player visual scale or directional sprites, separate the responsibilities of the physics root and the visual root.
- Private field naming defaults are: lower camelCase for `[SerializeField] private`, `_camelCase` for regular `private` and `private static`, and PascalCase for `private static readonly` and `private const`.
- Keep the Rider/Unity naming rules in `.editorconfig` so that `Unity serialized field` is applied before the general `Instance fields (private)` rule.
- Do not revert user-authored existing changes unless the user explicitly asks for it.

## 6. UI And Text Rules

- When changing TextMesh Pro fonts or default TMP settings, also verify the actual referenced asset paths.
- Check overlap, clipping, contrast, and panel overflow together for UI text.
- When hub popup UI (`요리 메뉴`, `업그레이드`, `재료`, `창고`) opens, pause gameplay; when it closes, restore the original time flow.
- Keep labels, prompts, guide text, and result text aligned so that builder code and runtime adjustment code use the same positioning baseline.
- Do not reset or overwrite scene-assigned `Image.sprite`, `PopupTitle`, or `PopupLeftCaption` font/layout values in hub popups unless explicitly requested.
- Do not leave behind replaced buttons, legacy cards, outdated docks, or duplicate unused UI paths.

## 7. Comment And Documentation Rules

- Keep method-level explanations and block comments in front of complex logic.
- New or modified code should keep Korean method explanations and complex block comments that make the current behavior clear.
- If a touched file contains important unannotated methods or blocks, improve them within the same change.
- Do not add long-winded comments for obvious assignments or self-explanatory behavior.
- New or updated comments and documents must use UTF-8 Korean by default, and when refreshing English comments, rewrite them in Korean while preserving their meaning.
- Update related documents when behavior changes.
- Write documents based on the actual game baseline and the current implementation state rather than abstract theory.
- If a new baseline is added to the rules, make sure it is visible in both `AGENTS.md` and the project rules document.

## 8. Font And Asset Naming Rules

- Generated font assets and source font filenames under `Assets/Generated/Fonts` must use lower camelCase.
- If a filename changes, update the builder code, documents, and TMP reference paths together.
- Keep filename conventions aligned with the asset naming conventions generated by the builder whenever possible.

## 9. Verification Rules

- If Unity play tests or compilation were not directly verified, state that clearly.
- If automated audits or batch compilation exist, check and report the results together.
- If runtime validation is impossible, record specifically which files, coordinates, or references were used as the review baseline.
- When changing generated structure or namespaces, make sure saved scenes, related `using` directives, builder code, automated audit code, and batch compilation results all follow the same baseline.

## 10. Git Commit Message Rules

- All Git commit messages must be written in Korean.
- The default format is `type : subject`. Only when needed, leave one blank line below the title and then write `body` and `footer`.
- Keep the title within 50 characters and do not end it with a period.
- Use only the lowercase `type` values listed below.
- Even if an English diff summary, PR title, or auto-generated commit draft is provided, rewrite the final commit message in natural Korean.
- Do not leave English sentences unchanged in the title or body unless they are untranslatable identifiers such as file paths, code identifiers, or branch names.
- If the title alone already makes the reason and core change clear enough, omit the body and footer.
- Write the body after one blank line below the title.
- Keep the body short and specific about why the change was made and what changed.
- Use a footer only when additional context such as issue numbers, follow-up work, or breaking changes is needed.
- Squash merge commit messages must use the format `[squash] branch-name`. Example: `[squash] hotfix/blabla`
- Use `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE_KO.md` as the local Git `commit.template`, and update this section together with the template when the template changes.

### Allowed `type` Values

- `feat` : add a new feature
- `update` : modify an existing feature
- `fix` : fix a bug
- `docs` : update documents or comments
- `design` : change CSS or UI design
- `style` : no-behavior-change edits such as typos, formatting, semicolons, or spacing
- `rename` : rename files
- `delete` : remove unnecessary files
- `refactor` : refactor code
- `test` : add tests
- `chore` : build settings, project settings, import changes, function renames, and similar maintenance work

### Format

```text
type : subject

body

footer
```

### Examples

```text
feat : 사용자 정보 가져오기

로그인한 사용자의 정보를 JSON에 담아 가져온다
```

```text
fix : 이미지 경로 수정

이미지 경로에 포함된 불필요한 경로를 제거하도록 수정한다
```

```text
docs : 커밋 메시지 규칙 정리
```

```text
fix : 허브 팝업 닫기 버튼 스프라이트 복구

빌더 재생성 시 닫기 버튼 이미지가 None 으로 초기화되던 경로를 막았다.
팝업 스프라이트 시트 매핑을 씬 기준으로 정리했다.

관련: popup ui sync
```

```text
design : 깊은 숲 씬 UI 요소와 레이아웃 보강

깊은 숲 씬에 안내 오브젝트와 이동 구역 설정을 추가하고
UI 배치와 텍스트 정렬, 자동 크기 조정을 함께 정리했다
```
