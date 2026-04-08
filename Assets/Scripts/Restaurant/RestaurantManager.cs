using System;
using System.Collections.Generic;
using System.Text;
using CoreLoop.Core;
using Shared.Data;
using Management.Economy;
using Management.Inventory;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Restaurant 네임스페이스
namespace Restaurant
{
    /// <summary>
    /// 메뉴 선택과 영업 결과를 관리한다. 원격 API가 준비되면 bootstrap 레시피 카탈로그를 우선 사용한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "RestaurantManager")]
    public class RestaurantManager : MonoBehaviour
    {
        [SerializeField] private List<RecipeData> availableRecipes = new();
        [SerializeField, Min(1)] private int serviceCapacity = 3;
        [SerializeField] private int selectedRecipeIndex;

        private readonly List<RecipeData> remoteRecipes = new();
        private bool remoteCatalogApplied;
        private string selectedRecipeId = string.Empty;

        public event Action<RecipeData> SelectedRecipeChanged;
        public event Action<string> ServiceResultChanged;

        public RecipeData SelectedRecipe { get; private set; }
        public string SelectedRecipeId => selectedRecipeId;
        public string LastServiceResult { get; private set; } = "메뉴를 고르고 영업을 시작하세요.";

        /// <summary>
        /// 시작 시 기본 레시피 목록을 복구하고 현재 선택 메뉴를 확정합니다.
        /// </summary>
        private void Start()
        {
            EnsureRecipeList();
            RefreshSelectedRecipe();
            BroadcastState();
        }

        public IReadOnlyList<RecipeData> AvailableRecipes
        {
            get
            {
                EnsureRecipeList();
                return availableRecipes;
            }
        }

        /// <summary>
        /// API bootstrap의 recipes/ingredients 배열을 메뉴 선택 정본으로 반영합니다.
        /// </summary>
        public void ApplyRemoteCatalog(
            IEnumerable<JongguApiRecipeDefinition> recipes,
            IEnumerable<JongguApiIngredientDefinition> ingredientCatalog = null)
        {
            if (recipes == null)
            {
                return;
            }

            Dictionary<string, JongguApiIngredientDefinition> ingredientsById = BuildIngredientCatalog(ingredientCatalog);
            ClearRemoteRecipes();

            foreach (JongguApiRecipeDefinition definition in recipes)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.recipeId))
                {
                    continue;
                }

                RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
                recipe.name = definition.recipeId;

                List<RecipeIngredient> ingredients = new();
                if (definition.ingredients != null)
                {
                    foreach (JongguApiRecipeIngredientDefinition ingredient in definition.ingredients)
                    {
                        if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.ingredientId))
                        {
                            continue;
                        }

                        ResourceData resource = GeneratedGameDataLocator.FindGeneratedResource(
                            ingredient.ingredientName,
                            ingredient.ingredientId);
                        RecipeIngredient recipeIngredient = RecipeIngredient.CreateRuntime(
                            ingredient.ingredientId,
                            ingredient.ingredientName,
                            ingredient.quantity,
                            resource);

                        if (ingredientsById.TryGetValue(ingredient.ingredientId, out JongguApiIngredientDefinition catalogIngredient))
                        {
                            recipeIngredient.ConfigureCatalogMetadata(
                                catalogIngredient.difficulty,
                                catalogIngredient.supplySource,
                                catalogIngredient.acquisitionSource,
                                catalogIngredient.acquisitionMethod,
                                catalogIngredient.acquisitionTool,
                                catalogIngredient.buyPrice,
                                catalogIngredient.sellPrice,
                                catalogIngredient.memo);
                        }

