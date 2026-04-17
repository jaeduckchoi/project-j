using System;
using Exploration.Interaction;
using UnityEngine;

namespace Restaurant.Kitchen
{
    public abstract class BackCounterToolStation : MonoBehaviour, IInteractable
    {
        protected abstract KitchenToolType ToolType { get; }

        public string InteractionPrompt => RestaurantFlowController.GetOrCreate().BuildToolPrompt(ToolType);
        public Transform InteractionTransform => transform;

        public bool CanInteract(GameObject interactor)
        {
            return RestaurantFlowController.GetOrCreate().CanUseTool(ToolType);
        }

        public void Interact(GameObject interactor)
        {
            RestaurantFlowController.GetOrCreate().TryUseTool(ToolType, interactor);
        }
    }

    public sealed class CuttingBoardStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.CuttingBoard;
    }

    public sealed class PotStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.Pot;
    }

    public sealed class FryingPanStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.FryingPan;
    }

    public sealed class FryerStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.Fryer;
    }

    public sealed class RefrigeratorStation : MonoBehaviour, IInteractable
    {
        public static event Action PanelRequested;

        public string InteractionPrompt => "[E] Refrigerator";
        public Transform InteractionTransform => transform;

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            RestaurantFlowController.GetOrCreate();
            PanelRequested?.Invoke();
        }

        public static void RequestPanel()
        {
            PanelRequested?.Invoke();
        }
    }

    public sealed class FrontCounterStation : MonoBehaviour, IInteractable
    {
        public static event Action PanelRequested;

        public string InteractionPrompt => "[E] FrontCounter";
        public Transform InteractionTransform => transform;

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            RestaurantFlowController.GetOrCreate();
            PanelRequested?.Invoke();
        }

        public static void RequestPanel()
        {
            PanelRequested?.Invoke();
        }
    }
}
