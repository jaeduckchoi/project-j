using System.Collections.Generic;
using CoreLoop.Core;
using Shared.Data;
using Exploration.Gathering;
using Exploration.Camera;
using Exploration.Player;
using Restaurant;
using Management.Storage;
using Management.Tools;
using Management.Upgrade;
using UnityEngine;
using UnityEngine.SceneManagement;

// World 네임스페이스
namespace Exploration.World
{
    /// <summary>
    /// 씬에 저장된 직렬화 값을 정본으로 두고, 누락된 오브젝트와 참조만 런타임에서 보강한다.
    /// </summary>
    public static class PrototypeSceneRuntimeAugmenter
    {
        private static readonly HashSet<int> RuntimeCreatedObjectIds = new();
        private static Shared.PrototypeGeneratedAssetSettings AssetSettings => Shared.PrototypeGeneratedAssetSettings.GetCurrent();
        private static string HubFloorTileResourcePath => AssetSettings.HubFloorTileResourcePath;
        private static string HubFloorBackgroundResourcePath => AssetSettings.HubFloorBackgroundResourcePath;
        private static string HubWallBackgroundResourcePath => AssetSettings.HubWallBackgroundResourcePath;
        private static string HubFrontOutlineResourcePath => AssetSettings.HubFrontOutlineResourcePath;
        private static string HubBarResourcePath => AssetSettings.HubBarResourcePath;
        private static string HubBarRightResourcePath => AssetSettings.HubBarRightResourcePath;
        private static string HubTableUnlockedResourcePath => AssetSettings.HubTableUnlockedResourcePath;
        private static string HubUpgradeSlotResourcePath => AssetSettings.HubUpgradeSlotResourcePath;
        private static string HubTodayMenuBgResourcePath => AssetSettings.HubTodayMenuBgResourcePath;
        private static string HubTodayMenuItem1ResourcePath => AssetSettings.HubTodayMenuItem1ResourcePath;
        private static string HubTodayMenuItem2ResourcePath => AssetSettings.HubTodayMenuItem2ResourcePath;
        private static string HubTodayMenuItem3ResourcePath => AssetSettings.HubTodayMenuItem3ResourcePath;
        private static string FloorSpriteResourcePath => AssetSettings.FloorSpriteResourcePath;

        public static void EnsureSceneReady(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            RuntimeCreatedObjectIds.Clear();
            CleanupLegacyObjects("PlayerLabel");

            switch (scene.name)
            {
                case "Hub":
                    EnsureHubReadyLayered();
                    break;

                case "Beach":
                    EnsureBeachReady();
                    break;

                case "DeepForest":
                    EnsureDeepForestReady();
                    break;

                case "AbandonedMine":
                    EnsureAbandonedMineReady();
                    break;

                case "WindHill":
                    EnsureWindHillReady();
                    break;
            }
        }

        /// <summary>
        /// 허브를 16:9 고정 화면 아트 기준으로 다시 맞춘다.
        /// 기존 보강 오브젝트가 남아 있어도 배경/오브젝트/전경 레이어를 다시 복구한다.
        /// </summary>
        private static void EnsureHubReadyLayered()
        {
            CleanupLegacyObjects("ExitSign", "ForestSign", "MineSign", "WindHillSign");
            EnsureHubLayeredPresentation();

            UpdatePortalDisplayLabel("GoToBeach", "\uBC14\uB2F7\uAC00");
            UpdatePortalDisplayLabel("GoToDeepForest", "\uAE4A\uC740 \uC232");
            UpdatePortalDisplayLabel("GoToAbandonedMine", "\uD3D0\uAD11\uC0B0");
            UpdatePortalDisplayLabel("GoToWindHill", "\uBC14\uB78C \uC5B8\uB355");

            EnsureObjectPosition("RecipeSelector", HubRoomLayout.RecipeSelectorPosition);
            EnsureObjectScale("RecipeSelector", HubRoomLayout.RecipeSelectorScale);
            EnsureObjectPosition("ServiceCounter", HubRoomLayout.ServiceCounterPosition);
            EnsureObjectScale("ServiceCounter", HubRoomLayout.ServiceCounterScale);
            EnsureObjectPosition("StorageStation", HubRoomLayout.StorageStationPosition);
            EnsureObjectScale("StorageStation", HubRoomLayout.StorageStationScale);
            EnsureObjectPosition("UpgradeStation", HubRoomLayout.UpgradeStationPosition);
            EnsureObjectScale("UpgradeStation", HubRoomLayout.UpgradeStationScale);

            EnsureObjectPosition("GoToBeach", HubRoomLayout.GoToBeachPosition);
            EnsureObjectScale("GoToBeach", HubRoomLayout.PortalScale);
            EnsureObjectPosition("GoToDeepForest", HubRoomLayout.GoToDeepForestPosition);
            EnsureObjectScale("GoToDeepForest", HubRoomLayout.PortalScale);
            EnsureObjectPosition("GoToAbandonedMine", HubRoomLayout.GoToAbandonedMinePosition);
            EnsureObjectScale("GoToAbandonedMine", HubRoomLayout.PortalScale);
            EnsureObjectPosition("GoToWindHill", HubRoomLayout.GoToWindHillPosition);
            EnsureObjectScale("GoToWindHill", HubRoomLayout.PortalScale);

            EnsureObjectPosition("BeachPortalPad", HubRoomLayout.BeachPortalPadPosition);
            EnsureObjectScale("BeachPortalPad", HubRoomLayout.PortalPadScale);
            EnsureObjectPosition("ForestPortalPad", HubRoomLayout.ForestPortalPadPosition);
            EnsureObjectScale("ForestPortalPad", HubRoomLayout.PortalPadScale);
            EnsureObjectPosition("MinePortalPad", HubRoomLayout.MinePortalPadPosition);
            EnsureObjectScale("MinePortalPad", HubRoomLayout.PortalPadScale);
            EnsureObjectPosition("WindPortalPad", HubRoomLayout.WindPortalPadPosition);
            EnsureObjectScale("WindPortalPad", HubRoomLayout.PortalPadScale);
            EnsureSpawnPoint("HubEntry", "HubEntry", HubRoomLayout.HubEntryPosition);

            HideHubInteractionPresentations();

            EnsureMinePortal();
        }

