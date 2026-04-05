using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// UI.Controllers 네임스페이스
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

    /// <summary>
    /// UI 런타임 로직과 분리해 에디터 모드 프리뷰 상태만 관리하는 보조 컨트롤러다.
    /// </summary>
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

        /// <summary>
        /// 에디터 확장에서 UIManager를 생성 직후 연결할 때 사용한다.
        /// </summary>
        public void Configure(UIManager manager)
        {
            uiManager = manager;
        }

        /// <summary>
        /// 컴포넌트를 붙였을 때 같은 오브젝트의 UIManager를 기본값으로 연결한다.
        /// </summary>
        private void Reset()
        {
            SyncUiManagerReference();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터 값이 바뀌면 프리뷰 대상 UIManager 참조를 먼저 맞춘다.
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying || _isApplyingPreview)
            {
                return;
            }

            SyncUiManagerReference();
        }

        /// <summary>
        /// 플레이 모드 없이 현재 프리뷰 설정을 UIManager에 반영한다.
        /// </summary>
        [ContextMenu("Apply Editor UI Preview")]
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

        /// <summary>
        /// 프리뷰 표시 플래그를 끄고 즉시 화면을 정리한다.
        /// </summary>
        [ContextMenu("Clear Editor UI Preview")]
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

        /// <summary>
        /// 명시적으로 연결하지 않았으면 같은 GameObject의 UIManager를 찾는다.
        /// </summary>
        private void SyncUiManagerReference()
        {
            if (uiManager == null)
            {
                uiManager = GetComponent<UIManager>();
            }
        }
    }
}
