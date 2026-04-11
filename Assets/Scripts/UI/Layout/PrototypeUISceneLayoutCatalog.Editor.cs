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
        /// Canvas 한 번 캡처에서 얻은 레이아웃/표시 값 묶음입니다.
        /// sync와 overlay가 같은 수집 결과를 재사용하도록 중간 표현으로 둡니다.
        /// </summary>
        private sealed class CapturedCanvasOverrides
        {
            public Dictionary<string, PrototypeUIRect> Layouts { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, PrototypeUISceneImageEntry> Images { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, PrototypeUISceneTextEntry> Texts { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, PrototypeUISceneButtonEntry> Buttons { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, PrototypeUISceneHierarchyEntry> Hierarchies { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, string> Names { get; } = new(StringComparer.Ordinal);
            public HashSet<string> DuplicateNames { get; } = new(StringComparer.Ordinal);
            public bool UsedPreviewCapture { get; set; }

            public bool HasEntries =>
                Layouts.Count > 0
                || Images.Count > 0
                || Texts.Count > 0
                || Buttons.Count > 0
                || Hierarchies.Count > 0
                || Names.Count > 0;
        }

        /// <summary>
        /// 현재 씬 Canvas 아래 모든 UI RectTransform, Image, TMP, Button 값을 공용 자산에 저장합니다.
        /// </summary>
        public static bool TrySyncCanvasLayoutsFromScene(Scene scene, out string message)
        {
            if (!TryCaptureCanvasOverrides(scene, out CapturedCanvasOverrides captured, out message))
            {
                return false;
            }

            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Sync Canvas UI Layouts");
            settings.ReplaceLayouts(ConvertToLayoutEntries(captured.Layouts));
            settings.ReplaceImages(ConvertToImageEntries(captured.Images));
            settings.ReplaceTexts(ConvertToTextEntries(captured.Texts));
            settings.ReplaceButtons(ConvertToButtonEntries(captured.Buttons));
            settings.ReplaceHierarchies(ConvertToHierarchyEntries(captured.Hierarchies));
            settings.ReplaceRemovedObjects(BuildRemovedObjectNameList(IsHubScene(scene), captured.Hierarchies.Keys, captured.Names));
            settings.ReplaceNames(ConvertToNameEntries(captured.Names));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            if (captured.DuplicateNames.Count == 0)
            {
                message = BuildCanvasCaptureSummary(captured, "공용 자산에 저장했습니다.");
            }
            else
            {
                message = BuildCanvasCaptureSummary(captured, $"저장했습니다. 중복 이름 {captured.DuplicateNames.Count}건은 마지막 값을 기준으로 덮어썼습니다.");
            }

            message = AppendPreviewCaptureMessage(captured, message);
            return true;
        }

        /// <summary>
        /// 현재 씬의 Canvas 값을 기존 공용 자산 위에 덮어써,
        /// 기본 Hub 기준값은 유지하면서 현재 씬에서 조정한 UI 값만 마지막에 반영합니다.
        /// </summary>
        public static bool TryOverlayCanvasLayoutsFromScene(Scene scene, out string message)
        {
            if (!TryCaptureCanvasOverrides(scene, out CapturedCanvasOverrides captured, out message))
            {
                return false;
            }

            if (!captured.HasEntries)
            {
                message = "현재 씬에서 저장할 Canvas UI 값을 찾지 못했습니다.";
                return false;
            }

            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Overlay Canvas UI Layouts");
            settings.UpsertLayouts(ConvertToLayoutEntries(captured.Layouts));
            settings.UpsertImages(ConvertToImageEntries(captured.Images));
            settings.UpsertTexts(ConvertToTextEntries(captured.Texts));
            settings.UpsertButtons(ConvertToButtonEntries(captured.Buttons));
            settings.UpsertHierarchies(ConvertToHierarchyEntries(captured.Hierarchies));
            settings.SyncRemovedObjects(GetManagedCanvasObjectNames(IsHubScene(scene), captured.Names), captured.Hierarchies.Keys);
            settings.UpsertNames(ConvertToNameEntries(captured.Names));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            message = BuildCanvasCaptureSummary(captured, "현재 씬 기준으로 덮어썼습니다.");
            message = AppendPreviewCaptureMessage(captured, message);
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

        private static bool TryCaptureCanvasOverrides(Scene scene, out CapturedCanvasOverrides captured, out string message)
        {
            captured = null;
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

            captured = new CapturedCanvasOverrides();
            CaptureCanvasOverridesFromScene(
                scene,
                canvases,
                captured.Layouts,
                captured.Images,
                captured.Texts,
                captured.Buttons,
                captured.Hierarchies,
                captured.Names,
                captured.DuplicateNames,
                out bool usedPreviewCapture);
            captured.UsedPreviewCapture = usedPreviewCapture;
            message = null;
            return true;
        }

        private static string BuildCanvasCaptureSummary(CapturedCanvasOverrides captured, string actionSuffix)
        {
            return $"Canvas UI 레이아웃 {captured.Layouts.Count}건, Image {captured.Images.Count}건, TMP {captured.Texts.Count}건, Button {captured.Buttons.Count}건을 {actionSuffix}";
        }

        private static string AppendPreviewCaptureMessage(CapturedCanvasOverrides captured, string baseMessage)
        {
            if (captured == null || !captured.UsedPreviewCapture)
            {
                return baseMessage;
            }

            return baseMessage + " 빈 Canvas는 UIManager editor preview 기준 baseline으로 캡처했습니다.";
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
