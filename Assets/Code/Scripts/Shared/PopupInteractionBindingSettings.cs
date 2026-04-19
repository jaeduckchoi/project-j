using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Code.Scripts.Shared
{
    [Serializable]
    public sealed class PopupInteractionBindingEntry
    {
        [SerializeField] private string popupKey = string.Empty;
        [SerializeField] private string sceneObjectPath = string.Empty;
        [SerializeField, TextArea] private string memo = string.Empty;

        public string PopupKey => popupKey ?? string.Empty;
        public string SceneObjectPath => sceneObjectPath ?? string.Empty;
        public string Memo => memo ?? string.Empty;

#if UNITY_EDITOR
        internal void SetPopupKey(string value)
        {
            popupKey = NormalizeKey(value);
        }

        internal void SetSceneObjectPath(string value)
        {
            sceneObjectPath = NormalizePath(value);
        }

        internal void SetMemo(string value)
        {
            memo = value?.Trim() ?? string.Empty;
        }
#endif

        private static string NormalizeKey(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string NormalizePath(string value)
        {
            string normalized = value?.Trim().Replace('\\', '/') ?? string.Empty;
            while (normalized.Contains("//"))
            {
                normalized = normalized.Replace("//", "/");
            }

            return normalized.Trim('/');
        }
    }

    [CreateAssetMenu(fileName = DefaultAssetFileName, menuName = "Jonggu Restaurant/Shared/Popup Interaction Binding Settings")]
    public sealed class PopupInteractionBindingSettings : ScriptableObject
    {
        public const string DefaultAssetFileName = "popup-interaction-bindings";
        public const string ResourcesLoadPath = "Generated/" + DefaultAssetFileName;

#if UNITY_EDITOR
        public const string AssetPath = ProjectAssetPaths.PopupInteractionBindingSettingsAssetPath;
#endif

        [SerializeField] private List<PopupInteractionBindingEntry> bindings = new();

        private static PopupInteractionBindingSettings _cachedSettings;

        public IReadOnlyList<PopupInteractionBindingEntry> Bindings => bindings;

        public bool TryGetEntry(string popupKey, out PopupInteractionBindingEntry entry)
        {
            string normalizedKey = NormalizeKey(popupKey);
            for (int i = 0; i < bindings.Count; i++)
            {
                PopupInteractionBindingEntry current = bindings[i];
                if (current != null && string.Equals(current.PopupKey, normalizedKey, StringComparison.Ordinal))
                {
                    entry = current;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public static PopupInteractionBindingSettings GetCurrent()
        {
#if UNITY_EDITOR
            _cachedSettings = AssetDatabase.LoadAssetAtPath<PopupInteractionBindingSettings>(AssetPath);
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

            _cachedSettings = Resources.Load<PopupInteractionBindingSettings>(ResourcesLoadPath);
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            _cachedSettings = CreateInstance<PopupInteractionBindingSettings>();
            _cachedSettings.hideFlags = HideFlags.HideAndDontSave;
            return _cachedSettings;
        }

#if UNITY_EDITOR
        public static PopupInteractionBindingSettings LoadOrCreateAsset()
        {
            PopupInteractionBindingSettings asset = AssetDatabase.LoadAssetAtPath<PopupInteractionBindingSettings>(AssetPath);
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

            asset = CreateInstance<PopupInteractionBindingSettings>();
            asset.name = DefaultAssetFileName;
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            _cachedSettings = asset;
            return asset;
        }

        public void SetBindingSource(string popupKey, string sceneObjectPath)
        {
            string normalizedKey = NormalizeKey(popupKey);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return;
            }

            PopupInteractionBindingEntry entry = GetOrCreateEntry(normalizedKey);
            entry.SetSceneObjectPath(sceneObjectPath);
            RemoveEntryIfEmpty(entry);
        }

        public void SetBindingMemo(string popupKey, string memo)
        {
            string normalizedKey = NormalizeKey(popupKey);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return;
            }

            PopupInteractionBindingEntry entry = GetOrCreateEntry(normalizedKey);
            entry.SetMemo(memo);
            RemoveEntryIfEmpty(entry);
        }

        public void RemoveBinding(string popupKey)
        {
            string normalizedKey = NormalizeKey(popupKey);
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                PopupInteractionBindingEntry current = bindings[i];
                if (current == null || string.Equals(current.PopupKey, normalizedKey, StringComparison.Ordinal))
                {
                    bindings.RemoveAt(i);
                }
            }
        }

        public void SortBindings()
        {
            bindings.RemoveAll(entry => entry == null || string.IsNullOrWhiteSpace(entry.PopupKey));
            bindings.Sort((left, right) => string.Compare(left.PopupKey, right.PopupKey, StringComparison.Ordinal));
        }

        private PopupInteractionBindingEntry GetOrCreateEntry(string popupKey)
        {
            if (TryGetEntry(popupKey, out PopupInteractionBindingEntry existing))
            {
                existing.SetPopupKey(popupKey);
                return existing;
            }

            PopupInteractionBindingEntry created = new();
            created.SetPopupKey(popupKey);
            bindings.Add(created);
            return created;
        }

        private void RemoveEntryIfEmpty(PopupInteractionBindingEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(entry.SceneObjectPath) || !string.IsNullOrWhiteSpace(entry.Memo))
            {
                return;
            }

            bindings.Remove(entry);
        }
#endif

        private void OnValidate()
        {
#if UNITY_EDITOR
            SortBindings();
#endif
        }

        private static string NormalizeKey(string value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}
