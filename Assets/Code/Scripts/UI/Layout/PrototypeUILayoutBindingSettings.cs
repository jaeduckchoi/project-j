using System;
using System.Collections.Generic;
using Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace UI.Layout
{
    /// <summary>
    /// Serializable RectTransform values used by the runtime layout binding asset.
    /// </summary>
    [Serializable]
    public struct PrototypeUILayoutBindingRect
    {
        [SerializeField] private Vector2 anchorMin;
        [SerializeField] private Vector2 anchorMax;
        [SerializeField] private Vector2 pivot;
        [SerializeField] private Vector2 anchoredPosition;
        [SerializeField] private Vector2 sizeDelta;

        public PrototypeUILayoutBindingRect(PrototypeUIRect rect)
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

#if UNITY_EDITOR
        public static PrototypeUILayoutBindingRect Capture(RectTransform rect)
        {
            if (rect == null)
            {
                return default;
            }

            return new PrototypeUILayoutBindingRect(new PrototypeUIRect(
                rect.anchorMin,
                rect.anchorMax,
                rect.pivot,
                rect.anchoredPosition,
                rect.sizeDelta));
        }
#endif
    }

    /// <summary>
    /// Serializable Image display values for one runtime UI object.
    /// </summary>
    [Serializable]
    public struct PrototypeUILayoutBindingImageOverride
    {
        [SerializeField] private bool overrideSprite;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Image.Type type;
        [SerializeField] private Color color;
        [SerializeField] private bool preserveAspect;
        [SerializeField] private bool raycastTarget;
        [SerializeField] private bool fillCenter;
        [SerializeField] private Image.FillMethod fillMethod;
        [SerializeField] private int fillOrigin;
        [SerializeField] private float fillAmount;
        [SerializeField] private bool fillClockwise;
        [SerializeField] private float pixelsPerUnitMultiplier;

        public bool OverridesSprite => overrideSprite;
        public Sprite Sprite => sprite;

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
            image.raycastTarget = raycastTarget;
            image.fillCenter = fillCenter;
            image.fillMethod = fillMethod;
            image.fillOrigin = fillOrigin;
            image.fillAmount = fillAmount;
            image.fillClockwise = fillClockwise;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        }

        public void SetSpriteOverride(Sprite overrideValue)
        {
            overrideSprite = overrideValue != null;
            sprite = overrideValue;
        }

        public static PrototypeUILayoutBindingImageOverride CreateDefault()
        {
            return new PrototypeUILayoutBindingImageOverride
            {
                overrideSprite = false,
                sprite = null,
                type = Image.Type.Simple,
                color = Color.white,
                preserveAspect = false,
                raycastTarget = true,
                fillCenter = true,
                fillMethod = Image.FillMethod.Horizontal,
                fillOrigin = 0,
                fillAmount = 1f,
                fillClockwise = true,
                pixelsPerUnitMultiplier = 1f
            };
        }

#if UNITY_EDITOR
        public static PrototypeUILayoutBindingImageOverride Capture(Image image)
        {
            if (image == null)
            {
                return default;
            }

            Sprite capturedSprite = image.sprite;
            bool canStoreSprite = capturedSprite == null || EditorUtility.IsPersistent(capturedSprite);
            return new PrototypeUILayoutBindingImageOverride
            {
                overrideSprite = canStoreSprite,
                sprite = canStoreSprite ? capturedSprite : null,
                type = image.type,
                color = image.color,
                preserveAspect = image.preserveAspect,
                raycastTarget = image.raycastTarget,
                fillCenter = image.fillCenter,
                fillMethod = image.fillMethod,
                fillOrigin = image.fillOrigin,
                fillAmount = image.fillAmount,
                fillClockwise = image.fillClockwise,
                pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier
            };
        }
#endif
    }

    /// <summary>
    /// Serializable TextMeshPro display values for one runtime UI object.
    /// </summary>
    [Serializable]
    public struct PrototypeUILayoutBindingTextOverride
    {
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

        public void ApplyTo(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            if (overrideFont && font != null)
            {
                text.font = font;
                if (TryGetFontMaterial(font, out Material material))
                {
                    text.fontSharedMaterial = material;
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

        private static bool TryGetFontMaterial(TMP_FontAsset sourceFont, out Material material)
        {
            material = null;
            if (sourceFont == null)
            {
                return false;
            }

            try
            {
                material = sourceFont.material;
                return material != null;
            }
            catch (MissingReferenceException)
            {
                material = null;
                return false;
            }
        }

#if UNITY_EDITOR
        public static PrototypeUILayoutBindingTextOverride Capture(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return default;
            }

            TMP_FontAsset capturedFont = text.font;
            bool canStoreFont = capturedFont != null && EditorUtility.IsPersistent(capturedFont);
            return new PrototypeUILayoutBindingTextOverride
            {
                overrideFont = canStoreFont,
                font = canStoreFont ? capturedFont : null,
                fontSize = text.fontSize,
                color = text.color,
                alignment = text.alignment,
                raycastTarget = text.raycastTarget,
                enableAutoSizing = text.enableAutoSizing,
                fontSizeMin = text.fontSizeMin,
                fontSizeMax = text.fontSizeMax,
                fontStyle = text.fontStyle,
                textWrappingMode = text.textWrappingMode,
                overflowMode = text.overflowMode,
                characterSpacing = text.characterSpacing,
                wordSpacing = text.wordSpacing,
                lineSpacing = text.lineSpacing,
                paragraphSpacing = text.paragraphSpacing,
                margin = text.margin,
                isRightToLeftText = text.isRightToLeftText
            };
        }
#endif
    }

    /// <summary>
    /// Serializable Button display values for one runtime UI object.
    /// </summary>
    [Serializable]
    public struct PrototypeUILayoutBindingButtonOverride
    {
        [SerializeField] private Selectable.Transition transition;
        [SerializeField] private ColorBlock colors;
        [SerializeField] private SpriteState spriteState;

        public void ApplyTo(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.transition = transition;
            button.colors = colors;
            button.spriteState = spriteState;
        }

#if UNITY_EDITOR
        public static PrototypeUILayoutBindingButtonOverride Capture(Button button)
        {
            if (button == null)
            {
                return default;
            }

            return new PrototypeUILayoutBindingButtonOverride
            {
                transition = button.transition,
                colors = button.colors,
                spriteState = button.spriteState
            };
        }
#endif
    }

    /// <summary>
    /// One explicit binding between a managed runtime UI object id and a scene object.
    /// </summary>
    [Serializable]
    public sealed class PrototypeUILayoutBindingEntry
    {
        [SerializeField] private string runtimeObjectName;
        [SerializeField] private string sceneObjectPath;
        [SerializeField] private string memo;
        [SerializeField] private bool applyRect;
        [SerializeField] private PrototypeUILayoutBindingRect rect;
        [SerializeField] private bool applyImage;
        [SerializeField] private PrototypeUILayoutBindingImageOverride image;
        [SerializeField] private bool applyText;
        [SerializeField] private PrototypeUILayoutBindingTextOverride text;
        [SerializeField] private bool applyButton;
        [SerializeField] private PrototypeUILayoutBindingButtonOverride button;

        public string RuntimeObjectName => runtimeObjectName;
        public string SceneObjectPath => sceneObjectPath;
        public string Memo => memo;
        public bool ApplyRect => applyRect;
        public bool ApplyImage => applyImage;
        public bool ApplyText => applyText;
        public bool ApplyButton => applyButton;

        public bool TryGetLayout(out PrototypeUIRect layout)
        {
            if (applyRect)
            {
                layout = rect.ToRuntimeRect();
                return true;
            }

            layout = default;
            return false;
        }

        public bool TryApplyImage(Image targetImage)
        {
            if (!applyImage || targetImage == null)
            {
                return false;
            }

            image.ApplyTo(targetImage);
            return true;
        }

        public bool TryApplyText(TextMeshProUGUI targetText)
        {
            if (!applyText || targetText == null)
            {
                return false;
            }

            text.ApplyTo(targetText);
            return true;
        }

        public bool TryApplyButton(Button targetButton)
        {
            if (!applyButton || targetButton == null)
            {
                return false;
            }

            button.ApplyTo(targetButton);
            return true;
        }

        public bool TryGetImageSpriteOverride(out Sprite overrideSprite)
        {
            if (!applyImage || !image.OverridesSprite)
            {
                overrideSprite = null;
                return false;
            }

            overrideSprite = image.Sprite;
            return true;
        }

#if UNITY_EDITOR
        public void Configure(string runtimeName, string scenePath)
        {
            runtimeObjectName = runtimeName;
            sceneObjectPath = scenePath;
        }

        /// <summary>
        /// Stores an editor-only memo for the selected runtime UI binding.
        /// </summary>
        public void SetMemo(string memoText)
        {
            memo = memoText ?? string.Empty;
        }

        public void SetApplyFlags(bool rectEnabled, bool imageEnabled, bool textEnabled, bool buttonEnabled)
        {
            applyRect = rectEnabled;
            applyImage = imageEnabled;
            applyText = textEnabled;
            applyButton = buttonEnabled;
        }

        /// <summary>
        /// Clears the stored source path and captured display overrides while keeping the runtime id and memo.
        /// </summary>
        public void ClearBinding()
        {
            sceneObjectPath = string.Empty;
            applyRect = false;
            rect = default;
            applyImage = false;
            image = default;
            applyText = false;
            text = default;
            applyButton = false;
            button = default;
        }

        /// <summary>
        /// Stores a sprite override for the selected runtime UI object and preserves existing image settings when available.
        /// </summary>
        public void SetSpriteOverride(Sprite overrideSprite, Image sourceImage)
        {
            if (overrideSprite == null)
            {
                applyImage = false;
                image = default;
                return;
            }

            if (sourceImage != null)
            {
                image = PrototypeUILayoutBindingImageOverride.Capture(sourceImage);
            }
            else if (!applyImage)
            {
                image = PrototypeUILayoutBindingImageOverride.CreateDefault();
            }

            image.SetSpriteOverride(overrideSprite);
            applyImage = true;
        }

        public void CaptureFrom(RectTransform sourceRect)
        {
            if (sourceRect == null)
            {
                return;
            }

            rect = PrototypeUILayoutBindingRect.Capture(sourceRect);
            applyRect = true;

            Image sourceImage = sourceRect.GetComponent<Image>();
            applyImage = sourceImage != null;
            if (sourceImage != null)
            {
                image = PrototypeUILayoutBindingImageOverride.Capture(sourceImage);
            }

            TextMeshProUGUI sourceText = sourceRect.GetComponent<TextMeshProUGUI>();
            applyText = sourceText != null;
            if (sourceText != null)
            {
                text = PrototypeUILayoutBindingTextOverride.Capture(sourceText);
            }

            Button sourceButton = sourceRect.GetComponent<Button>();
            applyButton = sourceButton != null;
            if (sourceButton != null)
            {
                button = PrototypeUILayoutBindingButtonOverride.Capture(sourceButton);
            }
        }
#endif
    }

    /// <summary>
    /// Stores explicit scene-object bindings for runtime-managed UI ids.
    /// </summary>
    [CreateAssetMenu(fileName = DefaultAssetFileName, menuName = "Jonggu Restaurant/UI/Layout Binding Settings")]
    public sealed class PrototypeUILayoutBindingSettings : ScriptableObject
    {
        public const string DefaultAssetFileName = "ui-layout-bindings";
        public const string ResourcesLoadPath = "Generated/" + DefaultAssetFileName;

#if UNITY_EDITOR
        public const string AssetPath = ProjectAssetPaths.UiLayoutBindingSettingsAssetPath;
#endif

        [SerializeField] private List<PrototypeUILayoutBindingEntry> bindings = new();

        public IReadOnlyList<PrototypeUILayoutBindingEntry> Bindings => bindings;

        public bool TryGetEntry(string runtimeObjectName, out PrototypeUILayoutBindingEntry binding)
        {
            if (!string.IsNullOrWhiteSpace(runtimeObjectName))
            {
                for (int index = 0; index < bindings.Count; index++)
                {
                    PrototypeUILayoutBindingEntry entry = bindings[index];
                    if (entry != null && string.Equals(entry.RuntimeObjectName, runtimeObjectName, StringComparison.Ordinal))
                    {
                        binding = entry;
                        return true;
                    }
                }
            }

            binding = null;
            return false;
        }

#if UNITY_EDITOR
        public PrototypeUILayoutBindingEntry GetOrCreateEntry(string runtimeObjectName)
        {
            if (TryGetEntry(runtimeObjectName, out PrototypeUILayoutBindingEntry existing))
            {
                return existing;
            }

            PrototypeUILayoutBindingEntry created = new();
            created.Configure(runtimeObjectName, string.Empty);
            bindings.Add(created);
            SortBindings();
            return created;
        }

        public void SetBindingSource(string runtimeObjectName, string sceneObjectPath)
        {
            PrototypeUILayoutBindingEntry entry = GetOrCreateEntry(runtimeObjectName);
            entry.Configure(runtimeObjectName, sceneObjectPath);
            SortBindings();
        }

        /// <summary>
        /// Stores an editor memo for one managed runtime UI object.
        /// </summary>
        public void SetBindingMemo(string runtimeObjectName, string memoText)
        {
            if (!TryGetEntry(runtimeObjectName, out PrototypeUILayoutBindingEntry entry))
            {
                if (string.IsNullOrWhiteSpace(memoText))
                {
                    return;
                }

                entry = GetOrCreateEntry(runtimeObjectName);
            }

            entry.SetMemo(memoText);
            RemoveEntryIfRedundant(runtimeObjectName);
            SortBindings();
        }

        /// <summary>
        /// Stores a sprite override for one managed runtime UI object.
        /// </summary>
        public void SetBindingSprite(string runtimeObjectName, Sprite sprite, Image sourceImage)
        {
            if (!TryGetEntry(runtimeObjectName, out PrototypeUILayoutBindingEntry entry))
            {
                if (sprite == null)
                {
                    return;
                }

                entry = GetOrCreateEntry(runtimeObjectName);
            }

            entry.SetSpriteOverride(sprite, sourceImage);
            RemoveEntryIfRedundant(runtimeObjectName);
            SortBindings();
        }

        public void SetApplyFlags(
            string runtimeObjectName,
            bool applyRect,
            bool applyImage,
            bool applyText,
            bool applyButton)
        {
            PrototypeUILayoutBindingEntry entry = GetOrCreateEntry(runtimeObjectName);
            entry.SetApplyFlags(applyRect, applyImage, applyText, applyButton);
        }

        public void CaptureFromSource(string runtimeObjectName, RectTransform sourceRect, string sceneObjectPath)
        {
            PrototypeUILayoutBindingEntry entry = GetOrCreateEntry(runtimeObjectName);
            entry.Configure(runtimeObjectName, sceneObjectPath);
            entry.CaptureFrom(sourceRect);
            SortBindings();
        }

        /// <summary>
        /// Clears the stored source path and overrides for one managed runtime UI object while preserving its memo.
        /// </summary>
        public bool ClearBinding(string runtimeObjectName)
        {
            for (int index = bindings.Count - 1; index >= 0; index--)
            {
                PrototypeUILayoutBindingEntry entry = bindings[index];
                if (entry == null || !string.Equals(entry.RuntimeObjectName, runtimeObjectName, StringComparison.Ordinal))
                {
                    continue;
                }

                entry.ClearBinding();
                if (IsRedundant(entry))
                {
                    bindings.RemoveAt(index);
                }

                return true;
            }

            return false;
        }

        public bool RemoveBinding(string runtimeObjectName)
        {
            for (int index = bindings.Count - 1; index >= 0; index--)
            {
                PrototypeUILayoutBindingEntry entry = bindings[index];
                if (entry != null && string.Equals(entry.RuntimeObjectName, runtimeObjectName, StringComparison.Ordinal))
                {
                    bindings.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public void SortBindings()
        {
            bindings.RemoveAll(entry => entry == null || string.IsNullOrWhiteSpace(entry.RuntimeObjectName));
            bindings.Sort((left, right) => string.CompareOrdinal(left.RuntimeObjectName, right.RuntimeObjectName));
        }

        private void RemoveEntryIfRedundant(string runtimeObjectName)
        {
            for (int index = bindings.Count - 1; index >= 0; index--)
            {
                PrototypeUILayoutBindingEntry entry = bindings[index];
                if (entry != null
                    && string.Equals(entry.RuntimeObjectName, runtimeObjectName, StringComparison.Ordinal)
                    && IsRedundant(entry))
                {
                    bindings.RemoveAt(index);
                }
            }
        }

        private static bool IsRedundant(PrototypeUILayoutBindingEntry entry)
        {
            return entry != null
                && string.IsNullOrWhiteSpace(entry.SceneObjectPath)
                && string.IsNullOrWhiteSpace(entry.Memo)
                && !entry.ApplyRect
                && !entry.ApplyImage
                && !entry.ApplyText
                && !entry.ApplyButton;
        }

        public static PrototypeUILayoutBindingSettings LoadOrCreateAsset()
        {
            PrototypeUILayoutBindingSettings settings = AssetDatabase.LoadAssetAtPath<PrototypeUILayoutBindingSettings>(AssetPath);
            if (settings != null)
            {
                return settings;
            }

            string directory = Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrWhiteSpace(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            settings = CreateInstance<PrototypeUILayoutBindingSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
#endif
    }
}
