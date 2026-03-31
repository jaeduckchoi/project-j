using System;
using Player;
using UnityEngine;

// World 네임스페이스
namespace World
{
    /// <summary>
    /// 특정 방 안에 들어왔을 때 카메라 화면과 가림 오브젝트를 전환하는 트리거다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class RoomViewZone : MonoBehaviour
    {
        [SerializeField] private RoomViewController controller;
        [SerializeField] private Collider2D cameraBounds;
        [SerializeField, Min(0)] private int priority;
        [SerializeField, Min(0f)] private float cameraOrthographicSize = 0f;
        [SerializeField] private bool snapCameraOnEnter = true;
        [SerializeField] private GameObject[] hideWhenActive = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] showOnlyWhenActive = Array.Empty<GameObject>();

        private Collider2D _trigger;
        private int _occupantCount;

        public Collider2D CameraBounds => cameraBounds;
        public int Priority => priority;
        public float CameraOrthographicSize => cameraOrthographicSize;
        public bool SnapCameraOnEnter => snapCameraOnEnter;
        public bool IsOccupied => _occupantCount > 0;

        private void Reset()
        {
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }
        }

        private void Awake()
        {
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }

            if (controller == null)
            {
                controller = FindFirstObjectByType<RoomViewController>();
            }

            ApplyPresentation(false);
        }

        private void OnDisable()
        {
            _occupantCount = 0;
            ApplyPresentation(false);
            controller?.NotifyZoneDisabled(this);
        }

        /// <summary>
        /// 빌더와 런타임 보강에서 존 구성 값을 한 번에 다시 설정한다.
        /// </summary>
        public void Configure(
            RoomViewController roomController,
            Collider2D bounds,
            float orthographicSize,
            int zonePriority,
            bool snapOnEnter,
            GameObject[] hiddenObjects,
            GameObject[] shownObjects)
        {
            controller = roomController;
            cameraBounds = bounds;
            cameraOrthographicSize = Mathf.Max(0f, orthographicSize);
            priority = Mathf.Max(0, zonePriority);
            snapCameraOnEnter = snapOnEnter;
            hideWhenActive = hiddenObjects ?? Array.Empty<GameObject>();
            showOnlyWhenActive = shownObjects ?? Array.Empty<GameObject>();
            ApplyPresentation(false);
        }

        public void ApplyPresentation(bool active)
        {
            SetObjectsActive(hideWhenActive, !active);
            SetObjectsActive(showOnlyWhenActive, active);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            _occupantCount++;
            controller?.NotifyZoneEntered(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            _occupantCount = Mathf.Max(0, _occupantCount - 1);
            controller?.NotifyZoneExited(this);
        }

        private static bool IsPlayerCollider(Collider2D other)
        {
            if (other == null || other.isTrigger)
            {
                return false;
            }

            return other.GetComponent<PlayerController>() != null
                || other.GetComponentInParent<PlayerController>() != null;
        }

        private static void SetObjectsActive(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }
    }
}
