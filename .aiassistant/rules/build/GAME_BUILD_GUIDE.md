---
적용: 항상
---

# Jonggu Restaurant Build And Generation Guide

## 1. How To Rebuild In The Editor

1. Open the project in Unity
2. Run `Tools > Jonggu Restaurant > Prototype Build and Audit`
3. After the build step finishes, generated-scene auditing should run automatically
4. If needed, run `Light Automation Audit` to recheck core gameplay rules
5. Open `Assets/Scenes/Hub.unity` and verify the flow in Play mode

## 2. Generated Or Refreshed Targets

- `Hub.unity`
- `Beach.unity`
- `DeepForest.unity`
- `AbandonedMine.unity`
- `WindHill.unity`
- generated resources and temporary assets
- generated sprites
- generated TMP fonts
- Build Settings scene list
- shared Canvas UI override asset

## 3. Work Performed During The Build Flow

- Sync the `Hub` Canvas values into the shared override asset first
- Regenerate `Beach`, `DeepForest`, `AbandonedMine`, and `WindHill` UI from that shared override baseline
- Reorganize supported scene world hierarchy around `SceneWorldRoot`, `SceneGameplayRoot`, `SceneSystemRoot`, and `Canvas`
- Normalize generated data assets in `Assets/Generated/GameData` using kebab-case file rules
- Regenerate body and heading TMP fonts around `maplestoryLightSdf` and `maplestoryBoldSdf`
- Keep the runtime data manifest aligned at `Assets/Resources/Generated/generated-game-data-manifest.asset`
- Preserve generated sprite folder roles under both `Assets/Generated/Sprites` and `Assets/Resources/Generated/Sprites`
- Run `PrototypeSceneAudit` to verify generated scene structure and layout

## 4. Menu Roles

- `Prototype Build and Audit`
  Runs generated asset preparation, base scene rebuild, and generated scene auditing in one default flow
- `Rebuild Generated Assets and Scenes`
  Runs only the generation steps without the audit
- `Run Generated Scene Audit Only`
  Rechecks the saved structures of `Hub`, `Beach`, `DeepForest`, `AbandonedMine`, and `WindHill`
- `Organize Active Scene Hierarchy`
  Re-groups the currently open supported scene around the shared world-group roots and saves it
- `Light Automation Audit`
  Quickly verifies core gameplay rules such as the day loop, hub popup pause, portal locks, and missing-scene guidance on top of structural auditing

## 5. When To Run It Again

- When scenes are damaged or generated asset references are missing
- When generated fonts or sprites need to be recreated
- When builder-based layout or UI structure needs to be restored
- When a supported Canvas scene changes shared managed UI values and those changes should propagate to the other supported scenes
- When day-loop rules, portal locking, or popup pause behavior changes and regression verification is needed

## 6. Cautions

- Do not patch only builder outputs. Change the source builder code and layout constants first.
- Saving a supported Canvas scene automatically stores Canvas child `RectTransform`, parent-group and sibling order, deletion state, and `Image`, `TextMeshProUGUI`, and `Button` display values into the shared asset.
- The shared UI override asset path is `Assets/Resources/Generated/ui-layout-overrides.asset`.
- Supported scene world hierarchy follows `scene/SCENE_HIERARCHY_GROUPING_RULES.md`.
- `Prototype Build and Audit` reads the `Hub` baseline first, then reapplies the currently open scene's Canvas values at the end.
- Runtime-only popup resources that the builder does not generate should remain under `Assets/Resources/Generated/Sprites/UI`.
- If generated-scene auditing fails, treat the build flow as failed and resolve the cause first.

## 7. Text And Font Notes

- Generated TMP fonts are recreated based on the project-default font baseline
- UI and world text generated after the build follow current TMP Settings and builder baseline values
- Default body font baseline is `maplestoryLightSdf`, and the heading baseline is `maplestoryBoldSdf`

## 8. Validation Notes

- This document was updated against the current menu structure and code baseline
- Unity execution and real batch compilation were not directly verified in this task, so editor-menu validation is still needed afterward
