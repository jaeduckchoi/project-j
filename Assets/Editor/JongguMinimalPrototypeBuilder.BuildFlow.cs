#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UI;
using UI.Controllers;
using UI.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if ENABLE_INPUT_SYSTEM
#endif

// ProjectEditor 네임스페이스
namespace Editor
{
    public static partial class JongguMinimalPrototypeBuilder
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051", Justification = "숨겨진 유지보수 경로로 보존합니다.")]
        private static void CleanMissingScriptsInActiveSceneForMaintenance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("누락 스크립트 정리는 플레이 모드를 종료한 뒤 실행하세요.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
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
                "generated 자산 동기화와 지원 씬 감사를 완료했습니다. 빌더는 더 이상 정적 월드 오브젝트를 생성하지 않으며, 씬에 저장한 값만 정본으로 유지합니다.",
                "OK");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051", Justification = "숨겨진 유지보수 경로로 보존합니다.")]
        private static void RunAssetSyncForMaintenance()
        {
            if (!EnsurePrototypeBuildToolReady("내부 생성 자산 동기화"))
            {
                return;
            }

            ExecutePrototypeBuild(runAudit: false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051", Justification = "숨겨진 유지보수 경로로 보존합니다.")]
        private static void RunGeneratedSceneAuditForMaintenance()
        {
            if (!EnsurePrototypeBuildToolReady("내부 생성 씬 감사"))
            {
                return;
            }

            RunGeneratedSceneAudit();
        }

        /// <summary>
        /// 메인 빌드와 내부 유지보수 경로가 공통으로 쓰는 실행 가능 조건을 한곳에서 맞춥니다.
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
        /// 메인 빌드는 generated 자산, Canvas 오버라이드, Build Settings, 감사만 비파괴로 동기화한다.
        /// 지원 씬의 정적 월드 오브젝트는 씬 저장값을 정본으로 두고 빌더가 생성하거나 재배치하지 않는다.
        /// </summary>
        private static void ExecutePrototypeBuild(bool runAudit)
        {
            SaveLoadedManagedScenesIfDirty();
            SyncBuildCanvasOverrides();
            SaveAndRefreshAssets();
            SyncManagedSceneFilesAndBuildSettings();
            SaveAndRefreshAssets();

            if (runAudit)
            {
                RunGeneratedSceneAudit();
            }
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
        /// 지원 씬은 더 이상 빌더가 생성하지 않고, 현재 저장소에 있는 씬 파일만 Build Settings와 감사 대상으로 유지한다.
        /// </summary>
        private static void SyncManagedSceneFilesAndBuildSettings()
        {
            SaveSceneIfLoadedAndDirty(SharedExplorationHudSourceScene);

            foreach (string scenePath in SharedExplorationHudTargetScenes)
            {
                SaveSceneIfLoadedAndDirty(scenePath);
            }

            UpdateBuildSettings();
        }

        /// <summary>
        /// 과거 재생성 유지보수 엔트리는 현재 sync + audit 동작만 다시 실행한다.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051", Justification = "숨겨진 유지보수 경로로 보존합니다.")]
        private static void ForceRebuildAllManagedScenesForMaintenance()
        {
            ExecutePrototypeBuild(runAudit: true);
        }

        /// <summary>
        /// 생성 씬 감사 호출을 한곳으로 모아 내부 유지보수 경로와 메인 빌드가 같은 경로를 쓰게 합니다.
        /// </summary>
        private static void RunGeneratedSceneAudit()
        {
            PrototypeSceneAudit.AuditGeneratedScenes();
        }

        /// <summary>
        /// 지원 씬을 에디터에서 직접 수정한 경우 메인 빌드 전에 dirty 상태를 먼저 저장해,
        /// 씬 직렬화에 넣어 둔 정적 값이 다음 동기화와 감사에서 그대로 반영되게 합니다.
        /// </summary>
        private static void SaveLoadedManagedScenesIfDirty()
        {
            foreach (string scenePath in ManagedScenePaths)
            {
                SaveSceneIfLoadedAndDirty(scenePath);
            }
        }

        private static void SaveAndRefreshAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = ManagedScenePaths
                .Select(scenePath => new EditorBuildSettingsScene(scenePath, true))
                .ToArray();
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

        private static int RemoveMissingScriptsInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                removed += RemoveMissingScriptsRecursive(root);
            }

            return removed;
        }

        /// <summary>
        /// 빌더 미리보기는 현재 메뉴로 노출하지 않고 내부 정리 경로에서만 사용한다.
        /// 정적 월드 오브젝트는 생성하거나 재배치하지 않는다.
        /// </summary>
        public static void ApplyOpenSceneBuilderPreview()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("열린 씬 빌더 미리보기는 플레이 모드를 종료한 뒤 실행하세요.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
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

            PrototypeSceneHierarchyOrganizer.OrganizeSceneHierarchy(activeScene, saveScene: false);
            ApplySceneUiPreviewIfAvailable();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();

            Debug.Log($"열린 씬 '{activeScene.name}'에 비파괴 빌더 미리보기를 적용했습니다.");
        }

        private static bool IsSupportedBuilderPreviewScene(string sceneName)
        {
            return sceneName is "Hub" or "Beach" or "DeepForest" or "AbandonedMine" or "WindHill";
        }

        /// <summary>
        /// 열린 씬 프리뷰는 월드 오브젝트를 건드리지 않고 UI 프리뷰만 다시 적용한다.
        /// </summary>
        private static void ApplySceneUiPreviewIfAvailable()
        {
            PrototypeUIDesignController controller = Object.FindFirstObjectByType<PrototypeUIDesignController>();
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

        internal static bool ShouldAutoSyncCanvasOnSceneSave(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path))
            {
                return false;
            }

            return IsAutoSyncCanvasScene(scene.path);
        }

