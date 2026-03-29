using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

// UI.Layout 네임스페이스
namespace UI.Layout
{
    /// <summary>
    /// PrototypeUIRect를 ScriptableObject에 저장하기 위한 직렬화 전용 값 타입입니다.
    /// </summary>
    [Serializable]
    public struct PrototypeUISerializableRect
    {
        [SerializeField] private Vector2 anchorMin;
        [SerializeField] private Vector2 anchorMax;
        [SerializeField] private Vector2 pivot;
        [SerializeField] private Vector2 anchoredPosition;
        [SerializeField] private Vector2 sizeDelta;

        public PrototypeUISerializableRect(PrototypeUIRect rect)
        {
            anchorMin = rect.AnchorMin;
            anchorMax = rect.AnchorMax;
            pivot = rect.Pivot;
            anchoredPosition = rect.AnchoredPosition;
            sizeDelta = rect.SizeDelta;
        }

        public PrototypeUIRect ToRuntimeRect()
        {
            return new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        }
    }

    /// <summary>
    /// 이름으로 찾는 Canvas UI RectTransform 레이아웃 1건을 저장합니다.
    /// </summary>
    [Serializable]
    public struct PrototypeUISceneLayoutEntry
    {
        [SerializeField] private string objectName;
        [SerializeField] private PrototypeUISerializableRect layout;

        public PrototypeUISceneLayoutEntry(string objectName, PrototypeUIRect layout)
        {
            this.objectName = objectName;
            this.layout = new PrototypeUISerializableRect(layout);
        }

        public string ObjectName => objectName;
        public PrototypeUIRect Layout => layout.ToRuntimeRect();
    }

    /// <summary>
    /// 에디터 씬에서 조정한 Canvas UI 레이아웃을 저장해 빌더, 런타임, 감사 코드가 함께 읽도록 유지합니다.
    /// </summary>
    [CreateAssetMenu(fileName = DefaultAssetFileName, menuName = "Jonggu Restaurant/UI/Scene Layout Settings")]
    public class PrototypeUISceneLayoutSettings : ScriptableObject
    {
        public const string DefaultAssetFileName = "uiLayoutOverrides";
        public const string ResourcesLoadPath = "Generated/UI/" + DefaultAssetFileName;

#if UNITY_EDITOR
        public const string AssetPath = "Assets/Resources/Generated/UI/" + DefaultAssetFileName + ".asset";
#endif

        [SerializeField] private List<PrototypeUISceneLayoutEntry> layoutEntries = new();

        public bool TryGetLayout(string objectName, out PrototypeUIRect layout)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                for (int index = 0; index < layoutEntries.Count; index++)
                {
                    PrototypeUISceneLayoutEntry entry = layoutEntries[index];
                    if (string.Equals(entry.ObjectName, objectName, StringComparison.Ordinal))
                    {
                        layout = entry.Layout;
                        return true;
                    }
                }
            }

            layout = default;
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 저장된 레이아웃 목록을 씬 기준 값으로 통째로 교체합니다.
        /// </summary>
        public void ReplaceLayouts(List<PrototypeUISceneLayoutEntry> entries)
        {
            layoutEntries = entries ?? new List<PrototypeUISceneLayoutEntry>();
        }

        /// <summary>
        /// 저장된 Canvas 레이아웃 오버라이드를 모두 비웁니다.
        /// </summary>
        public void ClearLayouts()
        {
            layoutEntries.Clear();
        }
#endif
    }

    /// <summary>
    /// Canvas UI 레이아웃 오버라이드 자산을 공통으로 읽고 에디터 동기화 작업을 제공합니다.
    /// </summary>
    public static class PrototypeUISceneLayoutCatalog
    {
        private static PrototypeUISceneLayoutSettings _cachedSettings;

        /// <summary>
        /// 저장된 레이아웃이 있으면 해당 이름의 값을, 없으면 전달된 기본값을 반환합니다.
        /// </summary>
        public static PrototypeUIRect ResolveLayout(string objectName, PrototypeUIRect fallback)
        {
            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetLayout(objectName, out PrototypeUIRect layout))
            {
                return layout;
            }

            return fallback;
        }

        private static bool TryGetSettings(out PrototypeUISceneLayoutSettings settings)
        {
            if (_cachedSettings != null)
            {
                settings = _cachedSettings;
                return true;
            }

            settings = Resources.Load<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.ResourcesLoadPath);

