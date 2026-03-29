using Core;
using Flow;
using Interaction;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Restaurant 네임스페이스
namespace Restaurant
{
    /// <summary>
    /// 허브에서 상호작용 시 다음 메뉴로 전환하는 최소 메뉴 선택 오브젝트다.
    /// 막힌 시간대에서도 상호작용 이유를 안내한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "RecipeSelectorStation")]
    public class RecipeSelectorStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private RestaurantManager restaurantManager;
        [SerializeField] private string promptLabel = "메뉴 바꾸기";

        public string InteractionPrompt
        {
            get
            {
                if (restaurantManager == null || restaurantManager.AvailableRecipes.Count == 0)
                {
                    return "등록된 메뉴 없음";
                }

                if (GameManager.Instance != null
                    && GameManager.Instance.DayCycle != null
                    && GameManager.Instance.DayCycle.CurrentPhase != DayPhase.AfternoonService)
                {
                    return "오후 장사 시간에 메뉴를 고를 수 있습니다";
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
        /// 메뉴가 하나라도 있으면 상호작용 대상으로 취급합니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            return restaurantManager != null && restaurantManager.AvailableRecipes.Count > 0;
        }

        /// <summary>
        /// 다음 메뉴로 순환하고 첫 메뉴 선택 힌트를 노출합니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (restaurantManager == null || restaurantManager.AvailableRecipes.Count == 0)
            {
                return;
            }

            if (GameManager.Instance != null
                && GameManager.Instance.DayCycle != null
                && GameManager.Instance.DayCycle.CurrentPhase != DayPhase.AfternoonService)
            {
                GameManager.Instance.DayCycle.ShowTemporaryGuide("메뉴 선택은 오후 장사 준비 시간에 진행할 수 있습니다.");
                return;
            }

            int currentIndex = GetCurrentRecipeIndex();
            int nextIndex = (currentIndex + 1) % restaurantManager.AvailableRecipes.Count;
            restaurantManager.SelectRecipeByIndex(nextIndex);
            GameManager.Instance?.DayCycle?.ShowHintOnce(
                "first_recipe_select",
                "메뉴를 바꾸면 오른쪽 패널에서 필요 재료와 가능한 수량을 바로 확인할 수 있습니다.");
        }

        /// <summary>
        /// 현재 선택된 레시피가 목록의 몇 번째인지 반환합니다.
        /// </summary>
        private int GetCurrentRecipeIndex()
        {
            if (restaurantManager == null || restaurantManager.SelectedRecipe == null)
            {
                return -1;
            }

            for (int index = 0; index < restaurantManager.AvailableRecipes.Count; index++)
            {
                if (restaurantManager.AvailableRecipes[index] == restaurantManager.SelectedRecipe)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
