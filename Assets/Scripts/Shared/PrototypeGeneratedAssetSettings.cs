using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Shared 네임스페이스
namespace Shared
{
    /// <summary>
    /// 생성 자산 경로와 허브 아트 원본, 플레이어/월드 텍스트 기본값을 한 곳에서 관리합니다.
    /// 빌더를 최소화하더라도 런타임, 에디터 감사, 보조 도구가 같은 경로 기준을 공유하도록 맞춥니다.
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
        private const string DefaultSceneRoot = "Assets/Scenes";
        private const string DefaultUiGeneratedSourceRoot = "Assets/Design/GeneratedSources/UI";
        private const string DefaultHubFloorTileDesignSourcePath = "Assets/Design/Tile set-20260405T131607Z-3-001/Tile set/Hub Tile/Wooden tile.png";
        private const string DefaultHubBarDesignSourcePath = "Assets/Design/Object-20260405T122251Z-3-001/Object/Main Hub/Counter.png";
        private const string DefaultHubWallBackgroundHorizontalWallDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 5.png";
        private const string DefaultHubWallBackgroundVerticalWallDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 11.png";
        private const string DefaultHubWallBackgroundFillDesignSourcePath = "Assets/Design/Tile set-20260405T131607Z-3-001/Tile set/Stone Tile/Stone Tile 4.png";
        private const string DefaultHubWallBackgroundBottomLeftDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 10.png";
        private const string DefaultHubWallBackgroundBottomRightDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 12.png";
        private const string DefaultHubFrontOutlineTopLeftDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 1.png";
        private const string DefaultHubFrontOutlineHorizontalWallDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 2.png";
        private const string DefaultHubFrontOutlineBottomRightDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 9.png";
        private const string DefaultHubFrontOutlineSideDesignSourcePath = "Assets/Design/Wall set-20260405T131612Z-3-001/Wall set/Stone Wall/Stone Wall 4.png";
        private const string DefaultCloseButtonDesignSourcePath = "Assets/Design/GeneratedSources/UI/Buttons/close-button.png";
        private const string DefaultHelpButtonDesignSourcePath = "Assets/Design/GeneratedSources/UI/Buttons/help-button.png";
        private const string DefaultSystemTextBoxDesignSourcePath = "Assets/Design/GeneratedSources/UI/MessageBoxes/system-text-box.png";
        private const string DefaultInteractionTextBoxDesignSourcePath = "Assets/Design/GeneratedSources/UI/MessageBoxes/interaction-text-box.png";
        private const string DefaultDarkOutlinePanelDesignSourcePath = "Assets/Design/GeneratedSources/UI/PanelVariants/dark-outline-panel.png";
        private const string DefaultDarkOutlinePanelAltDesignSourcePath = "Assets/Design/GeneratedSources/UI/PanelVariants/dark-outline-panel-alt.png";
        private const string DefaultDarkSolidPanelDesignSourcePath = "Assets/Design/GeneratedSources/UI/PanelVariants/dark-solid-panel.png";
        private const string DefaultDarkThinOutlinePanelDesignSourcePath = "Assets/Design/GeneratedSources/UI/PanelVariants/dark-thin-outline-panel.png";
        private const string DefaultLightOutlinePanelDesignSourcePath = "Assets/Design/GeneratedSources/UI/PanelVariants/light-outline-panel.png";
        private const string DefaultLightSolidPanelDesignSourcePath = "Assets/Design/GeneratedSources/UI/PanelVariants/light-solid-panel.png";

        [Header("출력 루트")]
        [SerializeField] private string resourcesGeneratedRoot = DefaultResourcesGeneratedRoot;
        [SerializeField] private string sceneRoot = DefaultSceneRoot;

        [Header("UI 원본 루트")]
        [SerializeField] private string uiGeneratedSourceRoot = DefaultUiGeneratedSourceRoot;

