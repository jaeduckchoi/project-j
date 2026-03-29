using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// 빌드에서도 generated 게임 데이터 에셋이 유지되도록 참조를 묶어두는 manifest다.
namespace Data
{
    [CreateAssetMenu(
        fileName = "GeneratedGameDataManifest",
        menuName = "Jonggu Restaurant/Data/Generated Game Data Manifest",
        order = 2)]
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "GeneratedGameDataManifest")]
    public class GeneratedGameDataManifest : ScriptableObject
    {
    // 런타임 복구에서 찾을 수 있도록 generated 자원 에셋을 유지합니다.
    [SerializeField] private List<ResourceData> resources = new();

    // 레시피도 동일한 방식으로 참조를 유지해 빌드 스트리핑을 피합니다.
    [SerializeField] private List<RecipeData> recipes = new();

    public IReadOnlyList<ResourceData> Resources => resources;
    public IReadOnlyList<RecipeData> Recipes => recipes;
    }
}