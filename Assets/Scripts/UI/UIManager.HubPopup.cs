using System;
using System.Collections.Generic;
using TMPro;
using UI.Content.Catalog;
using UI.Layout;
using UI.Style;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public partial class UIManager
    {
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
                ApplyNamedRectLayout(HudPanelButtonGroupObjectName, PrototypeUILayout.HubPanelButtonGroup);
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

            SetButtonGameObjectActive(recipePanelButton, isHubScene);
            SetButtonGameObjectActive(upgradePanelButton, isHubScene);
            SetButtonGameObjectActive(materialPanelButton, isHubScene);
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
            ApplyNamedRectLayout("PopupFrame", PrototypeUILayout.HubPopupFrame);
            ApplyNamedRectLayout("PopupFrameLeft", PrototypeUILayout.HubPopupFrameLeft);
            ApplyNamedRectLayout("PopupFrameRight", PrototypeUILayout.HubPopupFrameRight);
            ApplyNamedRectLayout("PopupLeftBody", PrototypeUILayout.HubPopupFrameBody);
            ApplyNamedRectLayout("PopupRightBody", PrototypeUILayout.HubPopupFrameBody);
            ApplyHubPopupFrameStyle(headingFont, textColor);
            ApplyPopupCloseButtonLayout(headingFont);
            NormalizeHubPopupHierarchyOrder();
            EnsurePopupBodyItemBoxes(bodyFont, textColor);

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
            SetLegacyHubPopupObjectsActive(false);
        }

        /// <summary>
        /// 허브 팝업 제목과 좌우 섹션 캡션을 현재 메뉴에 맞춰 갱신합니다.
        /// </summary>
        private void ApplyHubPopupFrameStyle(TMP_FontAsset headingFont, Color textColor)
        {
            PrototypeUIPopupDefinition popupDefinition = PrototypeUIPopupCatalog.GetDefinition(ConvertRuntimePopupPanel(activeHubPanel));
            EnsureHubPopupHeadings(popupDefinition, headingFont, textColor);
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

            ApplyManagedRectLayout(rect, layout, preserveExistingLayout: existing != null);

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

        private void RefreshPopupBodyItemBoxes(List<PopupListEntry> entries)
        {
            List<PopupListEntry> safeEntries = entries ?? new List<PopupListEntry>();
            int selectedIndex = safeEntries.FindIndex(entry => entry != null && entry.IsSelected);
            int windowStart = GetPopupBodyWindowStartIndex(safeEntries.Count, selectedIndex, PrototypeUILayout.HubPopupBodyItemBoxCount);

            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"PopupLeftItemBox{i + 1:00}";
                string iconName = $"PopupLeftItemIcon{i + 1:00}";
                string textName = $"PopupLeftItemText{i + 1:00}";
                int entryIndex = windowStart + i;
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
            SetNamedObjectActive(HubResourcePanelObjectName, isVisible);
            SetNamedObjectActive(HudPanelButtonGroupObjectName, isVisible);
            SetNamedObjectActive("ActionDock", false);
            SetNamedObjectActive("ActionAccent", false);
            SetNamedObjectActive("ActionCaption", false);
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

            SetButtonGameObjectActive(recipePanelButton, isVisible);
            SetButtonGameObjectActive(upgradePanelButton, isVisible);
            SetButtonGameObjectActive(materialPanelButton, isVisible);
        }
    }
}
