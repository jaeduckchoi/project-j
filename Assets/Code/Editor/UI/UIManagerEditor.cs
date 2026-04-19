using System;
using System.Collections.Generic;
using Code.Scripts.Management.Storage;
using Code.Scripts.Restaurant.Kitchen;
using Code.Scripts.Shared;
using TMPro;
using Code.Scripts.UI;
using Code.Scripts.UI.Controllers;
using Code.Scripts.UI.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Editor.UI
{
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            UIManager uiManager = target as UIManager;
            if (uiManager == null)
            {
                return;
            }

            PrototypeUIDesignController controller = uiManager.GetComponent<PrototypeUIDesignController>();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Design", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "프리뷰 생성은 PrototypeUIDesignController가 처리하고, 저장 및 반영할 레이아웃 값은 UI 레이아웃 편집기에서 관리합니다.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Canvas 그룹 정리"))
                {
                    uiManager.OrganizeCanvasHierarchyInEditor();
                    MarkSceneDirty(uiManager.gameObject);
                }

                if (controller == null)
                {
                    if (GUILayout.Button("디자인 컨트롤러 추가"))
                    {
                        controller = uiManager.gameObject.AddComponent<PrototypeUIDesignController>();
                        controller.Configure(uiManager);
                        MarkSceneDirty(uiManager.gameObject);
                        Selection.activeObject = controller;
                    }
                }
                else if (GUILayout.Button("디자인 컨트롤러 선택"))
                {
                    Selection.activeObject = controller;
                }
            }

            if (GUILayout.Button("UI 레이아웃 편집기 열기"))
            {
                PrototypeUILayoutEditorWindow.Open();
            }

            if (GUILayout.Button("에디터 UI 프리뷰 적용"))
            {
                if (controller == null)
                {
                    controller = uiManager.gameObject.AddComponent<PrototypeUIDesignController>();
                    controller.Configure(uiManager);
                }

                PrototypeUISceneLayoutCatalog.ReloadBindingSettingsForEditor();
                controller.ApplyEditorPreviewInEditor();
                MarkSceneDirty(uiManager.gameObject);
            }
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
    }

    /// <summary>
    /// Shows UI by structure and links big popup windows to their world interaction objects.
    /// </summary>
    public sealed class PrototypeUILayoutEditorWindow : EditorWindow
    {
        private const float TreeIndentWidth = 16f;
        private const float TreeStatusWidth = 64f;
        private const float TreeStatusSpacing = 8f;

        private enum SelectionKind { PopupConnection, LayoutBinding }

        private readonly struct PopupConnectionDef
        {
            public PopupConnectionDef(string key, string label, Type componentType, string help)
            {
                Key = key;
                Label = label;
                ComponentType = componentType;
                Help = help;
            }

            public string Key { get; }
            public string Label { get; }
            public Type ComponentType { get; }
            public string Help { get; }
        }

        private static readonly PopupConnectionDef[] PopupConnections =
        {
            new("Storage", "창고 팝업", typeof(StorageStation), "StorageStation을 가진 월드 오브젝트가 상호작용 시 창고 팝업을 엽니다."),
            new("Refrigerator", "냉장고 팝업", typeof(RefrigeratorStation), "RefrigeratorStation을 가진 월드 오브젝트가 상호작용 시 냉장고 팝업을 엽니다."),
            new("FrontCounter", "프런트 카운터 팝업", typeof(FrontCounterStation), "FrontCounterStation을 가진 월드 오브젝트가 상호작용 시 프런트 카운터 팝업을 엽니다.")
        };

        private readonly List<string> managedNames = new();
        private readonly HashSet<string> managedNameSet = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> folds = new(StringComparer.Ordinal)
        {
            ["connections"] = true,
            ["hud"] = true,
            ["popup"] = true,
            ["popup-common"] = true,
            ["popup-list"] = false,
            ["refrigerator"] = true
        };

        private Scene activeScene;
        private PrototypeUILayoutBindingSettings settings;
        private PopupInteractionBindingSettings popupSettings;
        private UIManager uiManager;
        private PrototypeUIDesignController designController;
        private Vector2 treeScroll;
        private Vector2 detailScroll;
        private SelectionKind selectionKind = SelectionKind.PopupConnection;
        private string selectedConnectionKey = "Refrigerator";
        private string selectedRuntimeName;
        private string searchText = string.Empty;
        private string status;
        private MessageType statusType = MessageType.None;
        private GUIStyle treeStatusStyle;

        [MenuItem("Window/Jonggu Restaurant/UI 레이아웃 편집기")]
        public static void Open()
        {
            GetWindow<PrototypeUILayoutEditorWindow>("UI 레이아웃 편집기");
        }

        private void OnEnable()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            RefreshContext();
        }

        private void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        }

        private void OnHierarchyChange()
        {
            RefreshContext();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("UI 레이아웃 편집기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "왼쪽 트리에서 팝업 연결이나 Canvas UI 오브젝트를 선택합니다. 팝업 연결은 popup-interaction-bindings 자산을 정본으로 편집하고 씬 station 컴포넌트를 동기화하며, 레이아웃 바인딩은 선택한 오브젝트의 표시값을 저장합니다. 씬 저장 시 연결된 managed UI 값은 ui-layout-bindings.asset으로 다시 동기화됩니다.",
                MessageType.Info);

            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                EditorGUILayout.HelpBox("활성 씬을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            DrawToolbar();
            if (!string.IsNullOrWhiteSpace(status))
            {
                EditorGUILayout.HelpBox(status, statusType);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTree();
                DrawDetail();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("새로고침", GUILayout.Width(90)))
                {
                    RefreshContext();
                }

                if (GUILayout.Button("레이아웃 설정 저장", GUILayout.Width(130)))
                {
                    SaveSettings("레이아웃 설정 자산을 저장했습니다.", MessageType.Info);
                }

                if (GUILayout.Button("현재 씬 전체 캡처", GUILayout.Width(160)))
                {
                    CaptureAllValidBindings();
                }

                using (new EditorGUI.DisabledScope(uiManager == null))
                {
                    if (GUILayout.Button("씬 프리뷰 적용", GUILayout.Width(120)))
                    {
                        ApplyScenePreview();
                    }
                }
            }

            EditorGUILayout.LabelField("활성 씬", activeScene.path);
            EditorGUILayout.LabelField("저장된 binding", $"{settings?.Bindings.Count ?? 0}개");
            if (settings != null && settings.Bindings.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "현재 binding 자산이 비어 있습니다. 씬 UI 오브젝트를 연결하면 즉시 캡처 및 저장 대상으로 반영되며, 저장된 이름 일치 managed UI는 씬 저장 시 자동으로 다시 동기화됩니다.",
                    MessageType.Warning);
            }
        }

        private void DrawTree()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(340)))
            {
                EditorGUILayout.LabelField("구조", EditorStyles.boldLabel);
                searchText = EditorGUILayout.TextField("검색", searchText);
                treeScroll = EditorGUILayout.BeginScrollView(treeScroll, GUI.skin.box);
                DrawConnectionGroup();
                DrawCanvasGroups();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawConnectionGroup()
        {
            if (!Fold("connections", "팝업 연결", 0))
            {
                return;
            }

            PopupConnectionDef[] popupConnections = GetActivePopupConnections();
            for (int i = 0; i < popupConnections.Length; i++)
            {
                PopupConnectionDef def = popupConnections[i];
                if (!Matches(def.Label) && !Matches(def.Key))
                {
                    continue;
                }

                TryGetPopupBinding(def.Key, out PopupInteractionBindingEntry entry);
                List<Component> components = FindConnectionComponents(def);
                TreeButton(def.Label, BuildPopupConnectionStatusLabel(def, entry, components), SelectionKind.PopupConnection, def.Key, 1);

                if (entry != null && !string.IsNullOrWhiteSpace(entry.SceneObjectPath))
                {
                    Label($"자산: {entry.SceneObjectPath}", 2);
                }

                for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
                {
                    Component component = components[componentIndex];
                    if (component != null)
                    {
                        string scenePath = BuildSceneObjectPath(component.transform);
                        if (entry == null || !string.Equals(entry.SceneObjectPath, scenePath, StringComparison.Ordinal))
                        {
                            Label($"씬: {scenePath}", 2);
                        }
                    }
                }
            }

        }

        private PopupConnectionDef[] GetActivePopupConnections()
        {
            return PopupConnections;
        }

        private void DrawCanvasGroups()
        {
            if (Fold("hud", "Canvas / HUD", 0))
            {
                DrawHudTree();
            }

            if (!Fold("popup", "Canvas / 팝업", 0))
            {
                return;
            }

            if (Fold("popup-common-frame", "공통 프레임", 1))
            {
                DrawLayoutNodes(
                    new[]
                    {
                        "PopupRoot",
                        "PopupOverlay",
                        "PopupFrame",
                        PrototypeUIObjectNames.PopupTitle,
                        "PopupCloseButton"
                    },
                    2);
            }

            if (Fold("popup-refrigerator-type", "냉장고 팝업", 1))
            {
                DrawLayoutNodes(
                    new[]
                    {
                        "UIComponent",
                        "RefrigeratorUI"
                    },
                    2);
                DrawRefrigeratorTree(2);
            }
        }
        private void DrawHudTree()
        {
            if (Fold("hud-status-tree", "Status", 1))
            {
                DrawLayoutNodes(
                    new[]
                    {
                        "HUDRoot",
                        "HUDStatusGroup",
                        "TopLeftPanel",
                        "GoldText",
                        "ResourcePanel",
                        "ResourceAmountText"
                    },
                    2);
            }

            if (Fold("hud-action-tree", "Action", 1))
            {
                DrawLayoutNodes(
                    new[]
                    {
                        "HUDActionGroup",
                        "ActionDock",
                        "ActionAccent",
                        "ActionCaption"
                    },
                    2);
            }

            if (Fold("hud-panel-buttons-tree", "Panel Buttons", 1))
            {
                DrawLayoutNodes(
                    new[]
                    {
                        "HUDPanelButtonGroup",
                        "RecipePanelButton",
                        "UpgradePanelButton",
                        "MaterialPanelButton"
                    },
                    2);
            }

            if (Fold("hud-overlay-tree", "Overlay", 1))
            {
                DrawLayoutNodes(
                    new[]
                    {
                        "HUDBottomGroup",
                        "HUDOverlayGroup",
                        "InteractionPromptBackdrop",
                        "InteractionPromptText",
                        "GuideBackdrop",
                        "GuideText",
                        "GuideHelpButton",
                        "ResultBackdrop",
                        "RestaurantResultText"
                    },
                    2);
            }
        }

        private void DrawRefrigeratorTree(int indent)
        {
            if (Fold("refrigerator-storage-tree", PrototypeUIObjectNames.RefrigeratorStorage, indent))
            {
                LayoutNode(PrototypeUIObjectNames.RefrigeratorStorage, indent + 1);
                LayoutNode(PrototypeUIObjectNames.RefrigeratorSelectedSlot, indent + 1);

                if (Fold("refrigerator-slots-tree", "Slots", indent + 1))
                {
                    for (int row = 0; row < PrototypeUILayout.RefrigeratorSlotRowCount; row++)
                    {
                        if (!Fold($"refrigerator-row-{row + 1}", $"{row + 1}행", indent + 2))
                        {
                            continue;
                        }

                        for (int column = 0; column < PrototypeUILayout.RefrigeratorSlotColumnCount; column++)
                        {
                            int index = (row * PrototypeUILayout.RefrigeratorSlotColumnCount) + column;
                            string slotName = $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{index + 1:00}";
                            string iconName = $"{PrototypeUIObjectNames.RefrigeratorSlotIconPrefix}{index + 1:00}";
                            string amountName = $"{PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix}{index + 1:00}";
                            if (!Fold($"refrigerator-slot-{index + 1:00}", slotName, indent + 3))
                            {
                                continue;
                            }

                            LayoutNode(slotName, indent + 4);
                            LayoutNode(iconName, indent + 4);
                            LayoutNode(amountName, indent + 4);
                        }
                    }
                }
            }

            if (Fold("refrigerator-info-tree", PrototypeUIObjectNames.RefrigeratorInfoPanel, indent))
            {
                LayoutNode(PrototypeUIObjectNames.RefrigeratorInfoPanel, indent + 1);
                LayoutNode(PrototypeUIObjectNames.RefrigeratorInfoIcon, indent + 1);
                LayoutNode(PrototypeUIObjectNames.RefrigeratorItemNameText, indent + 1);
                LayoutNode(PrototypeUIObjectNames.RefrigeratorItemDescriptionText, indent + 1);
            }

            if (Fold("refrigerator-remove-tree", PrototypeUIObjectNames.RefrigeratorRemoveZone, indent))
            {
                LayoutNode(PrototypeUIObjectNames.RefrigeratorRemoveZone, indent + 1);
                LayoutNode(PrototypeUIObjectNames.RefrigeratorRemoveIcon, indent + 1);
                LayoutNode(PrototypeUIObjectNames.RefrigeratorRemoveText, indent + 1);
            }

            LayoutNode(PrototypeUIObjectNames.RefrigeratorDragGhost, indent);
        }

        private void DrawLayoutNodes(IEnumerable<string> runtimeNames, int indent)
        {
            if (runtimeNames == null)
            {
                return;
            }

            foreach (string runtimeName in runtimeNames)
            {
                LayoutNode(runtimeName, indent);
            }
        }

        private void LayoutNode(string runtimeName, int indent)
        {
            if (string.IsNullOrWhiteSpace(runtimeName) || !Matches(runtimeName))
            {
                return;
            }

            TreeButton(runtimeName, BuildLayoutBindingStatusLabel(runtimeName), SelectionKind.LayoutBinding, runtimeName, indent);
        }

        private bool Fold(string key, string label, int indent)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent * TreeIndentWidth);
                folds.TryGetValue(key, out bool open);
                open = EditorGUILayout.Foldout(open, label, true);
                folds[key] = open;
                return open;
            }
        }

        private void TreeButton(string label, string statusLabel, SelectionKind kind, string key, int indent)
        {
            bool selected = selectionKind == kind
                            && (kind == SelectionKind.PopupConnection
                                ? string.Equals(selectedConnectionKey, key, StringComparison.Ordinal)
                                : string.Equals(selectedRuntimeName, key, StringComparison.Ordinal));

            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            rowRect.xMin += indent * TreeIndentWidth;

            float statusWidth = Mathf.Min(TreeStatusWidth, Mathf.Max(52f, rowRect.width * 0.22f));
            float labelWidth = Mathf.Max(0f, rowRect.width - statusWidth - TreeStatusSpacing);
            Rect buttonRect = new(rowRect.x, rowRect.y, labelWidth, rowRect.height);
            Rect statusRect = new(buttonRect.xMax + TreeStatusSpacing, rowRect.y, statusWidth, rowRect.height);

            if (GUI.Button(buttonRect, label, selected ? EditorStyles.toolbarButton : EditorStyles.miniButton))
            {
                selectionKind = kind;
                if (kind == SelectionKind.PopupConnection)
                {
                    selectedConnectionKey = key;
                }
                else
                {
                    selectedRuntimeName = key;
                }
            }

            EditorGUI.LabelField(statusRect, statusLabel, GetTreeStatusStyle());
        }

        private void Label(string text, int indent)
        {
            if (!Matches(text))
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent * TreeIndentWidth);
                EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
            }
        }

        private GUIStyle GetTreeStatusStyle()
        {
            treeStatusStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };

            return treeStatusStyle;
        }

        private void DrawDetail()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
                if (selectionKind == SelectionKind.PopupConnection)
                {
                    DrawConnectionDetail();
                }
                else
                {
                    DrawLayoutDetail();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawConnectionDetail()
        {
            if (!TryGetConnection(selectedConnectionKey, out PopupConnectionDef def))
            {
                EditorGUILayout.HelpBox("선택한 팝업 연결 정의를 찾지 못했습니다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(def.Label, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(def.Help, MessageType.Info);
            TryGetPopupBinding(def.Key, out PopupInteractionBindingEntry entry);
            GameObject current = ResolveSceneObject(entry?.SceneObjectPath);

            EditorGUI.BeginChangeCheck();
            GameObject next = EditorGUILayout.ObjectField("팝업 연결 오브젝트", current, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                SetPopupConnectionObject(def, next);
                TryGetPopupBinding(def.Key, out entry);
                current = ResolveSceneObject(entry?.SceneObjectPath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("메모", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            string nextMemo = EditorGUILayout.TextArea(entry?.Memo ?? string.Empty, GUILayout.MinHeight(52f));
            if (EditorGUI.EndChangeCheck())
            {
                UpdatePopupConnectionMemo(def.Key, nextMemo);
                TryGetPopupBinding(def.Key, out entry);
                current = ResolveSceneObject(entry?.SceneObjectPath);
            }

            List<Component> connected = FindConnectionComponents(def);
            bool isSynced = TryBuildPopupSyncMessage(def, entry, current, connected, out string syncMessage, out MessageType syncType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("자산 기준 대상", EditorStyles.boldLabel);
            if (entry == null || string.IsNullOrWhiteSpace(entry.SceneObjectPath))
            {
                EditorGUILayout.HelpBox("현재 popup-interaction-bindings 자산에 대상이 지정되지 않았습니다.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Scene Path", entry.SceneObjectPath);
                if (current == null)
                {
                    EditorGUILayout.HelpBox("자산 경로가 현재 활성 씬에서 resolve되지 않습니다.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 씬 동기화 상태", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(syncMessage, syncType);

            if (connected.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("현재 씬 컴포넌트", EditorStyles.boldLabel);
            }

            for (int i = 0; i < connected.Count; i++)
            {
                Component component = connected[i];
                if (component == null)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(BuildSceneObjectPath(component.transform));
                    if (GUILayout.Button("선택", GUILayout.Width(56)))
                    {
                        Selection.activeObject = component.gameObject;
                        EditorGUIUtility.PingObject(component.gameObject);
                    }
                }
            }

            if (!isSynced && connected.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 씬에 연결된 station 컴포넌트가 없습니다.", MessageType.None);
            }

            DrawSceneHierarchyPreview("연결 오브젝트 구조", current != null ? current.transform : null, $"connection-tree:{def.Key}");
        }

        private void DrawLayoutDetail()
        {
            if (string.IsNullOrWhiteSpace(selectedRuntimeName))
            {
                EditorGUILayout.HelpBox("Canvas UI 요소를 선택하세요.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField(selectedRuntimeName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("씬 UI 오브젝트를 연결하거나 현재 값을 캡처하면 ui-layout-bindings.asset에 RectTransform, Image, TMP, Button 표시값과 parent/sibling/initial active baseline hierarchy 계약이 함께 저장됩니다. 월드 팝업 연결은 popup-interaction-bindings 자산을 정본으로 하는 '팝업 연결'에서 설정하세요.", MessageType.None);
            if (PrototypeUISceneLayoutCatalog.IsRuntimeOnlyObjectName(selectedRuntimeName))
            {
                EditorGUILayout.HelpBox("이 이름은 runtime-only whitelist 대상이라 scene path와 hierarchy 값이 런타임 구조 계약으로 사용되지 않습니다.", MessageType.None);
            }

            settings.TryGetEntry(selectedRuntimeName, out PrototypeUILayoutBindingEntry entry);
            GameObject current = PrototypeUILayoutBindingSyncUtility.ResolveLayoutPreviewObject(activeScene, settings, selectedRuntimeName);
            DrawLayoutBindingObjectField(entry, current);
            settings.TryGetEntry(selectedRuntimeName, out entry);
            current = PrototypeUILayoutBindingSyncUtility.ResolveLayoutPreviewObject(activeScene, settings, selectedRuntimeName);
            DrawLayoutMemoField(entry);
            settings.TryGetEntry(selectedRuntimeName, out entry);

            Image currentImage = current != null ? current.GetComponent<Image>() : null;
            DrawLayoutSpriteField(entry, currentImage);
            DrawLayoutSpriteHelp(entry, current, currentImage);
            DrawSceneHierarchyPreview("현재 UI 오브젝트 구조", current != null ? current.transform : null, $"layout-tree:{selectedRuntimeName}");
        }

        private void DrawLayoutBindingObjectField(PrototypeUILayoutBindingEntry entry, GameObject current)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("연결 UI 오브젝트", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            GameObject next = EditorGUILayout.ObjectField("씬 UI 오브젝트", current, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                SetLayoutBindingObject(next);
                settings.TryGetEntry(selectedRuntimeName, out entry);
                current = PrototypeUILayoutBindingSyncUtility.ResolveLayoutPreviewObject(activeScene, settings, selectedRuntimeName);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(current == null || current.GetComponent<RectTransform>() == null))
                {
                    if (GUILayout.Button("현재 씬 값 캡처", GUILayout.Width(120)))
                    {
                        CaptureSelectedBindingFromCurrentObject(current);
                        settings.TryGetEntry(selectedRuntimeName, out entry);
                        current = PrototypeUILayoutBindingSyncUtility.ResolveLayoutPreviewObject(activeScene, settings, selectedRuntimeName);
                    }
                }

                using (new EditorGUI.DisabledScope(entry == null
                                                   || (string.IsNullOrWhiteSpace(entry.SceneObjectPath)
                                                       && !entry.ApplyHierarchy
                                                       && !entry.ApplyRect
                                                       && !entry.ApplyImage
                                                       && !entry.ApplyText
                                                       && !entry.ApplyButton)))
                {
                    if (GUILayout.Button("연결 해제", GUILayout.Width(90)))
                    {
                        ClearSelectedBinding();
                    }
                }
            }

            if (entry != null && !string.IsNullOrWhiteSpace(entry.SceneObjectPath))
            {
                EditorGUILayout.LabelField("Scene Path", entry.SceneObjectPath);
                if (entry.ApplyHierarchy)
                {
                    EditorGUILayout.LabelField("Hierarchy Parent", string.IsNullOrWhiteSpace(entry.HierarchyParentScenePath) ? "<root>" : entry.HierarchyParentScenePath);
                    EditorGUILayout.LabelField("Hierarchy Sibling", entry.HierarchySiblingIndex.ToString());
                    EditorGUILayout.LabelField("Hierarchy Initial Active", entry.HierarchyInitialActiveSelf ? "true" : "false");
                }

                GameObject explicitTarget = ResolveSceneObject(entry.SceneObjectPath);
                if (explicitTarget == null)
                {
                    EditorGUILayout.HelpBox("저장된 Scene Path가 현재 씬에서 resolve되지 않습니다. 런타임은 fallback grouping으로 내려가므로 silent drift가 생길 수 있습니다.", MessageType.Warning);
                }
                else if (entry.ApplyHierarchy)
                {
                    string currentParentPath = explicitTarget.transform.parent != null
                        ? BuildSceneObjectPath(explicitTarget.transform.parent)
                        : string.Empty;
                    if (!string.Equals(currentParentPath, entry.HierarchyParentScenePath, StringComparison.Ordinal)
                        || explicitTarget.transform.GetSiblingIndex() != entry.HierarchySiblingIndex
                        || explicitTarget.activeSelf != entry.HierarchyInitialActiveSelf)
                    {
                        EditorGUILayout.HelpBox("현재 씬 hierarchy가 저장된 contract와 다릅니다. 씬 값을 다시 캡처하거나 hierarchy를 자산 기준으로 정리하세요.", MessageType.Warning);
                    }
                }
            }
            else if (current != null)
            {
                EditorGUILayout.HelpBox("현재 씬에서 같은 이름의 UI 오브젝트를 찾았습니다. 씬 저장 시 이 오브젝트 값이 ui-layout-bindings.asset으로 자동 동기화됩니다.", MessageType.Info);
            }
            else if (PrototypeUISceneLayoutCatalog.IsRuntimeOnlyObjectName(selectedRuntimeName))
            {
                EditorGUILayout.HelpBox("이 이름은 명시적 runtime-only whitelist 예외 대상입니다. 구조 계약 없이 런타임 생성이 허용됩니다.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("이 이름은 runtime-only 예외 대상이 아닙니다. 씬 authored 구조를 유지하려면 씬 오브젝트를 연결하고 binding contract를 저장하세요.", MessageType.Info);
            }
        }

        private void DrawLayoutMemoField(PrototypeUILayoutBindingEntry entry)
        {
            EditorGUILayout.LabelField("메모", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            string nextMemo = EditorGUILayout.TextArea(entry?.Memo ?? string.Empty, GUILayout.MinHeight(52f));
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            UpdateSelectedBindingMemo(nextMemo);
        }

        private void DrawLayoutSpriteField(PrototypeUILayoutBindingEntry entry, Image currentImage)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("연결 스프라이트", EditorStyles.miniBoldLabel);

            Sprite currentSprite = null;
            if (entry != null)
            {
                entry.TryGetImageSpriteOverride(out currentSprite);
            }

            bool canEditSprite = currentImage != null || (entry != null && entry.ApplyImage);
            using (new EditorGUI.DisabledScope(!canEditSprite))
            {
                EditorGUI.BeginChangeCheck();
                Sprite nextSprite = EditorGUILayout.ObjectField(currentSprite, typeof(Sprite), false) as Sprite;
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSelectedBindingSprite(nextSprite, currentImage);
                }
            }
        }

        private void DrawLayoutSpriteHelp(PrototypeUILayoutBindingEntry entry, GameObject current, Image currentImage)
        {
            EditorGUILayout.Space();
            if (current == null)
            {
                EditorGUILayout.HelpBox("현재 씬에서 같은 이름의 UI 오브젝트를 찾지 못했습니다. UI 프리뷰를 먼저 적용하세요.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("현재 UI 오브젝트", BuildSceneObjectPath(current.transform));
            if (currentImage == null && (entry == null || !entry.ApplyImage))
            {
                EditorGUILayout.HelpBox("이 오브젝트는 현재 씬 기준으로 Image 컴포넌트가 없어 스프라이트 설정을 사용하지 않습니다.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox("스프라이트를 비우면 코드 기본값 또는 현재 프리뷰 스프라이트를 그대로 사용합니다.", MessageType.None);
        }

        private void RefreshContext()
        {
            activeScene = SceneManager.GetActiveScene();
            settings = PrototypeUILayoutBindingSettings.LoadOrCreateAsset();
            popupSettings = PopupInteractionBindingSettings.LoadOrCreateAsset();
            uiManager = FindComponentInScene<UIManager>(activeScene);
            designController = FindComponentInScene<PrototypeUIDesignController>(activeScene);
            SeedPopupBindingsFromScene();
            managedNames.Clear();
            managedNameSet.Clear();
            foreach (string managedName in PrototypeUISceneLayoutCatalog.GetManagedCanvasObjectNames(IsHubScene(activeScene)))
            {
                if (!string.IsNullOrWhiteSpace(managedName))
                {
                    managedNames.Add(managedName);
                    managedNameSet.Add(managedName);
                }
            }

            managedNames.Sort(string.CompareOrdinal);
            if (string.IsNullOrWhiteSpace(selectedRuntimeName) || !managedNames.Contains(selectedRuntimeName))
            {
                selectedRuntimeName = managedNames.Count > 0 ? managedNames[0] : null;
            }
        }

        private void SetPopupConnectionObject(PopupConnectionDef def, GameObject target)
        {
            if (popupSettings == null)
            {
                popupSettings = PopupInteractionBindingSettings.LoadOrCreateAsset();
            }

            if (popupSettings == null)
            {
                status = "팝업 연결 자산을 불러오지 못했습니다.";
                statusType = MessageType.Warning;
                return;
            }

            if (target != null && (EditorUtility.IsPersistent(target) || target.scene != activeScene))
            {
                status = "활성 씬에 있는 GameObject만 팝업 연결 오브젝트로 사용할 수 있습니다.";
                statusType = MessageType.Warning;
                return;
            }

            Undo.RecordObject(popupSettings, "Edit Popup Interaction Binding");
            if (target == null)
            {
                popupSettings.RemoveBinding(def.Key);
                SyncPopupConnectionComponents(def, null);
                SavePopupSettings($"{def.Label} 연결을 해제했습니다.", MessageType.Info);
                return;
            }

            popupSettings.SetBindingSource(def.Key, BuildSceneObjectPath(target.transform));
            SyncPopupConnectionComponents(def, target);
            Selection.activeObject = target;
            SavePopupSettings($"{target.name}을(를) {def.Label} 대상으로 저장했습니다.", MessageType.Info);
        }

        private void SyncPopupConnectionComponents(PopupConnectionDef def, GameObject target)
        {
            List<Component> connected = FindConnectionComponents(def);
            if (target != null && target.GetComponent(def.ComponentType) == null)
            {
                Undo.AddComponent(target, def.ComponentType);
                MarkSceneDirty(target);
            }

            for (int i = 0; i < connected.Count; i++)
            {
                Component component = connected[i];
                if (component == null)
                {
                    continue;
                }

                if (target != null && component.gameObject == target)
                {
                    continue;
                }

                GameObject owner = component.gameObject;
                Undo.DestroyObjectImmediate(component);
                if (owner != null)
                {
                    MarkSceneDirty(owner);
                }
            }
        }

        private void UpdatePopupConnectionMemo(string popupKey, string memoText)
        {
            if (popupSettings == null || string.IsNullOrWhiteSpace(popupKey))
            {
                return;
            }

            Undo.RecordObject(popupSettings, "Edit Popup Interaction Memo");
            popupSettings.SetBindingMemo(popupKey, memoText);
            SavePopupSettings("팝업 연결 메모를 저장했습니다.", MessageType.Info);
        }

        private void UpdateSelectedBindingMemo(string memoText)
        {
            if (settings == null || string.IsNullOrWhiteSpace(selectedRuntimeName))
            {
                return;
            }

            Undo.RecordObject(settings, "Edit UI Layout Binding Memo");
            settings.SetBindingMemo(selectedRuntimeName, memoText);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private void UpdateSelectedBindingSprite(Sprite sprite, Image sourceImage)
        {
            if (settings == null || string.IsNullOrWhiteSpace(selectedRuntimeName))
            {
                return;
            }

            Undo.RecordObject(settings, "Edit UI Layout Binding Sprite");
            settings.SetBindingSprite(selectedRuntimeName, sprite, sourceImage);
            SaveSettings(
                sprite != null ? "연결 스프라이트를 저장했습니다." : "연결 스프라이트를 해제했습니다.",
                MessageType.Info);
        }

        private void SetLayoutBindingObject(GameObject target)
        {
            if (settings == null || string.IsNullOrWhiteSpace(selectedRuntimeName))
            {
                return;
            }

            if (target != null && (EditorUtility.IsPersistent(target) || target.scene != activeScene))
            {
                status = "활성 씬에 있는 GameObject만 UI binding 대상으로 사용할 수 있습니다.";
                statusType = MessageType.Warning;
                return;
            }

            Undo.RecordObject(settings, "Edit UI Layout Binding");
            if (target == null)
            {
                settings.ClearBinding(selectedRuntimeName);
                SaveSettings($"{selectedRuntimeName} 연결을 해제했습니다.", MessageType.Info);
                return;
            }

            if (!target.TryGetComponent(out RectTransform rect))
            {
                status = "RectTransform이 있는 UI 오브젝트만 연결할 수 있습니다.";
                statusType = MessageType.Warning;
                return;
            }

            settings.CaptureFromSource(selectedRuntimeName, rect, PrototypeUILayoutBindingSyncUtility.BuildSceneObjectPath(target.transform));
            Selection.activeObject = target;
            SaveSettings($"{selectedRuntimeName} 값을 ui-layout-bindings.asset에 저장했습니다.", MessageType.Info);
        }

        private void CaptureSelectedBindingFromCurrentObject(GameObject target)
        {
            if (settings == null || string.IsNullOrWhiteSpace(selectedRuntimeName) || target == null)
            {
                return;
            }

            if (!target.TryGetComponent(out RectTransform rect))
            {
                status = "현재 UI 오브젝트에서 RectTransform을 찾지 못했습니다.";
                statusType = MessageType.Warning;
                return;
            }

            Undo.RecordObject(settings, "Capture UI Layout Binding");
            settings.CaptureFromSource(selectedRuntimeName, rect, PrototypeUILayoutBindingSyncUtility.BuildSceneObjectPath(target.transform));
            SaveSettings($"{selectedRuntimeName} 현재 값을 다시 캡처했습니다.", MessageType.Info);
        }

        private void ClearSelectedBinding()
        {
            if (settings == null || string.IsNullOrWhiteSpace(selectedRuntimeName))
            {
                return;
            }

            Undo.RecordObject(settings, "Clear UI Layout Binding");
            settings.ClearBinding(selectedRuntimeName);
            SaveSettings($"{selectedRuntimeName} binding을 해제했습니다.", MessageType.Info);
        }

        private void CaptureAllValidBindings()
        {
            if (settings == null)
            {
                return;
            }

            int count;
            Undo.RecordObject(settings, "Capture All UI Layout Bindings");
            count = PrototypeUILayoutBindingSyncUtility.SyncManagedBindingsFromScene(activeScene, settings);
            SaveSettings($"활성 씬의 관리 UI {count}개를 캡처해 저장했습니다.", MessageType.Info);
        }

        private void ApplyScenePreview()
        {
            SaveSettings(null, MessageType.None);
            PrototypeUISceneLayoutCatalog.ReloadBindingSettingsForEditor();
            if (uiManager == null)
            {
                status = "현재 씬에서 UIManager를 찾지 못했습니다.";
                statusType = MessageType.Warning;
                return;
            }

            if (designController == null)
            {
                designController = Undo.AddComponent<PrototypeUIDesignController>(uiManager.gameObject);
            }

            designController.Configure(uiManager);
            designController.ApplyEditorPreviewInEditor();
            uiManager.OrganizeCanvasHierarchyInEditor();
            MarkSceneDirty(uiManager.gameObject);
            status = "현재 binding 기준으로 씬 프리뷰를 적용했습니다.";
            statusType = MessageType.Info;
        }

        private void SaveSettings(string message, MessageType type)
        {
            if (settings == null)
            {
                return;
            }

            MarkSettingsDirty();
            AssetDatabase.SaveAssets();
            PrototypeUISceneLayoutCatalog.ReloadBindingSettingsForEditor();
            if (!string.IsNullOrWhiteSpace(message))
            {
                status = message;
                statusType = type;
            }
        }

        private void SavePopupSettings(string message, MessageType type)
        {
            if (popupSettings == null)
            {
                return;
            }

            MarkPopupSettingsDirty();
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrWhiteSpace(message))
            {
                status = message;
                statusType = type;
            }
        }

        private void MarkSettingsDirty()
        {
            settings.SortBindings();
            EditorUtility.SetDirty(settings);
        }

        private void MarkPopupSettingsDirty()
        {
            popupSettings.SortBindings();
            EditorUtility.SetDirty(popupSettings);
        }

        private void MarkSceneDirty(GameObject target)
        {
            EditorUtility.SetDirty(target);
            if (target.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(target.scene);
            }
        }

        private bool TryGetConnection(string key, out PopupConnectionDef def)
        {
            PopupConnectionDef[] popupConnections = GetActivePopupConnections();
            for (int i = 0; i < popupConnections.Length; i++)
            {
                if (string.Equals(popupConnections[i].Key, key, StringComparison.Ordinal))
                {
                    def = popupConnections[i];
                    return true;
                }
            }

            def = default;
            return false;
        }

        private bool TryGetPopupBinding(string popupKey, out PopupInteractionBindingEntry entry)
        {
            if (popupSettings != null && popupSettings.TryGetEntry(popupKey, out entry))
            {
                return true;
            }

            entry = null;
            return false;
        }

        private string BuildLayoutBindingStatusLabel(string runtimeName)
        {
            if (string.IsNullOrWhiteSpace(runtimeName) || settings == null)
            {
                return "미연결";
            }

            if (settings.TryGetEntry(runtimeName, out PrototypeUILayoutBindingEntry entry)
                && (entry.ApplyHierarchy
                    || entry.ApplyRect
                    || entry.ApplyImage
                    || entry.ApplyText
                    || entry.ApplyButton
                    || !string.IsNullOrWhiteSpace(entry.SceneObjectPath)))
            {
                return "연결";
            }

            return PrototypeUILayoutBindingSyncUtility.ResolveLayoutPreviewObject(activeScene, settings, runtimeName) != null
                ? "후보"
                : "미연결";
        }

        private string BuildPopupConnectionStatusLabel(PopupConnectionDef def, PopupInteractionBindingEntry entry, List<Component> connected)
        {
            GameObject assetTarget = ResolveSceneObject(entry?.SceneObjectPath);
            if (entry == null || string.IsNullOrWhiteSpace(entry.SceneObjectPath))
            {
                return connected.Count > 1 ? "경고" : "미연결";
            }

            return TryBuildPopupSyncMessage(def, entry, assetTarget, connected, out _, out _)
                ? "연결"
                : "불일치";
        }

        private bool TryBuildPopupSyncMessage(
            PopupConnectionDef def,
            PopupInteractionBindingEntry entry,
            GameObject assetTarget,
            List<Component> connected,
            out string message,
            out MessageType messageType)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.SceneObjectPath))
            {
                if (connected.Count > 1)
                {
                    message = "자산이 비어 있고 씬에 후보가 여러 개 있어 자동으로 선택하지 않았습니다.";
                    messageType = MessageType.Warning;
                    return false;
                }

                message = "자산 기준 대상이 아직 지정되지 않았습니다.";
                messageType = MessageType.Warning;
                return false;
            }

            if (assetTarget == null)
            {
                message = "자산 기준 대상 경로가 현재 활성 씬에서 resolve되지 않습니다.";
                messageType = MessageType.Warning;
                return false;
            }

            if (assetTarget.GetComponent(def.ComponentType) == null)
            {
                message = "자산 기준 대상에 필요한 station 컴포넌트가 없습니다.";
                messageType = MessageType.Warning;
                return false;
            }

            if (connected.Count == 0)
            {
                message = "현재 씬에는 연결된 station 컴포넌트가 없습니다.";
                messageType = MessageType.Warning;
                return false;
            }

            if (connected.Count > 1)
            {
                message = "같은 popupKey의 station 컴포넌트가 여러 오브젝트에 남아 있습니다.";
                messageType = MessageType.Warning;
                return false;
            }

            Component component = connected[0];
            if (component == null || component.gameObject != assetTarget)
            {
                message = "자산 기준 대상과 현재 씬 station 컴포넌트 대상이 다릅니다.";
                messageType = MessageType.Warning;
                return false;
            }

            message = "자산 기준 대상과 현재 씬 station 컴포넌트가 동기화되어 있습니다.";
            messageType = MessageType.Info;
            return true;
        }

        private void SeedPopupBindingsFromScene()
        {
            if (popupSettings == null || !activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < PopupConnections.Length; i++)
            {
                PopupConnectionDef def = PopupConnections[i];
                if (TryGetPopupBinding(def.Key, out PopupInteractionBindingEntry existing)
                    && !string.IsNullOrWhiteSpace(existing.SceneObjectPath))
                {
                    continue;
                }

                List<Component> connected = FindConnectionComponents(def);
                if (connected.Count != 1 || connected[0] == null)
                {
                    continue;
                }

                if (!changed)
                {
                    Undo.RecordObject(popupSettings, "Seed Popup Interaction Bindings");
                }

                popupSettings.SetBindingSource(def.Key, BuildSceneObjectPath(connected[0].transform));
                changed = true;
            }

            if (changed)
            {
                SavePopupSettings(null, MessageType.None);
            }
        }

        private List<Component> FindConnectionComponents(PopupConnectionDef def)
        {
            List<Component> result = new();
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                if (root != null)
                {
                    result.AddRange(root.GetComponentsInChildren(def.ComponentType, true));
                }
            }

            return result;
        }

        private void DrawSceneHierarchyPreview(string sectionTitle, Transform root, string foldKey)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(sectionTitle, EditorStyles.boldLabel);
            if (root == null)
            {
                EditorGUILayout.HelpBox("연결된 씬 오브젝트가 없습니다.", MessageType.None);
                return;
            }

            DrawSceneHierarchyNode(root, foldKey, 0);
        }

        private void DrawSceneHierarchyNode(Transform current, string foldKey, int indent)
        {
            if (current == null)
            {
                return;
            }

            string path = BuildSceneObjectPath(current);
            string nodeKey = $"{foldKey}:{path}";
            bool hasChildren = current.childCount > 0;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent * TreeIndentWidth);
                if (hasChildren)
                {
                    folds.TryGetValue(nodeKey, out bool open);
                    open = EditorGUILayout.Foldout(open, string.Empty, true);
                    folds[nodeKey] = open;
                }
                else
                {
                    GUILayout.Space(16f);
                }

                if (GUILayout.Button($"{current.name}{BuildComponentBadge(current.gameObject)}", EditorStyles.miniButton))
                {
                    Selection.activeObject = current.gameObject;
                    EditorGUIUtility.PingObject(current.gameObject);
                }
            }

            if (!hasChildren || !folds.TryGetValue(nodeKey, out bool isOpen) || !isOpen)
            {
                return;
            }

            for (int childIndex = 0; childIndex < current.childCount; childIndex++)
            {
                DrawSceneHierarchyNode(current.GetChild(childIndex), foldKey, indent + 1);
            }
        }

        private static string BuildComponentBadge(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return string.Empty;
            }

            List<string> badges = new();
            if (gameObject.GetComponent<RectTransform>() != null)
            {
                badges.Add("Rect");
            }

            if (gameObject.GetComponent<Image>() != null)
            {
                badges.Add("Image");
            }

            if (gameObject.GetComponent<TextMeshProUGUI>() != null)
            {
                badges.Add("TMP");
            }

            if (gameObject.GetComponent<Button>() != null)
            {
                badges.Add("Button");
            }

            if (gameObject.GetComponent<StorageStation>() != null)
            {
                badges.Add("Storage");
            }

            if (gameObject.GetComponent<RefrigeratorStation>() != null)
            {
                badges.Add("Refrigerator");
            }

            if (gameObject.GetComponent<FrontCounterStation>() != null)
            {
                badges.Add("FrontCounter");
            }

            return badges.Count > 0 ? $" [{string.Join(", ", badges)}]" : string.Empty;
        }

        private bool Matches(string text)
        {
            return string.IsNullOrWhiteSpace(searchText)
                   || (!string.IsNullOrWhiteSpace(text) && text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private GameObject ResolveSceneObject(string path)
        {
            Transform transform = ResolveSceneTransform(path);
            return transform != null ? transform.gameObject : null;
        }

        private Transform ResolveSceneTransform(string path)
        {
            return PrototypeUILayoutBindingSyncUtility.ResolveSceneTransform(activeScene, path);
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            return PrototypeUILayoutBindingSyncUtility.FindComponentInScene<T>(scene);
        }

        private static string BuildSceneObjectPath(Transform transform)
        {
            return PrototypeUILayoutBindingSyncUtility.BuildSceneObjectPath(transform);
        }

        private static bool IsHubScene(Scene scene)
        {
            return PrototypeUILayoutBindingSyncUtility.IsHubScene(scene);
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            RefreshContext();
            Repaint();
        }
    }
}

