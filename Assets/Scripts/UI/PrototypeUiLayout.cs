using UnityEngine;

/*
 * 프로토타입 HUD에서 반복해서 쓰는 패널/버튼 RectTransform 값을 한곳에 모읍니다.
 * 런타임 UI와 씬 생성기가 같은 좌표를 공유해 저장본과 실제 배치가 어긋나지 않게 합니다.
 */
public readonly struct PrototypeUiRect
{
    public PrototypeUiRect(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        AnchorMin = anchorMin;
        AnchorMax = anchorMax;
        Pivot = pivot;
        AnchoredPosition = anchoredPosition;
        SizeDelta = sizeDelta;
    }

    public Vector2 AnchorMin { get; }
    public Vector2 AnchorMax { get; }
    public Vector2 Pivot { get; }
    public Vector2 AnchoredPosition { get; }
    public Vector2 SizeDelta { get; }
}

public static class PrototypeUiLayout
{
    public static readonly PrototypeUiRect TopLeftPanel = new(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(318f, 68f));
    public static readonly PrototypeUiRect TopLeftAccent = new(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(318f, 6f));
    public static readonly PrototypeUiRect PhaseBadge = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(236f, 40f));
    public static readonly PrototypeUiRect GoldText = new(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -28f), new Vector2(286f, 30f));
    public static readonly PrototypeUiRect DayPhaseText = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(210f, 24f));

    public static readonly PrototypeUiRect HubPromptBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(700f, 54f));
    public static readonly PrototypeUiRect ExplorePromptBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(700f, 54f));
    public static readonly PrototypeUiRect HubGuideBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 154f), new Vector2(860f, 58f));
    public static readonly PrototypeUiRect ExploreGuideBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(860f, 58f));
    public static readonly PrototypeUiRect HubResultBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 232f), new Vector2(936f, 84f));
    public static readonly PrototypeUiRect ExploreResultBackdrop = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(936f, 84f));
    public static readonly PrototypeUiRect HubPromptText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(676f, 40f));
    public static readonly PrototypeUiRect ExplorePromptText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(676f, 40f));
    public static readonly PrototypeUiRect HubGuideText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 154f), new Vector2(836f, 48f));
    public static readonly PrototypeUiRect ExploreGuideText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(836f, 48f));
    public static readonly PrototypeUiRect HubResultText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 232f), new Vector2(900f, 72f));
    public static readonly PrototypeUiRect ExploreResultText = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 72f));

    public static readonly PrototypeUiRect HubInventoryCard = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 54f), new Vector2(760f, 360f));
    public static readonly PrototypeUiRect ExploreInventoryCard = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -60f), new Vector2(404f, 188f));
    public static readonly PrototypeUiRect HubInventoryAccent = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(760f, 6f));
    public static readonly PrototypeUiRect ExploreInventoryAccent = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -60f), new Vector2(404f, 6f));
    public static readonly PrototypeUiRect HubInventoryCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 212f), new Vector2(696f, 28f));
    public static readonly PrototypeUiRect ExploreInventoryCaption = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -72f), new Vector2(180f, 22f));
    public static readonly PrototypeUiRect HubInventoryText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(684f, 264f));
    public static readonly PrototypeUiRect ExploreInventoryText = new(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -94f), new Vector2(366f, 138f));

    public static readonly PrototypeUiRect HubCenterBottomPanel = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(610f, 70f));
    public static readonly PrototypeUiRect HubActionDock = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(198f, 158f));
    public static readonly PrototypeUiRect HubActionAccent = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 170f), new Vector2(198f, 6f));
    public static readonly PrototypeUiRect HubActionCaption = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 150f), new Vector2(120f, 22f));
    public static readonly PrototypeUiRect HubPopupOverlay = new(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    public static readonly PrototypeUiRect HubStorageCard = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 54f), new Vector2(760f, 280f));
    public static readonly PrototypeUiRect HubRecipeCard = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 54f), new Vector2(760f, 360f));
    public static readonly PrototypeUiRect HubUpgradeCard = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 54f), new Vector2(760f, 360f));
    public static readonly PrototypeUiRect HubStorageAccent = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 188f), new Vector2(760f, 6f));
    public static readonly PrototypeUiRect HubRecipeAccent = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(760f, 6f));
    public static readonly PrototypeUiRect HubUpgradeAccent = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(760f, 6f));
    public static readonly PrototypeUiRect HubStorageCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 174f), new Vector2(696f, 28f));
    public static readonly PrototypeUiRect HubRecipeCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 212f), new Vector2(696f, 28f));
    public static readonly PrototypeUiRect HubUpgradeCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 212f), new Vector2(696f, 28f));
    public static readonly PrototypeUiRect HubStorageText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(684f, 176f));
    public static readonly PrototypeUiRect HubRecipeText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(684f, 264f));
    public static readonly PrototypeUiRect HubUpgradeText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(684f, 264f));

    public static readonly PrototypeUiRect HubSkipExplorationButton = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 108f), new Vector2(174f, 40f));
    public static readonly PrototypeUiRect HubSkipServiceButton = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 62f), new Vector2(174f, 40f));
    public static readonly PrototypeUiRect HubNextDayButton = new(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 16f), new Vector2(174f, 40f));
    public static readonly PrototypeUiRect HubRecipePanelButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-184f, 18f), new Vector2(164f, 44f));
    public static readonly PrototypeUiRect HubUpgradePanelButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(164f, 44f));
    public static readonly PrototypeUiRect HubMaterialPanelButton = new(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(184f, 18f), new Vector2(164f, 44f));

    public static PrototypeUiRect PromptBackdrop(bool isHubScene) => isHubScene ? HubPromptBackdrop : ExplorePromptBackdrop;
    public static PrototypeUiRect GuideBackdrop(bool isHubScene) => isHubScene ? HubGuideBackdrop : ExploreGuideBackdrop;
    public static PrototypeUiRect ResultBackdrop(bool isHubScene) => isHubScene ? HubResultBackdrop : ExploreResultBackdrop;
    public static PrototypeUiRect PromptText(bool isHubScene) => isHubScene ? HubPromptText : ExplorePromptText;
    public static PrototypeUiRect GuideText(bool isHubScene) => isHubScene ? HubGuideText : ExploreGuideText;
    public static PrototypeUiRect ResultText(bool isHubScene) => isHubScene ? HubResultText : ExploreResultText;
    public static PrototypeUiRect InventoryCard(bool isHubScene) => isHubScene ? HubInventoryCard : ExploreInventoryCard;
    public static PrototypeUiRect InventoryAccent(bool isHubScene) => isHubScene ? HubInventoryAccent : ExploreInventoryAccent;
    public static PrototypeUiRect InventoryCaption(bool isHubScene) => isHubScene ? HubInventoryCaption : ExploreInventoryCaption;
    public static PrototypeUiRect InventoryText(bool isHubScene) => isHubScene ? HubInventoryText : ExploreInventoryText;
}
