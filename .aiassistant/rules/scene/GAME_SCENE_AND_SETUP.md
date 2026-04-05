---
applies: always
---

# Jonggu Restaurant Scene And Setup Guide

## 1. Supported Scene Composition

### Hub

- Main hub scene
- Uses a locked `16:9` camera baseline with world art layers, interaction points, and HUD/popup UI
- Contains the menu selector, service counter, storage, upgrade workbench, and region portals

### Beach

- Introductory exploration scene
- Used to verify the base gathering loop and return-to-hub flow

### DeepForest

- Mid-game exploration scene
- Used to verify mushroom/herb gathering and slowdown zones

### AbandonedMine

- Late-game exploration scene
- Used to verify lantern gating, `Glow Moss`, and darkness flow

### WindHill

- Final exploration scene
- Used to verify gust zones and the reputation-based shortcut

## 2. Shared Runtime Setup

### GameManager

Core linked references:

- `InventoryManager`
- `StorageManager`
- `EconomyManager`
- `ToolManager`
- `DayCycleManager`
- `UpgradeManager`

Some runtime augmentation can compensate for missing references, but direct scene wiring is still easier to maintain and verify.

### UIManager

Key connected fields include:

- `interactionPromptText`
- `inventoryText`
- `storageText`
- `upgradeText`
- `goldText`
- `selectedRecipeText`
- `dayPhaseText`
- `bodyFontAsset`
- `headingFontAsset`
- `skipExplorationButton`
- `skipServiceButton`
- `nextDayButton`
- `recipePanelButton`
- `upgradePanelButton`
- `materialPanelButton`
- `popupCloseButton`

Current UI baseline behavior:

- In the hub, the lower buttons open the `Cooking Menu`, `Upgrade`, and `Materials` popups.
- When a hub popup opens, gameplay pauses. Closing through `Esc` or `PopupCloseButton` restores the original time flow.
- Storage opens through `E` interaction at `StorageStation` rather than automatic proximity UI.
- Major panel and button imagery is shared through generated UI resource paths defined by `PrototypeUISkinCatalog`.

### PrototypeUIDesignController

- Path: `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
- Supports editor helpers such as `Apply Preview`, `Canvas Grouping`, `Open Scene Builder Preview`, `Refresh SVG Cache`, and `Reset Canvas UI Layouts`
- Keeps Canvas objects grouped under `HUDRoot` and `PopupRoot`, and the builder/runtime follow the same structure

## 3. World Hierarchy Baseline

Keep supported scenes aligned to the following top-level structure:

```text
Scene
├─ SceneWorldRoot
├─ SceneGameplayRoot
├─ SceneSystemRoot
└─ Canvas
```

- `SceneWorldRoot`
  World visuals and boundary objects
- `SceneGameplayRoot`
  Player, spawn points, portals, interactables, gatherables, and hazard zones
- `SceneSystemRoot`
  System objects such as `GameManager`, `RestaurantManager`, `Main Camera`, and `EventSystem`
- `Canvas`
  The UI root, with internal grouping based on `HUDRoot` and `PopupRoot`

## 4. Hub Checkpoints

- `RecipeSelector`
- `ServiceCounter`
- `StorageStation`
- `UpgradeStation`
- `GoToBeach`
- `GoToDeepForest`
- `GoToAbandonedMine`
- `GoToWindHill`
- `HubArtRoot`
- `HubTodayMenuBoard`
- `CameraBounds`

Expected hub flow:

1. Choose a menu
2. Choose storage items
3. Deposit or withdraw
4. Review upgrades
5. Travel to a region
6. Run service

## 5. Exploration Scene Checkpoints

- `GatherableResource.resourceData`
- `GatherableResource.requiredToolType`
- Return `ScenePortal`
- `MovementModifierZone`
- `DarknessZone`
- `WindGustZone`

The key is verifying interactability, blocking guidance, return-portal behavior, and hazard feel.

## 6. Recommended Playtest Order

1. In `Hub`, verify text readability, storage `E` popup behavior, and menu-selection UI
2. In `Beach`, gather basic resources and return to the hub
3. In `DeepForest`, verify mushroom/herb gathering and slowdown zones
4. At the hub workbench, check inventory-expansion or lantern-unlock costs
5. In `AbandonedMine`, verify `Glow Moss` and darkness traversal
6. Raise reputation and verify `WindHillShortcut`
7. In the hub, verify menu selection, service, settlement, and next-day flow

## 7. Related Editor Menus

- `Prototype Build and Audit`
- `Rebuild Generated Assets and Scenes`
- `Run Generated Scene Audit Only`
- `Organize Active Scene Hierarchy`
- `Light Automation Audit`

## 8. Current Risks

- Unity execution and C# compilation were not directly verified in this environment
- Final balance numbers may still need adjustment after real playtesting
- `PrototypeSceneRuntimeAugmenter` safety nets are still present and could be reduced later once scene serialization becomes more stable
