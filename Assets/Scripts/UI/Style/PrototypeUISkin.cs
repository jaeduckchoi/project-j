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
public static class PrototypeUISkin
{
    private const string LegacyResourceRoot = "Generated/UI/Kenney";
    private const float SpritePixelsPerUnit = 100f;
    private static readonly Dictionary<string, Sprite> CachedSprites = new();
    private static MethodInfo renderVectorImageToTextureMethod;

    public static string GetPanelResourcePath(string objectName)
    {
        return PrototypeUISkinCatalog.GetPanelResourcePath(objectName);
    }

    public static string GetButtonResourcePath(string objectName)
    {
        return PrototypeUISkinCatalog.GetButtonResourcePath(objectName);
    }

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

    public static bool ApplyButton(Image image, string objectName, Color fallbackColor)
    {
        if (image == null)
        {
            return false;
        }

        PrototypeUISpriteSpec spriteSpec = PrototypeUISkinCatalog.ResolveButton(objectName);
        return ApplySprite(image, spriteSpec, fallbackColor, fallbackColor);
    }

    private static bool ApplySprite(Image image, PrototypeUISpriteSpec spriteSpec, Color fallbackColor, Color accentColor)
    {
        if (!spriteSpec.IsValid)
        {
            ApplyFallback(image, fallbackColor);
            return false;
        }

        Sprite sprite = LoadSprite(spriteSpec);
        if (sprite == null)
        {
            ApplyFallback(image, fallbackColor);
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = PrototypeUISkinCatalog.ResolveAppliedColor(spriteSpec, accentColor);
        image.preserveAspect = false;
        return true;
    }

    private static void ApplyFallback(Image image, Color fallbackColor)
    {
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = fallbackColor;
        image.preserveAspect = false;
    }

    private static Sprite LoadSprite(PrototypeUISpriteSpec spriteSpec)
    {
        if (!spriteSpec.IsValid)
        {
            return null;
        }

        string cacheKey = BuildCacheKey(spriteSpec.SpriteName, spriteSpec.SliceBorder);
        if (CachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
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

    private static MethodInfo GetRenderVectorImageToTextureMethod()
    {
        if (renderVectorImageToTextureMethod != null)
        {
            return renderVectorImageToTextureMethod;
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

        renderVectorImageToTextureMethod = utilityType.GetMethod(
            "RenderVectorImageToTexture2D",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(UIVectorImage), typeof(int), typeof(int), typeof(int) },
            null);

        if (renderVectorImageToTextureMethod == null)
        {
            Debug.LogWarning("VectorImageUtils.RenderVectorImageToTexture2D 메서드를 찾지 못했습니다.");
        }

        return renderVectorImageToTextureMethod;
    }

    private static string BuildCacheKey(string spriteName, Vector4 border)
    {
        return $"{spriteName}_{border.x}_{border.y}_{border.z}_{border.w}";
    }

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

