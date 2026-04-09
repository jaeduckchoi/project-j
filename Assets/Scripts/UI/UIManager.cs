using System;
using System.Collections.Generic;
using System.Text;
using CoreLoop.Core;
using Shared.Data;
using Management.Economy;
using CoreLoop.Flow;
using Exploration.Gathering;
using Management.Inventory;
using Exploration.Interaction;
using Exploration.Player;
using Restaurant;
using Management.Storage;
using TMPro;
using Management.Tools;
using Shared;
using UI.Content;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using Management.Upgrade;
using Exploration.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
#endif

// UI 네임스페이스
namespace UI
{
    /// <summary>
    /// 현재 HUD, 허브 팝업, 창고 패널을 한곳에서 갱신하는 최소 UI 관리자입니다.
    /// 코인/평판, 재료, 업그레이드, 상호작용 문구, 단계 표시를 모두 여기서 동기화합니다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "UIManager")]
    public class UIManager : MonoBehaviour
    {
        private const string HudRootName = "HUDRoot";
        private const string PopupRootName = "PopupRoot";
        private const string HudStatusGroupName = "HUDStatusGroup";
        private const string HudActionGroupCanonicalName = "HUDActionGroup";
        private const string HudBottomGroupName = "HUDBottomGroup";
        private const string HudOverlayGroupName = "HUDOverlayGroup";
        private const string HudPanelButtonGroupCanonicalName = "HUDPanelButtonGroup";
        private const string PopupShellGroupName = "PopupShellGroup";
        private const string PopupFrameGroupName = "PopupFrame";
        private const string PopupFrameHeaderGroupName = "PopupFrameHeader";
        private const string PopupFrameLeftGroupName = "PopupFrameLeft";
        private const string PopupFrameRightGroupName = "PopupFrameRight";
        private static string HudActionGroupName => PrototypeUISceneLayoutCatalog.ResolveObjectName(HudActionGroupCanonicalName);
        private static string HudPanelButtonGroupObjectName => PrototypeUISceneLayoutCatalog.ResolveObjectName(HudPanelButtonGroupCanonicalName);
        private static readonly Color PopupItemBoxFallbackColor = new(0.96f, 0.92f, 0.78f, 1f);
        private static readonly Color PopupItemBoxSelectedColor = new(1f, 0.97f, 0.84f, 1f);
        private static readonly CaptionPresentationPreset DefaultCaptionPresentation = new(15f, true, Vector4.zero, 0.5f, 12f, 15f);
        private static readonly CaptionPresentationPreset FixedPopupTitlePresentation = new(40f, false, new Vector4(10f, 8f, 10f, 8f), 0f, 12f, 24f);
        private static readonly CaptionPresentationPreset FixedPopupCaptionPresentation = new(32f, false, new Vector4(10f, 8f, 10f, 8f), 0f, 12f, 20f);

        private static readonly HashSet<string> PopupCanvasObjectNames = new()
        {
            "PopupOverlay",
            "PopupFrame",
            "PopupFrameLeft",
            "PopupFrameRight",
            PrototypeUIObjectNames.PopupTitle,
            "PopupCloseButton",
            "PopupLeftBody",
            "PopupRightBody",
            PrototypeUIObjectNames.PopupLeftCaption,
            PrototypeUIObjectNames.PopupRightCaption,
            "StorageText",
            "SelectedRecipeText",
            "UpgradeText"
        };

        private static IEnumerable<string> EnumerateHudCanvasObjectNames()
        {
            yield return "TopLeftPanel";
            yield return "InteractionPromptBackdrop";
            yield return "GuideBackdrop";
            yield return "ResultBackdrop";
            yield return "GoldText";
            yield return "InteractionPromptText";
            yield return "GuideText";
            yield return "RestaurantResultText";
            yield return "GuideHelpButton";
            yield return HudPanelButtonGroupObjectName;
            yield return "ActionDock";
            yield return "ActionAccent";
            yield return "ActionCaption";
            yield return "RecipePanelButton";
            yield return "UpgradePanelButton";
            yield return "MaterialPanelButton";
            yield return "StorageCard";
            yield return "RecipeCard";
            yield return "UpgradeCard";
            yield return "StorageAccent";
            yield return "RecipeAccent";
            yield return "UpgradeAccent";
            yield return "StorageCaption";
            yield return "RecipeCaption";
            yield return "UpgradeCaption";
        }

        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private TextMeshProUGUI inventoryText;
        [SerializeField] private TextMeshProUGUI storageText;
        [SerializeField] private TextMeshProUGUI upgradeText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI selectedRecipeText;
        [SerializeField] private TextMeshProUGUI guideText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TMP_FontAsset bodyFontAsset;
        [SerializeField] private TMP_FontAsset headingFontAsset;
        [SerializeField] private Button recipePanelButton;
        [SerializeField] private Button upgradePanelButton;
        [SerializeField] private Button materialPanelButton;
        [SerializeField] private Button guideHelpButton;
        [SerializeField] private Button popupCloseButton;
        [SerializeField] private string defaultPromptText = "이동: WASD / 방향키   상호작용: E";

        private PlayerController cachedPlayer;
        private InventoryManager cachedInventory;
        private StorageManager cachedStorage;
        private EconomyManager cachedEconomy;
        private ToolManager cachedToolManager;
        private RestaurantManager cachedRestaurant;
        private DayCycleManager cachedDayCycle;
        private UpgradeManager cachedUpgradeManager;
        private HubPopupPanel activeHubPanel = HubPopupPanel.None;
        private ResourceData selectedMaterialPopupResource;
        private ResourceData selectedStoragePopupResource;
        private string selectedUpgradePopupKey;
        private bool showGuideHelpOverlay;
        private bool isPopupPauseApplied;
        private float popupPausePreviousTimeScale = 1f;
        private bool suppressCanvasGroupingInEditorPreview;

#if ENABLE_INPUT_SYSTEM
        private static InputActionAsset _runtimeUiActionsAsset;
#endif

        private enum HubPopupPanel
        {
            None,
            Storage,
            Recipe,
            Upgrade,
            Materials
        }

        private readonly struct CaptionPresentationPreset
        {
            public CaptionPresentationPreset(float fontSize, bool enableAutoSizing, Vector4 margin, float characterSpacing, float fontSizeMin, float fontSizeMax)
            {
                FontSize = fontSize;
                EnableAutoSizing = enableAutoSizing;
                Margin = margin;
                CharacterSpacing = characterSpacing;
                FontSizeMin = fontSizeMin;
                FontSizeMax = fontSizeMax;
            }

            public float FontSize { get; }
            public bool EnableAutoSizing { get; }
            public Vector4 Margin { get; }
            public float CharacterSpacing { get; }
            public float FontSizeMin { get; }
            public float FontSizeMax { get; }
        }

        private sealed class PopupListEntry
        {
            public PopupListEntry(string key, string title, string summary, string detail, Sprite icon, bool isSelected, Action onSelected)
            {
                Key = key;
                Title = title;
                Summary = summary;
                Detail = detail;
                Icon = icon;
                IsSelected = isSelected;
                OnSelected = onSelected;
            }

            public string Key { get; }
            public string Title { get; }
            public string Summary { get; }
            public string Detail { get; }
            public Sprite Icon { get; }
            public bool IsSelected { get; }
            public Action OnSelected { get; }
        }

        private sealed class PopupPanelContent
        {
            public PopupPanelContent(List<PopupListEntry> entries, string detailText)
            {
                Entries = entries ?? new List<PopupListEntry>();
                DetailText = detailText ?? string.Empty;
            }

            public List<PopupListEntry> Entries { get; }
            public string DetailText { get; }
        }

        private void Awake()
        {
            EnsureEventSystemExists();
        }

        /// <summary>
        /// 씬 전환 뒤에도 UI 바인딩을 다시 연결할 수 있도록 sceneLoaded 콜백을 등록합니다.
        /// </summary>
        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            StorageStation.StoragePanelRequested += HandleStoragePanelRequested;
        }

        /// <summary>
        /// 시작 시점에 현재 씬 기준 참조를 다시 찾고 전체 UI를 한 번 갱신합니다.
        /// </summary>
        private void Start()
        {
            BindSceneReferences();
            ApplyTextPresentation();
            BindButtons();
            RefreshAll();
        }

        /// <summary>
        /// 매 프레임 상호작용 문구와 버튼 표시 상태를 최신 상태로 유지합니다.
        /// </summary>
        private void Update()
        {
            HandlePopupCloseInput();
            HandleStoragePopupInput();
            RefreshInteractionPrompt();
            RefreshButtonStates();
        }

        /// <summary>
        /// 등록했던 이벤트와 버튼 리스너를 정리해 중복 호출을 막습니다.
        /// </summary>
        private void OnDisable()
        {
            RestorePopupPauseIfNeeded();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            StorageStation.StoragePanelRequested -= HandleStoragePanelRequested;
            UnbindInventory();
            UnbindStorage();
            UnbindEconomy();
            UnbindTools();
            UnbindRestaurant();
            UnbindDayCycle();
            UnbindUpgradeManager();
            UnbindButtons();
        }

        /// <summary>
        /// 창고 상호작용이 UI 계층으로 전달되면 현재 씬 조건을 다시 확인한 뒤 창고 팝업을 엽니다.
        /// </summary>
        private void HandleStoragePanelRequested()
        {
            ShowStoragePanel();
        }

#if UNITY_EDITOR
        public void ApplyEditorDesignPreview(bool showPopupPreview, PrototypeUIPreviewPanel previewPanel)
        {
            if (Application.isPlaying)
            {
                return;
            }

            PrototypeUISkin.ClearGeneratedCache();
            bool previousSuppressState = suppressCanvasGroupingInEditorPreview;
            suppressCanvasGroupingInEditorPreview = true;

            try
            {
                ResolveOptionalUiReferences();
                ApplyTextPresentation();
                ApplyEditorPreviewState(showPopupPreview, previewPanel);
            }
            finally
            {
                suppressCanvasGroupingInEditorPreview = previousSuppressState;
            }
        }

        public void ClearEditorDesignPreview()
        {
            ApplyEditorDesignPreview(false, PrototypeUIPreviewPanel.None);
        }

        public void OrganizeCanvasHierarchyInEditor()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureCanvasGroups();
            ResolveOptionalUiReferences();
        }

        private void ApplyEditorPreviewState(bool showPopupPreview, PrototypeUIPreviewPanel previewPanel)
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (goldText != null && string.IsNullOrWhiteSpace(goldText.text))
            {
                goldText.text = "골드 120 · 평판 4";
            }

            if (interactionPromptText != null && string.IsNullOrWhiteSpace(interactionPromptText.text))
            {
                interactionPromptText.text = defaultPromptText;
            }

            activeHubPanel = IsHubScene() && showPopupPreview ? ConvertPreviewPanel(previewPanel) : HubPopupPanel.None;
            ApplyMenuPanelState();

            if (guideText != null)
            {
                guideText.text = activeHubPanel == HubPopupPanel.None
                    ? IsHubScene()
                        ? "하단 버튼과 기본 HUD 배치를 편집 모드에서 확인하는 프리뷰입니다."
                        : "탐험 HUD 카드 배치를 편집 모드에서 확인하는 프리뷰입니다."
                    : "현재 팝업 스킨과 캡션 배치를 편집 모드에서 확인하는 프리뷰입니다.";
                guideText.gameObject.SetActive(true);
                SetNamedObjectActive("GuideBackdrop", true);
            }

            if (resultText != null)
            {
                bool showResultPreview = !IsHubScene() && activeHubPanel == HubPopupPanel.None;
                resultText.text = showResultPreview ? "탐험 및 영업 결과 문구가 이 위치에 표시됩니다." : string.Empty;
                resultText.gameObject.SetActive(showResultPreview);
                SetNamedObjectActive("ResultBackdrop", showResultPreview);
            }

            if (activeHubPanel != HubPopupPanel.None)
            {
                ApplyEditorPreviewPopupText(previewPanel);
            }
            else if (!IsHubScene() && inventoryText != null)
            {
                inventoryText.text = PrototypeUIPopupCatalog.GetExplorationInventoryPreviewText();
            }
        }

        private void ApplyEditorPreviewPopupText(PrototypeUIPreviewPanel previewPanel)
        {
            PrototypeUIPreviewContent previewContent = PrototypeUIPopupCatalog.GetPreviewContent(previewPanel);
            PopupPanelContent popupContent = BuildPreviewPopupContent(previewContent);

            if (inventoryText != null)
            {
                inventoryText.text = string.Empty;
            }

            if (selectedRecipeText != null)
            {
                selectedRecipeText.text = popupContent.DetailText;
            }

            RefreshPopupBodyItemBoxes(popupContent.Entries);
        }

        private static PopupPanelContent BuildPreviewPopupContent(PrototypeUIPreviewContent previewContent)
        {
            List<PopupListEntry> entries = new();
            if (!string.IsNullOrWhiteSpace(previewContent.LeftText))
            {
                string normalized = previewContent.LeftText.Replace("\r\n", "\n").Replace('\r', '\n');
                string[] lines = normalized.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        continue;
                    }

                    string title = trimmed.StartsWith("- ")
                        ? trimmed.Substring(2).Trim()
                        : trimmed;
                    title = title.Replace("[선택] ", string.Empty);
                    entries.Add(new PopupListEntry(
                        $"preview:{i}",
                        title,
                        "편집 모드 프리뷰 항목",
                        previewContent.RightText,
                        null,
                        entries.Count == 0,
                        null));
                }
            }

            return new PopupPanelContent(entries, previewContent.RightText);
        }

        private static HubPopupPanel ConvertPreviewPanel(PrototypeUIPreviewPanel previewPanel)
        {
            return previewPanel switch
            {
                PrototypeUIPreviewPanel.None => HubPopupPanel.None,
                PrototypeUIPreviewPanel.Storage => HubPopupPanel.Storage,
                PrototypeUIPreviewPanel.Recipe => HubPopupPanel.Recipe,
                PrototypeUIPreviewPanel.Upgrade => HubPopupPanel.Upgrade,
                PrototypeUIPreviewPanel.Materials => HubPopupPanel.Materials,
                _ => HubPopupPanel.None
            };
        }

