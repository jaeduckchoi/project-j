---
적용: 항상
---

# Jonggu Restaurant Scene Hierarchy Grouping Rules

## 1. Purpose

This document defines the shared world-hierarchy grouping baseline for supported scenes (`Hub`, `Beach`, `DeepForest`, `AbandonedMine`, and `WindHill`).
The builder, active-scene organizer, and generated-scene audit should all follow the same parent structure and group names.

## 2. Top-Level Roots

Keep supported scenes aligned to this top-level order:

```text
Scene
├─ SceneWorldRoot
├─ SceneGameplayRoot
├─ SceneSystemRoot
└─ Canvas
```

- `SceneWorldRoot`
  Groups world visuals and world-boundary objects
- `SceneGameplayRoot`
  Groups the player, spawn points, portals, interactables, gatherables, and special zones
- `SceneSystemRoot`
  Groups system objects such as `GameManager`, `RestaurantManager`, `Main Camera`, and `EventSystem`
- `Canvas`
  Keeps the UI at the root level, with `HUDRoot` and `PopupRoot` inside

## 3. Shared Child Groups

### `SceneWorldRoot`

```text
SceneWorldRoot
├─ WorldVisualRoot
└─ WorldBoundsRoot
```

- `WorldVisualRoot`
  Holds floors, backgrounds, props, and world-title visuals
- `WorldBoundsRoot`
  Holds `CameraBounds`, movement limits, and map-wall colliders

### `SceneGameplayRoot`

Only create the groups needed by the current scene.

```text
SceneGameplayRoot
├─ PlayerRoot
├─ SpawnRoot
├─ PortalRoot
├─ InteractionRoot   (hub)
├─ ResourceRoot      (exploration scenes)
└─ ZoneRoot          (scenes with special zones)
```

- `PlayerRoot`
  Holds `Jonggu`
- `SpawnRoot`
  Holds scene-entry spawn points
- `PortalRoot`
  Holds scene-travel portals
- `InteractionRoot`
  Holds hub interactables such as `RecipeSelector`, `ServiceCounter`, `StorageStation`, and `UpgradeStation`
- `ResourceRoot`
  Holds gatherable objects
- `ZoneRoot`
  Holds guide triggers, swamp zones, darkness zones, gust zones, and similar special areas

## 4. Object Placement Rules

- Place hub art under `WorldVisualRoot` through `HubArtRoot`
- Portal pads such as `BeachPortalPad`, `ForestPortalPad`, `MinePortalPad`, and `WindPortalPad` should remain children of their matching portal objects
- Exploration-scene gather pads such as `*_Pad` should remain children of their matching gatherable objects
- World labels such as `*_Title` should stay aligned to the corresponding world-visual or interactable anchor
- `CameraBounds` and movement limits such as `*MovementBounds` or `*Bounds` should remain under `WorldBoundsRoot`

## 5. Scene-Specific Placement Examples

### Hub

- `HubArtRoot` -> `WorldVisualRoot`
- `HubMovementBounds`, `CameraBounds` -> `WorldBoundsRoot`
- `HubEntry` -> `SpawnRoot`
- `GoToBeach`, `GoToDeepForest`, `GoToAbandonedMine`, `GoToWindHill` -> `PortalRoot`
- `RecipeSelector`, `ServiceCounter`, `StorageStation`, `UpgradeStation` -> `InteractionRoot`
- `GameManager`, `RestaurantManager`, `Main Camera`, `EventSystem` -> `SceneSystemRoot`

### Shared Exploration Baseline

- Floors, props, and world titles -> `WorldVisualRoot`
- `CameraBounds`, movement bounds, and map walls -> `WorldBoundsRoot`
- Entry spawn and return portal -> `SpawnRoot`, `PortalRoot`
- Gatherable objects -> `ResourceRoot`
- Guide or special zones -> `ZoneRoot`

## 6. `ZoneRoot` Examples

- `DeepForest`
  `ForestGuide`, `ForestSwampZone`
- `AbandonedMine`
  `MineGuide`, `MineDarkness`
- `WindHill`
  `WindGuide`, `WindLaneZone`

If extra runtime-safety-net objects appear, they should follow the same `ZoneRoot` grouping where practical.

## 7. Working Principles

- Do not fix only the result scenes. Update the builder and organizer so they follow the same grouping rule.
- If group names change, update the generated-scene audit and related documents together.
- UI grouping rules are defined separately in `ui/UI_GROUPING_RULES.md`.
- If you could not verify final scene saving in Unity, confirm the end state through `Tools > Jonggu Restaurant > Prototype Build and Audit` or `Organize Active Scene Hierarchy`.
