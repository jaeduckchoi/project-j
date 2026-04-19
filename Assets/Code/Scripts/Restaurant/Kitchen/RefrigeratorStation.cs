using System;
using Code.Scripts.Exploration.Interaction;
using UnityEngine;

namespace Code.Scripts.Restaurant.Kitchen
{
    /// <summary>
    /// 냉장고 팝업을 열도록 UI 계층에 요청하는 상호작용 지점입니다.
    /// </summary>
    public sealed class RefrigeratorStation : MonoBehaviour, IInteractable
    {
        public static event Action PanelRequested;
        public static Action RuntimePanelOpener;

        public string InteractionPrompt => "[E] Refrigerator";
        public Transform InteractionTransform => transform;

        /// <summary>
        /// 냉장고는 허브 상태에서 UI가 접근 가능 여부를 판단하므로 항상 상호작용 대상으로 둡니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        /// <summary>
        /// 냉장고 팝업 표시를 요청합니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (RestaurantFlowController.GetOrCreate() == null)
            {
                return;
            }

            NotifyPanelRequested();
        }

        /// <summary>
        /// legacy 상호작용 경로에서 냉장고 팝업을 열 때 사용합니다.
        /// </summary>
        public static void RequestPanel()
        {
            NotifyPanelRequested();
        }

        /// <summary>
        /// UI 계층이 등록한 런타임 콜백이 있으면 이벤트와 함께 냉장고 팝업 표시를 요청합니다.
        /// </summary>
        private static void NotifyPanelRequested()
        {
            PanelRequested?.Invoke();
            RuntimePanelOpener?.Invoke();
        }
    }
}