#if UNITY_EDITOR
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.AssetPath);
            }
#endif

            _cachedSettings = settings;
            return settings != null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 현재 씬 Canvas 아래 모든 UI RectTransform 값을 공용 자산에 저장합니다.
        /// </summary>
        public static bool TrySyncCanvasLayoutsFromScene(Scene scene, out string message)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                message = "열려 있는 씬이 없어서 Canvas UI 레이아웃을 읽을 수 없습니다.";
                return false;
            }

            List<Canvas> canvases = GetSceneCanvases(scene);
            if (canvases.Count == 0)
            {
                message = "현재 씬에서 Canvas 컴포넌트를 찾지 못했습니다.";
                return false;
            }

            Dictionary<string, PrototypeUIRect> layoutMap = new(StringComparer.Ordinal);
            List<string> duplicateNames = new();

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

                CaptureRectLayoutsRecursive(rootRect, rootRect, layoutMap, duplicateNames);
            }

            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Sync Canvas UI Layouts");
            settings.ReplaceLayouts(ConvertToEntries(layoutMap));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            message = duplicateNames.Count == 0
                ? $"Canvas UI 레이아웃 {layoutMap.Count}건을 공용 자산에 저장했습니다."
                : $"Canvas UI 레이아웃 {layoutMap.Count}건을 저장했습니다. 중복 이름 {duplicateNames.Count}건은 마지막 값으로 덮어썼습니다.";
            return true;
        }

        /// <summary>
        /// 저장된 Canvas UI 레이아웃 오버라이드를 모두 비웁니다.
        /// </summary>
        public static void ResetCanvasLayouts()
        {
            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Reset Canvas UI Layouts");
            settings.ClearLayouts();
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

        private static void CaptureRectLayoutsRecursive(
            RectTransform current,
            RectTransform canvasRoot,
            IDictionary<string, PrototypeUIRect> layoutMap,
            ICollection<string> duplicateNames)
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
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CaptureRectLayoutsRecursive(current.GetChild(index) as RectTransform, canvasRoot, layoutMap, duplicateNames);
            }
        }

        private static List<PrototypeUISceneLayoutEntry> ConvertToEntries(IDictionary<string, PrototypeUIRect> layoutMap)
        {
            List<PrototypeUISceneLayoutEntry> entries = new(layoutMap.Count);
            foreach (KeyValuePair<string, PrototypeUIRect> pair in layoutMap)
            {
                entries.Add(new PrototypeUISceneLayoutEntry(pair.Key, pair.Value));
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
            return entries;
        }

        private static PrototypeUISceneLayoutSettings LoadOrCreateSettingsAsset()
        {
            PrototypeUISceneLayoutSettings settings = AssetDatabase.LoadAssetAtPath<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.AssetPath);
            if (settings != null)
            {
                _cachedSettings = settings;
                return settings;
            }

            EnsureFolder("Assets/Resources", "Generated");
            EnsureFolder("Assets/Resources/Generated", "UI");

            settings = ScriptableObject.CreateInstance<PrototypeUISceneLayoutSettings>();
            AssetDatabase.CreateAsset(settings, PrototypeUISceneLayoutSettings.AssetPath);
            AssetDatabase.SaveAssets();
            _cachedSettings = settings;
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

        private static PrototypeUIRect ExtractLayout(RectTransform rectTransform)
        {
            return new PrototypeUIRect(
                rectTransform.anchorMin,
                rectTransform.anchorMax,
                rectTransform.pivot,
                rectTransform.anchoredPosition,
                rectTransform.sizeDelta);
        }
#endif
    }
}
