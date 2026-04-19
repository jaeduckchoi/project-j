using System;
using System.Collections.Generic;
using Restaurant.Kitchen;
using Shared.Data;
using UnityEngine;

namespace Restaurant
{
    /// <summary>
    /// 카탈로그 생성 시 씬 직렬화 레시피만 사용할지, generated fallback까지 허용할지 구분합니다.
    /// </summary>
    public enum RestaurantRecipeBootstrapMode
    {
        AllowGeneratedFallback,
        AuthoredOnly
    }

    /// <summary>
    /// 허브 영업, 냉장고 기본 재료, CookingUtensils 조리 단계, 완성 요리 데이터를 한 곳에서 해석하는 런타임 카탈로그다.
    /// </summary>
    public sealed class RestaurantRecipeCatalog
    {
        private static readonly (string ResourceId, string DisplayName)[] BasicIngredientDefinitions =
        {
            ("ingredient_001", "김치"),
            ("ingredient_002", "밥"),
            ("ingredient_003", "밀가루"),
            ("ingredient_004", "고춧가루")
        };

        private static readonly (string RecipeId, string DisplayName, string Description, string CookingMethod, int SellPrice, int ReputationDelta, (string Id, string Name, int Quantity)[] Ingredients)[] FallbackRecipeDefinitions =
        {
            ("food_001", "김치볶음밥", "기본 허브 주방 레시피", "후라이팬", 28, 1, new[] { ("ingredient_001", "김치", 1), ("ingredient_002", "밥", 1) }),
            ("food_002", "김치찌개", "기본 허브 주방 레시피", "냄비", 32, 1, new[] { ("ingredient_001", "김치", 1), ("ingredient_004", "고춧가루", 1) }),
            ("food_003", "김치전", "기본 허브 주방 레시피", "후라이팬", 24, 1, new[] { ("ingredient_001", "김치", 1), ("ingredient_003", "밀가루", 1) })
        };

        private readonly List<RecipeData> availableRecipes = new();
        private readonly List<ResourceData> basicIngredients = new();
        private readonly Dictionary<string, ResourceData> basicIngredientById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RecipeData> recipesById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, KitchenDishData> dishesByRecipeId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, KitchenDishData> dishesByFinalSignature = new(StringComparer.Ordinal);
        private readonly Dictionary<StageKey, KitchenStageRecipeData> stagesByKey = new();

        private readonly RestaurantRecipeBootstrapMode bootstrapMode;
        private readonly float autoCookingSeconds;
        private readonly float manualCookingSeconds;

        private RestaurantRecipeCatalog(
            IEnumerable<RecipeData> bootstrapRecipes,
            RestaurantRecipeBootstrapMode mode,
            float autoSeconds,
            float manualSeconds)
        {
            bootstrapMode = mode;
            autoCookingSeconds = Mathf.Max(0.1f, autoSeconds);
            manualCookingSeconds = Mathf.Max(0.1f, manualSeconds);

            EnsureBasicIngredients();
            BuildAvailableRecipes(bootstrapRecipes);
            BuildKitchenArtifacts();
        }

        /// <summary>
        /// 현재 허브 런타임이 공유할 카탈로그를 생성한다.
        /// </summary>
        public static RestaurantRecipeCatalog Create(IEnumerable<RecipeData> bootstrapRecipes, float autoSeconds = 3f, float manualSeconds = 2.5f)
        {
            return Create(
                bootstrapRecipes,
                RestaurantRecipeBootstrapMode.AllowGeneratedFallback,
                autoSeconds,
                manualSeconds);
        }

        /// <summary>
        /// 현재 허브 런타임이 공유할 카탈로그를 생성한다.
        /// </summary>
        public static RestaurantRecipeCatalog Create(
            IEnumerable<RecipeData> bootstrapRecipes,
            RestaurantRecipeBootstrapMode bootstrapMode,
            float autoSeconds = 3f,
            float manualSeconds = 2.5f)
        {
            return new RestaurantRecipeCatalog(bootstrapRecipes, bootstrapMode, autoSeconds, manualSeconds);
        }

        /// <summary>
        /// 현재 허브에서 메뉴 선정에 사용할 레시피 목록이다.
        /// </summary>
        public IReadOnlyList<RecipeData> AvailableRecipes => availableRecipes;

        /// <summary>
        /// 냉장고에서 수량 제한 없이 꺼낼 수 있는 기본 재료 목록이다.
        /// </summary>
        public IReadOnlyList<ResourceData> BasicIngredients => basicIngredients;

        /// <summary>
        /// 레시피 id 기준으로 현재 카탈로그의 레시피를 조회한다.
        /// </summary>
        public bool TryGetRecipe(string recipeId, out RecipeData recipe)
        {
            recipe = null;
            string normalizedRecipeId = NormalizeKey(recipeId);
            return !string.IsNullOrWhiteSpace(normalizedRecipeId)
                && recipesById.TryGetValue(normalizedRecipeId, out recipe)
                && recipe != null;
        }

