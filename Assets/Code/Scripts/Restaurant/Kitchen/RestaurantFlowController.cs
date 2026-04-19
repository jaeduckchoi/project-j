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
    /// <summary>
    /// 허브 조리 흐름, 손 상태, PassCounter 보관 슬롯, CookingUtensils 조리 세션을 관리한다.
    /// </summary>
    public sealed class RestaurantFlowController : MonoBehaviour
    {
        private static readonly KitchenToolType[] CookingToolTypes =
        {
            KitchenToolType.CuttingBoard,
            KitchenToolType.Pot,
            KitchenToolType.FryingPan,
            KitchenToolType.Fryer
        };

        [SerializeField] private RestaurantManager restaurantManager;

        private readonly Dictionary<KitchenToolType, ToolCookingSession> toolSessions = new();
        private readonly List<ResourceData> selectedToolIngredients = new();

        private PlayerController movementLockedPlayer;
        private ToolCookingSession movementLockSession;
        private bool kitchenStateEventsBound;
        private RestaurantRecipeCatalog standaloneRecipeCatalog;
        private KitchenToolType selectedCookingToolType = KitchenToolType.FryingPan;

        public static RestaurantFlowController Instance { get; private set; }
        public static event Action<KitchenToolType> CookingToolPanelRequested;

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
        public IReadOnlyList<ResourceData> SelectedToolIngredients => selectedToolIngredients;
        public KitchenToolType SelectedCookingToolType => selectedCookingToolType;

        /// <summary>
        /// 현재 씬에 이미 존재하는 식당 흐름 컨트롤러를 찾되 새 런타임 오브젝트는 만들지 않는다.
        /// </summary>
        public static bool TryGetExisting(out RestaurantFlowController controller)
        {
            controller = Instance;
            if (controller != null)
            {
                return true;
            }

            if (HubRuntimeContext.Active != null && HubRuntimeContext.Active.RestaurantFlowController != null)
            {
                controller = HubRuntimeContext.Active.RestaurantFlowController;
                Instance = controller;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 허브 씬에서 사용할 컨트롤러를 반환한다.
        /// 컨텍스트나 씬 직렬화가 없다면 null을 반환한다.
        /// </summary>
        public static RestaurantFlowController GetOrCreate()
        {
            return TryGetExisting(out RestaurantFlowController controller) ? controller : null;
        }

        public static void RequestCookingToolPanel(KitchenToolType toolType)
        {
            CookingToolPanelRequested?.Invoke(toolType);
        }

        public static bool TryGetLegacyPrompt(GameObject source, string promptLabel, out string prompt)
        {
            prompt = string.Empty;
            if (!Application.isPlaying || !TryGetExisting(out RestaurantFlowController controller))
            {
                return false;
            }

            if (!TryResolveLegacyStationType(source, promptLabel, out KitchenToolType stationType))
            {
                return false;
            }

            prompt = controller.BuildPrompt(stationType);
            return true;
        }

        public static bool IsLegacyKitchenStation(GameObject source, string promptLabel)
        {
            return Application.isPlaying && TryResolveLegacyStationType(source, promptLabel, out _);
        }

        public static bool TryHandleLegacyInteract(GameObject source, string promptLabel, GameObject interactor)
        {
            if (!Application.isPlaying || !TryResolveLegacyStationType(source, promptLabel, out KitchenToolType stationType))
            {
                return false;
            }

            RestaurantFlowController controller = GetOrCreate();
            if (controller == null)
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
        }

        private void Start()
        {
            EnsureServices();
        }

        private void Update()
        {
            TickToolSessions(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ClearMovementLock();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 냉장고 기본 재료 목록을 반환한다.
        /// </summary>
        public IReadOnlyList<ResourceData> GetBasicIngredients()
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            return recipeCatalog != null ? recipeCatalog.BasicIngredients : Array.Empty<ResourceData>();
        }

        /// <summary>
        /// 현재 조리 패널이 다룰 기구를 선택하고 기존 선택 재료를 초기화한다.
        /// </summary>
        public void SelectCookingTool(KitchenToolType toolType)
        {
            EnsureServices();
            if (!IsCookingTool(toolType))
            {
                return;
            }

            selectedCookingToolType = toolType;
            selectedToolIngredients.Clear();
            NotifyChanged(true);
        }

        /// <summary>
        /// 현재 선택된 조리기구에 사용할 재료 선택을 비운다.
        /// </summary>
        public bool ClearCookingSelection()
        {
            EnsureServices();
            if (selectedToolIngredients.Count == 0)
            {
                return false;
            }

            selectedToolIngredients.Clear();
            NotifyChanged(true);
            return true;
        }

        /// <summary>
        /// 지정한 재료를 현재 조리기구 선택 목록에 추가한다.
        /// </summary>
        public bool TryAddCookingIngredient(KitchenToolType toolType, ResourceData resource)
        {
            EnsureServices();
            if (!IsOpen || !IsCookingTool(toolType) || resource == null)
            {
                return false;
            }

            selectedCookingToolType = toolType;
            if (!CanAddCookingIngredient(resource))
            {
                return false;
            }

            selectedToolIngredients.Add(resource);
            NotifyChanged(true);
            return true;
        }

        /// <summary>
        /// 선택 목록의 지정 인덱스 재료를 제거한다.
        /// </summary>
        public bool TryRemoveCookingIngredientAt(int index)
        {
            EnsureServices();
            if (index < 0 || index >= selectedToolIngredients.Count)
            {
                return false;
            }

            selectedToolIngredients.RemoveAt(index);
            NotifyChanged(true);
            return true;
        }

        /// <summary>
        /// 현재 선택된 기구 기준으로 추가 가능한 재료 후보를 반환한다.
        /// </summary>
        public IReadOnlyList<ResourceData> GetSelectableIngredientsForTool(KitchenToolType toolType)
        {
            EnsureServices();
            List<ResourceData> resources = new();
            if (!IsCookingTool(toolType) || restaurantManager == null)
            {
                return resources;
            }

            HashSet<string> addedIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (RecipeData recipe in restaurantManager.TodayMenuRecipes)
            {
                if (recipe == null || !IsToolMatchedRecipe(toolType, recipe))
                {
                    continue;
                }

                IReadOnlyList<RecipeIngredient> ingredients = recipe.Ingredients;
                for (int index = 0; index < ingredients.Count; index++)
                {
                    if (!TryResolveRecipeIngredientResource(ingredients[index], out ResourceData resource) || resource == null)
                    {
                        continue;
                    }

                    if (!addedIds.Add(resource.ResourceId))
                    {
                        continue;
                    }

                    resources.Add(resource);
                }
            }

            return resources;
        }

        /// <summary>
        /// 현재 선택된 조리 재료에서 특정 자원이 몇 번 선택됐는지 반환한다.
        /// </summary>
        public int GetSelectedCookingIngredientCount(ResourceData resource)
        {
            EnsureServices();
            return GetSelectedIngredientCount(resource);
        }

        /// <summary>
        /// 지정한 재료의 현재 사용 가능 수량을 반환한다. 기본 재료는 사실상 무한으로 본다.
        /// </summary>
        public int GetAvailableCookingIngredientAmount(ResourceData resource)
        {
            EnsureServices();
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (resource == null || recipeCatalog == null)
            {
                return 0;
            }

            if (recipeCatalog.IsBasicIngredient(resource))
            {
                return int.MaxValue;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null)
            {
                return 0;
            }

            inventory.InitializeIfNeeded();
            return inventory.GetAmount(resource);
        }

        /// <summary>
        /// 지정한 재료가 기본 냉장고 재료인지 반환한다.
        /// </summary>
        public bool IsBasicIngredient(ResourceData resource)
        {
            EnsureServices();
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            return recipeCatalog != null && recipeCatalog.IsBasicIngredient(resource);
        }

        /// <summary>
        /// 현재 선택 조합으로 유효한 단일 조리 단계를 찾는다.
        /// </summary>
        public KitchenStageRecipeData FindSelectedCookingStage(KitchenToolType toolType)
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (recipeCatalog == null || selectedToolIngredients.Count == 0)
            {
                return null;
            }

            string signature = BuildSelectedIngredientSignature();
            if (!recipeCatalog.TryGetStage(toolType, signature, out KitchenStageRecipeData stage) || stage == null)
            {
                return null;
            }

            return IsStageAvailableToday(stage) ? stage : null;
        }

        /// <summary>
        /// 현재 선택 조합으로 바로 완성되는 요리를 찾는다.
        /// </summary>
        public KitchenDishData FindSelectedCookingDish(KitchenToolType toolType)
        {
            EnsureServices();
            KitchenStageRecipeData stage = FindSelectedCookingStage(toolType);
            if (stage == null || stage.OutputItem == null || string.IsNullOrWhiteSpace(stage.OutputItem.RecipeId))
            {
                return null;
            }

            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            return recipeCatalog != null && recipeCatalog.TryGetDish(stage.OutputItem.RecipeId, out KitchenDishData dish)
                ? dish
                : null;
        }

        /// <summary>
        /// 현재 기구가 조리 패널을 열어야 하는 상태인지 반환한다.
        /// </summary>
        public bool ShouldShowToolPanel(KitchenToolType toolType)
        {
            EnsureServices();
            if (!IsOpen || !IsCookingTool(toolType))
            {
                return false;
            }

            ToolCookingSession session = GetToolSession(toolType);
            return !session.IsCooking && !session.HasOutput;
        }

        /// <summary>
        /// 현재 선택 재료로 조리기구 작업을 시작한다.
        /// </summary>
        public bool TryStartSelectedCooking(KitchenToolType toolType, GameObject interactor)
        {
            EnsureServices();
            if (!IsOpen || !IsCookingTool(toolType) || !Carry.IsEmpty)
            {
                return false;
            }

            selectedCookingToolType = toolType;
            ToolCookingSession session = GetToolSession(toolType);
            if (session.IsCooking || session.HasOutput)
            {
                return false;
            }

            KitchenStageRecipeData stage = FindSelectedCookingStage(toolType);
            if (stage == null || !ConsumeStageInputs(stage))
            {
                return false;
            }

            KitchenCarryItem outputItem = KitchenCarryItem.FromKitchenItem(stage.OutputItem);
            if (outputItem == null)
            {
                return false;
            }

            selectedToolIngredients.Clear();
            session.Start(stage.ProgressMode, stage.CookingSeconds, outputItem);
            if (stage.ProgressMode == KitchenProgressMode.ManualHold)
            {
                ApplyMovementLock(interactor, session);
            }

            ToolGaugePresenter.SetSceneGaugeProgress(toolType, 0.01f);
            NotifyChanged(true);
            return true;
        }

        /// <summary>
        /// 기본 냉장고 재료를 손에 든다.
        /// </summary>
        public bool TryTakeBasicIngredient(ResourceData resource)
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (recipeCatalog == null || resource == null || !Carry.IsEmpty || !recipeCatalog.IsBasicIngredient(resource))
            {
                return false;
            }

            bool changed = Carry.TryHold(KitchenCarryItem.FromUnlimitedBasic(resource, 1));
            NotifyChanged(changed);
            return changed;
        }

        /// <summary>
        /// 인벤토리 재료를 예약 상태로 손에 든다.
        /// </summary>
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

        /// <summary>
        /// 들고 있던 원재료를 냉장고로 되돌리고 예약만 해제한다.
        /// </summary>
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

        /// <summary>
        /// 손에 든 단일 항목이나 레거시 묶음을 PassCounter 슬롯에 배치한다.
        /// </summary>
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

        /// <summary>
        /// PassCounter 슬롯에서 항목 하나를 집어 손으로 옮긴다.
        /// </summary>
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

        /// <summary>
        /// 레거시 호환 경로에서 현재 PassCounter 항목들을 조리용 묶음으로 변환한다.
        /// </summary>
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

        /// <summary>
        /// 레거시 호환 경로에서 PassCounter 시그니처가 완성 조건과 맞으면 최종 요리를 손에 든다.
        /// </summary>
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

        /// <summary>
        /// 레거시 호환 경로에서 PassCounter에 남은 원재료만 냉장고로 되돌린다.
        /// </summary>
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

        /// <summary>
        /// 레거시 호환 경로에서 현재 PassCounter 시그니처로 완성 가능한 오늘의 메뉴 요리를 찾는다.
        /// </summary>
        public KitchenDishData FindFinalizableDish()
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (recipeCatalog == null)
            {
                return null;
            }

            string signature = FrontWorkspace.CurrentSignature;
            return recipeCatalog.TryGetDishByFinalSignature(signature, out KitchenDishData dish)
                && IsDishAvailableToday(dish)
                ? dish
                : null;
        }

        /// <summary>
        /// 조리기구 상태와 손 상태에 따라 상호작용 프롬프트를 구성한다.
        /// </summary>
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
                return $"[E] {session.OutputItem.DisplayName} 회수";
            }

            if (session.IsCooking)
            {
                return $"{toolName} {Mathf.RoundToInt(session.NormalizedProgress * 100f)}%";
            }

            return $"[E] {toolName} 재료 선택";
        }

        /// <summary>
        /// 현재 손 상태와 조리 세션 기준으로 해당 기구를 사용할 수 있는지 반환한다.
        /// </summary>
        public bool CanUseTool(KitchenToolType toolType)
        {
            EnsureServices();
            return IsOpen && IsCookingTool(toolType);
        }

        /// <summary>
        /// 선택한 재료 조합으로 조리를 시작하거나 완료 결과물을 회수한다.
        /// </summary>
        public bool TryUseTool(KitchenToolType toolType, GameObject interactor)
        {
            EnsureServices();
            if (!IsOpen || !IsCookingTool(toolType))
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

            if (session.IsCooking)
            {
                return false;
            }

            return TryStartSelectedCooking(toolType, interactor);
        }

        /// <summary>
        /// 오늘의 메뉴 중 임의 한 개를 주문 후보로 반환한다.
        /// </summary>
        public KitchenDishData GetRandomTodayDish()
        {
            EnsureServices();
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (recipeCatalog == null || restaurantManager == null)
            {
                return null;
            }

            List<KitchenDishData> candidates = new();
            foreach (RecipeData recipe in restaurantManager.TodayMenuRecipes)
            {
                if (recipe != null && recipeCatalog.TryGetDish(recipe.RecipeId, out KitchenDishData dish))
                {
                    candidates.Add(dish);
                }
            }

            return candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// 손에 든 완성 요리가 주문 티켓과 일치하면 서빙을 완료한다.
        /// </summary>
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
                    if (ShouldShowToolPanel(stationType))
                    {
                        selectedCookingToolType = stationType;
                        CookingToolPanelRequested?.Invoke(stationType);
                        return;
                    }

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
                KitchenToolType.FrontCounter => IsOpen ? "[E] PassCounter" : "OPEN 후 사용 가능",
                KitchenToolType.CuttingBoard or KitchenToolType.Pot or KitchenToolType.FryingPan or KitchenToolType.Fryer
                    => IsOpen ? BuildToolPrompt(stationType) : "OPEN 후 사용 가능",
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

                if (ReferenceEquals(session, movementLockSession))
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
                if (HubRuntimeContext.Active != null && HubRuntimeContext.Active.RestaurantManager != null)
                {
                    restaurantManager = HubRuntimeContext.Active.RestaurantManager;
                }
                else
                {
                    restaurantManager = GetComponent<RestaurantManager>();
                }
            }

            EnsureKitchenStateEventBindings();
            foreach (KitchenToolType toolType in CookingToolTypes)
            {
                EnsureToolSession(toolType);
            }
        }

        private RestaurantRecipeCatalog ResolveRecipeCatalog()
        {
            if (HubRuntimeContext.Active != null)
            {
                return HubRuntimeContext.Active.RecipeCatalog;
            }

            standaloneRecipeCatalog ??= RestaurantRecipeCatalog.Create(
                restaurantManager != null ? restaurantManager.BootstrapRecipes : null);
            return standaloneRecipeCatalog;
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

        private bool IsDishAvailableToday(KitchenDishData dish)
        {
            if (dish == null || string.IsNullOrWhiteSpace(dish.RecipeId) || restaurantManager == null)
            {
                return false;
            }

            foreach (RecipeData recipe in restaurantManager.TodayMenuRecipes)
            {
                if (recipe != null && string.Equals(recipe.RecipeId, dish.RecipeId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsStageAvailableToday(KitchenStageRecipeData stage)
        {
            return stage != null
                && stage.OutputItem != null
                && !string.IsNullOrWhiteSpace(stage.OutputItem.RecipeId)
                && IsRecipeAvailableToday(stage.OutputItem.RecipeId);
        }

        private bool IsRecipeAvailableToday(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || restaurantManager == null)
            {
                return false;
            }

            foreach (RecipeData recipe in restaurantManager.TodayMenuRecipes)
            {
                if (recipe != null && string.Equals(recipe.RecipeId, recipeId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsToolMatchedRecipe(KitchenToolType toolType, RecipeData recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            string method = string.IsNullOrWhiteSpace(recipe.CookingMethod) ? string.Empty : recipe.CookingMethod.Trim();
            return toolType switch
            {
                KitchenToolType.CuttingBoard => method.IndexOf("도마", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.IndexOf("cutting", StringComparison.OrdinalIgnoreCase) >= 0,
                KitchenToolType.Pot => method.IndexOf("냄비", StringComparison.OrdinalIgnoreCase) >= 0
                    || string.Equals(method, "pot", StringComparison.OrdinalIgnoreCase),
                KitchenToolType.FryingPan => method.IndexOf("후라이팬", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.IndexOf("프라이팬", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.IndexOf("frying", StringComparison.OrdinalIgnoreCase) >= 0,
                KitchenToolType.Fryer => method.IndexOf("튀김기", StringComparison.OrdinalIgnoreCase) >= 0
                    || method.IndexOf("fryer", StringComparison.OrdinalIgnoreCase) >= 0,
                _ => false
            };
        }

        private bool TryResolveRecipeIngredientResource(RecipeIngredient ingredient, out ResourceData resource)
        {
            resource = null;
            if (ingredient == null)
            {
                return false;
            }

            if (ingredient.resource != null)
            {
                resource = ingredient.resource;
                return true;
            }

            resource = GeneratedGameDataLocator.FindGeneratedResource(ingredient.IngredientId, ingredient.IngredientName);
            if (resource != null)
            {
                return true;
            }

            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (recipeCatalog == null)
            {
                return false;
            }

            IReadOnlyList<ResourceData> basicIngredients = recipeCatalog.BasicIngredients;
            for (int index = 0; index < basicIngredients.Count; index++)
            {
                ResourceData basic = basicIngredients[index];
                if (basic != null && string.Equals(basic.ResourceId, ingredient.IngredientId, StringComparison.OrdinalIgnoreCase))
                {
                    resource = basic;
                    return true;
                }
            }

            return false;
        }

        private bool CanAddCookingIngredient(ResourceData resource)
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (resource == null || recipeCatalog == null)
            {
                return false;
            }

            if (recipeCatalog.IsBasicIngredient(resource))
            {
                return true;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null)
            {
                return false;
            }

            inventory.InitializeIfNeeded();
            return inventory.GetAmount(resource) > GetSelectedIngredientCount(resource);
        }

        private int GetSelectedIngredientCount(ResourceData resource)
        {
            if (resource == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < selectedToolIngredients.Count; index++)
            {
                ResourceData selected = selectedToolIngredients[index];
                if (selected != null && string.Equals(selected.ResourceId, resource.ResourceId, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        private string BuildSelectedIngredientSignature()
        {
            if (selectedToolIngredients.Count == 0)
            {
                return string.Empty;
            }

            List<KitchenCarryItem> items = new List<KitchenCarryItem>(selectedToolIngredients.Count);
            for (int index = 0; index < selectedToolIngredients.Count; index++)
            {
                ResourceData resource = selectedToolIngredients[index];
                if (resource == null)
                {
                    continue;
                }

                items.Add(new KitchenCarryItem(
                    null,
                    resource,
                    KitchenItemState.Raw,
                    1,
                    false,
                    false,
                    string.Empty,
                    null));
            }

            return KitchenSignatureUtility.BuildSignature(items);
        }

        private bool ConsumeStageInputs(KitchenStageRecipeData stage)
        {
            RestaurantRecipeCatalog recipeCatalog = ResolveRecipeCatalog();
            if (stage == null || recipeCatalog == null)
            {
                return false;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory != null)
            {
                inventory.InitializeIfNeeded();
            }

            Dictionary<ResourceData, int> required = new();
            IReadOnlyList<KitchenItemRequirement> inputs = stage.InputItems;
            for (int index = 0; index < inputs.Count; index++)
            {
                KitchenItemRequirement input = inputs[index];
                if (input == null || input.ResourceData == null)
                {
                    continue;
                }

                ResourceData resource = input.ResourceData;
                required.TryGetValue(resource, out int amount);
                required[resource] = amount + input.Quantity;
            }

            foreach (KeyValuePair<ResourceData, int> pair in required)
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                if (recipeCatalog.IsBasicIngredient(pair.Key))
                {
                    continue;
                }

                if (inventory == null || inventory.GetAmount(pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<ResourceData, int> pair in required)
            {
                if (pair.Key == null || pair.Value <= 0 || recipeCatalog.IsBasicIngredient(pair.Key))
                {
                    continue;
                }

                if (!inventory.TryRemove(pair.Key, pair.Value))
                {
                    return false;
                }
            }

            return true;
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

        private static bool IsCookingTool(KitchenToolType toolType)
        {
            return toolType == KitchenToolType.CuttingBoard
                || toolType == KitchenToolType.Pot
                || toolType == KitchenToolType.FryingPan
                || toolType == KitchenToolType.Fryer;
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

        private static bool TryResolveLegacyStationType(GameObject source, string promptLabel, out KitchenToolType stationType)
        {
            string normalizedPrompt = string.IsNullOrWhiteSpace(promptLabel) ? string.Empty : promptLabel.Trim();
            string sourceName = source != null ? source.name : string.Empty;
            string normalizedName = string.IsNullOrWhiteSpace(sourceName) ? string.Empty : sourceName.Trim();

            if (ContainsIgnoreCase(normalizedPrompt, "refrigerator") || ContainsIgnoreCase(normalizedName, "refrigerator"))
            {
                stationType = KitchenToolType.Refrigerator;
                return true;
            }

            if (ContainsIgnoreCase(normalizedPrompt, "frontcounter")
                || ContainsIgnoreCase(normalizedPrompt, "passcounter")
                || ContainsIgnoreCase(normalizedName, "frontcounter")
                || ContainsIgnoreCase(normalizedName, "passcounter")
                || ContainsIgnoreCase(normalizedPrompt, "영업 시작"))
            {
                stationType = KitchenToolType.FrontCounter;
                return true;
            }

            if (ContainsIgnoreCase(normalizedPrompt, "cuttingboard") || ContainsIgnoreCase(normalizedName, "cuttingboard"))
            {
                stationType = KitchenToolType.CuttingBoard;
                return true;
            }

            if (ContainsIgnoreCase(normalizedPrompt, "pot") || string.Equals(normalizedName, "Pot", StringComparison.OrdinalIgnoreCase))
            {
                stationType = KitchenToolType.Pot;
                return true;
            }

            if (ContainsIgnoreCase(normalizedPrompt, "fryingpan") || ContainsIgnoreCase(normalizedName, "FryingPan"))
            {
                stationType = KitchenToolType.FryingPan;
                return true;
            }

            if (ContainsIgnoreCase(normalizedPrompt, "fryer") || string.Equals(normalizedName, "Fryer", StringComparison.OrdinalIgnoreCase))
            {
                stationType = KitchenToolType.Fryer;
                return true;
            }

            stationType = default;
            return false;
        }

        private static bool ContainsIgnoreCase(string value, string token)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !string.IsNullOrWhiteSpace(token)
                && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetToolDisplayName(KitchenToolType toolType)
        {
            return toolType switch
            {
                KitchenToolType.CuttingBoard => "Cutting Board",
                KitchenToolType.Pot => "Pot",
                KitchenToolType.FryingPan => "Frying Pan",
                KitchenToolType.Fryer => "Fryer",
                KitchenToolType.Refrigerator => "Refrigerator",
                KitchenToolType.FrontCounter => "PassCounter",
                _ => toolType.ToString()
            };
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Instance = null;
        }
    }
}
