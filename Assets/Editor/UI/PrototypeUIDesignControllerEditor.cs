using UnityEditor;
using UnityEditor.SceneManagement;
using UI;
using UI.Controllers;
using UI.Layout;
using UI.Style;
using UnityEngine;

// ProjectEditor.UI 네임스페이스
namespace Editor.UI
{
    /// <summary>
    /// 편집 모드에서 UI 프리뷰 적용, SVG 경로 확인, Canvas 레이아웃 동기화를 묶어 제공합니다.
    /// </summary>
    [CustomEditor(typeof(PrototypeUIDesignController))]
    public class PrototypeUIDesignControllerEditor : UnityEditor.Editor
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

            EditorGUILayout.HelpBox(
                "[Canvas Layout Sync]\n"
                + "- 지원하는 Canvas 씬을 저장하면 현재 씬 Canvas 아래 UI의 RectTransform, Image, TMP, Button 값이 자동으로 동기화됩니다.\n"
                + "- Hub 저장 시 공용 UI 오버라이드와 탐험 씬 HUD 기준이 함께 갱신됩니다.\n"
                + "- 탐험 씬 저장 시 현재 씬 Canvas 값만 공용 오버라이드 위에 자동으로 덮어씁니다.\n"
                + "- 같은 이름 UI는 빌더와 UIManager가 저장된 값을 다시 적용합니다.\n"
                + "- 첫 Sync 시 Assets/Resources/Generated/ui-layout-overrides.asset이 자동 생성됩니다.\n"
                + "- 프로토타입 빌드 및 감사는 먼저 Hub Canvas 값을 읽고, 마지막에 현재 열려 있는 씬 Canvas 값을 다시 덮어씁니다.\n\n"
                + "[Tools > Jonggu Restaurant 메뉴 역할]\n"
                + "- 프로토타입 빌드 및 감사: 생성 자산, 기본 씬, 자동 감사를 한 번에 실행합니다.\n"
                + "- 생성 자산 및 씬 다시 만들기: 감사를 제외한 생성 단계만 다시 실행합니다.\n"
                + "- 생성 씬 감사만 실행: 현재 저장된 생성 씬 구조만 점검합니다.",
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

                if (GUILayout.Button("씬 빌더 미리보기"))
                {
                    JongguMinimalPrototypeBuilder.ApplyOpenSceneBuilderPreview();
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
                Debug.Log("Canvas UI 레이아웃, 표시 값, 이름 오버라이드를 초기화했습니다.");
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
