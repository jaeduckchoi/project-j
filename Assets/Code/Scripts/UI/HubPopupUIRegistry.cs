using UnityEngine;

namespace Code.Scripts.UI
{
    /// <summary>
    /// UIComponent 아래에서 공통 shell과 유형별 팝업 콘텐츠를 묶어 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubPopupUIRegistry : MonoBehaviour
    {
        [SerializeField] private PopupFrameUIComponent popupFrame;
        [SerializeField] private RefrigeratorUIComponent refrigeratorUI;

        /// <summary>
        /// 공통 팝업 shell 참조입니다.
        /// </summary>
        public PopupFrameUIComponent PopupFrame => popupFrame;

        /// <summary>
        /// 냉장고 팝업 콘텐츠 참조입니다.
        /// </summary>
        public RefrigeratorUIComponent RefrigeratorUI => refrigeratorUI;

        /// <summary>
        /// 현재 씬 구조를 기준으로 registry 참조를 다시 연결합니다.
        /// </summary>
        public void ResolveReferences()
        {
            Transform searchRoot = transform.parent != null ? transform.parent : transform.root;
            popupFrame = popupFrame != null ? popupFrame : searchRoot.GetComponentInChildren<PopupFrameUIComponent>(true);
            refrigeratorUI = refrigeratorUI != null ? refrigeratorUI : GetComponentInChildren<RefrigeratorUIComponent>(true);

            popupFrame?.ResolveReferences();
            refrigeratorUI?.ResolveReferences();
        }

        /// <summary>
        /// 지정한 유형의 콘텐츠 참조를 반환합니다.
        /// </summary>
        public bool TryGetContent(HubPopupUIType popupType, out RefrigeratorUIComponent content)
        {
            ResolveReferences();
            content = popupType == HubPopupUIType.Refrigerator ? refrigeratorUI : null;
            return content != null;
        }

        private void Reset()
        {
            ResolveReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
