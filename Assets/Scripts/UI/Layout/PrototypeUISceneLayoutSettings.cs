using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    /// 이름으로 찾는 Canvas UI Image 오버라이드 1건을 저장합니다.
    /// sprite가 에셋 참조가 가능한 경우에만 sprite까지 저장하고,
    /// 그렇지 않으면 현재 스킨이 잡은 sprite를 유지한 채 다른 속성만 덮어씁니다.
    /// </summary>
    [Serializable]
    public struct PrototypeUISceneImageEntry
    {
        [SerializeField] private string objectName;
        [SerializeField] private bool overrideSprite;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Image.Type type;
        [SerializeField] private Color color;
        [SerializeField] private bool preserveAspect;

        public PrototypeUISceneImageEntry(
            string objectName,
            bool overrideSprite,
            Sprite sprite,
            Image.Type type,
            Color color,
            bool preserveAspect)
        {
            this.objectName = objectName;
            this.overrideSprite = overrideSprite;
            this.sprite = sprite;
            this.type = type;
            this.color = color;
            this.preserveAspect = preserveAspect;
        }

        public string ObjectName => objectName;

        public void ApplyTo(Image image)
        {
            if (image == null)
            {
                return;
            }

            if (overrideSprite)
            {
                image.sprite = sprite;
            }

            image.type = type;
            image.color = color;
            image.preserveAspect = preserveAspect;
        }
    }

    /// <summary>
    /// 에디터 씬에서 조정한 Canvas UI 레이아웃과 Image 값을 저장해
    /// 빌더, 런타임, 감사 코드가 같은 기준을 사용하도록 맞춥니다.
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
        [SerializeField] private List<PrototypeUISceneImageEntry> imageEntries = new();

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

        public bool TryGetImageEntry(string objectName, out PrototypeUISceneImageEntry imageEntry)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                for (int index = 0; index < imageEntries.Count; index++)
                {
                    PrototypeUISceneImageEntry entry = imageEntries[index];
                    if (string.Equals(entry.ObjectName, objectName, StringComparison.Ordinal))
                    {
                        imageEntry = entry;
                        return true;
                    }
                }
            }

            imageEntry = default;
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 전달한 레이아웃 목록으로 현재 저장값을 통째로 교체합니다.
        /// </summary>
        public void ReplaceLayouts(List<PrototypeUISceneLayoutEntry> entries)
        {
            layoutEntries = entries ?? new List<PrototypeUISceneLayoutEntry>();
        }

        /// <summary>
        /// 전달한 Image 오버라이드 목록으로 현재 저장값을 통째로 교체합니다.
        /// </summary>
        public void ReplaceImages(List<PrototypeUISceneImageEntry> entries)
        {
            imageEntries = entries ?? new List<PrototypeUISceneImageEntry>();
        }

        /// <summary>
        /// 저장한 Canvas UI 레이아웃 값을 모두 비웁니다.
        /// </summary>
        public void ClearLayouts()
        {
            layoutEntries.Clear();
        }

        /// <summary>
        /// 저장한 Canvas UI Image 오버라이드 값을 모두 비웁니다.
        /// </summary>
        public void ClearImages()
        {
            imageEntries.Clear();
        }
#endif
    }

    /// <summary>
    /// Canvas UI 레이아웃과 Image 오버라이드 자산을 공용으로 읽고
    /// 에디터 동기화 작업을 제공합니다.
    /// </summary>
    public static class PrototypeUISceneLayoutCatalog
    {
        private static PrototypeUISceneLayoutSettings _cachedSettings;

        /// <summary>
        /// 저장된 레이아웃이 있으면 그 값을, 없으면 전달한 기본값을 반환합니다.
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

        /// <summary>
        /// 저장된 Canvas Image 오버라이드가 있으면 현재 Image에 다시 적용합니다.
        /// </summary>
        public static bool TryApplyImageOverride(Image image, string objectName)
        {
            if (image == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetImageEntry(objectName, out PrototypeUISceneImageEntry imageEntry))
            {
                imageEntry.ApplyTo(image);
                return true;
            }

            return false;
        }

        private static bool TryGetSettings(out PrototypeUISceneLayoutSettings settings)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                settings = AssetDatabase.LoadAssetAtPath<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.AssetPath);
                if (settings == null)
                {
                    settings = Resources.Load<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.ResourcesLoadPath);
                }

                _cachedSettings = settings;
                return settings != null;
            }
#endif

            if (_cachedSettings != null)
            {
                settings = _cachedSettings;
                return true;
            }

            settings = Resources.Load<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.ResourcesLoadPath);

            _cachedSettings = settings;
            return settings != null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 현재 씬 Canvas 아래 모든 UI RectTransform과 Image 값을 공용 자산에 저장합니다.
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
            HashSet<string> duplicateNames = new(StringComparer.Ordinal);

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

                CaptureCanvasOverridesRecursive(rootRect, rootRect, layoutMap, imageMap, duplicateNames);
            }

            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Sync Canvas UI Layouts");
            settings.ReplaceLayouts(ConvertToLayoutEntries(layoutMap));
            settings.ReplaceImages(ConvertToImageEntries(imageMap));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            message = duplicateNames.Count == 0
                ? $"Canvas UI 레이아웃 {layoutMap.Count}건과 Image {imageMap.Count}건을 공용 자산에 저장했습니다."
                : $"Canvas UI 레이아웃 {layoutMap.Count}건과 Image {imageMap.Count}건을 저장했습니다. 중복 이름 {duplicateNames.Count}건은 마지막 값으로 덮어썼습니다.";
            return true;
        }

        /// <summary>
        /// 저장한 Canvas UI 레이아웃과 Image 오버라이드 값을 모두 비웁니다.
        /// </summary>
        public static void ResetCanvasLayouts()
        {
            PrototypeUISceneLayoutSettings settings = LoadOrCreateSettingsAsset();
            Undo.RecordObject(settings, "Reset Canvas UI Layouts");
            settings.ClearLayouts();
            settings.ClearImages();
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

        private static void CaptureCanvasOverridesRecursive(
            RectTransform current,
            RectTransform canvasRoot,
            IDictionary<string, PrototypeUIRect> layoutMap,
            IDictionary<string, PrototypeUISceneImageEntry> imageMap,
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
                if (current.TryGetComponent(out Image image))
                {
                    imageMap[current.name] = ExtractImageEntry(current.name, image);
                }
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CaptureCanvasOverridesRecursive(
                    current.GetChild(index) as RectTransform,
                    canvasRoot,
                    layoutMap,
                    imageMap,
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

        private static bool IsPersistableSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            return !string.IsNullOrWhiteSpace(assetPath);
        }
#endif
    }
}
