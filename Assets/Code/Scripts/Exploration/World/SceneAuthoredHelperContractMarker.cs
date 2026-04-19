using UnityEngine;

namespace Code.Scripts.Exploration.World
{
    /// <summary>
    /// Catalog 관리 대상이 아닌 helper 오브젝트를 scene hierarchy contract 동기화 대상으로 명시합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneAuthoredHelperContractMarker : MonoBehaviour
    {
    }
}
