using System.Collections.Generic;
using Code.Scripts.CoreLoop.Core;
using Code.Scripts.Exploration.Player;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// World 네임스페이스
namespace Code.Scripts.Exploration.World
{
    /// <summary>
    /// 주기에 따라 켜지고 꺼지는 강풍 구간이다. 활성 상태에서는 플레이어를 한 방향으로 밀어낸다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [MovedFrom(false, sourceNamespace: "World", sourceAssembly: "Assembly-CSharp", sourceClassName: "WindGustZone")]
    public class WindGustZone : MonoBehaviour
    {
        // 바람 방향, 세기, 주기, 안내 문구를 인스펙터에서 조정한다.
        [SerializeField] private Vector2 gustDirection = Vector2.right;
        [SerializeField, Min(0f)] private float gustStrength = 2.5f;
        [SerializeField, Min(0.25f)] private float activeDuration = 2f;
        [SerializeField, Min(0.25f)] private float inactiveDuration = 1.5f;
        [SerializeField] private bool startActive = true;
        [SerializeField, TextArea] private string activeGuideText = "강풍이 불 때는 버티기보다 안전한 위치를 잡고 멈추는 편이 좋습니다.";
        [SerializeField, TextArea] private string calmGuideText = "바람이 멈춘 짧은 틈에 이동하면 안전합니다.";
        [SerializeField] private string hintIdPrefix = "wind_zone";

        private readonly HashSet<PlayerController> playersInZone = new();
        private Collider2D triggerCollider;
        private bool wasActiveLastFrame;

        /// <summary>
        /// 트리거를 준비하고 초기 활성 상태를 기억한다.
        /// </summary>
        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;
            wasActiveLastFrame = IsActiveNow();
        }

        /// <summary>
        /// 바람 주기 변화를 감지하고 영역 안 플레이어에게 힘을 적용한다.
        /// </summary>
        private void Update()
        {
            bool isActive = IsActiveNow();
            if (isActive != wasActiveLastFrame)
            {
                wasActiveLastFrame = isActive;

                if (playersInZone.Count > 0)
                {
                    string hintText = isActive ? activeGuideText : calmGuideText;
                    string hintId = isActive ? $"{hintIdPrefix}_active" : $"{hintIdPrefix}_calm";

                    if (!string.IsNullOrWhiteSpace(hintText))
                    {
                        GameManager.Instance?.DayCycle?.ShowHintOnce(hintId, hintText, 4f);
                    }
                }
            }

            ApplyWindToPlayers(isActive);
        }

        /// <summary>
        /// 새로 들어온 플레이어를 추적 목록에 추가한다.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            playersInZone.Add(player);
            ApplyWind(player, IsActiveNow());
        }

        /// <summary>
        /// 영역 안에 머무는 플레이어가 누락되지 않도록 계속 보정한다.
        /// </summary>
        private void OnTriggerStay2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            playersInZone.Add(player);
            ApplyWind(player, IsActiveNow());
        }

        /// <summary>
        /// 영역을 벗어난 플레이어에게 적용한 외부 속도를 제거한다.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            player.ClearExternalVelocitySource(this);
            playersInZone.Remove(player);
        }

        /// <summary>
        /// 오브젝트 비활성화 시 남아 있는 플레이어 상태를 정리한다.
        /// </summary>
        private void OnDisable()
        {
            foreach (PlayerController player in playersInZone)
            {
                if (player == null)
                {
                    continue;
                }

                player.ClearExternalVelocitySource(this);
            }

            playersInZone.Clear();
        }

        /// <summary>
        /// 현재 영역 안의 모든 플레이어에게 같은 바람 상태를 적용한다.
        /// </summary>
        private void ApplyWindToPlayers(bool isActive)
        {
            foreach (PlayerController player in playersInZone)
            {
                if (player == null)
                {
                    continue;
                }

                ApplyWind(player, isActive);
            }
        }

        /// <summary>
        /// 활성 시 외부 속도를 주고, 비활성 시 외부 속도를 제거한다.
        /// </summary>
        private void ApplyWind(PlayerController player, bool isActive)
        {
            if (player == null)
            {
                return;
            }

            if (!isActive || gustDirection.sqrMagnitude <= 0.0001f || gustStrength <= 0f)
            {
                player.ClearExternalVelocitySource(this);
                return;
            }

            player.SetExternalVelocitySource(this, gustDirection.normalized * gustStrength);
        }

        /// <summary>
        /// 현재 시간이 바람 활성 구간인지 계산한다.
        /// </summary>
        private bool IsActiveNow()
        {
            float cycleLength = Mathf.Max(0.25f, activeDuration + inactiveDuration);
            float cycleTime = Mathf.Repeat(Time.time, cycleLength);

            if (startActive)
            {
                return cycleTime < activeDuration;
            }

            return cycleTime >= inactiveDuration;
        }
    }
}