using Exploration.Interaction;
using UnityEngine;

namespace Restaurant.Kitchen
{
    public sealed class DiningTableStation : MonoBehaviour, IInteractable
    {
        public string InteractionPrompt
        {
            get
            {
                RestaurantFlowController flow = RestaurantFlowController.GetOrCreate();
                KitchenCarryItem heldItem = flow.Carry.HeldItem;
                return heldItem != null && heldItem.State == KitchenItemState.FinalDish
                    ? "[E] Serve dish"
                    : string.Empty;
            }
        }

        public Transform InteractionTransform => transform;

        public bool CanInteract(GameObject interactor)
        {
            return !string.IsNullOrWhiteSpace(InteractionPrompt);
        }

        public void Interact(GameObject interactor)
        {
            CustomerServiceController serviceController = FindFirstObjectByType<CustomerServiceController>();
            serviceController?.TryServeHeldDish();
        }
    }
}
