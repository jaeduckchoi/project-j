using System;

namespace Code.Scripts.UI.Style
{
    /// <summary>
    /// Popup 전용 프레임, 본문, 닫기 버튼 스킨 매핑을 따로 모아 관리합니다.
    /// </summary>
    public static partial class PrototypeUISkinCatalog
    {
        private static bool TryResolvePopupPanel(string objectName, out PrototypeUISpriteSpec spriteSpec)
        {
            switch (objectName)
            {
                case "RefrigeratorPopupFrame":
                    spriteSpec = BuildGeneratedUiPanelSpec("light_outline_panel");
                    return true;
                case "PopupFrame":
                    spriteSpec = BuildGeneratedUiPanelSpec("dark_thin_outline_panel");
                    return true;
                case "PopupFrameLeft":
                case "PopupLeftPanel":
                    spriteSpec = BuildGeneratedUiPanelSpec("dark_outline_panel");
                    return true;
                case "PopupFrameRight":
                case "PopupRightPanel":
                    spriteSpec = BuildGeneratedUiPanelSpec("dark_outline_panel_alt");
                    return true;
                case "PopupLeftBody":
                case "PopupRightBody":
                    spriteSpec = BuildGeneratedUiMessageBoxSpec("system_text_box");
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(objectName)
                && (objectName.StartsWith("PopupLeftItemBox", StringComparison.Ordinal)
                    || objectName.StartsWith("PopupRightItemBox", StringComparison.Ordinal)
                    || IsRefrigeratorSlotBoxName(objectName)))
            {
                spriteSpec = BuildGeneratedUiPanelSpec("light_solid_panel");
                return true;
            }

            spriteSpec = default;
            return false;
        }

        private static bool IsRefrigeratorSlotBoxName(string objectName)
        {
            return !string.IsNullOrWhiteSpace(objectName)
                   && objectName.StartsWith("RefrigeratorSlot", StringComparison.Ordinal)
                   && !objectName.StartsWith("RefrigeratorSlotIcon", StringComparison.Ordinal)
                   && !objectName.StartsWith("RefrigeratorSlotAmount", StringComparison.Ordinal);
        }

        private static bool TryResolvePopupButton(string objectName, out PrototypeUISpriteSpec spriteSpec)
        {
            if (!string.IsNullOrWhiteSpace(objectName)
                && objectName.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                spriteSpec = BuildGeneratedUiButtonSpec("close_button");
                return true;
            }

            spriteSpec = default;
            return false;
        }
    }
}
