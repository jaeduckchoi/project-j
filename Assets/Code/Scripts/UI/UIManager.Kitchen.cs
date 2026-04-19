using System.Collections.Generic;
using System.Text;
using Code.Scripts.CoreLoop.Core;
using Code.Scripts.Management.Inventory;
using Code.Scripts.Restaurant;
using Code.Scripts.Restaurant.Kitchen;
using Code.Scripts.Shared.Data;
using Code.Scripts.UI.Content.Catalog;
using UnityEngine;

namespace Code.Scripts.UI
{
    public partial class UIManager
    {
        private const string KitchenStartCookingKey = "action:start-cooking";
        private const string KitchenClearSelectionKey = "action:clear-selection";
        private const string KitchenIngredientKeyPrefix = "ingredient:";
        private const string KitchenSelectedIngredientKeyPrefix = "selected:";
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

        private void ShowCookingToolPanel(KitchenToolType toolType)
        {
            if (!IsHubScene() || !Application.isPlaying)
            {
                return;
            }

            BindKitchenFlow();
            if (cachedKitchenFlow == null)
            {
                return;
            }

            cachedKitchenFlow.SelectCookingTool(toolType);
            activeHubPanel = HubPopupPanel.KitchenTool;
            ApplyMenuPanelState();
        }

        private void BindKitchenFlow()
        {
            RestaurantFlowController flow = Application.isPlaying && IsHubScene() && HubRuntimeContext.Active != null
                ? HubRuntimeContext.Active.RestaurantFlowController
                : Application.isPlaying
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

        private void HandleCookingToolPanelRequested(KitchenToolType toolType)
        {
            ShowCookingToolPanel(toolType);
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
                return new PopupPanelContent(entries, "냉장고 정보를 찾지 못했습니다.");
            }

            foreach (ResourceData basicResource in flow.GetBasicIngredients())
            {
                if (basicResource == null)
                {
                    continue;
                }

                string key = BuildPopupEntryKey(KitchenIngredientKeyPrefix, basicResource.ResourceId);
                entries.Add(new PopupListEntry(
                    key,
                    $"{basicResource.DisplayName} x∞",
                    "기본 재료",
                    basicResource.Description,
                    basicResource.Icon,
                    selectedKitchenPopupKey == key,
                    () =>
                    {
                        selectedKitchenPopupKey = key;
                        RefreshHubPopupContent();
                    },
                    basicResource));
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
                    string key = BuildPopupEntryKey(KitchenIngredientKeyPrefix, resource.ResourceId);
                    entries.Add(new PopupListEntry(
                        key,
                        $"{resource.DisplayName} x{entry.amount}",
                        "보유 재료",
                        resource.Description,
                        resource.Icon,
                        selectedKitchenPopupKey == key,
                        () =>
                        {
                            selectedKitchenPopupKey = key;
                            RefreshHubPopupContent();
                        },
                        resource,
                        entry.amount));
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
                return new PopupPanelContent(entries, "PassCounter 정보를 찾지 못했습니다.");
            }

            IReadOnlyList<KitchenCarryItem> slots = flow.FrontWorkspace.Slots;
            for (int index = 0; index < KitchenWorkspaceState.MaxSlotCount; index++)
            {
                int slotIndex = index;
                KitchenCarryItem slotItem = slots[index];
                string key = BuildPopupEntryKey(KitchenFrontCounterSlotKeyPrefix, slotIndex.ToString());
                bool canPlace = slotItem == null && flow.Carry.HeldItem != null && !flow.Carry.HeldItem.IsBundle;
                bool canPick = slotItem != null && flow.Carry.IsEmpty;
                entries.Add(new PopupListEntry(
                    key,
                    slotItem != null ? $"보관 칸 {index + 1}: {slotItem.DisplayName}" : $"보관 칸 {index + 1}: 비어 있음",
                    BuildPassCounterSlotSummary(flow, slotItem),
                    BuildPassCounterDetailText(flow),
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

            return new PopupPanelContent(entries, BuildPassCounterDetailText(flow));
        }

        private PopupPanelContent BuildKitchenToolPopupContent()
        {
            BindKitchenFlow();
            List<PopupListEntry> entries = new();
            RestaurantFlowController flow = cachedKitchenFlow;
            if (flow == null)
            {
                return new PopupPanelContent(entries, "CookingUtensils 정보를 찾지 못했습니다.");
            }

            KitchenToolType toolType = flow.SelectedCookingToolType;
            KitchenStageRecipeData stage = flow.FindSelectedCookingStage(toolType);
            KitchenDishData dish = flow.FindSelectedCookingDish(toolType);
            bool canStartCooking = stage != null && flow.Carry.IsEmpty;

            entries.Add(new PopupListEntry(
                KitchenStartCookingKey,
                dish != null ? $"{dish.DisplayName} 조리 시작" : "조리 시작",
                canStartCooking
                    ? "선택한 재료로 조리를 시작합니다."
                    : flow.Carry.IsEmpty
                        ? "유효한 재료 조합이 필요합니다."
                        : "손이 비어 있어야 조리를 시작할 수 있습니다.",
                BuildCookingToolDetailText(flow, toolType, dish),
                dish != null && dish.FinalDishItem != null ? dish.FinalDishItem.Icon : null,
                selectedKitchenPopupKey == KitchenStartCookingKey,
                canStartCooking
                    ? () =>
                    {
                        selectedKitchenPopupKey = KitchenStartCookingKey;
                        if (flow.TryStartSelectedCooking(toolType, cachedPlayer != null ? cachedPlayer.gameObject : null))
                        {
                            activeHubPanel = HubPopupPanel.None;
                            ApplyMenuPanelState();
                            RefreshAll();
                            return;
                        }

                        RefreshHubPopupContent();
                    }
                    : null));

            entries.Add(new PopupListEntry(
                KitchenClearSelectionKey,
                "선택 재료 비우기",
                flow.SelectedToolIngredients.Count > 0 ? "현재 선택한 재료를 초기화합니다." : "아직 선택한 재료가 없습니다.",
                BuildCookingToolDetailText(flow, toolType, dish),
                null,
                selectedKitchenPopupKey == KitchenClearSelectionKey,
                flow.SelectedToolIngredients.Count > 0
                    ? () =>
                    {
                        selectedKitchenPopupKey = KitchenClearSelectionKey;
                        flow.ClearCookingSelection();
                        RefreshHubPopupContent();
                    }
                    : null));

            for (int index = 0; index < flow.SelectedToolIngredients.Count; index++)
            {
                int selectedIndex = index;
                ResourceData selectedResource = flow.SelectedToolIngredients[index];
                if (selectedResource == null)
                {
                    continue;
                }

                string key = BuildPopupEntryKey(KitchenSelectedIngredientKeyPrefix, index.ToString());
                entries.Add(new PopupListEntry(
                    key,
                    $"선택 재료 {index + 1}: {selectedResource.DisplayName}",
                    "클릭해 제거",
                    BuildCookingToolDetailText(flow, toolType, dish),
                    selectedResource.Icon,
                    selectedKitchenPopupKey == key,
                    () =>
                    {
                        selectedKitchenPopupKey = key;
                        flow.TryRemoveCookingIngredientAt(selectedIndex);
                        RefreshHubPopupContent();
                    },
                    selectedResource));
            }

            IReadOnlyList<ResourceData> availableIngredients = flow.GetSelectableIngredientsForTool(toolType);
            for (int index = 0; index < availableIngredients.Count; index++)
            {
                ResourceData resource = availableIngredients[index];
                if (resource == null)
                {
                    continue;
                }

                string key = BuildPopupEntryKey(KitchenIngredientKeyPrefix, resource.ResourceId);
                int selectedCount = flow.GetSelectedCookingIngredientCount(resource);
                int totalAmount = flow.GetAvailableCookingIngredientAmount(resource);
                bool isBasic = flow.IsBasicIngredient(resource);
                bool canAdd = isBasic || selectedCount < totalAmount;
                entries.Add(new PopupListEntry(
                    key,
                    resource.DisplayName,
                    BuildCookingIngredientSummary(selectedCount, totalAmount, isBasic, canAdd),
                    BuildCookingToolDetailText(flow, toolType, dish),
                    resource.Icon,
                    selectedKitchenPopupKey == key,
                    canAdd
                        ? () =>
                        {
                            selectedKitchenPopupKey = key;
                            flow.TryAddCookingIngredient(toolType, resource);
                            RefreshHubPopupContent();
                        }
                        : null,
                    resource,
                    isBasic ? 0 : totalAmount));
            }

            return new PopupPanelContent(entries, BuildCookingToolDetailText(flow, toolType, dish));
        }

        private PrototypeUIPopupDefinition BuildRuntimePopupDefinition()
        {
            return activeHubPanel switch
            {
                HubPopupPanel.Refrigerator => new PrototypeUIPopupDefinition("냉장고", "보유 재료", "재료 상세"),
                HubPopupPanel.FrontCounter => new PrototypeUIPopupDefinition("PassCounter", "보관 슬롯", "상세"),
                HubPopupPanel.KitchenTool => new PrototypeUIPopupDefinition(
                    GetKitchenToolPanelTitle(),
                    "재료 선택",
                    "조리 상태"),
                HubPopupPanel.Recipe => new PrototypeUIPopupDefinition("오늘의 메뉴", "메뉴 목록", "메뉴 상세"),
                HubPopupPanel.Upgrade => new PrototypeUIPopupDefinition("업그레이드", "업그레이드 목록", "업그레이드 상세"),
                HubPopupPanel.Materials => new PrototypeUIPopupDefinition("재료", "가방 재료", "재료 상세"),
                HubPopupPanel.Storage => new PrototypeUIPopupDefinition("창고", "보관 목록", "보관 상세"),
                _ => new PrototypeUIPopupDefinition(string.Empty, string.Empty, string.Empty)
            };
        }

        private string GetKitchenToolPanelTitle()
        {
            KitchenToolType toolType = cachedKitchenFlow != null ? cachedKitchenFlow.SelectedCookingToolType : KitchenToolType.FryingPan;
            return toolType switch
            {
                KitchenToolType.CuttingBoard => "CookingUtensils / 도마",
                KitchenToolType.Pot => "CookingUtensils / 냄비",
                KitchenToolType.FryingPan => "CookingUtensils / 후라이팬",
                KitchenToolType.Fryer => "CookingUtensils / 튀김기",
                _ => "CookingUtensils"
            };
        }

        private static string BuildRefrigeratorDetailText(RestaurantFlowController flow)
        {
            StringBuilder builder = new();
            builder.AppendLine("냉장고");
            builder.AppendLine("- 현재 조리에 사용할 수 있는 재료를 확인합니다.");
            builder.AppendLine("- 기본 재료는 무한 사용, 나머지는 탐험/보유 수량 기준입니다.");
            builder.Append("- 손: ");
            builder.Append(flow != null && flow.Carry.HeldItem != null ? flow.Carry.HeldItem.DisplayName : "비어 있음");
            return builder.ToString();
        }

        private static string BuildPassCounterDetailText(RestaurantFlowController flow)
        {
            StringBuilder builder = new();
            builder.AppendLine("PassCounter");
            builder.AppendLine("- 조리 완료품을 잠시 올려두고 다시 집을 수 있습니다.");
            builder.AppendLine("- 총 6칸을 사용합니다.");
            builder.Append("- 손: ");
            builder.Append(flow != null && flow.Carry.HeldItem != null ? flow.Carry.HeldItem.DisplayName : "비어 있음");
            return builder.ToString();
        }

        private static string BuildPassCounterSlotSummary(RestaurantFlowController flow, KitchenCarryItem slotItem)
        {
            if (slotItem != null)
            {
                return flow != null && flow.Carry.IsEmpty ? "클릭해 집기" : "이미 보관 중";
            }

            if (flow == null || flow.Carry.HeldItem == null)
            {
                return "비어 있음";
            }

            return flow.Carry.HeldItem.IsBundle ? "현재 허브 코어에서는 올릴 수 없음" : "클릭해 올리기";
        }

        private static string BuildCookingToolDetailText(RestaurantFlowController flow, KitchenToolType toolType, KitchenDishData dish)
        {
            StringBuilder builder = new();
            builder.AppendLine(toolType switch
            {
                KitchenToolType.CuttingBoard => "도마",
                KitchenToolType.Pot => "냄비",
                KitchenToolType.FryingPan => "후라이팬",
                KitchenToolType.Fryer => "튀김기",
                _ => "CookingUtensils"
            });
            builder.AppendLine("- 재료를 선택한 뒤 조리를 시작합니다.");
            builder.AppendLine("- 잘못된 조합이면 아무 일도 일어나지 않습니다.");
            builder.Append("- 현재 조합: ");

            if (flow == null || flow.SelectedToolIngredients.Count == 0)
            {
                builder.AppendLine("없음");
            }
            else
            {
                for (int index = 0; index < flow.SelectedToolIngredients.Count; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(", ");
                    }

                    ResourceData resource = flow.SelectedToolIngredients[index];
                    builder.Append(resource != null ? resource.DisplayName : "알 수 없음");
                }

                builder.AppendLine();
            }

            builder.Append("- 조리 결과: ");
            builder.AppendLine(dish != null ? dish.DisplayName : "유효한 조합 없음");
            builder.Append("- 손: ");
            builder.Append(flow != null && flow.Carry.HeldItem != null ? flow.Carry.HeldItem.DisplayName : "비어 있음");
            return builder.ToString();
        }

        private static string BuildCookingIngredientSummary(int selectedCount, int totalAmount, bool isBasic, bool canAdd)
        {
            if (isBasic)
            {
                return selectedCount > 0 ? $"기본 재료 / 선택 {selectedCount}" : "기본 재료";
            }

            if (canAdd)
            {
                return selectedCount > 0
                    ? $"보유 {totalAmount} / 선택 {selectedCount}"
                    : $"보유 {totalAmount}";
            }

            return $"보유 {totalAmount} / 더 선택할 수 없음";
        }

        private static string BuildPopupEntryKey(string prefix, string id)
        {
            return $"{prefix}{id ?? string.Empty}";
        }
    }
}
