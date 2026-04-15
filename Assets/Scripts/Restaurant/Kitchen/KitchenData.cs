using System.Collections.Generic;
using Shared.Data;
using UnityEngine;

namespace Restaurant.Kitchen
{
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

        public void ConfigureRuntime(KitchenItemData item, ResourceData resource, KitchenItemState itemState, int amount)
        {
            itemData = item;
            resourceData = resource;
            state = itemState;
            quantity = Mathf.Max(1, amount);
        }
    }

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
    }

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
