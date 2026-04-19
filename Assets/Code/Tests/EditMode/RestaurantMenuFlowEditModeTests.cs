using System.Collections.Generic;
using System.Reflection;
using CoreLoop.Core;
using Exploration.World;
using Management.Economy;
using Management.Inventory;
using NUnit.Framework;
using Restaurant;
using Restaurant.Kitchen;
using Shared;
using Shared.Data;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            ResetStaticProperty(typeof(GameManager), "Instance");
            ResetStaticProperty(typeof(RestaurantFlowController), "Instance");
            ResetStaticProperty(typeof(HubRuntimeContext), "Active");

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
        public void RestaurantRecipeCatalog_BuildsStageAndFinalDishFromSameRecipeDefinition()
        {
            ResourceData fish = CreateResource("fish", "생선");
            RecipeData recipe = CreateRecipe("custom_fish_soup", "생선 수프", "냄비", fish, 25, 3);
            RestaurantRecipeCatalog catalog = RestaurantRecipeCatalog.Create(new[] { recipe });

            Assert.That(catalog.TryGetRecipe(recipe.RecipeId, out RecipeData resolvedRecipe), Is.True);
            Assert.That(resolvedRecipe, Is.SameAs(recipe));

            KitchenItemRequirement requirement = new();
            requirement.ConfigureRuntime(null, fish, KitchenItemState.Raw, 1);
            string expectedStageSignature = KitchenSignatureUtility.BuildSignature(new[] { requirement });

            Assert.That(catalog.TryGetStage(KitchenToolType.Pot, expectedStageSignature, out KitchenStageRecipeData stage), Is.True);
            Assert.That(stage.OutputItem, Is.Not.Null);
            Assert.That(catalog.TryGetDish(recipe.RecipeId, out KitchenDishData dish), Is.True);
            Assert.That(dish.FinalDishItem, Is.Not.Null);
            Assert.That(dish.FinalDishItem.RecipeId, Is.EqualTo(recipe.RecipeId));

            KitchenItemRequirement finalRequirement = new();
            finalRequirement.ConfigureRuntime(dish.FinalDishItem, null, KitchenItemState.FinalDish, 1);
            Assert.That(dish.FinalSignature, Is.EqualTo(KitchenSignatureUtility.BuildSignature(new[] { finalRequirement })));
        }

        [Test]
        public void RestaurantRecipeCatalog_AuthoredOnly_DoesNotExpandMissingBootstrapToGeneratedRecipes()
        {
            RestaurantRecipeCatalog catalog = RestaurantRecipeCatalog.Create(
                new RecipeData[] { null },
                RestaurantRecipeBootstrapMode.AuthoredOnly);

            Assert.That(catalog.AvailableRecipes, Is.Empty);
        }

        [Test]
        public void InventoryReservationService_ReleasesOnReturnAndConsumesReservedInputsOnlyOnce()
        {
            GameManager gameManager = CreateConfiguredGameManager();
            InventoryManager inventoryManager = gameManager.Inventory;
            ResourceData fish = CreateResource("fish", "생선");
            inventoryManager.TryAdd(fish, 3, out _);

            InventoryReservationService service = new();
            Assert.That(service.TryReserve(inventoryManager, fish, 2, out KitchenCarryItem reservedItem), Is.True);
            Assert.That(service.GetReservedAmount(fish), Is.EqualTo(2));
            Assert.That(service.GetAvailableAmount(inventoryManager, fish), Is.EqualTo(1));

            service.Release(reservedItem);
            Assert.That(service.GetReservedAmount(fish), Is.EqualTo(0));
            Assert.That(inventoryManager.GetAmount(fish), Is.EqualTo(3));

            Assert.That(service.TryReserve(inventoryManager, fish, 2, out KitchenCarryItem secondReservation), Is.True);
            KitchenBundle bundle = new(new[] { secondReservation });

            Assert.That(service.ConsumeReservedInputs(inventoryManager, bundle), Is.True);
            Assert.That(inventoryManager.GetAmount(fish), Is.EqualTo(1));
            Assert.That(service.GetReservedAmount(fish), Is.EqualTo(0));
        }

        [Test]
        public void RestaurantFlowController_GetRandomTodayDish_UsesOnlyTodayMenuRecipes()
        {
            GameObject root = CreateGameObject("RestaurantRoot");
            RestaurantManager restaurantManager = root.AddComponent<RestaurantManager>();
            RestaurantFlowController flowController = root.AddComponent<RestaurantFlowController>();
            InvokePrivateMethod(flowController, "Awake");

            IReadOnlyList<RecipeData> recipes = restaurantManager.AvailableRecipes;
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]);
            restaurantManager.SelectTodayMenuSlot(1);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[1]);
            restaurantManager.SelectTodayMenuSlot(2);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[2]);
            restaurantManager.TryOpenRestaurant();

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
        public void CustomerServiceController_TryServeHeldDish_UsesMatchingTableOnly()
        {
            GameManager gameManager = CreateConfiguredGameManager();
            EconomyManager economyManager = gameManager.Economy;

            ResourceData fish = CreateResource("fish", "생선");
            List<RecipeData> recipes = new()
            {
                CreateRecipe("custom_fish_set", "생선 세트", "후라이팬", fish, 21, 2),
                CreateRecipe("custom_fish_soup", "생선 수프", "냄비", fish, 25, 3),
                CreateRecipe("custom_fish_roast", "생선 구이", "후라이팬", fish, 29, 4)
            };

            GameObject root = CreateGameObject("RestaurantRoot");
            RestaurantManager restaurantManager = root.AddComponent<RestaurantManager>();
            SetPrivateField(restaurantManager, "availableRecipes", recipes);
            RestaurantFlowController flowController = root.AddComponent<RestaurantFlowController>();
            CustomerServiceController serviceController = root.AddComponent<CustomerServiceController>();

            DiningTableStation tableTop = CreateTable("top");
            DiningTableStation tableMiddle = CreateTable("middle");
            DiningTableStation tableBottom = CreateTable("bottom");

            HubRuntimeContext context = CreateValidHubRuntimeContext(
                root,
                restaurantManager,
                flowController,
                serviceController,
                tableTop,
                tableMiddle,
                tableBottom);

            InvokePrivateMethod(flowController, "Awake");
            InvokePrivateMethod(serviceController, "Awake");

            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]);
            restaurantManager.SelectTodayMenuSlot(1);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[1]);
            restaurantManager.SelectTodayMenuSlot(2);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[2]);
            Assert.That(restaurantManager.TryOpenRestaurant(), Is.True);

            RestaurantRecipeCatalog catalog = context.RecipeCatalog;
            Assert.That(catalog.TryGetDish(recipes[1].RecipeId, out KitchenDishData servedDish), Is.True);
            flowController.Carry.TryHold(KitchenCarryItem.FromKitchenItem(servedDish.FinalDishItem));

            Dictionary<string, OrderTicket> ticketsByTableId = GetPrivateField<Dictionary<string, OrderTicket>>(serviceController, "ticketsByTableId");
            ticketsByTableId[tableTop.TableId] = new OrderTicket("ticket-top", tableTop.TableId, servedDish, 10f);
            ticketsByTableId[tableMiddle.TableId] = new OrderTicket("ticket-middle", tableMiddle.TableId, servedDish, 10f);
            InvokePrivateMethod(serviceController, "RaiseTicketsChanged");

            int goldBefore = economyManager.CurrentGold;
            int reputationBefore = economyManager.CurrentReputation;

            Assert.That(serviceController.TryServeHeldDish(tableBottom), Is.False);
            Assert.That(flowController.Carry.HeldItem, Is.Not.Null);
            Assert.That(ticketsByTableId.ContainsKey(tableTop.TableId), Is.True);
            Assert.That(ticketsByTableId.ContainsKey(tableMiddle.TableId), Is.True);

            Assert.That(serviceController.TryServeHeldDish(tableMiddle), Is.True);
            Assert.That(flowController.Carry.HeldItem, Is.Null);
            Assert.That(ticketsByTableId.ContainsKey(tableTop.TableId), Is.True);
            Assert.That(ticketsByTableId.ContainsKey(tableMiddle.TableId), Is.False);
            Assert.That(economyManager.CurrentGold, Is.EqualTo(goldBefore + recipes[1].SellPrice));
            Assert.That(economyManager.CurrentReputation, Is.EqualTo(reputationBefore + recipes[1].ReputationDelta));
        }

        [Test]
        public void HubRuntimeContext_Validate_FailsWhenRequiredReferencesAreMissing()
        {
            HubRuntimeContext context = CreateGameObject("HubRuntimeContext").AddComponent<HubRuntimeContext>();

            Assert.That(context.Validate(out string errorMessage), Is.False);
            Assert.That(errorMessage, Does.Contain("restaurantManager"));
            Assert.That(errorMessage, Does.Contain("diningTables(3)"));
        }

        [Test]
        public void HubScene_RuntimeContext_IsFullyBound()
        {
            string previousScenePath = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(ProjectAssetPaths.HubScenePath, OpenSceneMode.Single);

            try
            {
                HubRuntimeContext context = Object.FindFirstObjectByType<HubRuntimeContext>();
                Assert.That(context, Is.Not.Null, "Hub 씬에 HubRuntimeContext 가 없습니다.");
                Assert.That(context.Validate(out string errorMessage), Is.True, errorMessage);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        [Test]
        public void HubScene_RecipeCatalog_UsesExactThreeRecipeWhitelist()
        {
            string previousScenePath = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(ProjectAssetPaths.HubScenePath, OpenSceneMode.Single);

            try
            {
                HubRuntimeContext context = Object.FindFirstObjectByType<HubRuntimeContext>();
                Assert.That(context, Is.Not.Null, "Hub 씬에 HubRuntimeContext 가 없습니다.");

                IReadOnlyList<RecipeData> recipes = context.RecipeCatalog.AvailableRecipes;
                Assert.That(recipes.Count, Is.EqualTo(3));

                List<string> recipeIds = new();
                for (int index = 0; index < recipes.Count; index++)
                {
                    recipeIds.Add(recipes[index] != null ? recipes[index].RecipeId : string.Empty);
                }

                CollectionAssert.AreEqual(
                    new[] { "food_001", "food_002", "food_003" },
                    recipeIds);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        [Test]
        public void ScenePortal_BlocksLeavingHubWhileRestaurantIsOpen()
        {
            GameManager gameManager = CreateConfiguredGameManager();
            SetPrivateField(gameManager, "hubSceneName", SceneManager.GetActiveScene().name);

            GameObject root = CreateGameObject("RestaurantRoot");
            RestaurantManager restaurantManager = root.AddComponent<RestaurantManager>();
            RestaurantFlowController flowController = root.AddComponent<RestaurantFlowController>();
            CustomerServiceController serviceController = root.AddComponent<CustomerServiceController>();

            DiningTableStation tableTop = CreateTable("top");
            DiningTableStation tableMiddle = CreateTable("middle");
            DiningTableStation tableBottom = CreateTable("bottom");

            CreateValidHubRuntimeContext(
                root,
                restaurantManager,
                flowController,
                serviceController,
                tableTop,
                tableMiddle,
                tableBottom);

            IReadOnlyList<RecipeData> recipes = restaurantManager.AvailableRecipes;
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]);
            restaurantManager.SelectTodayMenuSlot(1);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[1]);
            restaurantManager.SelectTodayMenuSlot(2);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[2]);

            ScenePortal portal = CreateGameObject("BeachPortal").AddComponent<ScenePortal>();
            portal.Configure("Beach", string.Empty, "바닷가로 나가기");

            Assert.That(portal.InteractionPrompt, Is.EqualTo("[E] 바닷가로 나가기"));
            Assert.That(restaurantManager.TryOpenRestaurant(), Is.True);
            Assert.That(portal.InteractionPrompt, Is.EqualTo("영업 중에는 이동할 수 없습니다."));
            Assert.That(restaurantManager.TryCloseRestaurant(0), Is.True);
            Assert.That(portal.InteractionPrompt, Is.EqualTo("[E] 바닷가로 나가기"));
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
                CreateRecipe("custom_fish_set", "생선 세트", "후라이팬", fish, 21, 2),
                CreateRecipe("custom_fish_soup", "생선 수프", "냄비", fish, 25, 3),
                CreateRecipe("custom_fish_roast", "생선 구이", "후라이팬", fish, 29, 4)
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
            InventoryManager inventoryManager = gameManagerObject.AddComponent<InventoryManager>();
            EconomyManager economyManager = gameManagerObject.AddComponent<EconomyManager>();
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
            SetPrivateField(gameManager, "inventoryManager", inventoryManager);
            SetPrivateField(gameManager, "economyManager", economyManager);
            inventoryManager.InitializeIfNeeded();
            economyManager.InitializeIfNeeded();
            SetStaticProperty(typeof(GameManager), "Instance", gameManager);
            return gameManager;
        }

        private DiningTableStation CreateTable(string tableId)
        {
            GameObject tableObject = CreateGameObject($"Table-{tableId}");
            DiningTableStation tableStation = tableObject.AddComponent<DiningTableStation>();
            SetPrivateField(tableStation, "tableId", tableId);
            return tableStation;
        }

        private HubRuntimeContext CreateValidHubRuntimeContext(
            GameObject root,
            RestaurantManager restaurantManager,
            RestaurantFlowController flowController,
            CustomerServiceController serviceController,
            DiningTableStation tableTop,
            DiningTableStation tableMiddle,
            DiningTableStation tableBottom)
        {
            HubRuntimeContext context = root.AddComponent<HubRuntimeContext>();

            SetPrivateField(context, "restaurantManager", restaurantManager);
            SetPrivateField(context, "restaurantFlowController", flowController);
            SetPrivateField(context, "customerServiceController", serviceController);
            SetPrivateField(context, "diningTables", new List<DiningTableStation> { tableTop, tableMiddle, tableBottom });
            SetPrivateField(context, "refrigeratorStation", CreateGameObject("Refrigerator").AddComponent<RefrigeratorStation>());
            SetPrivateField(context, "frontCounterStation", CreateGameObject("PassCounter").AddComponent<ServiceCounterStation>());
            SetPrivateField(context, "cuttingBoardStation", CreateGameObject("CuttingBoard").AddComponent<ServiceCounterStation>());
            SetPrivateField(context, "potStation", CreateGameObject("Pot").AddComponent<ServiceCounterStation>());
            SetPrivateField(context, "fryingPanStation", CreateGameObject("FryingPan").AddComponent<ServiceCounterStation>());
            SetPrivateField(context, "fryerStation", CreateGameObject("Fryer").AddComponent<ServiceCounterStation>());
            SetPrivateField(context, "cuttingBoardGauge", CreateGameObject("CuttingBoardGauge").AddComponent<ToolGaugePresenter>());
            SetPrivateField(context, "potGauge", CreateGameObject("PotGauge").AddComponent<ToolGaugePresenter>());
            SetPrivateField(context, "fryingPanGauge", CreateGameObject("FryingPanGauge").AddComponent<ToolGaugePresenter>());
            SetPrivateField(context, "fryerGauge", CreateGameObject("FryerGauge").AddComponent<ToolGaugePresenter>());

            InvokePrivateMethod(context, "Awake");
            return context;
        }

        private ResourceData CreateResource(string resourceId, string displayName)
        {
            ResourceData resource = ScriptableObject.CreateInstance<ResourceData>();
            resource.hideFlags = HideFlags.HideAndDontSave;
            resource.ConfigureRuntime(resourceId, displayName, string.Empty, "테스트", 10, ResourceRarity.Common);
            cleanupTargets.Add(resource);
            return resource;
        }

        private RecipeData CreateRecipe(string recipeId, string displayName, string cookingMethod, ResourceData resource, int sellPrice, int reputationDelta)
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
                cookingMethod,
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstanceFlags);
            Assert.That(field, Is.Not.Null, $"{fieldName} 필드를 찾을 수 없습니다.");
            return (T)field.GetValue(target);
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

        private static void ResetStaticProperty(System.Type targetType, string propertyName)
        {
            FieldInfo field = targetType.GetField($"<{propertyName}>k__BackingField", PrivateStaticFlags);
            if (field != null)
            {
                field.SetValue(null, null);
            }
        }

        private static void SetStaticProperty(System.Type targetType, string propertyName, object value)
        {
            FieldInfo field = targetType.GetField($"<{propertyName}>k__BackingField", PrivateStaticFlags);
            Assert.That(field, Is.Not.Null, $"{propertyName} 정적 backing field 를 찾을 수 없습니다.");
            field.SetValue(null, value);
        }
    }
}
