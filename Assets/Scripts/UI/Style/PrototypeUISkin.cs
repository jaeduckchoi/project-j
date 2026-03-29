using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UIVectorImage = UnityEngine.UIElements.VectorImage;

/*
 * SVG를 실제 UI Sprite로 렌더링하고 캐시하는 역할만 담당합니다.
 * 어떤 오브젝트가 어떤 SVG를 쓰는지는 PrototypeUISkinCatalog에서 관리합니다.
 */
namespace UI.Style
{
public static class PrototypeUISkin
{
    private const string LegacyResourceRoot = "Generated/UI/Kenney";
    private const float SpritePixelsPerUnit = 100f;
    private static readonly Dictionary<string, Sprite> CachedSprites = new();
    private static MethodInfo _renderVectorImageToTextureMethod;

    // 패널 오브젝트 이름으로 실제 패널 리소스 경로를 조회한다.
    public static string GetPanelResourcePath(string objectName)
    {
        return PrototypeUISkinCatalog.GetPanelResourcePath(objectName);
    }

    // 버튼 오브젝트 이름으로 실제 버튼 리소스 경로를 조회한다.
    public static string GetButtonResourcePath(string objectName)
    {
        return PrototypeUISkinCatalog.GetButtonResourcePath(objectName);
    }

    // 캐시된 런타임 스프라이트와 임시 텍스처를 정리해 프리뷰를 새로 만든다.
    public static void ClearGeneratedCache()
    {
        foreach (Sprite cachedSprite in CachedSprites.Values)
        {
            if (cachedSprite == null)
            {
                continue;
            }

            Texture2D generatedTexture = cachedSprite.texture;
            DestroyGeneratedObject(cachedSprite);

            if (generatedTexture != null
                && !string.IsNullOrWhiteSpace(generatedTexture.name)
                && generatedTexture.name.StartsWith("VectorTexture_", StringComparison.Ordinal))
            {
                DestroyGeneratedObject(generatedTexture);
            }
        }

        CachedSprites.Clear();
    }

    // 패널 이름 규칙에 맞는 스킨을 Image에 적용한다.
    public static bool ApplyPanel(Image image, string objectName, Color fallbackColor)
    {
        if (image == null)
        {
            return false;
        }

        if (string.Equals(objectName, "PopupOverlay", StringComparison.Ordinal))
        {
            ApplyFallback(image, fallbackColor);
            return false;
        }

        PrototypeUISpriteSpec spriteSpec = PrototypeUISkinCatalog.ResolvePanel(objectName);
        return ApplySprite(image, spriteSpec, fallbackColor, Color.white);
    }

    // 버튼 이름 규칙에 맞는 스킨을 Image에 적용한다.
    public static bool ApplyButton(Image image, string objectName, Color fallbackColor)
    {
        if (image == null)
        {
            return false;
        }

        if (string.Equals(objectName, "PopupCloseButton", StringComparison.Ordinal))
        {
            PrototypeUISpriteSpec closeButtonSpec = PrototypeUISkinCatalog.ResolveButton(objectName);
            bool applied = ApplySprite(image, closeButtonSpec, fallbackColor, Color.white);
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.82f);
            image.preserveAspect = false;
            return applied;
        }

        PrototypeUISpriteSpec spriteSpec = PrototypeUISkinCatalog.ResolveButton(objectName);
        return ApplySprite(image, spriteSpec, fallbackColor, fallbackColor);
    }

