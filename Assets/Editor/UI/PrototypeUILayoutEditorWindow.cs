using System.Collections.Generic;
using UI;
using UI.Controllers;
using UI.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.UI
{
    /// <summary>
    /// 현재 활성 씬의 managed UI를 한눈에 확인하고,
    /// 에디터 프리뷰 적용과 레이아웃 저장 흐름을 같은 창에서 다루는 보조 도구입니다.
    /// </summary>
    public sealed class PrototypeUILayoutEditorWindow : EditorWindow
    {
        private readonly List<ManagedUiRow> managedRows = new();
        private Scene activeScene;
        private UIManager uiManager;
        private PrototypeUIDesignController designController;
        private Vector2 scrollPosition;
        private string statusMessage;
        private MessageType statusType = MessageType.None;

        [MenuItem("Window/Jonggu Restaurant/UI Layout Editor")]
        public static void Open()
        {
            GetWindow<PrototypeUILayoutEditorWindow>("UI Layout Editor");
        }

        private void OnEnable()
        {
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
            RefreshContextAndRows();
        }

        private void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= HandleActiveSceneChanged;
        }

        private void OnHierarchyChange()
        {
            RefreshContextAndRows();
            Repaint();
        }

        private void OnSelectionChange()
        {
            ResolveContext();
            Repaint();
        }

        private void OnGUI()
        {
            DrawHeader();

            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                EditorGUILayout.HelpBox("활성 씬을 찾지 못했습니다. 편집할 씬을 열고 활성 씬으로 지정하세요.", MessageType.Warning);
                return;
            }

            DrawToolbar();
            DrawContextSummary();
            DrawStatus();
            DrawManagedRows();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("UI Layout Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "현재 활성 씬의 관리 UI를 선택하고, Unity Scene/Inspector에서 수정한 뒤 기존 UI 설정 저장 흐름으로 반영합니다.",
                MessageType.Info);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(uiManager == null))
                {
                    if (GUILayout.Button("Apply Preview"))
                    {
                        ApplyPreview();
                    }
                }

                if (GUILayout.Button("Refresh List"))
                {
                    RefreshContextAndRows();
                    SetStatus("관리 UI 목록을 새로 고쳤습니다.", MessageType.Info);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Current Scene UI Settings"))
                {
                    SaveCurrentSceneUiSettings();
                }

                using (new EditorGUI.DisabledScope(designController == null))
                {
                    if (GUILayout.Button("Clear Preview"))
                    {
                        ClearPreview();
                    }
                }
            }
        }

        private void DrawContextSummary()
        {
            string uiManagerName = uiManager != null ? uiManager.name : "Missing";
            string controllerState = designController != null ? "Ready" : "Missing";

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene", activeScene.name);
            EditorGUILayout.LabelField("UIManager", uiManagerName);
            EditorGUILayout.LabelField("Design Controller", controllerState);

            if (uiManager == null)
            {
                EditorGUILayout.HelpBox("현재 활성 씬에서 UIManager를 찾지 못했습니다.", MessageType.Warning);
            }
            else if (designController == null)
            {
                EditorGUILayout.HelpBox("디자인 컨트롤러가 없습니다. Apply Preview를 누르면 UIManager 오브젝트에 추가합니다.", MessageType.None);
            }
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(statusMessage))
            {
                return;
            }

            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        private void DrawManagedRows()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(BuildManagedRowsSummary(), EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Object", EditorStyles.miniBoldLabel, GUILayout.MinWidth(180));
                EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel, GUILayout.Width(110));
                EditorGUILayout.LabelField("Target", EditorStyles.miniBoldLabel, GUILayout.MinWidth(140));
                GUILayout.Space(72);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int index = 0; index < managedRows.Count; index++)
            {
                DrawManagedRow(managedRows[index]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawManagedRow(ManagedUiRow row)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(row.ObjectName, GUILayout.MinWidth(180));
                EditorGUILayout.LabelField(row.StatusText, GUILayout.Width(110));

                using (new EditorGUI.DisabledScope(row.Target == null))
                {
                    EditorGUILayout.ObjectField(row.Target, typeof(RectTransform), true, GUILayout.MinWidth(140));

                    if (GUILayout.Button("Select", GUILayout.Width(64)))
                    {
                        SelectRow(row);
                    }
                }
            }
        }

        private string BuildManagedRowsSummary()
        {
            int readyCount = 0;
            int missingCount = 0;
            int duplicateCount = 0;
            for (int index = 0; index < managedRows.Count; index++)
            {
                ManagedUiRow row = managedRows[index];
                if (row.MatchCount == 0)
                {
                    missingCount++;
                }
                else if (row.MatchCount > 1)
                {
                    duplicateCount++;
                }
                else
                {
                    readyCount++;
                }
            }

            return $"Managed UI ({readyCount} Ready / {missingCount} Missing / {duplicateCount} Duplicate)";
        }

        private void ApplyPreview()
        {
            if (uiManager == null)
            {
                SetStatus("UIManager가 없어 에디터 프리뷰를 적용할 수 없습니다.", MessageType.Warning);
                return;
            }

            EnsureDesignController();
            if (designController == null)
            {
                SetStatus("디자인 컨트롤러를 만들지 못했습니다.", MessageType.Warning);
                return;
            }

            designController.ApplyEditorPreviewInEditor();
            MarkSceneDirty(designController);
            RefreshContextAndRows();
            SetStatus("에디터 UI 프리뷰를 적용했습니다.", MessageType.Info);
        }

        private void SaveCurrentSceneUiSettings()
        {
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                SetStatus("열려 있는 씬이 없어 Canvas UI 값을 저장할 수 없습니다.", MessageType.Warning);
                return;
            }

            bool success = PrototypeUISceneLayoutCatalog.TryOverlayCanvasLayoutsFromScene(activeScene, out string message);
            if (success)
            {
                Debug.Log(message);
                SetStatus(message, MessageType.Info);
            }
            else
            {
                Debug.LogWarning(message);
                SetStatus(message, MessageType.Warning);
            }

            RefreshContextAndRows();
        }

        private void ClearPreview()
        {
            if (designController == null)
            {
                SetStatus("정리할 디자인 컨트롤러가 없습니다.", MessageType.Warning);
                return;
            }

            designController.ClearEditorPreviewInEditor();
            MarkSceneDirty(designController);
            RefreshContextAndRows();
            SetStatus("에디터 UI 프리뷰를 정리했습니다.", MessageType.Info);
        }

        private void SelectRow(ManagedUiRow row)
        {
            if (row == null || row.Target == null)
            {
                return;
            }

            Selection.activeObject = row.Target;
            EditorGUIUtility.PingObject(row.Target.gameObject);
            FrameSceneViewSelection();
            SetStatus($"{row.ObjectName} RectTransform을 선택했습니다.", MessageType.Info);
        }

        private void EnsureDesignController()
        {
            if (uiManager == null)
            {
                return;
            }

            designController = uiManager.GetComponent<PrototypeUIDesignController>();
            if (designController != null)
            {
                designController.Configure(uiManager);
                return;
            }

            designController = Undo.AddComponent<PrototypeUIDesignController>(uiManager.gameObject);
            designController.Configure(uiManager);
            MarkSceneDirty(designController);
        }

        private void RefreshContextAndRows()
        {
            ResolveContext();
            RefreshRows();
        }

        private void ResolveContext()
        {
            activeScene = SceneManager.GetActiveScene();
            uiManager = null;
            designController = null;

            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            uiManager = ResolveSelectedUiManager(activeScene) ?? FindFirstUiManager(activeScene);
            if (uiManager != null)
            {
                designController = uiManager.GetComponent<PrototypeUIDesignController>();
            }
        }

        private void RefreshRows()
        {
            managedRows.Clear();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            Dictionary<string, List<RectTransform>> rectsByName = BuildRectLookup(activeScene);
            List<string> managedNames = new(PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(IsHubScene(activeScene)));
            managedNames.Sort(string.CompareOrdinal);

            for (int index = 0; index < managedNames.Count; index++)
            {
                string objectName = managedNames[index];
                if (!rectsByName.TryGetValue(objectName, out List<RectTransform> matches))
                {
                    matches = new List<RectTransform>();
                }

                managedRows.Add(new ManagedUiRow(objectName, matches));
            }
        }

        private static Dictionary<string, List<RectTransform>> BuildRectLookup(Scene scene)
        {
            Dictionary<string, List<RectTransform>> rectsByName = new(System.StringComparer.Ordinal);
            HashSet<int> visitedInstanceIds = new();

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Canvas[] canvases = roots[rootIndex].GetComponentsInChildren<Canvas>(true);
                for (int canvasIndex = 0; canvasIndex < canvases.Length; canvasIndex++)
                {
                    Canvas canvas = canvases[canvasIndex];
                    if (canvas == null)
                    {
                        continue;
                    }

                    RectTransform[] rectTransforms = canvas.GetComponentsInChildren<RectTransform>(true);
                    for (int rectIndex = 0; rectIndex < rectTransforms.Length; rectIndex++)
                    {
                        RectTransform rectTransform = rectTransforms[rectIndex];
                        if (rectTransform == null || string.IsNullOrWhiteSpace(rectTransform.name))
                        {
                            continue;
                        }

                        if (!visitedInstanceIds.Add(rectTransform.GetInstanceID()))
                        {
                            continue;
                        }

                        if (!rectsByName.TryGetValue(rectTransform.name, out List<RectTransform> matches))
                        {
                            matches = new List<RectTransform>();
                            rectsByName.Add(rectTransform.name, matches);
                        }

                        matches.Add(rectTransform);
                    }
                }
            }

            return rectsByName;
        }

        private static UIManager ResolveSelectedUiManager(Scene scene)
        {
            GameObject selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject == null
                && Selection.activeTransform != null)
            {
                selectedGameObject = Selection.activeTransform.gameObject;
            }

            if (selectedGameObject == null || selectedGameObject.scene.handle != scene.handle)
            {
                return null;
            }

            UIManager selectedManager = selectedGameObject.GetComponentInParent<UIManager>(true);
            if (selectedManager != null)
            {
                return selectedManager;
            }

            PrototypeUIDesignController selectedController = selectedGameObject.GetComponentInParent<PrototypeUIDesignController>(true);
            return selectedController != null ? selectedController.GetComponent<UIManager>() : null;
        }

        private static UIManager FindFirstUiManager(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                UIManager[] managers = roots[index].GetComponentsInChildren<UIManager>(true);
                if (managers.Length > 0)
                {
                    return managers[0];
                }
            }

            return null;
        }

        private static void FrameSceneViewSelection()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null && SceneView.sceneViews.Count > 0)
            {
                sceneView = SceneView.sceneViews[0] as SceneView;
            }

            if (sceneView != null)
            {
                sceneView.FrameSelected();
            }
        }

        private static bool IsHubScene(Scene scene)
        {
            return string.Equals(scene.name, "Hub", System.StringComparison.Ordinal);
        }

        private static void MarkSceneDirty(PrototypeUIDesignController controller)
        {
            if (controller == null)
            {
                return;
            }

            EditorUtility.SetDirty(controller);
            MarkSceneDirty(controller.gameObject);
        }

        private static void MarkSceneDirty(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            EditorUtility.SetDirty(targetObject);
            if (targetObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(targetObject.scene);
            }
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            RefreshContextAndRows();
            Repaint();
        }

        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusType = messageType;
            Repaint();
        }

        private sealed class ManagedUiRow
        {
            private readonly List<RectTransform> matches;

            public ManagedUiRow(string objectName, List<RectTransform> matches)
            {
                ObjectName = objectName;
                this.matches = matches ?? new List<RectTransform>();
            }

            public string ObjectName { get; }
            public int MatchCount => matches.Count;
            public RectTransform Target => matches.Count > 0 ? matches[0] : null;

            public string StatusText
            {
                get
                {
                    if (matches.Count == 0)
                    {
                        return "Missing";
                    }

                    return matches.Count == 1 ? "Ready" : $"Duplicate ({matches.Count})";
                }
            }
        }
    }
}