        [Header("허브 원본 경로")]
        [SerializeField] private string hubFloorTileDesignSourcePath = DefaultHubFloorTileDesignSourcePath;
        [SerializeField] private string hubBarDesignSourcePath = DefaultHubBarDesignSourcePath;
        [SerializeField] private string hubWallBackgroundHorizontalWallDesignSourcePath = DefaultHubWallBackgroundHorizontalWallDesignSourcePath;
        [SerializeField] private string hubWallBackgroundVerticalWallDesignSourcePath = DefaultHubWallBackgroundVerticalWallDesignSourcePath;
        [SerializeField] private string hubWallBackgroundFillDesignSourcePath = DefaultHubWallBackgroundFillDesignSourcePath;
        [SerializeField] private string hubWallBackgroundBottomLeftDesignSourcePath = DefaultHubWallBackgroundBottomLeftDesignSourcePath;
        [SerializeField] private string hubWallBackgroundBottomRightDesignSourcePath = DefaultHubWallBackgroundBottomRightDesignSourcePath;
        [SerializeField] private string hubFrontOutlineTopLeftDesignSourcePath = DefaultHubFrontOutlineTopLeftDesignSourcePath;
        [SerializeField] private string hubFrontOutlineHorizontalWallDesignSourcePath = DefaultHubFrontOutlineHorizontalWallDesignSourcePath;
        [SerializeField] private string hubFrontOutlineBottomRightDesignSourcePath = DefaultHubFrontOutlineBottomRightDesignSourcePath;
        [SerializeField] private string hubFrontOutlineSideDesignSourcePath = DefaultHubFrontOutlineSideDesignSourcePath;

        [Header("UI 원본 경로")]
        [SerializeField] private string closeButtonDesignSourcePath = DefaultCloseButtonDesignSourcePath;
        [SerializeField] private string helpButtonDesignSourcePath = DefaultHelpButtonDesignSourcePath;
        [SerializeField] private string systemTextBoxDesignSourcePath = DefaultSystemTextBoxDesignSourcePath;
        [SerializeField] private string interactionTextBoxDesignSourcePath = DefaultInteractionTextBoxDesignSourcePath;
        [SerializeField] private string darkOutlinePanelDesignSourcePath = DefaultDarkOutlinePanelDesignSourcePath;
        [SerializeField] private string darkOutlinePanelAltDesignSourcePath = DefaultDarkOutlinePanelAltDesignSourcePath;
        [SerializeField] private string darkSolidPanelDesignSourcePath = DefaultDarkSolidPanelDesignSourcePath;
        [SerializeField] private string darkThinOutlinePanelDesignSourcePath = DefaultDarkThinOutlinePanelDesignSourcePath;
        [SerializeField] private string lightOutlinePanelDesignSourcePath = DefaultLightOutlinePanelDesignSourcePath;
        [SerializeField] private string lightSolidPanelDesignSourcePath = DefaultLightSolidPanelDesignSourcePath;

        [Header("허브 카운터 설정")]
        [SerializeField] private RectInt hubBarRightLeftCapCropRect = new(0, 0, 12, 62);
        [SerializeField] private RectInt hubBarRightBodyCropRect = new(303, 0, 203, 62);
        [SerializeField] private Vector4 hubBarMainSpriteBorder = new(4f, 0f, 4f, 0f);
        [SerializeField] private Vector4 hubBarRightSpriteBorder = new(12f, 0f, 4f, 0f);

        [Header("허브 벽 배경 설정")]
        [SerializeField] private int hubWallBackgroundTextureWidth = 1524;
        [SerializeField] private int hubWallBackgroundTextureHeight = 140;
        [SerializeField] private int hubWallBackgroundBorderSize = 32;

        [Header("허브 전면 아웃라인 설정")]
        [SerializeField] private int hubFrontOutlineTextureWidth = 1920;
        [SerializeField] private int hubFrontOutlineTextureHeight = 1080;
        [SerializeField] private int hubFrontOutlineBorderWidth = 96;
        [SerializeField] private int hubFrontOutlineBorderHeight = 90;
        [SerializeField] private int hubFrontOutlineTopWallStartX;
        [SerializeField] private int hubFrontOutlineTopWallEndX = 1606;
        [SerializeField] private int hubFrontOutlineBottomWallStartX = 300;

