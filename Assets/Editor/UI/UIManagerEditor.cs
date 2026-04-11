using UI;
using UI.Controllers;
using UI.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            EditorGUILayout.HelpBox("UI 프리뷰, 에디터 설정 저장, SVG 경로 확인은 PrototypeUIDesignController에서 관리합니다.", MessageType.Info);

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

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("에디터 UI 프리뷰 적용"))
                {
                    if (controller == null)
                    {
                        controller = uiManager.gameObject.AddComponent<PrototypeUIDesignController>();
                        controller.Configure(uiManager);
                    }

                    controller.ApplyEditorPreviewInEditor();
                    MarkSceneDirty(uiManager.gameObject);
                }

                if (GUILayout.Button("현재 씬 UI 설정 저장"))
                {
                    LogLayoutSyncResult(PrototypeUISceneLayoutCatalog.TryOverlayCanvasLayoutsFromScene(uiManager.gameObject.scene, out string message), message);
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

        private static void LogLayoutSyncResult(bool success, string message)
        {
            if (success)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }
        }
    }
}
