using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// World 네임스페이스
namespace Code.Scripts.Exploration.World
{
    /// <summary>
    /// 씬 이동 뒤 플레이어를 배치할 위치를 식별하는 스폰 포인트입니다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "World", sourceAssembly: "Assembly-CSharp", sourceClassName: "SceneSpawnPoint")]
    public class SceneSpawnPoint : MonoBehaviour
    {
        // GameManager가 씬 진입 시 찾을 식별자입니다.
        [SerializeField] private string spawnId = "SpawnPoint";

        public string SpawnId => spawnId;

        /// <summary>
        /// 빌더나 씬 설정 코드에서 기본 스폰 식별자를 다시 지정합니다.
        /// </summary>
        public void Configure(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                spawnId = id;
            }
        }

        /// <summary>
        /// 요청한 스폰 식별자와 현재 포인트가 일치하는지 확인합니다.
        /// </summary>
        public bool Matches(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && spawnId == id;
        }

        /// <summary>
        /// 식별자가 비어 있으면 오브젝트 이름을 기본값으로 사용합니다.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(spawnId))
            {
                spawnId = gameObject.name;
            }
        }
    }
}
