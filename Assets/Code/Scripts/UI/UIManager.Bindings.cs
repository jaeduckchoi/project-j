using CoreLoop.Core;
using Exploration.Player;
using Restaurant;
using Restaurant.Kitchen;
using UI.Layout;
using UnityEngine;

namespace UI
{
    public partial class UIManager
    {
        // 런타임 매니저 이벤트와 선택적 UI 참조를 한 곳에서 다시 묶어 씬 재진입 시 중복 구독을 막습니다.
        private void BindSceneReferences()
        {
            cachedPlayer = FindFirstObjectByType<PlayerController>();
            BindTypedPopupUi();
            BindInventory();
            BindStorage();
            BindEconomy();
            BindTools();
            BindRestaurant();
            BindCustomerService();
            BindDayCycle();
            BindUpgradeManager();
        }

        private void BindInventory()
        {
            UnbindInventory();

            if (GameManager.Instance == null)
            {
                return;
            }

            cachedInventory = GameManager.Instance.Inventory;
            if (cachedInventory != null)
            {
                cachedInventory.InventoryChanged += HandleInventoryChanged;
            }
        }

        private void UnbindInventory()
        {
            if (cachedInventory != null)
            {
                cachedInventory.InventoryChanged -= HandleInventoryChanged;
                cachedInventory = null;
            }
        }

        private void BindStorage()
        {
            UnbindStorage();

            if (GameManager.Instance == null)
            {
                return;
            }

            cachedStorage = GameManager.Instance.Storage;
            if (cachedStorage != null)
            {
                cachedStorage.StorageChanged += RefreshStorageText;
            }
        }

        private void UnbindStorage()
        {
            if (cachedStorage != null)
            {
                cachedStorage.StorageChanged -= RefreshStorageText;
                cachedStorage = null;
            }
        }

        private void BindEconomy()
        {
            UnbindEconomy();

            if (GameManager.Instance == null)
            {
                return;
            }

            cachedEconomy = GameManager.Instance.Economy;
            if (cachedEconomy != null)
            {
                cachedEconomy.GoldChanged += HandleEconomyChanged;
                cachedEconomy.ReputationChanged += HandleEconomyChanged;
            }
        }

        private void UnbindEconomy()
        {
            if (cachedEconomy != null)
            {
                cachedEconomy.GoldChanged -= HandleEconomyChanged;
                cachedEconomy.ReputationChanged -= HandleEconomyChanged;
                cachedEconomy = null;
            }
        }

        private void BindTools()
        {
            UnbindTools();

            if (GameManager.Instance == null)
            {
                return;
            }

            cachedToolManager = GameManager.Instance.Tools;
            if (cachedToolManager != null)
            {
                cachedToolManager.ToolsChanged += HandleToolsChanged;
            }
        }

        private void UnbindTools()
        {
            if (cachedToolManager != null)
            {
                cachedToolManager.ToolsChanged -= HandleToolsChanged;
                cachedToolManager = null;
            }
        }

        private void BindRestaurant()
        {
            UnbindRestaurant();
            cachedRestaurant = IsHubScene() && HubRuntimeContext.Active != null
                ? HubRuntimeContext.Active.RestaurantManager
                : FindFirstObjectByType<RestaurantManager>();

            if (cachedRestaurant != null)
            {
                cachedRestaurant.SelectedRecipeChanged += RefreshSelectedRecipeText;
                cachedRestaurant.ServiceResultChanged += RefreshRestaurantResultText;
                cachedRestaurant.TodayMenuChanged += HandleTodayMenuChanged;
                cachedRestaurant.ServiceStateChanged += HandleRestaurantServiceStateChanged;
            }
        }

        private void UnbindRestaurant()
        {
            if (cachedRestaurant != null)
            {
                cachedRestaurant.SelectedRecipeChanged -= RefreshSelectedRecipeText;
                cachedRestaurant.ServiceResultChanged -= RefreshRestaurantResultText;
                cachedRestaurant.TodayMenuChanged -= HandleTodayMenuChanged;
                cachedRestaurant.ServiceStateChanged -= HandleRestaurantServiceStateChanged;
                cachedRestaurant = null;
            }
        }

