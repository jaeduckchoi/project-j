using UnityEditor;
using UnityEditor.SceneManagement;
using UI;
using UI.Controllers;
using UI.Style;
using UnityEngine;

// ProjectEditor.UI 네임스페이스
namespace ProjectEditor.UI
{
    /// <summary>
    /// 편집 모드에서 UI 프리뷰 적용, SVG 경로 확인, 캔버스 정리를 묶어 제공한다.
    /// </summary>
    [CustomEditor(typeof(PrototypeUIDesignController))]
    public class PrototypeUIDesignControllerEditor : Editor
    {
        /// <summary>
        /// 기본 인스펙터 아래에 프리뷰 관련 보조 버튼과 안내 박스를 그린다.
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
                + "일반 패널: " + PrototypeUISkin.GetPanelResourcePath("TopLeftPanel") + "\n"
                + "일반 버튼: " + PrototypeUISkin.GetButtonResourcePath("RecipePanelButton") + "\n\n"
                + "[Popup]\n"
                + "팝업 프레임: " + PrototypeUISkin.GetPanelResourcePath("PopupFrame") + "\n"
                + "팝업 좌우 반프레임: " + PrototypeUISkin.GetPanelResourcePath("PopupFrameLeft") + "\n"
                + "팝업 본문: " + PrototypeUISkin.GetPanelResourcePath("PopupLeftBody") + "\n"
                + "팝업 아이템 박스: " + PrototypeUISkin.GetPanelResourcePath("PopupLeftItemBox01") + "\n"
                + "팝업 닫기: " + PrototypeUISkin.GetButtonResourcePath("PopupCloseButton"),
                MessageType.Info);

            EditorGUILayout.HelpBox(
                "[Grouping Rules]\n"
                + "Canvas\n"
                + "- HUDRoot: HUD 계열 오브젝트\n"
                + "- PopupRoot: 허브 팝업 계열 오브젝트\n\n"
                + "HUDRoot\n"
                + "- HUDStatusGroup / HUDInventoryGroup / HUDActionGroup\n"
                + "- HUDButtonGroup / HUDPromptGroup / HUDOverlayGroup\n\n"
                + "PopupRoot\n"
                + "- PopupShellGroup / PopupFrame / PopupFrameHeader\n"
                + "- PopupOverlay는 PopupShellGroup 아래에 유지\n"
                + "- PopupFrame 안에 PopupTitle / PopupCloseButton / PopupFrameLeft / PopupFrameRight 배치\n"
                + "- 좌우 내용은 PopupFrameLeft / PopupFrameRight 기준으로 정리\n\n"
                + "PopupLeftBody / PopupRightBody\n"
                + "- ItemBox는 2자리 번호를 사용\n"
                + "- 각 ItemBox 아래에는 같은 번호의 ItemText 1개만 배치\n"
                + "- 프리뷰 적용은 계층 유지, 재배치는 'Canvas 그룹 정리'에서만 수행",
                MessageType.None);

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

        /// <summary>
        /// 프리뷰 적용으로 씬 상태가 바뀌면 저장 대상으로 표시한다.
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
