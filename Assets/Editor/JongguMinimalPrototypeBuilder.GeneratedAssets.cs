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
                PlayerFront = LoadPlayerSprite(PlayerFrontSpritePath),
                PlayerBack = LoadPlayerSprite(PlayerBackSpritePath),
                PlayerSide = LoadPlayerSprite(PlayerSideSpritePath),
                HubFloorTile = EnsureCopiedSpriteAsset(
                    HubFloorTileDesignSourcePath,
                    HubFloorTileSpritePath,
                    HubRoomLayout.FloorTilePixelsPerUnit,
                    new Vector2(0.5f, 0.5f),
                    FilterMode.Point,
                    TextureWrapMode.Repeat),
                HubFloorBackground = LoadConfiguredSprite(HubFloorBackgroundSpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubWallBackground = EnsureCopiedSpriteAsset(
                    HubWallBackgroundDesignSourcePath,
                    HubWallBackgroundSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f),
                    FilterMode.Point),
                HubFrontOutline = EnsureCopiedSpriteAsset(
                    HubFrontOutlineDesignSourcePath,
                    HubFrontOutlineSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f),
                    FilterMode.Point),
                FrontCounter = EnsureCopiedSpriteAsset(
                    FrontCounterDesignSourcePath,
                    FrontCounterSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f),
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    HubBarMainSpriteBorder),
                BackCounter = EnsureCopiedSpriteAsset(
                    BackCounterDesignSourcePath,
                    BackCounterSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f)),
                MosaicTileFloor = EnsureCopiedSpriteAsset(
                    MosaicTileFloorDesignSourcePath,
                    MosaicTileFloorSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f)),
                MosaicTileWall = EnsureCopiedSpriteAsset(
                    MosaicTileWallDesignSourcePath,
                    MosaicTileWallSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f)),
                TableChair2 = EnsureCopiedSpriteAsset(
                    TableChair2DesignSourcePath,
                    TableChair2SpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f)),
                AccountBoard = EnsureCopiedSpriteAsset(
                    AccountBoardDesignSourcePath,
                    AccountBoardSpritePath,
                    100f,
                    new Vector2(0.5f, 0.5f)),
                HubBar = LoadConfiguredSprite(FrontCounterSpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubBarRight = LoadConfiguredSprite(HubBarRightSpritePath, 100f, new Vector2(0.5f, 0.5f)),
                HubTableUnlocked = LoadConfiguredSprite(TableChair2SpritePath, 100f, new Vector2(0.5f, 0.5f)),
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

        private static Sprite LoadPlayerSprite(string assetPath)
        {
            Sprite importedSprite = LoadConfiguredSprite(
                assetPath,
                PlayerSpritePixelsPerUnit,
                new Vector2(0.5f, 0.08f),
                FilterMode.Point);

            if (importedSprite != null)
            {
                return importedSprite;
            }

            EnsureColorSpriteAssetExists(assetPath, Color.white);
            return LoadConfiguredSprite(
                assetPath,
                PlayerSpritePixelsPerUnit,
                new Vector2(0.5f, 0.08f),
                FilterMode.Point);
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

        private static Sprite LoadConfiguredSprite(string assetPath, float pixelsPerUnit, Vector2 pivot, FilterMode filterMode, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(fullPath))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return ConfigureSpriteAsset(assetPath, pixelsPerUnit, pivot, Vector4.zero, filterMode, wrapMode);
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