#endif

        private static PrototypeUIPreviewPanel ConvertRuntimePopupPanel(HubPopupPanel popupPanel)
        {
            return popupPanel switch
            {
                HubPopupPanel.None => PrototypeUIPreviewPanel.None,
                HubPopupPanel.Storage => PrototypeUIPreviewPanel.Storage,
                HubPopupPanel.Recipe => PrototypeUIPreviewPanel.Recipe,
                HubPopupPanel.Upgrade => PrototypeUIPreviewPanel.Upgrade,
                HubPopupPanel.Materials => PrototypeUIPreviewPanel.Materials,
                _ => PrototypeUIPreviewPanel.None
            };
        }

        /// <summary>
        /// 씬 전환 직후 새 씬의 플레이어와 매니저 참조를 다시 묶습니다.
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEventSystemExists();
            activeHubPanel = HubPopupPanel.None;
            BindSceneReferences();
            ApplyTextPresentation();
            BindButtons();
            RefreshAll();
        }

        private static void EnsureEventSystemExists()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureInputSystemUiModule(inputModule);
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Input System 기본 액션 생성 경로 대신 런타임 UI 액션 에셋을 직접 연결합니다.
        /// 이렇게 해야 InputActionReference 생성 예외 없이 버튼 입력을 안정적으로 받을 수 있습니다.
        /// </summary>
        private static void ConfigureInputSystemUiModule(InputSystemUIInputModule inputModule)
        {
            InputActionAsset asset = EnsureRuntimeUiInputActionsAsset();

            inputModule.actionsAsset = asset;
            inputModule.point = InputActionReference.Create(asset.FindAction("UI/Point", true));
            inputModule.leftClick = InputActionReference.Create(asset.FindAction("UI/LeftClick", true));
            inputModule.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick", true));
            inputModule.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick", true));
            inputModule.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel", true));
            inputModule.move = InputActionReference.Create(asset.FindAction("UI/Move", true));
            inputModule.submit = InputActionReference.Create(asset.FindAction("UI/Submit", true));
            inputModule.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", true));
            inputModule.trackedDevicePosition = InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition", true));
            inputModule.trackedDeviceOrientation = InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation", true));
        }

        private static InputActionAsset EnsureRuntimeUiInputActionsAsset()
        {
            if (_runtimeUiActionsAsset != null)
            {
                return _runtimeUiActionsAsset;
            }

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "RuntimeUiInputActions";
            asset.hideFlags = HideFlags.HideAndDontSave;

            InputActionMap uiMap = new("UI");
            asset.AddActionMap(uiMap);

            InputAction pointAction = uiMap.AddAction("Point", InputActionType.PassThrough);
            pointAction.expectedControlType = "Vector2";
            pointAction.AddBinding("<Mouse>/position");
            pointAction.AddBinding("<Pen>/position");
            pointAction.AddBinding("<Touchscreen>/primaryTouch/position");

            InputAction leftClickAction = uiMap.AddAction("LeftClick", InputActionType.PassThrough);
            leftClickAction.expectedControlType = "Button";
            leftClickAction.AddBinding("<Mouse>/leftButton");
            leftClickAction.AddBinding("<Pen>/tip");
            leftClickAction.AddBinding("<Touchscreen>/primaryTouch/press");

            InputAction rightClickAction = uiMap.AddAction("RightClick", InputActionType.PassThrough);
            rightClickAction.expectedControlType = "Button";
            rightClickAction.AddBinding("<Mouse>/rightButton");

            InputAction middleClickAction = uiMap.AddAction("MiddleClick", InputActionType.PassThrough);
            middleClickAction.expectedControlType = "Button";
            middleClickAction.AddBinding("<Mouse>/middleButton");

            InputAction scrollWheelAction = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough);
            scrollWheelAction.expectedControlType = "Vector2";
            scrollWheelAction.AddBinding("<Mouse>/scroll");

            InputAction moveAction = uiMap.AddAction("Move", InputActionType.PassThrough);
            moveAction.expectedControlType = "Vector2";
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            InputAction submitAction = uiMap.AddAction("Submit", InputActionType.Button);
            submitAction.expectedControlType = "Button";
            submitAction.AddBinding("<Keyboard>/enter");
            submitAction.AddBinding("<Keyboard>/numpadEnter");
            submitAction.AddBinding("<Keyboard>/space");
            submitAction.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancelAction = uiMap.AddAction("Cancel", InputActionType.Button);
            cancelAction.expectedControlType = "Button";
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.AddBinding("<Gamepad>/buttonEast");

            InputAction trackedPositionAction = uiMap.AddAction("TrackedDevicePosition", InputActionType.PassThrough);
            trackedPositionAction.expectedControlType = "Vector3";
            trackedPositionAction.AddBinding("<XRController>/devicePosition");

            InputAction trackedOrientationAction = uiMap.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough);
            trackedOrientationAction.expectedControlType = "Quaternion";
            trackedOrientationAction.AddBinding("<XRController>/deviceRotation");

            _runtimeUiActionsAsset = asset;
            return _runtimeUiActionsAsset;
        }
#endif

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
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved("RecipePanelButton"))
            {
                recipePanelButton = null;
            }
            else if (recipePanelButton == null)
            {
                Transform recipeTransform = FindNamedUiTransform("RecipePanelButton");
                if (recipeTransform != null)
                {
                    recipePanelButton = recipeTransform.GetComponent<Button>();
                }
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved("UpgradePanelButton"))
            {
                upgradePanelButton = null;
            }
            else if (upgradePanelButton == null)
            {
                Transform upgradeTransform = FindNamedUiTransform("UpgradePanelButton");
                if (upgradeTransform != null)
                {
                    upgradePanelButton = upgradeTransform.GetComponent<Button>();
                }
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved("MaterialPanelButton"))
            {
                materialPanelButton = null;
            }
            else if (materialPanelButton == null)
            {
                Transform materialTransform = FindNamedUiTransform("MaterialPanelButton");
                if (materialTransform != null)
                {
                    materialPanelButton = materialTransform.GetComponent<Button>();
                }
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved("PopupCloseButton"))
            {
                popupCloseButton = null;
            }
            else if (popupCloseButton == null)
            {
                Transform closeTransform = FindNamedUiTransform("PopupCloseButton");
                if (closeTransform != null)
                {
                    popupCloseButton = closeTransform.GetComponent<Button>();
                }
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved("GuideHelpButton"))
            {
                guideHelpButton = null;
            }
            else if (guideHelpButton == null)
            {
                Transform helpTransform = FindNamedUiTransform("GuideHelpButton");
                if (helpTransform != null)
                {
                    guideHelpButton = helpTransform.GetComponent<Button>();
                }
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved("GuideText"))
            {
                guideText = null;
            }
            else if (guideText == null)
            {
                Transform guideTransform = FindNamedUiTransform("GuideText");
                if (guideTransform != null)
                {
                    guideText = guideTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved("RestaurantResultText"))
            {
                resultText = null;
            }
            else if (resultText == null)
            {
                Transform resultTransform = FindNamedUiTransform("RestaurantResultText");
                if (resultTransform != null)
                {
                    resultText = resultTransform.GetComponent<TextMeshProUGUI>();
                }
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

        public void ShowStoragePanel()
        {
            if (!IsHubScene() || !IsPlayerNearStorageStation())
            {
                return;
            }

            activeHubPanel = HubPopupPanel.Storage;
            RefreshStorageText();
            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
        }

        private void ToggleHubPanel(HubPopupPanel targetPanel)
        {
            if (!IsHubScene())
            {
                return;
            }

            activeHubPanel = activeHubPanel == targetPanel ? HubPopupPanel.None : targetPanel;
            ApplyMenuPanelState();
        }

        private void HandlePopupCloseInput()
        {
            if (activeHubPanel == HubPopupPanel.None || !ReadPopupClosePressed())
            {
                return;
            }

            CloseActiveHubPanel();
        }

        private void CloseActiveHubPanel()
        {
            if (activeHubPanel == HubPopupPanel.None)
            {
                return;
            }

            activeHubPanel = HubPopupPanel.None;
            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
        }

        private static bool ReadPopupClosePressed()
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                pressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Escape);
#endif

            return pressed;
        }

        private void HandleStoragePopupInput()
        {
            if (activeHubPanel != HubPopupPanel.Storage || cachedStorage == null || GameManager.Instance == null || GameManager.Instance.Inventory == null)
            {
                return;
            }

            InventoryManager inventory = GameManager.Instance.Inventory;
            bool changed = false;

            if (ReadPopupActionPressed(KeyCode.Q, keyboard => keyboard.qKey))
            {
                changed |= cachedStorage.CycleInventorySelection(inventory);
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_select_deposit",
                    "왼쪽 목록에서 맡길 재료를 고르고 맡기기 동작으로 창고에 보관할 수 있습니다.");
            }

            if (ReadPopupActionPressed(KeyCode.W, keyboard => keyboard.wKey))
            {
                if (GameManager.Instance != null
                    && GameManager.Instance.RemoteSession != null
                    && GameManager.Instance.RemoteSession.TryStoreSelected(cachedStorage, inventory))
                {
                    changed = true;
                }
                else
                {
                    changed |= cachedStorage.StoreSelectedFromInventory(inventory) > 0;
                }
            }

            if (ReadPopupActionPressed(KeyCode.A, keyboard => keyboard.aKey))
            {
                changed |= cachedStorage.CycleStoredSelection();
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_storage_select_withdraw",
                    "보관 목록에서 꺼낼 재료를 고른 뒤 꺼내기 동작으로 가방으로 되돌릴 수 있습니다.");
            }

            if (ReadPopupActionPressed(KeyCode.S, keyboard => keyboard.sKey))
            {
                if (GameManager.Instance != null
                    && GameManager.Instance.RemoteSession != null
                    && GameManager.Instance.RemoteSession.TryWithdrawSelected(cachedStorage, inventory))
                {
                    changed = true;
                }
                else
                {
                    changed |= cachedStorage.WithdrawSelectedToInventory(inventory) > 0;
                }
            }

            if (changed)
            {
                RefreshAll();
            }
            else
            {
                RefreshHubPopupContent();
            }
        }

        private static bool ReadPopupActionPressed(KeyCode legacyKey, Func<Keyboard, KeyControl> keySelector)
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                KeyControl key = keySelector(keyboard);
                if (key != null && key.wasPressedThisFrame)
                {
                    pressed = true;
                }
            }
#endif

#if !ENABLE_LEGACY_INPUT_MANAGER
            _ = legacyKey;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(legacyKey);
