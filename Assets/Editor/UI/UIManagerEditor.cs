using UnityEditor;
using UnityEditor.SceneManagement;
using UI;
using UI.Controllers;
using UnityEngine;

// ProjectEditor.UI 네임스페이스
namespace ProjectEditor.UI
{
    /// <summary>
    /// UIManager 인스펙터에서 캔버스 정리와 디자인 컨트롤러 연결 진입점을 제공한다.
    /// </summary>
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : Editor
    {
        /// <summary>
        /// 기본 인스펙터 아래에 UI 구조 정리용 버튼을 추가한다.
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

            // 같은 오브젝트의 디자인 컨트롤러를 찾아 바로 생성하거나 선택할 수 있게 한다.
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

        /// <summary>
        /// 에디터 버튼으로 계층이나 컴포넌트를 바꿨을 때 씬 저장 필요 상태를 남긴다.
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
