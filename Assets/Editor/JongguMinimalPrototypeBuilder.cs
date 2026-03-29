#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Core;
using Data;
using Economy;
using Flow;
using Gathering;
using GameCamera;
using Inventory;
using Interaction;
using Player;
using Restaurant;
using Storage;
using TMPro;
using Tools;
using UI;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using Upgrade;
using World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// ProjectEditor 네임스페이스
namespace ProjectEditor
{
    public static class JongguMinimalPrototypeBuilder
    {
        private const string PopupTitleObjectName = "PopupTitle";
        private const string PopupLeftCaptionObjectName = "PopupLeftCaption";
        private const string PopupRightCaptionObjectName = "PopupRightCaption";
        private const string GeneratedRoot = "Assets/Generated";
        private const string DataRoot = GeneratedRoot + "/GameData";
        private const string SpriteRoot = GeneratedRoot + "/Sprites";
        private const string SceneRoot = "Assets/Scenes";
        private const string SharedExplorationHudSourceScene = SceneRoot + "/WindHill.unity";
        private const string FontRoot = GeneratedRoot + "/Fonts";
        private const string ResourceSpriteRoot = "Assets/Resources/Generated/Sprites";
        private const float PlayerSpritePixelsPerUnit = 1000f;
        private const float PlayerVisualScale = 0.76f;

        private static TMP_FontAsset _generatedKoreanFont;
        private static TMP_FontAsset _generatedHeadingFont;

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

        private static readonly string[] SharedExplorationHudTargetScenes =
        {
            SceneRoot + "/Beach.unity",
            SceneRoot + "/DeepForest.unity",
            SceneRoot + "/AbandonedMine.unity"
        };

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

        [MenuItem("Tools/Jonggu Restaurant/열린 씬 누락 스크립트 정리", true, 2110)]
        private static bool ValidateCleanMissingScriptsInOpenScene()
        {
            return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Tools/Jonggu Restaurant/열린 씬 누락 스크립트 정리", false, 2110)]
        public static void CleanMissingScriptsInOpenScene()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("누락 스크립트 정리는 플레이 모드를 종료한 뒤 실행하세요.");
                return;
            }

            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                Debug.LogWarning("정리할 열린 씬이 없습니다.");
                return;
            }

