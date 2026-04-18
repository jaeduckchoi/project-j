using System;
using System.Collections.Generic;
using Restaurant;
using Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Exploration.World
{
    /// <summary>
    /// 허브 상단 메뉴판의 3칸 표시를 오늘의 메뉴 상태 기준으로 갱신한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "World", sourceAssembly: "Assembly-CSharp", sourceClassName: "HubTodayMenuDisplay")]
    public class HubTodayMenuDisplay : MonoBehaviour
    {
        [SerializeField] private RestaurantManager restaurantManager;
        [SerializeField] private TextMeshPro headerLabel;
        [SerializeField] private SpriteRenderer[] menuBackdrops = Array.Empty<SpriteRenderer>();
        [SerializeField] private SpriteRenderer[] menuIcons = Array.Empty<SpriteRenderer>();
        [SerializeField] private string headerText = "오늘의 메뉴";

        private RestaurantManager subscribedRestaurant;
        private IReadOnlyList<RecipeData> displayRecipes = Array.Empty<RecipeData>();

        private void OnEnable()
        {
            TryBindRestaurant();
            RefreshDisplay();
        }

        private void Start()
        {
            TryBindRestaurant();
            RefreshDisplay();
        }

        private void OnDisable()
        {
            UnbindRestaurant();
        }

        public void Configure(
            RestaurantManager targetRestaurant,
            TextMeshPro targetHeaderLabel,
            SpriteRenderer[] targetMenuBackdrops,
            SpriteRenderer[] targetMenuIcons)
        {
            restaurantManager = targetRestaurant;
            headerLabel = targetHeaderLabel;
            menuBackdrops = targetMenuBackdrops ?? Array.Empty<SpriteRenderer>();
            menuIcons = targetMenuIcons ?? Array.Empty<SpriteRenderer>();

            TryBindRestaurant();
            RefreshDisplay();
        }

        /// <summary>
        /// 외부 주입 목록이 있으면 그것을, 없으면 RestaurantManager.TodayMenuRecipes를 사용한다.
        /// </summary>
        public void SetDisplayRecipes(IReadOnlyList<RecipeData> recipes)
        {
            displayRecipes = recipes ?? Array.Empty<RecipeData>();
            RefreshDisplay();
        }

        /// <summary>
        /// 현재 오늘의 메뉴 3칸과 활성 슬롯 강조 상태를 다시 그린다.
        /// </summary>
        public void RefreshDisplay()
        {
            if (headerLabel != null)
            {
                headerLabel.text = headerText;
            }

            IReadOnlyList<RecipeData> recipes = ResolveDisplayRecipes();
            int slotCount = Mathf.Max(menuBackdrops != null ? menuBackdrops.Length : 0, menuIcons != null ? menuIcons.Length : 0);
            int selectedSlotIndex = restaurantManager != null ? restaurantManager.SelectedTodayMenuSlotIndex : -1;

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                RecipeData recipe = slotIndex < recipes.Count ? recipes[slotIndex] : null;
                bool isSelectedSlot = slotIndex == selectedSlotIndex;

                if (menuBackdrops != null && slotIndex < menuBackdrops.Length && menuBackdrops[slotIndex] != null)
                {
                    menuBackdrops[slotIndex].color = recipe == null
                        ? HubRoomLayout.TodayMenuEmptyBackdropColor
                        : isSelectedSlot
                            ? HubRoomLayout.TodayMenuSelectedBackdropColor
                            : HubRoomLayout.TodayMenuBackdropColor;
                }

                if (menuIcons == null || slotIndex >= menuIcons.Length || menuIcons[slotIndex] == null)
                {
                    continue;
                }

                SpriteRenderer iconRenderer = menuIcons[slotIndex];
                if (recipe == null)
                {
                    iconRenderer.enabled = false;
                    continue;
                }

                Sprite resolvedSprite = LoadRecipeSprite(recipe);
                if (resolvedSprite != null)
                {
                    iconRenderer.sprite = resolvedSprite;
                }

                bool hasSprite = iconRenderer.sprite != null;
                iconRenderer.enabled = hasSprite;
                iconRenderer.color = hasSprite
                    ? HubRoomLayout.TodayMenuIconColor
                    : HubRoomLayout.TodayMenuEmptyIconColor;
            }
        }

        private void TryBindRestaurant()
        {
            RestaurantManager targetRestaurant = restaurantManager != null
                ? restaurantManager
                : FindFirstObjectByType<RestaurantManager>();

            if (targetRestaurant == subscribedRestaurant)
            {
                return;
            }

            UnbindRestaurant();

            restaurantManager = targetRestaurant;
            subscribedRestaurant = targetRestaurant;

            if (subscribedRestaurant == null)
            {
                return;
            }

            subscribedRestaurant.TodayMenuChanged += HandleTodayMenuChanged;
            subscribedRestaurant.ServiceStateChanged += HandleServiceStateChanged;
        }

        private void UnbindRestaurant()
        {
            if (subscribedRestaurant == null)
            {
                return;
            }

            subscribedRestaurant.TodayMenuChanged -= HandleTodayMenuChanged;
            subscribedRestaurant.ServiceStateChanged -= HandleServiceStateChanged;
            subscribedRestaurant = null;
        }

        private void HandleTodayMenuChanged()
        {
            RefreshDisplay();
        }

        private void HandleServiceStateChanged(bool _)
        {
            RefreshDisplay();
        }

        private IReadOnlyList<RecipeData> ResolveDisplayRecipes()
        {
            if (displayRecipes != null && displayRecipes.Count > 0)
            {
                return displayRecipes;
            }

            return restaurantManager != null
                ? restaurantManager.TodayMenuRecipes
                : Array.Empty<RecipeData>();
        }

        private static Sprite LoadRecipeSprite(RecipeData recipe)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.RecipeId))
            {
                return null;
            }

            string recipeId = recipe.RecipeId.Trim();
            return recipe.Icon != null
                ? recipe.Icon
                : Resources.Load<Sprite>($"Generated/Sprites/Item/Food/{recipeId}")
                ?? Resources.Load<Sprite>($"Generated/Sprites/Recipes/{recipeId}")
                ?? Resources.Load<Sprite>($"Generated/Sprites/Hub/{recipeId}");
        }
    }
}
