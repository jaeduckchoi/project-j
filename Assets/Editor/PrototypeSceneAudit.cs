#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exploration.Player;
using Exploration.World;
using Shared;
using UI;
using UI.Layout;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ProjectEditor 네임스페이스
namespace Editor
{
    /// <summary>
    /// 생성된 프로토타입 씬의 기본 구조를 점검하는 간단한 감사 도구입니다.
    /// 허브 전용 UI가 탐험 씬에 남았는지, 플레이어 비주얼이 중복되는지, 구 라벨이 남았는지를 확인합니다.
    /// </summary>
    public static class PrototypeSceneAudit
    {
        private static PrototypeGeneratedAssetSettings AssetSettings => PrototypeGeneratedAssetSettings.GetCurrent();
        private static string[] ScenePaths => AssetSettings.ManagedScenePaths;

        private static readonly string[] CommonHudNames =
        {
            "TopLeftPanel",
            "InteractionPromptBackdrop",
            "GoldText",
            "InteractionPromptText",
            "GuideBackdrop",
            "GuideText",
            "GuideHelpButton",
            "ResultBackdrop",
            "RestaurantResultText",
        };

        private static readonly string[] HubOnlyHudNames =
        {
            "PopupOverlay",
            "PopupFrame",
            "PopupFrameLeft",
            "PopupFrameRight",
            "PopupLeftBody",
            "PopupRightBody",
            PrototypeUIObjectNames.PopupTitle,
            PrototypeUIObjectNames.PopupLeftCaption,
            PrototypeUIObjectNames.PopupRightCaption,
            "PopupCloseButton",
            "InventoryText",
            "StorageText",
            "SelectedRecipeText",
            "UpgradeText",
            "ActionAccent",
            "ActionDock",
            "ActionCaption",
            "RecipePanelButton",
            "UpgradePanelButton",
            "MaterialPanelButton",
        };

        private static readonly string[] RemovedHubCardNames =
        {
            "InventoryCard",
            "InventoryAccent",
            "InventoryCaption",
            "StorageAccent",
            "StorageCard",
            "StorageCaption",
            "RecipeAccent",
            "RecipeCard",
            "RecipeCaption",
            "UpgradeAccent",
            "UpgradeCard",
            "UpgradeCaption",
        };

        private static readonly string[] RemovedDayRoutineNames =
        {
            "PhaseBadge",
            "DayPhaseText",
            "SkipExplorationButton",
            "SkipServiceButton",
            "NextDayButton",
        };

        private static readonly string[] LegacyLabelNames =
        {
            "ExitSign",
            "ForestSign",
            "MineSign",
            "WindHillSign",
            "BoatLabel",
            "ForestBoatLabel",
            "MineReturnLabel",
            "WindReturnLabel",
            "WindShortcutLabel",
        };

