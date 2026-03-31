using UnityEditor;
using UnityEditor.SceneManagement;
using UI;
using UI.Controllers;
using UnityEngine;

// ProjectEditor.UI 네임스페이스
namespace ProjectEditor.UI
{
    /// <summary>
    /// UIManager 인스펙터에서 Canvas 정리와 디자인 컨트롤러 진입점을 함께 제공한다.
    /// </summary>
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : Editor
    {
        /// <summary>
        /// 기본 인스펙터 아래에 편집 보조 버튼을 추가한다.
        /// </summary>
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

            // 같은 오브젝트의 디자인 컨트롤러를 바로 연결하거나 선택할 수 있게 유지한다.
            PrototypeUIDesignController controller = uiManager.GetComponent<PrototypeUIDesignController>();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Design", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("UI 프리뷰와 SVG 경로 확인은 PrototypeUIDesignController에서 함께 관리합니다.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Canvas 그룹 정리"))
                {
                    uiManager.OrganizeCanvasHierarchyInEditor();
                    MarkSceneDirty(uiManager.gameObject);
                }

                if (GUILayout.Button("씬 빌더 미리보기"))
                {
                    ProjectEditor.JongguMinimalPrototypeBuilder.ApplyOpenSceneBuilderPreview();
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

        /// <summary>
        /// 편집기 버튼으로 값이 바뀌면 씬 dirty 상태를 함께 표시한다.
        /// </summary>
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
}
