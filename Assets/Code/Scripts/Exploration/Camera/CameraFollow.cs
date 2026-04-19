using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Exploration.Player;
using CameraComponent = UnityEngine.Camera;

// GameCamera 네임스페이스
namespace Exploration.Camera
{
    /// <summary>
    /// 플레이어를 부드럽게 따라가고, 필요할 때는 맵 바깥으로 카메라가 벗어나지 않게 제한한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "GameCamera", sourceAssembly: "Assembly-CSharp", sourceClassName: "CameraFollow")]
    public class CameraFollow : MonoBehaviour
    {
        // 추적 대상과 카메라 감쇠 설정이다.
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float smoothTime = 0.15f;
        [SerializeField] private Collider2D mapBounds;

        private CameraComponent targetCamera;
        private Vector3 velocity;
        private Collider2D boundsOverride;
        private float defaultOrthographicSize;
        private float requestedOrthographicSize;
        private bool initialSnapPending = true;

        /// <summary>
        /// 카메라 컴포넌트 참조를 캐시한다.
        /// </summary>
        private void Awake()
        {
            targetCamera = GetComponent<CameraComponent>();
            if (targetCamera != null)
            {
                defaultOrthographicSize = targetCamera.orthographicSize;
                requestedOrthographicSize = defaultOrthographicSize;
            }
        }

        /// <summary>
        /// 시작 시점에 대상이 비어 있으면 플레이어를 자동으로 연결한다.
        /// </summary>
        private void Start()
        {
            ResolveTargetIfNeeded();
            TryInitialSnapToTarget();
        }

        /// <summary>
        /// 매 프레임 대상 위치를 따라가되, 부드럽게 이동한 뒤 경계 안으로 보정한다.
        /// </summary>
        private void LateUpdate()
        {
            ResolveTargetIfNeeded();
            TryInitialSnapToTarget();

            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = new(target.position.x, target.position.y, transform.position.z);
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
            transform.position = ClampToBounds(smoothedPosition);
        }

