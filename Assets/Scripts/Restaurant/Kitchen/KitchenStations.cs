using System;
using Exploration.Interaction;
using UnityEngine;

namespace Restaurant.Kitchen
{
    /// <summary>
    /// BackCounter 조리기구 상호작용의 공통 프롬프트와 사용 흐름을 제공합니다.
    /// </summary>
    public abstract class BackCounterToolStation : MonoBehaviour, IInteractable
    {
        protected abstract KitchenToolType ToolType { get; }

        public string InteractionPrompt => RestaurantFlowController.GetOrCreate().BuildToolPrompt(ToolType);
        public Transform InteractionTransform => transform;

        /// <summary>
        /// 현재 손 상태와 조리기구 상태 기준으로 사용할 수 있는지 확인합니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            return RestaurantFlowController.GetOrCreate().CanUseTool(ToolType);
        }

        /// <summary>
        /// 해당 조리기구 타입의 조리 시작 또는 결과물 회수를 요청합니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            RestaurantFlowController.GetOrCreate().TryUseTool(ToolType, interactor);
        }
    }

    /// <summary>
    /// 수동 홀드 진행을 사용하는 도마 스테이션입니다.
    /// </summary>
    public sealed class CuttingBoardStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.CuttingBoard;
    }

    /// <summary>
    /// 냄비 조리 스테이션입니다.
    /// </summary>
    public sealed class PotStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.Pot;
    }

    /// <summary>
    /// 후라이팬 조리 스테이션입니다.
    /// </summary>
    public sealed class FryingPanStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.FryingPan;
    }

    /// <summary>
    /// 튀김기 조리 스테이션입니다.
    /// </summary>
    public sealed class FryerStation : BackCounterToolStation
    {
        protected override KitchenToolType ToolType => KitchenToolType.Fryer;
    }

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
            RestaurantFlowController.GetOrCreate();
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

    /// <summary>
    /// FrontCounter 팝업을 열도록 UI 계층에 요청하는 상호작용 지점입니다.
    /// </summary>
    public sealed class FrontCounterStation : MonoBehaviour, IInteractable
    {
        public static event Action PanelRequested;

        public string InteractionPrompt => "[E] FrontCounter";
        public Transform InteractionTransform => transform;

        /// <summary>
        /// FrontCounter는 세부 가능 여부를 팝업 내부 액션에서 판단하므로 항상 상호작용 대상으로 둡니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        /// <summary>
        /// FrontCounter 팝업 표시를 요청합니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            RestaurantFlowController.GetOrCreate();
            PanelRequested?.Invoke();
        }

        /// <summary>
        /// legacy 상호작용 경로에서 FrontCounter 팝업을 열 때 사용합니다.
        /// </summary>
        public static void RequestPanel()
        {
            PanelRequested?.Invoke();
        }
    }
}
