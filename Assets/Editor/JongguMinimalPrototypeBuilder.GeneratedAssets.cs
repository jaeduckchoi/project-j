#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exploration.Player;
using Exploration.World;
using Shared.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;
#if ENABLE_INPUT_SYSTEM
#endif

// ProjectEditor 네임스페이스
namespace Editor
{
    public static partial class JongguMinimalPrototypeBuilder
    {
        private static ResourceLibrary CreateResources()
        {
            return new ResourceLibrary
            {
                Fish = CreateResourceAsset(ResourceDataRoot + "/resource-fish.asset", "fish", "생선", "바닷가에서 쉽게 얻을 수 있는 기본 재료입니다.", "바닷가", 10, ResourceRarity.Common),
                Shell = CreateResourceAsset(ResourceDataRoot + "/resource-shell.asset", "shell", "조개", "국물 요리에 쓰기 좋은 바닷가 재료입니다.", "바닷가", 12, ResourceRarity.Common),
                Seaweed = CreateResourceAsset(ResourceDataRoot + "/resource-seaweed.asset", "seaweed", "해초", "향이 좋은 해산 재료입니다.", "바닷가", 8, ResourceRarity.Common),
                Herb = CreateResourceAsset(ResourceDataRoot + "/resource-herb.asset", "herb", "약초", "깊은 숲에서 얻는 향이 짙은 약초입니다.", "깊은 숲", 14, ResourceRarity.Uncommon),
                Mushroom = CreateResourceAsset(ResourceDataRoot + "/resource-mushroom.asset", "mushroom", "버섯", "숲의 그늘 아래에서 자라는 식재료입니다.", "깊은 숲", 16, ResourceRarity.Uncommon),
                GlowMoss = CreateResourceAsset(ResourceDataRoot + "/resource-glow-moss.asset", "glow_moss", "발광 이끼", "폐광산 안쪽의 습한 벽면에서 자라는 희귀 식재료입니다.", "폐광산", 22, ResourceRarity.Rare),
                WindHerb = CreateResourceAsset(ResourceDataRoot + "/resource-wind-herb.asset", "wind_herb", "향초", "바람이 센 언덕에서만 자라는 고급 허브입니다.", "바람 언덕", 18, ResourceRarity.Rare)
            };
        }

