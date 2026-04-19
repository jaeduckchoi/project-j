using System.Collections.Generic;
using System.Reflection;
using CoreLoop.Core;
using Management.Economy;
using Management.Inventory;
using NUnit.Framework;
using Restaurant;
using Restaurant.Kitchen;
using Shared.Data;
using UnityEngine;

namespace Editor.Tests
{
    public class RestaurantMenuFlowEditModeTests
    {
        private const BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStaticFlags = BindingFlags.Static | BindingFlags.NonPublic;

        private readonly List<Object> cleanupTargets = new();

        [TearDown]
        public void TearDown()
        {
            ResetSingleton(typeof(GameManager));
            ResetSingleton(typeof(RestaurantFlowController));

            for (int index = cleanupTargets.Count - 1; index >= 0; index--)
            {
                Object target = cleanupTargets[index];
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            cleanupTargets.Clear();
        }

        [Test]
        public void RestaurantManager_OpenAndClose_RespectMenuCompletionAndActiveTickets()
        {
            RestaurantManager restaurantManager = CreateGameObject("RestaurantManager").AddComponent<RestaurantManager>();

            IReadOnlyList<RecipeData> recipes = restaurantManager.AvailableRecipes;
            Assert.That(recipes.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(restaurantManager.CanOpenRestaurant, Is.False);

            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]);
            restaurantManager.SelectTodayMenuSlot(1);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[1]);
            restaurantManager.SelectTodayMenuSlot(2);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[2]);

