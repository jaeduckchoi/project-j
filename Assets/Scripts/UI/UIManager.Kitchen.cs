using System.Collections.Generic;
using CoreLoop.Core;
using Management.Inventory;
using Restaurant.Kitchen;
using Shared.Data;
using UnityEngine;

namespace UI
{
    public partial class UIManager
    {
        private const string KitchenReturnHeldKey = "return-held";
        private const string KitchenFinalizeKey = "action:finalize";
        private const string KitchenBundleKey = "action:bundle";
        private const string KitchenReturnRawKey = "action:return-raw";
        private const string KitchenBasicIngredientKeyPrefix = "basic:";
        private const string KitchenInventoryIngredientKeyPrefix = "inventory:";
        private const string KitchenFrontCounterSlotKeyPrefix = "slot:";

        private RestaurantFlowController cachedKitchenFlow;
        private string selectedKitchenPopupKey;

        public void ShowRefrigeratorPanel()
        {
            if (!IsHubScene() || !Application.isPlaying)
            {
                return;
            }

            BindKitchenFlow();
            activeHubPanel = HubPopupPanel.Refrigerator;
            ApplyMenuPanelState();
        }

        public void ShowFrontCounterPanel()
        {
            if (!IsHubScene() || !Application.isPlaying)
            {
                return;
            }

            BindKitchenFlow();
            activeHubPanel = HubPopupPanel.FrontCounter;
            ApplyMenuPanelState();
        }

        private void BindKitchenFlow()
        {
            RestaurantFlowController flow = Application.isPlaying
                ? RestaurantFlowController.GetOrCreate()
                : FindFirstObjectByType<RestaurantFlowController>();

            if (cachedKitchenFlow == flow)
            {
                return;
            }

            UnbindKitchenFlow();
            cachedKitchenFlow = flow;
            if (cachedKitchenFlow != null)
            {
                cachedKitchenFlow.KitchenStateChanged += HandleKitchenFlowChanged;
            }
        }

        private void UnbindKitchenFlow()
        {
            if (cachedKitchenFlow != null)
            {
                cachedKitchenFlow.KitchenStateChanged -= HandleKitchenFlowChanged;
            }

            cachedKitchenFlow = null;
        }

        private void HandleRefrigeratorPanelRequested()
        {
            ShowRefrigeratorPanel();
        }

        private void HandleFrontCounterPanelRequested()
        {
            ShowFrontCounterPanel();
        }

        private void HandleKitchenFlowChanged()
        {
            RefreshAll();
        }

        private PopupPanelContent BuildRefrigeratorPopupContent()
        {
            BindKitchenFlow();
            List<PopupListEntry> entries = new();
            RestaurantFlowController flow = cachedKitchenFlow;
            if (flow == null)
            {
                return new PopupPanelContent(entries, "Refrigerator is not ready.");
            }

            KitchenCarryItem heldItem = flow.Carry.HeldItem;
            if (heldItem != null && !heldItem.IsBundle && heldItem.State == KitchenItemState.Raw)
            {
                entries.Add(new PopupListEntry(
                    KitchenReturnHeldKey,
                    "Return held ingredient",
                    heldItem.DisplayName,
                    BuildRefrigeratorDetailText(flow),
                    heldItem.Icon,
                    selectedKitchenPopupKey == KitchenReturnHeldKey,
                    () =>
                    {
                        selectedKitchenPopupKey = KitchenReturnHeldKey;
                        flow.TryReturnHeldItemToRefrigerator();
                        RefreshHubPopupContent();
                    }));
            }

            foreach (ResourceData basicResource in flow.GetBasicIngredients())
            {
                if (basicResource == null)
                {
                    continue;
                }

                string key = BuildPopupEntryKey(KitchenBasicIngredientKeyPrefix, basicResource.ResourceId);
                entries.Add(new PopupListEntry(
                    key,
                    $"{basicResource.DisplayName} x unlimited",
                    "Basic ingredient",
                    BuildRefrigeratorDetailText(flow),
                    basicResource.Icon,
                    selectedKitchenPopupKey == key,
                    () =>
                    {
                        selectedKitchenPopupKey = key;
                        flow.TryTakeBasicIngredient(basicResource);
                        RefreshHubPopupContent();
                    }));
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory != null)
            {
                inventory.InitializeIfNeeded();
                foreach (InventoryEntry entry in inventory.RuntimeItems)
                {
                    if (entry == null || entry.resource == null || entry.amount <= 0)
                    {
                        continue;
                    }

                    ResourceData resource = entry.resource;
                    int availableAmount = flow.Reservations.GetAvailableAmount(inventory, resource);
                    string key = BuildPopupEntryKey(KitchenInventoryIngredientKeyPrefix, resource.ResourceId);
                    entries.Add(new PopupListEntry(
                        key,
                        $"{resource.DisplayName} x{availableAmount}/{entry.amount}",
                        "Inventory ingredient",
                        BuildRefrigeratorDetailText(flow),
                        resource.Icon,
                        selectedKitchenPopupKey == key,
                        () =>
                        {
                            selectedKitchenPopupKey = key;
                            flow.TryTakeInventoryIngredient(resource);
                            RefreshHubPopupContent();
                        }));
                }
            }

            return new PopupPanelContent(entries, BuildRefrigeratorDetailText(flow));
        }

