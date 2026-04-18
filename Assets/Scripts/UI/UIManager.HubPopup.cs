using System;
using System.Collections.Generic;
using CoreLoop.Core;
using Management.Inventory;
using Shared.Data;
using TMPro;
using UI.Content.Catalog;
using UI.Layout;
using UI.Style;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public partial class UIManager
    {
        private const int RefrigeratorNoSlot = -1;
        private static readonly Color RefrigeratorBackgroundColor = new Color(0.0588f, 0.0784f, 0.1882f, 1f);
        private static readonly Color RefrigeratorTitleColor = new Color(0.8314f, 0.6588f, 0.1569f, 1f);
        private static readonly Color RefrigeratorStorageOutlineColor = new Color(0.1529f, 0.6824f, 0.3765f, 1f);
        private static readonly Color RefrigeratorSlotBaseColor = new Color(0.1647f, 0.2275f, 0.6588f, 1f);
        private static readonly Color RefrigeratorSlotOutlineColor = new Color(0.102f, 0.1255f, 0.4392f, 1f);
        private static readonly Color RefrigeratorSlotSelectedOutlineColor = new Color(0.9529f, 0.6118f, 0.0706f, 1f);
        private static readonly Color RefrigeratorInfoPanelColor = new Color(0.298f, 0.3725f, 0.8314f, 1f);
        private static readonly Color RefrigeratorInfoTextColor = new Color(0.8314f, 0.6588f, 0.1569f, 1f);
        private static readonly Color RefrigeratorRemoveZoneColor = new Color(0.1647f, 0.1882f, 0.4392f, 0.7f);
        private static readonly Color RefrigeratorRemoveTextColor = new Color(0.902f, 0.4941f, 0.1333f, 1f);
        private static readonly Color RefrigeratorTrashBodyColor = new Color(0.5333f, 0.5333f, 0.5333f, 1f);
        private static readonly Color RefrigeratorTrashDarkColor = new Color(0.2667f, 0.2667f, 0.2667f, 1f);
        private static readonly Color RefrigeratorTrashLidColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        private RefrigeratorSlotState[] refrigeratorSlotStates;
        private bool refrigeratorWorkspaceInitialized;
        private int selectedRefrigeratorSlotIndex = RefrigeratorNoSlot;
        private int draggingRefrigeratorSlotIndex = RefrigeratorNoSlot;
        private RectTransform refrigeratorDragGhostRect;
        private Image refrigeratorDragGhostImage;

        private sealed class RefrigeratorSlotState
        {
            public PopupListEntry Entry { get; private set; }
            public bool HasEntry => Entry != null;
            public string Key => Entry != null ? Entry.Key : string.Empty;
            public Sprite Icon => Entry != null ? Entry.Icon : null;
            public ResourceData Resource => Entry != null ? Entry.Resource : null;
            public int Amount => Entry != null ? Entry.Amount : 0;
            public bool CanRemoveFromInventory => Entry != null && Entry.CanRemoveFromInventory;

            public void Set(PopupListEntry entry)
            {
                Entry = entry;
            }

            public void Clear()
            {
                Entry = null;
            }
        }

        private void ApplyCompactHudLayout(
            TMP_FontAsset bodyFont,
            TMP_FontAsset headingFont,
            Color textColor,
            Color oceanAccent,
            Color amberAccent,
            Color goldAccent)
        {
            bool isHubScene = IsHubScene();

            ApplyNamedRectLayout(StatusPanelObjectName(isHubScene), PrototypeUILayout.StatusPanel(isHubScene));
            ApplyNamedRectLayout("InteractionPromptBackdrop", PrototypeUILayout.PromptBackdrop(isHubScene));
            ApplyNamedRectLayout("GuideBackdrop", PrototypeUILayout.GuideBackdrop(isHubScene));
            ApplyNamedRectLayout("ResultBackdrop", PrototypeUILayout.ResultBackdrop(isHubScene));
            ApplyButtonLayout(guideHelpButton, PrototypeUILayout.GuideHelpButton(isHubScene));
            ApplyButtonPresentation(guideHelpButton, headingFont, Color.white);
            if (isHubScene)
            {
                ApplyNamedRectLayout("ActionDock", PrototypeUILayout.HubActionDock);
                ApplyNamedRectLayout("ActionAccent", PrototypeUILayout.HubActionAccent);
                ApplyNamedRectLayout("ActionCaption", PrototypeUILayout.HubActionCaption);
                ApplyNamedRectLayout(HudPanelButtonGroupObjectName, PrototypeUILayout.HubPanelButtonGroup);
                ApplyButtonLayout(openRestaurantButton, PrototypeUILayout.HubOpenRestaurantButton);
                ApplyButtonLayout(closeRestaurantButton, PrototypeUILayout.HubCloseRestaurantButton);
                ApplyButtonPresentation(openRestaurantButton, headingFont, amberAccent);
                ApplyButtonPresentation(closeRestaurantButton, headingFont, goldAccent);
            }

            SetNamedObjectActive(HudPanelButtonGroupObjectName, isHubScene);
            SetNamedObjectActive("PopupOverlay", false);
            SetNamedObjectActive("ActionDock", false);
            SetNamedObjectActive("ActionAccent", false);
            SetNamedObjectActive("ActionCaption", false);
            SetHubCoinBadgeVisualState(isHubScene);

            ApplyManagedRectLayout(goldText != null ? goldText.rectTransform : null, PrototypeUILayout.EconomyText(isHubScene), preserveExistingLayout: true);
            ApplyManagedRectLayout(interactionPromptText != null ? interactionPromptText.rectTransform : null, PrototypeUILayout.PromptText(isHubScene), preserveExistingLayout: true);
            ApplyManagedRectLayout(guideText != null ? guideText.rectTransform : null, PrototypeUILayout.GuideText(isHubScene), preserveExistingLayout: true);
            ApplyManagedRectLayout(resultText != null ? resultText.rectTransform : null, PrototypeUILayout.ResultText(isHubScene), preserveExistingLayout: true);
            ApplyScreenTextStyle(
                goldText,
                headingFont,
                isHubScene ? 18f : 20f,
                isHubScene ? HubCoinTextColor : textColor,
                isHubScene ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.TopLeft,
                false,
                0f,
                isHubScene ? new Vector4(0f, 0f, 8f, 0f) : new Vector4(6f, 2f, 6f, 2f),
                !isHubScene);
            if (goldText != null)
            {
                goldText.enableAutoSizing = true;
                goldText.fontSizeMin = isHubScene ? 12f : goldText.fontSizeMin;
                goldText.fontSizeMax = isHubScene ? 18f : goldText.fontSizeMax;
                goldText.overflowMode = isHubScene ? TextOverflowModes.Truncate : goldText.overflowMode;
                goldText.fontStyle = isHubScene ? FontStyles.Bold : FontStyles.Normal;
            }
            ApplyScreenTextStyle(interactionPromptText, headingFont, 21f, textColor, TextAlignmentOptions.Center, false, 0f, new Vector4(12f, 8f, 12f, 8f), true);
            ApplyScreenTextStyle(guideText, bodyFont, 18f, textColor, TextAlignmentOptions.Center, !isHubScene, 4f, new Vector4(14f, 8f, 14f, 10f), false);
            ApplyScreenTextStyle(resultText, bodyFont, 18f, textColor, TextAlignmentOptions.Center, true, 4f, new Vector4(14f, 10f, 14f, 10f), false);
            ApplySceneTextOverride(goldText);
            ApplySceneTextOverride(interactionPromptText);
            ApplySceneTextOverride(guideText);
            ApplySceneTextOverride(resultText);

            HideLegacyDayRoutineObjects();

            if (isHubScene)
            {
                ApplyHubPanelLayout(bodyFont, headingFont, textColor, oceanAccent, amberAccent, goldAccent);
            }
            else
            {
                ApplyExplorationInventoryLayout();
            }

            bool showLegacyHubButtons = isHubScene && !ShouldUseTypedPopupUi();
            SetButtonGameObjectActive(recipePanelButton, showLegacyHubButtons);
            SetButtonGameObjectActive(upgradePanelButton, showLegacyHubButtons);
            SetButtonGameObjectActive(materialPanelButton, showLegacyHubButtons);
            SetButtonGameObjectActive(openRestaurantButton, false);
            SetButtonGameObjectActive(closeRestaurantButton, false);
        }

        /// <summary>
        /// 허브는 재료/메뉴/업그레이드/창고 팝업을 같은 카드 틀 안에서 공유합니다.
        /// </summary>
        private void ApplyHubPanelLayout(
            TMP_FontAsset bodyFont,
            TMP_FontAsset headingFont,
            Color textColor,
            Color oceanAccent,
            Color amberAccent,
            Color goldAccent)
        {
            if (ShouldUseTypedPopupUi())
            {
                ApplyTypedHubPopupLayout(bodyFont, headingFont, textColor);
                SetLegacyHubPopupObjectsActive(false);
                return;
            }

            ApplyNamedRectLayout("PopupFrame", PrototypeUILayout.HubPopupFrame);
            ApplyNamedRectLayout("PopupFrameLeft", PrototypeUILayout.HubPopupFrameLeft);
            ApplyNamedRectLayout("PopupFrameRight", PrototypeUILayout.HubPopupFrameRight);
            ApplyNamedRectLayout("PopupLeftBody", PrototypeUILayout.HubPopupFrameBody);
            ApplyNamedRectLayout("PopupRightBody", PrototypeUILayout.HubPopupFrameBody);
            ApplyHubPopupFrameStyle(headingFont, textColor);
            ApplyPopupCloseButtonLayout(headingFont);
            NormalizeHubPopupHierarchyOrder();
            EnsurePopupBodyItemBoxes(bodyFont, textColor);
            EnsureRefrigeratorPopupChrome(bodyFont, headingFont, textColor);

            ApplyManagedRectLayout(inventoryText != null ? inventoryText.rectTransform : null, PrototypeUILayout.HubPopupFrameText, preserveExistingLayout: true);
            ApplyManagedRectLayout(storageText != null ? storageText.rectTransform : null, PrototypeUILayout.HubPopupRightDetailText, preserveExistingLayout: true);
            ApplyManagedRectLayout(selectedRecipeText != null ? selectedRecipeText.rectTransform : null, PrototypeUILayout.HubPopupRightDetailText, preserveExistingLayout: true);
            ApplyManagedRectLayout(upgradeText != null ? upgradeText.rectTransform : null, PrototypeUILayout.HubPopupRightDetailText, preserveExistingLayout: true);

            ApplyPopupInventoryTextStyle(inventoryText, bodyFont, textColor);
            ApplyPopupDetailTextStyle(storageText, bodyFont, textColor);
            ApplyPopupDetailTextStyle(selectedRecipeText, bodyFont, textColor);
            ApplyPopupDetailTextStyle(upgradeText, bodyFont, textColor);

            ApplyHubMenuButtonLayout(recipePanelButton, headingFont, amberAccent, PrototypeUILayout.HubRecipePanelButton);
            ApplyHubMenuButtonLayout(upgradePanelButton, headingFont, goldAccent, PrototypeUILayout.HubUpgradePanelButton);
            ApplyHubMenuButtonLayout(materialPanelButton, headingFont, oceanAccent, PrototypeUILayout.HubMaterialPanelButton);
            SetRefrigeratorPopupDesignActive(activeHubPanel == HubPopupPanel.Refrigerator);
            SetLegacyHubPopupObjectsActive(false);
        }

        /// <summary>
        /// 허브 팝업 제목과 좌우 섹션 캡션을 현재 메뉴에 맞춰 갱신합니다.
        /// </summary>
        private void ApplyHubPopupFrameStyle(TMP_FontAsset headingFont, Color textColor)
        {
            PrototypeUIPopupDefinition popupDefinition = PrototypeUIPopupCatalog.GetDefinition(ConvertRuntimePopupPanel(activeHubPanel));
            Color headingColor = activeHubPanel == HubPopupPanel.Refrigerator
                ? RefrigeratorTitleColor
                : textColor;
            EnsureHubPopupHeadings(popupDefinition, headingFont, headingColor);
        }

        /// <summary>
        /// 허브 팝업 헤더는 현재 패널 정의를 한 곳에서 묶어 적용해 빌더와 런타임 기준이 갈라지지 않게 유지합니다.
        /// </summary>
        private void EnsureHubPopupHeadings(PrototypeUIPopupDefinition popupDefinition, TMP_FontAsset font, Color color)
        {
            EnsureUiCaption(PrototypeUIObjectNames.PopupTitle, popupDefinition.Title, PrototypeUILayout.HubPopupTitle, font, color, TextAlignmentOptions.TopLeft);
            EnsureUiCaption(PrototypeUIObjectNames.PopupLeftCaption, popupDefinition.LeftCaption, PrototypeUILayout.HubPopupLeftCaption, font, color, TextAlignmentOptions.TopLeft);
            EnsureUiCaption(PrototypeUIObjectNames.PopupRightCaption, popupDefinition.RightCaption, PrototypeUILayout.HubPopupFrameCaption, font, color, TextAlignmentOptions.TopLeft);
        }

        private static void ApplyPopupDetailTextStyle(TextMeshProUGUI text, TMP_FontAsset bodyFont, Color textColor)
        {
            ApplyScreenTextStyle(text, bodyFont, 18f, textColor, TextAlignmentOptions.TopLeft, true, 0f, new Vector4(10f, 8f, 10f, 8f), false);
            if (text != null)
            {
                text.paragraphSpacing = 0f;
                ApplySceneTextOverride(text);
            }
        }

        private static void ApplyPopupInventoryTextStyle(TextMeshProUGUI text, TMP_FontAsset bodyFont, Color textColor)
        {
            ApplyScreenTextStyle(text, bodyFont, 19f, textColor, TextAlignmentOptions.TopLeft, true, 0f, new Vector4(10f, 8f, 10f, 8f), false);
            if (text != null)
            {
                text.fontSizeMin = 13f;
                text.fontSizeMax = 19f;
                text.paragraphSpacing = 0f;
                text.overflowMode = TextOverflowModes.Masking;
                ApplySceneTextOverride(text);
            }
        }

        private static CaptionPresentationPreset ResolveCaptionPresentation(string objectName)
        {
            return objectName switch
            {
                PrototypeUIObjectNames.PopupTitle => FixedPopupTitlePresentation,
                PrototypeUIObjectNames.PopupLeftCaption or PrototypeUIObjectNames.PopupRightCaption => FixedPopupCaptionPresentation,
                _ => DefaultCaptionPresentation
            };
        }

        private static void ApplyHubPopupObjectIdentity(GameObject target)
        {
            if (target == null || !IsHubPopupDisplayObject(target.name))
            {
                return;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            target.layer = uiLayer >= 0 ? uiLayer : 5;
            target.tag = "Player";
        }

        private static bool IsHubPopupDisplayObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName is PopupRootName or PopupShellGroupName or PopupFrameHeaderGroupName or "PopupOverlay")
            {
                return false;
            }

            return objectName.StartsWith("Popup", StringComparison.Ordinal)
                   || objectName.StartsWith("Refrigerator", StringComparison.Ordinal)
                   || objectName is "InventoryText" or "StorageText" or "SelectedRecipeText" or "UpgradeText";
        }

        private TMP_FontAsset ResolveCaptionFont(string objectName, TMP_FontAsset font)
        {
            return objectName switch
            {
                PrototypeUIObjectNames.PopupTitle or PrototypeUIObjectNames.PopupLeftCaption or PrototypeUIObjectNames.PopupRightCaption
                    => headingFontAsset ?? bodyFontAsset ?? font ?? TMP_Settings.defaultFontAsset,
                _ => font
            };
        }

        private static bool IsFixedPopupHeading(string objectName)
        {
            return objectName == PrototypeUIObjectNames.PopupTitle || objectName == PrototypeUIObjectNames.PopupLeftCaption;
        }

        private void ApplyPopupCloseButtonLayout(TMP_FontAsset headingFont)
        {
            if (popupCloseButton == null)
            {
                return;
            }

            ApplyButtonLayout(popupCloseButton, PrototypeUILayout.HubPopupCloseButton);
            ApplyButtonPresentation(popupCloseButton, headingFont, Color.white);
        }

        private void NormalizeHubPopupHierarchyOrder()
        {
            if (transform == null || ShouldPreserveExistingEditorLayout(preserveExistingLayout: true))
            {
                return;
            }

            Transform popupRoot = transform.Find(PopupRootName);
            if (popupRoot == null)
            {
                return;
            }

            Transform popupShellGroup = popupRoot.Find(PopupShellGroupName);
            Transform popupFrame = popupRoot.Find(PopupFrameGroupName);
            Transform popupFrameHeader = popupRoot.Find(PopupFrameHeaderGroupName);

            if (popupShellGroup != null)
            {
                popupShellGroup.SetSiblingIndex(0);
            }

            if (popupFrame != null)
            {
                popupFrame.SetSiblingIndex(Mathf.Clamp(1, 0, Mathf.Max(0, popupRoot.childCount - 1)));

                Transform popupFrameLeft = popupFrame.Find(PopupFrameLeftGroupName);
                Transform popupFrameRight = popupFrame.Find(PopupFrameRightGroupName);
                if (popupFrameLeft != null)
                {
                    popupFrameLeft.SetSiblingIndex(0);
                }

                if (popupFrameRight != null)
                {
                    popupFrameRight.SetSiblingIndex(1);
                }
            }

            if (popupFrameHeader != null)
            {
                popupFrameHeader.SetSiblingIndex(Mathf.Clamp(2, 0, Mathf.Max(0, popupRoot.childCount - 1)));
            }
        }

        /// <summary>
        /// 허브 팝업 왼쪽 본문은 아이콘, 이름, 짧은 설명이 들어가는 선택 목록으로 유지합니다.
        /// </summary>
        private void EnsurePopupBodyItemBoxes(TMP_FontAsset bodyFont, Color textColor)
        {
            EnsurePopupBodyItemBoxSet("PopupLeftBody", "PopupLeftItemBox", "PopupLeftItemIcon", "PopupLeftItemText", bodyFont, textColor, true);
            SetPopupBodyItemBoxSetActive("PopupRightItemBox", "PopupRightItemIcon", "PopupRightItemText", false);
        }

        private void EnsurePopupBodyItemBoxSet(
            string bodyName,
            string boxPrefix,
            string iconPrefix,
            string textPrefix,
            TMP_FontAsset bodyFont,
            Color textColor,
            bool isInteractive)
        {
            Transform bodyTransform = FindNamedUiTransform(bodyName);
            if (bodyTransform == null)
            {
                return;
            }

            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"{boxPrefix}{i + 1:00}";
                string iconName = $"{iconPrefix}{i + 1:00}";
                string textName = $"{textPrefix}{i + 1:00}";
                Image boxImage = EnsurePopupBodyItemBox(bodyTransform, boxName, PrototypeUILayout.HubPopupBodyItemBox(i), isInteractive);
                Transform contentParent = boxImage != null ? boxImage.transform : bodyTransform;
                EnsurePopupBodyItemIcon(contentParent, iconName);
                EnsurePopupBodyItemText(contentParent, textName, bodyFont, textColor);
            }
        }

        private Image EnsurePopupBodyItemBox(Transform parent, string objectName, PrototypeUIRect layout, bool isInteractive)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject boxObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(boxObject);
            if (existing == null || boxObject.transform.parent != parent)
            {
                boxObject.transform.SetParent(parent, false);
            }

            RectTransform rect = boxObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = boxObject.AddComponent<RectTransform>();
            }

            ApplyManagedRectLayout(rect, layout, preserveExistingLayout: false);

            Image image = boxObject.GetComponent<Image>();
            if (image == null)
            {
                image = boxObject.AddComponent<Image>();
            }

            bool hasSkin = PrototypeUISkin.ApplyPanel(image, objectName, PopupItemBoxFallbackColor);
            bool hasImageOverride = PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
            if (!hasImageOverride)
            {
                image.color = hasSkin ? Color.white : PopupItemBoxFallbackColor;
            }
            image.raycastTarget = isInteractive;

            Shadow shadow = boxObject.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = boxObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;

            Button button = boxObject.GetComponent<Button>();
            if (isInteractive)
            {
                if (button == null)
                {
                    button = boxObject.AddComponent<Button>();
                }

                button.targetGraphic = image;
                button.transition = Selectable.Transition.ColorTint;
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
            }
            else if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
            }

            return image;
        }

        private void EnsurePopupBodyItemIcon(Transform parent, string objectName)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject iconObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(iconObject);
            if (existing == null || iconObject.transform.parent != parent)
            {
                iconObject.transform.SetParent(parent, false);
            }

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = iconObject.AddComponent<RectTransform>();
            }

            ApplyManagedRectLayout(
                rect,
                new PrototypeUIRect(
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(40f, 0f),
                    new Vector2(44f, 44f)),
                preserveExistingLayout: existing != null);

            Image image = iconObject.GetComponent<Image>();
            if (image == null)
            {
                image = iconObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            image.enabled = true;
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private void EnsurePopupBodyItemText(Transform parent, string objectName, TMP_FontAsset bodyFont, Color textColor)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(textObject);
            if (existing == null || textObject.transform.parent != parent)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            ApplyManagedRectLayout(
                rect,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(30f, 0f),
                    new Vector2(-88f, -20f)),
                preserveExistingLayout: existing != null);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = false;
            ApplyScreenTextStyle(text, bodyFont, 17f, textColor, TextAlignmentOptions.TopLeft, true, 0f, Vector4.zero, false);
            text.fontSizeMin = 12f;
            text.fontSizeMax = 17f;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            ApplySceneTextOverride(text);
        }

        private void SetPopupBodyItemBoxSetActive(string boxPrefix, string iconPrefix, string textPrefix, bool isActive)
        {
            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                SetNamedObjectActive($"{boxPrefix}{i + 1:00}", isActive);
                SetNamedObjectActive($"{iconPrefix}{i + 1:00}", isActive);
                SetNamedObjectActive($"{textPrefix}{i + 1:00}", isActive);
            }
        }

        private void EnsureRefrigeratorPopupChrome(TMP_FontAsset bodyFont, TMP_FontAsset headingFont, Color textColor)
        {
            Transform popupFrame = FindNamedUiTransform(PopupFrameGroupName);
            if (popupFrame == null)
            {
                return;
            }

            Image storageImage = EnsureRefrigeratorImage(
                popupFrame,
                PrototypeUIObjectNames.RefrigeratorStorage,
                PrototypeUILayout.HubRefrigeratorStorage,
                new Color(0f, 0f, 0f, 0f),
                false);
            ApplyRefrigeratorStorageVisual(storageImage);
            Transform storageParent = storageImage != null ? storageImage.transform : popupFrame;

            for (int index = 0; index < PrototypeUILayout.RefrigeratorSlotCount; index++)
            {
                string slotName = $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{index + 1:00}";
                Image slotImage = EnsureRefrigeratorImage(
                    storageParent,
                    slotName,
                    PrototypeUILayout.HubRefrigeratorSlot(index),
                    RefrigeratorSlotBaseColor,
                    true);
                ApplyRefrigeratorSlotVisual(slotImage, false);
                Transform slotParent = slotImage != null ? slotImage.transform : storageParent;
                RemoveRefrigeratorSlotHighlight(slotParent, slotName);
                EnsureRefrigeratorSlotIcon(slotParent, $"{PrototypeUIObjectNames.RefrigeratorSlotIconPrefix}{index + 1:00}");
                EnsureRefrigeratorSlotAmount(slotParent, $"{PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix}{index + 1:00}", bodyFont);
                if (slotImage != null)
                {
                    ConfigureRefrigeratorSlotInteraction(slotImage.gameObject, index);
                }
            }

            Image selectedImage = EnsureRefrigeratorImage(
                storageParent,
                PrototypeUIObjectNames.RefrigeratorSelectedSlot,
                PrototypeUILayout.HubRefrigeratorSelectedSlot,
                new Color(0f, 0f, 0f, 0f),
                false);
            if (selectedImage != null)
            {
                selectedImage.raycastTarget = false;
                ApplyRefrigeratorSelectedSlotVisual(selectedImage);
            }

            Image infoPanelImage = EnsureRefrigeratorImage(
                popupFrame,
                PrototypeUIObjectNames.RefrigeratorInfoPanel,
                PrototypeUILayout.HubRefrigeratorInfoPanel,
                RefrigeratorInfoPanelColor,
                false);
            ApplyRefrigeratorInfoPanelVisual(infoPanelImage);
            Transform infoParent = infoPanelImage != null ? infoPanelImage.transform : popupFrame;
            EnsureRefrigeratorInfoIcon(infoParent);
            EnsureRefrigeratorInfoText(
                infoParent,
                PrototypeUIObjectNames.RefrigeratorItemNameText,
                PrototypeUILayout.HubRefrigeratorItemNameText,
                headingFont,
                40f,
                "\uC544\uC774\uD15C \uC774\uB984");
            EnsureRefrigeratorInfoText(
                infoParent,
                PrototypeUIObjectNames.RefrigeratorItemDescriptionText,
                PrototypeUILayout.HubRefrigeratorItemDescriptionText,
                bodyFont,
                24f,
                "\uC544\uC774\uD15C \uC124\uBA85");

            Image removeZoneImage = EnsureRefrigeratorImage(
                popupFrame,
                PrototypeUIObjectNames.RefrigeratorRemoveZone,
                PrototypeUILayout.HubRefrigeratorRemoveZone,
                RefrigeratorRemoveZoneColor,
                true);
            ApplyRefrigeratorRemoveZoneVisual(removeZoneImage);
            Transform removeParent = removeZoneImage != null ? removeZoneImage.transform : popupFrame;
            Image removeIconImage = EnsureRefrigeratorImage(
                removeParent,
                PrototypeUIObjectNames.RefrigeratorRemoveIcon,
                PrototypeUILayout.HubRefrigeratorRemoveIcon,
                new Color(0f, 0f, 0f, 0f),
                false);
            EnsureRefrigeratorTrashIcon(removeIconImage);
            EnsureRefrigeratorRemoveText(removeParent, headingFont);

            Image dragGhost = EnsureRefrigeratorImage(
                popupFrame,
                PrototypeUIObjectNames.RefrigeratorDragGhost,
                PrototypeUILayout.HubRefrigeratorDragGhost,
                Color.white,
                false);
            if (dragGhost != null)
            {
                refrigeratorDragGhostImage = dragGhost;
                refrigeratorDragGhostRect = dragGhost.rectTransform;
                dragGhost.raycastTarget = false;
                dragGhost.preserveAspect = true;
                dragGhost.enabled = false;
                dragGhost.gameObject.SetActive(false);
            }
        }

        private Image EnsureRefrigeratorImage(Transform parent, string objectName, PrototypeUIRect layout, Color fallbackColor, bool isInteractive)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject imageObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(imageObject);
            if (existing == null || imageObject.transform.parent != parent)
            {
                imageObject.transform.SetParent(parent, false);
            }

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = imageObject.AddComponent<RectTransform>();
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(objectName, layout);
            ApplyManagedRectLayout(rect, resolvedLayout, preserveExistingLayout: existing != null);

            Image image = imageObject.GetComponent<Image>();
            if (image == null)
            {
                image = imageObject.AddComponent<Image>();
            }

            bool usesNativeRefrigeratorVisual = UsesNativeRefrigeratorVisual(objectName);
            if (usesNativeRefrigeratorVisual)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = fallbackColor;
            }
            else
            {
                bool hasSkin = PrototypeUISkin.ApplyPanel(image, objectName, fallbackColor);
                bool hasOverride = PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
                if (!hasOverride)
                {
                    image.color = hasSkin ? Color.white : fallbackColor;
                }
            }

            image.raycastTarget = isInteractive;
            image.preserveAspect = objectName == PrototypeUIObjectNames.RefrigeratorRemoveIcon
                                   || objectName == PrototypeUIObjectNames.RefrigeratorDragGhost;

            Button button = imageObject.GetComponent<Button>();
            if (isInteractive)
            {
                if (button == null)
                {
                    button = imageObject.AddComponent<Button>();
                }

                button.targetGraphic = image;
                button.transition = Selectable.Transition.ColorTint;
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                if (usesNativeRefrigeratorVisual)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
                    colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
                    colors.selectedColor = Color.white;
                    colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.75f);
                    button.colors = colors;
                }
            }
            else if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = false;
            }

            return image;
        }

        private static bool UsesNativeRefrigeratorVisual(string objectName)
        {
            return objectName == PrototypeUIObjectNames.RefrigeratorStorage
                   || objectName == PrototypeUIObjectNames.RefrigeratorInfoPanel
                   || objectName == PrototypeUIObjectNames.RefrigeratorSelectedSlot
                   || objectName == PrototypeUIObjectNames.RefrigeratorRemoveZone
                   || objectName == PrototypeUIObjectNames.RefrigeratorRemoveIcon
                   || objectName == PrototypeUIObjectNames.RefrigeratorDragGhost;
        }

        private static void ApplyRefrigeratorStorageVisual(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            Outline outline = EnsureOutline(image.gameObject, RefrigeratorStorageOutlineColor, 2f);
            outline.useGraphicAlpha = false;
        }

        private static void ApplyRefrigeratorSlotVisual(Image image, bool isSelected)
        {
            if (image == null)
            {
                return;
            }

            bool hasSkin = PrototypeUISkin.ApplyPanel(image, image.gameObject.name, RefrigeratorSlotBaseColor);
            image.color = hasSkin ? Color.white : RefrigeratorSlotBaseColor;
            Outline outline = EnsureOutline(
                image.gameObject,
                isSelected ? RefrigeratorSlotSelectedOutlineColor : RefrigeratorSlotOutlineColor,
                isSelected ? 3f : 1.5f);
            outline.useGraphicAlpha = false;
        }

        private static void ApplyRefrigeratorInfoPanelVisual(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = RefrigeratorInfoPanelColor;
            image.raycastTarget = false;
        }

        private static void ApplyRefrigeratorSelectedSlotVisual(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0f);
            Outline outline = EnsureOutline(image.gameObject, RefrigeratorSlotSelectedOutlineColor, 3f);
            outline.useGraphicAlpha = false;
        }

        private static void ApplyRefrigeratorRemoveZoneVisual(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.color = RefrigeratorRemoveZoneColor;
            Outline outline = EnsureOutline(image.gameObject, RefrigeratorSlotOutlineColor, 1.5f);
            outline.useGraphicAlpha = false;
        }

        private static Outline EnsureOutline(GameObject target, Color color, float distance)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            return outline;
        }

        private static void RemoveRefrigeratorSlotHighlight(Transform parent, string slotName)
        {
            if (parent == null)
            {
                return;
            }

            string objectName = $"{slotName}Highlight";
            Transform existing = parent.Find(objectName);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        private static void EnsureRefrigeratorTrashIcon(Image iconImage)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = null;
            iconImage.color = new Color(0f, 0f, 0f, 0f);
            iconImage.raycastTarget = false;
            RectTransform iconRect = iconImage.rectTransform;
            EnsureTrashIconPart(iconRect, "TrashBody", new Vector2(0f, -6f), new Vector2(36f, 34f), RefrigeratorTrashBodyColor);
            EnsureTrashIconPart(iconRect, "TrashLid", new Vector2(0f, 18f), new Vector2(44f, 8f), RefrigeratorTrashLidColor);
            EnsureTrashIconPart(iconRect, "TrashHandle", new Vector2(0f, 26f), new Vector2(16f, 6f), RefrigeratorTrashLidColor);
            EnsureTrashIconPart(iconRect, "TrashLine01", new Vector2(-10f, -6f), new Vector2(2f, 24f), RefrigeratorTrashDarkColor);
            EnsureTrashIconPart(iconRect, "TrashLine02", new Vector2(0f, -6f), new Vector2(2f, 24f), RefrigeratorTrashDarkColor);
            EnsureTrashIconPart(iconRect, "TrashLine03", new Vector2(10f, -6f), new Vector2(2f, 24f), RefrigeratorTrashDarkColor);
        }

        private static void EnsureTrashIconPart(RectTransform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            if (parent == null)
            {
                return;
            }

            Transform existing = parent.Find(objectName);
            GameObject partObject = existing != null ? existing.gameObject : new GameObject(objectName);
            if (existing == null || partObject.transform.parent != parent)
            {
                partObject.transform.SetParent(parent, false);
            }

            RectTransform rect = partObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = partObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Image image = partObject.GetComponent<Image>();
            if (image == null)
            {
                image = partObject.AddComponent<Image>();
            }

            image.sprite = null;
            image.color = color;
            image.raycastTarget = false;
        }

        private void EnsureRefrigeratorSlotIcon(Transform parent, string objectName)
        {
            Image icon = EnsureRefrigeratorImage(parent, objectName, PrototypeUILayout.HubRefrigeratorSlotIcon, Color.white, false);
            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
        }

        private void EnsureRefrigeratorSlotAmount(Transform parent, string objectName, TMP_FontAsset bodyFont)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(textObject);
            if (existing == null || textObject.transform.parent != parent)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                PrototypeUILayout.HubRefrigeratorSlotAmount);
            ApplyManagedRectLayout(rect, resolvedLayout, preserveExistingLayout: false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = false;
            ApplyScreenTextStyle(text, bodyFont, 18f, Color.white, TextAlignmentOptions.BottomRight, false, 0f, Vector4.zero, false);
            text.fontStyle = FontStyles.Bold;
            text.overflowMode = TextOverflowModes.Truncate;
            ApplySceneTextOverride(text);
        }

        private void EnsureRefrigeratorInfoIcon(Transform parent)
        {
            Image icon = EnsureRefrigeratorImage(
                parent,
                PrototypeUIObjectNames.RefrigeratorInfoIcon,
                PrototypeUILayout.HubRefrigeratorInfoIcon,
                Color.white,
                false);
            if (icon == null)
            {
                return;
            }

            icon.sprite = null;
            icon.enabled = false;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void EnsureRefrigeratorInfoText(
            Transform parent,
            string objectName,
            PrototypeUIRect layout,
            TMP_FontAsset font,
            float fontSize,
            string placeholder)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(textObject);
            if (existing == null || textObject.transform.parent != parent)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(objectName, layout);
            ApplyManagedRectLayout(rect, resolvedLayout, preserveExistingLayout: false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.text = placeholder;
            text.raycastTarget = false;
            ApplyScreenTextStyle(text, font, fontSize, RefrigeratorInfoTextColor, TextAlignmentOptions.TopLeft, false, 0f, Vector4.zero, false);
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.5f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            ApplySceneTextOverride(text);
        }

        private void EnsureRefrigeratorRemoveText(Transform parent, TMP_FontAsset headingFont)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(PrototypeUIObjectNames.RefrigeratorRemoveText))
            {
                return;
            }

            Transform existing = FindNamedUiTransform(PrototypeUIObjectNames.RefrigeratorRemoveText);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(PrototypeUIObjectNames.RefrigeratorRemoveText);
            ApplyHubPopupObjectIdentity(textObject);
            if (existing == null || textObject.transform.parent != parent)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                PrototypeUIObjectNames.RefrigeratorRemoveText,
                PrototypeUILayout.HubRefrigeratorRemoveText);
            ApplyManagedRectLayout(rect, resolvedLayout, preserveExistingLayout: false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.text = "\uC81C\uAC70";
            text.raycastTarget = false;
            ApplyScreenTextStyle(text, headingFont, 24f, RefrigeratorRemoveTextColor, TextAlignmentOptions.Center, false, 0f, Vector4.zero, false);
            text.fontStyle = FontStyles.Bold;
            ApplySceneTextOverride(text);
        }

        private void SetRefrigeratorPopupDesignActive(bool isActive)
        {
            ApplyRefrigeratorPopupFrameVisual(isActive);
            ApplyRefrigeratorPopupFrameLayout(isActive);
            EnsureRefrigeratorPopupTitleLayer(isActive);
            SetImageComponentEnabled("PopupFrameLeft", !isActive);
            SetImageComponentEnabled("PopupFrameRight", !isActive);
            SetNamedObjectActive("PopupLeftBody", !isActive && activeHubPanel != HubPopupPanel.None);
            SetNamedObjectActive("PopupRightBody", !isActive && activeHubPanel != HubPopupPanel.None);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupLeftCaption, !isActive && activeHubPanel != HubPopupPanel.None);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupRightCaption, !isActive && activeHubPanel != HubPopupPanel.None);
            SetPopupBodyItemBoxSetActive("PopupLeftItemBox", "PopupLeftItemIcon", "PopupLeftItemText", !isActive && activeHubPanel != HubPopupPanel.None);

            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorStorage, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorInfoPanel, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorInfoIcon, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorItemNameText, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorItemDescriptionText, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorSelectedSlot, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorRemoveZone, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorRemoveIcon, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorRemoveText, false);
            SetNamedObjectActive(PrototypeUIObjectNames.RefrigeratorDragGhost, false);
            for (int index = 0; index < PrototypeUILayout.RefrigeratorSlotCount; index++)
            {
                SetNamedObjectActive($"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{index + 1:00}", isActive);
                SetNamedObjectActive($"{PrototypeUIObjectNames.RefrigeratorSlotIconPrefix}{index + 1:00}", isActive);
                SetNamedObjectActive($"{PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix}{index + 1:00}", isActive);
            }

            if (!isActive)
            {
                refrigeratorWorkspaceInitialized = false;
                selectedRefrigeratorSlotIndex = RefrigeratorNoSlot;
                draggingRefrigeratorSlotIndex = RefrigeratorNoSlot;
            }
        }

        private void ApplyRefrigeratorPopupFrameLayout(bool isActive)
        {
            if (!isActive)
            {
                return;
            }

            Transform popupFrame = FindNamedUiTransform(PopupFrameGroupName);
            if (popupFrame is RectTransform popupFrameRect)
            {
                PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                    PopupFrameGroupName,
                    PrototypeUILayout.HubPopupFrame);
                ApplyManagedRectLayout(popupFrameRect, resolvedLayout, preserveExistingLayout: false);
            }
        }

        private void EnsureRefrigeratorPopupTitleLayer(bool isActive)
        {
            if (!isActive)
            {
                return;
            }

            Transform popupFrame = FindNamedUiTransform(PopupFrameGroupName);
            Transform titleTransform = FindNamedUiTransform(PrototypeUIObjectNames.PopupTitle);
            if (popupFrame == null || titleTransform == null)
            {
                return;
            }

            if (titleTransform.parent != popupFrame)
            {
                titleTransform.SetParent(popupFrame, false);
            }

            if (titleTransform is RectTransform titleRect)
            {
                PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                    PrototypeUIObjectNames.PopupTitle,
                    PrototypeUILayout.HubPopupTitle);
                ApplyManagedRectLayout(titleRect, resolvedLayout, preserveExistingLayout: false);
            }

            titleTransform.gameObject.SetActive(true);
        }

        private void ApplyRefrigeratorPopupFrameVisual(bool isActive)
        {
            Transform popupFrame = FindNamedUiTransform(PopupFrameGroupName);
            if (popupFrame == null || !popupFrame.TryGetComponent(out Image image))
            {
                return;
            }

            if (isActive)
            {
                bool hasRefrigeratorSkin = PrototypeUISkin.ApplyPanel(image, "RefrigeratorPopupFrame", RefrigeratorBackgroundColor);
                image.color = hasRefrigeratorSkin ? Color.white : RefrigeratorBackgroundColor;
                return;
            }

            bool hasDefaultSkin = PrototypeUISkin.ApplyPanel(image, PopupFrameGroupName, Color.clear);
            bool hasOverride = PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, PopupFrameGroupName);
            if (!hasOverride)
            {
                image.color = hasDefaultSkin ? Color.white : Color.clear;
            }
        }

        private void SetImageComponentEnabled(string objectName, bool isEnabled)
        {
            Transform target = FindNamedUiTransform(objectName);
            if (target != null && target.TryGetComponent(out Image image))
            {
                image.enabled = isEnabled;
            }
        }

        private void RefreshPopupBodyItemBoxes(List<PopupListEntry> entries)
        {
            List<PopupListEntry> safeEntries = entries ?? new List<PopupListEntry>();
            int selectedIndex = safeEntries.FindIndex(entry => entry != null && entry.IsSelected);
            bool pinTodayMenuSlots = activeHubPanel == HubPopupPanel.Recipe && safeEntries.Count > Restaurant.TodayMenuState.SlotCount;
            int pinnedSlotCount = pinTodayMenuSlots ? Restaurant.TodayMenuState.SlotCount : 0;
            int remainingEntryCount = Mathf.Max(0, safeEntries.Count - pinnedSlotCount);
            int visibleRecipeCount = Mathf.Max(0, PrototypeUILayout.HubPopupBodyItemBoxCount - pinnedSlotCount);
            int recipeSelectedIndex = selectedIndex >= pinnedSlotCount ? selectedIndex - pinnedSlotCount : -1;
            int recipeWindowStart = pinTodayMenuSlots
                ? GetPopupBodyWindowStartIndex(remainingEntryCount, recipeSelectedIndex, visibleRecipeCount)
                : 0;
            int windowStart = pinTodayMenuSlots
                ? 0
                : GetPopupBodyWindowStartIndex(safeEntries.Count, selectedIndex, PrototypeUILayout.HubPopupBodyItemBoxCount);

            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"PopupLeftItemBox{i + 1:00}";
                string iconName = $"PopupLeftItemIcon{i + 1:00}";
                string textName = $"PopupLeftItemText{i + 1:00}";
                int entryIndex;
                if (pinTodayMenuSlots && i < pinnedSlotCount)
                {
                    entryIndex = i;
                }
                else if (pinTodayMenuSlots)
                {
                    entryIndex = pinnedSlotCount + recipeWindowStart + (i - pinnedSlotCount);
                }
                else
                {
                    entryIndex = windowStart + i;
                }

                bool hasContent = entryIndex >= 0 && entryIndex < safeEntries.Count;
                PopupListEntry entry = hasContent ? safeEntries[entryIndex] : null;

                Transform boxTransform = FindNamedUiTransform(boxName);
                if (boxTransform != null)
                {
                    boxTransform.gameObject.SetActive(hasContent);

                    if (boxTransform.TryGetComponent(out Image boxImage))
                    {
                        boxImage.color = entry != null && entry.IsSelected
                            ? PopupItemBoxSelectedColor
                            : PopupItemBoxFallbackColor;
                    }

                    if (boxTransform.TryGetComponent(out Button button))
                    {
                        button.onClick.RemoveAllListeners();
                        button.interactable = entry != null && entry.OnSelected != null;

                        ColorBlock colors = button.colors;
                        colors.normalColor = entry != null && entry.IsSelected ? PopupItemBoxSelectedColor : PopupItemBoxFallbackColor;
                        colors.highlightedColor = entry != null && entry.IsSelected ? PopupItemBoxSelectedColor : new Color(1f, 0.96f, 0.82f, 1f);
                        colors.pressedColor = new Color(0.92f, 0.86f, 0.68f, 1f);
                        colors.selectedColor = PopupItemBoxSelectedColor;
                        colors.disabledColor = PopupItemBoxFallbackColor;
                        button.colors = colors;

                        if (entry != null && entry.OnSelected != null)
                        {
                            Action onSelected = entry.OnSelected;
                            button.onClick.AddListener(() => onSelected());
                        }
                    }
                }

                Transform iconTransform = FindNamedUiTransform(iconName);
                if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
                {
                    bool hasIcon = hasContent && entry.Icon != null;
                    iconTransform.gameObject.SetActive(hasIcon);
                    iconImage.sprite = hasIcon ? entry.Icon : null;
                    iconImage.color = Color.white;
                    iconImage.enabled = hasIcon;
                }

                Transform textTransform = FindNamedUiTransform(textName);
                if (textTransform != null && textTransform.TryGetComponent(out TextMeshProUGUI text))
                {
                    text.text = hasContent ? FormatPopupBodyEntryText(entry) : string.Empty;
                }
            }
        }

        private void RefreshRefrigeratorPopupSlots(List<PopupListEntry> entries)
        {
            if (ShouldUseTypedPopupUi())
            {
                RefreshTypedRefrigeratorPopupSlots(entries);
                return;
            }

            bool isActive = activeHubPanel == HubPopupPanel.Refrigerator;
            SetRefrigeratorPopupDesignActive(isActive);
            if (!isActive)
            {
                return;
            }

            SyncRefrigeratorWorkspace(entries ?? new List<PopupListEntry>());
            RenderRefrigeratorPopupSlots();
        }

        private void RenderRefrigeratorPopupSlots()
        {
            if (ShouldUseTypedPopupUi())
            {
                RenderTypedRefrigeratorPopupSlots();
                return;
            }

            EnsureRefrigeratorWorkspace();

            for (int index = 0; index < PrototypeUILayout.RefrigeratorSlotCount; index++)
            {
                RefrigeratorSlotState slotState = refrigeratorSlotStates[index];
                PopupListEntry entry = slotState.Entry;
                bool hasEntry = slotState.HasEntry;
                bool isSelected = index == selectedRefrigeratorSlotIndex && hasEntry;
                string slotName = $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{index + 1:00}";
                string iconName = $"{PrototypeUIObjectNames.RefrigeratorSlotIconPrefix}{index + 1:00}";
                string amountName = $"{PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix}{index + 1:00}";

                Transform slotTransform = FindNamedUiTransform(slotName);
                if (slotTransform != null)
                {
                    slotTransform.gameObject.SetActive(true);
                    ConfigureRefrigeratorSlotInteraction(slotTransform.gameObject, index);
                    if (slotTransform.TryGetComponent(out Image slotImage))
                    {
                        ApplyRefrigeratorSlotVisual(slotImage, isSelected);
                    }

                    if (slotTransform.TryGetComponent(out Button button))
                    {
                        button.onClick.RemoveAllListeners();
                        button.interactable = true;
                        int slotIndex = index;
                        button.onClick.AddListener(() => HandleRefrigeratorSlotClicked(slotIndex));
                    }
                }

                Transform iconTransform = FindNamedUiTransform(iconName);
                if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
                {
                    bool hasIcon = hasEntry && entry.Icon != null;
                    iconTransform.gameObject.SetActive(hasIcon);
                    iconImage.sprite = hasIcon ? entry.Icon : null;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                    iconImage.enabled = hasIcon;
                }

                Transform amountTransform = FindNamedUiTransform(amountName);
                if (amountTransform != null && amountTransform.TryGetComponent(out TextMeshProUGUI amountText))
                {
                    amountTransform.gameObject.SetActive(hasEntry);
                    amountText.text = hasEntry ? BuildRefrigeratorSlotAmountText(entry.Title) : string.Empty;
                }
            }

            Transform selectedTransform = FindNamedUiTransform(PrototypeUIObjectNames.RefrigeratorSelectedSlot);
            if (selectedTransform != null)
            {
                bool showSelected = IsValidRefrigeratorSlotIndex(selectedRefrigeratorSlotIndex)
                                    && refrigeratorSlotStates[selectedRefrigeratorSlotIndex].HasEntry;
                selectedTransform.gameObject.SetActive(showSelected);
                if (showSelected && selectedTransform is RectTransform selectedRect)
                {
                    string selectedSlotName = $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{selectedRefrigeratorSlotIndex + 1:00}";
                    PrototypeUIRect selectedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                        selectedSlotName,
                        PrototypeUILayout.HubRefrigeratorSlot(selectedRefrigeratorSlotIndex));
                    ApplyManagedRectLayout(selectedRect, selectedLayout, preserveExistingLayout: false);
                }
            }

            Transform removeZoneTransform = FindNamedUiTransform(PrototypeUIObjectNames.RefrigeratorRemoveZone);
            if (removeZoneTransform != null && removeZoneTransform.TryGetComponent(out Button removeButton))
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.interactable = CanRemoveSelectedRefrigeratorSlot();
                removeButton.onClick.AddListener(HandleRefrigeratorRemoveSelected);
            }

            PopupListEntry selectedEntry = IsValidRefrigeratorSlotIndex(selectedRefrigeratorSlotIndex)
                                           && refrigeratorSlotStates[selectedRefrigeratorSlotIndex].HasEntry
                ? refrigeratorSlotStates[selectedRefrigeratorSlotIndex].Entry
                : null;
            RefreshRefrigeratorInfoPanel(selectedEntry);
        }

        private void RefreshRefrigeratorInfoPanel(PopupListEntry selectedEntry)
        {
            if (ShouldUseTypedPopupUi())
            {
                RefreshTypedRefrigeratorInfoPanel(selectedEntry);
                return;
            }

            Transform iconTransform = FindNamedUiTransform(PrototypeUIObjectNames.RefrigeratorInfoIcon);
            if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
            {
                bool hasIcon = selectedEntry != null && selectedEntry.Icon != null;
                iconTransform.gameObject.SetActive(hasIcon);
                iconImage.sprite = hasIcon ? selectedEntry.Icon : null;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.enabled = hasIcon;
            }

            Transform nameTransform = FindNamedUiTransform(PrototypeUIObjectNames.RefrigeratorItemNameText);
            if (nameTransform != null && nameTransform.TryGetComponent(out TextMeshProUGUI nameText))
            {
                nameText.text = BuildRefrigeratorItemNameText(selectedEntry);
            }

            Transform descriptionTransform = FindNamedUiTransform(PrototypeUIObjectNames.RefrigeratorItemDescriptionText);
            if (descriptionTransform != null && descriptionTransform.TryGetComponent(out TextMeshProUGUI descriptionText))
            {
                descriptionText.text = BuildRefrigeratorItemDescriptionText(selectedEntry);
            }
        }

        private void EnsureRefrigeratorWorkspace()
        {
            if (refrigeratorSlotStates != null
                && refrigeratorSlotStates.Length == PrototypeUILayout.RefrigeratorSlotCount)
            {
                return;
            }

            refrigeratorSlotStates = new RefrigeratorSlotState[PrototypeUILayout.RefrigeratorSlotCount];
            for (int index = 0; index < refrigeratorSlotStates.Length; index++)
            {
                refrigeratorSlotStates[index] = new RefrigeratorSlotState();
            }

            selectedRefrigeratorSlotIndex = RefrigeratorNoSlot;
            draggingRefrigeratorSlotIndex = RefrigeratorNoSlot;
            refrigeratorWorkspaceInitialized = false;
        }

        private void SyncRefrigeratorWorkspace(List<PopupListEntry> entries)
        {
            EnsureRefrigeratorWorkspace();
            Dictionary<string, PopupListEntry> entryByKey = new(StringComparer.Ordinal);
            foreach (PopupListEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                entryByKey[entry.Key] = entry;
            }

            if (!refrigeratorWorkspaceInitialized)
            {
                ClearRefrigeratorWorkspace();
                AddRefrigeratorEntriesToWorkspace(entries, canRemoveFromInventory: true);
                AddRefrigeratorEntriesToWorkspace(entries, canRemoveFromInventory: false);

                selectedRefrigeratorSlotIndex = FindFirstOccupiedRefrigeratorSlot();
                refrigeratorWorkspaceInitialized = true;
                return;
            }

            HashSet<string> remainingKeys = new(entryByKey.Keys, StringComparer.Ordinal);
            for (int index = 0; index < refrigeratorSlotStates.Length; index++)
            {
                RefrigeratorSlotState slotState = refrigeratorSlotStates[index];
                if (!slotState.HasEntry)
                {
                    continue;
                }

                if (entryByKey.TryGetValue(slotState.Key, out PopupListEntry refreshedEntry))
                {
                    slotState.Set(refreshedEntry);
                    remainingKeys.Remove(slotState.Key);
                }
                else
                {
                    slotState.Clear();
                }
            }

            AddRemainingRefrigeratorEntries(entries, remainingKeys, canRemoveFromInventory: true);
            AddRemainingRefrigeratorEntries(entries, remainingKeys, canRemoveFromInventory: false);

            if (!IsValidRefrigeratorSlotIndex(selectedRefrigeratorSlotIndex)
                || !refrigeratorSlotStates[selectedRefrigeratorSlotIndex].HasEntry)
            {
                selectedRefrigeratorSlotIndex = FindFirstOccupiedRefrigeratorSlot();
            }
        }

        private void AddRefrigeratorEntriesToWorkspace(List<PopupListEntry> entries, bool canRemoveFromInventory)
        {
            foreach (PopupListEntry entry in entries)
            {
                if (entry == null || entry.CanRemoveFromInventory != canRemoveFromInventory)
                {
                    continue;
                }

                AddRefrigeratorEntryToFirstEmptySlot(entry);
            }
        }

        private void AddRemainingRefrigeratorEntries(List<PopupListEntry> entries, HashSet<string> remainingKeys, bool canRemoveFromInventory)
        {
            foreach (PopupListEntry entry in entries)
            {
                if (entry == null
                    || entry.CanRemoveFromInventory != canRemoveFromInventory
                    || string.IsNullOrWhiteSpace(entry.Key)
                    || !remainingKeys.Remove(entry.Key))
                {
                    continue;
                }

                AddRefrigeratorEntryToFirstEmptySlot(entry);
            }
        }

        private void ClearRefrigeratorWorkspace()
        {
            EnsureRefrigeratorWorkspace();
            for (int index = 0; index < refrigeratorSlotStates.Length; index++)
            {
                refrigeratorSlotStates[index].Clear();
            }

            selectedRefrigeratorSlotIndex = RefrigeratorNoSlot;
            draggingRefrigeratorSlotIndex = RefrigeratorNoSlot;
        }

        private void AddRefrigeratorEntryToFirstEmptySlot(PopupListEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            EnsureRefrigeratorWorkspace();
            for (int index = 0; index < refrigeratorSlotStates.Length; index++)
            {
                if (refrigeratorSlotStates[index].HasEntry)
                {
                    continue;
                }

                refrigeratorSlotStates[index].Set(entry);
                return;
            }
        }

        private int FindFirstOccupiedRefrigeratorSlot()
        {
            EnsureRefrigeratorWorkspace();
            for (int index = 0; index < refrigeratorSlotStates.Length; index++)
            {
                if (refrigeratorSlotStates[index].HasEntry)
                {
                    return index;
                }
            }

            return RefrigeratorNoSlot;
        }

        private static bool IsValidRefrigeratorSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < PrototypeUILayout.RefrigeratorSlotCount;
        }

        private void HandleRefrigeratorSlotClicked(int slotIndex)
        {
            if (!IsValidRefrigeratorSlotIndex(slotIndex))
            {
                return;
            }

            EnsureRefrigeratorWorkspace();
            if (IsValidRefrigeratorSlotIndex(selectedRefrigeratorSlotIndex)
                && selectedRefrigeratorSlotIndex != slotIndex
                && refrigeratorSlotStates[selectedRefrigeratorSlotIndex].HasEntry)
            {
                MoveOrSwapRefrigeratorSlots(selectedRefrigeratorSlotIndex, slotIndex);
                selectedRefrigeratorSlotIndex = slotIndex;
            }
            else
            {
                selectedRefrigeratorSlotIndex = refrigeratorSlotStates[slotIndex].HasEntry
                    ? slotIndex
                    : RefrigeratorNoSlot;
            }

            RenderRefrigeratorPopupSlots();
        }

        private void MoveOrSwapRefrigeratorSlots(int fromSlotIndex, int toSlotIndex)
        {
            if (!IsValidRefrigeratorSlotIndex(fromSlotIndex)
                || !IsValidRefrigeratorSlotIndex(toSlotIndex)
                || fromSlotIndex == toSlotIndex)
            {
                return;
            }

            RefrigeratorSlotState fromSlot = refrigeratorSlotStates[fromSlotIndex];
            RefrigeratorSlotState toSlot = refrigeratorSlotStates[toSlotIndex];
            PopupListEntry fromEntry = fromSlot.Entry;
            PopupListEntry toEntry = toSlot.Entry;
            fromSlot.Set(toEntry);

            if (fromEntry != null)
            {
                toSlot.Set(fromEntry);
            }
            else
            {
                toSlot.Clear();
            }
        }

        private void ConfigureRefrigeratorSlotInteraction(GameObject slotObject, int slotIndex)
        {
            if (slotObject == null)
            {
                return;
            }

            EventTrigger trigger = slotObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = slotObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            AddRefrigeratorEventTrigger(trigger, EventTriggerType.BeginDrag, eventData => HandleRefrigeratorBeginDrag(slotIndex, eventData));
            AddRefrigeratorEventTrigger(trigger, EventTriggerType.Drag, HandleRefrigeratorDrag);
            AddRefrigeratorEventTrigger(trigger, EventTriggerType.EndDrag, HandleRefrigeratorEndDrag);
        }

        private static void AddRefrigeratorEventTrigger(EventTrigger trigger, EventTriggerType eventType, Action<BaseEventData> callback)
        {
            if (trigger == null || callback == null)
            {
                return;
            }

            EventTrigger.Entry entry = new()
            {
                eventID = eventType
            };
            entry.callback.AddListener(eventData => callback(eventData));
            trigger.triggers.Add(entry);
        }

        private void HandleRefrigeratorBeginDrag(int slotIndex, BaseEventData eventData)
        {
            if (!IsValidRefrigeratorSlotIndex(slotIndex))
            {
                return;
            }

            EnsureRefrigeratorWorkspace();
            RefrigeratorSlotState slotState = refrigeratorSlotStates[slotIndex];
            if (!slotState.HasEntry)
            {
                return;
            }

            selectedRefrigeratorSlotIndex = slotIndex;
            draggingRefrigeratorSlotIndex = slotIndex;
            ShowRefrigeratorDragGhost(slotState.Icon, eventData as PointerEventData);
            RenderRefrigeratorPopupSlots();
        }

        private void HandleRefrigeratorDrag(BaseEventData eventData)
        {
            UpdateRefrigeratorDragGhostPosition(eventData as PointerEventData);
        }

        private void HandleRefrigeratorEndDrag(BaseEventData eventData)
        {
            int sourceSlotIndex = draggingRefrigeratorSlotIndex;
            draggingRefrigeratorSlotIndex = RefrigeratorNoSlot;
            HideRefrigeratorDragGhost();

            if (!IsValidRefrigeratorSlotIndex(sourceSlotIndex)
                || !refrigeratorSlotStates[sourceSlotIndex].HasEntry)
            {
                RenderRefrigeratorPopupSlots();
                return;
            }

            PointerEventData pointerEventData = eventData as PointerEventData;
            if (IsPointerOverRefrigeratorRemoveZone(pointerEventData))
            {
                TryRemoveRefrigeratorSlot(sourceSlotIndex);
                return;
            }

            if (TryGetPointerRefrigeratorSlotIndex(pointerEventData, out int targetSlotIndex)
                && targetSlotIndex != sourceSlotIndex)
            {
                MoveOrSwapRefrigeratorSlots(sourceSlotIndex, targetSlotIndex);
                selectedRefrigeratorSlotIndex = targetSlotIndex;
            }

            RenderRefrigeratorPopupSlots();
        }

        private void ShowRefrigeratorDragGhost(Sprite sprite, PointerEventData eventData)
        {
            if (ShouldUseTypedPopupUi() && refrigeratorUi != null)
            {
                refrigeratorDragGhostImage = refrigeratorUi.DragGhostImage;
                refrigeratorDragGhostRect = refrigeratorUi.DragGhostRect;
            }

            if (refrigeratorDragGhostImage == null || refrigeratorDragGhostRect == null)
            {
                Transform existing = FindNamedUiTransform(PrototypeUIObjectNames.RefrigeratorDragGhost);
                if (existing != null)
                {
                    refrigeratorDragGhostImage = existing.GetComponent<Image>();
                    refrigeratorDragGhostRect = existing as RectTransform;
                }
            }

            if (refrigeratorDragGhostImage == null || refrigeratorDragGhostRect == null || sprite == null)
            {
                return;
            }

            refrigeratorDragGhostImage.sprite = sprite;
            refrigeratorDragGhostImage.color = new Color(1f, 1f, 1f, 0.82f);
            refrigeratorDragGhostImage.preserveAspect = true;
            refrigeratorDragGhostImage.raycastTarget = false;
            refrigeratorDragGhostImage.enabled = true;
            refrigeratorDragGhostImage.gameObject.SetActive(true);
            refrigeratorDragGhostRect.SetAsLastSibling();
            UpdateRefrigeratorDragGhostPosition(eventData);
        }

        private void UpdateRefrigeratorDragGhostPosition(PointerEventData eventData)
        {
            if (refrigeratorDragGhostRect == null || eventData == null)
            {
                return;
            }

            RectTransform parentRect = refrigeratorDragGhostRect.parent as RectTransform;
            if (parentRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                refrigeratorDragGhostRect.anchoredPosition = localPoint;
            }
            else
            {
                refrigeratorDragGhostRect.position = eventData.position;
            }
        }

        private void HideRefrigeratorDragGhost()
        {
            if (refrigeratorDragGhostImage != null)
            {
                refrigeratorDragGhostImage.sprite = null;
                refrigeratorDragGhostImage.enabled = false;
                refrigeratorDragGhostImage.gameObject.SetActive(false);
            }
        }

        private bool TryGetPointerRefrigeratorSlotIndex(PointerEventData eventData, out int slotIndex)
        {
            slotIndex = RefrigeratorNoSlot;
            Transform current = eventData != null && eventData.pointerEnter != null
                ? eventData.pointerEnter.transform
                : null;
            while (current != null)
            {
                if (TryParseRefrigeratorSlotIndex(current.name, PrototypeUIObjectNames.RefrigeratorSlotPrefix, out slotIndex))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private bool IsPointerOverRefrigeratorRemoveZone(PointerEventData eventData)
        {
            Transform current = eventData != null && eventData.pointerEnter != null
                ? eventData.pointerEnter.transform
                : null;
            while (current != null)
            {
                if (current.name == PrototypeUIObjectNames.RefrigeratorRemoveZone)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private bool CanRemoveSelectedRefrigeratorSlot()
        {
            return IsValidRefrigeratorSlotIndex(selectedRefrigeratorSlotIndex)
                   && refrigeratorSlotStates != null
                   && refrigeratorSlotStates[selectedRefrigeratorSlotIndex].HasEntry
                   && refrigeratorSlotStates[selectedRefrigeratorSlotIndex].CanRemoveFromInventory;
        }

        private void HandleRefrigeratorRemoveSelected()
        {
            if (IsValidRefrigeratorSlotIndex(selectedRefrigeratorSlotIndex))
            {
                TryRemoveRefrigeratorSlot(selectedRefrigeratorSlotIndex);
            }
        }

        private void TryRemoveRefrigeratorSlot(int slotIndex)
        {
            if (!IsValidRefrigeratorSlotIndex(slotIndex)
                || refrigeratorSlotStates == null
                || !refrigeratorSlotStates[slotIndex].HasEntry)
            {
                return;
            }

            RefrigeratorSlotState slotState = refrigeratorSlotStates[slotIndex];
            ResourceData resource = slotState.Resource;
            if (!slotState.CanRemoveFromInventory || resource == null)
            {
                RenderRefrigeratorPopupSlots();
                return;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            bool removed = inventory != null && inventory.TryRemove(resource, 1);
            if (!removed)
            {
                RefreshHubPopupContent();
                return;
            }

            if (slotState.Amount <= 1)
            {
                slotState.Clear();
                if (selectedRefrigeratorSlotIndex == slotIndex)
                {
                    selectedRefrigeratorSlotIndex = RefrigeratorNoSlot;
                }
            }

            RefreshHubPopupContent();
        }

        private static string BuildRefrigeratorItemNameText(PopupListEntry entry)
        {
            if (entry == null)
            {
                return "\uC544\uC774\uD15C \uC774\uB984";
            }

            if (entry.Resource != null && !string.IsNullOrWhiteSpace(entry.Resource.DisplayName))
            {
                return entry.Resource.DisplayName;
            }

            string title = entry.Title ?? string.Empty;
            int amountIndex = title.LastIndexOf(" x", StringComparison.Ordinal);
            return amountIndex > 0 ? title[..amountIndex].Trim() : title.Trim();
        }

        private static string BuildRefrigeratorItemDescriptionText(PopupListEntry entry)
        {
            if (entry == null)
            {
                return "\uC544\uC774\uD15C \uC124\uBA85";
            }

            string description = entry.Resource != null ? entry.Resource.Description : string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                description = entry.Summary;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                description = entry.Detail;
            }

            return string.IsNullOrWhiteSpace(description)
                ? "\uC544\uC774\uD15C \uC124\uBA85"
                : description.Trim();
        }

        private static string BuildRefrigeratorSlotAmountText(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            int markerIndex = title.LastIndexOf('x');
            if (markerIndex < 0 || markerIndex >= title.Length - 1)
            {
                return string.Empty;
            }

            string suffix = title[(markerIndex + 1)..].Trim();
            int separatorIndex = suffix.IndexOf('/');
            if (separatorIndex >= 0)
            {
                suffix = suffix[..separatorIndex].Trim();
            }

            return string.IsNullOrWhiteSpace(suffix) ? string.Empty : $"x{suffix}";
        }

        private static int GetPopupBodyWindowStartIndex(int entryCount, int selectedIndex, int visibleCount)
        {
            if (entryCount <= visibleCount || visibleCount <= 0)
            {
                return 0;
            }

            int clampedSelectedIndex = Mathf.Clamp(selectedIndex, 0, entryCount - 1);
            int preferredStart = clampedSelectedIndex - (visibleCount / 2);
            return Mathf.Clamp(preferredStart, 0, entryCount - visibleCount);
        }

        private static string FormatPopupBodyEntryText(PopupListEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            string title = entry.IsSelected ? $"<b>{entry.Title}</b>" : entry.Title;
            if (string.IsNullOrWhiteSpace(entry.Summary))
            {
                return title;
            }

            return $"{title}\n<size=78%>{entry.Summary}</size>";
        }

        /// <summary>
        /// 탐험 씬은 우측 상단 재료/가방 HUD만 유지하고 허브용 카드는 숨깁니다.
        /// </summary>
        private void ApplyExplorationInventoryLayout()
        {
            SetHubPopupDesignActive(false);
            SetLegacyHubPopupObjectsActive(false);
        }

        /// <summary>
        /// 허브 하단 버튼은 같은 형태를 쓰므로 배치와 스타일을 공통 메서드로 묶습니다.
        /// </summary>
        private void ApplyHubMenuButtonLayout(Button button, TMP_FontAsset headingFont, Color accentColor, PrototypeUIRect layout)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            ApplyManagedRectLayout(rect, layout, preserveExistingLayout: true);
            ApplyButtonPresentation(button, headingFont, accentColor);
            button.gameObject.SetActive(true);
        }

        private static void SetButtonGameObjectActive(Button button, bool isActive)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isActive);
            }
        }

        private void SetLegacyHubPopupObjectsActive(bool isActive)
        {
            SetNamedObjectActive("StorageCard", isActive);
            SetNamedObjectActive("StorageAccent", isActive);
            SetNamedObjectActive("StorageCaption", isActive);
            SetNamedObjectActive("RecipeCard", isActive);
            SetNamedObjectActive("RecipeAccent", isActive);
            SetNamedObjectActive("RecipeCaption", isActive);
            SetNamedObjectActive("UpgradeCard", isActive);
            SetNamedObjectActive("UpgradeAccent", isActive);
            SetNamedObjectActive("UpgradeCaption", isActive);
            SetNamedObjectActive("PopupLeftPanel", false);
            SetNamedObjectActive("PopupRightPanel", false);
        }

        private void SetHubPopupDesignActive(bool isActive)
        {
            SetNamedObjectActive("PopupFrame", isActive);
            SetNamedObjectActive("PopupFrameLeft", isActive);
            SetNamedObjectActive("PopupFrameRight", isActive);
            SetNamedObjectActive("PopupLeftBody", isActive);
            SetNamedObjectActive("PopupRightBody", isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupTitle, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupLeftCaption, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupRightCaption, isActive);
            SetButtonGameObjectActive(popupCloseButton, isActive);
        }

        private void SetHubHudVisible(bool isVisible)
        {
            bool showActionHud = isVisible && IsHubScene() && !ShouldUseTypedPopupUi();
            SetNamedObjectActive(HubResourcePanelObjectName, isVisible);
            SetNamedObjectActive(HudPanelButtonGroupObjectName, isVisible);
            SetNamedObjectActive("ActionDock", showActionHud);
            SetNamedObjectActive("ActionAccent", showActionHud);
            SetNamedObjectActive("ActionCaption", showActionHud);
            HideLegacyDayRoutineObjects();

            if (goldText != null)
            {
                goldText.gameObject.SetActive(isVisible);
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(isVisible && !string.IsNullOrWhiteSpace(interactionPromptText.text));
            }

            SetNamedObjectActive("InteractionPromptBackdrop", isVisible && interactionPromptText != null && !string.IsNullOrWhiteSpace(interactionPromptText.text));
            SetButtonGameObjectActive(guideHelpButton, isVisible);

            bool showLegacyHubButtons = isVisible && !ShouldUseTypedPopupUi();
            SetButtonGameObjectActive(recipePanelButton, showLegacyHubButtons);
            SetButtonGameObjectActive(upgradePanelButton, showLegacyHubButtons);
            SetButtonGameObjectActive(materialPanelButton, showLegacyHubButtons);
            SetButtonGameObjectActive(openRestaurantButton, showActionHud);
            SetButtonGameObjectActive(closeRestaurantButton, showActionHud);
        }
    }
}
