using System;
using System.Collections.Generic;
using TMPro;
using UI.Controllers;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
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
    /// 이름으로 찾는 Canvas UI RectTransform 1건의 레이아웃을 저장합니다.
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
    /// 이름으로 찾는 Canvas UI Image 1건의 표시 값을 저장합니다.
    /// sprite가 에셋으로 저장 가능한 경우에만 sprite까지 저장하고,
    /// 런타임 생성 sprite면 나머지 표시 값만 덮어씁니다.
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
    /// 이름으로 찾는 TextMeshProUGUI 1건의 표시 값을 저장합니다.
    /// HUDRoot 하위 텍스트가 빌더 기본 스타일로 되돌아가지 않도록 사용합니다.
    /// </summary>
    [Serializable]
    public struct PrototypeUISceneTextEntry
    {
        [SerializeField] private string objectName;
        [SerializeField] private bool overrideFont;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private float fontSize;
        [SerializeField] private Color color;
        [SerializeField] private TextAlignmentOptions alignment;
        [SerializeField] private bool raycastTarget;
        [SerializeField] private bool enableAutoSizing;
        [SerializeField] private float fontSizeMin;
        [SerializeField] private float fontSizeMax;
        [SerializeField] private FontStyles fontStyle;
        [SerializeField] private TextWrappingModes textWrappingMode;
        [SerializeField] private TextOverflowModes overflowMode;
        [SerializeField] private float characterSpacing;
        [SerializeField] private float wordSpacing;
        [SerializeField] private float lineSpacing;
        [SerializeField] private float paragraphSpacing;
        [SerializeField] private Vector4 margin;
        [SerializeField] private bool isRightToLeftText;

        public PrototypeUISceneTextEntry(
            string objectName,
            bool overrideFont,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            bool raycastTarget,
            bool enableAutoSizing,
            float fontSizeMin,
            float fontSizeMax,
            FontStyles fontStyle,
            TextWrappingModes textWrappingMode,
            TextOverflowModes overflowMode,
            float characterSpacing,
            float wordSpacing,
            float lineSpacing,
            float paragraphSpacing,
            Vector4 margin,
            bool isRightToLeftText)
        {
            this.objectName = objectName;
            this.overrideFont = overrideFont;
            this.font = font;
            this.fontSize = fontSize;
            this.color = color;
            this.alignment = alignment;
            this.raycastTarget = raycastTarget;
            this.enableAutoSizing = enableAutoSizing;
            this.fontSizeMin = fontSizeMin;
            this.fontSizeMax = fontSizeMax;
            this.fontStyle = fontStyle;
            this.textWrappingMode = textWrappingMode;
            this.overflowMode = overflowMode;
            this.characterSpacing = characterSpacing;
            this.wordSpacing = wordSpacing;
            this.lineSpacing = lineSpacing;
            this.paragraphSpacing = paragraphSpacing;
            this.margin = margin;
            this.isRightToLeftText = isRightToLeftText;
        }

        public string ObjectName => objectName;

        public void ApplyTo(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            if (overrideFont && font != null)
            {
                text.font = font;
                if (font.material != null)
                {
                    text.fontSharedMaterial = font.material;
                }
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = raycastTarget;
            text.enableAutoSizing = enableAutoSizing;
            text.fontSizeMin = fontSizeMin;
            text.fontSizeMax = fontSizeMax;
            text.fontStyle = fontStyle;
            text.textWrappingMode = textWrappingMode;
            text.overflowMode = overflowMode;
            text.characterSpacing = characterSpacing;
            text.wordSpacing = wordSpacing;
            text.lineSpacing = lineSpacing;
            text.paragraphSpacing = paragraphSpacing;
            text.margin = margin;
            text.isRightToLeftText = isRightToLeftText;
        }
    }

    /// <summary>
    /// 이름으로 찾는 Button 1건의 상호작용과 ColorBlock 값을 저장합니다.
    /// HUDRoot 하위 버튼이 빌드 후 기본 버튼 색으로 돌아가지 않도록 사용합니다.
    /// </summary>
    [Serializable]
    public struct PrototypeUISceneButtonEntry
    {
        [SerializeField] private string objectName;
        [SerializeField] private bool interactable;
        [SerializeField] private Selectable.Transition transition;
        [SerializeField] private ColorBlock colors;
        [SerializeField] private Navigation.Mode navigationMode;

        public PrototypeUISceneButtonEntry(
            string objectName,
            bool interactable,
            Selectable.Transition transition,
            ColorBlock colors,
            Navigation.Mode navigationMode)
        {
            this.objectName = objectName;
            this.interactable = interactable;
            this.transition = transition;
            this.colors = colors;
            this.navigationMode = navigationMode;
        }

        public string ObjectName => objectName;

        public void ApplyTo(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            button.transition = transition;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = navigationMode;
            button.navigation = navigation;
        }
    }

    /// <summary>
    /// 이름으로 찾는 Canvas UI 1건의 부모 이름과 형제 순서를 저장한다.
    /// 빌더와 런타임이 같은 그룹 구조를 다시 만들 때 사용한다.
    /// </summary>
    [Serializable]
    public struct PrototypeUISceneHierarchyEntry
    {
        [SerializeField] private string objectName;
        [SerializeField] private string parentName;
        [SerializeField] private int siblingIndex;

        public PrototypeUISceneHierarchyEntry(string objectName, string parentName, int siblingIndex)
        {
            this.objectName = objectName;
            this.parentName = parentName;
            this.siblingIndex = siblingIndex;
        }

        public string ObjectName => objectName;
        public string ParentName => parentName;
        public int SiblingIndex => siblingIndex;
    }

    /// <summary>
    /// 빌더와 런타임이 공용으로 참조하는 기준 이름과 씬의 실제 이름 대응을 저장합니다.
    /// </summary>
    [Serializable]
    public struct PrototypeUISceneNameEntry
    {
        [SerializeField] private string canonicalName;
        [SerializeField] private string sceneName;

        public PrototypeUISceneNameEntry(string canonicalName, string sceneName)
        {
            this.canonicalName = canonicalName;
            this.sceneName = sceneName;
        }

        public string CanonicalName => canonicalName;
        public string SceneName => sceneName;
    }

    /// <summary>
    /// 에디터 씬에서 조정한 Canvas UI 레이아웃과 표시 값을 저장해
    /// 빌더, 런타임, 감사 코드가 같은 기준을 사용하도록 맞춥니다.
    /// </summary>
    [CreateAssetMenu(fileName = DefaultAssetFileName, menuName = "Jonggu Restaurant/UI/Scene Layout Settings")]
    public class PrototypeUISceneLayoutSettings : ScriptableObject
    {
        public const string DefaultAssetFileName = "ui-layout-overrides";
        public const string ResourcesLoadPath = "Generated/" + DefaultAssetFileName;

#if UNITY_EDITOR
        public const string AssetPath = "Assets/Resources/Generated/" + DefaultAssetFileName + ".asset";
#endif

        [SerializeField] private List<PrototypeUISceneLayoutEntry> layoutEntries = new();
        [SerializeField] private List<PrototypeUISceneImageEntry> imageEntries = new();
        [SerializeField] private List<PrototypeUISceneTextEntry> textEntries = new();
        [SerializeField] private List<PrototypeUISceneButtonEntry> buttonEntries = new();
        [SerializeField] private List<PrototypeUISceneHierarchyEntry> hierarchyEntries = new();
        [SerializeField] private List<string> removedObjectNames = new();
        [SerializeField] private List<PrototypeUISceneNameEntry> nameEntries = new();
        private static readonly HashSet<string> ProtectedRemovedObjectNames = new(StringComparer.Ordinal)
        {
            "HUDBottomGroup"
        };

        internal static bool IsProtectedManagedObject(string objectName)
        {
            return !string.IsNullOrWhiteSpace(objectName)
                   && ProtectedRemovedObjectNames.Contains(objectName);
        }

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

        public bool TryGetTextEntry(string objectName, out PrototypeUISceneTextEntry textEntry)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                for (int index = 0; index < textEntries.Count; index++)
                {
                    PrototypeUISceneTextEntry entry = textEntries[index];
                    if (string.Equals(entry.ObjectName, objectName, StringComparison.Ordinal))
                    {
                        textEntry = entry;
                        return true;
                    }
                }
            }

            textEntry = default;
            return false;
        }

        public bool TryGetButtonEntry(string objectName, out PrototypeUISceneButtonEntry buttonEntry)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                for (int index = 0; index < buttonEntries.Count; index++)
                {
                    PrototypeUISceneButtonEntry entry = buttonEntries[index];
                    if (string.Equals(entry.ObjectName, objectName, StringComparison.Ordinal))
                    {
                        buttonEntry = entry;
                        return true;
                    }
                }
            }

            buttonEntry = default;
            return false;
        }

        public bool TryGetHierarchyEntry(string objectName, out PrototypeUISceneHierarchyEntry hierarchyEntry)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                for (int index = 0; index < hierarchyEntries.Count; index++)
                {
                    PrototypeUISceneHierarchyEntry entry = hierarchyEntries[index];
                    if (string.Equals(entry.ObjectName, objectName, StringComparison.Ordinal))
                    {
                        hierarchyEntry = entry;
                        return true;
                    }
                }
            }

            hierarchyEntry = default;
            return false;
        }

        public bool TryGetSceneName(string canonicalName, out string sceneName)
        {
            if (!string.IsNullOrEmpty(canonicalName))
            {
                for (int index = 0; index < nameEntries.Count; index++)
                {
                    PrototypeUISceneNameEntry entry = nameEntries[index];
                    if (string.Equals(entry.CanonicalName, canonicalName, StringComparison.Ordinal))
                    {
                        sceneName = entry.SceneName;
                        return !string.IsNullOrWhiteSpace(sceneName);
                    }
                }
            }

            sceneName = null;
            return false;
        }

        public bool IsObjectRemoved(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            if (IsProtectedManagedObject(objectName))
            {
                return false;
            }

            for (int index = 0; index < removedObjectNames.Count; index++)
            {
                if (string.Equals(removedObjectNames[index], objectName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 전달한 레이아웃 목록으로 현재 저장 값을 통째로 교체합니다.
        /// </summary>
        public void ReplaceLayouts(List<PrototypeUISceneLayoutEntry> entries)
        {
            layoutEntries = entries ?? new List<PrototypeUISceneLayoutEntry>();
        }

        /// <summary>
        /// 전달한 레이아웃 목록만 현재 저장 값에 병합합니다.
        /// </summary>
        public void UpsertLayouts(List<PrototypeUISceneLayoutEntry> entries)
        {
            Dictionary<string, PrototypeUISceneLayoutEntry> layoutMap = new(StringComparer.Ordinal);
            for (int index = 0; index < layoutEntries.Count; index++)
            {
                PrototypeUISceneLayoutEntry entry = layoutEntries[index];
                layoutMap[entry.ObjectName] = entry;
            }

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    PrototypeUISceneLayoutEntry entry = entries[index];
                    layoutMap[entry.ObjectName] = entry;
                }
            }

            layoutEntries = new List<PrototypeUISceneLayoutEntry>(layoutMap.Values);
            layoutEntries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
        }

        /// <summary>
        /// 전달한 Image 표시 값 목록으로 현재 저장 값을 통째로 교체합니다.
        /// </summary>
        public void ReplaceImages(List<PrototypeUISceneImageEntry> entries)
        {
            imageEntries = entries ?? new List<PrototypeUISceneImageEntry>();
        }

        /// <summary>
        /// 전달한 Image 표시 값 목록만 현재 저장 값에 병합합니다.
        /// </summary>
        public void UpsertImages(List<PrototypeUISceneImageEntry> entries)
        {
            Dictionary<string, PrototypeUISceneImageEntry> imageMap = new(StringComparer.Ordinal);
            for (int index = 0; index < imageEntries.Count; index++)
            {
                PrototypeUISceneImageEntry entry = imageEntries[index];
                imageMap[entry.ObjectName] = entry;
            }

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    PrototypeUISceneImageEntry entry = entries[index];
                    imageMap[entry.ObjectName] = entry;
                }
            }

            imageEntries = new List<PrototypeUISceneImageEntry>(imageMap.Values);
            imageEntries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
        }

        /// <summary>
        /// 전달한 TMP 텍스트 표시 값 목록으로 현재 저장 값을 통째로 교체합니다.
        /// </summary>
        public void ReplaceTexts(List<PrototypeUISceneTextEntry> entries)
        {
            textEntries = entries ?? new List<PrototypeUISceneTextEntry>();
        }

        /// <summary>
        /// 전달한 TMP 텍스트 표시 값 목록만 현재 저장 값에 병합합니다.
        /// </summary>
        public void UpsertTexts(List<PrototypeUISceneTextEntry> entries)
        {
            Dictionary<string, PrototypeUISceneTextEntry> textMap = new(StringComparer.Ordinal);
            for (int index = 0; index < textEntries.Count; index++)
            {
                PrototypeUISceneTextEntry entry = textEntries[index];
                textMap[entry.ObjectName] = entry;
            }

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    PrototypeUISceneTextEntry entry = entries[index];
                    textMap[entry.ObjectName] = entry;
                }
            }

            textEntries = new List<PrototypeUISceneTextEntry>(textMap.Values);
            textEntries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
        }

        /// <summary>
        /// 전달한 버튼 표시 값 목록으로 현재 저장 값을 통째로 교체합니다.
        /// </summary>
        public void ReplaceButtons(List<PrototypeUISceneButtonEntry> entries)
        {
            buttonEntries = entries ?? new List<PrototypeUISceneButtonEntry>();
        }

        /// <summary>
        /// 전달한 버튼 표시 값 목록만 현재 저장 값에 병합합니다.
        /// </summary>
        public void UpsertButtons(List<PrototypeUISceneButtonEntry> entries)
        {
            Dictionary<string, PrototypeUISceneButtonEntry> buttonMap = new(StringComparer.Ordinal);
            for (int index = 0; index < buttonEntries.Count; index++)
            {
                PrototypeUISceneButtonEntry entry = buttonEntries[index];
                buttonMap[entry.ObjectName] = entry;
            }

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    PrototypeUISceneButtonEntry entry = entries[index];
                    buttonMap[entry.ObjectName] = entry;
                }
            }

            buttonEntries = new List<PrototypeUISceneButtonEntry>(buttonMap.Values);
            buttonEntries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
        }

        /// <summary>
        /// 전달한 이름 대응 목록으로 현재 저장 값을 통째로 교체합니다.
        /// </summary>
        public void ReplaceHierarchies(List<PrototypeUISceneHierarchyEntry> entries)
        {
            hierarchyEntries = entries ?? new List<PrototypeUISceneHierarchyEntry>();
        }

        public void UpsertHierarchies(List<PrototypeUISceneHierarchyEntry> entries)
        {
            Dictionary<string, PrototypeUISceneHierarchyEntry> hierarchyMap = new(StringComparer.Ordinal);
            for (int index = 0; index < hierarchyEntries.Count; index++)
            {
                PrototypeUISceneHierarchyEntry entry = hierarchyEntries[index];
                hierarchyMap[entry.ObjectName] = entry;
            }

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    PrototypeUISceneHierarchyEntry entry = entries[index];
                    hierarchyMap[entry.ObjectName] = entry;
                }
            }

            hierarchyEntries = new List<PrototypeUISceneHierarchyEntry>(hierarchyMap.Values);
            hierarchyEntries.Sort((left, right) => string.CompareOrdinal(left.ObjectName, right.ObjectName));
        }

        public void ReplaceRemovedObjects(List<string> objectNames)
        {
            removedObjectNames = NormalizeRemovedObjectNames(objectNames);
        }

        public void SyncRemovedObjects(IEnumerable<string> managedObjectNames, IEnumerable<string> presentObjectNames)
        {
            HashSet<string> removedNameSet = new(removedObjectNames, StringComparer.Ordinal);
            HashSet<string> managedNameSet = new(StringComparer.Ordinal);
            HashSet<string> presentNameSet = new(StringComparer.Ordinal);

            if (managedObjectNames != null)
            {
                foreach (string objectName in managedObjectNames)
                {
                    if (!string.IsNullOrWhiteSpace(objectName))
                    {
                        managedNameSet.Add(objectName);
                    }
                }
            }

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

            foreach (string managedObjectName in managedNameSet)
            {
                if (IsProtectedManagedObject(managedObjectName))
                {
                    removedNameSet.Remove(managedObjectName);
                    continue;
                }

                if (presentNameSet.Contains(managedObjectName))
                {
                    removedNameSet.Remove(managedObjectName);
                }
                else
                {
                    removedNameSet.Add(managedObjectName);
                }
            }

            removedObjectNames = NormalizeRemovedObjectNames(removedNameSet);
        }

        public void ReplaceNames(List<PrototypeUISceneNameEntry> entries)
        {
            nameEntries = entries ?? new List<PrototypeUISceneNameEntry>();
        }

        /// <summary>
        /// 전달한 이름 대응 목록만 현재 저장 값에 병합합니다.
        /// </summary>
        public void UpsertNames(List<PrototypeUISceneNameEntry> entries)
        {
            Dictionary<string, PrototypeUISceneNameEntry> nameMap = new(StringComparer.Ordinal);
            for (int index = 0; index < nameEntries.Count; index++)
            {
                PrototypeUISceneNameEntry entry = nameEntries[index];
                nameMap[entry.CanonicalName] = entry;
            }

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    PrototypeUISceneNameEntry entry = entries[index];
                    nameMap[entry.CanonicalName] = entry;
                }
            }

            nameEntries = new List<PrototypeUISceneNameEntry>(nameMap.Values);
            nameEntries.Sort((left, right) => string.CompareOrdinal(left.CanonicalName, right.CanonicalName));
        }

        /// <summary>
        /// 저장한 Canvas UI 레이아웃 값을 모두 비웁니다.
        /// </summary>
        public void ClearLayouts()
        {
            layoutEntries.Clear();
        }

        /// <summary>
        /// 저장한 Canvas UI Image 값을 모두 비웁니다.
        /// </summary>
        public void ClearImages()
        {
            imageEntries.Clear();
        }

        /// <summary>
        /// 저장한 Canvas UI TMP 텍스트 값을 모두 비웁니다.
        /// </summary>
        public void ClearTexts()
        {
            textEntries.Clear();
        }

        /// <summary>
        /// 저장한 Canvas UI 버튼 값을 모두 비웁니다.
        /// </summary>
        public void ClearButtons()
        {
            buttonEntries.Clear();
        }

        /// <summary>
        /// 저장한 Canvas UI 이름 대응 값을 모두 비웁니다.
        /// </summary>
        public void ClearHierarchies()
        {
            hierarchyEntries.Clear();
        }

        public void ClearRemovedObjects()
        {
            removedObjectNames.Clear();
        }

        public void ClearNames()
        {
            nameEntries.Clear();
        }

        private static List<string> NormalizeRemovedObjectNames(IEnumerable<string> objectNames)
        {
            HashSet<string> uniqueNames = new(StringComparer.Ordinal);
            if (objectNames != null)
            {
                foreach (string objectName in objectNames)
                {
                    if (!string.IsNullOrWhiteSpace(objectName)
                        && !IsProtectedManagedObject(objectName))
                    {
                        uniqueNames.Add(objectName);
                    }
                }
            }

            List<string> normalizedNames = new(uniqueNames);
            normalizedNames.Sort(string.CompareOrdinal);
            return normalizedNames;
        }
