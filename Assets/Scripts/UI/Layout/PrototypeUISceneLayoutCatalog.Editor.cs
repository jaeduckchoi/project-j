#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Layout
{
    /// <summary>
    /// 에디터 전용 sync/overlay/reset 진입점과 제거 검사 억제 스코프, 자산 로딩 도우미를 보관하는 파셜입니다.
    /// 캡처/추출 로직은 .Editor.Capture.cs 파셜에 분리되어 있습니다.
    /// </summary>
    public static partial class PrototypeUISceneLayoutCatalog
    {
        private static int _ignoreRemovedObjectChecksDepth;

        internal static IDisposable SuppressRemovedObjectChecks()
        {
            _ignoreRemovedObjectChecksDepth++;
            return new RemovedObjectSuppressionScope();
        }

        private sealed class RemovedObjectSuppressionScope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                _ignoreRemovedObjectChecksDepth = Math.Max(0, _ignoreRemovedObjectChecksDepth - 1);
            }
        }

        /// <summary>
        /// 현재 씬 Canvas 아래 모든 UI RectTransform, Image, TMP, Button 값을 공용 자산에 저장합니다.
        /// </summary>
        public static bool TrySyncCanvasLayoutsFromScene(Scene scene, out string message)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                message = "열려 있는 씬이 없어 Canvas UI 값을 읽을 수 없습니다.";
                return false;
            }

            List<Canvas> canvases = GetSceneCanvases(scene);
            if (canvases.Count == 0)
            {
                message = "현재 씬에서 Canvas 컴포넌트를 찾지 못했습니다.";
                return false;
            }

            Dictionary<string, PrototypeUIRect> layoutMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneImageEntry> imageMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneTextEntry> textMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneButtonEntry> buttonMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap = new(StringComparer.Ordinal);
            Dictionary<string, string> nameMap = new(StringComparer.Ordinal);
            HashSet<string> duplicateNames = new(StringComparer.Ordinal);

            CaptureCanvasOverridesFromScene(
                scene,
                canvases,
                layoutMap,
                imageMap,
                textMap,
                buttonMap,
                hierarchyMap,
                nameMap,
                duplicateNames,
                out bool usedPreviewCapture);

            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Sync Canvas UI Layouts");
            settings.ReplaceLayouts(ConvertToLayoutEntries(layoutMap));
            settings.ReplaceImages(ConvertToImageEntries(imageMap));
            settings.ReplaceTexts(ConvertToTextEntries(textMap));
            settings.ReplaceButtons(ConvertToButtonEntries(buttonMap));
            settings.ReplaceHierarchies(ConvertToHierarchyEntries(hierarchyMap));
            settings.ReplaceRemovedObjects(BuildRemovedObjectNameList(IsHubScene(scene), hierarchyMap.Keys, nameMap));
            settings.ReplaceNames(ConvertToNameEntries(nameMap));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            if (duplicateNames.Count == 0)
            {
                message = $"Canvas UI 레이아웃 {layoutMap.Count}건, Image {imageMap.Count}건, TMP {textMap.Count}건, Button {buttonMap.Count}건을 공용 자산에 저장했습니다.";
            }
            else
            {
                message = $"Canvas UI 레이아웃 {layoutMap.Count}건, Image {imageMap.Count}건, TMP {textMap.Count}건, Button {buttonMap.Count}건을 저장했습니다. 중복 이름 {duplicateNames.Count}건은 마지막 값을 기준으로 덮어썼습니다.";
            }

            if (usedPreviewCapture)
            {
                message += " 빈 Canvas는 UIManager editor preview 기준 baseline으로 캡처했습니다.";
            }

            return true;
        }

        /// <summary>
        /// 현재 씬의 HUD 그룹 이름과 기준 레이아웃만 공용 자산에 다시 저장합니다.
        /// 전체 Canvas 오버라이드를 덮지 않고, HUD 구조 기준만 현재 씬 값으로 맞출 때 사용합니다.
        /// </summary>
        /// <summary>
        /// 현재 씬의 Canvas 값을 기존 공용 자산 위에 덮어써,
        /// 기본 Hub 기준값은 유지하면서 현재 씬에서 조정한 UI 값만 마지막에 반영합니다.
        /// </summary>
        public static bool TryOverlayCanvasLayoutsFromScene(Scene scene, out string message)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                message = "열려 있는 씬이 없어 Canvas UI 값을 읽을 수 없습니다.";
                return false;
            }

            List<Canvas> canvases = GetSceneCanvases(scene);
            if (canvases.Count == 0)
            {
                message = "현재 씬에서 Canvas 컴포넌트를 찾지 못했습니다.";
                return false;
            }

            Dictionary<string, PrototypeUIRect> layoutMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneImageEntry> imageMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneTextEntry> textMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneButtonEntry> buttonMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap = new(StringComparer.Ordinal);
            Dictionary<string, string> nameMap = new(StringComparer.Ordinal);
            HashSet<string> duplicateNames = new(StringComparer.Ordinal);

            CaptureCanvasOverridesFromScene(
                scene,
                canvases,
                layoutMap,
                imageMap,
                textMap,
                buttonMap,
                hierarchyMap,
                nameMap,
                duplicateNames,
                out bool usedPreviewCapture);

            if (layoutMap.Count == 0
                && imageMap.Count == 0
                && textMap.Count == 0
                && buttonMap.Count == 0
                && hierarchyMap.Count == 0
                && nameMap.Count == 0)
            {
                message = "현재 씬에서 저장할 Canvas UI 값을 찾지 못했습니다.";
                return false;
            }

            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Overlay Canvas UI Layouts");
            settings.UpsertLayouts(ConvertToLayoutEntries(layoutMap));
            settings.UpsertImages(ConvertToImageEntries(imageMap));
            settings.UpsertTexts(ConvertToTextEntries(textMap));
            settings.UpsertButtons(ConvertToButtonEntries(buttonMap));
            settings.UpsertHierarchies(ConvertToHierarchyEntries(hierarchyMap));
            settings.SyncRemovedObjects(GetManagedCanvasObjectNames(IsHubScene(scene), nameMap), hierarchyMap.Keys);
            settings.UpsertNames(ConvertToNameEntries(nameMap));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            message = $"Canvas UI 레이아웃 {layoutMap.Count}건, Image {imageMap.Count}건, TMP {textMap.Count}건, Button {buttonMap.Count}건을 현재 씬 기준으로 덮어썼습니다.";
            if (usedPreviewCapture)
            {
                message += " 빈 Canvas는 UIManager editor preview 기준 baseline으로 캡처했습니다.";
            }

            return true;
        }

        public static bool TrySyncHudOverridesFromScene(Scene scene, out string message)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                message = "열려 있는 씬이 없어 HUD 값을 읽을 수 없습니다.";
                return false;
            }

            List<Canvas> canvases = GetSceneCanvases(scene);
            if (canvases.Count == 0)
            {
                message = "현재 씬에서 Canvas 컴포넌트를 찾지 못했습니다.";
                return false;
            }

            Dictionary<string, string> nameMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUIRect> layoutMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneImageEntry> imageMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneTextEntry> textMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneButtonEntry> buttonMap = new(StringComparer.Ordinal);
            Dictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap = new(StringComparer.Ordinal);
            for (int canvasIndex = 0; canvasIndex < canvases.Count; canvasIndex++)
            {
                Canvas canvas = canvases[canvasIndex];
                if (canvas == null)
                {
                    continue;
                }

                RectTransform rootRect = canvas.transform as RectTransform;
                if (rootRect == null)
                {
                    continue;
                }

                CaptureHudStructureOverrides(rootRect, nameMap, layoutMap, imageMap, textMap, buttonMap, hierarchyMap);
            }

            if (nameMap.Count == 0
                && layoutMap.Count == 0
                && imageMap.Count == 0
                && textMap.Count == 0
                && hierarchyMap.Count == 0
                && buttonMap.Count == 0)
            {
                message = "현재 씬에서 HUDRoot 기준을 찾지 못했습니다.";
                return false;
            }

            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Sync HUD Overrides");
            settings.UpsertNames(ConvertToNameEntries(nameMap));
            settings.UpsertLayouts(ConvertToLayoutEntries(layoutMap));
            settings.UpsertImages(ConvertToImageEntries(imageMap));
            settings.UpsertTexts(ConvertToTextEntries(textMap));
            settings.UpsertButtons(ConvertToButtonEntries(buttonMap));
            settings.UpsertHierarchies(ConvertToHierarchyEntries(hierarchyMap));
            settings.SyncRemovedObjects(GetManagedCanvasObjectNames(IsHubScene(scene), nameMap), hierarchyMap.Keys);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            message = $"HUD 이름 {nameMap.Count}건, 레이아웃 {layoutMap.Count}건, Image {imageMap.Count}건, TMP {textMap.Count}건, Button {buttonMap.Count}건을 현재 씬 HUDRoot 기준으로 저장했습니다.";
            return true;
        }

        /// <summary>
        /// 저장한 Canvas UI 레이아웃과 표시 값을 모두 비웁니다.
        /// </summary>
        public static void ResetCanvasLayouts()
        {
            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Reset Canvas UI Layouts");
            settings.ClearLayouts();
            settings.ClearImages();
            settings.ClearTexts();
            settings.ClearButtons();
            settings.ClearHierarchies();
            settings.ClearRemovedObjects();
            settings.ClearNames();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static List<Canvas> GetSceneCanvases(Scene scene)
        {
            List<Canvas> results = new();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                GameObject root = roots[index];
                if (root == null)
                {
                    continue;
                }

                results.AddRange(root.GetComponentsInChildren<Canvas>(true));
            }

            return results;
        }

        private static bool IsHubScene(Scene scene)
        {
            return string.Equals(scene.name, "Hub", StringComparison.Ordinal);
        }

        private static PrototypeUISceneLayoutSettings LoadOrCreateSettingsAsset()
        {
            PrototypeUISceneLayoutSettings settings = AssetDatabase.LoadAssetAtPath<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.AssetPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder("Assets/Resources", "Generated");

            settings = ScriptableObject.CreateInstance<PrototypeUISceneLayoutSettings>();
            AssetDatabase.CreateAsset(settings, PrototypeUISceneLayoutSettings.AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string childPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(childPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }
    }
}
#endif
