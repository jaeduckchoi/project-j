using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// 현재 HUD, 허브 팝업, 창고 패널을 한곳에서 갱신하는 최소 UI 관리자입니다.
// 코인/평판, 재료, 업그레이드, 상호작용 문구, 단계 표시를 모두 여기서 동기화합니다.
public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private TextMeshProUGUI inventoryText;
    [SerializeField] private TextMeshProUGUI storageText;
    [SerializeField] private TextMeshProUGUI upgradeText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI selectedRecipeText;
    [SerializeField] private TextMeshProUGUI dayPhaseText;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TMP_FontAsset bodyFontAsset;
    [SerializeField] private TMP_FontAsset headingFontAsset;
    [SerializeField] private Button skipExplorationButton;
    [SerializeField] private Button skipServiceButton;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button recipePanelButton;
    [SerializeField] private Button upgradePanelButton;
    [SerializeField] private Button materialPanelButton;
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
    private bool isPopupPauseApplied;
    private float popupPausePreviousTimeScale = 1f;

#if ENABLE_INPUT_SYSTEM
    private static InputActionAsset runtimeUiActionsAsset;
#endif

    private enum HubPopupPanel
    {
        None,
        Recipe,
        Upgrade,
        Materials
    }

    private void Awake()
    {
        EnsureEventSystemExists();
    }

    /*
     * 씬 전환 뒤에도 UI 바인딩을 다시 연결할 수 있도록 sceneLoaded 콜백을 등록합니다.
     */
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    /*
     * 시작 시점에 현재 씬 기준 참조를 다시 찾고 전체 UI를 한 번 갱신합니다.
     */
    private void Start()
    {
        BindSceneReferences();
        BindButtons();
        ApplyTextPresentation();
        RefreshAll();
    }

    /*
     * 매 프레임 상호작용 문구와 버튼 표시 상태를 최신 상태로 유지합니다.
     */
    private void Update()
    {
        RefreshInteractionPrompt();
        RefreshButtonStates();
    }

    /*
     * 등록했던 이벤트와 버튼 리스너를 정리해 중복 호출을 막습니다.
     */
    private void OnDisable()
    {
        RestorePopupPauseIfNeeded();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindInventory();
        UnbindStorage();
        UnbindEconomy();
        UnbindTools();
        UnbindRestaurant();
        UnbindDayCycle();
        UnbindUpgradeManager();
        UnbindButtons();
    }

    /*
     * 씬 전환 직후 새 씬의 플레이어와 매니저 참조를 다시 묶습니다.
     */
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystemExists();
        BindSceneReferences();
        BindButtons();
        ApplyTextPresentation();
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
    /*
     * Input System 기본 액션 생성 경로 대신 런타임 UI 액션 에셋을 직접 연결합니다.
     * 이렇게 해야 InputActionReference 생성 예외 없이 버튼 입력을 안정적으로 받을 수 있습니다.
     */
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
        if (runtimeUiActionsAsset != null)
        {
            return runtimeUiActionsAsset;
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

        runtimeUiActionsAsset = asset;
        return runtimeUiActionsAsset;
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
        if (recipePanelButton == null)
        {
            Transform recipeTransform = transform.Find("RecipePanelButton");
            if (recipeTransform != null)
            {
                recipePanelButton = recipeTransform.GetComponent<Button>();
            }
        }

        if (upgradePanelButton == null)
        {
            Transform upgradeTransform = transform.Find("UpgradePanelButton");
            if (upgradeTransform != null)
            {
                upgradePanelButton = upgradeTransform.GetComponent<Button>();
            }
        }

        if (materialPanelButton == null)
        {
            Transform materialTransform = transform.Find("MaterialPanelButton");
            if (materialTransform != null)
            {
                materialPanelButton = materialTransform.GetComponent<Button>();
            }
        }

        if (guideText == null)
        {
            Transform guideTransform = transform.Find("GuideText");
            if (guideTransform != null)
            {
                guideText = guideTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (resultText == null)
        {
            Transform resultTransform = transform.Find("RestaurantResultText");
            if (resultTransform != null)
            {
                resultText = resultTransform.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    /*
     * 스킵 버튼과 허브 팝업 버튼에 현재 씬 기준 클릭 리스너를 연결합니다.
     */
    private void BindButtons()
    {
        UnbindButtons();
        ResolveOptionalUiReferences();

        if (skipExplorationButton != null)
        {
            skipExplorationButton.onClick.AddListener(HandleSkipExplorationClicked);
        }

        if (skipServiceButton != null)
        {
            skipServiceButton.onClick.AddListener(HandleSkipServiceClicked);
        }

        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(HandleNextDayClicked);
        }

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
    }

    /*
     * 버튼 리스너를 해제해 씬 재진입 시 중복 구독을 막습니다.
     */
    private void UnbindButtons()
    {
        if (skipExplorationButton != null)
        {
            skipExplorationButton.onClick.RemoveListener(HandleSkipExplorationClicked);
        }

        if (skipServiceButton != null)
        {
            skipServiceButton.onClick.RemoveListener(HandleSkipServiceClicked);
        }

        if (nextDayButton != null)
        {
            nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
        }

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
    }

    /*
     * 오전 탐험 단계를 즉시 넘기고 HUD를 다시 맞춥니다.
     */
    private void HandleSkipExplorationClicked()
    {
        cachedDayCycle?.SkipExploration();
        RefreshAll();
    }

    /*
     * 허브 영업 단계를 즉시 넘기고, 식당 매니저가 있으면 그 경로를 우선 사용합니다.
     */
    private void HandleSkipServiceClicked()
    {
        if (cachedRestaurant != null)
        {
            cachedRestaurant.SkipService();
        }
        else
        {
            cachedDayCycle?.SkipService();
        }

        RefreshAll();
    }

    /*
     * 정산 이후 다음 날로 넘기고 관련 HUD를 다시 갱신합니다.
     */
    private void HandleNextDayClicked()
    {
        cachedDayCycle?.AdvanceToNextDay();
        RefreshAll();
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

    private void ToggleHubPanel(HubPopupPanel targetPanel)
    {
        if (!IsHubScene())
        {
            return;
        }

        activeHubPanel = activeHubPanel == targetPanel ? HubPopupPanel.None : targetPanel;
        ApplyMenuPanelState();
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

    /*
     * 해상도 차이로 HUD가 무너지지 않도록 캔버스 스케일 기준을 고정합니다.
     */
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

    /*
     * 현재 UI 구조에 맞춰 패널, 강조선, 텍스트, 버튼 스타일을 한 번에 다시 적용합니다.
     */
    private void ApplyTextPresentation()
    {
        bool isHubScene = IsHubScene();
        TMP_FontAsset preferredFont = bodyFontAsset != null
            ? bodyFontAsset
            : TMP_Settings.defaultFontAsset != null
                ? TMP_Settings.defaultFontAsset
                : interactionPromptText != null
                    ? interactionPromptText.font
                    : null;
        TMP_FontAsset headingFont = headingFontAsset != null ? headingFontAsset : preferredFont;

        ApplyCanvasScaleSettings();
        ResolveOptionalUiReferences();
        guideText = EnsureOverlayText(guideText, "GuideText");
        resultText = EnsureOverlayText(resultText, "RestaurantResultText");

        Color parchment = new(0.10f, 0.10f, 0.10f, 0.88f);
        Color paper = new(0.12f, 0.12f, 0.12f, 0.92f);
        Color glass = new(0.16f, 0.16f, 0.16f, 0.84f);
        Color ink = new(0.94f, 0.94f, 0.94f, 1f);
        Color oceanAccent = new(0.72f, 0.72f, 0.72f, 0.96f);
        Color forestAccent = new(0.62f, 0.62f, 0.62f, 0.96f);
        Color amberAccent = new(0.72f, 0.72f, 0.72f, 0.96f);
        Color coralAccent = new(0.56f, 0.56f, 0.56f, 0.96f);
        Color goldAccent = new(0.80f, 0.80f, 0.80f, 0.96f);
        Color nightDock = new(0.08f, 0.08f, 0.08f, 0.94f);

        EnsureCommonHudChrome(isHubScene, preferredFont, parchment, paper, glass, oceanAccent, amberAccent);
        if (isHubScene)
        {
            EnsureHubHudChrome(preferredFont, paper, nightDock, forestAccent, amberAccent, goldAccent);
        }

        // 실제 HUD 배치와 텍스트 스타일은 아래 공용 레이아웃 메서드에서 한 번만 적용합니다.
        ApplyCompactHudLayout(preferredFont, headingFont, ink, oceanAccent, forestAccent, amberAccent, coralAccent, goldAccent);
        ApplyMenuPanelState();
        RefreshStoragePanelVisibility();
        ApplyWorldTextPresentation(preferredFont, headingFont);
    }

    /*
     * 허브와 탐험 씬이 공통으로 쓰는 HUD 바탕과 캡션을 생성합니다.
     */
    private void EnsureCommonHudChrome(
        bool isHubScene,
        TMP_FontAsset preferredFont,
        Color parchment,
        Color paper,
        Color glass,
        Color oceanAccent,
        Color amberAccent)
    {
        EnsureUiBackdrop("TopLeftPanel", PrototypeUiLayout.TopLeftPanel, parchment);
        EnsureUiBackdrop("PhaseBadge", PrototypeUiLayout.PhaseBadge, glass);
        EnsureUiBackdrop("PromptBackdrop", PrototypeUiLayout.PromptBackdrop(isHubScene), new Color(0.08f, 0.08f, 0.08f, 0.82f));
        EnsureUiBackdrop("GuideBackdrop", PrototypeUiLayout.GuideBackdrop(isHubScene), new Color(0.10f, 0.10f, 0.10f, 0.78f));
        EnsureUiBackdrop("ResultBackdrop", PrototypeUiLayout.ResultBackdrop(isHubScene), new Color(0.14f, 0.14f, 0.14f, 0.80f));
        EnsureUiBackdrop("InventoryCard", PrototypeUiLayout.InventoryCard(isHubScene), paper);

        EnsureUiAccentBar("TopLeftAccent", PrototypeUiLayout.TopLeftAccent, amberAccent);
        EnsureUiAccentBar("InventoryAccent", PrototypeUiLayout.InventoryAccent(isHubScene), oceanAccent);
        EnsureUiCaption(
            "InventoryCaption",
            isHubScene ? "재료" : "재료 / 가방",
            PrototypeUiLayout.InventoryCaption(isHubScene),
            preferredFont,
            oceanAccent,
            TextAlignmentOptions.TopLeft);
    }

    /*
     * 허브에서만 필요한 팝업 카드와 진행 버튼 독을 생성합니다.
     */
    private void EnsureHubHudChrome(
        TMP_FontAsset preferredFont,
        Color paper,
        Color nightDock,
        Color forestAccent,
        Color amberAccent,
        Color goldAccent)
    {
        EnsureUiBackdrop("CenterBottomPanel", PrototypeUiLayout.HubCenterBottomPanel, new Color(0.10f, 0.10f, 0.10f, 0.84f));
        EnsureUiBackdrop("PopupOverlay", PrototypeUiLayout.HubPopupOverlay, new Color(0f, 0f, 0f, 0.52f));
        EnsureUiBackdrop("StorageCard", PrototypeUiLayout.HubStorageCard, paper);
        EnsureUiBackdrop("RecipeCard", PrototypeUiLayout.HubRecipeCard, paper);
        EnsureUiBackdrop("UpgradeCard", PrototypeUiLayout.HubUpgradeCard, paper);
        EnsureUiBackdrop("ActionDock", PrototypeUiLayout.HubActionDock, nightDock);

        EnsureUiAccentBar("StorageAccent", PrototypeUiLayout.HubStorageAccent, forestAccent);
        EnsureUiAccentBar("RecipeAccent", PrototypeUiLayout.HubRecipeAccent, amberAccent);
        EnsureUiAccentBar("UpgradeAccent", PrototypeUiLayout.HubUpgradeAccent, goldAccent);
        EnsureUiAccentBar("ActionAccent", PrototypeUiLayout.HubActionAccent, amberAccent);
        EnsureUiCaption("StorageCaption", "창고", PrototypeUiLayout.HubStorageCaption, preferredFont, forestAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("RecipeCaption", "요리 메뉴", PrototypeUiLayout.HubRecipeCaption, preferredFont, amberAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("UpgradeCaption", "업그레이드", PrototypeUiLayout.HubUpgradeCaption, preferredFont, goldAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("ActionCaption", "진행", PrototypeUiLayout.HubActionCaption, preferredFont, new Color(0.88f, 0.88f, 0.88f, 1f), TextAlignmentOptions.TopRight);
    }

    /*
     * 이름으로 찾은 패널 오브젝트에 카드형 배경과 그림자를 적용합니다.
     */
    private void EnsureUiBackdrop(
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        if (transform == null)
        {
            return;
        }

        Transform existing = transform.Find(name);
        GameObject backdropObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            backdropObject.transform.SetParent(transform, false);
        }

        RectTransform rect = backdropObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = backdropObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.SetSiblingIndex(0);

        Image image = backdropObject.GetComponent<Image>();
        if (image == null)
        {
            image = backdropObject.AddComponent<Image>();
        }

        image.color = color;
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

    private void EnsureUiBackdrop(string name, PrototypeUiRect layout, Color color)
    {
        EnsureUiBackdrop(
            name,
            layout.AnchorMin,
            layout.AnchorMax,
            layout.Pivot,
            layout.AnchoredPosition,
            layout.SizeDelta,
            color);
    }

    /*
     * 카드 상단 강조선을 추가해 HUD와 팝업 구획을 더 또렷하게 나눕니다.
     */
    private void EnsureUiAccentBar(
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        Transform existing = transform.Find(name);
        GameObject accentObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            accentObject.transform.SetParent(transform, false);
        }

        RectTransform rect = accentObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = accentObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.SetSiblingIndex(1);

        Image image = accentObject.GetComponent<Image>();
        if (image == null)
        {
            image = accentObject.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = false;
    }

    private void EnsureUiAccentBar(string name, PrototypeUiRect layout, Color color)
    {
        EnsureUiAccentBar(
            name,
            layout.AnchorMin,
            layout.AnchorMax,
            layout.Pivot,
            layout.AnchoredPosition,
            layout.SizeDelta,
            color);
    }

    /*
     * 카드 제목 캡션을 생성하거나 갱신해 각 정보 구역 이름을 표시합니다.
     */
    private void EnsureUiCaption(
        string name,
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
        Transform existing = transform.Find(name);
        GameObject captionObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            captionObject.transform.SetParent(transform, false);
        }

        RectTransform rect = captionObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = captionObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.SetSiblingIndex(2);

        TextMeshProUGUI text = captionObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = captionObject.AddComponent<TextMeshProUGUI>();
        }

        text.text = content;
        ApplyScreenTextStyle(text, font, 15f, color, alignment, false, 0f, new Vector4(0f, 0f, 0f, 0f), true);
        text.characterSpacing = 0.5f;
        text.raycastTarget = false;
    }

    private void EnsureUiCaption(
        string name,
        string content,
        PrototypeUiRect layout,
        TMP_FontAsset font,
        Color color,
        TextAlignmentOptions alignment)
    {
        EnsureUiCaption(
            name,
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

    private TextMeshProUGUI EnsureOverlayText(TextMeshProUGUI current, string name)
    {
        if (current != null)
        {
            return current;
        }

        Transform existing = transform.Find(name);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            textObject.transform.SetParent(transform, false);
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
        rect.SetSiblingIndex(Mathf.Max(0, transform.childCount - 1));

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        text.raycastTarget = false;
        return text;
    }

    /*
     * 텍스트나 버튼의 위치와 크기를 지정된 값으로 다시 맞춥니다.
     */
    private static void ApplyRectLayout(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void ApplyRectLayout(RectTransform rect, PrototypeUiRect layout)
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

    /*
     * UI 텍스트의 공통 폰트, 줄바꿈, 여백, 굵기 규칙을 한곳에서 통일합니다.
     */
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
        text.enableAutoSizing = allowWrap;
        text.fontSizeMin = allowWrap ? Mathf.Max(14f, fontSize - 5f) : fontSize;
        text.fontSizeMax = fontSize;
        text.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = allowWrap ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
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

    /*
     * 버튼 RectTransform을 조정해 허브 하단과 우측 액션 영역 배치를 맞춥니다.
     */
    private static void ApplyButtonLayout(Button button, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void ApplyButtonLayout(Button button, PrototypeUiRect layout)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        ApplyRectLayout(rect, layout);
    }

    /*
     * 버튼 배경색과 라벨 스타일을 현재 HUD 톤에 맞게 다시 입힙니다.
     */
    private void ApplyButtonPresentation(Button button, TMP_FontAsset font, Color accentColor)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = accentColor;
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
        colors.normalColor = accentColor;
        colors.highlightedColor = Color.Lerp(accentColor, Color.white, 0.14f);
        colors.pressedColor = Color.Lerp(accentColor, Color.black, 0.18f);
        colors.selectedColor = Color.Lerp(accentColor, Color.white, 0.10f);
        colors.disabledColor = new Color(accentColor.r * 0.55f, accentColor.g * 0.55f, accentColor.b * 0.55f, 0.45f);
        colors.fadeDuration = 0.08f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
    }

    /*
     * 월드 라벨은 한글 폰트, 굵기, 외곽선 기준을 통일해 장면마다 읽기 쉽게 만듭니다.
     */
    private static void ApplyWorldTextPresentation(TMP_FontAsset bodyFont, TMP_FontAsset headingFont)
    {
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
            compactY = 0.90f;
        }
        else if (parent.GetComponent<RecipeSelectorStation>() != null || parent.GetComponent<ServiceCounterStation>() != null)
        {
            compactY = 0.86f;
        }
        else if (parent.GetComponent<StorageStation>() != null)
        {
            compactY = 0.78f;
        }
        else if (parent.GetComponent<UpgradeStation>() != null)
        {
            compactY = 0.82f;
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
        Color forestAccent,
        Color amberAccent,
        Color coralAccent,
        Color goldAccent)
    {
        bool isHubScene = IsHubScene();

        ApplyNamedRectLayout("TopLeftPanel", PrototypeUiLayout.TopLeftPanel);
        ApplyNamedRectLayout("TopLeftAccent", PrototypeUiLayout.TopLeftAccent);
        ApplyNamedRectLayout("PhaseBadge", PrototypeUiLayout.PhaseBadge);
        ApplyNamedRectLayout("PromptBackdrop", PrototypeUiLayout.PromptBackdrop(isHubScene));
        ApplyNamedRectLayout("GuideBackdrop", PrototypeUiLayout.GuideBackdrop(isHubScene));
        ApplyNamedRectLayout("ResultBackdrop", PrototypeUiLayout.ResultBackdrop(isHubScene));
        if (isHubScene)
        {
            ApplyNamedRectLayout("ActionDock", PrototypeUiLayout.HubActionDock);
            ApplyNamedRectLayout("ActionAccent", PrototypeUiLayout.HubActionAccent);
            ApplyNamedRectLayout("CenterBottomPanel", PrototypeUiLayout.HubCenterBottomPanel);
        }

        SetNamedObjectActive("CenterBottomPanel", isHubScene);
        SetNamedObjectActive("PopupOverlay", false);
        SetNamedObjectActive("ActionDock", isHubScene);
        SetNamedObjectActive("ActionAccent", isHubScene);
        SetNamedObjectActive("ActionCaption", isHubScene);

        ApplyRectLayout(goldText != null ? goldText.rectTransform : null, PrototypeUiLayout.GoldText);
        ApplyRectLayout(dayPhaseText != null ? dayPhaseText.rectTransform : null, PrototypeUiLayout.DayPhaseText);
        ApplyRectLayout(interactionPromptText != null ? interactionPromptText.rectTransform : null, PrototypeUiLayout.PromptText(isHubScene));
        ApplyRectLayout(guideText != null ? guideText.rectTransform : null, PrototypeUiLayout.GuideText(isHubScene));
        ApplyRectLayout(resultText != null ? resultText.rectTransform : null, PrototypeUiLayout.ResultText(isHubScene));
        ApplyScreenTextStyle(goldText, headingFont, 20f, textColor, TextAlignmentOptions.TopLeft, false, 0f, new Vector4(6f, 2f, 6f, 2f), true);
        ApplyScreenTextStyle(dayPhaseText, headingFont, 20f, textColor, TextAlignmentOptions.Center, false, 0f, new Vector4(8f, 2f, 8f, 2f), true);
        ApplyScreenTextStyle(interactionPromptText, headingFont, 21f, textColor, TextAlignmentOptions.Center, false, 0f, new Vector4(12f, 8f, 12f, 8f), true);
        ApplyScreenTextStyle(guideText, bodyFont, 18f, textColor, TextAlignmentOptions.Center, true, 4f, new Vector4(14f, 8f, 14f, 10f), false);
        ApplyScreenTextStyle(resultText, bodyFont, 18f, textColor, TextAlignmentOptions.Center, true, 4f, new Vector4(14f, 10f, 14f, 10f), false);

        ApplyHubActionButtonsLayout(isHubScene, headingFont, oceanAccent, coralAccent, goldAccent);

        if (isHubScene)
        {
            ApplyHubPanelLayout(bodyFont, headingFont, textColor, oceanAccent, forestAccent, amberAccent, goldAccent);
        }
        else
        {
            ApplyExplorationInventoryLayout(bodyFont, headingFont, textColor, oceanAccent);
        }

        SetButtonGameObjectActive(recipePanelButton, isHubScene);
        SetButtonGameObjectActive(upgradePanelButton, isHubScene);
        SetButtonGameObjectActive(materialPanelButton, isHubScene);
    }

    /*
     * 허브 우측 진행 버튼은 같은 색상/위치 규칙을 쓰므로 한 메서드에서 정리합니다.
     */
    private void ApplyHubActionButtonsLayout(
        bool isHubScene,
        TMP_FontAsset headingFont,
        Color oceanAccent,
        Color coralAccent,
        Color goldAccent)
    {
        ApplyButtonLayout(skipExplorationButton, PrototypeUiLayout.HubSkipExplorationButton);
        ApplyButtonLayout(skipServiceButton, PrototypeUiLayout.HubSkipServiceButton);
        ApplyButtonLayout(nextDayButton, PrototypeUiLayout.HubNextDayButton);
        ApplyButtonPresentation(skipExplorationButton, headingFont, oceanAccent);
        ApplyButtonPresentation(skipServiceButton, headingFont, coralAccent);
        ApplyButtonPresentation(nextDayButton, headingFont, goldAccent);

        SetButtonGameObjectActive(skipExplorationButton, isHubScene);
        SetButtonGameObjectActive(skipServiceButton, isHubScene);
        SetButtonGameObjectActive(nextDayButton, isHubScene);
    }

    /*
     * 허브는 재료/메뉴/업그레이드/창고 팝업을 같은 카드 틀 안에서 공유합니다.
     */
    private void ApplyHubPanelLayout(
        TMP_FontAsset bodyFont,
        TMP_FontAsset headingFont,
        Color textColor,
        Color oceanAccent,
        Color forestAccent,
        Color amberAccent,
        Color goldAccent)
    {
        ApplyNamedRectLayout("InventoryCard", PrototypeUiLayout.HubInventoryCard);
        ApplyNamedRectLayout("InventoryAccent", PrototypeUiLayout.HubInventoryAccent);
        ApplyNamedRectLayout("RecipeCard", PrototypeUiLayout.HubRecipeCard);
        ApplyNamedRectLayout("RecipeAccent", PrototypeUiLayout.HubRecipeAccent);
        ApplyNamedRectLayout("UpgradeCard", PrototypeUiLayout.HubUpgradeCard);
        ApplyNamedRectLayout("UpgradeAccent", PrototypeUiLayout.HubUpgradeAccent);
        ApplyNamedRectLayout("StorageCard", PrototypeUiLayout.HubStorageCard);
        ApplyNamedRectLayout("StorageAccent", PrototypeUiLayout.HubStorageAccent);

        EnsureUiCaption("InventoryCaption", "재료", PrototypeUiLayout.HubInventoryCaption, headingFont, oceanAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("RecipeCaption", "요리 메뉴", PrototypeUiLayout.HubRecipeCaption, headingFont, amberAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("UpgradeCaption", "업그레이드", PrototypeUiLayout.HubUpgradeCaption, headingFont, goldAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("StorageCaption", "창고", PrototypeUiLayout.HubStorageCaption, headingFont, forestAccent, TextAlignmentOptions.TopLeft);

        ApplyRectLayout(inventoryText != null ? inventoryText.rectTransform : null, PrototypeUiLayout.HubInventoryText);
        ApplyRectLayout(selectedRecipeText != null ? selectedRecipeText.rectTransform : null, PrototypeUiLayout.HubRecipeText);
        ApplyRectLayout(upgradeText != null ? upgradeText.rectTransform : null, PrototypeUiLayout.HubUpgradeText);
        ApplyRectLayout(storageText != null ? storageText.rectTransform : null, PrototypeUiLayout.HubStorageText);

        ApplyScreenTextStyle(inventoryText, bodyFont, 19f, textColor, TextAlignmentOptions.TopLeft, true, 4f, new Vector4(12f, 6f, 12f, 8f), false);
        ApplyScreenTextStyle(selectedRecipeText, bodyFont, 18f, textColor, TextAlignmentOptions.TopLeft, true, 3f, new Vector4(12f, 6f, 12f, 8f), false);
        ApplyScreenTextStyle(upgradeText, bodyFont, 18f, textColor, TextAlignmentOptions.TopLeft, true, 3f, new Vector4(12f, 6f, 12f, 8f), false);
        ApplyScreenTextStyle(storageText, bodyFont, 18f, textColor, TextAlignmentOptions.TopLeft, true, 4f, new Vector4(12f, 6f, 12f, 8f), false);

        ApplyHubMenuButtonLayout(recipePanelButton, headingFont, amberAccent, PrototypeUiLayout.HubRecipePanelButton);
        ApplyHubMenuButtonLayout(upgradePanelButton, headingFont, goldAccent, PrototypeUiLayout.HubUpgradePanelButton);
        ApplyHubMenuButtonLayout(materialPanelButton, headingFont, oceanAccent, PrototypeUiLayout.HubMaterialPanelButton);
    }

    /*
     * 탐험 씬은 우측 상단 재료/가방 HUD만 유지하고 허브용 카드는 숨깁니다.
     */
    private void ApplyExplorationInventoryLayout(
        TMP_FontAsset bodyFont,
        TMP_FontAsset headingFont,
        Color textColor,
        Color oceanAccent)
    {
        ApplyNamedRectLayout("InventoryCard", PrototypeUiLayout.ExploreInventoryCard);
        ApplyNamedRectLayout("InventoryAccent", PrototypeUiLayout.ExploreInventoryAccent);
        EnsureUiCaption("InventoryCaption", "재료 / 가방", PrototypeUiLayout.ExploreInventoryCaption, headingFont, oceanAccent, TextAlignmentOptions.TopLeft);
        ApplyRectLayout(inventoryText != null ? inventoryText.rectTransform : null, PrototypeUiLayout.ExploreInventoryText);
        ApplyScreenTextStyle(inventoryText, bodyFont, 18f, textColor, TextAlignmentOptions.TopLeft, true, 3f, new Vector4(12f, 6f, 12f, 8f), false);
        SetStoragePanelUiActive(false);
    }

    /*
     * 허브 하단 버튼은 같은 형태를 쓰므로 배치와 스타일을 공통 메서드로 묶습니다.
     */
    private void ApplyHubMenuButtonLayout(Button button, TMP_FontAsset headingFont, Color accentColor, PrototypeUiRect layout)
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

    private void SetStoragePanelUiActive(bool isActive)
    {
        SetNamedObjectActive("StorageCard", isActive);
        SetNamedObjectActive("StorageAccent", isActive);
        SetNamedObjectActive("StorageCaption", isActive);

        if (storageText != null)
        {
            storageText.gameObject.SetActive(isActive);
        }
    }

    private void RefreshStoragePanelVisibility()
    {
        if (!IsHubScene())
        {
            SetStoragePanelUiActive(false);
            return;
        }

        bool isVisible = ShouldShowStoragePanel();
        SetStoragePanelUiActive(isVisible);
        RefreshHubPopupOverlay();
    }

    private void ApplyMenuPanelState()
    {
        bool isHubScene = IsHubScene();

        if (isHubScene)
        {
            bool showRecipe = activeHubPanel == HubPopupPanel.Recipe;
            bool showUpgrade = activeHubPanel == HubPopupPanel.Upgrade;
            bool showMaterials = activeHubPanel == HubPopupPanel.Materials;
            SetNamedObjectActive("RecipeCard", showRecipe);
            SetNamedObjectActive("RecipeAccent", showRecipe);
            SetNamedObjectActive("RecipeCaption", showRecipe);
            SetNamedObjectActive("UpgradeCard", showUpgrade);
            SetNamedObjectActive("UpgradeAccent", showUpgrade);
            SetNamedObjectActive("UpgradeCaption", showUpgrade);
            SetNamedObjectActive("InventoryCard", showMaterials);
            SetNamedObjectActive("InventoryAccent", showMaterials);
            SetNamedObjectActive("InventoryCaption", showMaterials);

            if (selectedRecipeText != null)
            {
                selectedRecipeText.gameObject.SetActive(showRecipe);
            }

            if (upgradeText != null)
            {
                upgradeText.gameObject.SetActive(showUpgrade);
            }

            if (inventoryText != null)
            {
                inventoryText.gameObject.SetActive(showMaterials);
            }
        }
        else
        {
            bool showInventory = true;
            SetNamedObjectActive("RecipeCard", false);
            SetNamedObjectActive("RecipeAccent", false);
            SetNamedObjectActive("RecipeCaption", false);
            SetNamedObjectActive("UpgradeCard", false);
            SetNamedObjectActive("UpgradeAccent", false);
            SetNamedObjectActive("UpgradeCaption", false);
            SetNamedObjectActive("InventoryCard", showInventory);
            SetNamedObjectActive("InventoryAccent", showInventory);
            SetNamedObjectActive("InventoryCaption", showInventory);

            if (inventoryText != null)
            {
                inventoryText.gameObject.SetActive(showInventory);
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
    }

    private void RefreshHubPopupOverlay()
    {
        bool shouldShowOverlay = IsHubScene() && (activeHubPanel != HubPopupPanel.None || ShouldShowStoragePanel());
        SetNamedObjectActive("PopupOverlay", shouldShowOverlay);
        // Pause only explicit hub popups so proximity-based storage prompts do not freeze movement.
        ApplyPopupPauseState(IsHubScene() && activeHubPanel != HubPopupPanel.None);
    }

    private bool ShouldShowStoragePanel()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<PlayerController>();
        }

        InteractionDetector detector = cachedPlayer != null ? cachedPlayer.InteractionDetector : null;
        return detector != null && detector.CurrentInteractable is StorageStation;
    }

    /*
     * 허브 팝업이 열려 있는 동안에는 시간을 멈춰 배경 진행을 막습니다.
     */
    private void ApplyPopupPauseState(bool shouldPause)
    {
        if (shouldPause)
        {
            if (isPopupPauseApplied)
            {
                return;
            }

            popupPausePreviousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPopupPauseApplied = true;
            return;
        }

        RestorePopupPauseIfNeeded();
    }

    private void RestorePopupPauseIfNeeded()
    {
        if (!isPopupPauseApplied)
        {
            return;
        }

        Time.timeScale = popupPausePreviousTimeScale;
        isPopupPauseApplied = false;
    }

    private static bool IsHubScene()
    {
        return SceneManager.GetActiveScene().name == "Hub";
    }

    private void ApplyNamedRectLayout(
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        if (transform == null)
        {
            return;
        }

        Transform target = transform.Find(name);
        RectTransform rect = target != null ? target.GetComponent<RectTransform>() : null;
        ApplyRectLayout(rect, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
    }

    private void ApplyNamedRectLayout(string name, PrototypeUiRect layout)
    {
        ApplyNamedRectLayout(
            name,
            layout.AnchorMin,
            layout.AnchorMax,
            layout.Pivot,
            layout.AnchoredPosition,
            layout.SizeDelta);
    }

    private void SetNamedObjectActive(string name, bool isActive)
    {
        if (transform == null)
        {
            return;
        }

        Transform target = transform.Find(name);
        if (target != null)
        {
            target.gameObject.SetActive(isActive);
        }
    }

    /*
     * 현재 씬의 모든 HUD 텍스트와 카드 표시 상태를 한 번에 다시 맞춥니다.
     */
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

    /*
     * 현재 플레이어가 상호작용할 수 있는 대상의 프롬프트를 하단에 표시합니다.
     */
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

        interactionPromptText.text = prompt;
        interactionPromptText.gameObject.SetActive(!string.IsNullOrWhiteSpace(prompt));
        SetNamedObjectActive("PromptBackdrop", interactionPromptText.gameObject.activeSelf);
        RefreshStoragePanelVisibility();
    }

    /*
     * 현재 보유 재료와 가방 사용량을 카드 본문용 문자열로 정리합니다.
     */
    private void RefreshInventoryText()
    {
        if (inventoryText == null)
        {
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.Inventory == null)
        {
            inventoryText.text = "인벤토리 없음";
            return;
        }

        IReadOnlyList<InventoryEntry> entries = GameManager.Instance.Inventory.RuntimeItems;
        int usedSlots = GameManager.Instance.Inventory.UsedSlotCount;
        int maxSlots = GameManager.Instance.Inventory.MaxSlotCount;

        if (entries.Count == 0)
        {
            inventoryText.text = $"인벤토리 {usedSlots}/{maxSlots}칸\n- 비어 있음";
            return;
        }

        StringBuilder builder = new();
        builder.AppendLine($"인벤토리 {usedSlots}/{maxSlots}칸");

        foreach (InventoryEntry entry in entries)
        {
            if (entry == null || entry.Resource == null)
            {
                continue;
            }

            builder.AppendLine($"- {entry.Resource.DisplayName} x{entry.Amount}");
        }

        inventoryText.text = builder.ToString().TrimEnd();
    }

    /*
     * 창고 목록과 마지막 작업 메시지를 창고 팝업 본문에 갱신합니다.
     */
    private void RefreshStorageText()
    {
        if (storageText == null)
        {
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
    }

    /*
     * 업그레이드 요약과 마지막 결과 메시지를 팝업 본문용으로 정리합니다.
     */
    private void RefreshUpgradeText()
    {
        if (upgradeText == null)
        {
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
    }

    /*
     * 좌측 상단 상태 줄에는 코인과 평판만 간단히 노출합니다.
     */
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

    /*
     * 허브 메뉴 팝업에서 보여 줄 요리 선택 요약을 갱신합니다.
     */
    private void RefreshSelectedRecipeText(RecipeData recipe)
    {
        if (selectedRecipeText == null)
        {
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
    }

    private void RefreshGuideText()
    {
        if (guideText == null)
        {
            return;
        }

        string guide = cachedDayCycle != null ? cachedDayCycle.CurrentGuideText : string.Empty;
        guideText.text = guide;
        guideText.gameObject.SetActive(!string.IsNullOrWhiteSpace(guide));
        SetNamedObjectActive("GuideBackdrop", guideText.gameObject.activeSelf);
    }

    private void RefreshRestaurantResultText(string result)
    {
        if (resultText == null)
        {
            return;
        }

        string finalText = string.Empty;

        if (IsHubScene())
        {
            if (cachedDayCycle != null && cachedDayCycle.CurrentPhase == DayPhase.Settlement)
            {
                finalText = cachedDayCycle.LastSettlementSummary;
            }
            else
            {
                finalText = result;
            }
        }

        resultText.text = finalText;
        resultText.gameObject.SetActive(!string.IsNullOrWhiteSpace(finalText));
        SetNamedObjectActive("ResultBackdrop", resultText.gameObject.activeSelf);
    }

    /*
     * 현재 날짜와 단계 문구, 단계별 버튼 노출 상태를 함께 맞춥니다.
     */
    private void RefreshDayCycleState()
    {
        if (dayPhaseText != null)
        {
            if (cachedDayCycle == null)
            {
                dayPhaseText.text = string.Empty;
            }
            else
            {
                dayPhaseText.text = $"{cachedDayCycle.CurrentDay}일차 · {DayCycleManager.GetPhaseDisplayName(cachedDayCycle.CurrentPhase)}";
            }

            SetNamedObjectActive("PhaseBadge", !string.IsNullOrWhiteSpace(dayPhaseText.text));
        }

        RefreshGuideText();
        RefreshRestaurantResultText(cachedRestaurant != null ? cachedRestaurant.LastServiceResult : string.Empty);
        RefreshButtonStates();

        if (cachedRestaurant != null)
        {
            RefreshSelectedRecipeText(cachedRestaurant.SelectedRecipe);
        }
    }

    /*
     * 현재 허브 단계에 맞춰 스킵/다음 날 버튼 표시 여부를 결정합니다.
     */
    private void RefreshButtonStates()
    {
        bool isHubScene = SceneManager.GetActiveScene().name == "Hub";
        DayPhase currentPhase = cachedDayCycle != null ? cachedDayCycle.CurrentPhase : DayPhase.MorningExplore;

        if (skipExplorationButton != null)
        {
            skipExplorationButton.gameObject.SetActive(isHubScene && currentPhase == DayPhase.MorningExplore);
        }

        if (skipServiceButton != null)
        {
            skipServiceButton.gameObject.SetActive(isHubScene && currentPhase == DayPhase.AfternoonService);
        }

        if (nextDayButton != null)
        {
            nextDayButton.gameObject.SetActive(isHubScene && currentPhase == DayPhase.Settlement);
        }
    }
}

