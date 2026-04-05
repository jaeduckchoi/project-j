---
적용: 항상
---

# Jonggu Restaurant Project Structure Guide

## 1. Purpose

This document defines the actual folder structure and responsibility boundaries in the `Jonggu Restaurant` repository.
The baseline is to keep working standards under `.aiassistant/rules` and actual Unity assets under `Assets`.

## 2. Current Top-Level Structure

```text
.
├─ .aiassistant
│  └─ rules
│     ├─ build
│     ├─ gameplay
│     ├─ local
│     ├─ project
│     ├─ scene
│     └─ ui
├─ Assets
│  ├─ Design
│  ├─ Editor
│  │  └─ UI
│  ├─ Generated
│  ├─ Resources
│  │  └─ Generated
│  ├─ Scenes
│  ├─ Scripts
│  ├─ Settings
│  ├─ TextMesh Pro
│  ├─ UI Toolkit
│  └─ _Recovery
└─ Tools
```

## 3. `.aiassistant/rules` Responsibilities

- `project`
  Shared working rules, the document index, and repository-structure standards.
- `gameplay`
  Core loop, exploration, restaurant or growth, and integrated gameplay references.
- `ui`
  HUD/popup structure, text, and Canvas grouping rules.
- `scene`
  Scene composition, hierarchy grouping, and scene checkpoint documents.
- `build`
  Builder flow, generated-asset recreation, and audit flow documents.
- `local`
  Personal-machine notes or local execution rules.

## 4. Current `Assets` Structure

```text
Assets
├─ Design
│  ├─ Archive
│  ├─ GeneratedSources
│  │  ├─ Data
│  │  ├─ Fonts
│  │  ├─ Sprites
│  │  └─ UI
│  ├─ References
│  └─ UIDesign
│     ├─ Exports
│     ├─ Mockups
│     └─ Vector
├─ Editor
│  └─ UI
├─ Generated
│  ├─ Fonts
│  ├─ GameData
│  └─ Sprites
│     ├─ Gather
│     ├─ Hub
│     ├─ Player
│     ├─ UI
│     └─ World
├─ Resources
│  └─ Generated
│     └─ Sprites
│        ├─ Gather
│        ├─ Hub
│        ├─ Player
│        ├─ UI
│        └─ World
├─ Scenes
├─ Scripts
│  ├─ Camera
│  ├─ Core
│  ├─ Data
│  ├─ Economy
│  ├─ Flow
│  ├─ Gathering
│  ├─ Interaction
│  ├─ Inventory
│  ├─ Player
│  ├─ Restaurant
│  ├─ Storage
│  ├─ Tools
│  ├─ UI
│  ├─ Upgrade
│  └─ World
├─ Settings
│  └─ Scenes
├─ TextMesh Pro
├─ UI Toolkit
└─ _Recovery
```

## 5. Folder Responsibilities

- `Assets/Scripts`
  Runtime code only. Use the feature roots `CoreLoop`, `Exploration`, `Management`, `Restaurant`, `UI`, and `Shared` to keep related systems close together.
- `Assets/Editor`
  Editor-only code such as builders, audits, Canvas auto-sync, and custom inspectors.
