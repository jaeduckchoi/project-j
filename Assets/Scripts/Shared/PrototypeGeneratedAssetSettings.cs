using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Shared 네임스페이스
namespace Shared
{
    /// <summary>
    /// 생성 자산 경로와 선택적 외부 원본, 플레이어/월드 텍스트 기본값을 한 곳에서 관리합니다.
    /// 외부 원본 경로가 비어 있으면 빌더는 현재 generated 출력물을 그대로 유지합니다.
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
        private const string DefaultUiGeneratedSourceRoot = "";
        private const string DefaultHubFloorTileDesignSourcePath = "";
        private const string DefaultHubBarDesignSourcePath = "";
        private const string DefaultFrontCounterDesignSourcePath = "";
        private const string DefaultBackCounterDesignSourcePath = "";
        private const string DefaultMosaicTileFloorDesignSourcePath = "";
        private const string DefaultMosaicTileWallDesignSourcePath = "";
        private const string DefaultTableChair2DesignSourcePath = "";
        private const string DefaultAccountBoardDesignSourcePath = "";
        private const string DefaultHubWallBackgroundDesignSourcePath = "";
        private const string DefaultHubFrontOutlineDesignSourcePath = "";
        private const string DefaultPlayerIdleFrame1DesignSourcePath = "Design/drive-download-20260409T040302Z-3-001/Character/JONGGU.png";
        private const string DefaultPlayerIdleFrame2DesignSourcePath = "Design/drive-download-20260409T040302Z-3-001/Character/JONGGU2.png";
        private const string DefaultCloseButtonDesignSourcePath = "";
        private const string DefaultHelpButtonDesignSourcePath = "";
        private const string DefaultSystemTextBoxDesignSourcePath = "";
        private const string DefaultInteractionTextBoxDesignSourcePath = "";
        private const string DefaultDarkOutlinePanelDesignSourcePath = "";
        private const string DefaultDarkOutlinePanelAltDesignSourcePath = "";
        private const string DefaultDarkSolidPanelDesignSourcePath = "";
        private const string DefaultDarkThinOutlinePanelDesignSourcePath = "";
        private const string DefaultLightOutlinePanelDesignSourcePath = "";
        private const string DefaultLightSolidPanelDesignSourcePath = "";

        [Header("출력 루트")]
        [SerializeField] private string resourcesGeneratedRoot = DefaultResourcesGeneratedRoot;
        [SerializeField] private string sceneRoot = DefaultSceneRoot;

        [Header("UI 외부 원본 루트(선택)")]
        [SerializeField] private string uiGeneratedSourceRoot = DefaultUiGeneratedSourceRoot;

        [Header("허브 외부 원본 경로(선택)")]
        [SerializeField] private string hubFloorTileDesignSourcePath = DefaultHubFloorTileDesignSourcePath;
        [SerializeField] private string hubBarDesignSourcePath = DefaultHubBarDesignSourcePath;
        [SerializeField] private string frontCounterDesignSourcePath = DefaultFrontCounterDesignSourcePath;
        [SerializeField] private string backCounterDesignSourcePath = DefaultBackCounterDesignSourcePath;
        [SerializeField] private string mosaicTileFloorDesignSourcePath = DefaultMosaicTileFloorDesignSourcePath;
        [SerializeField] private string mosaicTileWallDesignSourcePath = DefaultMosaicTileWallDesignSourcePath;
        [SerializeField] private string tableChair2DesignSourcePath = DefaultTableChair2DesignSourcePath;
        [SerializeField] private string accountBoardDesignSourcePath = DefaultAccountBoardDesignSourcePath;
        [SerializeField] private string hubWallBackgroundDesignSourcePath = DefaultHubWallBackgroundDesignSourcePath;
        [SerializeField] private string hubFrontOutlineDesignSourcePath = DefaultHubFrontOutlineDesignSourcePath;

        [Header("UI 외부 원본 경로(선택)")]
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

        [Header("허브 전면 아웃라인 설정")]

        [Header("플레이어 외부 원본 경로(선택)")]
        [SerializeField] private string playerIdleFrame1DesignSourcePath = DefaultPlayerIdleFrame1DesignSourcePath;
        [SerializeField] private string playerIdleFrame2DesignSourcePath = DefaultPlayerIdleFrame2DesignSourcePath;

