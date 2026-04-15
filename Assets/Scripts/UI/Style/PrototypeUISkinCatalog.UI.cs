using System;
using UnityEngine;

namespace UI.Style
{
    /// <summary>
    /// 일반 HUD와 버튼 스킨 매핑을 따로 모아 관리합니다.
    /// </summary>
    public static partial class PrototypeUISkinCatalog
    {
        /// <summary>
        /// Accent 패널은 본문 패널과 별개로 선형 강조선만 두고, 일반 패널만 스킨을 입힌다.
        /// </summary>
        private static PrototypeUISpriteSpec ResolveUIDesignPanel(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName) || objectName.EndsWith("Accent", StringComparison.Ordinal))
            {
                return default;
            }

            switch (objectName)
            {
                case "TopLeftPanel":
                    return BuildGeneratedUiPanelSpec("light_outline_panel");
                case "ResourcePanel":
                    return BuildGeneratedUiPanelSpec("dark_thin_outline_panel");
                case "ActionDock":
                case "HUDPanelButtonGroup":
                    return BuildGeneratedUiPanelSpec("dark_solid_panel");
                case "GuideBackdrop":
                case "ResultBackdrop":
                    return BuildGeneratedUiMessageBoxSpec("system_text_box");
                case "InteractionPromptBackdrop":
                    return BuildGeneratedUiMessageBoxSpec("interaction_text_box", new Vector4(8f, 14f, 8f, 14f));
            }

            return new PrototypeUISpriteSpec("PanelBrown", PanelSliceBorder, 6, 4, false);
        }

        /// <summary>
        /// 일반 HUD 버튼은 공통 브라운 버튼 스킨 하나로 맞춘다.
        /// </summary>
        private static PrototypeUISpriteSpec ResolveUIDesignButton(string objectName)
        {
            if (string.Equals(objectName, "GuideHelpButton", StringComparison.Ordinal))
            {
                return BuildGeneratedUiButtonSpec("help_button");
            }

            return new PrototypeUISpriteSpec("ButtonBrown", ButtonSliceBorder, 4, 4, true);
        }
    }
}
