using UnityEngine;

namespace UI.Layout
{
    /// <summary>
    /// 허브 Popup 프레임과 좌우 반프레임 배치를 따로 모아 관리합니다.
    /// </summary>
    public static partial class PrototypeUILayout
    {
        // 좌우 팝업 본문에서 반복적으로 쓰는 아이템 박스 개수다.
        public const int HubPopupBodyItemBoxCount = 4;

        // Hub.unity 팝업 실측 배치값이다. 빌더와 런타임이 따로 어긋나지 않도록 이 값을 공용 기준으로 쓴다.
        public static readonly PrototypeUIRect HubPopupOverlay = new(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        public static readonly PrototypeUIRect HubPopupFrame = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800f, 800f));
        public static readonly PrototypeUIRect HubPopupTitle = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-334f, 352f), new Vector2(180f, 64f));
        public static readonly PrototypeUIRect HubPopupCloseButton = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 360f), new Vector2(48f, 48f));
        public static readonly PrototypeUIRect HubPopupFrameLeft = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460f, 0f), new Vector2(880f, 800f));
        public static readonly PrototypeUIRect HubPopupFrameRight = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 0f), new Vector2(880f, 800f));
        public static readonly PrototypeUIRect HubPopupLeftCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, 176f), new Vector2(780f, 56f));
        public static readonly PrototypeUIRect HubPopupFrameCaption = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, 176f), new Vector2(780f, 56f));
        public static readonly PrototypeUIRect HubPopupFrameBody = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(820f, 520f));
        public static readonly PrototypeUIRect HubPopupFrameText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-2f, -38f), new Vector2(780f, 344f));
        public static readonly PrototypeUIRect HubPopupRightDetailText = new(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(780f, 488f));

        /// <summary>
        /// 아이템 박스는 세로 목록으로 균등 배치해 좌우 본문 구조를 고정한다.
        /// </summary>
        public static PrototypeUIRect HubPopupBodyItemBox(int index)
        {
            const float horizontalPadding = 18f;
            const float topPadding = 16f;
            const float spacing = 10f;
            const float itemHeight = 78f;

            int clampedIndex = Mathf.Clamp(index, 0, HubPopupBodyItemBoxCount - 1);
            float topOffset = topPadding + (clampedIndex * (itemHeight + spacing));
            return new PrototypeUIRect(
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -topOffset),
                new Vector2(-(horizontalPadding * 2f), itemHeight));
        }
    }
}
