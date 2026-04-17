using TMPro;
using UI.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 냉장고 팝업 콘텐츠 루트와 전용 자식 참조를 제공합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RefrigeratorUIComponent : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform storageRoot;
        [SerializeField] private Button[] slotButtons = new Button[PrototypeUILayout.RefrigeratorSlotCount];
        [SerializeField] private Image[] slotIcons = new Image[PrototypeUILayout.RefrigeratorSlotCount];
        [SerializeField] private TextMeshProUGUI[] slotAmountTexts = new TextMeshProUGUI[PrototypeUILayout.RefrigeratorSlotCount];
        [SerializeField] private RectTransform selectedHighlight;
        [SerializeField] private RectTransform infoPanelRoot;
        [SerializeField] private Image infoIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private Button removeZoneButton;
        [SerializeField] private Image removeIcon;
        [SerializeField] private TextMeshProUGUI removeText;
        [SerializeField] private Image dragGhostImage;

        /// <summary>
        /// 냉장고 콘텐츠 루트입니다.
        /// </summary>
        public RectTransform ContentRoot => contentRoot;

        /// <summary>
        /// 슬롯 그리드 루트입니다.
        /// </summary>
        public RectTransform StorageRoot => storageRoot;

        /// <summary>
        /// 슬롯 선택 하이라이트입니다.
        /// </summary>
        public RectTransform SelectedHighlight => selectedHighlight;

        /// <summary>
        /// 정보 패널 루트입니다.
        /// </summary>
        public RectTransform InfoPanelRoot => infoPanelRoot;

        /// <summary>
        /// 정보 패널 아이콘입니다.
        /// </summary>
        public Image InfoIcon => infoIcon;

        /// <summary>
        /// 정보 패널 아이템 이름 텍스트입니다.
        /// </summary>
        public TextMeshProUGUI ItemNameText => itemNameText;

        /// <summary>
        /// 정보 패널 아이템 설명 텍스트입니다.
        /// </summary>
        public TextMeshProUGUI ItemDescriptionText => itemDescriptionText;

        /// <summary>
        /// 제거 구역 버튼입니다.
        /// </summary>
        public Button RemoveZoneButton => removeZoneButton;

        /// <summary>
        /// 제거 아이콘입니다.
        /// </summary>
        public Image RemoveIcon => removeIcon;

        /// <summary>
        /// 제거 텍스트입니다.
        /// </summary>
        public TextMeshProUGUI RemoveText => removeText;

        /// <summary>
        /// 드래그 고스트 이미지입니다.
        /// </summary>
        public Image DragGhostImage => dragGhostImage;

        /// <summary>
        /// 드래그 고스트 RectTransform입니다.
        /// </summary>
        public RectTransform DragGhostRect => dragGhostImage != null ? dragGhostImage.rectTransform : null;

        /// <summary>
        /// 이 콘텐츠가 담당하는 팝업 유형입니다.
        /// </summary>
        public HubPopupUIType PopupType => HubPopupUIType.Refrigerator;

        /// <summary>
        /// 슬롯 버튼을 반환합니다.
        /// </summary>
        public Button GetSlotButton(int index)
        {
            return IsValidSlotIndex(index) ? slotButtons[index] : null;
        }

        /// <summary>
        /// 슬롯 배경 이미지를 반환합니다.
        /// </summary>
        public Image GetSlotBackgroundImage(int index)
        {
            if (!IsValidSlotIndex(index))
            {
                return null;
            }

            Button button = slotButtons[index];
            if (button != null && button.targetGraphic is Image targetImage)
            {
                return targetImage;
            }

            return button != null ? button.GetComponent<Image>() : null;
        }

        /// <summary>
        /// 슬롯 아이콘 이미지를 반환합니다.
        /// </summary>
        public Image GetSlotIcon(int index)
        {
            return IsValidSlotIndex(index) ? slotIcons[index] : null;
        }

        /// <summary>
        /// 슬롯 수량 텍스트를 반환합니다.
        /// </summary>
        public TextMeshProUGUI GetSlotAmountText(int index)
        {
            return IsValidSlotIndex(index) ? slotAmountTexts[index] : null;
        }

        /// <summary>
        /// 현재 씬 구조를 기준으로 냉장고 콘텐츠 참조를 다시 연결합니다.
        /// </summary>
        public void ResolveReferences()
        {
            contentRoot = contentRoot != null ? contentRoot : transform as RectTransform;
            storageRoot = storageRoot != null ? storageRoot : FindTransform(contentRoot, PrototypeUIObjectNames.RefrigeratorStorage) as RectTransform;
            selectedHighlight = selectedHighlight != null
                ? selectedHighlight
                : FindTransform(contentRoot, PrototypeUIObjectNames.RefrigeratorSelectedSlot) as RectTransform;
            infoPanelRoot = infoPanelRoot != null
                ? infoPanelRoot
                : FindTransform(contentRoot, PrototypeUIObjectNames.RefrigeratorInfoPanel) as RectTransform;
            infoIcon = infoIcon != null ? infoIcon : FindComponent<Image>(contentRoot, PrototypeUIObjectNames.RefrigeratorInfoIcon);
            itemNameText = itemNameText != null
                ? itemNameText
                : FindComponent<TextMeshProUGUI>(contentRoot, PrototypeUIObjectNames.RefrigeratorItemNameText);
            itemDescriptionText = itemDescriptionText != null
                ? itemDescriptionText
                : FindComponent<TextMeshProUGUI>(contentRoot, PrototypeUIObjectNames.RefrigeratorItemDescriptionText);
            removeZoneButton = removeZoneButton != null
                ? removeZoneButton
                : FindComponent<Button>(contentRoot, PrototypeUIObjectNames.RefrigeratorRemoveZone);
            removeIcon = removeIcon != null ? removeIcon : FindComponent<Image>(contentRoot, PrototypeUIObjectNames.RefrigeratorRemoveIcon);
            removeText = removeText != null
                ? removeText
                : FindComponent<TextMeshProUGUI>(contentRoot, PrototypeUIObjectNames.RefrigeratorRemoveText);
            dragGhostImage = dragGhostImage != null
                ? dragGhostImage
                : FindComponent<Image>(transform.root, "ItemDragGhost", PrototypeUIObjectNames.RefrigeratorDragGhost);

            EnsureSlotArrays();
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

        private void EnsureSlotArrays()
        {
            if (slotButtons == null || slotButtons.Length != PrototypeUILayout.RefrigeratorSlotCount)
            {
                slotButtons = new Button[PrototypeUILayout.RefrigeratorSlotCount];
            }

            if (slotIcons == null || slotIcons.Length != PrototypeUILayout.RefrigeratorSlotCount)
            {
                slotIcons = new Image[PrototypeUILayout.RefrigeratorSlotCount];
            }

            if (slotAmountTexts == null || slotAmountTexts.Length != PrototypeUILayout.RefrigeratorSlotCount)
            {
                slotAmountTexts = new TextMeshProUGUI[PrototypeUILayout.RefrigeratorSlotCount];
            }

            for (int index = 0; index < PrototypeUILayout.RefrigeratorSlotCount; index++)
            {
                string slotName = $"{PrototypeUIObjectNames.RefrigeratorSlotPrefix}{index + 1:00}";
                string iconName = $"{PrototypeUIObjectNames.RefrigeratorSlotIconPrefix}{index + 1:00}";
                string amountName = $"{PrototypeUIObjectNames.RefrigeratorSlotAmountPrefix}{index + 1:00}";

                slotButtons[index] = slotButtons[index] != null
                    ? slotButtons[index]
                    : FindComponent<Button>(storageRoot, slotName);
                slotIcons[index] = slotIcons[index] != null
                    ? slotIcons[index]
                    : FindComponent<Image>(storageRoot, iconName);
                slotAmountTexts[index] = slotAmountTexts[index] != null
                    ? slotAmountTexts[index]
                    : FindComponent<TextMeshProUGUI>(storageRoot, amountName);
            }
        }

        private static bool IsValidSlotIndex(int index)
        {
            return index >= 0 && index < PrototypeUILayout.RefrigeratorSlotCount;
        }

        private static T FindComponent<T>(Transform root, params string[] names) where T : Component
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
