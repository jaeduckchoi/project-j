using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Shared
{
    /// <summary>
    /// generated TMP 폰트가 비어 있거나 삭제된 작업 트리에서도 런타임 UI/월드 텍스트가
    /// 최소한의 기본 폰트를 확보하도록 보정한다.
    /// </summary>
    public static class TmpFontAssetResolver
    {
        private const string KoreanGlyphValidationSample = "메뉴를 고르고 영업을 시작하세요";
        private static readonly string[] KoreanOsFontNames =
        {
            "Malgun Gothic",
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "NanumGothic",
            "Apple SD Gothic Neo"
        };

        private const string DefaultFontResourcePath = "Fonts & Materials/LiberationSans SDF";
        private const string FallbackFontResourcePath = "Fonts & Materials/LiberationSans SDF - Fallback";

        private static TMP_FontAsset _cachedDefaultFont;
        private static TMP_FontAsset _cachedLatinFont;
        private static TMP_FontAsset _cachedKoreanFont;

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
            if (_cachedDefaultFont != null)
            {
                return _cachedDefaultFont;
            }

            _cachedLatinFont = ResolveLatinFontAsset();
            _cachedKoreanFont = ResolveKoreanFontAsset();

            if (_cachedKoreanFont != null)
            {
                AddFallbackFont(_cachedKoreanFont, _cachedLatinFont);
                _cachedDefaultFont = _cachedKoreanFont;
                return _cachedDefaultFont;
            }

            _cachedDefaultFont = _cachedLatinFont;
            return _cachedDefaultFont;
        }

        private static TMP_FontAsset ResolveLatinFontAsset()
        {
            if (_cachedLatinFont != null)
            {
                return _cachedLatinFont;
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                _cachedLatinFont = TMP_Settings.defaultFontAsset;
                return _cachedLatinFont;
            }

            _cachedLatinFont = Resources.Load<TMP_FontAsset>(DefaultFontResourcePath);
            if (_cachedLatinFont != null)
            {
                return _cachedLatinFont;
            }

            _cachedLatinFont = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);
            return _cachedLatinFont;
        }

        private static TMP_FontAsset ResolveKoreanFontAsset()
        {
            if (_cachedKoreanFont != null)
            {
                return _cachedKoreanFont;
            }

            foreach (string fontName in KoreanOsFontNames)
            {
                TMP_FontAsset runtimeFont = TryCreateKoreanFontAsset(fontName);
                if (runtimeFont == null)
                {
                    continue;
                }

                _cachedKoreanFont = runtimeFont;
                return _cachedKoreanFont;
            }

            Debug.LogWarning("Shared.TmpFontAssetResolver: 사용할 수 있는 한국어 OS 폰트를 찾지 못했습니다. 한글 표시가 필요하면 프로젝트에 TMP 폰트 에셋을 지정해야 합니다.");
            return null;
        }

        private static TMP_FontAsset TryCreateKoreanFontAsset(string fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return null;
            }

            Font osFont = Font.CreateDynamicFontFromOSFont(fontName, 16);
            if (osFont == null)
            {
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                osFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024);

            if (fontAsset == null)
            {
                return null;
            }

            if (!fontAsset.TryAddCharacters(KoreanGlyphValidationSample, out string missingCharacters)
                || !string.IsNullOrEmpty(missingCharacters))
            {
                Object.Destroy(fontAsset);
                return null;
            }

            fontAsset.name = $"Runtime Korean TMP Font ({fontName})";
            fontAsset.hideFlags = HideFlags.HideAndDontSave;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;
            return fontAsset;
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
