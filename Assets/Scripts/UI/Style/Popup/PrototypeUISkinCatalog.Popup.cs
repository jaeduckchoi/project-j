using System;

/*
 * Popup 전용 프레임, 본문, 닫기 버튼 스킨 매핑을 따로 모아 관리합니다.
 */
public static partial class PrototypeUISkinCatalog
{
    private static bool TryResolvePopupPanel(string objectName, out PrototypeUISpriteSpec spriteSpec)
    {
        if (string.Equals(objectName, "PopupFrame", StringComparison.Ordinal)
            || string.Equals(objectName, "PopupLeftPanel", StringComparison.Ordinal)
            || string.Equals(objectName, "PopupRightPanel", StringComparison.Ordinal))
        {
            spriteSpec = new PrototypeUISpriteSpec("PanelGreyBolts", PanelSliceBorder, 6, 4, false);
            return true;
        }

        if (string.Equals(objectName, "PopupLeftBody", StringComparison.Ordinal)
            || string.Equals(objectName, "PopupRightBody", StringComparison.Ordinal))
        {
            spriteSpec = new PrototypeUISpriteSpec("PanelGrey", PanelSliceBorder, 6, 4, false);
            return true;
        }

        spriteSpec = default;
        return false;
    }

    private static bool TryResolvePopupButton(string objectName, out PrototypeUISpriteSpec spriteSpec)
    {
        if (!string.IsNullOrWhiteSpace(objectName)
            && objectName.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            spriteSpec = new PrototypeUISpriteSpec("ButtonGreyClose", ButtonSliceBorder, 4, 4, false);
            return true;
        }

        spriteSpec = default;
        return false;
    }
}
