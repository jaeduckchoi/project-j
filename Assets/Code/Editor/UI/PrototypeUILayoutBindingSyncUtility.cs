using System;
using System.Collections.Generic;
using Code.Scripts.UI;
using Code.Scripts.UI.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.UI
{
    public static class PrototypeUILayoutBindingSyncUtility
    {
        public static int SyncManagedBindingsFromScene(Scene scene, PrototypeUILayoutBindingSettings settings)
        {
            if (!scene.IsValid() || !scene.isLoaded || settings == null)
            {
                return 0;
            }

            Dictionary<string, List<RectTransform>> rectLookup = BuildRectTransformLookup(scene);
            int capturedCount = 0;
            foreach (string runtimeName in PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(IsHubScene(scene)))
            {
                if (PrototypeUISceneLayoutCatalog.IsRuntimeOnlyObjectName(runtimeName))
                {
                    settings.RemoveBinding(runtimeName);
                    continue;
                }

                if (!TryResolveBindingRect(
                        scene,
                        settings,
                        rectLookup,
                        runtimeName,
                        out RectTransform rect,
                        out string sceneObjectPath,
                        logUnresolvedBindingWarning: true))
                {
                    continue;
                }

                settings.CaptureFromSource(runtimeName, rect, sceneObjectPath);
                capturedCount++;
            }

            return capturedCount;
        }

        public static GameObject ResolveLayoutPreviewObject(Scene scene, PrototypeUILayoutBindingSettings settings, string runtimeName)
        {
            if (TryResolveBindingRect(scene, settings, runtimeName, out RectTransform rect, out _))
            {
                return rect.gameObject;
            }

            return null;
        }

        public static bool TryResolveBindingRect(
            Scene scene,
            PrototypeUILayoutBindingSettings settings,
            string runtimeName,
            out RectTransform rect,
            out string sceneObjectPath)
        {
            return TryResolveBindingRect(
                scene,
                settings,
                BuildRectTransformLookup(scene),
                runtimeName,
                out rect,
                out sceneObjectPath,
                logUnresolvedBindingWarning: false);
        }

        private static bool TryResolveBindingRect(
            Scene scene,
            PrototypeUILayoutBindingSettings settings,
            IReadOnlyDictionary<string, List<RectTransform>> rectLookup,
            string runtimeName,
            out RectTransform rect,
            out string sceneObjectPath,
            bool logUnresolvedBindingWarning)
        {
            rect = null;
            sceneObjectPath = string.Empty;
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(runtimeName))
            {
                return false;
            }

            if (settings != null
                && settings.TryGetEntry(runtimeName, out PrototypeUILayoutBindingEntry entry)
                && !string.IsNullOrWhiteSpace(entry.SceneObjectPath))
            {
                Transform resolved = ResolveSceneTransform(scene, entry.SceneObjectPath);
                if (resolved is RectTransform boundRect)
                {
                    rect = boundRect;
                    sceneObjectPath = entry.SceneObjectPath;
                    return true;
                }

                if (logUnresolvedBindingWarning)
                {
                    Debug.LogWarning(
                        $"[PrototypeUILayoutBindingSync] '{runtimeName}' binding path '{entry.SceneObjectPath}' could not be resolved in scene '{scene.name}'. Runtime fallback grouping will be used until the binding is fixed.",
                        FindComponentInScene<UIManager>(scene));
                }
            }

            if (!rectLookup.TryGetValue(runtimeName, out List<RectTransform> matches) || matches == null || matches.Count != 1)
            {
                return false;
            }

            rect = matches[0];
            sceneObjectPath = rect != null ? BuildSceneObjectPath(rect.transform) : string.Empty;
            return rect != null && !string.IsNullOrWhiteSpace(sceneObjectPath);
        }

        public static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            return SceneObjectPathUtility.FindComponentInScene<T>(scene);
        }

        public static string BuildSceneObjectPath(Transform transform)
        {
            return SceneObjectPathUtility.BuildSceneObjectPath(transform);
        }

        public static Transform ResolveSceneTransform(Scene scene, string path)
        {
            return SceneObjectPathUtility.ResolveSceneTransform(scene, path);
        }

        public static bool IsHubScene(Scene scene)
        {
            return scene.IsValid()
                   && (scene.name == "Hub"
                       || scene.path.EndsWith("/Hub.unity", StringComparison.OrdinalIgnoreCase)
                       || scene.path.EndsWith("\\Hub.unity", StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, List<RectTransform>> BuildRectTransformLookup(Scene scene)
        {
            return SceneObjectPathUtility.BuildLookup(scene, root => root.GetComponentsInChildren<RectTransform>(true));
        }
    }

    [InitializeOnLoad]
    internal static class PrototypeUILayoutBindingSceneSaveSync
    {
        static PrototypeUILayoutBindingSceneSaveSync()
        {
            EditorSceneManager.sceneSaving += HandleSceneSaving;
        }

        private static void HandleSceneSaving(Scene scene, string path)
        {
            if (!scene.IsValid()
                || !scene.isLoaded
                || PrototypeUILayoutBindingSyncUtility.FindComponentInScene<UIManager>(scene) == null)
            {
                return;
            }

            PrototypeUILayoutBindingSettings settings = PrototypeUILayoutBindingSettings.LoadOrCreateAsset();
            if (settings == null)
            {
                return;
            }

            string before = EditorJsonUtility.ToJson(settings);
            int capturedCount = PrototypeUILayoutBindingSyncUtility.SyncManagedBindingsFromScene(scene, settings);
            if (capturedCount <= 0)
            {
                return;
            }

            settings.SortBindings();
            string after = EditorJsonUtility.ToJson(settings);
            if (string.Equals(before, after, StringComparison.Ordinal))
            {
                return;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            PrototypeUISceneLayoutCatalog.ReloadBindingSettingsForEditor();
        }
    }
}
