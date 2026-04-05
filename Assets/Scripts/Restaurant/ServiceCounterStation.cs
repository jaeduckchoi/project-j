using CoreLoop.Core;
using CoreLoop.Flow;
using Exploration.Interaction;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Restaurant 네임스페이스
namespace Restaurant
{
    /// <summary>
    /// 허브에서 현재 선택된 메뉴로 영업을 실행하는 상호작용 지점이다.
    /// 막힌 시간대에서도 상호작용 이유를 안내한다.
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
                if (restaurantManager == null)
                {
                    return "영업대를 준비 중입니다";
                }

                if (GameManager.Instance != null
                    && GameManager.Instance.DayCycle != null
                    && GameManager.Instance.DayCycle.CurrentPhase != DayPhase.AfternoonService)
                {
                    return "탐험 중에는 영업을 시작할 수 없습니다";
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
            return restaurantManager != null;
        }

        /// <summary>
        /// 현재 선택 메뉴로 영업을 시작하고 첫 영업 힌트를 노출합니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (restaurantManager == null)
            {
                return;
            }

            if (GameManager.Instance != null
                && GameManager.Instance.DayCycle != null
                && GameManager.Instance.DayCycle.CurrentPhase != DayPhase.AfternoonService)
            {
                GameManager.Instance.DayCycle.ShowTemporaryGuide("영업은 탐험에서 돌아온 뒤 오후 장사 시간에 시작할 수 있습니다.");
                return;
            }

            restaurantManager.RunServiceForSelectedRecipe();
            GameManager.Instance?.DayCycle?.ShowHintOnce(
                "first_service_start",
                "영업이 끝나면 정산 패널에서 결과를 확인하고 다음 날로 넘어갈 수 있습니다.");
        }
    }
}