        /// <summary>
        /// 기존 procedural 허브 장식을 정리하고 허브 아트를 레이어별 스프라이트로 다시 맞춘다.
        /// </summary>
        private static void EnsureHubLayeredPresentation()
        {
            CleanupLegacyObjects(
                "HubSingleScreenBackground",
                "HubBase",
                "KitchenBand",
                "DiningRug",
                "DoorPad",
                "DoorFrame",
                "KitchenCounter",
                "MenuBoardBack",
                "StorageWall",
                "WorkbenchWall",
                "TableLeft",
                "TableRight",
                "RestaurantTitle",
                "StorageSign",
                "WorkbenchSign",
                "WorkshopRoomFloor",
                "StorageRoomFloor",
                "UpperDividerWall",
                "WorkshopFrontOccluder",
                "StorageFrontOccluder",
                "WorkshopFrontColliderLeft",
                "WorkshopFrontColliderRight",
                "StorageFrontColliderLeft",
                "StorageFrontColliderRight",
                "WorkshopRoomZone",
                "StorageRoomZone",
                "WorkshopRoomCameraBounds",
                "StorageRoomCameraBounds",
                "HubBarCollider",
                "HubRoomViewRoot",
                "PortalPad",
                "TopWall",
                "BottomWall",
                "LeftWall",
                "RightWall");

            EnsureHubLayeredArt();
            EnsureHubCollisionLayout();

            CameraFollow cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
            BoxCollider2D bounds = EnsureBoundsCollider("CameraBounds", HubRoomLayout.CameraPosition, HubRoomLayout.CameraSize);
            if (cameraFollow != null)
            {
                cameraFollow.SetDefaultBounds(bounds, true);
                UnityEngine.Camera sceneCamera = Object.FindFirstObjectByType<UnityEngine.Camera>();
                float orthographicSize = sceneCamera != null && sceneCamera.orthographicSize > 0f
                    ? sceneCamera.orthographicSize
                    : HubRoomLayout.ScreenOrthographicSize;
                cameraFollow.SetDefaultOrthographicSize(orthographicSize, true);
            }
        }

        private static void EnsureBeachReady()
        {
            CleanupLegacyObjects("BoatLabel");
            EnsureObjectPosition("BoatMark", new Vector3(-13.1f, -2.0f, 0f));
            EnsureObjectPosition("ReturnToHub", new Vector3(-10.7f, -3.35f, 0f));
            EnsureSpawnPoint("BeachEntry", "BeachEntry", new Vector3(-8.25f, -2.25f, 0f));
            UpdatePortalDisplayLabel("ReturnToHub", "식당 복귀");
        }

        private static void EnsureDeepForestReady()
        {
            CleanupLegacyObjects("ForestBoatLabel");
            EnsureObjectPosition("ReturnFromForest", new Vector3(-13.6f, -6.15f, 0f));
            EnsureSpawnPoint("ForestEntry", "ForestEntry", new Vector3(-10.3f, -6.1f, 0f));
            UpdatePortalDisplayLabel("ReturnFromForest", "식당 복귀");
        }

        /// <summary>
        /// 바람 언덕은 복귀 포털 중복 라벨을 제거하고 지름길 포털을 유지한다.
        /// </summary>
        private static void EnsureWindHillReady()
        {
            CleanupLegacyObjects("WindReturnLabel", "WindShortcutLabel");
            EnsureObjectPosition("ReturnFromWindHill", new Vector3(-13.6f, -5.15f, 0f));
            UpdatePortalDisplayLabel("ReturnFromWindHill", "식당 복귀");

            EnsureSpawnPoint("WindHillEntry", "WindHillEntry", new Vector3(-10.7f, -5.35f, 0f));
            EnsureSpawnPoint("WindHillShortcutEntry", "WindHillShortcutEntry", new Vector3(7.8f, 4.4f, 0f));

            ScenePortal templatePortal = FindComponentByName<ScenePortal>("ReturnFromWindHill");
            if (templatePortal == null)
            {
                return;
            }

            ScenePortal shortcutPortal = FindComponentByName<ScenePortal>("WindHillShortcut");
            if (shortcutPortal == null)
            {
                GameObject clone = Object.Instantiate(templatePortal.gameObject);
                clone.name = "WindHillShortcut";
                clone.transform.position = new Vector3(-6.8f, -2.8f, 0f);
                RegisterRuntimeCreated(clone);
                shortcutPortal = clone.GetComponent<ScenePortal>();
            }

            ConfigurePortal(
                shortcutPortal,
                "WindHill",
                "WindHillShortcutEntry",
                "정상 지름길",
                "정상 지름길",
                true,
                    ToolType.None,
                6,
                "평판 6을 모으면 바람 언덕의 지름길을 이용할 수 있습니다.",
                new Color(0.55f, 0.77f, 0.95f));
        }