        [Header("플레이어/월드 텍스트 기본값")]
        [SerializeField] private float playerSpritePixelsPerUnit = 1000f;
        [SerializeField] private float playerVisualScale = 0.76f;
        [SerializeField] private Vector3 defaultPlayerRootScale = new(1.5f, 1.5f, 1f);
        [SerializeField] private float worldTitleFontSize = 5.1f;
        [SerializeField] private float worldLabelFontSize = 3.3f;
        [SerializeField] private float worldLabelSmallFontSize = 3.0f;

        private static PrototypeGeneratedAssetSettings _cachedSettings;

        public string ResourcesGeneratedRoot => resourcesGeneratedRoot;
        public string SceneRoot => sceneRoot;
        public string UiGeneratedSourceRoot => uiGeneratedSourceRoot;
        public string HubFloorTileDesignSourcePath => hubFloorTileDesignSourcePath;
        public string HubBarDesignSourcePath => hubBarDesignSourcePath;
        public string HubWallBackgroundHorizontalWallDesignSourcePath => hubWallBackgroundHorizontalWallDesignSourcePath;
        public string HubWallBackgroundVerticalWallDesignSourcePath => hubWallBackgroundVerticalWallDesignSourcePath;
        public string HubWallBackgroundFillDesignSourcePath => hubWallBackgroundFillDesignSourcePath;
        public string HubWallBackgroundBottomLeftDesignSourcePath => hubWallBackgroundBottomLeftDesignSourcePath;
        public string HubWallBackgroundBottomRightDesignSourcePath => hubWallBackgroundBottomRightDesignSourcePath;
        public string HubFrontOutlineTopLeftDesignSourcePath => hubFrontOutlineTopLeftDesignSourcePath;
        public string HubFrontOutlineHorizontalWallDesignSourcePath => hubFrontOutlineHorizontalWallDesignSourcePath;
        public string HubFrontOutlineBottomRightDesignSourcePath => hubFrontOutlineBottomRightDesignSourcePath;
        public string HubFrontOutlineSideDesignSourcePath => hubFrontOutlineSideDesignSourcePath;
        public string CloseButtonDesignSourcePath => closeButtonDesignSourcePath;
        public string HelpButtonDesignSourcePath => helpButtonDesignSourcePath;
        public string SystemTextBoxDesignSourcePath => systemTextBoxDesignSourcePath;
        public string InteractionTextBoxDesignSourcePath => interactionTextBoxDesignSourcePath;
        public string DarkOutlinePanelDesignSourcePath => darkOutlinePanelDesignSourcePath;
        public string DarkOutlinePanelAltDesignSourcePath => darkOutlinePanelAltDesignSourcePath;
        public string DarkSolidPanelDesignSourcePath => darkSolidPanelDesignSourcePath;
        public string DarkThinOutlinePanelDesignSourcePath => darkThinOutlinePanelDesignSourcePath;
        public string LightOutlinePanelDesignSourcePath => lightOutlinePanelDesignSourcePath;
        public string LightSolidPanelDesignSourcePath => lightSolidPanelDesignSourcePath;
        public RectInt HubBarRightLeftCapCropRect => hubBarRightLeftCapCropRect;
        public RectInt HubBarRightBodyCropRect => hubBarRightBodyCropRect;
        public Vector4 HubBarMainSpriteBorder => hubBarMainSpriteBorder;
        public Vector4 HubBarRightSpriteBorder => hubBarRightSpriteBorder;
        public int HubWallBackgroundTextureWidth => hubWallBackgroundTextureWidth;
        public int HubWallBackgroundTextureHeight => hubWallBackgroundTextureHeight;
        public int HubWallBackgroundBorderSize => hubWallBackgroundBorderSize;
        public int HubFrontOutlineTextureWidth => hubFrontOutlineTextureWidth;
        public int HubFrontOutlineTextureHeight => hubFrontOutlineTextureHeight;
        public int HubFrontOutlineBorderWidth => hubFrontOutlineBorderWidth;
        public int HubFrontOutlineBorderHeight => hubFrontOutlineBorderHeight;
        public int HubFrontOutlineTopWallStartX => hubFrontOutlineTopWallStartX;
        public int HubFrontOutlineTopWallEndX => hubFrontOutlineTopWallEndX;
        public int HubFrontOutlineBottomWallStartX => hubFrontOutlineBottomWallStartX;
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
        public string GatherSpriteRoot => CombineAssetPath(SpriteRoot, "Gather");
        public string UiSpriteRoot => CombineAssetPath(SpriteRoot, "UI");
        public string UiButtonSpriteRoot => CombineAssetPath(UiSpriteRoot, "Buttons");
        public string UiMessageBoxSpriteRoot => CombineAssetPath(UiSpriteRoot, "MessageBoxes");
        public string UiPanelSpriteRoot => CombineAssetPath(UiSpriteRoot, "Panels");
        public string WorldSpriteRoot => CombineAssetPath(SpriteRoot, "World");
        public string RecipeSpriteRoot => CombineAssetPath(SpriteRoot, "Recipes");
        public string HubSpriteRoot => CombineAssetPath(SpriteRoot, "Hub");
        public string FontRoot => CombineAssetPath(ResourcesGeneratedRoot, "Fonts");

