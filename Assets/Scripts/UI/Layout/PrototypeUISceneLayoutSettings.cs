using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
}
