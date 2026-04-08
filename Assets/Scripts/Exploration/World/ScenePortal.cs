using CoreLoop.Core;
using Exploration.Interaction;
using Management.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// World 네임스페이스
namespace Exploration.World
{
    /// <summary>
    /// 허브와 탐험 지역 사이의 이동을 처리하고, 잠금 조건을 검사한다.
    /// 막힌 상태에서도 상호작용을 받아 안내 문구를 띄울 수 있다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "World", sourceAssembly: "Assembly-CSharp", sourceClassName: "ScenePortal")]
    public class ScenePortal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetSpawnPointId;
        [SerializeField] private string promptLabel = "이동하기";
        [SerializeField] private ToolType requiredToolType = ToolType.None;
        [SerializeField, Min(0)] private int requiredReputation;
        [SerializeField, TextArea] private string lockedGuideText = string.Empty;

        public string InteractionPrompt
        {
            get
            {
                string blockingReason = GetBlockingReason();
                return string.IsNullOrWhiteSpace(blockingReason)
                    ? $"[E] {promptLabel}"
                    : blockingReason;
            }
        }

        public Transform InteractionTransform => transform;
        public string TargetSceneName => targetSceneName;
        public string TargetSpawnPointId => targetSpawnPointId;

        /// <summary>
        /// 런타임 또는 빌더에서 포탈 목적지와 잠금 조건을 다시 설정합니다.
        /// </summary>
        public void Configure(
            string sceneName,
            string spawnPointId,
            string label,
            ToolType toolType = ToolType.None,
            int reputation = 0,
            string guideText = "")
        {
            targetSceneName = sceneName;
            targetSpawnPointId = spawnPointId;

            if (!string.IsNullOrWhiteSpace(label))
            {
                promptLabel = label;
            }

            requiredToolType = toolType;
            requiredReputation = Mathf.Max(0, reputation);
            lockedGuideText = guideText;
        }

        /// <summary>
        /// 목적지 이름이 있는 포탈만 상호작용 대상으로 취급합니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            return !string.IsNullOrWhiteSpace(targetSceneName);
        }

        /// <summary>
        /// 조건이 맞으면 씬 이동을 수행하고, 막혀 있으면 안내 문구를 보여줍니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            string blockingReason = GetBlockingReason();
            if (!string.IsNullOrWhiteSpace(blockingReason))
            {
                string guideText = !string.IsNullOrWhiteSpace(lockedGuideText) ? lockedGuideText : blockingReason;
                GameManager.Instance?.DayCycle?.ShowTemporaryGuide(guideText);
                return;
            }

            if (GameManager.Instance != null
                && GameManager.Instance.RemoteSession != null
                && GameManager.Instance.RemoteSession.TryTravel(this))
            {
                return;
            }

            GameManager.Instance?.LoadScene(targetSceneName, targetSpawnPointId);
        }

        /// <summary>
        /// 현재 포탈이 막혀 있다면 사용자에게 보여줄 이유를 계산합니다.
        /// </summary>
        private string GetBlockingReason()
        {
            if (GameManager.Instance == null)
            {
                return "이동 준비 중";
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                return "이동 대상 없음";
            }

            if (requiredToolType != ToolType.None
                && (GameManager.Instance.Tools == null || !GameManager.Instance.Tools.HasTool(requiredToolType)))
            {
                return $"{requiredToolType.GetDisplayName()} 필요";
            }

            if (requiredReputation > 0
                && GameManager.Instance.Economy != null
                && GameManager.Instance.Economy.CurrentReputation < requiredReputation)
            {
                return $"평판 {requiredReputation} 필요";
            }

            return string.Empty;
        }
    }
}
