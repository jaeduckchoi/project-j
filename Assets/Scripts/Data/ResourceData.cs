using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Data 네임스페이스
namespace Data
{
    [CreateAssetMenu(
        fileName = "ResourceData",
        menuName = "Jonggu Restaurant/Data/Resource",
        order = 0)]
    /// <summary>
    /// 채집, 요리, 판매에 공통으로 사용하는 자원 정의 데이터다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "ResourceData")]
    public class ResourceData : ScriptableObject
    {
        // 자원을 식별하고 툴팁에 표시할 기본 정보다.
        [Header("Identity")] [SerializeField] private string resourceId = "resource_id";
        [SerializeField] private string displayName = "새 자원";
        [SerializeField, TextArea] private string description = "자원 설명";
        [SerializeField] private string regionTag = "기본 지역";

        // 아이콘과 희귀도처럼 표현에 직접 쓰는 값들이다.
        [Header("Presentation")] [SerializeField]
        private Sprite icon;

        [SerializeField] private ResourceRarity rarity = ResourceRarity.Common;

        // 판매나 보상 계산에 쓰는 경제 값이다.
        [Header("Economy")] [SerializeField, Min(0)]
        private int baseSellPrice = 10;

        public string ResourceId => resourceId;
        public string DisplayName => displayName;
        public string Description => description;
        public string RegionTag => regionTag;
        public Sprite Icon => icon;
        public ResourceRarity Rarity => rarity;
        public int BaseSellPrice => baseSellPrice;
    }

    /// <summary>
    /// 자원의 드문 정도를 대략적으로 표현한다.
    /// </summary>
    public enum ResourceRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }
}
