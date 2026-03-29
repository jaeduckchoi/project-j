using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Player 네임스페이스
namespace Player
{
    /// <summary>
    /// 플레이어가 맵 경계를 벗어나지 않도록 위치와 속도를 보정한다.
    /// </summary>
    [DisallowMultipleComponent]
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "PlayerBoundsLimiter")]
    public class PlayerBoundsLimiter : MonoBehaviour
    {
        // 이동 허용 영역과 여유 패딩이다.
        [SerializeField] private Collider2D movementBounds;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField, Min(0f)] private float horizontalPadding = 0.55f;
        [SerializeField, Min(0f)] private float verticalPadding = 0.65f;

        private Rigidbody2D _body;

        /// <summary>
        /// 플레이어 물리 참조를 캐시하고 콜라이더 기본값을 맞춘다.
        /// </summary>
        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();

            if (playerCollider == null)
            {
                playerCollider = GetComponent<Collider2D>();
            }
        }

        /// <summary>
        /// 활성화 직후 한 번 즉시 경계 안으로 보정한다.
        /// </summary>
        private void OnEnable()
        {
            ClampInsideBounds();
        }

        /// <summary>
        /// 매 프레임 늦은 시점에 플레이어 위치를 경계 안으로 유지한다.
        /// </summary>
        private void LateUpdate()
        {
            ClampInsideBounds();
        }

        /// <summary>
        /// 현재 위치를 허용 영역 안으로 제한하고, 벽에 닿은 축의 속도를 제거한다.
        /// </summary>
        private void ClampInsideBounds()
        {
            if (movementBounds == null)
            {
                return;
            }

            Bounds areaBounds = movementBounds.bounds;
            if (areaBounds.size.x <= horizontalPadding * 2f || areaBounds.size.y <= verticalPadding * 2f)
            {
                return;
            }

            Vector2 currentPosition = _body != null ? _body.position : (Vector2)transform.position;
            float minX = areaBounds.min.x + horizontalPadding;
            float maxX = areaBounds.max.x - horizontalPadding;
            float minY = areaBounds.min.y + verticalPadding;
            float maxY = areaBounds.max.y - verticalPadding;

            Vector2 clampedPosition = new(
                Mathf.Clamp(currentPosition.x, minX, maxX),
                Mathf.Clamp(currentPosition.y, minY, maxY));

            if ((clampedPosition - currentPosition).sqrMagnitude <= 0.0001f)
            {
                return;
            }

            if (_body != null)
            {
                Vector2 velocity = _body.linearVelocity;

                // 경계에 막힌 축은 속도를 0으로 만들어 벽에 떨리는 현상을 줄인다.
                if (!Mathf.Approximately(clampedPosition.x, currentPosition.x))
                {
                    velocity.x = 0f;
                }

                if (!Mathf.Approximately(clampedPosition.y, currentPosition.y))
                {
                    velocity.y = 0f;
                }

                _body.position = clampedPosition;
                _body.linearVelocity = velocity;
            }

            transform.position = new Vector3(clampedPosition.x, clampedPosition.y, transform.position.z);
        }
    }
}
