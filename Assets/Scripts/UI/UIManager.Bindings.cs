using CoreLoop.Core;
using Exploration.Player;
using Restaurant;
using TMPro;
using UI.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public partial class UIManager
    {
        // 런타임 매니저 이벤트와 선택적 UI 참조를 한 곳에서 다시 묶어 씬 재진입 시 중복 구독을 막습니다.
        private void BindSceneReferences()
        {
            cachedPlayer = FindFirstObjectByType<PlayerController>();
            BindInventory();
            BindStorage();
            BindEconomy();
            BindTools();
            BindRestaurant();
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
            cachedRestaurant = FindFirstObjectByType<RestaurantManager>();

            if (cachedRestaurant != null)
            {
                cachedRestaurant.SelectedRecipeChanged += RefreshSelectedRecipeText;
                cachedRestaurant.ServiceResultChanged += RefreshRestaurantResultText;
            }
        }

        private void UnbindRestaurant()
        {
            if (cachedRestaurant != null)
            {
                cachedRestaurant.SelectedRecipeChanged -= RefreshSelectedRecipeText;
                cachedRestaurant.ServiceResultChanged -= RefreshRestaurantResultText;
                cachedRestaurant = null;
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
            ResolveOptionalComponentReference(ref recipePanelButton, "RecipePanelButton");
            ResolveOptionalComponentReference(ref upgradePanelButton, "UpgradePanelButton");
            ResolveOptionalComponentReference(ref materialPanelButton, "MaterialPanelButton");
            ResolveOptionalComponentReference(ref popupCloseButton, "PopupCloseButton");
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

            if (recipePanelButton != null)
            {
                recipePanelButton.onClick.AddListener(HandleRecipePanelClicked);
            }

            if (upgradePanelButton != null)
            {
                upgradePanelButton.onClick.AddListener(HandleUpgradePanelClicked);
            }

            if (materialPanelButton != null)
            {
                materialPanelButton.onClick.AddListener(HandleMaterialPanelClicked);
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
            if (recipePanelButton != null)
            {
                recipePanelButton.onClick.RemoveListener(HandleRecipePanelClicked);
            }

            if (upgradePanelButton != null)
            {
                upgradePanelButton.onClick.RemoveListener(HandleUpgradePanelClicked);
            }

            if (materialPanelButton != null)
            {
                materialPanelButton.onClick.RemoveListener(HandleMaterialPanelClicked);
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
    }
}
