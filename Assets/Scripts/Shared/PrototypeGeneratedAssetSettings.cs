using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Shared
{
    /// <summary>
    /// 런타임이 직접 사용하는 generated 리소스 경로와 기본 표시 값을 관리한다.
    /// 정적 이미지 빌더/감사 경로는 더 이상 여기서 다루지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = DefaultAssetFileName, menuName = "Jonggu Restaurant/Shared/Generated Asset Settings")]
    public sealed class PrototypeGeneratedAssetSettings : ScriptableObject
    {
        public const string DefaultAssetFileName = "prototype-generated-asset-settings";
        public const string ResourcesLoadPath = "Generated/" + DefaultAssetFileName;

#if UNITY_EDITOR
        public const string AssetPath = "Assets/Resources/Generated/" + DefaultAssetFileName + ".asset";
#endif

        private const string DefaultResourcesGeneratedRoot = "Assets/Resources/Generated";

        [Header("출력 루트")]
        [SerializeField] private string resourcesGeneratedRoot = DefaultResourcesGeneratedRoot;

        [Header("플레이어/월드 기본값")]
        [SerializeField] private float playerSpritePixelsPerUnit = 80f;
        [SerializeField] private float playerVisualScale = 0.76f;
        [SerializeField] private Vector3 defaultPlayerRootScale = new(1.5f, 1.5f, 1f);
        [SerializeField] private float worldTitleFontSize = 5.1f;
        [SerializeField] private float worldLabelFontSize = 3.3f;
        [SerializeField] private float worldLabelSmallFontSize = 3.0f;

        private static PrototypeGeneratedAssetSettings _cachedSettings;

        public string ResourcesGeneratedRoot => resourcesGeneratedRoot;
        public float PlayerSpritePixelsPerUnit => playerSpritePixelsPerUnit;
        public float PlayerVisualScale => playerVisualScale;
        public Vector3 DefaultPlayerRootScale => defaultPlayerRootScale;
        public float WorldTitleFontSize => worldTitleFontSize;
        public float WorldLabelFontSize => worldLabelFontSize;
        public float WorldLabelSmallFontSize => worldLabelSmallFontSize;

        public string GameDataRoot => CombineAssetPath(ResourcesGeneratedRoot, "GameData");
        public string ResourceDataRoot => CombineAssetPath(GameDataRoot, "Resources");
        public string RecipeDataRoot => CombineAssetPath(GameDataRoot, "Recipes");
        public string InputDataRoot => CombineAssetPath(GameDataRoot, "Input");
        public string SpriteRoot => CombineAssetPath(ResourcesGeneratedRoot, "Sprites");
        public string PlayerSpriteRoot => CombineAssetPath(SpriteRoot, "Player");
        public string UiSpriteRoot => CombineAssetPath(SpriteRoot, "UI");
        public string UiButtonSpriteRoot => CombineAssetPath(UiSpriteRoot, "Buttons");
        public string UiMessageBoxSpriteRoot => CombineAssetPath(UiSpriteRoot, "MessageBoxes");
        public string UiPanelSpriteRoot => CombineAssetPath(UiSpriteRoot, "Panels");
        public string RecipeSpriteRoot => CombineAssetPath(SpriteRoot, "Recipes");

        public string PlayerFrontSpritePath => CombineAssetPath(PlayerSpriteRoot, "player-front.png");
        public string PlayerBackSpritePath => CombineAssetPath(PlayerSpriteRoot, "player-back.png");
        public string PlayerSideSpritePath => CombineAssetPath(PlayerSpriteRoot, "player-side.png");
        public string PlayerFrontIdleFrame2SpritePath => CombineAssetPath(PlayerSpriteRoot, "player-front-idle-2.png");
        public string PlayerBackIdleFrame2SpritePath => CombineAssetPath(PlayerSpriteRoot, "player-back-idle-2.png");
        public string PlayerSideIdleFrame2SpritePath => CombineAssetPath(PlayerSpriteRoot, "player-side-idle-2.png");

        public string PlayerFrontSpriteResourcePath => ToResourcesLoadPath(PlayerFrontSpritePath);
        public string PlayerBackSpriteResourcePath => ToResourcesLoadPath(PlayerBackSpritePath);
        public string PlayerSideSpriteResourcePath => ToResourcesLoadPath(PlayerSideSpritePath);
        public string PlayerFrontIdleFrame2SpriteResourcePath => ToResourcesLoadPath(PlayerFrontIdleFrame2SpritePath);
        public string PlayerBackIdleFrame2SpriteResourcePath => ToResourcesLoadPath(PlayerBackIdleFrame2SpritePath);
        public string PlayerSideIdleFrame2SpriteResourcePath => ToResourcesLoadPath(PlayerSideIdleFrame2SpritePath);
        public string RecipeSpriteResourceRoot => ToResourcesLoadPath(RecipeSpriteRoot);
        public string GeneratedUiResourceRoot => ToResourcesLoadPath(UiSpriteRoot);
        public string GeneratedUiButtonResourceRoot => ToResourcesLoadPath(UiButtonSpriteRoot);
        public string GeneratedUiMessageBoxResourceRoot => ToResourcesLoadPath(UiMessageBoxSpriteRoot);
        public string GeneratedUiPanelResourceRoot => ToResourcesLoadPath(UiPanelSpriteRoot);

        public static PrototypeGeneratedAssetSettings GetCurrent()
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

#if UNITY_EDITOR
            _cachedSettings = AssetDatabase.LoadAssetAtPath<PrototypeGeneratedAssetSettings>(AssetPath);
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }
#endif

            _cachedSettings = Resources.Load<PrototypeGeneratedAssetSettings>(ResourcesLoadPath);
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            _cachedSettings = CreateInstance<PrototypeGeneratedAssetSettings>();
            _cachedSettings.hideFlags = HideFlags.HideAndDontSave;
            return _cachedSettings;
        }

        private void OnValidate()
        {
            resourcesGeneratedRoot = NormalizeAssetPath(resourcesGeneratedRoot, DefaultResourcesGeneratedRoot);
            if (!resourcesGeneratedRoot.StartsWith("Assets/Resources", StringComparison.Ordinal))
            {
                resourcesGeneratedRoot = DefaultResourcesGeneratedRoot;
            }
        }

        private static string NormalizeAssetPath(string value, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim().Replace('\\', '/');

            while (normalized.Contains("//"))
            {
                normalized = normalized.Replace("//", "/");
            }

            return normalized.TrimEnd('/');
        }

        private static string CombineAssetPath(string root, string child)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return NormalizeAssetPath(child, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(child))
            {
                return NormalizeAssetPath(root, string.Empty);
            }

            return NormalizeAssetPath(root, string.Empty) + "/" + NormalizeAssetPath(child, string.Empty).TrimStart('/');
        }

        private static string ToResourcesLoadPath(string assetPath)
        {
            string normalizedPath = NormalizeAssetPath(assetPath, string.Empty);
            const string resourcesPrefix = "Assets/Resources/";
            if (!normalizedPath.StartsWith(resourcesPrefix, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            string relativePath = normalizedPath.Substring(resourcesPrefix.Length);
            int extensionIndex = relativePath.LastIndexOf('.');
            return extensionIndex >= 0
                ? relativePath.Substring(0, extensionIndex)
                : relativePath;
        }
    }
}
