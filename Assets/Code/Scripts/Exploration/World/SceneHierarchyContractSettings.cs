using System;
using System.Collections.Generic;
using System.IO;
using Code.Scripts.Shared;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Code.Scripts.Exploration.World
{
    [Serializable]
    public sealed class SceneHierarchyContractEntry
    {
        [SerializeField] private string sceneName = string.Empty;
        [SerializeField] private string objectName = string.Empty;
        [SerializeField] private string sceneObjectPath = string.Empty;
        [SerializeField] private string parentScenePath = string.Empty;
        [SerializeField] private int siblingIndex;
        [SerializeField, FormerlySerializedAs("activeSelf")] private bool initialActiveSelf = true;

        public string SceneName => sceneName ?? string.Empty;
        public string ObjectName => objectName ?? string.Empty;
        public string SceneObjectPath => sceneObjectPath ?? string.Empty;
        public string ParentScenePath => parentScenePath ?? string.Empty;
        public int SiblingIndex => siblingIndex;
        public bool InitialActiveSelf => initialActiveSelf;

#if UNITY_EDITOR
        internal void Configure(string nextSceneName, string nextObjectName, string nextSceneObjectPath)
        {
            sceneName = NormalizeToken(nextSceneName);
            objectName = NormalizeToken(nextObjectName);
            sceneObjectPath = NormalizeScenePath(nextSceneObjectPath);
        }

        internal void CaptureHierarchy(Transform target)
        {
            if (target == null)
            {
                parentScenePath = string.Empty;
                siblingIndex = 0;
                initialActiveSelf = false;
                return;
            }

            parentScenePath = NormalizeScenePath(BuildSceneObjectPath(target.parent));
            siblingIndex = target.GetSiblingIndex();
            initialActiveSelf = target.gameObject.activeSelf;
        }

        private static string BuildSceneObjectPath(Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            Stack<string> segments = new();
            for (Transform current = target; current != null; current = current.parent)
            {
                segments.Push(current.name);
            }

            return string.Join("/", segments);
        }

        private static string NormalizeToken(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string NormalizeScenePath(string value)
        {
            string normalized = value?.Trim().Replace('\\', '/') ?? string.Empty;
            while (normalized.Contains("//"))
            {
                normalized = normalized.Replace("//", "/");
            }

            return normalized.Trim('/');
        }
#endif
    }

    [CreateAssetMenu(fileName = DefaultAssetFileName, menuName = "Jonggu Restaurant/Scene/Scene Hierarchy Contract Settings")]
    public sealed class SceneHierarchyContractSettings : ScriptableObject
    {
        public const string DefaultAssetFileName = "scene-hierarchy-contracts";
        public const string ResourcesLoadPath = "Generated/" + DefaultAssetFileName;

#if UNITY_EDITOR
        public const string AssetPath = ProjectAssetPaths.SceneHierarchyContractSettingsAssetPath;
#endif

        [SerializeField] private List<SceneHierarchyContractEntry> entries = new();

        private static SceneHierarchyContractSettings _cachedSettings;

        public IReadOnlyList<SceneHierarchyContractEntry> Entries => entries;

        public bool TryGetEntry(string sceneName, string objectName, out SceneHierarchyContractEntry entry)
        {
            string normalizedSceneName = NormalizeToken(sceneName);
            string normalizedObjectName = NormalizeToken(objectName);
            for (int index = 0; index < entries.Count; index++)
            {
                SceneHierarchyContractEntry current = entries[index];
                if (current == null
                    || !string.Equals(current.SceneName, normalizedSceneName, StringComparison.Ordinal)
                    || !string.Equals(current.ObjectName, normalizedObjectName, StringComparison.Ordinal))
                {
                    continue;
                }

                entry = current;
                return true;
            }

            entry = null;
            return false;
        }

        public bool TryGetEntry(
            string sceneName,
            string objectName,
            string sceneObjectPath,
            out SceneHierarchyContractEntry entry)
        {
            string normalizedSceneName = NormalizeToken(sceneName);
            string normalizedObjectName = NormalizeToken(objectName);
            string normalizedSceneObjectPath = NormalizeScenePath(sceneObjectPath);
            for (int index = 0; index < entries.Count; index++)
            {
                SceneHierarchyContractEntry current = entries[index];
                if (current == null
                    || !string.Equals(current.SceneName, normalizedSceneName, StringComparison.Ordinal)
                    || !string.Equals(current.ObjectName, normalizedObjectName, StringComparison.Ordinal)
                    || !string.Equals(current.SceneObjectPath, normalizedSceneObjectPath, StringComparison.Ordinal))
                {
                    continue;
                }

                entry = current;
                return true;
            }

            entry = null;
            return false;
        }

        public bool TryGetEntryBySceneObjectPath(string sceneName, string sceneObjectPath, out SceneHierarchyContractEntry entry)
        {
            string normalizedSceneName = NormalizeToken(sceneName);
            string normalizedSceneObjectPath = NormalizeScenePath(sceneObjectPath);
            for (int index = 0; index < entries.Count; index++)
            {
                SceneHierarchyContractEntry current = entries[index];
                if (current == null
                    || !string.Equals(current.SceneName, normalizedSceneName, StringComparison.Ordinal)
                    || !string.Equals(current.SceneObjectPath, normalizedSceneObjectPath, StringComparison.Ordinal))
                {
                    continue;
                }

                entry = current;
                return true;
            }

            entry = null;
            return false;
        }

        public IEnumerable<SceneHierarchyContractEntry> EnumerateSceneEntries(string sceneName)
        {
            string normalizedSceneName = NormalizeToken(sceneName);
            for (int index = 0; index < entries.Count; index++)
            {
                SceneHierarchyContractEntry current = entries[index];
                if (current != null && string.Equals(current.SceneName, normalizedSceneName, StringComparison.Ordinal))
                {
                    yield return current;
                }
            }
        }

        public static SceneHierarchyContractSettings GetCurrent()
        {
#if UNITY_EDITOR
            _cachedSettings = AssetDatabase.LoadAssetAtPath<SceneHierarchyContractSettings>(AssetPath);
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }
#else
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }
#endif

            _cachedSettings = Resources.Load<SceneHierarchyContractSettings>(ResourcesLoadPath);
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            _cachedSettings = CreateInstance<SceneHierarchyContractSettings>();
            _cachedSettings.hideFlags = HideFlags.HideAndDontSave;
            return _cachedSettings;
        }

#if UNITY_EDITOR
        public static SceneHierarchyContractSettings LoadOrCreateAsset()
        {
            SceneHierarchyContractSettings asset = AssetDatabase.LoadAssetAtPath<SceneHierarchyContractSettings>(AssetPath);
            if (asset != null)
            {
                _cachedSettings = asset;
                return asset;
            }

            string directory = Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrWhiteSpace(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            asset = CreateInstance<SceneHierarchyContractSettings>();
            asset.name = DefaultAssetFileName;
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            _cachedSettings = asset;
            return asset;
        }

        public void CaptureFromSceneObject(string sceneName, string objectName, Transform target, string sceneObjectPath)
        {
            if (target == null)
            {
                return;
            }

            string normalizedSceneName = NormalizeToken(sceneName);
            string normalizedObjectName = NormalizeToken(objectName);
            string normalizedSceneObjectPath = NormalizeScenePath(sceneObjectPath);
            if (string.IsNullOrWhiteSpace(normalizedSceneName)
                || string.IsNullOrWhiteSpace(normalizedObjectName)
                || string.IsNullOrWhiteSpace(normalizedSceneObjectPath))
            {
                return;
            }

            SceneHierarchyContractEntry entry = GetOrCreateEntry(normalizedSceneName, normalizedObjectName, normalizedSceneObjectPath);
            entry.Configure(normalizedSceneName, normalizedObjectName, normalizedSceneObjectPath);
            entry.CaptureHierarchy(target);
            SortEntries();
        }

        public bool RemoveBinding(string sceneName, string objectName, string sceneObjectPath = null)
        {
            string normalizedSceneName = NormalizeToken(sceneName);
            string normalizedObjectName = NormalizeToken(objectName);
            string normalizedSceneObjectPath = NormalizeScenePath(sceneObjectPath);
            for (int index = entries.Count - 1; index >= 0; index--)
            {
                SceneHierarchyContractEntry current = entries[index];
                if (current == null
                    || !string.Equals(current.SceneName, normalizedSceneName, StringComparison.Ordinal)
                    || !string.Equals(current.ObjectName, normalizedObjectName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(normalizedSceneObjectPath)
                    && !string.Equals(current.SceneObjectPath, normalizedSceneObjectPath, StringComparison.Ordinal))
                {
                    continue;
                }

                entries.RemoveAt(index);
                return true;
            }

            return false;
        }

        public void SortEntries()
        {
            entries.RemoveAll(entry => entry == null
                                       || string.IsNullOrWhiteSpace(entry.SceneName)
                                       || string.IsNullOrWhiteSpace(entry.ObjectName)
                                       || string.IsNullOrWhiteSpace(entry.SceneObjectPath));
            entries.Sort((left, right) =>
            {
                int sceneComparison = string.Compare(left.SceneName, right.SceneName, StringComparison.Ordinal);
                if (sceneComparison != 0)
                {
                    return sceneComparison;
                }

                int pathComparison = string.Compare(left.SceneObjectPath, right.SceneObjectPath, StringComparison.Ordinal);
                if (pathComparison != 0)
                {
                    return pathComparison;
                }

                return string.Compare(left.ObjectName, right.ObjectName, StringComparison.Ordinal);
            });
        }

        private SceneHierarchyContractEntry GetOrCreateEntry(string sceneName, string objectName, string sceneObjectPath)
        {
            if (TryGetEntry(sceneName, objectName, sceneObjectPath, out SceneHierarchyContractEntry exact))
            {
                return exact;
            }

            if (TryGetEntryBySceneObjectPath(sceneName, sceneObjectPath, out SceneHierarchyContractEntry pathMatch))
            {
                return pathMatch;
            }

            if (TryGetEntry(sceneName, objectName, out SceneHierarchyContractEntry existing)
                && string.IsNullOrWhiteSpace(existing.SceneObjectPath))
            {
                return existing;
            }

            SceneHierarchyContractEntry created = new();
            created.Configure(sceneName, objectName, sceneObjectPath);
            entries.Add(created);
            return created;
        }
#endif

        private void OnValidate()
        {
#if UNITY_EDITOR
            SortEntries();
#endif
        }

        private static string NormalizeToken(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string NormalizeScenePath(string value)
        {
            string normalized = value?.Trim().Replace('\\', '/') ?? string.Empty;
            while (normalized.Contains("//"))
            {
                normalized = normalized.Replace("//", "/");
            }

            return normalized.Trim('/');
        }
    }
}
