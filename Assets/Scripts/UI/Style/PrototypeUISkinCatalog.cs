using UnityEngine;

// UI.Style 네임스페이스
namespace UI.Style
{
    /// <summary>
    /// 어떤 SVG를 어떤 슬라이스와 여백으로 쓸지 한 번에 넘기는 스킨 정의값이다.
    /// </summary>
    public readonly struct PrototypeUISpriteSpec
    {
        /// <summary>
        /// 스프라이트 이름과 9-slice, 패딩, 틴트 여부를 구조체로 묶는다.
        /// </summary>
        public PrototypeUISpriteSpec(
            string spriteName,
            Vector4 sliceBorder,
            int padding,
            int antiAliasing,
            bool useAccentTint,
            string resourcePath = null,
            string subSpriteName = null)
        {
            SpriteName = spriteName;
            SliceBorder = sliceBorder;
            Padding = padding;
            AntiAliasing = antiAliasing;
            UseAccentTint = useAccentTint;
            ResourcePath = resourcePath;
            SubSpriteName = subSpriteName;
        }

        public string SpriteName { get; }
        public Vector4 SliceBorder { get; }
        public int Padding { get; }
        public int AntiAliasing { get; }
        public bool UseAccentTint { get; }
        public string ResourcePath { get; }
        public string SubSpriteName { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(SpriteName);
    }

    /// <summary>
    /// UI/HUD와 Popup 스킨 매핑의 공용 진입점입니다.
    /// </summary>
    public static partial class PrototypeUISkinCatalog
    {
        private static readonly Vector4 ButtonSliceBorder = new(6f, 6f, 6f, 6f);
        private static readonly Vector4 PanelSliceBorder = new(8f, 8f, 8f, 8f);
        private const string VectorResourceRoot = "Generated/UI/Vector";
        private const string PopupDefaultSpriteSheetResource = "Generated/Sprites/SpritesheetDefault";
        private const string PopupDoubleSpriteSheetResource = "Generated/Sprites/SpritesheetDouble";

        /// <summary>
        /// 패널 오브젝트 이름을 실제 리소스 경로로 바꾼다.
        /// </summary>
        public static string GetPanelResourcePath(string objectName)
        {
            return BuildResourcePath(ResolvePanel(objectName));
        }

        /// <summary>
        /// 버튼 오브젝트 이름을 실제 리소스 경로로 바꾼다.
        /// </summary>
        public static string GetButtonResourcePath(string objectName)
        {
            return BuildResourcePath(ResolveButton(objectName));
        }

        /// <summary>
        /// 팝업 전용 규칙을 먼저 확인하고, 아니면 일반 HUD 규칙으로 떨어진다.
        /// </summary>
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

        /// <summary>
        /// 강조 색을 쓰는 스킨만 씬 테마 색을 섞고, 나머지는 원본 색을 유지한다.
        /// </summary>
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

        /// <summary>
        /// Resources 폴더 기준의 벡터 리소스 경로를 만든다.
        /// </summary>
        public static string BuildVectorResourcePath(string spriteName)
        {
            return string.IsNullOrWhiteSpace(spriteName)
                ? string.Empty
                : $"{VectorResourceRoot}/{spriteName}";
        }

        public static string BuildResourcePath(PrototypeUISpriteSpec spriteSpec)
        {
            if (!string.IsNullOrWhiteSpace(spriteSpec.ResourcePath))
            {
                string subSpriteName = string.IsNullOrWhiteSpace(spriteSpec.SubSpriteName)
                    ? spriteSpec.SpriteName
                    : spriteSpec.SubSpriteName;
                return string.IsNullOrWhiteSpace(subSpriteName)
                    ? spriteSpec.ResourcePath
                    : $"{spriteSpec.ResourcePath}#{subSpriteName}";
            }

            return BuildVectorResourcePath(spriteSpec.SpriteName);
        }
    }
}