using System;
using System.Collections.Generic;
using System.Text;
using CoreLoop.Core;
using Management.Economy;
using Management.Inventory;
using Shared.Data;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Restaurant
{
    /// <summary>
    /// 레시피 카탈로그 포커스, 오늘의 메뉴 3칸, OPEN/CLOSE 상태와 영업 보상을 관리한다.
    /// 정적 데이터는 RestaurantRecipeCatalog를 기준으로 읽고, 변하는 상태는 런타임 필드와 TodayMenuState에 둔다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "RestaurantManager")]
    public class RestaurantManager : MonoBehaviour
    {
        [SerializeField] private List<RecipeData> availableRecipes = new();
        [SerializeField, Min(1)] private int serviceCapacity = 3;
        [SerializeField] private int selectedRecipeIndex;
        [SerializeField] private List<string> todayMenuRecipeIds = new();
        [SerializeField] private int selectedTodayMenuSlotIndex;

        private TodayMenuState todayMenuState;
        private string selectedRecipeId = string.Empty;
        private readonly List<RecipeData> resolvedTodayMenuRecipes = new();

        private RestaurantRecipeCatalog standaloneRecipeCatalog;

        public event Action<RecipeData> SelectedRecipeChanged;
        public event Action<string> ServiceResultChanged;
        public event Action TodayMenuChanged;
        public event Action<bool> ServiceStateChanged;

        public RecipeData SelectedRecipe { get; private set; }
        public string SelectedRecipeId => selectedRecipeId;
        public string LastServiceResult { get; private set; } = "오늘의 메뉴 3칸을 정한 뒤 OPEN 하세요.";

        /// <summary>
        /// 카탈로그 부트스트랩에 사용할 씬 직렬화 레시피 목록이다.
        /// </summary>
        internal IReadOnlyList<RecipeData> BootstrapRecipes => availableRecipes;

        /// <summary>
        /// 현재 허브에서 고를 수 있는 전체 레시피 목록이다.
        /// </summary>
        public IReadOnlyList<RecipeData> AvailableRecipes => ResolveAvailableRecipes();

        /// <summary>
        /// 오늘의 메뉴 슬롯에 배치된 레시피 3칸을 순서대로 반환한다.
        /// 비어 있는 칸은 null이다.
        /// </summary>
        public IReadOnlyList<RecipeData> TodayMenuRecipes
        {
            get
            {
                EnsureTodayMenuState();
                return resolvedTodayMenuRecipes;
            }
        }

        /// <summary>
        /// 현재 활성화된 오늘의 메뉴 슬롯 인덱스다.
        /// </summary>
        public int SelectedTodayMenuSlotIndex
        {
            get
            {
                EnsureTodayMenuState();
                return todayMenuState.SelectedSlotIndex;
            }
        }

        /// <summary>
        /// OPEN 가능한지 여부다.
        /// 오늘의 메뉴 3칸이 모두 유효한 레시피로 채워져 있어야 한다.
        /// </summary>
        public bool CanOpenRestaurant
        {
            get
            {
                EnsureTodayMenuState();
                return !IsRestaurantOpen && HasCompleteTodayMenu();
            }
        }

        /// <summary>
        /// 현재 식당 영업 상태다.
        /// </summary>
        public bool IsRestaurantOpen { get; private set; }

        /// <summary>
        /// 시작 시 레시피 목록과 오늘의 메뉴 상태를 복구한다.
        /// </summary>
        private void Start()
        {
            ResolveRecipeCatalog();
            EnsureTodayMenuState();
            RefreshSelectedRecipe();
            BroadcastState();
        }

        /// <summary>
        /// 레시피 인덱스를 기준으로 카탈로그 포커스를 바꾼다.
        /// 오늘의 메뉴 슬롯 배치는 바꾸지 않는다.
        /// </summary>
        public void SelectRecipeByIndex(int recipeIndex)
        {
            IReadOnlyList<RecipeData> recipes = ResolveAvailableRecipes();

            if (recipes.Count == 0)
            {
                SelectedRecipe = null;
                selectedRecipeId = string.Empty;
                selectedRecipeIndex = 0;
                BroadcastState();
                return;
            }

            selectedRecipeIndex = Mathf.Clamp(recipeIndex, 0, recipes.Count - 1);
            RefreshSelectedRecipe();
            BroadcastState();
        }

        /// <summary>
        /// 오늘의 메뉴 활성 슬롯을 바꾼다.
        /// 영업 중에는 수정할 수 없다.
        /// </summary>
        public bool SelectTodayMenuSlot(int slotIndex)
        {
            EnsureTodayMenuState();
            if (IsRestaurantOpen || !todayMenuState.SelectSlot(slotIndex))
            {
                return false;
            }

            PersistTodayMenuState();
            RefreshResolvedTodayMenuRecipes();
            TodayMenuChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 현재 선택된 레시피를 활성 슬롯에 배치한다.
        /// 이미 다른 슬롯에 있으면 중복 생성하지 않고 현재 슬롯으로 이동한다.
        /// </summary>
        public bool AssignSelectedTodayMenuRecipe(RecipeData recipe)
        {
            EnsureTodayMenuState();

            if (IsRestaurantOpen || recipe == null || !ContainsRecipe(recipe))
            {
                return false;
            }

            SetSelectedRecipe(recipe);
            bool changed = todayMenuState.AssignRecipeToSelectedSlot(recipe.RecipeId);
            PersistTodayMenuState();
            RefreshResolvedTodayMenuRecipes();
            BroadcastState();
            return changed;
        }

        /// <summary>
        /// OPEN 상태로 전환한다.
        /// 오늘의 메뉴 3칸이 모두 채워져 있어야 한다.
        /// </summary>
        public bool TryOpenRestaurant()
        {
            EnsureTodayMenuState();

            if (IsRestaurantOpen)
            {
                SetResult("이미 영업 중입니다.");
                BroadcastState();
                return false;
            }

            if (!HasCompleteTodayMenu())
            {
                SetResult("오늘의 메뉴 3칸을 모두 채워야 OPEN 할 수 있습니다.");
                BroadcastState();
                return false;
            }

            IsRestaurantOpen = true;
            SetResult("영업을 시작했습니다. PassCounter와 CookingUtensils를 사용할 수 있습니다.");
            BroadcastState();
            return true;
        }

        /// <summary>
        /// CLOSE 상태로 전환한다.
        /// 활성 주문이 남아 있으면 실패한다.
        /// </summary>
        public bool TryCloseRestaurant(int activeOrderCount)
        {
            if (!IsRestaurantOpen)
            {
                SetResult("이미 CLOSE 상태입니다.");
                BroadcastState();
                return false;
            }

            if (activeOrderCount > 0)
            {
                SetResult("진행 중인 주문이 남아 있어 CLOSE 할 수 없습니다.");
                BroadcastState();
                return false;
            }

            IsRestaurantOpen = false;
            SetResult("영업을 종료했습니다. 오늘의 메뉴를 다시 조정할 수 있습니다.");
            BroadcastState();
            return true;
        }

        /// <summary>
        /// 현재 재료 기준으로 해당 레시피를 조리 가능한지 확인한다.
        /// 기본 냉장고 재료는 serviceCapacity 범위에서 무한으로 취급한다.
        /// </summary>
        public bool CanServe(RecipeData recipe)
        {
            return GetCookableServings(recipe) > 0;
        }

        /// <summary>
        /// UI 요약용으로 오늘의 메뉴 상태와 현재 선택 레시피 정보를 조합한다.
        /// </summary>
        public string BuildRecipeSelectionSummary()
        {
            IReadOnlyList<RecipeData> recipes = ResolveAvailableRecipes();
            EnsureTodayMenuState();
            RefreshSelectedRecipe();

            StringBuilder builder = new();
            builder.AppendLine(IsRestaurantOpen ? "영업 상태: OPEN" : "영업 상태: CLOSE");
            builder.AppendLine("- 오늘의 메뉴");

            for (int slotIndex = 0; slotIndex < TodayMenuState.SlotCount; slotIndex++)
            {
                RecipeData recipe = resolvedTodayMenuRecipes[slotIndex];
                string marker = slotIndex == SelectedTodayMenuSlotIndex ? "[활성] " : string.Empty;
                builder.AppendLine($"  {marker}{slotIndex + 1}. {(recipe != null ? recipe.DisplayName : "비어 있음")}");
            }

            if (SelectedRecipe == null)
            {
                builder.AppendLine();
                builder.Append(IsRestaurantOpen
                    ? "영업 중에는 오늘의 메뉴를 수정할 수 없습니다."
                    : recipes.Count == 0
                        ? "표시할 레시피가 없습니다."
                        : "왼쪽 슬롯을 고른 뒤 레시피를 눌러 배치하세요.");
                return builder.ToString().TrimEnd();
            }

            builder.AppendLine();
            builder.AppendLine($"선택 레시피: {SelectedRecipe.DisplayName}");

            if (!string.IsNullOrWhiteSpace(SelectedRecipe.Description))
            {
                builder.AppendLine(SelectedRecipe.Description);
            }

            builder.AppendLine($"- 판매가: {SelectedRecipe.SellPrice}");
            if (!string.IsNullOrWhiteSpace(SelectedRecipe.CookingMethod))
            {
                builder.AppendLine($"- 조리법: {SelectedRecipe.CookingMethod}");
            }

            builder.AppendLine($"- 가능 수량: {GetCookableServings(SelectedRecipe)}");
            builder.Append(IsRestaurantOpen
                ? "- 영업 중에는 메뉴를 읽기 전용으로 확인합니다."
                : "- 활성 슬롯을 고른 뒤 이 레시피를 배치할 수 있습니다.");
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 현재 재료로 몇 인분까지 조리 가능한지 계산한다.
        /// 기본 냉장고 재료만 쓰는 레시피는 serviceCapacity 만큼 가능하다고 본다.
        /// </summary>
        public int GetCookableServings(RecipeData recipe)
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (recipeCatalog == null || recipe == null || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
            {
                return 0;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            int servings = Mathf.Max(1, serviceCapacity);

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                {
                    return 0;
                }

                if (!RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out int ingredientAmount))
                {
                    if (recipeCatalog.IsBasicIngredient(ingredient.IngredientId))
                    {
                        continue;
                    }

                    return 0;
                }

                if (recipeCatalog.IsBasicIngredient(resource) || recipeCatalog.IsBasicIngredient(ingredient.IngredientId))
                {
                    continue;
                }

                if (inventory == null)
                {
                    return 0;
                }

                servings = Mathf.Min(servings, inventory.GetAmount(resource) / ingredientAmount);
                if (servings <= 0)
                {
                    return 0;
                }
            }

            return Mathf.Max(0, servings);
        }

        /// <summary>
        /// 직접 영업 시작 경로는 더 이상 사용하지 않는다.
        /// OPEN 후 조리와 서빙 흐름으로 진행하도록 안내만 갱신한다.
        /// </summary>
        public void RunServiceForSelectedRecipe()
        {
            SetResult("직접 영업 시작은 지원하지 않습니다. 오늘의 메뉴를 정하고 OPEN 후 조리와 서빙을 진행하세요.");
            BroadcastState();
        }

        /// <summary>
        /// 서빙 성공 시 보상과 결과 문구를 1회 반영한다.
        /// 재료는 여기서 다시 소모하지 않는다.
        /// </summary>
        public bool TryRecordCompletedOrder(string recipeId)
        {
            if (!IsRestaurantOpen || string.IsNullOrWhiteSpace(recipeId))
            {
                return false;
            }

            RecipeData servedRecipe = FindRecipeById(recipeId);
            if (servedRecipe == null)
            {
                return false;
            }

            ApplyServiceOutcome(servedRecipe, 1);
            return true;
        }

        private IReadOnlyList<RecipeData> ResolveAvailableRecipes()
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            return recipeCatalog != null ? recipeCatalog.AvailableRecipes : Array.Empty<RecipeData>();
        }

        private RestaurantRecipeCatalog ResolveRecipeCatalog()
        {
            if (HubRuntimeContext.Active != null)
            {
                return HubRuntimeContext.Active.RecipeCatalog;
            }

            standaloneRecipeCatalog ??= RestaurantRecipeCatalog.Create(availableRecipes);
            return standaloneRecipeCatalog;
        }

        private void RefreshSelectedRecipe()
        {
            IReadOnlyList<RecipeData> recipes = ResolveAvailableRecipes();

            if (recipes.Count == 0)
            {
                SelectedRecipe = null;
                selectedRecipeId = string.Empty;
                selectedRecipeIndex = 0;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedRecipeId))
            {
                RecipeData recipeById = FindRecipeById(selectedRecipeId);
                if (recipeById != null && ContainsRecipe(recipeById))
                {
                    SetSelectedRecipe(recipeById);
                    return;
                }
            }

            selectedRecipeIndex = Mathf.Clamp(selectedRecipeIndex, 0, recipes.Count - 1);
            SetSelectedRecipe(recipes[selectedRecipeIndex]);
        }

        private void SetSelectedRecipe(RecipeData recipe)
        {
            SelectedRecipe = recipe;
            selectedRecipeId = recipe != null ? recipe.RecipeId : string.Empty;

            IReadOnlyList<RecipeData> recipes = ResolveAvailableRecipes();
            if (recipe == null || recipes.Count == 0)
            {
                selectedRecipeIndex = 0;
                return;
            }

            for (int index = 0; index < recipes.Count; index++)
            {
                if (ReferenceEquals(recipes[index], recipe)
                    || string.Equals(recipes[index].RecipeId, recipe.RecipeId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedRecipeIndex = index;
                    return;
                }
            }

            selectedRecipeIndex = 0;
        }

        private bool ContainsRecipe(RecipeData recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            IReadOnlyList<RecipeData> recipes = ResolveAvailableRecipes();
            for (int index = 0; index < recipes.Count; index++)
            {
                RecipeData candidate = recipes[index];
                if (candidate == recipe)
                {
                    return true;
                }

                if (candidate != null
                    && !string.IsNullOrWhiteSpace(candidate.RecipeId)
                    && string.Equals(candidate.RecipeId, recipe.RecipeId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureTodayMenuState()
        {
            if (todayMenuState == null)
            {
                todayMenuState = new TodayMenuState(todayMenuRecipeIds, selectedTodayMenuSlotIndex);
            }

            PersistTodayMenuState();
            RefreshResolvedTodayMenuRecipes();
        }

        private void PersistTodayMenuState()
        {
            todayMenuRecipeIds ??= new List<string>(TodayMenuState.SlotCount);
            todayMenuRecipeIds.Clear();

            for (int slotIndex = 0; slotIndex < TodayMenuState.SlotCount; slotIndex++)
            {
                todayMenuRecipeIds.Add(todayMenuState != null ? todayMenuState.GetRecipeId(slotIndex) : string.Empty);
            }

            selectedTodayMenuSlotIndex = todayMenuState != null ? todayMenuState.SelectedSlotIndex : 0;
        }

        private void RefreshResolvedTodayMenuRecipes()
        {
            resolvedTodayMenuRecipes.Clear();

            for (int slotIndex = 0; slotIndex < TodayMenuState.SlotCount; slotIndex++)
            {
                string recipeId = todayMenuState != null ? todayMenuState.GetRecipeId(slotIndex) : string.Empty;
                resolvedTodayMenuRecipes.Add(FindRecipeById(recipeId));
            }
        }

        private bool HasCompleteTodayMenu()
        {
            if (resolvedTodayMenuRecipes.Count < TodayMenuState.SlotCount)
            {
                RefreshResolvedTodayMenuRecipes();
            }

            for (int slotIndex = 0; slotIndex < TodayMenuState.SlotCount; slotIndex++)
            {
                if (slotIndex >= resolvedTodayMenuRecipes.Count || resolvedTodayMenuRecipes[slotIndex] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private RecipeData FindRecipeById(string recipeId)
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            return recipeCatalog != null && recipeCatalog.TryGetRecipe(recipeId, out RecipeData recipe)
                ? recipe
                : null;
        }

        private void ApplyServiceOutcome(RecipeData recipe, int servings)
        {
            if (recipe == null || servings <= 0)
            {
                return;
            }

            int revenue = recipe.SellPrice * servings;
            int reputationGain = Mathf.Max(0, recipe.ReputationDelta * servings);

            EconomyManager economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy != null)
            {
                economy.AddGold(revenue);
                economy.AddReputation(reputationGain);
            }

            StringBuilder builder = new();
            builder.AppendLine("서빙 완료");
            builder.AppendLine($"- 판매 메뉴: {recipe.DisplayName} x{servings}");
            builder.AppendLine($"- 획득 골드: +{revenue}");
            builder.Append($"- 평판 변화: +{reputationGain}");

            SetResult(builder.ToString());
            BroadcastState();
        }

        private void BroadcastState()
        {
            SelectedRecipeChanged?.Invoke(SelectedRecipe);
            TodayMenuChanged?.Invoke();
            ServiceStateChanged?.Invoke(IsRestaurantOpen);
            ServiceResultChanged?.Invoke(LastServiceResult);
        }

        private void SetResult(string result)
        {
            LastServiceResult = result ?? string.Empty;
        }
    }
}
