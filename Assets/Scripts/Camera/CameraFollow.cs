using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Player;
using CameraComponent = UnityEngine.Camera;

// 플레이어를 부드럽게 따라가고, 필요할 때는 맵 바깥으로 카메라가 벗어나지 않게 제한한다.
namespace GameCamera
{
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CameraFollow")]
    public class CameraFollow : MonoBehaviour
    {
    // 추적 대상과 카메라 감쇠 설정이다.
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float smoothTime = 0.15f;
    [SerializeField] private Collider2D mapBounds;

    private CameraComponent targetCamera;
    private Vector3 velocity;

    /*
     * 카메라 컴포넌트 참조를 캐시한다.
     */
    private void Awake()
    {
        targetCamera = GetComponent<CameraComponent>();
    }

    /*
     * 시작 시점에 대상이 비어 있으면 플레이어를 자동으로 연결한다.
     */
    private void Start()
    {
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    /*
     * 매 프레임 대상 위치를 따라가되, 부드럽게 이동한 뒤 경계 안으로 보정한다.
     */
    private void LateUpdate()
    {
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = new(target.position.x, target.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.position = ClampToBounds(smoothedPosition);
    }

    /*
     * 직교 카메라 크기를 고려해 화면이 맵 경계를 넘지 않도록 위치를 제한한다.
     */
    private Vector3 ClampToBounds(Vector3 cameraPosition)
    {
        if (mapBounds == null || targetCamera == null || !targetCamera.orthographic)
        {
            return cameraPosition;
        }

        Bounds bounds = mapBounds.bounds;
        float verticalExtent = targetCamera.orthographicSize;
        float horizontalExtent = verticalExtent * targetCamera.aspect;
        float minX = bounds.min.x + horizontalExtent;
        float maxX = bounds.max.x - horizontalExtent;
        float minY = bounds.min.y + verticalExtent;
        float maxY = bounds.max.y - verticalExtent;

        cameraPosition.x = minX > maxX ? bounds.center.x : Mathf.Clamp(cameraPosition.x, minX, maxX);
        cameraPosition.y = minY > maxY ? bounds.center.y : Mathf.Clamp(cameraPosition.y, minY, maxY);
        return cameraPosition;
    }
    }
}
