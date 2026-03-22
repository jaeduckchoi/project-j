using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PrototypeUIPreviewPanel
{
    None,
    Storage,
    Recipe,
    Upgrade,
    Materials
}

/*
 * UI 런타임 로직과 별도로 편집 모드 프리뷰와 디자인 확인만 담당합니다.
 */
[RequireComponent(typeof(UIManager))]
[DisallowMultipleComponent]
public class PrototypeUIDesignController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool showEditorPreview = true;
    [SerializeField] private PrototypeUIPreviewPanel editorPreviewPanel = PrototypeUIPreviewPanel.Recipe;

#if UNITY_EDITOR
    private bool isApplyingPreview;
#endif

    public UIManager UiManager => uiManager;
    public bool ShowEditorPreview => showEditorPreview;
    public PrototypeUIPreviewPanel EditorPreviewPanel => editorPreviewPanel;

    public void Configure(UIManager manager)
    {
        uiManager = manager;
    }

    private void Reset()
    {
        SyncUiManagerReference();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || isApplyingPreview)
        {
            return;
        }

        SyncUiManagerReference();
        EditorApplication.delayCall -= ApplyEditorPreviewDelayed;
        EditorApplication.delayCall += ApplyEditorPreviewDelayed;
    }

    [ContextMenu("Apply Editor UI Preview")]
    public void ApplyEditorPreviewInEditor()
    {
        if (Application.isPlaying || this == null || gameObject == null || isApplyingPreview)
        {
            return;
        }

        SyncUiManagerReference();
        if (uiManager == null)
        {
            return;
        }

        isApplyingPreview = true;
        try
        {
            uiManager.OrganizeCanvasHierarchyInEditor();
            uiManager.ApplyEditorDesignPreview(showEditorPreview, editorPreviewPanel);
        }
        finally
        {
            isApplyingPreview = false;
        }
    }

    [ContextMenu("Clear Editor UI Preview")]
    public void ClearEditorPreviewInEditor()
    {
        if (Application.isPlaying || this == null || gameObject == null)
        {
            return;
        }

        showEditorPreview = false;
        ApplyEditorPreviewInEditor();
    }

    private void ApplyEditorPreviewDelayed()
    {
        EditorApplication.delayCall -= ApplyEditorPreviewDelayed;

        if (Application.isPlaying || this == null || gameObject == null || !gameObject.scene.IsValid())
        {
            return;
        }

        ApplyEditorPreviewInEditor();
    }
#endif

    private void SyncUiManagerReference()
    {
        if (uiManager == null)
        {
            uiManager = GetComponent<UIManager>();
        }
    }
}

