---
applies: always
---

# Jonggu Restaurant Core Loop

## 1. Core Play Structure

The center of the game is a day-based exploration and management loop.
The player moves directly through the world, gathers ingredients, returns to the hub, runs service, and converts the results into progress for the next run.

Current baseline flow:

1. Check status in the hub
2. Enter a morning exploration region
3. Gather resources and manage inventory
4. Return to the hub
5. Choose a menu
6. Run service
7. Review settlement results
8. Upgrade or advance to the next day

## 2. Phase Names And Transition Baseline

- `Morning Explore`
  The default starting phase. It begins the day and is also restored after advancing to the next day.
- `Afternoon Service`
  Reached automatically when the player returns from an exploration scene to the hub.
- `Settlement`
  Reached after running service or skipping service.

`DayCycleManager` uses these three phases as the baseline for buttons, guide text, and the latest settlement summary.

## 3. Intended Player Feel

- Anticipation when choosing where to go today
- A sense of judgment when deciding what to keep under inventory limits
- Exploration tension when deciding whether to push deeper or return
- Satisfaction when gathered materials become menus and gold
- Growth when upgrades open deeper exploration

## 4. Role Of Each Step

### Hub Preparation

- Check current gold, reputation, and held materials
- Organize storage
- Check upgrade availability
- Decide which region to visit

### Morning Exploration

- Gather region-specific resources
- Check tool requirements
- Feel environmental hazards such as slowdown, gusts, and darkness
- Make collection decisions within inventory limits

### Post-Return Service

- Choose a menu
- Check required ingredients
- Calculate service results
- Earn gold and reputation

### Settlement And Next Day

- Review the result text
- Apply upgrades if needed
- Advance to the next day

## 5. Code-Level Connection Points

- Hub departure and return are handled by `DayCycleManager.HandleSceneTravel`.
- Exploration skip uses `SkipExploration`, service skip uses `SkipService`, and day advancement uses `AdvanceToNextDay`.
- After service completes, `RestaurantManager` builds the result string and passes it to `DayCycleManager.CompleteService`.
- Temporary guides and one-time hints are shown through `ShowTemporaryGuide` and `ShowHintOnce`.

## 6. Important Loop Connections

- Resources gathered in exploration feed directly into menu ingredients and upgrade costs.
- Restaurant results feed back into gold, reputation, and upgrade availability.
- Lantern unlock and reputation both affect region access.
- UI updates button states and guide text based on the current phase.

## 7. Current Implementation Status

- Morning exploration, afternoon service, settlement, and next-day flow are connected in code.
- Exploration skip, service skip, and next-day buttons open and close by phase.
- `Light Automation Audit` checks that these transitions have not regressed badly.
- Final balance values still need real playtesting.
