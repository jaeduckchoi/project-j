#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CoreLoop.Core;
using Shared.Data;
using Management.Economy;
using CoreLoop.Flow;
using Exploration.Gathering;
using Exploration.Camera;
using Management.Inventory;
using Exploration.Interaction;
using Exploration.Player;
using Restaurant;
using Management.Storage;
using TMPro;
using Management.Tools;
using UI;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using Management.Upgrade;
using Exploration.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// ProjectEditor 네임스페이스
namespace Editor
{
    public static class JongguMinimalPrototypeBuilder
    {
        private const string PopupTitleObjectName = "PopupTitle";
        private const string PopupLeftCaptionObjectName = "PopupLeftCaption";
        private const string PopupRightCaptionObjectName = "PopupRightCaption";
        private const string GeneratedRoot = "Assets/Generated";
        private const string GameDataRoot = GeneratedRoot + "/GameData";
        private const string ResourceDataRoot = GameDataRoot + "/Resources";
        private const string RecipeDataRoot = GameDataRoot + "/Recipes";
        private const string InputDataRoot = GameDataRoot + "/Input";
        private const string SpriteRoot = GeneratedRoot + "/Sprites";
        private const string PlayerSpriteRoot = SpriteRoot + "/Player";
        private const string GatherSpriteRoot = SpriteRoot + "/Gather";
        private const string UiSpriteRoot = SpriteRoot + "/UI";
        private const string UiButtonSpriteRoot = UiSpriteRoot + "/Buttons";
        private const string UiMessageBoxSpriteRoot = UiSpriteRoot + "/MessageBoxes";
        private const string UiPanelSpriteRoot = UiSpriteRoot + "/Panels";
        private const string WorldSpriteRoot = SpriteRoot + "/World";
        private const string SceneRoot = "Assets/Scenes";
        private const string SharedExplorationHudSourceScene = SceneRoot + "/Hub.unity";
        private const string FontRoot = GeneratedRoot + "/Fonts";
        private const string ResourceSpriteRoot = "Assets/Resources/Generated/Sprites";
        private const string ResourceUiSpriteRoot = ResourceSpriteRoot + "/UI";
        private const string ResourceUiButtonSpriteRoot = ResourceUiSpriteRoot + "/Buttons";
        private const string ResourceUiMessageBoxSpriteRoot = ResourceUiSpriteRoot + "/MessageBoxes";
        private const string ResourceUiPanelSpriteRoot = ResourceUiSpriteRoot + "/Panels";
        private const string UiGeneratedSourceRoot = "Assets/Design/GeneratedSources/UI";
        private const string UiGeneratedSourceButtonRoot = UiGeneratedSourceRoot + "/Buttons";
        private const string UiGeneratedSourceMessageBoxRoot = UiGeneratedSourceRoot + "/MessageBoxes";
        private const string UiGeneratedSourcePanelRoot = UiGeneratedSourceRoot + "/PanelVariants";
        private const string HubSpriteRoot = SpriteRoot + "/Hub";
        private const string ResourceHubSpriteRoot = ResourceSpriteRoot + "/Hub";
        private const string HubFloorBackgroundSpritePath = HubSpriteRoot + "/hub-floor-background.png";
        private const string ResourceHubFloorBackgroundSpritePath = ResourceHubSpriteRoot + "/hub-floor-background.png";
        private const string HubWallBackgroundSpritePath = HubSpriteRoot + "/hub-wall-background.png";
        private const string ResourceHubWallBackgroundSpritePath = ResourceHubSpriteRoot + "/hub-wall-background.png";
        private const string HubFrontOutlineSpritePath = HubSpriteRoot + "/hub-front-outline.png";
        private const string ResourceHubFrontOutlineSpritePath = ResourceHubSpriteRoot + "/hub-front-outline.png";
        private const string HubBarSpritePath = HubSpriteRoot + "/hub-bar.png";
        private const string ResourceHubBarSpritePath = ResourceHubSpriteRoot + "/hub-bar.png";
        private const string HubTableUnlockedSpritePath = HubSpriteRoot + "/hub-table-unlocked.png";
        private const string ResourceHubTableUnlockedSpritePath = ResourceHubSpriteRoot + "/hub-table-unlocked.png";
        private const string HubUpgradeSlotSpritePath = HubSpriteRoot + "/hub-upgrade-slot-center.png";
        private const string ResourceHubUpgradeSlotSpritePath = ResourceHubSpriteRoot + "/hub-upgrade-slot-center.png";
        private const string HubTodayMenuBgSpritePath = HubSpriteRoot + "/hub-today-menu-bg-1.png";
        private const string ResourceHubTodayMenuBgSpritePath = ResourceHubSpriteRoot + "/hub-today-menu-bg-1.png";
        private const string HubTodayMenuItem1SpritePath = HubSpriteRoot + "/hub-today-menu-item-1.png";
        private const string ResourceHubTodayMenuItem1SpritePath = ResourceHubSpriteRoot + "/hub-today-menu-item-1.png";
        private const string HubTodayMenuItem2SpritePath = HubSpriteRoot + "/hub-today-menu-item-2.png";
        private const string ResourceHubTodayMenuItem2SpritePath = ResourceHubSpriteRoot + "/hub-today-menu-item-2.png";
        private const string HubTodayMenuItem3SpritePath = HubSpriteRoot + "/hub-today-menu-item-3.png";
        private const string ResourceHubTodayMenuItem3SpritePath = ResourceHubSpriteRoot + "/hub-today-menu-item-3.png";
        private const string CloseButtonDesignSourcePath = UiGeneratedSourceButtonRoot + "/close-button.png";
        private const string HelpButtonDesignSourcePath = UiGeneratedSourceButtonRoot + "/help-button.png";
        private const string SystemTextBoxDesignSourcePath = UiGeneratedSourceMessageBoxRoot + "/system-text-box.png";
        private const string InteractionTextBoxDesignSourcePath = UiGeneratedSourceMessageBoxRoot + "/interaction-text-box.png";
        private const string DarkOutlinePanelDesignSourcePath = UiGeneratedSourcePanelRoot + "/dark-outline-panel.png";
        private const string DarkOutlinePanelAltDesignSourcePath = UiGeneratedSourcePanelRoot + "/dark-outline-panel-alt.png";
        private const string DarkSolidPanelDesignSourcePath = UiGeneratedSourcePanelRoot + "/dark-solid-panel.png";
        private const string DarkThinOutlinePanelDesignSourcePath = UiGeneratedSourcePanelRoot + "/dark-thin-outline-panel.png";
        private const string LightOutlinePanelDesignSourcePath = UiGeneratedSourcePanelRoot + "/light-outline-panel.png";
        private const string LightSolidPanelDesignSourcePath = UiGeneratedSourcePanelRoot + "/light-solid-panel.png";
        private const float PlayerSpritePixelsPerUnit = 1000f;
        private const float PlayerVisualScale = 0.76f;
        private const float WorldTitleFontSize = 5.1f;
        private const float WorldLabelFontSize = 3.3f;
        private const float WorldLabelSmallFontSize = 3.0f;

        private static TMP_FontAsset _generatedKoreanFont;
        private static TMP_FontAsset _generatedHeadingFont;
        private static readonly Dictionary<string, Material> CachedWorldTextMaterials = new(StringComparer.Ordinal);

        private static readonly string[] HubPopupSceneImageNames =
        {
            "PopupOverlay",
            "PopupFrame",
            "PopupFrameLeft",
            "PopupFrameRight",
            "PopupLeftBody",
            "PopupRightBody",
            "PopupCloseButton"
        };

        private static readonly string[] SharedExplorationHudTargetScenes =
        {
            SceneRoot + "/Beach.unity",
            SceneRoot + "/DeepForest.unity",
            SceneRoot + "/AbandonedMine.unity",
            SceneRoot + "/WindHill.unity"
        };

        private static readonly Dictionary<string, SceneImageSnapshot> CachedHubPopupSceneImages = new(StringComparer.Ordinal);

        private readonly struct SceneImageSnapshot
        {
            public SceneImageSnapshot(Sprite sprite, Image.Type type, Color color, bool preserveAspect)
            {
                Sprite = sprite;
                Type = type;
                Color = color;
                PreserveAspect = preserveAspect;
            }

            public Sprite Sprite { get; }
            public Image.Type Type { get; }
            public Color Color { get; }
            public bool PreserveAspect { get; }
        }

        private sealed class ResourceLibrary
        {
            public ResourceData Fish;
            public ResourceData Shell;
            public ResourceData Seaweed;
            public ResourceData Herb;
            public ResourceData Mushroom;
            public ResourceData GlowMoss;
            public ResourceData WindHerb;
        }

        private sealed class RecipeLibrary
        {
            public RecipeData SushiSet;
            public RecipeData SeafoodSoup;
            public RecipeData HerbFishSoup;
            public RecipeData ForestBasket;
            public RecipeData GlowMossStew;
            public RecipeData WindHerbSalad;
        }

        private sealed class SpriteLibrary
        {
            public Sprite PlayerFront;
            public Sprite PlayerBack;
            public Sprite PlayerSide;
            public Sprite HubFloorBackground;
            public Sprite HubWallBackground;
            public Sprite HubFrontOutline;
            public Sprite HubBar;
            public Sprite HubTableUnlocked;
            public Sprite HubUpgradeSlot;
            public Sprite HubTodayMenuBg;
            public Sprite HubTodayMenuItem1;
            public Sprite HubTodayMenuItem2;
            public Sprite HubTodayMenuItem3;
            public Sprite Portal;
            public Sprite Selector;
            public Sprite Counter;
            public Sprite Fish;
            public Sprite Shell;
            public Sprite Seaweed;
            public Sprite Herb;
            public Sprite Mushroom;
            public Sprite GlowMoss;
            public Sprite WindHerb;
            public Sprite Floor;
        }

        [MenuItem("Tools/Jonggu Restaurant/열린 씬 누락 스크립트 정리", true, 2110)]
        private static bool ValidateCleanMissingScriptsInOpenScene()
        {
            return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Tools/Jonggu Restaurant/열린 씬 누락 스크립트 정리", false, 2110)]
        public static void CleanMissingScriptsInOpenScene()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("누락 스크립트 정리는 플레이 모드를 종료한 뒤 실행하세요.");
                return;
            }

            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                Debug.LogWarning("정리할 열린 씬이 없습니다.");
                return;
            }