        /// <summary>
        /// 인스펙터 참조가 비어 있으면 현재 씬 플레이어를 추적 대상으로 자동 연결한다.
        /// </summary>
        private void ResolveTargetIfNeeded()
        {
            if (target != null)
            {
                return;
            }

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        /// <summary>
        /// 타깃을 처음 확보한 프레임에는 한 번 즉시 스냅해 시작 화면을 경계 안에서 맞춘다.
        /// </summary>
        private void TryInitialSnapToTarget()
        {
            if (!initialSnapPending || target == null)
            {
                return;
            }

            SnapToTrackedTarget();
            initialSnapPending = false;
        }

        /// <summary>
        /// 기본 맵 bounds 위에 방 전용 bounds를 일시적으로 덮어쓴다.
        /// </summary>
        public void SetBoundsOverride(Collider2D bounds, bool snapImmediately = false)
        {
            boundsOverride = bounds;
            velocity = Vector3.zero;

            if (snapImmediately)
            {
                SnapToTrackedTarget();
            }
        }

        /// <summary>
        /// 방 전용 bounds를 해제하고 기본 맵 bounds로 되돌린다.
        /// </summary>
        public void ClearBoundsOverride(bool snapImmediately = false)
        {
            boundsOverride = null;
            velocity = Vector3.zero;

            if (snapImmediately)
            {
                SnapToTrackedTarget();
            }
        }

        /// <summary>
        /// 방 화면처럼 더 가까운 구도를 잡을 때 직교 카메라 크기를 일시적으로 바꾼다.
        /// </summary>
        public void SetOrthographicSizeOverride(float orthographicSize, bool snapImmediately = false)
        {
            if (targetCamera == null)
            {
                return;
            }

            requestedOrthographicSize = Mathf.Max(0.1f, orthographicSize);
            targetCamera.orthographicSize = requestedOrthographicSize;

            if (snapImmediately)
            {
                SnapToTrackedTarget();
            }
        }

        /// <summary>
        /// 일시적으로 바꾼 카메라 줌을 기본값으로 되돌린다.
        /// </summary>
        public void ClearOrthographicSizeOverride(bool snapImmediately = false)
        {
            if (targetCamera == null)
            {
                return;
            }

            requestedOrthographicSize = defaultOrthographicSize;
            targetCamera.orthographicSize = requestedOrthographicSize;

            if (snapImmediately)
            {
                SnapToTrackedTarget();
            }
        }

        /// <summary>
        /// 허브처럼 기본 구도 자체를 다시 잡아야 할 때 직교 카메라 기본값을 갱신한다.
        /// </summary>
        public void SetDefaultOrthographicSize(float orthographicSize, bool snapImmediately = false)
        {
            defaultOrthographicSize = Mathf.Max(0.1f, orthographicSize);

            if (targetCamera == null)
            {
                return;
            }

            requestedOrthographicSize = defaultOrthographicSize;
            targetCamera.orthographicSize = requestedOrthographicSize;

            if (snapImmediately)
            {
                SnapToTrackedTarget();
            }
        }

        /// <summary>
        /// 허브 기본 화면처럼 씬의 기준 bounds 자체를 다시 지정한다.
        /// </summary>
        public void SetDefaultBounds(Collider2D bounds, bool snapImmediately = false)
        {
            mapBounds = bounds;
            velocity = Vector3.zero;

            if (snapImmediately)
            {
                SnapToTrackedTarget();
            }
        }

        /// <summary>
        /// 직교 카메라 크기를 고려해 화면이 맵 경계를 넘지 않도록 위치를 제한한다.
        /// </summary>
        private Vector3 ClampToBounds(Vector3 cameraPosition)
        {
            if (!TryGetEffectiveBounds(out Bounds bounds) || targetCamera == null || !targetCamera.orthographic)
            {
                RestoreRequestedOrthographicSizeIfNeeded();
                return cameraPosition;
            }

            float verticalExtent = ResolveEffectiveOrthographicSize(bounds);
            float horizontalExtent = verticalExtent * targetCamera.aspect;
            float minX = bounds.min.x + horizontalExtent;
            float maxX = bounds.max.x - horizontalExtent;
            float minY = bounds.min.y + verticalExtent;
            float maxY = bounds.max.y - verticalExtent;

            cameraPosition.x = minX > maxX ? bounds.center.x : Mathf.Clamp(cameraPosition.x, minX, maxX);
            cameraPosition.y = minY > maxY ? bounds.center.y : Mathf.Clamp(cameraPosition.y, minY, maxY);
            return cameraPosition;
        }

        /// <summary>
        /// 기본 CameraBounds를 정본으로 유지하면서, 방 override가 있으면 두 영역의 교집합만 유효 bounds로 사용한다.
        /// </summary>
        private bool TryGetEffectiveBounds(out Bounds effectiveBounds)
        {
            if (mapBounds == null && boundsOverride == null)
            {
                effectiveBounds = default;
                return false;
            }

            if (mapBounds == null)
            {
                effectiveBounds = boundsOverride.bounds;
                return true;
            }

            effectiveBounds = mapBounds.bounds;
            if (boundsOverride == null)
            {
                return true;
            }

            Bounds overrideBounds = boundsOverride.bounds;
            if (TryIntersectBounds(effectiveBounds, overrideBounds, out Bounds intersectionBounds))
            {
                effectiveBounds = intersectionBounds;
            }

            return true;
        }

        /// <summary>
        /// 현재 bounds와 화면 비율 안에서 실제 카메라 size를 조정한다.
        /// </summary>
        private float ResolveEffectiveOrthographicSize(Bounds bounds)
        {
            if (targetCamera == null)
            {
                return 0.1f;
            }

            float aspect = Mathf.Max(0.0001f, targetCamera.aspect);
            float requestedSize = Mathf.Max(0.1f, requestedOrthographicSize > 0f ? requestedOrthographicSize : targetCamera.orthographicSize);
            float maxSizeByHeight = Mathf.Max(0.1f, bounds.extents.y);
            float maxSizeByWidth = Mathf.Max(0.1f, bounds.extents.x / aspect);
            float effectiveSize = Mathf.Min(requestedSize, maxSizeByHeight, maxSizeByWidth);

            if (!Mathf.Approximately(targetCamera.orthographicSize, effectiveSize))
            {
                targetCamera.orthographicSize = effectiveSize;
            }

            return effectiveSize;
        }

        /// <summary>
        /// 유효 bounds 제약이 없을 때는 요청된 기본/override size로 카메라를 되돌린다.
        /// </summary>
        private void RestoreRequestedOrthographicSizeIfNeeded()
        {
            if (targetCamera == null)
            {
                return;
            }

            float requestedSize = Mathf.Max(0.1f, requestedOrthographicSize > 0f ? requestedOrthographicSize : targetCamera.orthographicSize);
            if (!Mathf.Approximately(targetCamera.orthographicSize, requestedSize))
            {
                targetCamera.orthographicSize = requestedSize;
            }
        }

        /// <summary>
        /// 두 bounds의 교집합이 있으면 반환한다.
        /// </summary>
        private static bool TryIntersectBounds(Bounds currentBounds, Bounds nextBounds, out Bounds intersectionBounds)
        {
            if (!currentBounds.Intersects(nextBounds))
            {
                intersectionBounds = default;
                return false;
            }

            Vector3 intersectionMin = Vector3.Max(currentBounds.min, nextBounds.min);
            Vector3 intersectionMax = Vector3.Min(currentBounds.max, nextBounds.max);
            intersectionBounds = new Bounds();
            intersectionBounds.SetMinMax(intersectionMin, intersectionMax);
            return true;
        }

        /// <summary>
        /// 방 전환 직후 카메라가 화면 중앙으로 바로 붙도록 즉시 한 번 보정한다.
        /// </summary>
        private void SnapToTrackedTarget()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = new(target.position.x, target.position.y, transform.position.z);
            transform.position = ClampToBounds(desiredPosition);
        }
    }
}
