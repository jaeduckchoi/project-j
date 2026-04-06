#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CoreLoop.Core;
using CoreLoop.Flow;
using Exploration.Camera;
using Exploration.Gathering;
using Exploration.Interaction;
using Exploration.Player;
using Exploration.World;
using Management.Economy;
using Management.Inventory;
using Management.Storage;
using Management.Tools;
using Management.Upgrade;
using Restaurant;
using Shared;
using Shared.Data;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if ENABLE_INPUT_SYSTEM
#endif

// ProjectEditor 네임스페이스
namespace Editor
{
    public static partial class JongguMinimalPrototypeBuilder
    {
        private static PrototypeGeneratedAssetSettings AssetSettings => PrototypeGeneratedAssetSettings.GetCurrent();
        private static string GeneratedRoot => AssetSettings.ResourcesGeneratedRoot;
        private static string GameDataRoot => AssetSettings.GameDataRoot;
        private static string ResourceDataRoot => AssetSettings.ResourceDataRoot;
        private static string RecipeDataRoot => AssetSettings.RecipeDataRoot;
        private static string InputDataRoot => AssetSettings.InputDataRoot;
        private static string SpriteRoot => AssetSettings.SpriteRoot;
        private static string PlayerSpriteRoot => AssetSettings.PlayerSpriteRoot;
        private static string GatherSpriteRoot => AssetSettings.GatherSpriteRoot;
        private static string UiSpriteRoot => AssetSettings.UiSpriteRoot;
        private static string UiButtonSpriteRoot => AssetSettings.UiButtonSpriteRoot;
        private static string UiMessageBoxSpriteRoot => AssetSettings.UiMessageBoxSpriteRoot;
        private static string UiPanelSpriteRoot => AssetSettings.UiPanelSpriteRoot;
        private static string WorldSpriteRoot => AssetSettings.WorldSpriteRoot;
        private static string SceneRoot => AssetSettings.SceneRoot;
        private static string SharedExplorationHudSourceScene => AssetSettings.SharedExplorationHudSourceScene;
        private static string FontRoot => AssetSettings.FontRoot;
        private static string HubFloorBackgroundSpritePath => AssetSettings.HubFloorBackgroundSpritePath;
        private static string HubFloorTileDesignSourcePath => AssetSettings.HubFloorTileDesignSourcePath;
        private static string HubFloorTileSpritePath => AssetSettings.HubFloorTileSpritePath;
        private static string HubBarDesignSourcePath => AssetSettings.HubBarDesignSourcePath;
        private static string HubBarRightSpritePath => AssetSettings.HubBarRightSpritePath;
        private static RectInt HubBarRightLeftCapCropRect => AssetSettings.HubBarRightLeftCapCropRect;
        private static RectInt HubBarRightBodyCropRect => AssetSettings.HubBarRightBodyCropRect;
        private static Vector4 HubBarMainSpriteBorder => AssetSettings.HubBarMainSpriteBorder;
        private static Vector4 HubBarRightSpriteBorder => AssetSettings.HubBarRightSpriteBorder;
        private static string HubWallBackgroundHorizontalWallDesignSourcePath => AssetSettings.HubWallBackgroundHorizontalWallDesignSourcePath;
        private static string HubWallBackgroundVerticalWallDesignSourcePath => AssetSettings.HubWallBackgroundVerticalWallDesignSourcePath;
        private static string HubWallBackgroundFillDesignSourcePath => AssetSettings.HubWallBackgroundFillDesignSourcePath;
        private static string HubWallBackgroundBottomLeftDesignSourcePath => AssetSettings.HubWallBackgroundBottomLeftDesignSourcePath;
        private static string HubWallBackgroundBottomRightDesignSourcePath => AssetSettings.HubWallBackgroundBottomRightDesignSourcePath;
        private static int HubWallBackgroundTextureWidth => AssetSettings.HubWallBackgroundTextureWidth;
        private static int HubWallBackgroundTextureHeight => AssetSettings.HubWallBackgroundTextureHeight;
        private static int HubWallBackgroundBorderSize => AssetSettings.HubWallBackgroundBorderSize;
        private static string HubFrontOutlineTopLeftDesignSourcePath => AssetSettings.HubFrontOutlineTopLeftDesignSourcePath;
        private static string HubFrontOutlineHorizontalWallDesignSourcePath => AssetSettings.HubFrontOutlineHorizontalWallDesignSourcePath;
        private static string HubFrontOutlineBottomRightDesignSourcePath => AssetSettings.HubFrontOutlineBottomRightDesignSourcePath;
        private static string HubFrontOutlineSideDesignSourcePath => AssetSettings.HubFrontOutlineSideDesignSourcePath;
        private static int HubFrontOutlineTextureWidth => AssetSettings.HubFrontOutlineTextureWidth;
        private static int HubFrontOutlineTextureHeight => AssetSettings.HubFrontOutlineTextureHeight;
        private static int HubFrontOutlineBorderWidth => AssetSettings.HubFrontOutlineBorderWidth;
        private static int HubFrontOutlineBorderHeight => AssetSettings.HubFrontOutlineBorderHeight;
        private static int HubFrontOutlineTopWallStartX => AssetSettings.HubFrontOutlineTopWallStartX;
        private static int HubFrontOutlineTopWallEndX => AssetSettings.HubFrontOutlineTopWallEndX;
        private static int HubFrontOutlineBottomWallStartX => AssetSettings.HubFrontOutlineBottomWallStartX;
        private static string HubWallBackgroundSpritePath => AssetSettings.HubWallBackgroundSpritePath;
        private static string HubFrontOutlineSpritePath => AssetSettings.HubFrontOutlineSpritePath;
        private static string HubBarSpritePath => AssetSettings.HubBarSpritePath;
        private static string HubTableUnlockedSpritePath => AssetSettings.HubTableUnlockedSpritePath;
        private static string HubUpgradeSlotSpritePath => AssetSettings.HubUpgradeSlotSpritePath;
        private static string HubTodayMenuBgSpritePath => AssetSettings.HubTodayMenuBgSpritePath;
        private static string HubTodayMenuItem1SpritePath => AssetSettings.HubTodayMenuItem1SpritePath;
        private static string HubTodayMenuItem2SpritePath => AssetSettings.HubTodayMenuItem2SpritePath;
        private static string HubTodayMenuItem3SpritePath => AssetSettings.HubTodayMenuItem3SpritePath;
        private static string CloseButtonDesignSourcePath => AssetSettings.CloseButtonDesignSourcePath;
        private static string HelpButtonDesignSourcePath => AssetSettings.HelpButtonDesignSourcePath;
        private static string SystemTextBoxDesignSourcePath => AssetSettings.SystemTextBoxDesignSourcePath;
        private static string InteractionTextBoxDesignSourcePath => AssetSettings.InteractionTextBoxDesignSourcePath;
        private static string DarkOutlinePanelDesignSourcePath => AssetSettings.DarkOutlinePanelDesignSourcePath;
        private static string DarkOutlinePanelAltDesignSourcePath => AssetSettings.DarkOutlinePanelAltDesignSourcePath;
        private static string DarkSolidPanelDesignSourcePath => AssetSettings.DarkSolidPanelDesignSourcePath;
        private static string DarkThinOutlinePanelDesignSourcePath => AssetSettings.DarkThinOutlinePanelDesignSourcePath;
        private static string LightOutlinePanelDesignSourcePath => AssetSettings.LightOutlinePanelDesignSourcePath;
        private static string LightSolidPanelDesignSourcePath => AssetSettings.LightSolidPanelDesignSourcePath;
        private static float PlayerSpritePixelsPerUnit => AssetSettings.PlayerSpritePixelsPerUnit;
        private static float PlayerVisualScale => AssetSettings.PlayerVisualScale;
        private static Vector3 DefaultPlayerRootScale => AssetSettings.DefaultPlayerRootScale;
        private static float WorldTitleFontSize => AssetSettings.WorldTitleFontSize;
        private static float WorldLabelFontSize => AssetSettings.WorldLabelFontSize;
        private static float WorldLabelSmallFontSize => AssetSettings.WorldLabelSmallFontSize;

        private static TMP_FontAsset _generatedKoreanFont;
        private static TMP_FontAsset _generatedHeadingFont;
        private static readonly Dictionary<string, Material> CachedWorldTextMaterials = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, SceneTransformSnapshot> CachedSceneTransforms = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, bool> CachedSceneObjectActiveStates = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Dictionary<Type, SceneSerializedComponentSnapshot>> CachedSceneComponentOverrides = new(StringComparer.Ordinal);

        private static readonly Type[] SceneOverrideComponentTypes =
        {
            typeof(SpriteRenderer),
            typeof(TextMeshPro),
            typeof(MeshRenderer),
            typeof(Rigidbody2D),
            typeof(BoxCollider2D),
            typeof(CircleCollider2D),
            typeof(CapsuleCollider2D),
            typeof(Camera),
            typeof(AudioListener),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(InventoryManager),
            typeof(StorageManager),
            typeof(EconomyManager),
            typeof(ToolManager),
            typeof(DayCycleManager),
            typeof(UpgradeManager),
            typeof(GameManager),
            typeof(RestaurantManager),
            typeof(UIManager),
            typeof(InteractionDetector),
            typeof(PlayerController),
            typeof(PlayerDirectionalSprite),
            typeof(CameraFollow),
            typeof(PlayerBoundsLimiter),
            typeof(ScenePortal),
            typeof(SceneSpawnPoint),
            typeof(GuideTriggerZone),
            typeof(MovementModifierZone),
            typeof(DarknessZone),
            typeof(WindGustZone),
            typeof(GatherableResource),
            typeof(RecipeSelectorStation),
            typeof(ServiceCounterStation),
            typeof(StorageStation),
            typeof(UpgradeStation),
            typeof(HubTodayMenuDisplay)
        };

        private static readonly string[] HubPopupSceneImageNames =
        {
            "PopupOverlay",
            "PopupFrame",
            "PopupFrameLeft",
            "PopupFrameRight",
            "PopupLeftBody",
            "PopupRightBody",
            "PopupCloseButton"
        };