        internal static bool TryAutoSyncCanvasOnSceneSaved(Scene scene, out string message)
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
            if (!TryDescribeManagedSceneFileIssue(scenePath, out message))
            {
                return false;
            }

            Scene sourceScene = SceneManager.GetSceneByPath(scenePath);
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
            Scene activeScene = SceneManager.GetActiveScene();
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

        internal static bool TryDescribeManagedSceneFileIssue(string scenePath, out string issue)
        {
            issue = null;

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                issue = "씬 경로가 비어 있습니다.";
                return false;
            }

            if (!File.Exists(scenePath))
            {
                issue = $"씬 파일을 찾지 못했습니다: {scenePath}";
                return false;
            }

            FileInfo sceneFile = new(scenePath);
            if (sceneFile.Length <= 0)
            {
                issue = $"씬 파일이 비어 있습니다: {scenePath}";
                return false;
            }

            try
            {
                string firstLine = File.ReadLines(scenePath).FirstOrDefault();
                if (!string.Equals(firstLine, "%YAML 1.1", StringComparison.Ordinal))
                {
                    issue = $"씬 YAML 헤더가 올바르지 않습니다: {scenePath}";
                    return false;
                }
            }
            catch (Exception exception)
            {
                issue = $"씬 파일을 검사하지 못했습니다: {scenePath} ({exception.Message})";
                return false;
            }

            return true;
        }

        private static void SaveSceneIfLoadedAndDirty(string scenePath)
        {
            Scene loadedScene = SceneManager.GetSceneByPath(scenePath);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded || !loadedScene.isDirty)
            {
                return;
            }

            int removedMissingScripts = RemoveMissingScriptsInScene(loadedScene);
            if (removedMissingScripts > 0)
            {
                Debug.LogWarning($"'{loadedScene.name}' 열린 생성 씬에서 누락 스크립트 {removedMissingScripts}개를 저장 전에 정리했습니다.");
            }

            EditorSceneManager.SaveScene(loadedScene);
        }

        /// <summary>
        /// 저장한 기준 씬 Canvas를 다른 지원 씬 Canvas에 직접 반영합니다.
        /// 같은 이름 관리 대상 UI가 빠져 있으면 복제하고, hierarchy/레이아웃 오버라이드를 다시 적용합니다.
        /// </summary>
        private static void SyncCanvasBetweenScenes(string sourceScenePath, string targetScenePath)
        {
            if (!TryDescribeManagedSceneFileIssue(sourceScenePath, out _)
                || !TryDescribeManagedSceneFileIssue(targetScenePath, out _))
            {
                return;
            }

            if (string.Equals(sourceScenePath, targetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Scene sourceScene = SceneManager.GetSceneByPath(sourceScenePath);
            Scene targetScene = SceneManager.GetSceneByPath(targetScenePath);
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

        private static GameObject FindSceneCanvasRoot(Scene scene)
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
                GameObject clonedObject = Object.Instantiate(sourceTransform.gameObject, targetCanvas.transform);
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

            Object.DestroyImmediate(current.gameObject);
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

            Object.DestroyImmediate(current.gameObject);
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
            so.FindProperty("guideText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "GuideText");
            so.FindProperty("resultText").objectReferenceValue = FindNamedComponent<TextMeshProUGUI>(canvasObject.transform, "RestaurantResultText");
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
                    Object.DestroyImmediate(targets[index]);
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
    }
}
#endif
