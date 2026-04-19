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
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root != null ? root.GetComponentInChildren<T>(true) : null;
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static string BuildSceneObjectPath(Transform transform)
        {
            Stack<string> segments = new();
            for (Transform current = transform; current != null; current = current.parent)
            {
                segments.Push(current.name);
            }

            return string.Join("/", segments);
        }

        public static Transform ResolveSceneTransform(Scene scene, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null || parts.Length == 0 || !string.Equals(root.name, parts[0], StringComparison.Ordinal))
                {
                    continue;
                }

                Transform current = root.transform;
                for (int i = 1; i < parts.Length && current != null; i++)
                {
                    current = current.Find(parts[i]);
                }

                if (current != null)
                {
                    return current;
                }
            }

            return null;
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
            Dictionary<string, List<RectTransform>> lookup = new(StringComparer.Ordinal);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return lookup;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                RectTransform[] rectTransforms = root.GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rectTransforms.Length; i++)
                {
                    RectTransform current = rectTransforms[i];
                    if (current == null || string.IsNullOrWhiteSpace(current.name))
                    {
                        continue;
                    }

                    if (!lookup.TryGetValue(current.name, out List<RectTransform> matches))
                    {
                        matches = new List<RectTransform>();
                        lookup[current.name] = matches;
                    }

                    matches.Add(current);
                }
            }

            return lookup;
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