        public string SharedExplorationHudSourceScene => CombineAssetPath(SceneRoot, "Hub.unity");
        public string BeachScenePath => CombineAssetPath(SceneRoot, "Beach.unity");
        public string DeepForestScenePath => CombineAssetPath(SceneRoot, "DeepForest.unity");
        public string AbandonedMineScenePath => CombineAssetPath(SceneRoot, "AbandonedMine.unity");
        public string WindHillScenePath => CombineAssetPath(SceneRoot, "WindHill.unity");

        public string[] SharedExplorationHudTargetScenes =>
            new[] { BeachScenePath, DeepForestScenePath, AbandonedMineScenePath, WindHillScenePath };

        public string[] ManagedScenePaths =>
            new[] { SharedExplorationHudSourceScene, BeachScenePath, DeepForestScenePath, AbandonedMineScenePath, WindHillScenePath };

        public string HubFloorBackgroundSpritePath => CombineAssetPath(HubSpriteRoot, "hub-floor-background.png");
        public string HubFloorTileSpritePath => CombineAssetPath(HubSpriteRoot, "hub-floor-tile.png");
        public string HubBarSpritePath => CombineAssetPath(HubSpriteRoot, "hub-bar.png");
        public string HubBarRightSpritePath => CombineAssetPath(HubSpriteRoot, "hub-bar-right.png");
        public string HubWallBackgroundSpritePath => CombineAssetPath(HubSpriteRoot, "hub-wall-background.png");
        public string HubFrontOutlineSpritePath => CombineAssetPath(HubSpriteRoot, "hub-front-outline.png");
        public string HubTableUnlockedSpritePath => CombineAssetPath(HubSpriteRoot, "hub-table-unlocked.png");
        public string HubUpgradeSlotSpritePath => CombineAssetPath(HubSpriteRoot, "hub-upgrade-slot.png");
        public string HubTodayMenuBgSpritePath => CombineAssetPath(HubSpriteRoot, "hub-today-menu-bg.png");
        public string HubTodayMenuItem1SpritePath => CombineAssetPath(HubSpriteRoot, "hub-today-menu-item-1.png");
        public string HubTodayMenuItem2SpritePath => CombineAssetPath(HubSpriteRoot, "hub-today-menu-item-2.png");
        public string HubTodayMenuItem3SpritePath => CombineAssetPath(HubSpriteRoot, "hub-today-menu-item-3.png");

        public string PlayerFrontSpritePath => CombineAssetPath(PlayerSpriteRoot, "player-front.png");
        public string PlayerBackSpritePath => CombineAssetPath(PlayerSpriteRoot, "player-back.png");
        public string PlayerSideSpritePath => CombineAssetPath(PlayerSpriteRoot, "player-side.png");