        [Header("플레이어/월드 텍스트 기본값")]
        [SerializeField] private float playerSpritePixelsPerUnit = 80f;
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
        public string FrontCounterDesignSourcePath => !string.IsNullOrWhiteSpace(frontCounterDesignSourcePath) ? frontCounterDesignSourcePath : hubBarDesignSourcePath;
        public string BackCounterDesignSourcePath => backCounterDesignSourcePath;
        public string MosaicTileFloorDesignSourcePath => mosaicTileFloorDesignSourcePath;
        public string MosaicTileWallDesignSourcePath => mosaicTileWallDesignSourcePath;
        public string TableChair2DesignSourcePath => tableChair2DesignSourcePath;
        public string AccountBoardDesignSourcePath => accountBoardDesignSourcePath;
        public string HubWallBackgroundDesignSourcePath => hubWallBackgroundDesignSourcePath;
        public string HubFrontOutlineDesignSourcePath => hubFrontOutlineDesignSourcePath;
        public string PlayerIdleFrame1DesignSourcePath => playerIdleFrame1DesignSourcePath;
        public string PlayerIdleFrame2DesignSourcePath => playerIdleFrame2DesignSourcePath;
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

        public string[] CanonicalExplorationScenePaths =>
            new[] { BeachScenePath, DeepForestScenePath, AbandonedMineScenePath, WindHillScenePath };

        public string[] CanonicalManagedScenePaths =>
            new[] { SharedExplorationHudSourceScene, BeachScenePath, DeepForestScenePath, AbandonedMineScenePath, WindHillScenePath };

        public string[] SharedExplorationHudTargetScenes =>
#if UNITY_EDITOR
            FilterExistingScenePaths(CanonicalExplorationScenePaths);
#else
            CanonicalExplorationScenePaths;
#endif

        public string[] ManagedScenePaths =>
#if UNITY_EDITOR
            FilterExistingScenePaths(CanonicalManagedScenePaths);
#else
            CanonicalManagedScenePaths;
#endif

        public string HubFloorBackgroundSpritePath => CombineAssetPath(HubSpriteRoot, "hub_floor_background.png");
        public string HubFloorTileSpritePath => CombineAssetPath(HubSpriteRoot, "hub_floor_tile.png");
        public string FrontCounterSpritePath => CombineAssetPath(HubSpriteRoot, "front_counter.png");
        public string BackCounterSpritePath => CombineAssetPath(HubSpriteRoot, "back_counter.png");
        public string MosaicTileFloorSpritePath => CombineAssetPath(HubSpriteRoot, "mosaic_tile_floor.png");
        public string MosaicTileWallSpritePath => CombineAssetPath(HubSpriteRoot, "mosaic_tile_wall.png");
        public string TableChair2SpritePath => CombineAssetPath(HubSpriteRoot, "table_chair2.png");
        public string AccountBoardSpritePath => CombineAssetPath(HubSpriteRoot, "account_board.png");
        public string HubBarSpritePath => FrontCounterSpritePath;
        public string HubBarRightSpritePath => CombineAssetPath(HubSpriteRoot, "hub_bar_right.png");
        public string HubWallBackgroundSpritePath => CombineAssetPath(HubSpriteRoot, "hub_wall_background.png");
        public string HubFrontOutlineSpritePath => CombineAssetPath(HubSpriteRoot, "hub_front_outline.png");
        public string HubTableUnlockedSpritePath => TableChair2SpritePath;
        public string HubTodayMenuBgSpritePath => CombineAssetPath(HubSpriteRoot, "hub_today_menu_bg.png");
        public string HubTodayMenuItem1SpritePath => CombineAssetPath(HubSpriteRoot, "hub_today_menu_item_1.png");
        public string HubTodayMenuItem2SpritePath => CombineAssetPath(HubSpriteRoot, "hub_today_menu_item_2.png");
        public string HubTodayMenuItem3SpritePath => CombineAssetPath(HubSpriteRoot, "hub_today_menu_item_3.png");

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
        public string FloorSpriteResourcePath => ToResourcesLoadPath(CombineAssetPath(WorldSpriteRoot, "world-floor.png"));
        public string HubFloorTileResourcePath => ToResourcesLoadPath(HubFloorTileSpritePath);
        public string HubFloorBackgroundResourcePath => ToResourcesLoadPath(HubFloorBackgroundSpritePath);
        public string HubWallBackgroundResourcePath => ToResourcesLoadPath(HubWallBackgroundSpritePath);
        public string HubFrontOutlineResourcePath => ToResourcesLoadPath(HubFrontOutlineSpritePath);
        public string HubBarResourcePath => ToResourcesLoadPath(HubBarSpritePath);
        public string HubBarRightResourcePath => ToResourcesLoadPath(HubBarRightSpritePath);
        public string HubTableUnlockedResourcePath => ToResourcesLoadPath(HubTableUnlockedSpritePath);
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

