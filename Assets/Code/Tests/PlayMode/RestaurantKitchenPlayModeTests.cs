using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Code.Scripts.CoreLoop.Core;
using Code.Scripts.Management.Inventory;
using NUnit.Framework;
using Code.Scripts.Restaurant;
using Code.Scripts.Restaurant.Kitchen;
using Code.Scripts.Shared.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public sealed class RestaurantKitchenPlayModeTests
    {
        private const BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStaticFlags = BindingFlags.Static | BindingFlags.NonPublic;

        [TearDown]
        public void TearDown()
        {
            ResetStaticProperty(typeof(GameManager), "Instance");
            ResetStaticProperty(typeof(RestaurantFlowController), "Instance");
            ResetStaticProperty(typeof(HubRuntimeContext), "Active");
        }

        [UnityTest]
        public IEnumerator CloseOpenCookServeClose_SmokeFlow_Works()
        {
            GameObject gameManagerObject = new("GameManager");
            gameManagerObject.AddComponent<GameManager>();

            ResourceData fish = ScriptableObject.CreateInstance<ResourceData>();
            fish.hideFlags = HideFlags.HideAndDontSave;
            fish.ConfigureRuntime("fish", "생선", "테스트 재료", "테스트", 10, ResourceRarity.Common);

            List<RecipeData> recipes = new()
            {
                CreateRecipe("food_test_001", "생선탕", "냄비", fish, 25, 1),
                CreateRecipe("food_test_002", "생선조림", "냄비", fish, 30, 2),
                CreateRecipe("food_test_003", "생선국", "냄비", fish, 35, 3)
            };

            GameObject restaurantRoot = new("RestaurantRoot");
            RestaurantManager restaurantManager = restaurantRoot.AddComponent<RestaurantManager>();
            SetPrivateField(restaurantManager, "availableRecipes", recipes);
            RestaurantFlowController flowController = restaurantRoot.AddComponent<RestaurantFlowController>();
            CustomerServiceController serviceController = restaurantRoot.AddComponent<CustomerServiceController>();
            SetPrivateField(serviceController, "orderDelaySeconds", 0.05f);
            SetPrivateField(serviceController, "patienceSeconds", 5f);
            HubRuntimeContext context = restaurantRoot.AddComponent<HubRuntimeContext>();

            DiningTableStation tableTop = CreateTable("top");
            DiningTableStation tableMiddle = CreateTable("middle");
            DiningTableStation tableBottom = CreateTable("bottom");

            SetPrivateField(context, "restaurantManager", restaurantManager);
            SetPrivateField(context, "restaurantFlowController", flowController);
            SetPrivateField(context, "customerServiceController", serviceController);
            SetPrivateField(context, "diningTables", new List<DiningTableStation> { tableTop, tableMiddle, tableBottom });
            SetPrivateField(context, "refrigeratorStation", CreateChildComponent<RefrigeratorStation>(restaurantRoot.transform, "Refrigerator"));
            SetPrivateField(context, "frontCounterStation", CreateChildComponent<ServiceCounterStation>(restaurantRoot.transform, "PassCounter"));
            SetPrivateField(context, "cuttingBoardStation", CreateChildComponent<ServiceCounterStation>(restaurantRoot.transform, "CuttingBoard"));
            SetPrivateField(context, "potStation", CreateChildComponent<ServiceCounterStation>(restaurantRoot.transform, "Pot"));
            SetPrivateField(context, "fryingPanStation", CreateChildComponent<ServiceCounterStation>(restaurantRoot.transform, "FryingPan"));
            SetPrivateField(context, "fryerStation", CreateChildComponent<ServiceCounterStation>(restaurantRoot.transform, "Fryer"));
            SetPrivateField(context, "cuttingBoardGauge", CreateChildComponent<ToolGaugePresenter>(restaurantRoot.transform, "CuttingBoardGauge"));
            SetPrivateField(context, "potGauge", CreateChildComponent<ToolGaugePresenter>(restaurantRoot.transform, "PotGauge"));
            SetPrivateField(context, "fryingPanGauge", CreateChildComponent<ToolGaugePresenter>(restaurantRoot.transform, "FryingPanGauge"));
            SetPrivateField(context, "fryerGauge", CreateChildComponent<ToolGaugePresenter>(restaurantRoot.transform, "FryerGauge"));
            SetPrivateField(context, "autoCookingSeconds", 0.01f);
            SetPrivateField(context, "manualCookingSeconds", 0.01f);

            yield return null;

            InventoryManager inventoryManager = GameManager.Instance.Inventory;
            inventoryManager.TryAdd(fish, 10, out _);

            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[0]);
            restaurantManager.SelectTodayMenuSlot(1);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[1]);
            restaurantManager.SelectTodayMenuSlot(2);
            restaurantManager.AssignSelectedTodayMenuRecipe(recipes[2]);
            Assert.That(restaurantManager.TryOpenRestaurant(), Is.True);

            for (int frame = 0; frame < 120 && serviceController.ActiveTicketCount == 0; frame++)
            {
                yield return null;
            }

            Assert.That(serviceController.ActiveTicketCount, Is.GreaterThan(0));
            SetPrivateField(serviceController, "nextOrderTimer", 999f);

            OrderTicket ticket = serviceController.Tickets[0];
            Assert.That(ticket, Is.Not.Null);

            RecipeData orderedRecipe = null;
            for (int index = 0; index < restaurantManager.AvailableRecipes.Count; index++)
            {
                if (restaurantManager.AvailableRecipes[index] != null
                    && restaurantManager.AvailableRecipes[index].RecipeId == ticket.Dish.RecipeId)
                {
                    orderedRecipe = restaurantManager.AvailableRecipes[index];
                    break;
                }
            }

            Assert.That(orderedRecipe, Is.Not.Null);
            Assert.That(RecipeIngredient.TryResolve(orderedRecipe.Ingredients[0], out ResourceData ingredient, out _), Is.True);

            KitchenToolType toolType = ResolveToolType(orderedRecipe.CookingMethod);
            flowController.SelectCookingTool(toolType);
            Assert.That(flowController.TryAddCookingIngredient(toolType, ingredient), Is.True);
            Assert.That(flowController.TryStartSelectedCooking(toolType, null), Is.True);

            yield return null;
            yield return null;

            Assert.That(flowController.TryUseTool(toolType, null), Is.True);
            Assert.That(flowController.TryPlaceHeldOnFrontCounter(0), Is.True);
            Assert.That(flowController.TryPickFrontCounterSlot(0), Is.True);

            DiningTableStation targetTable = ResolveTable(ticket.TableId, tableTop, tableMiddle, tableBottom);
            Assert.That(targetTable, Is.Not.Null);
            Assert.That(serviceController.TryServeHeldDish(targetTable), Is.True);

            yield return null;

            Assert.That(serviceController.ActiveTicketCount, Is.EqualTo(0));
            Assert.That(restaurantManager.TryCloseRestaurant(serviceController.ActiveTicketCount), Is.True);

            Object.DestroyImmediate(tableTop.gameObject);
            Object.DestroyImmediate(tableMiddle.gameObject);
            Object.DestroyImmediate(tableBottom.gameObject);
            Object.DestroyImmediate(restaurantRoot);
            Object.DestroyImmediate(gameManagerObject);
            Object.DestroyImmediate(fish);
            for (int index = 0; index < recipes.Count; index++)
            {
                Object.DestroyImmediate(recipes[index]);
            }
        }

        [UnityTest]
        public IEnumerator FrontCounter_PlaceAndPick_RoundTripsHeldItem()
        {
            ResourceData fish = ScriptableObject.CreateInstance<ResourceData>();
            fish.hideFlags = HideFlags.HideAndDontSave;
            fish.ConfigureRuntime("fish", "생선", "테스트 재료", "테스트", 10, ResourceRarity.Common);

            GameObject restaurantRoot = new("RestaurantRoot");
            RestaurantFlowController flowController = restaurantRoot.AddComponent<RestaurantFlowController>();

            yield return null;

            Assert.That(flowController.Carry.TryHold(KitchenCarryItem.FromUnlimitedBasic(fish, 1)), Is.True);
            Assert.That(flowController.TryPlaceHeldOnFrontCounter(0), Is.True);
            Assert.That(flowController.Carry.HeldItem, Is.Null);
            Assert.That(flowController.FrontWorkspace.Slots[0], Is.Not.Null);

            Assert.That(flowController.TryPickFrontCounterSlot(0), Is.True);
            Assert.That(flowController.Carry.HeldItem, Is.Not.Null);
            Assert.That(flowController.FrontWorkspace.Slots[0], Is.Null);

            Object.DestroyImmediate(restaurantRoot);
            Object.DestroyImmediate(fish);
        }

        [UnityTest]
        public IEnumerator InventoryIngredient_TakeAndReturnToRefrigerator_ReleasesReservation()
        {
            GameObject gameManagerObject = new("GameManager");
            gameManagerObject.AddComponent<GameManager>();

            ResourceData fish = ScriptableObject.CreateInstance<ResourceData>();
            fish.hideFlags = HideFlags.HideAndDontSave;
            fish.ConfigureRuntime("fish", "생선", "테스트 재료", "테스트", 10, ResourceRarity.Common);

            GameObject restaurantRoot = new("RestaurantRoot");
            RestaurantFlowController flowController = restaurantRoot.AddComponent<RestaurantFlowController>();

            yield return null;

            InventoryManager inventoryManager = GameManager.Instance.Inventory;
            inventoryManager.TryAdd(fish, 3, out _);

            Assert.That(flowController.TryTakeInventoryIngredient(fish), Is.True);
            Assert.That(flowController.Reservations.GetReservedAmount(fish), Is.EqualTo(1));
            Assert.That(flowController.Carry.HeldItem, Is.Not.Null);

            Assert.That(flowController.TryReturnHeldItemToRefrigerator(), Is.True);
            Assert.That(flowController.Reservations.GetReservedAmount(fish), Is.EqualTo(0));
            Assert.That(flowController.Carry.HeldItem, Is.Null);
            Assert.That(inventoryManager.GetAmount(fish), Is.EqualTo(3));

            Object.DestroyImmediate(restaurantRoot);
            Object.DestroyImmediate(gameManagerObject);
            Object.DestroyImmediate(fish);
        }

        private static DiningTableStation ResolveTable(string tableId, params DiningTableStation[] tables)
        {
            for (int index = 0; index < tables.Length; index++)
            {
                if (tables[index] != null && tables[index].TableId == tableId)
                {
                    return tables[index];
                }
            }

            return null;
        }

        private static T CreateChildComponent<T>(Transform parent, string objectName) where T : Component
        {
            GameObject child = new(objectName);
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }

        private static DiningTableStation CreateTable(string tableId)
        {
            GameObject tableObject = new($"Table-{tableId}");
            DiningTableStation tableStation = tableObject.AddComponent<DiningTableStation>();
            SetPrivateField(tableStation, "tableId", tableId);
            return tableStation;
        }

        private static RecipeData CreateRecipe(string recipeId, string displayName, string cookingMethod, ResourceData resource, int sellPrice, int reputationDelta)
        {
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.hideFlags = HideFlags.HideAndDontSave;
            recipe.ConfigureRuntime(
                recipeId,
                displayName,
                "플레이모드 테스트 레시피",
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
            return recipe;
        }

        private static KitchenToolType ResolveToolType(string cookingMethod)
        {
            return cookingMethod switch
            {
                "도마" => KitchenToolType.CuttingBoard,
                "냄비" => KitchenToolType.Pot,
                "후라이팬" or "프라이팬" => KitchenToolType.FryingPan,
                "튀김기" => KitchenToolType.Fryer,
                _ => KitchenToolType.FrontCounter
            };
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
    }
}