        public string PlayerFrontSpriteResourcePath => ToResourcesLoadPath(PlayerFrontSpritePath);
        public string PlayerBackSpriteResourcePath => ToResourcesLoadPath(PlayerBackSpritePath);
        public string PlayerSideSpriteResourcePath => ToResourcesLoadPath(PlayerSideSpritePath);
        public string FloorSpriteResourcePath => ToResourcesLoadPath(CombineAssetPath(WorldSpriteRoot, "world-floor.png"));
        public string HubFloorTileResourcePath => ToResourcesLoadPath(HubFloorTileSpritePath);
        public string HubFloorBackgroundResourcePath => ToResourcesLoadPath(HubFloorBackgroundSpritePath);
        public string HubWallBackgroundResourcePath => ToResourcesLoadPath(HubWallBackgroundSpritePath);
        public string HubFrontOutlineResourcePath => ToResourcesLoadPath(HubFrontOutlineSpritePath);
        public string HubBarResourcePath => ToResourcesLoadPath(HubBarSpritePath);
        public string HubBarRightResourcePath => ToResourcesLoadPath(HubBarRightSpritePath);
        public string HubTableUnlockedResourcePath => ToResourcesLoadPath(HubTableUnlockedSpritePath);
        public string HubUpgradeSlotResourcePath => ToResourcesLoadPath(HubUpgradeSlotSpritePath);
        public string HubTodayMenuBgResourcePath => ToResourcesLoadPath(HubTodayMenuBgSpritePath);
        public string HubTodayMenuItem1ResourcePath => ToResourcesLoadPath(HubTodayMenuItem1SpritePath);
        public string HubTodayMenuItem2ResourcePath => ToResourcesLoadPath(HubTodayMenuItem2SpritePath);
        public string HubTodayMenuItem3ResourcePath => ToResourcesLoadPath(HubTodayMenuItem3SpritePath);
        public string RecipeSpriteResourceRoot => ToResourcesLoadPath(RecipeSpriteRoot);
        public string GeneratedUiResourceRoot => ToResourcesLoadPath(UiSpriteRoot);
        public string GeneratedUiButtonResourceRoot => ToResourcesLoadPath(UiButtonSpriteRoot);
        public string GeneratedUiMessageBoxResourceRoot => ToResourcesLoadPath(UiMessageBoxSpriteRoot);
        public string GeneratedUiPanelResourceRoot => ToResourcesLoadPath(UiPanelSpriteRoot);

        public static PrototypeGeneratedAssetSettings GetCurrent()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return LoadOrCreateEditorSettings();
            }
#endif

            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            _cachedSettings = Resources.Load<PrototypeGeneratedAssetSettings>(ResourcesLoadPath);
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            _cachedSettings = CreateInstance<PrototypeGeneratedAssetSettings>();
            _cachedSettings.hideFlags = HideFlags.HideAndDontSave;
            return _cachedSettings;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EnsureSettingsAssetExistsOnEditorLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            LoadOrCreateEditorSettings();
        }

        private static PrototypeGeneratedAssetSettings LoadOrCreateEditorSettings()
        {
            PrototypeGeneratedAssetSettings settings = AssetDatabase.LoadAssetAtPath<PrototypeGeneratedAssetSettings>(AssetPath);
            if (settings != null)
            {
                _cachedSettings = settings;
                return settings;
            }

            EnsureAssetFoldersExist();
            settings = CreateInstance<PrototypeGeneratedAssetSettings>();
            settings.name = DefaultAssetFileName;
            settings.NormalizeSerializedPaths();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            _cachedSettings = settings;
            return settings;
        }

        private static void EnsureAssetFoldersExist()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Generated");
        }

        private static void EnsureFolder(string parentPath, string childName)
        {
            string fullPath = parentPath + "/" + childName;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, childName);
            }
        }
