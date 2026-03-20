#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * 생성된 프로토타입 씬의 기본 구조를 점검하는 간단한 감사 도구입니다.
 * 허브 전용 UI가 탐험 씬에 남았는지, 플레이어 비주얼이 중복되는지, 구 라벨이 남았는지를 확인합니다.
 */
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
        "TopLeftAccent",
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
        "StorageAccent",
        "StorageCard",
        "StorageCaption",
        "StorageText",
        "RecipeAccent",
        "RecipeCard",
        "RecipeCaption",
        "SelectedRecipeText",
        "UpgradeAccent",
        "UpgradeCard",
        "UpgradeCaption",
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

        foreach (string legacyLabel in LegacyLabelNames)
        {
            ValidateExactCount(issues, sceneName, objects, legacyLabel, 0);
        }

        ValidateLayout(issues, sceneName, objects, "TopLeftPanel", PrototypeUiLayout.TopLeftPanel);
        ValidateLayout(issues, sceneName, objects, "TopLeftAccent", PrototypeUiLayout.TopLeftAccent);
        ValidateLayout(issues, sceneName, objects, "PhaseBadge", PrototypeUiLayout.PhaseBadge);
        ValidateLayout(issues, sceneName, objects, "GoldText", PrototypeUiLayout.GoldText);
        ValidateLayout(issues, sceneName, objects, "PromptBackdrop", PrototypeUiLayout.PromptBackdrop(isHubScene));
        ValidateLayout(issues, sceneName, objects, "InteractionPromptText", PrototypeUiLayout.PromptText(isHubScene));
        ValidateLayout(issues, sceneName, objects, "GuideBackdrop", PrototypeUiLayout.GuideBackdrop(isHubScene));
        ValidateLayout(issues, sceneName, objects, "GuideText", PrototypeUiLayout.GuideText(isHubScene));
        ValidateLayout(issues, sceneName, objects, "ResultBackdrop", PrototypeUiLayout.ResultBackdrop(isHubScene));
        ValidateLayout(issues, sceneName, objects, "RestaurantResultText", PrototypeUiLayout.ResultText(isHubScene));
        ValidateLayout(issues, sceneName, objects, "InventoryCard", PrototypeUiLayout.InventoryCard(isHubScene));
        ValidateLayout(issues, sceneName, objects, "InventoryAccent", PrototypeUiLayout.InventoryAccent(isHubScene));
        ValidateLayout(issues, sceneName, objects, "InventoryCaption", PrototypeUiLayout.InventoryCaption(isHubScene));
        ValidateLayout(issues, sceneName, objects, "InventoryText", PrototypeUiLayout.InventoryText(isHubScene));
        ValidateLayout(issues, sceneName, objects, "DayPhaseText", PrototypeUiLayout.DayPhaseText);

        if (isHubScene)
        {
            ValidateLayout(issues, sceneName, objects, "CenterBottomPanel", PrototypeUiLayout.HubCenterBottomPanel);
            ValidateLayout(issues, sceneName, objects, "PopupOverlay", PrototypeUiLayout.HubPopupOverlay);
            ValidateLayout(issues, sceneName, objects, "StorageCard", PrototypeUiLayout.HubStorageCard);
            ValidateLayout(issues, sceneName, objects, "StorageAccent", PrototypeUiLayout.HubStorageAccent);
            ValidateLayout(issues, sceneName, objects, "StorageCaption", PrototypeUiLayout.HubStorageCaption);
            ValidateLayout(issues, sceneName, objects, "StorageText", PrototypeUiLayout.HubStorageText);
            ValidateLayout(issues, sceneName, objects, "RecipeCard", PrototypeUiLayout.HubRecipeCard);
            ValidateLayout(issues, sceneName, objects, "RecipeAccent", PrototypeUiLayout.HubRecipeAccent);
            ValidateLayout(issues, sceneName, objects, "RecipeCaption", PrototypeUiLayout.HubRecipeCaption);
            ValidateLayout(issues, sceneName, objects, "SelectedRecipeText", PrototypeUiLayout.HubRecipeText);
            ValidateLayout(issues, sceneName, objects, "UpgradeCard", PrototypeUiLayout.HubUpgradeCard);
            ValidateLayout(issues, sceneName, objects, "UpgradeAccent", PrototypeUiLayout.HubUpgradeAccent);
            ValidateLayout(issues, sceneName, objects, "UpgradeCaption", PrototypeUiLayout.HubUpgradeCaption);
            ValidateLayout(issues, sceneName, objects, "UpgradeText", PrototypeUiLayout.HubUpgradeText);
            ValidateLayout(issues, sceneName, objects, "ActionDock", PrototypeUiLayout.HubActionDock);
            ValidateLayout(issues, sceneName, objects, "ActionAccent", PrototypeUiLayout.HubActionAccent);
            ValidateLayout(issues, sceneName, objects, "ActionCaption", PrototypeUiLayout.HubActionCaption);
            ValidateLayout(issues, sceneName, objects, "SkipExplorationButton", PrototypeUiLayout.HubSkipExplorationButton);
            ValidateLayout(issues, sceneName, objects, "SkipServiceButton", PrototypeUiLayout.HubSkipServiceButton);
            ValidateLayout(issues, sceneName, objects, "NextDayButton", PrototypeUiLayout.HubNextDayButton);
            ValidateLayout(issues, sceneName, objects, "RecipePanelButton", PrototypeUiLayout.HubRecipePanelButton);
            ValidateLayout(issues, sceneName, objects, "UpgradePanelButton", PrototypeUiLayout.HubUpgradePanelButton);
            ValidateLayout(issues, sceneName, objects, "MaterialPanelButton", PrototypeUiLayout.HubMaterialPanelButton);
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
        PrototypeUiRect expectedLayout)
    {
        GameObject target = objects.FirstOrDefault(gameObject => gameObject != null && gameObject.name == objectName);
        if (target == null)
        {
            issues.Add($"[{sceneName}] '{objectName}' not found while validating layout.");
            return;
        }

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
#endif
