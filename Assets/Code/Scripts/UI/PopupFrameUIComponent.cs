using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    /// <summary>
    /// 허브 팝업의 공통 shell 오브젝트 묶음을 제공합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupFrameUIComponent : MonoBehaviour
    {
        [SerializeField] private RectTransform frameRoot;
        [SerializeField] private Image frameImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Image dragGhostImage;

        /// <summary>
        /// 공통 팝업 프레임 RectTransform입니다.
        /// </summary>
        public RectTransform FrameRoot => frameRoot;

        /// <summary>
        /// 공통 팝업 프레임 Image입니다.
        /// </summary>
        public Image FrameImage => frameImage;

        /// <summary>
        /// 공통 닫기 버튼입니다.
        /// </summary>
        public Button CloseButton => closeButton;

        /// <summary>
        /// 공통 타이틀 텍스트입니다.
        /// </summary>
        public TextMeshProUGUI TitleText => titleText;

        /// <summary>
        /// 팝업 활성 시 함께 켜지는 overlay 오브젝트입니다.
        /// </summary>
        public GameObject OverlayRoot => overlayRoot;

        /// <summary>
        /// 드래그 고스트 이미지입니다.
        /// </summary>
        public Image DragGhostImage => dragGhostImage;

        /// <summary>
        /// shell 자체의 표시 루트입니다.
        /// </summary>
        public GameObject VisibilityRoot => gameObject;

        /// <summary>
        /// 현재 씬 구조를 기준으로 shell 참조를 다시 연결합니다.
        /// </summary>
        public void ResolveReferences()
        {
            frameRoot = frameRoot != null ? frameRoot : transform as RectTransform;
            frameImage = frameImage != null ? frameImage : GetComponent<Image>();
            closeButton = closeButton != null ? closeButton : FindInHierarchy<Button>(transform, "PopupCloseButton");
            titleText = titleText != null ? titleText : FindInHierarchy<TextMeshProUGUI>(transform, "PopupTitle");
            dragGhostImage = dragGhostImage != null
                ? dragGhostImage
                : FindInHierarchy<Image>(transform, "ItemDragGhost", "RefrigeratorDragGhost");

            if (overlayRoot == null)
            {
                Transform overlayTransform = FindInHierarchy<Transform>(transform.root, "PopupOverlay");
                overlayRoot = overlayTransform != null ? overlayTransform.gameObject : null;
            }
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

        private static T FindInHierarchy<T>(Transform root, params string[] names) where T : Component
        {
            if (root == null || names == null)
            {
                return null;
            }

            foreach (string name in names)
            {
                Transform match = FindTransform(root, name);
                if (match != null && match.TryGetComponent(out T component))
                {
                    return component;
                }
            }

            return null;
        }

        private static Transform FindTransform(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform match = FindTransform(child, objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
