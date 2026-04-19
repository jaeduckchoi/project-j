using CoreLoop.Core;
using Exploration.Interaction;
using Restaurant.Kitchen;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Restaurant 네임스페이스
namespace Restaurant
{
    /// <summary>
    /// 허브에서 현재 선택된 메뉴로 영업을 실행하는 상호작용 지점이다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "ServiceCounterStation")]
    public class ServiceCounterStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private RestaurantManager restaurantManager;
        [SerializeField] private string promptLabel = "영업 시작";

        public string InteractionPrompt
        {
            get
            {
                if (TryGetSiblingInteractable(out IInteractable siblingInteractable)
                    && !string.IsNullOrWhiteSpace(siblingInteractable.InteractionPrompt))
                {
                    return siblingInteractable.InteractionPrompt;
                }

                if (RestaurantFlowController.TryGetLegacyPrompt(gameObject, promptLabel, out string kitchenPrompt))
                {
                    return kitchenPrompt;
                }

                if (restaurantManager == null)
                {
                    return "영업대를 준비 중입니다";
                }

                return restaurantManager.IsRestaurantOpen
                    ? "직접 영업 시작은 비활성화되었습니다"
                    : "OPEN/CLOSE는 HUD에서 진행하세요";
            }
        }

        public Transform InteractionTransform => transform;

        /// <summary>
        /// 허브 컨텍스트가 있으면 명시적 참조를 사용합니다.
        /// </summary>
        private void Awake()
        {
            if (restaurantManager == null && HubRuntimeContext.Active != null)
            {
                restaurantManager = HubRuntimeContext.Active.RestaurantManager;
            }
        }

        /// <summary>
        /// RestaurantManager 만 있으면 안내 문구를 보여줄 수 있으므로 true 를 반환합니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            if (TryGetSiblingInteractable(out IInteractable siblingInteractable))
            {
                return siblingInteractable.CanInteract(interactor);
            }

            if (RestaurantFlowController.IsLegacyKitchenStation(gameObject, promptLabel))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 선택 메뉴로 영업을 시작하고 첫 영업 힌트를 노출합니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (TryGetSiblingInteractable(out IInteractable siblingInteractable))
            {
                siblingInteractable.Interact(interactor);
                return;
            }

            if (RestaurantFlowController.TryHandleLegacyInteract(gameObject, promptLabel, interactor))
            {
                return;
            }

            if (restaurantManager == null)
            {
                return;
            }

            GameManager.Instance?.DayCycle?.ShowTemporaryGuide(
                "직접 영업 시작은 사용하지 않습니다. 오늘의 메뉴를 정하고 OPEN 후 조리와 서빙을 진행하세요.");
        }

        /// <summary>
        /// 같은 오브젝트에 더 구체적인 상호작용 컴포넌트가 있으면 legacy 프록시보다 우선 사용합니다.
        /// </summary>
        private bool TryGetSiblingInteractable(out IInteractable interactable)
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                MonoBehaviour behaviour = behaviours[index];
                if (behaviour == null || ReferenceEquals(behaviour, this))
                {
                    continue;
                }

                if (behaviour is IInteractable siblingInteractable)
                {
                    interactable = siblingInteractable;
                    return true;
                }
            }

            interactable = null;
            return false;
        }
    }
}
