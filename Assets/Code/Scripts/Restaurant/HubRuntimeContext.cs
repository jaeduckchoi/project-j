using System;
using System.Collections.Generic;
using Restaurant.Kitchen;
using UnityEngine;

namespace Restaurant
{
    /// <summary>
    /// Hub 씬 런타임이 사용하는 핵심 참조와 공유 카탈로그를 한 곳에서 제공하는 명시적 컨텍스트다.
    /// </summary>
    public sealed class HubRuntimeContext : MonoBehaviour
    {
        [SerializeField] private RestaurantManager restaurantManager;
        [SerializeField] private RestaurantFlowController restaurantFlowController;
        [SerializeField] private CustomerServiceController customerServiceController;
        [SerializeField] private List<DiningTableStation> diningTables = new();
        [SerializeField] private RefrigeratorStation refrigeratorStation;
        [SerializeField] private ServiceCounterStation frontCounterStation;
        [SerializeField] private ServiceCounterStation cuttingBoardStation;
        [SerializeField] private ServiceCounterStation potStation;
        [SerializeField] private ServiceCounterStation fryingPanStation;
        [SerializeField] private ServiceCounterStation fryerStation;
        [SerializeField] private ToolGaugePresenter cuttingBoardGauge;
        [SerializeField] private ToolGaugePresenter potGauge;
        [SerializeField] private ToolGaugePresenter fryingPanGauge;
        [SerializeField] private ToolGaugePresenter fryerGauge;
        [SerializeField, Min(0.1f)] private float autoCookingSeconds = 3f;
        [SerializeField, Min(0.1f)] private float manualCookingSeconds = 2.5f;

        private readonly Dictionary<KitchenToolType, ToolGaugePresenter> gaugesByToolType = new();
        private RestaurantRecipeCatalog recipeCatalog;

        public static HubRuntimeContext Active { get; private set; }

        /// <summary>
        /// 현재 허브 씬의 RestaurantManager를 반환한다.
        /// </summary>
        public RestaurantManager RestaurantManager => restaurantManager;

        /// <summary>
        /// 현재 허브 씬의 RestaurantFlowController를 반환한다.
        /// </summary>
        public RestaurantFlowController RestaurantFlowController => restaurantFlowController;

        /// <summary>
        /// 현재 허브 씬의 CustomerServiceController를 반환한다.
        /// </summary>
        public CustomerServiceController CustomerServiceController => customerServiceController;

        /// <summary>
        /// 허브 씬에 직렬화된 테이블 상호작용 지점 목록을 반환한다.
        /// </summary>
        public IReadOnlyList<DiningTableStation> DiningTables => diningTables;

        /// <summary>
        /// 허브 씬이 공유하는 레시피/주방 카탈로그를 반환한다.
        /// </summary>
        public RestaurantRecipeCatalog RecipeCatalog
        {
            get
            {
                if (recipeCatalog == null)
                {
                    recipeCatalog = RestaurantRecipeCatalog.Create(
                        restaurantManager != null ? restaurantManager.BootstrapRecipes : null,
                        RestaurantRecipeBootstrapMode.AuthoredOnly,
                        autoCookingSeconds,
                        manualCookingSeconds);
                }

                return recipeCatalog;
            }
        }

        private void Awake()
        {
            Active = this;
            RebuildGaugeLookup();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Active, this))
            {
                Active = null;
            }
        }

        /// <summary>
        /// 현재 컨텍스트의 필수 참조와 테이블 구성이 유효한지 검사한다.
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            List<string> missing = new();
            ValidateReference(restaurantManager, nameof(restaurantManager), missing);
            ValidateReference(restaurantFlowController, nameof(restaurantFlowController), missing);
            ValidateReference(customerServiceController, nameof(customerServiceController), missing);
            ValidateReference(refrigeratorStation, nameof(refrigeratorStation), missing);
            ValidateReference(frontCounterStation, nameof(frontCounterStation), missing);
            ValidateReference(cuttingBoardStation, nameof(cuttingBoardStation), missing);
            ValidateReference(potStation, nameof(potStation), missing);
            ValidateReference(fryingPanStation, nameof(fryingPanStation), missing);
            ValidateReference(fryerStation, nameof(fryerStation), missing);
            ValidateReference(cuttingBoardGauge, nameof(cuttingBoardGauge), missing);
            ValidateReference(potGauge, nameof(potGauge), missing);
            ValidateReference(fryingPanGauge, nameof(fryingPanGauge), missing);
            ValidateReference(fryerGauge, nameof(fryerGauge), missing);

            if (diningTables == null || diningTables.Count != 3)
            {
                missing.Add("diningTables(3)");
            }
            else
            {
                HashSet<string> usedTableIds = new(StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < diningTables.Count; index++)
                {
                    DiningTableStation table = diningTables[index];
                    if (table == null)
                    {
                        missing.Add($"diningTables[{index}]");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(table.TableId))
                    {
                        missing.Add($"diningTables[{index}].tableId");
                        continue;
                    }

                    if (!usedTableIds.Add(table.TableId))
                    {
                        missing.Add($"duplicateTableId:{table.TableId}");
                    }
                }
            }

            errorMessage = missing.Count == 0
                ? string.Empty
                : $"HubRuntimeContext is missing required references: {string.Join(", ", missing)}";
            return missing.Count == 0;
        }

        /// <summary>
        /// 지정한 기구 타입의 게이지 프리젠터를 조회한다.
        /// </summary>
        public bool TryGetGaugePresenter(KitchenToolType toolType, out ToolGaugePresenter presenter)
        {
            if (gaugesByToolType.Count == 0)
            {
                RebuildGaugeLookup();
            }

            return gaugesByToolType.TryGetValue(toolType, out presenter) && presenter != null;
        }

        private void RebuildGaugeLookup()
        {
            gaugesByToolType.Clear();
            RegisterGauge(KitchenToolType.CuttingBoard, cuttingBoardGauge);
            RegisterGauge(KitchenToolType.Pot, potGauge);
            RegisterGauge(KitchenToolType.FryingPan, fryingPanGauge);
            RegisterGauge(KitchenToolType.Fryer, fryerGauge);
        }

        private void RegisterGauge(KitchenToolType toolType, ToolGaugePresenter presenter)
        {
            if (presenter != null)
            {
                gaugesByToolType[toolType] = presenter;
            }
        }

        private static void ValidateReference(UnityEngine.Object value, string label, List<string> missing)
        {
            if (value == null)
            {
                missing.Add(label);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Active = null;
        }
    }
}
