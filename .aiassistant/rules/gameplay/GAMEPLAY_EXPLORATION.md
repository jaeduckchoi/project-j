---
적용: 항상
---

# Jonggu Restaurant Exploration Systems

## 1. Exploration Overview

Exploration carries most of the game's weight.
The player moves directly through regions, gathers resources, checks tool requirements and environmental risk, and decides when to return.

## 2. Shared Exploration Elements

### Movement And Interaction

- Move with `WASD` or arrow keys
- Interact with `E`
- `InteractionDetector` picks the nearest valid interactable

### Gathering

- Gathering objects use `GatherableResource`
- If a required tool is missing, gathering is blocked and the reason is shown
- On success, the gathered material is added to inventory

### Tools

- Starting unlocked tools: `Rake`, `Fishing Rod`, `Sickle`
- Additional unlockable tool: `Lantern`

Tools do not consume inventory slots and remain permanently unlocked once obtained.

### Portal Locking Rules

- By default, exploration-region travel portals are only open during the `Morning Explore` phase.
- Return-to-hub portals stay open after afternoon begins so the day loop can resolve cleanly.
- Portals also check required tools and required reputation.
- If a condition is missing, the interaction prompt shows the blocking reason instead of the normal action prompt.

## 3. Region-Specific Characteristics

### Beach

- Introductory region
- Resources: `Fish`, `Shell`, `Seaweed`
- Purpose: teach the basic movement and gathering loop
- Trait: minimal hazards

### DeepForest

- Mid-game region
- Resources: `Mushroom`, `Herb`
- Purpose: connect exploration to upgrade-material flow
- Traits: `ForestGuide`, `ForestSwampZone`, and route-choice pressure

### AbandonedMine

- Late-game region
- Resource: `Glow Moss`
- Purpose: high-risk region entered after unlocking the lantern
- Traits: `MineGuide`, `MineDarkness`, and narrow pathing
- Note: runtime augmentation may still add supporting hazard objects such as `MineLooseRubble`

### WindHill

- Final region
- Resource: `Wind Herb`
- Purpose: repeat farming and reputation-based convenience progression
- Traits: `WindGuide`, `WindLaneZone`, and `WindHillShortcut`

## 4. Environmental Hazards

### Slowdown Zones

- Implemented through `MovementModifierZone`
- Used for terrain such as swamp or rubble that should feel hard to cross

### Darkness Zones

- Implemented through `DarknessZone`
- Intended to make traversal and access significantly harder without the lantern

### Gust Zones

- Implemented through `WindGustZone`
- Periodically push the player in one direction

## 5. What To Verify In Exploration

- Do resources block correctly based on intended tool requirements?
- Is the blocking reason communicated clearly?
- Do return portals preserve the return-to-hub flow?
- Are hazard zones visually distinguishable?
- Does inventory pressure produce meaningful collection decisions?
- Does `WindHillShortcut` open after reaching reputation 6?

## 6. Related Code And Assets

- `Assets/Scripts/Exploration/Gathering/GatherableResource.cs`
- `Assets/Scripts/Exploration/World/ScenePortal.cs`
- `Assets/Scripts/Exploration/World/DarknessZone.cs`
- `Assets/Scripts/Exploration/World/MovementModifierZone.cs`
- `Assets/Scripts/Exploration/World/WindGustZone.cs`
- `Assets/Scripts/Exploration/World/PrototypeSceneRuntimeAugmenter.cs`
- `Assets/Scripts/Exploration/World/PrototypeSceneHierarchyCatalog.cs`

## 7. Current Implementation Status

- The four exploration regions `Beach`, `DeepForest`, `AbandonedMine`, and `WindHill` are connected.
- Lantern conditions, gusts, slowdown, darkness, and shortcut flows are connected through code and scene data.
- `Light Automation Audit` also checks portal-locking rules and missing-scene guidance behavior.
- Remaining work is mostly route and balance tuning based on real playtests.