        /// <summary>
        /// 폐광산은 복귀 라벨 정리와 탐험 보강을 함께 처리한다.
        /// </summary>
        private static void EnsureAbandonedMineReady()
        {
            CleanupLegacyObjects("MineReturnLabel");
            EnsureObjectPosition("ReturnFromMine", new Vector3(-13.6f, -6.0f, 0f));
            UpdatePortalDisplayLabel("ReturnFromMine", "식당 복귀");

            EnsureSpawnPoint("MineEntry", "MineEntry", new Vector3(-10.7f, -6.0f, 0f));
            EnsureMineDarknessZone();
            ConfigureMineSlowZone();
            ConfigureMineGatherable("MushroomPatch01", new Vector3(4.4f, 3.2f, 0f));
            ConfigureMineGatherable("HerbPatch01", new Vector3(8.2f, 1.0f, 0f));
            ConfigureMineGatherable("HerbPatch02", new Vector3(11.6f, 4.4f, 0f));

            GatherableResource extraGatherable = FindComponentByName<GatherableResource>("MushroomPatch02");
            if (extraGatherable != null)
            {
                extraGatherable.gameObject.SetActive(false);
            }

            // 씬에 저장한 MineGuide 값이 런타임에 덮이지 않도록 별도 재설정은 하지 않습니다.
            GuideTriggerZone guideTrigger = FindComponentByName<GuideTriggerZone>("MineGuide");
            if (guideTrigger != null && WasCreatedAtRuntime(guideTrigger.gameObject))
            {
                guideTrigger.Configure(
                    "mine_intro",
                    "폐광산은 어둡고 동선이 좁습니다. 안쪽으로 들어가기 전 귀환 길을 먼저 확인하세요.");
            }

            UpdateTextByObjectName("MineTitle", "폐광산");
        }

        /// <summary>
        /// 허브에 폐광산 포털이 빠져 있으면 기존 포털을 복제해 채운다.
        /// </summary>
        private static void EnsureMinePortal()
        {
            ScenePortal templatePortal = FindComponentByName<ScenePortal>("GoToWindHill");
            if (templatePortal == null)
            {
                templatePortal = FindComponentByName<ScenePortal>("GoToDeepForest");
            }

            if (templatePortal == null)
            {
                return;
            }

            ScenePortal minePortal = FindComponentByName<ScenePortal>("GoToAbandonedMine");
            if (minePortal == null)
            {
                GameObject clone = Object.Instantiate(templatePortal.gameObject);
                clone.name = "GoToAbandonedMine";
                clone.transform.position = new Vector3(9.55f, -0.65f, 0f);
                RegisterRuntimeCreated(clone);
                minePortal = clone.GetComponent<ScenePortal>();
            }

            ConfigurePortal(
                minePortal,
                "AbandonedMine",
                "MineEntry",
                "폐광산으로 이동",
                "폐광산",
                true,
                ToolType.Lantern,
                0,
                "작업대에서 랜턴을 준비해야 폐광산 안쪽을 안전하게 탐험할 수 있습니다.",
                new Color(0.72f, 0.74f, 0.78f));
        }

        private static void EnsureMineDarknessZone()
        {
            GameObject zone = GameObject.Find("MineDarkness");
            bool createdZone = zone == null;
            if (zone == null)
            {
                zone = new GameObject("MineDarkness");
                RegisterRuntimeCreated(zone);
            }

            if (createdZone)
            {
                zone.transform.position = new Vector3(4.8f, 0.6f, 0f);
            }

            BoxCollider2D collider = zone.GetComponent<BoxCollider2D>();
            bool addedCollider = collider == null;
            if (collider == null)
            {
                collider = zone.AddComponent<BoxCollider2D>();
            }

            if (createdZone || addedCollider)
            {
                collider.isTrigger = true;
                collider.size = new Vector2(18f, 10.8f);
            }

            DarknessZone darknessZone = zone.GetComponent<DarknessZone>();
            bool addedDarknessZone = darknessZone == null;
            if (darknessZone == null)
            {
                darknessZone = zone.AddComponent<DarknessZone>();
            }

            if (createdZone || addedDarknessZone)
            {
                darknessZone.Configure(
                    0.45f,
                    "랜턴이 없으면 폐광산 안쪽을 안전하게 이동할 수 없습니다.",
                    "mine_darkness");
            }
        }

        private static void ConfigureMineSlowZone()
        {
            MovementModifierZone slowZone = FindComponentByName<MovementModifierZone>("MineLooseRubble");
            if (slowZone == null)
            {
                MovementModifierZone templateZone = FindComponentByName<MovementModifierZone>("ForestSwampZone");
                if (templateZone == null)
                {
                    return;
                }

                GameObject clone = Object.Instantiate(templateZone.gameObject);
                clone.name = "MineLooseRubble";
                RegisterRuntimeCreated(clone);
                slowZone = clone.GetComponent<MovementModifierZone>();
            }

            if (slowZone == null)
            {
                return;
            }

            if (WasCreatedAtRuntime(slowZone.gameObject))
            {
                slowZone.Configure(
                    0.68f,
                    ToolType.None,
                    "무너진 잔해가 발을 붙잡습니다. 좁은 길에서는 욕심내지 말고 천천히 움직이세요.",
                    "mine_loose_rubble");
            }
        }