            int removedCount = 0;
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                removedCount += RemoveMissingScriptsRecursive(root);
            }

            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            Debug.Log($"열린 씬에서 누락 스크립트 컴포넌트 {removedCount}개를 정리했습니다.");
        }

        [MenuItem("Tools/Jonggu Restaurant/프로토타입 빌드 및 감사", true, 2100)]
        [MenuItem("Tools/Jonggu Restaurant/생성 자산 및 씬 다시 만들기", true, 2200)]
        [MenuItem("Tools/Jonggu Restaurant/생성 씬 감사만 실행", true, 2210)]
        private static bool ValidatePrototypeBuildToolMenu()
        {
            return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Tools/Jonggu Restaurant/프로토타입 빌드 및 감사", false, 2100)]
        public static void BuildMinimalPrototype()
        {
            if (!EnsurePrototypeBuildToolReady("프로토타입 빌드 및 감사"))
            {
                return;
            }

            ExecutePrototypeBuild(runAudit: true);
            EditorUtility.DisplayDialog(
                "종구의 식당",
                "최소 프로토타입 씬 생성과 생성 씬 감사를 완료했습니다. Assets/Scenes/Hub.unity를 열고 실행하세요.",
                "OK");
        }

        [MenuItem("Tools/Jonggu Restaurant/생성 자산 및 씬 다시 만들기", false, 2200)]
        public static void RebuildGeneratedAssetsAndScenes()
        {
            if (!EnsurePrototypeBuildToolReady("생성 자산 및 씬 다시 만들기"))
            {
                return;
            }

            ExecutePrototypeBuild(runAudit: false);
            EditorUtility.DisplayDialog(
                "종구의 식당",
                "생성 자산과 기본 씬을 다시 만들었습니다. 필요하면 생성 씬 감사를 이어서 실행하세요.",
                "OK");
        }

        [MenuItem("Tools/Jonggu Restaurant/생성 씬 감사만 실행", false, 2210)]
        public static void AuditGeneratedScenesOnly()
        {
            if (!EnsurePrototypeBuildToolReady("생성 씬 감사만 실행"))
            {
                return;
            }

            RunGeneratedSceneAudit();
            EditorUtility.DisplayDialog("종구의 식당", "생성 씬 감사를 완료했습니다.", "OK");
        }

        /// <summary>
        /// 메인 빌드와 유지보수 메뉴가 공통으로 쓰는 실행 가능 조건을 한곳에서 맞춥니다.
        /// </summary>
        private static bool EnsurePrototypeBuildToolReady(string menuName)
        {
            if (!ValidatePrototypeBuildToolMenu())
            {
                Debug.LogWarning($"{menuName}는 플레이 모드를 종료한 뒤 실행하세요.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 기본 메뉴는 유지하되, 내부 단계는 역할별 메서드로 나눠 빌드 흐름을 읽기 쉽게 정리합니다.
        /// </summary>
        private static void ExecutePrototypeBuild(bool runAudit)
        {
            CachedWorldTextMaterials.Clear();
            PrepareGeneratedFolders();
            PrepareGeneratedAssets(out SpriteLibrary sprites, out ResourceLibrary resources, out RecipeLibrary recipes);
            SyncBuildCanvasOverrides();
            SaveAndRefreshAssets();
            BuildAllPrototypeScenes(resources, recipes, sprites);
            SaveAndRefreshAssets();

            if (runAudit)
            {
                RunGeneratedSceneAudit();
            }
        }

        /// <summary>
        /// 생성 자산과 빌드 산출물이 들어갈 기본 폴더를 먼저 맞춥니다.
        /// </summary>
        private static void PrepareGeneratedFolders()
        {
            ProjectStructureUtility.EnsureBaseProjectFolders();
            EnsureFolder("Assets", "Generated");
            EnsureFolder(GeneratedRoot, "GameData");
            EnsureFolder(GameDataRoot, "Input");
            EnsureFolder(GameDataRoot, "Resources");
            EnsureFolder(GameDataRoot, "Recipes");
            EnsureFolder(GeneratedRoot, "Sprites");
            EnsureFolder(SpriteRoot, "UI");
            EnsureFolder(UiSpriteRoot, "Buttons");
            EnsureFolder(UiSpriteRoot, "MessageBoxes");
            EnsureFolder(UiSpriteRoot, "Panels");
            EnsureFolder(GeneratedRoot, "Fonts");
            EnsureFolder("Assets/Resources", "Generated");
            EnsureFolder("Assets/Resources/Generated", "Sprites");
            EnsureFolder(ResourceSpriteRoot, "UI");
            EnsureFolder(ResourceUiSpriteRoot, "Buttons");
            EnsureFolder(ResourceUiSpriteRoot, "MessageBoxes");
            EnsureFolder(ResourceUiSpriteRoot, "Panels");
            EnsureFolder("Assets", "Scenes");
        }

        /// <summary>
        /// 씬 생성 전에 폰트, 스프라이트, 데이터처럼 공통으로 쓰는 generated 자산을 준비합니다.
        /// </summary>
        private static void PrepareGeneratedAssets(out SpriteLibrary sprites, out ResourceLibrary resources, out RecipeLibrary recipes)
        {
            _generatedHeadingFont = CreateHeadingFontAsset();
            _generatedKoreanFont = CreateKoreanFontAsset();
            EnsurePreferredTmpFontAsset();
            CreateUiDesignSprites();
            sprites = CreateSprites();
            resources = CreateResources();
            recipes = CreateRecipes(resources);
        }

        /// <summary>
        /// Hub 씬 기준 UI 값과 현재 열려 있는 씬의 마지막 조정값을 같은 순서로 오버라이드 자산에 반영합니다.
        /// </summary>
        private static void SyncBuildCanvasOverrides()
        {
            SaveSceneIfLoadedAndDirty(SharedExplorationHudSourceScene);
            if (!TrySyncCanvasOverridesFromScenePath(SharedExplorationHudSourceScene, out string hubSyncMessage))
            {
                Debug.LogWarning(hubSyncMessage);
            }

            if (!TrySyncCanvasOverridesFromActiveScene(out string activeSceneSyncMessage))
            {
                Debug.LogWarning(activeSceneSyncMessage);
            }
        }

        /// <summary>
        /// 빌더가 관리하는 모든 기본 씬과 Build Settings 목록을 한 번에 다시 맞춥니다.
        /// </summary>
        private static void BuildAllPrototypeScenes(ResourceLibrary resources, RecipeLibrary recipes, SpriteLibrary sprites)
        {
            BuildHubScene(resources, recipes, sprites);
            BuildBeachScene(resources, sprites);
            BuildDeepForestScene(resources, sprites);
            BuildAbandonedMineScene(resources, sprites);
            BuildWindHillScene(resources, sprites);
            UpdateBuildSettings();
        }

        /// <summary>
        /// 생성 씬 감사 호출을 한곳으로 모아 유지보수 메뉴와 메인 빌드가 같은 경로를 쓰게 합니다.
        /// </summary>
        private static void RunGeneratedSceneAudit()
        {
            PrototypeSceneAudit.AuditGeneratedScenes();
        }

        private static void SaveAndRefreshAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 빌더 미리보기는 현재 메뉴로 노출하지 않고 내부 보강 경로에서만 사용한다.
        /// </summary>
        public static void ApplyOpenSceneBuilderPreview()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("열린 씬 빌더 미리보기는 플레이 모드를 종료한 뒤 실행하세요.");
                return;
            }

            CachedWorldTextMaterials.Clear();

            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                Debug.LogWarning("미리보기를 적용할 열린 씬이 없습니다.");
                return;
            }

            if (!IsSupportedBuilderPreviewScene(activeScene.name))
            {
                Debug.LogWarning($"'{activeScene.name}' 씬은 빌더 미리보기를 지원하지 않습니다.");
                return;
            }

            EnsureGeneratedAssetsForBuilderPreview();
            PrototypeSceneRuntimeAugmenter.EnsureSceneReady(activeScene);
            PrototypeSceneHierarchyOrganizer.OrganizeSceneHierarchy(activeScene, saveScene: false);
            ApplySceneUiPreviewIfAvailable();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();

            Debug.Log($"열린 씬 '{activeScene.name}'에 빌더 미리보기를 적용했습니다.");
        }

        private static bool IsSupportedBuilderPreviewScene(string sceneName)
        {
            return sceneName is "Hub" or "Beach" or "DeepForest" or "AbandonedMine" or "WindHill";
        }

        /// <summary>
        /// 열린 씬 미리보기에서도 빌더와 같은 generated 자산 경로를 먼저 맞춰 둔다.
        /// 메뉴판 스프라이트나 TMP 자산이 아직 없으면 여기서 바로 생성해 SceneView 프리뷰가 비지 않게 한다.
        /// </summary>
        private static void EnsureGeneratedAssetsForBuilderPreview()
        {
            CachedWorldTextMaterials.Clear();
            PrepareGeneratedFolders();
            PrepareGeneratedAssets(out _, out _, out _);
            SaveAndRefreshAssets();
        }

        /// <summary>
        /// 월드 빌더 프리뷰를 씬에 덮어쓴 뒤 UI 프리뷰도 함께 다시 적용하면
        /// 팝업과 허브 월드 요소를 SceneView에서 한 번에 맞춰 볼 수 있다.
        /// </summary>
        private static void ApplySceneUiPreviewIfAvailable()
        {
            PrototypeUIDesignController controller = UnityEngine.Object.FindFirstObjectByType<PrototypeUIDesignController>();
            if (controller == null || !controller.ShowEditorPreview)
            {
                return;
            }

            controller.ApplyEditorPreviewInEditor();
        }

        private static IEnumerable<string> EnumerateAutoSyncCanvasScenePaths()
        {
            yield return SharedExplorationHudSourceScene;

            foreach (string scenePath in SharedExplorationHudTargetScenes)
            {
                if (!string.IsNullOrWhiteSpace(scenePath))
                {
                    yield return scenePath;
                }
            }
        }

        /// <summary>
        /// 한 지원 씬에서 저장한 Canvas 변경을 다른 지원 씬 파일에도 바로 반영합니다.
        /// 공용 오버라이드 자산을 먼저 갱신한 뒤 대상 씬 Canvas에 빠진 관리 대상 UI를 복제하고,
        /// hierarchy/레이아웃 오버라이드를 다시 적용해 같은 구조로 맞춥니다.
        /// </summary>
        private static void SyncCanvasAcrossSupportedScenes(string sourceScenePath)
        {
            if (string.IsNullOrWhiteSpace(sourceScenePath))
            {
                return;
            }

            SaveSceneIfLoadedAndDirty(sourceScenePath);

            HashSet<string> visitedScenePaths = new(StringComparer.OrdinalIgnoreCase);
            foreach (string targetScenePath in EnumerateAutoSyncCanvasScenePaths())
            {
                if (string.IsNullOrWhiteSpace(targetScenePath)
                    || !visitedScenePaths.Add(targetScenePath)
                    || string.Equals(sourceScenePath, targetScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SyncCanvasBetweenScenes(sourceScenePath, targetScenePath);
            }
        }

        internal static bool ShouldAutoSyncCanvasOnSceneSave(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path))
            {
                return false;
            }

            return IsAutoSyncCanvasScene(scene.path);
        }

        internal static bool TryAutoSyncCanvasOnSceneSaved(UnityEngine.SceneManagement.Scene scene, out string message)
        {
            if (!ShouldAutoSyncCanvasOnSceneSave(scene))
            {
                message = "자동 동기화 대상 Canvas 씬이 아닙니다.";
                return false;
            }

            if (string.Equals(scene.path, SharedExplorationHudSourceScene, StringComparison.OrdinalIgnoreCase))
            {
                if (!PrototypeUISceneLayoutCatalog.TrySyncCanvasLayoutsFromScene(scene, out message))
                {
                    return false;
                }

                SyncCanvasAcrossSupportedScenes(scene.path);
                message = $"{scene.name} Canvas 변경을 공용 UI 오버라이드와 다른 지원 씬 Canvas에 자동 반영했습니다.";
                return true;
            }

            if (!PrototypeUISceneLayoutCatalog.TryOverlayCanvasLayoutsFromScene(scene, out message))
            {
                return false;
            }

            SyncCanvasAcrossSupportedScenes(scene.path);
            message = $"{scene.name} Canvas 변경을 공용 UI 오버라이드와 다른 지원 씬 Canvas에 자동 반영했습니다.";
            return true;
        }

        private static bool IsAutoSyncCanvasScene(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            if (string.Equals(scenePath, SharedExplorationHudSourceScene, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Array.Exists(
                SharedExplorationHudTargetScenes,
                targetScenePath => string.Equals(scenePath, targetScenePath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 기준 씬 Canvas를 공용 오버라이드 자산으로 먼저 동기화해,
        /// 같은 이름 UI 요소가 다른 씬에도 같은 표시 값으로 다시 생성되도록 맞춥니다.
        /// </summary>
        private static bool TrySyncCanvasOverridesFromScenePath(string scenePath, out string message)
        {
            if (!File.Exists(scenePath))
            {
                message = $"씬 파일을 찾지 못했습니다: {scenePath}";
                return false;
            }

            UnityEngine.SceneManagement.Scene sourceScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool openedTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                message = $"씬을 열지 못했습니다: {scenePath}";
                return false;
            }

            try
            {
                if (!PrototypeUISceneLayoutCatalog.TrySyncCanvasLayoutsFromScene(sourceScene, out message))
                {
                    return false;
                }

                message = $"{sourceScene.name} Canvas 기준을 공용 오버라이드 자산에 저장했습니다.";
                return true;
            }
            finally
            {
                if (openedTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        /// <summary>
        /// 현재 열려 있는 씬의 Canvas 값을 빌드 직전에 다시 저장해,
        /// Hub 기준 Canvas 오버라이드 위에 현재 씬 값을 마지막으로 덮어씁니다.
        /// </summary>
        private static bool TrySyncCanvasOverridesFromActiveScene(out string message)
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded || string.IsNullOrWhiteSpace(activeScene.path))
            {
                message = "열려 있는 저장 대상 씬이 없어 현재 씬 Canvas 값을 덮어쓸 수 없습니다.";
                return false;
            }

            string normalizedSceneRoot = Path.GetFullPath(SceneRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedActiveDirectory = Path.GetDirectoryName(Path.GetFullPath(activeScene.path))?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(normalizedActiveDirectory)
                || !string.Equals(normalizedActiveDirectory, normalizedSceneRoot, StringComparison.OrdinalIgnoreCase))
            {
                message = $"현재 씬 '{activeScene.name}'은(는) Assets/Scenes 아래에 없어 Canvas 값을 덮어쓸 수 없습니다.";
                return false;
            }

            SaveSceneIfLoadedAndDirty(activeScene.path);
            if (!PrototypeUISceneLayoutCatalog.TryOverlayCanvasLayoutsFromScene(activeScene, out message))
            {
                return false;
            }

            message = $"현재 씬 '{activeScene.name}' Canvas 값을 공용 오버라이드 자산에 덮어썼습니다.";
            return true;
        }

        private static void BuildHubScene(ResourceLibrary resources, RecipeLibrary recipes, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/Hub.unity");
            CacheHubPopupSceneImages(SceneRoot + "/Hub.unity");
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                const float mapWidth = HubRoomLayout.ScreenWidth;
                const float mapHeight = HubRoomLayout.ScreenHeight;

                GameObject gameManagerObject = CreateGameManager("Hub", "Beach", resources);
                GameObject player = CreatePlayer(HubRoomLayout.PlayerStartPosition, sprites);
                BoxCollider2D cameraBounds = CreateCamera(
                    player.transform,
                    mapWidth,
                    mapHeight,
                    new Color(0.88f, 0.80f, 0.70f),
                    HubRoomLayout.ScreenOrthographicSize);
                cameraBounds.transform.position = HubRoomLayout.CameraPosition;
                cameraBounds.size = HubRoomLayout.CameraSize;

                BoxCollider2D movementBounds = CreateMovementBounds(
                    "HubMovementBounds",
                    HubRoomLayout.MovementBoundsSize.x,
                    HubRoomLayout.MovementBoundsSize.y);
                movementBounds.transform.position = HubRoomLayout.MovementBoundsPosition;
                AttachPlayerBoundsLimiter(player, movementBounds);

                CreateHubLayerRoots(out Transform hubBackgroundLayer, out Transform hubObjectLayer, out Transform hubForegroundLayer, out Transform hubTableGroup);
                BuildHubArtLayout(sprites, mapWidth, mapHeight, hubBackgroundLayer, hubObjectLayer, hubForegroundLayer);
                BuildHubTableLayout(sprites, hubTableGroup);
                BuildHubUpgradeSlotLayout(sprites, hubObjectLayer);
                BuildHubCollisionLayout();

                CreateSpawnPoint("HubEntry", HubRoomLayout.HubEntryPosition, "HubEntry");
                CreatePortal("GoToBeach", HubRoomLayout.GoToBeachPosition, sprites.Portal, "Beach", "BeachEntry", "바닷가로 이동", "바닷가로", true, ToolType.None, 0, "", HubRoomLayout.PortalScale);
                CreatePortal("GoToDeepForest", HubRoomLayout.GoToDeepForestPosition, sprites.Portal, "DeepForest", "ForestEntry", "깊은 숲으로 이동", "깊은 숲", true, ToolType.None, 0, "", HubRoomLayout.PortalScale);
                CreatePortal(
                    "GoToAbandonedMine",
                    HubRoomLayout.GoToAbandonedMinePosition,
                    sprites.Portal,
                    "AbandonedMine",
                    "MineEntry",
                    "폐광산으로 이동",
                    "폐광산",
                    true,
                    ToolType.Lantern,
                    0,
                    "작업대에서 랜턴을 준비해야 폐광산 안쪽을 안전하게 탐험할 수 있습니다.",
                    HubRoomLayout.PortalScale);
                CreatePortal("GoToWindHill", HubRoomLayout.GoToWindHillPosition, sprites.Portal, "WindHill", "WindHillEntry", "바람 언덕으로 이동", "바람 언덕", true, ToolType.None, 0, "", HubRoomLayout.PortalScale);
                CreateFeaturePad("BeachPortalPad", HubRoomLayout.BeachPortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.98f, 0.83f, 0.51f));
                CreateFeaturePad("ForestPortalPad", HubRoomLayout.ForestPortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.70f, 0.86f, 0.44f));
                CreateFeaturePad("MinePortalPad", HubRoomLayout.MinePortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.74f, 0.74f, 0.78f));
                CreateFeaturePad("WindPortalPad", HubRoomLayout.WindPortalPadPosition, HubRoomLayout.PortalPadScale, sprites.Floor, new Color(0.82f, 0.92f, 0.98f));

                RestaurantManager restaurantManager = CreateRestaurantManager(recipes);
                CreateRecipeSelector(HubRoomLayout.RecipeSelectorPosition, sprites.Selector, restaurantManager);
                CreateServiceCounter(HubRoomLayout.ServiceCounterPosition, sprites.Counter, restaurantManager);
                CreateHubTodayMenuBoard(HubRoomLayout.TodayMenuBoardPosition, sprites, restaurantManager, hubObjectLayer);
                StorageManager storageManager = gameManagerObject.GetComponent<StorageManager>();
                UpgradeManager upgradeManager = gameManagerObject.GetComponent<UpgradeManager>();
                CreateStorageStation("StorageStation", HubRoomLayout.StorageStationPosition, HubRoomLayout.StorageStationScale, sprites.Floor, new Color(0.86f, 0.70f, 0.36f), "창고", storageManager, StorageStationAction.StoreAll);
                CreateUpgradeStation(HubRoomLayout.UpgradeStationPosition, HubRoomLayout.UpgradeStationScale, sprites.Floor, new Color(0.54f, 0.72f, 0.78f), upgradeManager);

                HideHubInteractionPresentations();

                CreateUiCanvas(true);

                EnsureUiEventSystem();
                SaveGeneratedScene(SceneRoot + "/Hub.unity");
            }
            finally
            {
                CachedHubPopupSceneImages.Clear();
            }
        }

        private static void BuildBeachScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/Beach.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 30f;
            const float mapHeight = 18f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-8.25f, -2.25f, 0f), sprites);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.67f, 0.86f, 0.96f), 6.8f);
            BoxCollider2D movementBounds = CreateMovementBounds("BeachMovementBounds", mapWidth - 2.2f, mapHeight - 2.2f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("SandBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.93f, 0.85f, 0.64f), -20);
            CreateFloorZone("OceanBand", new Vector3(0f, 4.5f, 0f), new Vector3(mapWidth, 7f, 1f), sprites.Floor, new Color(0.50f, 0.76f, 0.92f), -19);
            CreateFloorZone("ShoreLine", new Vector3(0f, 1.5f, 0f), new Vector3(mapWidth, 1f, 1f), sprites.Floor, new Color(0.98f, 0.95f, 0.84f), -18);
            CreateFloorZone("Dock", new Vector3(-11f, -3f, 0f), new Vector3(4.5f, 3.4f, 1f), sprites.Floor, new Color(0.60f, 0.43f, 0.24f), -17);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.32f, 0.44f, 0.28f));
            CreateDecorBlock("RockClusterA", new Vector3(8f, -4.2f, 0f), new Vector3(3f, 1.6f, 1f), sprites.Floor, new Color(0.45f, 0.47f, 0.50f), 1);
            CreateDecorBlock("RockClusterB", new Vector3(10f, 1.5f, 0f), new Vector3(2.2f, 1.4f, 1f), sprites.Floor, new Color(0.45f, 0.47f, 0.50f), 1);
            CreateDecorBlock("GrassPatch", new Vector3(7f, 4.8f, 0f), new Vector3(4f, 2.2f, 1f), sprites.Floor, new Color(0.42f, 0.68f, 0.35f), 1);
            CreateDecorBlock("BoatMark", new Vector3(-13.1f, -2.0f, 0f), new Vector3(1.8f, 2.8f, 1f), sprites.Floor, new Color(0.87f, 0.38f, 0.21f), 2);

            CreateWorldLabel("BeachTitle", null, new Vector3(0f, 7.1f, 0f), "바닷가", Color.black, WorldTitleFontSize, 40);
            CreateSpawnPoint("BeachEntry", new Vector3(-8.25f, -2.25f, 0f), "BeachEntry");
            CreatePortal("ReturnToHub", new Vector3(-10.7f, -3.35f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGatherable("FishSpot01", new Vector3(-2f, 2.2f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
            CreateGatherable("FishSpot02", new Vector3(2.8f, 4f, 0f), sprites.Fish, resources.Fish, ToolType.FishingRod, 1, 2, "생선");
            CreateGatherable("ShellSpot01", new Vector3(-1f, -3f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
            CreateGatherable("ShellSpot02", new Vector3(4.5f, -1.8f, 0f), sprites.Shell, resources.Shell, ToolType.Rake, 1, 1, "조개");
            CreateGatherable("SeaweedSpot01", new Vector3(7f, 3.8f, 0f), sprites.Seaweed, resources.Seaweed, ToolType.Sickle, 1, 2, "해초");

            CreateUiCanvas(false);
            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/Beach.unity");
        }

        /// <summary>
        /// 허브 씬에 직접 지정한 팝업 Image 설정을 먼저 읽어 두면,
        /// 빌더가 씬을 다시 저장해도 수동으로 맞춘 이미지 소스를 유지할 수 있습니다.
        /// </summary>
        private static void CacheHubPopupSceneImages(string scenePath)
        {
            CachedHubPopupSceneImages.Clear();
            if (!File.Exists(scenePath))
            {
                return;
            }

            UnityEngine.SceneManagement.Scene sourceScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool openedTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                return;
            }

            try
            {
                foreach (string objectName in HubPopupSceneImageNames)
                {
                    Transform target = FindNamedTransformInScene(sourceScene, objectName);
                    if (target == null || !target.TryGetComponent(out Image image))
                    {
                        continue;
                    }

                    CachedHubPopupSceneImages[objectName] = new SceneImageSnapshot(
                        image.sprite,
                        image.type,
                        image.color,
                        image.preserveAspect);
                }
            }
            finally
            {
                if (openedTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        /// <summary>
        /// 열려 있는 대상 씬에 아직 저장되지 않은 변경이 있으면 먼저 저장해,
        /// 빌더가 방금 수정한 Canvas 기준을 그대로 다시 사용할 수 있게 맞춥니다.
        /// </summary>
        private static void SaveSceneIfLoadedAndDirty(string scenePath)
        {
            UnityEngine.SceneManagement.Scene loadedScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded || !loadedScene.isDirty)
            {
                return;
            }

            EditorSceneManager.SaveScene(loadedScene);
        }

        /// <summary>
        /// 저장한 기준 씬 Canvas를 다른 지원 씬 Canvas에 직접 반영합니다.
        /// 같은 이름 관리 대상 UI가 빠져 있으면 복제하고, hierarchy/레이아웃 오버라이드를 다시 적용합니다.
        /// </summary>
        private static void SyncCanvasBetweenScenes(string sourceScenePath, string targetScenePath)
        {
            if (!File.Exists(sourceScenePath) || !File.Exists(targetScenePath))
            {
                return;
            }

            if (string.Equals(sourceScenePath, targetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            UnityEngine.SceneManagement.Scene sourceScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sourceScenePath);
            UnityEngine.SceneManagement.Scene targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(targetScenePath);
            bool openedSourceTemporarily = false;
            bool openedTargetTemporarily = false;

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Additive);
                openedSourceTemporarily = sourceScene.IsValid() && sourceScene.isLoaded;
            }

            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Additive);
                openedTargetTemporarily = targetScene.IsValid() && targetScene.isLoaded;
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded || !targetScene.IsValid() || !targetScene.isLoaded)
            {
                return;
            }

            try
            {
                GameObject sourceCanvas = FindSceneCanvasRoot(sourceScene);
                GameObject targetCanvas = FindSceneCanvasRoot(targetScene);
                if (sourceCanvas == null || targetCanvas == null)
                {
                    return;
                }

                bool isHubTargetScene = string.Equals(targetScenePath, SharedExplorationHudSourceScene, StringComparison.OrdinalIgnoreCase);
                SyncMissingManagedCanvasObjects(sourceCanvas, targetCanvas, isHubTargetScene);
                ApplySceneOverridesToHierarchy(targetCanvas.transform);
                RebindCanvasUiManagerReferences(targetCanvas);

                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }
            finally
            {
                if (openedTargetTemporarily)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                if (openedSourceTemporarily)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }
        }

        private static GameObject FindSceneCanvasRoot(UnityEngine.SceneManagement.Scene scene)
        {
            return scene
                .GetRootGameObjects()
                .FirstOrDefault(root => root != null
                                        && string.Equals(root.name, "Canvas", StringComparison.Ordinal)
                                        && root.GetComponent<Canvas>() != null);
        }

        /// <summary>
        /// 기준 씬 Canvas에 있는 관리 대상 UI가 대상 씬에 없으면 복제합니다.
        /// 전체 Canvas를 통째로 갈아엎지 않고, 빠진 오브젝트만 채운 뒤 오버라이드로 최종 정렬합니다.
        /// </summary>
        private static void SyncMissingManagedCanvasObjects(
            GameObject sourceCanvas,
            GameObject targetCanvas,
            bool isHubTargetScene)
        {
            if (sourceCanvas == null || targetCanvas == null)
            {
                return;
            }

            Dictionary<string, Transform> sourceMap = new(StringComparer.Ordinal);
            Dictionary<string, Transform> targetMap = new(StringComparer.Ordinal);
            CollectNamedHierarchyTransforms(sourceCanvas.transform, sourceMap);
            CollectNamedHierarchyTransforms(targetCanvas.transform, targetMap);

            HashSet<string> allowedNames = PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(isHubTargetScene);
            HashSet<string> allManagedNames = PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(true);
            List<Transform> missingTransforms = new();
            foreach (string objectName in allowedNames)
            {
                if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName)
                    || !sourceMap.TryGetValue(objectName, out Transform sourceTransform)
                    || sourceTransform == null
                    || targetMap.ContainsKey(objectName))
                {
                    continue;
                }

                missingTransforms.Add(sourceTransform);
            }

            missingTransforms.Sort(CompareTransformDepth);
            for (int index = 0; index < missingTransforms.Count; index++)
            {
                Transform sourceTransform = missingTransforms[index];
                if (sourceTransform == null || targetMap.ContainsKey(sourceTransform.name))
                {
                    continue;
                }

                HashSet<string> existingNames = new(targetMap.Keys, StringComparer.Ordinal);
                GameObject clonedObject = UnityEngine.Object.Instantiate(sourceTransform.gameObject, targetCanvas.transform);
                ApplyHubPopupObjectIdentity(clonedObject);
                PruneDuplicateNamedCanvasObjects(clonedObject.transform, existingNames, includeCurrent: false);

                if (!isHubTargetScene)
                {
                    PruneUnsupportedManagedCanvasObjects(clonedObject.transform, allowedNames, allManagedNames, includeCurrent: false);
                }

                CollectNamedHierarchyTransforms(clonedObject.transform, targetMap);
            }
        }

        private static void PruneUnsupportedManagedCanvasObjects(
            Transform current,
            ISet<string> allowedNames,
            ISet<string> managedNames,
            bool includeCurrent)
        {
            if (current == null || allowedNames == null || managedNames == null)
            {
                return;
            }

            for (int index = current.childCount - 1; index >= 0; index--)
            {
                PruneUnsupportedManagedCanvasObjects(current.GetChild(index), allowedNames, managedNames, includeCurrent: true);
            }

            if (!includeCurrent
                || string.IsNullOrWhiteSpace(current.name)
                || !managedNames.Contains(current.name)
                || allowedNames.Contains(current.name))
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(current.gameObject);
        }

        private static void PruneDuplicateNamedCanvasObjects(
            Transform current,
            ISet<string> existingNames,
            bool includeCurrent)
        {
            if (current == null || existingNames == null)
            {
                return;
            }

            for (int index = current.childCount - 1; index >= 0; index--)
            {
                PruneDuplicateNamedCanvasObjects(current.GetChild(index), existingNames, includeCurrent: true);
            }

            if (!includeCurrent || string.IsNullOrWhiteSpace(current.name) || !existingNames.Contains(current.name))
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(current.gameObject);
        }

        /// <summary>
        /// Canvas 구조를 직접 동기화한 뒤 UIManager가 들고 있던 텍스트/버튼 참조를 새 오브젝트로 다시 맞춥니다.
        /// </summary>
        private static void RebindCanvasUiManagerReferences(GameObject canvasObject)
        {
            if (canvasObject == null || !canvasObject.TryGetComponent(out UIManager uiManager))
            {
                return;
            }

            SerializedObject so = new(uiManager);
            so.FindProperty("interactionPromptText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "InteractionPromptText");
            so.FindProperty("inventoryText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "InventoryText");
            so.FindProperty("storageText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "StorageText");
            so.FindProperty("upgradeText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "UpgradeText");
            so.FindProperty("goldText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "GoldText");
            so.FindProperty("selectedRecipeText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "SelectedRecipeText");
            so.FindProperty("dayPhaseText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "DayPhaseText");
            so.FindProperty("guideText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "GuideText");
            so.FindProperty("resultText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "RestaurantResultText");
            so.FindProperty("skipExplorationButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "SkipExplorationButton");
            so.FindProperty("skipServiceButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "SkipServiceButton");
            so.FindProperty("nextDayButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "NextDayButton");
            so.FindProperty("recipePanelButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "RecipePanelButton");
            so.FindProperty("upgradePanelButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "UpgradePanelButton");
            so.FindProperty("materialPanelButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "MaterialPanelButton");
            so.FindProperty("guideHelpButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "GuideHelpButton");
            so.FindProperty("popupCloseButton").objectReferenceValue = FindNamedComponent<Button>(canvasObject.transform, "PopupCloseButton");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T FindNamedComponent<T>(Transform root, string objectName)
            where T : Component
        {
            Transform target = FindChildRecursive(root, objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        /// <summary>
        /// 씬에서 저장한 Canvas 오버라이드를 복제된 계층 전체에 다시 적용합니다.
        /// HUDRoot를 다른 씬에서 복제한 뒤에도 같은 이름 기준 표시 값이 유지되도록 맞춥니다.
        /// </summary>
        private static void ApplySceneOverridesToHierarchy(Transform root)
        {
            if (root == null)
            {
                return;
            }

            RemoveDeletedCanvasObjects(root);

            Dictionary<string, Transform> transformMap = new(StringComparer.Ordinal);
            CollectNamedHierarchyTransforms(root, transformMap);

            List<Transform> existingTransforms = new(transformMap.Values);
            for (int index = 0; index < existingTransforms.Count; index++)
            {
                Transform current = existingTransforms[index];
                if (current == null || string.IsNullOrWhiteSpace(current.name))
                {
                    continue;
                }

                EnsureHierarchyTransform(root, current.name, transformMap, new HashSet<string>(StringComparer.Ordinal));
            }

            transformMap.Clear();
            CollectNamedHierarchyTransforms(root, transformMap);

            List<Transform> orderedTransforms = new(transformMap.Values);
            orderedTransforms.Sort((left, right) => CompareTransformDepth(left, right));
            for (int index = 0; index < orderedTransforms.Count; index++)
            {
                ApplySceneHierarchyOverride(root, orderedTransforms[index], transformMap);
            }

            ApplySceneOverridesRecursively(root);
        }

        private static void ApplySceneOverridesRecursively(Transform root)
        {
            if (root == null)
            {
                return;
            }

            ApplySceneOverridesToTransform(root);
            for (int index = 0; index < root.childCount; index++)
            {
                ApplySceneOverridesRecursively(root.GetChild(index));
            }
        }

        private static void CollectNamedHierarchyTransforms(Transform current, IDictionary<string, Transform> transformMap)
        {
            if (current == null || transformMap == null || string.IsNullOrWhiteSpace(current.name))
            {
                return;
            }

            transformMap[current.name] = current;
            for (int index = 0; index < current.childCount; index++)
            {
                CollectNamedHierarchyTransforms(current.GetChild(index), transformMap);
            }
        }

        private static Transform EnsureHierarchyTransform(
            Transform root,
            string objectName,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (root == null)
            {
                return null;
            }

            if (PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(objectName) || string.Equals(objectName, root.name, StringComparison.Ordinal))
            {
                return root;
            }

            if (transformMap.TryGetValue(objectName, out Transform existing))
            {
                return existing;
            }

            if (visiting == null || !visiting.Add(objectName))
            {
                return root;
            }

            Transform parent = ResolveHierarchyParent(root, objectName, transformMap, visiting);
            if (parent == null)
            {
                visiting.Remove(objectName);
                return null;
            }

            GameObject groupObject = new(objectName);
            ApplyHubPopupObjectIdentity(groupObject);
            groupObject.transform.SetParent(parent != null ? parent : root, false);

            RectTransform rect = groupObject.AddComponent<RectTransform>();
            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out _, out int siblingIndex))
            {
                rect.SetSiblingIndex(ClampSiblingIndex(rect.parent, siblingIndex));
            }

            transformMap[objectName] = rect;
            visiting.Remove(objectName);
            return rect;
        }

        private static Transform ResolveHierarchyParent(
            Transform root,
            string objectName,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (root == null)
            {
                return null;
            }

            if (!PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string parentName, out _)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return root;
            }

            if (string.Equals(parentName, root.name, StringComparison.Ordinal))
            {
                return root;
            }

            return EnsureHierarchyTransform(root, parentName, transformMap, visiting);
        }

        private static void ApplySceneHierarchyOverride(
            Transform root,
            Transform target,
            IDictionary<string, Transform> transformMap)
        {
            if (root == null
                || target == null
                || target == root
                || string.IsNullOrWhiteSpace(target.name)
                || !PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(target.name, out string parentName, out int siblingIndex)
                || string.IsNullOrWhiteSpace(parentName))
            {
                return;
            }

            Transform targetParent = string.Equals(parentName, root.name, StringComparison.Ordinal)
                ? root
                : EnsureHierarchyTransform(root, parentName, transformMap, new HashSet<string>(StringComparer.Ordinal));
            if (targetParent == null || targetParent == target)
            {
                return;
            }

            if (target.parent != targetParent)
            {
                target.SetParent(targetParent, false);
            }

            target.SetSiblingIndex(ClampSiblingIndex(target.parent, siblingIndex));
            transformMap[target.name] = target;
        }

        private static void RemoveDeletedCanvasObjects(Transform root)
        {
            if (root == null)
            {
                return;
            }

            List<GameObject> targets = new();
            CollectDeletedCanvasObjects(root, targets, includeCurrent: false);
            for (int index = 0; index < targets.Count; index++)
            {
                if (targets[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(targets[index]);
                }
            }
        }

        private static void CollectDeletedCanvasObjects(Transform current, ICollection<GameObject> targets, bool includeCurrent)
        {
            if (current == null || targets == null)
            {
                return;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CollectDeletedCanvasObjects(current.GetChild(index), targets, includeCurrent: true);
            }

            if (includeCurrent && PrototypeUISceneLayoutCatalog.IsObjectRemoved(current.name))
            {
                targets.Add(current.gameObject);
            }
        }

        private static int CompareTransformDepth(Transform left, Transform right)
        {
            return GetTransformDepth(left).CompareTo(GetTransformDepth(right));
        }

        private static int GetTransformDepth(Transform target)
        {
            int depth = 0;
            Transform current = target;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static int ClampSiblingIndex(Transform parent, int siblingIndex)
        {
            if (parent == null)
            {
                return 0;
            }

            return Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, parent.childCount - 1));
        }

        private static void ApplySceneOverridesToTransform(Transform target)
        {
            if (target == null || string.IsNullOrEmpty(target.name))
            {
                return;
            }

            if (target.TryGetComponent(out RectTransform rect))
            {
                PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                    target.name,
                    new PrototypeUIRect(
                        rect.anchorMin,
                        rect.anchorMax,
                        rect.pivot,
                        rect.anchoredPosition,
                        rect.sizeDelta));
                rect.anchorMin = resolvedLayout.AnchorMin;
                rect.anchorMax = resolvedLayout.AnchorMax;
                rect.pivot = resolvedLayout.Pivot;
                rect.anchoredPosition = resolvedLayout.AnchoredPosition;
                rect.sizeDelta = resolvedLayout.SizeDelta;
            }

            if (target.TryGetComponent(out Image image))
            {
                PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, target.name);
            }

            if (target.TryGetComponent(out TextMeshProUGUI text))
            {
                PrototypeUISceneLayoutCatalog.TryApplyTextOverride(text, target.name);
            }

            if (target.TryGetComponent(out Button button))
            {
                ApplySceneButtonOverride(button);
            }
        }

        private static void BuildDeepForestScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/DeepForest.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 32f;
            const float mapHeight = 20f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.3f, -6.1f, 0f), sprites);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.63f, 0.74f, 0.56f), 7.2f);

            BoxCollider2D movementBounds = CreateMovementBounds("ForestMovementBounds", mapWidth - 2.4f, mapHeight - 2.4f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("ForestBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.49f, 0.63f, 0.37f), -20);
            CreateFloorZone("ForestPath", new Vector3(-2f, -3.8f, 0f), new Vector3(23f, 3.4f, 1f), sprites.Floor, new Color(0.68f, 0.58f, 0.39f), -19);
            CreateFloorZone("ForestCanopy", new Vector3(3.5f, 4.8f, 0f), new Vector3(20f, 8f, 1f), sprites.Floor, new Color(0.36f, 0.54f, 0.24f), -18);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.23f, 0.33f, 0.18f));
            CreateDecorBlock("SwampPoolA", new Vector3(0f, 0.5f, 0f), new Vector3(4.8f, 2.6f, 1f), sprites.Floor, new Color(0.29f, 0.41f, 0.23f), 1);
            CreateDecorBlock("SwampPoolB", new Vector3(5.8f, 1.4f, 0f), new Vector3(3.6f, 2.2f, 1f), sprites.Floor, new Color(0.29f, 0.41f, 0.23f), 1);
            CreateDecorBlock("NarrowPathRock", new Vector3(8.5f, -2.4f, 0f), new Vector3(2.4f, 1.4f, 1f), sprites.Floor, new Color(0.35f, 0.38f, 0.34f), 2);

            CreateWorldLabel("ForestTitle", null, new Vector3(0f, 8.2f, 0f), "깊은 숲", Color.black, WorldTitleFontSize, 40);
            CreateSpawnPoint("ForestEntry", new Vector3(-10.3f, -6.1f, 0f), "ForestEntry");
            CreatePortal("ReturnFromForest", new Vector3(-13.6f, -6.15f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGuideTriggerZone("ForestGuide", new Vector3(-8.4f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "forest_intro", "숲은 갈림길과 늪지대 때문에 인벤토리보다 귀환 동선을 더 자주 확인해야 합니다.");
            CreateMovementModifierZone("ForestSwampZone", new Vector3(1.8f, 1f, 0f), new Vector2(9f, 4.2f), 0.55f, "늪지에서는 이동이 느려집니다. 좁은 길을 따라 움직이면 더 안전합니다.");

            CreateGatherable("HerbPatch01", new Vector3(-4f, -1.1f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
            CreateGatherable("HerbPatch02", new Vector3(4.8f, -3.6f, 0f), sprites.Herb, resources.Herb, ToolType.Sickle, 1, 2, "약초");
            CreateGatherable("MushroomPatch01", new Vector3(2.6f, 4.1f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");
            CreateGatherable("MushroomPatch02", new Vector3(8.5f, 5.2f, 0f), sprites.Mushroom, resources.Mushroom, ToolType.Sickle, 1, 2, "버섯");

            CreateUiCanvas(false);
            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/DeepForest.unity");
        }

        private static void BuildAbandonedMineScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/AbandonedMine.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 32f;
            const float mapHeight = 20f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.7f, -5.95f, 0f), sprites);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.18f, 0.19f, 0.22f), 7.2f);

            BoxCollider2D movementBounds = CreateMovementBounds("MineBounds", mapWidth - 2.4f, mapHeight - 2.4f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("MineBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.26f, 0.27f, 0.30f), -20);
            CreateFloorZone("MineTunnel", new Vector3(-2f, -4f, 0f), new Vector3(21f, 3.2f, 1f), sprites.Floor, new Color(0.39f, 0.34f, 0.28f), -19);
            CreateFloorZone("MineChamber", new Vector3(6.8f, 1.2f, 0f), new Vector3(14f, 10f, 1f), sprites.Floor, new Color(0.21f, 0.23f, 0.25f), -18);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.10f, 0.10f, 0.12f));
            CreateDecorBlock("MineRockA", new Vector3(1.2f, -0.4f, 0f), new Vector3(3.8f, 1.8f, 1f), sprites.Floor, new Color(0.34f, 0.34f, 0.36f), 2);
            CreateDecorBlock("MineRockB", new Vector3(8.8f, 5.4f, 0f), new Vector3(2.6f, 1.4f, 1f), sprites.Floor, new Color(0.34f, 0.34f, 0.36f), 2);
            CreateDecorBlock("MineRockC", new Vector3(10.8f, -2.5f, 0f), new Vector3(3.2f, 1.6f, 1f), sprites.Floor, new Color(0.28f, 0.29f, 0.32f), 2);

            CreateWorldLabel("MineTitle", null, new Vector3(0f, 8.1f, 0f), "폐광산", Color.white, WorldTitleFontSize, 40);
            CreateSpawnPoint("MineEntry", new Vector3(-10.7f, -6.0f, 0f), "MineEntry");
            CreatePortal("ReturnFromMine", new Vector3(-13.6f, -6.0f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");

            CreateGuideTriggerZone("MineGuide", new Vector3(-8.8f, -4.6f, 0f), new Vector2(3.4f, 2.2f), "mine_intro", "폐광산은 어둡고 동선이 좁습니다. 안쪽으로 들어가기 전 귀환 길을 먼저 확인하세요.");
            CreateDarknessZone("MineDarkness", new Vector3(4.8f, 0.6f, 0f), new Vector2(18f, 10.8f));

            CreateGatherable("GlowMoss01", new Vector3(4.4f, 3.2f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
            CreateGatherable("GlowMoss02", new Vector3(8.2f, 1.0f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");
            CreateGatherable("GlowMoss03", new Vector3(11.6f, 4.4f, 0f), sprites.GlowMoss, resources.GlowMoss, ToolType.Lantern, 1, 2, "발광 이끼");

            CreateUiCanvas(false);
            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/AbandonedMine.unity");
        }

        private static void BuildWindHillScene(ResourceLibrary resources, SpriteLibrary sprites)
        {
            SaveSceneIfLoadedAndDirty(SceneRoot + "/WindHill.unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float mapWidth = 30f;
            const float mapHeight = 18f;

            CreateGameManager("Hub", "Beach", resources);
            GameObject player = CreatePlayer(new Vector3(-10.7f, -5.3f, 0f), sprites);
            CreateCamera(player.transform, mapWidth, mapHeight, new Color(0.85f, 0.92f, 0.98f), 6.8f);

            BoxCollider2D movementBounds = CreateMovementBounds("WindHillBounds", mapWidth - 2.2f, mapHeight - 2.2f);
            AttachPlayerBoundsLimiter(player, movementBounds);

            CreateFloorZone("HillBase", Vector3.zero, new Vector3(mapWidth, mapHeight, 1f), sprites.Floor, new Color(0.79f, 0.86f, 0.66f), -20);
            CreateFloorZone("CliffBand", new Vector3(6.8f, 0f, 0f), new Vector3(12f, 14f, 1f), sprites.Floor, new Color(0.70f, 0.80f, 0.58f), -19);
            CreateFloorZone("WindLane", new Vector3(6.8f, 0.8f, 0f), new Vector3(10f, 6.4f, 1f), sprites.Floor, new Color(0.91f, 0.96f, 0.98f), -18);

            CreateBoundaryWalls(mapWidth, mapHeight, sprites.Floor, new Color(0.37f, 0.43f, 0.31f));
            CreateDecorBlock("CliffRockA", new Vector3(10.4f, -4.2f, 0f), new Vector3(3.2f, 1.5f, 1f), sprites.Floor, new Color(0.48f, 0.50f, 0.46f), 2);
            CreateDecorBlock("CliffRockB", new Vector3(4.8f, 4.8f, 0f), new Vector3(2.6f, 1.2f, 1f), sprites.Floor, new Color(0.48f, 0.50f, 0.46f), 2);

            CreateWorldLabel("WindHillTitle", null, new Vector3(0f, 7.1f, 0f), "바람 언덕", Color.black, WorldTitleFontSize, 40);
            CreateSpawnPoint("WindHillEntry", new Vector3(-10.7f, -5.35f, 0f), "WindHillEntry");
            CreateSpawnPoint("WindHillShortcutEntry", new Vector3(7.8f, 4.4f, 0f), "WindHillShortcutEntry");
            CreatePortal("ReturnFromWindHill", new Vector3(-13.6f, -5.15f, 0f), sprites.Portal, "Hub", "HubEntry", "식당으로 이동", "식당 복귀");
            CreatePortal(
                "WindHillShortcut",
                new Vector3(-6.8f, -2.8f, 0f),
                sprites.Portal,
                "WindHill",
                "WindHillShortcutEntry",
                "정상 지름길",
                "정상 지름길",
                true,
                ToolType.None,
                6,
                "평판 6을 모으면 바람 언덕의 지름길을 이용할 수 있습니다.");

            CreateGuideTriggerZone("WindGuide", new Vector3(-8.8f, -4.2f, 0f), new Vector2(3.4f, 2.2f), "wind_intro", "바람 언덕에서는 강풍이 꺼질 때 이동하는 편이 안전합니다.");
            CreateWindGustZone("WindLaneZone", new Vector3(6.8f, 0.8f, 0f), new Vector2(10f, 6.2f), Vector2.right, 2.8f, 2f, 1.5f);

            CreateGatherable("WindHerb01", new Vector3(2.4f, -2.2f, 0f), sprites.WindHerb, resources.WindHerb, ToolType.Sickle, 1, 2, "향초");
            CreateGatherable("WindHerb02", new Vector3(8.6f, 4.6f, 0f), sprites.WindHerb, resources.WindHerb, ToolType.Sickle, 1, 2, "향초");
            CreateGatherable("WindHerb03", new Vector3(10.8f, -0.2f, 0f), sprites.WindHerb, resources.WindHerb, ToolType.Sickle, 1, 2, "향초");

            CreateUiCanvas(false);

            EnsureUiEventSystem();
            SaveGeneratedScene(SceneRoot + "/WindHill.unity");
        }

        private static GameObject CreateGameManager(string hubSceneName, string explorationSceneName, ResourceLibrary resources)
        {
            GameObject go = new("GameManager");
            InventoryManager inventory = go.AddComponent<InventoryManager>();
            StorageManager storage = go.AddComponent<StorageManager>();
            EconomyManager economy = go.AddComponent<EconomyManager>();
            ToolManager toolManager = go.AddComponent<ToolManager>();
            DayCycleManager dayCycleManager = go.AddComponent<DayCycleManager>();
            UpgradeManager upgradeManager = go.AddComponent<UpgradeManager>();
            GameManager gameManager = go.AddComponent<GameManager>();

            SerializedObject gameManagerSo = new(gameManager);
            gameManagerSo.FindProperty("hubSceneName").stringValue = hubSceneName;
            gameManagerSo.FindProperty("firstExplorationSceneName").stringValue = explorationSceneName;
            gameManagerSo.FindProperty("inventoryManager").objectReferenceValue = inventory;
            gameManagerSo.FindProperty("storageManager").objectReferenceValue = storage;
            gameManagerSo.FindProperty("economyManager").objectReferenceValue = economy;
            gameManagerSo.FindProperty("toolManager").objectReferenceValue = toolManager;
            gameManagerSo.FindProperty("dayCycleManager").objectReferenceValue = dayCycleManager;
            gameManagerSo.FindProperty("upgradeManager").objectReferenceValue = upgradeManager;
            gameManagerSo.ApplyModifiedPropertiesWithoutUndo();

            if (resources != null)
            {
                SerializedObject upgradeSo = new(upgradeManager);
                SerializedProperty inventoryCostsProperty = upgradeSo.FindProperty("inventoryUpgradeCosts");
                inventoryCostsProperty.arraySize = 2;

                SerializedProperty firstCost = inventoryCostsProperty.GetArrayElementAtIndex(0);
                firstCost.FindPropertyRelative("goldCost").intValue = 30;
                firstCost.FindPropertyRelative("requiredResource").objectReferenceValue = resources.Shell;
                firstCost.FindPropertyRelative("requiredAmount").intValue = 3;
                firstCost.FindPropertyRelative("description").stringValue = "조개 상자를 묶어 12칸까지 넓힙니다.";

                SerializedProperty secondCost = inventoryCostsProperty.GetArrayElementAtIndex(1);
                secondCost.FindPropertyRelative("goldCost").intValue = 65;
                secondCost.FindPropertyRelative("requiredResource").objectReferenceValue = resources.Herb;
                secondCost.FindPropertyRelative("requiredAmount").intValue = 4;
                secondCost.FindPropertyRelative("description").stringValue = "정리 상자를 더 달아 16칸까지 확장합니다.";

                SerializedProperty toolCostsProperty = upgradeSo.FindProperty("toolUnlockCosts");
                toolCostsProperty.arraySize = 1;

                SerializedProperty lanternCost = toolCostsProperty.GetArrayElementAtIndex(0);
                lanternCost.FindPropertyRelative("toolType").enumValueIndex = (int)ToolType.Lantern;
                lanternCost.FindPropertyRelative("goldCost").intValue = 45;
                lanternCost.FindPropertyRelative("requiredResource").objectReferenceValue = resources.Mushroom;
                lanternCost.FindPropertyRelative("requiredAmount").intValue = 2;
                lanternCost.FindPropertyRelative("description").stringValue = "폐광산처럼 어두운 지역 진입에 필요합니다.";

                upgradeSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return go;
        }

        private static GameObject CreatePlayer(Vector3 position, SpriteLibrary sprites)
        {
            GameObject player = new("Jonggu");
            player.transform.position = position;

            Sprite frontSprite = sprites.PlayerFront != null ? sprites.PlayerFront : sprites.PlayerSide;
            GameObject shadow = CreateDecorBlock("Shadow", Vector3.zero, new Vector3(0.46f, 0.14f, 1f), sprites.Floor, new Color(0f, 0f, 0f, 0.20f), 9, player.transform);
            shadow.transform.localPosition = new Vector3(0f, -0.28f, 0f);

            // 물리는 루트에 유지하고, 맵 크기 보정은 비주얼 자식만 스케일해서 처리합니다.
            GameObject visualRoot = new("PlayerVisual");
            visualRoot.transform.SetParent(player.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localScale = Vector3.one * PlayerVisualScale;

            SpriteRenderer renderer = visualRoot.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visualRoot.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = frontSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 12;

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = player.AddComponent<Rigidbody2D>();
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = player.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                collider = player.AddComponent<CapsuleCollider2D>();
            }

            collider.size = new Vector2(0.9f, 1.05f);

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = player.AddComponent<PlayerController>();
            }

            PlayerDirectionalSprite directionalSprite = player.GetComponent<PlayerDirectionalSprite>();
            if (directionalSprite == null)
            {
                directionalSprite = player.AddComponent<PlayerDirectionalSprite>();
            }

            directionalSprite.Configure(renderer, sprites.PlayerFront, sprites.PlayerBack, sprites.PlayerSide);

            GameObject interactionRange = new("InteractionRange");
            interactionRange.transform.SetParent(player.transform, false);
            CircleCollider2D rangeCollider = interactionRange.AddComponent<CircleCollider2D>();
            rangeCollider.isTrigger = true;
            rangeCollider.radius = 1.35f;
            InteractionDetector detector = interactionRange.AddComponent<InteractionDetector>();

            SerializedObject controllerSo = new(controller);
            controllerSo.FindProperty("interactionDetector").objectReferenceValue = detector;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static BoxCollider2D CreateCamera(Transform target, float mapWidth, float mapHeight, Color backgroundColor, float orthographicSize)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.backgroundColor = backgroundColor;

            cameraObject.AddComponent<AudioListener>();

            GameObject boundsObject = new("CameraBounds");
            BoxCollider2D bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(mapWidth, mapHeight);

            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            SerializedObject followSo = new(follow);
            followSo.FindProperty("target").objectReferenceValue = target;
            followSo.FindProperty("mapBounds").objectReferenceValue = bounds;
            followSo.ApplyModifiedPropertiesWithoutUndo();
            return bounds;
        }

        private static void CreateBoundaryWalls(float mapWidth, float mapHeight, Sprite sprite, Color color)
        {
            const float thickness = 0.8f;

            CreateWall("TopWall", new Vector3(0f, mapHeight * 0.5f, 0f), new Vector3(mapWidth + thickness, thickness, 1f), sprite, color);
            CreateWall("BottomWall", new Vector3(0f, -mapHeight * 0.5f, 0f), new Vector3(mapWidth + thickness, thickness, 1f), sprite, color);
            CreateWall("LeftWall", new Vector3(-mapWidth * 0.5f, 0f, 0f), new Vector3(thickness, mapHeight + thickness, 1f), sprite, color);
            CreateWall("RightWall", new Vector3(mapWidth * 0.5f, 0f, 0f), new Vector3(thickness, mapHeight + thickness, 1f), sprite, color);
        }

        private static void CreateWall(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color)
        {
            GameObject wall = CreateDecorBlock(objectName, position, scale, sprite, color, 15);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private static void CreateInvisibleWall(string objectName, Vector3 position, Vector3 scale, Transform parent = null)
        {
            GameObject wall = new(objectName);
            if (parent != null)
            {
                wall.transform.SetParent(parent, false);
                wall.transform.localPosition = position;
            }
            else
            {
                wall.transform.position = position;
            }

            wall.transform.localScale = scale;

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private static void CreatePortal(
            string objectName,
            Vector3 position,
            Sprite sprite,
            string targetSceneName,
            string targetSpawnPointId,
            string promptLabel,
            string worldLabel = null,
            bool requireMorningExplore = true,
            ToolType requiredToolType = ToolType.None,
            int requiredReputation = 0,
            string lockedGuideText = "",
            Vector3? sizeOverride = null)
        {
            Vector3 portalSize = sizeOverride ?? new Vector3(1.6f, 2.2f, 1f);
            GameObject portal = CreateDecorBlock(objectName, position, portalSize, sprite, new Color(0.94f, 0.50f, 0.18f), 7);
            BoxCollider2D collider = portal.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;

            ScenePortal scenePortal = portal.AddComponent<ScenePortal>();
            SerializedObject so = new(scenePortal);
            so.FindProperty("targetSceneName").stringValue = targetSceneName;
            so.FindProperty("targetSpawnPointId").stringValue = targetSpawnPointId;
            so.FindProperty("promptLabel").stringValue = promptLabel;
            so.FindProperty("requireMorningExplore").boolValue = requireMorningExplore;
            so.FindProperty("requiredToolType").enumValueIndex = (int)requiredToolType;
            so.FindProperty("requiredReputation").intValue = requiredReputation;
            so.FindProperty("lockedGuideText").stringValue = lockedGuideText;
            so.ApplyModifiedPropertiesWithoutUndo();

            string displayLabel = string.IsNullOrWhiteSpace(worldLabel) ? promptLabel : worldLabel;
            CreateWorldLabel(objectName + "_Label", portal.transform, new Vector3(0f, 0.82f, 0f), displayLabel, Color.black, WorldLabelFontSize, 50);
        }

        private static RestaurantManager CreateRestaurantManager(RecipeLibrary recipes)
        {
            GameObject go = new("RestaurantManager");
            RestaurantManager manager = go.AddComponent<RestaurantManager>();

            SerializedObject so = new(manager);
            SerializedProperty recipesProperty = so.FindProperty("availableRecipes");
            recipesProperty.arraySize = 6;
            recipesProperty.GetArrayElementAtIndex(0).objectReferenceValue = recipes.SushiSet;
            recipesProperty.GetArrayElementAtIndex(1).objectReferenceValue = recipes.SeafoodSoup;
            recipesProperty.GetArrayElementAtIndex(2).objectReferenceValue = recipes.HerbFishSoup;
            recipesProperty.GetArrayElementAtIndex(3).objectReferenceValue = recipes.ForestBasket;
            recipesProperty.GetArrayElementAtIndex(4).objectReferenceValue = recipes.GlowMossStew;
            recipesProperty.GetArrayElementAtIndex(5).objectReferenceValue = recipes.WindHerbSalad;
            so.FindProperty("serviceCapacity").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            return manager;
        }

        private static void CreateRecipeSelector(Vector3 position, Sprite sprite, RestaurantManager restaurantManager)
        {
            GameObject go = CreateDecorBlock("RecipeSelector", position, new Vector3(1.55f, 1.55f, 1f), sprite, new Color(0.98f, 0.84f, 0.18f), 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;

            RecipeSelectorStation station = go.AddComponent<RecipeSelectorStation>();
            SerializedObject so = new(station);
            so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
            so.FindProperty("promptLabel").stringValue = "메뉴 변경";
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel("RecipeSelectorLabel", go.transform, new Vector3(0f, 0.80f, 0f), "메뉴판", Color.black, WorldLabelFontSize, 50);
        }

        private static void CreateServiceCounter(Vector3 position, Sprite sprite, RestaurantManager restaurantManager)
        {
            GameObject go = CreateDecorBlock("ServiceCounter", position, new Vector3(1.95f, 1.55f, 1f), sprite, new Color(0.82f, 0.30f, 0.22f), 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;

            ServiceCounterStation station = go.AddComponent<ServiceCounterStation>();
            SerializedObject so = new(station);
            so.FindProperty("restaurantManager").objectReferenceValue = restaurantManager;
            so.FindProperty("promptLabel").stringValue = "영업 시작";
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel("ServiceCounterLabel", go.transform, new Vector3(0f, 0.80f, 0f), "영업대", Color.black, WorldLabelFontSize, 50);
        }

        /// <summary>
        /// 허브 아트는 레이어 루트와 테이블 그룹 루트를 먼저 만든 뒤 배치한다.
        /// </summary>
        private static void CreateHubLayerRoots(out Transform backgroundLayer, out Transform objectLayer, out Transform foregroundLayer, out Transform tableGroup)
        {
            GameObject hubArtRoot = new("HubArtRoot");
            GameObject backgroundObject = new("HubBackgroundLayer");
            backgroundObject.transform.SetParent(hubArtRoot.transform, false);
            GameObject objectObject = new("HubObjectLayer");
            objectObject.transform.SetParent(hubArtRoot.transform, false);
            GameObject foregroundObject = new("HubForegroundLayer");
            foregroundObject.transform.SetParent(hubArtRoot.transform, false);
            GameObject tableObject = new(HubRoomLayout.TableRootObjectName);
            tableObject.transform.SetParent(objectObject.transform, false);
            tableObject.transform.localPosition = HubRoomLayout.TableGroupPosition;

            backgroundLayer = backgroundObject.transform;
            objectLayer = objectObject.transform;
            foregroundLayer = foregroundObject.transform;
            tableGroup = tableObject.transform;
        }

        /// <summary>
        /// 허브 고정 아트와 가격 텍스트, 바닥 표지판, 오늘의 메뉴 보드를 한 번에 만든다.
        /// </summary>
        private static void BuildHubArtLayout(
            SpriteLibrary sprites,
            float mapWidth,
            float mapHeight,
            Transform backgroundLayer,
            Transform objectLayer,
            Transform foregroundLayer)
        {
            foreach (HubRoomLayout.HubArtPlacement placement in HubRoomLayout.ArtPlacements)
            {
                Transform parent = ResolveHubArtParent(placement.Anchor, backgroundLayer, objectLayer, foregroundLayer);
                Sprite sprite = ResolveHubArtSprite(sprites, placement.SpriteId);

                if (placement.SpriteId == HubRoomLayout.HubArtSpriteId.FloorBackground && sprite == null)
                {
                    GameObject fallbackFloor = CreateFloorZone(
                        placement.ObjectName,
                        HubRoomLayout.BackgroundPosition,
                        new Vector3(mapWidth, mapHeight, 1f),
                        sprites.Floor,
                        new Color(0.95f, 0.91f, 0.82f),
                        placement.SortingOrder);
                    fallbackFloor.transform.SetParent(parent, false);
                    continue;
                }

                CreateHubArtSprite(placement.ObjectName, placement.LocalPosition, sprite, placement.SortingOrder, parent);
            }
            foreach (HubRoomLayout.HubFloorSignPlacement placement in HubRoomLayout.FloorSignPlacements)
            {
                CreateHubFloorSign(placement, sprites.Floor, objectLayer);
            }
        }

        /// <summary>
        /// 테이블은 개별 그룹 아래에 스프라이트와 콜라이더를 함께 두어,
        /// 이후 위치 커스터마이즈가 와도 그룹 이동만으로 정렬을 유지한다.
        /// </summary>
        private static void BuildHubTableLayout(SpriteLibrary sprites, Transform tableGroup)
        {
            foreach (HubRoomLayout.HubTablePlacement placement in HubRoomLayout.TablePlacements)
            {
                GameObject groupObject = new(placement.GroupObjectName);
                groupObject.transform.SetParent(tableGroup, false);
                groupObject.transform.localPosition = placement.LocalPosition;

                GameObject tableObject = CreateHubArtSprite(placement.TableObjectName, Vector3.zero, sprites.HubTableUnlocked, HubRoomLayout.ObjectSortingOrder, groupObject.transform);
                Transform colliderParent = tableObject != null ? tableObject.transform : groupObject.transform;
                CreateInvisibleWall(placement.ColliderObjectName, placement.ColliderLocalPosition, HubRoomLayout.TableColliderScale, colliderParent);
            }
        }

        /// <summary>
        /// 업그레이드 슬롯은 위치, 스프라이트, 비용 표시를 한 스펙으로 관리해
        /// 이후 위치별 해금 비용 로직을 같은 기준으로 연결할 수 있게 만든다.
        /// </summary>
        private static void BuildHubUpgradeSlotLayout(SpriteLibrary sprites, Transform objectLayer)
        {
            foreach (HubRoomLayout.HubUpgradeSlotPlacement placement in HubRoomLayout.UpgradeSlotPlacements)
            {
                Sprite sprite = ResolveHubArtSprite(sprites, placement.SpriteId);
                GameObject slotObject = CreateHubArtSprite(placement.SlotObjectName, placement.Position, sprite, HubRoomLayout.ObjectSortingOrder, objectLayer);
                Transform priceParent = slotObject != null ? slotObject.transform : objectLayer;
                CreateHubUpgradePriceText(placement.PriceObjectName, priceParent, HubRoomLayout.UpgradePriceTextLocalOffset, placement.GoldCostLabel);
            }
        }

        /// <summary>
        /// 허브 고정 충돌은 각 월드 오브젝트 하위로 붙여 씬 Hierarchy에서 아트와 함께 보이게 맞춘다.
        /// </summary>
        private static void BuildHubCollisionLayout()
        {
            foreach (HubRoomLayout.HubColliderPlacement placement in HubRoomLayout.ColliderPlacements)
            {
                Transform parent = GameObject.Find(placement.ParentObjectName)?.transform;
                CreateInvisibleWall(placement.ObjectName, placement.LocalPosition, placement.Scale, parent);
            }
        }

        private static Transform ResolveHubArtParent(
            HubRoomLayout.HubArtAnchor anchor,
            Transform backgroundLayer,
            Transform objectLayer,
            Transform foregroundLayer)
        {
            return anchor switch
            {
                HubRoomLayout.HubArtAnchor.BackgroundLayer => backgroundLayer,
                HubRoomLayout.HubArtAnchor.ObjectLayer => objectLayer,
                HubRoomLayout.HubArtAnchor.ForegroundLayer => foregroundLayer,
                _ => objectLayer
            };
        }

        private static Sprite ResolveHubArtSprite(SpriteLibrary sprites, HubRoomLayout.HubArtSpriteId spriteId)
        {
            return spriteId switch
            {
                HubRoomLayout.HubArtSpriteId.FloorBackground => sprites.HubFloorBackground,
                HubRoomLayout.HubArtSpriteId.WallBackground => sprites.HubWallBackground,
                HubRoomLayout.HubArtSpriteId.Bar => sprites.HubBar,
                HubRoomLayout.HubArtSpriteId.TableUnlocked => sprites.HubTableUnlocked,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotLeft => sprites.HubUpgradeSlot,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotCenter => sprites.HubUpgradeSlot,
                HubRoomLayout.HubArtSpriteId.UpgradeSlotRight => sprites.HubUpgradeSlot,
                HubRoomLayout.HubArtSpriteId.FrontOutline => sprites.HubFrontOutline,
                _ => null
            };
        }

        private static void HideHubInteractionPresentations()
        {
            foreach (string objectName in HubRoomLayout.HiddenInteractionObjectNames)
            {
                HideWorldInteractionPresentation(GameObject.Find(objectName));
            }
        }

        /// <summary>
        /// 허브 벽면의 메뉴판을 PSD 배치 기준으로 다시 만든다.
        /// 제목 텍스트와 슬롯 배경, 음식 아이콘을 각각 월드 오브젝트로 분리해 허브 아트처럼 유지한다.
        /// </summary>
        private static void CreateHubTodayMenuBoard(Vector3 position, SpriteLibrary sprites, RestaurantManager restaurantManager, Transform parent = null)
        {
            GameObject boardRoot = new("HubTodayMenuBoard");
            if (parent != null)
            {
                boardRoot.transform.SetParent(parent, false);
                boardRoot.transform.localPosition = position;
            }
            else
            {
                boardRoot.transform.position = position;
            }

            CreateWorldTextObject(
                "HubTodayMenuHeaderShadow",
                boardRoot.transform,
                HubRoomLayout.TodayMenuHeaderLabelLocalPosition + HubRoomLayout.TodayMenuHeaderShadowLocalOffset,
                "오늘의 메뉴",
                HubRoomLayout.TodayMenuHeaderShadowColor,
                HubRoomLayout.TodayMenuHeaderFontSize,
                HubRoomLayout.TodayMenuItemSortingOrder,
                labelScale: HubRoomLayout.TodayMenuHeaderTextScale,
                fontStyle: FontStyles.Bold,
                characterSpacing: 0.04f);

            TextMeshPro headerLabel = CreateWorldTextObject(
                "HubTodayMenuHeaderLabel",
                boardRoot.transform,
                HubRoomLayout.TodayMenuHeaderLabelLocalPosition,
                "오늘의 메뉴",
                HubRoomLayout.TodayMenuHeaderTextColor,
                HubRoomLayout.TodayMenuHeaderFontSize,
                HubRoomLayout.TodayMenuTextSortingOrder,
                labelScale: HubRoomLayout.TodayMenuHeaderTextScale,
                fontStyle: FontStyles.Bold,
                characterSpacing: 0.04f);

            Sprite[] itemSprites =
            {
                sprites.HubTodayMenuItem2,
                sprites.HubTodayMenuItem1,
                sprites.HubTodayMenuItem3
            };

            SpriteRenderer[] entryBackdrops = new SpriteRenderer[HubRoomLayout.TodayMenuEntryLocalPositions.Length];
            SpriteRenderer[] entryIcons = new SpriteRenderer[HubRoomLayout.TodayMenuEntryLocalPositions.Length];

            for (int i = 0; i < HubRoomLayout.TodayMenuEntryLocalPositions.Length; i++)
            {
                GameObject backdrop = CreateDecorBlock(
                    $"HubTodayMenuEntryBackdrop{i + 1}",
                    HubRoomLayout.TodayMenuEntryLocalPositions[i],
                    HubRoomLayout.TodayMenuEntryBackdropScale,
                    sprites.HubTodayMenuBg,
                    HubRoomLayout.TodayMenuBackdropColor,
                    HubRoomLayout.TodayMenuBackdropSortingOrder,
                    boardRoot.transform);

                entryBackdrops[i] = backdrop.GetComponent<SpriteRenderer>();

                GameObject item = CreateDecorBlock(
                    $"HubTodayMenuEntryItem{i + 1}",
                    HubRoomLayout.TodayMenuEntryIconLocalOffset,
                    HubRoomLayout.TodayMenuEntryIconScale,
                    itemSprites[i],
                    HubRoomLayout.TodayMenuIconColor,
                    HubRoomLayout.TodayMenuItemSortingOrder,
                    backdrop.transform);

                entryIcons[i] = item.GetComponent<SpriteRenderer>();
            }

            HubTodayMenuDisplay display = boardRoot.AddComponent<HubTodayMenuDisplay>();
            display.Configure(restaurantManager, headerLabel, entryBackdrops, entryIcons);
        }

        private static void CreateStorageStation(string objectName, Vector3 position, Vector3 size, Sprite sprite, Color color, string label, StorageManager storageManager, StorageStationAction action, Transform parent = null)
        {
            GameObject go = CreateDecorBlock(objectName, position, size, sprite, color, 8, parent);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            StorageStation station = go.AddComponent<StorageStation>();
            SerializedObject so = new(station);
            so.FindProperty("storageManager").objectReferenceValue = storageManager;
            so.FindProperty("stationAction").enumValueIndex = (int)action;
            so.FindProperty("promptLabel").stringValue = action switch
            {
                StorageStationAction.StoreSelected => "창고 맡기기",
                StorageStationAction.WithdrawSelected => "창고 꺼내기",
                StorageStationAction.CycleInventorySelection => "맡길 품목 변경",
                StorageStationAction.CycleStorageSelection => "꺼낼 품목 변경",
                StorageStationAction.StoreAll => "모두 맡기기",
                StorageStationAction.WithdrawAll => "모두 꺼내기",
                _ => "창고 사용"
            };
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel(objectName + "_Label", go.transform, new Vector3(0f, 0.72f, 0f), label, Color.black, WorldLabelFontSize, 50);
        }

        private static void CreateUpgradeStation(Vector3 position, Vector3 size, Sprite sprite, Color color, UpgradeManager upgradeManager)
        {
            GameObject go = CreateDecorBlock("UpgradeStation", position, size, sprite, color, 8);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            UpgradeStation station = go.AddComponent<UpgradeStation>();
            SerializedObject so = new(station);
            so.FindProperty("upgradeManager").objectReferenceValue = upgradeManager;
            so.FindProperty("promptLabel").stringValue = "작업대 사용";
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel("UpgradeStationLabel", go.transform, new Vector3(0f, 0.68f, 0f), "작업대", Color.black, WorldLabelFontSize, 50);
        }

        private static void CreateGatherable(string objectName, Vector3 position, Sprite sprite, ResourceData resource, ToolType requiredToolType, int minAmount, int maxAmount, string label)
        {
            CreateFeaturePad(objectName + "_Pad", position + new Vector3(0f, -0.35f, 0f), new Vector3(1.6f, 0.5f, 1f), sprite, new Color(0f, 0f, 0f, 0.12f));

            GameObject go = CreateDecorBlock(objectName, position, new Vector3(1.05f, 1.05f, 1f), sprite, Color.white, 6);
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            GatherableResource gatherable = go.AddComponent<GatherableResource>();
            SerializedObject so = new(gatherable);
            so.FindProperty("resourceData").objectReferenceValue = resource;
            so.FindProperty("requiredToolType").enumValueIndex = (int)requiredToolType;
            so.FindProperty("minAmount").intValue = minAmount;
            so.FindProperty("maxAmount").intValue = maxAmount;
            so.FindProperty("promptLabel").stringValue = "채집하기";
            so.FindProperty("blockingCollider").objectReferenceValue = collider;
            so.ApplyModifiedPropertiesWithoutUndo();
            typeof(GatherableResource)
                .GetField("resourceData", BindingFlags.Instance | BindingFlags.NonPublic)?
                .SetValue(gatherable, resource);
            EditorUtility.SetDirty(gatherable);

            CreateWorldLabel(objectName + "_Label", go.transform, new Vector3(0f, 0.64f, 0f), label, Color.black, WorldLabelSmallFontSize, 45);
        }

        private static void CreateGuideTriggerZone(string objectName, Vector3 position, Vector2 size, string hintId, string guideText)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            GuideTriggerZone trigger = go.AddComponent<GuideTriggerZone>();
            SerializedObject so = new(trigger);
            so.FindProperty("hintId").stringValue = hintId;
            so.FindProperty("guideText").stringValue = guideText;
            so.FindProperty("duration").floatValue = 5f;
            so.FindProperty("triggerOnlyOnce").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMovementModifierZone(string objectName, Vector3 position, Vector2 size, float multiplier, string guideText)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            MovementModifierZone zone = go.AddComponent<MovementModifierZone>();
            SerializedObject so = new(zone);
            so.FindProperty("movementMultiplier").floatValue = multiplier;
            so.FindProperty("guideText").stringValue = guideText;
            so.FindProperty("hintId").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDarknessZone(string objectName, Vector3 position, Vector2 size)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            DarknessZone zone = go.AddComponent<DarknessZone>();
            SerializedObject so = new(zone);
            so.FindProperty("noLanternMovementMultiplier").floatValue = 0.45f;
            so.FindProperty("noLanternGuideText").stringValue = "랜턴이 없으면 폐광산 안쪽을 천천히 더듬어 움직여야 합니다.";
            so.FindProperty("hintId").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWindGustZone(string objectName, Vector3 position, Vector2 size, Vector2 direction, float strength, float activeDuration, float inactiveDuration)
        {
            GameObject go = new(objectName);
            go.transform.position = position;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            WindGustZone zone = go.AddComponent<WindGustZone>();
            SerializedObject so = new(zone);
            so.FindProperty("gustDirection").vector2Value = direction;
            so.FindProperty("gustStrength").floatValue = strength;
            so.FindProperty("activeDuration").floatValue = activeDuration;
            so.FindProperty("inactiveDuration").floatValue = inactiveDuration;
            so.FindProperty("startActive").boolValue = true;
            so.FindProperty("hintIdPrefix").stringValue = objectName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSpawnPoint(string objectName, Vector3 position, string spawnId)
        {
            GameObject go = new(objectName);
            go.transform.position = position;
            SceneSpawnPoint spawnPoint = go.AddComponent<SceneSpawnPoint>();

            SerializedObject so = new(spawnPoint);
            so.FindProperty("spawnId").stringValue = spawnId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 새로 생성하는 씬에는 현재 HUD 구조만 심습니다.
        /// 더 이상 쓰지 않는 레거시 카드와 텍스트는 여기서 만들지 않습니다.
        /// </summary>
        private static void CreateUiCanvas(bool isHubScene)
        {
            GameObject canvasObject = new("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            EnsureUiEventSystem();

            Color chromeDark = new(0.96f, 0.97f, 0.99f, 1f);
            Color chromeSurface = new(0.98f, 0.98f, 0.99f, 1f);
            Color chromeGlass = new(0.93f, 0.95f, 0.98f, 1f);
            Color chromeOverlay = new(0f, 0f, 0f, 0.52f);
            Color chromeAmber = new(0.94f, 0.74f, 0.10f, 1f);
            Color chromeText = new(0.23f, 0.27f, 0.34f, 1f);
            Color chromeDock = new(0.22f, 0.60f, 0.87f, 1f);
            string hudActionGroupName = PrototypeUISceneLayoutCatalog.ResolveObjectName("HUDActionGroup");
            string hudPanelButtonGroupName = PrototypeUISceneLayoutCatalog.ResolveObjectName("HUDPanelButtonGroup");

            RectTransform hudRoot = CreateCanvasGroupRoot("HUDRoot", canvasObject.transform, 0);
            RectTransform popupRoot = CreateCanvasGroupRoot("PopupRoot", canvasObject.transform, 1);
            RectTransform hudStatusGroup = CreateCanvasGroupRoot("HUDStatusGroup", hudRoot, 0);
            RectTransform hudActionGroup = CreateCanvasGroupRoot(hudActionGroupName, hudRoot, 1);
            RectTransform hudBottomGroup = CreateCanvasGroupRoot("HUDBottomGroup", hudRoot, 2);
            RectTransform hudOverlayGroup = CreateCanvasGroupRoot("HUDOverlayGroup", hudRoot, 4);
            RectTransform popupShellGroup = CreateCanvasGroupRoot("PopupShellGroup", popupRoot, 0);
            CreateCanvasGroupRoot("PopupFrameHeader", popupRoot, 2);
            RectTransform popupFrameGroup = null;
            RectTransform popupFrameLeftGroup = null;
            RectTransform popupFrameRightGroup = null;
            RectTransform actionDock = null;
            RectTransform hudPanelButtonGroup = null;

			CreatePanel("TopLeftPanel", hudStatusGroup, PrototypeUILayout.TopLeftPanel, chromeDark);
			CreatePanel("PhaseBadge", hudStatusGroup, PrototypeUILayout.PhaseBadge, chromeGlass);
            CreatePanel("InteractionPromptBackdrop", hudRoot, PrototypeUILayout.PromptBackdrop(isHubScene), Color.white);
			CreatePanel("GuideBackdrop", hudOverlayGroup, PrototypeUILayout.GuideBackdrop(isHubScene), chromeSurface);
			CreatePanel("ResultBackdrop", hudOverlayGroup, PrototypeUILayout.ResultBackdrop(isHubScene), chromeSurface);
            if (isHubScene)
            {
                CreatePanel(hudPanelButtonGroupName, hudRoot, PrototypeUILayout.HubPanelButtonGroup, chromeSurface);
                hudPanelButtonGroup = FindChildRecursive(hudRoot, hudPanelButtonGroupName) as RectTransform;
                CreatePanel("PopupOverlay", popupShellGroup, PrototypeUILayout.HubPopupOverlay, chromeOverlay);
                CreatePanel("ActionDock", hudActionGroup, PrototypeUILayout.HubActionDock, chromeDock);
                actionDock = FindChildRecursive(hudActionGroup, "ActionDock") as RectTransform;
                CreatePanel("ActionAccent", hudActionGroup, PrototypeUILayout.HubActionAccent, chromeAmber);
                CreatePanel("PopupFrame", popupRoot, PrototypeUILayout.HubPopupFrame, new Color(1f, 1f, 1f, 0f));
                popupFrameGroup = FindChildRecursive(popupRoot, "PopupFrame") as RectTransform;

                if (popupFrameGroup != null)
                {
                    CreatePanel("PopupFrameLeft", popupFrameGroup, PrototypeUILayout.HubPopupFrameLeft, Color.white);
                    CreatePanel("PopupFrameRight", popupFrameGroup, PrototypeUILayout.HubPopupFrameRight, new Color(0.92f, 0.95f, 0.99f, 1f));

                    popupFrameLeftGroup = FindChildRecursive(popupFrameGroup, "PopupFrameLeft") as RectTransform;
                    popupFrameRightGroup = FindChildRecursive(popupFrameGroup, "PopupFrameRight") as RectTransform;

                    if (popupFrameLeftGroup != null)
                    {
                        CreatePanel("PopupLeftBody", popupFrameLeftGroup, PrototypeUILayout.HubPopupFrameBody, Color.white);
                        CreatePopupBodyItemBoxes("PopupLeftBody", "PopupLeftItemBox", "PopupLeftItemIcon", "PopupLeftItemText", popupFrameLeftGroup, chromeText, true);
                    }

                    if (popupFrameRightGroup != null)
                    {
                        CreatePanel("PopupRightBody", popupFrameRightGroup, PrototypeUILayout.HubPopupFrameBody, Color.white);
                    }
                }
            }

            UIManager uiManager = canvasObject.AddComponent<UIManager>();
            PrototypeUIDesignController designController = canvasObject.AddComponent<PrototypeUIDesignController>();
            designController.Configure(uiManager);

            TextMeshProUGUI storageCaption = null;
            TextMeshProUGUI recipeCaption = null;
            TextMeshProUGUI upgradeCaption = null;
            TextMeshProUGUI actionCaption = null;

            if (isHubScene)
            {
                Transform popupFrameTextRoot = popupFrameGroup != null ? popupFrameGroup : popupRoot;
                Transform popupFrameLeftTextRoot = popupFrameLeftGroup != null ? popupFrameLeftGroup : popupFrameTextRoot;
                Transform popupFrameRightTextRoot = popupFrameRightGroup != null ? popupFrameRightGroup : popupFrameTextRoot;

                actionCaption = CreateScreenText(
                    "ActionCaption",
                    actionDock != null ? actionDock : hudActionGroup,
                    PrototypeUILayout.HubActionCaption,
                    15,
                    TextAlignmentOptions.TopRight,
                    new Color(0.88f, 0.88f, 0.88f, 1f));
                CreatePopupHeadingText(PopupTitleObjectName, popupFrameLeftTextRoot, PrototypeUILayout.HubPopupTitle, 40f, 24f, "\uC694\uB9AC \uBA54\uB274", chromeText, false);
                CreatePopupHeadingText(PopupLeftCaptionObjectName, popupFrameLeftTextRoot, PrototypeUILayout.HubPopupLeftCaption, 32f, 20f, "\uBA54\uB274 \uBAA9\uB85D", chromeText, false);
                CreatePopupHeadingText(PopupRightCaptionObjectName, popupFrameRightTextRoot, PrototypeUILayout.HubPopupFrameCaption, 32f, 20f, "\uBA54\uB274 \uC0C1\uC138", chromeText, false);
            }

            TextMeshProUGUI goldText = CreateScreenText("GoldText", hudStatusGroup, PrototypeUILayout.GoldText, 20, TextAlignmentOptions.TopLeft, chromeText);
            TextMeshProUGUI inventoryText = isHubScene
                ? CreateScreenText("InventoryText", popupFrameLeftGroup != null ? popupFrameLeftGroup : popupRoot, PrototypeUILayout.HubPopupFrameText, 19, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI storageText = isHubScene
                ? CreateScreenText("StorageText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI promptText = CreateScreenText("InteractionPromptText", hudRoot, PrototypeUILayout.PromptText(isHubScene), 21, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI guideText = CreateScreenText("GuideText", hudOverlayGroup, PrototypeUILayout.GuideText(isHubScene), 18, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI resultText = CreateScreenText("RestaurantResultText", hudOverlayGroup, PrototypeUILayout.ResultText(isHubScene), 18, TextAlignmentOptions.Center, chromeText);
            TextMeshProUGUI selectedRecipeText = isHubScene
                ? CreateScreenText("SelectedRecipeText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI upgradeText = isHubScene
                ? CreateScreenText("UpgradeText", popupFrameRightGroup != null ? popupFrameRightGroup : popupRoot, PrototypeUILayout.HubPopupRightDetailText, 18, TextAlignmentOptions.TopLeft, chromeText)
                : null;
            TextMeshProUGUI dayPhaseText = CreateScreenText("DayPhaseText", hudStatusGroup, PrototypeUILayout.DayPhaseText, 20, TextAlignmentOptions.Center, chromeText);

            ApplyPopupInventoryTextPresentation(inventoryText);
            ApplyPopupDetailTextPresentation(storageText);
            ApplyPopupDetailTextPresentation(selectedRecipeText);
            ApplyPopupDetailTextPresentation(upgradeText);

            // 허브 진행 버튼은 ActionDock 안에서 phase별로 교체 표시한다.
            Button skipExplorationButton = isHubScene ? CreateUiButton("SkipExplorationButton", actionDock != null ? actionDock : hudBottomGroup, PrototypeUILayout.HubSkipExplorationButton, "\uD0D0\uD5D8 \uC2A4\uD0B5") : null;
            Button skipServiceButton = isHubScene ? CreateUiButton("SkipServiceButton", actionDock != null ? actionDock : hudBottomGroup, PrototypeUILayout.HubSkipServiceButton, "\uC601\uC5C5 \uC2A4\uD0B5") : null;
            Button nextDayButton = isHubScene ? CreateUiButton("NextDayButton", actionDock != null ? actionDock : hudBottomGroup, PrototypeUILayout.HubNextDayButton, "\uB2E4\uC74C \uB0A0") : null;
            Button recipePanelButton = isHubScene ? CreateUiButton("RecipePanelButton", hudPanelButtonGroup != null ? hudPanelButtonGroup : hudRoot, PrototypeUILayout.HubRecipePanelButton, "\uC694\uB9AC \uBA54\uB274") : null;
            Button upgradePanelButton = isHubScene ? CreateUiButton("UpgradePanelButton", hudPanelButtonGroup != null ? hudPanelButtonGroup : hudRoot, PrototypeUILayout.HubUpgradePanelButton, "\uC5C5\uADF8\uB808\uC774\uB4DC") : null;
            Button materialPanelButton = isHubScene ? CreateUiButton("MaterialPanelButton", hudPanelButtonGroup != null ? hudPanelButtonGroup : hudRoot, PrototypeUILayout.HubMaterialPanelButton, "\uC7AC\uB8CC") : null;
            Button guideHelpButton = CreateUiButton("GuideHelpButton", hudOverlayGroup, PrototypeUILayout.GuideHelpButton(isHubScene), string.Empty);
            Button popupCloseButton = isHubScene ? CreateUiButton("PopupCloseButton", popupFrameRightGroup != null ? popupFrameRightGroup : (popupFrameGroup != null ? popupFrameGroup : popupRoot), PrototypeUILayout.HubPopupCloseButton, string.Empty) : null;
            HideGeneratedButtonLabel(guideHelpButton);
            HideGeneratedButtonLabel(popupCloseButton);

            if (storageCaption != null) storageCaption.text = "\uCC3D\uACE0";
            if (recipeCaption != null) recipeCaption.text = "\uC694\uB9AC \uBA54\uB274";
            if (upgradeCaption != null) upgradeCaption.text = "\uC5C5\uADF8\uB808\uC774\uB4DC";
            if (actionCaption != null) actionCaption.text = "\uC9C4\uD589";

            if (storageCaption != null) storageCaption.fontStyle = FontStyles.Bold;
            if (recipeCaption != null) recipeCaption.fontStyle = FontStyles.Bold;
            if (upgradeCaption != null) upgradeCaption.fontStyle = FontStyles.Bold;
            if (actionCaption != null) actionCaption.fontStyle = FontStyles.Bold;
            if (storageCaption != null) storageCaption.characterSpacing = 0.5f;
            if (recipeCaption != null) recipeCaption.characterSpacing = 0.5f;
            if (upgradeCaption != null) upgradeCaption.characterSpacing = 0.5f;
            if (actionCaption != null) actionCaption.characterSpacing = 0.5f;
            if (storageCaption != null) storageCaption.margin = Vector4.zero;
            if (recipeCaption != null) recipeCaption.margin = Vector4.zero;
            if (upgradeCaption != null) upgradeCaption.margin = Vector4.zero;
            if (actionCaption != null) actionCaption.margin = Vector4.zero;

            if (dayPhaseText != null) dayPhaseText.fontStyle = FontStyles.Bold;
            if (inventoryText != null) inventoryText.textWrappingMode = TextWrappingModes.Normal;
            if (inventoryText != null) inventoryText.overflowMode = TextOverflowModes.Masking;
            if (storageText != null) storageText.textWrappingMode = TextWrappingModes.Normal;
            if (storageText != null) storageText.overflowMode = TextOverflowModes.Masking;
            if (selectedRecipeText != null) selectedRecipeText.textWrappingMode = TextWrappingModes.Normal;
            if (selectedRecipeText != null) selectedRecipeText.overflowMode = TextOverflowModes.Masking;
            if (upgradeText != null) upgradeText.textWrappingMode = TextWrappingModes.Normal;
            if (upgradeText != null) upgradeText.overflowMode = TextOverflowModes.Masking;

            if (goldText != null) goldText.text = "\uCF54\uC778: 0   \uD3C9\uD310: 0";
            if (inventoryText != null) inventoryText.text = "\uC778\uBCA4\uD1A0\uB9AC 0/8\uCE78\n- \uBE44\uC5B4 \uC788\uC74C";
            if (storageText != null) storageText.text = "- \uBE44\uC5B4 \uC788\uC74C";
            if (promptText != null) promptText.text = "\uC774\uB3D9: WASD / \uBC29\uD5A5\uD0A4   \uC0C1\uD638\uC791\uC6A9: E";
            if (guideText != null) guideText.text = string.Empty;
            if (resultText != null) resultText.text = string.Empty;
            if (selectedRecipeText != null) selectedRecipeText.text = "\uC120\uD0DD \uBA54\uB274: \uC5C6\uC74C";
            if (upgradeText != null) upgradeText.text = "- \uC778\uBCA4\uD1A0\uB9AC 8\uCE78 -> 12\uCE78";
            if (dayPhaseText != null) dayPhaseText.text = "1\uC77C\uCC28 \u00B7 \uC624\uC804 \uD0D0\uD5D8";

            ApplySceneOverridesToHierarchy(canvasObject.transform);

            if (isHubScene)
            {
                SetChildActive(canvasObject.transform, hudPanelButtonGroupName, false);
                SetChildActive(canvasObject.transform, "PopupOverlay", false);
                SetChildActive(canvasObject.transform, "PopupFrame", false);
                SetChildActive(canvasObject.transform, "PopupFrameLeft", false);
                SetChildActive(canvasObject.transform, "PopupFrameRight", false);
                SetChildActive(canvasObject.transform, "PopupLeftBody", false);
                SetChildActive(canvasObject.transform, "PopupRightBody", false);
                SetChildActive(canvasObject.transform, "StorageCard", false);
                SetChildActive(canvasObject.transform, "RecipeCard", false);
                SetChildActive(canvasObject.transform, "UpgradeCard", false);
                SetChildActive(canvasObject.transform, "ActionDock", false);
                SetChildActive(canvasObject.transform, "StorageAccent", false);
                SetChildActive(canvasObject.transform, "RecipeAccent", false);
                SetChildActive(canvasObject.transform, "UpgradeAccent", false);
                SetChildActive(canvasObject.transform, "ActionAccent", false);
            }

			SetChildActive(canvasObject.transform, "GuideBackdrop", false);
			SetChildActive(canvasObject.transform, "ResultBackdrop", false);
            if (storageCaption != null) storageCaption.gameObject.SetActive(false);
            if (recipeCaption != null) recipeCaption.gameObject.SetActive(false);
            if (upgradeCaption != null) upgradeCaption.gameObject.SetActive(false);
            if (actionCaption != null) actionCaption.gameObject.SetActive(false);
            SetChildActive(canvasObject.transform, PopupTitleObjectName, false);
            SetChildActive(canvasObject.transform, PopupLeftCaptionObjectName, false);
            SetChildActive(canvasObject.transform, PopupRightCaptionObjectName, false);
            if (inventoryText != null) inventoryText.gameObject.SetActive(false);
            if (storageText != null) storageText.gameObject.SetActive(false);
            if (guideText != null) guideText.gameObject.SetActive(false);
            if (resultText != null) resultText.gameObject.SetActive(false);
            if (selectedRecipeText != null) selectedRecipeText.gameObject.SetActive(false);
            if (upgradeText != null) upgradeText.gameObject.SetActive(false);
            if (skipExplorationButton != null) skipExplorationButton.gameObject.SetActive(false);
            if (skipServiceButton != null) skipServiceButton.gameObject.SetActive(false);
            if (nextDayButton != null) nextDayButton.gameObject.SetActive(false);
            if (recipePanelButton != null) recipePanelButton.gameObject.SetActive(false);
            if (upgradePanelButton != null) upgradePanelButton.gameObject.SetActive(false);
            if (materialPanelButton != null) materialPanelButton.gameObject.SetActive(false);
            if (popupCloseButton != null) popupCloseButton.gameObject.SetActive(false);

            SerializedObject so = new(uiManager);
            so.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            so.FindProperty("inventoryText").objectReferenceValue = inventoryText;
            so.FindProperty("storageText").objectReferenceValue = storageText;
            so.FindProperty("upgradeText").objectReferenceValue = upgradeText;
            so.FindProperty("goldText").objectReferenceValue = goldText;
            so.FindProperty("selectedRecipeText").objectReferenceValue = selectedRecipeText;
            so.FindProperty("dayPhaseText").objectReferenceValue = dayPhaseText;
            so.FindProperty("guideText").objectReferenceValue = guideText;
            so.FindProperty("resultText").objectReferenceValue = resultText;
            so.FindProperty("bodyFontAsset").objectReferenceValue = _generatedKoreanFont;
            so.FindProperty("headingFontAsset").objectReferenceValue = _generatedHeadingFont;
            so.FindProperty("skipExplorationButton").objectReferenceValue = skipExplorationButton;
            so.FindProperty("skipServiceButton").objectReferenceValue = skipServiceButton;
            so.FindProperty("nextDayButton").objectReferenceValue = nextDayButton;
            so.FindProperty("recipePanelButton").objectReferenceValue = recipePanelButton;
            so.FindProperty("upgradePanelButton").objectReferenceValue = upgradePanelButton;
            so.FindProperty("materialPanelButton").objectReferenceValue = materialPanelButton;
            so.FindProperty("guideHelpButton").objectReferenceValue = guideHelpButton;
            so.FindProperty("popupCloseButton").objectReferenceValue = popupCloseButton;
            so.FindProperty("defaultPromptText").stringValue = "\uC774\uB3D9: WASD / \uBC29\uD5A5\uD0A4   \uC0C1\uD638\uC791\uC6A9: E";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureUiEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureInputSystemUiModule(inputModule);
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Input System 기본 액션 생성 경로가 에디터에서 예외를 내는 환경이 있어
        /// 프로젝트 내부 UI 액션 에셋을 직접 만들고 모듈에 연결합니다.
        /// </summary>
        private static void ConfigureInputSystemUiModule(InputSystemUIInputModule inputModule)
        {
            InputActionAsset asset = EnsureUiInputActionsAsset();

            inputModule.actionsAsset = asset;
            inputModule.point = InputActionReference.Create(asset.FindAction("UI/Point", true));
            inputModule.leftClick = InputActionReference.Create(asset.FindAction("UI/LeftClick", true));
            inputModule.rightClick = InputActionReference.Create(asset.FindAction("UI/RightClick", true));
            inputModule.middleClick = InputActionReference.Create(asset.FindAction("UI/MiddleClick", true));
            inputModule.scrollWheel = InputActionReference.Create(asset.FindAction("UI/ScrollWheel", true));
            inputModule.move = InputActionReference.Create(asset.FindAction("UI/Move", true));
            inputModule.submit = InputActionReference.Create(asset.FindAction("UI/Submit", true));
            inputModule.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", true));
            inputModule.trackedDevicePosition = InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition", true));
            inputModule.trackedDeviceOrientation = InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation", true));
        }

        private static InputActionAsset EnsureUiInputActionsAsset()
        {
            string assetPath = InputDataRoot + "/generated-ui-input-actions.asset";
            InputActionAsset existingAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            EnsureMainObjectNameMatchesFileName(existingAsset, assetPath);
            if (existingAsset != null && HasRequiredUiActions(existingAsset))
            {
                return existingAsset;
            }

            if (existingAsset != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "generated-ui-input-actions";
            InputActionMap uiMap = new("UI");
            asset.AddActionMap(uiMap);

            InputAction pointAction = uiMap.AddAction("Point", InputActionType.PassThrough);
            pointAction.expectedControlType = "Vector2";
            pointAction.AddBinding("<Mouse>/position");
            pointAction.AddBinding("<Pen>/position");
            pointAction.AddBinding("<Touchscreen>/primaryTouch/position");

            InputAction leftClickAction = uiMap.AddAction("LeftClick", InputActionType.PassThrough);
            leftClickAction.expectedControlType = "Button";
            leftClickAction.AddBinding("<Mouse>/leftButton");
            leftClickAction.AddBinding("<Pen>/tip");
            leftClickAction.AddBinding("<Touchscreen>/primaryTouch/press");

            InputAction rightClickAction = uiMap.AddAction("RightClick", InputActionType.PassThrough);
            rightClickAction.expectedControlType = "Button";
            rightClickAction.AddBinding("<Mouse>/rightButton");

            InputAction middleClickAction = uiMap.AddAction("MiddleClick", InputActionType.PassThrough);
            middleClickAction.expectedControlType = "Button";
            middleClickAction.AddBinding("<Mouse>/middleButton");

            InputAction scrollWheelAction = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough);
            scrollWheelAction.expectedControlType = "Vector2";
            scrollWheelAction.AddBinding("<Mouse>/scroll");

            InputAction moveAction = uiMap.AddAction("Move", InputActionType.PassThrough);
            moveAction.expectedControlType = "Vector2";
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            InputAction submitAction = uiMap.AddAction("Submit", InputActionType.Button);
            submitAction.expectedControlType = "Button";
            submitAction.AddBinding("<Keyboard>/enter");
            submitAction.AddBinding("<Keyboard>/numpadEnter");
            submitAction.AddBinding("<Keyboard>/space");
            submitAction.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancelAction = uiMap.AddAction("Cancel", InputActionType.Button);
            cancelAction.expectedControlType = "Button";
            cancelAction.AddBinding("<Keyboard>/escape");
            cancelAction.AddBinding("<Gamepad>/buttonEast");

            InputAction trackedPositionAction = uiMap.AddAction("TrackedDevicePosition", InputActionType.PassThrough);
            trackedPositionAction.expectedControlType = "Vector3";
            trackedPositionAction.AddBinding("<XRController>/devicePosition");

            InputAction trackedOrientationAction = uiMap.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough);
            trackedOrientationAction.expectedControlType = "Quaternion";
            trackedOrientationAction.AddBinding("<XRController>/deviceRotation");

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static bool HasRequiredUiActions(InputActionAsset asset)
        {
            return asset.FindAction("UI/Point") != null
                   && asset.FindAction("UI/LeftClick") != null
                   && asset.FindAction("UI/RightClick") != null
                   && asset.FindAction("UI/MiddleClick") != null
                   && asset.FindAction("UI/ScrollWheel") != null
                   && asset.FindAction("UI/Move") != null
                   && asset.FindAction("UI/Submit") != null
                   && asset.FindAction("UI/Cancel") != null
                   && asset.FindAction("UI/TrackedDevicePosition") != null
                   && asset.FindAction("UI/TrackedDeviceOrientation") != null;
        }
#endif

        /// <summary>
        /// 화면 고정 UI 텍스트를 만들고 generated 한글 폰트와 기본 여백을 같이 적용합니다.
        /// </summary>
        private static bool ShouldSkipCanvasObjectCreation(string objectName, Transform parent)
        {
            return parent == null
                || string.IsNullOrWhiteSpace(objectName)
                || PrototypeUISceneLayoutCatalog.IsObjectRemoved(objectName);
        }

        private static TextMeshProUGUI CreateScreenText(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject go = new(objectName);
            ApplyHubPopupObjectIdentity(go);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            TMP_FontAsset preferredFont = EnsurePreferredTmpFontAsset();
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize - 6f);
            text.fontSizeMax = fontSize;
            text.overflowMode = TextOverflowModes.Truncate;

            if (preferredFont != null)
            {
                text.font = preferredFont;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            ApplySceneTextOverride(text);
            return text;
        }

        /// <summary>
        /// 공용 레이아웃 프리셋을 바로 넘겨 HUD 텍스트 생성 중복을 줄입니다.
        /// </summary>
        private static TextMeshProUGUI CreateScreenText(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            return CreateScreenText(
                objectName,
                parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                fontSize,
                alignment,
                color);
        }

        /// <summary>
        /// 씬에서 저장한 TMP 표시 오버라이드가 있으면 기본 스타일 적용 뒤 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneTextOverride(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            PrototypeUISceneLayoutCatalog.TryApplyTextOverride(text, text.name);
        }

        /// <summary>
        /// 씬에서 저장한 버튼과 라벨 오버라이드가 있으면 기본 스타일 적용 뒤 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneButtonOverride(Button button)
        {
            if (button == null)
            {
                return;
            }

            PrototypeUISceneLayoutCatalog.TryApplyButtonOverride(button, button.name);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                PrototypeUISceneLayoutCatalog.TryApplyTextOverride(label, label.name);
            }
        }

        private static void CreatePopupHeadingText(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            float fontSize,
            float sceneFontSizeMax,
            string content,
            Color color,
            bool enableAutoSizing)
        {
            TextMeshProUGUI text = CreateScreenText(objectName, parent, layout, fontSize, TextAlignmentOptions.TopLeft, color);
            if (text == null)
            {
                return;
            }

            text.text = content;
            TMP_FontAsset headingFont = EnsureHeadingTmpFontAsset();
            if (headingFont != null)
            {
                text.font = headingFont;
                if (headingFont.material != null)
                {
                    text.fontSharedMaterial = headingFont.material;
                }
            }

            ApplyPopupHeadingPresentation(text, fontSize, sceneFontSizeMax, enableAutoSizing);
            ApplySceneTextOverride(text);
        }

        private static void ApplyPopupHeadingPresentation(TextMeshProUGUI text, float fontSize, float sceneFontSizeMax, bool enableAutoSizing)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = enableAutoSizing;
            text.fontSize = fontSize;
            text.fontSizeMin = 12f;
            text.fontSizeMax = sceneFontSizeMax;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.overflowMode = TextOverflowModes.Truncate;
        }

        /// <summary>
        /// 왼쪽 본문 인벤토리 텍스트는 Hub.unity 직렬화 값과 같은 크기/여백으로
        /// 맞춰서 빌더 미리보기와 실제 씬 표시 밀도가 달라지지 않게 합니다.
        /// </summary>
        private static void ApplyPopupInventoryTextPresentation(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = 19f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 13f;
            text.fontSizeMax = 19f;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.lineSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Masking;
            ApplySceneTextOverride(text);
        }

        private static void ApplyPopupDetailTextPresentation(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = 18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 18f;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.lineSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.margin = new Vector4(10f, 8f, 10f, 8f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Masking;
            ApplySceneTextOverride(text);
        }

        /// <summary>
        /// 카드 배경이나 포인트 바 같은 평면 UI 블록을 그림자와 함께 생성합니다.
        /// </summary>
        private static void CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject panelObject = new(objectName);
            ApplyHubPopupObjectIdentity(panelObject);
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = panelObject.AddComponent<Image>();
            ApplyScenePanelImagePresentation(image, objectName, color);

            image.raycastTarget = false;

            if (!objectName.EndsWith("Accent"))
            {
                Shadow shadow = panelObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
                shadow.effectDistance = new Vector2(0f, -4f);
                shadow.useGraphicAlpha = true;
            }
        }

        /// <summary>
        /// 공용 레이아웃 프리셋으로 배경 패널을 생성합니다.
        /// </summary>
        private static void CreatePanel(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            Color color)
        {
            CreatePanel(
                objectName,
                parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                color);
        }

        /// <summary>
        /// 허브 팝업 본문 안에 반복 아이템 박스를 미리 만들어 두면 에디터 프리뷰와 새 씬 기본 구조가 같은 기준을 쓸 수 있습니다.
        /// </summary>
        private static void CreatePopupBodyItemBoxes(
            string bodyName,
            string boxPrefix,
            string iconPrefix,
            string textPrefix,
            Transform popupRoot,
            Color textColor,
            bool isInteractive)
        {
            Transform bodyTransform = FindChildRecursive(popupRoot, bodyName);
            if (bodyTransform == null)
            {
                return;
            }

            Color boxColor = new(0.96f, 0.92f, 0.78f, 1f);
            for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
            {
                string boxName = $"{boxPrefix}{i + 1:00}";
                string iconName = $"{iconPrefix}{i + 1:00}";
                string textName = $"{textPrefix}{i + 1:00}";
                CreatePanel(boxName, bodyTransform, PrototypeUILayout.HubPopupBodyItemBox(i), boxColor);

                Transform boxTransform = FindChildRecursive(bodyTransform, boxName);
                if (boxTransform == null)
                {
                    continue;
                }

                Image boxImage = boxTransform.GetComponent<Image>();
                if (boxImage != null)
                {
                    boxImage.raycastTarget = isInteractive;
                }

                if (isInteractive)
                {
                    Button button = boxTransform.GetComponent<Button>();
                    if (button == null)
                    {
                        button = boxTransform.gameObject.AddComponent<Button>();
                    }

                    button.targetGraphic = boxImage;
                    button.transition = Selectable.Transition.ColorTint;
                    Navigation navigation = button.navigation;
                    navigation.mode = Navigation.Mode.None;
                    button.navigation = navigation;
                    ApplySceneButtonOverride(button);
                }

                CreatePopupBodyItemIcon(iconName, boxTransform);
                TextMeshProUGUI text = CreateScreenText(
                    textName,
                    boxTransform,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    17f,
                    TextAlignmentOptions.TopLeft,
                    textColor);
                if (text == null)
                {
                    continue;
                }

                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.margin = Vector4.zero;
                text.lineSpacing = 0f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = 17f;
                text.rectTransform.offsetMin = new Vector2(74f, 10f);
                text.rectTransform.offsetMax = new Vector2(-14f, -10f);
                text.text = string.Empty;
                ApplySceneTextOverride(text);
            }
        }

        private static void CreatePopupBodyItemIcon(string objectName, Transform parent)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(40f, 0f),
                    new Vector2(44f, 44f)));

            GameObject iconObject = new(objectName);
            ApplyHubPopupObjectIdentity(iconObject);
            iconObject.transform.SetParent(parent, false);

            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = iconObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private static RectTransform CreateCanvasGroupRoot(string objectName, Transform parent, int siblingIndex)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero));

            GameObject groupObject = new(objectName);
            ApplyHubPopupObjectIdentity(groupObject);
            groupObject.transform.SetParent(parent, false);

            RectTransform rect = groupObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(siblingIndex);
            return rect;
        }

        private static void SetChildActive(Transform parent, string objectName, bool isActive)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = FindChildRecursive(parent, objectName);
            if (child != null)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == objectName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Transform FindNamedTransformInScene(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                if (root.name == objectName)
                {
                    return root.transform;
                }

                Transform nested = FindChildRecursive(root.transform, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// 빠른 행동 버튼을 만들고 텍스트와 그림자까지 기본 스타일로 맞춥니다.
        /// </summary>
        private static Button CreateUiButton(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            string label)
        {
            if (ShouldSkipCanvasObjectCreation(objectName, parent))
            {
                return null;
            }

            PrototypeUIRect resolvedLayout = PrototypeUISceneLayoutCatalog.ResolveLayout(
                objectName,
                new PrototypeUIRect(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta));

            GameObject buttonObject = new(objectName);
            ApplyHubPopupObjectIdentity(buttonObject);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = resolvedLayout.AnchorMin;
            rect.anchorMax = resolvedLayout.AnchorMax;
            rect.pivot = resolvedLayout.Pivot;
            rect.anchoredPosition = resolvedLayout.AnchoredPosition;
            rect.sizeDelta = resolvedLayout.SizeDelta;

            Image image = buttonObject.AddComponent<Image>();
            ApplySceneButtonImagePresentation(image, objectName, new Color(0.18f, 0.18f, 0.18f, 0.82f));

            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            Button button = buttonObject.AddComponent<Button>();

            TextMeshProUGUI labelText = CreateScreenText(
                objectName + "_Label",
                buttonObject.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                20,
                TextAlignmentOptions.Center,
                Color.white);
            labelText.text = label;
            if (_generatedHeadingFont != null)
            {
                labelText.font = _generatedHeadingFont;
            }

            labelText.fontStyle = FontStyles.Bold;
            labelText.margin = new Vector4(8f, 6f, 8f, 6f);

            ApplySceneTextOverride(labelText);
            ApplySceneButtonOverride(button);
            return button;
        }

        /// <summary>
        /// 공용 레이아웃 프리셋으로 버튼을 생성해 허브 메뉴/액션 배치를 통일합니다.
        /// </summary>
        private static Button CreateUiButton(
            string objectName,
            Transform parent,
            PrototypeUIRect layout,
            string label)
        {
            return CreateUiButton(
                objectName,
                parent,
                layout.AnchorMin,
                layout.AnchorMax,
                layout.Pivot,
                layout.AnchoredPosition,
                layout.SizeDelta,
                label);
        }

        private static void HideGeneratedButtonLabel(Button button)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                return;
            }

            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }

        private static void ApplyHubPopupObjectIdentity(GameObject target)
        {
            if (target == null || !IsHubPopupDisplayObject(target.name))
            {
                return;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            target.layer = uiLayer >= 0 ? uiLayer : 5;
            target.tag = "Player";
        }

        /// <summary>
        /// 허브 팝업은 기존 Hub.unity에 저장된 Image 설정을 우선 적용해
        /// generated 스킨보다 씬에 직접 지정한 소스 이미지를 기준으로 맞춥니다.
        /// </summary>
        private static bool TryApplyHubPopupSceneImage(Image image, string objectName)
        {
            if (image == null || string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (PrototypeUISkinCatalog.UsesGeneratedUiDesignPanel(objectName)
                || PrototypeUISkinCatalog.UsesGeneratedUiDesignButton(objectName))
            {
                return false;
            }

            if (!CachedHubPopupSceneImages.TryGetValue(objectName, out SceneImageSnapshot snapshot))
            {
                return false;
            }

            image.sprite = snapshot.Sprite;
            image.type = snapshot.Type;
            image.color = snapshot.Color;
            image.preserveAspect = snapshot.PreserveAspect;
            return true;
        }

        /// <summary>
        /// 패널 기본 스킨을 적용한 뒤, 씬에서 동기화한 Image 오버라이드가 있으면 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplyScenePanelImagePresentation(Image image, string objectName, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            if (!TryApplyHubPopupSceneImage(image, objectName))
            {
                PrototypeUISkin.ApplyPanel(image, objectName, fallbackColor);
            }

            // generated UI 스킨은 기본값으로 적용하고, 씬 저장으로 동기화한 오버라이드가 있으면 마지막에 우선 반영한다.
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        /// <summary>
        /// 버튼 기본 스킨을 적용한 뒤, 씬에서 동기화한 Image 오버라이드가 있으면 마지막에 다시 덮어씁니다.
        /// </summary>
        private static void ApplySceneButtonImagePresentation(Image image, string objectName, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            if (!TryApplyHubPopupSceneImage(image, objectName))
            {
                PrototypeUISkin.ApplyButton(image, objectName, fallbackColor);
            }

            // generated UI 스킨은 기본값으로 적용하고, 씬 저장으로 동기화한 오버라이드가 있으면 마지막에 우선 반영한다.
            PrototypeUISceneLayoutCatalog.TryApplyImageOverride(image, objectName);
        }

        private static bool IsHubPopupDisplayObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName is "PopupRoot" or "PopupShellGroup" or "PopupFrameHeader" or "PopupOverlay")
            {
                return false;
            }

            return objectName.StartsWith("Popup", StringComparison.Ordinal)
                   || objectName is "InventoryText" or "StorageText" or "SelectedRecipeText" or "UpgradeText";
        }

        private static void CreateWorldLabel(string objectName, Transform parent, Vector3 localPosition, string content, Color color, float fontSize, int sortingOrder)
        {
            CreateWorldTextObject(objectName, parent, localPosition, content, color, fontSize, sortingOrder);
        }

        /// <summary>
        /// 월드 배치용 TextMeshPro 오브젝트를 만들고 참조를 바로 돌려준다.
        /// 메뉴 보드처럼 후속 구성에서 텍스트 컴포넌트가 필요할 때 사용한다.
        /// </summary>
        private static TextMeshPro CreateWorldTextObject(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            string content,
            Color color,
            float fontSize,
            int sortingOrder,
            float? labelScale = null,
            FontStyles? fontStyle = null,
            float? characterSpacing = null)
        {
            bool isLargeLabel = fontSize >= 3.4f;
            bool isPrimaryLabel = fontSize >= 2.5f;
            TMP_FontAsset preferredFont = isLargeLabel ? EnsureHeadingTmpFontAsset() : EnsurePreferredTmpFontAsset();

            GameObject labelObject = new(objectName);
            if (parent != null)
            {
                labelObject.transform.SetParent(parent, false);
                labelObject.transform.localPosition = localPosition;
            }
            else
            {
                labelObject.transform.position = localPosition;
            }

            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.characterSpacing = characterSpacing ?? (isLargeLabel ? 0.22f : isPrimaryLabel ? 0.08f : 0.02f);
            text.wordSpacing = 0f;
            text.lineSpacing = 0f;
            text.fontStyle = fontStyle ?? (isLargeLabel || isPrimaryLabel ? FontStyles.Bold : FontStyles.Normal);

            if (preferredFont != null)
            {
                text.font = preferredFont;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            float resolvedLabelScale = labelScale ?? (isLargeLabel ? 0.39f : isPrimaryLabel ? 0.36f : 0.33f);
            labelObject.transform.localScale = Vector3.one * resolvedLabelScale;
            Material worldTextMaterial = EnsureWorldTextSharedMaterial(text.font, isLargeLabel || isPrimaryLabel);
            if (worldTextMaterial != null)
            {
                text.fontSharedMaterial = worldTextMaterial;
            }

            ApplyWorldTextReadability(text);

            MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = sortingOrder;
            return text;
        }

        /// <summary>
        /// 허브 월드 텍스트는 배경 그림 위에서도 읽히도록 외곽선과 패딩을 기본 적용한다.
        /// </summary>
        private static void ApplyWorldTextReadability(TextMeshPro text)
        {
            if (text == null)
            {
                return;
            }

            text.extraPadding = true;
        }

        /// <summary>
        /// 에디터 빌드에서는 TMP setter가 renderer.material을 열지 않도록
        /// 공유 머티리얼 에셋을 만들어 폰트별/강도별로 재사용한다.
        /// </summary>
        private static Material EnsureWorldTextSharedMaterial(TMP_FontAsset fontAsset, bool useStrongOutline)
        {
            if (fontAsset == null || fontAsset.material == null)
            {
                return null;
            }

            string materialName = fontAsset.name + (useStrongOutline ? "WorldTextStrong" : "WorldTextNormal");
            if (CachedWorldTextMaterials.TryGetValue(materialName, out Material cachedMaterial) && cachedMaterial != null)
            {
                return cachedMaterial;
            }

            string materialPath = $"{FontRoot}/{materialName}.mat";
            Material materialAsset = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (materialAsset == null)
            {
                materialAsset = new Material(fontAsset.material)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(materialAsset, materialPath);
            }

            materialAsset.SetColor(ShaderUtilities.ID_OutlineColor, HubRoomLayout.WorldTextOutlineColor);
            materialAsset.SetFloat(
                ShaderUtilities.ID_OutlineWidth,
                useStrongOutline
                    ? HubRoomLayout.WorldTextStrongOutlineWidth
                    : HubRoomLayout.WorldTextNormalOutlineWidth);
            EditorUtility.SetDirty(materialAsset);

            CachedWorldTextMaterials[materialName] = materialAsset;
            return materialAsset;
        }

        /// <summary>
        /// 업그레이드 슬롯 내부 가격 표시는 슬롯 자식 텍스트로 생성해 슬롯 이동과 함께 유지한다.
        /// </summary>
        private static void CreateHubUpgradePriceText(string objectName, Transform parent, Vector3 localPosition, string content)
        {
            CreateWorldTextObject(
                objectName,
                parent,
                localPosition,
                content,
                HubRoomLayout.UpgradePriceTextColor,
                HubRoomLayout.UpgradePriceFontSize,
                HubRoomLayout.SignTextSortingOrder,
                labelScale: HubRoomLayout.UpgradePriceTextScale,
                fontStyle: FontStyles.Bold,
                characterSpacing: 0.08f);
        }

        /// <summary>
        /// 허브 바닥 표시는 별도 이미지 대신 얇은 바닥 패널과 텍스트 조합으로 다시 만든다.
        /// </summary>
        private static void CreateHubFloorSign(HubRoomLayout.HubFloorSignPlacement placement, Sprite floorSprite, Transform parent)
        {
            GameObject sign = CreateDecorBlock(
                placement.ObjectName,
                placement.Position,
                placement.BackdropScale,
                floorSprite,
                HubRoomLayout.SignBackdropColor,
                HubRoomLayout.SignSortingOrder,
                parent);

            CreateWorldTextObject(
                placement.ObjectName + "Label",
                sign.transform,
                placement.TextLocalPosition,
                placement.Content,
                HubRoomLayout.SignTextColor,
                placement.FontSize,
                HubRoomLayout.SignTextSortingOrder,
                labelScale: placement.TextScale,
                fontStyle: FontStyles.Bold,
                characterSpacing: placement.CharacterSpacing);
        }

        private static GameObject CreateFloorZone(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder)
        {
            return CreateDecorBlock(objectName, position, scale, sprite, color, sortingOrder);
        }

        private static void CreateFeaturePad(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color)
        {
            CreateDecorBlock(objectName, position, scale, sprite, color, 3);
        }

        /// <summary>
        /// 허브 전용 배경 아트를 레이어별 자식 오브젝트로 배치한다.
        /// </summary>
        private static GameObject CreateHubArtSprite(string objectName, Vector3 position, Sprite sprite, int sortingOrder, Transform parent)
        {
            if (sprite == null)
            {
                return null;
            }

            return CreateDecorBlock(objectName, position, Vector3.one, sprite, Color.white, sortingOrder, parent);
        }

        private static GameObject CreateDecorBlock(string objectName, Vector3 position, Vector3 scale, Sprite sprite, Color color, int sortingOrder, Transform parent = null)
        {
            GameObject go = new(objectName);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position;
            }
            else
            {
                go.transform.position = position;
            }

            go.transform.localScale = scale;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        /// <summary>
        /// 배경 아트 위에 상호작용만 남기고 싶은 허브 오브젝트는 렌더러와 월드 라벨을 숨긴다.
        /// 콜라이더와 상호작용 컴포넌트는 유지하므로 프롬프트와 기능은 그대로 동작한다.
        /// </summary>
        private static void HideWorldInteractionPresentation(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (TextMeshPro label in root.GetComponentsInChildren<TextMeshPro>(true))
            {
                label.gameObject.SetActive(false);
            }
        }

        private static ResourceLibrary CreateResources()
        {
            return new ResourceLibrary
            {
                Fish = CreateResourceAsset(ResourceDataRoot + "/resource-fish.asset", "fish", "생선", "바닷가에서 쉽게 얻을 수 있는 기본 재료입니다.", "바닷가", 10, ResourceRarity.Common),
                Shell = CreateResourceAsset(ResourceDataRoot + "/resource-shell.asset", "shell", "조개", "국물 요리에 쓰기 좋은 바닷가 재료입니다.", "바닷가", 12, ResourceRarity.Common),
                Seaweed = CreateResourceAsset(ResourceDataRoot + "/resource-seaweed.asset", "seaweed", "해초", "향이 좋은 해산 재료입니다.", "바닷가", 8, ResourceRarity.Common),
                Herb = CreateResourceAsset(ResourceDataRoot + "/resource-herb.asset", "herb", "약초", "깊은 숲에서 얻는 향이 짙은 약초입니다.", "깊은 숲", 14, ResourceRarity.Uncommon),
                Mushroom = CreateResourceAsset(ResourceDataRoot + "/resource-mushroom.asset", "mushroom", "버섯", "숲의 그늘 아래에서 자라는 식재료입니다.", "깊은 숲", 16, ResourceRarity.Uncommon),
                GlowMoss = CreateResourceAsset(ResourceDataRoot + "/resource-glow-moss.asset", "glow_moss", "발광 이끼", "폐광산 안쪽의 습한 벽면에서 자라는 희귀 식재료입니다.", "폐광산", 22, ResourceRarity.Rare),
                WindHerb = CreateResourceAsset(ResourceDataRoot + "/resource-wind-herb.asset", "wind_herb", "향초", "바람이 센 언덕에서만 자라는 고급 허브입니다.", "바람 언덕", 18, ResourceRarity.Rare)
            };
        }

        private static RecipeLibrary CreateRecipes(ResourceLibrary resources)
        {
            return new RecipeLibrary
            {
                SushiSet = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-sushi-set.asset",
                    "sushi_set",
                    "생선 한 접시",
                    "생선으로 빠르게 준비할 수 있는 기본 메뉴입니다.",
                    30,
                    1,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1)
                    }),
                SeafoodSoup = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-seafood-soup.asset",
                    "seafood_soup",
                    "해물탕",
                    "생선, 조개, 해초를 모두 넣은 고가 메뉴입니다.",
                    55,
                    2,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1),
                        new RecipeIngredientDefinition(resources.Shell, 1),
                        new RecipeIngredientDefinition(resources.Seaweed, 1)
                    }),
                HerbFishSoup = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-herb-fish-soup.asset",
                    "herb_fish_soup",
                    "약초 생선탕",
                    "바닷가 생선과 숲 약초를 넣어 향을 살린 메뉴입니다.",
                    42,
                    2,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1),
                        new RecipeIngredientDefinition(resources.Herb, 1)
                    }),
                ForestBasket = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-forest-basket.asset",
                    "forest_basket",
                    "숲 버섯 모둠",
                    "약초와 버섯을 엮어 만든 가벼운 숲 메뉴입니다.",
                    38,
                    1,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Herb, 1),
                        new RecipeIngredientDefinition(resources.Mushroom, 1)
                    }),
                GlowMossStew = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-glow-moss-stew.asset",
                    "glow_moss_stew",
                    "광채 해물탕",
                    "발광 이끼와 해초를 함께 넣어 진한 향을 낸 후반 메뉴입니다.",
                    68,
                    3,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Fish, 1),
                        new RecipeIngredientDefinition(resources.Seaweed, 1),
                        new RecipeIngredientDefinition(resources.GlowMoss, 1)
                    }),
                WindHerbSalad = CreateRecipeAsset(
                    RecipeDataRoot + "/recipe-wind-herb-salad.asset",
                    "wind_herb_salad",
                    "향초 해초 무침",
                    "바람 언덕 향초와 해초를 함께 버무린 고급 메뉴입니다.",
                    46,
                    2,
                    new[]
                    {
                        new RecipeIngredientDefinition(resources.Seaweed, 1),
                        new RecipeIngredientDefinition(resources.WindHerb, 1)
                    })
            };
        }

        private static SpriteLibrary CreateSprites()
        {
            return new SpriteLibrary
            {
                PlayerFront = CreatePlayerSprite(PlayerSpriteRoot + "/player-front.png", "image (2).png"),
                PlayerBack = CreatePlayerSprite(PlayerSpriteRoot + "/player-back.png", "image (1).png"),
                PlayerSide = CreatePlayerSprite(PlayerSpriteRoot + "/player-side.png", "image.png"),
                HubFloorBackground = LoadConfiguredSprite(HubFloorBackgroundSpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubFloorBackgroundSpritePath),
                HubWallBackground = LoadConfiguredSprite(HubWallBackgroundSpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubWallBackgroundSpritePath),
                HubFrontOutline = LoadConfiguredSprite(HubFrontOutlineSpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubFrontOutlineSpritePath),
                HubBar = LoadConfiguredSprite(HubBarSpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubBarSpritePath),
                HubTableUnlocked = LoadConfiguredSprite(HubTableUnlockedSpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubTableUnlockedSpritePath),
                HubUpgradeSlot = LoadConfiguredSprite(HubUpgradeSlotSpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubUpgradeSlotSpritePath),
                HubTodayMenuBg = LoadConfiguredSprite(HubTodayMenuBgSpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubTodayMenuBgSpritePath),
                HubTodayMenuItem1 = LoadConfiguredSprite(HubTodayMenuItem1SpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubTodayMenuItem1SpritePath),
                HubTodayMenuItem2 = LoadConfiguredSprite(HubTodayMenuItem2SpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubTodayMenuItem2SpritePath),
                HubTodayMenuItem3 = LoadConfiguredSprite(HubTodayMenuItem3SpritePath, 100f, new Vector2(0.5f, 0.5f), ResourceHubTodayMenuItem3SpritePath),
                Portal = CreateColorSprite(WorldSpriteRoot + "/world-portal.png", new Color(0.95f, 0.52f, 0.22f)),
                Selector = CreateColorSprite(WorldSpriteRoot + "/world-selector.png", new Color(0.98f, 0.84f, 0.23f)),
                Counter = CreateColorSprite(WorldSpriteRoot + "/world-counter.png", new Color(0.84f, 0.34f, 0.24f)),
                Fish = CreateColorSprite(GatherSpriteRoot + "/gather-fish.png", new Color(0.19f, 0.73f, 0.92f)),
                Shell = CreateColorSprite(GatherSpriteRoot + "/gather-shell.png", new Color(0.90f, 0.79f, 0.66f)),
                Seaweed = CreateColorSprite(GatherSpriteRoot + "/gather-seaweed.png", new Color(0.24f, 0.66f, 0.35f)),
                Herb = CreateColorSprite(GatherSpriteRoot + "/gather-herb.png", new Color(0.47f, 0.78f, 0.27f)),
                Mushroom = CreateColorSprite(GatherSpriteRoot + "/gather-mushroom.png", new Color(0.71f, 0.55f, 0.36f)),
                GlowMoss = CreateColorSprite(GatherSpriteRoot + "/gather-glow-moss.png", new Color(0.45f, 0.95f, 0.78f)),
                WindHerb = CreateColorSprite(GatherSpriteRoot + "/gather-wind-herb.png", new Color(0.79f, 0.93f, 0.61f)),
                Floor = CreateColorSprite(WorldSpriteRoot + "/world-floor.png", Color.white)
            };
        }

        /// <summary>
        /// 디자인 원본 UI PNG를 generated 스프라이트 경로로 복사하고 런타임 Resources 경로에도 같은 구조로 미러링한다.
        /// </summary>
        private static void CreateUiDesignSprites()
        {
            EnsureUiDesignSpriteAsset(
                CloseButtonDesignSourcePath,
                UiButtonSpriteRoot + "/close-button.png",
                ResourceUiButtonSpriteRoot + "/close-button.png",
                Vector4.zero);
            EnsureUiDesignSpriteAsset(
                HelpButtonDesignSourcePath,
                UiButtonSpriteRoot + "/help-button.png",
                ResourceUiButtonSpriteRoot + "/help-button.png",
                Vector4.zero);
            EnsureUiDesignSpriteAsset(
                SystemTextBoxDesignSourcePath,
                UiMessageBoxSpriteRoot + "/system-text-box.png",
                ResourceUiMessageBoxSpriteRoot + "/system-text-box.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                InteractionTextBoxDesignSourcePath,
                UiMessageBoxSpriteRoot + "/interaction-text-box.png",
                ResourceUiMessageBoxSpriteRoot + "/interaction-text-box.png",
                new Vector4(8f, 14f, 8f, 14f));
            EnsureUiDesignSpriteAsset(
                DarkOutlinePanelDesignSourcePath,
                UiPanelSpriteRoot + "/dark-outline-panel.png",
                ResourceUiPanelSpriteRoot + "/dark-outline-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                DarkOutlinePanelAltDesignSourcePath,
                UiPanelSpriteRoot + "/dark-outline-panel-alt.png",
                ResourceUiPanelSpriteRoot + "/dark-outline-panel-alt.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                DarkSolidPanelDesignSourcePath,
                UiPanelSpriteRoot + "/dark-solid-panel.png",
                ResourceUiPanelSpriteRoot + "/dark-solid-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                DarkThinOutlinePanelDesignSourcePath,
                UiPanelSpriteRoot + "/dark-thin-outline-panel.png",
                ResourceUiPanelSpriteRoot + "/dark-thin-outline-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                LightOutlinePanelDesignSourcePath,
                UiPanelSpriteRoot + "/light-outline-panel.png",
                ResourceUiPanelSpriteRoot + "/light-outline-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
            EnsureUiDesignSpriteAsset(
                LightSolidPanelDesignSourcePath,
                UiPanelSpriteRoot + "/light-solid-panel.png",
                ResourceUiPanelSpriteRoot + "/light-solid-panel.png",
                new Vector4(8f, 8f, 8f, 8f));
        }

        private static void EnsureUiDesignSpriteAsset(string sourceAssetPath, string generatedAssetPath, string resourceAssetPath, Vector4 border)
        {
            string sourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), sourceAssetPath);
            if (!File.Exists(sourceFullPath))
            {
                return;
            }

            string generatedFullPath = Path.Combine(Directory.GetCurrentDirectory(), generatedAssetPath);
            string resourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), resourceAssetPath);

            CopyFileIfDifferent(sourceFullPath, generatedFullPath);
            CopyFileIfDifferent(sourceFullPath, resourceFullPath);

            AssetDatabase.ImportAsset(generatedAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(resourceAssetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteAsset(generatedAssetPath, 100f, new Vector2(0.5f, 0.5f), border);
            ConfigureSpriteAsset(resourceAssetPath, 100f, new Vector2(0.5f, 0.5f), border);
        }

        private static Sprite CreatePlayerSprite(string assetPath, string sourceFileName)
        {
            string sourceFullPath = Path.Combine(Directory.GetCurrentDirectory(), "temperature", sourceFileName);
            string targetFullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            string resourceAssetPath = GetMirroredResourceSpriteAssetPath(assetPath);
            string resourceFullPath = string.IsNullOrWhiteSpace(resourceAssetPath)
                ? null
                : Path.Combine(Directory.GetCurrentDirectory(), resourceAssetPath);

            if (File.Exists(sourceFullPath))
            {
                CopyFileIfDifferent(sourceFullPath, targetFullPath);
                if (!string.IsNullOrWhiteSpace(resourceFullPath))
                {
                    CopyFileIfDifferent(sourceFullPath, resourceFullPath);
                }

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                if (!string.IsNullOrWhiteSpace(resourceAssetPath))
                {
                    AssetDatabase.ImportAsset(resourceAssetPath, ImportAssetOptions.ForceUpdate);
                }
            }
            else if (!string.IsNullOrWhiteSpace(resourceFullPath)
                && File.Exists(targetFullPath)
                && !File.Exists(resourceFullPath))
            {
                CopyFileIfDifferent(targetFullPath, resourceFullPath);
                AssetDatabase.ImportAsset(resourceAssetPath, ImportAssetOptions.ForceUpdate);
            }

            Sprite importedSprite = ConfigureSpriteAsset(assetPath, PlayerSpritePixelsPerUnit);
            if (!string.IsNullOrWhiteSpace(resourceAssetPath))
            {
                ConfigureSpriteAsset(resourceAssetPath, PlayerSpritePixelsPerUnit);
            }

            if (importedSprite != null)
            {
                return importedSprite;
            }

            return CreateColorSprite(assetPath, Color.white);
        }

        private static Sprite LoadConfiguredSprite(string assetPath, float pixelsPerUnit, Vector2 pivot, string pairedAssetPath = null)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(fullPath))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Sprite sprite = ConfigureSpriteAsset(assetPath, pixelsPerUnit, pivot);

            if (!string.IsNullOrWhiteSpace(pairedAssetPath))
            {
                string pairedFullPath = Path.Combine(Directory.GetCurrentDirectory(), pairedAssetPath);
                if (!File.Exists(pairedFullPath) && File.Exists(fullPath))
                {
                    CopyFileIfDifferent(fullPath, pairedFullPath);
                }

                if (File.Exists(pairedFullPath))
                {
                    AssetDatabase.ImportAsset(pairedAssetPath, ImportAssetOptions.ForceUpdate);
                    ConfigureSpriteAsset(pairedAssetPath, pixelsPerUnit, pivot);
                }
            }

            return sprite;
        }

        private static ResourceData CreateResourceAsset(string assetPath, string id, string displayName, string description, string regionTag, int sellPrice, ResourceRarity rarity)
        {
            ResourceData asset = AssetDatabase.LoadAssetAtPath<ResourceData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ResourceData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            EnsureMainObjectNameMatchesFileName(asset, assetPath);

            SerializedObject so = new(asset);
            so.FindProperty("resourceId").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("regionTag").stringValue = regionTag;
            so.FindProperty("baseSellPrice").intValue = sellPrice;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static RecipeData CreateRecipeAsset(
            string assetPath,
            string id,
            string displayName,
            string description,
            int sellPrice,
            int reputationDelta,
            IReadOnlyList<RecipeIngredientDefinition> ingredients)
        {
            RecipeData asset = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RecipeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            EnsureMainObjectNameMatchesFileName(asset, assetPath);

            SerializedObject so = new(asset);
            so.FindProperty("recipeId").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("sellPrice").intValue = sellPrice;
            so.FindProperty("reputationDelta").intValue = reputationDelta;

            SerializedProperty ingredientsProperty = so.FindProperty("ingredients");
            ingredientsProperty.arraySize = ingredients.Count;

            for (int index = 0; index < ingredients.Count; index++)
            {
                SerializedProperty item = ingredientsProperty.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("resource").objectReferenceValue = ingredients[index].Resource;
                item.FindPropertyRelative("amount").intValue = ingredients[index].Amount;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>
        /// generated 자산은 파일명과 메인 오브젝트 이름을 같게 유지해 저장 경고를 막는다.
        /// </summary>
        private static void EnsureMainObjectNameMatchesFileName(UnityEngine.Object asset, string assetPath)
        {
            if (asset == null || string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            string expectedName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrWhiteSpace(expectedName)
                || string.Equals(asset.name, expectedName, StringComparison.Ordinal))
            {
                return;
            }

            asset.name = expectedName;
            EditorUtility.SetDirty(asset);
        }

        private static Sprite CreateColorSprite(string assetPath, Color color)
        {
            EnsureColorSpriteAssetExists(assetPath, color);

            string mirroredResourceAssetPath = GetMirroredResourceSpriteAssetPath(assetPath);
            if (!string.IsNullOrWhiteSpace(mirroredResourceAssetPath))
            {
                EnsureColorSpriteAssetExists(mirroredResourceAssetPath, color);
                ConfigureSpriteAsset(mirroredResourceAssetPath, 100f);
            }

            return ConfigureSpriteAsset(assetPath, 100f);
        }

        /// <summary>
        /// generated 스프라이트는 Resources 폴더에도 같은 상대 경로로 한 벌 더 만들어 런타임 폴백에서 재사용한다.
        /// </summary>
        private static string GetMirroredResourceSpriteAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)
                || !assetPath.StartsWith(SpriteRoot + "/", StringComparison.Ordinal))
            {
                return null;
            }

            return assetPath.Replace(SpriteRoot, ResourceSpriteRoot);
        }

        /// <summary>
        /// 단색 스프라이트는 파일이 없을 때만 만들고, 이미 있으면 기존 GUID와 참조를 유지한다.
        /// </summary>
        private static void EnsureColorSpriteAssetExists(string assetPath, Color color)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return;
            }

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            Texture2D texture = new(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static Sprite ConfigureSpriteAsset(string assetPath, float pixelsPerUnit)
        {
            return ConfigureSpriteAsset(assetPath, pixelsPerUnit, new Vector2(0.5f, 0.08f), Vector4.zero);
        }

        private static Sprite ConfigureSpriteAsset(string assetPath, float pixelsPerUnit, Vector2 pivot)
        {
            return ConfigureSpriteAsset(assetPath, pixelsPerUnit, pivot, Vector4.zero);
        }

        private static Sprite ConfigureSpriteAsset(string assetPath, float pixelsPerUnit, Vector2 pivot, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 100;

            TextureImporterSettings spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = pivot;
            spriteSettings.spriteBorder = border;
            importer.SetTextureSettings(spriteSettings);

            ApplyUncompressedPlatformSettings(importer, "DefaultTexturePlatform");
            ApplyUncompressedPlatformSettings(importer, "Standalone");
            ApplyUncompressedPlatformSettings(importer, "WebGL");

            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ApplyUncompressedPlatformSettings(TextureImporter importer, string platformName)
        {
            TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platformName);
            platformSettings.name = platformName;
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = 2048;
            platformSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            platformSettings.compressionQuality = 100;
            platformSettings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(platformSettings);
        }

        private static BoxCollider2D CreateMovementBounds(string objectName, float width, float height)
        {
            GameObject boundsObject = new(objectName);
            BoxCollider2D bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(width, height);
            return bounds;
        }

        private static void AttachPlayerBoundsLimiter(GameObject player, Collider2D movementBounds)
        {
            if (player == null || movementBounds == null)
            {
                return;
            }

            PlayerBoundsLimiter limiter = player.AddComponent<PlayerBoundsLimiter>();
            SerializedObject so = new(limiter);
            so.FindProperty("movementBounds").objectReferenceValue = movementBounds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// TMP 컴포넌트 생성 전에 builder가 선호하는 기본 폰트를 다시 묶어 누락 경고를 막습니다.
        /// </summary>
        private static TMP_FontAsset EnsurePreferredTmpFontAsset()
        {
            TMP_FontAsset preferredFont = _generatedKoreanFont;

            if (preferredFont == null)
            {
                preferredFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRoot + "/maplestoryLightSdf.asset");
            }

            if (preferredFont != null && TMP_Settings.defaultFontAsset != preferredFont)
            {
                TMP_Settings.defaultFontAsset = preferredFont;

                if (TMP_Settings.instance != null)
                {
                    EditorUtility.SetDirty(TMP_Settings.instance);
                }
            }

            return preferredFont != null ? preferredFont : TMP_Settings.defaultFontAsset;
        }

        private static TMP_FontAsset EnsureHeadingTmpFontAsset()
        {
            TMP_FontAsset headingFont = _generatedHeadingFont;

            if (headingFont == null)
            {
                headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRoot + "/maplestoryBoldSdf.asset");
            }

            return headingFont != null ? headingFont : EnsurePreferredTmpFontAsset();
        }

        private static TMP_FontAsset CreateHeadingFontAsset()
        {
            return CreateProjectFontAsset(FontRoot + "/maplestoryBold.ttf", "maplestoryBoldSdf");
        }

        private static TMP_FontAsset CreateKoreanFontAsset()
        {
            return CreateProjectFontAsset(FontRoot + "/maplestoryLight.ttf", "maplestoryLightSdf");
        }

        private static TMP_FontAsset CreateProjectFontAsset(string importedFontPath, string fontAssetName)
        {
            AssetDatabase.ImportAsset(importedFontPath, ImportAssetOptions.ForceUpdate);

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(importedFontPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"프로젝트 폰트 '{importedFontPath}'를 불러오지 못해 기본 TMP 폰트를 사용합니다.");
                return TMP_Settings.defaultFontAsset;
            }

            string fontAssetPath = $"{FontRoot}/{fontAssetName}.asset";
            TMP_FontAsset existingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (existingFont != null)
            {
                NormalizeGeneratedFontAssetNames(existingFont, fontAssetName);
                existingFont.TryAddCharacters(CollectRequiredCharacters());
                EditorUtility.SetDirty(existingFont);

                if (existingFont.material != null)
                {
                    EditorUtility.SetDirty(existingFont.material);
                }

                if (existingFont.atlasTextures != null)
                {
                    foreach (Texture2D atlasTexture in existingFont.atlasTextures)
                    {
                        if (atlasTexture != null)
                        {
                            EditorUtility.SetDirty(atlasTexture);
                        }
                    }
                }

                return existingFont;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            if (fontAsset == null)
            {
                Debug.LogWarning($"TMP 폰트 자산 '{fontAssetName}' 생성에 실패해 기본 폰트를 사용합니다.");
                return TMP_Settings.defaultFontAsset;
            }

            fontAsset.name = fontAssetName;
            fontAsset.TryAddCharacters(CollectRequiredCharacters());
            AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

            if (fontAsset.atlasTextures != null)
            {
                for (int index = 0; index < fontAsset.atlasTextures.Length; index++)
                {
                    Texture2D atlasTexture = fontAsset.atlasTextures[index];
                    if (atlasTexture == null || AssetDatabase.Contains(atlasTexture))
                    {
                        continue;
                    }

                    atlasTexture.name = $"{fontAssetName} Atlas {index}";
                    AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
                }
            }

            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = $"{fontAssetName} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            NormalizeGeneratedFontAssetNames(fontAsset, fontAssetName);
            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.material != null)
            {
                EditorUtility.SetDirty(fontAsset.material);
            }

            if (fontAsset.atlasTextures != null)
            {
                foreach (Texture2D atlasTexture in fontAsset.atlasTextures)
                {
                    if (atlasTexture != null)
                    {
                        EditorUtility.SetDirty(atlasTexture);
                    }
                }
            }

            return fontAsset;
        }

        private static void NormalizeGeneratedFontAssetNames(TMP_FontAsset fontAsset, string fontAssetName)
        {
            if (fontAsset == null)
            {
                return;
            }

            fontAsset.name = fontAssetName;

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontAssetName}Material";
            }

            if (fontAsset.atlasTextures == null)
            {
                return;
            }

            for (int index = 0; index < fontAsset.atlasTextures.Length; index++)
            {
                Texture2D atlasTexture = fontAsset.atlasTextures[index];
                if (atlasTexture != null)
                {
                    atlasTexture.name = $"{fontAssetName}Atlas{index}";
                }
            }
        }

        private static string CollectRequiredCharacters()
        {
            // TMP 말줄임표 overflow는 U+2026을 직접 사용하므로 generated 폰트에 해당 글리프가 꼭 있어야 합니다.
            return "종구의 식당바닷가 깊은 숲 폐광산 바람 언덕 이동 방향키 상호작용 메뉴 변경 영업 시작 메뉴판 영업대 채집하기 생선 조개 해초 약초 버섯 향초 발광 이끼 인벤토리 비어 있음 골드 코인 평판 선택 가능 수량 결과 없음 메뉴를 고르고 영업을 시작하세요 선택된 메뉴가 없습니다 재료가 부족합니다 접시 판매 식당으로 이동 바닷가로 이동 깊은 숲으로 이동 폐광산으로 이동 바람 언덕으로 이동 식당 복귀 생선 한 접시 해물탕 약초 생선탕 숲 버섯 모둠 광채 해물탕 향초 해초 무침 늪지 강풍 랜턴 맡길 품목 꺼낼 품목 지름길 정상 어두운 업그레이드 재료 창고 닫기 열기 / : + [] WASD E …";
        }

        private static void CopyFileIfDifferent(string sourcePath, string targetPath)
        {
            string directoryPath = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (File.Exists(targetPath))
            {
                FileInfo sourceInfo = new(sourcePath);
                FileInfo targetInfo = new(targetPath);

                if (sourceInfo.Length == targetInfo.Length)
                {
                    return;
                }
            }

            File.Copy(sourcePath, targetPath, true);
        }

        /// <summary>
        /// generated 씬은 빌더가 항상 전체를 다시 쓰므로,
        /// 기존 손상 본문이 남아 저장을 막지 않게 `.unity` 파일만 지운 뒤 같은 경로에 다시 저장합니다.
        /// `.meta`는 유지해서 씬 GUID와 Build Settings 참조는 바꾸지 않습니다.
        /// </summary>
        private static void SaveGeneratedScene(string scenePath)
        {
            string directoryPath = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            PrototypeSceneHierarchyOrganizer.OrganizeSceneHierarchy(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), saveScene: false);

            if (File.Exists(scenePath))
            {
                File.Delete(scenePath);
            }

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneRoot + "/Hub.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/Beach.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/DeepForest.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/AbandonedMine.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/WindHill.unity", true)
            };
        }

        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static int RemoveMissingScriptsRecursive(GameObject target)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);

            foreach (Transform child in target.transform)
            {
                removed += RemoveMissingScriptsRecursive(child.gameObject);
            }

            return removed;
        }

        private readonly struct RecipeIngredientDefinition
        {
            public RecipeIngredientDefinition(ResourceData resource, int amount)
            {
                Resource = resource;
                Amount = amount;
            }

            public ResourceData Resource { get; }
            public int Amount { get; }
        }
    }
}
#endif

