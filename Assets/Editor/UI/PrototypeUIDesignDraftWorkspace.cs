using System;
using System.Collections.Generic;
using TMPro;
using UI.Layout;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Editor.UI
{
    /// <summary>
    /// UI 레이아웃 편집기에서 쓰는 에디터 전용 드래프트 Canvas 생성과 저장 흐름입니다.
    /// </summary>
    internal static class PrototypeUIDesignDraftWorkspace
    {
        internal const string DraftCanvasName = "UIDesignDraftCanvas";
        internal const string DraftAssetPath = "Assets/Editor/UI/PrototypeUIDesignDraft.asset";

        private const float CanvasReferenceWidth = 1920f;
        private const float CanvasReferenceHeight = 1080f;
        private const float DraftCanvasScale = 0.01f;

        internal static RectTransform FindDraftRoot(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                GameObject root = roots[index];
                if (root != null
                    && string.Equals(root.name, DraftCanvasName, StringComparison.Ordinal))
                {
                    return root.transform as RectTransform;
                }
            }

            return null;
        }

        internal static bool CreateBlankDraft(Scene scene, bool isHubScene, out RectTransform draftRoot, out string message)
        {
            return CreateDraft(scene, isHubScene, loadRuntimeOverrides: false, out draftRoot, out message);
        }

        internal static bool LoadRuntimeAsDraft(Scene scene, bool isHubScene, out RectTransform draftRoot, out string message)
        {
            return CreateDraft(scene, isHubScene, loadRuntimeOverrides: true, out draftRoot, out message);
        }

        internal static bool SaveDraft(RectTransform draftRoot, bool isHubScene, out string message)
        {
            return PrototypeUISceneLayoutCatalog.TrySaveCanvasLayoutsFromRoot(
                draftRoot,
                isHubScene,
                DraftAssetPath,
                out message);
        }

        internal static bool ApplyDraftToRuntime(RectTransform draftRoot, bool isHubScene, out string message)
        {
            return PrototypeUISceneLayoutCatalog.TryOverlayCanvasLayoutsFromRoot(draftRoot, isHubScene, out message);
        }

        internal static bool DiscardDraft(Scene scene, out string message)
        {
            RectTransform draftRoot = FindDraftRoot(scene);
            if (draftRoot == null)
            {
                message = "제거할 드래프트 Canvas가 없습니다.";
                return false;
            }

            Undo.DestroyObjectImmediate(draftRoot.gameObject);
            message = "드래프트 Canvas를 제거했습니다.";
            return true;
        }

        internal static RectTransform FindDraftChild(RectTransform draftRoot, string objectName)
        {
            if (draftRoot == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            if (string.Equals(draftRoot.name, objectName, StringComparison.Ordinal))
            {
                return draftRoot;
            }

            Transform found = FindChildRecursive(draftRoot, objectName);
            return found as RectTransform;
        }

        internal static string ResolveDefaultParentName(string objectName)
        {
            return objectName switch
            {
                "HUDRoot" or "PopupRoot" => DraftCanvasName,
                "HUDStatusGroup" or "HUDActionGroup" or "HUDBottomGroup" or "HUDOverlayGroup" or "HUDPanelButtonGroup" => "HUDRoot",
                "TopLeftPanel" or "GoldText" or "ResourcePanel" or "ResourceAmountText" => "HUDStatusGroup",
                "ActionDock" or "ActionAccent" or "ActionCaption" => "HUDActionGroup",
                "RecipePanelButton" or "UpgradePanelButton" or "MaterialPanelButton" => "HUDPanelButtonGroup",
                "GuideBackdrop" or "GuideText" or "GuideHelpButton" or "ResultBackdrop" or "RestaurantResultText" => "HUDOverlayGroup",
                "InteractionPromptBackdrop" or "InteractionPromptText" => "HUDRoot",
                "PopupShellGroup" or "PopupFrame" or "PopupFrameHeader" => "PopupRoot",
                "PopupOverlay" => "PopupShellGroup",
                "PopupFrameLeft" or "PopupFrameRight" => "PopupFrame",
                "PopupTitle" or "PopupLeftCaption" or "PopupLeftBody" or "InventoryText" => "PopupFrameLeft",
                "PopupCloseButton" or "PopupRightCaption" or "PopupRightBody" or "StorageText" or "SelectedRecipeText" or "UpgradeText" => "PopupFrameRight",
                _ when objectName.StartsWith("PopupLeftItemBox", StringComparison.Ordinal) => "PopupLeftBody",
                _ when objectName.StartsWith("PopupRightItemBox", StringComparison.Ordinal) => "PopupRightBody",
                _ when objectName.StartsWith("PopupLeftItemIcon", StringComparison.Ordinal) => $"PopupLeftItemBox{objectName[^2..]}",
                _ when objectName.StartsWith("PopupLeftItemText", StringComparison.Ordinal) => $"PopupLeftItemBox{objectName[^2..]}",
                _ when objectName.StartsWith("PopupRightItemIcon", StringComparison.Ordinal) => $"PopupRightItemBox{objectName[^2..]}",
                _ when objectName.StartsWith("PopupRightItemText", StringComparison.Ordinal) => $"PopupRightItemBox{objectName[^2..]}",
                _ => "HUDRoot"
            };
        }

        private static bool CreateDraft(
            Scene scene,
            bool isHubScene,
            bool loadRuntimeOverrides,
            out RectTransform draftRoot,
            out string message)
        {
            draftRoot = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                message = "드래프트를 만들 활성 씬이 없습니다.";
                return false;
            }

            RectTransform existing = FindDraftRoot(scene);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            draftRoot = CreateDraftCanvas(scene);
            BuildManagedShells(draftRoot, isHubScene, loadRuntimeOverrides);
            Selection.activeObject = draftRoot.gameObject;
            EditorGUIUtility.PingObject(draftRoot.gameObject);

            message = loadRuntimeOverrides
                ? "현재 런타임 UI 값을 드래프트 작업대로 불러왔습니다."
                : "빈 UI 드래프트 작업대를 만들었습니다.";
            return true;
        }

        private static RectTransform CreateDraftCanvas(Scene scene)
        {
            GameObject rootObject = new(DraftCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create UI Design Draft");
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            TrySetEditorOnlyTag(rootObject);

            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(CanvasReferenceWidth, CanvasReferenceHeight);
            root.position = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * DraftCanvasScale;

            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = rootObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasReferenceWidth, CanvasReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return root;
        }

        private static void BuildManagedShells(RectTransform draftRoot, bool isHubScene, bool loadRuntimeOverrides)
        {
            Dictionary<string, RectTransform> created = new(StringComparer.Ordinal)
            {
                [DraftCanvasName] = draftRoot
            };

            HashSet<string> managedNames = PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(isHubScene);
            managedNames.Add("HUDRoot");
            managedNames.Add("PopupRoot");

            foreach (string objectName in managedNames)
            {
                if (loadRuntimeOverrides && PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
                {
                    continue;
                }

                EnsureDraftObject(draftRoot, objectName, isHubScene, loadRuntimeOverrides, created);
            }

            EnsureButtonLabel(draftRoot, "RecipePanelButton", "요리");
            EnsureButtonLabel(draftRoot, "UpgradePanelButton", "업그레이드");
            EnsureButtonLabel(draftRoot, "MaterialPanelButton", "재료");
            EnsureButtonLabel(draftRoot, "PopupCloseButton", "X");
            EnsureButtonLabel(draftRoot, "GuideHelpButton", "?");
        }

        private static RectTransform EnsureDraftObject(
            RectTransform draftRoot,
            string objectName,
            bool isHubScene,
            bool loadRuntimeOverrides,
            IDictionary<string, RectTransform> created)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            if (string.Equals(objectName, DraftCanvasName, StringComparison.Ordinal))
            {
                return draftRoot;
            }

            if (created.TryGetValue(objectName, out RectTransform existing))
            {
                return existing;
            }

            string parentName = ResolveParentName(objectName, loadRuntimeOverrides);
            RectTransform parent = string.IsNullOrWhiteSpace(parentName) || string.Equals(parentName, DraftCanvasName, StringComparison.Ordinal)
                ? draftRoot
                : EnsureDraftObject(draftRoot, parentName, isHubScene, loadRuntimeOverrides, created);
            parent ??= draftRoot;

            GameObject targetObject = new(objectName, typeof(RectTransform));
            TrySetEditorOnlyTag(targetObject);
            targetObject.transform.SetParent(parent, false);
            RectTransform rect = targetObject.GetComponent<RectTransform>();
            PrototypeUIRect fallbackLayout = ResolveFallbackLayout(objectName, isHubScene);
            PrototypeUIRect layout = loadRuntimeOverrides
                ? PrototypeUISceneLayoutCatalog.ResolveLayout(objectName, fallbackLayout)
                : fallbackLayout;
            ApplyRectLayout(rect, layout);

            if (ShouldHaveImage(objectName))
            {
                Image image = targetObject.AddComponent<Image>();
                image.color = ResolvePlaceholderColor(objectName);
                image.raycastTarget = ShouldHaveButton(objectName);
                if (loadRuntimeOverrides)
                {
                    PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
                }
            }

            if (ShouldHaveButton(objectName))
            {
                Button button = targetObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                if (loadRuntimeOverrides)
                {
                    PrototypeUISceneLayoutCatalog.TryApplyButtonOverride(button, objectName);
                }
            }

            if (ShouldHaveText(objectName))
            {
                TextMeshProUGUI text = targetObject.AddComponent<TextMeshProUGUI>();
                ConfigurePlaceholderText(text, objectName);
                if (loadRuntimeOverrides)
                {
                    PrototypeUISceneLayoutCatalog.TryApplyTextOverride(text, objectName);
                }
            }

            created[objectName] = rect;
            ApplySiblingIndex(rect, objectName, loadRuntimeOverrides);
            return rect;
        }

        private static string ResolveParentName(string objectName, bool loadRuntimeOverrides)
        {
            if (loadRuntimeOverrides
                && PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string savedParentName, out _)
                && !string.IsNullOrWhiteSpace(savedParentName))
            {
                return string.Equals(savedParentName, "Canvas", StringComparison.Ordinal)
                    ? DraftCanvasName
                    : savedParentName;
            }

            return ResolveDefaultParentName(objectName);
        }

        private static PrototypeUIRect ResolveFallbackLayout(string objectName, bool isHubScene)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return CenterRect(0f, 0f, 180f, 48f);
            }

            if (TryParsePopupItemIndex(objectName, "PopupLeftItemBox", out int leftBoxIndex)
                || TryParsePopupItemIndex(objectName, "PopupRightItemBox", out leftBoxIndex))
            {
                return PrototypeUILayout.HubPopupBodyItemBox(leftBoxIndex);
            }

            if (objectName.StartsWith("PopupLeftItemIcon", StringComparison.Ordinal)
                || objectName.StartsWith("PopupRightItemIcon", StringComparison.Ordinal))
            {
                return new PrototypeUIRect(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(44f, 44f));
            }

            if (objectName.StartsWith("PopupLeftItemText", StringComparison.Ordinal)
                || objectName.StartsWith("PopupRightItemText", StringComparison.Ordinal))
            {
                return new PrototypeUIRect(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(-88f, -20f));
            }

            return objectName switch
            {
                "HUDRoot" or "PopupRoot" or "HUDStatusGroup" or "HUDActionGroup" or "HUDBottomGroup" or "HUDOverlayGroup"
                    or "PopupShellGroup" or "PopupFrameHeader" or "PopupOverlay" => FullRect(),
                "TopLeftPanel" => PrototypeUILayout.TopLeftPanel(isHubScene),
                "GoldText" => PrototypeUILayout.GoldText(isHubScene),
                "ResourcePanel" => PrototypeUILayout.HubResourcePanel,
                "ResourceAmountText" => PrototypeUILayout.HubResourceAmountText,
                "InteractionPromptBackdrop" => PrototypeUILayout.PromptBackdrop(isHubScene),
                "InteractionPromptText" => PrototypeUILayout.PromptText(isHubScene),
                "GuideBackdrop" => PrototypeUILayout.GuideBackdrop(isHubScene),
                "GuideText" => PrototypeUILayout.GuideText(isHubScene),
                "GuideHelpButton" => PrototypeUILayout.GuideHelpButton(isHubScene),
                "ResultBackdrop" => PrototypeUILayout.ResultBackdrop(isHubScene),
                "RestaurantResultText" => PrototypeUILayout.ResultText(isHubScene),
                "HUDPanelButtonGroup" => PrototypeUILayout.HubPanelButtonGroup,
                "ActionDock" => PrototypeUILayout.HubActionDock,
                "ActionAccent" => PrototypeUILayout.HubActionAccent,
                "ActionCaption" => PrototypeUILayout.HubActionCaption,
                "RecipePanelButton" => PrototypeUILayout.HubRecipePanelButton,
                "UpgradePanelButton" => PrototypeUILayout.HubUpgradePanelButton,
                "MaterialPanelButton" => PrototypeUILayout.HubMaterialPanelButton,
                "PopupFrame" => PrototypeUILayout.HubPopupFrame,
                "PopupFrameLeft" => PrototypeUILayout.HubPopupFrameLeft,
                "PopupFrameRight" => PrototypeUILayout.HubPopupFrameRight,
                "PopupTitle" => PrototypeUILayout.HubPopupTitle,
                "PopupCloseButton" => PrototypeUILayout.HubPopupCloseButton,
                "PopupLeftCaption" => PrototypeUILayout.HubPopupLeftCaption,
                "PopupRightCaption" => PrototypeUILayout.HubPopupFrameCaption,
                "PopupLeftBody" or "PopupRightBody" => PrototypeUILayout.HubPopupFrameBody,
                "InventoryText" => PrototypeUILayout.HubPopupFrameText,
                "StorageText" or "SelectedRecipeText" or "UpgradeText" => PrototypeUILayout.HubPopupRightDetailText,
                _ => CenterRect(0f, 0f, 180f, 48f)
            };
        }

        private static PrototypeUIRect FullRect()
        {
            return new PrototypeUIRect(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static PrototypeUIRect CenterRect(float x, float y, float width, float height)
        {
            return new PrototypeUIRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(width, height));
        }

        private static void ApplyRectLayout(RectTransform rect, PrototypeUIRect layout)
        {
            rect.anchorMin = layout.AnchorMin;
            rect.anchorMax = layout.AnchorMax;
            rect.pivot = layout.Pivot;
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.SizeDelta;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void ApplySiblingIndex(RectTransform rect, string objectName, bool loadRuntimeOverrides)
        {
            if (rect == null)
            {
                return;
            }

            if (loadRuntimeOverrides
                && PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out _, out int savedSiblingIndex))
            {
                rect.SetSiblingIndex(Mathf.Clamp(savedSiblingIndex, 0, Mathf.Max(0, rect.parent.childCount - 1)));
            }
        }

        private static void EnsureButtonLabel(RectTransform draftRoot, string buttonName, string label)
        {
            RectTransform buttonRect = FindDraftChild(draftRoot, buttonName);
            if (buttonRect == null || buttonRect.Find(buttonName + "_Label") != null)
            {
                return;
            }

            GameObject labelObject = new(buttonName + "_Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            TrySetEditorOnlyTag(labelObject);
            labelObject.transform.SetParent(buttonRect, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            ApplyRectLayout(rect, FullRect());

            TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.color = Color.white;
        }

        private static bool ShouldHaveImage(string objectName)
        {
            return ShouldHaveButton(objectName)
                   || objectName.Contains("Panel", StringComparison.Ordinal)
                   || objectName.Contains("Backdrop", StringComparison.Ordinal)
                   || objectName.Contains("Frame", StringComparison.Ordinal)
                   || objectName.Contains("Body", StringComparison.Ordinal)
                   || objectName.Contains("Box", StringComparison.Ordinal)
                   || objectName.Contains("Overlay", StringComparison.Ordinal)
                   || objectName.Contains("Dock", StringComparison.Ordinal)
                   || objectName.Contains("Accent", StringComparison.Ordinal);
        }

        private static bool ShouldHaveButton(string objectName)
        {
            return objectName.EndsWith("Button", StringComparison.Ordinal)
                   || objectName.StartsWith("PopupLeftItemBox", StringComparison.Ordinal);
        }

        private static bool ShouldHaveText(string objectName)
        {
            return objectName.EndsWith("Text", StringComparison.Ordinal)
                   || objectName.EndsWith("Caption", StringComparison.Ordinal)
                   || string.Equals(objectName, "PopupTitle", StringComparison.Ordinal);
        }

        private static Color ResolvePlaceholderColor(string objectName)
        {
            if (objectName.Contains("Overlay", StringComparison.Ordinal))
            {
                return new Color(0f, 0f, 0f, 0.35f);
            }

            if (objectName.Contains("Accent", StringComparison.Ordinal))
            {
                return new Color(1f, 0.72f, 0.18f, 0.9f);
            }

            if (ShouldHaveButton(objectName))
            {
                return new Color(0.16f, 0.32f, 0.46f, 0.88f);
            }

            if (objectName.Contains("Frame", StringComparison.Ordinal)
                || objectName.Contains("Dock", StringComparison.Ordinal))
            {
                return new Color(0.10f, 0.18f, 0.25f, 0.78f);
            }

            return new Color(0.92f, 0.96f, 1f, 0.74f);
        }

        private static void ConfigurePlaceholderText(TextMeshProUGUI text, string objectName)
        {
            text.text = ResolvePlaceholderText(objectName);
            text.fontSize = objectName.EndsWith("Caption", StringComparison.Ordinal) || string.Equals(objectName, "PopupTitle", StringComparison.Ordinal)
                ? 28f
                : 18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = text.fontSize;
            text.alignment = objectName.Contains("Amount", StringComparison.Ordinal)
                ? TextAlignmentOptions.MidlineRight
                : TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.color = new Color(0.08f, 0.12f, 0.16f, 1f);
            text.margin = new Vector4(8f, 6f, 8f, 6f);
        }

        private static string ResolvePlaceholderText(string objectName)
        {
            return objectName switch
            {
                "ResourceAmountText" => "120",
                "GoldText" => "골드 120 / 평판 4",
                "InteractionPromptText" => "WASD 이동 / E 상호작용",
                "GuideText" => "안내 문구",
                "RestaurantResultText" => "결과 문구",
                "PopupTitle" => "팝업 제목",
                "PopupLeftCaption" => "목록",
                "PopupRightCaption" => "상세",
                "InventoryText" => "조개 x4\n허브 x2",
                "StorageText" => "보관 상세",
                "SelectedRecipeText" => "메뉴 상세",
                "UpgradeText" => "업그레이드 상세",
                _ when objectName.StartsWith("PopupLeftItemText", StringComparison.Ordinal) => "항목 이름\n짧은 설명",
                _ when objectName.StartsWith("PopupRightItemText", StringComparison.Ordinal) => "상세 항목",
                _ => objectName
            };
        }

        private static bool TryParsePopupItemIndex(string objectName, string prefix, out int index)
        {
            index = 0;
            if (!objectName.StartsWith(prefix, StringComparison.Ordinal)
                || objectName.Length < prefix.Length + 2)
            {
                return false;
            }

            string suffix = objectName[^2..];
            if (!int.TryParse(suffix, out int displayIndex))
            {
                return false;
            }

            index = Mathf.Max(0, displayIndex - 1);
            return true;
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child == null)
                {
                    continue;
                }

                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void TrySetEditorOnlyTag(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                target.tag = "EditorOnly";
            }
            catch (UnityException)
            {
                // EditorOnly는 Unity 기본 태그지만, 태그 설정이 손상된 프로젝트에서도 드래프트 생성을 막지 않습니다.
            }
        }
    }
}
