---
applies: always
---

# Jonggu Restaurant UI And Text Guide

## 1. UI Direction

The UI should stay readable without covering too much of the exploration screen, while still making the flow `check state -> decide -> act` clear at a glance.
The intended visual direction is `brown/gray vector panels + a darker action dock + role-based accent colors`.

- Base design source: `Assets/Design/UIDesign/Vector`
- Mockups and review boards: `Assets/Design/UIDesign/Mockups`
- Generated UI source art: `Assets/Design/GeneratedSources/UI`

`Assets/Design` is only for design-source storage. The actual game should reference generated paths and runtime resource paths instead.

## 2. Current HUD Layout

### Top-Left Status Card

- Current region name
- Gold and reputation
- Quick read of where the player is in the flow

### Top-Center Flow Ribbon

- Current day and phase
- Guide text for what should happen next
- Wide single-line shape that avoids covering too much of the active play area

### Bottom-Right Action Dock

- `Skip Exploration`
- `Skip Service`
- `Next Day`

This area should read clearly as an action cluster, separate from information cards.

### Hub Popups

- `Cooking Menu`
- `Upgrade`
- `Materials`
- `Storage`

All hub popups share the same frame structure and use common anchors such as `PopupTitle`, `PopupLeftCaption`, and the left/right body panels.

## 3. Color And Visual Rules

- Status and flow cards use an `amber` accent
- Bag or material-related surfaces use an `ocean` accent
- Storage-related surfaces use a `forest` accent
- Service-result surfaces use a `coral` accent
- Upgrade-related surfaces use a `gold` accent
- The action dock uses a dark navy-like base

## 4. Text Readability Baseline

- Standardize major UI text with TextMesh Pro
- Give card text dedicated padding and line spacing so multi-line information does not collapse into dense blocks
- Use smaller bold captions with wider tracking to separate card zones quickly
- Use bold world labels with outlines so they remain readable on mixed backgrounds
- Keep multi-line body text inside its card Rect using auto-sizing and masking

## 5. Korean Text And Font Baseline

- Default TMP body font: `Assets/Generated/Fonts/maplestoryLightSdf.asset`
- Default TMP heading font: `Assets/Generated/Fonts/maplestoryBoldSdf.asset`
- TMP Settings should keep Korean line-breaking enabled
- `malgunGothicSdf.asset` may remain as a fallback or legacy asset, but the current builder default is the Maplestory family

## 6. Runtime And Builder Responsibilities

- `Assets/Scripts/UI/UIManager.cs`
  Main runtime entry point for UI behavior and state updates.
- `Assets/Scripts/UI/PopupPauseStateUtility.cs`
  Calculates time-pause apply and restore values for hub popups.
- `Assets/Scripts/UI/Controllers/PrototypeUIDesignController.cs`
  Holds editor-preview state and preview helpers.
- `Assets/Scripts/UI/Content/PrototypeUIPopupCatalog.cs`
  Stores hub popup titles, side captions, and preview sample text.
- `Assets/Scripts/UI/Layout/PrototypeUILayout.UI.cs`
  Manages general HUD layout.
- `Assets/Scripts/UI/Layout/PrototypeUILayout.Popup.cs`
  Manages popup frame layout and repeated body-box layout.
- `Assets/Scripts/UI/Layout/PrototypeUISceneLayoutSettings.cs`
  Stores and applies Canvas layout and display-value overrides.
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.UI.cs`
  Maps general HUD panels and buttons to generated resources.
- `Assets/Scripts/UI/Style/PrototypeUISkinCatalog.Popup.cs`
  Maps popup frame, body, and close-button skins.
- `Assets/Scripts/UI/Style/PrototypeUISkin.cs`
  Loads sprites from resource paths and rebuilds them as 9-slice sprites.
- `Assets/Editor/JongguMinimalPrototypeBuilder.cs`
  Sets up the same HUD/popup structure and default fonts when generating scenes.

## 7. Editor Preview And Auto Sync

- `PrototypeUIDesignControllerEditor`
  Provides `Canvas Grouping`, `Apply Preview`, `Open Scene Builder Preview`, `Refresh SVG Cache`, and `Reset Canvas UI Layouts`.
- `PrototypeUICanvasAutoSync`
  Re-syncs the shared UI override asset when a supported Canvas scene is saved and propagates matching managed changes to other supported scenes.
- The shared asset `Assets/Resources/Generated/ui-layout-overrides.asset` is auto-created on first sync.
- Saving `Hub` refreshes the shared HUD baseline, while saving exploration scenes overlays only the relevant shared managed values from that scene.

## 8. Generated UI Resource Baseline

- Generated UI source art: `Assets/Design/GeneratedSources/UI`
- Generated output path: `Assets/Generated/Sprites/UI`
- Runtime reference path: `Assets/Resources/Generated/Sprites/UI`

Key common objects currently using this system:

- `PopupCloseButton`
- `GuideHelpButton`
- `InteractionPromptBackdrop`
- `GuideBackdrop`
- `ResultBackdrop`
- `PopupFrame`
- `PopupFrameLeft`
- `PopupFrameRight`
- `PopupLeftBody`
- `PopupRightBody`
- `PopupLeftItemBox*`
- `PopupRightItemBox*`

## 9. Resolution Support

- Canvas layout is based on `1920 x 1080`
- The hub world camera also uses a `16:9` locked composition baseline
- Existing scenes should reapply scaler settings at runtime

## 10. What To Check

- Is Korean text rendering stable?
- Are prompts and top guides readable in every exploration scene?
- Does multi-line text stay inside card bounds?
- Does time pause while storage and hub popups are open?
- Is the original time flow restored after closing them?
- Do generated sprite references stay aligned to `Assets/Resources/Generated`?
