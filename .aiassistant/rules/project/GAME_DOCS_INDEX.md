---
적용: 항상
---

# Jonggu Restaurant Rules Index

## 1. Purpose

This document is the index for the standards that were reorganized from `Assets/Docs` into category-based folders under `.aiassistant/rules`.
Its goal is to separate runtime assets from working standards so both human collaborators and AI assistants can quickly find the right rule documents.

## 2. Current Document Categories

### `project`

- `GAME_ASSISTANT_RULES.md`
  Shared working rules, documentation policy, UI/builder/audit standards, and Git rules.
- `GAME_DOCS_INDEX.md`
  Overall document map and recommended reading order.
- `GAME_PROJECT_STRUCTURE.md`
  Repository structure, `.aiassistant/rules`, `Assets`, generated assets, and editor-code placement rules.

### `gameplay`

- `GAME_FEATURE_REFERENCE.md`
  Integrated summary of game systems, regions, data, and progression axes.
- `GAMEPLAY_CORE_LOOP.md`
  The day loop and state-transition baseline.
- `GAMEPLAY_EXPLORATION.md`
  Exploration regions, tools, hazard zones, and portal-locking rules.
- `GAMEPLAY_RESTAURANT_AND_GROWTH.md`
  Menu flow, restaurant service, storage, upgrades, and reputation flow.

### `ui`

- `UI_AND_TEXT_GUIDE.md`
  HUD/popup structure, TMP fonts, generated UI sprites, and editor-preview guidance.
- `UI_GROUPING_RULES.md`
  Canvas grouping rules centered on `HUDRoot` and `PopupRoot`.

### `scene`

- `GAME_SCENE_AND_SETUP.md`
  Supported scene structure, inspector checkpoints, and recommended test order.
- `SCENE_HIERARCHY_GROUPING_RULES.md`
  World hierarchy rules based on `SceneWorldRoot`, `SceneGameplayRoot`, `SceneSystemRoot`, and `Canvas`.

### `build`

- `GAME_BUILD_GUIDE.md`
  `Tools > Jonggu Restaurant` menu roles and the generated-asset or audit flow.

## 3. Recommended Reading Order

1. `project/GAME_ASSISTANT_RULES.md`
2. `project/GAME_DOCS_INDEX.md`
3. `project/GAME_PROJECT_STRUCTURE.md`
4. `gameplay/GAME_FEATURE_REFERENCE.md`
5. `gameplay/GAMEPLAY_CORE_LOOP.md`
6. `ui/UI_AND_TEXT_GUIDE.md`
7. `scene/GAME_SCENE_AND_SETUP.md`
8. `build/GAME_BUILD_GUIDE.md`

## 4. Quick Navigation By Task Type

- Structure changes
  `project/GAME_PROJECT_STRUCTURE.md`, `scene/SCENE_HIERARCHY_GROUPING_RULES.md`, `build/GAME_BUILD_GUIDE.md`
- Gameplay changes
  `gameplay/GAME_FEATURE_REFERENCE.md`, `gameplay/GAMEPLAY_CORE_LOOP.md`, `gameplay/GAMEPLAY_EXPLORATION.md`, `gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md`
- UI changes
  `ui/UI_AND_TEXT_GUIDE.md`, `ui/UI_GROUPING_RULES.md`, `scene/GAME_SCENE_AND_SETUP.md`
- Builder or audit changes
  `build/GAME_BUILD_GUIDE.md`, `scene/SCENE_HIERARCHY_GROUPING_RULES.md`, `project/GAME_ASSISTANT_RULES.md`

## 5. Current Project Snapshot

- Supported playable scenes are `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, and `WindHill`.
- The core loop is `hub preparation -> morning exploration -> return to hub -> afternoon service -> settlement -> next day`.
- Main progression axes are inventory expansion, lantern unlock, and the reputation-6 shortcut unlock.
- The shared UI baseline is stored in `Assets/Resources/Generated/ui-layout-overrides.asset` and auto-syncs when supported scenes are saved.
- Scene-structure rules are shared through `PrototypeSceneHierarchyCatalog`, and UI-structure rules are shared through `PrototypeUISceneLayoutSettings` and `UIManager`.

## 6. Validation Notes

- This index was updated against the current repository structure and code.
- Unity play mode and batch compilation were not directly verified in this task, so build and audit menu validation is still needed after applying changes.
