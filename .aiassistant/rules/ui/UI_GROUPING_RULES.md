---
적용: 항상
---

# Jonggu Restaurant UI Grouping Rules

## 1. Purpose

This document defines the shared `Canvas` child-group structure used by runtime UI, editor preview, and the default UI builder.
The same rules apply when hand-organizing hub popups or adding new UI objects.

## 2. Top-Level Rules

- Keep only `HUDRoot` and `PopupRoot` directly under `Canvas`
- Put HUD-related objects under `HUDRoot`
- Put hub-popup-related objects under `PopupRoot`
- Keep `InventoryText` as a hub-popup body element only, under the `PopupRoot` side structure

Recommended shape:

```text
Canvas
├─ HUDRoot
└─ PopupRoot
```

## 3. `HUDRoot` Child Groups

- Objects under `HUDRoot` should be split by role
- Group names are fixed
- Keep hub progression buttons under `ActionDock`, while `HUDBottomGroup` remains as the shared Canvas group slot
- Legacy groups such as `HUDInventoryGroup` or `HUDButtonGroup` should not remain

Recommended structure:

```text
Canvas
└─ HUDRoot
   ├─ HUDStatusGroup
   │  ├─ TopLeftPanel
   │  ├─ TopLeftAccent
   │  ├─ PhaseBadge
   │  ├─ GoldText
   │  └─ DayPhaseText
   ├─ HUDActionGroup
   │  ├─ ActionDock
   │  │  ├─ ActionCaption
   │  │  ├─ SkipExplorationButton
   │  │  ├─ SkipServiceButton
   │  │  └─ NextDayButton
   │  └─ ActionAccent
   ├─ HUDBottomGroup
   ├─ HUDPanelButtonGroup
   │  ├─ RecipePanelButton
   │  ├─ UpgradePanelButton
   │  └─ MaterialPanelButton
   ├─ InteractionPromptBackdrop
   ├─ InteractionPromptText
   └─ HUDOverlayGroup
      ├─ GuideBackdrop
      ├─ GuideText
      ├─ GuideHelpButton
      ├─ ResultBackdrop
      └─ RestaurantResultText
```

## 4. `PopupRoot` Child Groups

- `PopupOverlay` belongs under `PopupShellGroup`
- The popup body is managed around a single `PopupFrame`
- `PopupFrame` groups the title, close button, and left/right half-frames
- `PopupFrameHeader` may remain as a legacy compatibility group, but the active baseline is inside `PopupFrame`

Recommended structure:

```text
Canvas
└─ PopupRoot
   ├─ PopupShellGroup
   │  └─ PopupOverlay
   ├─ PopupFrame
   │  ├─ PopupTitle
   │  ├─ PopupCloseButton
   │  ├─ PopupFrameLeft
   │  │  ├─ PopupLeftCaption
   │  │  ├─ PopupLeftBody
   │  │  │  ├─ PopupLeftItemBox01
   │  │  │  │  └─ PopupLeftItemText01
   │  │  │  ├─ PopupLeftItemBox02
   │  │  │  │  └─ PopupLeftItemText02
   │  │  │  ├─ PopupLeftItemBox03
   │  │  │  │  └─ PopupLeftItemText03
   │  │  │  └─ PopupLeftItemBox04
   │  │  │     └─ PopupLeftItemText04
   │  │  └─ InventoryText
   │  └─ PopupFrameRight
   │     ├─ PopupRightCaption
   │     ├─ PopupRightBody
   │     │  ├─ PopupRightItemBox01
   │     │  │  └─ PopupRightItemText01
   │     │  ├─ PopupRightItemBox02
   │     │  │  └─ PopupRightItemText02
   │     │  ├─ PopupRightItemBox03
   │     │  │  └─ PopupRightItemText03
   │     │  └─ PopupRightItemBox04
   │     │     └─ PopupRightItemText04
   │     ├─ StorageText
   │     ├─ SelectedRecipeText
   │     └─ UpgradeText
   └─ PopupFrameHeader
```

## 5. Naming Rules

- Keep the top-level roots as `HUDRoot` and `PopupRoot`
- Keep HUD group names as `HUDStatusGroup`, `HUDActionGroup`, `HUDBottomGroup`, `HUDPanelButtonGroup`, and `HUDOverlayGroup`
- Keep popup group names as `PopupShellGroup`, `PopupFrame`, `PopupFrameLeft`, `PopupFrameRight`, and `PopupFrameHeader`
- Use `PopupLeftBody` and `PopupRightBody` for the side bodies
- Use two-digit numbering such as `PopupLeftItemBox01` and `PopupRightItemBox01` for repeated item boxes
- Each item box should contain exactly one matching `ItemText`

## 6. Editing Rules

- `Apply Preview` should update only style and layout preview
- Actual hierarchy regrouping should happen through `Canvas Grouping`
- If hub popup structure changes, update both `Assets/Scripts/UI/UIManager.cs` and `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
- If popup frame structure changes, update `Assets/Scripts/UI/Layout/PrototypeUILayout.Popup.cs` and `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.Popup.cs` as well
- Because supported-scene saves also auto-sync managed values, re-check saved results after manual regrouping

## 7. Checklist

- Is `Canvas` organized directly into `HUDRoot` and `PopupRoot`?
- Are HUD objects placed under the correct HUD child groups?
- Is `PopupOverlay` under `PopupShellGroup`?
- Are `PopupTitle`, `PopupCloseButton`, `PopupFrameLeft`, and `PopupFrameRight` all under `PopupFrame`?
- Are `PopupLeftBody` and `PopupRightBody` inside the correct half-frame?
- Is the `ItemBox -> ItemText` repetition structure preserved?
- After `Canvas Grouping`, do manually maintained managed groups remain stable?
