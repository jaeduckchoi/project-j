#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UI.Controllers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UI.Layout
{
    /// <summary>
    /// 에디터 전용 캡처/추출 도우미를 보관하는 파셜입니다.
    /// 씬 Canvas/HUD 트리에서 RectTransform, Image, TMP, Button 값을 읽어 entry 컬렉션으로 변환합니다.
    /// </summary>
    public static partial class PrototypeUISceneLayoutCatalog
    {
        private static void CaptureCanvasOverridesFromScene(
            Scene scene,
            IEnumerable<Canvas> canvases,
            IDictionary<string, PrototypeUIRect> layoutMap,
            IDictionary<string, PrototypeUISceneImageEntry> imageMap,
            IDictionary<string, PrototypeUISceneTextEntry> textMap,
            IDictionary<string, PrototypeUISceneButtonEntry> buttonMap,
            IDictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap,
            IDictionary<string, string> nameMap,
            ISet<string> duplicateNames,
            out bool usedPreviewCapture)
        {
            usedPreviewCapture = false;
            if (canvases == null)
            {
                return;
            }

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                {
                    continue;
                }

                RectTransform rootRect = canvas.transform as RectTransform;
                if (rootRect == null)
                {
                    continue;
                }

                if (HasPersistedManagedCanvasHierarchy(scene, rootRect))
                {
                    CaptureCanvasOverridesFromRoot(rootRect, layoutMap, imageMap, textMap, buttonMap, hierarchyMap, nameMap, duplicateNames);
                    continue;
                }

                if (TryCapturePreviewCanvasOverrides(scene, canvas, layoutMap, imageMap, textMap, buttonMap, hierarchyMap, nameMap, duplicateNames))
                {
                    usedPreviewCapture = true;
                    continue;
                }

                CaptureCanvasOverridesFromRoot(rootRect, layoutMap, imageMap, textMap, buttonMap, hierarchyMap, nameMap, duplicateNames);
            }
        }

        private static void CaptureCanvasOverridesFromRoot(
            RectTransform canvasRoot,
            IDictionary<string, PrototypeUIRect> layoutMap,
            IDictionary<string, PrototypeUISceneImageEntry> imageMap,
            IDictionary<string, PrototypeUISceneTextEntry> textMap,
            IDictionary<string, PrototypeUISceneButtonEntry> buttonMap,
            IDictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap,
            IDictionary<string, string> nameMap,
            ISet<string> duplicateNames)
        {
            if (canvasRoot == null)
            {
                return;
            }

            CaptureCanvasOverridesRecursive(canvasRoot, canvasRoot, layoutMap, imageMap, textMap, buttonMap, hierarchyMap, duplicateNames);
            CaptureSceneNameOverrides(canvasRoot, nameMap);
        }

        private static bool HasPersistedManagedCanvasHierarchy(Scene scene, RectTransform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return false;
            }

            foreach (string objectName in GetManagedCanvasObjectNames(IsHubScene(scene)))
            {
                if (FindChildRecursive(canvasRoot, objectName) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryCapturePreviewCanvasOverrides(
            Scene sourceScene,
            Canvas sourceCanvas,
            IDictionary<string, PrototypeUIRect> layoutMap,
            IDictionary<string, PrototypeUISceneImageEntry> imageMap,
            IDictionary<string, PrototypeUISceneTextEntry> textMap,
            IDictionary<string, PrototypeUISceneButtonEntry> buttonMap,
            IDictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap,
            IDictionary<string, string> nameMap,
            ISet<string> duplicateNames)
        {
            if (sourceCanvas == null)
            {
                return false;
            }

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            Scene previousActiveScene = SceneManager.GetActiveScene();
            GameObject previewCanvasObject = null;
            int originalLayoutCount = layoutMap != null ? layoutMap.Count : 0;
            int originalImageCount = imageMap != null ? imageMap.Count : 0;
            int originalTextCount = textMap != null ? textMap.Count : 0;
            int originalButtonCount = buttonMap != null ? buttonMap.Count : 0;
            int originalHierarchyCount = hierarchyMap != null ? hierarchyMap.Count : 0;
            int originalNameCount = nameMap != null ? nameMap.Count : 0;

            try
            {
                previewCanvasObject = Object.Instantiate(sourceCanvas.gameObject);
                previewCanvasObject.name = sourceCanvas.gameObject.name;
                SceneManager.MoveGameObjectToScene(previewCanvasObject, previewScene);

                RectTransform previewRoot = previewCanvasObject.transform as RectTransform;
                if (previewRoot == null || !previewCanvasObject.TryGetComponent(out UIManager uiManager))
                {
                    return false;
                }

                DestroyImmediateChildren(previewRoot);

                if (sourceScene.IsValid() && sourceScene.isLoaded)
                {
                    SceneManager.SetActiveScene(sourceScene);
                }

                using (SuppressRemovedObjectChecks())
                {
                    uiManager.OrganizeCanvasHierarchyInEditor();
                    uiManager.ApplyEditorDesignPreview(false, PrototypeUIPreviewPanel.None);
                }

                CaptureCanvasOverridesFromRoot(previewRoot, layoutMap, imageMap, textMap, buttonMap, hierarchyMap, nameMap, duplicateNames);
                return (layoutMap != null && layoutMap.Count > originalLayoutCount)
                    || (imageMap != null && imageMap.Count > originalImageCount)
                    || (textMap != null && textMap.Count > originalTextCount)
                    || (buttonMap != null && buttonMap.Count > originalButtonCount)
                    || (hierarchyMap != null && hierarchyMap.Count > originalHierarchyCount)
                    || (nameMap != null && nameMap.Count > originalNameCount);
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                if (previewCanvasObject != null)
                {
                    Object.DestroyImmediate(previewCanvasObject);
                }

                if (previewScene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(previewScene);
                }
            }
        }

        private static void DestroyImmediateChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(root.GetChild(index).gameObject);
            }
        }

        private static List<string> BuildRemovedObjectNameList(bool isHubScene, IEnumerable<string> presentObjectNames, IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            HashSet<string> presentNameSet = new(StringComparer.Ordinal);
            if (presentObjectNames != null)
            {
                foreach (string objectName in presentObjectNames)
                {
                    if (!string.IsNullOrWhiteSpace(objectName))
                    {
                        presentNameSet.Add(objectName);
                    }
                }
            }

            List<string> removedObjectNames = new();
            foreach (string objectName in GetManagedCanvasObjectNames(isHubScene, sceneNameOverrides))
            {
                if (PrototypeUISceneLayoutSettings.IsProtectedManagedObject(objectName))
                {
                    continue;
                }

                if (!presentNameSet.Contains(objectName))
                {
                    removedObjectNames.Add(objectName);
                }
            }

            removedObjectNames.Sort(string.CompareOrdinal);
            return removedObjectNames;
        }

        private static void CaptureCanvasOverridesRecursive(
            RectTransform current,
            RectTransform canvasRoot,
            IDictionary<string, PrototypeUIRect> layoutMap,
            IDictionary<string, PrototypeUISceneImageEntry> imageMap,
            IDictionary<string, PrototypeUISceneTextEntry> textMap,
            IDictionary<string, PrototypeUISceneButtonEntry> buttonMap,
            IDictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap,
            ISet<string> duplicateNames)
        {
            if (current == null)
            {
                return;
            }

            if (current != canvasRoot && !string.IsNullOrEmpty(current.name))
            {
                if (layoutMap.ContainsKey(current.name))
                {
                    duplicateNames.Add(current.name);
                }

                layoutMap[current.name] = ExtractLayout(current);
                hierarchyMap[current.name] = ExtractHierarchyEntry(current.name, current);

                if (current.TryGetComponent(out Image image))
                {
                    imageMap[current.name] = ExtractImageEntry(current.name, image);
                }

                if (current.TryGetComponent(out TextMeshProUGUI text))
                {
                    textMap[current.name] = ExtractTextEntry(current.name, text);
                }

                if (current.TryGetComponent(out Button button))
                {
                    buttonMap[current.name] = ExtractButtonEntry(current.name, button);
                }
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CaptureCanvasOverridesRecursive(
                    current.GetChild(index) as RectTransform,
                    canvasRoot,
                    layoutMap,
                    imageMap,
                    textMap,
                    buttonMap,
                    hierarchyMap,
                    duplicateNames);
            }
        }

        private static List<PrototypeUISceneLayoutEntry> ConvertToLayoutEntries(IDictionary<string, PrototypeUIRect> layoutMap)
        {
            List<PrototypeUISceneLayoutEntry> entries = new(layoutMap.Count);
            foreach (KeyValuePair<string, PrototypeUIRect> pair in layoutMap)
            {
                entries.Add(new PrototypeUISceneLayoutEntry(pair.Key, pair.Value));
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
            return entries;
        }

        private static List<PrototypeUISceneImageEntry> ConvertToImageEntries(IDictionary<string, PrototypeUISceneImageEntry> imageMap)
        {
            List<PrototypeUISceneImageEntry> entries = new(imageMap.Count);
            foreach (KeyValuePair<string, PrototypeUISceneImageEntry> pair in imageMap)
            {
                entries.Add(pair.Value);
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
            return entries;
        }

        private static List<PrototypeUISceneHierarchyEntry> ConvertToHierarchyEntries(IDictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap)
        {
            List<PrototypeUISceneHierarchyEntry> entries = new(hierarchyMap.Count);
            foreach (KeyValuePair<string, PrototypeUISceneHierarchyEntry> pair in hierarchyMap)
            {
                entries.Add(pair.Value);
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
            return entries;
        }

        private static List<PrototypeUISceneTextEntry> ConvertToTextEntries(IDictionary<string, PrototypeUISceneTextEntry> textMap)
        {
            List<PrototypeUISceneTextEntry> entries = new(textMap.Count);
            foreach (KeyValuePair<string, PrototypeUISceneTextEntry> pair in textMap)
            {
                entries.Add(pair.Value);
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
            return entries;
        }

        private static List<PrototypeUISceneButtonEntry> ConvertToButtonEntries(IDictionary<string, PrototypeUISceneButtonEntry> buttonMap)
        {
            List<PrototypeUISceneButtonEntry> entries = new(buttonMap.Count);
            foreach (KeyValuePair<string, PrototypeUISceneButtonEntry> pair in buttonMap)
            {
                entries.Add(pair.Value);
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
            return entries;
        }

        private static List<PrototypeUISceneNameEntry> ConvertToNameEntries(IDictionary<string, string> nameMap)
        {
            List<PrototypeUISceneNameEntry> entries = new(nameMap.Count);
            foreach (KeyValuePair<string, string> pair in nameMap)
            {
                entries.Add(new PrototypeUISceneNameEntry(pair.Key, pair.Value));
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.CanonicalName, right.CanonicalName));
            return entries;
        }

        private static void CaptureSceneNameOverrides(RectTransform canvasRoot, IDictionary<string, string> nameMap)
        {
            if (canvasRoot == null || nameMap == null)
            {
                return;
            }

            Transform hudRoot = FindChildRecursive(canvasRoot, "HUDRoot");
            if (hudRoot == null)
            {
                return;
            }

            Transform actionGroup = FindHudActionGroup(hudRoot);
            if (actionGroup != null)
            {
                nameMap["HUDActionGroup"] = actionGroup.name;
            }

            Transform hudPanelButtonGroup = FindHudPanelButtonGroup(actionGroup);
            if (hudPanelButtonGroup == null)
            {
                hudPanelButtonGroup = FindHudPanelButtonGroup(hudRoot);
            }

            if (hudPanelButtonGroup != null)
            {
                nameMap["HUDPanelButtonGroup"] = hudPanelButtonGroup.name;
            }

        }

        private static void CaptureHudStructureOverrides(
            RectTransform canvasRoot,
            IDictionary<string, string> nameMap,
            IDictionary<string, PrototypeUIRect> layoutMap,
            IDictionary<string, PrototypeUISceneImageEntry> imageMap,
            IDictionary<string, PrototypeUISceneTextEntry> textMap,
            IDictionary<string, PrototypeUISceneButtonEntry> buttonMap,
            IDictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap)
        {
            if (canvasRoot == null)
            {
                return;
            }

            CaptureSceneNameOverrides(canvasRoot, nameMap);

            Transform hudRoot = FindChildRecursive(canvasRoot, "HUDRoot");
            if (hudRoot == null)
            {
                return;
            }

            if (hudRoot is RectTransform hudRootRect)
            {
                CaptureTransformOverridesRecursive(hudRootRect, layoutMap, imageMap, textMap, buttonMap, hierarchyMap);
            }
        }

        private static Transform FindHudActionGroup(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return null;
            }

            Transform direct = hudRoot.Find("HUDActionGroup");
            if (direct != null)
            {
                return direct;
            }

            for (int index = 0; index < hudRoot.childCount; index++)
            {
                Transform child = hudRoot.GetChild(index);
                if (child == null)
                {
                    continue;
                }

                if (FindChildRecursive(child, "ActionDock") != null
                    || FindChildRecursive(child, "ActionAccent") != null
                    || FindChildRecursive(child, "ActionCaption") != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindHudPanelButtonGroup(Transform searchRoot)
        {
            if (searchRoot == null)
            {
                return null;
            }

            Transform direct = searchRoot.Find("HUDPanelButtonGroup");
            if (direct != null)
            {
                return direct;
            }

            if ((searchRoot.Find("RecipePanelButton") != null
                    || searchRoot.Find("UpgradePanelButton") != null
                    || searchRoot.Find("MaterialPanelButton") != null)
                && !string.Equals(searchRoot.name, "HUDRoot", StringComparison.Ordinal))
            {
                return searchRoot;
            }

            if (MatchesLayout(searchRoot as RectTransform, PrototypeUILayout.HubPanelButtonGroup))
            {
                return searchRoot;
            }

            for (int index = 0; index < searchRoot.childCount; index++)
            {
                Transform child = searchRoot.GetChild(index);
                if (child == null)
                {
                    continue;
                }

                Transform found = FindHudPanelButtonGroup(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CaptureTransformOverridesRecursive(
            RectTransform current,
            IDictionary<string, PrototypeUIRect> layoutMap,
            IDictionary<string, PrototypeUISceneImageEntry> imageMap,
            IDictionary<string, PrototypeUISceneTextEntry> textMap,
            IDictionary<string, PrototypeUISceneButtonEntry> buttonMap,
            IDictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap)
        {
            if (current == null || string.IsNullOrWhiteSpace(current.name))
            {
                return;
            }

            layoutMap[current.name] = ExtractLayout(current);
            hierarchyMap[current.name] = ExtractHierarchyEntry(current.name, current);

            if (current.TryGetComponent(out Image image))
            {
                imageMap[current.name] = ExtractImageEntry(current.name, image);
            }

            if (current.TryGetComponent(out TextMeshProUGUI text))
            {
                textMap[current.name] = ExtractTextEntry(current.name, text);
            }

            if (current.TryGetComponent(out Button button))
            {
                buttonMap[current.name] = ExtractButtonEntry(current.name, button);
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CaptureTransformOverridesRecursive(
                    current.GetChild(index) as RectTransform,
                    layoutMap,
                    imageMap,
                    textMap,
                    buttonMap,
                    hierarchyMap);
            }
        }

        private static bool MatchesLayout(RectTransform rectTransform, PrototypeUIRect expectedLayout)
        {
            if (rectTransform == null)
            {
                return false;
            }

            const float tolerance = 0.1f;
            return Approximately(rectTransform.anchorMin, expectedLayout.AnchorMin, tolerance)
                && Approximately(rectTransform.anchorMax, expectedLayout.AnchorMax, tolerance)
                && Approximately(rectTransform.pivot, expectedLayout.Pivot, tolerance)
                && Approximately(rectTransform.anchoredPosition, expectedLayout.AnchoredPosition, tolerance)
                && Approximately(rectTransform.sizeDelta, expectedLayout.SizeDelta, tolerance);
        }

        private static bool Approximately(Vector2 left, Vector2 right, float tolerance)
        {
            return Mathf.Abs(left.x - right.x) <= tolerance
                && Mathf.Abs(left.y - right.y) <= tolerance;
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            if (string.Equals(root.name, objectName, StringComparison.Ordinal))
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindChildRecursive(root.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static PrototypeUIRect ExtractLayout(RectTransform rectTransform)
        {
            return new PrototypeUIRect(
                rectTransform.anchorMin,
                rectTransform.anchorMax,
                rectTransform.pivot,
                rectTransform.anchoredPosition,
                rectTransform.sizeDelta);
        }

        private static PrototypeUISceneHierarchyEntry ExtractHierarchyEntry(string objectName, Transform target)
        {
            Transform parent = target != null ? target.parent : null;
            string parentName = parent != null ? parent.name : string.Empty;
            int siblingIndex = target != null ? target.GetSiblingIndex() : 0;
            return new PrototypeUISceneHierarchyEntry(objectName, parentName, siblingIndex);
        }

        private static PrototypeUISceneImageEntry ExtractImageEntry(string objectName, Image image)
        {
            Sprite sprite = image != null ? image.sprite : null;
            bool overrideSprite = sprite == null || IsPersistableSprite(sprite);
            if (!overrideSprite)
            {
                sprite = null;
            }

            return new PrototypeUISceneImageEntry(
                objectName,
                overrideSprite,
                sprite,
                image != null ? image.type : Image.Type.Simple,
                image != null ? image.color : Color.white,
                image != null && image.preserveAspect);
        }

        private static PrototypeUISceneTextEntry ExtractTextEntry(string objectName, TextMeshProUGUI text)
        {
            TMP_FontAsset font = text != null ? text.font : null;
            bool overrideFont = font == null || IsPersistableFontAsset(font);
            if (!overrideFont)
            {
                font = null;
            }

            return new PrototypeUISceneTextEntry(
                objectName,
                overrideFont,
                font,
                text != null ? text.fontSize : 0f,
                text != null ? text.color : Color.white,
                text != null ? text.alignment : TextAlignmentOptions.TopLeft,
                text != null && text.raycastTarget,
                text != null && text.enableAutoSizing,
                text != null ? text.fontSizeMin : 0f,
                text != null ? text.fontSizeMax : 0f,
                text != null ? text.fontStyle : FontStyles.Normal,
                text != null ? text.textWrappingMode : TextWrappingModes.NoWrap,
                text != null ? text.overflowMode : TextOverflowModes.Truncate,
                text != null ? text.characterSpacing : 0f,
                text != null ? text.wordSpacing : 0f,
                text != null ? text.lineSpacing : 0f,
                text != null ? text.paragraphSpacing : 0f,
                text != null ? text.margin : Vector4.zero,
                text != null && text.isRightToLeftText);
        }

        private static PrototypeUISceneButtonEntry ExtractButtonEntry(string objectName, Button button)
        {
            Navigation navigation = button != null ? button.navigation : default;
            return new PrototypeUISceneButtonEntry(
                objectName,
                button != null && button.interactable,
                button != null ? button.transition : Selectable.Transition.ColorTint,
                button != null ? button.colors : default,
                navigation.mode);
        }

        private static bool IsPersistableSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            return !string.IsNullOrWhiteSpace(assetPath);
        }

        private static bool IsPersistableFontAsset(TMP_FontAsset font)
        {
            if (font == null)
            {
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(font);
            return !string.IsNullOrWhiteSpace(assetPath);
        }
    }
}
#endif
