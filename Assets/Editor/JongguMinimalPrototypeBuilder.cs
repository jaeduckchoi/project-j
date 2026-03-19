#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class JongguMinimalPrototypeBuilder
{
    private const string GeneratedRoot = "Assets/Generated";
    private const string DataRoot = GeneratedRoot + "/GameData";
    private const string SpriteRoot = GeneratedRoot + "/Sprites";
    private const string SceneRoot = "Assets/Scenes";
    private const string FontRoot = GeneratedRoot + "/Fonts";

    private static TMP_FontAsset generatedKoreanFont;

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
        public Sprite Player;
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

    [MenuItem("Tools/Jonggu Restaurant/Clean Missing Scripts In Open Scene", true)]
    private static bool ValidateCleanMissingScriptsInOpenScene()
    {
        return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Tools/Jonggu Restaurant/Clean Missing Scripts In Open Scene")]
    public static void CleanMissingScriptsInOpenScene()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before cleaning missing scripts.");
            return;
        }

        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            Debug.LogWarning("No open scene to clean.");
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

        Debug.Log($"Removed {removedCount} missing script component(s) from the open scene.");
    }

    [MenuItem("Tools/Jonggu Restaurant/Build Minimal Prototype", true)]
    private static bool ValidateBuildMinimalPrototype()
    {
        return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Tools/Jonggu Restaurant/Build Minimal Prototype")]
    public static void BuildMinimalPrototype()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before running the minimal prototype builder.");
            return;
        }

        EnsureFolder("Assets", "Generated");
        EnsureFolder(GeneratedRoot, "GameData");
        EnsureFolder(GeneratedRoot, "Sprites");
        EnsureFolder(GeneratedRoot, "Fonts");
        EnsureFolder("Assets", "Scenes");

        generatedKoreanFont = CreateKoreanFontAsset();
        EnsurePreferredTmpFontAsset();
        SpriteLibrary sprites = CreateSprites();
        ResourceLibrary resources = CreateResources();
        RecipeLibrary recipes = CreateRecipes(resources);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BuildHubScene(resources, recipes, sprites);
        BuildBeachScene(resources, sprites);
        BuildDeepForestScene(resources, sprites);
        BuildAbandonedMineScene(resources, sprites);
        BuildWindHillScene(resources, sprites);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "종구의 식당",
            "최소 프로토타입 씬이 생성되었습니다. Assets/Scenes/Hub.unity를 열고 실행하세요.",
            "OK");
    }

    private static void BuildHubScene(ResourceLibrary resources, RecipeLibrary recipes, SpriteLibrary sprites)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        const float mapWidth = 26f;
        const float mapHeight = 16f;

        GameObject gameManagerObject = CreateGameManager("Hub", "Beach", resources);
        GameObject player = CreatePlayer(new Vector3(-1f, -3f, 0f), sprites.Player);
        CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.94f, 0.90f, 0.82f), 6.2f);
        BoxCollider2D movementBounds = CreateMovementBounds("HubMovementBounds", mapWidth - 2.2f, mapHeight - 2.2f);
        AttachPlayerBoundsLimiter(player, movementBounds);

        CreateFloorZone("HubBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.95f, 0.91f, 0.82f), -20);
        CreateFloorZone("KitchenBand", new Vector3(0f, 3.5f, 0f), new Vector3(mapWidth, 5.5f, 1f), sprites.Floor, new Color(0.70f, 0.55f, 0.40f), -19);
        CreateFloorZone("DiningRug", new Vector3(-0.8f, -2.4f, 0f), new Vector3(16f, 5.5f, 1f), sprites.Floor, new Color(0.83f, 0.71f, 0.55f), -18);
        CreateFloorZone("DoorPad", new Vector3(8.5f, -3f, 0f), new Vector3(4.5f, 4.2f, 1f), sprites.Floor, new Color(0.78f, 0.60f, 0.32f), -17);

        CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.35f, 0.22f, 0.14f));
        CreateDecorBlock("KitchenCounter", new Vector3(0f, 1.7f, 0f), new Vector3(12f, 1f, 1f), sprites.Floor, new Color(0.48f, 0.28f, 0.17f), 2);
        CreateDecorBlock("MenuBoardBack", new Vector3(-7f, 0.2f, 0f), new Vector3(3f, 3.4f, 1f), sprites.Floor, new Color(0.34f, 0.23f, 0.18f), 1);
        CreateDecorBlock("DoorFrame", new Vector3(8.5f, -2.2f, 0f), new Vector3(2.2f, 3.6f, 1f), sprites.Floor, new Color(0.42f, 0.24f, 0.12f), 1);
        CreateDecorBlock("TableLeft", new Vector3(-4.5f, -1.8f, 0f), new Vector3(2.2f, 1.4f, 1f), sprites.Floor, new Color(0.62f, 0.44f, 0.24f), 1);
        CreateDecorBlock("TableRight", new Vector3(2.5f, -1.2f, 0f), new Vector3(2.4f, 1.4f, 1f), sprites.Floor, new Color(0.62f, 0.44f, 0.24f), 1);
        CreateDecorBlock("StorageWall", new Vector3(6.2f, 1.2f, 0f), new Vector3(4.8f, 2.8f, 1f), sprites.Floor, new Color(0.55f, 0.38f, 0.20f), 1);
        CreateDecorBlock("WorkbenchWall", new Vector3(6.2f, -1.9f, 0f), new Vector3(4.8f, 2.2f, 1f), sprites.Floor, new Color(0.46f, 0.33f, 0.23f), 1);

        CreateWorldLabel("RestaurantTitle", null, new Vector3(0f, 6f, 0f), "종구의 식당", Color.black, 4.6f, 40);
        CreateWorldLabel("ExitSign", null, new Vector3(8.5f, -0.4f, 0f), "바닷가로", Color.black, 3.2f, 40);
        CreateWorldLabel("ForestSign", null, new Vector3(11.1f, -0.4f, 0f), "깊은 숲", Color.black, 3.0f, 40);
        CreateWorldLabel("MineSign", null, new Vector3(8.5f, 2.2f, 0f), "폐광산", Color.black, 2.8f, 40);
        CreateWorldLabel("WindHillSign", null, new Vector3(11.1f, 2.2f, 0f), "바람 언덕", Color.black, 2.8f, 40);
        CreateWorldLabel("StorageSign", null, new Vector3(6.2f, 3.1f, 0f), "창고 구역", Color.black, 3f, 40);
        CreateWorldLabel("WorkbenchSign", null, new Vector3(6.2f, -0.1f, 0f), "작업대", Color.black, 3f, 40);

        CreateSpawnPoint("HubEntry", new Vector3(0f, -4.6f, 0f), "HubEntry");
        CreatePortal("GoToBeach", new Vector3(8.5f, -3f, 0f), sprites.Portal, "Beach", "BeachEntry", "바닷가로 이동");
        CreatePortal("GoToDeepForest", new Vector3(11.1f, -3f, 0f), sprites.Portal, "DeepForest", "ForestEntry", "깊은 숲으로 이동");
        CreatePortal(
            "GoToAbandonedMine",
            new Vector3(8.5f, -0.2f, 0f),
            sprites.Portal,
            "AbandonedMine",
            "MineEntry",
            "폐광산으로 이동",
            true,
            ToolType.Lantern,
            0,
            "작업대에서 랜턴을 준비해야 폐광산 안쪽을 안전하게 탐험할 수 있습니다.");
        CreatePortal("GoToWindHill", new Vector3(11.1f, -0.2f, 0f), sprites.Portal, "WindHill", "WindHillEntry", "바람 언덕으로 이동");
        CreateFeaturePad("PortalPad", new Vector3(8.5f, -3.7f, 0f), new Vector3(2.6f, 0.6f, 1f), sprites.Floor, new Color(0.98f, 0.83f, 0.51f));
        CreateFeaturePad("ForestPortalPad", new Vector3(11.1f, -3.7f, 0f), new Vector3(2.1f, 0.6f, 1f), sprites.Floor, new Color(0.70f, 0.86f, 0.44f));
        CreateFeaturePad("MinePortalPad", new Vector3(8.5f, -0.9f, 0f), new Vector3(2.0f, 0.6f, 1f), sprites.Floor, new Color(0.74f, 0.74f, 0.78f));
        CreateFeaturePad("WindPortalPad", new Vector3(11.1f, -0.9f, 0f), new Vector3(2.0f, 0.6f, 1f), sprites.Floor, new Color(0.82f, 0.92f, 0.98f));

        RestaurantManager restaurantManager = CreateRestaurantManager(recipes);
        CreateRecipeSelector(new Vector3(-7f, 0.4f, 0f), sprites.Selector, restaurantManager);
        CreateServiceCounter(new Vector3(0f, 1.9f, 0f), sprites.Counter, restaurantManager);
        StorageManager storageManager = gameManagerObject.GetComponent<StorageManager>();
        UpgradeManager upgradeManager = gameManagerObject.GetComponent<UpgradeManager>();
        CreateStorageStation("StorageSelectDeposit", new Vector3(4.9f, 1.8f, 0f), new Vector3(1.6f, 1.0f, 1f), sprites.Floor, new Color(0.93f, 0.77f, 0.43f), "맡길 품목", storageManager, StorageStationAction.CycleInventorySelection);
        CreateStorageStation("StorageDeposit", new Vector3(7.5f, 1.8f, 0f), new Vector3(1.6f, 1.0f, 1f), sprites.Floor, new Color(0.83f, 0.66f, 0.33f), "맡기기", storageManager, StorageStationAction.StoreSelected);
        CreateStorageStation("StorageSelectWithdraw", new Vector3(4.9f, 0.5f, 0f), new Vector3(1.6f, 1.0f, 1f), sprites.Floor, new Color(0.79f, 0.67f, 0.43f), "꺼낼 품목", storageManager, StorageStationAction.CycleStorageSelection);
        CreateStorageStation("StorageWithdraw", new Vector3(7.5f, 0.5f, 0f), new Vector3(1.6f, 1.0f, 1f), sprites.Floor, new Color(0.68f, 0.54f, 0.29f), "꺼내기", storageManager, StorageStationAction.WithdrawSelected);
        CreateUpgradeStation(new Vector3(6.2f, -1.9f, 0f), new Vector3(2.2f, 1.4f, 1f), sprites.Floor, new Color(0.54f, 0.72f, 0.78f), upgradeManager);

        CreateUiCanvas();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/Hub.unity");
    }

    private static void BuildBeachScene(ResourceLibrary resources, SpriteLibrary sprites)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        const float mapWidth = 30f;
        const float mapHeight = 18f;

        CreateGameManager("Hub", "Beach", resources);
        GameObject player = CreatePlayer(new Vector3(-8.5f, -2.2f, 0f), sprites.Player);
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
        CreateDecorBlock("BoatMark", new Vector3(-11f, -2.1f, 0f), new Vector3(1.8f, 2.8f, 1f), sprites.Floor, new Color(0.87f, 0.38f, 0.21f), 2);

        CreateWorldLabel("BeachTitle", null, new Vector3(0f, 7.1f, 0f), "바닷가", Color.black, 4.2f, 40);
        CreateWorldLabel("BoatLabel", null, new Vector3(-11f, -0.2f, 0f), "식당 복귀", Color.black, 3.2f, 40);

        CreateSpawnPoint("BeachEntry", new Vector3(-9.2f, -2.4f, 0f), "BeachEntry");
        CreatePortal("ReturnToHub", new Vector3(-11f, -3f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동");

        CreateGatherable("FishSpot01", new Vector3(-2f, 2.2f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
        CreateGatherable("FishSpot02", new Vector3(2.8f, 4f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
        CreateGatherable("ShellSpot01", new Vector3(-1f, -3f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
        CreateGatherable("ShellSpot02", new Vector3(4.5f, -1.8f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
        CreateGatherable("SeaweedSpot01", new Vector3(7f, 3.8f, 0f), sprites.Seaweed, resources.Seaweed, ToolType.Sickle, 1, 2, "해초");

        CreateUiCanvas();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/Beach.unity");
    }

    private static void BuildDeepForestScene(ResourceLibrary resources, SpriteLibrary sprites)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        const float mapWidth = 32f;
        const float mapHeight = 20f;

        CreateGameManager("Hub", "Beach", resources);
        GameObject player = CreatePlayer(new Vector3(-11f, -6f, 0f), sprites.Player);
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
        CreateWorldLabel("ForestBoatLabel", null, new Vector3(-12f, -4.4f, 0f), "식당 복귀", Color.black, 3.0f, 40);

        CreateSpawnPoint("ForestEntry", new Vector3(-11.5f, -6.4f, 0f), "ForestEntry");
        CreatePortal("ReturnFromForest", new Vector3(-12f, -6f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동");

        CreateGuideTriggerZone("ForestGuide", new Vector3(-8.4f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "forest_intro", "숲은 갈림길과 늪지대 때문에 인벤토리보다 귀환 동선을 더 자주 확인해야 합니다.");
        CreateMovementModifierZone("ForestSwampZone", new Vector3(1.8f, 1f, 0f), new Vector2(9f, 4.2f), 0.55f, "늪지에서는 이동이 느려집니다. 좁은 길을 따라 움직이면 더 안전합니다.");

        CreateGatherable("HerbPatch01", new Vector3(-4f, -1.1f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
        CreateGatherable("HerbPatch02", new Vector3(4.8f, -3.6f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
        CreateGatherable("MushroomPatch01", new Vector3(2.6f, 4.1f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");
        CreateGatherable("MushroomPatch02", new Vector3(8.5f, 5.2f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");

        CreateUiCanvas();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/DeepForest.unity");
    }

    private static void BuildAbandonedMineScene(ResourceLibrary resources, SpriteLibrary sprites)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        const float mapWidth = 32f;
        const float mapHeight = 20f;

        CreateGameManager("Hub", "Beach", resources);
        GameObject player = CreatePlayer(new Vector3(-11.5f, -5.8f, 0f), sprites.Player);
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
        CreateWorldLabel("MineReturnLabel", null, new Vector3(-12f, -4.4f, 0f), "식당 복귀", Color.white, 3.0f, 40);

        CreateSpawnPoint("MineEntry", new Vector3(-12f, -6.2f, 0f), "MineEntry");
        CreatePortal("ReturnFromMine", new Vector3(-12f, -5.8f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동");

        CreateGuideTriggerZone("MineGuide", new Vector3(-8.8f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "mine_intro", "폐광산은 어둡고 동선이 좁습니다. 안쪽으로 들어가기 전 귀환 길을 먼저 확인하세요.");
        CreateDarknessZone("MineDarkness", new Vector3(4.8f, 0.6f, 0f), new Vector2(18f, 10.8f));

        CreateGatherable("GlowMoss01", new Vector3(4.4f, 3.2f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
        CreateGatherable("GlowMoss02", new Vector3(8.2f, 1.0f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
        CreateGatherable("GlowMoss03", new Vector3(11.6f, 4.4f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");

        CreateUiCanvas();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SceneRoot + "/AbandonedMine.unity");
    }

    private static void BuildWindHillScene(ResourceLibrary resources, SpriteLibrary sprites)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        const float mapWidth = 30f;
        const float mapHeight = 18f;

        CreateGameManager("Hub", "Beach", resources);
        GameObject player = CreatePlayer(new Vector3(-11.5f, -5.2f, 0f), sprites.Player);
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
        CreateWorldLabel("WindReturnLabel", null, new Vector3(-12f, -3.8f, 0f), "식당 복귀", Color.black, 3.0f, 40);
        CreateWorldLabel("WindShortcutLabel", null, new Vector3(-6.8f, -1.0f, 0f), "정상 지름길", Color.black, 2.8f, 40);

        CreateSpawnPoint("WindHillEntry", new Vector3(-12f, -5.6f, 0f), "WindHillEntry");
        CreateSpawnPoint("WindHillShortcutEntry", new Vector3(7.8f, 4.4f, 0f), "WindHillShortcutEntry");
        CreatePortal("ReturnFromWindHill", new Vector3(-12f, -5f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동");
        CreatePortal(
            "WindHillShortcut",
            new Vector3(-6.8f, -2.8f, 0f),
            sprites.Portal,
            "WindHill",
            "WindHillShortcutEntry",
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

        CreateUiCanvas();
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

    private static GameObject CreatePlayer(Vector3 position, Sprite sprite)
    {
        GameObject player = new("Player");
        player.transform.position = position;

        GameObject shadow = CreateDecorBlock("Shadow", Vector3.zero, new Vector3(1f, 0.35f, 1f), sprite, new Color(0f, 0f, 0f, 0.20f), 9, player.transform);
        shadow.transform.localPosition = new Vector3(0f, -0.42f, 0f);

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.22f, 0.47f, 0.94f);
        renderer.sortingOrder = 12;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.9f, 1.05f);

        PlayerController controller = player.AddComponent<PlayerController>();

        GameObject interactionRange = new("InteractionRange");
        interactionRange.transform.SetParent(player.transform, false);
        CircleCollider2D rangeCollider = interactionRange.AddComponent<CircleCollider2D>();
        rangeCollider.isTrigger = true;
        rangeCollider.radius = 1.35f;
        InteractionDetector detector = interactionRange.AddComponent<InteractionDetector>();

        SerializedObject controllerSo = new(controller);
        controllerSo.FindProperty("interactionDetector").objectReferenceValue = detector;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        CreateWorldLabel("PlayerLabel", player.transform, new Vector3(0f, 1.15f, 0f), "종구", new Color(0.05f, 0.08f, 0.16f), 3f, 50);
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

    private static void CreateWall(string name, Vector3 position, Vector3 scale, Sprite sprite, Color color)
    {
        GameObject wall = CreateDecorBlock(name, position, scale, sprite, color, 15);
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static GameObject CreatePortal(
        string name,
        Vector3 position,
        Sprite sprite,
        string targetSceneName,
        string targetSpawnPointId,
        string promptLabel,
        bool requireMorningExplore = true,
        ToolType requiredToolType = ToolType.None,
        int requiredReputation = 0,
        string lockedGuideText = "")
    {
        GameObject portal = CreateDecorBlock(name, position, new Vector3(1.6f, 2.2f, 1f), sprite, new Color(0.94f, 0.50f, 0.18f), 7);
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

        CreateWorldLabel(name + "_Label", portal.transform, new Vector3(0f, 1.5f, 0f), promptLabel, Color.black, 2.6f, 50);
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
        GameObject go = CreateDecorBlock("RecipeSelector", position, new Vector3(1.8f, 1.8f, 1f), sprite, new Color(0.98f, 0.84f, 0.18f), 8);
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;

        RecipeSelectorStation station = go.AddComponent<RecipeSelectorStation>();
        SerializedObject so = new(station);
        so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
        so.FindProperty("promptLabel").stringValue = "메뉴 변경";
        so.ApplyModifiedPropertiesWithoutUndo();

        CreateWorldLabel("RecipeSelectorLabel", go.transform, new Vector3(0f, 1.35f, 0f), "메뉴판", Color.black, 2.6f, 50);
    }

    private static void CreateServiceCounter(Vector3 position, Sprite sprite, RestaurantManager restaurantManager)
    {
        GameObject go = CreateDecorBlock("ServiceCounter", position, new Vector3(2.2f, 1.8f, 1f), sprite, new Color(0.82f, 0.30f, 0.22f), 8);
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;

        ServiceCounterStation station = go.AddComponent<ServiceCounterStation>();
        SerializedObject so = new(station);
        so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
        so.FindProperty("promptLabel").stringValue = "영업 시작";
        so.ApplyModifiedPropertiesWithoutUndo();

        CreateWorldLabel("ServiceCounterLabel", go.transform, new Vector3(0f, 1.35f, 0f), "영업대", Color.black, 2.6f, 50);
    }

    private static void CreateStorageStation(string name, Vector3 position, Vector3 size, Sprite sprite, Color color, string label, StorageManager storageManager, StorageStationAction action)
    {
        GameObject go = CreateDecorBlock(name, position, size, sprite, color, 8);
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

        CreateWorldLabel(name + "_Label", go.transform, new Vector3(0f, 1.2f, 0f), label, Color.black, 2.6f, 50);
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

        CreateWorldLabel("UpgradeStationLabel", go.transform, new Vector3(0f, 1.25f, 0f), "작업대", Color.black, 2.6f, 50);
    }

    private static void CreateGatherable(string name, Vector3 position, Sprite sprite, ResourceData resource, ToolType requiredToolType, int minAmount, int maxAmount, string label)
    {
        CreateFeaturePad(name + "_Pad", position + new Vector3(0f, -0.35f, 0f), new Vector3(1.6f, 0.5f, 1f), sprite, new Color(0f, 0f, 0f, 0.12f));

        GameObject go = CreateDecorBlock(name, position, new Vector3(1.05f, 1.05f, 1f), sprite, Color.white, 6);
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

        CreateWorldLabel(name + "_Label", go.transform, new Vector3(0f, 1.0f, 0f), label, Color.black, 2.4f, 45);
    }

    private static void CreateGuideTriggerZone(string name, Vector3 position, Vector2 size, string hintId, string guideText)
    {
        GameObject go = new(name);
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

    private static void CreateMovementModifierZone(string name, Vector3 position, Vector2 size, float multiplier, string guideText)
    {
        GameObject go = new(name);
        go.transform.position = position;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        MovementModifierZone zone = go.AddComponent<MovementModifierZone>();
        SerializedObject so = new(zone);
        so.FindProperty("movementMultiplier").floatValue = multiplier;
        so.FindProperty("guideText").stringValue = guideText;
        so.FindProperty("hintId").stringValue = name;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateDarknessZone(string name, Vector3 position, Vector2 size)
    {
        GameObject go = new(name);
        go.transform.position = position;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        DarknessZone zone = go.AddComponent<DarknessZone>();
        SerializedObject so = new(zone);
        so.FindProperty("noLanternMovementMultiplier").floatValue = 0.45f;
        so.FindProperty("noLanternGuideText").stringValue = "랜턴이 없으면 폐광산 안쪽을 천천히 더듬어 움직여야 합니다.";
        so.FindProperty("hintId").stringValue = name;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateWindGustZone(string name, Vector3 position, Vector2 size, Vector2 direction, float strength, float activeDuration, float inactiveDuration)
    {
        GameObject go = new(name);
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
        so.FindProperty("hintIdPrefix").stringValue = name;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateSpawnPoint(string name, Vector3 position, string spawnId)
    {
        GameObject go = new(name);
        go.transform.position = position;
        SceneSpawnPoint spawnPoint = go.AddComponent<SceneSpawnPoint>();

        SerializedObject so = new(spawnPoint);
        so.FindProperty("spawnId").stringValue = spawnId;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /*
     * 새로 생성하는 씬에도 카드형 HUD 기본 배치와 텍스트 스타일을 바로 심어 둡니다.
     */
    private static void CreateUiCanvas()
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

        CreatePanel("TopLeftPanel", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(336f, 98f), new Color(0.97f, 0.94f, 0.89f, 0.86f));
        CreatePanel("BottomLeftPanel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(372f, 364f), new Color(0.96f, 0.98f, 0.98f, 0.08f));
        CreatePanel("CenterBottomPanel", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(620f, 58f), new Color(0.07f, 0.11f, 0.17f, 0.78f));
        CreatePanel("TopRightPanel", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(494f, 364f), new Color(0.97f, 0.98f, 0.99f, 0.08f));
        CreatePanel("TopCenterPanel", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(782f, 92f), new Color(0.97f, 0.98f, 0.99f, 0.80f));
        CreatePanel("InventoryCard", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 206f), new Vector2(372f, 176f), new Color(0.98f, 0.98f, 0.99f, 0.84f));
        CreatePanel("StorageCard", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(372f, 176f), new Color(0.98f, 0.98f, 0.99f, 0.84f));
        CreatePanel("RecipeCard", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(494f, 170f), new Color(0.98f, 0.98f, 0.99f, 0.84f));
        CreatePanel("ResultCard", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -192f), new Vector2(494f, 128f), new Color(0.98f, 0.98f, 0.99f, 0.84f));
        CreatePanel("UpgradeCard", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -324f), new Vector2(494f, 118f), new Color(0.98f, 0.98f, 0.99f, 0.84f));
        CreatePanel("ActionDock", canvasObject.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 18f), new Vector2(186f, 154f), new Color(0.10f, 0.15f, 0.22f, 0.90f));
        CreatePanel("TopLeftAccent", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(336f, 8f), new Color(0.77f, 0.49f, 0.16f, 0.95f));
        CreatePanel("TopCenterAccent", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(782f, 8f), new Color(0.77f, 0.49f, 0.16f, 0.95f));
        CreatePanel("InventoryAccent", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 374f), new Vector2(372f, 8f), new Color(0.18f, 0.50f, 0.58f, 0.95f));
        CreatePanel("StorageAccent", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 186f), new Vector2(372f, 8f), new Color(0.33f, 0.49f, 0.27f, 0.95f));
        CreatePanel("RecipeAccent", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(494f, 8f), new Color(0.77f, 0.49f, 0.16f, 0.95f));
        CreatePanel("ResultAccent", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -192f), new Vector2(494f, 8f), new Color(0.69f, 0.37f, 0.28f, 0.95f));
        CreatePanel("UpgradeAccent", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -324f), new Vector2(494f, 8f), new Color(0.68f, 0.57f, 0.17f, 0.95f));
        CreatePanel("ActionAccent", canvasObject.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 164f), new Vector2(186f, 8f), new Color(0.77f, 0.49f, 0.16f, 0.95f));

        UIManager uiManager = canvasObject.AddComponent<UIManager>();

        TextMeshProUGUI statusCaption = CreateScreenText("StatusCaption", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(120f, 22f), 15, TextAlignmentOptions.TopLeft, new Color(0.77f, 0.49f, 0.16f, 0.95f));
        TextMeshProUGUI flowCaption = CreateScreenText("FlowCaption", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(180f, 22f), 15, TextAlignmentOptions.Top, new Color(0.77f, 0.49f, 0.16f, 0.95f));
        TextMeshProUGUI inventoryCaption = CreateScreenText("InventoryCaption", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 350f), new Vector2(120f, 22f), 15, TextAlignmentOptions.TopLeft, new Color(0.18f, 0.50f, 0.58f, 0.95f));
        TextMeshProUGUI storageCaption = CreateScreenText("StorageCaption", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 162f), new Vector2(120f, 22f), 15, TextAlignmentOptions.TopLeft, new Color(0.33f, 0.49f, 0.27f, 0.95f));
        TextMeshProUGUI recipeCaption = CreateScreenText("RecipeCaption", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -22f), new Vector2(160f, 22f), 15, TextAlignmentOptions.TopRight, new Color(0.77f, 0.49f, 0.16f, 0.95f));
        TextMeshProUGUI resultCaption = CreateScreenText("ResultCaption", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -196f), new Vector2(160f, 22f), 15, TextAlignmentOptions.TopRight, new Color(0.69f, 0.37f, 0.28f, 0.95f));
        TextMeshProUGUI upgradeCaption = CreateScreenText("UpgradeCaption", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -328f), new Vector2(160f, 22f), 15, TextAlignmentOptions.TopRight, new Color(0.68f, 0.57f, 0.17f, 0.95f));
        TextMeshProUGUI actionCaption = CreateScreenText("ActionCaption", canvasObject.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 150f), new Vector2(120f, 22f), 15, TextAlignmentOptions.TopRight, new Color(1f, 0.93f, 0.78f, 1f));

        TextMeshProUGUI sceneNameText = CreateScreenText("SceneNameText", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -42f), new Vector2(286f, 34f), 30, TextAlignmentOptions.TopLeft, new Color(0.11f, 0.13f, 0.18f, 1f));
        TextMeshProUGUI goldText = CreateScreenText("GoldText", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -72f), new Vector2(312f, 30f), 22, TextAlignmentOptions.TopLeft, new Color(0.22f, 0.24f, 0.29f));
        TextMeshProUGUI inventoryText = CreateScreenText("InventoryText", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 212f), new Vector2(342f, 132f), 21, TextAlignmentOptions.TopLeft, new Color(0.11f, 0.13f, 0.18f, 1f));
        TextMeshProUGUI storageText = CreateScreenText("StorageText", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(342f, 132f), 20, TextAlignmentOptions.TopLeft, new Color(0.11f, 0.13f, 0.18f, 1f));
        TextMeshProUGUI promptText = CreateScreenText("InteractionPromptText", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(580f, 44f), 24, TextAlignmentOptions.Center, Color.white);
        TextMeshProUGUI selectedRecipeText = CreateScreenText("SelectedRecipeText", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -40f), new Vector2(452f, 114f), 21, TextAlignmentOptions.TopRight, new Color(0.11f, 0.13f, 0.18f, 1f));
        TextMeshProUGUI resultText = CreateScreenText("RestaurantResultText", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -212f), new Vector2(452f, 74f), 19, TextAlignmentOptions.TopRight, new Color(0.11f, 0.13f, 0.18f, 1f));
        TextMeshProUGUI upgradeText = CreateScreenText("UpgradeText", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -344f), new Vector2(452f, 64f), 18, TextAlignmentOptions.TopRight, new Color(0.11f, 0.13f, 0.18f, 1f));
        TextMeshProUGUI dayPhaseText = CreateScreenText("DayPhaseText", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(540f, 30f), 24, TextAlignmentOptions.Top, new Color(0.11f, 0.13f, 0.18f, 1f));
        TextMeshProUGUI guideText = CreateScreenText("GuideText", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(736f, 42f), 18, TextAlignmentOptions.Top, new Color(0.23f, 0.26f, 0.31f));
        Button skipExplorationButton = CreateUiButton("SkipExplorationButton", canvasObject.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 102f), new Vector2(154f, 38f), "탐험 스킵");
        Button skipServiceButton = CreateUiButton("SkipServiceButton", canvasObject.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 58f), new Vector2(154f, 38f), "장사 스킵");
        Button nextDayButton = CreateUiButton("NextDayButton", canvasObject.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 14f), new Vector2(154f, 38f), "다음 날");
        statusCaption.text = "상태";
        flowCaption.text = "오늘의 흐름";
        inventoryCaption.text = "가방";
        storageCaption.text = "창고";
        recipeCaption.text = "오늘의 메뉴";
        resultCaption.text = "영업 결과";
        upgradeCaption.text = "업그레이드";
        actionCaption.text = "빠른 행동";

        statusCaption.fontStyle = FontStyles.Bold;
        flowCaption.fontStyle = FontStyles.Bold;
        inventoryCaption.fontStyle = FontStyles.Bold;
        storageCaption.fontStyle = FontStyles.Bold;
        recipeCaption.fontStyle = FontStyles.Bold;
        resultCaption.fontStyle = FontStyles.Bold;
        upgradeCaption.fontStyle = FontStyles.Bold;
        actionCaption.fontStyle = FontStyles.Bold;
        statusCaption.characterSpacing = 2f;
        flowCaption.characterSpacing = 2f;
        inventoryCaption.characterSpacing = 2f;
        storageCaption.characterSpacing = 2f;
        recipeCaption.characterSpacing = 2f;
        resultCaption.characterSpacing = 2f;
        upgradeCaption.characterSpacing = 2f;
        actionCaption.characterSpacing = 2f;
        statusCaption.margin = Vector4.zero;
        flowCaption.margin = Vector4.zero;
        inventoryCaption.margin = Vector4.zero;
        storageCaption.margin = Vector4.zero;
        recipeCaption.margin = Vector4.zero;
        resultCaption.margin = Vector4.zero;
        upgradeCaption.margin = Vector4.zero;
        actionCaption.margin = Vector4.zero;

        sceneNameText.fontStyle = FontStyles.Bold;
        dayPhaseText.fontStyle = FontStyles.Bold;
        inventoryText.textWrappingMode = TextWrappingModes.Normal;
        storageText.textWrappingMode = TextWrappingModes.Normal;
        selectedRecipeText.textWrappingMode = TextWrappingModes.Normal;
        resultText.textWrappingMode = TextWrappingModes.Normal;
        upgradeText.textWrappingMode = TextWrappingModes.Normal;
        guideText.textWrappingMode = TextWrappingModes.Normal;

        skipExplorationButton.GetComponent<Image>().color = new Color(0.18f, 0.50f, 0.58f, 0.95f);
        skipServiceButton.GetComponent<Image>().color = new Color(0.69f, 0.37f, 0.28f, 0.95f);
        nextDayButton.GetComponent<Image>().color = new Color(0.68f, 0.57f, 0.17f, 0.95f);

        sceneNameText.text = "종구의 식당";
        goldText.text = "골드: 0   평판: 0";
        inventoryText.text = "인벤토리 0/8칸\n- 비어 있음";
        storageText.text = "창고\n- 비어 있음";
        promptText.text = "이동: WASD / 방향키   상호작용: E";
        selectedRecipeText.text = "선택 메뉴\n- 아직 고르지 않았습니다.";
        resultText.text = "영업 결과\n- 아직 영업 전입니다.";
        upgradeText.text = "업그레이드\n- 인벤토리 8칸 -> 12칸";
        dayPhaseText.text = "1일차 · 오전 탐험";
        guideText.text = "오전 탐험 준비 시간입니다. 오늘 갈 지역을 정하고 출발하세요.";

        SerializedObject so = new(uiManager);
        so.FindProperty("interactionPromptText").objectReferenceValue = promptText;
        so.FindProperty("inventoryText").objectReferenceValue = inventoryText;
        so.FindProperty("storageText").objectReferenceValue = storageText;
        so.FindProperty("upgradeText").objectReferenceValue = upgradeText;
        so.FindProperty("sceneNameText").objectReferenceValue = sceneNameText;
        so.FindProperty("goldText").objectReferenceValue = goldText;
        so.FindProperty("selectedRecipeText").objectReferenceValue = selectedRecipeText;
        so.FindProperty("restaurantResultText").objectReferenceValue = resultText;
        so.FindProperty("dayPhaseText").objectReferenceValue = dayPhaseText;
        so.FindProperty("guideText").objectReferenceValue = guideText;
        so.FindProperty("skipExplorationButton").objectReferenceValue = skipExplorationButton;
        so.FindProperty("skipServiceButton").objectReferenceValue = skipServiceButton;
        so.FindProperty("nextDayButton").objectReferenceValue = nextDayButton;
        so.FindProperty("defaultPromptText").stringValue = "이동: WASD / 방향키   상호작용: E";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /*
     * 화면 고정 UI 텍스트를 만들고 generated 한글 폰트와 기본 여백을 같이 적용합니다.
     */
    private static TextMeshProUGUI CreateScreenText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TMP_FontAsset preferredFont = EnsurePreferredTmpFontAsset();
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.margin = new Vector4(10f, 8f, 10f, 8f);
        text.overflowMode = TextOverflowModes.Overflow;

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

    /*
     * 카드 배경이나 포인트 바 같은 평면 UI 블록을 그림자와 함께 생성합니다.
     */
    private static void CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        GameObject panelObject = new(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (!name.EndsWith("Accent"))
        {
            Shadow shadow = panelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;
        }
    }

    /*
     * 빠른 행동 버튼을 만들고 텍스트와 그림자까지 기본 스타일로 맞춥니다.
     */
    private static Button CreateUiButton(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        string label)
    {
        GameObject buttonObject = new(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 0.82f);
        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        shadow.effectDistance = new Vector2(0f, -3f);
        shadow.useGraphicAlpha = true;

        Button button = buttonObject.AddComponent<Button>();

        TextMeshProUGUI labelText = CreateScreenText(
            name + "_Label",
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
        labelText.fontStyle = FontStyles.Bold;
        labelText.margin = new Vector4(8f, 6f, 8f, 6f);

        return button;
    }

    private static void CreateWorldLabel(string name, Transform parent, Vector3 localPosition, string content, Color color, float fontSize, int sortingOrder)
    {
        TMP_FontAsset preferredFont = EnsurePreferredTmpFontAsset();

        GameObject labelObject = new(name);
        if (parent != null)
        {
            labelObject.transform.SetParent(parent, false);
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
        text.characterSpacing = 1.5f;
        text.fontStyle = FontStyles.Bold;
        // Runtime presentation applies world-text outlines without leaking edit-mode materials.

        if (preferredFont != null)
        {
            text.font = preferredFont;
        }
        else if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        labelObject.transform.localScale = Vector3.one * 0.30f;

        MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
        meshRenderer.sortingOrder = sortingOrder;
    }

    private static GameObject CreateFloorZone(string name, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder)
    {
        return CreateDecorBlock(name, position, scale, sprite, color, sortingOrder);
    }

    private static GameObject CreateFeaturePad(string name, Vector3 position, Vector3 scale, Sprite sprite, Color color)
    {
        return CreateDecorBlock(name, position, scale, sprite, color, 3);
    }

    private static GameObject CreateDecorBlock(string name, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder, Transform parent = null)
    {
        GameObject go = new(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
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
            Player = CreateColorSprite(SpriteRoot + "/Player.png", new Color(0.23f, 0.48f, 0.94f)),
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

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static BoxCollider2D CreateMovementBounds(string name, float width, float height)
    {
        GameObject boundsObject = new(name);
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

    /*
     * TMP 컴포넌트 생성 전에 builder가 선호하는 기본 폰트를 다시 묶어 누락 경고를 막습니다.
     */
    private static TMP_FontAsset EnsurePreferredTmpFontAsset()
    {
        TMP_FontAsset preferredFont = generatedKoreanFont;

        if (preferredFont == null)
        {
            preferredFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRoot + "/MalgunGothic SDF.asset");
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

    private static TMP_FontAsset CreateKoreanFontAsset()
    {
        string sourceFontPath = FindSystemKoreanFontPath();
        if (string.IsNullOrWhiteSpace(sourceFontPath))
        {
            Debug.LogWarning("시스템에서 한글 폰트를 찾지 못해 기본 TMP 폰트를 사용합니다.");
            return TMP_Settings.defaultFontAsset;
        }

        string importedFontPath = FontRoot + "/MalgunGothic.ttf";
        string importedFontFullPath = Path.Combine(Directory.GetCurrentDirectory(), importedFontPath);
        CopyFileIfDifferent(sourceFontPath, importedFontFullPath);
        AssetDatabase.ImportAsset(importedFontPath, ImportAssetOptions.ForceUpdate);

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(importedFontPath);
        if (sourceFont == null)
        {
            Debug.LogWarning("가져온 한글 폰트를 불러오지 못해 기본 TMP 폰트를 사용합니다.");
            return TMP_Settings.defaultFontAsset;
        }

        string fontAssetPath = FontRoot + "/MalgunGothic SDF.asset";
        TMP_FontAsset existingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (existingFont != null)
        {
            existingFont.TryAddCharacters(CollectRequiredCharacters());
            EditorUtility.SetDirty(existingFont);

            if (existingFont.material != null)
            {
                EditorUtility.SetDirty(existingFont.material);
            }

            return existingFont;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (fontAsset == null)
        {
            Debug.LogWarning("한글 TMP 폰트 자산 생성에 실패해 기본 폰트를 사용합니다.");
            return TMP_Settings.defaultFontAsset;
        }

        fontAsset.name = "MalgunGothic SDF";
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

                atlasTexture.name = $"MalgunGothic Atlas {index}";
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }
        }

        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
        {
            fontAsset.material.name = "MalgunGothic Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        if (fontAsset.material != null)
        {
            EditorUtility.SetDirty(fontAsset.material);
        }

        return fontAsset;
    }

    private static string FindSystemKoreanFontPath()
    {
        string fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[] candidates =
        {
            Path.Combine(fontsFolder, "malgun.ttf"),
            Path.Combine(fontsFolder, "malgunbd.ttf"),
            Path.Combine(fontsFolder, "gulim.ttc")
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static string CollectRequiredCharacters()
    {
        return "종구의 식당바닷가 깊은 숲 폐광산 바람 언덕 이동 방향키 상호작용 메뉴 변경 영업 시작 메뉴판 영업대 채집하기 생선 조개 해초 약초 버섯 향초 발광 이끼 인벤토리 비어 있음 골드 평판 선택 가능 수량 결과 없음 메뉴를 고르고 영업을 시작하세요 선택된 메뉴가 없습니다 재료가 부족합니다 접시 판매 식당으로 이동 바닷가로 이동 깊은 숲으로 이동 폐광산으로 이동 바람 언덕으로 이동 식당 복귀 생선 한 접시 해물탕 약초 생선탕 숲 버섯 모둠 광채 해물탕 향초 해초 무침 늪지 강풍 랜턴 맡길 품목 꺼낼 품목 지름길 정상 어두운 / : + [] WASD E";
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
#endif

