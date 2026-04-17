using System;
using System.Collections.Generic;
using Management.Storage;
using Restaurant.Kitchen;
using TMPro;
using UI;
using UI.Controllers;
using UI.Layout;
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

        private static readonly PopupConnectionDef[] TypedPopupConnections =
        {
            new("Refrigerator", "냉장고 팝업", typeof(RefrigeratorStation), "RefrigeratorStation 오브젝트가 PopupFrame + RefrigeratorUI 조합과 연결됩니다.")
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
                "왼쪽 트리에서 팝업 연결이나 Canvas UI 오브젝트를 선택합니다. 팝업 연결은 런타임 컴포넌트와 실제 씬 오브젝트의 관계를 보여주고, 레이아웃 바인딩은 선택한 오브젝트의 표시값을 저장합니다.",
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

                if (GUILayout.Button("유효 연결 전체 캡처", GUILayout.Width(160)))
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
                    "현재 binding 자산이 비어 있습니다. 씬 UI 오브젝트를 연결하면 즉시 캡처 및 저장 대상으로 반영됩니다.",
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

                List<Component> components = FindConnectionComponents(def);
                bool isConnected = components.Count > 0;
                TreeButton(def.Label, BuildConnectionStatusLabel(isConnected), SelectionKind.PopupConnection, def.Key, 1);

                for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
                {
                    Component component = components[componentIndex];
                    if (component != null)
                    {
                        Label(BuildSceneObjectPath(component.transform), 2);
                    }
                }
            }

        }

        private PopupConnectionDef[] GetActivePopupConnections()
        {
            return UsesTypedPopupUi() ? TypedPopupConnections : PopupConnections;
        }

        private bool UsesTypedPopupUi()
        {
            return uiManager != null && uiManager.GetComponentInChildren<HubPopupUIRegistry>(true) != null;
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

        private void DrawSharedPopupTypeTree(string foldKey, string label, int indent)
        {
            if (!Fold(foldKey, label, indent))
            {
                return;
            }

            DrawLayoutNodes(
                new[]
                {
                    PrototypeUIObjectNames.PopupTitle,
                    PrototypeUIObjectNames.PopupLeftCaption,
                    PrototypeUIObjectNames.PopupRightCaption
                },
                indent + 1);

            if (Fold($"{foldKey}-left", "왼쪽 목록", indent + 1))
            {
                LayoutNode("PopupLeftBody", indent + 2);
                if (Fold($"{foldKey}-left-items", "PopupLeft Items", indent + 2))
                {
                    for (int i = 0; i < PrototypeUILayout.HubPopupBodyItemBoxCount; i++)
                    {
                        DrawPopupItemTree($"{foldKey}-left-item", "PopupLeftItemBox", "PopupLeftItemIcon", "PopupLeftItemText", i, indent + 3);
                    }
                }
            }

            if (Fold($"{foldKey}-right", "오른쪽 상세", indent + 1))
            {
                LayoutNode("PopupRightBody", indent + 2);
                LayoutNode("SelectedRecipeText", indent + 2);
            }
        }

        private void DrawPopupItemTree(
            string foldPrefix,
            string boxPrefix,
            string iconPrefix,
            string textPrefix,
            int index,
            int indent)
        {
            string boxName = $"{boxPrefix}{index + 1:00}";
            string iconName = $"{iconPrefix}{index + 1:00}";
            string textName = $"{textPrefix}{index + 1:00}";
            if (!Fold($"{foldPrefix}-{index + 1:00}", boxName, indent))
            {
                return;
            }

            LayoutNode(boxName, indent + 1);
            LayoutNode(iconName, indent + 1);
            LayoutNode(textName, indent + 1);
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

            bool isConnected = settings != null
                               && settings.TryGetEntry(runtimeName, out PrototypeUILayoutBindingEntry entry)
                               && entry.TryGetImageSpriteOverride(out _);
            TreeButton(runtimeName, BuildConnectionStatusLabel(isConnected), SelectionKind.LayoutBinding, runtimeName, indent);
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

        private static string BuildConnectionStatusLabel(bool isConnected)
        {
            return isConnected ? "연결" : "미연결";
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
            List<Component> connected = FindConnectionComponents(def);
            GameObject current = connected.Count > 0 ? connected[0].gameObject : null;

            EditorGUI.BeginChangeCheck();
            GameObject next = EditorGUILayout.ObjectField("팝업 연결 오브젝트", current, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                SetPopupConnectionObject(def, next);
                connected = FindConnectionComponents(def);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 연결", EditorStyles.boldLabel);
            if (connected.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 씬에서 연결 컴포넌트를 찾지 못했습니다.", MessageType.Warning);
                return;
            }

            if (connected.Count > 1)
            {
                EditorGUILayout.HelpBox("같은 팝업에 연결된 오브젝트가 여러 개 있습니다.", MessageType.Warning);
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
            EditorGUILayout.HelpBox("상세 오브젝트는 씬 오브젝트 연결 대신 메모와 스프라이트 override만 관리합니다. 월드 팝업 연결은 '팝업 연결'에서 설정하세요.", MessageType.None);

            settings.TryGetEntry(selectedRuntimeName, out PrototypeUILayoutBindingEntry entry);
            DrawLayoutMemoField(entry);
            settings.TryGetEntry(selectedRuntimeName, out entry);

            GameObject current = FindLayoutPreviewObject(entry);
            Image currentImage = current != null ? current.GetComponent<Image>() : null;
            DrawLayoutSpriteField(entry, currentImage);
            DrawLayoutSpriteHelp(entry, current, currentImage);
            DrawSceneHierarchyPreview("현재 UI 오브젝트 구조", current != null ? current.transform : null, $"layout-tree:{selectedRuntimeName}");
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
            uiManager = FindComponentInScene<UIManager>(activeScene);
            designController = FindComponentInScene<PrototypeUIDesignController>(activeScene);
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
            if (target == null)
            {
                return;
            }

            if (EditorUtility.IsPersistent(target) || target.scene != activeScene)
            {
                status = "활성 씬에 있는 GameObject만 팝업 연결 오브젝트로 사용할 수 있습니다.";
                statusType = MessageType.Warning;
                return;
            }

            if (target.GetComponent(def.ComponentType) == null)
            {
                Undo.AddComponent(target, def.ComponentType);
                MarkSceneDirty(target);
                status = $"{target.name}에 {def.ComponentType.Name} 컴포넌트를 추가했습니다.";
            }
            else
            {
                Selection.activeObject = target;
                status = $"{target.name}은 이미 {def.ComponentType.Name}로 연결되어 있습니다.";
            }

            statusType = MessageType.Info;
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

        private void CaptureAllValidBindings()
        {
            if (settings == null)
            {
                return;
            }

            int count = 0;
            Undo.RecordObject(settings, "Capture All UI Layout Bindings");
            for (int i = 0; i < settings.Bindings.Count; i++)
            {
                PrototypeUILayoutBindingEntry entry = settings.Bindings[i];
                GameObject source = ResolveSceneObject(entry?.SceneObjectPath);
                if (entry != null && managedNameSet.Contains(entry.RuntimeObjectName) && source != null && source.transform is RectTransform rect)
                {
                    settings.CaptureFromSource(entry.RuntimeObjectName, rect, entry.SceneObjectPath);
                    count++;
                }
            }

            SaveSettings($"유효한 레이아웃 연결 {count}개를 캡처해 저장했습니다.", MessageType.Info);
        }

        private GameObject FindLayoutPreviewObject(PrototypeUILayoutBindingEntry entry)
        {
            GameObject current = ResolveSceneObject(entry?.SceneObjectPath);
            if (current != null)
            {
                return current;
            }

            return FindSceneObjectByName(selectedRuntimeName);
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

        private void MarkSettingsDirty()
        {
            settings.SortBindings();
            EditorUtility.SetDirty(settings);
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

        private void DrawSceneHierarchyPreview(string title, Transform root, string foldKey)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
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

        private static bool IsGeneralPopupName(string name)
        {
            return !string.IsNullOrWhiteSpace(name)
                   && !IsRefrigeratorName(name)
                   && (name.StartsWith("PopupLeft", StringComparison.Ordinal)
                       || name.StartsWith("PopupRight", StringComparison.Ordinal)
                       || name == "InventoryText"
                       || name == "StorageText"
                       || name == "SelectedRecipeText"
                       || name == "UpgradeText");
        }

        private static bool IsRefrigeratorName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.StartsWith("Refrigerator", StringComparison.Ordinal);
        }

        private GameObject ResolveSceneObject(string path)
        {
            Transform transform = ResolveSceneTransform(path);
            return transform != null ? transform.gameObject : null;
        }

        private Transform ResolveSceneTransform(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                if (root == null || parts.Length == 0 || root.name != parts[0])
                {
                    continue;
                }

                Transform current = root.transform;
                for (int i = 1; i < parts.Length && current != null; i++)
                {
                    current = current.Find(parts[i]);
                }

                if (current != null)
                {
                    return current;
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root != null ? root.GetComponentInChildren<T>(true) : null;
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private GameObject FindSceneObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform current = transforms[i];
                    if (current != null && string.Equals(current.name, objectName, StringComparison.Ordinal))
                    {
                        return current.gameObject;
                    }
                }
            }

            return null;
        }

        private static string BuildSceneObjectPath(Transform transform)
        {
            Stack<string> segments = new();
            for (Transform current = transform; current != null; current = current.parent)
            {
                segments.Push(current.name);
            }

            return string.Join("/", segments);
        }

        private static bool IsHubScene(Scene scene)
        {
            return scene.IsValid()
                   && (scene.name == "Hub"
                       || scene.path.EndsWith("/Hub.unity", StringComparison.OrdinalIgnoreCase)
                       || scene.path.EndsWith("\\Hub.unity", StringComparison.OrdinalIgnoreCase));
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            RefreshContext();
            Repaint();
        }
    }
}

