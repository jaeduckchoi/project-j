---
applies: always
---

# Jonggu Restaurant Working Rules

## 1. Project Overview

- This repository is a Unity prototype that combines 2D top-down exploration with restaurant management.
- The core loop is `check hub state -> explore -> collect materials -> return to hub -> choose menu -> run simple service -> settle results -> grow -> advance to next day`.
- Keep the overall feel close to `exploration 8 : restaurant 2`.
- Default user-facing responses and documentation should be written in Korean unless explicitly requested otherwise.

## 2. Documents To Review Before Working

- `project/GAME_DOCS_INDEX.md`
- `project/GAME_PROJECT_STRUCTURE.md`
- `gameplay/GAME_FEATURE_REFERENCE.md`
- `gameplay/GAMEPLAY_CORE_LOOP.md`
- `gameplay/GAMEPLAY_EXPLORATION.md`
- `gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md`
- `ui/UI_AND_TEXT_GUIDE.md`
- `ui/UI_GROUPING_RULES.md`
- `scene/GAME_SCENE_AND_SETUP.md`
- `scene/SCENE_HIERARCHY_GROUPING_RULES.md`
- `build/GAME_BUILD_GUIDE.md`

## 3. Prototype Scope

- Basic player controls are `WASD` or arrow-key movement plus `E` for interaction.
- Click-to-move, equipment swap UI, discard systems, and complex real-time restaurant operation are out of scope for the base prototype.
- The inventory should remain a material-only slot system and expand from `8 -> 12 -> 16` slots through upgrades.
- Tools should remain permanently unlocked items that do not consume inventory slots.
- Storage should remain a simple hub-only storage system.
- Upgrades should remain data-driven and consume both `gold + specific materials`.
- The default exploration region order is `Beach -> DeepForest -> AbandonedMine -> WindHill`.

## 4. Implementation Structure Rules

- Maintain system separation by responsibility. Example types include `PlayerController`, `InteractionDetector`, `IInteractable`, `InventoryManager`, `StorageManager`, `RestaurantManager`, `EconomyManager`, `UpgradeManager`, `ScenePortal`, `DayCycleManager`, and `UIManager`.
- Keep shared working standards under `.aiassistant/rules/{project|gameplay|ui|scene|build}` and separate them from runtime asset paths.
- Use `Assets/Scripts` for runtime code, `Assets/Editor` for editor-only code, `Assets/Scenes` for playable scenes, `Assets/Generated` for builder-managed generated assets, and `Assets/Resources/Generated` for generated assets loaded through `Resources.Load`.
- 런타임 코드는 기능 기준으로 `Assets/Scripts/CoreLoop`, `Assets/Scripts/Exploration`, `Assets/Scripts/Management`, `Assets/Scripts/Restaurant`, `Assets/Scripts/UI`, `Assets/Scripts/Shared` 아래에 배치한다.
- Shared regional logic or augmentation logic should go under `Assets/Scripts/Exploration/World`.
- Generated game data should stay grouped under `Assets/Generated/GameData/Resources`, `Assets/Generated/GameData/Recipes`, and `Assets/Generated/GameData/Input`.
- Maintain UI role separation across `UIManager`, `UI/Controllers`, `UI/Content`, `UI/Layout`, and `UI/Style`.
- Prefer data-first structures such as `ScriptableObject` where practical.
- Unity serialized files and asset references have wide impact, so verify both paths and reference links together.
- Do not fix only generated scene YAML, generated assets, or resource outputs directly. Fix the generation path first.
- When changing generated structure, update `Assets/Editor/JongguMinimalPrototypeBuilder.cs`, `Assets/Editor/PrototypeSceneAudit.cs`, related documents, and generated resource paths together.
- When changing UI, review both `Assets/Scripts/UI/UIManager.cs` and `Assets/Editor/JongguMinimalPrototypeBuilder.cs`.

## 5. Namespace And Naming Rules

- Runtime scripts and editor scripts should follow folder-based namespaces.
- For folder names that collide with Unity or .NET types such as `Camera` or `Editor`, use exception namespaces such as `GameCamera` or `ProjectEditor`.
- Place helper files for partial types in folders that keep the same namespace as the parent type.
- When moving existing `MonoBehaviour`, `ScriptableObject`, or serialized types into a namespace, preserve serialized paths with `UnityEngine.Scripting.APIUpdating.MovedFrom`.
- Default private-field naming rules are:
  - `[SerializeField] private` : lower camelCase
  - regular `private`, `private static` : `_camelCase`
  - `private static readonly`, `private const` : PascalCase
- In `.editorconfig`, keep the `Unity serialized field` rule applied before the general `Instance fields (private)` rule.

## 6. UI, Builder, And Audit Rules

