using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Data 네임스페이스
namespace Shared.Data
{
    /// <summary>
    /// 빌드에서 generated 게임 데이터 에셋이 유지되도록 참조를 묶어두는 manifest다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "generated-game-data-manifest",
        menuName = "Jonggu Restaurant/Data/Generated Game Data Manifest",
        order = 2)]
    [MovedFrom(false, sourceNamespace: "Data", sourceAssembly: "Assembly-CSharp", sourceClassName: "GeneratedGameDataManifest")]
    public class GeneratedGameDataManifest : ScriptableObject
    {
        // 런타임 복구에서 찾을 수 있도록 generated 자원 에셋을 유지합니다.
        [SerializeField] private List<ResourceData> resources = new();

        // 레시피도 동일한 방식으로 참조를 유지해 빌드 스트리핑을 피합니다.
        [SerializeField] private List<RecipeData> recipes = new();

        public IReadOnlyList<ResourceData> Resources => resources;
        public IReadOnlyList<RecipeData> Recipes => recipes;

#if UNITY_EDITOR
        /// <summary>
        /// CSV importer가 활성 generated 데이터 참조만 manifest에 다시 기록할 때 사용한다.
        /// </summary>
        public void ConfigureGeneratedAssets(IEnumerable<ResourceData> generatedResources, IEnumerable<RecipeData> generatedRecipes)
        {
            resources.Clear();
            if (generatedResources != null)
            {
                foreach (ResourceData resource in generatedResources)
                {
                    if (resource != null)
                    {
                        resources.Add(resource);
                    }
                }
            }

            recipes.Clear();
            if (generatedRecipes != null)
            {
                foreach (RecipeData recipe in generatedRecipes)
                {
                    if (recipe != null)
                    {
                        recipes.Add(recipe);
                    }
                }
            }
        }
#endif
    }
}
