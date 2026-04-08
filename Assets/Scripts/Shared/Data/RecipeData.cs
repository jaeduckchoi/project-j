using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

// Data 네임스페이스
namespace Shared.Data
{
    /// <summary>
    /// 식당에서 판매 가능한 메뉴의 이름, 설명, 가격과 필요 재료를 담는 데이터 에셋이다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "recipe-data",
        menuName = "Jonggu Restaurant/Data/Recipe",
        order = 1)]
    [MovedFrom(false, sourceNamespace: "Data", sourceAssembly: "Assembly-CSharp", sourceClassName: "RecipeData")]
    public class RecipeData : ScriptableObject
    {
        // 레시피 식별자와 기본 표시 정보다.
        [SerializeField] private string recipeId = "recipe_id";
        [SerializeField] private string displayName = "새 메뉴";
        [SerializeField, TextArea] private string description = "메뉴 설명";
        [SerializeField, Min(1)] private int sellPrice = 25;
        [SerializeField] private int reputationDelta = 1;
        [SerializeField] private string supplySource = string.Empty;
        [SerializeField] private int difficulty;
        [SerializeField] private string cookingMethod = string.Empty;
        [SerializeField] private string memo = string.Empty;

        // 장사 계산에 사용할 재료 목록이다.
        [SerializeField] private List<RecipeIngredient> ingredients = new();

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public string Description => description;
        public int SellPrice => sellPrice;
        public int ReputationDelta => reputationDelta;
        public string SupplySource => supplySource;
        public int Difficulty => difficulty;
        public string CookingMethod => cookingMethod;
        public string Memo => memo;
        public IReadOnlyList<RecipeIngredient> Ingredients => ingredients;

        /// <summary>
        /// 서버 bootstrap 레시피를 런타임 표시용 ScriptableObject에 그대로 반영합니다.
        /// </summary>
        public void ConfigureRuntime(
            string id,
            string recipeName,
            string recipeDescription,
            int price,
            int reputation,
            string source,
            int recipeDifficulty,
            string method,
            string note,
            IEnumerable<RecipeIngredient> recipeIngredients)
        {
            recipeId = string.IsNullOrWhiteSpace(id) ? string.Empty : id;
            displayName = string.IsNullOrWhiteSpace(recipeName) ? recipeId : recipeName;
            description = string.IsNullOrWhiteSpace(recipeDescription) ? string.Empty : recipeDescription;
            sellPrice = Mathf.Max(0, price);
            reputationDelta = Mathf.Max(0, reputation);
            supplySource = string.IsNullOrWhiteSpace(source) ? string.Empty : source;
            difficulty = Mathf.Max(0, recipeDifficulty);
            cookingMethod = string.IsNullOrWhiteSpace(method) ? string.Empty : method;
            memo = string.IsNullOrWhiteSpace(note) ? string.Empty : note;

            ingredients.Clear();
            if (recipeIngredients == null)
            {
                return;
            }

            foreach (RecipeIngredient ingredient in recipeIngredients)
            {
                if (ingredient != null)
                {
                    ingredients.Add(ingredient);
                }
            }
        }
    }

    /// <summary>
    /// 레시피 한 개에 들어가는 자원과 수량 한 쌍이다.
    /// </summary>
    [Serializable]
    public class RecipeIngredient
    {
        [SerializeField] private string ingredientId = string.Empty;
        [SerializeField] private string ingredientName = string.Empty;
        [SerializeField] private int difficulty;
        [SerializeField] private string supplySource = string.Empty;
        [SerializeField] private string acquisitionSource = string.Empty;
        [SerializeField] private string acquisitionMethod = string.Empty;
        [SerializeField] private string acquisitionTool = string.Empty;
        [SerializeField] private int buyPrice;
        [SerializeField] private int sellPrice;
        [SerializeField] private string memo = string.Empty;
        [FormerlySerializedAs("Resource")] public ResourceData resource;
        [FormerlySerializedAs("Amount"), Min(1)] public int amount = 1;

        public string IngredientId => !string.IsNullOrWhiteSpace(ingredientId)
            ? ingredientId
            : resource != null
                ? resource.ResourceId
                : string.Empty;

        public string IngredientName => !string.IsNullOrWhiteSpace(ingredientName)
            ? ingredientName
            : resource != null
                ? resource.DisplayName
                : IngredientId;

        public int Quantity => Mathf.Max(1, amount);
        public int Difficulty => Mathf.Max(0, difficulty);
        public string SupplySource => supplySource;
        public string AcquisitionSource => acquisitionSource;
        public string AcquisitionMethod => acquisitionMethod;
        public string AcquisitionTool => acquisitionTool;
        public int BuyPrice => Mathf.Max(0, buyPrice);
        public int SellPrice => Mathf.Max(0, sellPrice);
        public string Memo => memo;

        /// <summary>
        /// 서버 bootstrap의 ingredientId/name/quantity 구조를 그대로 담는 재료 항목을 만듭니다.
        /// </summary>
        public static RecipeIngredient CreateRuntime(string id, string displayName, int quantity, ResourceData resolvedResource = null)
        {
            return new RecipeIngredient
            {
                ingredientId = string.IsNullOrWhiteSpace(id) ? string.Empty : id,
                ingredientName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName,
                resource = resolvedResource,
                amount = Mathf.Max(1, quantity)
            };
        }

        /// <summary>
        /// bootstrap.ingredients 카탈로그에서 내려온 부가 정보를 재료 항목에 보존합니다.
        /// </summary>
        public void ConfigureCatalogMetadata(
            int ingredientDifficulty,
            string source,
            string acquisition,
            string method,
            string tool,
            int buy,
            int sell,
            string note)
        {
            difficulty = Mathf.Max(0, ingredientDifficulty);
            supplySource = string.IsNullOrWhiteSpace(source) ? string.Empty : source;
            acquisitionSource = string.IsNullOrWhiteSpace(acquisition) ? string.Empty : acquisition;
            acquisitionMethod = string.IsNullOrWhiteSpace(method) ? string.Empty : method;
            acquisitionTool = string.IsNullOrWhiteSpace(tool) ? string.Empty : tool;
            buyPrice = Mathf.Max(0, buy);
            sellPrice = Mathf.Max(0, sell);
            memo = string.IsNullOrWhiteSpace(note) ? string.Empty : note;
        }

        public string BuildCatalogSummary()
        {
            List<string> parts = new();
            if (Difficulty > 0)
            {
                parts.Add($"난이도 {Difficulty}");
            }

            if (!string.IsNullOrWhiteSpace(SupplySource))
            {
                parts.Add(SupplySource);
            }
            else if (!string.IsNullOrWhiteSpace(AcquisitionSource))
            {
                parts.Add(AcquisitionSource);
            }

            if (!string.IsNullOrWhiteSpace(AcquisitionMethod))
            {
                parts.Add(AcquisitionMethod);
            }

            if (!string.IsNullOrWhiteSpace(AcquisitionTool))
            {
                parts.Add(AcquisitionTool);
            }

            if (BuyPrice > 0)
            {
                parts.Add($"구매 {BuyPrice}");
            }

            if (SellPrice > 0)
            {
                parts.Add($"판매 {SellPrice}");
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// 직렬화 데이터가 비정상이더라도 재료 참조와 수량을 안전하게 꺼낸다.
        /// </summary>
        public static bool TryResolve(RecipeIngredient ingredient, out ResourceData resolvedResource, out int resolvedAmount)
        {
            resolvedResource = default;
            resolvedAmount = 0;

            if (ReferenceEquals(ingredient, null))
            {
                return false;
            }

            ResourceData candidateResource = ingredient.resource;
            if (ReferenceEquals(candidateResource, null))
            {
                candidateResource = GeneratedGameDataLocator.FindGeneratedResource(
                    ingredient.ingredientId,
                    ingredient.ingredientName);
            }

            if (ReferenceEquals(candidateResource, null))
            {
                return false;
            }

            resolvedResource = candidateResource;
            resolvedAmount = ingredient.Quantity;
            return true;
        }
    }
}
