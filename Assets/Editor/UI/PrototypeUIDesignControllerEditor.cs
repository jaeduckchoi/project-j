using UI;
using UI.Controllers;
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
            EditorGUILayout.LabelField("UI 디자인", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "UI 배치와 저장은 UI 레이아웃 편집기에서 처리합니다. 인스펙터에는 진입과 기본 미리보기만 둡니다.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("UI 레이아웃 편집기 열기"))
                {
                    PrototypeUILayoutEditorWindow.Open();
                }

                if (GUILayout.Button("미리보기 적용"))
                {
                    controller.ApplyEditorPreviewInEditor();
                    MarkSceneDirty(controller);
                }
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
