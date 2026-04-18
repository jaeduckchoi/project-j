using System.Collections.Generic;
using System.Text;
using CoreLoop.Core;
using Exploration.Interaction;
using Exploration.Player;
using Management.Inventory;
using Management.Storage;
using Management.Tools;
using Management.Upgrade;
using Shared.Data;
using TMPro;
using UI.Layout;
using UI.Style;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public partial class UIManager
    {
        private const string UpgradeToolKeyPrefix = "tool:";
        private const string UpgradeInventoryKeyPrefix = "inventory:";

        // 동적 텍스트, 상세 설명, 팝업 리스트 구성은 Refresh partial에서만 조립합니다.
        private void RefreshHubPopupContent()
        {
            if (!IsHubScene())
            {
                return;
            }

            if (ShouldUseTypedPopupUi())
            {
                RefreshTypedHubPopupContent();
                return;
            }

            PopupPanelContent popupContent = BuildCurrentHubPopupContent();

            if (inventoryText != null)
            {
                inventoryText.text = string.Empty;
            }

            if (selectedRecipeText != null)
            {
                selectedRecipeText.text = popupContent.DetailText;
            }

            RefreshPopupBodyItemBoxes(popupContent.Entries);
            RefreshRefrigeratorPopupSlots(activeHubPanel == HubPopupPanel.Refrigerator ? popupContent.Entries : null);
        }

        private PopupPanelContent BuildCurrentHubPopupContent()
        {
            if (ShouldUseTypedPopupUi())
            {
                return activeHubPanel == HubPopupPanel.Refrigerator
                    ? BuildRefrigeratorPopupContent()
                    : new PopupPanelContent(new List<PopupListEntry>(), string.Empty);
            }

            return activeHubPanel switch
            {
                HubPopupPanel.Storage => BuildStoragePopupContent(),
                HubPopupPanel.Refrigerator => BuildRefrigeratorPopupContent(),
                HubPopupPanel.FrontCounter => BuildFrontCounterPopupContent(),
                HubPopupPanel.Recipe => BuildRecipePopupContent(),
                HubPopupPanel.Upgrade => BuildUpgradePopupContent(),
                HubPopupPanel.Materials => BuildMaterialPopupContent(),
                _ => new PopupPanelContent(new List<PopupListEntry>(), string.Empty)
            };
        }

        private PopupPanelContent BuildRecipePopupContent()
        {
            List<PopupListEntry> entries = new();
            if (cachedRestaurant == null)
            {
                return new PopupPanelContent(entries, "식당 정보를 찾지 못했습니다.");
            }

            IReadOnlyList<RecipeData> recipes = cachedRestaurant.AvailableRecipes;
            IReadOnlyList<RecipeData> todayMenuRecipes = cachedRestaurant.TodayMenuRecipes;
            RecipeData detailRecipe = cachedRestaurant.SelectedRecipe;
            if (detailRecipe == null)
            {
                for (int i = 0; i < todayMenuRecipes.Count; i++)
                {
                    if (todayMenuRecipes[i] != null)
                    {
                        detailRecipe = todayMenuRecipes[i];
                        break;
                    }
                }
            }

            if (detailRecipe == null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i] != null)
                    {
                        detailRecipe = recipes[i];
                        break;
                    }
                }
            }

            for (int i = 0; i < Restaurant.TodayMenuState.SlotCount; i++)
            {
                RecipeData assignedRecipe = i < todayMenuRecipes.Count ? todayMenuRecipes[i] : null;
                int slotIndex = i;
                entries.Add(new PopupListEntry(
                    $"today-menu-slot-{i}",
                    $"오늘의 메뉴 {i + 1}",
                    BuildPopupItemSummary(
                        assignedRecipe != null ? assignedRecipe.DisplayName : "비어 있음",
                        cachedRestaurant.IsRestaurantOpen ? "OPEN 중에는 수정 불가" : "클릭해 활성 슬롯 선택"),
                    BuildRecipePopupDetailText(detailRecipe),
                    ResolveRecipePopupIcon(assignedRecipe),
                    cachedRestaurant.SelectedTodayMenuSlotIndex == i,
                    cachedRestaurant.IsRestaurantOpen
                        ? null
                        : () =>
                        {
                            if (cachedRestaurant != null)
                            {
                                cachedRestaurant.SelectTodayMenuSlot(slotIndex);
                            }

                            RefreshHubPopupContent();
                        }));
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeData recipe = recipes[i];
                if (recipe == null)
                {
                    continue;
                }

                int recipeIndex = i;
                int assignedSlotIndex = FindTodayMenuSlotIndex(recipe);
                entries.Add(new PopupListEntry(
                    recipe.RecipeId,
                    recipe.DisplayName,
                    BuildPopupItemSummary(
                        recipe.Description,
                        assignedSlotIndex >= 0
                            ? $"오늘의 메뉴 {assignedSlotIndex + 1} 배치됨"
                            : $"판매가 {recipe.SellPrice} · 가능 {cachedRestaurant.GetCookableServings(recipe)}"),
                    BuildRecipePopupDetailText(recipe),
                    ResolveRecipePopupIcon(recipe),
                    recipe == detailRecipe,
                    () =>
                    {
                        if (cachedRestaurant == null)
                        {
                            return;
                        }

                        if (cachedRestaurant.IsRestaurantOpen)
                        {
                            cachedRestaurant.SelectRecipeByIndex(recipeIndex);
                        }
                        else
                        {
                            cachedRestaurant.AssignSelectedTodayMenuRecipe(recipe);
                        }

                        RefreshHubPopupContent();
                    }));
            }

            return new PopupPanelContent(entries, BuildRecipePopupDetailText(detailRecipe));
        }

        private PopupPanelContent BuildMaterialPopupContent()
        {
            List<PopupListEntry> entries = new();
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null)
            {
                return new PopupPanelContent(entries, "가방 정보를 찾지 못했습니다.");
            }

            InventoryEntry detailEntry = null;
            foreach (InventoryEntry entry in inventory.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                if (detailEntry == null || entry.resource == selectedMaterialPopupResource)
                {
                    detailEntry = entry;
                }
            }

            foreach (InventoryEntry entry in inventory.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                ResourceData resource = entry.resource;
                int amount = entry.amount;
                entries.Add(new PopupListEntry(
                    resource.ResourceId,
                    $"{resource.DisplayName} x{amount}",
                    BuildPopupItemSummary(resource.Description, $"{resource.RegionTag} · {GetRarityLabel(resource.Rarity)}"),
                    BuildMaterialPopupDetailText(resource, amount),
                    resource.Icon,
                    detailEntry != null && resource == detailEntry.resource,
                    () =>
                    {
                        selectedMaterialPopupResource = resource;
                        RefreshHubPopupContent();
                    }));
            }

            return new PopupPanelContent(entries, BuildMaterialPopupDetailText(detailEntry != null ? detailEntry.resource : null, detailEntry != null ? detailEntry.amount : 0));
        }

        private PopupPanelContent BuildStoragePopupContent()
        {
            List<PopupListEntry> entries = new();
            if (cachedStorage == null)
            {
                return new PopupPanelContent(entries, "창고 정보를 찾지 못했습니다.");
            }

            cachedStorage.InitializeIfNeeded();

            InventoryEntry detailEntry = null;
            foreach (InventoryEntry entry in cachedStorage.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                if (detailEntry == null || entry.resource == selectedStoragePopupResource)
                {
                    detailEntry = entry;
                }
            }

            foreach (InventoryEntry entry in cachedStorage.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                ResourceData resource = entry.resource;
                int amount = entry.amount;
                entries.Add(new PopupListEntry(
                    resource.ResourceId,
                    $"{resource.DisplayName} x{amount}",
                    BuildPopupItemSummary(resource.Description, "보관 중인 재료"),
                    BuildStoragePopupDetailText(entry),
                    resource.Icon,
                    detailEntry != null && resource == detailEntry.resource,
                    () =>
                    {
                        selectedStoragePopupResource = resource;
                        RefreshHubPopupContent();
                    }));
            }

            return new PopupPanelContent(entries, BuildStoragePopupDetailText(detailEntry));
        }

        private PopupPanelContent BuildUpgradePopupContent()
        {
            List<PopupListEntry> rawEntries = new();
            if (cachedUpgradeManager == null)
            {
                return new PopupPanelContent(new List<PopupListEntry>(), "업그레이드 정보를 찾지 못했습니다.");
            }

            cachedUpgradeManager.InitializeIfNeeded();

            foreach (ToolUnlockCost cost in cachedUpgradeManager.ToolUnlockCosts)
            {
                if (cost == null || cost.toolType == ToolType.None)
                {
                    continue;
                }

                string key = BuildPopupEntryKey(UpgradeToolKeyPrefix, cost.toolType.ToString());
                ToolType toolType = cost.toolType;
                rawEntries.Add(new PopupListEntry(
                    key,
                    $"{toolType.GetDisplayName()} 해금",
                    BuildPopupItemSummary(cost.description, BuildUpgradeAvailabilityLabel(toolType)),
                    BuildToolUnlockPopupDetailText(cost),
                    ResolveUpgradePopupIcon(cost.requiredResource),
                    false,
                    () =>
                    {
                        selectedUpgradePopupKey = key;
                        RefreshHubPopupContent();
                    }));
            }

            IReadOnlyList<InventoryUpgradeCost> inventoryUpgradeCosts = cachedUpgradeManager.InventoryUpgradeCosts;
            for (int i = 0; i < inventoryUpgradeCosts.Count; i++)
            {
                InventoryUpgradeCost cost = inventoryUpgradeCosts[i];
                if (cost == null)
                {
                    continue;
                }

                string key = BuildPopupEntryKey(UpgradeInventoryKeyPrefix, i.ToString());
                int upgradeIndex = i;
                rawEntries.Add(new PopupListEntry(
                    key,
                    BuildInventoryUpgradeTitle(upgradeIndex),
                    BuildPopupItemSummary(cost.description, BuildInventoryUpgradeAvailabilityLabel(upgradeIndex)),
                    BuildInventoryUpgradePopupDetailText(upgradeIndex, cost),
                    ResolveUpgradePopupIcon(cost.requiredResource),
                    false,
                    () =>
                    {
                        selectedUpgradePopupKey = key;
                        RefreshHubPopupContent();
                    }));
            }

            if (rawEntries.Count == 0)
            {
                return new PopupPanelContent(new List<PopupListEntry>(), "남아 있는 업그레이드가 없습니다.");
            }

            string preferredKey = ResolvePreferredUpgradePopupKey();
            string selectedKey = ContainsPopupEntryKey(rawEntries, selectedUpgradePopupKey)
                ? selectedUpgradePopupKey
                : ContainsPopupEntryKey(rawEntries, preferredKey)
                    ? preferredKey
                    : rawEntries[0].Key;

            List<PopupListEntry> entries = new(rawEntries.Count);
            string detailText = rawEntries[0].Detail;
            foreach (PopupListEntry entry in rawEntries)
            {
                bool isSelected = entry.Key == selectedKey;
                entries.Add(new PopupListEntry(entry.Key, entry.Title, entry.Summary, entry.Detail, entry.Icon, isSelected, entry.OnSelected));
                if (isSelected)
                {
                    detailText = entry.Detail;
                }
            }

            return new PopupPanelContent(entries, detailText);
        }

        private static bool ContainsPopupEntryKey(List<PopupListEntry> entries, string key)
        {
            if (entries == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            foreach (PopupListEntry entry in entries)
            {
                if (entry != null && entry.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        private string ResolvePreferredUpgradePopupKey()
        {
            if (cachedUpgradeManager == null)
            {
                return string.Empty;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;

            return cachedUpgradeManager.GetPreferredAction() switch
            {
                UpgradeWorkbenchAction.UnlockTool when cachedUpgradeManager.GetPreferredToolType() != ToolType.None
                    => BuildPopupEntryKey(UpgradeToolKeyPrefix, cachedUpgradeManager.GetPreferredToolType().ToString()),
                UpgradeWorkbenchAction.UpgradeInventory when inventory != null
                    => BuildPopupEntryKey(UpgradeInventoryKeyPrefix, inventory.CapacityLevel.ToString()),
                _ => string.Empty
            };
        }

        private static string BuildPopupItemSummary(string description, string fallback)
        {
            string source = string.IsNullOrWhiteSpace(description) ? fallback : description;
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            string normalized = source.Replace("\r\n", " ").Replace('\n', ' ').Trim();
            return normalized.Length > 32 ? $"{normalized.Substring(0, 29)}..." : normalized;
        }

        private Sprite ResolveRecipePopupIcon(RecipeData recipe)
        {
            if (recipe == null)
            {
                return null;
            }

            if (recipe.Icon != null)
            {
                return recipe.Icon;
            }

            Sprite recipeSprite = LoadRecipeSpriteById(recipe.RecipeId);
            if (recipeSprite != null)
            {
                return recipeSprite;
            }

            if (recipe.Ingredients == null)
            {
                return null;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out _)
                    && resource.Icon != null)
                {
                    return resource.Icon;
                }
            }

            return null;
        }

        private static Sprite LoadRecipeSpriteById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return null;
            }

            string normalizedRecipeId = recipeId.Trim();
            return Resources.Load<Sprite>($"Generated/Sprites/Item/Food/{normalizedRecipeId}")
                ?? Resources.Load<Sprite>($"Generated/Sprites/Recipes/{normalizedRecipeId}")
                ?? Resources.Load<Sprite>($"Generated/Sprites/Hub/{normalizedRecipeId}");
        }

        private static Sprite ResolveUpgradePopupIcon(ResourceData requiredResource)
        {
            return requiredResource != null ? requiredResource.Icon : null;
        }

        private static string GetRarityLabel(ResourceRarity rarity)
        {
            return rarity switch
            {
                ResourceRarity.Uncommon => "고급",
                ResourceRarity.Rare => "희귀",
                ResourceRarity.Epic => "특급",
                _ => "일반"
            };
        }

        private string BuildRecipePopupDetailText(RecipeData recipe)
        {
            if (cachedRestaurant == null)
            {
                return "선택된 메뉴가 없습니다.";
            }

            StringBuilder builder = new();
            builder.AppendLine(cachedRestaurant.IsRestaurantOpen ? "영업 상태: OPEN" : "영업 상태: CLOSE");
            builder.AppendLine("- 오늘의 메뉴");

            IReadOnlyList<RecipeData> todayMenuRecipes = cachedRestaurant.TodayMenuRecipes;
            for (int slotIndex = 0; slotIndex < Restaurant.TodayMenuState.SlotCount; slotIndex++)
            {
                RecipeData assignedRecipe = slotIndex < todayMenuRecipes.Count ? todayMenuRecipes[slotIndex] : null;
                string marker = cachedRestaurant.SelectedTodayMenuSlotIndex == slotIndex ? "[활성] " : string.Empty;
                builder.AppendLine($"  {marker}{slotIndex + 1}. {(assignedRecipe != null ? assignedRecipe.DisplayName : "비어 있음")}");
            }

            builder.AppendLine();

            if (recipe == null)
            {
                builder.Append(cachedRestaurant.IsRestaurantOpen
                    ? "영업 중에는 오늘의 메뉴를 읽기 전용으로 확인합니다."
                    : "왼쪽 슬롯을 고른 뒤 아래 레시피를 눌러 배치하세요.");
                return builder.ToString().TrimEnd();
            }

            builder.AppendLine(recipe.DisplayName);

            if (!string.IsNullOrWhiteSpace(recipe.Description))
            {
                builder.AppendLine(recipe.Description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 판매가: {recipe.SellPrice}");
            if (!string.IsNullOrWhiteSpace(recipe.SupplySource))
            {
                builder.AppendLine($"- 공급처: {recipe.SupplySource}");
            }

            if (recipe.Difficulty > 0)
            {
                builder.AppendLine($"- 난이도: {recipe.Difficulty}");
            }

            if (!string.IsNullOrWhiteSpace(recipe.CookingMethod))
            {
                builder.AppendLine($"- 조리법: {recipe.CookingMethod}");
            }

            builder.AppendLine($"- 가능 수량: {cachedRestaurant.GetCookableServings(recipe)}");
            int assignedSlotIndex = FindTodayMenuSlotIndex(recipe);
            if (assignedSlotIndex >= 0)
            {
                builder.AppendLine($"- 배치 슬롯: 오늘의 메뉴 {assignedSlotIndex + 1}");
            }

            builder.AppendLine();
            builder.AppendLine("- 필요 재료");

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                {
                    continue;
                }

                string displayName = ingredient.BuildDisplayNameWithCatalogSummary();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                if (!RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out int ingredientAmount))
                {
                    builder.AppendLine($"  {displayName} x{ingredient.Quantity}");
                    continue;
                }

                int ownedAmount = GameManager.Instance != null && GameManager.Instance.Inventory != null
                    ? GameManager.Instance.Inventory.GetAmount(resource)
                    : 0;
                builder.AppendLine($"  {displayName} {ownedAmount}/{ingredientAmount}");
            }

            builder.AppendLine();
            builder.AppendLine("- 배치 안내");
            if (cachedRestaurant.IsRestaurantOpen)
            {
                builder.Append("  영업 중에는 메뉴를 수정할 수 없습니다.");
            }
            else
            {
                builder.AppendLine($"  현재 활성 슬롯: 오늘의 메뉴 {cachedRestaurant.SelectedTodayMenuSlotIndex + 1}");
                builder.Append("  이미 다른 슬롯에 있는 메뉴를 고르면 중복되지 않고 현재 슬롯으로 이동합니다.");
            }

            return builder.ToString().TrimEnd();
        }

        private int FindTodayMenuSlotIndex(RecipeData recipe)
        {
            if (cachedRestaurant == null || recipe == null || string.IsNullOrWhiteSpace(recipe.RecipeId))
            {
                return -1;
            }

            IReadOnlyList<RecipeData> todayMenuRecipes = cachedRestaurant.TodayMenuRecipes;
            for (int slotIndex = 0; slotIndex < todayMenuRecipes.Count; slotIndex++)
            {
                RecipeData assignedRecipe = todayMenuRecipes[slotIndex];
                if (assignedRecipe == null)
                {
                    continue;
                }

                if (assignedRecipe == recipe
                    || string.Equals(assignedRecipe.RecipeId, recipe.RecipeId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return slotIndex;
                }
            }

            return -1;
        }

        private string BuildMaterialPopupDetailText(ResourceData resource, int amount)
        {
            if (resource == null)
            {
                return "선택된 재료가 없습니다.";
            }

            StringBuilder builder = new();
            builder.AppendLine(resource.DisplayName);

            if (!string.IsNullOrWhiteSpace(resource.Description))
            {
                builder.AppendLine(resource.Description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 보유 수량: x{amount}");
            builder.AppendLine($"- 채집 지역: {resource.RegionTag}");
            builder.AppendLine($"- 희귀도: {GetRarityLabel(resource.Rarity)}");
            builder.AppendLine($"- 기본 판매가: {resource.BaseSellPrice}");

            if (cachedRestaurant != null && cachedRestaurant.SelectedRecipe != null)
            {
                RecipeData recipe = cachedRestaurant.SelectedRecipe;
                int requiredAmount = GetRecipeIngredientAmount(recipe, resource);
                builder.AppendLine();
                builder.AppendLine($"- 선택 메뉴: {recipe.DisplayName}");
                builder.AppendLine(requiredAmount > 0
                    ? $"- 메뉴 필요 수량: {requiredAmount}"
                    : "- 메뉴 사용처: 현재 선택 메뉴에는 들어가지 않음");
                builder.AppendLine($"- 현재 가능 수량: {cachedRestaurant.GetCookableServings(recipe)}");
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildToolUnlockPopupDetailText(ToolUnlockCost cost)
        {
            if (cost == null || cost.toolType == ToolType.None || cachedUpgradeManager == null)
            {
                return "업그레이드 정보를 찾지 못했습니다.";
            }

            bool isUnlocked = cachedUpgradeManager.IsToolUnlocked(cost.toolType);
            bool canUnlock = !isUnlocked && cachedUpgradeManager.CanUnlockTool(cost.toolType);
            int ownedAmount = cost.requiredResource != null && GameManager.Instance != null && GameManager.Instance.Inventory != null
                ? GameManager.Instance.Inventory.GetAmount(cost.requiredResource)
                : 0;

            StringBuilder builder = new();
            builder.AppendLine($"{cost.toolType.GetDisplayName()} 해금");

            if (!string.IsNullOrWhiteSpace(cost.description))
            {
                builder.AppendLine(cost.description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 상태: {BuildUpgradeAvailabilityLabel(cost.toolType)}");
            builder.AppendLine($"- 비용: {BuildUpgradeCostText(cost.goldCost, cost.requiredResource, cost.requiredAmount)}");
            if (cost.requiredResource != null && cost.requiredAmount > 0)
            {
                builder.AppendLine($"- 보유 재료: {ownedAmount}/{cost.requiredAmount}");
            }

            builder.AppendLine($"- 활용: {BuildToolUnlockUseDescription(cost.toolType)}");
            if ((isUnlocked || canUnlock) && !string.IsNullOrWhiteSpace(cachedUpgradeManager.LastUpgradeMessage))
            {
                builder.AppendLine();
                builder.AppendLine(cachedUpgradeManager.LastUpgradeMessage);
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildInventoryUpgradePopupDetailText(int index, InventoryUpgradeCost cost)
        {
            if (cost == null)
            {
                return "업그레이드 정보를 찾지 못했습니다.";
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            int currentSlots = inventory != null ? inventory.GetSlotCapacityForLevel(index) : 0;
            int nextSlots = inventory != null ? inventory.GetSlotCapacityForLevel(index + 1) : 0;
            int ownedAmount = cost.requiredResource != null && inventory != null
                ? inventory.GetAmount(cost.requiredResource)
                : 0;

            StringBuilder builder = new();
            builder.AppendLine(BuildInventoryUpgradeTitle(index));

            if (!string.IsNullOrWhiteSpace(cost.description))
            {
                builder.AppendLine(cost.description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 상태: {BuildInventoryUpgradeAvailabilityLabel(index)}");
            builder.AppendLine($"- 확장: {currentSlots}칸 -> {nextSlots}칸");
            builder.AppendLine($"- 비용: {BuildUpgradeCostText(cost.goldCost, cost.requiredResource, cost.requiredAmount)}");
            if (cost.requiredResource != null && cost.requiredAmount > 0)
            {
                builder.AppendLine($"- 보유 재료: {ownedAmount}/{cost.requiredAmount}");
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildInventoryUpgradeTitle(int index)
        {
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null)
            {
                return $"가방 확장 단계 {index + 1}";
            }

            return $"가방 확장 {inventory.GetSlotCapacityForLevel(index)}칸 -> {inventory.GetSlotCapacityForLevel(index + 1)}칸";
        }

        private string BuildInventoryUpgradeAvailabilityLabel(int index)
        {
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null || cachedUpgradeManager == null)
            {
                return "정보 없음";
            }

            if (inventory.CapacityLevel > index)
            {
                return "완료";
            }

            if (inventory.CapacityLevel == index)
            {
                return cachedUpgradeManager.CanUpgradeInventory() ? "지금 진행 가능" : "재료 준비 필요";
            }

            return "이전 단계 필요";
        }

        private string BuildUpgradeAvailabilityLabel(ToolType toolType)
        {
            if (cachedUpgradeManager == null || toolType == ToolType.None)
            {
                return "정보 없음";
            }

            if (cachedUpgradeManager.IsToolUnlocked(toolType))
            {
                return "완료";
            }

            return cachedUpgradeManager.CanUnlockTool(toolType) ? "지금 진행 가능" : "재료 준비 필요";
        }

        private static string BuildToolUnlockUseDescription(ToolType toolType)
        {
            return toolType switch
            {
                ToolType.Lantern => "폐광산처럼 어두운 지역 진입에 필요",
                ToolType.Sickle => "풀숲과 약초 채집 범위를 넓힘",
                ToolType.FishingRod => "바닷가 채집 효율을 보강",
                ToolType.Rake => "얕은 채집 지점 정리에 사용",
                _ => "새 지역이나 상호작용 해금에 사용"
            };
        }

        private static string BuildUpgradeCostText(int goldCost, ResourceData requiredResource, int requiredAmount)
        {
            List<string> parts = new();
            if (goldCost > 0)
            {
                parts.Add($"골드 {goldCost}");
            }

            if (requiredResource != null && requiredAmount > 0)
            {
                parts.Add($"{requiredResource.DisplayName} x{requiredAmount}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "비용 없음";
        }

        private string BuildStoragePopupDetailText(InventoryEntry entry)
        {
            if (cachedStorage == null)
            {
                return "창고 정보를 찾지 못했습니다.";
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            InventoryEntry depositEntry = cachedStorage.GetSelectedInventoryEntry(inventory);
            InventoryEntry withdrawEntry = cachedStorage.GetSelectedStoredEntry();
            depositEntry = depositEntry != null && depositEntry.resource != null ? depositEntry : null;
            withdrawEntry = withdrawEntry != null && withdrawEntry.resource != null ? withdrawEntry : null;

            StringBuilder builder = new();
            if (entry != null && entry.resource != null)
            {
                builder.AppendLine(entry.resource.DisplayName);

                if (!string.IsNullOrWhiteSpace(entry.resource.Description))
                {
                    builder.AppendLine(entry.resource.Description);
                    builder.AppendLine();
                }

                builder.AppendLine($"- 보관 수량: x{entry.amount}");
                builder.AppendLine($"- 원산지: {entry.resource.RegionTag}");
                builder.AppendLine($"- 기본 판매가: {entry.resource.BaseSellPrice}");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine("선택된 보관 재료가 없습니다.");
                builder.AppendLine();
            }

            builder.AppendLine(depositEntry != null
                ? $"맡길 재료: {depositEntry.resource.DisplayName} x{depositEntry.amount}"
                : "맡길 재료: 없음");
            builder.AppendLine(withdrawEntry != null
                ? $"꺼낼 재료: {withdrawEntry.resource.DisplayName} x{withdrawEntry.amount}"
                : "꺼낼 재료: 없음");
            builder.AppendLine();
            builder.AppendLine("Q 품목 변경");
            builder.AppendLine("W 맡기기");
            builder.AppendLine("A 꺼낼 재료 변경");
            builder.AppendLine("S 꺼내기");

            if (!string.IsNullOrWhiteSpace(cachedStorage.LastOperationMessage))
            {
                builder.AppendLine();
                builder.AppendLine(cachedStorage.LastOperationMessage);
            }

            return builder.ToString().TrimEnd();
        }

        private static int GetRecipeIngredientAmount(RecipeData recipe, ResourceData resource)
        {
            if (recipe == null || resource == null || recipe.Ingredients == null)
            {
                return 0;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (RecipeIngredient.TryResolve(ingredient, out ResourceData ingredientResource, out int ingredientAmount)
                    && ingredientResource == resource)
                {
                    return ingredientAmount;
                }
            }

            return 0;
        }

        private string BuildHubMessageLine()
        {
            if (!IsHubScene() || activeHubPanel != HubPopupPanel.None)
            {
                return string.Empty;
            }

            string message = cachedDayCycle != null ? cachedDayCycle.CurrentGuideText : string.Empty;

            if (cachedRestaurant != null
                && !string.IsNullOrWhiteSpace(cachedRestaurant.LastServiceResult))
            {
                message = cachedRestaurant.LastServiceResult;
            }

            return CompactToSingleLine(message);
        }

        private static string CompactToSingleLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string compact = text.Replace("\r", " ").Replace("\n", " / ");
            while (compact.Contains("  "))
            {
                compact = compact.Replace("  ", " ");
            }

            return compact.Trim();
        }

        private void RefreshStoragePanelVisibility()
        {
            if (!IsHubScene())
            {
                if (storageText != null)
                {
                    storageText.gameObject.SetActive(false);
                }

                return;
            }

            if (ShouldUseTypedPopupUi())
            {
                RefreshHubPopupOverlay();
                return;
            }

            if (!Application.isPlaying)
            {
                RefreshHubPopupOverlay();
                return;
            }

            if (activeHubPanel == HubPopupPanel.Storage && !IsPlayerNearStorageStation())
            {
                activeHubPanel = HubPopupPanel.None;
                ApplyMenuPanelState();
                return;
            }

            RefreshHubPopupOverlay();
        }

        private void ApplyMenuPanelState()
        {
            if (ShouldUseTypedPopupUi())
            {
                ApplyTypedPopupState();
                SetHubHudVisible(activeHubPanel == HubPopupPanel.None);
                RefreshHubPopupOverlay();
                RefreshGuideText();
                RefreshButtonStates();
                RefreshHubPopupContent();
                return;
            }

            bool isHubScene = IsHubScene();

            if (isHubScene)
            {
                PrototypeUITheme theme = PrototypeUIThemePalette.GetForScene(SceneManager.GetActiveScene().name);
                TMP_FontAsset headingFont = headingFontAsset ?? bodyFontAsset ?? TMP_Settings.defaultFontAsset;
                TMP_FontAsset bodyFont = bodyFontAsset ?? headingFont;
                bool showPopup = activeHubPanel != HubPopupPanel.None;
                bool showRefrigeratorPopup = showPopup && activeHubPanel == HubPopupPanel.Refrigerator;

                ApplyHubPopupFrameStyle(headingFont, theme.Text);
                if (showRefrigeratorPopup)
                {
                    EnsureRefrigeratorPopupChrome(bodyFont, headingFont, theme.Text);
                }

                SetHubPopupDesignActive(showPopup);
                SetRefrigeratorPopupDesignActive(showRefrigeratorPopup);
                SetHubHudVisible(!showPopup);
                SetLegacyHubPopupObjectsActive(false);

                RefreshHubPopupContent();

                if (inventoryText != null)
                {
                    inventoryText.gameObject.SetActive(false);
                }

                if (selectedRecipeText != null)
                {
                    selectedRecipeText.gameObject.SetActive(showPopup && activeHubPanel != HubPopupPanel.Refrigerator);
                }

                if (storageText != null)
                {
                    storageText.gameObject.SetActive(false);
                }

                if (upgradeText != null)
                {
                    upgradeText.gameObject.SetActive(false);
                }
            }
            else
            {
                SetHubPopupDesignActive(false);
                SetLegacyHubPopupObjectsActive(false);

                if (inventoryText != null)
                {
                    inventoryText.gameObject.SetActive(false);
                }

                if (storageText != null)
                {
                    storageText.gameObject.SetActive(false);
                }

                if (selectedRecipeText != null)
                {
                    selectedRecipeText.gameObject.SetActive(false);
                }

                if (upgradeText != null)
                {
                    upgradeText.gameObject.SetActive(false);
                }
            }

            RefreshHubPopupOverlay();
            RefreshGuideText();
            RefreshButtonStates();
        }

        private void RefreshHubPopupOverlay()
        {
            bool shouldShowOverlay = IsHubScene() && activeHubPanel != HubPopupPanel.None;
            if (ShouldUseTypedPopupUi())
            {
                if (popupFrameUi != null && popupFrameUi.OverlayRoot != null)
                {
                    popupFrameUi.OverlayRoot.SetActive(shouldShowOverlay);
                }
            }
            else
            {
                SetNamedObjectActive("PopupOverlay", shouldShowOverlay);
            }

            ApplyPopupPauseState(shouldShowOverlay);
        }

        private bool IsPlayerNearStorageStation()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }

            InteractionDetector detector = cachedPlayer != null ? cachedPlayer.InteractionDetector : null;
            return detector != null && detector.CurrentInteractable is StorageStation;
        }

        /// <summary>
        /// 허브 팝업이 열려 있는 동안에는 시간을 멈춰 배경 진행을 막습니다.
        /// </summary>
        private void ApplyPopupPauseState(bool shouldPause)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            PopupPauseStateUtility.Snapshot snapshot = PopupPauseStateUtility.Apply(
                shouldPause,
                isPopupPauseApplied,
                popupPausePreviousTimeScale,
                Time.timeScale);
            popupPausePreviousTimeScale = snapshot.PreviousTimeScale;
            Time.timeScale = snapshot.NextTimeScale;
            isPopupPauseApplied = snapshot.IsPauseApplied;
        }

        private void RestorePopupPauseIfNeeded()
        {
            PopupPauseStateUtility.Snapshot snapshot = PopupPauseStateUtility.Restore(
                isPopupPauseApplied,
                popupPausePreviousTimeScale,
                Time.timeScale);
            popupPausePreviousTimeScale = snapshot.PreviousTimeScale;
            Time.timeScale = snapshot.NextTimeScale;
            isPopupPauseApplied = snapshot.IsPauseApplied;
        }

        private static bool IsHubScene()
        {
            return SceneManager.GetActiveScene().name == "Hub";
        }

        private void ApplyNamedRectLayout(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            RectTransform rect = target != null ? target.GetComponent<RectTransform>() : null;
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));
            ApplyManagedRectLayout(rect, resolvedLayout, preserveExistingLayout: target != null);
        }

        private void ApplyNamedRectLayout(string objectName, PrototypeUIRect layout)
        {
            ApplyNamedRectLayout(
                objectName,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta);
        }

        private void SetNamedObjectActive(string objectName, bool isActive)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            if (target != null)
            {
                target.gameObject.SetActive(isActive);
            }
        }

        private void SetNamedObjectActiveRaw(string objectName, bool isActive)
        {
            if (transform == null || string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            if (target != null)
            {
                target.gameObject.SetActive(isActive);
            }
        }

        private void HideLegacyDayRoutineObjects()
        {
            SetNamedObjectActiveRaw("PhaseBadge", false);
            SetNamedObjectActiveRaw("DayPhaseText", false);
            SetNamedObjectActiveRaw("SkipExplorationButton", false);
            SetNamedObjectActiveRaw("SkipServiceButton", false);
            SetNamedObjectActiveRaw("NextDayButton", false);
        }

        /// <summary>
        /// 현재 씬의 모든 HUD 텍스트와 카드 표시 상태를 한 번에 다시 맞춥니다.
        /// </summary>
        private void RefreshAll()
        {
            // 개별 이벤트가 누락돼도 전체 재계산 한 번으로 화면 상태를 다시 복구합니다.
            RefreshInventoryText();
            RefreshStorageText();
            RefreshUpgradeText();
            RefreshEconomyText();
            RefreshInteractionPrompt();
            RefreshSelectedRecipeText(cachedRestaurant != null ? cachedRestaurant.SelectedRecipe : null);
            RefreshRestaurantResultText(cachedRestaurant != null ? cachedRestaurant.LastServiceResult : string.Empty);
            RefreshDayCycleState();

            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
        }

        /// <summary>
        /// 현재 플레이어가 상호작용할 수 있는 대상의 프롬프트를 하단에 표시합니다.
        /// </summary>
        private void RefreshInteractionPrompt()
        {
            if (interactionPromptText == null)
            {
                return;
            }

            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }

            string prompt = string.Empty;
            InteractionDetector detector = cachedPlayer != null ? cachedPlayer.InteractionDetector : null;

            if (detector != null && detector.CurrentInteractable != null)
            {
                prompt = detector.CurrentInteractable.InteractionPrompt;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                prompt = defaultPromptText;
            }

            bool shouldShowPrompt = activeHubPanel == HubPopupPanel.None;

            interactionPromptText.text = prompt;
            interactionPromptText.gameObject.SetActive(shouldShowPrompt && !string.IsNullOrWhiteSpace(prompt));
            SetNamedObjectActive("InteractionPromptBackdrop", shouldShowPrompt && !string.IsNullOrWhiteSpace(prompt));
            RefreshStoragePanelVisibility();
        }

        /// <summary>
        /// 현재 보유 재료와 가방 사용량을 카드 본문용 문자열로 정리합니다.
        /// </summary>
        private void RefreshInventoryText()
        {
            if (inventoryText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.Inventory == null)
            {
                inventoryText.text = "인벤토리 없음";
                RefreshHubPopupContent();
                return;
            }

            IReadOnlyList<InventoryEntry> entries = GameManager.Instance.Inventory.RuntimeItems;
            int usedSlots = GameManager.Instance.Inventory.UsedSlotCount;
            int maxSlots = GameManager.Instance.Inventory.MaxSlotCount;

            if (entries.Count == 0)
            {
                inventoryText.text = $"인벤토리 {usedSlots}/{maxSlots}칸\n- 비어 있음";
                RefreshHubPopupContent();
                return;
            }

            StringBuilder builder = new();
            builder.AppendLine($"인벤토리 {usedSlots}/{maxSlots}칸");

            foreach (InventoryEntry entry in entries)
            {
                if (entry == null || entry.resource == null)
                {
                    continue;
                }

                builder.AppendLine($"- {entry.resource.DisplayName} x{entry.amount}");
            }

            inventoryText.text = builder.ToString().TrimEnd();
            RefreshHubPopupContent();
        }

        /// <summary>
        /// 창고 목록과 마지막 작업 메시지를 창고 팝업 본문에 갱신합니다.
        /// </summary>
        private void RefreshStorageText()
        {
            if (storageText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (cachedStorage == null)
            {
                storageText.text = string.Empty;
                return;
            }

            string summary = cachedStorage.BuildSummaryText();
            if (!string.IsNullOrWhiteSpace(cachedStorage.LastOperationMessage))
            {
                summary += $"\n\n{cachedStorage.LastOperationMessage}";
            }

            storageText.text = summary;
            RefreshHubPopupContent();
        }

        /// <summary>
        /// 업그레이드 요약과 마지막 결과 메시지를 팝업 본문용으로 정리합니다.
        /// </summary>
        private void RefreshUpgradeText()
        {
            if (upgradeText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (cachedUpgradeManager == null)
            {
                upgradeText.text = string.Empty;
                return;
            }

            string summary = cachedUpgradeManager.BuildUpgradeSummary();
            if (!string.IsNullOrWhiteSpace(cachedUpgradeManager.LastUpgradeMessage))
            {
                summary += $"\n\n{cachedUpgradeManager.LastUpgradeMessage}";
            }

            upgradeText.text = summary;
            RefreshHubPopupContent();
        }

        /// <summary>
        /// 허브에서는 코인 배지만, 그 외 씬에서는 코인과 평판 상태 줄을 노출합니다.
        /// </summary>
        private void RefreshEconomyText()
        {
            if (goldText == null)
            {
                return;
            }

            int gold = GameManager.Instance != null && GameManager.Instance.Economy != null
                ? GameManager.Instance.Economy.CurrentGold
                : 0;
            int reputation = GameManager.Instance != null && GameManager.Instance.Economy != null
                ? GameManager.Instance.Economy.CurrentReputation
                : 0;

            goldText.text = IsHubScene()
                ? gold.ToString("N0")
                : $"코인: {gold}   평판: {reputation}";
        }

        /// <summary>
        /// 허브 메뉴 팝업에서 보여 줄 요리 선택 요약을 갱신합니다.
        /// </summary>
        private void RefreshSelectedRecipeText(RecipeData recipe)
        {
            if (selectedRecipeText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (cachedRestaurant == null)
            {
                selectedRecipeText.text = "메뉴 선택: 허브에서 확인";
                return;
            }

            string summary = cachedRestaurant.BuildRecipeSelectionSummary();
            if (string.IsNullOrWhiteSpace(summary) && recipe == null)
            {
                selectedRecipeText.text = "선택 메뉴: 없음";
                return;
            }

            selectedRecipeText.text = summary;
            RefreshHubPopupContent();
        }

        private void RefreshGuideText()
        {
            if (guideText == null)
            {
                return;
            }

            if (showGuideHelpOverlay)
            {
                string helpText = BuildGuideHelpOverlayText();
                bool shouldShowHelp = activeHubPanel == HubPopupPanel.None && !string.IsNullOrWhiteSpace(helpText);
                guideText.text = helpText;
                guideText.gameObject.SetActive(shouldShowHelp);
                SetNamedObjectActive("GuideBackdrop", shouldShowHelp);
                return;
            }

            if (IsHubScene())
            {
                string message = BuildHubMessageLine();
                guideText.text = message;
                guideText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
                SetNamedObjectActive("GuideBackdrop", false);

                if (resultText != null)
                {
                    resultText.text = string.Empty;
                    resultText.gameObject.SetActive(false);
                }

                SetNamedObjectActive("ResultBackdrop", false);
                return;
            }

            string guide = cachedDayCycle != null ? cachedDayCycle.CurrentGuideText : string.Empty;
            guideText.text = guide;
            guideText.gameObject.SetActive(!string.IsNullOrWhiteSpace(guide));
            SetNamedObjectActive("GuideBackdrop", !IsHubScene() && guideText.gameObject.activeSelf);
        }

        private string BuildGuideHelpOverlayText()
        {
            if (IsHubScene())
            {
                return "이동: WASD / 방향키   상호작용: E   하단 메뉴 버튼으로 요리, 업그레이드, 재료 패널을 연다";
            }

            return "이동: WASD / 방향키   상호작용: E   채집물과 포탈 앞에서 E로 수집하거나 이동한다";
        }

        private void RefreshRestaurantResultText(string result)
        {
            if (resultText == null)
            {
                return;
            }

            if (IsHubScene())
            {
                resultText.text = string.Empty;
                resultText.gameObject.SetActive(false);
                SetNamedObjectActive("ResultBackdrop", false);
                RefreshGuideText();
                return;
            }

            resultText.text = result;
            resultText.gameObject.SetActive(!string.IsNullOrWhiteSpace(result));
            SetNamedObjectActive("ResultBackdrop", !IsHubScene() && resultText.gameObject.activeSelf);
        }

        /// <summary>
        /// 현재 안내 문구와 허브 패널 버튼 상태를 함께 맞춥니다.
        /// </summary>
        private void RefreshDayCycleState()
        {
            HideLegacyDayRoutineObjects();
            RefreshGuideText();
            RefreshRestaurantResultText(cachedRestaurant != null ? cachedRestaurant.LastServiceResult : string.Empty);
            RefreshButtonStates();

            if (cachedRestaurant != null)
            {
                RefreshSelectedRecipeText(cachedRestaurant.SelectedRecipe);
            }
        }

        private void RefreshButtonStates()
        {
            HideLegacyDayRoutineObjects();

            bool showActionButtons = IsHubScene() && activeHubPanel == HubPopupPanel.None && !ShouldUseTypedPopupUi();
            SetNamedObjectActive("ActionDock", showActionButtons);
            SetNamedObjectActive("ActionAccent", showActionButtons);
            SetNamedObjectActive("ActionCaption", showActionButtons);
            SetButtonGameObjectActive(openRestaurantButton, showActionButtons);
            SetButtonGameObjectActive(closeRestaurantButton, showActionButtons);

            if (openRestaurantButton != null)
            {
                openRestaurantButton.interactable = cachedRestaurant != null && cachedRestaurant.CanOpenRestaurant;
            }

            if (closeRestaurantButton != null)
            {
                bool hasActiveTickets = cachedCustomerService != null && cachedCustomerService.HasActiveTickets;
                closeRestaurantButton.interactable = cachedRestaurant != null
                    && cachedRestaurant.IsRestaurantOpen
                    && !hasActiveTickets;
            }
        }
    }
}