            Assert.That(restaurantManager.CanOpenRestaurant, Is.True);
            Assert.That(restaurantManager.TryOpenRestaurant(), Is.True);
            Assert.That(restaurantManager.IsRestaurantOpen, Is.True);
            Assert.That(restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]), Is.False);
            Assert.That(restaurantManager.TryCloseRestaurant(1), Is.False);
            Assert.That(restaurantManager.IsRestaurantOpen, Is.True);
            Assert.That(restaurantManager.TryCloseRestaurant(0), Is.True);
            Assert.That(restaurantManager.IsRestaurantOpen, Is.False);
        }

        [Test]
        public void RestaurantFlowController_GetRandomTodayDish_UsesOnlyTodayMenuRecipes()
        {
            RestaurantManager restaurantManager = CreateGameObject("RestaurantManager").AddComponent<RestaurantManager>();
            IReadOnlyList<RecipeData> recipes = restaurantManager.AvailableRecipes;

            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]);
            restaurantManager.SelectTodayMenuSlot(1);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[1]);
            restaurantManager.SelectTodayMenuSlot(2);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[2]);
            restaurantManager.TryOpenRestaurant();

            RestaurantFlowController flowController = CreateGameObject("RestaurantFlowController").AddComponent<RestaurantFlowController>();
            HashSet<string> todayMenuIds = new();
            for (int index = 0; index < restaurantManager.TodayMenuRecipes.Count; index++)
            {
                RecipeData recipe = restaurantManager.TodayMenuRecipes[index];
                if (recipe != null)
                {
                    todayMenuIds.Add(recipe.RecipeId);
                }
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                KitchenDishData dish = flowController.GetRandomTodayDish();
                Assert.That(dish, Is.Not.Null);
                Assert.That(todayMenuIds.Contains(dish.RecipeId), Is.True, $"오늘의 메뉴가 아닌 요리 {dish.RecipeId} 가 후보로 선택되었습니다.");
            }
        }

        [Test]
        public void RestaurantFlowController_AddStationIfMissing_InstallsStationOnInactiveSceneObject()
        {
            GameObject tableCollider = CreateGameObject("HubTableTopCollider");
            tableCollider.SetActive(false);

            InvokePrivateStaticMethod(
                typeof(RestaurantFlowController),
                "AddStationIfMissing",
                "HubTableTopCollider",
                typeof(DiningTableStation));

            Assert.That(tableCollider.GetComponent<DiningTableStation>(), Is.Not.Null);
        }

        [Test]
        public void TryRecordCompletedOrder_AddsRewardsWithoutConsumingIngredientsAgain()
        {
            GameManager gameManager = CreateConfiguredGameManager();
            InventoryManager inventoryManager = gameManager.Inventory;
            EconomyManager economyManager = gameManager.Economy;

            ResourceData fish = CreateResource("fish", "생선");
            List<RecipeData> recipes = new()
            {
                CreateRecipe("custom_fish_set", "생선 세트", fish, 21, 2),
                CreateRecipe("custom_fish_soup", "생선 수프", fish, 25, 3),
                CreateRecipe("custom_fish_roast", "생선 구이", fish, 29, 4)
            };

            inventoryManager.TryAdd(fish, 10, out _);

            RestaurantManager restaurantManager = CreateGameObject("RestaurantManager").AddComponent<RestaurantManager>();
            SetPrivateField(restaurantManager, "availableRecipes", recipes);

            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]);
            restaurantManager.SelectTodayMenuSlot(1);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[1]);
            restaurantManager.SelectTodayMenuSlot(2);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[2]);
            Assert.That(restaurantManager.TryOpenRestaurant(), Is.True);

            int goldBefore = economyManager.CurrentGold;
            int reputationBefore = economyManager.CurrentReputation;
            int inventoryBefore = inventoryManager.GetAmount(fish);

            Assert.That(restaurantManager.TryRecordCompletedOrder(recipes[0].RecipeId), Is.True);
            Assert.That(economyManager.CurrentGold, Is.EqualTo(goldBefore + recipes[0].SellPrice));
            Assert.That(economyManager.CurrentReputation, Is.EqualTo(reputationBefore + recipes[0].ReputationDelta));
            Assert.That(inventoryManager.GetAmount(fish), Is.EqualTo(inventoryBefore));
        }

        private GameManager CreateConfiguredGameManager()
        {
            GameObject gameManagerObject = CreateGameObject("GameManager");
            gameManagerObject.AddComponent<InventoryManager>();
            gameManagerObject.AddComponent<EconomyManager>();
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
            InvokePrivateMethod(gameManager, "Awake");
            return gameManager;
        }

        private ResourceData CreateResource(string resourceId, string displayName)
        {
            ResourceData resource = ScriptableObject.CreateInstance<ResourceData>();
            resource.hideFlags = HideFlags.HideAndDontSave;
            resource.ConfigureRuntime(resourceId, displayName, string.Empty, "테스트", 10, ResourceRarity.Common);
            cleanupTargets.Add(resource);
            return resource;
        }

        private RecipeData CreateRecipe(string recipeId, string displayName, ResourceData resource, int sellPrice, int reputationDelta)
        {
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.hideFlags = HideFlags.HideAndDontSave;
            recipe.ConfigureRuntime(
                recipeId,
                displayName,
                "테스트 레시피",
                sellPrice,
                reputationDelta,
                string.Empty,
                0,
                "조리",
                string.Empty,
                new[]
                {
                    RecipeIngredient.CreateRuntime(resource.ResourceId, resource.DisplayName, 1, resource)
                });
            cleanupTargets.Add(recipe);
            return recipe;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            cleanupTargets.Add(gameObject);
            return gameObject;
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstanceFlags);
            Assert.That(method, Is.Not.Null, $"{methodName} 를 찾을 수 없습니다.");
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceFlags);
            Assert.That(field, Is.Not.Null, $"{fieldName} 필드를 찾을 수 없습니다.");
            field.SetValue(target, value);
        }

        private static void InvokePrivateStaticMethod(System.Type targetType, string methodName, params object[] args)
        {
            MethodInfo method = targetType.GetMethod(methodName, PrivateStaticFlags);
            Assert.That(method, Is.Not.Null, $"{methodName} 를 찾을 수 없습니다.");
            method.Invoke(null, args);
        }

        private static void ResetSingleton(System.Type targetType)
        {
            FieldInfo field = targetType.GetField("<Instance>k__BackingField", PrivateStaticFlags);
            if (field != null)
            {
                field.SetValue(null, null);
            }
        }
    }
}
