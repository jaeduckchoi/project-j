#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Player;
using UI;
using UI.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ProjectEditor 네임스페이스
namespace ProjectEditor
{
    /// <summary>
    /// 생성된 프로토타입 씬의 기본 구조를 점검하는 간단한 감사 도구입니다.
    /// 허브 전용 UI가 탐험 씬에 남았는지, 플레이어 비주얼이 중복되는지, 구 라벨이 남았는지를 확인합니다.
    /// </summary>
    public static class PrototypeSceneAudit
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Hub.unity",
            "Assets/Scenes/Beach.unity",
            "Assets/Scenes/DeepForest.unity",
            "Assets/Scenes/AbandonedMine.unity",
            "Assets/Scenes/WindHill.unity",
        };

        private static readonly string[] CommonHudNames =
        {
            "TopLeftPanel",
            "PhaseBadge",
            "GoldText",
            "PromptBackdrop",
            "InteractionPromptText",
            "GuideBackdrop",
            "GuideText",
            "ResultBackdrop",
            "RestaurantResultText",
            "InventoryCard",
            "InventoryAccent",
            "InventoryCaption",
            "InventoryText",
        };

        private static readonly string[] HubOnlyHudNames =
        {
            "CenterBottomPanel",
            "PopupOverlay",
            "PopupFrame",
            "PopupFrameLeft",
            "PopupFrameRight",
            "PopupLeftBody",
            "PopupRightBody",
            "PopupTitle",
            "PopupLeftCaption",
            "PopupRightCaption",
            "PopupCloseButton",
            "StorageText",
            "SelectedRecipeText",
            "UpgradeText",
            "ActionAccent",
            "ActionDock",
            "ActionCaption",
            "SkipExplorationButton",
            "SkipServiceButton",
            "NextDayButton",
            "RecipePanelButton",
            "UpgradePanelButton",
            "MaterialPanelButton",
        };

        private static readonly string[] RemovedHubCardNames =
        {
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

        [MenuItem("Tools/Jonggu Restaurant/Audit Generated Scenes")]
        public static void AuditGeneratedScenes()
        {
            List<string> issues = new();

            foreach (string scenePath in ScenePaths)
            {
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
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            bool isHubScene = string.Equals(sceneName, "Hub", StringComparison.OrdinalIgnoreCase);

            List<GameObject> objects = GetAllSceneObjects(scene);

            ValidateExactCount(issues, sceneName, objects, "Canvas", 1);
            ValidateComponentCount<UIManager>(issues, sceneName, objects, 1);
            ValidateExactCount(issues, sceneName, objects, "Jonggu", 1);
            ValidateChildCount(issues, sceneName, objects, "Jonggu", "PlayerVisual", 1);
            ValidateComponentOnNamedObject<PlayerDirectionalSprite>(issues, sceneName, objects, "Jonggu");

            foreach (string hudName in CommonHudNames)
            {
                ValidateExactCount(issues, sceneName, objects, hudName, 1);
            }

            foreach (string hudName in HubOnlyHudNames)
            {
                ValidateExactCount(issues, sceneName, objects, hudName, isHubScene ? 1 : 0);
            }

            foreach (string removedName in RemovedHubCardNames)
            {
                ValidateExactCount(issues, sceneName, objects, removedName, 0);
            }

            foreach (string legacyLabel in LegacyLabelNames)
            {
                ValidateExactCount(issues, sceneName, objects, legacyLabel, 0);
            }

            ValidateLayout(issues, sceneName, objects, "TopLeftPanel", PrototypeUILayout.TopLeftPanel);
            ValidateLayout(issues, sceneName, objects, "PhaseBadge", PrototypeUILayout.PhaseBadge);
            ValidateLayout(issues, sceneName, objects, "GoldText", PrototypeUILayout.GoldText);
            ValidateLayout(issues, sceneName, objects, "PromptBackdrop", PrototypeUILayout.PromptBackdrop(isHubScene));
            ValidateLayout(issues, sceneName, objects, "InteractionPromptText", PrototypeUILayout.PromptText(isHubScene));
            ValidateLayout(issues, sceneName, objects, "GuideBackdrop", PrototypeUILayout.GuideBackdrop(isHubScene));
            ValidateLayout(issues, sceneName, objects, "GuideText", PrototypeUILayout.GuideText(isHubScene));
            ValidateLayout(issues, sceneName, objects, "ResultBackdrop", PrototypeUILayout.ResultBackdrop(isHubScene));
            ValidateLayout(issues, sceneName, objects, "RestaurantResultText", PrototypeUILayout.ResultText(isHubScene));
            ValidateLayout(issues, sceneName, objects, "InventoryCard", PrototypeUILayout.InventoryCard(isHubScene));
            ValidateLayout(issues, sceneName, objects, "InventoryAccent", PrototypeUILayout.InventoryAccent(isHubScene));
            ValidateLayout(issues, sceneName, objects, "InventoryCaption", PrototypeUILayout.InventoryCaption(isHubScene));
            ValidateLayout(issues, sceneName, objects, "InventoryText", isHubScene ? PrototypeUILayout.HubPopupFrameText : PrototypeUILayout.InventoryText(false));
            ValidateLayout(issues, sceneName, objects, "DayPhaseText", PrototypeUILayout.DayPhaseText);

            if (isHubScene)
            {
                ValidateLayout(issues, sceneName, objects, "CenterBottomPanel", PrototypeUILayout.HubCenterBottomPanel);
                ValidateLayout(issues, sceneName, objects, "PopupOverlay", PrototypeUILayout.HubPopupOverlay);
                ValidateLayout(issues, sceneName, objects, "PopupFrame", PrototypeUILayout.HubPopupFrame);
                ValidateLayout(issues, sceneName, objects, "PopupFrameLeft", PrototypeUILayout.HubPopupFrameLeft);
                ValidateLayout(issues, sceneName, objects, "PopupFrameRight", PrototypeUILayout.HubPopupFrameRight);
                ValidateLayout(issues, sceneName, objects, "PopupLeftBody", PrototypeUILayout.HubPopupFrameBody);
                ValidateLayout(issues, sceneName, objects, "PopupRightBody", PrototypeUILayout.HubPopupFrameBody);
                ValidateLayout(issues, sceneName, objects, "PopupTitle", PrototypeUILayout.HubPopupTitle);
                ValidateLayout(issues, sceneName, objects, "PopupLeftCaption", PrototypeUILayout.HubPopupLeftCaption);
                ValidateLayout(issues, sceneName, objects, "PopupRightCaption", PrototypeUILayout.HubPopupFrameCaption);
                ValidateLayout(issues, sceneName, objects, "PopupCloseButton", PrototypeUILayout.HubPopupCloseButton);
                ValidateLayout(issues, sceneName, objects, "StorageText", PrototypeUILayout.HubPopupRightDetailText);
                ValidateLayout(issues, sceneName, objects, "SelectedRecipeText", PrototypeUILayout.HubPopupRightDetailText);
                ValidateLayout(issues, sceneName, objects, "UpgradeText", PrototypeUILayout.HubPopupRightDetailText);
                ValidateLayout(issues, sceneName, objects, "ActionDock", PrototypeUILayout.HubActionDock);
                ValidateLayout(issues, sceneName, objects, "ActionAccent", PrototypeUILayout.HubActionAccent);
                ValidateLayout(issues, sceneName, objects, "ActionCaption", PrototypeUILayout.HubActionCaption);
                ValidateLayout(issues, sceneName, objects, "SkipExplorationButton", PrototypeUILayout.HubSkipExplorationButton);
                ValidateLayout(issues, sceneName, objects, "SkipServiceButton", PrototypeUILayout.HubSkipServiceButton);
                ValidateLayout(issues, sceneName, objects, "NextDayButton", PrototypeUILayout.HubNextDayButton);
                ValidateLayout(issues, sceneName, objects, "RecipePanelButton", PrototypeUILayout.HubRecipePanelButton);
                ValidateLayout(issues, sceneName, objects, "UpgradePanelButton", PrototypeUILayout.HubUpgradePanelButton);
                ValidateLayout(issues, sceneName, objects, "MaterialPanelButton", PrototypeUILayout.HubMaterialPanelButton);
            }

            return issues;
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
            int actualCount = objects.Count(gameObject => gameObject != null && gameObject.name == objectName);
            if (actualCount != expectedCount)
            {
                issues.Add($"[{sceneName}] '{objectName}' count mismatch. expected={expectedCount}, actual={actualCount}");
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
            GameObject parent = objects.FirstOrDefault(gameObject => gameObject != null && gameObject.name == parentName);
            int actualCount = 0;

            if (parent != null)
            {
                Transform directChild = parent.transform.Find(childName);
                actualCount = directChild != null ? 1 : 0;
            }

            if (actualCount != expectedCount)
            {
                issues.Add($"[{sceneName}] '{parentName}/{childName}' count mismatch. expected={expectedCount}, actual={actualCount}");
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

        private static void ValidateLayout(
            ICollection<string> issues,
            string sceneName,
            IEnumerable<GameObject> objects,
            string objectName,
            PrototypeUIRect expectedLayout)
        {
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
