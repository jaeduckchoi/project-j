using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(UIManager))]
public class UIManagerEditor : Editor
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
        EditorGUILayout.HelpBox("UI 프리뷰와 SVG 경로 확인은 PrototypeUIDesignController에서 분리 관리합니다.", MessageType.Info);

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