        private static string[] SharedExplorationHudTargetScenes => AssetSettings.SharedExplorationHudTargetScenes;

        private static string[] ManagedScenePaths => AssetSettings.ManagedScenePaths;

        private static readonly Dictionary<string, SceneImageSnapshot> CachedHubPopupSceneImages = new(StringComparer.Ordinal);

        private readonly struct SceneImageSnapshot
        {
            public SceneImageSnapshot(Sprite sprite, Image.Type type, Color color, bool preserveAspect)
            {
                Sprite = sprite;
                Type = type;
                Color = color;
                PreserveAspect = preserveAspect;
            }

            public Sprite Sprite { get; }
            public Image.Type Type { get; }
            public Color Color { get; }
            public bool PreserveAspect { get; }
        }

        private readonly struct SceneTransformSnapshot
        {
            public SceneTransformSnapshot(Vector3 position, Quaternion rotation, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                Position = position;
                Rotation = rotation;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private enum SceneSerializedValueKind
        {
            Boolean,
            Integer,
            Float,
            String,
            Color,
            ObjectReference,
            Vector2,
            Vector3,
            Vector4,
            Quaternion,
            Rect
        }

        private readonly struct SceneSerializedPropertySnapshot
        {
            public SceneSerializedPropertySnapshot(
                string propertyPath,
                SceneSerializedValueKind valueKind,
                bool boolValue = default,
                int intValue = default,
                float floatValue = default,
                string stringValue = null,
                Color colorValue = default,
                Object objectReferenceValue = null,
                Vector2 vector2Value = default,
                Vector3 vector3Value = default,
                Vector4 vector4Value = default,
                Quaternion quaternionValue = default,
                Rect rectValue = default)
            {
                PropertyPath = propertyPath;
                ValueKind = valueKind;
                BoolValue = boolValue;
                IntValue = intValue;
                FloatValue = floatValue;
                StringValue = stringValue;
                ColorValue = colorValue;
                ObjectReferenceValue = objectReferenceValue;
                Vector2Value = vector2Value;
                Vector3Value = vector3Value;
                Vector4Value = vector4Value;
                QuaternionValue = quaternionValue;
                RectValue = rectValue;
            }

            public string PropertyPath { get; }
            public SceneSerializedValueKind ValueKind { get; }
            public bool BoolValue { get; }
            public int IntValue { get; }
            public float FloatValue { get; }
            public string StringValue { get; }
            public Color ColorValue { get; }
            public Object ObjectReferenceValue { get; }
            public Vector2 Vector2Value { get; }
            public Vector3 Vector3Value { get; }
            public Vector4 Vector4Value { get; }
            public Quaternion QuaternionValue { get; }
            public Rect RectValue { get; }
        }

        private sealed class SceneSerializedComponentSnapshot
        {
            public SceneSerializedComponentSnapshot(IReadOnlyList<SceneSerializedPropertySnapshot> properties)
            {
                Properties = properties ?? Array.Empty<SceneSerializedPropertySnapshot>();
            }

            public IReadOnlyList<SceneSerializedPropertySnapshot> Properties { get; }
        }

        private sealed class ResourceLibrary
        {
            public ResourceData Fish;
            public ResourceData Shell;
            public ResourceData Seaweed;
            public ResourceData Herb;
            public ResourceData Mushroom;
            public ResourceData GlowMoss;
            public ResourceData WindHerb;
        }

        private sealed class RecipeLibrary
        {
            public RecipeData SushiSet;
            public RecipeData SeafoodSoup;
            public RecipeData HerbFishSoup;
            public RecipeData ForestBasket;
            public RecipeData GlowMossStew;
            public RecipeData WindHerbSalad;
        }

        private sealed class SpriteLibrary
        {
            public Sprite PlayerFront;
            public Sprite PlayerBack;
            public Sprite PlayerSide;
            public Sprite HubFloorTile;
            public Sprite HubFloorBackground;
            public Sprite HubWallBackground;
            public Sprite HubFrontOutline;
            public Sprite HubBar;
            public Sprite HubBarRight;
            public Sprite HubTableUnlocked;
            public Sprite HubUpgradeSlot;
            public Sprite HubTodayMenuBg;
            public Sprite HubTodayMenuItem1;
            public Sprite HubTodayMenuItem2;
            public Sprite HubTodayMenuItem3;
            public Sprite Portal;
            public Sprite Selector;
            public Sprite Counter;
            public Sprite Fish;
            public Sprite Shell;
            public Sprite Seaweed;
            public Sprite Herb;
            public Sprite Mushroom;
            public Sprite GlowMoss;
            public Sprite WindHerb;
            public Sprite Floor;
        }

        // 빌드 플로우와 Canvas 동기화 로직은 partial 파일로 분리합니다.

        private static void BuildHubScene(ResourceLibrary resources, RecipeLibrary recipes, SpriteLibrary sprites)
        {
            LoadSceneObjectOverrides(SceneRoot + "/Hub.unity");
            SaveSceneIfLoadedAndDirty(SceneRoot + "/Hub.unity");
            CacheHubPopupSceneImages(SceneRoot + "/Hub.unity");
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                const float mapWidth = HubRoomLayout.ScreenWidth;
                const float mapHeight = HubRoomLayout.ScreenHeight;

                GameObject gameManagerObject = CreateGameManager("Hub", "Beach", resources);
                GameObject player = CreatePlayer(HubRoomLayout.PlayerStartPosition, sprites, DefaultPlayerRootScale);
                BoxCollider2D cameraBounds = CreateCamera(
                    player.transform,
                    mapWidth,
                    mapHeight,
                    new Color(0.88f, 0.80f, 0.70f),
                    HubRoomLayout.ScreenOrthographicSize);
                ApplySceneTransformOverride(cameraBounds.transform, cameraBounds.name, HubRoomLayout.CameraPosition, Quaternion.identity, Vector3.one, useLocalSpace: false);
                cameraBounds.size = HubRoomLayout.CameraSize;
                ApplySceneComponentOverride(cameraBounds, cameraBounds.name);

                BoxCollider2D movementBounds = CreateMovementBounds(
                    "HubMovementBounds",
                    HubRoomLayout.MovementBoundsSize.x,
                    HubRoomLayout.MovementBoundsSize.y);
                ApplySceneTransformOverride(movementBounds.transform, movementBounds.name, HubRoomLayout.MovementBoundsPosition, Quaternion.identity, Vector3.one, useLocalSpace: false);
                AttachPlayerBoundsLimiter(player, movementBounds);

                CreateHubLayerRoots(out Transform hubBackgroundLayer, out Transform hubObjectLayer, out Transform hubForegroundLayer, out Transform hubTableGroup);
                BuildHubArtLayout(sprites, mapWidth, mapHeight, hubBackgroundLayer, hubObjectLayer, hubForegroundLayer);
                BuildHubTableLayout(sprites, hubTableGroup);
                BuildHubUpgradeSlotLayout(sprites, hubObjectLayer);
                BuildHubCollisionLayout();

                CreateSpawnPoint("HubEntry", HubRoomLayout.HubEntryPosition, "HubEntry");
                CreatePortal("GoToBeach", HubRoomLayout.GoToBeachPosition, sprites.Portal, "Beach", "BeachEntry", "바닷가로 이동", "바닷가로", true, ToolType.None, 0, "", HubRoomLayout.PortalScale);
                CreatePortal("GoToDeepForest", HubRoomLayout.GoToDeepForestPosition, sprites.Portal, "DeepForest", "ForestEntry", "깊은 숲으로 이동", "깊은 숲", true, ToolType.None, 0, "", HubRoomLayout.PortalScale);
                CreatePortal(
                    "GoToAbandonedMine",
                    HubRoomLayout.GoToAbandonedMinePosition,
                    sprites.Portal,
                    "AbandonedMine",
                    "MineEntry",
                    "폐광산으로 이동",
                    "폐광산",
                    true,
                    ToolType.Lantern,
                    0,
                    "작업대에서 랜턴을 준비해야 폐광산 안쪽을 안전하게 탐험할 수 있습니다.",
                    HubRoomLayout.PortalScale);
                CreatePortal("GoToWindHill", HubRoomLayout.GoToWindHillPosition, sprites.Portal, "WindHill", "WindHillEntry", "바람 언덕으로 이동", "바람 언덕", true, ToolType.None, 0, "", HubRoomLayout.PortalScale);
                CreateFeaturePad("BeachPortalPad", HubRoomLayout.BeachPortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.98f, 0.83f, 0.51f));
                CreateFeaturePad("ForestPortalPad", HubRoomLayout.ForestPortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.70f, 0.86f, 0.44f));
                CreateFeaturePad("MinePortalPad", HubRoomLayout.MinePortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.74f, 0.74f, 0.78f));
                CreateFeaturePad("WindPortalPad", HubRoomLayout.WindPortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.82f, 0.92f, 0.98f));

                RestaurantManager restaurantManager = CreateRestaurantManager(recipes);
                CreateRecipeSelector(HubRoomLayout.RecipeSelectorPosition, sprites.Selector, restaurantManager);
                CreateServiceCounter(HubRoomLayout.ServiceCounterPosition, sprites.Counter, restaurantManager);
                CreateHubTodayMenuBoard(HubRoomLayout.TodayMenuBoardPosition, sprites, restaurantManager, hubObjectLayer);
                StorageManager storageManager = gameManagerObject.GetComponent<StorageManager>();
                UpgradeManager upgradeManager = gameManagerObject.GetComponent<UpgradeManager>();
                CreateStorageStation("StorageStation", HubRoomLayout.StorageStationPosition, HubRoomLayout.StorageStationScale, sprites.Floor, new Color(0.86f, 0.70f, 0.36f), "창고", storageManager, StorageStationAction.StoreAll);
                CreateUpgradeStation(HubRoomLayout.UpgradeStationPosition, HubRoomLayout.UpgradeStationScale, sprites.Floor, new Color(0.54f, 0.72f, 0.78f), upgradeManager);

