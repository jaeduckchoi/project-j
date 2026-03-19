using UnityEngine;

// 허브와 탐험 지역 사이의 이동을 처리하고, 잠금 조건을 검사한다.
// 막힌 상태에서도 상호작용을 받아 안내 문구를 띄울 수 있다.
public class ScenePortal : MonoBehaviour, IInteractable
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;
    [SerializeField] private string promptLabel = "이동하기";
    [SerializeField] private bool requireMorningExplore = true;
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

    /*
     * 런타임 또는 빌더에서 포탈 목적지와 잠금 조건을 다시 설정합니다.
     */
    public void Configure(
        string sceneName,
        string spawnPointId,
        string label,
        bool morningOnly = true,
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

        requireMorningExplore = morningOnly;
        requiredToolType = toolType;
        requiredReputation = Mathf.Max(0, reputation);
        lockedGuideText = guideText;
    }

    /*
     * 목적지 이름이 있는 포탈만 상호작용 대상으로 취급합니다.
     */
    public bool CanInteract(GameObject interactor)
    {
        return !string.IsNullOrWhiteSpace(targetSceneName);
    }

    /*
     * 조건이 맞으면 씬 이동을 수행하고, 막혀 있으면 안내 문구를 보여줍니다.
     */
    public void Interact(GameObject interactor)
    {
        string blockingReason = GetBlockingReason();
        if (!string.IsNullOrWhiteSpace(blockingReason))
        {
            string guideText = !string.IsNullOrWhiteSpace(lockedGuideText) ? lockedGuideText : blockingReason;
            GameManager.Instance?.DayCycle?.ShowTemporaryGuide(guideText);
            return;
        }

        GameManager.Instance?.LoadScene(targetSceneName, targetSpawnPointId);
    }

    /*
     * 현재 포탈이 막혀 있다면 사용자에게 보여줄 이유를 계산합니다.
     */
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

        // 허브 복귀는 오후 이후에도 허용해야 하루 루프가 자연스럽게 닫힙니다.
        bool isReturningToHub = targetSceneName == GameManager.Instance.HubSceneName;
        if (!isReturningToHub
            && requireMorningExplore
            && GameManager.Instance.DayCycle != null
            && GameManager.Instance.DayCycle.CurrentPhase != DayPhase.MorningExplore)
        {
            return "오늘 탐험은 이미 마감되었습니다.";
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