        private static RecipeLibrary CreateRecipes(ResourceLibrary resources)
        {
            return new RecipeLibrary
            {
                SushiSet = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-sushi-set.asset",
                    "sushi_set",
                    "생선 한 접시",
                    "생선으로 빠르게 준비할 수 있는 기본 메뉴입니다.",
                    30,
                    1,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1)
                    }),
                SeafoodSoup = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-seafood-soup.asset",
                    "seafood_soup",
                    "해물탕",
                    "생선, 조개, 해초를 모두 넣은 고가 메뉴입니다.",
                    55,
                    2,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1),
                        new RecipeIngredientDefinition(resources.Shell, 1),
                        new RecipeIngredientDefinition(resources.Seaweed, 1)
                    }),
                HerbFishSoup = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-herb-fish-soup.asset",
                    "herb_fish_soup",
                    "약초 생선탕",
                    "바닷가 생선과 숲 약초를 넣어 향을 살린 메뉴입니다.",
                    42,
                    2,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1),
                        new RecipeIngredientDefinition(resources.Herb, 1)
                    }),
                ForestBasket = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-forest-basket.asset",
                    "forest_basket",
                    "숲 버섯 모둠",
                    "약초와 버섯을 엮어 만든 가벼운 숲 메뉴입니다.",
                    38,
                    1,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Herb, 1),
                        new RecipeIngredientDefinition(resources.Mushroom, 1)
                    }),
                GlowMossStew = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-glow-moss-stew.asset",
                    "glow_moss_stew",
                    "광채 해물탕",
                    "발광 이끼와 해초를 함께 넣어 진한 향을 낸 후반 메뉴입니다.",
                    68,
                    3,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1),
                        new RecipeIngredientDefinition(resources.Seaweed, 1),
                        new RecipeIngredientDefinition(resources.GlowMoss, 1)
                    }),
                WindHerbSalad = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-wind-herb-salad.asset",
                    "wind_herb_salad",
                    "향초 해초 무침",
                    "바람 언덕 향초와 해초를 함께 버무린 고급 메뉴입니다.",
                    46,
                    2,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Seaweed, 1),
                        new RecipeIngredientDefinition(resources.WindHerb, 1)
                    })
            };
        }

        private static SpriteLibrary CreateSprites()
        {
            CreateDefaultRecipeSprites();

            return new SpriteLibrary
            {
                PlayerFront = CreatePlayerSprite(PlayerSpriteRoot + "/player-front.png", "image (2).png"),
                PlayerBack = CreatePlayerSprite(PlayerSpriteRoot + "/player-back.png", "image (1).png"),
                PlayerSide = CreatePlayerSprite(PlayerSpriteRoot + "/player-side.png", "image.png"),
                HubFloorTile = EnsureCopiedSpriteAsset(
                    HubFloorTileDesignSourcePath,
                    HubFloorTileSpritePath,
                    HubRoomLayout.FloorTilePixelsPerUnit,
                    new Vector2(0.5f, 0.5f),
                    FilterMode.Point,
                    TextureWrapMode.Repeat),
                HubFloorBackground = LoadConfiguredSprite(HubFloorBackgroundSpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubWallBackground = EnsureHubWallBackgroundSpriteAsset(),
                HubFrontOutline = EnsureHubFrontOutlineSpriteAsset(),
                HubBar = EnsureCopiedSpriteAsset(
                    HubBarDesignSourcePath,
                    HubBarSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f),
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    HubBarMainSpriteBorder),
                HubBarRight = EnsureCompositeSpriteAsset(
                    HubBarDesignSourcePath,
                    HubBarRightSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f),
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    HubBarRightSpriteBorder,
                    HubBarRightLeftCapCropRect,
                    HubBarRightBodyCropRect),
                HubTableUnlocked = LoadConfiguredSprite(HubTableUnlockedSpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubTodayMenuBg = LoadConfiguredSprite(HubTodayMenuBgSpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubTodayMenuItem1 = LoadConfiguredSprite(HubTodayMenuItem1SpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubTodayMenuItem2 = LoadConfiguredSprite(HubTodayMenuItem2SpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubTodayMenuItem3 = LoadConfiguredSprite(HubTodayMenuItem3SpritePath, 100f, new Vector2(0.5f, 0.5f)),
                Portal = CreateColorSprite(WorldSpriteRoot + "/world-portal.png", new Color(0.95f, 0.52f, 0.22f)),
                Selector = CreateColorSprite(WorldSpriteRoot + "/world-selector.png", new Color(0.98f, 0.84f, 0.23f)),
                Counter = CreateColorSprite(WorldSpriteRoot + "/world-counter.png", new Color(0.84f, 0.34f, 0.24f)),
                Fish = CreateColorSprite(GatherSpriteRoot + "/gather-fish.png", new Color(0.19f, 0.73f, 0.92f)),
                Shell = CreateColorSprite(GatherSpriteRoot + "/gather-shell.png", new Color(0.90f, 0.79f, 0.66f)),
                Seaweed = CreateColorSprite(GatherSpriteRoot + "/gather-seaweed.png", new Color(0.24f, 0.66f, 0.35f)),
                Herb = CreateColorSprite(GatherSpriteRoot + "/gather-herb.png", new Color(0.47f, 0.78f, 0.27f)),
                Mushroom = CreateColorSprite(GatherSpriteRoot + "/gather-mushroom.png", new Color(0.71f, 0.55f, 0.36f)),
                GlowMoss = CreateColorSprite(GatherSpriteRoot + "/gather-glow-moss.png", new Color(0.45f, 0.95f, 0.78f)),
                WindHerb = CreateColorSprite(GatherSpriteRoot + "/gather-wind-herb.png", new Color(0.79f, 0.93f, 0.61f)),
                Floor = CreateColorSprite(WorldSpriteRoot + "/world-floor.png", Color.white)
            };
        }

        private static void CreateDefaultRecipeSprites()
        {
            CreateColorSprite(RecipeSpriteRoot + "/food_001.png", new Color(0.91f, 0.42f, 0.26f));
            CreateColorSprite(RecipeSpriteRoot + "/food_041.png", new Color(0.98f, 0.70f, 0.30f));
            CreateColorSprite(RecipeSpriteRoot + "/food_069.png", new Color(0.38f, 0.72f, 0.56f));
        }

        private static Sprite EnsureCopiedSpriteAsset(
            string sourceAssetPath,
            string assetPath,
            float pixelsPerUnit,
            Vector2 pivot,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            Vector4? borderOverride = null)
        {
            string sourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), sourceAssetPath);
            string assetFullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            if (File.Exists(sourceFullPath))
            {
                CopyFileIfDifferent(sourceFullPath, assetFullPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            Vector4 border = borderOverride ?? Vector4.zero;

            return File.Exists(assetFullPath)
                ? ConfigureSpriteAsset(assetPath, pixelsPerUnit, pivot, border, filterMode, wrapMode)
                : AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite EnsureCompositeSpriteAsset(
            string sourceAssetPath,
            string assetPath,
            float pixelsPerUnit,
            Vector2 pivot,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            Vector4? borderOverride = null,
            params RectInt[] segments)
        {
            string sourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), sourceAssetPath);
            string assetFullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            if (File.Exists(sourceFullPath))
            {
                byte[] compositeBytes = ComposeTextureSegmentsToPngBytes(sourceFullPath, segments);
                if (compositeBytes != null && compositeBytes.Length > 0)
                {
                    WriteFileIfDifferent(assetFullPath, compositeBytes);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }

            Vector4 border = borderOverride ?? Vector4.zero;

            return File.Exists(assetFullPath)
                ? ConfigureSpriteAsset(assetPath, pixelsPerUnit, pivot, border, filterMode, wrapMode)
                : AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static byte[] ComposeTextureSegmentsToPngBytes(string sourceFullPath, IReadOnlyList<RectInt> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                return null;
            }

            Texture2D sourceTexture = new(2, 2, TextureFormat.RGBA32, false);
            if (!sourceTexture.LoadImage(File.ReadAllBytes(sourceFullPath)))
            {
                Object.DestroyImmediate(sourceTexture);
                return null;
            }

            List<RectInt> normalizedSegments = new(segments.Count);
            int totalWidth = 0;
            int maxHeight = 0;

            foreach (RectInt segment in segments)
            {
                int startX = Mathf.Clamp(segment.x, 0, sourceTexture.width - 1);
                int startY = Mathf.Clamp(segment.y, 0, sourceTexture.height - 1);
                int width = Mathf.Clamp(segment.width, 1, sourceTexture.width - startX);
                int height = Mathf.Clamp(segment.height, 1, sourceTexture.height - startY);
                RectInt normalized = new(startX, startY, width, height);
                normalizedSegments.Add(normalized);
                totalWidth += width;
                maxHeight = Mathf.Max(maxHeight, height);
            }

            try
            {
                Texture2D composedTexture = new(totalWidth, maxHeight, TextureFormat.RGBA32, false);
                try
                {
                    int writeX = 0;
                    foreach (RectInt segment in normalizedSegments)
                    {
                        composedTexture.SetPixels(writeX, 0, segment.width, segment.height, sourceTexture.GetPixels(segment.x, segment.y, segment.width, segment.height));
                        writeX += segment.width;
                    }

                    composedTexture.Apply();
                    return composedTexture.EncodeToPNG();
                }
                finally
                {
                    Object.DestroyImmediate(composedTexture);
                }
            }
            finally
            {
                Object.DestroyImmediate(sourceTexture);
            }
        }

        /// <summary>
        /// 허브 벽 배경은 런타임 Resources 경로를 유지하면서도 선택적 외부 타일 조합으로 다시 생성한다.
        /// 외부 타일이 없으면 현재 generated 스프라이트를 그대로 사용한다.
        /// </summary>
        private static Sprite EnsureHubWallBackgroundSpriteAsset()
        {
            Texture2D horizontalWallTexture = LoadPngTexture(HubWallBackgroundHorizontalWallDesignSourcePath);
            Texture2D verticalWallTexture = LoadPngTexture(HubWallBackgroundVerticalWallDesignSourcePath);
            Texture2D bottomLeftCornerTexture = LoadPngTexture(HubWallBackgroundBottomLeftDesignSourcePath);
            Texture2D bottomRightCornerTexture = LoadPngTexture(HubWallBackgroundBottomRightDesignSourcePath);
            Texture2D fillTexture = LoadPngTexture(HubWallBackgroundFillDesignSourcePath);

            if (horizontalWallTexture == null
                || verticalWallTexture == null
                || bottomLeftCornerTexture == null
                || bottomRightCornerTexture == null
                || fillTexture == null)
            {
                DestroyTextureIfNeeded(horizontalWallTexture);
                DestroyTextureIfNeeded(verticalWallTexture);
                DestroyTextureIfNeeded(bottomLeftCornerTexture);
                DestroyTextureIfNeeded(bottomRightCornerTexture);
                DestroyTextureIfNeeded(fillTexture);
                return LoadConfiguredSprite(HubWallBackgroundSpritePath, 100f, new Vector2(0.5f, 0.5f));
            }

            Texture2D backgroundTexture = null;

            try
            {
                backgroundTexture = CreateHubWallBackgroundTexture(
                    horizontalWallTexture,
                    verticalWallTexture,
                    bottomLeftCornerTexture,
                    bottomRightCornerTexture,
                    fillTexture);

                byte[] pngBytes = backgroundTexture.EncodeToPNG();
                WriteBytesToAssetPath(HubWallBackgroundSpritePath, pngBytes);
            }
            finally
            {
                DestroyTextureIfNeeded(backgroundTexture);
                DestroyTextureIfNeeded(horizontalWallTexture);
                DestroyTextureIfNeeded(verticalWallTexture);
                DestroyTextureIfNeeded(bottomLeftCornerTexture);
                DestroyTextureIfNeeded(bottomRightCornerTexture);
                DestroyTextureIfNeeded(fillTexture);
            }

            AssetDatabase.ImportAsset(HubWallBackgroundSpritePath, ImportAssetOptions.ForceUpdate);
            return ConfigureSpriteAsset(
                HubWallBackgroundSpritePath,
                100f,
                new Vector2(0.5f, 0.5f),
                Vector4.zero,
                filterMode: FilterMode.Point);
        }

        private static Texture2D CreateHubWallBackgroundTexture(
            Texture2D horizontalWallTexture,
            Texture2D verticalWallTexture,
            Texture2D bottomLeftCornerTexture,
            Texture2D bottomRightCornerTexture,
            Texture2D fillTexture)
        {
            Texture2D targetTexture = new(HubWallBackgroundTextureWidth, HubWallBackgroundTextureHeight, TextureFormat.RGBA32, false);
            Color32[] targetPixels = new Color32[HubWallBackgroundTextureWidth * HubWallBackgroundTextureHeight];

            int topEdgeY = HubWallBackgroundTextureHeight - HubWallBackgroundBorderSize;
            int rightEdgeX = HubWallBackgroundTextureWidth - HubWallBackgroundBorderSize;
            int horizontalWallStartX = HubWallBackgroundBorderSize;
            int horizontalWallWidth = HubWallBackgroundTextureWidth - (HubWallBackgroundBorderSize * 2);
            int verticalWallHeight = topEdgeY;
            int innerStartX = HubWallBackgroundBorderSize;
            int innerWidth = HubWallBackgroundTextureWidth - (HubWallBackgroundBorderSize * 2);
            int innerHeight = topEdgeY;

            DrawTextureTiledArea(
                targetPixels,
                HubWallBackgroundTextureWidth,
                HubWallBackgroundTextureHeight,
                fillTexture,
                innerStartX,
                0,
                innerWidth,
                innerHeight);

            DrawTextureRepeatedHorizontally(
                targetPixels,
                HubWallBackgroundTextureWidth,
                HubWallBackgroundTextureHeight,
                horizontalWallTexture,
                horizontalWallStartX,
                topEdgeY,
                horizontalWallWidth,
                HubWallBackgroundBorderSize,
                HubWallBackgroundBorderSize);
            DrawTextureRepeatedVertically(
                targetPixels,
                HubWallBackgroundTextureWidth,
                HubWallBackgroundTextureHeight,
                verticalWallTexture,
                0,
                0,
                verticalWallHeight,
                HubWallBackgroundBorderSize,
                HubWallBackgroundBorderSize);
            DrawTextureRepeatedVertically(
                targetPixels,
                HubWallBackgroundTextureWidth,
                HubWallBackgroundTextureHeight,
                verticalWallTexture,
                rightEdgeX,
                0,
                verticalWallHeight,
                HubWallBackgroundBorderSize,
                HubWallBackgroundBorderSize,
                flipX: true);
            DrawTextureScaled(
                targetPixels,
                HubWallBackgroundTextureWidth,
                HubWallBackgroundTextureHeight,
                bottomLeftCornerTexture,
                0,
                topEdgeY,
                HubWallBackgroundBorderSize,
                HubWallBackgroundBorderSize,
                flipY: true);
            DrawTextureScaled(
                targetPixels,
                HubWallBackgroundTextureWidth,
                HubWallBackgroundTextureHeight,
                bottomRightCornerTexture,
                rightEdgeX,
                topEdgeY,
                HubWallBackgroundBorderSize,
                HubWallBackgroundBorderSize,
                flipY: true);

            targetTexture.SetPixels32(targetPixels);
            targetTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return targetTexture;
        }

        private static Sprite EnsureHubFrontOutlineSpriteAsset()
        {
            Texture2D topLeftCornerTexture = LoadPngTexture(HubFrontOutlineTopLeftDesignSourcePath);
            Texture2D horizontalWallTexture = LoadPngTexture(HubFrontOutlineHorizontalWallDesignSourcePath);
            Texture2D bottomRightCornerTexture = LoadPngTexture(HubFrontOutlineBottomRightDesignSourcePath);
            Texture2D sideWallTexture = LoadPngTexture(HubFrontOutlineSideDesignSourcePath);

            if (topLeftCornerTexture == null
                || horizontalWallTexture == null
                || bottomRightCornerTexture == null
                || sideWallTexture == null)
            {
                DestroyTextureIfNeeded(topLeftCornerTexture);
                DestroyTextureIfNeeded(horizontalWallTexture);
                DestroyTextureIfNeeded(bottomRightCornerTexture);
                DestroyTextureIfNeeded(sideWallTexture);
                return LoadConfiguredSprite(HubFrontOutlineSpritePath, 100f, new Vector2(0.5f, 0.5f));
            }

            Texture2D outlineTexture = null;

            try
            {
                outlineTexture = CreateHubFrontOutlineTexture(
                    topLeftCornerTexture,
                    horizontalWallTexture,
                    bottomRightCornerTexture,
                    sideWallTexture);

                byte[] pngBytes = outlineTexture.EncodeToPNG();
                WriteBytesToAssetPath(HubFrontOutlineSpritePath, pngBytes);
            }
            finally
            {
                DestroyTextureIfNeeded(outlineTexture);
                DestroyTextureIfNeeded(topLeftCornerTexture);
                DestroyTextureIfNeeded(horizontalWallTexture);
                DestroyTextureIfNeeded(bottomRightCornerTexture);
                DestroyTextureIfNeeded(sideWallTexture);
            }

            AssetDatabase.ImportAsset(HubFrontOutlineSpritePath, ImportAssetOptions.ForceUpdate);
            return ConfigureSpriteAsset(
                HubFrontOutlineSpritePath,
                100f,
                new Vector2(0.5f, 0.5f),
                Vector4.zero,
                filterMode: FilterMode.Point);
        }

        private static Texture2D CreateHubFrontOutlineTexture(
            Texture2D topLeftCornerTexture,
            Texture2D horizontalWallTexture,
            Texture2D bottomRightCornerTexture,
            Texture2D sideWallTexture)
        {
            Texture2D targetTexture = new(HubFrontOutlineTextureWidth, HubFrontOutlineTextureHeight, TextureFormat.RGBA32, false);
            Color32[] targetPixels = new Color32[HubFrontOutlineTextureWidth * HubFrontOutlineTextureHeight];

            int topEdgeY = HubFrontOutlineTextureHeight - HubFrontOutlineBorderHeight;
            int rightEdgeX = HubFrontOutlineTextureWidth - HubFrontOutlineBorderWidth;
            int topWallWidth = HubFrontOutlineTopWallEndX - HubFrontOutlineTopWallStartX;
            int bottomWallWidth = HubFrontOutlineTextureWidth - HubFrontOutlineBottomWallStartX;
            int leftWallHeight = topEdgeY;
            int rightWallStartY = HubFrontOutlineBorderHeight;
            int rightWallHeight = HubFrontOutlineTextureHeight - rightWallStartY;
            int mirroredTopWallStartX = GetMirroredStartX(HubFrontOutlineTopWallStartX, topWallWidth, HubFrontOutlineTextureWidth);
            int mirroredBottomWallStartX = GetMirroredStartX(HubFrontOutlineBottomWallStartX, bottomWallWidth, HubFrontOutlineTextureWidth);
            int mirroredLeftWallStartX = GetMirroredStartX(0, HubFrontOutlineBorderWidth, HubFrontOutlineTextureWidth);
            int mirroredLeftWallStartY = GetMirroredStartY(0, leftWallHeight, HubFrontOutlineTextureHeight);
            int mirroredRightWallStartX = GetMirroredStartX(rightEdgeX, HubFrontOutlineBorderWidth, HubFrontOutlineTextureWidth);
            int mirroredRightWallStartY = GetMirroredStartY(rightWallStartY, rightWallHeight, HubFrontOutlineTextureHeight);
            int mirroredTopLeftCornerStartX = GetMirroredStartX(0, HubFrontOutlineBorderWidth, HubFrontOutlineTextureWidth);
            int mirroredTopLeftCornerStartY = GetMirroredStartY(topEdgeY, HubFrontOutlineBorderHeight, HubFrontOutlineTextureHeight);
            int mirroredBottomRightCornerStartX = GetMirroredStartX(rightEdgeX, HubFrontOutlineBorderWidth, HubFrontOutlineTextureWidth);
            int mirroredBottomRightCornerStartY = GetMirroredStartY(0, HubFrontOutlineBorderHeight, HubFrontOutlineTextureHeight);

            DrawTextureRepeatedHorizontally(
                targetPixels,
                HubFrontOutlineTextureWidth,
                HubFrontOutlineTextureHeight,
                horizontalWallTexture,
                mirroredTopWallStartX,
                0,
                topWallWidth,
                HubFrontOutlineBorderWidth,
                HubFrontOutlineBorderHeight);
            DrawTextureRepeatedHorizontally(
                targetPixels,
                HubFrontOutlineTextureWidth,
                HubFrontOutlineTextureHeight,
                horizontalWallTexture,
                mirroredBottomWallStartX,
                topEdgeY,
                bottomWallWidth,
                HubFrontOutlineBorderWidth,
                HubFrontOutlineBorderHeight);
            DrawTextureRepeatedVertically(
                targetPixels,
                HubFrontOutlineTextureWidth,
                HubFrontOutlineTextureHeight,
                sideWallTexture,
                mirroredLeftWallStartX,
                mirroredLeftWallStartY,
                leftWallHeight,
                HubFrontOutlineBorderWidth,
                HubFrontOutlineBorderHeight);
            DrawTextureRepeatedVertically(
                targetPixels,
                HubFrontOutlineTextureWidth,
                HubFrontOutlineTextureHeight,
                sideWallTexture,
                mirroredRightWallStartX,
                mirroredRightWallStartY,
                rightWallHeight,
                HubFrontOutlineBorderWidth,
                HubFrontOutlineBorderHeight,
                flipX: true);

            DrawTextureScaled(
                targetPixels,
                HubFrontOutlineTextureWidth,
                HubFrontOutlineTextureHeight,
                topLeftCornerTexture,
                mirroredBottomRightCornerStartX,
                mirroredBottomRightCornerStartY,
                HubFrontOutlineBorderWidth,
                HubFrontOutlineBorderHeight);
            DrawTextureScaled(
                targetPixels,
                HubFrontOutlineTextureWidth,
                HubFrontOutlineTextureHeight,
                bottomRightCornerTexture,
                mirroredTopLeftCornerStartX,
                mirroredTopLeftCornerStartY,
                HubFrontOutlineBorderWidth,
                HubFrontOutlineBorderHeight);

            targetTexture.SetPixels32(targetPixels);
            targetTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return targetTexture;
        }

        private static int GetMirroredStartX(int startX, int width, int containerWidth)
        {
            return containerWidth - startX - width;
        }

        private static int GetMirroredStartY(int startY, int height, int containerHeight)
        {
            return containerHeight - startY - height;
        }

        private static void DrawTextureRepeatedHorizontally(
            Color32[] targetPixels,
            int targetWidth,
            int targetHeight,
            Texture2D sourceTexture,
            int startX,
            int startY,
            int totalWidth,
            int tileWidth,
            int tileHeight,
            bool flipX = false,
            bool flipY = false)
        {
            if (sourceTexture == null || totalWidth <= 0 || tileWidth <= 0 || tileHeight <= 0)
            {
                return;
            }

            int offset = 0;
            while (offset < totalWidth)
            {
                int currentTileWidth = Mathf.Min(tileWidth, totalWidth - offset);
                DrawTextureScaled(
                    targetPixels,
                    targetWidth,
                    targetHeight,
                    sourceTexture,
                    startX + offset,
                    startY,
                    currentTileWidth,
                    tileHeight,
                    flipX,
                    flipY);
                offset += tileWidth;
            }
        }

        private static void DrawTextureRepeatedVertically(
            Color32[] targetPixels,
            int targetWidth,
            int targetHeight,
            Texture2D sourceTexture,
            int startX,
            int startY,
            int totalHeight,
            int tileWidth,
            int tileHeight,
            bool flipX = false,
            bool flipY = false)
        {
            if (sourceTexture == null || totalHeight <= 0 || tileWidth <= 0 || tileHeight <= 0)
            {
                return;
            }

            int offset = 0;
            while (offset < totalHeight)
            {
                int currentTileHeight = Mathf.Min(tileHeight, totalHeight - offset);
                DrawTextureScaled(
                    targetPixels,
                    targetWidth,
                    targetHeight,
                    sourceTexture,
                    startX,
                    startY + offset,
                    tileWidth,
                    currentTileHeight,
                    flipX,
                    flipY);
                offset += tileHeight;
            }
        }

        private static void DrawTextureTiledArea(
            Color32[] targetPixels,
            int targetWidth,
            int targetHeight,
            Texture2D sourceTexture,
            int destinationX,
            int destinationY,
            int destinationWidth,
            int destinationHeight,
            bool flipX = false,
            bool flipY = false)
        {
            if (sourceTexture == null || destinationWidth <= 0 || destinationHeight <= 0)
            {
                return;
            }

            Color32[] sourcePixels = sourceTexture.GetPixels32();
            int sourceWidth = sourceTexture.width;
            int sourceHeight = sourceTexture.height;

            for (int y = 0; y < destinationHeight; y++)
            {
                int targetY = destinationY + y;
                if (targetY < 0 || targetY >= targetHeight)
                {
                    continue;
                }

                int tiledY = y % sourceHeight;
                int sampleY = flipY ? (sourceHeight - 1 - tiledY) : tiledY;

                for (int x = 0; x < destinationWidth; x++)
                {
                    int targetX = destinationX + x;
                    if (targetX < 0 || targetX >= targetWidth)
                    {
                        continue;
                    }

                    int tiledX = x % sourceWidth;
                    int sampleX = flipX ? (sourceWidth - 1 - tiledX) : tiledX;
                    Color32 color = sourcePixels[(sampleY * sourceWidth) + sampleX];
                    if (color.a == 0)
                    {
                        continue;
                    }

                    targetPixels[(targetY * targetWidth) + targetX] = color;
                }
            }
        }

        private static void DrawTextureScaled(
            Color32[] targetPixels,
            int targetWidth,
            int targetHeight,
            Texture2D sourceTexture,
            int destinationX,
            int destinationY,
            int destinationWidth,
            int destinationHeight,
            bool flipX = false,
            bool flipY = false)
        {
            if (sourceTexture == null || destinationWidth <= 0 || destinationHeight <= 0)
            {
                return;
            }

            Color32[] sourcePixels = sourceTexture.GetPixels32();
            int sourceWidth = sourceTexture.width;
            int sourceHeight = sourceTexture.height;

            for (int y = 0; y < destinationHeight; y++)
            {
                int targetY = destinationY + y;
                if (targetY < 0 || targetY >= targetHeight)
                {
                    continue;
                }

                int sourceY = Mathf.Min(sourceHeight - 1, Mathf.FloorToInt((float)y * sourceHeight / destinationHeight));
                if (flipY)
                {
                    sourceY = (sourceHeight - 1) - sourceY;
                }

                int targetRowIndex = targetY * targetWidth;
                int sourceRowIndex = sourceY * sourceWidth;

                for (int x = 0; x < destinationWidth; x++)
                {
                    int targetX = destinationX + x;
                    if (targetX < 0 || targetX >= targetWidth)
                    {
                        continue;
                    }

                    int sourceX = Mathf.Min(sourceWidth - 1, Mathf.FloorToInt((float)x * sourceWidth / destinationWidth));
                    if (flipX)
                    {
                        sourceX = (sourceWidth - 1) - sourceX;
                    }

                    Color32 sourceColor = sourcePixels[sourceRowIndex + sourceX];
                    if (sourceColor.a == 0)
                    {
                        continue;
                    }

                    targetPixels[targetRowIndex + targetX] = sourceColor;
                }
            }
        }

        private static Texture2D LoadPngTexture(string assetPath)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Object.DestroyImmediate(texture);
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static void WriteBytesToAssetPath(string assetPath, byte[] bytes)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(fullPath, bytes);
        }

        private static void DestroyTextureIfNeeded(Texture2D texture)
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// 선택적 외부 UI PNG를 generated 스프라이트 경로로 복사한다.
        /// 외부 PNG가 없으면 현재 generated 출력물을 그대로 유지한다.
        /// </summary>
        private static void CreateUiDesignSprites()
        {
            EnsureUiDesignSpriteAsset(
                CloseButtonDesignSourcePath,
                UiButtonSpriteRoot + "/close-button.png",
                Vector4.zero);
            EnsureUiDesignSpriteAsset(
                HelpButtonDesignSourcePath,
                UiButtonSpriteRoot + "/help-button.png",
                Vector4.zero);
            EnsureUiDesignSpriteAsset(
                SystemTextBoxDesignSourcePath,
                UiMessageBoxSpriteRoot + "/system-text-box.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                InteractionTextBoxDesignSourcePath,
                UiMessageBoxSpriteRoot + "/interaction-text-box.png",
                new Vector4(8f, 14f, 8f, 14f));
            EnsureUiDesignSpriteAsset(
                DarkOutlinePanelDesignSourcePath,
                UiPanelSpriteRoot + "/dark-outline-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                DarkOutlinePanelAltDesignSourcePath,
                UiPanelSpriteRoot + "/dark-outline-panel-alt.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                DarkSolidPanelDesignSourcePath,
                UiPanelSpriteRoot + "/dark-solid-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                DarkThinOutlinePanelDesignSourcePath,
                UiPanelSpriteRoot + "/dark-thin-outline-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                LightOutlinePanelDesignSourcePath,
                UiPanelSpriteRoot + "/light-outline-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                LightSolidPanelDesignSourcePath,
                UiPanelSpriteRoot + "/light-solid-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
        }

        private static void EnsureUiDesignSpriteAsset(string sourceAssetPath, string assetPath, Vector4 border)
        {
            string sourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), sourceAssetPath);
            if (!File.Exists(sourceFullPath))
            {
                return;
            }

            string assetFullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            CopyFileIfDifferent(sourceFullPath, assetFullPath);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteAsset(assetPath, 100f, new Vector2(0.5f, 0.5f), border);
        }

        private static Sprite CreatePlayerSprite(string assetPath, string sourceFileName)
        {
            string sourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), "temperature", sourceFileName);
            string targetFullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            if (File.Exists(sourceFullPath))
            {
                CopyFileIfDifferent(sourceFullPath, targetFullPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            Sprite importedSprite = ConfigureSpriteAsset(assetPath, PlayerSpritePixelsPerUnit);

            if (importedSprite != null)
            {
                return importedSprite;
            }

            return CreateColorSprite(assetPath, Color.white);
        }

        private static Sprite LoadConfiguredSprite(string assetPath, float pixelsPerUnit, Vector2 pivot)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(fullPath))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return ConfigureSpriteAsset(assetPath, pixelsPerUnit, pivot);
        }

        private static ResourceData CreateResourceAsset(string assetPath, string id, string displayName, string description, string regionTag, int sellPrice, ResourceRarity rarity)
        {
            ResourceData asset = AssetDatabase.LoadAssetAtPath<ResourceData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ResourceData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            EnsureMainObjectNameMatchesFileName(asset, assetPath);

            SerializedObject so = new(asset);
            so.FindProperty("resourceId").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("regionTag").stringValue = regionTag;
            so.FindProperty("baseSellPrice").intValue = sellPrice;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static RecipeData CreateRecipeAsset(
            string assetPath,
            string id,
            string displayName,
            string description,
            int sellPrice,
            int reputationDelta,
            IReadOnlyList<RecipeIngredientDefinition> ingredients)
        {
            RecipeData asset = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RecipeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            EnsureMainObjectNameMatchesFileName(asset, assetPath);

            SerializedObject so = new(asset);
            so.FindProperty("recipeId").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("sellPrice").intValue = sellPrice;
            so.FindProperty("reputationDelta").intValue = reputationDelta;

            SerializedProperty ingredientsProperty = so.FindProperty("ingredients");
            ingredientsProperty.arraySize = ingredients.Count;

            for (int index = 0; index < ingredients.Count; index++)
            {
                SerializedProperty item = ingredientsProperty.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("resource").objectReferenceValue = ingredients[index].Resource;
                item.FindPropertyRelative("amount").intValue = ingredients[index].Amount;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        /// generated 자산은 파일명과 메인 오브젝트 이름을 같게 유지해 저장 경고를 막는다.
        /// </summary>
        private static void EnsureMainObjectNameMatchesFileName(Object asset, string assetPath)
        {
            if (asset == null || string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            string expectedName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrWhiteSpace(expectedName)
                || string.Equals(asset.name, expectedName, StringComparison.Ordinal))
            {
                return;
            }

            asset.name = expectedName;
            EditorUtility.SetDirty(asset);
        }

        private static Sprite CreateColorSprite(string assetPath, Color color)
        {
            EnsureColorSpriteAssetExists(assetPath, color);
            return ConfigureSpriteAsset(assetPath, 100f);
        }

        /// <summary>
        /// generated 스프라이트는 Resources 폴더에도 같은 상대 경로로 한 벌 더 만들어 런타임 폴백에서 재사용한다.
        /// </summary>
        /// <summary>
        /// 단색 스프라이트는 파일이 없을 때만 만들고, 이미 있으면 기존 GUID와 참조를 유지한다.
        /// </summary>
        private static void EnsureColorSpriteAssetExists(string assetPath, Color color)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return;
            }

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Texture2D texture = new(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static Sprite ConfigureSpriteAsset(string assetPath, float pixelsPerUnit)
        {
            return ConfigureSpriteAsset(assetPath, pixelsPerUnit, new Vector2(0.5f, 0.08f), Vector4.zero);
        }

        private static Sprite ConfigureSpriteAsset(string assetPath, float pixelsPerUnit, Vector2 pivot)
        {
            return ConfigureSpriteAsset(assetPath, pixelsPerUnit, pivot, Vector4.zero);
        }

        private static Sprite ConfigureSpriteAsset(
            string assetPath,
            float pixelsPerUnit,
            Vector2 pivot,
            Vector4 border,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = wrapMode;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 100;

            TextureImporterSettings spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = pivot;
            spriteSettings.spriteBorder = border;
            importer.SetTextureSettings(spriteSettings);

            ApplyUncompressedPlatformSettings(importer, "DefaultTexturePlatform");
            ApplyUncompressedPlatformSettings(importer, "Standalone");
            ApplyUncompressedPlatformSettings(importer, "WebGL");

            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ApplyUncompressedPlatformSettings(TextureImporter importer, string platformName)
        {
            TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platformName);
            platformSettings.name = platformName;
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = 2048;
            platformSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            platformSettings.compressionQuality = 100;
            platformSettings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(platformSettings);
        }

        private static BoxCollider2D CreateMovementBounds(string objectName, float width, float height)
        {
            GameObject boundsObject = new(objectName);
            ApplySceneTransformOverride(boundsObject.transform, objectName, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: false);
            BoxCollider2D bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(width, height);
            ApplySceneComponentOverride(bounds, objectName);
            ApplySceneActiveOverride(boundsObject, objectName);
            return bounds;
        }

        private static void AttachPlayerBoundsLimiter(GameObject player, Collider2D movementBounds)
        {
            if (player == null || movementBounds == null)
            {
                return;
            }

            PlayerBoundsLimiter limiter = player.AddComponent<PlayerBoundsLimiter>();
            SerializedObject so = new(limiter);
            so.FindProperty("movementBounds").objectReferenceValue = movementBounds;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(limiter, player.name);
        }

        /// <summary>
        /// TMP 컴포넌트 생성 전에 builder가 선호하는 기본 폰트를 다시 묶어 누락 경고를 막습니다.
        /// </summary>
        private static TMP_FontAsset EnsurePreferredTmpFontAsset()
        {
            TMP_FontAsset preferredFont = _generatedKoreanFont;

            if (preferredFont == null)
            {
                preferredFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRoot + "/maplestoryLightSdf.asset");
            }

            if (preferredFont != null && TMP_Settings.defaultFontAsset != preferredFont)
            {
                TMP_Settings.defaultFontAsset = preferredFont;

                if (TMP_Settings.instance != null)
                {
                    EditorUtility.SetDirty(TMP_Settings.instance);
                }
            }

            return preferredFont != null ? preferredFont : TMP_Settings.defaultFontAsset;
        }

        private static TMP_FontAsset EnsureHeadingTmpFontAsset()
        {
            TMP_FontAsset headingFont = _generatedHeadingFont;

            if (headingFont == null)
            {
                headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRoot + "/maplestoryBoldSdf.asset");
            }

            return headingFont != null ? headingFont : EnsurePreferredTmpFontAsset();
        }

        private static TMP_FontAsset CreateHeadingFontAsset()
        {
            return CreateProjectFontAsset(FontRoot + "/maplestoryBold.ttf", "maplestoryBoldSdf");
        }

        private static TMP_FontAsset CreateKoreanFontAsset()
        {
            return CreateProjectFontAsset(FontRoot + "/maplestoryLight.ttf", "maplestoryLightSdf");
        }

        private static TMP_FontAsset CreateProjectFontAsset(string importedFontPath, string fontAssetName)
        {
            AssetDatabase.ImportAsset(importedFontPath, ImportAssetOptions.ForceUpdate);

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(importedFontPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"프로젝트 폰트 '{importedFontPath}'를 불러오지 못해 기본 TMP 폰트를 사용합니다.");
                return TMP_Settings.defaultFontAsset;
            }

            string fontAssetPath = $"{FontRoot}/{fontAssetName}.asset";
            TMP_FontAsset existingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (existingFont != null)
            {
                NormalizeGeneratedFontAssetNames(existingFont, fontAssetName);
                existingFont.TryAddCharacters(CollectRequiredCharacters());
                EditorUtility.SetDirty(existingFont);

                if (existingFont.material != null)
                {
                    EditorUtility.SetDirty(existingFont.material);
                }

                if (existingFont.atlasTextures != null)
                {
                    foreach (Texture2D atlasTexture in existingFont.atlasTextures)
                    {
                        if (atlasTexture != null)
                        {
                            EditorUtility.SetDirty(atlasTexture);
                        }
                    }
                }

                return existingFont;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            if (fontAsset == null)
            {
                Debug.LogWarning($"TMP 폰트 자산 '{fontAssetName}' 생성에 실패해 기본 폰트를 사용합니다.");
                return TMP_Settings.defaultFontAsset;
            }

            fontAsset.name = fontAssetName;
            fontAsset.TryAddCharacters(CollectRequiredCharacters());
            AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

            if (fontAsset.atlasTextures != null)
            {
                for (int index = 0; index < fontAsset.atlasTextures.Length; index++)
                {
                    Texture2D atlasTexture = fontAsset.atlasTextures[index];
                    if (atlasTexture == null || AssetDatabase.Contains(atlasTexture))
                    {
                        continue;
                    }

                    atlasTexture.name = $"{fontAssetName} Atlas {index}";
                    AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
                }
            }

            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = $"{fontAssetName} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            NormalizeGeneratedFontAssetNames(fontAsset, fontAssetName);
            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.material != null)
            {
                EditorUtility.SetDirty(fontAsset.material);
            }

            if (fontAsset.atlasTextures != null)
            {
                foreach (Texture2D atlasTexture in fontAsset.atlasTextures)
                {
                    if (atlasTexture != null)
                    {
                        EditorUtility.SetDirty(atlasTexture);
                    }
                }
            }

            return fontAsset;
        }

        private static void NormalizeGeneratedFontAssetNames(TMP_FontAsset fontAsset, string fontAssetName)
        {
            if (fontAsset == null)
            {
                return;
            }

            fontAsset.name = fontAssetName;

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontAssetName}Material";
            }

            if (fontAsset.atlasTextures == null)
            {
                return;
            }

            for (int index = 0; index < fontAsset.atlasTextures.Length; index++)
            {
                Texture2D atlasTexture = fontAsset.atlasTextures[index];
                if (atlasTexture != null)
                {
                    atlasTexture.name = $"{fontAssetName}Atlas{index}";
                }
            }
        }

        private static string CollectRequiredCharacters()
        {
            // TMP 말줄임표 overflow는 U+2026을 직접 사용하므로 generated 폰트에 해당 글리프가 꼭 있어야 합니다.
            return "종구의 식당바닷가 깊은 숲 폐광산 바람 언덕 이동 방향키 상호작용 메뉴 변경 영업 시작 메뉴판 영업대 채집하기 생선 조개 해초 약초 버섯 향초 발광 이끼 인벤토리 비어 있음 골드 코인 평판 선택 가능 수량 결과 없음 메뉴를 고르고 영업을 시작하세요 선택된 메뉴가 없습니다 재료가 부족합니다 접시 판매 식당으로 이동 바닷가로 이동 깊은 숲으로 이동 폐광산으로 이동 바람 언덕으로 이동 식당 복귀 생선 한 접시 해물탕 약초 생선탕 숲 버섯 모둠 광채 해물탕 향초 해초 무침 늪지 강풍 랜턴 맡길 품목 꺼낼 품목 지름길 정상 어두운 업그레이드 재료 창고 닫기 열기 / : + [] WASD E …";
        }

        private static void CopyFileIfDifferent(string sourcePath, string targetPath)
        {
            string directoryPath = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (File.Exists(targetPath))
            {
                FileInfo sourceInfo = new(sourcePath);
                FileInfo targetInfo = new(targetPath);

                if (sourceInfo.Length == targetInfo.Length)
                {
                    return;
                }
            }

            File.Copy(sourcePath, targetPath, true);
        }

        private static void WriteFileIfDifferent(string targetPath, byte[] content)
        {
            string directoryPath = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (File.Exists(targetPath))
            {
                byte[] existingContent = File.ReadAllBytes(targetPath);
                if (existingContent.SequenceEqual(content))
                {
                    return;
                }
            }

            File.WriteAllBytes(targetPath, content);
        }

        /// <summary>
        /// generated 씬은 빌더가 항상 전체를 다시 쓰므로,
        /// 기존 손상 본문이 남아 저장을 막지 않게 `.unity` 파일만 지운 뒤 같은 경로에 다시 저장합니다.
        /// `.meta`는 유지해서 씬 GUID와 Build Settings 참조는 바꾸지 않습니다.
        /// 아직 저장되지 않은 새 씬도 저장 경로 이름을 기준으로 Hierarchy 그룹을 먼저 맞춥니다.
        /// </summary>
        private static void SaveGeneratedScene(string scenePath)
        {
            string directoryPath = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            int removedMissingScripts = RemoveMissingScriptsInScene(activeScene);
            if (removedMissingScripts > 0)
            {
                Debug.LogWarning($"'{Path.GetFileNameWithoutExtension(scenePath)}' 생성 씬에서 누락 스크립트 {removedMissingScripts}개를 저장 전에 정리했습니다.");
            }

            string managedSceneName = Path.GetFileNameWithoutExtension(scenePath);
            PrototypeSceneHierarchyOrganizer.OrganizeSceneHierarchy(activeScene, managedSceneName, saveScene: false);

            if (File.Exists(scenePath))
            {
                File.Delete(scenePath);
            }

            EditorSceneManager.SaveScene(activeScene, scenePath);
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = ManagedScenePaths
                .Select(scenePath => new EditorBuildSettingsScene(scenePath, true))
                .ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static int RemoveMissingScriptsRecursive(GameObject target)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);

            foreach (Transform child in target.transform)
            {
                removed += RemoveMissingScriptsRecursive(child.gameObject);
            }

            return removed;
        }

        private static int RemoveMissingScriptsInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                removed += RemoveMissingScriptsRecursive(root);
            }

            return removed;
        }
    }
}
#endif
