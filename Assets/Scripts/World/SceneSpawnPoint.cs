using UnityEngine;

// 씬 이동 후 플레이어를 배치할 위치를 식별하는 스폰 포인트다.
public class SceneSpawnPoint : MonoBehaviour
{
    // GameManager가 장면 진입 후 찾을 식별자다.
    [SerializeField] private string spawnId = "SpawnPoint";

    public string SpawnId => spawnId;

    /*
     * 런타임 보강이나 빌더에서 스폰 식별자를 다시 지정한다.
     */
    public void Configure(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            spawnId = id;
        }
    }

    /*
     * 요청된 스폰 식별자와 현재 포인트가 일치하는지 확인한다.
     */
    public bool Matches(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && spawnId == id;
    }

    /*
     * 식별자가 비어 있으면 오브젝트 이름으로 자동 보정한다.
     */
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(spawnId))
        {
            spawnId = gameObject.name;
        }
    }
}
