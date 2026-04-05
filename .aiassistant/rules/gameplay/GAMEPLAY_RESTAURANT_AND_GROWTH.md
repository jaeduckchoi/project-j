---
적용: 항상
---

# Jonggu Restaurant Service And Growth Systems

## 1. Hub Operations Overview

The hub is the core space for post-exploration cleanup, progression, and restaurant service.
The player walks through the hub directly to choose menus, organize storage, upgrade, and travel to new regions.

## 2. Restaurant Service

### Menu Data

- Menus are managed through `RecipeData`
- Each recipe defines a display name, description, required ingredients, sell price, and reputation delta
- Current generated recipes include:
  - `Fish Platter`
  - `Seafood Soup`
  - `Herb Fish Soup`
  - `Forest Basket`
  - `Glow Moss Stew`
  - `Wind Herb Salad`

### Service Flow

1. Choose a menu
2. Check required ingredients
3. Run service
4. Review the result text
5. Apply gold and reputation gains

### Currently Displayed Information

- Selected recipe description
- Required ingredients
- Currently owned amount
- Whether cooking is possible
- Service result
- Gold gain
- Reputation change

### Service Calculation Baseline

- `RestaurantManager` calculates cookable servings from the currently selected recipe.
- The default daily service cap is `serviceCapacity = 3`.
- The result string is built in a `Today's Service Result` style and passed into the settlement phase.

## 3. Inventory And Storage

### Inventory

- Material-only slot structure
- Starts at `8 slots`
- Expands to `12 slots`
- Ends at `16 slots`

### Storage

- Hub-only access
- Deposit
- Withdraw
- Deposit selected item
- Withdraw selected item
- Deposit all or withdraw all

Storage smooths the hub loop by letting the player manage leftover materials after exploration.

## 4. Economy And Progression

### Gold And Reputation

- Gold is used for service outcomes and upgrade costs
- Reputation is tied to convenience unlocks
- `WindHillShortcut` opens at `Reputation 6`

### Upgrades

Current upgrade focus:

- Inventory expansion
- Lantern unlock

Default costs:

- Expand to 12 slots: `Gold 30 + Shell x3`
- Expand to 16 slots: `Gold 65 + Herb x4`
- Unlock Lantern: `Gold 45 + Mushroom x2`

`UpgradeManager` prefers an action that is currently affordable when choosing what to present first.

## 5. Hub Interaction Points

- Menu selector `RecipeSelector`
- Service counter `ServiceCounter`
- Storage `StorageStation`
- Upgrade station `UpgradeStation`
- Regional portals

All of them use `E` interaction, and if the condition is not met they explain why.

## 6. Hub Popups And Time Pause

- `Cooking Menu`, `Upgrade`, `Materials`, and `Storage` popups all use the same shared popup frame in the hub scene.
- When a popup opens, time is paused using `PopupPauseStateUtility`.
- Closing through `Esc` or `PopupCloseButton` restores the previous time flow.

## 7. Related Code And Assets

- `Assets/Scripts/Restaurant/RestaurantManager.cs`
- `Assets/Scripts/Management/Storage/StorageManager.cs`
- `Assets/Scripts/Management/Storage/StorageStation.cs`
- `Assets/Scripts/Management/Upgrade/UpgradeManager.cs`
- `Assets/Scripts/Management/Economy/EconomyManager.cs`
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Generated/GameData/Recipes/recipe-*.asset`

## 8. Current Implementation Status

- Menu selection, service execution, and settlement all connect end-to-end.
- Storage supports selected-item and full deposit/withdraw flows.
- Upgrades are organized so the currently actionable item is surfaced first.
- Remaining work is mostly balance tuning for service values and upgrade costs.
