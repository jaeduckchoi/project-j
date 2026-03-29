using System;

/*
 * Popup 전용 프레임, 본문, 닫기 버튼 스킨 매핑을 따로 모아 관리합니다.
 */
namespace UI.Style
{
public static partial class PrototypeUISkinCatalog
{
    private static bool TryResolvePopupPanel(string objectName, out PrototypeUISpriteSpec spriteSpec)
    {
        if (string.Equals(objectName, "PopupFrame", StringComparison.Ordinal))
        {
            spriteSpec = default;
            return true;
        }

        if (string.Equals(objectName, "PopupFrameLeft", StringComparison.Ordinal)
            || string.Equals(objectName, "PopupLeftPanel", StringComparison.Ordinal))
        {
            spriteSpec = new PrototypeUISpriteSpec("panel_grey_bolts", PanelSliceBorder, 6, 4, false, PopupDoubleSpriteSheetResource, "panel_grey_bolts");
            return true;
        }

        if (string.Equals(objectName, "PopupFrameRight", StringComparison.Ordinal)
            || string.Equals(objectName, "PopupRightPanel", StringComparison.Ordinal))
        {
            spriteSpec = new PrototypeUISpriteSpec("panel_grey_bolts_dark", PanelSliceBorder, 6, 4, true, PopupDoubleSpriteSheetResource, "panel_grey_bolts_dark");
            return true;
        }

        if (string.Equals(objectName, "PopupLeftBody", StringComparison.Ordinal))
        {
            spriteSpec = new PrototypeUISpriteSpec("panel_brown", PanelSliceBorder, 6, 4, false, PopupDefaultSpriteSheetResource, "panel_brown");
            return true;
        }

        if (string.Equals(objectName, "PopupRightBody", StringComparison.Ordinal))
        {
            spriteSpec = new PrototypeUISpriteSpec("panel_grey_bolts", PanelSliceBorder, 6, 4, false, PopupDefaultSpriteSheetResource, "panel_grey_bolts");
            return true;
        }

        if (!string.IsNullOrWhiteSpace(objectName)
            && (objectName.StartsWith("PopupLeftItemBox", StringComparison.Ordinal)
                || objectName.StartsWith("PopupRightItemBox", StringComparison.Ordinal)))
        {
            spriteSpec = new PrototypeUISpriteSpec("panel_brown_dark", PanelSliceBorder, 6, 4, false, PopupDefaultSpriteSheetResource, "panel_brown_dark");
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
            spriteSpec = new PrototypeUISpriteSpec("checkbox_grey_cross", ButtonSliceBorder, 4, 4, false, PopupDoubleSpriteSheetResource, "checkbox_grey_cross");
            return true;
        }

        spriteSpec = default;
        return false;
    }
}
}