            int removedCount = 0;
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                removedCount += RemoveMissingScriptsRecursive(root);
            }

            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            Debug.Log($"열린 씬에서 누락 스크립트 컴포넌트 {removedCount}개를 정리했습니다.");
        }

        [MenuItem("Tools/Jonggu Restaurant/프로토타입 빌드 및 감사", true, 2100)]
        private static bool ValidateBuildMinimalPrototype()
        {
            return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Tools/Jonggu Restaurant/프로토타입 빌드 및 감사", false, 2100)]
        public static void BuildMinimalPrototype()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("프로토타입 빌드 및 감사는 플레이 모드를 종료한 뒤 실행하세요.");
                return;
            }

            EnsureFolder("Assets", "Generated");
            EnsureFolder(GeneratedRoot, "GameData");
            EnsureFolder(GeneratedRoot, "Sprites");
            EnsureFolder(GeneratedRoot, "Fonts");
            EnsureFolder("Assets", "Scenes");

            _generatedHeadingFont = CreateHeadingFontAsset();
            _generatedKoreanFont = CreateKoreanFontAsset();
            EnsurePreferredTmpFontAsset();
            SpriteLibrary sprites = CreateSprites();
            ResourceLibrary resources = CreateResources();
            RecipeLibrary recipes = CreateRecipes(resources);

            // WindHill의 HUDRoot를 탐험 씬 공용 기준으로 사용하므로,
            // 빌드 전에 최신 WindHill 씬 저장본을 먼저 확보합니다.
            SaveSceneIfLoadedAndDirty(SharedExplorationHudSourceScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildHubScene(resources, recipes, sprites);
            BuildBeachScene(resources, sprites);
            BuildDeepForestScene(resources, sprites);
            BuildAbandonedMineScene(resources, sprites);
            BuildWindHillScene(resources, sprites);
            SyncSharedExplorationHudRoots();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PrototypeSceneAudit.AuditGeneratedScenes();

            EditorUtility.DisplayDialog(
                "종구의 식당",
                "최소 프로토타입 씬이 생성되었습니다. Assets/Scenes/Hub.unity를 열고 실행하세요.",
                "OK");
        }

        private static void SyncSharedExplorationHudRoots()
        {
            SaveSceneIfLoadedAndDirty(SharedExplorationHudSourceScene);

            foreach (string targetScenePath in SharedExplorationHudTargetScenes)
            {
                SyncHudRootBetweenScenes(SharedExplorationHudSourceScene, targetScenePath);
            }
        }

        private static void BuildHubScene(ResourceLibrary resources, RecipeLibrary recipes, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/Hub.unity");
            CacheHubPopupSceneImages(SceneRoot + "/Hub.unity");
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                const float mapWidth = 26f;
                const float mapHeight = 16f;

                GameObject gameManagerObject = CreateGameManager("Hub", "Beach", resources);
                GameObject player = CreatePlayer(new Vector3(-1f, -3f, 0f), sprites);
                CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.94f, 0.90f, 0.82f), 6.2f);
                BoxCollider2D movementBounds = CreateMovementBounds("HubMovementBounds", mapWidth - 2.2f, mapHeight - 2.2f);
                AttachPlayerBoundsLimiter(player, movementBounds);

                CreateFloorZone("HubBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.95f, 0.91f, 0.82f), -20);
                CreateFloorZone("KitchenBand", new Vector3(0f, 3.5f, 0f), new Vector3(mapWidth, 5.5f, 1f), sprites.Floor, new Color(0.70f, 0.55f, 0.40f), -19);
                CreateFloorZone("DiningRug", new Vector3(-0.8f, -2.4f, 0f), new Vector3(16f, 5.5f, 1f), sprites.Floor, new Color(0.83f, 0.71f, 0.55f), -18);
                CreateFloorZone("DoorPad", new Vector3(10.45f, -3.0f, 0f), new Vector3(3.9f, 3.8f, 1f), sprites.Floor, new Color(0.78f, 0.60f, 0.32f), -17);

                CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.35f, 0.22f, 0.14f));
                CreateDecorBlock("KitchenCounter", new Vector3(0f, 1.7f, 0f), new Vector3(12f, 1f, 1f), sprites.Floor, new Color(0.48f, 0.28f, 0.17f), 2);
                CreateDecorBlock("MenuBoardBack", new Vector3(-7.1f, 0.25f, 0f), new Vector3(2.6f, 3.0f, 1f), sprites.Floor, new Color(0.34f, 0.23f, 0.18f), 1);
                CreateDecorBlock("DoorFrame", new Vector3(10.45f, -2.15f, 0f), new Vector3(1.8f, 3.0f, 1f), sprites.Floor, new Color(0.42f, 0.24f, 0.12f), 1);
                CreateDecorBlock("TableLeft", new Vector3(-4.5f, -1.8f, 0f), new Vector3(2.2f, 1.4f, 1f), sprites.Floor, new Color(0.62f, 0.44f, 0.24f), 1);
                CreateDecorBlock("TableRight", new Vector3(2.5f, -1.2f, 0f), new Vector3(2.4f, 1.4f, 1f), sprites.Floor, new Color(0.62f, 0.44f, 0.24f), 1);
                GameObject storageArea = new("StorageArea");
                storageArea.transform.position = Vector3.zero;

                CreateDecorBlock("StorageWall", new Vector3(6.15f, 1.18f, 0f), new Vector3(4.2f, 2.45f, 1f), sprites.Floor, new Color(0.55f, 0.38f, 0.20f), 1, storageArea.transform);
                CreateDecorBlock("WorkbenchWall", new Vector3(6.05f, -1.98f, 0f), new Vector3(4.1f, 1.95f, 1f), sprites.Floor, new Color(0.46f, 0.33f, 0.23f), 1);

                CreateWorldLabel("RestaurantTitle", null, new Vector3(0f, 6f, 0f), "종구의 식당", Color.black, 4.6f, 40);
                CreateWorldLabel("StorageSign", storageArea.transform, new Vector3(5.4f, 3.45f, 0f), "창고 구역", Color.black, 2.8f, 40);
                CreateWorldLabel("WorkbenchSign", null, new Vector3(5.4f, -0.55f, 0f), "작업대", Color.black, 2.8f, 40);

                CreateSpawnPoint("HubEntry", new Vector3(0f, -4.6f, 0f), "HubEntry");
                CreatePortal("GoToBeach", new Vector3(9.55f, -3.05f, 0f), sprites.Portal, "Beach", "BeachEntry", "바닷가로 이동", "바닷가로", true, ToolType.None, 0, "", new Vector3(1.35f, 1.9f, 1f));
                CreatePortal("GoToDeepForest", new Vector3(11.35f, -3.05f, 0f), sprites.Portal, "DeepForest", "ForestEntry", "깊은 숲으로 이동", "깊은 숲", true, ToolType.None, 0, "", new Vector3(1.35f, 1.9f, 1f));
                CreatePortal(
                    "GoToAbandonedMine",
                    new Vector3(9.55f, -0.65f, 0f),
                    sprites.Portal,
                    "AbandonedMine",
                    "MineEntry",
                    "폐광산으로 이동",
                    "폐광산",
                    true,
                    ToolType.Lantern,
                    0,
                    "작업대에서 랜턴을 준비해야 폐광산 안쪽을 안전하게 탐험할 수 있습니다.",
                    new Vector3(1.35f, 1.9f, 1f));
                CreatePortal("GoToWindHill", new Vector3(11.35f, -0.65f, 0f), sprites.Portal, "WindHill", "WindHillEntry", "바람 언덕으로 이동", "바람 언덕", true, ToolType.None, 0, "", new Vector3(1.35f, 1.9f, 1f));
                CreateFeaturePad("PortalPad", new Vector3(9.55f, -3.72f, 0f), new Vector3(2.0f, 0.55f, 1f), sprites.Floor, new Color(0.98f, 0.83f, 0.51f));
                CreateFeaturePad("ForestPortalPad", new Vector3(11.35f, -3.72f, 0f), new Vector3(2.0f, 0.55f, 1f), sprites.Floor, new Color(0.70f, 0.86f, 0.44f));
                CreateFeaturePad("MinePortalPad", new Vector3(9.55f, -1.32f, 0f), new Vector3(2.0f, 0.55f, 1f), sprites.Floor, new Color(0.74f, 0.74f, 0.78f));
                CreateFeaturePad("WindPortalPad", new Vector3(11.35f, -1.32f, 0f), new Vector3(2.0f, 0.55f, 1f), sprites.Floor, new Color(0.82f, 0.92f, 0.98f));

                RestaurantManager restaurantManager = CreateRestaurantManager(recipes);
                CreateRecipeSelector(new Vector3(-7.1f, 0.35f, 0f), sprites.Selector, restaurantManager);
                CreateServiceCounter(new Vector3(0f, 1.92f, 0f), sprites.Counter, restaurantManager);
                StorageManager storageManager = gameManagerObject.GetComponent<StorageManager>();
                UpgradeManager upgradeManager = gameManagerObject.GetComponent<UpgradeManager>();
                CreateStorageStation("StorageStation", new Vector3(6.1f, 1.2f, 0f), new Vector3(3.6f, 2.0f, 1f), sprites.Floor, new Color(0.86f, 0.70f, 0.36f), "창고", storageManager, StorageStationAction.StoreAll, storageArea.transform);
                CreateUpgradeStation(new Vector3(5.25f, -2.35f, 0f), new Vector3(1.95f, 1.2f, 1f), sprites.Floor, new Color(0.54f, 0.72f, 0.78f), upgradeManager);

                if (!TryReuseExistingSceneCanvas(SceneRoot + "/Hub.unity"))
                {
                    CreateUiCanvas(true);
                }

                EnsureUiEventSystem();
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/Hub.unity");
            }
            finally
            {
                CachedHubPopupSceneImages.Clear();
            }
        }

        private static void BuildBeachScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/Beach.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 30f;
            const float mapHeight = 18f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-8.25f, -2.25f, 0f), sprites);
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

            CreateWorldLabel("BeachTitle", null, new Vector3(0f, 7.1f, 0f), "바닷가", Color.black, 4.2f, 40);
            CreateSpawnPoint("BeachEntry", new Vector3(-8.25f, -2.25f, 0f), "BeachEntry");
            CreatePortal("ReturnToHub", new Vector3(-10.7f, -3.35f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGatherable("FishSpot01", new Vector3(-2f, 2.2f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
            CreateGatherable("FishSpot02", new Vector3(2.8f, 4f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
            CreateGatherable("ShellSpot01", new Vector3(-1f, -3f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
            CreateGatherable("ShellSpot02", new Vector3(4.5f, -1.8f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
            CreateGatherable("SeaweedSpot01", new Vector3(7f, 3.8f, 0f), sprites.Seaweed, resources.Seaweed, ToolType.Sickle, 1, 2, "해초");

            if (!TryReuseExistingSceneCanvas(SceneRoot + "/Beach.unity"))
            {
                CreateUiCanvas(false);
            }

            TryApplySharedHudRootFromScene(SharedExplorationHudSourceScene);
            EnsureUiEventSystem();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/Beach.unity");
        }

        /// <summary>
        /// 허브 씬에 직접 지정한 팝업 Image 설정을 먼저 읽어 두면,
        /// 빌더가 씬을 다시 저장해도 수동으로 맞춘 이미지 소스를 유지할 수 있습니다.
        /// </summary>
        private static void CacheHubPopupSceneImages(string scenePath)
        {
            CachedHubPopupSceneImages.Clear();
            if (!File.Exists(scenePath))
            {
                return;
            }

            UnityEngine.SceneManagement.Scene sourceScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
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

        /// <summary>
        /// 기존 씬에 저장된 Canvas 루트를 통째로 복제해 현재 빌드 씬으로 가져옵니다.
        /// 이렇게 하면 수동으로 맞춘 Canvas 하위 구조와 컴포넌트 값이 빌드 후에도 그대로 유지됩니다.
        /// </summary>
        private static bool TryReuseExistingSceneCanvas(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene sourceScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool openedTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                return false;
            }

            try
            {
                GameObject sourceCanvas = sourceScene
                    .GetRootGameObjects()
                    .FirstOrDefault(root => root != null
                                            && string.Equals(root.name, "Canvas", StringComparison.Ordinal)
                                            && root.GetComponent<Canvas>() != null);
                if (sourceCanvas == null)
                {
                    return false;
                }

                GameObject clonedCanvas = UnityEngine.Object.Instantiate(sourceCanvas);
                clonedCanvas.name = sourceCanvas.name;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                    clonedCanvas,
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                return true;
            }
            finally
            {
                if (openedTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        /// <summary>
        /// 열려 있는 대상 씬에 아직 저장되지 않은 변경이 있으면 먼저 저장해,
        /// 빌더가 방금 수정한 Canvas 기준을 그대로 다시 사용할 수 있게 맞춥니다.
        /// </summary>
        private static void SaveSceneIfLoadedAndDirty(string scenePath)
        {
            UnityEngine.SceneManagement.Scene loadedScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded || !loadedScene.isDirty)
            {
                return;
            }

            EditorSceneManager.SaveScene(loadedScene);
        }

        /// <summary>
        /// WindHill 씬의 HUDRoot를 현재 활성 씬 Canvas에 복제해 탐험 HUD 기준을 통일합니다.
        /// PopupRoot는 대상 씬 값을 유지하고, HUDRoot만 교체합니다.
        /// </summary>
        private static bool TryApplySharedHudRootFromScene(string sourceScenePath)
        {
            if (!File.Exists(sourceScenePath))
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene targetScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject targetCanvas = FindSceneCanvasRoot(targetScene);
            if (targetCanvas == null)
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene sourceScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sourceScenePath);
            bool openedTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Additive);
                openedTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                return false;
            }

            try
            {
                GameObject sourceCanvas = FindSceneCanvasRoot(sourceScene);
                Transform sourceHudRoot = sourceCanvas != null ? sourceCanvas.transform.Find("HUDRoot") : null;
                if (sourceHudRoot == null)
                {
                    return false;
                }

                return ReplaceHudRoot(targetScene, targetCanvas, sourceHudRoot);
            }
            finally
            {
                if (openedTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        /// <summary>
        /// WindHill 씬의 HUDRoot를 대상 탐험 씬 자산에 직접 반영해,
        /// 빌드를 다시 돌리지 않아도 같은 HUD 구조를 유지하도록 맞춥니다.
        /// </summary>
        private static bool SyncHudRootBetweenScenes(string sourceScenePath, string targetScenePath)
        {
            if (!File.Exists(sourceScenePath) || !File.Exists(targetScenePath))
            {
                return false;
            }

            if (string.Equals(sourceScenePath, targetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene sourceScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sourceScenePath);
            UnityEngine.SceneManagement.Scene targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(targetScenePath);
            bool openedSourceTemporarily = false;
            bool openedTargetTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Additive);
                openedSourceTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Additive);
                openedTargetTemporarily = targetScene.IsValid() && targetScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded || !targetScene.IsValid() || !targetScene.isLoaded)
            {
                return false;
            }

            try
            {
                GameObject sourceCanvas = FindSceneCanvasRoot(sourceScene);
                GameObject targetCanvas = FindSceneCanvasRoot(targetScene);
                Transform sourceHudRoot = sourceCanvas != null ? sourceCanvas.transform.Find("HUDRoot") : null;
                if (targetCanvas == null || sourceHudRoot == null)
                {
                    return false;
                }

                bool replaced = ReplaceHudRoot(targetScene, targetCanvas, sourceHudRoot);
                if (!replaced)
                {
                    return false;
                }

                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
                return true;
            }
            finally
            {
                if (openedTargetTemporarily)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                if (openedSourceTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        private static GameObject FindSceneCanvasRoot(UnityEngine.SceneManagement.Scene scene)
        {
            return scene
                .GetRootGameObjects()
                .FirstOrDefault(root => root != null
                                        && string.Equals(root.name, "Canvas", StringComparison.Ordinal)
                                        && root.GetComponent<Canvas>() != null);
        }

        /// <summary>
        /// 대상 Canvas 아래 HUDRoot만 교체하고 나머지 PopupRoot 및 개별 팝업 값은 유지합니다.
        /// </summary>
        private static bool ReplaceHudRoot(
            UnityEngine.SceneManagement.Scene targetScene,
            GameObject targetCanvas,
            Transform sourceHudRoot)
        {
            if (targetCanvas == null || sourceHudRoot == null)
            {
                return false;
            }

            Transform existingHudRoot = targetCanvas.transform.Find("HUDRoot");
            int siblingIndex = existingHudRoot != null ? existingHudRoot.GetSiblingIndex() : 0;
            if (existingHudRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingHudRoot.gameObject);
            }

            GameObject clonedHudRoot = UnityEngine.Object.Instantiate(sourceHudRoot.gameObject, targetCanvas.transform);
            clonedHudRoot.name = "HUDRoot";
            clonedHudRoot.transform.SetSiblingIndex(siblingIndex);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clonedHudRoot, targetScene);

            RebindCanvasUiManagerReferences(targetCanvas);
            return true;
        }

        /// <summary>
        /// HUDRoot를 다른 씬 기준으로 교체하면 UIManager가 들고 있던 텍스트/버튼 참조를 새 오브젝트로 다시 맞춥니다.
        /// </summary>
        private static void RebindCanvasUiManagerReferences(GameObject canvasObject)
        {
            if (canvasObject == null || !canvasObject.TryGetComponent(out UIManager uiManager))
            {
                return;
            }

            SerializedObject so = new(uiManager);
            so.FindProperty("interactionPromptText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "InteractionPromptText");
            so.FindProperty("inventoryText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "InventoryText");
            so.FindProperty("storageText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "StorageText");
            so.FindProperty("upgradeText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "UpgradeText");
            so.FindProperty("goldText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "GoldText");
            so.FindProperty("selectedRecipeText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "SelectedRecipeText");
            so.FindProperty("dayPhaseText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "DayPhaseText");
            so.FindProperty("guideText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "GuideText");
            so.FindProperty("resultText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "RestaurantResultText");
            so.FindProperty("skipExplorationButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "SkipExplorationButton");
            so.FindProperty("skipServiceButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "SkipServiceButton");
            so.FindProperty("nextDayButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "NextDayButton");
            so.FindProperty("recipePanelButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "RecipePanelButton");
            so.FindProperty("upgradePanelButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "UpgradePanelButton");
            so.FindProperty("materialPanelButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "MaterialPanelButton");
            so.FindProperty("popupCloseButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "PopupCloseButton");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T FindNamedComponent<T>(Transform root, string objectName)
            where T : Component
        {
            Transform target = FindChildRecursive(root, objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static void BuildDeepForestScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/DeepForest.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 32f;
            const float mapHeight = 20f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.3f, -6.1f, 0f), sprites);
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

            CreateWorldLabel("ForestTitle", null, new Vector3(0f, 8.2f, 0f), "깊은 숲", Color.black, 4.2f, 40);
            CreateSpawnPoint("ForestEntry", new Vector3(-10.3f, -6.1f, 0f), "ForestEntry");
            CreatePortal("ReturnFromForest", new Vector3(-13.6f, -6.15f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGuideTriggerZone("ForestGuide", new Vector3(-8.4f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "forest_intro", "숲은 갈림길과 늪지대 때문에 인벤토리보다 귀환 동선을 더 자주 확인해야 합니다.");
            CreateMovementModifierZone("ForestSwampZone", new Vector3(1.8f, 1f, 0f), new Vector2(9f, 4.2f), 0.55f, "늪지에서는 이동이 느려집니다. 좁은 길을 따라 움직이면 더 안전합니다.");

            CreateGatherable("HerbPatch01", new Vector3(-4f, -1.1f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
            CreateGatherable("HerbPatch02", new Vector3(4.8f, -3.6f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
            CreateGatherable("MushroomPatch01", new Vector3(2.6f, 4.1f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");
            CreateGatherable("MushroomPatch02", new Vector3(8.5f, 5.2f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");

            if (!TryReuseExistingSceneCanvas(SceneRoot + "/DeepForest.unity"))
            {
                CreateUiCanvas(false);
            }

            TryApplySharedHudRootFromScene(SharedExplorationHudSourceScene);
            EnsureUiEventSystem();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/DeepForest.unity");
        }

        private static void BuildAbandonedMineScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/AbandonedMine.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 32f;
            const float mapHeight = 20f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.7f, -5.95f, 0f), sprites);
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

            CreateWorldLabel("MineTitle", null, new Vector3(0f, 8.1f, 0f), "폐광산", Color.white, 4.2f, 40);
            CreateSpawnPoint("MineEntry", new Vector3(-10.7f, -6.0f, 0f), "MineEntry");
            CreatePortal("ReturnFromMine", new Vector3(-13.6f, -6.0f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGuideTriggerZone("MineGuide", new Vector3(-8.8f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "mine_intro", "폐광산은 어둡고 동선이 좁습니다. 안쪽으로 들어가기 전 귀환 길을 먼저 확인하세요.");
            CreateDarknessZone("MineDarkness", new Vector3(4.8f, 0.6f, 0f), new Vector2(18f, 10.8f));

            CreateGatherable("GlowMoss01", new Vector3(4.4f, 3.2f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
            CreateGatherable("GlowMoss02", new Vector3(8.2f, 1.0f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
            CreateGatherable("GlowMoss03", new Vector3(11.6f, 4.4f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");

            if (!TryReuseExistingSceneCanvas(SceneRoot + "/AbandonedMine.unity"))
            {
                CreateUiCanvas(false);
            }

            TryApplySharedHudRootFromScene(SharedExplorationHudSourceScene);
            EnsureUiEventSystem();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/AbandonedMine.unity");
        }

        private static void BuildWindHillScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/WindHill.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 30f;
            const float mapHeight = 18f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.7f, -5.3f, 0f), sprites);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.85f, 0.92f, 0.98f), 6.8f);

            BoxCollider2D movementBounds = CreateMovementBounds("WindHillBounds", mapWidth - 2.2f, mapHeight - 2.2f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("HillBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.79f, 0.86f, 0.66f), -20);
            CreateFloorZone("CliffBand", new Vector3(6.8f, 0f, 0f), new Vector3(12f, 14f, 1f), sprites.Floor, new Color(0.70f, 0.80f, 0.58f), -19);
            CreateFloorZone("WindLane", new Vector3(6.8f, 0.8f, 0f), new Vector3(10f, 6.4f, 1f), sprites.Floor, new Color(0.91f, 0.96f, 0.98f), -18);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.37f, 0.43f, 0.31f));
            CreateDecorBlock("CliffRockA", new Vector3(10.4f, -4.2f, 0f), new Vector3(3.2f, 1.5f, 1f), sprites.Floor, new Color(0.48f, 0.50f, 0.46f), 2);
            CreateDecorBlock("CliffRockB", new Vector3(4.8f, 4.8f, 0f), new Vector3(2.6f, 1.2f, 1f), sprites.Floor, new Color(0.48f, 0.50f, 0.46f), 2);

            CreateWorldLabel("WindHillTitle", null, new Vector3(0f, 7.1f, 0f), "바람 언덕", Color.black, 4.2f, 40);
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

            if (!TryReuseExistingSceneCanvas(SceneRoot + "/WindHill.unity"))
            {
                CreateUiCanvas(false);
            }

            EnsureUiEventSystem();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/WindHill.unity");
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

            return go;
        }

        private static GameObject CreatePlayer(Vector3 position, SpriteLibrary sprites)
        {
            GameObject player = new("Jonggu");
            player.transform.position = position;

            Sprite frontSprite = sprites.PlayerFront != null ? sprites.PlayerFront : sprites.PlayerSide;
            GameObject shadow = CreateDecorBlock("Shadow", Vector3.zero, new Vector3(0.46f, 0.14f, 1f), sprites.Floor, new Color(0f, 0f, 0f, 0.20f), 9, player.transform);
            shadow.transform.localPosition = new Vector3(0f, -0.28f, 0f);

            // 물리는 루트에 유지하고, 맵 크기 보정은 비주얼 자식만 스케일해서 처리합니다.
            GameObject visualRoot = new("PlayerVisual");
            visualRoot.transform.SetParent(player.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localScale = Vector3.one * PlayerVisualScale;

            SpriteRenderer renderer = visualRoot.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visualRoot.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = frontSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 12;

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = player.AddComponent<Rigidbody2D>();
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = player.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                collider = player.AddComponent<CapsuleCollider2D>();
            }

            collider.size = new Vector2(0.9f, 1.05f);

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = player.AddComponent<PlayerController>();
            }

            PlayerDirectionalSprite directionalSprite = player.GetComponent<PlayerDirectionalSprite>();
            if (directionalSprite == null)
            {
                directionalSprite = player.AddComponent<PlayerDirectionalSprite>();
            }

            directionalSprite.Configure(renderer, sprites.PlayerFront, sprites.PlayerBack, sprites.PlayerSide);

            GameObject interactionRange = new("InteractionRange");
            interactionRange.transform.SetParent(player.transform, false);
            CircleCollider2D rangeCollider = interactionRange.AddComponent<CircleCollider2D>();
            rangeCollider.isTrigger = true;
            rangeCollider.radius = 1.35f;
            InteractionDetector detector = interactionRange.AddComponent<InteractionDetector>();

            SerializedObject controllerSo = new(controller);
            controllerSo.FindProperty("interactionDetector").objectReferenceValue = detector;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel("PlayerLabel", player.transform, new Vector3(0f, 0.46f, 0f), "Jonggu", new Color(0.05f, 0.08f, 0.16f), 3f, 50);
            return player;
        }

        private static void CreateCamera(Transform target, float mapWidth, float mapHeight, Color backgroundColor, float orthographicSize)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.backgroundColor = backgroundColor;

            cameraObject.AddComponent<AudioListener>();

            GameObject boundsObject = new("CameraBounds");
            BoxCollider2D bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(mapWidth, mapHeight);

            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            SerializedObject followSo = new(follow);
            followSo.FindProperty("target").objectReferenceValue = target;
            followSo.FindProperty("mapBounds").objectReferenceValue = bounds;
            followSo.ApplyModifiedPropertiesWithoutUndo();
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
        }

        private static GameObject CreatePortal(
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

            string displayLabel = string.IsNullOrWhiteSpace(worldLabel) ? promptLabel : worldLabel;
            CreateWorldLabel(objectName + "_Label", portal.transform, new Vector3(0f, 0.82f, 0f), displayLabel, Color.black, 2.6f, 50);
            return portal;
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

            return manager;
        }

        private static void CreateRecipeSelector(Vector3 position, Sprite sprite, RestaurantManager restaurantManager)
        {
            GameObject go = CreateDecorBlock("RecipeSelector", position, new Vector3(1.55f, 1.55f, 1f), sprite, new Color(0.98f, 0.84f, 0.18f), 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;

            RecipeSelectorStation station = go.AddComponent<RecipeSelectorStation>();
            SerializedObject so = new(station);
            so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
            so.FindProperty("promptLabel").stringValue = "메뉴 변경";
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel("RecipeSelectorLabel", go.transform, new Vector3(0f, 0.80f, 0f), "메뉴판", Color.black, 2.6f, 50);
        }

        private static void CreateServiceCounter(Vector3 position, Sprite sprite, RestaurantManager restaurantManager)
        {
            GameObject go = CreateDecorBlock("ServiceCounter", position, new Vector3(1.95f, 1.55f, 1f), sprite, new Color(0.82f, 0.30f, 0.22f), 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;

            ServiceCounterStation station = go.AddComponent<ServiceCounterStation>();
            SerializedObject so = new(station);
            so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
            so.FindProperty("promptLabel").stringValue = "영업 시작";
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel("ServiceCounterLabel", go.transform, new Vector3(0f, 0.80f, 0f), "영업대", Color.black, 2.6f, 50);
        }

        private static void CreateStorageStation(string objectName, Vector3 position, Vector3 size, Sprite sprite, Color color, string label, StorageManager storageManager, StorageStationAction action, Transform _parent = null)
        {
            GameObject go = CreateDecorBlock(objectName, position, size, sprite, color, 8, _parent);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

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

            CreateWorldLabel(objectName + "_Label", go.transform, new Vector3(0f, 0.72f, 0f), label, Color.black, 2.6f, 50);
        }

        private static void CreateUpgradeStation(Vector3 position, Vector3 size, Sprite sprite, Color color, UpgradeManager upgradeManager)
        {
            GameObject go = CreateDecorBlock("UpgradeStation", position, size, sprite, color, 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            UpgradeStation station = go.AddComponent<UpgradeStation>();
            SerializedObject so = new(station);
            so.FindProperty("upgradeManager").objectReferenceValue = upgradeManager;
            so.FindProperty("promptLabel").stringValue = "작업대 사용";
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel("UpgradeStationLabel", go.transform, new Vector3(0f, 0.68f, 0f), "작업대", Color.black, 2.6f, 50);
        }

        private static void CreateGatherable(string objectName, Vector3 position, Sprite sprite, ResourceData resource, ToolType requiredToolType, int minAmount, int maxAmount, string label)
        {
            CreateFeaturePad(objectName + "_Pad", position + new Vector3(0f, -0.35f, 0f), new Vector3(1.6f, 0.5f, 1f), sprite, new Color(0f, 0f, 0f, 0.12f));

            GameObject go = CreateDecorBlock(objectName, position, new Vector3(1.05f, 1.05f, 1f), sprite, Color.white, 6);
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

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

            CreateWorldLabel(objectName + "_Label", go.transform, new Vector3(0f, 0.64f, 0f), label, Color.black, 2.4f, 45);
        }

        private static void CreateGuideTriggerZone(string objectName, Vector3 position, Vector2 size, string hintId, string guideText)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            GuideTriggerZone trigger = go.AddComponent<GuideTriggerZone>();
            SerializedObject so = new(trigger);
            so.FindProperty("hintId").stringValue = hintId;
            so.FindProperty("guideText").stringValue = guideText;
            so.FindProperty("duration").floatValue = 5f;
            so.FindProperty("triggerOnlyOnce").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMovementModifierZone(string objectName, Vector3 position, Vector2 size, float multiplier, string guideText)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            MovementModifierZone zone = go.AddComponent<MovementModifierZone>();
            SerializedObject so = new(zone);
            so.FindProperty("movementMultiplier").floatValue = multiplier;
            so.FindProperty("guideText").stringValue = guideText;
            so.FindProperty("hintId").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDarknessZone(string objectName, Vector3 position, Vector2 size)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            DarknessZone zone = go.AddComponent<DarknessZone>();
            SerializedObject so = new(zone);
            so.FindProperty("noLanternMovementMultiplier").floatValue = 0.45f;
            so.FindProperty("noLanternGuideText").stringValue = "랜턴이 없으면 폐광산 안쪽을 천천히 더듬어 움직여야 합니다.";
            so.FindProperty("hintId").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWindGustZone(string objectName, Vector3 position, Vector2 size, Vector2 direction, float strength, float activeDuration, float inactiveDuration)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            WindGustZone zone = go.AddComponent<WindGustZone>();
            SerializedObject so = new(zone);
            so.FindProperty("gustDirection").vector2Value = direction;
            so.FindProperty("gustStrength").floatValue = strength;
            so.FindProperty("activeDuration").floatValue = activeDuration;
            so.FindProperty("inactiveDuration").floatValue = inactiveDuration;
            so.FindProperty("startActive").boolValue = true;
            so.FindProperty("hintIdPrefix").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSpawnPoint(string objectName, Vector3 position, string spawnId)
        {
            GameObject go = new(objectName);
            go.transform.position = position;
            SceneSpawnPoint spawnPoint = go.AddComponent<SceneSpawnPoint>();

            SerializedObject so = new(spawnPoint);
            so.FindProperty("spawnId").stringValue = spawnId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 새로 생성하는 씬에는 현재 HUD 구조만 심습니다.
        /// 더 이상 쓰지 않는 레거시 카드와 텍스트는 여기서 만들지 않습니다.
        /// </summary>
        private static void CreateUiCanvas(bool isHubScene)
        {
            GameObject canvasObject = new("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            EnsureUiEventSystem();

            Color chromeDark = new(0.96f, 0.97f, 0.99f, 1f);
            Color chromeSurface = new(0.98f, 0.98f, 0.99f, 1f);
            Color chromeGlass = new(0.93f, 0.95f, 0.98f, 1f);
            Color chromeOverlay = new(0f, 0f, 0f, 0.52f);
            Color chromeOcean = new(0.18f, 0.66f, 0.90f, 1f);
            Color chromeAmber = new(0.94f, 0.74f, 0.10f, 1f);
            Color chromeText = new(0.23f, 0.27f, 0.34f, 1f);
            Color chromeDock = new(0.22f, 0.60f, 0.87f, 1f);

            RectTransform hudRoot = CreateCanvasGroupRoot("HUDRoot", canvasObject.transform, 0);
            RectTransform popupRoot = CreateCanvasGroupRoot("PopupRoot", canvasObject.transform, 1);
            RectTransform hudStatusGroup = CreateCanvasGroupRoot("HUDStatusGroup", hudRoot, 0);
            RectTransform hudInventoryGroup = CreateCanvasGroupRoot("HUDInventoryGroup", hudRoot, 1);
            RectTransform hudActionGroup = CreateCanvasGroupRoot("HUDActionGroup", hudRoot, 2);
            RectTransform hudButtonGroup = CreateCanvasGroupRoot("HUDButtonGroup", hudRoot, 3);
            RectTransform hudPromptGroup = CreateCanvasGroupRoot("HUDPromptGroup", hudRoot, 4);
            RectTransform hudOverlayGroup = CreateCanvasGroupRoot("HUDOverlayGroup", hudRoot, 5);
            RectTransform popupShellGroup = CreateCanvasGroupRoot("PopupShellGroup", popupRoot, 0);
            RectTransform popupFrameHeaderGroup = CreateCanvasGroupRoot("PopupFrameHeader", popupRoot, 2);
            RectTransform popupFrameGroup = null;
            RectTransform popupFrameLeftGroup = null;
            RectTransform popupFrameRightGroup = null;

            CreatePanel("TopLeftPanel", hudStatusGroup, PrototypeUILayout.TopLeftPanel, chromeDark);
            CreatePanel("PhaseBadge", hudStatusGroup, PrototypeUILayout.PhaseBadge, chromeGlass);
            CreatePanel("PromptBackdrop", hudPromptGroup, PrototypeUILayout.PromptBackdrop(isHubScene), chromeGlass);
            CreatePanel("GuideBackdrop", hudOverlayGroup, PrototypeUILayout.GuideBackdrop(isHubScene), chromeSurface);
            CreatePanel("ResultBackdrop", hudOverlayGroup, PrototypeUILayout.ResultBackdrop(isHubScene), chromeSurface);
            CreatePanel("InventoryCard", hudInventoryGroup, PrototypeUILayout.InventoryCard(isHubScene), chromeSurface);
            CreatePanel("InventoryAccent", hudInventoryGroup, PrototypeUILayout.InventoryAccent(isHubScene), chromeOcean);

            if (isHubScene)
            {
                CreatePanel("CenterBottomPanel", hudActionGroup, PrototypeUILayout.HubCenterBottomPanel, chromeSurface);
                CreatePanel("PopupOverlay", popupShellGroup, PrototypeUILayout.HubPopupOverlay, chromeOverlay);
                CreatePanel("ActionDock", hudActionGroup, PrototypeUILayout.HubActionDock, chromeDock);
                CreatePanel("ActionAccent", hudActionGroup, PrototypeUILayout.HubActionAccent, chromeAmber);
                CreatePanel("PopupFrame", popupRoot, PrototypeUILayout.HubPopupFrame, new Color(1f, 1f, 1f, 0f));
                popupFrameGroup = FindChildRecursive(popupRoot, "PopupFrame") as RectTransform;

                if (popupFrameGroup != null)
                {
                    CreatePanel("PopupFrameLeft", popupFrameGroup, PrototypeUILayout.HubPopupFrameLeft, Color.white);
                    CreatePanel("PopupFrameRight", popupFrameGroup, PrototypeUILayout.HubPopupFrameRight, new Color(0.92f, 0.95f, 0.99f, 1f));

                    popupFrameLeftGroup = FindChildRecursive(popupFrameGroup, "PopupFrameLeft") as RectTransform;
                    popupFrameRightGroup = FindChildRecursive(popupFrameGroup, "PopupFrameRight") as RectTransform;

                    if (popupFrameLeftGroup != null)
                    {
                        CreatePanel("PopupLeftBody", popupFrameLeftGroup, PrototypeUILayout.HubPopupFrameBody, Color.white);
                        CreatePopupBodyItemBoxes("PopupLeftBody", "PopupLeftItemBox", "PopupLeftItemIcon", "PopupLeftItemText", popupFrameLeftGroup, chromeText, true);
                    }

                    if (popupFrameRightGroup != null)
                    {
                        CreatePanel("PopupRightBody", popupFrameRightGroup, PrototypeUILayout.HubPopupFrameBody, Color.white);
                    }
                }
            }

            UIManager uiManager = canvasObject.AddComponent<UIManager>();
            PrototypeUIDesignController designController = canvasObject.AddComponent<PrototypeUIDesignController>();
            designController.Configure(uiManager);

            TextMeshProUGUI inventoryCaption = CreateScreenText(
                "InventoryCaption",
                hudInventoryGroup,
                PrototypeUILayout.InventoryCaption(isHubScene),
                16,
                TextAlignmentOptions.TopLeft,
                chromeOcean);
            TextMeshProUGUI storageCaption = null;
            TextMeshProUGUI recipeCaption = null;
            TextMeshProUGUI upgradeCaption = null;
            TextMeshProUGUI actionCaption = null;

            if (isHubScene)
            {
                Transform popupFrameTextRoot = popupFrameGroup != null ? popupFrameGroup : popupRoot;
                Transform popupFrameLeftTextRoot = popupFrameLeftGroup != null ? popupFrameLeftGroup : popupFrameTextRoot;
                Transform popupFrameRightTextRoot = popupFrameRightGroup != null ? popupFrameRightGroup : popupFrameTextRoot;

                actionCaption = CreateScreenText(
                    "ActionCaption",
                    hudActionGroup,
                    PrototypeUILayout.HubActionCaption,
                    15,
                    TextAlignmentOptions.TopRight,
                    new Color(0.88f, 0.88f, 0.88f, 1f));
                CreatePopupHeadingText(PopupTitleObjectName, popupFrameLeftTextRoot, PrototypeUILayout.HubPopupTitle, 40f, 24f, "\uC694\uB9AC \uBA54\uB274", chromeText, false);
                CreatePopupHeadingText(PopupLeftCaptionObjectName, popupFrameLeftTextRoot, PrototypeUILayout.HubPopupLeftCaption, 32f, 20f, "\uBA54\uB274 \uBAA9\uB85D", chromeText, false);
                CreatePopupHeadingText(PopupRightCaptionObjectName, popupFrameRightTextRoot, PrototypeUILayout.HubPopupFrameCaption, 32f, 20f, "\uBA54\uB274 \uC0C1\uC138", chromeText, false);
            }

            TextMeshProUGUI goldText = CreateScreenText("GoldText", hudStatusGroup, PrototypeUILayout.GoldText, 20, TextAlignmentOptions.TopLeft, chromeText);
            TextMeshProUGUI inventoryText = CreateScreenText("InventoryText", isHubScene ? (popupFrameLeftGroup != null ? popupFrameLeftGroup : popupRoot) : hudInventoryGroup, isHubScene ? PrototypeUILayout.HubPopupFrameText : PrototypeUILayout.InventoryText(false), 19, TextAlignmentOptions.TopLeft, chromeText);
            TextMeshProUGUI storageText = isHubScene
                ? CreateScreenText("StorageText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI promptText = CreateScreenText("InteractionPromptText", hudPromptGroup, PrototypeUILayout.PromptText(isHubScene), 21, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI guideText = CreateScreenText("GuideText", hudOverlayGroup, PrototypeUILayout.GuideText(isHubScene), 18, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI resultText = CreateScreenText("RestaurantResultText", hudOverlayGroup, PrototypeUILayout.ResultText(isHubScene), 18, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI selectedRecipeText = isHubScene
                ? CreateScreenText("SelectedRecipeText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI upgradeText = isHubScene
                ? CreateScreenText("UpgradeText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI dayPhaseText = CreateScreenText("DayPhaseText", hudStatusGroup, PrototypeUILayout.DayPhaseText, 20, TextAlignmentOptions.Center, chromeText);

            ApplyPopupInventoryTextPresentation(inventoryText);
            ApplyPopupDetailTextPresentation(storageText);
            ApplyPopupDetailTextPresentation(selectedRecipeText);
            ApplyPopupDetailTextPresentation(upgradeText);

            Button skipExplorationButton = isHubScene ? CreateUiButton("SkipExplorationButton", hudButtonGroup, PrototypeUILayout.HubSkipExplorationButton, "\uD0D0\uD5D8 \uC2A4\uD0B5") : null;
            Button skipServiceButton = isHubScene ? CreateUiButton("SkipServiceButton", hudButtonGroup, PrototypeUILayout.HubSkipServiceButton, "\uC601\uC5C5 \uC2A4\uD0B5") : null;
            Button nextDayButton = isHubScene ? CreateUiButton("NextDayButton", hudButtonGroup, PrototypeUILayout.HubNextDayButton, "\uB2E4\uC74C \uB0A0") : null;
            Button recipePanelButton = isHubScene ? CreateUiButton("RecipePanelButton", hudButtonGroup, PrototypeUILayout.HubRecipePanelButton, "\uC694\uB9AC \uBA54\uB274") : null;
            Button upgradePanelButton = isHubScene ? CreateUiButton("UpgradePanelButton", hudButtonGroup, PrototypeUILayout.HubUpgradePanelButton, "\uC5C5\uADF8\uB808\uC774\uB4DC") : null;
            Button materialPanelButton = isHubScene ? CreateUiButton("MaterialPanelButton", hudButtonGroup, PrototypeUILayout.HubMaterialPanelButton, "\uC7AC\uB8CC") : null;
            Button popupCloseButton = isHubScene ? CreateUiButton("PopupCloseButton", popupFrameRightGroup != null ? popupFrameRightGroup : (popupFrameGroup != null ? popupFrameGroup : popupRoot), PrototypeUILayout.HubPopupCloseButton, string.Empty) : null;

            inventoryCaption.text = isHubScene ? "\uC7AC\uB8CC" : "\uC7AC\uB8CC / \uAC00\uBC29";
            if (storageCaption != null) storageCaption.text = "\uCC3D\uACE0";
            if (recipeCaption != null) recipeCaption.text = "\uC694\uB9AC \uBA54\uB274";
            if (upgradeCaption != null) upgradeCaption.text = "\uC5C5\uADF8\uB808\uC774\uB4DC";
            if (actionCaption != null) actionCaption.text = "\uC9C4\uD589";

            inventoryCaption.fontStyle = FontStyles.Bold;
            if (storageCaption != null) storageCaption.fontStyle = FontStyles.Bold;
            if (recipeCaption != null) recipeCaption.fontStyle = FontStyles.Bold;
            if (upgradeCaption != null) upgradeCaption.fontStyle = FontStyles.Bold;
            if (actionCaption != null) actionCaption.fontStyle = FontStyles.Bold;
            inventoryCaption.characterSpacing = 0.5f;
            if (storageCaption != null) storageCaption.characterSpacing = 0.5f;
            if (recipeCaption != null) recipeCaption.characterSpacing = 0.5f;
            if (upgradeCaption != null) upgradeCaption.characterSpacing = 0.5f;
            if (actionCaption != null) actionCaption.characterSpacing = 0.5f;
            inventoryCaption.margin = Vector4.zero;
            if (storageCaption != null) storageCaption.margin = Vector4.zero;
            if (recipeCaption != null) recipeCaption.margin = Vector4.zero;
            if (upgradeCaption != null) upgradeCaption.margin = Vector4.zero;
            if (actionCaption != null) actionCaption.margin = Vector4.zero;

            dayPhaseText.fontStyle = FontStyles.Bold;
            inventoryText.textWrappingMode = TextWrappingModes.Normal;
            inventoryText.overflowMode = TextOverflowModes.Masking;
            if (storageText != null) storageText.textWrappingMode = TextWrappingModes.Normal;
            if (storageText != null) storageText.overflowMode = TextOverflowModes.Masking;
            if (selectedRecipeText != null) selectedRecipeText.textWrappingMode = TextWrappingModes.Normal;
            if (selectedRecipeText != null) selectedRecipeText.overflowMode = TextOverflowModes.Masking;
            if (upgradeText != null) upgradeText.textWrappingMode = TextWrappingModes.Normal;
            if (upgradeText != null) upgradeText.overflowMode = TextOverflowModes.Masking;

            goldText.text = "\uCF54\uC778: 0   \uD3C9\uD310: 0";
            inventoryText.text = "\uC778\uBCA4\uD1A0\uB9AC 0/8\uCE78\n- \uBE44\uC5B4 \uC788\uC74C";
            if (storageText != null) storageText.text = "- \uBE44\uC5B4 \uC788\uC74C";
            promptText.text = "\uC774\uB3D9: WASD / \uBC29\uD5A5\uD0A4   \uC0C1\uD638\uC791\uC6A9: E";
            guideText.text = string.Empty;
            resultText.text = string.Empty;
            if (selectedRecipeText != null) selectedRecipeText.text = "\uC120\uD0DD \uBA54\uB274: \uC5C6\uC74C";
            if (upgradeText != null) upgradeText.text = "- \uC778\uBCA4\uD1A0\uB9AC 8\uCE78 -> 12\uCE78";
            dayPhaseText.text = "1\uC77C\uCC28 \u00B7 \uC624\uC804 \uD0D0\uD5D8";

            if (isHubScene)
            {
                SetChildActive(canvasObject.transform, "CenterBottomPanel", false);
                SetChildActive(canvasObject.transform, "PopupOverlay", false);
                SetChildActive(canvasObject.transform, "PopupFrame", false);
                SetChildActive(canvasObject.transform, "PopupFrameLeft", false);
                SetChildActive(canvasObject.transform, "PopupFrameRight", false);
                SetChildActive(canvasObject.transform, "PopupLeftBody", false);
                SetChildActive(canvasObject.transform, "PopupRightBody", false);
                SetChildActive(canvasObject.transform, "StorageCard", false);
                SetChildActive(canvasObject.transform, "RecipeCard", false);
                SetChildActive(canvasObject.transform, "UpgradeCard", false);
                SetChildActive(canvasObject.transform, "ActionDock", false);
                SetChildActive(canvasObject.transform, "StorageAccent", false);
                SetChildActive(canvasObject.transform, "RecipeAccent", false);
                SetChildActive(canvasObject.transform, "UpgradeAccent", false);
                SetChildActive(canvasObject.transform, "ActionAccent", false);
            }

            SetChildActive(canvasObject.transform, "PromptBackdrop", false);
            SetChildActive(canvasObject.transform, "GuideBackdrop", false);
            SetChildActive(canvasObject.transform, "ResultBackdrop", false);
            SetChildActive(canvasObject.transform, "InventoryCard", false);
            SetChildActive(canvasObject.transform, "InventoryAccent", false);
            inventoryCaption.gameObject.SetActive(false);
            if (storageCaption != null) storageCaption.gameObject.SetActive(false);
            if (recipeCaption != null) recipeCaption.gameObject.SetActive(false);
            if (upgradeCaption != null) upgradeCaption.gameObject.SetActive(false);
            if (actionCaption != null) actionCaption.gameObject.SetActive(false);
            SetChildActive(canvasObject.transform, PopupTitleObjectName, false);
            SetChildActive(canvasObject.transform, PopupLeftCaptionObjectName, false);
            SetChildActive(canvasObject.transform, PopupRightCaptionObjectName, false);
            inventoryText.gameObject.SetActive(false);
            if (storageText != null) storageText.gameObject.SetActive(false);
            guideText.gameObject.SetActive(false);
            resultText.gameObject.SetActive(false);
            if (selectedRecipeText != null) selectedRecipeText.gameObject.SetActive(false);
            if (upgradeText != null) upgradeText.gameObject.SetActive(false);
            if (skipExplorationButton != null) skipExplorationButton.gameObject.SetActive(false);
            if (skipServiceButton != null) skipServiceButton.gameObject.SetActive(false);
            if (nextDayButton != null) nextDayButton.gameObject.SetActive(false);
            if (recipePanelButton != null) recipePanelButton.gameObject.SetActive(false);
            if (upgradePanelButton != null) upgradePanelButton.gameObject.SetActive(false);
            if (materialPanelButton != null) materialPanelButton.gameObject.SetActive(false);
            if (popupCloseButton != null) popupCloseButton.gameObject.SetActive(false);

            SerializedObject so = new(uiManager);
            so.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            so.FindProperty("inventoryText").objectReferenceValue = inventoryText;
            so.FindProperty("storageText").objectReferenceValue = storageText;
            so.FindProperty("upgradeText").objectReferenceValue = upgradeText;
            so.FindProperty("goldText").objectReferenceValue = goldText;
            so.FindProperty("selectedRecipeText").objectReferenceValue = selectedRecipeText;
            so.FindProperty("dayPhaseText").objectReferenceValue = dayPhaseText;
            so.FindProperty("guideText").objectReferenceValue = guideText;
            so.FindProperty("resultText").objectReferenceValue = resultText;
            so.FindProperty("bodyFontAsset").objectReferenceValue = _generatedKoreanFont;
            so.FindProperty("headingFontAsset").objectReferenceValue = _generatedHeadingFont;
            so.FindProperty("skipExplorationButton").objectReferenceValue = skipExplorationButton;
            so.FindProperty("skipServiceButton").objectReferenceValue = skipServiceButton;
            so.FindProperty("nextDayButton").objectReferenceValue = nextDayButton;
            so.FindProperty("recipePanelButton").objectReferenceValue = recipePanelButton;
            so.FindProperty("upgradePanelButton").objectReferenceValue = upgradePanelButton;
            so.FindProperty("materialPanelButton").objectReferenceValue = materialPanelButton;
            so.FindProperty("popupCloseButton").objectReferenceValue = popupCloseButton;
            so.FindProperty("defaultPromptText").stringValue = "\uC774\uB3D9: WASD / \uBC29\uD5A5\uD0A4   \uC0C1\uD638\uC791\uC6A9: E";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureUiEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureInputSystemUiModule(inputModule);
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Input System 기본 액션 생성 경로가 에디터에서 예외를 내는 환경이 있어
        /// 프로젝트 내부 UI 액션 에셋을 직접 만들고 모듈에 연결합니다.
        /// </summary>
        private static void ConfigureInputSystemUiModule(InputSystemUIInputModule inputModule)
        {
            InputActionAsset asset = EnsureUiInputActionsAsset();

            inputModule.actionsAsset = asset;
            inputModule.point = InputActionReference.Create(asset.FindAction("UI/Point", true));
            inputModule.leftClick = InputActionReference.Create(asset.FindAction("UI/LeftClick", true));
            inputModule.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick", true));
            inputModule.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick", true));
            inputModule.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel", true));
            inputModule.move = InputActionReference.Create(asset.FindAction("UI/Move", true));
            inputModule.submit = InputActionReference.Create(asset.FindAction("UI/Submit", true));
            inputModule.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", true));
            inputModule.trackedDevicePosition = InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition", true));
            inputModule.trackedDeviceOrientation = InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation", true));
        }

        private static InputActionAsset EnsureUiInputActionsAsset()
        {
            string assetPath = DataRoot + "/GeneratedUiInputActions.asset";
            InputActionAsset existingAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (existingAsset != null && HasRequiredUiActions(existingAsset))
            {
                return existingAsset;
            }

            if (existingAsset != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "GeneratedUiInputActions";
            InputActionMap uiMap = new("UI");
            asset.AddActionMap(uiMap);

            InputAction pointAction = uiMap.AddAction("Point", InputActionType.PassThrough);
            pointAction.expectedControlType = "Vector2";
            pointAction.AddBinding("<Mouse>/position");
            pointAction.AddBinding("<Pen>/position");
            pointAction.AddBinding("<Touchscreen>/primaryTouch/position");

            InputAction leftClickAction = uiMap.AddAction("LeftClick", InputActionType.PassThrough);
            leftClickAction.expectedControlType = "Button";
            leftClickAction.AddBinding("<Mouse>/leftButton");
            leftClickAction.AddBinding("<Pen>/tip");
            leftClickAction.AddBinding("<Touchscreen>/primaryTouch/press");

            InputAction rightClickAction = uiMap.AddAction("RightClick", InputActionType.PassThrough);
            rightClickAction.expectedControlType = "Button";
            rightClickAction.AddBinding("<Mouse>/rightButton");

            InputAction middleClickAction = uiMap.AddAction("MiddleClick", InputActionType.PassThrough);
            middleClickAction.expectedControlType = "Button";
            middleClickAction.AddBinding("<Mouse>/middleButton");

            InputAction scrollWheelAction = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough);
            scrollWheelAction.expectedControlType = "Vector2";
            scrollWheelAction.AddBinding("<Mouse>/scroll");

            InputAction moveAction = uiMap.AddAction("Move", InputActionType.PassThrough);
            moveAction.expectedControlType = "Vector2";
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            InputAction submitAction = uiMap.AddAction("Submit", InputActionType.Button);
            submitAction.expectedControlType = "Button";
            submitAction.AddBinding("<Keyboard>/enter");
            submitAction.AddBinding("<Keyboard>/numpadEnter");
            submitAction.AddBinding("<Keyboard>/space");
            submitAction.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancelAction = uiMap.AddAction("Cancel", InputActionType.Button);
            cancelAction.expectedControlType = "Button";
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.AddBinding("<Gamepad>/buttonEast");

            InputAction trackedPositionAction = uiMap.AddAction("TrackedDevicePosition", InputActionType.PassThrough);
            trackedPositionAction.expectedControlType = "Vector3";
            trackedPositionAction.AddBinding("<XRController>/devicePosition");

            InputAction trackedOrientationAction = uiMap.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough);
            trackedOrientationAction.expectedControlType = "Quaternion";
            trackedOrientationAction.AddBinding("<XRController>/deviceRotation");

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static bool HasRequiredUiActions(InputActionAsset asset)
        {
            return asset.FindAction("UI/Point") != null
                   && asset.FindAction("UI/LeftClick") != null
                   && asset.FindAction("UI/RightClick") != null
                   && asset.FindAction("UI/MiddleClick") != null
                   && asset.FindAction("UI/ScrollWheel") != null
                   && asset.FindAction("UI/Move") != null
                   && asset.FindAction("UI/Submit") != null
                   && asset.FindAction("UI/Cancel") != null
                   && asset.FindAction("UI/TrackedDevicePosition") != null
                   && asset.FindAction("UI/TrackedDeviceOrientation") != null;
        }
#endif

        /// <summary>
        /// 화면 고정 UI 텍스트를 만들고 generated 한글 폰트와 기본 여백을 같이 적용합니다.
        /// </summary>
        private static TextMeshProUGUI CreateScreenText(
            string objectName,
            Transform _parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject go = new(objectName);
            ApplyHubPopupObjectIdentity(go);
            go.transform.SetParent(_parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            TMP_FontAsset preferredFont = EnsurePreferredTmpFontAsset();
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize - 6f);
            text.fontSizeMax = fontSize;
            text.overflowMode = TextOverflowModes.Truncate;

            if (preferredFont != null)
            {
                text.font = preferredFont;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            return text;
        }

        /// <summary>
        /// 공용 레이아웃 프리셋을 바로 넘겨 HUD 텍스트 생성 중복을 줄입니다.
        /// </summary>
        private static TextMeshProUGUI CreateScreenText(
            string objectName,
            Transform _parent,
            PrototypeUIRect layout,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            return CreateScreenText(
                objectName,
                _parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                fontSize,
                alignment,
                color);
        }

        private static TextMeshProUGUI CreatePopupHeadingText(
            string objectName,
            Transform _parent,
            PrototypeUIRect layout,
            float fontSize,
            float sceneFontSizeMax,
            string content,
            Color color,
            bool enableAutoSizing)
        {
            TextMeshProUGUI text = CreateScreenText(objectName, _parent, layout, fontSize, TextAlignmentOptions.TopLeft, color);
            text.text = content;
            TMP_FontAsset headingFont = EnsureHeadingTmpFontAsset();
            if (headingFont != null)
            {
                text.font = headingFont;
                if (headingFont.material != null)
                {
                    text.fontSharedMaterial = headingFont.material;
                }
            }

            ApplyPopupHeadingPresentation(text, fontSize, sceneFontSizeMax, enableAutoSizing);
            return text;
        }

        private static void ApplyPopupHeadingPresentation(TextMeshProUGUI text, float fontSize, float sceneFontSizeMax, bool enableAutoSizing)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = enableAutoSizing;
            text.fontSize = fontSize;
            text.fontSizeMin = 12f;
            text.fontSizeMax = sceneFontSizeMax;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.overflowMode = TextOverflowModes.Truncate;
        }

        /// <summary>
        /// 왼쪽 본문 인벤토리 텍스트는 Hub.unity 직렬화 값과 같은 크기/여백으로
        /// 맞춰서 빌더 미리보기와 실제 씬 표시 밀도가 달라지지 않게 합니다.
        /// </summary>
        private static void ApplyPopupInventoryTextPresentation(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = 19f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 13f;
            text.fontSizeMax = 19f;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.lineSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Masking;
        }

        private static void ApplyPopupDetailTextPresentation(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = 18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 18f;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.lineSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Masking;
        }

        /// <summary>
        /// 카드 배경이나 포인트 바 같은 평면 UI 블록을 그림자와 함께 생성합니다.
        /// </summary>
        private static void CreatePanel(
            string objectName,
            Transform _parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject panelObject = new(objectName);
            ApplyHubPopupObjectIdentity(panelObject);
            panelObject.transform.SetParent(_parent, false);

            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = panelObject.AddComponent<Image>();
            ApplyScenePanelImagePresentation(image, objectName, color);

            image.raycastTarget = false;

            if (!objectName.EndsWith("Accent"))
            {
                Shadow shadow = panelObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
                shadow.effectDistance = new Vector2(0f, -4f);
                shadow.useGraphicAlpha = true;
            }
        }

        /// <summary>
        /// 공용 레이아웃 프리셋으로 배경 패널을 생성합니다.
        /// </summary>
        private static void CreatePanel(
            string objectName,
            Transform _parent,
            PrototypeUIRect layout,
            Color color)
        {
            CreatePanel(
                objectName,
                _parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                color);
        }

        /// <summary>
        /// 허브 팝업 본문 안에 반복 아이템 박스를 미리 만들어 두면 에디터 프리뷰와 새 씬 기본 구조가 같은 기준을 쓸 수 있습니다.
        /// </summary>
        private static void CreatePopupBodyItemBoxes(
            string bodyName,
            string boxPrefix,
            string iconPrefix,
            string textPrefix,
            Transform popupRoot,
            Color textColor,
            bool isInteractive)
        {
            Transform bodyTransform = FindChildRecursive(popupRoot, bodyName);
            if (bodyTransform == null)
            {
                return;
            }

            Color boxColor = new(0.96f, 0.92f, 0.78f, 1f);
            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"{boxPrefix}{i + 1:00}";
                string iconName = $"{iconPrefix}{i + 1:00}";
                string textName = $"{textPrefix}{i + 1:00}";
                CreatePanel(boxName, bodyTransform, PrototypeUILayout.HubPopupBodyItemBox(i), boxColor);

                Transform boxTransform = FindChildRecursive(bodyTransform, boxName);
                if (boxTransform == null)
                {
                    continue;
                }

                Image boxImage = boxTransform.GetComponent<Image>();
                if (boxImage != null)
                {
                    boxImage.raycastTarget = isInteractive;
                }

                if (isInteractive)
                {
                    Button button = boxTransform.GetComponent<Button>();
                    if (button == null)
                    {
                        button = boxTransform.gameObject.AddComponent<Button>();
                    }

                    button.targetGraphic = boxImage;
                    button.transition = Selectable.Transition.ColorTint;
                    Navigation navigation = button.navigation;
                    navigation.mode = Navigation.Mode.None;
                    button.navigation = navigation;
                }

                CreatePopupBodyItemIcon(iconName, boxTransform);
                TextMeshProUGUI text = CreateScreenText(
                    textName,
                    boxTransform,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    17f,
                    TextAlignmentOptions.TopLeft,
                    textColor);
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.margin = Vector4.zero;
                text.lineSpacing = 0f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = 17f;
                text.rectTransform.offsetMin = new Vector2(74f, 10f);
                text.rectTransform.offsetMax = new Vector2(-14f, -10f);
                text.text = string.Empty;
            }
        }

        private static void CreatePopupBodyItemIcon(string objectName, Transform _parent)
        {
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(40f, 0f),
                    new Vector2(44f, 44f)));

            GameObject iconObject = new(objectName);
            ApplyHubPopupObjectIdentity(iconObject);
            iconObject.transform.SetParent(_parent, false);

            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = iconObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private static RectTransform CreateCanvasGroupRoot(string objectName, Transform _parent, int siblingIndex)
        {
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));

            GameObject groupObject = new(objectName);
            ApplyHubPopupObjectIdentity(groupObject);
            groupObject.transform.SetParent(_parent, false);

            RectTransform rect = groupObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(siblingIndex);
            return rect;
        }

        private static void SetChildActive(Transform _parent, string objectName, bool isActive)
        {
            if (_parent == null)
            {
                return;
            }

            Transform child = FindChildRecursive(_parent, objectName);
            if (child != null)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        private static Transform FindChildRecursive(Transform _parent, string objectName)
        {
            foreach (Transform child in _parent)
            {
                if (child.name == objectName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Transform FindNamedTransformInScene(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                if (root.name == objectName)
                {
                    return root.transform;
                }

                Transform nested = FindChildRecursive(root.transform, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// 빠른 행동 버튼을 만들고 텍스트와 그림자까지 기본 스타일로 맞춥니다.
        /// </summary>
        private static Button CreateUiButton(
            string objectName,
            Transform _parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            string label)
        {
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject buttonObject = new(objectName);
            ApplyHubPopupObjectIdentity(buttonObject);
            buttonObject.transform.SetParent(_parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = buttonObject.AddComponent<Image>();
            ApplySceneButtonImagePresentation(image, objectName, new Color(0.18f, 0.18f, 0.18f, 0.82f));

            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            Button button = buttonObject.AddComponent<Button>();

            TextMeshProUGUI labelText = CreateScreenText(
                objectName + "_Label",
                buttonObject.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                20,
                TextAlignmentOptions.Center,
                Color.white);
            labelText.text = label;
            if (_generatedHeadingFont != null)
            {
                labelText.font = _generatedHeadingFont;
            }

            labelText.fontStyle = FontStyles.Bold;
            labelText.margin = new Vector4(8f, 6f, 8f, 6f);

            return button;
        }

        /// <summary>
        /// 공용 레이아웃 프리셋으로 버튼을 생성해 허브 메뉴/액션 배치를 통일합니다.
        /// </summary>
        private static Button CreateUiButton(
            string objectName,
            Transform _parent,
            PrototypeUIRect layout,
            string label)
        {
            return CreateUiButton(
                objectName,
                _parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                label);
        }

        private static void ApplyHubPopupObjectIdentity(GameObject target)
        {
            if (target == null || !IsHubPopupDisplayObject(target.name))
            {
                return;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            target.layer = uiLayer >= 0 ? uiLayer : 5;
            target.tag = "Player";
        }

        /// <summary>
        /// 허브 팝업은 기존 Hub.unity에 저장된 Image 설정을 우선 적용해
        /// generated 스킨보다 씬에 직접 지정한 소스 이미지를 기준으로 맞춥니다.
        /// </summary>
        private static bool TryApplyHubPopupSceneImage(Image image, string objectName)
        {
            if (image == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (!CachedHubPopupSceneImages.TryGetValue(objectName, out SceneImageSnapshot snapshot))
            {
                return false;
            }

            image.sprite = snapshot.Sprite;
            image.type = snapshot.Type;
            image.color = snapshot.Color;
            image.preserveAspect = snapshot.PreserveAspect;
            return true;
        }

        /// <summary>
        /// 패널 기본 스킨을 적용한 뒤, 씬에서 동기화한 Image 오버라이드가 있으면 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplyScenePanelImagePresentation(Image image, string objectName, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            if (!TryApplyHubPopupSceneImage(image, objectName))
            {
                PrototypeUISkin.ApplyPanel(image, objectName, fallbackColor);
            }

            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        /// <summary>
        /// 버튼 기본 스킨을 적용한 뒤, 씬에서 동기화한 Image 오버라이드가 있으면 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneButtonImagePresentation(Image image, string objectName, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            if (!TryApplyHubPopupSceneImage(image, objectName))
            {
                PrototypeUISkin.ApplyButton(image, objectName, fallbackColor);
            }

            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private static bool IsHubPopupDisplayObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName is "PopupRoot" or "PopupShellGroup" or "PopupFrameHeader" or "PopupOverlay")
            {
                return false;
            }

            return objectName.StartsWith("Popup", StringComparison.Ordinal)
                   || objectName is "InventoryText" or "StorageText" or "SelectedRecipeText" or "UpgradeText";
        }

        private static void CreateWorldLabel(string objectName, Transform _parent, Vector3 localPosition, string content, Color color, float fontSize, int sortingOrder)
        {
            bool isLargeLabel = fontSize >= 3.4f;
            bool isPrimaryLabel = fontSize >= 2.5f;
            TMP_FontAsset preferredFont = isLargeLabel ? EnsureHeadingTmpFontAsset() : EnsurePreferredTmpFontAsset();

            GameObject labelObject = new(objectName);
            if (_parent != null)
            {
                labelObject.transform.SetParent(_parent, false);
                labelObject.transform.localPosition = localPosition;
            }
            else
            {
                labelObject.transform.position = localPosition;
            }

            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.characterSpacing = isLargeLabel ? 0.22f : isPrimaryLabel ? 0.08f : 0.02f;
            text.wordSpacing = 0f;
            text.lineSpacing = 0f;
            text.fontStyle = isLargeLabel || isPrimaryLabel ? FontStyles.Bold : FontStyles.Normal;
            // 런타임 월드 텍스트 외곽선은 적용하되 편집 모드 머티리얼을 오염시키지 않도록 분리합니다.

            if (preferredFont != null)
            {
                text.font = preferredFont;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            float labelScale = isLargeLabel ? 0.30f : isPrimaryLabel ? 0.27f : 0.25f;
            labelObject.transform.localScale = Vector3.one * labelScale;

            MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = sortingOrder;
        }

        private static GameObject CreateFloorZone(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder)
        {
            return CreateDecorBlock(objectName, position, scale, sprite, color, sortingOrder);
        }

        private static GameObject CreateFeaturePad(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color)
        {
            return CreateDecorBlock(objectName, position, scale, sprite, color, 3);
        }

        private static GameObject CreateDecorBlock(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder, Transform _parent = null)
        {
            GameObject go = new(objectName);
            if (_parent != null)
            {
                go.transform.SetParent(_parent, false);
                go.transform.localPosition = position;
            }
            else
            {
                go.transform.position = position;
            }

            go.transform.localScale = scale;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        private static ResourceLibrary CreateResources()
        {
            return new ResourceLibrary
            {
                Fish = CreateResourceAsset(DataRoot + "/Fish.asset", "fish", "생선", "바닷가에서 쉽게 얻을 수 있는 기본 재료입니다.", "바닷가", 10, ResourceRarity.Common),
                Shell = CreateResourceAsset(DataRoot + "/Shell.asset", "shell", "조개", "국물 요리에 쓰기 좋은 바닷가 재료입니다.", "바닷가", 12, ResourceRarity.Common),
                Seaweed = CreateResourceAsset(DataRoot + "/Seaweed.asset", "seaweed", "해초", "향이 좋은 해산 재료입니다.", "바닷가", 8, ResourceRarity.Common),
                Herb = CreateResourceAsset(DataRoot + "/Herb.asset", "herb", "약초", "깊은 숲에서 얻는 향이 짙은 약초입니다.", "깊은 숲", 14, ResourceRarity.Uncommon),
                Mushroom = CreateResourceAsset(DataRoot + "/Mushroom.asset", "mushroom", "버섯", "숲의 그늘 아래에서 자라는 식재료입니다.", "깊은 숲", 16, ResourceRarity.Uncommon),
                GlowMoss = CreateResourceAsset(DataRoot + "/GlowMoss.asset", "glow_moss", "발광 이끼", "폐광산 안쪽의 습한 벽면에서 자라는 희귀 식재료입니다.", "폐광산", 22, ResourceRarity.Rare),
                WindHerb = CreateResourceAsset(DataRoot + "/WindHerb.asset", "wind_herb", "향초", "바람이 센 언덕에서만 자라는 고급 허브입니다.", "바람 언덕", 18, ResourceRarity.Rare)
            };
        }

        private static RecipeLibrary CreateRecipes(ResourceLibrary resources)
        {
            return new RecipeLibrary
            {
                SushiSet = CreateRecipeAsset(
                    DataRoot + "/SushiSet.asset",
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
                    DataRoot + "/SeafoodSoup.asset",
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
                    DataRoot + "/HerbFishSoup.asset",
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
                    DataRoot + "/ForestBasket.asset",
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
                    DataRoot + "/GlowMossStew.asset",
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
                    DataRoot + "/WindHerbSalad.asset",
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
            return new SpriteLibrary
            {
                PlayerFront = CreatePlayerSprite(SpriteRoot + "/PlayerFront.png", "image (2).png"),
                PlayerBack = CreatePlayerSprite(SpriteRoot + "/PlayerBack.png", "image (1).png"),
                PlayerSide = CreatePlayerSprite(SpriteRoot + "/PlayerSide.png", "image.png"),
                Portal = CreateColorSprite(SpriteRoot + "/Portal.png", new Color(0.95f, 0.52f, 0.22f)),
                Selector = CreateColorSprite(SpriteRoot + "/Selector.png", new Color(0.98f, 0.84f, 0.23f)),
                Counter = CreateColorSprite(SpriteRoot + "/Counter.png", new Color(0.84f, 0.34f, 0.24f)),
                Fish = CreateColorSprite(SpriteRoot + "/Fish.png", new Color(0.19f, 0.73f, 0.92f)),
                Shell = CreateColorSprite(SpriteRoot + "/Shell.png", new Color(0.90f, 0.79f, 0.66f)),
                Seaweed = CreateColorSprite(SpriteRoot + "/Seaweed.png", new Color(0.24f, 0.66f, 0.35f)),
                Herb = CreateColorSprite(SpriteRoot + "/Herb.png", new Color(0.47f, 0.78f, 0.27f)),
                Mushroom = CreateColorSprite(SpriteRoot + "/Mushroom.png", new Color(0.71f, 0.55f, 0.36f)),
                GlowMoss = CreateColorSprite(SpriteRoot + "/GlowMoss.png", new Color(0.45f, 0.95f, 0.78f)),
                WindHerb = CreateColorSprite(SpriteRoot + "/WindHerb.png", new Color(0.79f, 0.93f, 0.61f)),
                Floor = CreateColorSprite(SpriteRoot + "/Floor.png", Color.white)
            };
        }

        private static Sprite CreatePlayerSprite(string assetPath, string sourceFileName)
        {
            string sourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), "temperature", sourceFileName);
            string targetFullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            string resourceAssetPath = assetPath.Replace(SpriteRoot, ResourceSpriteRoot);
            string resourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), resourceAssetPath);

            if (File.Exists(sourceFullPath))
            {
                CopyFileIfDifferent(sourceFullPath, targetFullPath);
                CopyFileIfDifferent(sourceFullPath, resourceFullPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(resourceAssetPath, ImportAssetOptions.ForceUpdate);
            }

            Sprite importedSprite = ConfigureSpriteAsset(assetPath, PlayerSpritePixelsPerUnit);
            ConfigureSpriteAsset(resourceAssetPath, PlayerSpritePixelsPerUnit);
            if (importedSprite != null)
            {
                return importedSprite;
            }

            return CreateColorSprite(assetPath, Color.white);
        }

        private static ResourceData CreateResourceAsset(string assetPath, string id, string displayName, string description, string regionTag, int sellPrice, ResourceRarity rarity)
        {
            ResourceData asset = AssetDatabase.LoadAssetAtPath<ResourceData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ResourceData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

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
                item.FindPropertyRelative("Resource").objectReferenceValue = ingredients[index].Resource;
                item.FindPropertyRelative("Amount").intValue = ingredients[index].Amount;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Sprite CreateColorSprite(string assetPath, Color color)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            Texture2D texture = new(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return ConfigureSpriteAsset(assetPath, 100f);
        }

        private static Sprite ConfigureSpriteAsset(string assetPath, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 100;

            TextureImporterSettings spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = new Vector2(0.5f, 0.08f);
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
            BoxCollider2D bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(width, height);
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

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
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

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneRoot + "/Hub.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/Beach.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/DeepForest.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/AbandonedMine.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/WindHill.unity", true)
            };
        }

        private static void EnsureFolder(string _parent, string child)
        {
            string fullPath = _parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(_parent, child);
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