    // 스프라이트를 찾으면 적용하고, 찾지 못하면 기존 sprite 또는 단색 fallback 으로 처리한다.
    private static bool ApplySprite(Image image, PrototypeUISpriteSpec spriteSpec, Color fallbackColor, Color accentColor)
    {
        if (!spriteSpec.IsValid)
        {
            return PreserveExistingSprite(image, fallbackColor);
        }

        Sprite sprite = LoadSprite(spriteSpec);
        if (sprite == null)
        {
            return PreserveExistingSprite(image, fallbackColor);
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = PrototypeUISkinCatalog.ResolveAppliedColor(spriteSpec, accentColor);
        image.preserveAspect = false;
        return true;
    }

    private static bool PreserveExistingSprite(Image image, Color fallbackColor)
    {
        if (image != null && image.sprite != null)
        {
            return true;
        }

        ApplyFallback(image, fallbackColor);
        return false;
    }

    // 사용할 스프라이트가 없을 때는 단색 패널처럼 보이도록 기본값만 적용한다.
    private static void ApplyFallback(Image image, Color fallbackColor)
    {
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = fallbackColor;
        image.preserveAspect = false;
    }

    // VectorImage, legacy 텍스처 순서로 시도해 런타임 스프라이트를 준비한다.
    private static Sprite LoadSprite(PrototypeUISpriteSpec spriteSpec)
    {
        if (!spriteSpec.IsValid)
        {
            return null;
        }

        string cacheKey = BuildCacheKey(spriteSpec);
        if (CachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Sprite explicitResourceSprite = LoadExplicitResourceSprite(spriteSpec);
        if (explicitResourceSprite != null)
        {
            return explicitResourceSprite;
        }

        Sprite vectorSprite = LoadVectorResourceSprite(spriteSpec, cacheKey);
        if (vectorSprite != null)
        {
            return vectorSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>($"{LegacyResourceRoot}/{spriteSpec.SpriteName}");
        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            SpritePixelsPerUnit,
            0u,
            SpriteMeshType.FullRect,
            spriteSpec.SliceBorder);
        sprite.name = $"Legacy_{cacheKey}";
        CachedSprites[cacheKey] = sprite;
        return sprite;
    }

    private static Sprite LoadExplicitResourceSprite(PrototypeUISpriteSpec spriteSpec)
    {
        if (string.IsNullOrWhiteSpace(spriteSpec.ResourcePath))
        {
            return null;
        }

        string targetSpriteName = string.IsNullOrWhiteSpace(spriteSpec.SubSpriteName)
            ? spriteSpec.SpriteName
            : spriteSpec.SubSpriteName;

        Sprite directSprite = Resources.Load<Sprite>(spriteSpec.ResourcePath);
        if (directSprite != null
            && (string.IsNullOrWhiteSpace(targetSpriteName)
                || string.Equals(directSprite.name, targetSpriteName, StringComparison.Ordinal)))
        {
            return directSprite;
        }

        Sprite[] spriteAssets = Resources.LoadAll<Sprite>(spriteSpec.ResourcePath);
        if (spriteAssets == null || spriteAssets.Length == 0)
        {
            return directSprite;
        }

        foreach (Sprite spriteAsset in spriteAssets)
        {
            if (spriteAsset != null && string.Equals(spriteAsset.name, targetSpriteName, StringComparison.Ordinal))
            {
                return spriteAsset;
            }
        }

        return directSprite;
    }

    // Resources에 저장된 VectorImage나 Sprite를 실제 UI Sprite로 변환한다.
    private static Sprite LoadVectorResourceSprite(PrototypeUISpriteSpec spriteSpec, string cacheKey)
    {
        string resourcePath = PrototypeUISkinCatalog.BuildVectorResourcePath(spriteSpec.SpriteName);

        Sprite directSprite = Resources.Load<Sprite>(resourcePath);
        if (directSprite != null)
        {
            return directSprite;
        }

        Sprite[] spriteAssets = Resources.LoadAll<Sprite>(resourcePath);
        if (spriteAssets != null && spriteAssets.Length > 0)
        {
            return spriteAssets[0];
        }

        UIVectorImage vectorImage = Resources.Load<UIVectorImage>(resourcePath);
        if (vectorImage != null)
        {
            int width = Mathf.Max(8, Mathf.CeilToInt(vectorImage.width));
            int height = Mathf.Max(8, Mathf.CeilToInt(vectorImage.height));
            Texture2D texture = RenderVectorImageToTexture(vectorImage, width, height, spriteSpec.AntiAliasing);
            if (texture == null)
            {
                Debug.LogWarning($"UI SVG 리소스 '{resourcePath}'를 Texture2D로 렌더하지 못했습니다.");
                return null;
            }

            Texture2D paddedTexture = ApplyTransparentPadding(texture, spriteSpec.Padding);
            Vector4 effectiveBorder = ExpandBorder(spriteSpec.SliceBorder, spriteSpec.Padding);

            paddedTexture.name = $"VectorTexture_{cacheKey}";
            paddedTexture.wrapMode = TextureWrapMode.Clamp;
            paddedTexture.filterMode = FilterMode.Bilinear;

            Sprite sprite = Sprite.Create(
                paddedTexture,
                new Rect(0f, 0f, paddedTexture.width, paddedTexture.height),
                new Vector2(0.5f, 0.5f),
                SpritePixelsPerUnit,
                0u,
                SpriteMeshType.FullRect,
                effectiveBorder);
            sprite.name = $"Vector_{cacheKey}";
            CachedSprites[cacheKey] = sprite;
            return sprite;
        }

        UnityEngine.Object rawAsset = Resources.Load(resourcePath);
        if (rawAsset != null)
        {
            Debug.LogWarning($"UI SVG 리소스 '{resourcePath}'를 찾았지만 UI Sprite 또는 VectorImage로 불러오지 못했습니다. 타입: {rawAsset.GetType().FullName}");
        }

        return null;
    }

    // 9-slice 가장자리 여유를 주기 위해 투명 패딩을 둘러 렌더 결과를 확장한다.
    private static Texture2D ApplyTransparentPadding(Texture2D source, int padding)
    {
        if (source == null || padding <= 0)
        {
            return source;
        }

        int paddedWidth = source.width + (padding * 2);
        int paddedHeight = source.height + (padding * 2);
        Texture2D paddedTexture = new(paddedWidth, paddedHeight, TextureFormat.RGBA32, false);
        Color32[] clearPixels = new Color32[paddedWidth * paddedHeight];
        paddedTexture.SetPixels32(clearPixels);
        paddedTexture.SetPixels(padding, padding, source.width, source.height, source.GetPixels());
        paddedTexture.Apply();
        return paddedTexture;
    }

    // 패딩만큼 슬라이스 경계도 함께 확장해 잘리는 현상을 줄인다.
    private static Vector4 ExpandBorder(Vector4 border, int padding)
    {
        if (padding <= 0)
        {
            return border;
        }

        return new Vector4(
            border.x + padding,
            border.y + padding,
            border.z + padding,
            border.w + padding);
    }

    // Unity VectorGraphics 내부 유틸리티를 리플렉션으로 호출해 텍스처를 만든다.
    private static Texture2D RenderVectorImageToTexture(UIVectorImage vectorImage, int width, int height, int antiAliasing)
    {
        if (vectorImage == null)
        {
            return null;
        }

        MethodInfo renderMethod = GetRenderVectorImageToTextureMethod();
        if (renderMethod == null)
        {
            return null;
        }

        try
        {
            return renderMethod.Invoke(null, new object[] { vectorImage, width, height, antiAliasing }) as Texture2D;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"VectorImage 렌더 호출 중 예외가 발생했습니다: {exception.Message}");
            return null;
        }
    }

    // 버전 차이를 흡수하기 위해 VectorGraphics 렌더 메서드를 한 번만 찾아 캐시한다.
    private static MethodInfo GetRenderVectorImageToTextureMethod()
    {
        if (_renderVectorImageToTextureMethod != null)
        {
            return _renderVectorImageToTextureMethod;
        }

        Type utilityType = Type.GetType("Unity.VectorGraphics.VectorImageUtils, UnityEngine.VectorGraphicsModule");
        if (utilityType == null)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                utilityType = assembly.GetType("Unity.VectorGraphics.VectorImageUtils", false);
                if (utilityType != null)
                {
                    break;
                }
            }
        }