        /// <summary>
        /// CookingUtensils 입력 시그니처와 기구 타입 기준으로 조리 단계를 조회한다.
        /// </summary>
        public bool TryGetStage(KitchenToolType toolType, string inputSignature, out KitchenStageRecipeData stage)
        {
            string normalizedSignature = NormalizeSignature(inputSignature);
            return stagesByKey.TryGetValue(new StageKey(toolType, normalizedSignature), out stage) && stage != null;
        }

        /// <summary>
        /// 레시피 id 기준으로 서빙 가능한 완성 요리 데이터를 조회한다.
        /// </summary>
        public bool TryGetDish(string recipeId, out KitchenDishData dish)
        {
            dish = null;
            string normalizedRecipeId = NormalizeKey(recipeId);
            return !string.IsNullOrWhiteSpace(normalizedRecipeId)
                && dishesByRecipeId.TryGetValue(normalizedRecipeId, out dish)
                && dish != null;
        }

        /// <summary>
        /// 완성 요리 시그니처 기준으로 레거시 finalize 호환 조회를 수행한다.
        /// </summary>
        public bool TryGetDishByFinalSignature(string finalSignature, out KitchenDishData dish)
        {
            dish = null;
            string normalizedSignature = NormalizeSignature(finalSignature);
            return !string.IsNullOrWhiteSpace(normalizedSignature)
                && dishesByFinalSignature.TryGetValue(normalizedSignature, out dish)
                && dish != null;
        }

        /// <summary>
        /// 지정한 자원이 기본 냉장고 재료인지 확인한다.
        /// </summary>
        public bool IsBasicIngredient(ResourceData resource)
        {
            return resource != null && IsBasicIngredient(resource.ResourceId);
        }

        /// <summary>
        /// 지정한 자원 id가 기본 냉장고 재료인지 확인한다.
        /// </summary>
        public bool IsBasicIngredient(string resourceId)
        {
            string normalizedResourceId = NormalizeKey(resourceId);
            return !string.IsNullOrWhiteSpace(normalizedResourceId) && basicIngredientById.ContainsKey(normalizedResourceId);
        }

        private void EnsureBasicIngredients()
        {
            foreach ((string resourceId, string displayName) in BasicIngredientDefinitions)
            {
                if (basicIngredientById.ContainsKey(resourceId))
                {
                    continue;
                }

                ResourceData resource = GeneratedGameDataLocator.FindGeneratedResource(resourceId, displayName);
                if (resource == null)
                {
                    resource = ScriptableObject.CreateInstance<ResourceData>();
                    resource.name = $"runtime-resource-{resourceId}";
                    resource.hideFlags = HideFlags.HideAndDontSave;
                    resource.ConfigureRuntime(resourceId, displayName, "기본 냉장고 재료", "기본 냉장고", 0, ResourceRarity.Common);
                }

                basicIngredientById[resourceId] = resource;
                basicIngredients.Add(resource);
            }
        }

        private void BuildAvailableRecipes(IEnumerable<RecipeData> bootstrapRecipes)
        {
            AddRecipes(bootstrapRecipes);

            if (bootstrapMode == RestaurantRecipeBootstrapMode.AuthoredOnly)
            {
                return;
            }

            if (availableRecipes.Count == 0)
            {
                AddRecipes(GeneratedGameDataLocator.GetGeneratedRecipes());
            }

            if (availableRecipes.Count > 0)
            {
                return;
            }

            foreach ((string recipeId, string displayName, string description, string cookingMethod, int sellPrice, int reputationDelta, (string Id, string Name, int Quantity)[] ingredients) in FallbackRecipeDefinitions)
            {
                RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
                recipe.name = $"runtime-recipe-{recipeId}";
                recipe.hideFlags = HideFlags.HideAndDontSave;

                List<RecipeIngredient> resolvedIngredients = new();
                foreach ((string ingredientId, string ingredientName, int quantity) in ingredients)
                {
                    basicIngredientById.TryGetValue(ingredientId, out ResourceData basicResource);
                    resolvedIngredients.Add(RecipeIngredient.CreateRuntime(ingredientId, ingredientName, quantity, basicResource));
                }

                recipe.ConfigureRuntime(
                    recipeId,
                    displayName,
                    description,
                    sellPrice,
                    reputationDelta,
                    string.Empty,
                    0,
                    cookingMethod,
                    string.Empty,
                    resolvedIngredients);
                AddRecipe(recipe);
            }
        }

        private void AddRecipes(IEnumerable<RecipeData> recipes)
        {
            if (recipes == null)
            {
                return;
            }

            foreach (RecipeData recipe in recipes)
            {
                AddRecipe(recipe);
            }
        }

        private void AddRecipe(RecipeData recipe)
        {
            if (recipe == null)
            {
                return;
            }

            string recipeId = NormalizeKey(recipe.RecipeId);
            if (string.IsNullOrWhiteSpace(recipeId) || recipesById.ContainsKey(recipeId))
            {
                return;
            }

            recipesById[recipeId] = recipe;
            availableRecipes.Add(recipe);
        }

