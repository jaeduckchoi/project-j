using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace Shared
{
    /// <summary>
    /// generated TMP 폰트가 비어 있거나 삭제된 작업 트리에서도 런타임 UI/월드 텍스트가
    /// 최소한의 기본 폰트를 확보하도록 보정한다.
    /// </summary>
    public static class TmpFontAssetResolver
    {
        private static readonly string[] KoreanOsFontNames =
        {
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "NanumGothic",
            "Apple SD Gothic Neo"
        };

        private const string DefaultFontResourcePath = "Fonts & Materials/LiberationSans SDF";
        private const string FallbackFontResourcePath = "Fonts & Materials/LiberationSans SDF - Fallback";

        private static TMP_FontAsset cachedDefaultFont;
        private static TMP_FontAsset cachedLatinFont;
        private static TMP_FontAsset cachedKoreanFont;

        public static TMP_FontAsset EnsureDefaultFontAsset()
        {
            TMP_FontAsset resolved = ResolveDefaultFontAsset();
            if (resolved != null && TMP_Settings.defaultFontAsset != resolved)
            {
                TMP_Settings.defaultFontAsset = resolved;
            }

            return resolved;
        }

        public static TMP_FontAsset ResolveFontOrDefault(TMP_FontAsset preferred)
        {
            TMP_FontAsset latinFont = ResolveLatinFontAsset();
            if (preferred == null || preferred == latinFont)
            {
                return EnsureDefaultFontAsset();
            }

            return preferred;
        }

        public static TMP_FontAsset ResolveHeadingFontOrDefault(TMP_FontAsset headingFont, TMP_FontAsset bodyFont)
        {
            if (headingFont != null)
            {
                return ResolveFontOrDefault(headingFont);
            }

            if (bodyFont != null)
            {
                return ResolveFontOrDefault(bodyFont);
            }

            return EnsureDefaultFontAsset();
        }

        private static TMP_FontAsset ResolveDefaultFontAsset()
        {
            if (cachedDefaultFont != null)
            {
                return cachedDefaultFont;
            }

            cachedLatinFont = ResolveLatinFontAsset();
            cachedKoreanFont = ResolveKoreanFontAsset();

            if (cachedKoreanFont != null)
            {
                AddFallbackFont(cachedKoreanFont, cachedLatinFont);
                cachedDefaultFont = cachedKoreanFont;
                return cachedDefaultFont;
            }

            cachedDefaultFont = cachedLatinFont;
            return cachedDefaultFont;
        }

        private static TMP_FontAsset ResolveLatinFontAsset()
        {
            if (cachedLatinFont != null)
            {
                return cachedLatinFont;
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                cachedLatinFont = TMP_Settings.defaultFontAsset;
                return cachedLatinFont;
            }

            cachedLatinFont = Resources.Load<TMP_FontAsset>(DefaultFontResourcePath);
            if (cachedLatinFont != null)
            {
                return cachedLatinFont;
            }

            cachedLatinFont = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);
            return cachedLatinFont;
        }

        private static TMP_FontAsset ResolveKoreanFontAsset()
        {
            if (cachedKoreanFont != null)
            {
                return cachedKoreanFont;
            }

            Font osFont = Font.CreateDynamicFontFromOSFont(KoreanOsFontNames, 16);
            if (osFont == null)
            {
                return null;
            }

            cachedKoreanFont = TMP_FontAsset.CreateFontAsset(
                osFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (cachedKoreanFont == null)
            {
                return null;
            }

            cachedKoreanFont.name = "Runtime Korean TMP Font";
            cachedKoreanFont.hideFlags = HideFlags.HideAndDontSave;
            cachedKoreanFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            cachedKoreanFont.isMultiAtlasTexturesEnabled = true;
            return cachedKoreanFont;
        }

        private static void AddFallbackFont(TMP_FontAsset primary, TMP_FontAsset fallback)
        {
            if (primary == null || fallback == null || primary == fallback)
            {
                return;
            }

            if (primary.fallbackFontAssetTable == null)
            {
                primary.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }

            if (!primary.fallbackFontAssetTable.Contains(fallback))
            {
                primary.fallbackFontAssetTable.Add(fallback);
            }
        }
    }
}
