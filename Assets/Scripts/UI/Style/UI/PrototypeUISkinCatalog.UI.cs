using System;

/*
 * 일반 HUD와 버튼 스킨 매핑을 따로 모아 관리합니다.
 */
namespace UI.Style
{
public static partial class PrototypeUISkinCatalog
{
    // Accent 패널은 본문 패널과 별개로 선형 강조선만 두고, 일반 패널만 스킨을 입힌다.
    private static PrototypeUISpriteSpec ResolveUIDesignPanel(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) || objectName.EndsWith("Accent", StringComparison.Ordinal))
        {
            return default;
        }

        return new PrototypeUISpriteSpec("PanelBrown", PanelSliceBorder, 6, 4, false);
    }

    // 일반 HUD 버튼은 공통 브라운 버튼 스킨 하나로 맞춘다.
    private static PrototypeUISpriteSpec ResolveUIDesignButton(string objectName)
    {
        return new PrototypeUISpriteSpec("ButtonBrown", ButtonSliceBorder, 4, 4, true);
    }
}
}