        private void BuildKitchenArtifacts()
        {
            foreach (RecipeData recipe in availableRecipes)
            {
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    continue;
                }

                if (!TryResolveToolType(recipe.CookingMethod, out KitchenToolType toolType))
                {
                    continue;
                }

                List<KitchenItemRequirement> rawRequirements = BuildRawRequirements(recipe);
                if (rawRequirements.Count == 0)
                {
                    continue;
                }

                KitchenItemData finalDishItem = ScriptableObject.CreateInstance<KitchenItemData>();
                finalDishItem.name = $"runtime-kitchen-item-{recipe.RecipeId}-final";
                finalDishItem.hideFlags = HideFlags.HideAndDontSave;
                finalDishItem.ConfigureRuntime(
                    recipe.RecipeId,
                    recipe.DisplayName,
                    KitchenItemState.FinalDish,
                    null,
                    recipe.RecipeId,
                    recipe.Icon);

                KitchenStageRecipeData stage = ScriptableObject.CreateInstance<KitchenStageRecipeData>();
                stage.name = $"runtime-kitchen-stage-{recipe.RecipeId}";
                stage.hideFlags = HideFlags.HideAndDontSave;
                stage.ConfigureRuntime(
                    $"{recipe.RecipeId}_stage",
                    toolType,
                    toolType == KitchenToolType.CuttingBoard ? KitchenProgressMode.ManualHold : KitchenProgressMode.AutoProgress,
                    toolType == KitchenToolType.CuttingBoard ? manualCookingSeconds : autoCookingSeconds,
                    rawRequirements,
                    finalDishItem);
                stagesByKey[new StageKey(toolType, stage.InputSignature)] = stage;

                KitchenItemRequirement cookedRequirement = new();
                cookedRequirement.ConfigureRuntime(finalDishItem, null, KitchenItemState.FinalDish, 1);

                KitchenDishData dish = ScriptableObject.CreateInstance<KitchenDishData>();
                dish.name = $"runtime-kitchen-dish-{recipe.RecipeId}";
                dish.hideFlags = HideFlags.HideAndDontSave;
                dish.ConfigureRuntime(recipe.RecipeId, recipe.DisplayName, new[] { cookedRequirement }, finalDishItem);

                string normalizedRecipeId = NormalizeKey(recipe.RecipeId);
                dishesByRecipeId[normalizedRecipeId] = dish;
                dishesByFinalSignature[NormalizeSignature(dish.FinalSignature)] = dish;
            }
        }

        private List<KitchenItemRequirement> BuildRawRequirements(RecipeData recipe)
        {
            List<KitchenItemRequirement> requirements = new();
            if (recipe == null || recipe.Ingredients == null)
            {
                return requirements;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                {
                    continue;
                }

                ResourceData resolvedResource = null;
                if (!RecipeIngredient.TryResolve(ingredient, out resolvedResource, out int amount))
                {
                    if (!IsBasicIngredient(ingredient.IngredientId))
                    {
                        requirements.Clear();
                        return requirements;
                    }

                    basicIngredientById.TryGetValue(ingredient.IngredientId, out resolvedResource);
                    amount = ingredient.Quantity;
                }

                if (resolvedResource == null)
                {
                    requirements.Clear();
                    return requirements;
                }

                KitchenItemRequirement requirement = new();
                requirement.ConfigureRuntime(null, resolvedResource, KitchenItemState.Raw, amount);
                requirements.Add(requirement);
            }

            return requirements;
        }

        private static bool TryResolveToolType(string cookingMethod, out KitchenToolType toolType)
        {
            string normalizedMethod = string.IsNullOrWhiteSpace(cookingMethod)
                ? string.Empty
                : cookingMethod.Trim();

            switch (normalizedMethod)
            {
                case "도마":
                case "cuttingboard":
                case "cutting_board":
                case "cutting board":
                    toolType = KitchenToolType.CuttingBoard;
                    return true;
                case "냄비":
                case "pot":
                    toolType = KitchenToolType.Pot;
                    return true;
                case "후라이팬":
                case "프라이팬":
                case "fryingpan":
                case "frying_pan":
                case "frying pan":
                    toolType = KitchenToolType.FryingPan;
                    return true;
                case "튀김기":
                case "fryer":
                    toolType = KitchenToolType.Fryer;
                    return true;
                default:
                    toolType = KitchenToolType.FrontCounter;
                    return false;
            }
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeSignature(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private readonly struct StageKey : IEquatable<StageKey>
        {
            public StageKey(KitchenToolType toolType, string inputSignature)
            {
                ToolType = toolType;
                InputSignature = NormalizeSignature(inputSignature);
            }

            public KitchenToolType ToolType { get; }
            public string InputSignature { get; }

            public bool Equals(StageKey other)
            {
                return ToolType == other.ToolType
                    && string.Equals(InputSignature, other.InputSignature, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is StageKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int)ToolType, InputSignature);
            }
        }
    }
}