        private PopupPanelContent BuildFrontCounterPopupContent()
        {
            BindKitchenFlow();
            List<PopupListEntry> entries = new();
            RestaurantFlowController flow = cachedKitchenFlow;
            if (flow == null)
            {
                return new PopupPanelContent(entries, "FrontCounter is not ready.");
            }

            KitchenDishData finalizableDish = flow.FindFinalizableDish();
            entries.Add(new PopupListEntry(
                KitchenFinalizeKey,
                finalizableDish != null ? $"Finalize {finalizableDish.DisplayName}" : "Finalize",
                finalizableDish != null && flow.Carry.IsEmpty ? "Create final dish" : "Requires exact current-menu signature and empty hands",
                BuildFrontCounterDetailText(flow, finalizableDish),
                finalizableDish != null && finalizableDish.FinalDishItem != null ? finalizableDish.FinalDishItem.Icon : null,
                selectedKitchenPopupKey == KitchenFinalizeKey,
                finalizableDish != null && flow.Carry.IsEmpty
                    ? () =>
                    {
                        selectedKitchenPopupKey = KitchenFinalizeKey;
                        flow.TryFinalizeFrontCounter();
                        RefreshHubPopupContent();
                    }
                    : null));

            entries.Add(new PopupListEntry(
                KitchenBundleKey,
                "Bundle slots",
                flow.Carry.IsEmpty && flow.FrontWorkspace.HasAnyItem ? "Make one KitchenBundle for BackCounter" : "Requires filled slots and empty hands",
                BuildFrontCounterDetailText(flow, finalizableDish),
                null,
                selectedKitchenPopupKey == KitchenBundleKey,
                flow.Carry.IsEmpty && flow.FrontWorkspace.HasAnyItem
                    ? () =>
                    {
                        selectedKitchenPopupKey = KitchenBundleKey;
                        flow.TryBundleFrontCounter();
                        RefreshHubPopupContent();
                    }
                    : null));

            entries.Add(new PopupListEntry(
                KitchenReturnRawKey,
                "Return raw inputs",
                "Return raw ingredients from slots to refrigerator",
                BuildFrontCounterDetailText(flow, finalizableDish),
                null,
                selectedKitchenPopupKey == KitchenReturnRawKey,
                () =>
                {
                    selectedKitchenPopupKey = KitchenReturnRawKey;
                    flow.TryReturnFrontCounterRawInputs();
                    RefreshHubPopupContent();
                }));

            IReadOnlyList<KitchenCarryItem> slots = flow.FrontWorkspace.Slots;
            for (int i = 0; i < KitchenWorkspaceState.MaxSlotCount; i++)
            {
                int slotIndex = i;
                KitchenCarryItem slotItem = slots[i];
                string key = BuildPopupEntryKey(KitchenFrontCounterSlotKeyPrefix, slotIndex.ToString());
                bool canPlace = slotItem == null && flow.Carry.HeldItem != null;
                bool canPick = slotItem != null && flow.Carry.IsEmpty;
                entries.Add(new PopupListEntry(
                    key,
                    slotItem != null ? $"Slot {i + 1}: {slotItem.DisplayName}" : $"Slot {i + 1}: Empty",
                    BuildFrontCounterSlotSummary(flow, slotItem),
                    BuildFrontCounterDetailText(flow, finalizableDish),
                    slotItem != null ? slotItem.Icon : null,
                    selectedKitchenPopupKey == key,
                    canPlace || canPick
                        ? () =>
                        {
                            selectedKitchenPopupKey = key;
                            if (flow.Carry.HeldItem != null)
                            {
                                flow.TryPlaceHeldOnFrontCounter(slotIndex);
                            }
                            else
                            {
                                flow.TryPickFrontCounterSlot(slotIndex);
                            }

                            RefreshHubPopupContent();
                        }
                        : () =>
                        {
                            selectedKitchenPopupKey = key;
                            RefreshHubPopupContent();
                        }));
            }

            return new PopupPanelContent(entries, BuildFrontCounterDetailText(flow, finalizableDish));
        }

        private static string BuildRefrigeratorDetailText(RestaurantFlowController flow)
        {
            KitchenCarryItem heldItem = flow != null ? flow.Carry.HeldItem : null;
            string heldText = heldItem != null ? heldItem.DisplayName : "Empty";
            return "Refrigerator\n"
                + "- Check owned ingredients and basic ingredients.\n"
                + "- Taking an inventory ingredient reserves it; BackCounter consumes it when cooking starts.\n"
                + $"- Hand: {heldText}";
        }

        private static string BuildFrontCounterDetailText(RestaurantFlowController flow, KitchenDishData finalizableDish)
        {
            KitchenCarryItem heldItem = flow != null ? flow.Carry.HeldItem : null;
            string heldText = heldItem != null ? heldItem.DisplayName : "Empty";
            string finalText = finalizableDish != null ? finalizableDish.DisplayName : "No exact final signature";
            string signature = flow != null ? flow.FrontWorkspace.CurrentSignature : string.Empty;
            return "FrontCounter\n"
                + "- Free 5-slot workspace.\n"
                + "- Slot order does not matter; signature is item id + state + quantity.\n"
                + "- Wrong combinations stay as-is and create no failed dish.\n"
                + $"- Hand: {heldText}\n"
                + $"- Final check: {finalText}\n"
                + $"- Signature: {signature}";
        }

        private static string BuildFrontCounterSlotSummary(RestaurantFlowController flow, KitchenCarryItem slotItem)
        {
            if (slotItem != null)
            {
                return flow != null && flow.Carry.IsEmpty ? "Click to pick up" : "Occupied";
            }

            if (flow == null || flow.Carry.HeldItem == null)
            {
                return "Empty";
            }

            return flow.Carry.HeldItem.IsBundle ? "Click to unpack bundle" : "Click to place held item";
        }

        private static string BuildPopupEntryKey(string prefix, string id)
        {
            return $"{prefix}{id ?? string.Empty}";
        }
    }
}
