using CoreLoop.Core;
using Exploration.Interaction;
using Management.Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Upgrade 네임스페이스
namespace Management.Upgrade
{
    /// <summary>
    /// 허브 작업대에서 현재 우선순위 업그레이드를 실행하는 상호작용 지점입니다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "Upgrade", sourceAssembly: "Assembly-CSharp", sourceClassName: "UpgradeStation")]
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
                    return $"{promptLabel} 완료";
                }

                string actionLabel = currentUpgradeManager.GetPreferredActionLabel();
                if (currentUpgradeManager.CanAffordPreferredAction())
                {
                    return $"[E] {promptLabel}: {actionLabel}";
                }

                return $"{promptLabel}: {actionLabel} 비용 확인";
            }
        }

        public Transform InteractionTransform => transform;

        /// <summary>
        /// 씬에 직접 연결되지 않았을 때도 업그레이드 매니저를 자동으로 찾습니다.
        /// </summary>
        private void Awake()
        {
            if (upgradeManager == null)
            {
                upgradeManager = FindFirstObjectByType<UpgradeManager>();
            }
        }

        /// <summary>
        /// 업그레이드 매니저가 있으면 작업대 상호작용을 허용합니다.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            UpgradeManager currentUpgradeManager = ResolveUpgradeManager();
            return currentUpgradeManager != null;
        }

        /// <summary>
        /// 현재 우선순위 업그레이드를 실행하고, 첫 해금 보상은 가이드 문구로 안내합니다.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            UpgradeManager currentUpgradeManager = ResolveUpgradeManager();
            if (currentUpgradeManager == null)
            {
                return;
            }

            if (GameManager.Instance != null
                && GameManager.Instance.RemoteSession != null
                && GameManager.Instance.RemoteSession.TryPerformPreferredUpgrade(currentUpgradeManager))
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
                        "인벤토리가 넓어지면 한 번 탐험에서 더 많은 재료를 챙겨 돌아올 수 있습니다.");
                    break;

                case UpgradeWorkbenchAction.UnlockTool when unlockedToolType == ToolType.Lantern:
                    GameManager.Instance?.DayCycle?.ShowHintOnce(
                        "first_unlock_lantern",
                        "랜턴을 준비했습니다. 이제 폐광산처럼 어두운 지역에도 들어갈 수 있습니다.");
                    break;
            }
        }

        /// <summary>
        /// 우선 GameManager에서 찾고, 없으면 씬 검색으로 보강합니다.
        /// </summary>
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
}
