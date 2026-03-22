using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(PrototypeUIDesignController))]
public class PrototypeUIDesignControllerEditor : Editor
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
            + "일반 패널: " + PrototypeUISkin.GetPanelResourcePath("TopLeftPanel") + "\n"
            + "일반 버튼: " + PrototypeUISkin.GetButtonResourcePath("RecipePanelButton") + "\n\n"
            + "[Popup]\n"
            + "팝업 외곽: " + PrototypeUISkin.GetPanelResourcePath("PopupFrame") + "\n"
            + "팝업 내부: " + PrototypeUISkin.GetPanelResourcePath("PopupLeftBody") + "\n"
            + "팝업 닫기: " + PrototypeUISkin.GetButtonResourcePath("PopupCloseButton"),
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Canvas 그룹 정리"))
            {
                UIManager uiManager = controller.UiManager != null ? controller.UiManager : controller.GetComponent<UIManager>();
                if (uiManager != null)
                {
                    uiManager.OrganizeCanvasHierarchyInEditor();
                    MarkSceneDirty(controller);
                }
            }

            if (GUILayout.Button("현재 설정 프리뷰 적용"))
            {
                controller.ApplyEditorPreviewInEditor();
                MarkSceneDirty(controller);
            }

            if (GUILayout.Button("SVG 캐시 새로고침"))
            {
                PrototypeUISkin.ClearGeneratedCache();
                controller.ApplyEditorPreviewInEditor();
                MarkSceneDirty(controller);
            }
        }

        if (GUILayout.Button("팝업 프리뷰 끄기"))
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