- `Assets/Scenes`
  Playable scenes. Supported scenes are `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, and `WindHill`.
- `Assets/Generated`
  Builder-generated or builder-refreshed source outputs.
- `Assets/Resources/Generated`
  Generated assets loaded directly at runtime via `Resources.Load`.
- `Assets/Design`
  Design-source and review materials only. Runtime should not reference this path directly.
- `Assets/Settings/Scenes`
  Project settings related to Build Settings and scene configuration.
- `Assets/TextMesh Pro`
  TMP Settings and default TMP resources.
- `Assets/UI Toolkit`
  Unity UI Toolkit default resources.
- `Assets/_Recovery`
  Recovery or temporary storage path, not a formal runtime baseline.

## 6. Generated Asset Structure

### `Assets/Generated/GameData`

- Group generated data by feature role under `Resources`, `Recipes`, and `Input`.
- Use kebab-case filenames with patterns like `resource-*`, `recipe-*`, and `generated-ui-*`.
- Core resources live under `Assets/Generated/GameData/Resources`, recipes live under `Assets/Generated/GameData/Recipes`, and generated input assets live under `Assets/Generated/GameData/Input`.

### `Assets/Generated/Fonts`

- Keep the body font baseline at `maplestoryLightSdf.asset` and the heading font baseline at `maplestoryBoldSdf.asset`.
- Source TTFs are `maplestoryLight.ttf` and `maplestoryBold.ttf`, and generated font filenames keep lower camelCase.
- `malgunGothicSdf.asset` may remain as a fallback or legacy asset, but the current builder default is the Maplestory family.

### `Assets/Generated/Sprites`

- Generated sprites are organized under `Player`, `Gather`, `World`, `Hub`, and `UI`.
- `Assets/Design/GeneratedSources/UI` is mirrored by the builder into both `Assets/Generated/Sprites/UI` and `Assets/Resources/Generated/Sprites/UI`.
- UI sources should stay organized by categories such as `Buttons`, `MessageBoxes`, and `Panels`.

### `Assets/Resources/Generated`

- Store shared generated assets that runtime reads directly.
- `generated-game-data-manifest.asset`
  The manifest used for generated-data recovery and runtime loading.
- `ui-layout-overrides.asset`
  The shared Canvas layout and display-value override asset for supported scenes.

## 7. Runtime Code Structure

- Assets/Scripts/CoreLoop
  Global composition and day-loop entry points such as GameManager and DayCycleManager.
- Assets/Scripts/Exploration
  Player movement, camera, gathering, interaction, portals, hazard zones, and scene augmentation.
- Assets/Scripts/Management
  Economy, inventory, storage, tools, and upgrade progression.
- Assets/Scripts/Restaurant
  Menu selection, service execution, and related hub interaction logic.
- Assets/Scripts/Shared
  Shared data definitions such as ResourceData, RecipeData, generated-data locators, and manifest types.
- Assets/Scripts/UI
  UIManager, popup pause logic, layouts, styles, and content catalogs.

## 8. Editor Code Structure

- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
  Recreates generated assets, base scenes, and base UI placement.
- `Assets/Editor/PrototypeSceneAudit.cs`
  Audits generated-scene structure and UI baselines.
- `Assets/Editor/GameplayAutomationAudit.cs`
  Checks day-loop flow, popup pause, portal locking, and missing-scene guidance.
- `Assets/Editor/PrototypeSceneHierarchyOrganizer.cs`
  Reorganizes supported scene hierarchy into the shared root structure.
- `Assets/Editor/UI/*`
  Canvas auto-sync, UI preview tools, and generated-sprite helper editors.

## 9. Placement Rules

### Adding New Scripts

- Runtime code must be placed under the correct feature root in `Assets/Scripts`.
- Editor code must be placed under `Assets/Editor`.
- Shared scene augmentation, spawn logic, or travel helpers should prefer `Assets/Scripts/Exploration/World`.

### Adding New Data

- Put data type definitions under `Assets/Scripts/Shared/Data`.
- Put builder-generated data assets under the matching subfolder in `Assets/Generated/GameData` and keep role-revealing kebab-case filenames.
- If runtime loads the data through `Resources.Load`, keep `Assets/Resources/Generated/generated-game-data-manifest.asset` and related paths aligned.

### Adding New UI

- Check `Assets/Scripts/UI/UIManager.cs` first for state updates and button connections.
- Put static strings and text catalogs under `Assets/Scripts/UI/Content`.
- Put spacing, positions, and grouping baselines under `Assets/Scripts/UI/Layout`.
- Put sprite paths and skin application under `Assets/Scripts/UI/Style`.
- When changing UI, also review `Assets/Editor/JongguMinimalPrototypeBuilder.cs` and `Assets/Editor/UI/PrototypeUIDesignControllerEditor.cs`.

## 10. Generation Path Rules

- Do not patch only generated scenes, generated assets, or runtime outputs. Change the builder path first.
- `Assets/Generated` is the builder-source output path, while `Assets/Resources/Generated` is the runtime-loading path. Preserve that distinction.
- If runtime code uses `Resources.Load`, documented paths and actual folder structure must match exactly.
- Supported-scene Canvas layout should follow `ui-layout-overrides.asset`, and the auto-sync flow triggered on scene save must remain intact.

## 11. Working Checklist

### Gameplay Changes

- Is the new code placed under the correct responsibility folder?
- Are related `GameManager` or manager references required?
- If new data was added, were `Assets/Generated/GameData` conventions and documents updated too?
- If world layout changed, were `Assets/Scripts/Exploration/World`, `Scenes`, builder code, and hierarchy-rule documents reviewed together?

### UI Changes

- Was `Assets/Scripts/UI/UIManager.cs` reviewed?
- Was `Assets/Editor/JongguMinimalPrototypeBuilder.cs` reviewed together?
- Are save/load paths aligned with `Assets/Resources/Generated/ui-layout-overrides.asset`?
- If the change touches a popup, does pause-and-restore behavior still work?

### Generated Structure Changes

- Was builder code changed instead of only scene output?
- Was `Assets/Editor/PrototypeSceneAudit.cs` updated to match?
- Were `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs` and organizer baselines kept aligned?
- Are namespaces, serialized paths, and resource paths all consistent?

## 12. Current Baseline Notes

- Shared working standards live under `.aiassistant/rules`.
- Generated data is grouped under `Assets/Generated/GameData/Resources`, `Assets/Generated/GameData/Recipes`, and `Assets/Generated/GameData/Input`.
- The generated font defaults are `maplestoryLightSdf` and `maplestoryBoldSdf`.
- Canvas UI overrides are stored in `Assets/Resources/Generated/ui-layout-overrides.asset`.
- Unity execution and compilation were not directly verified in this task, so builder, audit, and play-mode validation are still needed afterward.