#endif
    }

    /// <summary>
    /// Canvas UI 레이아웃과 표시 값을 공용 자산으로 읽고 쓰는 도우미입니다.
    /// </summary>
    public static class PrototypeUISceneLayoutCatalog
    {
        private static PrototypeUISceneLayoutSettings _cachedSettings;
#if UNITY_EDITOR
        private static int _ignoreRemovedObjectChecksDepth;
#endif

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
        /// 저장된 Canvas Image 표시 값이 있으면 현재 Image에 다시 적용합니다.
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

        /// <summary>
        /// 저장된 TMP 텍스트 표시 값이 있으면 현재 TextMeshProUGUI에 다시 적용합니다.
        /// </summary>
        public static bool TryApplyTextOverride(TextMeshProUGUI text, string objectName)
        {
            if (text == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetTextEntry(objectName, out PrototypeUISceneTextEntry textEntry))
            {
                textEntry.ApplyTo(text);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 저장된 Button 표시 값이 있으면 현재 버튼에 다시 적용합니다.
        /// </summary>
        public static bool TryApplyButtonOverride(Button button, string objectName)
        {
            if (button == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetButtonEntry(objectName, out PrototypeUISceneButtonEntry buttonEntry))
            {
                buttonEntry.ApplyTo(button);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 씬 기준 이름 오버라이드가 있으면 그 값을, 없으면 기준 이름을 그대로 반환합니다.
        /// </summary>
        public static bool TryGetHierarchyOverride(string objectName, out string parentName, out int siblingIndex)
        {
            parentName = null;
            siblingIndex = 0;

            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetHierarchyEntry(objectName, out PrototypeUISceneHierarchyEntry hierarchyEntry))
            {
                parentName = hierarchyEntry.ParentName;
                siblingIndex = hierarchyEntry.SiblingIndex;
                return !string.IsNullOrWhiteSpace(parentName);
            }

            return false;
        }

        public static bool IsObjectRemoved(string objectName)
        {
#if UNITY_EDITOR
            if (_ignoreRemovedObjectChecksDepth > 0)
            {
                return false;
            }
#endif
            return !string.IsNullOrWhiteSpace(objectName)
                && TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.IsObjectRemoved(objectName);
        }

#if UNITY_EDITOR
        internal static IDisposable SuppressRemovedObjectChecks()
        {
            _ignoreRemovedObjectChecksDepth++;
            return new RemovedObjectSuppressionScope();
        }

        private sealed class RemovedObjectSuppressionScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _ignoreRemovedObjectChecksDepth = Math.Max(0, _ignoreRemovedObjectChecksDepth - 1);
            }
        }
#endif

        public static HashSet<string> GetManagedCanvasObjectNames(bool isHubScene)
        {
            return GetManagedCanvasObjectNames(isHubScene, null);
        }

        public static HashSet<string> GetManagedCanvasObjectNames(bool isHubScene, IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            HashSet<string> objectNames = new(StringComparer.Ordinal)
            {
                "HUDRoot",
                "PopupRoot",
                "HUDStatusGroup",
                ResolveManagedObjectName("HUDActionGroup", sceneNameOverrides),
                "HUDBottomGroup",
                "HUDOverlayGroup",
                "PopupShellGroup",
                "PopupFrameHeader",
                "TopLeftPanel",
                "GoldText",
                "InteractionPromptBackdrop",
                "InteractionPromptText",
                "GuideBackdrop",
                "GuideText",
                "GuideHelpButton",
                "ResultBackdrop",
                "RestaurantResultText",
            };

            if (!isHubScene)
            {
                return objectNames;
            }

            objectNames.Add(ResolveManagedObjectName("HUDPanelButtonGroup", sceneNameOverrides));
            objectNames.Add("PopupOverlay");
            objectNames.Add("PopupFrame");
            objectNames.Add("PopupFrameLeft");
            objectNames.Add("PopupFrameRight");
            objectNames.Add("PopupLeftBody");
            objectNames.Add("PopupRightBody");
            objectNames.Add(PrototypeUIObjectNames.PopupTitle);
            objectNames.Add(PrototypeUIObjectNames.PopupLeftCaption);
            objectNames.Add(PrototypeUIObjectNames.PopupRightCaption);
            objectNames.Add("PopupCloseButton");
            objectNames.Add("InventoryText");
            objectNames.Add("StorageText");
            objectNames.Add("SelectedRecipeText");
            objectNames.Add("UpgradeText");
            objectNames.Add("ActionDock");
            objectNames.Add("ActionAccent");
            objectNames.Add("ActionCaption");
            objectNames.Add("RecipePanelButton");
            objectNames.Add("UpgradePanelButton");
            objectNames.Add("MaterialPanelButton");

            for (int index = 0; index < PrototypeUILayout.HubPopupBodyItemBoxCount; index++)
            {
                int displayIndex = index + 1;
                objectNames.Add($"PopupLeftItemBox{displayIndex:00}");
                objectNames.Add($"PopupLeftItemIcon{displayIndex:00}");
                objectNames.Add($"PopupLeftItemText{displayIndex:00}");
                objectNames.Add($"PopupRightItemBox{displayIndex:00}");
                objectNames.Add($"PopupRightItemIcon{displayIndex:00}");
                objectNames.Add($"PopupRightItemText{displayIndex:00}");
            }

            return objectNames;
        }

        private static string ResolveManagedObjectName(string canonicalName, IReadOnlyDictionary<string, string> sceneNameOverrides)
        {
            if (sceneNameOverrides != null
                && sceneNameOverrides.TryGetValue(canonicalName, out string sceneName)
                && !string.IsNullOrWhiteSpace(sceneName))
            {
                return sceneName;
            }

            return ResolveObjectName(canonicalName);
        }

        public static string ResolveObjectName(string canonicalName)
        {
            if (string.IsNullOrWhiteSpace(canonicalName))
            {
                return canonicalName;
            }

            if (TryGetSettings(out PrototypeUISceneLayoutSettings settings)
                && settings.TryGetSceneName(canonicalName, out string sceneName))
            {
                return sceneName;
            }

            return canonicalName;
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
                if (previewRoot == null || !previewCanvasObject.TryGetComponent(out global::UI.UIManager uiManager))
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

        private static bool IsHubScene(Scene scene)
        {
            return string.Equals(scene.name, "Hub", StringComparison.Ordinal);
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

        private static PrototypeUISceneLayoutSettings LoadOrCreateSettingsAsset()
        {
            PrototypeUISceneLayoutSettings settings = AssetDatabase.LoadAssetAtPath<PrototypeUISceneLayoutSettings>(PrototypeUISceneLayoutSettings.AssetPath);
            if (settings != null)
            {
                _cachedSettings = settings;
                return settings;
            }

            EnsureFolder("Assets/Resources", "Generated");

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
#endif
    }
}
