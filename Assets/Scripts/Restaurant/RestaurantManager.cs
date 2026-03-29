using System;
using System.Collections.Generic;
using System.Text;
using Core;
using Data;
using Flow;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// 오후 장사의 메뉴 선택, 재료 소모, 골드와 평판 정산을 처리한다.
namespace Restaurant
{
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "RestaurantManager")]
    public class RestaurantManager : MonoBehaviour
    {
    [SerializeField] private List<RecipeData> availableRecipes = new();
    [SerializeField, Min(1)] private int serviceCapacity = 3;
    [SerializeField] private int selectedRecipeIndex;

    public event Action<RecipeData> SelectedRecipeChanged;
    public event Action<string> ServiceResultChanged;

    public RecipeData SelectedRecipe { get; private set; }
    public string LastServiceResult { get; private set; } = "메뉴를 고르고 영업을 시작하세요.";

    /*
     * 시작 시 기본 레시피 목록을 복구하고 현재 선택 메뉴를 확정합니다.
     */
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

    /*
     * 레시피 인덱스를 기준으로 현재 메뉴를 바꿉니다.
     */
    public void SelectRecipeByIndex(int recipeIndex)
    {
        EnsureRecipeList();

        if (availableRecipes.Count == 0)
        {
            SelectedRecipe = null;
            selectedRecipeIndex = 0;
            BroadcastState();
            return;
        }

        selectedRecipeIndex = Mathf.Clamp(recipeIndex, 0, availableRecipes.Count - 1);
        RefreshSelectedRecipe();
        BroadcastState();
    }

    /*
     * 해당 레시피를 현재 재료로 한 번 이상 판매할 수 있는지 확인합니다.
     */
    public bool CanServe(RecipeData recipe)
    {
        EnsureRecipeList();
        return GetCookableServings(recipe) > 0;
    }

    /*
     * UI 에 표시할 메뉴 목록과 선택 메뉴 상세 문구를 조합합니다.
     */
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
        builder.AppendLine($"- 평판: +{Mathf.Max(0, SelectedRecipe.ReputationDelta)}");
        builder.AppendLine("- 필요 재료");

        foreach (RecipeIngredient ingredient in SelectedRecipe.Ingredients)
        {
            if (ingredient == null || ingredient.Resource == null || ingredient.Amount <= 0)
            {
                continue;
            }

            int ownedAmount = GameManager.Instance != null && GameManager.Instance.Inventory != null
                ? GameManager.Instance.Inventory.GetAmount(ingredient.Resource)
                : 0;

            builder.AppendLine($"  {ingredient.Resource.DisplayName} {ownedAmount}/{ingredient.Amount}");
        }

        return builder.ToString().TrimEnd();
    }

    /*
     * 현재 보유 재료로 몇 인분까지 판매 가능한지 계산합니다.
     */
    public int GetCookableServings(RecipeData recipe)
    {
        EnsureRecipeList();

        if (recipe == null || GameManager.Instance == null || GameManager.Instance.Inventory == null)
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
            if (ingredient == null || ingredient.Resource == null || ingredient.Amount <= 0)
            {
                return 0;
            }

            int ownedAmount = GameManager.Instance.Inventory.GetAmount(ingredient.Resource);
            servings = Mathf.Min(servings, ownedAmount / ingredient.Amount);
        }

        return servings == int.MaxValue ? 0 : servings;
    }

    /*
     * 현재 선택 메뉴를 기준으로 재료를 소모하고 판매 결과를 정산합니다.
     */
    public void RunServiceForSelectedRecipe()
    {
        EnsureRecipeList();
        RefreshSelectedRecipe();

        if (GameManager.Instance != null && GameManager.Instance.DayCycle != null && GameManager.Instance.DayCycle.CurrentPhase != DayPhase.AfternoonService)
        {
            SetResult("아직 장사 시간이 아닙니다.");
            return;
        }

        if (SelectedRecipe == null)
        {
            SetResult("선택된 메뉴가 없습니다.");
            return;
        }

        int cookableServings = GetCookableServings(SelectedRecipe);
        int servingsToSell = Mathf.Min(serviceCapacity, cookableServings);

        if (servingsToSell <= 0)
        {
            SetResult($"{SelectedRecipe.DisplayName} 재료가 부족합니다.");
            return;
        }

        // 실제 판매 인분 수에 맞춰 재료를 차감합니다.
        foreach (RecipeIngredient ingredient in SelectedRecipe.Ingredients)
        {
            GameManager.Instance.Inventory.TryRemove(ingredient.Resource, ingredient.Amount * servingsToSell);
        }

        int revenue = SelectedRecipe.SellPrice * servingsToSell;
        int reputationGain = Mathf.Max(0, SelectedRecipe.ReputationDelta * servingsToSell);

        if (GameManager.Instance != null && GameManager.Instance.Economy != null)
        {
            GameManager.Instance.Economy.AddGold(revenue);
            GameManager.Instance.Economy.AddReputation(reputationGain);
        }

        StringBuilder builder = new();
        builder.AppendLine("오늘 영업 결과");
        builder.AppendLine($"- 판매 메뉴: {SelectedRecipe.DisplayName} x{servingsToSell}");
        builder.AppendLine($"- 소모 재료: {BuildIngredientUsageText(SelectedRecipe, servingsToSell)}");
        builder.AppendLine($"- 획득 골드: +{revenue}");
        builder.Append($"- 평판 변화: +{reputationGain}");

        SetResult(builder.ToString());
        GameManager.Instance?.DayCycle?.CompleteService(LastServiceResult);
        BroadcastState();
    }

    /*
     * 장사 단계를 직접 스킵하고 DayCycleManager 쪽 정산 로직으로 넘깁니다.
     */
    public void SkipService()
    {
        SetResult("오늘 영업 결과\n- 장사를 건너뛰었습니다.");
        GameManager.Instance?.DayCycle?.SkipService();
        BroadcastState();
    }

    /*
     * 현재 인덱스와 레시피 목록을 기준으로 선택 메뉴를 다시 맞춥니다.
     */
    private void RefreshSelectedRecipe()
    {
        EnsureRecipeList();

        if (availableRecipes.Count == 0)
        {
            SelectedRecipe = null;
            selectedRecipeIndex = 0;
            return;
        }

        selectedRecipeIndex = Mathf.Clamp(selectedRecipeIndex, 0, availableRecipes.Count - 1);
        SelectedRecipe = availableRecipes[selectedRecipeIndex];
    }

    /*
     * 씬 참조가 비어 있어도 기본 레시피 에셋을 런타임에 다시 채웁니다.
     */
    private void EnsureRecipeList()
    {
        availableRecipes ??= new List<RecipeData>();
        availableRecipes.RemoveAll(recipe => recipe == null);

        TryAddDefaultRecipe("SushiSet", "스시 세트");
        TryAddDefaultRecipe("SeafoodSoup", "해물탕");
        TryAddDefaultRecipe("HerbFishSoup", "약초 생선탕");
        TryAddDefaultRecipe("ForestBasket", "숲 바구니");
        TryAddDefaultRecipe("GlowMossStew", "발광 이끼 수프");
        TryAddDefaultRecipe("WindHerbSalad", "향초 샐러드");
    }

    /*
     * generated 데이터에서 레시피를 찾아 목록에 중복 없이 추가합니다.
     */
    private void TryAddDefaultRecipe(string assetName, params string[] alternateKeys)
    {
        RecipeData recipe = GeneratedGameDataLocator.FindGeneratedRecipe(assetName, alternateKeys);
        if (recipe != null && !availableRecipes.Contains(recipe))
        {
            availableRecipes.Add(recipe);
        }
    }

    /*
     * 선택 메뉴와 결과 텍스트 변경 이벤트를 한 번에 전달합니다.
     */
    private void BroadcastState()
    {
        SelectedRecipeChanged?.Invoke(SelectedRecipe);
        ServiceResultChanged?.Invoke(LastServiceResult);
    }

    /*
     * 마지막 장사 결과 문자열을 저장하고 구독자에게 알립니다.
     */
    private void SetResult(string result)
    {
        LastServiceResult = result;
        ServiceResultChanged?.Invoke(LastServiceResult);
    }

    /*
     * 결과 화면용 재료 소모 문자열을 생성합니다.
     */
    private static string BuildIngredientUsageText(RecipeData recipe, int servings)
    {
        if (recipe == null || servings <= 0 || recipe.Ingredients == null || recipe.Ingredients.Count == 0)
        {
            return "없음";
        }

        List<string> parts = new();

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            if (ingredient == null || ingredient.Resource == null || ingredient.Amount <= 0)
            {
                continue;
            }

            parts.Add($"{ingredient.Resource.DisplayName} x{ingredient.Amount * servings}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "없음";
    }
    }
}
