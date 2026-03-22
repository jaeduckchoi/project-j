using System;

/*
 * 일반 HUD와 버튼 스킨 매핑을 따로 모아 관리합니다.
 */
public static partial class PrototypeUISkinCatalog
{
    private static PrototypeUISpriteSpec ResolveUIDesignPanel(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) || objectName.EndsWith("Accent", StringComparison.Ordinal))
        {
            return default;
        }

        return new PrototypeUISpriteSpec("PanelBrown", PanelSliceBorder, 6, 4, false);
    }

    private static PrototypeUISpriteSpec ResolveUIDesignButton(string objectName)
    {
        return new PrototypeUISpriteSpec("ButtonBrown", ButtonSliceBorder, 4, 4, true);
    }
}