#endif

            return pressed;
        }

        private void HandleInventoryChanged()
        {
            RefreshInventoryText();
            RefreshStorageText();
            RefreshSelectedRecipeText(cachedRestaurant != null ? cachedRestaurant.SelectedRecipe : null);
            RefreshUpgradeText();
        }

        private void HandleEconomyChanged(int _)
        {
            RefreshEconomyText();
            RefreshUpgradeText();
        }

        private void HandleToolsChanged()
        {
            RefreshUpgradeText();
        }

        /// <summary>
        /// 해상도 차이로 HUD가 무너지지 않도록 캔버스 스케일 기준을 고정합니다.
        /// </summary>
        private void ApplyCanvasScaleSettings()
        {
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        private void EnsureCanvasGroups()
        {
            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                return;
            }

            if (transform == null)
            {
                return;
            }

            RemoveDeletedCanvasObjects();

            Transform hudRoot = EnsureCanvasGroupRoot(HudRootName, 0);
            Transform popupRoot = EnsureCanvasGroupRoot(PopupRootName, 1);

            EnsureHudSubgroupRoots(hudRoot);
            EnsurePopupSubgroupRoots(popupRoot);
            ReparentHudCanvasObjects(hudRoot);
            ReparentPopupCanvasObjects(popupRoot);
            ReparentCanvasObject("InventoryText", IsHubScene() ? GetPopupCanvasGroupParent("InventoryText", popupRoot) : GetHudCanvasGroupParent("InventoryText", hudRoot));
            ApplySavedCanvasHierarchyOverrides();
            RemoveDeletedCanvasObjects();
        }

        private Transform EnsureCanvasGroupRoot(string groupName, int siblingIndex)
        {
            return EnsureCanvasGroupRoot(transform, groupName, siblingIndex);
        }

        private static Transform EnsureCanvasGroupRoot(Transform parent, string groupName, int siblingIndex)
        {
            if (parent == null
                || string.IsNullOrWhiteSpace(groupName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(groupName))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                groupName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));

            Transform existing = parent.Find(groupName);
            if (existing == null)
            {
                existing = FindNamedUiTransformRecursive(parent, groupName);
            }

            GameObject rootObject = existing != null ? existing.gameObject : new GameObject(groupName, typeof(RectTransform));
            ApplyHubPopupObjectIdentity(rootObject);
            if (existing == null)
            {
                rootObject.transform.SetParent(parent, false);
            }

            RectTransform rect = rootObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = rootObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            Transform siblingParent = rect.parent != null ? rect.parent : parent;
            rect.SetSiblingIndex(ClampSiblingIndex(siblingParent, siblingIndex));
            return rect;
        }

        private void EnsureHudSubgroupRoots(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            EnsureCanvasGroupRoot(hudRoot, HudStatusGroupName, 0);
            EnsureCanvasGroupRoot(hudRoot, HudActionGroupName, 1);
            EnsureCanvasGroupRoot(hudRoot, HudBottomGroupName, 2);

            if (IsHubScene() || hudRoot.Find(HudPanelButtonGroupObjectName) != null)
            {
                EnsureCanvasGroupRoot(hudRoot, HudPanelButtonGroupObjectName, 3);
            }

            EnsureCanvasGroupRoot(hudRoot, HudOverlayGroupName, 4);
        }

        private void ReparentHudCanvasObjects(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            foreach (string objectName in EnumerateHudCanvasObjectNames())
            {
                ReparentCanvasObject(objectName, GetHudCanvasGroupParent(objectName, hudRoot));
            }
        }

        private void EnsurePopupSubgroupRoots(Transform popupRoot)
        {
            if (popupRoot == null)
            {
                return;
            }

            Transform popupFrame = EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1);
            EnsureCanvasGroupRoot(popupRoot, PopupShellGroupName, 0);
            EnsureCanvasGroupRoot(popupRoot, PopupFrameHeaderGroupName, 2);
            EnsurePopupLayoutContainer(popupFrame, PopupFrameLeftGroupName, 2);
            EnsurePopupLayoutContainer(popupFrame, PopupFrameRightGroupName, 3);
        }

        private void ReparentPopupCanvasObjects(Transform popupRoot)
        {
            if (popupRoot == null)
            {
                return;
            }

            foreach (string objectName in PopupCanvasObjectNames)
            {
                ReparentCanvasObject(objectName, GetPopupCanvasGroupParent(objectName, popupRoot));
            }
        }

        private Transform GetPopupCanvasGroupParent(string objectName, Transform popupRoot)
        {
            if (popupRoot == null)
            {
                return null;
            }

            if (TryGetSavedCanvasGroupParent(objectName, out Transform savedParent))
            {
                return savedParent;
            }

            string subgroupName = GetPopupSubgroupName(objectName);
            if (string.IsNullOrWhiteSpace(subgroupName))
            {
                return popupRoot;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                Transform existingGroup = FindNamedUiTransform(subgroupName);
                return existingGroup != null ? existingGroup : popupRoot;
            }

            return subgroupName switch
            {
                PopupShellGroupName => EnsureCanvasGroupRoot(popupRoot, PopupShellGroupName, 0),
                PopupFrameGroupName => EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1),
                PopupFrameLeftGroupName or PopupFrameRightGroupName => EnsurePopupLayoutContainer(
                    EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1),
                    subgroupName,
                    GetPopupSubgroupSiblingIndex(subgroupName)),
                _ => popupRoot
            };
        }

        private Transform GetHudCanvasGroupParent(string objectName, Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return null;
            }

            if (TryGetSavedCanvasGroupParent(objectName, out Transform savedParent))
            {
                return savedParent;
            }

            string subgroupName = GetHudSubgroupName(objectName);
            if (string.IsNullOrWhiteSpace(subgroupName))
            {
                return hudRoot;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                Transform existingGroup = hudRoot.Find(subgroupName);
                return existingGroup != null ? existingGroup : hudRoot;
            }

            Transform currentGroup = hudRoot.Find(subgroupName);
            return currentGroup != null ? currentGroup : EnsureCanvasGroupRoot(hudRoot, subgroupName, GetHudSubgroupSiblingIndex(subgroupName));
        }

        private static string GetHudSubgroupName(string objectName)
        {
            return objectName switch
            {
                "TopLeftPanel" or "GoldText" => HudStatusGroupName,
                "ActionDock" or "ActionAccent" or "ActionCaption" => HudActionGroupName,
                "RecipePanelButton" or "UpgradePanelButton" or "MaterialPanelButton" => HudPanelButtonGroupObjectName,
                "GuideBackdrop" or "GuideText" or "ResultBackdrop" or "RestaurantResultText" or "GuideHelpButton" => HudOverlayGroupName,
                _ => null
            };
        }

        private static string GetPopupSubgroupName(string objectName)
        {
            return objectName switch
            {
                "PopupOverlay" => PopupShellGroupName,
                "PopupFrameLeft" or "PopupFrameRight" => PopupFrameGroupName,
                PrototypeUIObjectNames.PopupTitle or PrototypeUIObjectNames.PopupLeftCaption or "PopupLeftBody" or "InventoryText" => PopupFrameLeftGroupName,
                "PopupCloseButton" or PrototypeUIObjectNames.PopupRightCaption or "PopupRightBody"
                    or "StorageText" or "SelectedRecipeText" or "UpgradeText" => PopupFrameRightGroupName,
                _ => null
            };
        }

        private static int GetHudSubgroupSiblingIndex(string subgroupName)
        {
            if (subgroupName == HudStatusGroupName)
            {
                return 0;
            }

            if (subgroupName == HudActionGroupName)
            {
                return 1;
            }

            if (subgroupName == HudBottomGroupName)
            {
                return 2;
            }

            if (subgroupName == HudPanelButtonGroupObjectName)
            {
                return 3;
            }

            if (subgroupName == HudOverlayGroupName)
            {
                return 4;
            }

            return 0;
        }

        private static int GetPopupSubgroupSiblingIndex(string subgroupName)
        {
            return subgroupName switch
            {
                PopupShellGroupName => 0,
                PopupFrameGroupName => 1,
                PopupFrameHeaderGroupName => 2,
                PopupFrameLeftGroupName => 2,
                PopupFrameRightGroupName => 3,
                _ => 0
            };
        }

        private Transform EnsurePopupLayoutContainer(Transform parent, string objectName, int siblingIndex)
        {
            if (parent == null
                || string.IsNullOrWhiteSpace(objectName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            Transform existing = parent.Find(objectName);
            if (existing == null)
            {
                existing = FindNamedUiTransform(objectName);
            }

            GameObject containerObject = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform));
            ApplyHubPopupObjectIdentity(containerObject);
            if (containerObject.transform.parent != parent)
            {
                containerObject.transform.SetParent(parent, false);
            }

            RectTransform rect = containerObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = containerObject.AddComponent<RectTransform>();
            }

            if (existing == null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            rect.SetSiblingIndex(ClampSiblingIndex(rect.parent != null ? rect.parent : parent, siblingIndex));
            return rect;
        }

        private void ReparentCanvasObject(string objectName, Transform targetParent)
        {
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                DestroyNamedUiTransform(objectName);
                return;
            }

            if (targetParent == null)
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            if (target == null || target == transform || target == targetParent)
            {
                return;
            }

            if (target.parent != targetParent)
            {
                target.SetParent(targetParent, false);
            }
        }

        private Transform GetCanvasGroupParent(string objectName)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (TryGetSavedCanvasGroupParent(objectName, out Transform savedParent))
            {
                return savedParent;
            }

            bool usePopupRoot = objectName == "InventoryText" ? IsHubScene() : PopupCanvasObjectNames.Contains(objectName);

            if (usePopupRoot)
            {
                if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
                {
                    Transform existingGroup = transform.Find(PopupRootName);
                    return GetPopupCanvasGroupParent(objectName, existingGroup != null ? existingGroup : transform);
                }

                return GetPopupCanvasGroupParent(objectName, EnsureCanvasGroupRoot(PopupRootName, 1));
            }

            Transform hudRoot = !Application.isPlaying && suppressCanvasGroupingInEditorPreview
                ? transform.Find(HudRootName)
                : EnsureCanvasGroupRoot(HudRootName, 0);
            if (hudRoot == null)
            {
                return transform;
            }

            return GetHudCanvasGroupParent(objectName, hudRoot);
        }

        private void AssignCanvasGroupParent(Transform target, string objectName)
        {
            if (target == null)
            {
                return;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                return;
            }

            Transform groupParent = GetCanvasGroupParent(objectName);
            if (groupParent != null && target.parent != groupParent)
            {
                target.SetParent(groupParent, false);
            }
        }

        private void ApplySavedCanvasHierarchyOverrides()
        {
            if (transform == null)
            {
                return;
            }

            Dictionary<string, Transform> transformMap = new(StringComparer.Ordinal);
            CollectNamedUiTransforms(transform, transformMap);

            List<Transform> existingTransforms = new(transformMap.Values);
            for (int index = 0; index < existingTransforms.Count; index++)
            {
                Transform current = existingTransforms[index];
                if (current == null || string.IsNullOrWhiteSpace(current.name))
                {
                    continue;
                }

                EnsureSavedHierarchyTransform(current.name, transformMap, new HashSet<string>(StringComparer.Ordinal));
            }

            transformMap.Clear();
            CollectNamedUiTransforms(transform, transformMap);

            List<Transform> orderedTransforms = new(transformMap.Values);
            orderedTransforms.Sort((left, right) => CompareTransformDepth(left, right));
            for (int index = 0; index < orderedTransforms.Count; index++)
            {
                ApplySavedCanvasHierarchyOverride(orderedTransforms[index], transformMap);
            }
        }

        private void RemoveDeletedCanvasObjects()
        {
            if (transform == null)
            {
                return;
            }

            List<GameObject> targets = new();
            CollectDeletedCanvasObjects(transform, targets, includeCurrent: false);
            for (int index = 0; index < targets.Count; index++)
            {
                DestroyCanvasObject(targets[index]);
            }
        }

        private void DestroyNamedUiTransform(string objectName)
        {
            Transform target = FindNamedUiTransform(objectName);
            if (target != null)
            {
                DestroyCanvasObject(target.gameObject);
            }
        }

        private void DestroyCanvasObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void CollectDeletedCanvasObjects(Transform current, ICollection<GameObject> targets, bool includeCurrent)
        {
            if (current == null || targets == null)
            {
                return;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CollectDeletedCanvasObjects(current.GetChild(index), targets, includeCurrent: true);
            }

            if (includeCurrent && PrototypeUISceneLayoutCatalog.IsObjectRemoved(current.name))
            {
                targets.Add(current.gameObject);
            }
        }

        private bool TryGetSavedCanvasGroupParent(string objectName, out Transform targetParent)
        {
            targetParent = null;

            if (transform == null
                || string.IsNullOrWhiteSpace(objectName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName)
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string parentName, out _)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return false;
            }

            if (string.Equals(parentName, transform.name, StringComparison.Ordinal))
            {
                targetParent = transform;
                return true;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                targetParent = FindNamedUiTransform(parentName);
                return targetParent != null;
            }

            Dictionary<string, Transform> transformMap = new(StringComparer.Ordinal);
            CollectNamedUiTransforms(transform, transformMap);
            targetParent = EnsureSavedHierarchyTransform(parentName, transformMap, new HashSet<string>(StringComparer.Ordinal));
            return targetParent != null;
        }

        private Transform EnsureSavedHierarchyTransform(
            string objectName,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (transform == null)
            {
                return null;
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(objectName) || string.Equals(objectName, transform.name, StringComparison.Ordinal))
            {
                return transform;
            }

            if (transformMap.TryGetValue(objectName, out Transform existing))
            {
                return existing;
            }

            if (visiting == null || !visiting.Add(objectName))
            {
                return transform;
            }

            Transform parent = ResolveSavedHierarchyParent(objectName, transformMap, visiting);
            if (parent == null)
            {
                visiting.Remove(objectName);
                return null;
            }

            GameObject groupObject = new(objectName, typeof(RectTransform));
            ApplyHubPopupObjectIdentity(groupObject);
            groupObject.transform.SetParent(parent != null ? parent : transform, false);

            RectTransform rect = groupObject.GetComponent<RectTransform>();
            ApplySavedHierarchyLayout(rect, objectName);
            if (PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out _, out int siblingIndex))
            {
                rect.SetSiblingIndex(ClampSiblingIndex(rect.parent, siblingIndex));
            }

            transformMap[objectName] = rect;
            visiting.Remove(objectName);
            return rect;
        }

        private Transform ResolveSavedHierarchyParent(
            string objectName,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (transform == null)
            {
                return null;
            }

            if (!PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string parentName, out _)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return transform;
            }

            if (string.Equals(parentName, transform.name, StringComparison.Ordinal))
            {
                return transform;
            }

            return EnsureSavedHierarchyTransform(parentName, transformMap, visiting);
        }

        private void ApplySavedCanvasHierarchyOverride(Transform target, IDictionary<string, Transform> transformMap)
        {
            if (transform == null
                || target == null
                || target == transform
                || string.IsNullOrWhiteSpace(target.name)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(target.name)
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(target.name, out string parentName, out int siblingIndex)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return;
            }

            Transform targetParent = string.Equals(parentName, transform.name, StringComparison.Ordinal)
                ? transform
                : EnsureSavedHierarchyTransform(parentName, transformMap, new HashSet<string>(StringComparer.Ordinal));
            if (targetParent == null || targetParent == target)
            {
                return;
            }

            if (target.parent != targetParent)
            {
                target.SetParent(targetParent, false);
            }

            target.SetSiblingIndex(ClampSiblingIndex(target.parent, siblingIndex));
            transformMap[target.name] = target;
        }

        private static void CollectNamedUiTransforms(Transform current, IDictionary<string, Transform> transformMap)
        {
            if (current == null || transformMap == null || string.IsNullOrWhiteSpace(current.name))
            {
                return;
            }

            transformMap[current.name] = current;
            for (int index = 0; index < current.childCount; index++)
            {
                CollectNamedUiTransforms(current.GetChild(index), transformMap);
            }
        }

        private static void ApplySavedHierarchyLayout(RectTransform rect, string objectName)
        {
            if (rect == null)
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
        }

        private static int CompareTransformDepth(Transform left, Transform right)
        {
            return GetTransformDepth(left).CompareTo(GetTransformDepth(right));
        }

        private static int GetTransformDepth(Transform target)
        {
            int depth = 0;
            Transform current = target;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static int ClampSiblingIndex(Transform parent, int siblingIndex)
        {
            if (parent == null)
            {
                return 0;
            }

            return Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, parent.childCount - 1));
        }

        private Transform FindNamedUiTransform(string objectName)
        {
            if (transform == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            Transform direct = transform.Find(objectName);
            if (direct != null)
            {
                return direct;
            }

            return FindNamedUiTransformRecursive(transform, objectName);
        }

        private static Transform FindNamedUiTransformRecursive(Transform parent, string objectName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == objectName)
                {
                    return child;
                }

                Transform nested = FindNamedUiTransformRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// 현재 UI 구조에 맞춰 패널, 강조선, 텍스트, 버튼 스타일을 한 번에 다시 적용합니다.
        /// </summary>
        private void ApplyTextPresentation()
        {
            bool isHubScene = IsHubScene();
            TMP_FontAsset runtimeDefaultFont = TmpFontAssetResolver.EnsureDefaultFontAsset();
            TMP_FontAsset preferredFont = TmpFontAssetResolver.ResolveFontOrDefault(
                bodyFontAsset != null
                    ? bodyFontAsset
                    : interactionPromptText != null
                        ? interactionPromptText.font
                        : runtimeDefaultFont);
            TMP_FontAsset headingFont = TmpFontAssetResolver.ResolveHeadingFontOrDefault(headingFontAsset, preferredFont);

            ApplyCanvasScaleSettings();
            EnsureCanvasGroups();
            ResolveOptionalUiReferences();
            guideText = EnsureOverlayText(guideText, "GuideText");
            resultText = EnsureOverlayText(resultText, "RestaurantResultText");
            PrototypeUITheme theme = PrototypeUIThemePalette.GetForScene(SceneManager.GetActiveScene().name);

            EnsureCommonHudChrome(isHubScene, preferredFont, theme.Parchment, theme.Paper, theme.Glass);
            if (isHubScene)
            {
                EnsureHubHudChrome(preferredFont, theme.Paper, theme.Dock, theme.AmberAccent, theme.ActionText);
            }

            // 실제 HUD 배치와 텍스트 스타일은 아래 공용 레이아웃 메서드에서 한 번만 적용합니다.
            ApplyCompactHudLayout(preferredFont, headingFont, theme.Text, theme.OceanAccent, theme.AmberAccent, theme.CoralAccent, theme.GoldAccent);
            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
            ApplyWorldTextPresentation(preferredFont, headingFont);
        }

        /// <summary>
        /// 허브와 탐험 씬이 공통으로 쓰는 HUD 바탕과 캡션을 생성합니다.
        /// </summary>
        private void EnsureCommonHudChrome(
            bool isHubScene,
            TMP_FontAsset preferredFont,
            Color parchment,
            Color paper,
            Color glass)
        {
            EnsureUiBackdrop("TopLeftPanel", PrototypeUILayout.TopLeftPanel, parchment);
            EnsureUiBackdrop("InteractionPromptBackdrop", PrototypeUILayout.PromptBackdrop(isHubScene), Color.white);
            EnsureUiBackdrop("GuideBackdrop", PrototypeUILayout.GuideBackdrop(isHubScene), paper);
            EnsureUiBackdrop("ResultBackdrop", PrototypeUILayout.ResultBackdrop(isHubScene), paper);
            EnsureGuideHelpButton(preferredFont, isHubScene);
        }

        /// <summary>
        /// 허브에서만 필요한 팝업 카드와 진행 버튼 독을 생성합니다.
        /// </summary>
        private void EnsureHubHudChrome(
            TMP_FontAsset preferredFont,
            Color paper,
            Color nightDock,
            Color amberAccent,
            Color actionTextColor)
        {
            PrototypeUIPopupDefinition recipePopupDefinition = PrototypeUIPopupCatalog.GetDefinition(PrototypeUIPreviewPanel.Recipe);
            Color popupShell = new(1f, 1f, 1f, 0f);
            Color popupFrameLeft = Color.white;
            Color popupFrameRight = new(0.92f, 0.95f, 0.99f, 1f);
            Color popupBody = Color.white;

            EnsureUiBackdrop(HudPanelButtonGroupObjectName, PrototypeUILayout.HubPanelButtonGroup, paper);
            EnsureUiBackdrop("PopupOverlay", PrototypeUILayout.HubPopupOverlay, new Color(0f, 0f, 0f, 0.52f));
            EnsureUiBackdrop("ActionDock", PrototypeUILayout.HubActionDock, nightDock);
            EnsureUiBackdrop("PopupFrame", PrototypeUILayout.HubPopupFrame, popupShell);
            EnsureUiBackdrop("PopupFrameLeft", PrototypeUILayout.HubPopupFrameLeft, popupFrameLeft);
            EnsureUiBackdrop("PopupFrameRight", PrototypeUILayout.HubPopupFrameRight, popupFrameRight);
            EnsureUiBackdrop("PopupLeftBody", PrototypeUILayout.HubPopupFrameBody, popupBody);
            EnsureUiBackdrop("PopupRightBody", PrototypeUILayout.HubPopupFrameBody, popupBody);

            EnsureUiAccentBar("ActionAccent", PrototypeUILayout.HubActionAccent, amberAccent);
            EnsureUiCaption("ActionCaption", "진행", PrototypeUILayout.HubActionCaption, preferredFont, actionTextColor, TextAlignmentOptions.TopRight);
            EnsureHubPopupHeadings(recipePopupDefinition, preferredFont, actionTextColor);
            EnsurePopupCloseButton(preferredFont);
        }

        /// <summary>
        /// 이름으로 찾은 패널 오브젝트에 카드형 배경과 그림자를 적용합니다.
        /// </summary>
        private void EnsureUiBackdrop(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return;
            }

            GameObject backdropObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(backdropObject);
            if (existing == null)
            {
                backdropObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(backdropObject.transform, objectName);
            }

            RectTransform rect = backdropObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = backdropObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.SetSiblingIndex(0);

            Image image = backdropObject.GetComponent<Image>();
            if (image == null)
            {
                image = backdropObject.AddComponent<Image>();
            }

            bool shouldUseGeneratedUiDesign = PrototypeUISkinCatalog.UsesGeneratedUiDesignPanel(objectName);
            bool preserveSceneSprite = existing != null
                                       && IsHubPopupDisplayObject(objectName)
                                       && image.sprite != null
                                       && !shouldUseGeneratedUiDesign;
            if (!preserveSceneSprite)
            {
                PrototypeUISkin.ApplyPanel(image, objectName, color);
            }

            // generated UI 스킨은 기본값으로 적용하고, 씬에서 저장한 이미지 오버라이드가 있으면 마지막에 우선 반영한다.
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);

            image.raycastTarget = false;

            Shadow shadow = backdropObject.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = backdropObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;
        }

        private void EnsureUiBackdrop(string objectName, PrototypeUIRect layout, Color color)
        {
            EnsureUiBackdrop(
                objectName,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                color);
        }

        /// <summary>
        /// 카드 상단 강조선을 추가해 HUD와 팝업 구획을 더 또렷하게 나눕니다.
        /// </summary>
        private void EnsureUiAccentBar(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return;
            }

            GameObject accentObject = existing != null ? existing.gameObject : new GameObject(objectName);
            if (existing == null)
            {
                accentObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(accentObject.transform, objectName);
            }

            RectTransform rect = accentObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = accentObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.SetSiblingIndex(1);

            Image image = accentObject.GetComponent<Image>();
            if (image == null)
            {
                image = accentObject.AddComponent<Image>();
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
            image.raycastTarget = false;
        }

        private void EnsureUiAccentBar(string objectName, PrototypeUIRect layout, Color color)
        {
            EnsureUiAccentBar(
                objectName,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                color);
        }

        /// <summary>
        /// 카드 제목 캡션을 생성하거나 갱신해 각 정보 구역 이름을 표시합니다.
        /// </summary>
        private void EnsureUiCaption(
            string objectName,
            string content,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            TMP_FontAsset font,
            Color color,
            TextAlignmentOptions alignment)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return;
            }

            GameObject captionObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(captionObject);
            TextMeshProUGUI existingText = captionObject.GetComponent<TextMeshProUGUI>();
            bool preserveExistingPopupHeading = existing != null && existingText != null && IsFixedPopupHeading(objectName);
            if (existing == null)
            {
                captionObject.transform.SetParent(targetParent, false);
            }
            else if (!preserveExistingPopupHeading)
            {
                AssignCanvasGroupParent(captionObject.transform, objectName);
            }

            RectTransform rect = captionObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = captionObject.AddComponent<RectTransform>();
            }

            if (!preserveExistingPopupHeading)
            {
                rect.anchorMin = resolvedLayout.AnchorMin;
                rect.anchorMax = resolvedLayout.AnchorMax;
                rect.pivot = resolvedLayout.Pivot;
                rect.anchoredPosition = resolvedLayout.AnchoredPosition;
                rect.sizeDelta = resolvedLayout.SizeDelta;
                rect.SetSiblingIndex(2);
            }

            TextMeshProUGUI text = existingText;
            if (text == null)
            {
                text = captionObject.AddComponent<TextMeshProUGUI>();
            }

            CaptionPresentationPreset presentation = ResolveCaptionPresentation(objectName);
            TMP_FontAsset resolvedFont = ResolveCaptionFont(objectName, font);
            text.text = content;
            if (!preserveExistingPopupHeading)
            {
                ApplyScreenTextStyle(text, resolvedFont, presentation.FontSize, color, alignment, false, 0f, presentation.Margin, presentation.EnableAutoSizing);
                text.enableAutoSizing = presentation.EnableAutoSizing;
                text.fontSizeMin = presentation.FontSizeMin;
                text.fontSizeMax = presentation.FontSizeMax;
                text.fontSize = presentation.FontSize;
                text.characterSpacing = presentation.CharacterSpacing;
            }
            else if (resolvedFont != null && text.font != resolvedFont)
            {
                text.font = resolvedFont;
                if (resolvedFont.material != null)
                {
                    text.fontSharedMaterial = resolvedFont.material;
                }
            }

            text.raycastTarget = false;
            ApplySceneTextOverride(text);
        }

        private void EnsureUiCaption(
            string objectName,
            string content,
            PrototypeUIRect layout,
            TMP_FontAsset font,
            Color color,
            TextAlignmentOptions alignment)
        {
            EnsureUiCaption(
                objectName,
                content,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                font,
                color,
                alignment);
        }

        private TextMeshProUGUI EnsureOverlayText(TextMeshProUGUI current, string objectName)
        {
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (current != null)
            {
                AssignCanvasGroupParent(current.transform, objectName);
                return current;
            }

            Transform existing = FindNamedUiTransform(objectName);
            Transform targetParent = existing == null ? GetCanvasGroupParent(objectName) : null;
            if (existing == null && targetParent == null)
            {
                return null;
            }

            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(textObject);
            if (existing == null)
            {
                textObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(textObject.transform, objectName);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(920f, 60f);
            rect.SetAsLastSibling();

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 허브 팝업 우측 상단에 닫기 버튼을 생성하거나 다시 스타일링합니다.
        /// </summary>
        private void EnsurePopupCloseButton(TMP_FontAsset font)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved("PopupCloseButton"))
            {
                return;
            }

            Transform existing = FindNamedUiTransform("PopupCloseButton");
            Transform targetParent = existing == null ? GetCanvasGroupParent("PopupCloseButton") : null;
            if (existing == null && targetParent == null)
            {
                popupCloseButton = null;
                return;
            }

            GameObject buttonObject = existing != null ? existing.gameObject : new GameObject("PopupCloseButton");
            ApplyHubPopupObjectIdentity(buttonObject);
            if (existing == null)
            {
                buttonObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(buttonObject.transform, "PopupCloseButton");
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            ApplyRectLayout(rect, PrototypeUILayout.HubPopupCloseButton);
            rect.SetAsLastSibling();

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.targetGraphic = image;
            popupCloseButton = button;
            ApplyButtonPresentation(button, font, Color.white);

            TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                GameObject labelObject = new("PopupCloseButton_Label");
                ApplyHubPopupObjectIdentity(labelObject);
                labelObject.transform.SetParent(buttonObject.transform, false);

                RectTransform labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = Vector2.zero;

                label = labelObject.AddComponent<TextMeshProUGUI>();
            }

            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }

        private void EnsureGuideHelpButton(TMP_FontAsset font, bool isHubScene)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved("GuideHelpButton"))
            {
                return;
            }

            Transform existing = FindNamedUiTransform("GuideHelpButton");
            Transform targetParent = existing == null ? GetCanvasGroupParent("GuideHelpButton") : null;
            if (existing == null && targetParent == null)
            {
                guideHelpButton = null;
                return;
            }

            GameObject buttonObject = existing != null ? existing.gameObject : new GameObject("GuideHelpButton");
            if (existing == null)
            {
                buttonObject.transform.SetParent(targetParent, false);
            }
            else
            {
                AssignCanvasGroupParent(buttonObject.transform, "GuideHelpButton");
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            ApplyRectLayout(rect, PrototypeUILayout.GuideHelpButton(isHubScene));
            rect.SetAsLastSibling();

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.targetGraphic = image;
            guideHelpButton = button;
            ApplyButtonPresentation(button, font, Color.white);

            TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = string.Empty;
                label.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 텍스트나 버튼의 위치와 크기를 지정된 값으로 다시 맞춥니다.
        /// </summary>
        private static void ApplyRectLayout(RectTransform rect, PrototypeUIRect layout)
        {
            ApplyRectLayout(
                rect,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta);
        }

        private static void ApplyRectLayout(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// UI 텍스트의 공통 폰트, 줄바꿈, 여백, 굵기 규칙을 한곳에서 통일합니다.
        /// </summary>
        private static void ApplyScreenTextStyle(
            TextMeshProUGUI text,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            bool allowWrap,
            float lineSpacing,
            Vector4 margin,
            bool bold)
        {
            if (text == null)
            {
                return;
            }

            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.margin = margin;
            text.lineSpacing = lineSpacing;
            text.isRightToLeftText = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = allowWrap ? Mathf.Max(12f, fontSize - 7f) : Mathf.Max(13f, fontSize - 6f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = allowWrap ? TextOverflowModes.Masking : TextOverflowModes.Truncate;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.characterSpacing = bold ? 0.35f : 0f;
            text.wordSpacing = 0f;

            if (allowWrap)
            {
                text.paragraphSpacing = 2f;
            }
            else
            {
                text.paragraphSpacing = 0f;
            }
        }

        /// <summary>
        /// 씬에서 저장한 TMP 표시 값이 있으면 기본 HUD 스타일 적용 뒤 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneTextOverride(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            PrototypeUISceneLayoutCatalog.TryApplyTextOverride(text, text.name);
        }

        /// <summary>
        /// 씬에서 저장한 버튼과 라벨 표시 값이 있으면 기본 스타일 적용 뒤 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneButtonOverride(Button button)
        {
            if (button == null)
            {
                return;
            }

            PrototypeUISceneLayoutCatalog.TryApplyButtonOverride(button, button.name);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                PrototypeUISceneLayoutCatalog.TryApplyTextOverride(label, label.name);
            }
        }

        /// <summary>
        /// 버튼 RectTransform을 조정해 허브 하단과 우측 액션 영역 배치를 맞춥니다.
        /// </summary>
        private static void ApplyButtonLayout(Button button, PrototypeUIRect layout)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(button.name, layout);
            ApplyRectLayout(rect, resolvedLayout);
        }

        /// <summary>
        /// 버튼 배경색과 라벨 스타일을 현재 HUD 톤에 맞게 다시 입힙니다.
        /// </summary>
        private void ApplyButtonPresentation(Button button, TMP_FontAsset font, Color accentColor)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            bool hasKenneySkin = false;
            if (image != null)
            {
                hasKenneySkin = PrototypeUISkin.ApplyButton(image, button.name, accentColor);
                if (PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, button.name))
                {
                    hasKenneySkin = true;
                }
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            ApplyScreenTextStyle(label, font, 20f, Color.white, TextAlignmentOptions.Center, false, 0f, new Vector4(8f, 6f, 8f, 6f), true);

            Shadow shadow = button.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = button.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            ColorBlock colors = button.colors;
            if (hasKenneySkin)
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
                colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
                colors.selectedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
                colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            }
            else
            {
                colors.normalColor = accentColor;
                colors.highlightedColor = Color.Lerp(accentColor, Color.white, 0.14f);
                colors.pressedColor = Color.Lerp(accentColor, Color.black, 0.18f);
                colors.selectedColor = Color.Lerp(accentColor, Color.white, 0.10f);
                colors.disabledColor = new Color(accentColor.r * 0.55f, accentColor.g * 0.55f, accentColor.b * 0.55f, 0.45f);
            }

            colors.fadeDuration = 0.08f;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;
            ApplySceneButtonOverride(button);
        }

        /// <summary>
        /// 월드 라벨은 한글 폰트, 굵기, 외곽선 기준을 통일해 장면마다 읽기 쉽게 만듭니다.
        /// </summary>
        private static void ApplyWorldTextPresentation(TMP_FontAsset bodyFont, TMP_FontAsset headingFont)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            TextMeshPro[] worldTexts = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
            foreach (TextMeshPro worldText in worldTexts)
            {
                if (worldText == null)
                {
                    continue;
                }

                bool isLargeLabel = worldText.fontSize >= 3.4f;
                TMP_FontAsset font = isLargeLabel ? headingFont : bodyFont;
                if (font != null)
                {
                    worldText.font = font;
                }

                worldText.enableAutoSizing = false;
                bool isPrimaryLabel = worldText.fontSize >= 2.5f;
                worldText.characterSpacing = isLargeLabel ? 0.22f : isPrimaryLabel ? 0.08f : 0.02f;
                worldText.wordSpacing = 0f;
                worldText.lineSpacing = 0f;
                worldText.fontStyle = isLargeLabel || isPrimaryLabel ? FontStyles.Bold : FontStyles.Normal;
                float labelScale = isLargeLabel ? 0.30f : isPrimaryLabel ? 0.27f : 0.25f;
                worldText.transform.localScale = Vector3.one * labelScale;
                ApplyCompactWorldLabelOffset(worldText);

                float luminance = (worldText.color.r * 0.299f) + (worldText.color.g * 0.587f) + (worldText.color.b * 0.114f);
                worldText.outlineWidth = isLargeLabel ? 0.10f : isPrimaryLabel ? 0.08f : 0.06f;
                worldText.outlineColor = luminance < 0.45f
                    ? new Color(1f, 1f, 1f, 0.90f)
                    : new Color(0f, 0f, 0f, 0.88f);
            }
        }

        private static void ApplyCompactWorldLabelOffset(TextMeshPro worldText)
        {
            if (worldText == null || worldText.transform.parent == null)
            {
                return;
            }

            Transform parent = worldText.transform.parent;
            float? compactY = null;

            if (parent.GetComponent<ScenePortal>() != null)
            {
                compactY = 0.82f;
            }
            else if (parent.GetComponent<RecipeSelectorStation>() != null || parent.GetComponent<ServiceCounterStation>() != null)
            {
                compactY = 0.80f;
            }
            else if (parent.GetComponent<StorageStation>() != null)
            {
                compactY = 0.72f;
            }
            else if (parent.GetComponent<UpgradeStation>() != null)
            {
                compactY = 0.68f;
            }
            else if (parent.GetComponent<GatherableResource>() != null)
            {
                compactY = 0.64f;
            }
            else if (parent.GetComponent<PlayerController>() != null)
            {
                compactY = 0.46f;
            }

            if (!compactY.HasValue)
            {
                return;
            }

            Vector3 localPosition = worldText.transform.localPosition;
            worldText.transform.localPosition = new Vector3(localPosition.x, compactY.Value, localPosition.z);
        }

        private void ApplyCompactHudLayout(
            TMP_FontAsset bodyFont,
            TMP_FontAsset headingFont,
            Color textColor,
            Color oceanAccent,
            Color amberAccent,
            Color coralAccent,
            Color goldAccent)
        {
            bool isHubScene = IsHubScene();

            ApplyNamedRectLayout("TopLeftPanel", PrototypeUILayout.TopLeftPanel);
            ApplyNamedRectLayout("InteractionPromptBackdrop", PrototypeUILayout.PromptBackdrop(isHubScene));
            ApplyNamedRectLayout("GuideBackdrop", PrototypeUILayout.GuideBackdrop(isHubScene));
            ApplyNamedRectLayout("ResultBackdrop", PrototypeUILayout.ResultBackdrop(isHubScene));
            ApplyButtonLayout(guideHelpButton, PrototypeUILayout.GuideHelpButton(isHubScene));
            ApplyButtonPresentation(guideHelpButton, headingFont, Color.white);
            if (isHubScene)
            {
                ApplyNamedRectLayout("ActionDock", PrototypeUILayout.HubActionDock);
                ApplyNamedRectLayout("ActionAccent", PrototypeUILayout.HubActionAccent);
                ApplyNamedRectLayout(HudPanelButtonGroupObjectName, PrototypeUILayout.HubPanelButtonGroup);
            }

            SetNamedObjectActive(HudPanelButtonGroupObjectName, isHubScene);
            SetNamedObjectActive("PopupOverlay", false);
            SetNamedObjectActive("ActionDock", false);
            SetNamedObjectActive("ActionAccent", false);
            SetNamedObjectActive("ActionCaption", false);

            ApplyRectLayout(goldText != null ? goldText.rectTransform : null, PrototypeUILayout.GoldText);
            ApplyRectLayout(interactionPromptText != null ? interactionPromptText.rectTransform : null, PrototypeUILayout.PromptText(isHubScene));
            ApplyRectLayout(guideText != null ? guideText.rectTransform : null, PrototypeUILayout.GuideText(isHubScene));
            ApplyRectLayout(resultText != null ? resultText.rectTransform : null, PrototypeUILayout.ResultText(isHubScene));
            ApplyScreenTextStyle(goldText, headingFont, 20f, textColor, TextAlignmentOptions.TopLeft, false, 0f, new Vector4(6f, 2f, 6f, 2f), true);
            ApplyScreenTextStyle(interactionPromptText, headingFont, 21f, textColor, TextAlignmentOptions.Center, false, 0f, new Vector4(12f, 8f, 12f, 8f), true);
            ApplyScreenTextStyle(guideText, bodyFont, 18f, textColor, TextAlignmentOptions.Center, !isHubScene, 4f, new Vector4(14f, 8f, 14f, 10f), false);
            ApplyScreenTextStyle(resultText, bodyFont, 18f, textColor, TextAlignmentOptions.Center, true, 4f, new Vector4(14f, 10f, 14f, 10f), false);
            ApplySceneTextOverride(goldText);
            ApplySceneTextOverride(interactionPromptText);
            ApplySceneTextOverride(guideText);
            ApplySceneTextOverride(resultText);

            HideLegacyDayRoutineObjects();

            if (isHubScene)
            {
                ApplyHubPanelLayout(bodyFont, headingFont, textColor, oceanAccent, amberAccent, goldAccent);
            }
            else
            {
                ApplyExplorationInventoryLayout();
            }

            SetButtonGameObjectActive(recipePanelButton, isHubScene);
            SetButtonGameObjectActive(upgradePanelButton, isHubScene);
            SetButtonGameObjectActive(materialPanelButton, isHubScene);
        }

        /// <summary>
        /// 허브는 재료/메뉴/업그레이드/창고 팝업을 같은 카드 틀 안에서 공유합니다.
        /// </summary>
        private void ApplyHubPanelLayout(
            TMP_FontAsset bodyFont,
            TMP_FontAsset headingFont,
            Color textColor,
            Color oceanAccent,
            Color amberAccent,
            Color goldAccent)
        {
            ApplyNamedRectLayout("PopupFrame", PrototypeUILayout.HubPopupFrame);
            ApplyNamedRectLayout("PopupFrameLeft", PrototypeUILayout.HubPopupFrameLeft);
            ApplyNamedRectLayout("PopupFrameRight", PrototypeUILayout.HubPopupFrameRight);
            ApplyNamedRectLayout("PopupLeftBody", PrototypeUILayout.HubPopupFrameBody);
            ApplyNamedRectLayout("PopupRightBody", PrototypeUILayout.HubPopupFrameBody);
            ApplyHubPopupFrameStyle(headingFont, textColor);
            ApplyPopupCloseButtonLayout(headingFont);
            NormalizeHubPopupHierarchyOrder();
            EnsurePopupBodyItemBoxes(bodyFont, textColor);

            ApplyRectLayout(inventoryText != null ? inventoryText.rectTransform : null, PrototypeUILayout.HubPopupFrameText);
            ApplyRectLayout(storageText != null ? storageText.rectTransform : null, PrototypeUILayout.HubPopupRightDetailText);
            ApplyRectLayout(selectedRecipeText != null ? selectedRecipeText.rectTransform : null, PrototypeUILayout.HubPopupRightDetailText);
            ApplyRectLayout(upgradeText != null ? upgradeText.rectTransform : null, PrototypeUILayout.HubPopupRightDetailText);

            ApplyPopupInventoryTextStyle(inventoryText, bodyFont, textColor);
            ApplyPopupDetailTextStyle(storageText, bodyFont, textColor);
            ApplyPopupDetailTextStyle(selectedRecipeText, bodyFont, textColor);
            ApplyPopupDetailTextStyle(upgradeText, bodyFont, textColor);

            ApplyHubMenuButtonLayout(recipePanelButton, headingFont, amberAccent, PrototypeUILayout.HubRecipePanelButton);
            ApplyHubMenuButtonLayout(upgradePanelButton, headingFont, goldAccent, PrototypeUILayout.HubUpgradePanelButton);
            ApplyHubMenuButtonLayout(materialPanelButton, headingFont, oceanAccent, PrototypeUILayout.HubMaterialPanelButton);
            SetLegacyHubPopupObjectsActive(false);
        }

        /// <summary>
        /// 허브 팝업 제목과 좌우 섹션 캡션을 현재 메뉴에 맞춰 갱신합니다.
        /// </summary>
        private void ApplyHubPopupFrameStyle(TMP_FontAsset headingFont, Color textColor)
        {
            PrototypeUIPopupDefinition popupDefinition = PrototypeUIPopupCatalog.GetDefinition(ConvertRuntimePopupPanel(activeHubPanel));
            EnsureHubPopupHeadings(popupDefinition, headingFont, textColor);
        }

        /// <summary>
        /// 허브 팝업 헤더는 현재 패널 정의를 한 곳에서 묶어 적용해 빌더와 런타임 기준이 갈라지지 않게 유지합니다.
        /// </summary>
        private void EnsureHubPopupHeadings(PrototypeUIPopupDefinition popupDefinition, TMP_FontAsset font, Color color)
        {
            EnsureUiCaption(PrototypeUIObjectNames.PopupTitle, popupDefinition.Title, PrototypeUILayout.HubPopupTitle, font, color, TextAlignmentOptions.TopLeft);
            EnsureUiCaption(PrototypeUIObjectNames.PopupLeftCaption, popupDefinition.LeftCaption, PrototypeUILayout.HubPopupLeftCaption, font, color, TextAlignmentOptions.TopLeft);
            EnsureUiCaption(PrototypeUIObjectNames.PopupRightCaption, popupDefinition.RightCaption, PrototypeUILayout.HubPopupFrameCaption, font, color, TextAlignmentOptions.TopLeft);
        }

        private static void ApplyPopupDetailTextStyle(TextMeshProUGUI text, TMP_FontAsset bodyFont, Color textColor)
        {
            ApplyScreenTextStyle(text, bodyFont, 18f, textColor, TextAlignmentOptions.TopLeft, true, 0f, new Vector4(10f, 8f, 10f, 8f), false);
            if (text != null)
            {
                text.paragraphSpacing = 0f;
                ApplySceneTextOverride(text);
            }
        }

        private static void ApplyPopupInventoryTextStyle(TextMeshProUGUI text, TMP_FontAsset bodyFont, Color textColor)
        {
            ApplyScreenTextStyle(text, bodyFont, 19f, textColor, TextAlignmentOptions.TopLeft, true, 0f, new Vector4(10f, 8f, 10f, 8f), false);
            if (text != null)
            {
                text.fontSizeMin = 13f;
                text.fontSizeMax = 19f;
                text.paragraphSpacing = 0f;
                text.overflowMode = TextOverflowModes.Masking;
                ApplySceneTextOverride(text);
            }
        }

        private static CaptionPresentationPreset ResolveCaptionPresentation(string objectName)
        {
            return objectName switch
            {
                PrototypeUIObjectNames.PopupTitle => FixedPopupTitlePresentation,
                PrototypeUIObjectNames.PopupLeftCaption or PrototypeUIObjectNames.PopupRightCaption => FixedPopupCaptionPresentation,
                _ => DefaultCaptionPresentation
            };
        }

        private static void ApplyHubPopupObjectIdentity(GameObject target)
        {
            if (target == null || !IsHubPopupDisplayObject(target.name))
            {
                return;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            target.layer = uiLayer >= 0 ? uiLayer : 5;
            target.tag = "Player";
        }

        private static bool IsHubPopupDisplayObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName is PopupRootName or PopupShellGroupName or PopupFrameHeaderGroupName or "PopupOverlay")
            {
                return false;
            }

            return objectName.StartsWith("Popup", StringComparison.Ordinal)
                   || objectName is "InventoryText" or "StorageText" or "SelectedRecipeText" or "UpgradeText";
        }

        private TMP_FontAsset ResolveCaptionFont(string objectName, TMP_FontAsset font)
        {
            return objectName switch
            {
                PrototypeUIObjectNames.PopupTitle or PrototypeUIObjectNames.PopupLeftCaption or PrototypeUIObjectNames.PopupRightCaption
                    => headingFontAsset != null ? headingFontAsset : font,
                _ => font
            };
        }

        private static bool IsFixedPopupHeading(string objectName)
        {
            return objectName == PrototypeUIObjectNames.PopupTitle || objectName == PrototypeUIObjectNames.PopupLeftCaption;
        }

        private void ApplyPopupCloseButtonLayout(TMP_FontAsset headingFont)
        {
            if (popupCloseButton == null)
            {
                return;
            }

            ApplyButtonLayout(popupCloseButton, PrototypeUILayout.HubPopupCloseButton);
            ApplyButtonPresentation(popupCloseButton, headingFont, Color.white);
        }

        private void NormalizeHubPopupHierarchyOrder()
        {
            if (transform == null)
            {
                return;
            }

            Transform popupRoot = transform.Find(PopupRootName);
            if (popupRoot == null)
            {
                return;
            }

            Transform popupShellGroup = popupRoot.Find(PopupShellGroupName);
            Transform popupFrame = popupRoot.Find(PopupFrameGroupName);
            Transform popupFrameHeader = popupRoot.Find(PopupFrameHeaderGroupName);

            if (popupShellGroup != null)
            {
                popupShellGroup.SetSiblingIndex(0);
            }

            if (popupFrame != null)
            {
                popupFrame.SetSiblingIndex(Mathf.Clamp(1, 0, Mathf.Max(0, popupRoot.childCount - 1)));

                Transform popupFrameLeft = popupFrame.Find(PopupFrameLeftGroupName);
                Transform popupFrameRight = popupFrame.Find(PopupFrameRightGroupName);
                if (popupFrameLeft != null)
                {
                    popupFrameLeft.SetSiblingIndex(0);
                }

                if (popupFrameRight != null)
                {
                    popupFrameRight.SetSiblingIndex(1);
                }
            }

            if (popupFrameHeader != null)
            {
                popupFrameHeader.SetSiblingIndex(Mathf.Clamp(2, 0, Mathf.Max(0, popupRoot.childCount - 1)));
            }
        }

        /// <summary>
        /// 허브 팝업 왼쪽 본문은 아이콘, 이름, 짧은 설명이 들어가는 선택 목록으로 유지합니다.
        /// </summary>
        private void EnsurePopupBodyItemBoxes(TMP_FontAsset bodyFont, Color textColor)
        {
            EnsurePopupBodyItemBoxSet("PopupLeftBody", "PopupLeftItemBox", "PopupLeftItemIcon", "PopupLeftItemText", bodyFont, textColor, true);
            SetPopupBodyItemBoxSetActive("PopupRightItemBox", "PopupRightItemIcon", "PopupRightItemText", false);
        }

        private void EnsurePopupBodyItemBoxSet(
            string bodyName,
            string boxPrefix,
            string iconPrefix,
            string textPrefix,
            TMP_FontAsset bodyFont,
            Color textColor,
            bool isInteractive)
        {
            Transform bodyTransform = FindNamedUiTransform(bodyName);
            if (bodyTransform == null)
            {
                return;
            }

            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"{boxPrefix}{i + 1:00}";
                string iconName = $"{iconPrefix}{i + 1:00}";
                string textName = $"{textPrefix}{i + 1:00}";
                Image boxImage = EnsurePopupBodyItemBox(bodyTransform, boxName, PrototypeUILayout.HubPopupBodyItemBox(i), isInteractive);
                Transform contentParent = boxImage != null ? boxImage.transform : bodyTransform;
                EnsurePopupBodyItemIcon(contentParent, iconName);
                EnsurePopupBodyItemText(contentParent, textName, bodyFont, textColor);
            }
        }

        private Image EnsurePopupBodyItemBox(Transform parent, string objectName, PrototypeUIRect layout, bool isInteractive)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject boxObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(boxObject);
            if (existing == null || boxObject.transform.parent != parent)
            {
                boxObject.transform.SetParent(parent, false);
            }

            RectTransform rect = boxObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = boxObject.AddComponent<RectTransform>();
            }

            ApplyRectLayout(rect, layout);

            Image image = boxObject.GetComponent<Image>();
            if (image == null)
            {
                image = boxObject.AddComponent<Image>();
            }

            bool hasSkin = PrototypeUISkin.ApplyPanel(image, objectName, PopupItemBoxFallbackColor);
            bool hasImageOverride = PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
            if (!hasImageOverride)
            {
                image.color = hasSkin ? Color.white : PopupItemBoxFallbackColor;
            }
            image.raycastTarget = isInteractive;

            Shadow shadow = boxObject.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = boxObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;

            Button button = boxObject.GetComponent<Button>();
            if (isInteractive)
            {
                if (button == null)
                {
                    button = boxObject.AddComponent<Button>();
                }

                button.targetGraphic = image;
                button.transition = Selectable.Transition.ColorTint;
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
            }
            else if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
            }

            return image;
        }

        private void EnsurePopupBodyItemIcon(Transform parent, string objectName)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject iconObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(iconObject);
            if (existing == null || iconObject.transform.parent != parent)
            {
                iconObject.transform.SetParent(parent, false);
            }

            RectTransform rect = iconObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = iconObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(40f, 0f);
            rect.sizeDelta = new Vector2(44f, 44f);

            Image image = iconObject.GetComponent<Image>();
            if (image == null)
            {
                image = iconObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            image.enabled = true;
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private void EnsurePopupBodyItemText(Transform parent, string objectName, TMP_FontAsset bodyFont, Color textColor)
        {
            if (parent == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform existing = FindNamedUiTransform(objectName);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            ApplyHubPopupObjectIdentity(textObject);
            if (existing == null || textObject.transform.parent != parent)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = new Vector2(74f, 10f);
            rect.offsetMax = new Vector2(-14f, -10f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = false;
            ApplyScreenTextStyle(text, bodyFont, 17f, textColor, TextAlignmentOptions.TopLeft, true, 0f, Vector4.zero, false);
            text.fontSizeMin = 12f;
            text.fontSizeMax = 17f;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            ApplySceneTextOverride(text);
        }

        private void SetPopupBodyItemBoxSetActive(string boxPrefix, string iconPrefix, string textPrefix, bool isActive)
        {
            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                SetNamedObjectActive($"{boxPrefix}{i + 1:00}", isActive);
                SetNamedObjectActive($"{iconPrefix}{i + 1:00}", isActive);
                SetNamedObjectActive($"{textPrefix}{i + 1:00}", isActive);
            }
        }

        private void RefreshPopupBodyItemBoxes(List<PopupListEntry> entries)
        {
            List<PopupListEntry> safeEntries = entries ?? new List<PopupListEntry>();
            int selectedIndex = safeEntries.FindIndex(entry => entry != null && entry.IsSelected);
            int windowStart = GetPopupBodyWindowStartIndex(safeEntries.Count, selectedIndex, PrototypeUILayout.HubPopupBodyItemBoxCount);

            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"PopupLeftItemBox{i + 1:00}";
                string iconName = $"PopupLeftItemIcon{i + 1:00}";
                string textName = $"PopupLeftItemText{i + 1:00}";
                int entryIndex = windowStart + i;
                bool hasContent = entryIndex >= 0 && entryIndex < safeEntries.Count;
                PopupListEntry entry = hasContent ? safeEntries[entryIndex] : null;

                Transform boxTransform = FindNamedUiTransform(boxName);
                if (boxTransform != null)
                {
                    boxTransform.gameObject.SetActive(hasContent);

                    if (boxTransform.TryGetComponent(out Image boxImage))
                    {
                        boxImage.color = entry != null && entry.IsSelected
                            ? PopupItemBoxSelectedColor
                            : PopupItemBoxFallbackColor;
                    }

                    if (boxTransform.TryGetComponent(out Button button))
                    {
                        button.onClick.RemoveAllListeners();
                        button.interactable = entry != null && entry.OnSelected != null;

                        ColorBlock colors = button.colors;
                        colors.normalColor = entry != null && entry.IsSelected ? PopupItemBoxSelectedColor : PopupItemBoxFallbackColor;
                        colors.highlightedColor = entry != null && entry.IsSelected ? PopupItemBoxSelectedColor : new Color(1f, 0.96f, 0.82f, 1f);
                        colors.pressedColor = new Color(0.92f, 0.86f, 0.68f, 1f);
                        colors.selectedColor = PopupItemBoxSelectedColor;
                        colors.disabledColor = PopupItemBoxFallbackColor;
                        button.colors = colors;

                        if (entry != null && entry.OnSelected != null)
                        {
                            Action onSelected = entry.OnSelected;
                            button.onClick.AddListener(() => onSelected());
                        }
                    }
                }

                Transform iconTransform = FindNamedUiTransform(iconName);
                if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
                {
                    bool hasIcon = hasContent && entry.Icon != null;
                    iconTransform.gameObject.SetActive(hasIcon);
                    iconImage.sprite = hasIcon ? entry.Icon : null;
                    iconImage.color = Color.white;
                    iconImage.enabled = hasIcon;
                }

                Transform textTransform = FindNamedUiTransform(textName);
                if (textTransform != null && textTransform.TryGetComponent(out TextMeshProUGUI text))
                {
                    text.text = hasContent ? FormatPopupBodyEntryText(entry) : string.Empty;
                }
            }
        }

        private static int GetPopupBodyWindowStartIndex(int entryCount, int selectedIndex, int visibleCount)
        {
            if (entryCount <= visibleCount || visibleCount <= 0)
            {
                return 0;
            }

            int clampedSelectedIndex = Mathf.Clamp(selectedIndex, 0, entryCount - 1);
            int preferredStart = clampedSelectedIndex - (visibleCount / 2);
            return Mathf.Clamp(preferredStart, 0, entryCount - visibleCount);
        }

        private static string FormatPopupBodyEntryText(PopupListEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            string title = entry.IsSelected ? $"<b>{entry.Title}</b>" : entry.Title;
            if (string.IsNullOrWhiteSpace(entry.Summary))
            {
                return title;
            }

            return $"{title}\n<size=78%>{entry.Summary}</size>";
        }

        /// <summary>
        /// 탐험 씬은 우측 상단 재료/가방 HUD만 유지하고 허브용 카드는 숨깁니다.
        /// </summary>
        private void ApplyExplorationInventoryLayout()
        {
            SetHubPopupDesignActive(false);
            SetLegacyHubPopupObjectsActive(false);
        }

        /// <summary>
        /// 허브 하단 버튼은 같은 형태를 쓰므로 배치와 스타일을 공통 메서드로 묶습니다.
        /// </summary>
        private void ApplyHubMenuButtonLayout(Button button, TMP_FontAsset headingFont, Color accentColor, PrototypeUIRect layout)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            ApplyRectLayout(rect, layout);
            ApplyButtonPresentation(button, headingFont, accentColor);
            button.gameObject.SetActive(true);
        }

        private static void SetButtonGameObjectActive(Button button, bool isActive)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isActive);
            }
        }

        private void SetLegacyHubPopupObjectsActive(bool isActive)
        {
            SetNamedObjectActive("StorageCard", isActive);
            SetNamedObjectActive("StorageAccent", isActive);
            SetNamedObjectActive("StorageCaption", isActive);
            SetNamedObjectActive("RecipeCard", isActive);
            SetNamedObjectActive("RecipeAccent", isActive);
            SetNamedObjectActive("RecipeCaption", isActive);
            SetNamedObjectActive("UpgradeCard", isActive);
            SetNamedObjectActive("UpgradeAccent", isActive);
            SetNamedObjectActive("UpgradeCaption", isActive);
            SetNamedObjectActive("PopupLeftPanel", false);
            SetNamedObjectActive("PopupRightPanel", false);
        }

        private void SetHubPopupDesignActive(bool isActive)
        {
            SetNamedObjectActive("PopupFrame", isActive);
            SetNamedObjectActive("PopupFrameLeft", isActive);
            SetNamedObjectActive("PopupFrameRight", isActive);
            SetNamedObjectActive("PopupLeftBody", isActive);
            SetNamedObjectActive("PopupRightBody", isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupTitle, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupLeftCaption, isActive);
            SetNamedObjectActive(PrototypeUIObjectNames.PopupRightCaption, isActive);
            SetButtonGameObjectActive(popupCloseButton, isActive);
        }

        private void SetHubHudVisible(bool isVisible)
        {
            SetNamedObjectActive("TopLeftPanel", isVisible);
            SetNamedObjectActive(HudPanelButtonGroupObjectName, isVisible);
            SetNamedObjectActive("ActionDock", false);
            SetNamedObjectActive("ActionAccent", false);
            SetNamedObjectActive("ActionCaption", false);
            HideLegacyDayRoutineObjects();

            if (goldText != null)
            {
                goldText.gameObject.SetActive(isVisible);
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(isVisible && !string.IsNullOrWhiteSpace(interactionPromptText.text));
            }

            SetNamedObjectActive("InteractionPromptBackdrop", isVisible && interactionPromptText != null && !string.IsNullOrWhiteSpace(interactionPromptText.text));
            SetButtonGameObjectActive(guideHelpButton, isVisible);

            SetButtonGameObjectActive(recipePanelButton, isVisible);
            SetButtonGameObjectActive(upgradePanelButton, isVisible);
            SetButtonGameObjectActive(materialPanelButton, isVisible);
        }

        private void RefreshHubPopupContent()
        {
            if (!IsHubScene())
            {
                return;
            }

            PopupPanelContent popupContent = BuildCurrentHubPopupContent();

            if (inventoryText != null)
            {
                inventoryText.text = string.Empty;
            }

            if (selectedRecipeText != null)
            {
                selectedRecipeText.text = popupContent.DetailText;
            }

            RefreshPopupBodyItemBoxes(popupContent.Entries);
        }

        private PopupPanelContent BuildCurrentHubPopupContent()
        {
            return activeHubPanel switch
            {
                HubPopupPanel.Storage => BuildStoragePopupContent(),
                HubPopupPanel.Recipe => BuildRecipePopupContent(),
                HubPopupPanel.Upgrade => BuildUpgradePopupContent(),
                HubPopupPanel.Materials => BuildMaterialPopupContent(),
                _ => new PopupPanelContent(new List<PopupListEntry>(), string.Empty)
            };
        }

        private PopupPanelContent BuildRecipePopupContent()
        {
            List<PopupListEntry> entries = new();
            if (cachedRestaurant == null)
            {
                return new PopupPanelContent(entries, "선택된 메뉴가 없습니다.");
            }

            IReadOnlyList<RecipeData> recipes = cachedRestaurant.AvailableRecipes;
            RecipeData detailRecipe = cachedRestaurant.SelectedRecipe;
            if (detailRecipe == null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i] != null)
                    {
                        detailRecipe = recipes[i];
                        break;
                    }
                }
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeData recipe = recipes[i];
                if (recipe == null)
                {
                    continue;
                }

                int recipeIndex = i;
                entries.Add(new PopupListEntry(
                    recipe.RecipeId,
                    recipe.DisplayName,
                    BuildPopupItemSummary(recipe.Description, $"판매가 {recipe.SellPrice} · 가능 {cachedRestaurant.GetCookableServings(recipe)}"),
                    BuildRecipePopupDetailText(recipe),
                    ResolveRecipePopupIcon(recipe),
                    recipe == detailRecipe,
                    () =>
                    {
                        if (cachedRestaurant != null)
                        {
                            if (GameManager.Instance != null
                                && GameManager.Instance.RemoteSession != null
                                && GameManager.Instance.RemoteSession.TrySelectRecipe(cachedRestaurant, recipeIndex))
                            {
                                return;
                            }

                            cachedRestaurant.SelectRecipeByIndex(recipeIndex);
                        }
                    }));
            }

            return new PopupPanelContent(entries, BuildRecipePopupDetailText(detailRecipe));
        }

        private PopupPanelContent BuildMaterialPopupContent()
        {
            List<PopupListEntry> entries = new();
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null)
            {
                return new PopupPanelContent(entries, "가방 정보를 찾지 못했습니다.");
            }

            InventoryEntry detailEntry = null;
            foreach (InventoryEntry entry in inventory.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                if (detailEntry == null || entry.resource == selectedMaterialPopupResource)
                {
                    detailEntry = entry;
                }
            }

            foreach (InventoryEntry entry in inventory.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                ResourceData resource = entry.resource;
                int amount = entry.amount;
                entries.Add(new PopupListEntry(
                    resource.ResourceId,
                    $"{resource.DisplayName} x{amount}",
                    BuildPopupItemSummary(resource.Description, $"{resource.RegionTag} · {GetRarityLabel(resource.Rarity)}"),
                    BuildMaterialPopupDetailText(resource, amount),
                    resource.Icon,
                    detailEntry != null && resource == detailEntry.resource,
                    () =>
                    {
                        selectedMaterialPopupResource = resource;
                        RefreshHubPopupContent();
                    }));
            }

            return new PopupPanelContent(entries, BuildMaterialPopupDetailText(detailEntry != null ? detailEntry.resource : null, detailEntry != null ? detailEntry.amount : 0));
        }

        private PopupPanelContent BuildStoragePopupContent()
        {
            List<PopupListEntry> entries = new();
            if (cachedStorage == null)
            {
                return new PopupPanelContent(entries, "창고 정보를 찾지 못했습니다.");
            }

            cachedStorage.InitializeIfNeeded();

            InventoryEntry detailEntry = null;
            foreach (InventoryEntry entry in cachedStorage.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                if (detailEntry == null || entry.resource == selectedStoragePopupResource)
                {
                    detailEntry = entry;
                }
            }

            foreach (InventoryEntry entry in cachedStorage.RuntimeItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                ResourceData resource = entry.resource;
                int amount = entry.amount;
                entries.Add(new PopupListEntry(
                    resource.ResourceId,
                    $"{resource.DisplayName} x{amount}",
                    BuildPopupItemSummary(resource.Description, "보관 중인 재료"),
                    BuildStoragePopupDetailText(entry),
                    resource.Icon,
                    detailEntry != null && resource == detailEntry.resource,
                    () =>
                    {
                        selectedStoragePopupResource = resource;
                        RefreshHubPopupContent();
                    }));
            }

            return new PopupPanelContent(entries, BuildStoragePopupDetailText(detailEntry));
        }

        private PopupPanelContent BuildUpgradePopupContent()
        {
            List<PopupListEntry> rawEntries = new();
            if (cachedUpgradeManager == null)
            {
                return new PopupPanelContent(new List<PopupListEntry>(), "업그레이드 정보를 찾지 못했습니다.");
            }

            cachedUpgradeManager.InitializeIfNeeded();

            foreach (ToolUnlockCost cost in cachedUpgradeManager.ToolUnlockCosts)
            {
                if (cost == null || cost.toolType == ToolType.None)
                {
                    continue;
                }

                string key = $"tool:{cost.toolType}";
                ToolType toolType = cost.toolType;
                rawEntries.Add(new PopupListEntry(
                    key,
                    $"{toolType.GetDisplayName()} 해금",
                    BuildPopupItemSummary(cost.description, BuildUpgradeAvailabilityLabel(toolType)),
                    BuildToolUnlockPopupDetailText(cost),
                    ResolveUpgradePopupIcon(cost.requiredResource),
                    false,
                    () =>
                    {
                        selectedUpgradePopupKey = key;
                        RefreshHubPopupContent();
                    }));
            }

            IReadOnlyList<InventoryUpgradeCost> inventoryUpgradeCosts = cachedUpgradeManager.InventoryUpgradeCosts;
            for (int i = 0; i < inventoryUpgradeCosts.Count; i++)
            {
                InventoryUpgradeCost cost = inventoryUpgradeCosts[i];
                if (cost == null)
                {
                    continue;
                }

                string key = $"inventory:{i}";
                int upgradeIndex = i;
                rawEntries.Add(new PopupListEntry(
                    key,
                    BuildInventoryUpgradeTitle(upgradeIndex),
                    BuildPopupItemSummary(cost.description, BuildInventoryUpgradeAvailabilityLabel(upgradeIndex)),
                    BuildInventoryUpgradePopupDetailText(upgradeIndex, cost),
                    ResolveUpgradePopupIcon(cost.requiredResource),
                    false,
                    () =>
                    {
                        selectedUpgradePopupKey = key;
                        RefreshHubPopupContent();
                    }));
            }

            if (rawEntries.Count == 0)
            {
                return new PopupPanelContent(new List<PopupListEntry>(), "남아 있는 업그레이드가 없습니다.");
            }

            string preferredKey = ResolvePreferredUpgradePopupKey();
            string selectedKey = ContainsPopupEntryKey(rawEntries, selectedUpgradePopupKey)
                ? selectedUpgradePopupKey
                : ContainsPopupEntryKey(rawEntries, preferredKey)
                    ? preferredKey
                    : rawEntries[0].Key;

            List<PopupListEntry> entries = new(rawEntries.Count);
            string detailText = rawEntries[0].Detail;
            foreach (PopupListEntry entry in rawEntries)
            {
                bool isSelected = entry.Key == selectedKey;
                entries.Add(new PopupListEntry(entry.Key, entry.Title, entry.Summary, entry.Detail, entry.Icon, isSelected, entry.OnSelected));
                if (isSelected)
                {
                    detailText = entry.Detail;
                }
            }

            return new PopupPanelContent(entries, detailText);
        }

        private static bool ContainsPopupEntryKey(List<PopupListEntry> entries, string key)
        {
            if (entries == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            foreach (PopupListEntry entry in entries)
            {
                if (entry != null && entry.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        private string ResolvePreferredUpgradePopupKey()
        {
            if (cachedUpgradeManager == null)
            {
                return string.Empty;
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;

            return cachedUpgradeManager.GetPreferredAction() switch
            {
                UpgradeWorkbenchAction.UnlockTool when cachedUpgradeManager.GetPreferredToolType() != ToolType.None
                    => $"tool:{cachedUpgradeManager.GetPreferredToolType()}",
                UpgradeWorkbenchAction.UpgradeInventory when inventory != null
                    => $"inventory:{inventory.CapacityLevel}",
                _ => string.Empty
            };
        }

        private static string BuildPopupItemSummary(string description, string fallback)
        {
            string source = string.IsNullOrWhiteSpace(description) ? fallback : description;
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            string normalized = source.Replace("\r\n", " ").Replace('\n', ' ').Trim();
            return normalized.Length > 32 ? $"{normalized.Substring(0, 29)}..." : normalized;
        }

        private Sprite ResolveRecipePopupIcon(RecipeData recipe)
        {
            if (recipe == null)
            {
                return null;
            }

            Sprite recipeSprite = LoadRecipeSpriteById(recipe.RecipeId);
            if (recipeSprite != null)
            {
                return recipeSprite;
            }

            if (recipe.Ingredients == null)
            {
                return null;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out _)
                    && resource.Icon != null)
                {
                    return resource.Icon;
                }
            }

            return null;
        }

        private static Sprite LoadRecipeSpriteById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return null;
            }

            string normalizedRecipeId = recipeId.Trim();
            return Resources.Load<Sprite>($"Generated/Sprites/Recipes/{normalizedRecipeId}")
                ?? Resources.Load<Sprite>($"Generated/Sprites/Hub/{normalizedRecipeId}");
        }

        private static Sprite ResolveUpgradePopupIcon(ResourceData requiredResource)
        {
            return requiredResource != null ? requiredResource.Icon : null;
        }

        private static string GetRarityLabel(ResourceRarity rarity)
        {
            return rarity switch
            {
                ResourceRarity.Uncommon => "고급",
                ResourceRarity.Rare => "희귀",
                ResourceRarity.Epic => "특급",
                _ => "일반"
            };
        }

        private string BuildRecipePopupDetailText(RecipeData recipe)
        {
            if (cachedRestaurant == null || recipe == null)
            {
                return "선택된 메뉴가 없습니다.";
            }

            StringBuilder builder = new();
            builder.AppendLine(recipe.DisplayName);

            if (!string.IsNullOrWhiteSpace(recipe.Description))
            {
                builder.AppendLine(recipe.Description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 판매가: {recipe.SellPrice}");
            if (!string.IsNullOrWhiteSpace(recipe.SupplySource))
            {
                builder.AppendLine($"- 공급처: {recipe.SupplySource}");
            }

            if (recipe.Difficulty > 0)
            {
                builder.AppendLine($"- 난이도: {recipe.Difficulty}");
            }

            if (!string.IsNullOrWhiteSpace(recipe.CookingMethod))
            {
                builder.AppendLine($"- 조리법: {recipe.CookingMethod}");
            }

            builder.AppendLine($"- 가능 수량: {cachedRestaurant.GetCookableServings(recipe)}");
            builder.AppendLine();
            builder.AppendLine("- 필요 재료");

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                {
                    continue;
                }

                string displayName = ingredient.BuildDisplayNameWithCatalogSummary();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                if (!RecipeIngredient.TryResolve(ingredient, out ResourceData resource, out int ingredientAmount))
                {
                    builder.AppendLine($"  {displayName} x{ingredient.Quantity}");
                    continue;
                }

                int ownedAmount = GameManager.Instance != null && GameManager.Instance.Inventory != null
                    ? GameManager.Instance.Inventory.GetAmount(resource)
                    : 0;
                builder.AppendLine($"  {displayName} {ownedAmount}/{ingredientAmount}");
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildMaterialPopupDetailText(ResourceData resource, int amount)
        {
            if (resource == null)
            {
                return "선택된 재료가 없습니다.";
            }

            StringBuilder builder = new();
            builder.AppendLine(resource.DisplayName);

            if (!string.IsNullOrWhiteSpace(resource.Description))
            {
                builder.AppendLine(resource.Description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 보유 수량: x{amount}");
            builder.AppendLine($"- 채집 지역: {resource.RegionTag}");
            builder.AppendLine($"- 희귀도: {GetRarityLabel(resource.Rarity)}");
            builder.AppendLine($"- 기본 판매가: {resource.BaseSellPrice}");

            if (cachedRestaurant != null && cachedRestaurant.SelectedRecipe != null)
            {
                RecipeData recipe = cachedRestaurant.SelectedRecipe;
                int requiredAmount = GetRecipeIngredientAmount(recipe, resource);
                builder.AppendLine();
                builder.AppendLine($"- 선택 메뉴: {recipe.DisplayName}");
                builder.AppendLine(requiredAmount > 0
                    ? $"- 메뉴 필요 수량: {requiredAmount}"
                    : "- 메뉴 사용처: 현재 선택 메뉴에는 들어가지 않음");
                builder.AppendLine($"- 현재 가능 수량: {cachedRestaurant.GetCookableServings(recipe)}");
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildToolUnlockPopupDetailText(ToolUnlockCost cost)
        {
            if (cost == null || cost.toolType == ToolType.None || cachedUpgradeManager == null)
            {
                return "업그레이드 정보를 찾지 못했습니다.";
            }

            bool isUnlocked = cachedUpgradeManager.IsToolUnlocked(cost.toolType);
            bool canUnlock = !isUnlocked && cachedUpgradeManager.CanUnlockTool(cost.toolType);
            int ownedAmount = cost.requiredResource != null && GameManager.Instance != null && GameManager.Instance.Inventory != null
                ? GameManager.Instance.Inventory.GetAmount(cost.requiredResource)
                : 0;

            StringBuilder builder = new();
            builder.AppendLine($"{cost.toolType.GetDisplayName()} 해금");

            if (!string.IsNullOrWhiteSpace(cost.description))
            {
                builder.AppendLine(cost.description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 상태: {BuildUpgradeAvailabilityLabel(cost.toolType)}");
            builder.AppendLine($"- 비용: {BuildUpgradeCostText(cost.goldCost, cost.requiredResource, cost.requiredAmount)}");
            if (cost.requiredResource != null && cost.requiredAmount > 0)
            {
                builder.AppendLine($"- 보유 재료: {ownedAmount}/{cost.requiredAmount}");
            }

            builder.AppendLine($"- 활용: {BuildToolUnlockUseDescription(cost.toolType)}");
            if ((isUnlocked || canUnlock) && !string.IsNullOrWhiteSpace(cachedUpgradeManager.LastUpgradeMessage))
            {
                builder.AppendLine();
                builder.AppendLine(cachedUpgradeManager.LastUpgradeMessage);
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildInventoryUpgradePopupDetailText(int index, InventoryUpgradeCost cost)
        {
            if (cost == null)
            {
                return "업그레이드 정보를 찾지 못했습니다.";
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            int currentSlots = inventory != null ? inventory.GetSlotCapacityForLevel(index) : 0;
            int nextSlots = inventory != null ? inventory.GetSlotCapacityForLevel(index + 1) : 0;
            int ownedAmount = cost.requiredResource != null && inventory != null
                ? inventory.GetAmount(cost.requiredResource)
                : 0;

            StringBuilder builder = new();
            builder.AppendLine(BuildInventoryUpgradeTitle(index));

            if (!string.IsNullOrWhiteSpace(cost.description))
            {
                builder.AppendLine(cost.description);
                builder.AppendLine();
            }

            builder.AppendLine($"- 상태: {BuildInventoryUpgradeAvailabilityLabel(index)}");
            builder.AppendLine($"- 확장: {currentSlots}칸 -> {nextSlots}칸");
            builder.AppendLine($"- 비용: {BuildUpgradeCostText(cost.goldCost, cost.requiredResource, cost.requiredAmount)}");
            if (cost.requiredResource != null && cost.requiredAmount > 0)
            {
                builder.AppendLine($"- 보유 재료: {ownedAmount}/{cost.requiredAmount}");
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildInventoryUpgradeTitle(int index)
        {
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null)
            {
                return $"가방 확장 단계 {index + 1}";
            }

            return $"가방 확장 {inventory.GetSlotCapacityForLevel(index)}칸 -> {inventory.GetSlotCapacityForLevel(index + 1)}칸";
        }

        private string BuildInventoryUpgradeAvailabilityLabel(int index)
        {
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            if (inventory == null || cachedUpgradeManager == null)
            {
                return "정보 없음";
            }

            if (inventory.CapacityLevel > index)
            {
                return "완료";
            }

            if (inventory.CapacityLevel == index)
            {
                return cachedUpgradeManager.CanUpgradeInventory() ? "지금 진행 가능" : "재료 준비 필요";
            }

            return "이전 단계 필요";
        }

        private string BuildUpgradeAvailabilityLabel(ToolType toolType)
        {
            if (cachedUpgradeManager == null || toolType == ToolType.None)
            {
                return "정보 없음";
            }

            if (cachedUpgradeManager.IsToolUnlocked(toolType))
            {
                return "완료";
            }

            return cachedUpgradeManager.CanUnlockTool(toolType) ? "지금 진행 가능" : "재료 준비 필요";
        }

        private static string BuildToolUnlockUseDescription(ToolType toolType)
        {
            return toolType switch
            {
                ToolType.Lantern => "폐광산처럼 어두운 지역 진입에 필요",
                ToolType.Sickle => "풀숲과 약초 채집 범위를 넓힘",
                ToolType.FishingRod => "바닷가 채집 효율을 보강",
                ToolType.Rake => "얕은 채집 지점 정리에 사용",
                _ => "새 지역이나 상호작용 해금에 사용"
            };
        }

        private static string BuildUpgradeCostText(int goldCost, ResourceData requiredResource, int requiredAmount)
        {
            List<string> parts = new();
            if (goldCost > 0)
            {
                parts.Add($"골드 {goldCost}");
            }

            if (requiredResource != null && requiredAmount > 0)
            {
                parts.Add($"{requiredResource.DisplayName} x{requiredAmount}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "비용 없음";
        }

        private string BuildStoragePopupDetailText(InventoryEntry entry)
        {
            if (cachedStorage == null)
            {
                return "창고 정보를 찾지 못했습니다.";
            }

            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
            InventoryEntry depositEntry = cachedStorage.GetSelectedInventoryEntry(inventory);
            InventoryEntry withdrawEntry = cachedStorage.GetSelectedStoredEntry();
            depositEntry = depositEntry != null && depositEntry.resource != null ? depositEntry : null;
            withdrawEntry = withdrawEntry != null && withdrawEntry.resource != null ? withdrawEntry : null;

            StringBuilder builder = new();
            if (entry != null && entry.resource != null)
            {
                builder.AppendLine(entry.resource.DisplayName);

                if (!string.IsNullOrWhiteSpace(entry.resource.Description))
                {
                    builder.AppendLine(entry.resource.Description);
                    builder.AppendLine();
                }

                builder.AppendLine($"- 보관 수량: x{entry.amount}");
                builder.AppendLine($"- 원산지: {entry.resource.RegionTag}");
                builder.AppendLine($"- 기본 판매가: {entry.resource.BaseSellPrice}");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine("선택된 보관 재료가 없습니다.");
                builder.AppendLine();
            }

            builder.AppendLine(depositEntry != null
                ? $"맡길 재료: {depositEntry.resource.DisplayName} x{depositEntry.amount}"
                : "맡길 재료: 없음");
            builder.AppendLine(withdrawEntry != null
                ? $"꺼낼 재료: {withdrawEntry.resource.DisplayName} x{withdrawEntry.amount}"
                : "꺼낼 재료: 없음");
            builder.AppendLine();
            builder.AppendLine("Q 품목 변경");
            builder.AppendLine("W 맡기기");
            builder.AppendLine("A 꺼낼 재료 변경");
            builder.AppendLine("S 꺼내기");

            if (!string.IsNullOrWhiteSpace(cachedStorage.LastOperationMessage))
            {
                builder.AppendLine();
                builder.AppendLine(cachedStorage.LastOperationMessage);
            }

            return builder.ToString().TrimEnd();
        }

        private static int GetRecipeIngredientAmount(RecipeData recipe, ResourceData resource)
        {
            if (recipe == null || resource == null || recipe.Ingredients == null)
            {
                return 0;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (RecipeIngredient.TryResolve(ingredient, out ResourceData ingredientResource, out int ingredientAmount)
                    && ingredientResource == resource)
                {
                    return ingredientAmount;
                }
            }

            return 0;
        }

        private string BuildHubMessageLine()
        {
            if (!IsHubScene() || activeHubPanel != HubPopupPanel.None)
            {
                return string.Empty;
            }

            string message = cachedDayCycle != null ? cachedDayCycle.CurrentGuideText : string.Empty;

            if (cachedRestaurant != null
                && !string.IsNullOrWhiteSpace(cachedRestaurant.LastServiceResult))
            {
                message = cachedRestaurant.LastServiceResult;
            }

            return CompactToSingleLine(message);
        }

        private static string CompactToSingleLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string compact = text.Replace("\r", " ").Replace("\n", " / ");
            while (compact.Contains("  "))
            {
                compact = compact.Replace("  ", " ");
            }

            return compact.Trim();
        }

        private void RefreshStoragePanelVisibility()
        {
            if (!IsHubScene())
            {
                if (storageText != null)
                {
                    storageText.gameObject.SetActive(false);
                }

                return;
            }

            if (!Application.isPlaying)
            {
                RefreshHubPopupOverlay();
                return;
            }

            if (activeHubPanel == HubPopupPanel.Storage && !IsPlayerNearStorageStation())
            {
                activeHubPanel = HubPopupPanel.None;
                ApplyMenuPanelState();
                return;
            }

            RefreshHubPopupOverlay();
        }

        private void ApplyMenuPanelState()
        {
            bool isHubScene = IsHubScene();

            if (isHubScene)
            {
                PrototypeUITheme theme = PrototypeUIThemePalette.GetForScene(SceneManager.GetActiveScene().name);
                TMP_FontAsset headingFont = TmpFontAssetResolver.ResolveHeadingFontOrDefault(headingFontAsset, bodyFontAsset);
                bool showPopup = activeHubPanel != HubPopupPanel.None;

                ApplyHubPopupFrameStyle(headingFont, theme.Text);
                SetHubPopupDesignActive(showPopup);
                SetHubHudVisible(!showPopup);
                SetLegacyHubPopupObjectsActive(false);

                RefreshHubPopupContent();

                if (inventoryText != null)
                {
                    inventoryText.gameObject.SetActive(false);
                }

                if (selectedRecipeText != null)
                {
                    selectedRecipeText.gameObject.SetActive(showPopup);
                }

                if (storageText != null)
                {
                    storageText.gameObject.SetActive(false);
                }

                if (upgradeText != null)
                {
                    upgradeText.gameObject.SetActive(false);
                }
            }
            else
            {
                SetHubPopupDesignActive(false);
                SetLegacyHubPopupObjectsActive(false);

                if (inventoryText != null)
                {
                    inventoryText.gameObject.SetActive(false);
                }

                if (storageText != null)
                {
                    storageText.gameObject.SetActive(false);
                }

                if (selectedRecipeText != null)
                {
                    selectedRecipeText.gameObject.SetActive(false);
                }

                if (upgradeText != null)
                {
                    upgradeText.gameObject.SetActive(false);
                }
            }

            RefreshHubPopupOverlay();
            RefreshGuideText();
            RefreshButtonStates();
        }

        private void RefreshHubPopupOverlay()
        {
            bool shouldShowOverlay = IsHubScene() && activeHubPanel != HubPopupPanel.None;
            SetNamedObjectActive("PopupOverlay", shouldShowOverlay);
            ApplyPopupPauseState(shouldShowOverlay);
        }

        private bool IsPlayerNearStorageStation()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }

            InteractionDetector detector = cachedPlayer != null ? cachedPlayer.InteractionDetector : null;
            return detector != null && detector.CurrentInteractable is StorageStation;
        }

        /// <summary>
        /// 허브 팝업이 열려 있는 동안에는 시간을 멈춰 배경 진행을 막습니다.
        /// </summary>
        private void ApplyPopupPauseState(bool shouldPause)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            PopupPauseStateUtility.Snapshot snapshot = PopupPauseStateUtility.Apply(
                shouldPause,
                isPopupPauseApplied,
                popupPausePreviousTimeScale,
                Time.timeScale);
            popupPausePreviousTimeScale = snapshot.PreviousTimeScale;
            Time.timeScale = snapshot.NextTimeScale;
            isPopupPauseApplied = snapshot.IsPauseApplied;
        }

        private void RestorePopupPauseIfNeeded()
        {
            PopupPauseStateUtility.Snapshot snapshot = PopupPauseStateUtility.Restore(
                isPopupPauseApplied,
                popupPausePreviousTimeScale,
                Time.timeScale);
            popupPausePreviousTimeScale = snapshot.PreviousTimeScale;
            Time.timeScale = snapshot.NextTimeScale;
            isPopupPauseApplied = snapshot.IsPauseApplied;
        }

        private static bool IsHubScene()
        {
            return SceneManager.GetActiveScene().name == "Hub";
        }

        private void ApplyNamedRectLayout(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            RectTransform rect = target != null ? target.GetComponent<RectTransform>() : null;
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));
            ApplyRectLayout(
                rect,
                resolvedLayout.AnchorMin,
                resolvedLayout.AnchorMax,
                resolvedLayout.Pivot,
                resolvedLayout.AnchoredPosition,
                resolvedLayout.SizeDelta);
        }

        private void ApplyNamedRectLayout(string objectName, PrototypeUIRect layout)
        {
            ApplyNamedRectLayout(
                objectName,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta);
        }

        private void SetNamedObjectActive(string objectName, bool isActive)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            if (target != null)
            {
                target.gameObject.SetActive(isActive);
            }
        }

        private void SetNamedObjectActiveRaw(string objectName, bool isActive)
        {
            if (transform == null || string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            if (target != null)
            {
                target.gameObject.SetActive(isActive);
            }
        }

        private void HideLegacyDayRoutineObjects()
        {
            SetNamedObjectActiveRaw("PhaseBadge", false);
            SetNamedObjectActiveRaw("DayPhaseText", false);
            SetNamedObjectActiveRaw("SkipExplorationButton", false);
            SetNamedObjectActiveRaw("SkipServiceButton", false);
            SetNamedObjectActiveRaw("NextDayButton", false);
        }

        /// <summary>
        /// 현재 씬의 모든 HUD 텍스트와 카드 표시 상태를 한 번에 다시 맞춥니다.
        /// </summary>
        private void RefreshAll()
        {
            // 개별 이벤트가 누락돼도 전체 재계산 한 번으로 화면 상태를 다시 복구합니다.
            RefreshInventoryText();
            RefreshStorageText();
            RefreshUpgradeText();
            RefreshEconomyText();
            RefreshInteractionPrompt();
            RefreshSelectedRecipeText(cachedRestaurant != null ? cachedRestaurant.SelectedRecipe : null);
            RefreshRestaurantResultText(cachedRestaurant != null ? cachedRestaurant.LastServiceResult : string.Empty);
            RefreshDayCycleState();

            ApplyMenuPanelState();
            RefreshStoragePanelVisibility();
        }

        /// <summary>
        /// 현재 플레이어가 상호작용할 수 있는 대상의 프롬프트를 하단에 표시합니다.
        /// </summary>
        private void RefreshInteractionPrompt()
        {
            if (interactionPromptText == null)
            {
                return;
            }

            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }

            string prompt = string.Empty;
            InteractionDetector detector = cachedPlayer != null ? cachedPlayer.InteractionDetector : null;

            if (detector != null && detector.CurrentInteractable != null)
            {
                prompt = detector.CurrentInteractable.InteractionPrompt;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                prompt = defaultPromptText;
            }

            bool shouldShowPrompt = activeHubPanel == HubPopupPanel.None;

            interactionPromptText.text = prompt;
            interactionPromptText.gameObject.SetActive(shouldShowPrompt && !string.IsNullOrWhiteSpace(prompt));
            SetNamedObjectActive("InteractionPromptBackdrop", shouldShowPrompt && !string.IsNullOrWhiteSpace(prompt));
            RefreshStoragePanelVisibility();
        }

        /// <summary>
        /// 현재 보유 재료와 가방 사용량을 카드 본문용 문자열로 정리합니다.
        /// </summary>
        private void RefreshInventoryText()
        {
            if (inventoryText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.Inventory == null)
            {
                inventoryText.text = "인벤토리 없음";
                RefreshHubPopupContent();
                return;
            }

            IReadOnlyList<InventoryEntry> entries = GameManager.Instance.Inventory.RuntimeItems;
            int usedSlots = GameManager.Instance.Inventory.UsedSlotCount;
            int maxSlots = GameManager.Instance.Inventory.MaxSlotCount;

            if (entries.Count == 0)
            {
                inventoryText.text = $"인벤토리 {usedSlots}/{maxSlots}칸\n- 비어 있음";
                RefreshHubPopupContent();
                return;
            }

            StringBuilder builder = new();
            builder.AppendLine($"인벤토리 {usedSlots}/{maxSlots}칸");

            foreach (InventoryEntry entry in entries)
            {
                if (entry == null || entry.resource == null)
                {
                    continue;
                }

                builder.AppendLine($"- {entry.resource.DisplayName} x{entry.amount}");
            }

            inventoryText.text = builder.ToString().TrimEnd();
            RefreshHubPopupContent();
        }

        /// <summary>
        /// 창고 목록과 마지막 작업 메시지를 창고 팝업 본문에 갱신합니다.
        /// </summary>
        private void RefreshStorageText()
        {
            if (storageText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (cachedStorage == null)
            {
                storageText.text = string.Empty;
                return;
            }

            string summary = cachedStorage.BuildSummaryText();
            if (!string.IsNullOrWhiteSpace(cachedStorage.LastOperationMessage))
            {
                summary += $"\n\n{cachedStorage.LastOperationMessage}";
            }

            storageText.text = summary;
            RefreshHubPopupContent();
        }

        /// <summary>
        /// 업그레이드 요약과 마지막 결과 메시지를 팝업 본문용으로 정리합니다.
        /// </summary>
        private void RefreshUpgradeText()
        {
            if (upgradeText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (cachedUpgradeManager == null)
            {
                upgradeText.text = string.Empty;
                return;
            }

            string summary = cachedUpgradeManager.BuildUpgradeSummary();
            if (!string.IsNullOrWhiteSpace(cachedUpgradeManager.LastUpgradeMessage))
            {
                summary += $"\n\n{cachedUpgradeManager.LastUpgradeMessage}";
            }

            upgradeText.text = summary;
            RefreshHubPopupContent();
        }

        /// <summary>
        /// 좌측 상단 상태 줄에는 코인과 평판만 간단히 노출합니다.
        /// </summary>
        private void RefreshEconomyText()
        {
            if (goldText == null)
            {
                return;
            }

            int gold = GameManager.Instance != null && GameManager.Instance.Economy != null
                ? GameManager.Instance.Economy.CurrentGold
                : 0;
            int reputation = GameManager.Instance != null && GameManager.Instance.Economy != null
                ? GameManager.Instance.Economy.CurrentReputation
                : 0;

            goldText.text = $"코인: {gold}   평판: {reputation}";
        }

        /// <summary>
        /// 허브 메뉴 팝업에서 보여 줄 요리 선택 요약을 갱신합니다.
        /// </summary>
        private void RefreshSelectedRecipeText(RecipeData recipe)
        {
            if (selectedRecipeText == null)
            {
                RefreshHubPopupContent();
                return;
            }

            if (cachedRestaurant == null)
            {
                selectedRecipeText.text = "메뉴 선택: 허브에서 확인";
                return;
            }

            string summary = cachedRestaurant.BuildRecipeSelectionSummary();
            if (string.IsNullOrWhiteSpace(summary) && recipe == null)
            {
                selectedRecipeText.text = "선택 메뉴: 없음";
                return;
            }

            selectedRecipeText.text = summary;
            RefreshHubPopupContent();
        }

        private void RefreshGuideText()
        {
            if (guideText == null)
            {
                return;
            }

            if (showGuideHelpOverlay)
            {
                string helpText = BuildGuideHelpOverlayText();
                bool shouldShowHelp = activeHubPanel == HubPopupPanel.None && !string.IsNullOrWhiteSpace(helpText);
                guideText.text = helpText;
                guideText.gameObject.SetActive(shouldShowHelp);
                SetNamedObjectActive("GuideBackdrop", shouldShowHelp);
                return;
            }

            if (IsHubScene())
            {
                string message = BuildHubMessageLine();
                guideText.text = message;
                guideText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
                SetNamedObjectActive("GuideBackdrop", false);

                if (resultText != null)
                {
                    resultText.text = string.Empty;
                    resultText.gameObject.SetActive(false);
                }

                SetNamedObjectActive("ResultBackdrop", false);
                return;
            }

            string guide = cachedDayCycle != null ? cachedDayCycle.CurrentGuideText : string.Empty;
            guideText.text = guide;
            guideText.gameObject.SetActive(!string.IsNullOrWhiteSpace(guide));
            SetNamedObjectActive("GuideBackdrop", !IsHubScene() && guideText.gameObject.activeSelf);
        }

        private string BuildGuideHelpOverlayText()
        {
            if (IsHubScene())
            {
                return "이동: WASD / 방향키   상호작용: E   하단 메뉴 버튼으로 요리, 업그레이드, 재료 패널을 연다";
            }

            return "이동: WASD / 방향키   상호작용: E   채집물과 포탈 앞에서 E로 수집하거나 이동한다";
        }

        private void RefreshRestaurantResultText(string result)
        {
            if (resultText == null)
            {
                return;
            }

            if (IsHubScene())
            {
                resultText.text = string.Empty;
                resultText.gameObject.SetActive(false);
                SetNamedObjectActive("ResultBackdrop", false);
                RefreshGuideText();
                return;
            }

            resultText.text = result;
            resultText.gameObject.SetActive(!string.IsNullOrWhiteSpace(result));
            SetNamedObjectActive("ResultBackdrop", !IsHubScene() && resultText.gameObject.activeSelf);
        }

        /// <summary>
        /// 현재 안내 문구와 허브 패널 버튼 상태를 함께 맞춥니다.
        /// </summary>
        private void RefreshDayCycleState()
        {
            HideLegacyDayRoutineObjects();
            RefreshGuideText();
            RefreshRestaurantResultText(cachedRestaurant != null ? cachedRestaurant.LastServiceResult : string.Empty);
            RefreshButtonStates();

            if (cachedRestaurant != null)
            {
                RefreshSelectedRecipeText(cachedRestaurant.SelectedRecipe);
            }
        }

        private void RefreshButtonStates()
        {
            HideLegacyDayRoutineObjects();
        }
    }
}