        if (utilityType == null)
        {
            Debug.LogWarning("Unity.VectorGraphics.VectorImageUtils 타입을 찾지 못했습니다.");
            return null;
        }

        _renderVectorImageToTextureMethod = utilityType.GetMethod(
            "RenderVectorImageToTexture2D",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(UIVectorImage), typeof(int), typeof(int), typeof(int) },
            null);

        if (_renderVectorImageToTextureMethod == null)
        {
            Debug.LogWarning("VectorImageUtils.RenderVectorImageToTexture2D 메서드를 찾지 못했습니다.");
        }

        return _renderVectorImageToTextureMethod;
    }

    // 캐시 키에는 스프라이트 이름과 슬라이스 설정을 함께 포함한다.
    private static string BuildCacheKey(PrototypeUISpriteSpec spriteSpec)
    {
        string resourcePath = string.IsNullOrWhiteSpace(spriteSpec.ResourcePath) ? "vector" : spriteSpec.ResourcePath;
        string subSpriteName = string.IsNullOrWhiteSpace(spriteSpec.SubSpriteName) ? spriteSpec.SpriteName : spriteSpec.SubSpriteName;
        Vector4 border = spriteSpec.SliceBorder;
        return $"{resourcePath}_{subSpriteName}_{border.x}_{border.y}_{border.z}_{border.w}";
    }

    // 플레이 모드 여부에 맞춰 즉시 파괴 또는 일반 파괴를 선택한다.
    private static void DestroyGeneratedObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
            return;
        }

        UnityEngine.Object.DestroyImmediate(target);
    }
}
}
