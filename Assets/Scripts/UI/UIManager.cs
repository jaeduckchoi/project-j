using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 인벤토리, 창고, 업그레이드, 골드, 평판, 하루 흐름, 메뉴 상태, 상호작용 문구를 갱신하는 최소 UI 관리자다.
public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private TextMeshProUGUI inventoryText;
    [SerializeField] private TextMeshProUGUI storageText;
    [SerializeField] private TextMeshProUGUI upgradeText;
    [SerializeField] private TextMeshProUGUI sceneNameText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI selectedRecipeText;
    [SerializeField] private TextMeshProUGUI restaurantResultText;
    [SerializeField] private TextMeshProUGUI dayPhaseText;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private Button skipExplorationButton;
    [SerializeField] private Button skipServiceButton;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private string defaultPromptText = "이동: WASD / 방향키   상호작용: E";

    private PlayerController cachedPlayer;
    private InventoryManager cachedInventory;
    private StorageManager cachedStorage;
    private EconomyManager cachedEconomy;
    private ToolManager cachedToolManager;
    private RestaurantManager cachedRestaurant;
    private DayCycleManager cachedDayCycle;
    private UpgradeManager cachedUpgradeManager;

    /*
     * 씬 전환 시 UI 바인딩을 다시 잡기 위해 sceneLoaded 콜백을 등록합니다.
     */
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    /*
     * 시작 시 현재 씬 기준 참조를 잡고 전체 UI 를 한 번 갱신합니다.
     */
    private void Start()
    {
        BindSceneReferences();
        BindButtons();
        ApplyTextPresentation();
        RefreshAll();
    }

    /*
     * 프레임마다 상호작용 프롬프트와 버튼 표시 상태를 갱신합니다.
     */
    private void Update()
    {
        RefreshInteractionPrompt();
        RefreshButtonStates();
    }

    /*
     * 구독 중인 이벤트와 버튼 리스너를 모두 정리합니다.
     */
    private void OnDisable()
    {
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
     * 씬 전환 직후 새 씬의 플레이어와 매니저 참조를 다시 바인딩합니다.
     */
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneReferences();
        BindButtons();
        ApplyTextPresentation();
        RefreshAll();
    }

    /*
     * 현재 씬의 플레이어와 전역 매니저 참조를 한 번에 다시 연결합니다.
     */
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

    /*
     * 스킵 / 다음 날 버튼에 클릭 리스너를 연결합니다.
     */
    private void BindButtons()
    {
        UnbindButtons();

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
    }

    /*
     * 버튼 리스너를 제거해 중복 구독을 막습니다.
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
    }

    /*
     * 오전 탐험 스킵 버튼 클릭을 하루 흐름 매니저로 전달합니다.
     */
    private void HandleSkipExplorationClicked()
    {
        cachedDayCycle?.SkipExploration();
        RefreshAll();
    }

    /*
     * 오후 장사 스킵 버튼 클릭을 장사 또는 하루 흐름 매니저로 전달합니다.
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
     * 다음 날 버튼 클릭을 하루 흐름 매니저로 전달합니다.
     */
    private void HandleNextDayClicked()
    {
        cachedDayCycle?.AdvanceToNextDay();
        RefreshAll();
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
     * 해상도 차이로 UI 블록이 뭉개지지 않도록 캔버스 스케일 기준을 고정합니다.
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
     * 에디터 플레이 기준으로 UI 텍스트와 배경 패널을 다시 정리해 가독성을 높입니다.
     */
    private void ApplyTextPresentation()
    {
        TMP_FontAsset preferredFont = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : interactionPromptText != null
                ? interactionPromptText.font
                : null;

        ApplyCanvasScaleSettings();

        Color parchment = new(0.97f, 0.94f, 0.89f, 0.86f);
        Color paper = new(0.98f, 0.98f, 0.99f, 0.84f);
        Color glass = new(0.97f, 0.98f, 0.99f, 0.80f);
        Color ink = new(0.11f, 0.13f, 0.18f, 1f);
        Color oceanAccent = new(0.18f, 0.50f, 0.58f, 0.95f);
        Color forestAccent = new(0.33f, 0.49f, 0.27f, 0.95f);
        Color amberAccent = new(0.77f, 0.49f, 0.16f, 0.95f);
        Color coralAccent = new(0.69f, 0.37f, 0.28f, 0.95f);
        Color goldAccent = new(0.68f, 0.57f, 0.17f, 0.95f);
        Color nightDock = new(0.10f, 0.15f, 0.22f, 0.90f);

        EnsureUiBackdrop(
            "TopLeftPanel",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(18f, -18f),
            new Vector2(336f, 98f),
            parchment);
        EnsureUiBackdrop(
            "BottomLeftPanel",
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(18f, 18f),
            new Vector2(372f, 364f),
            new Color(0.96f, 0.98f, 0.98f, 0.08f));
        EnsureUiBackdrop(
            "CenterBottomPanel",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 18f),
            new Vector2(620f, 58f),
            new Color(0.07f, 0.11f, 0.17f, 0.78f));
        EnsureUiBackdrop(
            "TopRightPanel",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-18f, -18f),
            new Vector2(494f, 364f),
            new Color(0.97f, 0.98f, 0.99f, 0.08f));
        EnsureUiBackdrop(
            "TopCenterPanel",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -14f),
            new Vector2(782f, 92f),
            glass);
        EnsureUiBackdrop(
            "InventoryCard",
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(18f, 206f),
            new Vector2(372f, 176f),
            paper);
        EnsureUiBackdrop(
            "StorageCard",
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(18f, 18f),
            new Vector2(372f, 176f),
            paper);
        EnsureUiBackdrop(
            "RecipeCard",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-18f, -18f),
            new Vector2(494f, 170f),
            paper);
        EnsureUiBackdrop(
            "ResultCard",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-18f, -192f),
            new Vector2(494f, 128f),
            paper);
        EnsureUiBackdrop(
            "UpgradeCard",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-18f, -324f),
            new Vector2(494f, 118f),
            paper);
        EnsureUiBackdrop(
            "ActionDock",
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-18f, 18f),
            new Vector2(186f, 154f),
            nightDock);

        EnsureUiAccentBar("TopLeftAccent", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(336f, 8f), amberAccent);
        EnsureUiAccentBar("TopCenterAccent", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(782f, 8f), amberAccent);
        EnsureUiAccentBar("InventoryAccent", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 374f), new Vector2(372f, 8f), oceanAccent);
        EnsureUiAccentBar("StorageAccent", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 186f), new Vector2(372f, 8f), forestAccent);
        EnsureUiAccentBar("RecipeAccent", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(494f, 8f), amberAccent);
        EnsureUiAccentBar("ResultAccent", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -192f), new Vector2(494f, 8f), coralAccent);
        EnsureUiAccentBar("UpgradeAccent", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -324f), new Vector2(494f, 8f), goldAccent);
        EnsureUiAccentBar("ActionAccent", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 164f), new Vector2(186f, 8f), amberAccent);

        EnsureUiCaption("StatusCaption", "상태", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(120f, 22f), preferredFont, amberAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("FlowCaption", "오늘의 흐름", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(180f, 22f), preferredFont, amberAccent, TextAlignmentOptions.Top);
        EnsureUiCaption("InventoryCaption", "가방", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 350f), new Vector2(120f, 22f), preferredFont, oceanAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("StorageCaption", "창고", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 162f), new Vector2(120f, 22f), preferredFont, forestAccent, TextAlignmentOptions.TopLeft);
        EnsureUiCaption("RecipeCaption", "오늘의 메뉴", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -22f), new Vector2(160f, 22f), preferredFont, amberAccent, TextAlignmentOptions.TopRight);
        EnsureUiCaption("ResultCaption", "영업 결과", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -196f), new Vector2(160f, 22f), preferredFont, coralAccent, TextAlignmentOptions.TopRight);
        EnsureUiCaption("UpgradeCaption", "업그레이드", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -328f), new Vector2(160f, 22f), preferredFont, goldAccent, TextAlignmentOptions.TopRight);
        EnsureUiCaption("ActionCaption", "빠른 행동", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 150f), new Vector2(120f, 22f), preferredFont, new Color(1f, 0.93f, 0.78f, 1f), TextAlignmentOptions.TopRight);

        ApplyRectLayout(sceneNameText != null ? sceneNameText.rectTransform : null, new Vector2(28f, -42f), new Vector2(286f, 34f));
        ApplyRectLayout(goldText != null ? goldText.rectTransform : null, new Vector2(28f, -72f), new Vector2(312f, 30f));
        ApplyRectLayout(inventoryText != null ? inventoryText.rectTransform : null, new Vector2(28f, 212f), new Vector2(342f, 132f));
        ApplyRectLayout(storageText != null ? storageText.rectTransform : null, new Vector2(28f, 24f), new Vector2(342f, 132f));
        ApplyRectLayout(interactionPromptText != null ? interactionPromptText.rectTransform : null, new Vector2(0f, 22f), new Vector2(580f, 44f));
        ApplyRectLayout(selectedRecipeText != null ? selectedRecipeText.rectTransform : null, new Vector2(-28f, -40f), new Vector2(452f, 114f));
        ApplyRectLayout(restaurantResultText != null ? restaurantResultText.rectTransform : null, new Vector2(-28f, -212f), new Vector2(452f, 74f));
        ApplyRectLayout(upgradeText != null ? upgradeText.rectTransform : null, new Vector2(-28f, -344f), new Vector2(452f, 64f));
        ApplyRectLayout(dayPhaseText != null ? dayPhaseText.rectTransform : null, new Vector2(0f, -32f), new Vector2(540f, 30f));
        ApplyRectLayout(guideText != null ? guideText.rectTransform : null, new Vector2(0f, -58f), new Vector2(736f, 42f));

        ApplyScreenTextStyle(sceneNameText, preferredFont, 30f, ink, TextAlignmentOptions.TopLeft, false, 0f, new Vector4(8f, 4f, 8f, 2f), true);
        ApplyScreenTextStyle(goldText, preferredFont, 22f, new Color(0.22f, 0.24f, 0.29f), TextAlignmentOptions.TopLeft, false, 0f, new Vector4(8f, 2f, 8f, 4f), false);
        ApplyScreenTextStyle(inventoryText, preferredFont, 21f, ink, TextAlignmentOptions.TopLeft, true, 6f, new Vector4(12f, 6f, 12f, 8f), false);
        ApplyScreenTextStyle(storageText, preferredFont, 20f, ink, TextAlignmentOptions.TopLeft, true, 6f, new Vector4(12f, 6f, 12f, 8f), false);
        ApplyScreenTextStyle(interactionPromptText, preferredFont, 24f, Color.white, TextAlignmentOptions.Center, false, 0f, new Vector4(12f, 8f, 12f, 8f), true);
        ApplyScreenTextStyle(selectedRecipeText, preferredFont, 21f, ink, TextAlignmentOptions.TopRight, true, 5f, new Vector4(12f, 8f, 12f, 8f), false);
        ApplyScreenTextStyle(restaurantResultText, preferredFont, 19f, ink, TextAlignmentOptions.TopRight, true, 5f, new Vector4(12f, 8f, 12f, 8f), false);
        ApplyScreenTextStyle(upgradeText, preferredFont, 18f, ink, TextAlignmentOptions.TopRight, true, 4f, new Vector4(12f, 8f, 12f, 8f), false);
        ApplyScreenTextStyle(dayPhaseText, preferredFont, 24f, ink, TextAlignmentOptions.Top, false, 0f, new Vector4(8f, 2f, 8f, 2f), true);
        ApplyScreenTextStyle(guideText, preferredFont, 18f, new Color(0.23f, 0.26f, 0.31f), TextAlignmentOptions.Top, true, 3f, new Vector4(12f, 2f, 12f, 8f), false);

        ApplyButtonLayout(skipExplorationButton, new Vector2(-28f, 102f), new Vector2(154f, 38f));
        ApplyButtonLayout(skipServiceButton, new Vector2(-28f, 58f), new Vector2(154f, 38f));
        ApplyButtonLayout(nextDayButton, new Vector2(-28f, 14f), new Vector2(154f, 38f));
        ApplyButtonPresentation(skipExplorationButton, preferredFont, oceanAccent);
        ApplyButtonPresentation(skipServiceButton, preferredFont, coralAccent);
        ApplyButtonPresentation(nextDayButton, preferredFont, goldAccent);
        ApplyWorldTextPresentation(preferredFont);
    }

    /*
     * 화면 고정 패널을 보강하거나 다시 배치해 텍스트 배경 대비를 확보합니다.
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

    /*
     * 카드 상단에 가는 강조선을 추가해 섹션 위계를 분명하게 만듭니다.
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

    /*
     * 각 카드 위에 짧은 캡션을 추가해 정보 구역 이름을 분명하게 보여줍니다.
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
        text.characterSpacing = 2f;
        text.raycastTarget = false;
    }

    /*
     * UI 텍스트 사각형의 위치와 크기를 읽기 쉬운 값으로 다시 맞춥니다.
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

    /*
     * UI 텍스트 공통 스타일을 적용하고 한글 폰트와 줄바꿈 규칙을 통일합니다.
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
        text.enableAutoSizing = false;
        text.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

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
     * 하단 액션 버튼과 라벨을 현재 UI 스타일에 맞게 다시 맞춥니다.
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

    /*
     * 하단 액션 버튼과 라벨을 현재 UI 스타일에 맞게 다시 맞춥니다.
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
    }

    /*
     * 월드 라벨도 같은 폰트와 외곽선 기준으로 정리해 바닥색에 덜 묻히게 만듭니다.
     */
    private static void ApplyWorldTextPresentation(TMP_FontAsset font)
    {
        TextMeshPro[] worldTexts = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
        foreach (TextMeshPro worldText in worldTexts)
        {
            if (worldText == null)
            {
                continue;
            }

            if (font != null)
            {
                worldText.font = font;
            }

            worldText.enableAutoSizing = false;
            worldText.characterSpacing = 1.5f;
            worldText.fontStyle = FontStyles.Bold;

            float luminance = (worldText.color.r * 0.299f) + (worldText.color.g * 0.587f) + (worldText.color.b * 0.114f);
            worldText.outlineWidth = 0.18f;
            worldText.outlineColor = luminance < 0.45f
                ? new Color(1f, 1f, 1f, 0.90f)
                : new Color(0f, 0f, 0f, 0.88f);
        }
    }

    /*
     * 화면에 보이는 모든 주요 UI 블록을 한 번에 다시 그립니다.
     */
    private void RefreshAll()
    {
        // 개별 이벤트가 누락돼도 전체 갱신 한 번으로 화면 상태를 맞출 수 있게 둡니다.
        RefreshInventoryText();
        RefreshStorageText();
        RefreshUpgradeText();
        RefreshEconomyText();
        RefreshInteractionPrompt();
        RefreshSelectedRecipeText(cachedRestaurant != null ? cachedRestaurant.SelectedRecipe : null);
        RefreshRestaurantResultText(cachedRestaurant != null ? cachedRestaurant.LastServiceResult : string.Empty);
        RefreshDayCycleState();

        if (sceneNameText != null)
        {
            sceneNameText.text = GetSceneDisplayName(SceneManager.GetActiveScene().name);
        }
    }

    /*
     * 씬 내부 이름을 플레이어에게 보여줄 지역 이름으로 변환합니다.
     */
    private static string GetSceneDisplayName(string sceneName)
    {
        return sceneName switch
        {
            "Hub" => "종구의 식당",
            "Beach" => "바닷가",
            "DeepForest" => "깊은 숲",
            "AbandonedMine" => "폐광산",
            "WindHill" => "바람 언덕",
            _ => sceneName
        };
    }

    /*
     * 현재 가까운 상호작용 대상의 프롬프트를 화면에 표시합니다.
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
    }

    /*
     * 인벤토리 슬롯 수와 보유 자원 목록을 UI 문자열로 갱신합니다.
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
     * 창고 목록과 마지막 작업 메시지를 UI 에 갱신합니다.
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
     * 업그레이드 요약과 마지막 메시지를 UI 에 갱신합니다.
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
     * 골드와 평판 표시 문자열을 갱신합니다.
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

        goldText.text = $"골드: {gold}   평판: {reputation}";
    }

    /*
     * 메뉴 선택 목록과 현재 선택 메뉴 상세를 갱신합니다.
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

    /*
     * 장사 결과나 정산 결과 문자열을 결과 패널에 갱신합니다.
     */
    private void RefreshRestaurantResultText(string result)
    {
        if (restaurantResultText == null)
        {
            return;
        }

        string finalText = result;

        if (cachedDayCycle != null && cachedDayCycle.CurrentPhase == DayPhase.Settlement)
        {
            finalText = cachedDayCycle.LastSettlementSummary;
        }

        restaurantResultText.text = string.IsNullOrWhiteSpace(finalText)
            ? "영업 결과 없음"
            : finalText;
    }

    /*
     * 날짜, 단계, 안내 문구, 결과 패널을 하루 상태에 맞춰 갱신합니다.
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
        }

        if (guideText != null)
        {
            guideText.text = cachedDayCycle != null ? cachedDayCycle.CurrentGuideText : string.Empty;
        }

        RefreshButtonStates();

        if (cachedRestaurant != null)
        {
            RefreshSelectedRecipeText(cachedRestaurant.SelectedRecipe);
            RefreshRestaurantResultText(cachedRestaurant.LastServiceResult);
        }
    }

    /*
     * 현재 씬과 단계에 따라 스킵 / 다음 날 버튼 표시 여부를 결정합니다.
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
