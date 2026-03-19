using UnityEngine;

// 허브 작업대에서 현재 우선 업그레이드를 실행하는 상호작용 지점이다.
public class UpgradeStation : MonoBehaviour, IInteractable
{
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private string promptLabel = "작업대 사용";

    public string InteractionPrompt
    {
        get
        {
            UpgradeManager currentUpgradeManager = ResolveUpgradeManager();
            if (currentUpgradeManager == null)
            {
                return string.Empty;
            }

            if (!currentUpgradeManager.HasAnyPendingUpgrade())
            {
                return "업그레이드 완료";
            }

            string actionLabel = currentUpgradeManager.GetPreferredActionLabel();
            if (currentUpgradeManager.CanAffordPreferredAction())
            {
                return $"[E] {actionLabel}";
            }

            return $"{actionLabel} 비용 확인";
        }
    }

    public Transform InteractionTransform => transform;

    /*
     * 허브에 배치된 UpgradeManager 참조를 자동으로 찾습니다.
     */
    private void Awake()
    {
        if (upgradeManager == null)
        {
            upgradeManager = FindFirstObjectByType<UpgradeManager>();
        }
    }

    /*
     * 업그레이드 매니저만 있으면 비용 안내까지 포함해 상호작용을 허용합니다.
     */
    public bool CanInteract(GameObject interactor)
    {
        UpgradeManager currentUpgradeManager = ResolveUpgradeManager();
        return currentUpgradeManager != null;
    }

    /*
     * 현재 우선 업그레이드를 실행하고 최초 해금 힌트를 노출합니다.
     */
    public void Interact(GameObject interactor)
    {
        UpgradeManager currentUpgradeManager = ResolveUpgradeManager();
        if (currentUpgradeManager == null)
        {
            return;
        }

        bool upgraded = currentUpgradeManager.TryPerformPreferredUpgrade(out UpgradeWorkbenchAction action, out ToolType unlockedToolType);
        if (!upgraded)
        {
            return;
        }

        switch (action)
        {
            case UpgradeWorkbenchAction.UpgradeInventory:
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_upgrade_inventory",
                    "인벤토리가 넓어지면 한 번 탐험에서 더 많은 재료를 들고 돌아올 수 있습니다.");
                break;

            case UpgradeWorkbenchAction.UnlockTool when unlockedToolType == ToolType.Lantern:
                GameManager.Instance?.DayCycle?.ShowHintOnce(
                    "first_unlock_lantern",
                    "랜턴을 준비했습니다. 이제 폐광산 같은 어두운 지역에도 들어갈 수 있습니다.");
                break;
        }
    }

    /*
     * 우선 GameManager 에서, 없으면 씬 검색으로 UpgradeManager 를 찾습니다.
     */
    private UpgradeManager ResolveUpgradeManager()
    {
        if (upgradeManager == null && GameManager.Instance != null)
        {
            upgradeManager = GameManager.Instance.Upgrades;
        }

        if (upgradeManager == null)
        {
            upgradeManager = FindFirstObjectByType<UpgradeManager>();
        }

        return upgradeManager;
    }
}
