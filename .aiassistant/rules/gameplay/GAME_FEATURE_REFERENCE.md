---
applies: always
---

# Jonggu Restaurant Feature Reference

## 1. Game Overview

`Jonggu Restaurant` is a 2D top-down exploration and management prototype.
The central feel is a day loop where the player gathers materials during exploration, returns to the hub, runs simplified restaurant service, and converts the results into progression.

The current implementation follows this order:

1. Check status in the hub
2. Enter a morning exploration region
3. Gather resources and manage inventory
4. Return to the hub
5. Choose a menu
6. Run service
7. Settle gold and reputation
8. Upgrade or advance to the next day

## 2. Playable Scenes And Region Characteristics

### Hub

- The hub is named `Jonggu Restaurant`.
- It contains the menu selector, service counter, storage, upgrade workbench, and regional portals.
- A world-space `Today Menu` board shows three featured dishes and keeps a stable anchor for future daily-selection logic.
- Hub popups share a common frame for `Cooking Menu`, `Upgrade`, `Materials`, and `Storage`.

### Beach

- Introductory exploration region.
- Main resources are `Fish`, `Shell`, and `Seaweed`.
- Focuses on the base movement and gathering loop.

### DeepForest

- Mid-game exploration region.
- Main resources are `Mushroom` and `Herb`.
- Includes `ForestSwampZone` slowdown and guide-zone elements.

### AbandonedMine

- Late-game exploration region.
- Main resource is `Glow Moss`.
- `Lantern` is effectively required to access and traverse it.
- Core risk comes from `MineDarkness` and narrow pathing.
- Runtime safety nets may still add augmentation objects such as `MineLooseRubble`.

### WindHill

- Final exploration region.
- Main resource is `Wind Herb`.
- Built around `WindLaneZone` gust timing and the reputation-6 shortcut.

## 3. Core Systems

### Player And Interaction

- `PlayerController`
  Handles movement input and interaction input.
- `InteractionDetector`
  Picks the nearest interactable target and updates the prompt.
- `IInteractable`
  Standardizes interaction contracts for world objects.

### Resources And Gathering

- `ResourceData`
  Stores the resource name, description, icon, rarity, and sell value.
- `GatherableResource`
  Checks required tools and adds the resource to inventory on success.
- Even when blocked, gathering interactions still explain the reason.

### Tools And Access Conditions

- Starting unlocked tools are `Rake`, `Fishing Rod`, and `Sickle`.
- The additional unlockable tool is `Lantern`.
- `ScenePortal` checks morning-only travel, required tools, and required reputation together.
- Returning to the hub remains allowed after the afternoon stage so the day loop can close naturally.

### Inventory And Storage

- Inventory is a material-only slot system.
- Slot progression is `8 -> 12 -> 16`.
- Storage is a hub-only storage system.
- It supports selected-item deposit or withdrawal as well as full deposit or withdrawal.

### Restaurant Service And Menus

- `RecipeData`
  Defines menu name, description, sell price, reputation change, and required ingredients.
- `RestaurantManager`
  Handles the currently selected recipe, cookable servings, material consumption, and result settlement.
- The default daily service cap is `serviceCapacity = 3`.

### Economy And Upgrades

- `EconomyManager`
  Manages gold and reputation.
- `UpgradeManager`
  Handles inventory expansion and tool unlocks.
- Default upgrade costs are:
  - Expand to 12 slots: `Gold 30 + Shell x3`
  - Expand to 16 slots: `Gold 65 + Herb x4`
  - Unlock Lantern: `Gold 45 + Mushroom x2`

### Day Flow

- `DayCycleManager`
  Manages `Morning Explore -> Afternoon Service -> Settlement`.
- Hub departure and return, skipping exploration, skipping service, and advancing to the next day all use the same manager baseline.
- It also handles one-time scene-entry guides and temporary guide messages.

### UI And Text

- Hub HUD and popups are centered around `UIManager`.
- Supported scenes share the Canvas structure rooted at `HUDRoot` and `PopupRoot`.
- When a hub popup is open, time is paused through `PopupPauseStateUtility`.

### Generation And Runtime Safety Nets

- `GeneratedGameDataLocator`
  Re-finds default resources and recipes if generated data references are missing.
- `GeneratedGameDataManifest`
  Keeps generated asset references available in builds.
- `PrototypeSceneRuntimeAugmenter`
  Fills in missing hub pads, portals, shortcuts, and hazard zones at runtime if needed.

## 4. Current Data Baseline

### Resources

- `Fish`
- `Shell`
- `Seaweed`
- `Mushroom`
- `Herb`
- `Glow Moss`
- `Wind Herb`

### Recipes

- `Fish Platter`
- `Seafood Soup`
- `Herb Fish Soup`
- `Forest Basket`
- `Glow Moss Stew`
- `Wind Herb Salad`

## 5. Editor Support Tools

- `Prototype Build and Audit`
  Rebuilds generated assets and base scenes, then runs structural auditing.
- `Organize Active Scene Hierarchy`
  Reorganizes supported scene world groups according to the shared hierarchy baseline.
- `Light Automation Audit`
  Verifies day-loop flow, popup pause behavior, portal locking, and missing-scene guidance regressions.

## 6. Recommended Reference Documents

1. `project/GAME_DOCS_INDEX.md`
2. `gameplay/GAMEPLAY_CORE_LOOP.md`
3. `gameplay/GAMEPLAY_EXPLORATION.md`
4. `gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md`
5. `ui/UI_AND_TEXT_GUIDE.md`
6. `scene/GAME_SCENE_AND_SETUP.md`
7. `build/GAME_BUILD_GUIDE.md`

## 7. Validation Notes

- This document was updated against the current code and generated-data baseline.
- Unity play mode and C# compilation were not directly verified in this task, so runtime validation is still required afterward.
