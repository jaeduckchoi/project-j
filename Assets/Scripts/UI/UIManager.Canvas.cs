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
            MigrateHubResourcePanelObjects();
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

        private Transform EnsureCanvasGroupRoot(Transform parent, string groupName, int siblingIndex)
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

            ApplyManagedRectLayout(rect, resolvedLayout, preserveExistingLayout: existing != null);
            SetManagedSiblingIndex(rect, siblingIndex, preserveExistingLayout: existing != null);
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

            foreach (string objectName in EnumerateHudCanvasObjectNames(IsHubScene()))
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
            Transform popupFrameLeft = EnsurePopupLayoutContainer(popupFrame, PopupFrameLeftGroupName, 2);
            Transform popupFrameRight = EnsurePopupLayoutContainer(popupFrame, PopupFrameRightGroupName, 3);

            if (!Application.isPlaying)
            {
                EnsureEditorPopupTypeGroupRoots(popupFrame, popupFrameLeft, popupFrameRight);
            }
        }

        private void ReparentPopupCanvasObjects(Transform popupRoot)
        {
            if (popupRoot == null)
            {
                return;
            }

            foreach (string objectName in PrototypeUISceneLayoutCatalog.EnumeratePopupCanvasObjectNames())
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

            if (TryGetRefrigeratorPopupParent(objectName, popupRoot, out Transform refrigeratorParent))
            {
                return refrigeratorParent;
            }

            if (!Application.isPlaying
                && !suppressCanvasGroupingInEditorPreview
                && TryGetEditorPopupTypeParent(objectName, popupRoot, out Transform editorPopupTypeParent))
            {
                return editorPopupTypeParent;
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
                ExploreStatusPanelObjectName
                    or ExploreEconomyTextObjectName
                    or HubResourcePanelObjectName
                    or HubResourceAmountTextObjectName => HudStatusGroupName,
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
                PrototypeUIObjectNames.PopupTitle => PopupFrameGroupName,
                PrototypeUIObjectNames.PopupLeftCaption or "PopupLeftBody" or "InventoryText" => PopupFrameLeftGroupName,
                "PopupCloseButton" or PrototypeUIObjectNames.PopupRightCaption or "PopupRightBody"
                    or "StorageText" or "SelectedRecipeText" or "UpgradeText" => PopupFrameRightGroupName,
                PrototypeUIObjectNames.RefrigeratorStorage
                    or PrototypeUIObjectNames.RefrigeratorInfoPanel
                    or PrototypeUIObjectNames.RefrigeratorInfoIcon
                    or PrototypeUIObjectNames.RefrigeratorItemNameText
                    or PrototypeUIObjectNames.RefrigeratorItemDescriptionText
                    or PrototypeUIObjectNames.RefrigeratorSelectedSlot
                    or PrototypeUIObjectNames.RefrigeratorRemoveZone
                    or PrototypeUIObjectNames.RefrigeratorRemoveIcon
                    or PrototypeUIObjectNames.RefrigeratorRemoveText
                    or PrototypeUIObjectNames.RefrigeratorDragGhost => PopupFrameGroupName,
                _ => null
            };
        }

        private bool TryGetRefrigeratorPopupParent(string objectName, Transform popupRoot, out Transform parent)
        {
            parent = null;
            if (popupRoot == null || string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            Transform popupFrame = EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1);
            if (popupFrame == null)
            {
                return false;
            }

            if (!Application.isPlaying && !suppressCanvasGroupingInEditorPreview)
            {
                popupFrame = EnsurePopupEditorGroupRoot(popupFrame, PopupRefrigeratorEditorGroupName, 4);
            }

            if (objectName == PrototypeUIObjectNames.RefrigeratorStorage
                || objectName == PrototypeUIObjectNames.RefrigeratorInfoPanel
                || objectName == PrototypeUIObjectNames.RefrigeratorRemoveZone
                || objectName == PrototypeUIObjectNames.RefrigeratorDragGhost)
            {
                parent = popupFrame;
                return true;
            }

            if (objectName == PrototypeUIObjectNames.RefrigeratorInfoIcon
                || objectName == PrototypeUIObjectNames.RefrigeratorItemNameText
                || objectName == PrototypeUIObjectNames.RefrigeratorItemDescriptionText)
            {
                parent = EnsureRefrigeratorPopupContainer(
                    popupFrame,
                    PrototypeUIObjectNames.RefrigeratorInfoPanel,
                    PrototypeUILayout.HubRefrigeratorInfoPanel,
                    5);
                return parent != null;
            }

            if (objectName == PrototypeUIObjectNames.RefrigeratorSelectedSlot
                || TryParseRefrigeratorSlotIndex(objectName, PrototypeUIObjectNames.RefrigeratorSlotPrefix, out _))
            {
                parent = EnsureRefrigeratorPopupContainer(
                    popupFrame,
                    PrototypeUIObjectNames.RefrigeratorStorage,
                    PrototypeUILayout.HubRefrigeratorStorage,
                    4);
                return parent != null;
            }

            if (TryParseRefrigeratorSlotIndex(objectName, PrototypeUIObjectNames.RefrigeratorSlotIconPrefix, out int iconIndex)
                || TryParseRefrigeratorSlotIndex(objectName, PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix, out iconIndex))
            {
                Transform storage = EnsureRefrigeratorPopupContainer(
                    popupFrame,
                    PrototypeUIObjectNames.RefrigeratorStorage,
                    PrototypeUILayout.HubRefrigeratorStorage,
                    4);
                parent = EnsureRefrigeratorPopupContainer(
                    storage,
                    $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{iconIndex + 1:00}",
                    PrototypeUILayout.HubRefrigeratorSlot(iconIndex),
                    iconIndex);
                return parent != null;
            }

            if (objectName == PrototypeUIObjectNames.RefrigeratorRemoveIcon
                || objectName == PrototypeUIObjectNames.RefrigeratorRemoveText)
            {
                parent = EnsureRefrigeratorPopupContainer(
                    popupFrame,
                    PrototypeUIObjectNames.RefrigeratorRemoveZone,
                    PrototypeUILayout.HubRefrigeratorRemoveZone,
                    5);
                return parent != null;
            }

            return false;
        }

        private void EnsureEditorPopupTypeGroupRoots(Transform popupFrame, Transform popupFrameLeft, Transform popupFrameRight)
        {
            if (popupFrame == null)
            {
                return;
            }

            EnsurePopupEditorGroupRoot(popupFrameLeft, PopupSharedLeftEditorGroupName, 0);
            EnsurePopupEditorGroupRoot(popupFrameRight, PopupSharedRightEditorGroupName, 0);
            EnsurePopupEditorGroupRoot(popupFrameRight, PopupStorageRightEditorGroupName, 1);
            EnsurePopupEditorGroupRoot(popupFrameRight, PopupUpgradeRightEditorGroupName, 2);
            EnsurePopupEditorGroupRoot(popupFrame, PopupRefrigeratorEditorGroupName, 4);
        }

        private Transform EnsurePopupEditorGroupRoot(Transform parent, string objectName, int siblingIndex)
        {
            if (parent == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            Transform existing = parent.Find(objectName) ?? FindNamedUiTransform(objectName);
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

            ApplyManagedRectLayout(
                rect,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                preserveExistingLayout: existing != null);
            SetManagedSiblingIndex(rect, siblingIndex, preserveExistingLayout: existing != null);
            return rect;
        }

        private bool TryGetEditorPopupTypeParent(string objectName, Transform popupRoot, out Transform parent)
        {
            parent = null;
            if (popupRoot == null || string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            Transform popupFrame = EnsurePopupLayoutContainer(popupRoot, PopupFrameGroupName, 1);
            Transform popupFrameLeft = EnsurePopupLayoutContainer(popupFrame, PopupFrameLeftGroupName, 2);
            Transform popupFrameRight = EnsurePopupLayoutContainer(popupFrame, PopupFrameRightGroupName, 3);
            EnsureEditorPopupTypeGroupRoots(popupFrame, popupFrameLeft, popupFrameRight);

            if (IsEditorSharedLeftPopupObject(objectName))
            {
                parent = EnsurePopupEditorGroupRoot(popupFrameLeft, PopupSharedLeftEditorGroupName, 0);
                return parent != null;
            }

            if (IsEditorSharedRightPopupObject(objectName))
            {
                parent = EnsurePopupEditorGroupRoot(popupFrameRight, PopupSharedRightEditorGroupName, 0);
                return parent != null;
            }

            if (string.Equals(objectName, "StorageText", StringComparison.Ordinal))
            {
                parent = EnsurePopupEditorGroupRoot(popupFrameRight, PopupStorageRightEditorGroupName, 1);
                return parent != null;
            }

            if (string.Equals(objectName, "UpgradeText", StringComparison.Ordinal))
            {
                parent = EnsurePopupEditorGroupRoot(popupFrameRight, PopupUpgradeRightEditorGroupName, 2);
                return parent != null;
            }

            return false;
        }

        private static bool IsEditorSharedLeftPopupObject(string objectName)
        {
            return objectName == PrototypeUIObjectNames.PopupLeftCaption
                   || objectName == "PopupLeftBody"
                   || objectName == "InventoryText"
                   || objectName.StartsWith("PopupLeftItemBox", StringComparison.Ordinal)
                   || objectName.StartsWith("PopupLeftItemIcon", StringComparison.Ordinal)
                   || objectName.StartsWith("PopupLeftItemText", StringComparison.Ordinal);
        }

        private static bool IsEditorSharedRightPopupObject(string objectName)
        {
            return objectName == PrototypeUIObjectNames.PopupRightCaption
                   || objectName == "PopupRightBody"
                   || objectName == "SelectedRecipeText"
                   || objectName.StartsWith("PopupRightItemBox", StringComparison.Ordinal)
                   || objectName.StartsWith("PopupRightItemIcon", StringComparison.Ordinal)
                   || objectName.StartsWith("PopupRightItemText", StringComparison.Ordinal);
        }


        private Transform EnsureRefrigeratorPopupContainer(Transform parent, string objectName, PrototypeUIRect layout, int siblingIndex)
        {
            if (parent == null
                || string.IsNullOrWhiteSpace(objectName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            Transform existing = parent.Find(objectName) ?? FindNamedUiTransform(objectName);
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

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(objectName, layout);
            ApplyManagedRectLayout(rect, resolvedLayout, preserveExistingLayout: existing != null);
            SetManagedSiblingIndex(rect, siblingIndex, preserveExistingLayout: existing != null);
            return rect;
        }

        private static bool TryParseRefrigeratorSlotIndex(string objectName, string prefix, out int index)
        {
            index = 0;
            if (string.IsNullOrWhiteSpace(objectName)
                || string.IsNullOrWhiteSpace(prefix)
                || !objectName.StartsWith(prefix, StringComparison.Ordinal)
                || objectName.Length < prefix.Length + 2)
            {
                return false;
            }

            if (prefix == PrototypeUIObjectNames.RefrigeratorSlotPrefix
                && (objectName.StartsWith(PrototypeUIObjectNames.RefrigeratorSlotIconPrefix, StringComparison.Ordinal)
                    || objectName.StartsWith(PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix, StringComparison.Ordinal)))
            {
                return false;
            }

            string suffix = objectName[^2..];
            if (!int.TryParse(suffix, out int displayIndex)
                || displayIndex < 1
                || displayIndex > PrototypeUILayout.RefrigeratorSlotCount)
            {
                return false;
            }

            index = displayIndex - 1;
            return true;
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

            ApplyManagedRectLayout(
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                preserveExistingLayout: existing != null);
            SetManagedSiblingIndex(rect, siblingIndex, preserveExistingLayout: existing != null);
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

            bool usePopupRoot = objectName == "InventoryText"
                ? IsHubScene()
                : PrototypeUISceneLayoutCatalog.IsPopupCanvasObjectName(objectName);

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
            if (transform == null || ShouldPreserveExistingEditorLayout(preserveExistingLayout: true))
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

            ApplySavedHierarchyLayout(target as RectTransform, target.name);
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

        private void MigrateHubResourcePanelObjects()
        {
            if (!IsHubScene() || transform == null)
            {
                return;
            }

            MigrateNamedUiTransform(LegacyHubResourcePanelObjectName, HubResourcePanelObjectName);
            MigrateNamedUiTransform(LegacyHubResourceAmountTextObjectName, HubResourceAmountTextObjectName);

            Transform panelTransform = FindNamedUiTransform(HubResourcePanelObjectName);
            if (panelTransform == null)
            {
                return;
            }

            MigrateChildUiTransform(panelTransform, LegacyHubResourcePanelInnerObjectName, HubResourcePanelInnerObjectName);
            MigrateChildUiTransform(panelTransform, LegacyHubResourcePanelCoinIconObjectName, HubResourcePanelCoinIconObjectName);
        }

        private void MigrateNamedUiTransform(string legacyName, string targetName)
        {
            Transform legacyTransform = FindNamedUiTransform(legacyName);
            if (legacyTransform == null)
            {
                return;
            }

            Transform targetTransform = FindNamedUiTransform(targetName);
            if (targetTransform == null)
            {
                legacyTransform.name = targetName;
                return;
            }

            if (legacyTransform != targetTransform)
            {
                DestroyCanvasObject(legacyTransform.gameObject);
            }
        }

        private void MigrateChildUiTransform(Transform parent, string legacyName, string targetName)
        {
            if (parent == null)
            {
                return;
            }

            Transform legacyTransform = parent.Find(legacyName);
            if (legacyTransform == null)
            {
                return;
            }

            Transform targetTransform = parent.Find(targetName);
            if (targetTransform == null)
            {
                legacyTransform.name = targetName;
                return;
            }

            if (legacyTransform != targetTransform)
            {
                DestroyCanvasObject(legacyTransform.gameObject);
            }
        }
    }
}