        internal static void AuditGeneratedScenes()
        {
            List<string> issues = new();

            foreach (string scenePath in ScenePaths)
            {
                if (!JongguMinimalPrototypeBuilder.TryDescribeManagedSceneFileIssue(scenePath, out string sceneFileIssue))
                {
                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    issues.Add($"[{sceneName}] {sceneFileIssue}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                issues.AddRange(AuditScene(scene, scenePath));
            }

            if (issues.Count == 0)
            {
                Debug.Log("Generated scene audit passed.");
                return;
            }

            foreach (string issue in issues)
            {
                Debug.LogError(issue);
            }

            throw new InvalidOperationException($"Generated scene audit failed with {issues.Count} issue(s).");
        }

        private static IEnumerable<string> AuditScene(Scene scene, string scenePath)
        {
            List<string> issues = new();
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            bool isHubScene = string.Equals(sceneName, "Hub", StringComparison.OrdinalIgnoreCase);
            string hudActionGroupName = PrototypeUISceneLayoutCatalog.ResolveObjectName("HUDActionGroup");
            string hudPanelButtonGroupName = PrototypeUISceneLayoutCatalog.ResolveObjectName("HUDPanelButtonGroup");

            List<GameObject> objects = GetAllSceneObjects(scene);
            bool hasPersistedManagedCanvasHierarchy = HasPersistedManagedCanvasHierarchy(scene, isHubScene);
            ValidateExpectedSceneHierarchy(issues, sceneName, objects);

            ValidateExactCount(issues, sceneName, objects, "Canvas", 1);
            ValidateComponentCount<UIManager>(issues, sceneName, objects, 1);
            ValidateExactCount(issues, sceneName, objects, "Jonggu", 1);
            ValidateComponentCount<RoomViewController>(issues, sceneName, objects, 0);
            ValidateChildCount(issues, sceneName, objects, "Jonggu", "PlayerVisual", 1);
            ValidateComponentOnNamedObject<PlayerDirectionalSprite>(issues, sceneName, objects, "Jonggu");

            if (hasPersistedManagedCanvasHierarchy)
            {
                ValidateExactCount(issues, sceneName, objects, "HUDRoot", 1);
                ValidateExactCount(issues, sceneName, objects, hudActionGroupName, 1);
                ValidateExactCount(issues, sceneName, objects, "HUDBottomGroup", 1);
                ValidateExactCount(issues, sceneName, objects, "HUDInventoryGroup", 0);
                ValidateExactCount(issues, sceneName, objects, "HUDButtonGroup", 0);
                ValidateChildCount(issues, sceneName, objects, "HUDRoot", "HUDStatusGroup", 1);
                ValidateChildCount(issues, sceneName, objects, "HUDRoot", hudActionGroupName, 1);
                ValidateChildCount(issues, sceneName, objects, "HUDRoot", "HUDBottomGroup", 1);
                ValidateChildCount(issues, sceneName, objects, "HUDRoot", hudPanelButtonGroupName, isHubScene ? 1 : 0);
                ValidateChildCount(issues, sceneName, objects, "HUDRoot", "InteractionPromptBackdrop", 1);
                ValidateChildCount(issues, sceneName, objects, "HUDRoot", "InteractionPromptText", 1);
                ValidateChildCount(issues, sceneName, objects, "HUDRoot", "HUDOverlayGroup", 1);
                ValidateChildCount(issues, sceneName, objects, "HUDOverlayGroup", "GuideHelpButton", 1);

                foreach (string hudName in CommonHudNames)
                {
                    ValidateExactCount(issues, sceneName, objects, hudName, 1);
                }

                foreach (string hudName in HubOnlyHudNames)
                {
                    ValidateExactCount(issues, sceneName, objects, hudName, isHubScene ? 1 : 0);
                }

                ValidateExactCount(issues, sceneName, objects, hudPanelButtonGroupName, isHubScene ? 1 : 0);

                foreach (string removedName in RemovedHubCardNames)
                {
                    ValidateExactCount(issues, sceneName, objects, removedName, 0);
                }

                foreach (string removedName in RemovedDayRoutineNames)
                {
                    ValidateExactCount(issues, sceneName, objects, removedName, 0);
                }
            }

            foreach (string legacyLabel in LegacyLabelNames)
            {
                ValidateExactCount(issues, sceneName, objects, legacyLabel, 0);
            }

            ValidateExactCount(issues, sceneName, objects, "HubArtRoot", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubBackgroundLayer", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubObjectLayer", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubForegroundLayer", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, HubRoomLayout.TableRootObjectName, isHubScene ? 1 : 0);

            ValidateExactCount(issues, sceneName, objects, "HubFloorBackground", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubWallBackground", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Mosaic Tile Wall", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Back Counter", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Mosaic Tile Floor", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Front Counter", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Front Counter Collider", isHubScene ? 1 : 0);

            ValidateExactCount(issues, sceneName, objects, "HubTableTopGroup", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableMiddleGroup", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableBottomGroup", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Table Chair2 Top", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Table Chair2 Middle", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Table Chair2 Bottom", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableTopCollider", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableMiddleCollider", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableBottomCollider", isHubScene ? 1 : 0);

            ValidateChildCount(issues, sceneName, objects, "HubBackgroundLayer", "HubFloorBackground", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubBackgroundLayer", "HubWallBackground", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubBackgroundLayer", "Mosaic Tile Wall", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubObjectLayer", HubRoomLayout.TableRootObjectName, isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubObjectLayer", "Back Counter", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubObjectLayer", "Mosaic Tile Floor", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubObjectLayer", "Front Counter", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubObjectLayer", "HubTodayMenuBoard", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubForegroundLayer", "HubFrontOutline", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "Front Counter", "Front Counter Collider", isHubScene ? 1 : 0);

            ValidateChildCount(issues, sceneName, objects, HubRoomLayout.TableRootObjectName, "HubTableTopGroup", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, HubRoomLayout.TableRootObjectName, "HubTableMiddleGroup", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, HubRoomLayout.TableRootObjectName, "HubTableBottomGroup", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTableTopGroup", "Table Chair2 Top", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTableMiddleGroup", "Table Chair2 Middle", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTableBottomGroup", "Table Chair2 Bottom", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "Table Chair2 Top", "HubTableTopCollider", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "Table Chair2 Middle", "HubTableMiddleCollider", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "Table Chair2 Bottom", "HubTableBottomCollider", isHubScene ? 1 : 0);

            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuBoard", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "Account Board", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuHeaderShadow", 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuHeaderLabel", 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuEntryBackdrop1", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuEntryBackdrop2", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuEntryBackdrop3", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuEntryItem1", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuEntryItem2", isHubScene ? 1 : 0);
            ValidateExactCount(issues, sceneName, objects, "HubTodayMenuEntryItem3", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTodayMenuBoard", "Account Board", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTodayMenuBoard", "HubTodayMenuEntryBackdrop1", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTodayMenuBoard", "HubTodayMenuEntryBackdrop2", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTodayMenuBoard", "HubTodayMenuEntryBackdrop3", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTodayMenuEntryBackdrop1", "HubTodayMenuEntryItem1", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTodayMenuEntryBackdrop2", "HubTodayMenuEntryItem2", isHubScene ? 1 : 0);
            ValidateChildCount(issues, sceneName, objects, "HubTodayMenuEntryBackdrop3", "HubTodayMenuEntryItem3", isHubScene ? 1 : 0);

            ValidateExactCount(issues, sceneName, objects, "HubSingleScreenBackground", 0);
            ValidateExactCount(issues, sceneName, objects, "HubBarCollider", 0);
            ValidateExactCount(issues, sceneName, objects, "HubBar", 0);
            ValidateExactCount(issues, sceneName, objects, "HubBarLeftVisual", 0);
            ValidateExactCount(issues, sceneName, objects, "HubBarRightVisual", 0);
            ValidateExactCount(issues, sceneName, objects, "HubBarLeftCollider", 0);
            ValidateExactCount(issues, sceneName, objects, "HubBarRightCollider", 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableTop", 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableMiddle", 0);
            ValidateExactCount(issues, sceneName, objects, "HubTableBottom", 0);
            ValidateExactCount(issues, sceneName, objects, "HubExploreSign", 0);
            ValidateExactCount(issues, sceneName, objects, "HubWarehouseSign", 0);
            ValidateExactCount(issues, sceneName, objects, HubRoomLayout.StorageVisualRootObjectName, 0);
            ValidateExactCount(issues, sceneName, objects, HubRoomLayout.UpgradeWorkbenchVisualRootObjectName, 0);
            ValidateExactCount(issues, sceneName, objects, HubRoomLayout.PortalZoneVisualRootObjectName, 0);
            ValidateExactCount(issues, sceneName, objects, "WorkshopRoomZone", 0);
            ValidateExactCount(issues, sceneName, objects, "StorageRoomZone", 0);
            ValidateExactCount(issues, sceneName, objects, "WorkshopRoomCameraBounds", 0);
            ValidateExactCount(issues, sceneName, objects, "StorageRoomCameraBounds", 0);
            ValidateExactCount(issues, sceneName, objects, "WorkshopFrontOccluder", 0);
            ValidateExactCount(issues, sceneName, objects, "StorageFrontOccluder", 0);

            if (hasPersistedManagedCanvasHierarchy)
            {
                ValidateLayout(issues, sceneName, objects, "TopLeftPanel", PrototypeUILayout.TopLeftPanel);
                ValidateLayout(issues, sceneName, objects, "GoldText", PrototypeUILayout.GoldText);
                ValidateLayout(issues, sceneName, objects, "InteractionPromptBackdrop", PrototypeUILayout.PromptBackdrop(isHubScene));
                ValidateLayout(issues, sceneName, objects, "InteractionPromptText", PrototypeUILayout.PromptText(isHubScene));
                ValidateLayout(issues, sceneName, objects, "GuideBackdrop", PrototypeUILayout.GuideBackdrop(isHubScene));
                ValidateLayout(issues, sceneName, objects, "GuideText", PrototypeUILayout.GuideText(isHubScene));
                ValidateLayout(issues, sceneName, objects, "GuideHelpButton", PrototypeUILayout.GuideHelpButton(isHubScene));
                ValidateLayout(issues, sceneName, objects, "ResultBackdrop", PrototypeUILayout.ResultBackdrop(isHubScene));
                ValidateLayout(issues, sceneName, objects, "RestaurantResultText", PrototypeUILayout.ResultText(isHubScene));

                if (isHubScene)
                {
                    ValidateChildCount(issues, sceneName, objects, hudActionGroupName, "ActionDock", 1);
                    ValidateChildCount(issues, sceneName, objects, hudActionGroupName, "ActionAccent", 1);
                    ValidateChildCount(issues, sceneName, objects, hudActionGroupName, "ActionCaption", 1);
                    ValidateChildCount(issues, sceneName, objects, hudPanelButtonGroupName, "RecipePanelButton", 1);
                    ValidateChildCount(issues, sceneName, objects, hudPanelButtonGroupName, "UpgradePanelButton", 1);
                    ValidateChildCount(issues, sceneName, objects, hudPanelButtonGroupName, "MaterialPanelButton", 1);
                    ValidateLayout(issues, sceneName, objects, hudPanelButtonGroupName, PrototypeUILayout.HubPanelButtonGroup);
                    ValidateLayout(issues, sceneName, objects, "PopupOverlay", PrototypeUILayout.HubPopupOverlay);
                    ValidateLayout(issues, sceneName, objects, "PopupFrame", PrototypeUILayout.HubPopupFrame);
                    ValidateLayout(issues, sceneName, objects, "PopupFrameLeft", PrototypeUILayout.HubPopupFrameLeft);
                    ValidateLayout(issues, sceneName, objects, "PopupFrameRight", PrototypeUILayout.HubPopupFrameRight);
                    ValidateLayout(issues, sceneName, objects, "PopupLeftBody", PrototypeUILayout.HubPopupFrameBody);
                    ValidateLayout(issues, sceneName, objects, "PopupRightBody", PrototypeUILayout.HubPopupFrameBody);
                    ValidateLayout(issues, sceneName, objects, PrototypeUIObjectNames.PopupTitle, PrototypeUILayout.HubPopupTitle);
                    ValidateLayout(issues, sceneName, objects, PrototypeUIObjectNames.PopupLeftCaption, PrototypeUILayout.HubPopupLeftCaption);
                    ValidateLayout(issues, sceneName, objects, PrototypeUIObjectNames.PopupRightCaption, PrototypeUILayout.HubPopupFrameCaption);
                    ValidateLayout(issues, sceneName, objects, "PopupCloseButton", PrototypeUILayout.HubPopupCloseButton);
                    ValidateLayout(issues, sceneName, objects, "InventoryText", PrototypeUILayout.HubPopupFrameText);
                    ValidateLayout(issues, sceneName, objects, "StorageText", PrototypeUILayout.HubPopupRightDetailText);
                    ValidateLayout(issues, sceneName, objects, "SelectedRecipeText", PrototypeUILayout.HubPopupRightDetailText);
                    ValidateLayout(issues, sceneName, objects, "UpgradeText", PrototypeUILayout.HubPopupRightDetailText);
                    ValidateLayout(issues, sceneName, objects, "ActionDock", PrototypeUILayout.HubActionDock);
                    ValidateLayout(issues, sceneName, objects, "ActionAccent", PrototypeUILayout.HubActionAccent);
                    ValidateLayout(issues, sceneName, objects, "ActionCaption", PrototypeUILayout.HubActionCaption);
                    ValidateLayout(issues, sceneName, objects, "RecipePanelButton", PrototypeUILayout.HubRecipePanelButton);
                    ValidateLayout(issues, sceneName, objects, "UpgradePanelButton", PrototypeUILayout.HubUpgradePanelButton);
                    ValidateLayout(issues, sceneName, objects, "MaterialPanelButton", PrototypeUILayout.HubMaterialPanelButton);
                }
                else
                {
                    ValidateExactCount(issues, sceneName, objects, "InventoryText", 0);
                }
            }

            return issues;
        }

        private static bool HasPersistedManagedCanvasHierarchy(Scene scene, bool isHubScene)
        {
            GameObject canvasRoot = scene
                .GetRootGameObjects()
                .FirstOrDefault(root => root != null
                                        && string.Equals(root.name, "Canvas", StringComparison.Ordinal)
                                        && root.GetComponent<Canvas>() != null);
            if (canvasRoot == null)
            {
                return false;
            }

            HashSet<string> managedObjectNames = PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(isHubScene);
            return HasManagedCanvasObject(canvasRoot.transform, managedObjectNames, includeCurrent: false);
        }

        private static bool HasManagedCanvasObject(Transform current, ISet<string> managedObjectNames, bool includeCurrent)
        {
            if (current == null || managedObjectNames == null)
            {
                return false;
            }

            if (includeCurrent
                && !string.IsNullOrWhiteSpace(current.name)
                && managedObjectNames.Contains(current.name))
            {
                return true;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                if (HasManagedCanvasObject(current.GetChild(index), managedObjectNames, includeCurrent: true))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<GameObject> GetAllSceneObjects(Scene scene)
        {
            List<GameObject> results = new();
            Queue<Transform> queue = new();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                results.Add(root);
                queue.Enqueue(root.transform);
            }

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                for (int index = 0; index < current.childCount; index++)
                {
                    Transform child = current.GetChild(index);
                    if (child == null)
                    {
                        continue;
                    }

                    results.Add(child.gameObject);
                    queue.Enqueue(child);
                }
            }

            return results;
        }

        private static void ValidateExactCount(
            ICollection<string> issues,
            string sceneName,
            IEnumerable<GameObject> objects,
            string objectName,
            int expectedCount)
        {
            int resolvedExpectedCount = PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName)
                ? 0
                : expectedCount;
            int actualCount = objects.Count(gameObject => gameObject != null && gameObject.name == objectName);
            if (actualCount != resolvedExpectedCount)
            {
                issues.Add($"[{sceneName}] '{objectName}' count mismatch. expected={resolvedExpectedCount}, actual={actualCount}");
            }
        }

        private static void ValidateChildCount(
            ICollection<string> issues,
            string sceneName,
            IEnumerable<GameObject> objects,
            string parentName,
            string childName,
            int expectedCount)
        {
            string resolvedParentName = parentName;
            int resolvedExpectedCount = expectedCount;
            if (PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(childName, out string overriddenParentName, out _)
                && !string.IsNullOrWhiteSpace(overriddenParentName))
            {
                resolvedParentName = overriddenParentName;
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(childName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(resolvedParentName))
            {
                resolvedExpectedCount = 0;
            }

            GameObject parent = objects.FirstOrDefault(gameObject => gameObject != null && gameObject.name == resolvedParentName);
            int actualCount = 0;

            if (parent != null)
            {
                Transform directChild = parent.transform.Find(childName);
                actualCount = directChild != null ? 1 : 0;
            }

            if (actualCount != resolvedExpectedCount)
            {
                issues.Add($"[{sceneName}] '{resolvedParentName}/{childName}' count mismatch. expected={resolvedExpectedCount}, actual={actualCount}");
            }
        }

        private static void ValidateComponentCount<T>(
            ICollection<string> issues,
            string sceneName,
            IEnumerable<GameObject> objects,
            int expectedCount)
            where T : Component
        {
            int actualCount = objects.Select(gameObject => gameObject != null ? gameObject.GetComponent<T>() : null).Count(component => component != null);
            if (actualCount != expectedCount)
            {
                issues.Add($"[{sceneName}] component '{typeof(T).Name}' count mismatch. expected={expectedCount}, actual={actualCount}");
            }
        }

        private static void ValidateComponentOnNamedObject<T>(
            ICollection<string> issues,
            string sceneName,
            IEnumerable<GameObject> objects,
            string objectName)
            where T : Component
        {
            GameObject target = objects.FirstOrDefault(gameObject => gameObject != null && gameObject.name == objectName);
            if (target == null)
            {
                issues.Add($"[{sceneName}] '{objectName}' not found while validating '{typeof(T).Name}'.");
                return;
            }

            if (target.GetComponent<T>() == null)
            {
                issues.Add($"[{sceneName}] '{objectName}' is missing component '{typeof(T).Name}'.");
            }
        }

        private static void ValidateExpectedSceneHierarchy(
            ICollection<string> issues,
            string sceneName,
            IReadOnlyList<GameObject> objects)
        {
            if (!PrototypeSceneHierarchyCatalog.IsSupportedScene(sceneName))
            {
                return;
            }

            foreach (PrototypeSceneHierarchyEntry entry in PrototypeSceneHierarchyCatalog.EnumerateManagedEntries(sceneName))
            {
                ValidateExactCount(issues, sceneName, objects, entry.ObjectName, 1);

                if (string.IsNullOrWhiteSpace(entry.ParentName))
                {
                    ValidateRootPlacement(issues, sceneName, objects, entry.ObjectName);
                    continue;
                }

                ValidateChildCount(issues, sceneName, objects, entry.ParentName, entry.ObjectName, 1);
            }
        }

        private static void ValidateRootPlacement(
            ICollection<string> issues,
            string sceneName,
            IEnumerable<GameObject> objects,
            string objectName)
        {
            GameObject target = objects.FirstOrDefault(gameObject => gameObject != null && gameObject.name == objectName);
            if (target == null)
            {
                return;
            }

            if (target.transform.parent != null)
            {
                issues.Add($"[{sceneName}] '{objectName}' should be a root object.");
            }
        }

        private static void ValidateLayout(
            ICollection<string> issues,
            string sceneName,
            IEnumerable<GameObject> objects,
            string objectName,
            PrototypeUIRect expectedLayout)
        {
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return;
            }

            GameObject target = objects.FirstOrDefault(gameObject => gameObject != null && gameObject.name == objectName);
            if (target == null)
            {
                issues.Add($"[{sceneName}] '{objectName}' not found while validating layout.");
                return;
            }

            expectedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(objectName, expectedLayout);

            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect == null)
            {
                issues.Add($"[{sceneName}] '{objectName}' is missing RectTransform.");
                return;
            }

            if (!Approximately(rect.anchorMin, expectedLayout.AnchorMin)
                || !Approximately(rect.anchorMax, expectedLayout.AnchorMax)
                || !Approximately(rect.pivot, expectedLayout.Pivot)
                || !Approximately(rect.anchoredPosition, expectedLayout.AnchoredPosition)
                || !Approximately(rect.sizeDelta, expectedLayout.SizeDelta))
            {
                issues.Add(
                    $"[{sceneName}] '{objectName}' layout mismatch. " +
                    $"anchorMin={rect.anchorMin}, anchorMax={rect.anchorMax}, pivot={rect.pivot}, " +
                    $"anchoredPosition={rect.anchoredPosition}, sizeDelta={rect.sizeDelta}");
            }
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Abs(left.x - right.x) < 0.01f && Mathf.Abs(left.y - right.y) < 0.01f;
        }
    }
}
#endif
