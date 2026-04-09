using System;
using System.Collections.Generic;
using UI.Layout;
using UnityEngine;

namespace UI
{
    public partial class UIManager
    {
        private void EnsureCanvasGroups()
        {
            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                return;
            }

            if (transform == null)
            {
                return;
            }

            RemoveDeletedCanvasObjects();

            Transform hudRoot = EnsureCanvasGroupRoot(HudRootName, 0);
            Transform popupRoot = EnsureCanvasGroupRoot(PopupRootName, 1);

            EnsureHudSubgroupRoots(hudRoot);
            EnsurePopupSubgroupRoots(popupRoot);
            ReparentHudCanvasObjects(hudRoot);
            ReparentPopupCanvasObjects(popupRoot);
            ReparentCanvasObject("InventoryText", IsHubScene() ? GetPopupCanvasGroupParent("InventoryText", popupRoot) : GetHudCanvasGroupParent("InventoryText", hudRoot));
            ApplySavedCanvasHierarchyOverrides();
            RemoveDeletedCanvasObjects();
        }

        private Transform EnsureCanvasGroupRoot(string groupName, int siblingIndex)
        {
            return EnsureCanvasGroupRoot(transform, groupName, siblingIndex);
        }

        private static Transform EnsureCanvasGroupRoot(Transform parent, string groupName, int siblingIndex)
        {
            if (parent == null
                || string.IsNullOrWhiteSpace(groupName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(groupName))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                groupName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));

            Transform existing = parent.Find(groupName);
            if (existing == null)
            {
                existing = FindNamedUiTransformRecursive(parent, groupName);
            }

            GameObject rootObject = existing != null ? existing.gameObject : new GameObject(groupName, typeof(RectTransform));
            ApplyHubPopupObjectIdentity(rootObject);
            if (existing == null)
            {
                rootObject.transform.SetParent(parent, false);
            }

