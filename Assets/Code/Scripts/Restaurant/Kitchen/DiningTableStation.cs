using Code.Scripts.CoreLoop.Core;
using Code.Scripts.Exploration.Interaction;
using UnityEngine;

namespace Code.Scripts.Restaurant.Kitchen
{
    /// <summary>
    /// 허브 테이블 하나와 1:1로 연결된 서빙 상호작용 지점이다.
    /// </summary>
    public sealed class DiningTableStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private string tableId = "table";

        /// <summary>
        /// 주문/서빙 모델에서 이 테이블을 식별하는 안정적인 id다.
        /// </summary>
        public string TableId => string.IsNullOrWhiteSpace(tableId) ? string.Empty : tableId.Trim();

        public string InteractionPrompt
        {
            get
            {
                if (!RestaurantFlowController.TryGetExisting(out RestaurantFlowController flow))
                {
                    return string.Empty;
                }

                CustomerServiceController serviceController = GameRuntimeAccess.HubContext != null
                    ? GameRuntimeAccess.HubContext.CustomerServiceController
                    : null;
                if (serviceController == null)
                {
                    return string.Empty;
                }

                OrderTicket ticket = serviceController.GetTicketForTable(this);
                KitchenCarryItem heldItem = flow.Carry.HeldItem;
                return ticket != null
                    && heldItem != null
                    && heldItem.State == KitchenItemState.FinalDish
                    ? $"[E] Serve {ticket.Dish.DisplayName}"
                    : string.Empty;
            }
        }

        public Transform InteractionTransform => transform;

        /// <summary>
        /// 손에 든 완성 요리가 있고 이 테이블에 활성 주문이 있을 때만 상호작용을 허용한다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            return !string.IsNullOrWhiteSpace(InteractionPrompt);
        }

        /// <summary>
        /// 자신의 활성 주문과만 서빙 판정을 수행한다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            CustomerServiceController serviceController = GameRuntimeAccess.HubContext != null
                ? GameRuntimeAccess.HubContext.CustomerServiceController
                : null;
            serviceController?.TryServeHeldDish(this);
        }
    }
}
