using UnityEngine;

public readonly struct PrototypeUISpriteSpec
{
    public PrototypeUISpriteSpec(string spriteName, Vector4 sliceBorder, int padding, int antiAliasing, bool useAccentTint)
    {
        SpriteName = spriteName;
        SliceBorder = sliceBorder;
        Padding = padding;
        AntiAliasing = antiAliasing;
        UseAccentTint = useAccentTint;
    }

    public string SpriteName { get; }
    public Vector4 SliceBorder { get; }
    public int Padding { get; }
    public int AntiAliasing { get; }
    public bool UseAccentTint { get; }
    public bool IsValid => !string.IsNullOrWhiteSpace(SpriteName);
}

/*
 * UI/HUD와 Popup 스킨 매핑의 공용 진입점입니다.
 */
public static partial class PrototypeUISkinCatalog
{
    private static readonly Vector4 ButtonSliceBorder = new(6f, 6f, 6f, 6f);
    private static readonly Vector4 PanelSliceBorder = new(8f, 8f, 8f, 8f);
    private const string VectorResourceRoot = "Generated/UI/Vector";

    public static string GetPanelResourcePath(string objectName)
    {
        return BuildVectorResourcePath(ResolvePanel(objectName).SpriteName);
    }

    public static string GetButtonResourcePath(string objectName)
    {
        return BuildVectorResourcePath(ResolveButton(objectName).SpriteName);
    }

    public static PrototypeUISpriteSpec ResolvePanel(string objectName)
    {
        if (TryResolvePopupPanel(objectName, out PrototypeUISpriteSpec popupPanelSpec))
        {
            return popupPanelSpec;
        }

        return ResolveUIDesignPanel(objectName);
    }

    public static PrototypeUISpriteSpec ResolveButton(string objectName)
    {
        if (TryResolvePopupButton(objectName, out PrototypeUISpriteSpec popupButtonSpec))
        {
            return popupButtonSpec;
        }

        return ResolveUIDesignButton(objectName);
    }

    public static Color ResolveAppliedColor(PrototypeUISpriteSpec spriteSpec, Color accentColor)
    {
        if (!spriteSpec.UseAccentTint)
        {
            return Color.white;
        }

        Color targetColor = accentColor.a <= 0f ? Color.white : accentColor;
        targetColor.a = 1f;
        return Color.Lerp(Color.white, targetColor, 0.62f);
    }

    public static string BuildVectorResourcePath(string spriteName)
    {
        return string.IsNullOrWhiteSpace(spriteName)
            ? string.Empty
            : $"{VectorResourceRoot}/{spriteName}";
    }
}