        private static string[] FilterExistingScenePaths(string[] scenePaths)
        {
            if (scenePaths == null || scenePaths.Length == 0)
            {
                return Array.Empty<string>();
            }

            string[] existingScenePaths = new string[scenePaths.Length];
            int existingCount = 0;

            for (int index = 0; index < scenePaths.Length; index++)
            {
                string scenePath = scenePaths[index];
                if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath))
                {
                    continue;
                }

                existingScenePaths[existingCount++] = scenePath;
            }

            if (existingCount == existingScenePaths.Length)
            {
                return existingScenePaths;
            }

            Array.Resize(ref existingScenePaths, existingCount);
            return existingScenePaths;
        }

#if UNITY_EDITOR
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
            uiGeneratedSourceRoot = NormalizeOptionalAssetPath(uiGeneratedSourceRoot);

            hubFloorTileDesignSourcePath = NormalizeOptionalAssetPath(hubFloorTileDesignSourcePath);
            hubBarDesignSourcePath = NormalizeOptionalAssetPath(hubBarDesignSourcePath);
            frontCounterDesignSourcePath = NormalizeOptionalAssetPath(frontCounterDesignSourcePath);
            backCounterDesignSourcePath = NormalizeOptionalAssetPath(backCounterDesignSourcePath);
            mosaicTileFloorDesignSourcePath = NormalizeOptionalAssetPath(mosaicTileFloorDesignSourcePath);
            mosaicTileWallDesignSourcePath = NormalizeOptionalAssetPath(mosaicTileWallDesignSourcePath);
            tableChair2DesignSourcePath = NormalizeOptionalAssetPath(tableChair2DesignSourcePath);
            accountBoardDesignSourcePath = NormalizeOptionalAssetPath(accountBoardDesignSourcePath);
            hubWallBackgroundDesignSourcePath = NormalizeOptionalAssetPath(hubWallBackgroundDesignSourcePath);
            hubFrontOutlineDesignSourcePath = NormalizeOptionalAssetPath(hubFrontOutlineDesignSourcePath);
            playerIdleFrame1DesignSourcePath = NormalizeOptionalAssetPath(playerIdleFrame1DesignSourcePath);
            playerIdleFrame2DesignSourcePath = NormalizeOptionalAssetPath(playerIdleFrame2DesignSourcePath);

            closeButtonDesignSourcePath = NormalizeOptionalAssetPath(closeButtonDesignSourcePath);
            helpButtonDesignSourcePath = NormalizeOptionalAssetPath(helpButtonDesignSourcePath);
            systemTextBoxDesignSourcePath = NormalizeOptionalAssetPath(systemTextBoxDesignSourcePath);
            interactionTextBoxDesignSourcePath = NormalizeOptionalAssetPath(interactionTextBoxDesignSourcePath);
            darkOutlinePanelDesignSourcePath = NormalizeOptionalAssetPath(darkOutlinePanelDesignSourcePath);
            darkOutlinePanelAltDesignSourcePath = NormalizeOptionalAssetPath(darkOutlinePanelAltDesignSourcePath);
            darkSolidPanelDesignSourcePath = NormalizeOptionalAssetPath(darkSolidPanelDesignSourcePath);
            darkThinOutlinePanelDesignSourcePath = NormalizeOptionalAssetPath(darkThinOutlinePanelDesignSourcePath);
            lightOutlinePanelDesignSourcePath = NormalizeOptionalAssetPath(lightOutlinePanelDesignSourcePath);
            lightSolidPanelDesignSourcePath = NormalizeOptionalAssetPath(lightSolidPanelDesignSourcePath);
        }

        private static string NormalizeOptionalAssetPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return NormalizeAssetPath(value, string.Empty);
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
