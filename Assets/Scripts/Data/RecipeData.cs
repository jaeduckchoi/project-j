using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Data 네임스페이스
namespace Data
{
    [CreateAssetMenu(
        fileName = "RecipeData",
        menuName = "Jonggu Restaurant/Data/Recipe",
        order = 1)]
    /// <summary>
    /// 식당에서 판매 가능한 메뉴의 이름, 설명, 가격, 필요 재료를 묶는 데이터 에셋이다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "RecipeData")]
    public class RecipeData : ScriptableObject
    {
        // 레시피 식별자와 기본 표시 정보다.
        [SerializeField] private string recipeId = "recipe_id";
        [SerializeField] private string displayName = "새 메뉴";
        [SerializeField, TextArea] private string description = "메뉴 설명";
        [SerializeField, Min(1)] private int sellPrice = 25;

        [SerializeField] private int reputationDelta = 1;

        // 장사 정산에 사용할 재료 목록이다.
        [SerializeField] private List<RecipeIngredient> ingredients = new();

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public string Description => description;
        public int SellPrice => sellPrice;
        public int ReputationDelta => reputationDelta;
        public IReadOnlyList<RecipeIngredient> Ingredients => ingredients;
    }

    /// <summary>
    /// 레시피 한 개에 들어가는 자원과 수량 한 쌍이다.
    /// </summary>
    [Serializable]
    public class RecipeIngredient
    {
        public ResourceData Resource;
        [Min(1)] public int Amount = 1;
    }
}
