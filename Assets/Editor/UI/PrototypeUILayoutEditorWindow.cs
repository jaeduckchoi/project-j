using System;
using System.Collections.Generic;
using UI;
using UI.Controllers;
using UI.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Editor.UI
{
    /// <summary>
    /// 현재 활성 씬의 managed UI를 한눈에 확인하고,
    /// 에디터 프리뷰 적용과 레이아웃 저장 흐름을 같은 창에서 다루는 보조 도구입니다.
    /// </summary>
    public sealed class PrototypeUILayoutEditorWindow : EditorWindow
    {
        private const float CanvasReferenceWidth = 1920f;
        private const float CanvasReferenceHeight = 1080f;
        private const float TreeIndentWidth = 18f;
        private const string HudRootName = "HUDRoot";
        private const string PopupRootName = "PopupRoot";
        private static readonly string[] WorkModeLabels = { "드래프트 작업", "런타임 확인", "오브젝트 목록" };
        private static readonly Color CanvasFrameColor = new(0.2f, 0.72f, 1f, 0.92f);
        private static readonly Color DraftFrameColor = new(0.46f, 1f, 0.62f, 0.95f);
        private static readonly Color FocusFrameColor = new(1f, 0.72f, 0.16f, 0.95f);

        private readonly List<ManagedUiNode> managedTreeRoots = new();
        private readonly Dictionary<string, bool> managedNodeFoldouts = new(StringComparer.Ordinal);
        private Scene activeScene;
        private UIManager uiManager;
        private PrototypeUIDesignController designController;
        private Vector2 scrollPosition;
        private string statusMessage;
        private string selectedManagedPath;
        private MessageType statusType = MessageType.None;
        private WorkMode workMode = WorkMode.Objects;
        private bool showDesignFrame = true;
        private DesignFocusTarget designFocusTarget = DesignFocusTarget.WholeUi;

        [MenuItem("Window/Jonggu Restaurant/UI 레이아웃 편집기")]
        public static void Open()
        {
            GetWindow<PrototypeUILayoutEditorWindow>("UI 레이아웃 편집기");
        }

        private void OnEnable()
        {
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
            SceneView.duringSceneGui += DrawSceneViewDesignFrame;
            RefreshContextAndRows();
        }

        private void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= HandleActiveSceneChanged;
            SceneView.duringSceneGui -= DrawSceneViewDesignFrame;
        }

        private void OnHierarchyChange()
        {
            RefreshContextAndRows();
            Repaint();
        }

        private void OnSelectionChange()
        {
            ResolveContext();
            UpdateSelectedManagedPath();
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
            if (workMode == WorkMode.Objects)
            {
                DrawManagedRows();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("UI 레이아웃 편집기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "상단에는 핵심 실행 버튼만 두고, 나머지 기능은 더보기 메뉴에서 실행합니다. 실제 관리 오브젝트 이름은 런타임 저장 키이므로 변경하지 않습니다.",
                MessageType.Info);
        }
        private void DrawToolbar()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("작업 모드", GUILayout.Width(72));
                workMode = (WorkMode)EditorGUILayout.Popup((int)workMode, WorkModeLabels);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!CanRunPrimaryAction()))
                {
                    if (GUILayout.Button(GetPrimaryActionLabel(), GUILayout.Height(28)))
                    {
                        RunPrimaryAction();
                    }
                }

                using (new EditorGUI.DisabledScope(!CanFrameSelectedForCurrentMode()))
                {
                    if (GUILayout.Button("선택 항목 맞춤", GUILayout.Height(28)))
                    {
                        FrameSelectedForCurrentMode();
                    }
                }

                if (GUILayout.Button("더보기", GUILayout.Width(88), GUILayout.Height(28)))
                {
                    ShowMoreMenu();
                }
            }

            DrawWorkModeSummary();
        }

        private void DrawWorkModeSummary()
        {
            RectTransform draftRoot = PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene);
            switch (workMode)
            {
                case WorkMode.Draft:
                    EditorGUILayout.LabelField("드래프트 Canvas", draftRoot != null ? "준비됨" : "없음");
                    EditorGUILayout.HelpBox(
                        "드래프트 작업대에서 새 UI 배치를 실험합니다. 런타임 반영은 더보기 > 드래프트 > 런타임에 반영에서만 실행됩니다.",
                        MessageType.None);
                    break;
                case WorkMode.Runtime:
                    EditorGUILayout.HelpBox(
                        "실제 Canvas 미리보기와 Scene 뷰 맞춤을 확인합니다. 저장은 오브젝트 목록 모드의 현재 씬 UI 저장을 사용합니다.",
                        MessageType.None);
                    break;
                case WorkMode.Objects:
                    EditorGUILayout.HelpBox(
                        "관리 UI를 상하위 트리로 확인하고 선택합니다. 트리 표시명은 경로 안내용이며 실제 GameObject 이름은 유지합니다.",
                        MessageType.None);
                    break;
            }
        }

        private string GetPrimaryActionLabel()
        {
            switch (workMode)
            {
                case WorkMode.Draft:
                    return PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene) == null
                        ? "드래프트 만들기"
                        : "드래프트 저장";
                case WorkMode.Runtime:
                    return "미리보기 적용";
                case WorkMode.Objects:
                    return "현재 씬 UI 저장";
                default:
                    return "실행";
            }
        }

        private bool CanRunPrimaryAction()
        {
            switch (workMode)
            {
                case WorkMode.Draft:
                    return activeScene.IsValid() && activeScene.isLoaded;
                case WorkMode.Runtime:
                    return uiManager != null;
                case WorkMode.Objects:
                    return activeScene.IsValid() && activeScene.isLoaded;
                default:
                    return false;
            }
        }

        private void RunPrimaryAction()
        {
            switch (workMode)
            {
                case WorkMode.Draft:
                    RectTransform draftRoot = PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene);
                    if (draftRoot == null)
                    {
                        CreateBlankDraft();
                    }
                    else
                    {
                        SaveDraft(draftRoot);
                    }
                    break;
                case WorkMode.Runtime:
                    ApplyPreview();
                    break;
                case WorkMode.Objects:
                    SaveCurrentSceneUiSettings();
                    break;
            }
        }

        private bool CanFrameSelectedForCurrentMode()
        {
            if (workMode == WorkMode.Draft)
            {
                return PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene) != null;
            }

            return uiManager != null;
        }

        private void FrameSelectedForCurrentMode()
        {
            if (workMode == WorkMode.Draft)
            {
                FrameDraftTarget(DraftFocusTarget.Selected);
                return;
            }

            if (TryResolveSelectedUiRect(out _))
            {
                FrameDesignFocus(DesignFocusTarget.Selected);
                return;
            }

            RectTransform draftRoot = PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene);
            if (draftRoot != null && TryResolveSelectedDraftRect(draftRoot, out _))
            {
                FrameDraftTarget(DraftFocusTarget.Selected);
                return;
            }

            SetStatus("맞출 UI RectTransform을 먼저 선택하세요.", MessageType.Warning);
        }

        private void ShowMoreMenu()
        {
            RectTransform draftRoot = PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene);
            GenericMenu menu = new();
            AddMenuItem(menu, "드래프트/빈 드래프트 만들기", activeScene.IsValid() && activeScene.isLoaded, CreateBlankDraft);
            AddMenuItem(menu, "드래프트/런타임 값 불러오기", activeScene.IsValid() && activeScene.isLoaded, LoadRuntimeAsDraft);
            AddMenuItem(menu, "드래프트/드래프트 저장", draftRoot != null, () => SaveDraft(draftRoot));
            AddMenuItem(menu, "드래프트/런타임에 반영", draftRoot != null, () => ApplyDraftToRuntime(draftRoot));
            AddMenuItem(menu, "드래프트/드래프트 삭제", draftRoot != null, DiscardDraft);
            menu.AddSeparator(string.Empty);
            AddMenuItem(menu, "보기 맞춤/작업대 맞춤", draftRoot != null, () => FrameDraftTarget(DraftFocusTarget.Board));
            AddMenuItem(menu, "보기 맞춤/전체 UI 맞춤", uiManager != null, () => FrameDesignFocus(DesignFocusTarget.WholeUi));
            AddMenuItem(menu, "보기 맞춤/HUD 맞춤", CanFrameCurrentHudOrPopup(), FrameHudForCurrentMode);
            AddMenuItem(menu, "보기 맞춤/팝업 맞춤", CanFrameCurrentHudOrPopup(), FramePopupForCurrentMode);
            menu.AddSeparator(string.Empty);
            AddMenuItem(menu, "런타임/미리보기 정리", designController != null, ClearPreview);
            AddMenuItem(menu, "런타임/목록 새로고침", activeScene.IsValid() && activeScene.isLoaded, RefreshListFromMenu);
            AddMenuItem(menu, "런타임/현재 씬 UI 저장", activeScene.IsValid() && activeScene.isLoaded, SaveCurrentSceneUiSettings);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("표시/1920x1080 기준 프레임 표시"), showDesignFrame, ToggleDesignFrame);
            menu.ShowAsContext();
        }

        private static void AddMenuItem(GenericMenu menu, string path, bool enabled, GenericMenu.MenuFunction action)
        {
            if (enabled)
            {
                menu.AddItem(new GUIContent(path), false, action);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(path));
            }
        }

        private void RefreshListFromMenu()
        {
            RefreshContextAndRows();
            SetStatus("관리 UI 목록을 새로 고쳤습니다.", MessageType.Info);
        }

        private void ToggleDesignFrame()
        {
            showDesignFrame = !showDesignFrame;
            SceneView.RepaintAll();
        }

        private bool CanFrameCurrentHudOrPopup()
        {
            return workMode == WorkMode.Draft
                ? PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene) != null
                : uiManager != null;
        }

        private void FrameHudForCurrentMode()
        {
            if (workMode == WorkMode.Draft)
            {
                FrameDraftTarget(DraftFocusTarget.Hud);
            }
            else
            {
                FrameDesignFocus(DesignFocusTarget.Hud);
            }
        }

        private void FramePopupForCurrentMode()
        {
            if (workMode == WorkMode.Draft)
            {
                FrameDraftTarget(DraftFocusTarget.Popup);
            }
            else
            {
                FrameDesignFocus(DesignFocusTarget.Popup);
            }
        }

        private void DrawContextSummary()
        {
            string uiManagerName = uiManager != null ? uiManager.name : "없음";
            string controllerState = designController != null ? "준비됨" : "없음";

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("활성 씬", activeScene.name);
            EditorGUILayout.LabelField("UIManager", uiManagerName);
            EditorGUILayout.LabelField("디자인 컨트롤러", controllerState);

            if (uiManager == null)
            {
                EditorGUILayout.HelpBox("현재 활성 씬에서 UIManager를 찾지 못했습니다.", MessageType.Warning);
            }
            else if (designController == null)
            {
                EditorGUILayout.HelpBox("디자인 컨트롤러가 없습니다. 미리보기 적용을 누르면 UIManager 오브젝트에 추가합니다.", MessageType.None);
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
                EditorGUILayout.LabelField("오브젝트", EditorStyles.miniBoldLabel, GUILayout.MinWidth(220));
                EditorGUILayout.LabelField("상태", EditorStyles.miniBoldLabel, GUILayout.Width(84));
                EditorGUILayout.LabelField("연결 대상", EditorStyles.miniBoldLabel, GUILayout.MinWidth(140));
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int index = 0; index < managedTreeRoots.Count; index++)
            {
                DrawManagedNode(managedTreeRoots[index]);
            }

            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(selectedManagedPath))
            {
                EditorGUILayout.HelpBox("선택 경로: " + selectedManagedPath, MessageType.None);
            }
        }

        private void DrawManagedNode(ManagedUiNode node)
        {
            if (node == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(node.Depth * TreeIndentWidth);

                if (node.Children.Count > 0)
                {
                    bool expanded = IsNodeExpanded(node);
                    bool nextExpanded = EditorGUILayout.Foldout(expanded, string.Empty, true, EditorStyles.foldout);
                    if (expanded != nextExpanded)
                    {
                        managedNodeFoldouts[node.ObjectName] = nextExpanded;
                    }
                }
                else
                {
                    GUILayout.Space(14);
                }

                using (new EditorGUI.DisabledScope(node.Target == null))
                {
                    if (GUILayout.Button(node.ObjectName, EditorStyles.label, GUILayout.MinWidth(190)))
                    {
                        SelectNode(node);
                    }
                }

                EditorGUILayout.LabelField(node.StatusText, GUILayout.Width(84));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(node.Target, typeof(RectTransform), true, GUILayout.MinWidth(140));
                }
            }

            if (node.Children.Count == 0 || !IsNodeExpanded(node))
            {
                return;
            }

            for (int index = 0; index < node.Children.Count; index++)
            {
                DrawManagedNode(node.Children[index]);
            }
        }

        private bool IsNodeExpanded(ManagedUiNode node)
        {
            if (node == null || node.Children.Count == 0)
            {
                return false;
            }

            if (managedNodeFoldouts.TryGetValue(node.ObjectName, out bool expanded))
            {
                return expanded;
            }

            return node.Depth < 2;
        }

        private string BuildManagedRowsSummary()
        {
            int readyCount = 0;
            int missingCount = 0;
            int duplicateCount = 0;
            CountManagedNodes(managedTreeRoots, ref readyCount, ref missingCount, ref duplicateCount);
            return $"관리 UI ({readyCount} 정상 / {missingCount} 없음 / {duplicateCount} 중복)";
        }

        private static void CountManagedNodes(
            IReadOnlyList<ManagedUiNode> nodes,
            ref int readyCount,
            ref int missingCount,
            ref int duplicateCount)
        {
            if (nodes == null)
            {
                return;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                ManagedUiNode node = nodes[index];
                if (node.HasDuplicate)
                {
                    duplicateCount++;
                }
                else if (node.Target == null)
                {
                    missingCount++;
                }
                else
                {
                    readyCount++;
                }

                CountManagedNodes(node.Children, ref readyCount, ref missingCount, ref duplicateCount);
            }
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

        private void CreateBlankDraft()
        {
            if (PrototypeUIDesignDraftWorkspace.CreateBlankDraft(activeScene, IsHubScene(activeScene), out RectTransform draftRoot, out string message))
            {
                MarkSceneDirty(draftRoot.gameObject);
                RefreshContextAndRows();
                FrameDraftTarget(DraftFocusTarget.Board);
                SetStatus(message, MessageType.Info);
            }
            else
            {
                SetStatus(message, MessageType.Warning);
            }
        }

        private void LoadRuntimeAsDraft()
        {
            if (PrototypeUIDesignDraftWorkspace.LoadRuntimeAsDraft(activeScene, IsHubScene(activeScene), out RectTransform draftRoot, out string message))
            {
                MarkSceneDirty(draftRoot.gameObject);
                RefreshContextAndRows();
                FrameDraftTarget(DraftFocusTarget.Board);
                SetStatus(message, MessageType.Info);
            }
            else
            {
                SetStatus(message, MessageType.Warning);
            }
        }

        private void SaveDraft(RectTransform draftRoot)
        {
            if (PrototypeUIDesignDraftWorkspace.SaveDraft(draftRoot, IsHubScene(activeScene), out string message))
            {
                Debug.Log(message);
                SetStatus(message, MessageType.Info);
            }
            else
            {
                Debug.LogWarning(message);
                SetStatus(message, MessageType.Warning);
            }
        }

        private void ApplyDraftToRuntime(RectTransform draftRoot)
        {
            if (PrototypeUIDesignDraftWorkspace.ApplyDraftToRuntime(draftRoot, IsHubScene(activeScene), out string message))
            {
                Debug.Log(message);
                RefreshContextAndRows();
                SetStatus(message, MessageType.Info);
            }
            else
            {
                Debug.LogWarning(message);
                SetStatus(message, MessageType.Warning);
            }
        }

        private void DiscardDraft()
        {
            if (PrototypeUIDesignDraftWorkspace.DiscardDraft(activeScene, out string message))
            {
                RefreshContextAndRows();
                SetStatus(message, MessageType.Info);
            }
            else
            {
                SetStatus(message, MessageType.Warning);
            }
        }

        private void SelectNode(ManagedUiNode node)
        {
            if (node == null || node.Target == null)
            {
                return;
            }

            Selection.activeObject = node.Target;
            EditorGUIUtility.PingObject(node.Target.gameObject);
            selectedManagedPath = node.DisplayPath;
            SetStatus($"{node.DisplayPath} 항목을 선택했습니다.", MessageType.Info);
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
            managedTreeRoots.Clear();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                selectedManagedPath = string.Empty;
                return;
            }

            Dictionary<string, List<RectTransform>> runtimeRectsByName = BuildRectLookup(activeScene, includeDraftCanvas: false);
            Dictionary<string, List<RectTransform>> draftRectsByName = BuildRectLookup(activeScene, includeDraftCanvas: true);
            HashSet<string> managedNames = PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(IsHubScene(activeScene));
            managedNames.Add(HudRootName);
            managedNames.Add(PopupRootName);

            List<string> sortedNames = new(managedNames);
            sortedNames.Sort(string.CompareOrdinal);
            Dictionary<string, ManagedUiNode> nodesByName = new(StringComparer.Ordinal);
            for (int index = 0; index < sortedNames.Count; index++)
            {
                string objectName = sortedNames[index];
                runtimeRectsByName.TryGetValue(objectName, out List<RectTransform> runtimeMatches);
                draftRectsByName.TryGetValue(objectName, out List<RectTransform> draftMatches);
                string parentName = ResolveManagedParentName(objectName, runtimeMatches, draftMatches);
                int siblingIndex = ResolveManagedSiblingIndex(objectName, runtimeMatches, draftMatches);
                nodesByName[objectName] = new ManagedUiNode(objectName, runtimeMatches, draftMatches, parentName, siblingIndex);
            }

            for (int index = 0; index < sortedNames.Count; index++)
            {
                ManagedUiNode node = nodesByName[sortedNames[index]];
                if (!string.IsNullOrWhiteSpace(node.ParentName)
                    && !string.Equals(node.ParentName, node.ObjectName, StringComparison.Ordinal)
                    && nodesByName.TryGetValue(node.ParentName, out ManagedUiNode parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    managedTreeRoots.Add(node);
                }
            }

            SortManagedNodes(managedTreeRoots);
            for (int index = 0; index < managedTreeRoots.Count; index++)
            {
                AssignManagedNodePath(managedTreeRoots[index], 0, managedTreeRoots[index].ObjectName);
            }

            UpdateSelectedManagedPath();
        }

        private static Dictionary<string, List<RectTransform>> BuildRectLookup(Scene scene, bool includeDraftCanvas)
        {
            Dictionary<string, List<RectTransform>> rectsByName = new(StringComparer.Ordinal);
            HashSet<int> visitedInstanceIds = new();

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Canvas[] canvases = roots[rootIndex].GetComponentsInChildren<Canvas>(true);
                for (int canvasIndex = 0; canvasIndex < canvases.Length; canvasIndex++)
                {
                    Canvas canvas = canvases[canvasIndex];
                    if (canvas == null || IsDraftCanvas(canvas) != includeDraftCanvas)
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

        private static string ResolveManagedParentName(
            string objectName,
            IReadOnlyList<RectTransform> runtimeMatches,
            IReadOnlyList<RectTransform> draftMatches)
        {
            RectTransform target = FirstRect(runtimeMatches) ?? FirstRect(draftMatches);
            if (target != null && target.parent != null)
            {
                string actualParentName = target.parent.name;
                if (!IsCanvasRootName(actualParentName))
                {
                    return actualParentName;
                }
            }

            if (PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out string savedParentName, out _)
                && !IsCanvasRootName(savedParentName))
            {
                return savedParentName;
            }

            string defaultParentName = PrototypeUIDesignDraftWorkspace.ResolveDefaultParentName(objectName);
            return IsCanvasRootName(defaultParentName) ? string.Empty : defaultParentName;
        }

        private static int ResolveManagedSiblingIndex(
            string objectName,
            IReadOnlyList<RectTransform> runtimeMatches,
            IReadOnlyList<RectTransform> draftMatches)
        {
            RectTransform target = FirstRect(runtimeMatches) ?? FirstRect(draftMatches);
            if (target != null)
            {
                return target.GetSiblingIndex();
            }

            return PrototypeUISceneLayoutCatalog.TryGetHierarchyOverride(objectName, out _, out int savedSiblingIndex)
                ? savedSiblingIndex
                : int.MaxValue;
        }

        private static RectTransform FirstRect(IReadOnlyList<RectTransform> rects)
        {
            return rects != null && rects.Count > 0 ? rects[0] : null;
        }

        private static bool IsCanvasRootName(string objectName)
        {
            return string.IsNullOrWhiteSpace(objectName)
                   || string.Equals(objectName, "Canvas", StringComparison.Ordinal)
                   || string.Equals(objectName, PrototypeUIDesignDraftWorkspace.DraftCanvasName, StringComparison.Ordinal);
        }

        private static void SortManagedNodes(List<ManagedUiNode> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            nodes.Sort(CompareManagedNodes);
            for (int index = 0; index < nodes.Count; index++)
            {
                SortManagedNodes(nodes[index].Children);
            }
        }

        private static int CompareManagedNodes(ManagedUiNode left, ManagedUiNode right)
        {
            if (left == null || right == null)
            {
                return left == null ? -1 : 1;
            }

            int siblingCompare = left.SiblingIndex.CompareTo(right.SiblingIndex);
            return siblingCompare != 0
                ? siblingCompare
                : string.CompareOrdinal(left.ObjectName, right.ObjectName);
        }

        private static void AssignManagedNodePath(ManagedUiNode node, int depth, string path)
        {
            if (node == null)
            {
                return;
            }

            node.Depth = depth;
            node.DisplayPath = path;
            for (int index = 0; index < node.Children.Count; index++)
            {
                ManagedUiNode child = node.Children[index];
                AssignManagedNodePath(child, depth + 1, path + " > " + child.ObjectName);
            }
        }

        private void UpdateSelectedManagedPath()
        {
            Transform selectedTransform = Selection.activeTransform;
            if (selectedTransform == null)
            {
                selectedManagedPath = string.Empty;
                return;
            }

            ManagedUiNode selectedNode = FindNodeByTarget(managedTreeRoots, selectedTransform);
            selectedManagedPath = selectedNode != null ? selectedNode.DisplayPath : string.Empty;
        }

        private static ManagedUiNode FindNodeByTarget(IReadOnlyList<ManagedUiNode> nodes, Transform target)
        {
            if (nodes == null || target == null)
            {
                return null;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                ManagedUiNode node = nodes[index];
                if (node.Target == target)
                {
                    return node;
                }

                ManagedUiNode found = FindNodeByTarget(node.Children, target);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private void FrameDesignFocus(DesignFocusTarget target)
        {
            if (!TryResolveDesignFocusBounds(target, out Bounds bounds, out string label, out string warning))
            {
                SetStatus(warning, MessageType.Warning);
                return;
            }

            designFocusTarget = target;
            showDesignFrame = true;
            FrameSceneViewBounds(bounds);
            SceneView.RepaintAll();
            SetStatus($"{label} 기준으로 Scene 뷰를 맞췄습니다.", MessageType.Info);
        }

        private void FrameDraftTarget(DraftFocusTarget target)
        {
            if (!TryResolveDraftFocusBounds(target, out Bounds bounds, out string label, out string warning))
            {
                SetStatus(warning, MessageType.Warning);
                return;
            }

            showDesignFrame = true;
            FrameSceneViewBounds(bounds);
            SceneView.RepaintAll();
            SetStatus($"{label} 기준으로 드래프트 Scene 뷰를 맞췄습니다.", MessageType.Info);
        }

        private bool TryResolveDraftFocusBounds(
            DraftFocusTarget target,
            out Bounds bounds,
            out string label,
            out string warning)
        {
            bounds = default;
            label = string.Empty;
            warning = string.Empty;

            RectTransform draftRoot = PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene);
            if (draftRoot == null)
            {
                warning = "드래프트 Canvas가 없습니다. 드래프트 만들기 또는 더보기 > 드래프트 > 런타임 값 불러오기를 먼저 실행하세요.";
                return false;
            }

            RectTransform targetRect = target switch
            {
                DraftFocusTarget.Board => draftRoot,
                DraftFocusTarget.Hud => PrototypeUIDesignDraftWorkspace.FindDraftChild(draftRoot, HudRootName),
                DraftFocusTarget.Popup => PrototypeUIDesignDraftWorkspace.FindDraftChild(draftRoot, PopupRootName),
                DraftFocusTarget.Selected => TryResolveSelectedDraftRect(draftRoot, out RectTransform selectedRect) ? selectedRect : null,
                _ => null
            };

            if (targetRect == null)
            {
                warning = target == DraftFocusTarget.Selected
                    ? "드래프트 Canvas 아래의 UI RectTransform을 선택한 뒤 다시 시도하세요."
                    : "드래프트에서 대상 UI 루트를 찾지 못했습니다.";
                return false;
            }

            if (!TryGetRectTransformBounds(targetRect, out bounds))
            {
                warning = "드래프트 기준 영역을 계산하지 못했습니다.";
                return false;
            }

            label = targetRect.name;
            return true;
        }

        private bool TryResolveDesignFocusBounds(
            DesignFocusTarget target,
            out Bounds bounds,
            out string label,
            out string warning)
        {
            bounds = default;
            label = string.Empty;
            warning = string.Empty;

            RectTransform canvasRoot = ResolveCanvasRoot();
            if (canvasRoot == null)
            {
                warning = "현재 활성 씬에서 Canvas UIManager를 찾지 못했습니다.";
                return false;
            }

            switch (target)
            {
                case DesignFocusTarget.WholeUi:
                    if (TryGetCanvasReferenceBounds(canvasRoot, out bounds))
                    {
                        label = "전체 UI";
                        return true;
                    }

                    warning = "Canvas 기준 영역을 계산하지 못했습니다.";
                    return false;
                case DesignFocusTarget.Hud:
                    return TryResolveNamedRectBounds(HudRootName, "HUD", "HUDRoot가 없습니다. 미리보기 적용을 먼저 실행해 관리 UI를 생성하세요.", out bounds, out label, out warning);
                case DesignFocusTarget.Popup:
                    return TryResolveNamedRectBounds(PopupRootName, "팝업", "PopupRoot가 없습니다. 미리보기 적용을 먼저 실행해 관리 UI를 생성하세요.", out bounds, out label, out warning);
                case DesignFocusTarget.Selected:
                    if (TryResolveSelectedUiRect(out RectTransform selectedRect)
                        && TryGetRectTransformBounds(selectedRect, out bounds))
                    {
                        label = selectedRect.name;
                        return true;
                    }

                    warning = "현재 Canvas 아래의 UI RectTransform을 선택한 뒤 다시 시도하세요.";
                    return false;
                default:
                    warning = "알 수 없는 디자인 포커스입니다.";
                    return false;
            }
        }

        private bool TryResolveNamedRectBounds(
            string objectName,
            string targetLabel,
            string missingMessage,
            out Bounds bounds,
            out string label,
            out string warning)
        {
            bounds = default;
            label = targetLabel;
            warning = string.Empty;

            RectTransform target = FindUiRect(objectName);
            if (target == null)
            {
                warning = missingMessage;
                return false;
            }

            if (TryGetRectTransformBounds(target, out bounds))
            {
                return true;
            }

            RectTransform canvasRoot = ResolveCanvasRoot();
            if (TryGetCanvasReferenceBounds(canvasRoot, out bounds))
            {
                return true;
            }

            warning = $"{objectName} 기준 영역을 계산하지 못했습니다.";
            return false;
        }

        private RectTransform ResolveCanvasRoot()
        {
            return uiManager != null ? uiManager.transform as RectTransform : null;
        }

        private RectTransform FindUiRect(string objectName)
        {
            RectTransform canvasRoot = ResolveCanvasRoot();
            if (canvasRoot == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            if (string.Equals(canvasRoot.name, objectName, StringComparison.Ordinal))
            {
                return canvasRoot;
            }

            Transform found = canvasRoot.Find(objectName) ?? FindChildRecursive(canvasRoot, objectName);
            return found as RectTransform;
        }

        private bool TryResolveSelectedUiRect(out RectTransform selectedRect)
        {
            selectedRect = null;
            RectTransform canvasRoot = ResolveCanvasRoot();
            Transform selectedTransform = Selection.activeTransform;
            if (canvasRoot == null || selectedTransform == null)
            {
                return false;
            }

            selectedRect = selectedTransform as RectTransform;
            if (selectedRect == null)
            {
                selectedRect = selectedTransform.GetComponent<RectTransform>();
            }

            return selectedRect != null && IsDescendantOf(selectedRect, canvasRoot);
        }

        private static bool TryResolveSelectedDraftRect(RectTransform draftRoot, out RectTransform selectedRect)
        {
            selectedRect = null;
            Transform selectedTransform = Selection.activeTransform;
            if (draftRoot == null || selectedTransform == null)
            {
                return false;
            }

            selectedRect = selectedTransform as RectTransform;
            if (selectedRect == null)
            {
                selectedRect = selectedTransform.GetComponent<RectTransform>();
            }

            return selectedRect != null && IsDescendantOf(selectedRect, draftRoot);
        }

        private void DrawSceneViewDesignFrame(SceneView sceneView)
        {
            if (!showDesignFrame
                || Application.isPlaying
                || sceneView == null
                || !activeScene.IsValid()
                || !activeScene.isLoaded)
            {
                return;
            }

            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.Always;

            try
            {
                DrawRuntimeSceneFrame();
                DrawDraftSceneFrame();
            }
            finally
            {
                Handles.color = previousColor;
                Handles.zTest = previousZTest;
            }
        }

        private void DrawRuntimeSceneFrame()
        {
            RectTransform canvasRoot = ResolveCanvasRoot();
            if (uiManager == null
                || uiManager.gameObject.scene.handle != activeScene.handle
                || canvasRoot == null
                || !TryGetCanvasReferenceBounds(canvasRoot, out Bounds canvasBounds))
            {
                return;
            }

            DrawBoundsFrame(canvasBounds, CanvasFrameColor, "Canvas 1920x1080");
            if (TryResolveDesignFocusBounds(designFocusTarget, out Bounds focusBounds, out string focusLabel, out _))
            {
                DrawBoundsFrame(focusBounds, FocusFrameColor, focusLabel);
            }
        }

        private void DrawDraftSceneFrame()
        {
            RectTransform draftRoot = PrototypeUIDesignDraftWorkspace.FindDraftRoot(activeScene);
            if (draftRoot == null || !TryGetRectTransformBounds(draftRoot, out Bounds draftBounds))
            {
                return;
            }

            DrawBoundsFrame(draftBounds, DraftFrameColor, PrototypeUIDesignDraftWorkspace.DraftCanvasName);
            if (TryResolveSelectedDraftRect(draftRoot, out RectTransform selectedDraftRect)
                && TryGetRectTransformBounds(selectedDraftRect, out Bounds selectedDraftBounds))
            {
                DrawBoundsFrame(selectedDraftBounds, FocusFrameColor, selectedDraftRect.name);
            }
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child == null)
                {
                    continue;
                }

                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsDescendantOf(Transform target, Transform root)
        {
            Transform current = target;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool TryGetCanvasReferenceBounds(RectTransform canvasRoot, out Bounds bounds)
        {
            if (TryGetRectTransformBounds(canvasRoot, out bounds))
            {
                return true;
            }

            bounds = default;
            if (canvasRoot == null)
            {
                return false;
            }

            bounds = new Bounds(canvasRoot.position, new Vector3(CanvasReferenceWidth, CanvasReferenceHeight, 1f));
            return true;
        }

        private static bool TryGetRectTransformBounds(RectTransform rectTransform, out Bounds bounds)
        {
            bounds = default;
            if (rectTransform == null)
            {
                return false;
            }

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            bounds = new Bounds(corners[0], Vector3.zero);
            for (int index = 1; index < corners.Length; index++)
            {
                bounds.Encapsulate(corners[index]);
            }

            if (bounds.size.x < 1f || bounds.size.y < 1f)
            {
                Rect rect = rectTransform.rect;
                if (rect.width < 1f || rect.height < 1f)
                {
                    return false;
                }

                bounds = new Bounds(rectTransform.position, new Vector3(rect.width, rect.height, 1f));
            }

            if (bounds.size.z < 1f)
            {
                bounds.Expand(new Vector3(0f, 0f, 1f));
            }

            return true;
        }

        private static void DrawBoundsFrame(Bounds bounds, Color color, string label)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] points =
            {
                new(min.x, min.y, bounds.center.z),
                new(max.x, min.y, bounds.center.z),
                new(max.x, max.y, bounds.center.z),
                new(min.x, max.y, bounds.center.z),
                new(min.x, min.y, bounds.center.z)
            };

            Handles.color = color;
            Handles.DrawAAPolyLine(3f, points);

            GUIStyle style = new(EditorStyles.boldLabel)
            {
                normal = { textColor = color }
            };
            float labelOffset = HandleUtility.GetHandleSize(max) * 0.08f;
            Handles.Label(new Vector3(min.x, max.y + labelOffset, bounds.center.z), label, style);
        }

        private static void FrameSceneViewBounds(Bounds bounds)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null && SceneView.sceneViews.Count > 0)
            {
                sceneView = SceneView.sceneViews[0] as SceneView;
            }

            if (sceneView != null)
            {
                sceneView.Frame(bounds, false);
                sceneView.Repaint();
            }
        }

        private static bool IsHubScene(Scene scene)
        {
            return string.Equals(scene.name, "Hub", StringComparison.Ordinal);
        }

        private static bool IsDraftCanvas(Canvas canvas)
        {
            return canvas != null
                   && canvas.gameObject != null
                   && string.Equals(canvas.gameObject.name, PrototypeUIDesignDraftWorkspace.DraftCanvasName, StringComparison.Ordinal);
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

        private enum WorkMode
        {
            Draft,
            Runtime,
            Objects
        }

        private enum DesignFocusTarget
        {
            WholeUi,
            Hud,
            Popup,
            Selected
        }

        private enum DraftFocusTarget
        {
            Board,
            Hud,
            Popup,
            Selected
        }

        private sealed class ManagedUiNode
        {
            private readonly List<RectTransform> runtimeMatches;
            private readonly List<RectTransform> draftMatches;

            public ManagedUiNode(
                string objectName,
                List<RectTransform> runtimeMatches,
                List<RectTransform> draftMatches,
                string parentName,
                int siblingIndex)
            {
                ObjectName = objectName;
                this.runtimeMatches = runtimeMatches ?? new List<RectTransform>();
                this.draftMatches = draftMatches ?? new List<RectTransform>();
                ParentName = parentName;
                SiblingIndex = siblingIndex;
            }

            public string ObjectName { get; }
            public string ParentName { get; }
            public int SiblingIndex { get; }
            public int Depth { get; set; }
            public string DisplayPath { get; set; }
            public List<ManagedUiNode> Children { get; } = new();
            public RectTransform Target => runtimeMatches.Count > 0 ? runtimeMatches[0] : draftMatches.Count > 0 ? draftMatches[0] : null;
            public bool HasDuplicate => runtimeMatches.Count > 1 || draftMatches.Count > 1;

            public string StatusText
            {
                get
                {
                    if (HasDuplicate)
                    {
                        return "중복";
                    }

                    return Target != null ? "정상" : "없음";
                }
            }
        }
    }
}
