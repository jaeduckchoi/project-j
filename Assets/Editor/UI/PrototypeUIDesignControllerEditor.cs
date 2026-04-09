using UI;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor.UI
{
    [CustomEditor(typeof(PrototypeUIDesignController))]
    public class PrototypeUIDesignControllerEditor : UnityEditor.Editor
    {
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
            EditorGUILayout.LabelField("UI / Popup SVG Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "[UI]\n"
                + "General Panel: " + PrototypeUISkin.GetPanelResourcePath("TopLeftPanel") + "\n"
                + "General Button: " + PrototypeUISkin.GetButtonResourcePath("RecipePanelButton") + "\n\n"
                + "[Popup]\n"
                + "Popup Frame: " + PrototypeUISkin.GetPanelResourcePath("PopupFrame") + "\n"
                + "Popup Half Frame: " + PrototypeUISkin.GetPanelResourcePath("PopupFrameLeft") + "\n"
                + "Popup Body: " + PrototypeUISkin.GetPanelResourcePath("PopupLeftBody") + "\n"
                + "Popup Item Box: " + PrototypeUISkin.GetPanelResourcePath("PopupLeftItemBox01") + "\n"
                + "Popup Close: " + PrototypeUISkin.GetButtonResourcePath("PopupCloseButton"),
                MessageType.Info);

            EditorGUILayout.HelpBox(
                "[Grouping Rules]\n"
                + "Canvas\n"
                + "- HUDRoot: HUD group objects\n"
                + "- PopupRoot: hub popup group objects\n\n"
                + "HUDRoot\n"
                + "- HUDStatusGroup / HUDActionGroup / HUDBottomGroup\n"
                + "- HUDPanelButtonGroup / HUDOverlayGroup\n"
                + "- InteractionPromptText stays directly under HUDRoot\n\n"
                + "PopupRoot\n"
                + "- PopupShellGroup / PopupFrame / PopupFrameHeader\n"
                + "- PopupOverlay stays under PopupShellGroup\n"
                + "- PopupFrame contains PopupTitle / PopupCloseButton / PopupFrameLeft / PopupFrameRight\n"
                + "- Left and right contents are grouped under PopupFrameLeft / PopupFrameRight\n\n"
                + "PopupLeftBody / PopupRightBody\n"
                + "- ItemBox names use a two-digit suffix\n"
                + "- Each ItemBox contains one matching ItemText\n"
                + "- Preview keeps hierarchy, while regrouping only happens through Canvas Grouping",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Canvas Grouping"))
                {
                    UIManager uiManager = controller.UiManager != null ? controller.UiManager : controller.GetComponent<UIManager>();
                    if (uiManager != null)
                    {
                        uiManager.OrganizeCanvasHierarchyInEditor();
                        MarkSceneDirty(controller);
                    }
                }

                if (GUILayout.Button("Apply Preview"))
                {
                    controller.ApplyEditorPreviewInEditor();
                    MarkSceneDirty(controller);
                }

                if (GUILayout.Button("Refresh SVG Cache"))
                {
                    PrototypeUISkin.ClearGeneratedCache();
                    controller.ApplyEditorPreviewInEditor();
                    MarkSceneDirty(controller);
                }
            }

            if (GUILayout.Button("Reset Canvas UI Layouts"))
            {
                PrototypeUISceneLayoutCatalog.ResetCanvasLayouts();
                controller.ApplyEditorPreviewInEditor();
                MarkSceneDirty(controller);
                Debug.Log("Canvas UI 레이아웃과 표시 값 오버라이드를 초기화했습니다.");
            }

            if (GUILayout.Button("Clear Preview"))
            {
                controller.ClearEditorPreviewInEditor();
                MarkSceneDirty(controller);
            }
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
    }
}
