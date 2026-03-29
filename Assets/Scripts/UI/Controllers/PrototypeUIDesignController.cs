using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Controllers
{
    using UI;

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
[MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "PrototypeUIDesignController")]
public class PrototypeUIDesignController : MonoBehaviour
{
    // 프리뷰를 적용할 UIManager와 현재 에디터 프리뷰 상태다.
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool showEditorPreview = true;
    [SerializeField] private PrototypeUIPreviewPanel editorPreviewPanel = PrototypeUIPreviewPanel.Recipe;

#if UNITY_EDITOR
    private bool _isApplyingPreview;
#endif

    public UIManager UiManager => uiManager;
    public bool ShowEditorPreview => showEditorPreview;
    public PrototypeUIPreviewPanel EditorPreviewPanel => editorPreviewPanel;

    /*
     * 에디터 확장에서 UIManager를 생성 직후 연결할 수 있게 한다.
     */
    public void Configure(UIManager manager)
    {
        uiManager = manager;
    }

    /*
     * 컴포넌트를 붙였을 때 같은 오브젝트의 UIManager를 기본값으로 잡는다.
     */
    private void Reset()
    {
        SyncUiManagerReference();
    }

#if UNITY_EDITOR
    /*
     * 인스펙터 값이 바뀌면 프리뷰 대상 UIManager 참조를 먼저 맞춘다.
     */
    private void OnValidate()
    {
        if (Application.isPlaying || _isApplyingPreview)
        {
            return;
        }

        SyncUiManagerReference();
    }

    [ContextMenu("Apply Editor UI Preview")]
    /*
     * 플레이 모드 없이 현재 프리뷰 설정을 UIManager에 반영한다.
     */
    public void ApplyEditorPreviewInEditor()
    {
        if (Application.isPlaying || _isApplyingPreview)
        {
            return;
        }

        SyncUiManagerReference();
        if (uiManager == null)
        {
            return;
        }

        _isApplyingPreview = true;
        try
        {
            uiManager.ApplyEditorDesignPreview(showEditorPreview, editorPreviewPanel);
        }
        finally
        {
            _isApplyingPreview = false;
        }
    }

    [ContextMenu("Clear Editor UI Preview")]
    /*
     * 프리뷰 표시 플래그를 끄고 즉시 화면도 정리한다.
     */
    public void ClearEditorPreviewInEditor()
    {
        if (Application.isPlaying)
        {
            return;
        }

        showEditorPreview = false;
        ApplyEditorPreviewInEditor();
    }

#endif

    /*
     * 명시적으로 연결되지 않았으면 같은 GameObject의 UIManager를 찾아 쓴다.
     */
    private void SyncUiManagerReference()
    {
        if (uiManager == null)
        {
            uiManager = GetComponent<UIManager>();
        }
    }
}
}