                        ingredients.Add(recipeIngredient);
                    }
                }

                recipe.ConfigureRuntime(
                    definition.recipeId,
                    definition.recipeName,
                    BuildRemoteRecipeDescription(definition),
                    definition.price,
                    0,
                    definition.supplySource,
                    definition.difficulty,
                    definition.cookingMethod,
                    definition.memo,
                    ingredients);

                remoteRecipes.Add(recipe);
            }

            if (remoteRecipes.Count == 0)
            {
                return;
            }

            remoteCatalogApplied = true;
            availableRecipes.Clear();
            availableRecipes.AddRange(remoteRecipes);
            RefreshSelectedRecipe();
            BroadcastState();
        }

        /// <summary>
        /// 레시피 인덱스를 기준으로 현재 메뉴를 바꿉니다.
        /// </summary>
        public void SelectRecipeByIndex(int recipeIndex)
        {
            EnsureRecipeList();

            if (availableRecipes.Count == 0)
            {
                SelectedRecipe = null;
                selectedRecipeId = string.Empty;
                selectedRecipeIndex = 0;
                BroadcastState();
                return;
            }

            selectedRecipeIndex = Mathf.Clamp(recipeIndex, 0, availableRecipes.Count - 1);
            RefreshSelectedRecipe();
            BroadcastState();
        }

        /// <summary>
        /// 해당 레시피를 현재 재료로 한 번 이상 판매할 수 있는지 확인합니다.
        /// </summary>
        public bool CanServe(RecipeData recipe)
        {
            EnsureRecipeList();
            return GetCookableServings(recipe) > 0;
        }

        /// <summary>
        /// UI 에 표시할 메뉴 목록과 선택 메뉴 상세 문구를 조합합니다.
        /// </summary>
        public string BuildRecipeSelectionSummary()
        {
            EnsureRecipeList();
            RefreshSelectedRecipe();

            if (availableRecipes.Count == 0)
            {
                return "- 등록된 메뉴가 없습니다.";
            }

            StringBuilder builder = new();

            foreach (RecipeData recipe in availableRecipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                string selectedMark = recipe == SelectedRecipe ? "[선택] " : string.Empty;
                builder.AppendLine($"- {selectedMark}{recipe.DisplayName} (가능 {GetCookableServings(recipe)})");
            }

            if (SelectedRecipe == null)
            {
                return builder.ToString().TrimEnd();
            }

            builder.AppendLine();
            builder.AppendLine($"현재 메뉴: {SelectedRecipe.DisplayName}");

            if (!string.IsNullOrWhiteSpace(SelectedRecipe.Description))
            {
                builder.AppendLine(SelectedRecipe.Description);
            }

            builder.AppendLine($"- 판매가: {SelectedRecipe.SellPrice}");
            if (!string.IsNullOrWhiteSpace(SelectedRecipe.SupplySource))
            {
                builder.AppendLine($"- 공급처: {SelectedRecipe.SupplySource}");
            }

            if (!string.IsNullOrWhiteSpace(SelectedRecipe.CookingMethod))
            {
                builder.AppendLine($"- 조리법: {SelectedRecipe.CookingMethod}");
            }

            builder.AppendLine("- 필요 재료");

            foreach (RecipeIngredient ingredient in SelectedRecipe.Ingredients)
            {
                string line = BuildIngredientRequirementLine(ingredient);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    builder.AppendLine(line);
                }
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 현재 보유 재료로 몇 인분까지 판매 가능한지 계산합니다.
        /// </summary>
        public int GetCookableServings(RecipeData recipe)
        {
            EnsureRecipeList();

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (recipe == null || inventory == null)
            {
                return 0;
            }

            IReadOnlyList<RecipeIngredient> ingredients = recipe.Ingredients;
            if (ingredients == null || ingredients.Count == 0)
            {
                return 0;
            }

            int servings = int.MaxValue;

            foreach (RecipeIngredient ingredient in ingredients)
            {
                if (!RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out int ingredientAmount))
                {
                    return 0;
                }

                int ownedAmount = inventory.GetAmount(resource);
                servings = Mathf.Min(servings, ownedAmount / ingredientAmount);
            }

            return servings == int.MaxValue ? 0 : servings;
        }

        /// <summary>
        /// 현재 선택 메뉴를 기준으로 재료를 소모하고 판매 결과를 계산합니다.
        /// </summary>
        public void RunServiceForSelectedRecipe()
        {
            EnsureRecipeList();
            RefreshSelectedRecipe();

            if (SelectedRecipe == null)
            {
                SetResult("선택된 메뉴가 없습니다.");
                return;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null)
            {
                SetResult("영업을 진행할 인벤토리를 찾을 수 없습니다.");
                return;
            }

            int cookableServings = GetCookableServings(SelectedRecipe);
            int servingsToSell = Mathf.Min(serviceCapacity, cookableServings);

            if (servingsToSell <= 0)
            {
                SetResult($"{SelectedRecipe.DisplayName} 재료가 부족합니다.");
                return;
            }

            foreach (RecipeIngredient ingredient in SelectedRecipe.Ingredients)
            {
                if (!RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out int ingredientAmount))
                {
                    continue;
                }

                inventory.TryRemove(resource, ingredientAmount * servingsToSell);
            }

            int revenue = SelectedRecipe.SellPrice * servingsToSell;
            int reputationGain = Mathf.Max(0, SelectedRecipe.ReputationDelta * servingsToSell);

            EconomyManager economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy != null)
            {
                economy.AddGold(revenue);
                economy.AddReputation(reputationGain);
            }

            StringBuilder builder = new();
            builder.AppendLine("영업 결과");
            builder.AppendLine($"- 판매 메뉴: {SelectedRecipe.DisplayName} x{servingsToSell}");
            builder.AppendLine($"- 소모 재료: {BuildIngredientUsageText(SelectedRecipe, servingsToSell)}");
            builder.AppendLine($"- 획득 골드: +{revenue}");
            builder.Append($"- 평판 변화: +{reputationGain}");

            SetResult(builder.ToString());
            BroadcastState();
        }

        /// <summary>
        /// API 스냅샷 기준으로 선택 메뉴와 영업 가능 인분을 다시 맞춘다.
        /// </summary>
        public void ApplyRemoteState(string snapshotSelectedRecipeId, int capacity, string serviceResult = null)
        {
            EnsureRecipeList();

            serviceCapacity = Mathf.Max(1, capacity);
            selectedRecipeId = string.IsNullOrWhiteSpace(snapshotSelectedRecipeId) ? string.Empty : snapshotSelectedRecipeId;
            SelectedRecipe = FindRecipeById(selectedRecipeId);
            selectedRecipeIndex = SelectedRecipe != null ? Mathf.Max(availableRecipes.IndexOf(SelectedRecipe), 0) : 0;

            if (!string.IsNullOrWhiteSpace(serviceResult))
            {
                LastServiceResult = serviceResult;
            }
            else if (SelectedRecipe == null)
            {
                LastServiceResult = "메뉴를 고르고 영업을 시작하세요.";
            }

            BroadcastState();
        }

        /// <summary>
        /// 현재 인덱스와 레시피 목록을 기준으로 선택 메뉴를 다시 맞춥니다.
        /// </summary>
        private void RefreshSelectedRecipe()
        {
            EnsureRecipeList();

            if (availableRecipes.Count == 0)
            {
                SelectedRecipe = null;
                selectedRecipeId = string.Empty;
                selectedRecipeIndex = 0;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedRecipeId))
            {
                RecipeData recipeById = FindRecipeById(selectedRecipeId);
                if (recipeById != null)
                {
                    SelectedRecipe = recipeById;
                    selectedRecipeIndex = Mathf.Max(availableRecipes.IndexOf(SelectedRecipe), 0);
                    return;
                }
            }

            selectedRecipeIndex = Mathf.Clamp(selectedRecipeIndex, 0, availableRecipes.Count - 1);
            SelectedRecipe = availableRecipes[selectedRecipeIndex];
            selectedRecipeId = SelectedRecipe != null ? SelectedRecipe.RecipeId : string.Empty;
        }

        private RecipeData FindRecipeById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return null;
            }

            foreach (RecipeData recipe in availableRecipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                if (string.Equals(recipe.RecipeId, recipeId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(recipe.name, recipeId, StringComparison.OrdinalIgnoreCase))
                {
                    return recipe;
                }
            }

            return GeneratedGameDataLocator.FindGeneratedRecipe(recipeId);
        }

        /// <summary>
        /// 씬 참조가 비어 있어도 기본 레시피 에셋을 런타임에 다시 채웁니다.
        /// </summary>
        private void EnsureRecipeList()
        {
            availableRecipes ??= new List<RecipeData>();
            availableRecipes.RemoveAll(recipe => recipe == null);

            if (remoteCatalogApplied)
            {
                return;
            }

            TryAddDefaultRecipe("SushiSet", "스시 세트");
            TryAddDefaultRecipe("SeafoodSoup", "해물탕");
            TryAddDefaultRecipe("HerbFishSoup", "약초 생선탕");
            TryAddDefaultRecipe("ForestBasket", "숲 바구니");
            TryAddDefaultRecipe("GlowMossStew", "발광 이끼 수프");
            TryAddDefaultRecipe("WindHerbSalad", "향초 샐러드");
        }

        /// <summary>
        /// generated 데이터에서 레시피를 찾아 목록에 중복 없이 추가합니다.
        /// </summary>
        private void TryAddDefaultRecipe(string assetName, params string[] alternateKeys)
        {
            RecipeData recipe = GeneratedGameDataLocator.FindGeneratedRecipe(assetName, alternateKeys);
            if (recipe != null && !availableRecipes.Contains(recipe))
            {
                availableRecipes.Add(recipe);
            }
        }

        /// <summary>
        /// 선택 메뉴와 결과 텍스트 변경 이벤트를 한 번에 전달합니다.
        /// </summary>
        private void BroadcastState()
        {
            SelectedRecipeChanged?.Invoke(SelectedRecipe);
            ServiceResultChanged?.Invoke(LastServiceResult);
        }

        /// <summary>
        /// 마지막 장사 결과 문자열을 저장하고 구독자에게 알립니다.
        /// </summary>
        private void SetResult(string result)
        {
            LastServiceResult = result;
            ServiceResultChanged?.Invoke(LastServiceResult);
        }

        private void ClearRemoteRecipes()
        {
            foreach (RecipeData recipe in remoteRecipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(recipe);
                }
                else
                {
                    DestroyImmediate(recipe);
                }
            }

            remoteRecipes.Clear();
        }

        private static string BuildRemoteRecipeDescription(JongguApiRecipeDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(definition.memo))
            {
                return definition.memo;
            }

            List<string> parts = new();
            if (!string.IsNullOrWhiteSpace(definition.supplySource))
            {
                parts.Add(definition.supplySource);
            }

            if (!string.IsNullOrWhiteSpace(definition.cookingMethod))
            {
                parts.Add(definition.cookingMethod);
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
        }

        private static string BuildIngredientRequirementLine(RecipeIngredient ingredient)
        {
            if (ingredient == null)
            {
                return string.Empty;
            }

            string ingredientName = string.IsNullOrWhiteSpace(ingredient.IngredientName)
                ? ingredient.IngredientId
                : ingredient.IngredientName;
            if (string.IsNullOrWhiteSpace(ingredientName))
            {
                return string.Empty;
            }

            string catalogSummary = ingredient.BuildCatalogSummary();
            string displayName = string.IsNullOrWhiteSpace(catalogSummary)
                ? ingredientName
                : $"{ingredientName} ({catalogSummary})";

            if (!RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out int ingredientAmount))
            {
                return $"  {displayName} x{ingredient.Quantity}";
            }

            int ownedAmount = GameManager.Instance != null && GameManager.Instance.Inventory != null
                ? GameManager.Instance.Inventory.GetAmount(resource)
                : 0;

            return $"  {displayName} {ownedAmount}/{ingredientAmount}";
        }

        private static Dictionary<string, JongguApiIngredientDefinition> BuildIngredientCatalog(
            IEnumerable<JongguApiIngredientDefinition> ingredientCatalog)
        {
            Dictionary<string, JongguApiIngredientDefinition> ingredientsById = new(StringComparer.Ordinal);
            if (ingredientCatalog == null)
            {
                return ingredientsById;
            }

            foreach (JongguApiIngredientDefinition ingredient in ingredientCatalog)
            {
                if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.ingredientId))
                {
                    continue;
                }

                ingredientsById[ingredient.ingredientId] = ingredient;
            }

            return ingredientsById;
        }

        /// <summary>
        /// 결과 화면용 재료 소모 문자열을 생성합니다.
        /// </summary>
        private static string BuildIngredientUsageText(RecipeData recipe, int servings)
        {
            if (recipe == null || servings <= 0 || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
            {
                return "없음";
            }

            List<string> parts = new();

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                {
                    continue;
                }

                string ingredientName = string.IsNullOrWhiteSpace(ingredient.IngredientName)
                    ? ingredient.IngredientId
                    : ingredient.IngredientName;
                if (string.IsNullOrWhiteSpace(ingredientName))
                {
                    continue;
                }

                parts.Add($"{ingredientName} x{ingredient.Quantity * servings}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "없음";
        }
    }
}
