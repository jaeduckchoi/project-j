using System;
using System.Collections.Generic;
using Shared.Data;
using Management.Economy;
using CoreLoop.Flow;
using Management.Inventory;
using Exploration.Player;
using Restaurant;
using Management.Storage;
using TMPro;
using Management.Tools;
using UI.Controllers;
using UI.Layout;
using Management.Upgrade;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// UI 네임스페이스
namespace UI
{
    /// <summary>
    /// 현재 HUD, 허브 팝업, 창고 패널을 한곳에서 갱신하는 최소 UI 관리자입니다.
    /// 코인/평판, 재료, 업그레이드, 상호작용 문구, 단계 표시를 모두 여기서 동기화합니다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "UIManager")]
    public partial class UIManager : MonoBehaviour
    {
        private const string HudRootName = "HUDRoot";
        private const string PopupRootName = "PopupRoot";
        private const string HudStatusGroupName = "HUDStatusGroup";
        private const string TopLeftPanelInnerObjectName = "TopLeftPanelInner";
        private const string TopLeftPanelCoinIconObjectName = "TopLeftPanelCoinIcon";
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
        private static readonly Color HubCoinBadgeOuterColor = new(0.93f, 0.71f, 0.18f, 1f);
        private static readonly Color HubCoinBadgeInnerColor = new(0.24f, 0.15f, 0.05f, 1f);
        private static readonly Color HubCoinTextColor = new(1f, 0.93f, 0.63f, 1f);
        private static readonly Color HubCoinIconColor = new(1f, 0.79f, 0.24f, 1f);
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
    }
}