            RectTransform rect = rootObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = rootObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            Transform siblingParent = rect.parent != null ? rect.parent : parent;
            rect.SetSiblingIndex(ClampSiblingIndex(siblingParent, siblingIndex));
            return rect;
        }

        private void EnsureHudSubgroupRoots(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            EnsureCanvasGroupRoot(hudRoot, HudStatusGroupName, 0);
            EnsureCanvasGroupRoot(hudRoot, HudActionGroupName, 1);
            EnsureCanvasGroupRoot(hudRoot, HudBottomGroupName, 2);

            if (IsHubScene() || hudRoot.Find(HudPanelButtonGroupObjectName) != null)
            {
                EnsureCanvasGroupRoot(hudRoot, HudPanelButtonGroupObjectName, 3);
            }

            EnsureCanvasGroupRoot(hudRoot, HudOverlayGroupName, 4);
        }

        private void ReparentHudCanvasObjects(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            foreach (string objectName in EnumerateHudCanvasObjectNames())
            {
                ReparentCanvasObject(objectName, GetHudCanvasGroupParent(objectName, hudRoot));
            }
        }

        private void EnsurePopupSubgroupRoots(Transform popupRoot)
        {
            if (popupRoot == null)
            {
                return;
            }

            Transform popupFrame = EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1);
            EnsureCanvasGroupRoot(popupRoot, PopupShellGroupName, 0);
            EnsureCanvasGroupRoot(popupRoot, PopupFrameHeaderGroupName, 2);
            EnsurePopupLayoutContainer(popupFrame, PopupFrameLeftGroupName, 2);
            EnsurePopupLayoutContainer(popupFrame, PopupFrameRightGroupName, 3);
        }

        private void ReparentPopupCanvasObjects(Transform popupRoot)
        {
            if (popupRoot == null)
            {
                return;
            }

            foreach (string objectName in PopupCanvasObjectNames)
            {
                ReparentCanvasObject(objectName, GetPopupCanvasGroupParent(objectName, popupRoot));
            }
        }

        private Transform GetPopupCanvasGroupParent(string objectName, Transform popupRoot)
        {
            if (popupRoot == null)
            {
                return null;
            }

            if (TryGetSavedCanvasGroupParent(objectName, out Transform savedParent))
            {
                return savedParent;
            }

            string subgroupName = GetPopupSubgroupName(objectName);
            if (string.IsNullOrWhiteSpace(subgroupName))
            {
                return popupRoot;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                Transform existingGroup = FindNamedUiTransform(subgroupName);
                return existingGroup != null ? existingGroup : popupRoot;
            }

            return subgroupName switch
            {
                PopupShellGroupName => EnsureCanvasGroupRoot(popupRoot, PopupShellGroupName, 0),
                PopupFrameGroupName => EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1),
                PopupFrameLeftGroupName or PopupFrameRightGroupName => EnsurePopupLayoutContainer(
                    EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1),
                    subgroupName,
                    GetPopupSubgroupSiblingIndex(subgroupName)),
                _ => popupRoot
            };
        }

        private Transform GetHudCanvasGroupParent(string objectName, Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return null;
            }

            if (TryGetSavedCanvasGroupParent(objectName, out Transform savedParent))
            {
                return savedParent;
            }

            string subgroupName = GetHudSubgroupName(objectName);
            if (string.IsNullOrWhiteSpace(subgroupName))
            {
                return hudRoot;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                Transform existingGroup = hudRoot.Find(subgroupName);
                return existingGroup != null ? existingGroup : hudRoot;
            }

            Transform currentGroup = hudRoot.Find(subgroupName);
            return currentGroup != null ? currentGroup : EnsureCanvasGroupRoot(hudRoot, subgroupName, GetHudSubgroupSiblingIndex(subgroupName));
        }

        private static string GetHudSubgroupName(string objectName)
        {
            return objectName switch
            {
                "TopLeftPanel" or "GoldText" => HudStatusGroupName,
                "ActionDock" or "ActionAccent" or "ActionCaption" => HudActionGroupName,
                "RecipePanelButton" or "UpgradePanelButton" or "MaterialPanelButton" => HudPanelButtonGroupObjectName,
                "GuideBackdrop" or "GuideText" or "ResultBackdrop" or "RestaurantResultText" or "GuideHelpButton" => HudOverlayGroupName,
                _ => null
            };
        }

        private static string GetPopupSubgroupName(string objectName)
        {
            return objectName switch
            {
                "PopupOverlay" => PopupShellGroupName,
                "PopupFrameLeft" or "PopupFrameRight" => PopupFrameGroupName,
                PrototypeUIObjectNames.PopupTitle or PrototypeUIObjectNames.PopupLeftCaption or "PopupLeftBody" or "InventoryText" => PopupFrameLeftGroupName,
                "PopupCloseButton" or PrototypeUIObjectNames.PopupRightCaption or "PopupRightBody"
                    or "StorageText" or "SelectedRecipeText" or "UpgradeText" => PopupFrameRightGroupName,
                _ => null
            };
        }

        private static int GetHudSubgroupSiblingIndex(string subgroupName)
        {
            if (subgroupName == HudStatusGroupName)
            {
                return 0;
            }

            if (subgroupName == HudActionGroupName)
            {
                return 1;
            }

            if (subgroupName == HudBottomGroupName)
            {
                return 2;
            }

            if (subgroupName == HudPanelButtonGroupObjectName)
            {
                return 3;
            }

            if (subgroupName == HudOverlayGroupName)
            {
                return 4;
            }

            return 0;
        }

        private static int GetPopupSubgroupSiblingIndex(string subgroupName)
        {
            return subgroupName switch
            {
                PopupShellGroupName => 0,
                PopupFrameGroupName => 1,
                PopupFrameHeaderGroupName => 2,
                PopupFrameLeftGroupName => 2,
                PopupFrameRightGroupName => 3,
                _ => 0
            };
        }

        private Transform EnsurePopupLayoutContainer(Transform parent, string objectName, int siblingIndex)
        {
            if (parent == null
                || string.IsNullOrWhiteSpace(objectName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            Transform existing = parent.Find(objectName);
            if (existing == null)
            {
                existing = FindNamedUiTransform(objectName);
            }

            GameObject containerObject = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform));
            ApplyHubPopupObjectIdentity(containerObject);
            if (containerObject.transform.parent != parent)
            {
                containerObject.transform.SetParent(parent, false);
            }

            RectTransform rect = containerObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = containerObject.AddComponent<RectTransform>();
            }

            if (existing == null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            rect.SetSiblingIndex(ClampSiblingIndex(rect.parent != null ? rect.parent : parent, siblingIndex));
            return rect;
        }

        private void ReparentCanvasObject(string objectName, Transform targetParent)
        {
            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                DestroyNamedUiTransform(objectName);
                return;
            }

            if (targetParent == null)
            {
                return;
            }

            Transform target = FindNamedUiTransform(objectName);
            if (target == null || target == transform || target == targetParent)
            {
                return;
            }

            if (target.parent != targetParent)
            {
                target.SetParent(targetParent, false);
            }
        }

        private Transform GetCanvasGroupParent(string objectName)
        {
            if (transform == null || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (TryGetSavedCanvasGroupParent(objectName, out Transform savedParent))
            {
                return savedParent;
            }

            bool usePopupRoot = objectName == "InventoryText" ? IsHubScene() : PopupCanvasObjectNames.Contains(objectName);

            if (usePopupRoot)
            {
                if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
                {
                    Transform existingGroup = transform.Find(PopupRootName);
                    return GetPopupCanvasGroupParent(objectName, existingGroup != null ? existingGroup : transform);
                }

                return GetPopupCanvasGroupParent(objectName, EnsureCanvasGroupRoot(PopupRootName, 1));
            }

            Transform hudRoot = !Application.isPlaying && suppressCanvasGroupingInEditorPreview
                ? transform.Find(HudRootName)
                : EnsureCanvasGroupRoot(HudRootName, 0);
            if (hudRoot == null)
            {
                return transform;
            }

            return GetHudCanvasGroupParent(objectName, hudRoot);
        }

        private void AssignCanvasGroupParent(Transform target, string objectName)
        {
            if (target == null)
            {
                return;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                return;
            }

            Transform groupParent = GetCanvasGroupParent(objectName);
            if (groupParent != null && target.parent != groupParent)
            {
                target.SetParent(groupParent, false);
            }
        }

        private void ApplySavedCanvasHierarchyOverrides()
        {
            if (transform == null)
            {
                return;
            }

            Dictionary<string, Transform> transformMap = new(StringComparer.Ordinal);
            CollectNamedUiTransforms(transform, transformMap);

            List<Transform> existingTransforms = new(transformMap.Values);
            for (int index = 0; index < existingTransforms.Count; index++)
            {
                Transform current = existingTransforms[index];
                if (current == null || string.IsNullOrWhiteSpace(current.name))
                {
                    continue;
                }

                EnsureSavedHierarchyTransform(current.name, transformMap, new HashSet<string>(StringComparer.Ordinal));
            }

            transformMap.Clear();
            CollectNamedUiTransforms(transform, transformMap);

            List<Transform> orderedTransforms = new(transformMap.Values);
            orderedTransforms.Sort((left, right) => CompareTransformDepth(left, right));
            for (int index = 0; index < orderedTransforms.Count; index++)
            {
                ApplySavedCanvasHierarchyOverride(orderedTransforms[index], transformMap);
            }
        }

        private void RemoveDeletedCanvasObjects()
        {
            if (transform == null)
            {
                return;
            }

            List<GameObject> targets = new();
            CollectDeletedCanvasObjects(transform, targets, includeCurrent: false);
            for (int index = 0; index < targets.Count; index++)
            {
                DestroyCanvasObject(targets[index]);
            }
        }

        private void DestroyNamedUiTransform(string objectName)
        {
            Transform target = FindNamedUiTransform(objectName);
            if (target != null)
            {
                DestroyCanvasObject(target.gameObject);
            }
        }

        private void DestroyCanvasObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void CollectDeletedCanvasObjects(Transform current, ICollection<GameObject> targets, bool includeCurrent)
        {
            if (current == null || targets == null)
            {
                return;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CollectDeletedCanvasObjects(current.GetChild(index), targets, includeCurrent: true);
            }

            if (includeCurrent && PrototypeUISceneLayoutCatalog.IsObjectRemoved(current.name))
            {
                targets.Add(current.gameObject);
            }
        }

        private bool TryGetSavedCanvasGroupParent(string objectName, out Transform targetParent)
        {
            targetParent = null;

            if (transform == null
                || string.IsNullOrWhiteSpace(objectName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName)
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string parentName, out _)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return false;
            }

            if (string.Equals(parentName, transform.name, StringComparison.Ordinal))
            {
                targetParent = transform;
                return true;
            }

            if (!Application.isPlaying && suppressCanvasGroupingInEditorPreview)
            {
                targetParent = FindNamedUiTransform(parentName);
                return targetParent != null;
            }

            Dictionary<string, Transform> transformMap = new(StringComparer.Ordinal);
            CollectNamedUiTransforms(transform, transformMap);
            targetParent = EnsureSavedHierarchyTransform(parentName, transformMap, new HashSet<string>(StringComparer.Ordinal));
            return targetParent != null;
        }

        private Transform EnsureSavedHierarchyTransform(
            string objectName,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (transform == null)
            {
                return null;
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(objectName) || string.Equals(objectName, transform.name, StringComparison.Ordinal))
            {
                return transform;
            }

            if (transformMap.TryGetValue(objectName, out Transform existing))
            {
                return existing;
            }

            if (visiting == null || !visiting.Add(objectName))
            {
                return transform;
            }

            Transform parent = ResolveSavedHierarchyParent(objectName, transformMap, visiting);
            if (parent == null)
            {
                visiting.Remove(objectName);
                return null;
            }

            GameObject groupObject = new(objectName, typeof(RectTransform));
            ApplyHubPopupObjectIdentity(groupObject);
            groupObject.transform.SetParent(parent != null ? parent : transform, false);

            RectTransform rect = groupObject.GetComponent<RectTransform>();
            ApplySavedHierarchyLayout(rect, objectName);
            if (PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out _, out int siblingIndex))
            {
                rect.SetSiblingIndex(ClampSiblingIndex(rect.parent, siblingIndex));
            }

            transformMap[objectName] = rect;
            visiting.Remove(objectName);
            return rect;
        }

        private Transform ResolveSavedHierarchyParent(
            string objectName,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (transform == null)
            {
                return null;
            }

            if (!PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string parentName, out _)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return transform;
            }

            if (string.Equals(parentName, transform.name, StringComparison.Ordinal))
            {
                return transform;
            }

            return EnsureSavedHierarchyTransform(parentName, transformMap, visiting);
        }

        private void ApplySavedCanvasHierarchyOverride(Transform target, IDictionary<string, Transform> transformMap)
        {
            if (transform == null
                || target == null
                || target == transform
                || string.IsNullOrWhiteSpace(target.name)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(target.name)
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(target.name, out string parentName, out int siblingIndex)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return;
            }

            Transform targetParent = string.Equals(parentName, transform.name, StringComparison.Ordinal)
                ? transform
                : EnsureSavedHierarchyTransform(parentName, transformMap, new HashSet<string>(StringComparer.Ordinal));
            if (targetParent == null || targetParent == target)
            {
                return;
            }

            if (target.parent != targetParent)
            {
                target.SetParent(targetParent, false);
            }

            target.SetSiblingIndex(ClampSiblingIndex(target.parent, siblingIndex));
            transformMap[target.name] = target;
        }

        private static void CollectNamedUiTransforms(Transform current, IDictionary<string, Transform> transformMap)
        {
            if (current == null || transformMap == null || string.IsNullOrWhiteSpace(current.name))
            {
                return;
            }

            transformMap[current.name] = current;
            for (int index = 0; index < current.childCount; index++)
            {
                CollectNamedUiTransforms(current.GetChild(index), transformMap);
            }
        }

        private static void ApplySavedHierarchyLayout(RectTransform rect, string objectName)
        {
            if (rect == null)
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
        }

        private static int CompareTransformDepth(Transform left, Transform right)
        {
            return GetTransformDepth(left).CompareTo(GetTransformDepth(right));
        }

        private static int GetTransformDepth(Transform target)
        {
            int depth = 0;
            Transform current = target;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static int ClampSiblingIndex(Transform parent, int siblingIndex)
        {
            if (parent == null)
            {
                return 0;
            }

            return Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, parent.childCount - 1));
        }

        private Transform FindNamedUiTransform(string objectName)
        {
            if (transform == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            Transform direct = transform.Find(objectName);
            if (direct != null)
            {
                return direct;
            }

            return FindNamedUiTransformRecursive(transform, objectName);
        }

        private static Transform FindNamedUiTransformRecursive(Transform parent, string objectName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == objectName)
                {
                    return child;
                }

                Transform nested = FindNamedUiTransformRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
