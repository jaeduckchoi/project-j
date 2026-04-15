using System.Collections;
using CoreLoop.Core;
using Shared.Data;
using Exploration.Interaction;
using Management.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Gathering 네임스페이스
namespace Exploration.Gathering
{
    /// <summary>
    /// 맵 위의 채집 지점이다. 필요한 도구를 확인하고 상호작용 시 인벤토리에 자원을 추가한다.
    /// 막힌 상태에서도 상호작용을 받아 이유를 안내한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "Gathering", sourceAssembly: "Assembly-CSharp", sourceClassName: "GatherableResource")]
    public class GatherableResource : MonoBehaviour, IInteractable
    {
        [SerializeField] private ResourceData resourceData;
        [SerializeField] private ToolType requiredToolType = ToolType.None;
        [SerializeField, Min(1)] private int minAmount = 1;
        [SerializeField, Min(1)] private int maxAmount = 2;
        [SerializeField] private bool respawnAfterGathering;
        [SerializeField, Min(0.5f)] private float respawnDelay = 10f;
        [SerializeField] private string promptLabel = "채집하기";
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private GameObject visualsRoot;

        private bool isAvailable = true;
        private Coroutine respawnRoutine;

        public string InteractionPrompt
        {
            get
            {
                if (!isAvailable)
                {
                    return string.Empty;
                }

                string blockingReason = GetBlockingReason();
                return string.IsNullOrWhiteSpace(blockingReason)
                    ? $"[E] {promptLabel}"
                    : blockingReason;
            }
        }

        public Transform InteractionTransform => transform;
        public bool HasConfiguredResource => resourceData != null;

        /// <summary>
        /// 채집 오브젝트의 기본 충돌체를 차단 콜라이더로 연결합니다.
        /// </summary>
        private void Awake()
        {
            if (blockingCollider == null)
            {
                blockingCollider = GetComponent<Collider2D>();
            }
        }

        /// <summary>
        /// 씬 설정 코드나 빌더에서 채집 자원과 필요 도구를 다시 설정합니다.
        /// </summary>
        public void Configure(ResourceData resource, ToolType toolType, int minimumAmount = 1, int maximumAmount = 2)
        {
            resourceData = resource;
            requiredToolType = toolType;
            minAmount = Mathf.Max(1, minimumAmount);
            maxAmount = Mathf.Max(minAmount, maximumAmount);
        }

        /// <summary>
        /// Inspector 에서 최소 / 최대 수량이 뒤집히지 않도록 보정합니다.
        /// </summary>
        private void OnValidate()
        {
            if (maxAmount < minAmount)
            {
                maxAmount = minAmount;
            }
        }

        /// <summary>
        /// 자원 데이터가 있고 현재 활성 상태일 때만 상호작용 대상으로 취급합니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            return isAvailable && resourceData != null;
        }

        /// <summary>
        /// 도구와 인벤토리 조건이 맞으면 자원을 획득하고 오브젝트를 숨깁니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            string blockingReason = GetBlockingReason();
            if (!string.IsNullOrWhiteSpace(blockingReason))
            {
                GameManager.Instance?.DayCycle?.ShowTemporaryGuide(blockingReason);
                return;
            }

            // 최소 / 최대 수량 사이에서 실제 획득량을 결정합니다.
            int gatheredAmount = Random.Range(minAmount, maxAmount + 1);
            bool added = GameManager.Instance.Inventory.TryAdd(resourceData, gatheredAmount, out int addedAmount);

            if (!added || addedAmount <= 0)
            {
                GameManager.Instance?.DayCycle?.ShowTemporaryGuide("인벤토리가 가득 차 자원을 더 담을 수 없습니다.");
                return;
            }

            GameManager.Instance.DayCycle?.ShowHintOnce(
                "first_gather_hint",
                "모은 재료는 식당 메뉴와 인벤토리 업그레이드에 바로 사용할 수 있습니다.",
                5f);

            SetAvailable(false);

            if (respawnAfterGathering)
            {
                respawnRoutine = StartCoroutine(RespawnRoutine());
            }
        }

        /// <summary>
        /// 채집을 막는 이유를 계산해 프롬프트와 안내 문구에 재사용합니다.
        /// </summary>
        private string GetBlockingReason()
        {
            if (resourceData == null)
            {
                return "자원 데이터 누락";
            }

            if (GameManager.Instance == null || GameManager.Instance.Inventory == null)
            {
                return "인벤토리 준비 중";
            }

            if (!HasRequiredTool())
            {
                return $"{requiredToolType.GetDisplayName()} 필요";
            }

            if (!GameManager.Instance.Inventory.CanAdd(resourceData))
            {
                return "인벤토리 칸 부족";
            }

            return string.Empty;
        }

        /// <summary>
        /// 현재 플레이어가 이 채집 오브젝트에 필요한 도구를 가지고 있는지 확인합니다.
        /// </summary>
        private bool HasRequiredTool()
        {
            if (requiredToolType == ToolType.None)
            {
                return true;
            }

            return GameManager.Instance != null
                   && GameManager.Instance.Tools != null
                   && GameManager.Instance.Tools.HasTool(requiredToolType);
        }

        /// <summary>
        /// 리스폰 옵션이 켜진 채집 오브젝트를 일정 시간 뒤 다시 활성화합니다.
        /// </summary>
        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            respawnRoutine = null;
            SetAvailable(true);
        }

        /// <summary>
        /// 채집 가능 여부에 맞춰 충돌체와 시각 표현을 함께 토글합니다.
        /// </summary>
        private void SetAvailable(bool available)
        {
            isAvailable = available;

            if (blockingCollider != null)
            {
                blockingCollider.enabled = available;
            }

            // visualsRoot 가 있으면 루트 단위로, 없으면 모든 SpriteRenderer 를 직접 제어합니다.
            if (visualsRoot != null)
            {
                visualsRoot.SetActive(available);
                return;
            }

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                spriteRenderer.enabled = available;
            }
        }

        /// <summary>
        /// 비활성화 시 남아 있는 리스폰 코루틴을 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
        }
    }
}
