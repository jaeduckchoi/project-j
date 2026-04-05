using System;
using System.Collections.Generic;
using Shared.Data;
using Restaurant;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Exploration.World
{
    /// <summary>
    /// 허브 상단 메뉴판의 제목과 슬롯 강조 상태를 현재 레시피 선택 상태에 맞춰 갱신한다.
    /// 아이콘 자체는 고정 아트로 두고, 슬롯 배경과 표시 여부만 런타임에서 조정한다.
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
        /// 이후 금일 판매 메뉴를 별도 로직으로 고정하고 싶을 때 외부에서 표시 목록만 따로 주입할 수 있게 둔다.
        /// 목록을 주지 않으면 RestaurantManager 앞 3개 레시피를 그대로 사용한다.
        /// </summary>
        public void SetDisplayRecipes(IReadOnlyList<RecipeData> recipes)
        {
            displayRecipes = recipes ?? Array.Empty<RecipeData>();
            RefreshDisplay();
        }

        /// <summary>
        /// 현재 선택된 메뉴와 표시 가능한 메뉴 수에 맞춰 메뉴판 슬롯 색과 아이콘 표시 상태를 조정한다.
        /// </summary>
        public void RefreshDisplay()
        {
            if (headerLabel != null)
            {
                headerLabel.text = headerText;
            }

            IReadOnlyList<RecipeData> recipes = ResolveDisplayRecipes();
            int slotCount = Mathf.Max(
                menuBackdrops != null ? menuBackdrops.Length : 0,
                menuIcons != null ? menuIcons.Length : 0);

            for (int i = 0; i < slotCount; i++)
            {
                RecipeData recipe = i < recipes.Count ? recipes[i] : null;
                bool isSelected = restaurantManager != null && recipe != null && recipe == restaurantManager.SelectedRecipe;

                if (menuBackdrops != null && i < menuBackdrops.Length && menuBackdrops[i] != null)
                {
                    menuBackdrops[i].color = recipe == null
                        ? HubRoomLayout.TodayMenuEmptyBackdropColor
                        : isSelected
                            ? HubRoomLayout.TodayMenuSelectedBackdropColor
                            : HubRoomLayout.TodayMenuBackdropColor;
                }

                if (menuIcons != null && i < menuIcons.Length && menuIcons[i] != null)
                {
                    bool hasRecipe = recipe != null && menuIcons[i].sprite != null;
                    menuIcons[i].enabled = hasRecipe;
                    menuIcons[i].color = hasRecipe
                        ? HubRoomLayout.TodayMenuIconColor
                        : HubRoomLayout.TodayMenuEmptyIconColor;
                }
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

            subscribedRestaurant.SelectedRecipeChanged += HandleSelectedRecipeChanged;
        }

        private void UnbindRestaurant()
        {
            if (subscribedRestaurant == null)
            {
                return;
            }

            subscribedRestaurant.SelectedRecipeChanged -= HandleSelectedRecipeChanged;
            subscribedRestaurant = null;
        }

        private void HandleSelectedRecipeChanged(RecipeData _)
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
                ? restaurantManager.AvailableRecipes
                : Array.Empty<RecipeData>();
        }
    }
}