        private void BindCustomerService()
        {
            UnbindCustomerService();
            cachedCustomerService = IsHubScene() && HubRuntimeContext.Active != null
                ? HubRuntimeContext.Active.CustomerServiceController
                : FindFirstObjectByType<CustomerServiceController>();
            if (cachedCustomerService != null)
            {
                cachedCustomerService.TicketsChanged += HandleCustomerTicketsChanged;
            }
        }

        private void UnbindCustomerService()
        {
            if (cachedCustomerService != null)
            {
                cachedCustomerService.TicketsChanged -= HandleCustomerTicketsChanged;
                cachedCustomerService = null;
            }
        }

        private void BindDayCycle()
        {
            UnbindDayCycle();

            if (GameManager.Instance == null)
            {
                return;
            }

            cachedDayCycle = GameManager.Instance.DayCycle;
            if (cachedDayCycle != null)
            {
                cachedDayCycle.StateChanged += RefreshDayCycleState;
            }
        }

        private void UnbindDayCycle()
        {
            if (cachedDayCycle != null)
            {
                cachedDayCycle.StateChanged -= RefreshDayCycleState;
                cachedDayCycle = null;
            }
        }

        private void BindUpgradeManager()
        {
            UnbindUpgradeManager();

            if (GameManager.Instance == null)
            {
                return;
            }

            cachedUpgradeManager = GameManager.Instance.Upgrades;
            if (cachedUpgradeManager != null)
            {
                cachedUpgradeManager.UpgradeStateChanged += RefreshUpgradeText;
            }
        }

        private void UnbindUpgradeManager()
        {
            if (cachedUpgradeManager != null)
            {
                cachedUpgradeManager.UpgradeStateChanged -= RefreshUpgradeText;
                cachedUpgradeManager = null;
            }
        }

        private void ResolveOptionalUiReferences()
        {
            BindTypedPopupUi();
            if (ShouldUseTypedPopupUi())
            {
                recipePanelButton = null;
                upgradePanelButton = null;
                materialPanelButton = null;
                openRestaurantButton = null;
                closeRestaurantButton = null;
                popupCloseButton = popupFrameUi != null ? popupFrameUi.CloseButton : null;
            }
            else
            {
                ResolveOptionalComponentReference(ref recipePanelButton, "RecipePanelButton");
                ResolveOptionalComponentReference(ref upgradePanelButton, "UpgradePanelButton");
                ResolveOptionalComponentReference(ref materialPanelButton, "MaterialPanelButton");
                ResolveOptionalComponentReference(ref openRestaurantButton, "OpenRestaurantButton");
                ResolveOptionalComponentReference(ref closeRestaurantButton, "CloseRestaurantButton");
                ResolveOptionalComponentReference(ref popupCloseButton, "PopupCloseButton");
            }

            ResolveOptionalComponentReference(ref guideHelpButton, "GuideHelpButton");

            string economyTextObjectName = EconomyTextObjectName(IsHubScene());
            ResolveOptionalComponentReference(ref goldText, economyTextObjectName);
            ResolveOptionalComponentReference(ref guideText, "GuideText");
            ResolveOptionalComponentReference(ref resultText, "RestaurantResultText");
        }

        private void ResolveOptionalComponentReference<T>(ref T component, string objectName) where T : Component
        {
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                component = null;
                return;
            }

            if (component != null)
            {
                return;
            }

            Transform targetTransform = FindNamedUiTransform(objectName);
            if (targetTransform != null)
            {
                component = targetTransform.GetComponent<T>();
            }
        }

