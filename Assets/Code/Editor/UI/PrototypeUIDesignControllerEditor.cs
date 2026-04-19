using Code.Scripts.UI.Controllers;
using Code.Scripts.UI.Layout;
using Code.Scripts.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.UI
{
    [CustomEditor(typeof(PrototypeUIDesignController))]
    public class PrototypeUIDesignControllerEditor : UnityEditor.Editor
    {
        private const string HubScenePath = ProjectAssetPaths.HubScenePath;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            PrototypeUIDesignController controller = target as PrototypeUIDesignController;
            if (controller == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("UI 레이아웃 편집기에서 현재 씬 오브젝트를 런타임 관리 ID에 연결할 수 있습니다.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("UI 레이아웃 편집기 열기"))
                {
                    PrototypeUILayoutEditorWindow.Open();
                }

                if (GUILayout.Button("프리뷰 적용"))
                {
                    PrototypeUISceneLayoutCatalog.ReloadBindingSettingsForEditor();
                    controller.ApplyEditorPreviewInEditor();
                    MarkSceneDirty(controller);
                }
            }
        }

        public static void ApplyHubRefrigeratorPreviewAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
            PrototypeUIDesignController controller = Object.FindFirstObjectByType<PrototypeUIDesignController>();
            if (controller == null)
            {
                throw new UnityException("Hub scene PrototypeUIDesignController를 찾지 못했습니다.");
            }

            SerializedObject serializedController = new(controller);
            SerializedProperty showPreviewProperty = serializedController.FindProperty("showEditorPreview");
            SerializedProperty previewPanelProperty = serializedController.FindProperty("editorPreviewPanel");
            if (showPreviewProperty == null || previewPanelProperty == null)
            {
                throw new UnityException("PrototypeUIDesignController 프리뷰 직렬화 필드를 찾지 못했습니다.");
            }

            showPreviewProperty.boolValue = true;
            previewPanelProperty.enumValueIndex = (int)PrototypeUIPreviewPanel.Refrigerator;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            controller.ApplyEditorPreviewInEditor();
            ApplySceneRectLayout(controller.transform, PrototypeUIObjectNames.PopupTitle, PrototypeUILayout.HubPopupTitle);
            MarkSceneDirty(controller);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new UnityException("Hub scene 저장에 실패했습니다.");
            }

            Debug.Log("Hub refrigerator editor preview applied and saved.");
        }

        private static void MarkSceneDirty(PrototypeUIDesignController controller)
        {
            if (controller == null)
            {
                return;
            }

            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }

        private static void ApplySceneRectLayout(Transform root, string objectName, PrototypeUIRect layout)
        {
            RectTransform rect = FindChildRecursive(root, objectName) as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = layout.AnchorMin;
            rect.anchorMax = layout.AnchorMax;
            rect.pivot = layout.Pivot;
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.SizeDelta;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            EditorUtility.SetDirty(rect);
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindChildRecursive(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
