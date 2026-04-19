#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Code.Scripts.Exploration.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    public static class PrototypeSceneHierarchyContractSyncUtility
    {
        public static int SyncSceneHierarchyContractsFromScene(Scene scene, SceneHierarchyContractSettings settings)
        {
            return SyncSceneHierarchyContractsFromScene(scene, settings, null);
        }

        public static int SyncSceneHierarchyContractsFromScene(
            Scene scene,
            SceneHierarchyContractSettings settings,
            string sceneNameOverride)
        {
            string contractSceneName = ResolveContractSceneName(scene, sceneNameOverride);
            if (!scene.IsValid()
                || !scene.isLoaded
                || settings == null
                || string.IsNullOrWhiteSpace(contractSceneName))
            {
                return 0;
            }

            Dictionary<string, List<Transform>> transformLookup = BuildTransformLookup(scene);
            HashSet<string> capturedSceneObjectPaths = new(StringComparer.Ordinal);
            HashSet<string> managedObjectNames = PrototypeSceneHierarchyCatalog.IsSupportedScene(contractSceneName)
                ? PrototypeSceneHierarchyCatalog.GetManagedObjectNames(contractSceneName)
                : new HashSet<string>(StringComparer.Ordinal);

            int capturedCount = 0;
            if (PrototypeSceneHierarchyCatalog.IsSupportedScene(contractSceneName))
            {
                foreach (PrototypeSceneHierarchyEntry entry in PrototypeSceneHierarchyCatalog.EnumerateManagedEntries(contractSceneName))
                {
                    if (!TryResolveContractTarget(
                            scene,
                            settings,
                            contractSceneName,
                            transformLookup,
                            entry.ObjectName,
                            out Transform target,
                            out string sceneObjectPath))
                    {
                        continue;
                    }

                    settings.CaptureFromSceneObject(contractSceneName, entry.ObjectName, target, sceneObjectPath);
                    capturedSceneObjectPaths.Add(sceneObjectPath);
                    capturedCount++;
                }
            }

            List<SceneAuthoredHelperContractMarker> markers = FindHelperMarkers(scene);
            for (int index = 0; index < markers.Count; index++)
            {
                SceneAuthoredHelperContractMarker marker = markers[index];
                if (marker == null || marker.transform == null || string.IsNullOrWhiteSpace(marker.name))
                {
                    continue;
                }

                string sceneObjectPath = BuildSceneObjectPath(marker.transform);
                settings.CaptureFromSceneObject(contractSceneName, marker.name, marker.transform, sceneObjectPath);
                if (capturedSceneObjectPaths.Add(sceneObjectPath))
                {
                    capturedCount++;
                }
            }

            RemoveStaleSceneEntries(scene, settings, contractSceneName, capturedSceneObjectPaths, managedObjectNames);
            settings.SortEntries();
            return capturedCount;
        }

        public static string BuildSceneObjectPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

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
                for (int index = 1; index < parts.Length && current != null; index++)
                {
                    current = current.Find(parts[index]);
                }

                if (current != null)
                {
                    return current;
                }
            }

            return null;
        }

        private static bool TryResolveContractTarget(
            Scene scene,
            SceneHierarchyContractSettings settings,
            string contractSceneName,
            IReadOnlyDictionary<string, List<Transform>> transformLookup,
            string objectName,
            out Transform target,
            out string sceneObjectPath)
        {
            target = null;
            sceneObjectPath = string.Empty;
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            if (settings.TryGetEntry(contractSceneName, objectName, out SceneHierarchyContractEntry entry)
                && !string.IsNullOrWhiteSpace(entry.SceneObjectPath))
            {
                Transform resolved = ResolveSceneTransform(scene, entry.SceneObjectPath);
                if (resolved != null)
                {
                    target = resolved;
                    sceneObjectPath = entry.SceneObjectPath;
                    return true;
                }

                Debug.LogWarning(
                    $"[PrototypeSceneHierarchyContractSync] '{contractSceneName}/{objectName}' contract path '{entry.SceneObjectPath}' could not be resolved in scene '{scene.name}'. Organizer fallback rules will be used until the contract is fixed.");
            }

            if (!transformLookup.TryGetValue(objectName, out List<Transform> matches) || matches == null || matches.Count != 1)
            {
                return false;
            }

            target = matches[0];
            sceneObjectPath = target != null ? BuildSceneObjectPath(target) : string.Empty;
            return target != null && !string.IsNullOrWhiteSpace(sceneObjectPath);
        }

        private static Dictionary<string, List<Transform>> BuildTransformLookup(Scene scene)
        {
            Dictionary<string, List<Transform>> lookup = new(StringComparer.Ordinal);
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

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
                    Transform current = transforms[index];
                    if (current == null || string.IsNullOrWhiteSpace(current.name))
                    {
                        continue;
                    }

                    if (!lookup.TryGetValue(current.name, out List<Transform> matches))
                    {
                        matches = new List<Transform>();
                        lookup[current.name] = matches;
                    }

                    matches.Add(current);
                }
            }

            return lookup;
        }

        private static List<SceneAuthoredHelperContractMarker> FindHelperMarkers(Scene scene)
        {
            List<SceneAuthoredHelperContractMarker> markers = new();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return markers;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                SceneAuthoredHelperContractMarker[] found = root.GetComponentsInChildren<SceneAuthoredHelperContractMarker>(true);
                for (int index = 0; index < found.Length; index++)
                {
                    if (found[index] != null)
                    {
                        markers.Add(found[index]);
                    }
                }
            }

            return markers;
        }

        private static void RemoveStaleSceneEntries(
            Scene scene,
            SceneHierarchyContractSettings settings,
            string contractSceneName,
            ISet<string> capturedSceneObjectPaths,
            ISet<string> managedObjectNames)
        {
            List<SceneHierarchyContractEntry> staleEntries = new();
            foreach (SceneHierarchyContractEntry entry in settings.EnumerateSceneEntries(contractSceneName))
            {
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.SceneObjectPath)
                    || capturedSceneObjectPaths.Contains(entry.SceneObjectPath))
                {
                    continue;
                }

                Transform resolved = ResolveSceneTransform(scene, entry.SceneObjectPath);
                bool isManagedObject = managedObjectNames != null && managedObjectNames.Contains(entry.ObjectName);
                bool isMarkedHelper = resolved != null && resolved.GetComponent<SceneAuthoredHelperContractMarker>() != null;
                if (isManagedObject || resolved == null || !isMarkedHelper)
                {
                    staleEntries.Add(entry);
                }
            }

            for (int index = 0; index < staleEntries.Count; index++)
            {
                SceneHierarchyContractEntry entry = staleEntries[index];
                settings.RemoveBinding(entry.SceneName, entry.ObjectName, entry.SceneObjectPath);
            }
        }

        private static string ResolveContractSceneName(Scene scene, string sceneNameOverride)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(sceneNameOverride))
            {
                return sceneNameOverride.Trim();
            }

            return scene.name;
        }
    }

    [InitializeOnLoad]
    internal static class PrototypeSceneHierarchyContractSceneSaveSync
    {
        static PrototypeSceneHierarchyContractSceneSaveSync()
        {
            EditorSceneManager.sceneSaving += HandleSceneSaving;
        }

        private static void HandleSceneSaving(Scene scene, string path)
        {
            if (!HasContractTargets(scene))
            {
                return;
            }

            SceneHierarchyContractSettings settings = SceneHierarchyContractSettings.LoadOrCreateAsset();
            if (settings == null)
            {
                return;
            }

            string before = EditorJsonUtility.ToJson(settings);
            int capturedCount = PrototypeSceneHierarchyContractSyncUtility.SyncSceneHierarchyContractsFromScene(scene, settings);
            if (capturedCount <= 0)
            {
                return;
            }

            string after = EditorJsonUtility.ToJson(settings);
            if (string.Equals(before, after, StringComparison.Ordinal))
            {
                return;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static bool HasContractTargets(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (PrototypeSceneHierarchyCatalog.IsSupportedScene(scene.name))
            {
                return true;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.GetComponentInChildren<SceneAuthoredHelperContractMarker>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