- When a hub popup UI (`Cooking Menu`, `Upgrade`, `Materials`, `Storage`) opens, gameplay must pause; when it closes, the previous time flow must be restored.
- The popup pause rule is shared by `PopupPauseStateUtility` and `UIManager`, so do not change only one side.
- Remove duplicate UI paths such as legacy buttons, outdated docks, or unused cards.
- Do not reset or overwrite scene-assigned `Image.sprite`, `PopupTitle`, or `PopupLeftCaption` font and layout values in hub popups unless explicitly requested.
- Keep the common Canvas root names as `HUDRoot` and `PopupRoot`.
- The shared HUD baseline for supported Canvas scenes is stored in `Assets/Resources/Generated/ui-layout-overrides.asset`. Saving one supported scene should propagate the same managed Canvas changes to the other supported scene Canvases.
- `Tools > Jonggu Restaurant > Prototype Build and Audit` should run generated asset preparation, base scene regeneration, Canvas sync, and generated scene auditing in one flow.
- Prefer the automatic audit inside the build flow over separate manual scene-audit routines.
- Saving a supported Canvas scene should automatically store `RectTransform`, parent group and sibling order, deletion state, `Image.sprite/type/color/preserveAspect`, `TextMeshProUGUI`, `Button` display values, and `HUDActionGroup` or `HUDPanelButtonGroup` name overrides into `Assets/Resources/Generated/ui-layout-overrides.asset`.
- The builder, runtime `UIManager`, and automatic audit code must all use the same override asset baseline.
- Keep supported scene world hierarchy aligned to `SceneWorldRoot`, `SceneGameplayRoot`, `SceneSystemRoot`, and `Canvas`. If that structure changes, update `PrototypeSceneHierarchyCatalog`, the organizer, and audit logic together.
- When adding or changing menu entries under `Tools > Jonggu Restaurant`, use Korean labels by default and keep maintenance tools below frequently used build tools by adjusting `MenuItem` priority.
- Frequently regressed core rules are covered by `Light Automation Audit`, so when you change day-cycle flow, portal locking, or popup pause rules, update that audit together.

## 7. Comments And Documentation Rules

- Keep method summaries and block comments where they help reveal the intended current behavior of complex logic.
- New or updated code comments and documents should be written in UTF-8 Korean by default unless there is an explicit request to use another language.
- When editing existing English comments or documents, preserve meaning but normalize them into the requested language policy for the task.
- Do not add verbose comments for obvious assignments or self-explanatory logic.
- If you touch important methods or blocks that currently lack comments, improve them within the same task.
- When behavior changes, update related documents together.
- If a new standard becomes a rule, reflect it in both `AGENTS.md` and this document.

## 8. Font And Asset Rules

- Asset filenames under `Assets` should use kebab-case by default. Generated font assets and source font files under `Assets/Generated/Fonts` keep the existing lower camelCase convention.
- Keep the default TMP body font at `Assets/Generated/Fonts/maplestoryLightSdf.asset` and the heading font at `Assets/Generated/Fonts/maplestoryBoldSdf.asset`, and keep the source TTF paths aligned so the builder can regenerate them.
- `Assets/Design` is for design-source storage only; game-facing resources should resolve through `Assets/Resources` or generated paths.
- If a filename changes, update builder code, documents, and TMP reference paths together.
- Match asset naming rules to builder-generated naming rules whenever practical.

## 9. Validation Rules

- If you could not directly verify Unity play mode or compilation, state that explicitly.
- If automatic audits or batch compilation exist, check and report their results together.
- When changing generated structure or UI baselines, validate with `Tools > Jonggu Restaurant > Prototype Build and Audit` and `Light Automation Audit` whenever practical.
- If runtime validation is not possible, record exactly which files, coordinates, and references were reviewed.
- When changing generated structure, namespaces, or UI baselines, keep saved scenes, related `using` directives, builder code, audit code, and batch compilation results aligned.

## 10. Git Commit Message Rules

- All Git commit messages must be written in Korean.
- The basic format is `type : subject`.
- Keep the title within 50 characters and do not end it with a period.
- If the title is sufficient on its own, body and footer may be omitted.
- Write the body after one blank line, briefly and concretely.
- Even if you receive an English diff summary, PR title, or auto-generated draft, rewrite the final commit message into natural Korean.
- Do not leave English sentences as-is except for proper nouns that should not be translated, such as file paths, code identifiers, or branch names.
- Use a footer only for issue numbers, follow-up work, or breaking changes.
- Squash-merge commit messages follow the format `[squash] branch-name`.
- Use `.aiassistant/rules/project/GIT_COMMIT_TEMPLATE.md` as the local `commit.template` baseline, and update the template together with the documents if the rule changes.

### Allowed `type` Values

- `feat` : add a new feature
- `update` : modify an existing feature
- `fix` : bug fix
- `docs` : documentation or comment change
- `design` : UI design or CSS-like presentation change
- `style` : formatting-only changes with no behavior change, such as typos, spacing, or semicolons
- `rename` : rename a file or identifier
- `delete` : remove unnecessary files
- `refactor` : structural refactoring
- `test` : add or improve tests
- `chore` : maintenance such as build settings, project settings, import changes, or function-name cleanup

### Format

```text
type : subject

body

footer
```
