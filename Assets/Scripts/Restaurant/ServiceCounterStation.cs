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

                if (restaurantManager.SelectedRecipe == null)
                {
                    return "메뉴를 먼저 고르세요";
                }

                if (!restaurantManager.CanServe(restaurantManager.SelectedRecipe))
                {
                    return "재료가 부족합니다";
                }

                return $"[E] {promptLabel}";
            }
        }

        public Transform InteractionTransform => transform;

        /// <summary>
        /// 허브에 배치된 RestaurantManager 참조를 자동으로 찾습니다.
        /// </summary>
        private void Awake()
        {
            if (Application.isPlaying)
            {
                RestaurantFlowController.GetOrCreate();
            }

            if (restaurantManager == null)
            {
                restaurantManager = FindFirstObjectByType<RestaurantManager>();
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

            return restaurantManager != null;
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

            restaurantManager.RunServiceForSelectedRecipe();
            GameManager.Instance?.DayCycle?.ShowHintOnce(
                "first_service_start",
                "영업 결과는 하단 안내와 요리 패널에서 바로 확인할 수 있습니다.");
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
