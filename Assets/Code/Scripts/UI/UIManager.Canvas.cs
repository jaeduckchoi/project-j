using System;
using System.Collections.Generic;
using Code.Scripts.UI.Layout;
using UnityEngine;

namespace Code.Scripts.UI
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
            ApplyInitialCanvasActiveContractsOnce();
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

            if (existing == null && ShouldSkipCreatingMissingManagedObject(groupName))
            {
                return null;
            }

            GameObject rootObject = existing != null ? existing.gameObject : new GameObject(groupName, typeof(RectTransform));
            ApplyHubPopupObjectIdentity(rootObject);
            if (rootObject.transform.parent != parent)
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
                "ActionDock" or "ActionAccent" or "ActionCaption" or "OpenRestaurantButton" or "CloseRestaurantButton" => HudActionGroupName,
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
            if (existing == null && ShouldSkipCreatingMissingManagedObject(objectName))
            {
                return null;
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
            if (existing == null && ShouldSkipCreatingMissingManagedObject(objectName))
            {
                return null;
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

            if (existing == null && ShouldSkipCreatingMissingManagedObject(objectName))
            {
                return null;
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

            if (TryApplyBoundCanvasHierarchy(target, objectName))
            {
                return;
            }

            if (ShouldPreserveAuthoredCanvasParent(target, objectName))
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

            if (TryApplyBoundCanvasHierarchy(target, objectName))
            {
                return;
            }

            if (ShouldPreserveAuthoredCanvasParent(target, objectName))
            {
                return;
            }

            Transform groupParent = GetCanvasGroupParent(objectName);
            if (groupParent != null && target.parent != groupParent)
            {
                target.SetParent(groupParent, false);
            }
        }

        private bool ShouldPreserveAuthoredCanvasParent(Transform target, string objectName)
        {
            if (target == null)
            {
                return false;
            }

            if (TryGetResolvedHierarchyOverride(objectName, out Transform contractParent, out _, out _))
            {
                return target.parent == contractParent;
            }

            return target.parent != null
                   && ((string.Equals(objectName, "InteractionPromptText", StringComparison.Ordinal)
                        && string.Equals(target.parent.name, "InteractionPromptBackdrop", StringComparison.Ordinal))
                       || (string.Equals(objectName, "GuideText", StringComparison.Ordinal)
                           && string.Equals(target.parent.name, "GuideBackdrop", StringComparison.Ordinal))
                       || (string.Equals(objectName, "RestaurantResultText", StringComparison.Ordinal)
                           && string.Equals(target.parent.name, "ResultBackdrop", StringComparison.Ordinal))
                       || (string.Equals(objectName, "PopupCloseButton", StringComparison.Ordinal)
                           && string.Equals(target.parent.name, "PopupFrame", StringComparison.Ordinal)));
        }

        private void ApplySavedCanvasHierarchyOverrides()
        {
            if (transform == null)
            {
                return;
            }

            List<Transform> orderedTransforms = new();
            foreach (string objectName in PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(IsHubScene()))
            {
                if (!PrototypeUISceneLayoutCatalog.HasHierarchyOverride(objectName))
                {
                    continue;
                }

                Transform target = FindNamedUiTransform(objectName);
                if (target != null)
                {
                    orderedTransforms.Add(target);
                }
            }

            orderedTransforms.Sort((left, right) => CompareTransformDepth(left, right));
            for (int index = 0; index < orderedTransforms.Count; index++)
            {
                ApplySavedCanvasHierarchyOverride(orderedTransforms[index]);
            }
        }

        private void ApplyInitialCanvasActiveContractsOnce()
        {
            if (!Application.isPlaying || didApplyInitialCanvasActiveContracts || transform == null)
            {
                return;
            }

            foreach (string objectName in PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(IsHubScene()))
            {
                if (!PrototypeUISceneLayoutCatalog.TryGetHierarchyInitialActiveSelf(objectName, out bool initialActiveSelf))
                {
                    continue;
                }

                Transform target = FindNamedUiTransform(objectName);
                if (target != null)
                {
                    target.gameObject.SetActive(initialActiveSelf);
                }
            }

            didApplyInitialCanvasActiveContracts = true;
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
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string parentScenePath, out _)
                || string.IsNullOrWhiteSpace(parentScenePath))
            {
                return false;
            }

            targetParent = ResolveUiScenePath(parentScenePath);
            return targetParent != null;
        }

        private void ApplySavedCanvasHierarchyOverride(Transform target)
        {
            if (transform == null
                || target == null
                || target == transform
                || string.IsNullOrWhiteSpace(target.name)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(target.name)
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(target.name, out string parentScenePath, out int siblingIndex, out _)
                || string.IsNullOrWhiteSpace(parentScenePath))
            {
                return;
            }

            Transform targetParent = ResolveUiScenePath(parentScenePath);
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

            if (TryResolveBoundUiTransform(objectName, out Transform bound))
            {
                return bound;
            }

            Transform direct = transform.Find(objectName);
            if (direct != null)
            {
                return direct;
            }

            return FindNamedUiTransformRecursive(transform, objectName);
        }

        private bool TryApplyBoundCanvasHierarchy(Transform target, string objectName)
        {
            if (target == null || !TryGetResolvedHierarchyOverride(objectName, out Transform targetParent, out int siblingIndex, out _))
            {
                return false;
            }

            if (targetParent == target)
            {
                return true;
            }

            if (target.parent != targetParent)
            {
                target.SetParent(targetParent, false);
            }

            target.SetSiblingIndex(ClampSiblingIndex(target.parent, siblingIndex));
            return true;
        }

        private bool TryGetResolvedHierarchyOverride(
            string objectName,
            out Transform targetParent,
            out int siblingIndex,
            out bool initialActiveSelf)
        {
            targetParent = null;
            siblingIndex = 0;
            initialActiveSelf = false;

            if (transform == null
                || string.IsNullOrWhiteSpace(objectName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName)
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string parentScenePath, out siblingIndex, out initialActiveSelf)
                || string.IsNullOrWhiteSpace(parentScenePath))
            {
                return false;
            }

            targetParent = ResolveUiScenePath(parentScenePath);
            return targetParent != null;
        }

        private bool TryResolveBoundUiTransform(string objectName, out Transform target)
        {
            target = null;
            if (transform == null
                || string.IsNullOrWhiteSpace(objectName)
                || !PrototypeUISceneLayoutCatalog.TryGetSceneObjectPath(objectName, out string sceneObjectPath)
                || string.IsNullOrWhiteSpace(sceneObjectPath))
            {
                return false;
            }

            target = ResolveUiScenePath(sceneObjectPath);
            return target != null;
        }

        private Transform ResolveUiScenePath(string scenePath)
        {
            if (transform == null || string.IsNullOrWhiteSpace(scenePath))
            {
                return null;
            }

            string normalizedPath = scenePath.Trim().Replace('\\', '/');
            while (normalizedPath.Contains("//"))
            {
                normalizedPath = normalizedPath.Replace("//", "/");
            }

            normalizedPath = normalizedPath.Trim('/');
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return null;
            }

            string[] parts = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !string.Equals(parts[0], transform.name, StringComparison.Ordinal))
            {
                return null;
            }

            Transform current = transform;
            for (int index = 1; index < parts.Length && current != null; index++)
            {
                current = current.Find(parts[index]);
            }

            return current;
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
