using System;
using System.Collections.Generic;
using CoreLoop.Core;
using Exploration.Player;
using Management.Inventory;
using Shared.Data;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Restaurant.Kitchen
{
    public sealed class RestaurantFlowController : MonoBehaviour
    {
        private const string RuntimeObjectName = "RestaurantFlowController";

        private static readonly KitchenToolType[] CookingToolTypes =
        {
            KitchenToolType.CuttingBoard,
            KitchenToolType.Pot,
            KitchenToolType.FryingPan,
            KitchenToolType.Fryer
        };

        private static readonly LegacyStationDefinition[] LegacyStationDefinitions =
        {
            new(KitchenToolType.Refrigerator, "Refrigerator", new[] { "Refrigerator" }, new[] { "refrigerator" }),
            new(KitchenToolType.FrontCounter, "FrontCounter", new[] { "FrontCounter" }, new[] { "front", "counter" }),
            new(KitchenToolType.CuttingBoard, "CuttingBoard", new[] { "CuttingBoard" }, new[] { "cutting" }),
            new(KitchenToolType.Fryer, "Fryer", new[] { "Fryer" }, new[] { "fryer" }),
            new(KitchenToolType.FryingPan, "FryingPan", new[] { "FryingPan" }, new[] { "frying" }),
            new(KitchenToolType.Pot, "Pot", new[] { "Pot" }, new[] { "pot" })
        };

        private static readonly SceneStationBinding[] SceneStationBindings =
        {
            new("HubTableTopCollider", typeof(DiningTableStation)),
            new("HubTableMiddleCollider", typeof(DiningTableStation)),
            new("HubTableBottomCollider", typeof(DiningTableStation))
        };

        private readonly Dictionary<KitchenToolType, ToolCookingSession> toolSessions = new();
        private readonly Dictionary<string, ResourceData> basicResources = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<KitchenStageRuntimeDefinition> fallbackStages = new();
        private readonly List<KitchenDishData> fallbackDishes = new();

        [SerializeField] private RestaurantManager restaurantManager;
        [SerializeField] private List<KitchenStageRecipeData> stageRecipes = new();
        [SerializeField] private List<KitchenDishData> dishRecipes = new();
        [SerializeField, Min(0.1f)] private float autoCookingSeconds = 3f;
        [SerializeField, Min(0.1f)] private float manualCookingSeconds = 2.5f;

        private PlayerController movementLockedPlayer;
        private ToolCookingSession movementLockSession;
        private bool kitchenStateEventsBound;

        public static RestaurantFlowController Instance { get; private set; }

        public event Action KitchenStateChanged;

        public bool IsOpen
        {
            get
            {
                EnsureServices();
                return restaurantManager != null && restaurantManager.IsRestaurantOpen;
            }
        }
        public KitchenCarryController Carry { get; } = new();
        public KitchenWorkspaceState FrontWorkspace { get; } = new();
        public InventoryReservationService Reservations { get; } = new();
        public IReadOnlyList<KitchenDishData> FallbackDishes => fallbackDishes;

        public static RestaurantFlowController GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            Instance = FindFirstObjectByType<RestaurantFlowController>();
            if (Instance != null)
            {
                return Instance;
            }

            GameObject controllerObject = new(RuntimeObjectName);
            Instance = controllerObject.AddComponent<RestaurantFlowController>();
            return Instance;
        }

        public static bool TryGetLegacyPrompt(GameObject source, string promptLabel, out string prompt)
        {
            prompt = string.Empty;
            if (!Application.isPlaying)
            {
                return false;
            }

            RestaurantFlowController controller = GetOrCreate();
            if (!controller.TryResolveLegacyStationType(source, promptLabel, out KitchenToolType stationType))
            {
                return false;
            }

            prompt = controller.BuildPrompt(stationType);
            return true;
        }

        public static bool IsLegacyKitchenStation(GameObject source, string promptLabel)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            return GetOrCreate().TryResolveLegacyStationType(source, promptLabel, out _);
        }

        public static bool TryHandleLegacyInteract(GameObject source, string promptLabel, GameObject interactor)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            RestaurantFlowController controller = GetOrCreate();
            if (!controller.TryResolveLegacyStationType(source, promptLabel, out KitchenToolType stationType))
            {
                return false;
            }

            controller.HandleStationInteract(stationType, interactor);
            return true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureServices();
            InstallSceneStations();
        }

        private void Start()
        {
            EnsureServices();
            InstallSceneStations();
        }

        private void Update()
        {
            TickToolSessions(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ClearMovementLock();
        }

        public IReadOnlyList<ResourceData> GetBasicIngredients()
        {
            EnsureServices();
            List<ResourceData> resources = new();
            foreach (ResourceData resource in basicResources.Values)
            {
                if (resource != null)
                {
                    resources.Add(resource);
                }
            }

            resources.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal));
            return resources;
        }

        public bool TryTakeBasicIngredient(ResourceData resource)
        {
            EnsureServices();
            if (resource == null || !Carry.IsEmpty || !IsBasicResource(resource))
            {
                return false;
            }

            bool changed = Carry.TryHold(KitchenCarryItem.FromUnlimitedBasic(resource, 1));
            NotifyChanged(changed);
            return changed;
        }

        public bool TryTakeInventoryIngredient(ResourceData resource)
        {
            EnsureServices();
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (resource == null || !Carry.IsEmpty || inventory == null)
            {
                return false;
            }

            bool changed = Reservations.TryReserve(inventory, resource, 1, out KitchenCarryItem item)
                && Carry.TryHold(item);
            NotifyChanged(changed);
            return changed;
        }

        public bool TryReturnHeldItemToRefrigerator()
        {
            EnsureServices();
            if (!Carry.TryTake(out KitchenCarryItem item))
            {
                return false;
            }

            if (item.IsBundle || item.State != KitchenItemState.Raw)
            {
                Carry.TryHold(item);
                return false;
            }

            Reservations.Release(item);
            NotifyChanged(true);
            return true;
        }

        public bool TryPlaceHeldOnFrontCounter(int slotIndex)
        {
            EnsureServices();
            if (!Carry.TryTake(out KitchenCarryItem item))
            {
                return false;
            }

            bool placed = item.IsBundle
                ? FrontWorkspace.TryUnpackBundle(item.Bundle)
                : FrontWorkspace.TryPlace(slotIndex, item);

            if (!placed)
            {
                Carry.TryHold(item);
                return false;
            }

            NotifyChanged(true);
            return true;
        }

        public bool TryPickFrontCounterSlot(int slotIndex)
        {
            EnsureServices();
            if (!Carry.IsEmpty || !FrontWorkspace.TryPick(slotIndex, out KitchenCarryItem item))
            {
                return false;
            }

            bool changed = Carry.TryHold(item);
            NotifyChanged(changed);
            return changed;
        }

        public bool TryBundleFrontCounter()
        {
            EnsureServices();
            if (!Carry.IsEmpty || !FrontWorkspace.TryCreateBundleFromFilledSlots(out KitchenBundle bundle))
            {
                return false;
            }

            bool changed = Carry.TryHold(KitchenCarryItem.FromBundle(bundle));
            NotifyChanged(changed);
            return changed;
        }

        public bool TryFinalizeFrontCounter()
        {
            EnsureServices();
            if (!Carry.IsEmpty)
            {
                return false;
            }

            KitchenDishData dish = FindFinalizableDish();
            if (dish == null || !FrontWorkspace.TryFinalize(dish, out KitchenCarryItem finalDish))
            {
                return false;
            }

            bool changed = Carry.TryHold(finalDish);
            NotifyChanged(changed);
            return changed;
        }

        public bool TryReturnFrontCounterRawInputs()
        {
            EnsureServices();
            List<KitchenCarryItem> returned = FrontWorkspace.ReturnRawInputs();
            foreach (KitchenCarryItem item in returned)
            {
                Reservations.Release(item);
            }

            bool changed = returned.Count > 0;
            NotifyChanged(changed);
            return changed;
        }

        public KitchenDishData FindFinalizableDish()
        {
            EnsureServices();
            string signature = FrontWorkspace.CurrentSignature;
            if (string.IsNullOrWhiteSpace(signature))
            {
                return null;
            }

            foreach (KitchenDishData dish in EnumerateDishes())
            {
                if (dish != null
                    && IsDishAvailableToday(dish)
                    && string.Equals(signature, dish.FinalSignature, StringComparison.Ordinal))
                {
                    return dish;
                }
            }

            return null;
        }

        public string BuildToolPrompt(KitchenToolType toolType)
        {
            EnsureServices();
            if (!IsOpen)
            {
                return "OPEN 후 사용 가능";
            }

            ToolCookingSession session = GetToolSession(toolType);
            string toolName = GetToolDisplayName(toolType);

            if (session.HasOutput)
            {
                return $"[E] Pick up {session.OutputItem.DisplayName}";
            }

            if (session.IsCooking)
            {
                return $"{toolName} {Mathf.RoundToInt(session.NormalizedProgress * 100f)}%";
            }

            if (Carry.HeldItem != null && Carry.HeldItem.IsBundle)
            {
                return $"[E] Start {toolName}";
            }

            return $"[E] {toolName}: need FrontCounter bundle";
        }

        public bool CanUseTool(KitchenToolType toolType)
        {
            EnsureServices();
            if (!IsOpen)
            {
                return false;
            }

            ToolCookingSession session = GetToolSession(toolType);
            return session.IsCooking || session.HasOutput || (Carry.HeldItem != null && Carry.HeldItem.IsBundle);
        }

        public bool TryUseTool(KitchenToolType toolType, GameObject interactor)
        {
            EnsureServices();
            if (!IsOpen)
            {
                return false;
            }

            ToolCookingSession session = GetToolSession(toolType);

            if (session.HasOutput)
            {
                if (!Carry.IsEmpty)
                {
                    return false;
                }

                bool picked = Carry.TryHold(session.TakeOutput());
                ToolGaugePresenter.SetSceneGaugeProgress(toolType, 0f);
                NotifyChanged(picked);
                return picked;
            }

            if (session.IsCooking || Carry.HeldItem == null || !Carry.HeldItem.IsBundle)
            {
                return false;
            }

            KitchenBundle bundle = Carry.HeldItem.Bundle;
            KitchenStageRuntimeDefinition stage = FindStageDefinition(toolType, bundle);
            if (stage == null)
            {
                return false;
            }

            if (!ConsumeBundleInputs(bundle))
            {
                return false;
            }

            Carry.Clear();
            session.Start(stage.ProgressMode, stage.CookingSeconds, stage.OutputItem);
            if (stage.ProgressMode == KitchenProgressMode.ManualHold)
            {
                ApplyMovementLock(interactor, session);
            }

            ToolGaugePresenter.SetSceneGaugeProgress(toolType, 0.01f);
            NotifyChanged(true);
            return true;
        }

        public KitchenDishData GetRandomTodayDish()
        {
            EnsureServices();
            List<KitchenDishData> candidates = new();
            foreach (KitchenDishData dish in EnumerateDishes())
            {
                if (dish != null && IsDishAvailableToday(dish))
                {
                    candidates.Add(dish);
                }
            }

            return candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        public bool TryServeHeldDish(OrderTicket ticket)
        {
            EnsureServices();
            if (ticket == null || Carry.HeldItem == null || Carry.HeldItem.State != KitchenItemState.FinalDish)
            {
                return false;
            }

            if (!ticket.TryComplete(Carry.HeldItem))
            {
                return false;
            }

            Carry.Clear();
            NotifyChanged(true);
            return true;
        }

        private void HandleStationInteract(KitchenToolType stationType, GameObject interactor)
        {
            if (stationType != KitchenToolType.Refrigerator && !IsOpen)
            {
                GameManager.Instance?.DayCycle?.ShowTemporaryGuide("영업을 시작하면 사용할 수 있습니다.");
                return;
            }

            switch (stationType)
            {
                case KitchenToolType.Refrigerator:
                    RefrigeratorStation.RequestPanel();
                    return;
                case KitchenToolType.FrontCounter:
                    FrontCounterStation.RequestPanel();
                    return;
                case KitchenToolType.CuttingBoard:
                case KitchenToolType.Pot:
                case KitchenToolType.FryingPan:
                case KitchenToolType.Fryer:
                    TryUseTool(stationType, interactor);
                    return;
                default:
                    return;
            }
        }

        private string BuildPrompt(KitchenToolType stationType)
        {
            return stationType switch
            {
                KitchenToolType.Refrigerator => "[E] Refrigerator",
                KitchenToolType.FrontCounter => IsOpen ? "[E] FrontCounter" : "OPEN 후 사용 가능",
                KitchenToolType.CuttingBoard or KitchenToolType.Pot or KitchenToolType.FryingPan or KitchenToolType.Fryer => IsOpen ? BuildToolPrompt(stationType) : "OPEN 후 사용 가능",
                _ => string.Empty
            };
        }

        private void TickToolSessions(float deltaSeconds)
        {
            foreach (ToolCookingSession session in toolSessions.Values)
            {
                bool manualHeld = session.ProgressMode == KitchenProgressMode.ManualHold && ReadInteractHeld();
                bool completed = session.Tick(deltaSeconds, manualHeld);
                bool shouldShowGauge = session.IsCooking || session.HasOutput;
                ToolGaugePresenter.SetSceneGaugeProgress(session.ToolType, shouldShowGauge ? session.NormalizedProgress : 0f);

                if (!completed)
                {
                    continue;
                }

                if (session == movementLockSession)
                {
                    ClearMovementLock();
                }

                NotifyChanged(true);
            }
        }

        private void EnsureServices()
        {
            if (restaurantManager == null)
            {
                restaurantManager = FindFirstObjectByType<RestaurantManager>();
            }

            EnsureKitchenStateEventBindings();

            foreach (KitchenToolType toolType in CookingToolTypes)
            {
                EnsureToolSession(toolType);
            }

            EnsureBasicIngredients();
            EnsureFallbackRecipes();
        }

        private void EnsureKitchenStateEventBindings()
        {
            if (kitchenStateEventsBound)
            {
                return;
            }

            kitchenStateEventsBound = true;
            Carry.Changed += HandleKitchenStateChanged;
            FrontWorkspace.Changed += HandleKitchenStateChanged;
            Reservations.Changed += HandleKitchenStateChanged;
        }

        private void InstallSceneStations()
        {
            foreach (SceneStationBinding binding in SceneStationBindings)
            {
                AddStationIfMissing(binding.ObjectName, binding.ComponentType);
            }

            if (GetComponent<CustomerServiceController>() == null)
            {
                gameObject.AddComponent<CustomerServiceController>();
            }
        }

        private static void AddStationIfMissing(string objectName, Type componentType)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null || componentType == null || target.GetComponent(componentType) != null)
            {
                return;
            }

            target.AddComponent(componentType);
        }

        private bool IsBasicResource(ResourceData resource)
        {
            foreach (ResourceData basicResource in basicResources.Values)
            {
                if (basicResource == resource)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureBasicIngredients()
        {
            EnsureBasicResource("ingredient_001", "김치");
            EnsureBasicResource("ingredient_002", "밥");
            EnsureBasicResource("ingredient_003", "밀가루");
            EnsureBasicResource("ingredient_004", "고춧가루");
        }

        private void EnsureBasicResource(string id, string displayName)
        {
            if (basicResources.ContainsKey(id))
            {
                return;
            }

            ResourceData generatedResource = GeneratedGameDataLocator.FindGeneratedResource(id, displayName);
            if (generatedResource != null)
            {
                basicResources[id] = generatedResource;
                return;
            }

            ResourceData resource = ScriptableObject.CreateInstance<ResourceData>();
            resource.name = $"runtime-resource-{id}";
            resource.hideFlags = HideFlags.HideAndDontSave;
            resource.ConfigureRuntime(id, displayName, "Basic refrigerator ingredient.", "Basic", 0, ResourceRarity.Common);
            basicResources[id] = resource;
        }

        private void EnsureFallbackRecipes()
        {
            if (fallbackStages.Count > 0 && fallbackDishes.Count > 0)
            {
                return;
            }

            CreateFallbackRecipe("food_001", "김치볶음밥", KitchenToolType.FryingPan, "ingredient_001", "ingredient_002");
            CreateFallbackRecipe("food_002", "김치찌개", KitchenToolType.Pot, "ingredient_001", "ingredient_004");
            CreateFallbackRecipe("food_003", "김치전", KitchenToolType.FryingPan, "ingredient_001", "ingredient_003");
        }

        private void CreateFallbackRecipe(string recipeId, string displayName, KitchenToolType toolType, string firstIngredientId, string secondIngredientId)
        {
            if (!basicResources.TryGetValue(firstIngredientId, out ResourceData firstResource)
                || !basicResources.TryGetValue(secondIngredientId, out ResourceData secondResource))
            {
                return;
            }

            KitchenItemData cookedItem = ScriptableObject.CreateInstance<KitchenItemData>();
            cookedItem.name = $"runtime-kitchen-item-{recipeId}-cooked";
            cookedItem.hideFlags = HideFlags.HideAndDontSave;
            cookedItem.ConfigureRuntime($"{recipeId}_cooked", $"{displayName} Cooked Base", KitchenItemState.Cooked, null, recipeId);

            KitchenItemData finalItem = ScriptableObject.CreateInstance<KitchenItemData>();
            finalItem.name = $"runtime-kitchen-item-{recipeId}-final";
            finalItem.hideFlags = HideFlags.HideAndDontSave;
            finalItem.ConfigureRuntime(recipeId, displayName, KitchenItemState.FinalDish, null, recipeId);

            KitchenItemRequirement firstRequirement = new();
            firstRequirement.ConfigureRuntime(null, firstResource, KitchenItemState.Raw, 1);
            KitchenItemRequirement secondRequirement = new();
            secondRequirement.ConfigureRuntime(null, secondResource, KitchenItemState.Raw, 1);
            KitchenItemRequirement cookedRequirement = new();
            cookedRequirement.ConfigureRuntime(cookedItem, null, KitchenItemState.Cooked, 1);

            fallbackStages.Add(new KitchenStageRuntimeDefinition(
                toolType,
                KitchenSignatureUtility.BuildSignature(new[] { firstRequirement, secondRequirement }),
                toolType == KitchenToolType.CuttingBoard ? KitchenProgressMode.ManualHold : KitchenProgressMode.AutoProgress,
                toolType == KitchenToolType.CuttingBoard ? manualCookingSeconds : autoCookingSeconds,
                KitchenCarryItem.FromKitchenItem(cookedItem)));

            KitchenDishData dishData = ScriptableObject.CreateInstance<KitchenDishData>();
            dishData.name = $"runtime-kitchen-dish-{recipeId}";
            dishData.hideFlags = HideFlags.HideAndDontSave;
            dishData.ConfigureRuntime(recipeId, displayName, new[] { cookedRequirement }, finalItem);
            fallbackDishes.Add(dishData);
        }

        private IEnumerable<KitchenDishData> EnumerateDishes()
        {
            foreach (KitchenDishData dish in dishRecipes)
            {
                if (dish != null)
                {
                    yield return dish;
                }
            }

            foreach (KitchenDishData dish in fallbackDishes)
            {
                if (dish != null)
                {
                    yield return dish;
                }
            }
        }

        private bool IsDishAvailableToday(KitchenDishData dish)
        {
            if (dish == null || string.IsNullOrWhiteSpace(dish.RecipeId))
            {
                return false;
            }

            EnsureServices();

            if (restaurantManager == null)
            {
                return false;
            }

            foreach (RecipeData recipe in restaurantManager.TodayMenuRecipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                if (string.Equals(recipe.RecipeId, dish.RecipeId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private KitchenStageRuntimeDefinition FindStageDefinition(KitchenToolType toolType, KitchenBundle bundle)
        {
            if (bundle == null || bundle.IsEmpty)
            {
                return null;
            }

            string signature = bundle.Signature;
            foreach (KitchenStageRecipeData recipe in stageRecipes)
            {
                if (recipe == null || recipe.ToolType != toolType)
                {
                    continue;
                }

                if (!string.Equals(recipe.InputSignature, signature, StringComparison.Ordinal))
                {
                    continue;
                }

                KitchenCarryItem output = KitchenCarryItem.FromKitchenItem(recipe.OutputItem);
                if (output == null)
                {
                    continue;
                }

                return new KitchenStageRuntimeDefinition(
                    recipe.ToolType,
                    recipe.InputSignature,
                    recipe.ProgressMode,
                    recipe.CookingSeconds,
                    output);
            }

            foreach (KitchenStageRuntimeDefinition fallback in fallbackStages)
            {
                if (fallback.ToolType == toolType
                    && string.Equals(fallback.InputSignature, signature, StringComparison.Ordinal))
                {
                    return fallback;
                }
            }

            return null;
        }

        private bool ConsumeBundleInputs(KitchenBundle bundle)
        {
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (Reservations.ConsumeReservedInputs(inventory, bundle))
            {
                return true;
            }

            foreach (KitchenCarryItem item in bundle.Items)
            {
                if (item != null && item.IsInventoryReservation)
                {
                    return false;
                }
            }

            return true;
        }

        private ToolCookingSession GetToolSession(KitchenToolType toolType)
        {
            EnsureToolSession(toolType);
            return toolSessions[toolType];
        }

        private void EnsureToolSession(KitchenToolType toolType)
        {
            if (!toolSessions.ContainsKey(toolType))
            {
                toolSessions[toolType] = new ToolCookingSession(toolType);
            }
        }

        private bool TryResolveLegacyStationType(GameObject source, string promptLabel, out KitchenToolType stationType)
        {
            stationType = default;
            return TryResolveStationType(BuildLineageName(source), StationTokenSource.Lineage, out stationType)
                || TryResolveStationType(promptLabel ?? string.Empty, StationTokenSource.PromptLabel, out stationType);
        }

        private static string BuildLineageName(GameObject source)
        {
            if (source == null)
            {
                return string.Empty;
            }

            List<string> names = new();
            Transform current = source.transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static bool ContainsOrdinal(string value, string token)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !string.IsNullOrWhiteSpace(token)
                && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryResolveStationType(string value, StationTokenSource source, out KitchenToolType stationType)
        {
            stationType = default;
            foreach (LegacyStationDefinition definition in LegacyStationDefinitions)
            {
                string[] tokens = source == StationTokenSource.Lineage
                    ? definition.LineageTokens
                    : definition.PromptTokens;

                foreach (string token in tokens)
                {
                    if (!ContainsOrdinal(value, token))
                    {
                        continue;
                    }

                    stationType = definition.ToolType;
                    return true;
                }
            }

            return false;
        }

        private static string GetToolDisplayName(KitchenToolType toolType)
        {
            foreach (LegacyStationDefinition definition in LegacyStationDefinitions)
            {
                if (definition.ToolType == toolType)
                {
                    return definition.DisplayName;
                }
            }

            return toolType.ToString();
        }

        private void ApplyMovementLock(GameObject interactor, ToolCookingSession session)
        {
            PlayerController player = interactor != null ? interactor.GetComponent<PlayerController>() : null;
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (player == null)
            {
                return;
            }

            ClearMovementLock();
            movementLockedPlayer = player;
            movementLockSession = session;
            movementLockedPlayer.SetMovementMultiplierSource(session, 0f);
        }

        private void ClearMovementLock()
        {
            if (movementLockedPlayer != null && movementLockSession != null)
            {
                movementLockedPlayer.ClearMovementMultiplierSource(movementLockSession);
            }

            movementLockedPlayer = null;
            movementLockSession = null;
        }

        private static bool ReadInteractHeld()
        {
            bool held = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            held |= keyboard != null && (keyboard.eKey.isPressed || keyboard.enterKey.isPressed);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            held |= Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Return);
#endif
            return held;
        }

        private void HandleKitchenStateChanged()
        {
            NotifyChanged(true);
        }

        private void NotifyChanged(bool changed)
        {
            if (changed)
            {
                KitchenStateChanged?.Invoke();
            }
        }

        private enum StationTokenSource
        {
            Lineage,
            PromptLabel
        }

        private readonly struct LegacyStationDefinition
        {
            public LegacyStationDefinition(
                KitchenToolType toolType,
                string displayName,
                string[] lineageTokens,
                string[] promptTokens)
            {
                ToolType = toolType;
                DisplayName = displayName ?? string.Empty;
                LineageTokens = lineageTokens ?? Array.Empty<string>();
                PromptTokens = promptTokens ?? Array.Empty<string>();
            }

            public KitchenToolType ToolType { get; }
            public string DisplayName { get; }
            public string[] LineageTokens { get; }
            public string[] PromptTokens { get; }
        }

        private readonly struct SceneStationBinding
        {
            public SceneStationBinding(string objectName, Type componentType)
            {
                ObjectName = objectName ?? string.Empty;
                ComponentType = componentType;
            }

            public string ObjectName { get; }
            public Type ComponentType { get; }
        }

        private sealed class KitchenStageRuntimeDefinition
        {
            public KitchenStageRuntimeDefinition(
                KitchenToolType toolType,
                string inputSignature,
                KitchenProgressMode progressMode,
                float cookingSeconds,
                KitchenCarryItem outputItem)
            {
                ToolType = toolType;
                InputSignature = inputSignature ?? string.Empty;
                ProgressMode = progressMode;
                CookingSeconds = Mathf.Max(0.1f, cookingSeconds);
                OutputItem = outputItem;
            }

            public KitchenToolType ToolType { get; }
            public string InputSignature { get; }
            public KitchenProgressMode ProgressMode { get; }
            public float CookingSeconds { get; }
            public KitchenCarryItem OutputItem { get; }
        }
    }
}