#endif

        private void OnValidate()
        {
            NormalizeSerializedPaths();
        }

        private void NormalizeSerializedPaths()
        {
            resourcesGeneratedRoot = NormalizeAssetPath(resourcesGeneratedRoot, DefaultResourcesGeneratedRoot);
            if (!resourcesGeneratedRoot.StartsWith("Assets/Resources", StringComparison.Ordinal))
            {
                resourcesGeneratedRoot = DefaultResourcesGeneratedRoot;
            }

            sceneRoot = NormalizeAssetPath(sceneRoot, DefaultSceneRoot);
            uiGeneratedSourceRoot = NormalizeAssetPath(uiGeneratedSourceRoot, DefaultUiGeneratedSourceRoot);

            hubFloorTileDesignSourcePath = NormalizeAssetPath(hubFloorTileDesignSourcePath, DefaultHubFloorTileDesignSourcePath);
            hubBarDesignSourcePath = NormalizeAssetPath(hubBarDesignSourcePath, DefaultHubBarDesignSourcePath);
            hubWallBackgroundHorizontalWallDesignSourcePath = NormalizeAssetPath(hubWallBackgroundHorizontalWallDesignSourcePath, DefaultHubWallBackgroundHorizontalWallDesignSourcePath);
            hubWallBackgroundVerticalWallDesignSourcePath = NormalizeAssetPath(hubWallBackgroundVerticalWallDesignSourcePath, DefaultHubWallBackgroundVerticalWallDesignSourcePath);
            hubWallBackgroundFillDesignSourcePath = NormalizeAssetPath(hubWallBackgroundFillDesignSourcePath, DefaultHubWallBackgroundFillDesignSourcePath);
            hubWallBackgroundBottomLeftDesignSourcePath = NormalizeAssetPath(hubWallBackgroundBottomLeftDesignSourcePath, DefaultHubWallBackgroundBottomLeftDesignSourcePath);
            hubWallBackgroundBottomRightDesignSourcePath = NormalizeAssetPath(hubWallBackgroundBottomRightDesignSourcePath, DefaultHubWallBackgroundBottomRightDesignSourcePath);
            hubFrontOutlineTopLeftDesignSourcePath = NormalizeAssetPath(hubFrontOutlineTopLeftDesignSourcePath, DefaultHubFrontOutlineTopLeftDesignSourcePath);
            hubFrontOutlineHorizontalWallDesignSourcePath = NormalizeAssetPath(hubFrontOutlineHorizontalWallDesignSourcePath, DefaultHubFrontOutlineHorizontalWallDesignSourcePath);
            hubFrontOutlineBottomRightDesignSourcePath = NormalizeAssetPath(hubFrontOutlineBottomRightDesignSourcePath, DefaultHubFrontOutlineBottomRightDesignSourcePath);
            hubFrontOutlineSideDesignSourcePath = NormalizeAssetPath(hubFrontOutlineSideDesignSourcePath, DefaultHubFrontOutlineSideDesignSourcePath);

            closeButtonDesignSourcePath = NormalizeAssetPath(closeButtonDesignSourcePath, DefaultCloseButtonDesignSourcePath);
            helpButtonDesignSourcePath = NormalizeAssetPath(helpButtonDesignSourcePath, DefaultHelpButtonDesignSourcePath);
            systemTextBoxDesignSourcePath = NormalizeAssetPath(systemTextBoxDesignSourcePath, DefaultSystemTextBoxDesignSourcePath);
            interactionTextBoxDesignSourcePath = NormalizeAssetPath(interactionTextBoxDesignSourcePath, DefaultInteractionTextBoxDesignSourcePath);
            darkOutlinePanelDesignSourcePath = NormalizeAssetPath(darkOutlinePanelDesignSourcePath, DefaultDarkOutlinePanelDesignSourcePath);
            darkOutlinePanelAltDesignSourcePath = NormalizeAssetPath(darkOutlinePanelAltDesignSourcePath, DefaultDarkOutlinePanelAltDesignSourcePath);
            darkSolidPanelDesignSourcePath = NormalizeAssetPath(darkSolidPanelDesignSourcePath, DefaultDarkSolidPanelDesignSourcePath);
            darkThinOutlinePanelDesignSourcePath = NormalizeAssetPath(darkThinOutlinePanelDesignSourcePath, DefaultDarkThinOutlinePanelDesignSourcePath);
            lightOutlinePanelDesignSourcePath = NormalizeAssetPath(lightOutlinePanelDesignSourcePath, DefaultLightOutlinePanelDesignSourcePath);
            lightSolidPanelDesignSourcePath = NormalizeAssetPath(lightSolidPanelDesignSourcePath, DefaultLightSolidPanelDesignSourcePath);
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
