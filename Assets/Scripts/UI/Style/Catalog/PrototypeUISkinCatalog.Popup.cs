using System;

// UI.Style 네임스페이스
namespace UI.Style
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
                case "PopupFrame":
                    spriteSpec = BuildGeneratedUiPanelSpec("dark-thin-outline-panel");
                    return true;
                case "PopupFrameLeft":
                case "PopupLeftPanel":
                    spriteSpec = BuildGeneratedUiPanelSpec("dark-outline-panel");
                    return true;
                case "PopupFrameRight":
                case "PopupRightPanel":
                    spriteSpec = BuildGeneratedUiPanelSpec("dark-outline-panel-alt");
                    return true;
                case "PopupLeftBody":
                case "PopupRightBody":
                    spriteSpec = BuildGeneratedUiMessageBoxSpec("system-text-box");
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(objectName)
                && (objectName.StartsWith("PopupLeftItemBox", StringComparison.Ordinal)
                    || objectName.StartsWith("PopupRightItemBox", StringComparison.Ordinal)))
            {
                spriteSpec = BuildGeneratedUiPanelSpec("light-solid-panel");
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
                spriteSpec = BuildGeneratedUiButtonSpec("close-button");
                return true;
            }

            spriteSpec = default;
            return false;
        }
    }
}