        /// <summary>
        /// 허브 팝업 버튼에 현재 씬 기준 클릭 리스너를 연결합니다.
        /// </summary>
        private void BindButtons()
        {
            UnbindButtons();
            ResolveOptionalUiReferences();

            if (!ShouldUseTypedPopupUi() && recipePanelButton != null)
            {
                recipePanelButton.onClick.AddListener(HandleRecipePanelClicked);
            }

            if (!ShouldUseTypedPopupUi() && upgradePanelButton != null)
            {
                upgradePanelButton.onClick.AddListener(HandleUpgradePanelClicked);
            }

            if (!ShouldUseTypedPopupUi() && materialPanelButton != null)
            {
                materialPanelButton.onClick.AddListener(HandleMaterialPanelClicked);
            }

            if (!ShouldUseTypedPopupUi() && openRestaurantButton != null)
            {
                openRestaurantButton.onClick.AddListener(HandleOpenRestaurantButtonClicked);
            }

            if (!ShouldUseTypedPopupUi() && closeRestaurantButton != null)
            {
                closeRestaurantButton.onClick.AddListener(HandleCloseRestaurantButtonClicked);
            }

            if (popupCloseButton != null)
            {
                popupCloseButton.onClick.AddListener(HandlePopupCloseButtonClicked);
            }

            if (guideHelpButton != null)
            {
                guideHelpButton.onClick.AddListener(HandleGuideHelpButtonClicked);
            }
        }

        /// <summary>
        /// 버튼 리스너를 해제해 씬 재진입 시 중복 구독을 막습니다.
        /// </summary>
        private void UnbindButtons()
        {
            if (!ShouldUseTypedPopupUi() && recipePanelButton != null)
            {
                recipePanelButton.onClick.RemoveListener(HandleRecipePanelClicked);
            }

            if (!ShouldUseTypedPopupUi() && upgradePanelButton != null)
            {
                upgradePanelButton.onClick.RemoveListener(HandleUpgradePanelClicked);
            }

            if (!ShouldUseTypedPopupUi() && materialPanelButton != null)
            {
                materialPanelButton.onClick.RemoveListener(HandleMaterialPanelClicked);
            }

            if (!ShouldUseTypedPopupUi() && openRestaurantButton != null)
            {
                openRestaurantButton.onClick.RemoveListener(HandleOpenRestaurantButtonClicked);
            }

            if (!ShouldUseTypedPopupUi() && closeRestaurantButton != null)
            {
                closeRestaurantButton.onClick.RemoveListener(HandleCloseRestaurantButtonClicked);
            }

            if (popupCloseButton != null)
            {
                popupCloseButton.onClick.RemoveListener(HandlePopupCloseButtonClicked);
            }

            if (guideHelpButton != null)
            {
                guideHelpButton.onClick.RemoveListener(HandleGuideHelpButtonClicked);
            }
        }

        private void HandleRecipePanelClicked()
        {
            ToggleHubPanel(HubPopupPanel.Recipe);
        }

        private void HandleUpgradePanelClicked()
        {
            ToggleHubPanel(HubPopupPanel.Upgrade);
        }

        private void HandleMaterialPanelClicked()
        {
            ToggleHubPanel(HubPopupPanel.Materials);
        }

        private void HandlePopupCloseButtonClicked()
        {
            CloseActiveHubPanel();
        }

        private void HandleGuideHelpButtonClicked()
        {
            showGuideHelpOverlay = !showGuideHelpOverlay;
            RefreshGuideText();
        }

        private void HandleOpenRestaurantButtonClicked()
        {
            if (cachedRestaurant != null)
            {
                cachedRestaurant.TryOpenRestaurant();
            }

            RefreshAll();
        }

        private void HandleCloseRestaurantButtonClicked()
        {
            int activeTicketCount = cachedCustomerService != null ? cachedCustomerService.ActiveTicketCount : 0;
            if (cachedRestaurant != null)
            {
                cachedRestaurant.TryCloseRestaurant(activeTicketCount);
            }

            RefreshAll();
        }

        private void HandleTodayMenuChanged()
        {
            RefreshAll();
        }

        private void HandleRestaurantServiceStateChanged(bool _)
        {
            RefreshAll();
        }

        private void HandleCustomerTicketsChanged()
        {
            RefreshAll();
        }
    }
}
