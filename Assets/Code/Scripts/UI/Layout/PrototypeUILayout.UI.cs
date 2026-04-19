using UnityEngine;

namespace Code.Scripts.UI.Layout
{
    /// <summary>
    /// 일반 HUD와 허브 기본 UI 배치를 따로 모아 관리합니다.
    /// </summary>
    public static partial class PrototypeUILayout
    {
        // 상단 상태 카드 배치다.
        public static readonly PrototypeUIRect ExploreTopLeftPanel = new(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(320f, 72f));
        public static readonly PrototypeUIRect HubResourcePanel = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -16f), new Vector2(144f, 34f));
        public static readonly PrototypeUIRect TopLeftAccent = new(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(318f, 6f));
        public static readonly PrototypeUIRect ExploreGoldText = new(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -28f), new Vector2(286f, 30f));
        public static readonly PrototypeUIRect HubResourceAmountText = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-34f, -33f), new Vector2(92f, 24f));

        // 허브와 탐험 씬 공용 프롬프트, 안내, 결과 카드 배치다.
        public static readonly PrototypeUIRect HubGuideBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 154f), new Vector2(860f, 58f));
        public static readonly PrototypeUIRect ExploreGuideBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(860f, 58f));
        public static readonly PrototypeUIRect HubResultBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 232f), new Vector2(936f, 84f));
        public static readonly PrototypeUIRect ExploreResultBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(936f, 84f));
        public static readonly PrototypeUIRect HubPromptBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(744f, 58f));
        public static readonly PrototypeUIRect ExplorePromptBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(744f, 62f));
        public static readonly PrototypeUIRect HubPromptText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(676f, 40f));
        public static readonly PrototypeUIRect ExplorePromptText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(676f, 40f));
        public static readonly PrototypeUIRect HubGuideText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(900f, 40f));
        public static readonly PrototypeUIRect ExploreGuideText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(836f, 48f));
        public static readonly PrototypeUIRect HubResultText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 186f), new Vector2(980f, 64f));
        public static readonly PrototypeUIRect ExploreResultText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 72f));
        public static readonly PrototypeUIRect HubGuideHelpButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(454f, 154f), new Vector2(34f, 34f));
        public static readonly PrototypeUIRect ExploreGuideHelpButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(454f, 84f), new Vector2(34f, 34f));

        // 인벤토리 카드와 허브 상세 카드 공용 배치다.
        public static readonly PrototypeUIRect HubInventoryCard = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 72f), new Vector2(820f, 396f));
        public static readonly PrototypeUIRect ExploreInventoryCard = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -60f), new Vector2(420f, 228f));
        public static readonly PrototypeUIRect HubInventoryAccent = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 264f), new Vector2(820f, 6f));
        public static readonly PrototypeUIRect ExploreInventoryAccent = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -60f), new Vector2(420f, 6f));
        public static readonly PrototypeUIRect HubInventoryCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 244f), new Vector2(744f, 28f));
        public static readonly PrototypeUIRect ExploreInventoryCaption = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -72f), new Vector2(180f, 22f));
        public static readonly PrototypeUIRect HubInventoryText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(736f, 292f));
        public static readonly PrototypeUIRect ExploreInventoryText = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -94f), new Vector2(382f, 174f));

        public static readonly PrototypeUIRect HubPanelButtonGroup = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(610f, 70f));
        public static readonly PrototypeUIRect HubActionDock = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(198f, 126f));
        public static readonly PrototypeUIRect HubActionAccent = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 138f), new Vector2(198f, 6f));
        public static readonly PrototypeUIRect HubActionCaption = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 114f), new Vector2(120f, 22f));
        public static readonly PrototypeUIRect HubOpenRestaurantButton = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 64f), new Vector2(170f, 32f));
        public static readonly PrototypeUIRect HubCloseRestaurantButton = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(170f, 32f));

        public static readonly PrototypeUIRect HubStorageCard = HubInventoryCard;
        public static readonly PrototypeUIRect HubRecipeCard = HubInventoryCard;
        public static readonly PrototypeUIRect HubUpgradeCard = HubInventoryCard;
        public static readonly PrototypeUIRect HubStorageAccent = HubInventoryAccent;
        public static readonly PrototypeUIRect HubRecipeAccent = HubInventoryAccent;
        public static readonly PrototypeUIRect HubUpgradeAccent = HubInventoryAccent;
        public static readonly PrototypeUIRect HubStorageCaption = HubInventoryCaption;
        public static readonly PrototypeUIRect HubRecipeCaption = HubInventoryCaption;
        public static readonly PrototypeUIRect HubUpgradeCaption = HubInventoryCaption;
        public static readonly PrototypeUIRect HubStorageText = HubInventoryText;
        public static readonly PrototypeUIRect HubRecipeText = HubInventoryText;
        public static readonly PrototypeUIRect HubUpgradeText = HubInventoryText;

        // 허브 하단 패널 버튼 배치다.
        public static readonly PrototypeUIRect HubRecipePanelButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-184f, 18f), new Vector2(164f, 44f));
        public static readonly PrototypeUIRect HubUpgradePanelButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(164f, 44f));
        public static readonly PrototypeUIRect HubMaterialPanelButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(184f, 18f), new Vector2(164f, 44f));

        /// <summary>
        /// 허브/탐험 구분에 따라 맞는 좌표 묶음을 고르는 선택자다.
        /// </summary>
        public static PrototypeUIRect GuideBackdrop(bool isHubScene) => isHubScene ? HubGuideBackdrop : ExploreGuideBackdrop;
        public static PrototypeUIRect ResultBackdrop(bool isHubScene) => isHubScene ? HubResultBackdrop : ExploreResultBackdrop;
        public static PrototypeUIRect PromptBackdrop(bool isHubScene) => isHubScene ? HubPromptBackdrop : ExplorePromptBackdrop;
        public static PrototypeUIRect PromptText(bool isHubScene) => isHubScene ? HubPromptText : ExplorePromptText;
        public static PrototypeUIRect GuideText(bool isHubScene) => isHubScene ? HubGuideText : ExploreGuideText;
        public static PrototypeUIRect GuideHelpButton(bool isHubScene) => isHubScene ? HubGuideHelpButton : ExploreGuideHelpButton;
        public static PrototypeUIRect ResultText(bool isHubScene) => isHubScene ? HubResultText : ExploreResultText;
        public static PrototypeUIRect StatusPanel(bool isHubScene) => isHubScene ? HubResourcePanel : ExploreTopLeftPanel;
        public static PrototypeUIRect EconomyText(bool isHubScene) => isHubScene ? HubResourceAmountText : ExploreGoldText;
        public static PrototypeUIRect TopLeftPanel(bool isHubScene) => StatusPanel(isHubScene);
        public static PrototypeUIRect GoldText(bool isHubScene) => EconomyText(isHubScene);
        public static PrototypeUIRect InventoryCard(bool isHubScene) => isHubScene ? HubInventoryCard : ExploreInventoryCard;
        public static PrototypeUIRect InventoryAccent(bool isHubScene) => isHubScene ? HubInventoryAccent : ExploreInventoryAccent;
        public static PrototypeUIRect InventoryCaption(bool isHubScene) => isHubScene ? HubInventoryCaption : ExploreInventoryCaption;
        public static PrototypeUIRect InventoryText(bool isHubScene) => isHubScene ? HubInventoryText : ExploreInventoryText;
    }
}
