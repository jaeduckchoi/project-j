using System.Collections.Generic;
using Code.Scripts.CoreLoop.Core;
using Code.Scripts.Exploration.Player;
using Code.Scripts.Management.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// World 네임스페이스
namespace Code.Scripts.Exploration.World
{
    /// <summary>
    /// 랜턴 보유 여부에 따라 어두운 구역의 이동 페널티를 적용합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [MovedFrom(false, sourceNamespace: "World", sourceAssembly: "Assembly-CSharp", sourceClassName: "DarknessZone")]
    public class DarknessZone : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 1f)] private float noLanternMovementMultiplier = 0.55f;
        [SerializeField, TextArea] private string noLanternGuideText = "랜턴이 있으면 어두운 지형을 더 안전하게 이동할 수 있습니다.";
        [SerializeField] private string hintId = "darkness_zone";

        private readonly HashSet<PlayerController> playersInZone = new();
        private Collider2D triggerCollider;

        /// <summary>
        /// 어둠 지대를 트리거 영역으로 고정합니다.
        /// </summary>
        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;
        }

        /// <summary>
        /// 씬 설정 코드에서 기본 이동 배수와 안내 문구를 보충합니다.
        /// </summary>
        public void Configure(float movementMultiplier, string text = "", string id = "")
        {
            noLanternMovementMultiplier = Mathf.Clamp(movementMultiplier, 0.1f, 1f);

            if (!string.IsNullOrWhiteSpace(text))
            {
                noLanternGuideText = text;
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                hintId = id;
            }
        }

        /// <summary>
        /// 진입 즉시 현재 플레이어 상태를 다시 계산합니다.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            UpdatePlayerState(other);
        }

        /// <summary>
        /// 체류 중 랜턴 획득 여부가 바뀌는 경우를 반영합니다.
        /// </summary>
        private void OnTriggerStay2D(Collider2D other)
        {
            UpdatePlayerState(other);
        }

        /// <summary>
        /// 구역을 벗어나면 이 지대가 준 이동 배수를 제거합니다.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            player.ClearMovementMultiplierSource(this);
            playersInZone.Remove(player);
        }

        /// <summary>
        /// 비활성화될 때 남아 있는 이동 배수 출처를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            foreach (PlayerController player in playersInZone)
            {
                if (player == null)
                {
                    continue;
                }

                player.ClearMovementMultiplierSource(this);
            }

            playersInZone.Clear();
        }

        /// <summary>
        /// 플레이어의 랜턴 보유 여부에 따라 이동 배수와 안내 문구를 적용합니다.
        /// </summary>
        private void UpdatePlayerState(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            playersInZone.Add(player);

            if (HasLantern())
            {
                player.ClearMovementMultiplierSource(this);
                return;
            }

            player.SetMovementMultiplierSource(this, noLanternMovementMultiplier);
            GameManager.Instance?.DayCycle?.ShowHintOnce(hintId, noLanternGuideText);
        }

        /// <summary>
        /// 현재 플레이어가 랜턴 도구를 보유했는지 확인합니다.
        /// </summary>
        private static bool HasLantern()
        {
            return GameManager.Instance != null
                   && GameManager.Instance.Tools != null
                   && GameManager.Instance.Tools.HasTool(ToolType.Lantern);
        }
    }
}
