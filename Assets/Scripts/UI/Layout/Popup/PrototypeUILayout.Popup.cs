using UnityEngine;

/*
 * 허브 Popup 프레임과 내부 본문 배치를 따로 모아 관리합니다.
 */
public static partial class PrototypeUILayout
{
    public static readonly PrototypeUIRect HubPopupOverlay = new(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    public static readonly PrototypeUIRect HubPopupFrame = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(1260f, 640f));
    public static readonly PrototypeUIRect HubPopupTitle = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 286f), new Vector2(1140f, 40f));
    public static readonly PrototypeUIRect HubPopupCloseButton = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(548f, 280f), new Vector2(96f, 48f));
    public static readonly PrototypeUIRect HubPopupLeftPanel = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-308f, -8f), new Vector2(582f, 488f));
    public static readonly PrototypeUIRect HubPopupRightPanel = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(308f, -8f), new Vector2(582f, 488f));
    public static readonly PrototypeUIRect HubPopupLeftBody = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-308f, -54f), new Vector2(510f, 372f));
    public static readonly PrototypeUIRect HubPopupRightBody = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(308f, -54f), new Vector2(510f, 372f));
    public static readonly PrototypeUIRect HubPopupLeftCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-308f, 194f), new Vector2(470f, 30f));
    public static readonly PrototypeUIRect HubPopupRightCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(308f, 194f), new Vector2(470f, 30f));
    public static readonly PrototypeUIRect HubPopupLeftText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-308f, -54f), new Vector2(458f, 340f));
    public static readonly PrototypeUIRect HubPopupRightText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(308f, -54f), new Vector2(458f, 340f));
}
