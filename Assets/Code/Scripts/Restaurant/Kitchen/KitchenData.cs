using System.Collections.Generic;
using Code.Scripts.Shared.Data;
using UnityEngine;

namespace Code.Scripts.Restaurant.Kitchen
{
    /// <summary>
    /// 조리 흐름에서 사용하는 원재료, 중간 결과물, 완성 요리 항목 데이터를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "kitchen-item-data", menuName = "Jonggu Restaurant/Restaurant/Kitchen Item", order = 20)]
    public class KitchenItemData : ScriptableObject
    {
        [SerializeField] private string itemId = "kitchen_item";
        [SerializeField] private string displayName = "Kitchen Item";
        [SerializeField] private KitchenItemState state = KitchenItemState.Raw;
        [SerializeField] private Sprite icon;
        [SerializeField] private ResourceData resource;
        [SerializeField] private string recipeId = string.Empty;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public KitchenItemState State => state;
        public Sprite Icon => icon;
        public ResourceData Resource => resource;
        public string RecipeId => recipeId;

        /// <summary>
        /// generated 데이터나 런타임 fallback에서 주방 항목 값을 구성합니다.
        /// </summary>
        public void ConfigureRuntime(
            string id,
            string itemName,
            KitchenItemState itemState,
            ResourceData linkedResource = null,
            string linkedRecipeId = null,
            Sprite itemIcon = null)
        {
            itemId = string.IsNullOrWhiteSpace(id) ? string.Empty : id;
            displayName = string.IsNullOrWhiteSpace(itemName) ? itemId : itemName;
            state = itemState;
            resource = linkedResource;
            recipeId = string.IsNullOrWhiteSpace(linkedRecipeId) ? string.Empty : linkedRecipeId;
            icon = itemIcon;
        }
    }

    /// <summary>
    /// 조리 단계나 완성 요리 판정에 필요한 단일 항목 조건입니다.
    /// </summary>
    [System.Serializable]
    public class KitchenItemRequirement
    {
        [SerializeField] private KitchenItemData itemData;
        [SerializeField] private ResourceData resourceData;
        [SerializeField] private KitchenItemState state = KitchenItemState.Raw;
        [SerializeField, Min(1)] private int quantity = 1;

        public KitchenItemData ItemData => itemData;
        public ResourceData ResourceData => resourceData;
        public KitchenItemState State => state;
        public int Quantity => Mathf.Max(1, quantity);

        /// <summary>
        /// 아이템 데이터 또는 자원 데이터에서 조합 판정용 id를 해석합니다.
        /// </summary>
        public string RequirementId
        {
            get
            {
                if (itemData != null && !string.IsNullOrWhiteSpace(itemData.ItemId))
                {
                    return itemData.ItemId;
                }

                if (resourceData != null && !string.IsNullOrWhiteSpace(resourceData.ResourceId))
                {
                    return resourceData.ResourceId;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// 런타임 fallback 레시피에서 요구 조건을 구성합니다.
        /// </summary>
        public void ConfigureRuntime(KitchenItemData item, ResourceData resource, KitchenItemState itemState, int amount)
        {
            itemData = item;
            resourceData = resource;
            state = itemState;
            quantity = Mathf.Max(1, amount);
        }
    }

    /// <summary>
    /// 특정 CookingUtensils 기구에서 입력 재료를 결과물로 바꾸는 단일 조리 단계를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "kitchen-stage-recipe", menuName = "Jonggu Restaurant/Restaurant/Kitchen Stage Recipe", order = 21)]
    public class KitchenStageRecipeData : ScriptableObject
    {
        [SerializeField] private string stageId = "stage_recipe";
        [SerializeField] private KitchenToolType toolType = KitchenToolType.FryingPan;
        [SerializeField] private KitchenProgressMode progressMode = KitchenProgressMode.AutoProgress;
        [SerializeField, Min(0.1f)] private float cookingSeconds = 3f;
        [SerializeField] private List<KitchenItemRequirement> inputItems = new();
        [SerializeField] private KitchenItemData outputItem;

        public string StageId => stageId;
        public KitchenToolType ToolType => toolType;
        public KitchenProgressMode ProgressMode => progressMode;
        public float CookingSeconds => Mathf.Max(0.1f, cookingSeconds);
        public IReadOnlyList<KitchenItemRequirement> InputItems => inputItems;
        public KitchenItemData OutputItem => outputItem;
        public string InputSignature => KitchenSignatureUtility.BuildSignature(inputItems);

        /// <summary>
        /// generated 데이터나 런타임 fallback에서 조리 단계 값을 구성합니다.
        /// </summary>
        public void ConfigureRuntime(
            string id,
            KitchenToolType kitchenToolType,
            KitchenProgressMode kitchenProgressMode,
            float seconds,
            IEnumerable<KitchenItemRequirement> requirements,
            KitchenItemData producedItem)
        {
            stageId = string.IsNullOrWhiteSpace(id) ? string.Empty : id;
            toolType = kitchenToolType;
            progressMode = kitchenProgressMode;
            cookingSeconds = Mathf.Max(0.1f, seconds);
            outputItem = producedItem;
            inputItems.Clear();

            if (requirements == null)
            {
                return;
            }

            foreach (KitchenItemRequirement requirement in requirements)
            {
                if (requirement != null)
                {
                    inputItems.Add(requirement);
                }
            }
        }
    }

    /// <summary>
    /// 서빙 가능한 완성 요리와 레거시 finalize 호환 시그니처를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "kitchen-dish-data", menuName = "Jonggu Restaurant/Restaurant/Kitchen Dish", order = 22)]
    public class KitchenDishData : ScriptableObject
    {
        [SerializeField] private string recipeId = "recipe_id";
        [SerializeField] private string displayName = "Dish";
        [SerializeField] private List<KitchenItemRequirement> finalSignatureItems = new();
        [SerializeField] private KitchenItemData finalDishItem;

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public IReadOnlyList<KitchenItemRequirement> FinalSignatureItems => finalSignatureItems;
        public KitchenItemData FinalDishItem => finalDishItem;
        public string FinalSignature => KitchenSignatureUtility.BuildSignature(finalSignatureItems);

        /// <summary>
        /// 런타임 fallback에서 완성 요리 데이터와 최종 시그니처 조건을 구성합니다.
        /// </summary>
        public void ConfigureRuntime(
            string id,
            string dishName,
            IEnumerable<KitchenItemRequirement> signatureItems,
            KitchenItemData dishItem)
        {
            recipeId = string.IsNullOrWhiteSpace(id) ? string.Empty : id;
            displayName = string.IsNullOrWhiteSpace(dishName) ? recipeId : dishName;
            finalDishItem = dishItem;
            finalSignatureItems.Clear();

            if (signatureItems == null)
            {
                return;
            }

            foreach (KitchenItemRequirement item in signatureItems)
            {
                if (item != null)
                {
                    finalSignatureItems.Add(item);
                }
            }
        }
    }
}
