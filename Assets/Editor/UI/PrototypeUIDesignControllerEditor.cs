using UnityEditor;
using UnityEditor.SceneManagement;
using UI;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using UnityEngine;

// ProjectEditor.UI 네임스페이스
namespace ProjectEditor.UI
{
    /// <summary>
    /// 편집 모드에서 UI 프리뷰 적용, SVG 경로 확인, Canvas 레이아웃 동기화를 묶어 제공합니다.
    /// </summary>
    [CustomEditor(typeof(PrototypeUIDesignController))]
    public class PrototypeUIDesignControllerEditor : Editor
    {
        /// <summary>
        /// 기본 인스펙터 아래에 프리뷰와 동기화 보조 버튼을 그립니다.
        /// </summary>
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
                + "- HUDStatusGroup / HUDInventoryGroup / HUDActionGroup\n"
                + "- HUDButtonGroup / HUDPromptGroup / HUDOverlayGroup\n\n"
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

            EditorGUILayout.HelpBox(
                "[Canvas Layout Sync]\n"
                + "- Sync Canvas UI Layouts saves the current RectTransform and Image values under Canvas.\n"
                + "- Image sync stores sprite, type, color, and preserveAspect when an Image exists.\n"
                + "- The first sync automatically creates Assets/Resources/Generated/UI/uiLayoutOverrides.asset.\n"
                + "- Builder, UIManager, and the scene audit will all use those saved values after syncing.\n"
                + "- Build Minimal Prototype also reuses WindHill's HUDRoot for the exploration scenes automatically.",
                MessageType.Info);

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

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sync Canvas UI Layouts"))
                {
                    if (PrototypeUISceneLayoutCatalog.TrySyncCanvasLayoutsFromScene(controller.gameObject.scene, out string message))
                    {
                        MarkSceneDirty(controller);
                        Debug.Log(message);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Canvas Layout Sync", message, "OK");
                    }
                }

                if (GUILayout.Button("Reset Canvas UI Layouts"))
                {
                    PrototypeUISceneLayoutCatalog.ResetCanvasLayouts();
                    controller.ApplyEditorPreviewInEditor();
                    MarkSceneDirty(controller);
                    Debug.Log("Canvas UI layout and image overrides were reset.");
                }
            }

            if (GUILayout.Button("Clear Preview"))
            {
                controller.ClearEditorPreviewInEditor();
                MarkSceneDirty(controller);
            }
        }

        /// <summary>
        /// 에디터 버튼으로 값이 바뀌면 씬 dirty 상태를 함께 표시합니다.
        /// </summary>
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