        private static void ConfigureMineGatherable(string objectName, Vector3 position)
        {
            GatherableResource gatherable = FindComponentByName<GatherableResource>(objectName);
            ResourceData glowMoss = GeneratedGameDataLocator.FindGeneratedResource("GlowMoss", "발광 이끼");
            if (gatherable == null || glowMoss == null)
            {
                return;
            }

            if (WasCreatedAtRuntime(gatherable.gameObject))
            {
                gatherable.transform.position = position;
            }

            if (WasCreatedAtRuntime(gatherable.gameObject) || !gatherable.HasConfiguredResource)
            {
                gatherable.Configure(glowMoss, ToolType.Lantern);
            }
            UpdatePrimaryRendererColor(gatherable.gameObject, new Color(0.45f, 0.95f, 0.78f));
            UpdateWorldLabel(gatherable.gameObject, "발광 이끼");
        }

        private static void ConfigurePortal(
            ScenePortal portal,
            string targetSceneName,
            string spawnPointId,
            string promptLabel,
            string worldLabel,
            bool requireMorningExplore,
            ToolType requiredToolType,
            int requiredReputation,
            string lockedGuideText,
            Color color)
        {
            if (portal == null)
            {
                return;
            }

            if (WasCreatedAtRuntime(portal.gameObject) || !portal.CanInteract(null))
            {
                portal.Configure(
                    targetSceneName,
                    spawnPointId,
                    promptLabel,
                    requireMorningExplore,
                    requiredToolType,
                    requiredReputation,
                    lockedGuideText);
            }

            UpdatePrimaryRendererColor(portal.gameObject, color);
            UpdateWorldLabel(portal.gameObject, string.IsNullOrWhiteSpace(worldLabel) ? promptLabel : worldLabel);
        }

        private static void EnsureSpawnPoint(string objectName, string spawnId, Vector3 position)
        {
            SceneSpawnPoint spawnPoint = FindComponentByName<SceneSpawnPoint>(objectName);
            bool createdSpawnPoint = spawnPoint == null;
            if (spawnPoint == null)
            {
                GameObject go = new(objectName);
                go.transform.position = position;
                RegisterRuntimeCreated(go);
                spawnPoint = go.AddComponent<SceneSpawnPoint>();
            }

            if (createdSpawnPoint)
            {
                spawnPoint.transform.position = position;
            }

            if (createdSpawnPoint || string.IsNullOrWhiteSpace(spawnPoint.SpawnId))
            {
                spawnPoint.Configure(spawnId);
            }
        }

        private static void CleanupLegacyObjects(params string[] objectNames)
        {
            if (objectNames == null)
            {
                return;
            }

            foreach (string objectName in objectNames)
            {
                if (string.IsNullOrWhiteSpace(objectName))
                {
                    continue;
                }

                GameObject go = GameObject.Find(objectName);
                if (go != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(go);
                    }
                    else
                    {
                        Object.DestroyImmediate(go);
                    }
                }
            }
        }

        private static void UpdatePortalDisplayLabel(string objectName, string worldLabel)
        {
            ScenePortal portal = FindComponentByName<ScenePortal>(objectName);
            if (portal != null)
            {
                UpdateWorldLabel(portal.gameObject, worldLabel);
            }
        }

        private static void UpdatePrimaryRendererColor(GameObject root, Color color)
        {
            if (root == null || !WasCreatedAtRuntime(root))
            {
                return;
            }

            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = root.GetComponentInChildren<SpriteRenderer>();
            }

            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        private static void UpdateWorldLabel(GameObject root, string labelText)
        {
            if (root == null || string.IsNullOrWhiteSpace(labelText))
            {
                return;
            }

            TMPro.TextMeshPro label = root.GetComponentInChildren<TMPro.TextMeshPro>(true);
            if (label == null)
            {
                return;
            }

            if (!WasCreatedAtRuntime(root) && !string.IsNullOrWhiteSpace(label.text))
            {
                return;
            }

            label.text = labelText;
            label.gameObject.name = root.name + "_Label";
            ApplyCompactLabelOffset(root, label);
        }

        private static void UpdateTextByObjectName(string objectName, string text)
        {
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                return;
            }

