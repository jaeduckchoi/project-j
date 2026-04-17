using System.Collections.Generic;
using TMPro;
using UI.Layout;
using UI.Style;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public partial class UIManager
    {
        [SerializeField] private HubPopupUIRegistry popupUiRegistry;

        private PopupFrameUIComponent popupFrameUi;
        private RefrigeratorUIComponent refrigeratorUi;

        private void BindTypedPopupUi()
        {
            if (popupUiRegistry == null)
            {
                popupUiRegistry = GetComponentInChildren<HubPopupUIRegistry>(true);
            }

            popupUiRegistry?.ResolveReferences();
            popupFrameUi = popupUiRegistry != null ? popupUiRegistry.PopupFrame : popupFrameUi;
            refrigeratorUi = popupUiRegistry != null ? popupUiRegistry.RefrigeratorUI : refrigeratorUi;
            popupFrameUi = popupFrameUi != null ? popupFrameUi : GetComponentInChildren<PopupFrameUIComponent>(true);
            refrigeratorUi = refrigeratorUi != null ? refrigeratorUi : GetComponentInChildren<RefrigeratorUIComponent>(true);

            popupFrameUi?.ResolveReferences();
            refrigeratorUi?.ResolveReferences();

            if (popupFrameUi != null)
            {
                popupCloseButton = popupFrameUi.CloseButton;
            }
        }

        private bool ShouldUseTypedPopupUi()
        {
            if (!IsHubScene())
            {
                return false;
            }

            BindTypedPopupUi();
            return popupFrameUi != null && refrigeratorUi != null;
        }

        private void ApplyTypedHubPopupLayout(
            TMP_FontAsset bodyFont,
            TMP_FontAsset headingFont,
            Color textColor)
        {
            if (!ShouldUseTypedPopupUi())
            {
                return;
            }

            if (popupFrameUi.FrameImage != null)
            {
                bool hasShellSkin = PrototypeUISkin.ApplyPanel(
                    popupFrameUi.FrameImage,
                    "RefrigeratorPopupFrame",
                    RefrigeratorBackgroundColor);
                popupFrameUi.FrameImage.color = hasShellSkin ? Color.white : RefrigeratorBackgroundColor;
                popupFrameUi.FrameImage.raycastTarget = false;
            }

            if (popupFrameUi.TitleText != null)
            {
                popupFrameUi.TitleText.text = "냉장고";
                ApplyScreenTextStyle(
                    popupFrameUi.TitleText,
                    headingFont,
                    40f,
                    RefrigeratorTitleColor,
                    TextAlignmentOptions.TopLeft,
                    false,
                    0f,
                    new Vector4(10f, 8f, 10f, 8f),
                    false);
                ApplySceneTextOverride(popupFrameUi.TitleText);
            }

            if (refrigeratorUi.ItemNameText != null)
            {
                ApplyScreenTextStyle(
                    refrigeratorUi.ItemNameText,
                    headingFont,
                    40f,
                    RefrigeratorInfoTextColor,
                    TextAlignmentOptions.TopLeft,
                    false,
                    0f,
                    new Vector4(0f, 0f, 0f, 0f),
                    false);
                ApplySceneTextOverride(refrigeratorUi.ItemNameText);
            }

            if (refrigeratorUi.ItemDescriptionText != null)
            {
                ApplyScreenTextStyle(
                    refrigeratorUi.ItemDescriptionText,
                    bodyFont,
                    24f,
                    RefrigeratorInfoTextColor,
                    TextAlignmentOptions.TopLeft,
                    false,
                    0f,
                    new Vector4(0f, 0f, 0f, 0f),
                    false);
                ApplySceneTextOverride(refrigeratorUi.ItemDescriptionText);
            }

            if (refrigeratorUi.RemoveText != null)
            {
                ApplyScreenTextStyle(
                    refrigeratorUi.RemoveText,
                    headingFont,
                    16f,
                    RefrigeratorRemoveTextColor,
                    TextAlignmentOptions.Center,
                    false,
                    0f,
                    new Vector4(0f, 0f, 0f, 0f),
                    false);
                ApplySceneTextOverride(refrigeratorUi.RemoveText);
            }

            for (int index = 0; index < PrototypeUILayout.RefrigeratorSlotCount; index++)
            {
                Button slotButton = refrigeratorUi.GetSlotButton(index);
                Image slotBackground = refrigeratorUi.GetSlotBackgroundImage(index);
                TextMeshProUGUI amountText = refrigeratorUi.GetSlotAmountText(index);

                if (slotButton != null)
                {
                    slotButton.transition = Selectable.Transition.None;
                    slotButton.interactable = true;
                }

                if (slotBackground != null)
                {
                    bool hasSkin = PrototypeUISkin.ApplyPanel(
                        slotBackground,
                        $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{index + 1:00}",
                        RefrigeratorSlotBaseColor);
                    slotBackground.color = hasSkin ? Color.white : RefrigeratorSlotBaseColor;
                    slotBackground.raycastTarget = true;
                }

                if (amountText != null)
                {
                    ApplyScreenTextStyle(
                        amountText,
                        headingFont,
                        14f,
                        RefrigeratorInfoTextColor,
                        TextAlignmentOptions.BottomRight,
                        false,
                        0f,
                        new Vector4(0f, 0f, 0f, 0f),
                        false);
                    ApplySceneTextOverride(amountText);
                }
            }

            if (refrigeratorUi.SelectedHighlight != null)
            {
                refrigeratorUi.SelectedHighlight.gameObject.SetActive(false);
            }

            if (popupFrameUi.DragGhostImage != null)
            {
                popupFrameUi.DragGhostImage.gameObject.SetActive(false);
                popupFrameUi.DragGhostImage.enabled = false;
                popupFrameUi.DragGhostImage.raycastTarget = false;
            }
        }

        private void ApplyTypedPopupState()
        {
            if (!ShouldUseTypedPopupUi())
            {
                return;
            }

            bool showRefrigeratorPopup = activeHubPanel == HubPopupPanel.Refrigerator;
            if (popupFrameUi.VisibilityRoot != null)
            {
                popupFrameUi.VisibilityRoot.SetActive(showRefrigeratorPopup);
            }

            if (refrigeratorUi.ContentRoot != null)
            {
                refrigeratorUi.ContentRoot.gameObject.SetActive(showRefrigeratorPopup);
            }

            if (!showRefrigeratorPopup)
            {
                refrigeratorWorkspaceInitialized = false;
                selectedRefrigeratorSlotIndex = RefrigeratorNoSlot;
                draggingRefrigeratorSlotIndex = RefrigeratorNoSlot;
                HideRefrigeratorDragGhost();
            }

            SetButtonGameObjectActive(recipePanelButton, false);
            SetButtonGameObjectActive(upgradePanelButton, false);
            SetButtonGameObjectActive(materialPanelButton, false);
        }

        private void RefreshTypedHubPopupContent()
        {
            if (!ShouldUseTypedPopupUi())
            {
                return;
            }

            if (activeHubPanel != HubPopupPanel.Refrigerator)
            {
                RefreshTypedRefrigeratorPopupSlots(null);
                return;
            }

            PopupPanelContent popupContent = BuildRefrigeratorPopupContent();
            RefreshTypedRefrigeratorPopupSlots(popupContent.Entries);
        }

        private void RefreshTypedRefrigeratorPopupSlots(List<PopupListEntry> entries)
        {
            if (!ShouldUseTypedPopupUi())
            {
                return;
            }

            bool isActive = activeHubPanel == HubPopupPanel.Refrigerator;
            if (refrigeratorUi.ContentRoot != null)
            {
                refrigeratorUi.ContentRoot.gameObject.SetActive(isActive);
            }

            if (!isActive)
            {
                if (refrigeratorUi.SelectedHighlight != null)
                {
                    refrigeratorUi.SelectedHighlight.gameObject.SetActive(false);
                }

                HideRefrigeratorDragGhost();
                return;
            }

            SyncRefrigeratorWorkspace(entries ?? new List<PopupListEntry>());
            RenderTypedRefrigeratorPopupSlots();
        }

        private void RenderTypedRefrigeratorPopupSlots()
        {
            if (!ShouldUseTypedPopupUi())
            {
                return;
            }

            EnsureRefrigeratorWorkspace();
            for (int index = 0; index < PrototypeUILayout.RefrigeratorSlotCount; index++)
            {
                RefrigeratorSlotState slotState = refrigeratorSlotStates[index];
                PopupListEntry entry = slotState.Entry;
                bool hasEntry = slotState.HasEntry;

                Button slotButton = refrigeratorUi.GetSlotButton(index);
                Image slotBackground = refrigeratorUi.GetSlotBackgroundImage(index);
                Image slotIcon = refrigeratorUi.GetSlotIcon(index);
                TextMeshProUGUI slotAmountText = refrigeratorUi.GetSlotAmountText(index);

                if (slotButton != null)
                {
                    ConfigureRefrigeratorSlotInteraction(slotButton.gameObject, index);
                    slotButton.onClick.RemoveAllListeners();
                    int slotIndex = index;
                    slotButton.onClick.AddListener(() => HandleRefrigeratorSlotClicked(slotIndex));
                }

                if (slotBackground != null)
                {
                    bool hasSkin = PrototypeUISkin.ApplyPanel(
                        slotBackground,
                        $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{index + 1:00}",
                        RefrigeratorSlotBaseColor);
                    slotBackground.color = hasSkin ? Color.white : RefrigeratorSlotBaseColor;
                }

                if (slotIcon != null)
                {
                    bool showIcon = hasEntry && entry.Icon != null;
                    slotIcon.gameObject.SetActive(showIcon);
                    slotIcon.sprite = showIcon ? entry.Icon : null;
                    slotIcon.color = Color.white;
                    slotIcon.enabled = showIcon;
                    slotIcon.preserveAspect = true;
                }

                if (slotAmountText != null)
                {
                    slotAmountText.gameObject.SetActive(hasEntry);
                    slotAmountText.text = hasEntry ? BuildRefrigeratorSlotAmountText(entry.Title) : string.Empty;
                }
            }

            if (refrigeratorUi.SelectedHighlight != null)
            {
                refrigeratorUi.SelectedHighlight.gameObject.SetActive(false);
            }

            if (refrigeratorUi.RemoveZoneButton != null)
            {
                refrigeratorUi.RemoveZoneButton.onClick.RemoveAllListeners();
                refrigeratorUi.RemoveZoneButton.interactable = CanRemoveSelectedRefrigeratorSlot();
                refrigeratorUi.RemoveZoneButton.onClick.AddListener(HandleRefrigeratorRemoveSelected);
            }

            PopupListEntry selectedEntry = IsValidRefrigeratorSlotIndex(selectedRefrigeratorSlotIndex)
                                           && refrigeratorSlotStates[selectedRefrigeratorSlotIndex].HasEntry
                ? refrigeratorSlotStates[selectedRefrigeratorSlotIndex].Entry
                : null;
            RefreshTypedRefrigeratorInfoPanel(selectedEntry);
        }

        private void RefreshTypedRefrigeratorInfoPanel(PopupListEntry selectedEntry)
        {
            if (!ShouldUseTypedPopupUi())
            {
                return;
            }

            if (refrigeratorUi.InfoIcon != null)
            {
                bool hasIcon = selectedEntry != null && selectedEntry.Icon != null;
                refrigeratorUi.InfoIcon.gameObject.SetActive(hasIcon);
                refrigeratorUi.InfoIcon.sprite = hasIcon ? selectedEntry.Icon : null;
                refrigeratorUi.InfoIcon.color = Color.white;
                refrigeratorUi.InfoIcon.enabled = hasIcon;
                refrigeratorUi.InfoIcon.preserveAspect = true;
            }

            if (refrigeratorUi.ItemNameText != null)
            {
                refrigeratorUi.ItemNameText.text = BuildRefrigeratorItemNameText(selectedEntry);
            }

            if (refrigeratorUi.ItemDescriptionText != null)
            {
                refrigeratorUi.ItemDescriptionText.text = BuildRefrigeratorItemDescriptionText(selectedEntry);
            }
        }
    }
}