                HideHubInteractionPresentations();

                CreateUiCanvas(true);

                EnsureUiEventSystem();
                SaveGeneratedScene(SceneRoot + "/Hub.unity");
            }
            finally
            {
                CachedSceneTransforms.Clear();
                CachedSceneObjectActiveStates.Clear();
                CachedSceneComponentOverrides.Clear();
                CachedHubPopupSceneImages.Clear();
            }
        }

        private static void BuildBeachScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            LoadSceneObjectOverrides(SceneRoot + "/Beach.unity");
            SaveSceneIfLoadedAndDirty(SceneRoot + "/Beach.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 30f;
            const float mapHeight = 18f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-8.25f, -2.25f, 0f), sprites, DefaultPlayerRootScale);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.67f, 0.86f, 0.96f), 6.8f);
            BoxCollider2D movementBounds = CreateMovementBounds("BeachMovementBounds", mapWidth - 2.2f, mapHeight - 2.2f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("SandBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.93f, 0.85f, 0.64f), -20);
            CreateFloorZone("OceanBand", new Vector3(0f, 4.5f, 0f), new Vector3(mapWidth, 7f, 1f), sprites.Floor, new Color(0.50f, 0.76f, 0.92f), -19);
            CreateFloorZone("ShoreLine", new Vector3(0f, 1.5f, 0f), new Vector3(mapWidth, 1f, 1f), sprites.Floor, new Color(0.98f, 0.95f, 0.84f), -18);
            CreateFloorZone("Dock", new Vector3(-11f, -3f, 0f), new Vector3(4.5f, 3.4f, 1f), sprites.Floor, new Color(0.60f, 0.43f, 0.24f), -17);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.32f, 0.44f, 0.28f));
            CreateDecorBlock("RockClusterA", new Vector3(8f, -4.2f, 0f), new Vector3(3f, 1.6f, 1f), sprites.Floor, new Color(0.45f, 0.47f, 0.50f), 1);
            CreateDecorBlock("RockClusterB", new Vector3(10f, 1.5f, 0f), new Vector3(2.2f, 1.4f, 1f), sprites.Floor, new Color(0.45f, 0.47f, 0.50f), 1);
            CreateDecorBlock("GrassPatch", new Vector3(7f, 4.8f, 0f), new Vector3(4f, 2.2f, 1f), sprites.Floor, new Color(0.42f, 0.68f, 0.35f), 1);
            CreateDecorBlock("BoatMark", new Vector3(-13.1f, -2.0f, 0f), new Vector3(1.8f, 2.8f, 1f), sprites.Floor, new Color(0.87f, 0.38f, 0.21f), 2);

            CreateWorldLabel("BeachTitle", null, new Vector3(0f, 7.1f, 0f), "바닷가", Color.black, WorldTitleFontSize, 40);
            CreateSpawnPoint("BeachEntry", new Vector3(-8.25f, -2.25f, 0f), "BeachEntry");
            CreatePortal("ReturnToHub", new Vector3(-10.7f, -3.35f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGatherable("FishSpot01", new Vector3(-2f, 2.2f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
            CreateGatherable("FishSpot02", new Vector3(2.8f, 4f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
            CreateGatherable("ShellSpot01", new Vector3(-1f, -3f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
            CreateGatherable("ShellSpot02", new Vector3(4.5f, -1.8f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
            CreateGatherable("SeaweedSpot01", new Vector3(7f, 3.8f, 0f), sprites.Seaweed, resources.Seaweed, ToolType.Sickle, 1, 2, "해초");

            CreateUiCanvas(false);
            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/Beach.unity");
            CachedSceneTransforms.Clear();
            CachedSceneObjectActiveStates.Clear();
            CachedSceneComponentOverrides.Clear();
        }

        /// <summary>
        /// 허브 씬에 직접 지정한 팝업 Image 설정을 먼저 읽어 두면,
        /// 빌더가 씬을 다시 저장해도 수동으로 맞춘 이미지 소스를 유지할 수 있습니다.
        /// </summary>
        private static void CacheHubPopupSceneImages(string scenePath)
        {
            CachedHubPopupSceneImages.Clear();
            if (!TryDescribeManagedSceneFileIssue(scenePath, out _))
            {
                return;
            }

            Scene sourceScene = SceneManager.GetSceneByPath(scenePath);
            bool openedTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                return;
            }

            try
            {
                foreach (string objectName in HubPopupSceneImageNames)
                {
                    Transform target = FindNamedTransformInScene(sourceScene, objectName);
                    if (target == null || !target.TryGetComponent(out Image image))
                    {
                        continue;
                    }

                    CachedHubPopupSceneImages[objectName] = new SceneImageSnapshot(
                        image.sprite,
                        image.type,
                        image.color,
                        image.preserveAspect);
                }
            }
            finally
            {
                if (openedTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        private static void BuildDeepForestScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            LoadSceneObjectOverrides(SceneRoot + "/DeepForest.unity");
            SaveSceneIfLoadedAndDirty(SceneRoot + "/DeepForest.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 32f;
            const float mapHeight = 20f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.3f, -6.1f, 0f), sprites, DefaultPlayerRootScale);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.63f, 0.74f, 0.56f), 7.2f);

            BoxCollider2D movementBounds = CreateMovementBounds("ForestMovementBounds", mapWidth - 2.4f, mapHeight - 2.4f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("ForestBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.49f, 0.63f, 0.37f), -20);
            CreateFloorZone("ForestPath", new Vector3(-2f, -3.8f, 0f), new Vector3(23f, 3.4f, 1f), sprites.Floor, new Color(0.68f, 0.58f, 0.39f), -19);
            CreateFloorZone("ForestCanopy", new Vector3(3.5f, 4.8f, 0f), new Vector3(20f, 8f, 1f), sprites.Floor, new Color(0.36f, 0.54f, 0.24f), -18);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.23f, 0.33f, 0.18f));
            CreateDecorBlock("SwampPoolA", new Vector3(0f, 0.5f, 0f), new Vector3(4.8f, 2.6f, 1f), sprites.Floor, new Color(0.29f, 0.41f, 0.23f), 1);
            CreateDecorBlock("SwampPoolB", new Vector3(5.8f, 1.4f, 0f), new Vector3(3.6f, 2.2f, 1f), sprites.Floor, new Color(0.29f, 0.41f, 0.23f), 1);
            CreateDecorBlock("NarrowPathRock", new Vector3(8.5f, -2.4f, 0f), new Vector3(2.4f, 1.4f, 1f), sprites.Floor, new Color(0.35f, 0.38f, 0.34f), 2);

            CreateWorldLabel("ForestTitle", null, new Vector3(0f, 8.2f, 0f), "깊은 숲", Color.black, WorldTitleFontSize, 40);
            CreateSpawnPoint("ForestEntry", new Vector3(-10.3f, -6.1f, 0f), "ForestEntry");
            CreatePortal("ReturnFromForest", new Vector3(-13.6f, -6.15f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGuideTriggerZone("ForestGuide", new Vector3(-8.4f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "forest_intro", "숲은 갈림길과 늪지대 때문에 인벤토리보다 귀환 동선을 더 자주 확인해야 합니다.");
            CreateMovementModifierZone("ForestSwampZone", new Vector3(1.8f, 1f, 0f), new Vector2(9f, 4.2f), 0.55f, "늪지에서는 이동이 느려집니다. 좁은 길을 따라 움직이면 더 안전합니다.");

            CreateGatherable("HerbPatch01", new Vector3(-4f, -1.1f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
            CreateGatherable("HerbPatch02", new Vector3(4.8f, -3.6f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
            CreateGatherable("MushroomPatch01", new Vector3(2.6f, 4.1f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");
            CreateGatherable("MushroomPatch02", new Vector3(8.5f, 5.2f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");

            CreateUiCanvas(false);
            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/DeepForest.unity");
            CachedSceneTransforms.Clear();
            CachedSceneObjectActiveStates.Clear();
            CachedSceneComponentOverrides.Clear();
        }

        private static void BuildAbandonedMineScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            LoadSceneObjectOverrides(SceneRoot + "/AbandonedMine.unity");
            SaveSceneIfLoadedAndDirty(SceneRoot + "/AbandonedMine.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 32f;
            const float mapHeight = 20f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.7f, -5.95f, 0f), sprites, DefaultPlayerRootScale);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.18f, 0.19f, 0.22f), 7.2f);

            BoxCollider2D movementBounds = CreateMovementBounds("MineBounds", mapWidth - 2.4f, mapHeight - 2.4f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("MineBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.26f, 0.27f, 0.30f), -20);
            CreateFloorZone("MineTunnel", new Vector3(-2f, -4f, 0f), new Vector3(21f, 3.2f, 1f), sprites.Floor, new Color(0.39f, 0.34f, 0.28f), -19);
            CreateFloorZone("MineChamber", new Vector3(6.8f, 1.2f, 0f), new Vector3(14f, 10f, 1f), sprites.Floor, new Color(0.21f, 0.23f, 0.25f), -18);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.10f, 0.10f, 0.12f));
            CreateDecorBlock("MineRockA", new Vector3(1.2f, -0.4f, 0f), new Vector3(3.8f, 1.8f, 1f), sprites.Floor, new Color(0.34f, 0.34f, 0.36f), 2);
            CreateDecorBlock("MineRockB", new Vector3(8.8f, 5.4f, 0f), new Vector3(2.6f, 1.4f, 1f), sprites.Floor, new Color(0.34f, 0.34f, 0.36f), 2);
            CreateDecorBlock("MineRockC", new Vector3(10.8f, -2.5f, 0f), new Vector3(3.2f, 1.6f, 1f), sprites.Floor, new Color(0.28f, 0.29f, 0.32f), 2);

            CreateWorldLabel("MineTitle", null, new Vector3(0f, 8.1f, 0f), "폐광산", Color.white, WorldTitleFontSize, 40);
            CreateSpawnPoint("MineEntry", new Vector3(-10.7f, -6.0f, 0f), "MineEntry");
            CreatePortal("ReturnFromMine", new Vector3(-13.6f, -6.0f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGuideTriggerZone("MineGuide", new Vector3(-8.8f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "mine_intro", "폐광산은 어둡고 동선이 좁습니다. 안쪽으로 들어가기 전 귀환 길을 먼저 확인하세요.");
            CreateDarknessZone("MineDarkness", new Vector3(4.8f, 0.6f, 0f), new Vector2(18f, 10.8f));

            CreateGatherable("GlowMoss01", new Vector3(4.4f, 3.2f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
            CreateGatherable("GlowMoss02", new Vector3(8.2f, 1.0f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
            CreateGatherable("GlowMoss03", new Vector3(11.6f, 4.4f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");

            CreateUiCanvas(false);
            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/AbandonedMine.unity");
            CachedSceneTransforms.Clear();
            CachedSceneObjectActiveStates.Clear();
            CachedSceneComponentOverrides.Clear();
        }

        private static void BuildWindHillScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            LoadSceneObjectOverrides(SceneRoot + "/WindHill.unity");
            SaveSceneIfLoadedAndDirty(SceneRoot + "/WindHill.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 30f;
            const float mapHeight = 18f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.7f, -5.3f, 0f), sprites, DefaultPlayerRootScale);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.85f, 0.92f, 0.98f), 6.8f);

            BoxCollider2D movementBounds = CreateMovementBounds("WindHillBounds", mapWidth - 2.2f, mapHeight - 2.2f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("HillBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.79f, 0.86f, 0.66f), -20);
            CreateFloorZone("CliffBand", new Vector3(6.8f, 0f, 0f), new Vector3(12f, 14f, 1f), sprites.Floor, new Color(0.70f, 0.80f, 0.58f), -19);
            CreateFloorZone("WindLane", new Vector3(6.8f, 0.8f, 0f), new Vector3(10f, 6.4f, 1f), sprites.Floor, new Color(0.91f, 0.96f, 0.98f), -18);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.37f, 0.43f, 0.31f));
            CreateDecorBlock("CliffRockA", new Vector3(10.4f, -4.2f, 0f), new Vector3(3.2f, 1.5f, 1f), sprites.Floor, new Color(0.48f, 0.50f, 0.46f), 2);
            CreateDecorBlock("CliffRockB", new Vector3(4.8f, 4.8f, 0f), new Vector3(2.6f, 1.2f, 1f), sprites.Floor, new Color(0.48f, 0.50f, 0.46f), 2);

            CreateWorldLabel("WindHillTitle", null, new Vector3(0f, 7.1f, 0f), "바람 언덕", Color.black, WorldTitleFontSize, 40);
            CreateSpawnPoint("WindHillEntry", new Vector3(-10.7f, -5.35f, 0f), "WindHillEntry");
            CreateSpawnPoint("WindHillShortcutEntry", new Vector3(7.8f, 4.4f, 0f), "WindHillShortcutEntry");
            CreatePortal("ReturnFromWindHill", new Vector3(-13.6f, -5.15f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");
            CreatePortal(
                "WindHillShortcut",
                new Vector3(-6.8f, -2.8f, 0f),
                sprites.Portal,
                "WindHill",
                "WindHillShortcutEntry",
                "정상 지름길",
                "정상 지름길",
                true,
                ToolType.None,
                6,
                "평판 6을 모으면 바람 언덕의 지름길을 이용할 수 있습니다.");

            CreateGuideTriggerZone("WindGuide", new Vector3(-8.8f, -4.2f, 0f), new Vector2(3.4f, 2.2f), "wind_intro", "바람 언덕에서는 강풍이 꺼질 때 이동하는 편이 안전합니다.");
            CreateWindGustZone("WindLaneZone", new Vector3(6.8f, 0.8f, 0f), new Vector2(10f, 6.2f), Vector2.right, 2.8f, 2f, 1.5f);

            CreateGatherable("WindHerb01", new Vector3(2.4f, -2.2f, 0f), sprites.WindHerb, resources.WindHerb, ToolType.Sickle, 1, 2, "향초");
            CreateGatherable("WindHerb02", new Vector3(8.6f, 4.6f, 0f), sprites.WindHerb, resources.WindHerb, ToolType.Sickle, 1, 2, "향초");
            CreateGatherable("WindHerb03", new Vector3(10.8f, -0.2f, 0f), sprites.WindHerb, resources.WindHerb, ToolType.Sickle, 1, 2, "향초");

            CreateUiCanvas(false);

            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/WindHill.unity");
            CachedSceneTransforms.Clear();
            CachedSceneObjectActiveStates.Clear();
            CachedSceneComponentOverrides.Clear();
        }

        private static GameObject CreateGameManager(string hubSceneName, string explorationSceneName, ResourceLibrary resources)
        {
            GameObject go = new("GameManager");
            InventoryManager inventory = go.AddComponent<InventoryManager>();
            StorageManager storage = go.AddComponent<StorageManager>();
            EconomyManager economy = go.AddComponent<EconomyManager>();
            ToolManager toolManager = go.AddComponent<ToolManager>();
            DayCycleManager dayCycleManager = go.AddComponent<DayCycleManager>();
            UpgradeManager upgradeManager = go.AddComponent<UpgradeManager>();
            GameManager gameManager = go.AddComponent<GameManager>();

            SerializedObject gameManagerSo = new(gameManager);
            gameManagerSo.FindProperty("hubSceneName").stringValue = hubSceneName;
            gameManagerSo.FindProperty("firstExplorationSceneName").stringValue = explorationSceneName;
            gameManagerSo.FindProperty("inventoryManager").objectReferenceValue = inventory;
            gameManagerSo.FindProperty("storageManager").objectReferenceValue = storage;
            gameManagerSo.FindProperty("economyManager").objectReferenceValue = economy;
            gameManagerSo.FindProperty("toolManager").objectReferenceValue = toolManager;
            gameManagerSo.FindProperty("dayCycleManager").objectReferenceValue = dayCycleManager;
            gameManagerSo.FindProperty("upgradeManager").objectReferenceValue = upgradeManager;
            gameManagerSo.ApplyModifiedPropertiesWithoutUndo();

            if (resources != null)
            {
                SerializedObject upgradeSo = new(upgradeManager);
                SerializedProperty inventoryCostsProperty = upgradeSo.FindProperty("inventoryUpgradeCosts");
                inventoryCostsProperty.arraySize = 2;

                SerializedProperty firstCost = inventoryCostsProperty.GetArrayElementAtIndex(0);
                firstCost.FindPropertyRelative("goldCost").intValue = 30;
                firstCost.FindPropertyRelative("requiredResource").objectReferenceValue = resources.Shell;
                firstCost.FindPropertyRelative("requiredAmount").intValue = 3;
                firstCost.FindPropertyRelative("description").stringValue = "조개 상자를 묶어 12칸까지 넓힙니다.";

                SerializedProperty secondCost = inventoryCostsProperty.GetArrayElementAtIndex(1);
                secondCost.FindPropertyRelative("goldCost").intValue = 65;
                secondCost.FindPropertyRelative("requiredResource").objectReferenceValue = resources.Herb;
                secondCost.FindPropertyRelative("requiredAmount").intValue = 4;
                secondCost.FindPropertyRelative("description").stringValue = "정리 상자를 더 달아 16칸까지 확장합니다.";

                SerializedProperty toolCostsProperty = upgradeSo.FindProperty("toolUnlockCosts");
                toolCostsProperty.arraySize = 1;

                SerializedProperty lanternCost = toolCostsProperty.GetArrayElementAtIndex(0);
                lanternCost.FindPropertyRelative("toolType").enumValueIndex = (int)ToolType.Lantern;
                lanternCost.FindPropertyRelative("goldCost").intValue = 45;
                lanternCost.FindPropertyRelative("requiredResource").objectReferenceValue = resources.Mushroom;
                lanternCost.FindPropertyRelative("requiredAmount").intValue = 2;
                lanternCost.FindPropertyRelative("description").stringValue = "폐광산처럼 어두운 지역 진입에 필요합니다.";

                upgradeSo.ApplyModifiedPropertiesWithoutUndo();
            }

            ApplySceneComponentOverride(inventory, go.name);
            ApplySceneComponentOverride(storage, go.name);
            ApplySceneComponentOverride(economy, go.name);
            ApplySceneComponentOverride(toolManager, go.name);
            ApplySceneComponentOverride(dayCycleManager, go.name);
            ApplySceneComponentOverride(upgradeManager, go.name);
            ApplySceneComponentOverride(gameManager, go.name);
            ApplySceneActiveOverride(go, go.name);

            return go;
        }

        private static void LoadSceneObjectOverrides(string scenePath)
        {
            CachedSceneTransforms.Clear();
            CachedSceneObjectActiveStates.Clear();
            CachedSceneComponentOverrides.Clear();

            if (!TryDescribeManagedSceneFileIssue(scenePath, out _))
            {
                return;
            }

            Scene sourceScene = SceneManager.GetSceneByPath(scenePath);
            bool openedTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                return;
            }

            try
            {
                foreach (GameObject root in sourceScene.GetRootGameObjects())
                {
                    if (root == null)
                    {
                        continue;
                    }

                    CacheSceneObjectOverridesRecursive(root.transform);
                }
            }
            finally
            {
                if (openedTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        private static void CacheSceneObjectOverridesRecursive(Transform current)
        {
            if (current == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(current.name))
            {
                CachedSceneObjectActiveStates[current.name] = current.gameObject.activeSelf;
                CachedSceneTransforms[current.name] = new SceneTransformSnapshot(
                    current.position,
                    current.rotation,
                    current.localPosition,
                    current.localRotation,
                    current.localScale);

                CacheSupportedComponentOverrides(current.gameObject);
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CacheSceneObjectOverridesRecursive(current.GetChild(index));
            }
        }

        private static void CacheSupportedComponentOverrides(GameObject gameObject)
        {
            if (gameObject == null || string.IsNullOrWhiteSpace(gameObject.name))
            {
                return;
            }

            Dictionary<Type, SceneSerializedComponentSnapshot> objectOverrides = null;

            foreach (Type componentType in SceneOverrideComponentTypes)
            {
                Component component = gameObject.GetComponent(componentType);
                if (component == null || !TryCreateSceneSerializedComponentSnapshot(component, out SceneSerializedComponentSnapshot snapshot))
                {
                    continue;
                }

                objectOverrides ??= new Dictionary<Type, SceneSerializedComponentSnapshot>();
                objectOverrides[componentType] = snapshot;
            }

            if (objectOverrides != null && objectOverrides.Count > 0)
            {
                CachedSceneComponentOverrides[gameObject.name] = objectOverrides;
            }
        }

        private static bool TryCreateSceneSerializedComponentSnapshot(
            Component component,
            out SceneSerializedComponentSnapshot snapshot)
        {
            snapshot = null;

            if (component == null)
            {
                return false;
            }

            SerializedObject serializedObject = new(component);
            SerializedProperty iterator = serializedObject.GetIterator();
            List<SceneSerializedPropertySnapshot> properties = new();
            while (iterator.NextVisible(true))
            {
                if (ShouldSkipSceneSerializedProperty(iterator)
                    || !TryCreateSceneSerializedPropertySnapshot(iterator, out SceneSerializedPropertySnapshot propertySnapshot))
                {
                    continue;
                }

                properties.Add(propertySnapshot);
            }

            if (properties.Count == 0)
            {
                return false;
            }

            snapshot = new SceneSerializedComponentSnapshot(properties);
            return true;
        }

        private static bool ShouldSkipSceneSerializedProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return true;
            }

            string propertyPath = property.propertyPath;
            return string.IsNullOrWhiteSpace(propertyPath)
                   || string.Equals(propertyPath, "m_Script", StringComparison.Ordinal)
                   || string.Equals(propertyPath, "m_GameObject", StringComparison.Ordinal)
                   || string.Equals(propertyPath, "m_EditorClassIdentifier", StringComparison.Ordinal);
        }

        private static bool TryCreateSceneSerializedPropertySnapshot(
            SerializedProperty property,
            out SceneSerializedPropertySnapshot snapshot)
        {
            snapshot = default;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Boolean, boolValue: property.boolValue);
                    return true;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.LayerMask:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Integer, intValue: property.intValue);
                    return true;

                case SerializedPropertyType.Float:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Float, floatValue: property.floatValue);
                    return true;

                case SerializedPropertyType.String:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.String, stringValue: property.stringValue);
                    return true;

                case SerializedPropertyType.Color:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Color, colorValue: property.colorValue);
                    return true;

                case SerializedPropertyType.ObjectReference:
                    if (property.objectReferenceValue == null || !EditorUtility.IsPersistent(property.objectReferenceValue))
                    {
                        return false;
                    }

                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.ObjectReference, objectReferenceValue: property.objectReferenceValue);
                    return true;

                case SerializedPropertyType.Vector2:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Vector2, vector2Value: property.vector2Value);
                    return true;

                case SerializedPropertyType.Vector3:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Vector3, vector3Value: property.vector3Value);
                    return true;

                case SerializedPropertyType.Vector4:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Vector4, vector4Value: property.vector4Value);
                    return true;

                case SerializedPropertyType.Quaternion:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Quaternion, quaternionValue: property.quaternionValue);
                    return true;

                case SerializedPropertyType.Rect:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Rect, rectValue: property.rectValue);
                    return true;

                default:
                    return false;
            }
        }

        private static void ApplySceneComponentOverride(Component component, string objectName)
        {
            if (component == null
                || string.IsNullOrWhiteSpace(objectName)
                || !CachedSceneComponentOverrides.TryGetValue(objectName, out Dictionary<Type, SceneSerializedComponentSnapshot> objectOverrides)
                || !objectOverrides.TryGetValue(component.GetType(), out SceneSerializedComponentSnapshot snapshot))
            {
                return;
            }

            SerializedObject serializedObject = new(component);
            bool changed = false;

            foreach (SceneSerializedPropertySnapshot propertySnapshot in snapshot.Properties)
            {
                SerializedProperty property = serializedObject.FindProperty(propertySnapshot.PropertyPath);
                if (property == null)
                {
                    continue;
                }

                ApplySceneSerializedPropertySnapshot(property, propertySnapshot);
                changed = true;
            }

            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ApplySceneSerializedPropertySnapshot(
            SerializedProperty property,
            SceneSerializedPropertySnapshot snapshot)
        {
            switch (snapshot.ValueKind)
            {
                case SceneSerializedValueKind.Boolean:
                    property.boolValue = snapshot.BoolValue;
                    break;

                case SceneSerializedValueKind.Integer:
                    property.intValue = snapshot.IntValue;
                    break;

                case SceneSerializedValueKind.Float:
                    property.floatValue = snapshot.FloatValue;
                    break;

                case SceneSerializedValueKind.String:
                    property.stringValue = snapshot.StringValue;
                    break;

                case SceneSerializedValueKind.Color:
                    property.colorValue = snapshot.ColorValue;
                    break;

                case SceneSerializedValueKind.ObjectReference:
                    property.objectReferenceValue = snapshot.ObjectReferenceValue;
                    break;

                case SceneSerializedValueKind.Vector2:
                    property.vector2Value = snapshot.Vector2Value;
                    break;

                case SceneSerializedValueKind.Vector3:
                    property.vector3Value = snapshot.Vector3Value;
                    break;

                case SceneSerializedValueKind.Vector4:
                    property.vector4Value = snapshot.Vector4Value;
                    break;

                case SceneSerializedValueKind.Quaternion:
                    property.quaternionValue = snapshot.QuaternionValue;
                    break;

                case SceneSerializedValueKind.Rect:
                    property.rectValue = snapshot.RectValue;
                    break;
            }
        }

        private static void ApplySceneActiveOverride(GameObject gameObject, string objectName)
        {
            if (gameObject == null
                || string.IsNullOrWhiteSpace(objectName)
                || !CachedSceneObjectActiveStates.TryGetValue(objectName, out bool isActive))
            {
                return;
            }

            gameObject.SetActive(isActive);
        }

        private static Vector3 ResolveSceneObjectScale(string objectName, Vector3 fallbackScale)
        {
            if (string.IsNullOrWhiteSpace(objectName)
                || !CachedSceneTransforms.TryGetValue(objectName, out SceneTransformSnapshot snapshot)
                || Mathf.Approximately(snapshot.LocalScale.x, 0f)
                || Mathf.Approximately(snapshot.LocalScale.y, 0f))
            {
                return fallbackScale;
            }

            return snapshot.LocalScale;
        }

        private static void ApplySceneTransformOverride(
            Transform target,
            string objectName,
            Vector3 fallbackPosition,
            Quaternion fallbackRotation,
            Vector3 fallbackScale,
            bool useLocalSpace)
        {
            if (target == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(objectName)
                && CachedSceneTransforms.TryGetValue(objectName, out SceneTransformSnapshot snapshot))
            {
                if (useLocalSpace)
                {
                    target.localPosition = snapshot.LocalPosition;
                    target.localRotation = snapshot.LocalRotation;
                }
                else
                {
                    target.position = snapshot.Position;
                    target.rotation = snapshot.Rotation;
                }

                target.localScale = ResolveSceneObjectScale(objectName, fallbackScale);
                return;
            }

            if (useLocalSpace)
            {
                target.localPosition = fallbackPosition;
                target.localRotation = fallbackRotation;
            }
            else
            {
                target.position = fallbackPosition;
                target.rotation = fallbackRotation;
            }

            target.localScale = fallbackScale;
        }

        private static GameObject CreatePlayer(Vector3 position, SpriteLibrary sprites, Vector3 rootScale)
        {
            GameObject player = new("Jonggu");
            ApplySceneTransformOverride(player.transform, player.name, position, Quaternion.identity, rootScale, useLocalSpace: false);

            Sprite frontSprite = sprites.PlayerFront != null ? sprites.PlayerFront : sprites.PlayerSide;
            GameObject shadow = CreateDecorBlock("Shadow", Vector3.zero, new Vector3(0.46f, 0.14f, 1f), sprites.Floor, new Color(0f, 0f, 0f, 0.20f), 9, player.transform);
            ApplySceneTransformOverride(shadow.transform, shadow.name, new Vector3(0f, -0.28f, 0f), Quaternion.identity, new Vector3(0.46f, 0.14f, 1f), useLocalSpace: true);

            // 물리는 루트에 유지하고, 맵 크기 보정은 비주얼 자식만 스케일해서 처리합니다.
            GameObject visualRoot = new("PlayerVisual");
            visualRoot.transform.SetParent(player.transform, false);
            ApplySceneTransformOverride(visualRoot.transform, visualRoot.name, Vector3.zero, Quaternion.identity, Vector3.one * PlayerVisualScale, useLocalSpace: true);

            SpriteRenderer renderer = visualRoot.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visualRoot.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = frontSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 12;
            ApplySceneComponentOverride(renderer, visualRoot.name);

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = player.AddComponent<Rigidbody2D>();
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ApplySceneComponentOverride(body, player.name);

            CapsuleCollider2D collider = player.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                collider = player.AddComponent<CapsuleCollider2D>();
            }

            collider.size = new Vector2(0.9f, 1.05f);
            ApplySceneComponentOverride(collider, player.name);

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = player.AddComponent<PlayerController>();
            }

            ApplySceneComponentOverride(controller, player.name);

            PlayerDirectionalSprite directionalSprite = player.GetComponent<PlayerDirectionalSprite>();
            if (directionalSprite == null)
            {
                directionalSprite = player.AddComponent<PlayerDirectionalSprite>();
            }

            directionalSprite.Configure(renderer, sprites.PlayerFront, sprites.PlayerBack, sprites.PlayerSide);
            ApplySceneComponentOverride(directionalSprite, player.name);

            GameObject interactionRange = new("InteractionRange");
            interactionRange.transform.SetParent(player.transform, false);
            ApplySceneTransformOverride(interactionRange.transform, interactionRange.name, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: true);
            CircleCollider2D rangeCollider = interactionRange.AddComponent<CircleCollider2D>();
            rangeCollider.isTrigger = true;
            rangeCollider.radius = 1.35f;
            ApplySceneComponentOverride(rangeCollider, interactionRange.name);
            InteractionDetector detector = interactionRange.AddComponent<InteractionDetector>();
            ApplySceneComponentOverride(detector, interactionRange.name);

            SerializedObject controllerSo = new(controller);
            controllerSo.FindProperty("interactionDetector").objectReferenceValue = detector;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(controller, player.name);
            ApplySceneActiveOverride(player, player.name);
            ApplySceneActiveOverride(visualRoot, visualRoot.name);
            ApplySceneActiveOverride(interactionRange, interactionRange.name);

            return player;
        }

        private static BoxCollider2D CreateCamera(Transform target, float mapWidth, float mapHeight, Color backgroundColor, float orthographicSize)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            ApplySceneTransformOverride(cameraObject.transform, cameraObject.name, new Vector3(0f, 0f, -10f), Quaternion.identity, Vector3.one, useLocalSpace: false);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.backgroundColor = backgroundColor;
            ApplySceneComponentOverride(camera, cameraObject.name);

            AudioListener listener = cameraObject.AddComponent<AudioListener>();
            ApplySceneComponentOverride(listener, cameraObject.name);

            GameObject boundsObject = new("CameraBounds");
            ApplySceneTransformOverride(boundsObject.transform, boundsObject.name, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: false);
            BoxCollider2D bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(mapWidth, mapHeight);
            ApplySceneComponentOverride(bounds, boundsObject.name);

            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            SerializedObject followSo = new(follow);
            followSo.FindProperty("target").objectReferenceValue = target;
            followSo.FindProperty("mapBounds").objectReferenceValue = bounds;
            followSo.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(follow, cameraObject.name);
            ApplySceneActiveOverride(cameraObject, cameraObject.name);
            ApplySceneActiveOverride(boundsObject, boundsObject.name);
            return bounds;
        }

        private static void CreateBoundaryWalls(float mapWidth, float mapHeight, Sprite sprite, Color color)
        {
            const float thickness = 0.8f;

            CreateWall("TopWall", new Vector3(0f, mapHeight * 0.5f, 0f), new Vector3(mapWidth + thickness, thickness, 1f), sprite, color);
            CreateWall("BottomWall", new Vector3(0f, -mapHeight * 0.5f, 0f), new Vector3(mapWidth + thickness, thickness, 1f), sprite, color);
            CreateWall("LeftWall", new Vector3(-mapWidth * 0.5f, 0f, 0f), new Vector3(thickness, mapHeight + thickness, 1f), sprite, color);
            CreateWall("RightWall", new Vector3(mapWidth * 0.5f, 0f, 0f), new Vector3(thickness, mapHeight + thickness, 1f), sprite, color);
        }

        private static void CreateWall(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color)
        {
            GameObject wall = CreateDecorBlock(objectName, position, scale, sprite, color, 15);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            ApplySceneComponentOverride(collider, objectName);
        }

        private static void CreateInvisibleWall(string objectName, Vector3 position, Vector3 scale, Transform parent = null)
        {
            GameObject wall = new(objectName);
            if (parent != null)
            {
                wall.transform.SetParent(parent, false);
                ApplySceneTransformOverride(wall.transform, objectName, position, Quaternion.identity, scale, useLocalSpace: true);
            }
            else
            {
                ApplySceneTransformOverride(wall.transform, objectName, position, Quaternion.identity, scale, useLocalSpace: false);
            }

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            ApplySceneComponentOverride(collider, objectName);
            ApplySceneActiveOverride(wall, objectName);
        }

        private static void CreatePortal(
            string objectName,
            Vector3 position,
            Sprite sprite,
            string targetSceneName,
            string targetSpawnPointId,
            string promptLabel,
            string worldLabel = null,
            bool requireMorningExplore = true,
            ToolType requiredToolType = ToolType.None,
            int requiredReputation = 0,
            string lockedGuideText = "",
            Vector3? sizeOverride = null)
        {
            Vector3 portalSize = sizeOverride ?? new Vector3(1.6f, 2.2f, 1f);
            GameObject portal = CreateDecorBlock(objectName, position, portalSize, sprite, new Color(0.94f, 0.50f, 0.18f), 7);
            BoxCollider2D collider = portal.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;
            ApplySceneComponentOverride(collider, objectName);

            ScenePortal scenePortal = portal.AddComponent<ScenePortal>();
            SerializedObject so = new(scenePortal);
            so.FindProperty("targetSceneName").stringValue = targetSceneName;
            so.FindProperty("targetSpawnPointId").stringValue = targetSpawnPointId;
            so.FindProperty("promptLabel").stringValue = promptLabel;
            so.FindProperty("requireMorningExplore").boolValue = requireMorningExplore;
            so.FindProperty("requiredToolType").enumValueIndex = (int)requiredToolType;
            so.FindProperty("requiredReputation").intValue = requiredReputation;
            so.FindProperty("lockedGuideText").stringValue = lockedGuideText;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(scenePortal, objectName);
            ApplySceneActiveOverride(portal, objectName);

            string displayLabel = string.IsNullOrWhiteSpace(worldLabel) ? promptLabel : worldLabel;
            CreateWorldLabel(objectName + "_Label", portal.transform, new Vector3(0f, 0.82f, 0f), displayLabel, Color.black, WorldLabelFontSize, 50);
        }

        private static RestaurantManager CreateRestaurantManager(RecipeLibrary recipes)
        {
            GameObject go = new("RestaurantManager");
            RestaurantManager manager = go.AddComponent<RestaurantManager>();

            SerializedObject so = new(manager);
            SerializedProperty recipesProperty = so.FindProperty("availableRecipes");
            recipesProperty.arraySize = 6;
            recipesProperty.GetArrayElementAtIndex(0).objectReferenceValue = recipes.SushiSet;
            recipesProperty.GetArrayElementAtIndex(1).objectReferenceValue = recipes.SeafoodSoup;
            recipesProperty.GetArrayElementAtIndex(2).objectReferenceValue = recipes.HerbFishSoup;
            recipesProperty.GetArrayElementAtIndex(3).objectReferenceValue = recipes.ForestBasket;
            recipesProperty.GetArrayElementAtIndex(4).objectReferenceValue = recipes.GlowMossStew;
            recipesProperty.GetArrayElementAtIndex(5).objectReferenceValue = recipes.WindHerbSalad;
            so.FindProperty("serviceCapacity").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(manager, go.name);
            ApplySceneActiveOverride(go, go.name);

            return manager;
        }

        private static void CreateRecipeSelector(Vector3 position, Sprite sprite, RestaurantManager restaurantManager)
        {
            GameObject go = CreateDecorBlock("RecipeSelector", position, new Vector3(1.55f, 1.55f, 1f), sprite, new Color(0.98f, 0.84f, 0.18f), 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;
            ApplySceneComponentOverride(collider, go.name);

            RecipeSelectorStation station = go.AddComponent<RecipeSelectorStation>();
            SerializedObject so = new(station);
            so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
            so.FindProperty("promptLabel").stringValue = "메뉴 변경";
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(station, go.name);
            ApplySceneActiveOverride(go, go.name);

            CreateWorldLabel("RecipeSelectorLabel", go.transform, new Vector3(0f, 0.80f, 0f), "메뉴판", Color.black, WorldLabelFontSize, 50);
        }

        private static void CreateServiceCounter(Vector3 position, Sprite sprite, RestaurantManager restaurantManager)
        {
            GameObject go = CreateDecorBlock("ServiceCounter", position, new Vector3(1.95f, 1.55f, 1f), sprite, new Color(0.82f, 0.30f, 0.22f), 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;
            ApplySceneComponentOverride(collider, go.name);

            ServiceCounterStation station = go.AddComponent<ServiceCounterStation>();
            SerializedObject so = new(station);
            so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
            so.FindProperty("promptLabel").stringValue = "영업 시작";
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(station, go.name);
            ApplySceneActiveOverride(go, go.name);

            CreateWorldLabel("ServiceCounterLabel", go.transform, new Vector3(0f, 0.80f, 0f), "영업대", Color.black, WorldLabelFontSize, 50);
        }

        /// <summary>
        /// 허브 아트는 레이어 루트와 테이블 그룹 루트를 먼저 만든 뒤 배치한다.
        /// </summary>
        private static void CreateHubLayerRoots(out Transform backgroundLayer, out Transform objectLayer, out Transform foregroundLayer, out Transform tableGroup)
        {
            GameObject hubArtRoot = new("HubArtRoot");
            GameObject backgroundObject = new("HubBackgroundLayer");
            backgroundObject.transform.SetParent(hubArtRoot.transform, false);
            GameObject objectObject = new("HubObjectLayer");
            objectObject.transform.SetParent(hubArtRoot.transform, false);
            GameObject foregroundObject = new("HubForegroundLayer");
            foregroundObject.transform.SetParent(hubArtRoot.transform, false);
            GameObject tableObject = new(HubRoomLayout.TableRootObjectName);
            tableObject.transform.SetParent(objectObject.transform, false);
            ApplySceneTransformOverride(hubArtRoot.transform, hubArtRoot.name, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: false);
            ApplySceneTransformOverride(backgroundObject.transform, backgroundObject.name, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: true);
            ApplySceneTransformOverride(objectObject.transform, objectObject.name, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: true);
            ApplySceneTransformOverride(foregroundObject.transform, foregroundObject.name, Vector3.zero, Quaternion.identity, Vector3.one, useLocalSpace: true);
            ApplySceneTransformOverride(tableObject.transform, tableObject.name, HubRoomLayout.TableGroupPosition, Quaternion.identity, Vector3.one, useLocalSpace: true);
            ApplySceneActiveOverride(hubArtRoot, hubArtRoot.name);
            ApplySceneActiveOverride(backgroundObject, backgroundObject.name);
            ApplySceneActiveOverride(objectObject, objectObject.name);
            ApplySceneActiveOverride(foregroundObject, foregroundObject.name);
            ApplySceneActiveOverride(tableObject, tableObject.name);

            backgroundLayer = backgroundObject.transform;
            objectLayer = objectObject.transform;
            foregroundLayer = foregroundObject.transform;
            tableGroup = tableObject.transform;
        }

        /// <summary>
        /// 허브 고정 아트와 가격 텍스트, 바닥 표지판, 오늘의 메뉴 보드를 한 번에 만든다.
        /// </summary>
        private static void BuildHubArtLayout(
            SpriteLibrary sprites,
            float mapWidth,
            float mapHeight,
            Transform backgroundLayer,
            Transform objectLayer,
            Transform foregroundLayer)
        {
            foreach (HubRoomLayout.HubArtPlacement placement in HubRoomLayout.ArtPlacements)
            {
                Transform parent = ResolveHubArtParent(placement.Anchor, backgroundLayer, objectLayer, foregroundLayer);
                Sprite sprite = ResolveHubArtSprite(sprites, placement.SpriteId);

                if (placement.SpriteId == HubRoomLayout.HubArtSpriteId.FloorBackground && sprites.HubFloorTile != null)
                {
                    CreateHubTiledArtSprite(
                        placement.ObjectName,
                        placement.LocalPosition,
                        new Vector2(mapWidth, mapHeight),
                        HubRoomLayout.FloorTileScale,
                        sprites.HubFloorTile,
                        placement.SortingOrder,
                        parent);
                    continue;
                }

                if (placement.SpriteId == HubRoomLayout.HubArtSpriteId.Bar && sprite != null)
                {
                    CreateHubSplitBarArt(
                        placement.ObjectName,
                        placement.LocalPosition,
                        sprite,
                        sprites.HubBarRight,
                        placement.SortingOrder,
                        parent);
                    continue;
                }

                if (placement.SpriteId == HubRoomLayout.HubArtSpriteId.FloorBackground && sprite == null)
                {
                    GameObject fallbackFloor = CreateFloorZone(
                        placement.ObjectName,
                        HubRoomLayout.BackgroundPosition,
                        new Vector3(mapWidth, mapHeight, 1f),
                        sprites.Floor,
                        new Color(0.95f, 0.91f, 0.82f),
                        placement.SortingOrder);
                    fallbackFloor.transform.SetParent(parent, false);
                    continue;
                }

                CreateHubArtSprite(placement.ObjectName, placement.LocalPosition, sprite, placement.SortingOrder, parent);
            }

            foreach (HubRoomLayout.HubFloorSignPlacement placement in HubRoomLayout.FloorSignPlacements)
            {
                CreateHubFloorSign(placement, sprites.Floor, objectLayer);
            }
        }

        /// <summary>
        /// 테이블은 개별 그룹 아래에 스프라이트와 콜라이더를 함께 두어,
        /// 이후 위치 커스터마이즈가 와도 그룹 이동만으로 정렬을 유지한다.
        /// </summary>
        private static void BuildHubTableLayout(SpriteLibrary sprites, Transform tableGroup)
        {
            foreach (HubRoomLayout.HubTablePlacement placement in HubRoomLayout.TablePlacements)
            {
                GameObject groupObject = new(placement.GroupObjectName);
                groupObject.transform.SetParent(tableGroup, false);
                ApplySceneTransformOverride(groupObject.transform, groupObject.name, placement.LocalPosition, Quaternion.identity, Vector3.one, useLocalSpace: true);
                ApplySceneActiveOverride(groupObject, groupObject.name);

                GameObject tableObject = CreateHubArtSprite(placement.TableObjectName, Vector3.zero, sprites.HubTableUnlocked, HubRoomLayout.ObjectSortingOrder, groupObject.transform);
                Transform colliderParent = tableObject != null ? tableObject.transform : groupObject.transform;
                CreateInvisibleWall(placement.ColliderObjectName, placement.ColliderLocalPosition, HubRoomLayout.TableColliderScale, colliderParent);
            }
        }

        /// <summary>
        /// 업그레이드 슬롯은 위치, 스프라이트, 비용 표시를 한 스펙으로 관리해
        /// 이후 위치별 해금 비용 로직을 같은 기준으로 연결할 수 있게 만든다.
        /// </summary>
        private static void BuildHubUpgradeSlotLayout(SpriteLibrary sprites, Transform objectLayer)
        {
            foreach (HubRoomLayout.HubUpgradeSlotPlacement placement in HubRoomLayout.UpgradeSlotPlacements)
            {
                Sprite sprite = ResolveHubArtSprite(sprites, placement.SpriteId);
                GameObject slotObject = CreateHubArtSprite(placement.SlotObjectName, placement.Position, sprite, HubRoomLayout.ObjectSortingOrder, objectLayer);
                Transform priceParent = slotObject != null ? slotObject.transform : objectLayer;
                CreateHubUpgradePriceText(placement.PriceObjectName, priceParent, HubRoomLayout.UpgradePriceTextLocalOffset, placement.GoldCostLabel);
            }
        }

        /// <summary>
        /// 허브 고정 충돌은 각 월드 오브젝트 하위로 붙여 씬 Hierarchy에서 아트와 함께 보이게 맞춘다.
        /// </summary>
        private static void BuildHubCollisionLayout()
        {
            foreach (HubRoomLayout.HubColliderPlacement placement in HubRoomLayout.ColliderPlacements)
            {
                Transform parent = GameObject.Find(placement.ParentObjectName)?.transform;
                CreateInvisibleWall(placement.ObjectName, placement.LocalPosition, placement.Scale, parent);
            }
        }

        private static Transform ResolveHubArtParent(
            HubRoomLayout.HubArtAnchor anchor,
            Transform backgroundLayer,
            Transform objectLayer,
            Transform foregroundLayer)
        {
            return anchor switch
            {
                HubRoomLayout.HubArtAnchor.BackgroundLayer => backgroundLayer,
                HubRoomLayout.HubArtAnchor.ObjectLayer => objectLayer,
                HubRoomLayout.HubArtAnchor.ForegroundLayer => foregroundLayer,
                _ => objectLayer
            };
        }

        private static Sprite ResolveHubArtSprite(SpriteLibrary sprites, HubRoomLayout.HubArtSpriteId spriteId)
        {
            return spriteId switch
            {
                HubRoomLayout.HubArtSpriteId.FloorBackground => sprites.HubFloorBackground,
                HubRoomLayout.HubArtSpriteId.WallBackground => sprites.HubWallBackground,
                HubRoomLayout.HubArtSpriteId.Bar => sprites.HubBar,
                HubRoomLayout.HubArtSpriteId.TableUnlocked => sprites.HubTableUnlocked,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotLeft => sprites.HubUpgradeSlot,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotCenter => sprites.HubUpgradeSlot,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotRight => sprites.HubUpgradeSlot,
                HubRoomLayout.HubArtSpriteId.FrontOutline => sprites.HubFrontOutline,
                _ => null
            };
        }

        private static void HideHubInteractionPresentations()
        {
            foreach (string objectName in HubRoomLayout.HiddenInteractionObjectNames)
            {
                HideWorldInteractionPresentation(GameObject.Find(objectName));
            }
        }

        /// <summary>
        /// 허브 벽면의 메뉴판을 PSD 배치 기준으로 다시 만든다.
        /// 제목 텍스트와 슬롯 배경, 음식 아이콘을 각각 월드 오브젝트로 분리해 허브 아트처럼 유지한다.
        /// </summary>
        private static void CreateHubTodayMenuBoard(Vector3 position, SpriteLibrary sprites, RestaurantManager restaurantManager, Transform parent = null)
        {
            GameObject boardRoot = new("HubTodayMenuBoard");
            if (parent != null)
            {
                boardRoot.transform.SetParent(parent, false);
                ApplySceneTransformOverride(boardRoot.transform, boardRoot.name, position, Quaternion.identity, Vector3.one, useLocalSpace: true);
            }
            else
            {
                ApplySceneTransformOverride(boardRoot.transform, boardRoot.name, position, Quaternion.identity, Vector3.one, useLocalSpace: false);
            }

            ApplySceneActiveOverride(boardRoot, boardRoot.name);

            CreateWorldTextObject(
                "HubTodayMenuHeaderShadow",
                boardRoot.transform,
                HubRoomLayout.TodayMenuHeaderLabelLocalPosition + HubRoomLayout.TodayMenuHeaderShadowLocalOffset,
                "오늘의 메뉴",
                HubRoomLayout.TodayMenuHeaderShadowColor,
                HubRoomLayout.TodayMenuHeaderFontSize,
                HubRoomLayout.TodayMenuItemSortingOrder,
                labelScale: HubRoomLayout.TodayMenuHeaderTextScale,
                fontStyle: FontStyles.Bold,
                characterSpacing: 0.04f);

            TextMeshPro headerLabel = CreateWorldTextObject(
                "HubTodayMenuHeaderLabel",
                boardRoot.transform,
                HubRoomLayout.TodayMenuHeaderLabelLocalPosition,
                "오늘의 메뉴",
                HubRoomLayout.TodayMenuHeaderTextColor,
                HubRoomLayout.TodayMenuHeaderFontSize,
                HubRoomLayout.TodayMenuTextSortingOrder,
                labelScale: HubRoomLayout.TodayMenuHeaderTextScale,
                fontStyle: FontStyles.Bold,
                characterSpacing: 0.04f);

            Sprite[] itemSprites =
            {
                sprites.HubTodayMenuItem2,
                sprites.HubTodayMenuItem1,
                sprites.HubTodayMenuItem3
            };

            SpriteRenderer[] entryBackdrops = new SpriteRenderer[HubRoomLayout.TodayMenuEntryLocalPositions.Length];
            SpriteRenderer[] entryIcons = new SpriteRenderer[HubRoomLayout.TodayMenuEntryLocalPositions.Length];

            for (int i = 0; i < HubRoomLayout.TodayMenuEntryLocalPositions.Length; i++)
            {
                GameObject backdrop = CreateDecorBlock(
                    $"HubTodayMenuEntryBackdrop{i + 1}",
                    HubRoomLayout.TodayMenuEntryLocalPositions[i],
                    HubRoomLayout.TodayMenuEntryBackdropScale,
                    sprites.HubTodayMenuBg,
                    HubRoomLayout.TodayMenuBackdropColor,
                    HubRoomLayout.TodayMenuBackdropSortingOrder,
                    boardRoot.transform);

                entryBackdrops[i] = backdrop.GetComponent<SpriteRenderer>();

                GameObject item = CreateDecorBlock(
                    $"HubTodayMenuEntryItem{i + 1}",
                    HubRoomLayout.TodayMenuEntryIconLocalOffset,
                    HubRoomLayout.TodayMenuEntryIconScale,
                    itemSprites[i],
                    HubRoomLayout.TodayMenuIconColor,
                    HubRoomLayout.TodayMenuItemSortingOrder,
                    backdrop.transform);

                entryIcons[i] = item.GetComponent<SpriteRenderer>();
            }

            HubTodayMenuDisplay display = boardRoot.AddComponent<HubTodayMenuDisplay>();
            display.Configure(restaurantManager, headerLabel, entryBackdrops, entryIcons);
            ApplySceneComponentOverride(display, boardRoot.name);
        }

        private static void CreateStorageStation(string objectName, Vector3 position, Vector3 size, Sprite sprite, Color color, string label, StorageManager storageManager, StorageStationAction action, Transform parent = null)
        {
            GameObject go = CreateDecorBlock(objectName, position, size, sprite, color, 8, parent);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;
            ApplySceneComponentOverride(collider, objectName);

            StorageStation station = go.AddComponent<StorageStation>();
            SerializedObject so = new(station);
            so.FindProperty("storageManager").objectReferenceValue = storageManager;
            so.FindProperty("stationAction").enumValueIndex = (int)action;
            so.FindProperty("promptLabel").stringValue = action switch
            {
                StorageStationAction.StoreSelected => "창고 맡기기",
                StorageStationAction.WithdrawSelected => "창고 꺼내기",
                StorageStationAction.CycleInventorySelection => "맡길 품목 변경",
                StorageStationAction.CycleStorageSelection => "꺼낼 품목 변경",
                StorageStationAction.StoreAll => "모두 맡기기",
                StorageStationAction.WithdrawAll => "모두 꺼내기",
                _ => "창고 사용"
            };
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(station, objectName);
            ApplySceneActiveOverride(go, objectName);

            CreateWorldLabel(objectName + "_Label", go.transform, new Vector3(0f, 0.72f, 0f), label, Color.black, WorldLabelFontSize, 50);
        }

        private static void CreateUpgradeStation(Vector3 position, Vector3 size, Sprite sprite, Color color, UpgradeManager upgradeManager)
        {
            GameObject go = CreateDecorBlock("UpgradeStation", position, size, sprite, color, 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;
            ApplySceneComponentOverride(collider, go.name);

            UpgradeStation station = go.AddComponent<UpgradeStation>();
            SerializedObject so = new(station);
            so.FindProperty("upgradeManager").objectReferenceValue = upgradeManager;
            so.FindProperty("promptLabel").stringValue = "작업대 사용";
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(station, go.name);
            ApplySceneActiveOverride(go, go.name);

            CreateWorldLabel("UpgradeStationLabel", go.transform, new Vector3(0f, 0.68f, 0f), "작업대", Color.black, WorldLabelFontSize, 50);
        }

        private static void CreateGatherable(string objectName, Vector3 position, Sprite sprite, ResourceData resource, ToolType requiredToolType, int minAmount, int maxAmount, string label)
        {
            CreateFeaturePad(objectName + "_Pad", position + new Vector3(0f, -0.35f, 0f), new Vector3(1.6f, 0.5f, 1f), sprite, new Color(0f, 0f, 0f, 0.12f));

            GameObject go = CreateDecorBlock(objectName, position, new Vector3(1.05f, 1.05f, 1f), sprite, Color.white, 6);
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            ApplySceneComponentOverride(collider, objectName);

            GatherableResource gatherable = go.AddComponent<GatherableResource>();
            SerializedObject so = new(gatherable);
            so.FindProperty("resourceData").objectReferenceValue = resource;
            so.FindProperty("requiredToolType").enumValueIndex = (int)requiredToolType;
            so.FindProperty("minAmount").intValue = minAmount;
            so.FindProperty("maxAmount").intValue = maxAmount;
            so.FindProperty("promptLabel").stringValue = "채집하기";
            so.FindProperty("blockingCollider").objectReferenceValue = collider;
            so.ApplyModifiedPropertiesWithoutUndo();
            typeof(GatherableResource)
                .GetField("resourceData", BindingFlags.Instance | BindingFlags.NonPublic)?
                .SetValue(gatherable, resource);
            EditorUtility.SetDirty(gatherable);
            ApplySceneComponentOverride(gatherable, objectName);
            ApplySceneActiveOverride(go, objectName);

            CreateWorldLabel(objectName + "_Label", go.transform, new Vector3(0f, 0.64f, 0f), label, Color.black, WorldLabelSmallFontSize, 45);
        }

        private static void CreateGuideTriggerZone(string objectName, Vector3 position, Vector2 size, string hintId, string guideText)
        {
            GameObject go = new(objectName);
            ApplySceneTransformOverride(go.transform, objectName, position, Quaternion.identity, Vector3.one, useLocalSpace: false);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            ApplySceneComponentOverride(collider, objectName);

            GuideTriggerZone trigger = go.AddComponent<GuideTriggerZone>();
            SerializedObject so = new(trigger);
            so.FindProperty("hintId").stringValue = hintId;
            so.FindProperty("guideText").stringValue = guideText;
            so.FindProperty("duration").floatValue = 5f;
            so.FindProperty("triggerOnlyOnce").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(trigger, objectName);
            ApplySceneActiveOverride(go, objectName);
        }

        private static void CreateMovementModifierZone(string objectName, Vector3 position, Vector2 size, float multiplier, string guideText)
        {
            GameObject go = new(objectName);
            ApplySceneTransformOverride(go.transform, objectName, position, Quaternion.identity, Vector3.one, useLocalSpace: false);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            ApplySceneComponentOverride(collider, objectName);

            MovementModifierZone zone = go.AddComponent<MovementModifierZone>();
            SerializedObject so = new(zone);
            so.FindProperty("movementMultiplier").floatValue = multiplier;
            so.FindProperty("guideText").stringValue = guideText;
            so.FindProperty("hintId").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(zone, objectName);
            ApplySceneActiveOverride(go, objectName);
        }

        private static void CreateDarknessZone(string objectName, Vector3 position, Vector2 size)
        {
            GameObject go = new(objectName);
            ApplySceneTransformOverride(go.transform, objectName, position, Quaternion.identity, Vector3.one, useLocalSpace: false);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            ApplySceneComponentOverride(collider, objectName);

            DarknessZone zone = go.AddComponent<DarknessZone>();
            SerializedObject so = new(zone);
            so.FindProperty("noLanternMovementMultiplier").floatValue = 0.45f;
            so.FindProperty("noLanternGuideText").stringValue = "랜턴이 없으면 폐광산 안쪽을 천천히 더듬어 움직여야 합니다.";
            so.FindProperty("hintId").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(zone, objectName);
            ApplySceneActiveOverride(go, objectName);
        }

        private static void CreateWindGustZone(string objectName, Vector3 position, Vector2 size, Vector2 direction, float strength, float activeDuration, float inactiveDuration)
        {
            GameObject go = new(objectName);
            ApplySceneTransformOverride(go.transform, objectName, position, Quaternion.identity, Vector3.one, useLocalSpace: false);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            ApplySceneComponentOverride(collider, objectName);

            WindGustZone zone = go.AddComponent<WindGustZone>();
            SerializedObject so = new(zone);
            so.FindProperty("gustDirection").vector2Value = direction;
            so.FindProperty("gustStrength").floatValue = strength;
            so.FindProperty("activeDuration").floatValue = activeDuration;
            so.FindProperty("inactiveDuration").floatValue = inactiveDuration;
            so.FindProperty("startActive").boolValue = true;
            so.FindProperty("hintIdPrefix").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(zone, objectName);
            ApplySceneActiveOverride(go, objectName);
        }

        private static void CreateSpawnPoint(string objectName, Vector3 position, string spawnId)
        {
            GameObject go = new(objectName);
            ApplySceneTransformOverride(go.transform, objectName, position, Quaternion.identity, Vector3.one, useLocalSpace: false);
            SceneSpawnPoint spawnPoint = go.AddComponent<SceneSpawnPoint>();

            SerializedObject so = new(spawnPoint);
            so.FindProperty("spawnId").stringValue = spawnId;
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplySceneComponentOverride(spawnPoint, objectName);
            ApplySceneActiveOverride(go, objectName);
        }

        /// <summary>
        /// 새로 생성하는 씬에는 현재 HUD 구조만 심습니다.
        /// 더 이상 쓰지 않는 레거시 카드와 텍스트는 여기서 만들지 않습니다.
        /// </summary>
        // Canvas/UI 생성 로직은 partial 파일로 분리합니다.

        // generated 자산 생성과 저장 유틸리티는 partial 파일로 분리합니다.
        private readonly struct RecipeIngredientDefinition
        {
            public RecipeIngredientDefinition(ResourceData resource, int amount)
            {
                Resource = resource;
                Amount = amount;
            }

            public ResourceData Resource { get; }
            public int Amount { get; }
        }
    }
}
#endif