            TMPro.TextMeshPro textMesh = go.GetComponent<TMPro.TextMeshPro>();
            if (textMesh != null && string.IsNullOrWhiteSpace(textMesh.text))
            {
                textMesh.text = text;
            }
        }

        private static T FindComponentByName<T>(string objectName) where T : Component
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static void EnsureObjectPosition(string objectName, Vector3 position)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            GameObject go = GameObject.Find(objectName);
            if (go == null || !WasCreatedAtRuntime(go))
            {
                return;
            }

            go.transform.position = position;
        }

        private static void EnsureObjectScale(string objectName, Vector3 scale)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            GameObject go = GameObject.Find(objectName);
            if (go == null || !WasCreatedAtRuntime(go))
            {
                return;
            }

            go.transform.localScale = scale;
        }

        private static void EnsureInvisibleWall(string objectName, Vector3 position, Vector3 scale, Transform parent = null)
        {
            GameObject go = GameObject.Find(objectName);
            bool createdWall = go == null;
            if (go == null)
            {
                go = new GameObject(objectName);
                RegisterRuntimeCreated(go);
            }

            if (createdWall)
            {
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = position;
                }
                else
                {
                    go.transform.SetParent(null);
                    go.transform.position = position;
                }

                go.transform.localScale = scale;
            }

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null && createdWall)
            {
                renderer.enabled = false;
            }

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(go);
            if (createdWall)
            {
                collider.size = Vector2.one;
                collider.isTrigger = false;
            }
        }

        private static BoxCollider2D EnsureBoundsCollider(string objectName, Vector3 position, Vector2 size)
        {
            GameObject go = GameObject.Find(objectName);
            bool createdBounds = go == null;
            if (go == null)
            {
                go = new GameObject(objectName);
                RegisterRuntimeCreated(go);
            }

            if (createdBounds)
            {
                go.transform.SetParent(null);
                go.transform.position = position;
                go.transform.localScale = Vector3.one;
            }

            BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
            bool addedCollider = collider == null;
            if (collider == null)
            {
                collider = go.AddComponent<BoxCollider2D>();
            }

            if (createdBounds || addedCollider)
            {
                collider.size = size;
                collider.isTrigger = true;
            }

            return collider;
        }

        private static void EnsureHubLayeredArt()
        {
            CleanupLegacyObjects("HubExploreSignGraphic", "HubWarehouseSignGraphic");
            EnsureHubLayerRoots(out Transform backgroundLayer, out Transform objectLayer, out Transform foregroundLayer, out Transform tableGroup);

            foreach (HubRoomLayout.HubArtPlacement placement in HubRoomLayout.ArtPlacements)
            {
                Transform parent = ResolveHubArtParent(placement.Anchor, backgroundLayer, objectLayer, foregroundLayer);

                if (placement.SpriteId == HubRoomLayout.HubArtSpriteId.FloorBackground)
                {
                    GameObject tiledFloor = EnsureSceneSpriteObject(
                        placement.ObjectName,
                        HubFloorTileResourcePath,
                        placement.LocalPosition,
                        placement.SortingOrder,
                        parent,
                        SpriteDrawMode.Tiled,
                        HubRoomLayout.FloorTileTiledSize,
                        HubRoomLayout.FloorTileScale);
                    SpriteRenderer tiledRenderer = tiledFloor != null ? tiledFloor.GetComponent<SpriteRenderer>() : null;
                    if (tiledRenderer != null && tiledRenderer.sprite != null)
                    {
                        continue;
                    }
                }

                if (placement.SpriteId == HubRoomLayout.HubArtSpriteId.Bar)
                {
                    EnsureHubSplitBarVisuals(placement.ObjectName, placement.LocalPosition, placement.SortingOrder, parent);
                    continue;
                }

                string resourcePath = ResolveHubArtResourcePath(placement.SpriteId);
                EnsureSceneSpriteObject(placement.ObjectName, resourcePath, placement.LocalPosition, placement.SortingOrder, parent);
            }

            EnsureHubTableLayout(tableGroup);
            EnsureHubUpgradeSlotLayout(objectLayer);
            foreach (HubRoomLayout.HubFloorSignPlacement placement in HubRoomLayout.FloorSignPlacements)
            {
                EnsureHubFloorSign(placement, objectLayer);
            }
            EnsureHubTodayMenuBoard(objectLayer);
        }

        private static void EnsureHubLayerRoots(out Transform backgroundLayer, out Transform objectLayer, out Transform foregroundLayer, out Transform tableGroup)
        {
            GameObject artRoot = EnsureRootObject("HubArtRoot", null);
            GameObject backgroundObject = EnsureRootObject("HubBackgroundLayer", artRoot.transform);
            GameObject objectObject = EnsureRootObject("HubObjectLayer", artRoot.transform);
            GameObject foregroundObject = EnsureRootObject("HubForegroundLayer", artRoot.transform);
            GameObject tableObject = EnsureRootObject(HubRoomLayout.TableRootObjectName, objectObject.transform);
            if (WasCreatedAtRuntime(tableObject))
            {
                tableObject.transform.localPosition = HubRoomLayout.TableGroupPosition;
            }

            backgroundLayer = backgroundObject.transform;
            objectLayer = objectObject.transform;
            foregroundLayer = foregroundObject.transform;
            tableGroup = tableObject.transform;
        }

        private static void EnsureHubTableLayout(Transform tableGroup)
        {
            foreach (HubRoomLayout.HubTablePlacement placement in HubRoomLayout.TablePlacements)
            {
                GameObject groupObject = EnsureRootObject(placement.GroupObjectName, tableGroup);
                if (WasCreatedAtRuntime(groupObject))
                {
                    groupObject.transform.localPosition = placement.LocalPosition;
                }

                GameObject tableObject = EnsureSceneSpriteObject(
                    placement.TableObjectName,
                    HubTableUnlockedResourcePath,
                    Vector3.zero,
                    HubRoomLayout.ObjectSortingOrder,
                    groupObject.transform);

                Transform colliderParent = tableObject != null ? tableObject.transform : groupObject.transform;
                EnsureInvisibleWall(placement.ColliderObjectName, placement.ColliderLocalPosition, HubRoomLayout.TableColliderScale, colliderParent);
            }
        }

        private static void EnsureHubUpgradeSlotLayout(Transform objectLayer)
        {
            foreach (HubRoomLayout.HubUpgradeSlotPlacement placement in HubRoomLayout.UpgradeSlotPlacements)
            {
                string resourcePath = ResolveHubArtResourcePath(placement.SpriteId);
                GameObject slotObject = EnsureSceneSpriteObject(
                    placement.SlotObjectName,
                    resourcePath,
                    placement.Position,
                    HubRoomLayout.ObjectSortingOrder,
                    objectLayer);

                Transform priceParent = slotObject != null ? slotObject.transform : objectLayer;
                EnsureHubUpgradePriceText(
                    placement.PriceObjectName,
                    priceParent,
                    HubRoomLayout.UpgradePriceTextLocalOffset,
                    placement.GoldCostLabel);
            }
        }

        private static void EnsureHubCollisionLayout()
        {
            foreach (HubRoomLayout.HubColliderPlacement placement in HubRoomLayout.ColliderPlacements)
            {
                Transform parent = GameObject.Find(placement.ParentObjectName)?.transform;
                EnsureInvisibleWall(placement.ObjectName, placement.LocalPosition, placement.Scale, parent);
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

        private static string ResolveHubArtResourcePath(HubRoomLayout.HubArtSpriteId spriteId)
        {
            return spriteId switch
            {
                HubRoomLayout.HubArtSpriteId.FloorBackground => HubFloorBackgroundResourcePath,
                HubRoomLayout.HubArtSpriteId.WallBackground => HubWallBackgroundResourcePath,
                HubRoomLayout.HubArtSpriteId.Bar => HubBarResourcePath,
                HubRoomLayout.HubArtSpriteId.TableUnlocked => HubTableUnlockedResourcePath,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotLeft => HubUpgradeSlotResourcePath,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotCenter => HubUpgradeSlotResourcePath,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotRight => HubUpgradeSlotResourcePath,
                HubRoomLayout.HubArtSpriteId.FrontOutline => HubFrontOutlineResourcePath,
                _ => string.Empty
            };
        }

        /// <summary>
        /// 레이어 루트는 원점 기준 자식 정렬이 쉽도록 항상 0,0,0과 기본 스케일을 유지한다.
        /// </summary>
        private static GameObject EnsureRootObject(string objectName, Transform parent)
        {
            GameObject go = GameObject.Find(objectName);
            bool createdObject = go == null;
            if (go == null)
            {
                go = new GameObject(objectName);
                RegisterRuntimeCreated(go);
            }

            if (createdObject)
            {
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = Vector3.zero;
                }
                else
                {
                    go.transform.SetParent(null);
                    go.transform.position = Vector3.zero;
                }

                go.transform.localScale = Vector3.one;
            }

            return go;
        }

        /// <summary>
        /// 허브 아트 조각은 Resources 스프라이트를 그대로 올리고, 정렬 순서만 고정한다.
        /// </summary>
        private static GameObject EnsureSceneSpriteObject(
            string objectName,
            string resourcePath,
            Vector3 position,
            int sortingOrder,
            Transform parent,
            SpriteDrawMode drawMode = SpriteDrawMode.Simple,
            Vector2? tiledSize = null,
            Vector3? localScale = null)
        {
            GameObject go = GameObject.Find(objectName);
            bool createdObject = go == null;
            if (go == null)
            {
                go = new GameObject(objectName);
                RegisterRuntimeCreated(go);
            }

            if (createdObject)
            {
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = position;
                }
                else
                {
                    go.transform.SetParent(null);
                    go.transform.position = position;
                }

                go.transform.localScale = localScale ?? Vector3.one;
            }

            SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(go);
            bool applyDefaultPresentation = createdObject || renderer.sprite == null;
            if (applyDefaultPresentation)
            {
                renderer.sprite = Resources.Load<Sprite>(resourcePath);
                renderer.color = Color.white;
                renderer.sortingOrder = sortingOrder;
                renderer.drawMode = drawMode;
                if (tiledSize.HasValue)
                {
                    renderer.size = tiledSize.Value;
                }

                renderer.enabled = renderer.sprite != null;
            }

            return go;
        }

        private static void EnsureHubSplitBarVisuals(string objectName, Vector3 position, int sortingOrder, Transform parent)
        {
            GameObject root = GameObject.Find(objectName);
            bool createdRoot = root == null;
            if (root == null)
            {
                root = new GameObject(objectName);
                RegisterRuntimeCreated(root);
            }

            if (createdRoot)
            {
                if (parent != null)
                {
                    root.transform.SetParent(parent, false);
                    root.transform.localPosition = position;
                }
                else
                {
                    root.transform.SetParent(null);
                    root.transform.position = position;
                }

                root.transform.localScale = Vector3.one;
                root.SetActive(true);
            }

            SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
            if (rootRenderer != null && createdRoot)
            {
                rootRenderer.enabled = false;
            }

            EnsureSceneSpriteObject(
                HubRoomLayout.BarLeftVisualObjectName,
                HubBarResourcePath,
                HubRoomLayout.BarLeftVisualLocalPosition,
                sortingOrder,
                root.transform,
                SpriteDrawMode.Sliced,
                HubRoomLayout.BarLeftVisualSize);

            EnsureSceneSpriteObject(
                HubRoomLayout.BarRightVisualObjectName,
                HubBarRightResourcePath,
                HubRoomLayout.BarRightVisualLocalPosition,
                sortingOrder,
                root.transform,
                SpriteDrawMode.Sliced,
                HubRoomLayout.BarRightVisualSize);
        }

        /// <summary>
        /// 허브 업그레이드 가격은 슬롯 자식 텍스트로 유지해 슬롯 기준 배치를 함께 따라가게 만든다.
        /// 기존 스프라이트가 남아 있으면 텍스트만 보이도록 렌더러를 끈다.
        /// </summary>
        private static void EnsureHubUpgradePriceText(string objectName, Transform parent, Vector3 localPosition, string content)
        {
            TMPro.TextMeshPro text = EnsureWorldTextObject(
                objectName,
                parent,
                localPosition,
                content,
                HubRoomLayout.UpgradePriceTextColor,
                HubRoomLayout.UpgradePriceFontSize,
                HubRoomLayout.SignTextSortingOrder,
                labelScale: HubRoomLayout.UpgradePriceTextScale,
                fontStyle: TMPro.FontStyles.Bold,
                characterSpacing: 0.08f);

            if (text == null)
            {
                return;
            }

            SpriteRenderer renderer = text.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = null;
                renderer.enabled = false;
            }
        }

        /// <summary>
        /// 허브 바닥 표시는 별도 그림 대신 얇은 바닥 패널과 텍스트로 다시 맞춘다.
        /// </summary>
        private static void EnsureHubFloorSign(HubRoomLayout.HubFloorSignPlacement placement, Transform parent)
        {
            GameObject sign = EnsureDecorSpriteObject(
                placement.ObjectName,
                FloorSpriteResourcePath,
                placement.Position,
                placement.BackdropScale,
                HubRoomLayout.SignBackdropColor,
                HubRoomLayout.SignSortingOrder,
                parent);

            EnsureWorldTextObject(
                placement.ObjectName + "Label",
                sign != null ? sign.transform : parent,
                placement.TextLocalPosition,
                placement.Content,
                HubRoomLayout.SignTextColor,
                placement.FontSize,
                HubRoomLayout.SignTextSortingOrder,
                labelScale: placement.TextScale,
                fontStyle: TMPro.FontStyles.Bold,
                characterSpacing: placement.CharacterSpacing);
        }

        /// <summary>
        /// 허브 벽면의 오늘의 메뉴 보드를 보강한다.
        /// 빌더로 다시 만들지 않은 기존 씬에서도 동일한 자리와 슬롯 구성을 채운다.
        /// </summary>
        private static void EnsureHubTodayMenuBoard(Transform parent)
        {
            CleanupLegacyObjects(
                "HubTodayMenuHeaderBackdrop",
                "HubTodayMenuEntryLabel1",
                "HubTodayMenuEntryLabel2",
                "HubTodayMenuEntryLabel3");

            GameObject boardRoot = EnsureRootObject("HubTodayMenuBoard", parent);
            if (WasCreatedAtRuntime(boardRoot))
            {
                boardRoot.transform.localPosition = HubRoomLayout.TodayMenuBoardPosition;
            }

            EnsureWorldTextObject(
                "HubTodayMenuHeaderShadow",
                boardRoot.transform,
                HubRoomLayout.TodayMenuHeaderLabelLocalPosition + HubRoomLayout.TodayMenuHeaderShadowLocalOffset,
                "오늘의 메뉴",
                HubRoomLayout.TodayMenuHeaderShadowColor,
                HubRoomLayout.TodayMenuHeaderFontSize,
                HubRoomLayout.TodayMenuItemSortingOrder,
                labelScale: HubRoomLayout.TodayMenuHeaderTextScale,
                fontStyle: TMPro.FontStyles.Bold,
                characterSpacing: 0.04f);

            TMPro.TextMeshPro headerLabel = EnsureWorldTextObject(
                "HubTodayMenuHeaderLabel",
                boardRoot.transform,
                HubRoomLayout.TodayMenuHeaderLabelLocalPosition,
                "오늘의 메뉴",
                HubRoomLayout.TodayMenuHeaderTextColor,
                HubRoomLayout.TodayMenuHeaderFontSize,
                HubRoomLayout.TodayMenuTextSortingOrder,
                labelScale: HubRoomLayout.TodayMenuHeaderTextScale,
                fontStyle: TMPro.FontStyles.Bold,
                characterSpacing: 0.04f);

            string[] itemResourcePaths =
            {
                HubTodayMenuItem2ResourcePath,
                HubTodayMenuItem1ResourcePath,
                HubTodayMenuItem3ResourcePath
            };

            SpriteRenderer[] entryBackdrops = new SpriteRenderer[HubRoomLayout.TodayMenuEntryLocalPositions.Length];
            SpriteRenderer[] entryIcons = new SpriteRenderer[HubRoomLayout.TodayMenuEntryLocalPositions.Length];

            for (int i = 0; i < HubRoomLayout.TodayMenuEntryLocalPositions.Length; i++)
            {
                GameObject entryBackdrop = EnsureDecorSpriteObject(
                    $"HubTodayMenuEntryBackdrop{i + 1}",
                    HubTodayMenuBgResourcePath,
                    HubRoomLayout.TodayMenuEntryLocalPositions[i],
                    HubRoomLayout.TodayMenuEntryBackdropScale,
                    HubRoomLayout.TodayMenuBackdropColor,
                    HubRoomLayout.TodayMenuBackdropSortingOrder,
                    boardRoot.transform);

                entryBackdrops[i] = entryBackdrop != null ? entryBackdrop.GetComponent<SpriteRenderer>() : null;

                GameObject entryItem = EnsureDecorSpriteObject(
                    $"HubTodayMenuEntryItem{i + 1}",
                    itemResourcePaths[i],
                    HubRoomLayout.TodayMenuEntryIconLocalOffset,
                    HubRoomLayout.TodayMenuEntryIconScale,
                    HubRoomLayout.TodayMenuIconColor,
                    HubRoomLayout.TodayMenuItemSortingOrder,
                    entryBackdrop != null ? entryBackdrop.transform : boardRoot.transform);

                entryIcons[i] = entryItem != null ? entryItem.GetComponent<SpriteRenderer>() : null;
            }

            RestaurantManager restaurantManager = Object.FindFirstObjectByType<RestaurantManager>();
            HubTodayMenuDisplay display = GetOrAddComponent<HubTodayMenuDisplay>(boardRoot);
            display.Configure(restaurantManager, headerLabel, entryBackdrops, entryIcons);
        }

        private static GameObject EnsureDecorSpriteObject(
            string objectName,
            string resourcePath,
            Vector3 position,
            Vector3 scale,
            Color color,
            int sortingOrder,
            Transform parent)
        {
            GameObject go = GameObject.Find(objectName);
            bool createdObject = go == null;
            if (go == null)
            {
                go = new GameObject(objectName);
                RegisterRuntimeCreated(go);
            }

            if (createdObject)
            {
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = position;
                }
                else
                {
                    go.transform.SetParent(null);
                    go.transform.position = position;
                }

                go.transform.localScale = scale;
            }

            SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(go);
            bool applyDefaultPresentation = createdObject || renderer.sprite == null;
            if (applyDefaultPresentation)
            {
                renderer.sprite = Resources.Load<Sprite>(resourcePath);
                renderer.color = color;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = renderer.sprite != null;
            }

            return go;
        }

        private static TMPro.TextMeshPro EnsureWorldTextObject(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            string content,
            Color color,
            float fontSize,
            int sortingOrder,
            float? labelScale = null,
            TMPro.FontStyles? fontStyle = null,
            float? characterSpacing = null)
        {
            GameObject go = GameObject.Find(objectName);
            bool createdObject = go == null;
            if (go == null)
            {
                go = new GameObject(objectName);
                RegisterRuntimeCreated(go);
            }

            if (createdObject)
            {
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = localPosition;
                }
                else
                {
                    go.transform.SetParent(null);
                    go.transform.position = localPosition;
                }
            }

            bool isLargeLabel = fontSize >= 3.4f;
            bool isPrimaryLabel = fontSize >= 2.5f;
            float resolvedLabelScale = labelScale ?? (isLargeLabel ? 0.39f : isPrimaryLabel ? 0.36f : 0.33f);

            TMPro.TextMeshPro text = GetOrAddComponent<TMPro.TextMeshPro>(go);
            bool applyDefaultPresentation = createdObject || string.IsNullOrWhiteSpace(text.text);
            if (applyDefaultPresentation)
            {
                go.transform.localScale = Vector3.one * resolvedLabelScale;
                text.text = content;
                text.fontSize = fontSize;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.color = color;
                text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                text.characterSpacing = characterSpacing ?? (isLargeLabel ? 0.22f : isPrimaryLabel ? 0.08f : 0.02f);
                text.wordSpacing = 0f;
                text.lineSpacing = 0f;
                text.fontStyle = fontStyle ?? (isLargeLabel || isPrimaryLabel ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal);

                if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    text.font = TMPro.TMP_Settings.defaultFontAsset;
                }

                ApplyWorldTextReadability(text, isLargeLabel || isPrimaryLabel);

                MeshRenderer renderer = text.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = sortingOrder;
                }
            }

            return text;
        }

        /// <summary>
        /// 런타임 보강으로 다시 만든 월드 텍스트도 동일한 외곽선과 패딩을 적용해 가독성을 맞춘다.
        /// </summary>
        private static void ApplyWorldTextReadability(TMPro.TextMeshPro text, bool useStrongOutline)
        {
            if (text == null)
            {
                return;
            }

            text.extraPadding = true;

            if (!Application.isPlaying)
            {
                return;
            }

            text.outlineColor = HubRoomLayout.WorldTextOutlineColor;
            text.outlineWidth = useStrongOutline
                ? HubRoomLayout.WorldTextStrongOutlineWidth
                : HubRoomLayout.WorldTextNormalOutlineWidth;
        }

        private static void HideWorldPresentation(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            GameObject root = GameObject.Find(objectName);
            if (root == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (TMPro.TextMeshPro label in root.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            {
                label.gameObject.SetActive(false);
            }
        }

        private static void HideHubInteractionPresentations()
        {
            foreach (string objectName in HubRoomLayout.HiddenInteractionObjectNames)
            {
                HideWorldPresentation(objectName);
            }
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (go == null)
            {
                return null;
            }

            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }

        private static void ApplyCompactLabelOffset(GameObject root, TMPro.TextMeshPro label)
        {
            if (root == null || label == null)
            {
                return;
            }

            float? compactY = null;

            if (root.GetComponent<ScenePortal>() != null)
            {
                compactY = 0.82f;
            }
            else if (root.GetComponent<RecipeSelectorStation>() != null || root.GetComponent<ServiceCounterStation>() != null)
            {
                compactY = 0.80f;
            }
            else if (root.GetComponent<StorageStation>() != null)
            {
                compactY = 0.72f;
            }
            else if (root.GetComponent<UpgradeStation>() != null)
            {
                compactY = 0.68f;
            }
            else if (root.GetComponent<GatherableResource>() != null)
            {
                compactY = 0.64f;
            }
            else if (root.GetComponent<PlayerController>() != null)
            {
                compactY = 0.46f;
            }

            if (!compactY.HasValue)
            {
                return;
            }

            Transform labelTransform = label.transform;
            Vector3 localPosition = labelTransform.localPosition;
            labelTransform.localPosition = new Vector3(localPosition.x, compactY.Value, localPosition.z);
        }

        private static void RegisterRuntimeCreated(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            RuntimeCreatedObjectIds.Add(gameObject.GetInstanceID());
        }

        private static bool WasCreatedAtRuntime(GameObject gameObject)
        {
            return gameObject != null && RuntimeCreatedObjectIds.Contains(gameObject.GetInstanceID());
        }
    }
}
