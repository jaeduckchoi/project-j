using System;
using System.Collections.Generic;
using System.Text;
using Core;
using Data;
using Economy;
using Inventory;
using Tools;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Upgrade 네임스페이스
namespace Upgrade
{
    /// <summary>
    /// 작업대 업그레이드 비용과 적용 결과를 관리한다.
    /// 인벤토리 확장과 도구 해금 모두 골드와 재료 조합으로 처리한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "UpgradeManager")]
    public class UpgradeManager : MonoBehaviour
    {
        [SerializeField] private List<InventoryUpgradeCost> inventoryUpgradeCosts = new();
        [SerializeField] private List<ToolUnlockCost> toolUnlockCosts = new();

        private bool _initialized;

        public event Action UpgradeStateChanged;

        public string LastUpgradeMessage { get; private set; } = "작업대에서 다음 준비를 확인하세요.";
        public IReadOnlyList<InventoryUpgradeCost> InventoryUpgradeCosts => inventoryUpgradeCosts;
        public IReadOnlyList<ToolUnlockCost> ToolUnlockCosts => toolUnlockCosts;

        /// <summary>
        /// 업그레이드 비용 데이터와 초기 상태를 한 번만 준비합니다.
        /// </summary>
        public void InitializeIfNeeded()
        {
            bool changed = EnsureUpgradeCostsConfigured();

            if (_initialized)
            {
                if (changed)
                {
                    RaiseChanged();
                }

                return;
            }

            _initialized = true;
            RaiseChanged();
        }

        /// <summary>
        /// 다음 인벤토리 확장 단계가 남아 있는지 확인합니다.
        /// </summary>
        public bool HasNextInventoryUpgrade()
        {
            return GetNextInventoryUpgradeCost() != null;
        }

        /// <summary>
        /// 아직 해금하지 않은 도구가 남아 있는지 확인합니다.
        /// </summary>
        public bool HasPendingToolUnlocks()
        {
            return GetNextToolUnlockCost() != null;
        }

        /// <summary>
        /// 인벤토리 확장이나 도구 해금 중 하나라도 남아 있는지 확인합니다.
        /// </summary>
        public bool HasAnyPendingUpgrade()
        {
            return HasPendingToolUnlocks() || HasNextInventoryUpgrade();
        }

        /// <summary>
        /// 다음 인벤토리 확장 비용을 현재 감당할 수 있는지 확인합니다.
        /// </summary>
        public bool CanUpgradeInventory()
        {
            return CanPayCost(GetNextInventoryUpgradeCost());
        }

        /// <summary>
        /// 다음 인벤토리 확장을 실제로 적용합니다.
        /// </summary>
        public bool TryUpgradeInventory()
        {
            InitializeIfNeeded();

            InventoryUpgradeCost nextCost = GetNextInventoryUpgradeCost();
            InventoryManager inventory = GetInventory();
            if (nextCost == null || inventory == null)
            {
                SetMessage("인벤토리 확장을 더 이상 진행할 수 없습니다.");
                return false;
            }

            if (!TrySpendCost(nextCost.goldCost, nextCost.requiredResource, nextCost.requiredAmount, "인벤토리 확장"))
            {
                return false;
            }

            if (!inventory.TryUpgradeCapacity())
            {
                RefundCost(nextCost.goldCost, nextCost.requiredResource, nextCost.requiredAmount);
                SetMessage("인벤토리 확장을 적용하지 못했습니다.");
                return false;
            }

            SetMessage($"인벤토리가 확장되었습니다. 현재 {inventory.MaxSlotCount}칸을 사용할 수 있습니다.");
            return true;
        }

        /// <summary>
        /// 지정한 도구에 대한 해금 비용 데이터가 있는지 확인합니다.
        /// </summary>
        public bool HasToolUnlockCost(ToolType toolType)
        {
            return GetToolUnlockCost(toolType) != null;
        }

        /// <summary>
        /// 지정한 도구가 이미 해금됐는지 확인합니다.
        /// </summary>
        public bool IsToolUnlocked(ToolType toolType)
        {
            ToolManager tools = GetTools();
            return tools != null && tools.HasTool(toolType);
        }

        /// <summary>
        /// 지정한 도구를 현재 비용으로 해금할 수 있는지 확인합니다.
        /// </summary>
        public bool CanUnlockTool(ToolType toolType)
        {
            ToolUnlockCost cost = GetToolUnlockCost(toolType);
            return cost != null && !IsToolUnlocked(toolType) && CanPayCost(cost);
        }

        /// <summary>
        /// 지정한 도구를 실제로 해금합니다.
        /// </summary>
        public bool TryUnlockTool(ToolType toolType)
        {
            InitializeIfNeeded();

            ToolUnlockCost cost = GetToolUnlockCost(toolType);
            ToolManager tools = GetTools();
            if (cost == null || tools == null || toolType == ToolType.None)
            {
                SetMessage("해금할 도구를 찾지 못했습니다.");
                return false;
            }

            if (tools.HasTool(toolType))
            {
                SetMessage($"{toolType.GetDisplayName()}은 이미 해금되어 있습니다.");
                return false;
            }

            if (!TrySpendCost(cost.goldCost, cost.requiredResource, cost.requiredAmount, $"{toolType.GetDisplayName()} 해금"))
            {
                return false;
            }

            if (!tools.UnlockTool(toolType))
            {
                RefundCost(cost.goldCost, cost.requiredResource, cost.requiredAmount);
                SetMessage($"{toolType.GetDisplayName()} 해금에 실패했습니다.");
                return false;
            }

            SetMessage($"{toolType.GetDisplayName()}을 준비했습니다. 이제 새 지역 탐험에 활용할 수 있습니다.");
            return true;
        }

        /// <summary>
        /// 작업대에서 우선으로 보여줄 액션을 계산합니다.
        /// </summary>
        public UpgradeWorkbenchAction GetPreferredAction()
        {
            InitializeIfNeeded();

            UpgradeWorkbenchAction affordableAction = GetFirstAffordableAction();
            if (affordableAction != UpgradeWorkbenchAction.None)
            {
                return affordableAction;
            }

            if (GetNextInventoryUpgradeCost() != null)
            {
                return UpgradeWorkbenchAction.UpgradeInventory;
            }

            if (GetNextToolUnlockCost() != null)
            {
                return UpgradeWorkbenchAction.UnlockTool;
            }

            return UpgradeWorkbenchAction.None;
        }

        /// <summary>
        /// 우선순위가 도구 해금일 때 대상 도구를 반환합니다.
        /// </summary>
        public ToolType GetPreferredToolType()
        {
            ToolUnlockCost nextToolUnlock = GetNextToolUnlockCost();
            return nextToolUnlock != null ? nextToolUnlock.toolType : ToolType.None;
        }

        /// <summary>
        /// 현재 우선 액션을 UI 에 쓸 문자열로 변환합니다.
        /// </summary>
        public string GetPreferredActionLabel()
        {
            return GetPreferredAction() switch
            {
                UpgradeWorkbenchAction.UnlockTool when GetPreferredToolType() != ToolType.None => $"{GetPreferredToolType().GetDisplayName()} 해금",
                UpgradeWorkbenchAction.UpgradeInventory => "인벤토리 확장",
                _ => "업그레이드 완료"
            };
        }

        /// <summary>
        /// 우선 액션의 비용을 감당할 수 있는지 확인합니다.
        /// </summary>
        public bool CanAffordPreferredAction()
        {
            return GetPreferredAction() switch
            {
                UpgradeWorkbenchAction.UnlockTool => CanUnlockTool(GetPreferredToolType()),
                UpgradeWorkbenchAction.UpgradeInventory => CanUpgradeInventory(),
                _ => false
            };
        }

        /// <summary>
        /// 현재 우선 액션을 실제로 수행하고 결과를 반환합니다.
        /// </summary>
        public bool TryPerformPreferredUpgrade(out UpgradeWorkbenchAction performedAction, out ToolType unlockedToolType)
        {
            InitializeIfNeeded();

            performedAction = GetPreferredAction();
            unlockedToolType = ToolType.None;

            switch (performedAction)
            {
                case UpgradeWorkbenchAction.UnlockTool:
                    unlockedToolType = GetPreferredToolType();
                    return TryUnlockTool(unlockedToolType);

                case UpgradeWorkbenchAction.UpgradeInventory:
                    return TryUpgradeInventory();

                default:
                    SetMessage("남아 있는 업그레이드가 없습니다.");
                    return false;
            }
        }

        /// <summary>
        /// UI 에 표시할 업그레이드 현황과 다음 비용 요약을 생성합니다.
        /// </summary>
        public string BuildUpgradeSummary()
        {
            InitializeIfNeeded();

            StringBuilder builder = new();
            builder.AppendLine($"- 보유 도구: {BuildUnlockedToolSummary()}");

            ToolUnlockCost nextToolUnlock = GetNextToolUnlockCost();
            if (nextToolUnlock == null)
            {
                builder.AppendLine("- 도구 해금: 완료");
            }
            else
            {
                string availability = CanUnlockTool(nextToolUnlock.toolType) ? "가능" : "준비 필요";
                builder.AppendLine($"- 다음 도구: {nextToolUnlock.toolType.GetDisplayName()} ({availability})");

                if (!string.IsNullOrWhiteSpace(nextToolUnlock.description))
                {
                    builder.AppendLine($"  {nextToolUnlock.description}");
                }

                builder.AppendLine($"  비용: {BuildCostText(nextToolUnlock.goldCost, nextToolUnlock.requiredResource, nextToolUnlock.requiredAmount)}");
            }

            InventoryManager inventory = GetInventory();
            InventoryUpgradeCost nextInventoryUpgrade = GetNextInventoryUpgradeCost();
            if (nextInventoryUpgrade == null || inventory == null)
            {
                builder.AppendLine("- 인벤토리 확장: 완료");
            }
            else
            {
                int currentSlots = inventory.MaxSlotCount;
                int nextSlots = inventory.GetSlotCapacityForLevel(inventory.CapacityLevel + 1);
                string availability = CanUpgradeInventory() ? "가능" : "준비 필요";

                builder.AppendLine($"- 인벤토리: {currentSlots}칸 -> {nextSlots}칸 ({availability})");
                if (!string.IsNullOrWhiteSpace(nextInventoryUpgrade.description))
                {
                    builder.AppendLine($"  {nextInventoryUpgrade.description}");
                }

                builder.AppendLine($"  비용: {BuildCostText(nextInventoryUpgrade.goldCost, nextInventoryUpgrade.requiredResource, nextInventoryUpgrade.requiredAmount)}");
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 기존 UI 호환용으로 같은 업그레이드 요약 문자열을 반환합니다.
        /// </summary>
        public string BuildInventoryUpgradeSummary()
        {
            return BuildUpgradeSummary();
        }

        /// <summary>
        /// 누락된 generated 자원 참조를 기준으로 기본 업그레이드 비용을 채웁니다.
        /// </summary>
        private bool EnsureUpgradeCostsConfigured()
        {
            bool changed = false;

            ResourceData shell = GeneratedGameDataLocator.FindGeneratedResource("Shell", "조개");
            changed |= EnsureInventoryUpgradeCost(
                0,
                30,
                shell,
                3,
                "조개 상자를 묶어 12칸까지 넓힙니다.");

            ResourceData herb = GeneratedGameDataLocator.FindGeneratedResource("Herb", "약초");
            changed |= EnsureInventoryUpgradeCost(
                1,
                65,
                herb,
                4,
                "정리 상자를 더 달아 16칸까지 확장합니다.");

            ResourceData mushroom = GeneratedGameDataLocator.FindGeneratedResource("Mushroom", "버섯");
            changed |= EnsureToolUnlockCost(
                ToolType.Lantern,
                45,
                mushroom,
                2,
                "폐광산처럼 어두운 지역 진입에 필요합니다.");

            return changed;
        }

        /// <summary>
        /// 지정한 인벤토리 확장 단계의 기본 비용 데이터를 보정합니다.
        /// </summary>
        private bool EnsureInventoryUpgradeCost(int index, int goldCost, ResourceData requiredResource, int requiredAmount, string description)
        {
            inventoryUpgradeCosts ??= new List<InventoryUpgradeCost>();

            bool changed = false;
            while (inventoryUpgradeCosts.Count <= index)
            {
                inventoryUpgradeCosts.Add(new InventoryUpgradeCost());
                changed = true;
            }

            inventoryUpgradeCosts[index] ??= new InventoryUpgradeCost();
            InventoryUpgradeCost cost = inventoryUpgradeCosts[index];

            if (cost.goldCost <= 0)
            {
                cost.goldCost = goldCost;
                changed = true;
            }

            if (cost.requiredResource == null && requiredResource != null)
            {
                cost.requiredResource = requiredResource;
                changed = true;
            }

            if (cost.requiredAmount <= 0)
            {
                cost.requiredAmount = requiredAmount;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(cost.description))
            {
                cost.description = description;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// 지정한 도구 해금 비용 데이터를 보정합니다.
        /// </summary>
        private bool EnsureToolUnlockCost(ToolType toolType, int goldCost, ResourceData requiredResource, int requiredAmount, string description)
        {
            toolUnlockCosts ??= new List<ToolUnlockCost>();

            ToolUnlockCost cost = GetToolUnlockCost(toolType);
            bool changed = false;

            if (cost == null)
            {
                cost = new ToolUnlockCost { toolType = toolType };
                toolUnlockCosts.Add(cost);
                changed = true;
            }

            if (cost.goldCost <= 0)
            {
                cost.goldCost = goldCost;
                changed = true;
            }

            if (cost.requiredResource == null && requiredResource != null)
            {
                cost.requiredResource = requiredResource;
                changed = true;
            }

            if (cost.requiredAmount <= 0)
            {
                cost.requiredAmount = requiredAmount;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(cost.description))
            {
                cost.description = description;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// 현재 자원으로 실제 수행 가능한 첫 액션을 계산합니다.
        /// </summary>
        private UpgradeWorkbenchAction GetFirstAffordableAction()
        {
            if (CanUpgradeInventory())
            {
                return UpgradeWorkbenchAction.UpgradeInventory;
            }

            ToolUnlockCost nextToolUnlock = GetNextToolUnlockCost();
            if (nextToolUnlock != null && CanUnlockTool(nextToolUnlock.toolType))
            {
                return UpgradeWorkbenchAction.UnlockTool;
            }

            return UpgradeWorkbenchAction.None;
        }

        /// <summary>
        /// 현재 인벤토리 단계 기준으로 다음 확장 비용을 반환합니다.
        /// </summary>
        private InventoryUpgradeCost GetNextInventoryUpgradeCost()
        {
            InventoryManager inventory = GetInventory();
            if (inventory == null || inventoryUpgradeCosts == null)
            {
                return null;
            }

            int nextIndex = inventory.CapacityLevel;
            return nextIndex >= 0 && nextIndex < inventoryUpgradeCosts.Count
                ? inventoryUpgradeCosts[nextIndex]
                : null;
        }

        /// <summary>
        /// 아직 해금하지 않은 첫 도구의 비용을 반환합니다.
        /// </summary>
        private ToolUnlockCost GetNextToolUnlockCost()
        {
            if (toolUnlockCosts == null)
            {
                return null;
            }

            foreach (ToolUnlockCost cost in toolUnlockCosts)
            {
                if (cost == null || cost.toolType == ToolType.None || IsToolUnlocked(cost.toolType))
                {
                    continue;
                }

                return cost;
            }

            return null;
        }

        /// <summary>
        /// 지정한 도구 타입의 비용 데이터를 조회합니다.
        /// </summary>
        private ToolUnlockCost GetToolUnlockCost(ToolType toolType)
        {
            if (toolType == ToolType.None || toolUnlockCosts == null)
            {
                return null;
            }

            foreach (ToolUnlockCost cost in toolUnlockCosts)
            {
                if (cost != null && cost.toolType == toolType)
                {
                    return cost;
                }
            }

            return null;
        }

        /// <summary>
        /// 인벤토리 확장 비용을 현재 감당할 수 있는지 확인합니다.
        /// </summary>
        private bool CanPayCost(InventoryUpgradeCost cost)
        {
            return cost != null && CanPayCost(cost.goldCost, cost.requiredResource, cost.requiredAmount);
        }

        /// <summary>
        /// 도구 해금 비용을 현재 감당할 수 있는지 확인합니다.
        /// </summary>
        private bool CanPayCost(ToolUnlockCost cost)
        {
            return cost != null && CanPayCost(cost.goldCost, cost.requiredResource, cost.requiredAmount);
        }

        /// <summary>
        /// 골드와 자원 수량 기준으로 일반 비용 지불 가능 여부를 확인합니다.
        /// </summary>
        private bool CanPayCost(int goldCost, ResourceData requiredResource, int requiredAmount)
        {
            EconomyManager economy = GetEconomy();
            InventoryManager inventory = GetInventory();

            if (goldCost > 0 && (economy == null || economy.CurrentGold < goldCost))
            {
                return false;
            }

            if (requiredResource != null && requiredAmount > 0 && (inventory == null || !inventory.Has(requiredResource, requiredAmount)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 비용을 실제로 차감하고 실패 시 메시지를 남깁니다.
        /// </summary>
        private bool TrySpendCost(int goldCost, ResourceData requiredResource, int requiredAmount, string actionLabel)
        {
            if (!CanPayCost(goldCost, requiredResource, requiredAmount))
            {
                SetMessage($"{actionLabel} 비용이 부족합니다. 필요: {BuildCostText(goldCost, requiredResource, requiredAmount)}");
                return false;
            }

            InventoryManager inventory = GetInventory();
            EconomyManager economy = GetEconomy();

            if (requiredResource != null && requiredAmount > 0 && (inventory == null || !inventory.TryRemove(requiredResource, requiredAmount)))
            {
                SetMessage($"{actionLabel} 재료를 소모하지 못했습니다.");
                return false;
            }

            if (goldCost > 0 && (economy == null || !economy.TrySpendGold(goldCost)))
            {
                if (requiredResource != null && requiredAmount > 0)
                {
                    inventory?.TryAdd(requiredResource, requiredAmount, out _);
                }

                SetMessage($"{actionLabel} 골드가 부족합니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 업그레이드 적용 실패 시 차감했던 비용을 되돌립니다.
        /// </summary>
        private void RefundCost(int goldCost, ResourceData requiredResource, int requiredAmount)
        {
            if (goldCost > 0)
            {
                GetEconomy()?.AddGold(goldCost);
            }

            if (requiredResource != null && requiredAmount > 0)
            {
                GetInventory()?.TryAdd(requiredResource, requiredAmount, out _);
            }
        }

        /// <summary>
        /// 현재 해금된 도구 목록을 요약 문자열로 만듭니다.
        /// </summary>
        private string BuildUnlockedToolSummary()
        {
            ToolManager tools = GetTools();
            if (tools == null)
            {
                return "없음";
            }

            tools.InitializeIfNeeded();

            List<string> names = new();
            foreach (ToolType toolType in tools.RuntimeUnlockedTools)
            {
                if (toolType == ToolType.None)
                {
                    continue;
                }

                names.Add(toolType.GetDisplayName());
            }

            return names.Count > 0 ? string.Join(", ", names) : "없음";
        }

        /// <summary>
        /// 비용 정보를 UI 용 텍스트로 조합합니다.
        /// </summary>
        private static string BuildCostText(int goldCost, ResourceData requiredResource, int requiredAmount)
        {
            List<string> parts = new();

            if (goldCost > 0)
            {
                parts.Add($"골드 {goldCost}");
            }

            if (requiredResource != null && requiredAmount > 0)
            {
                parts.Add($"{requiredResource.DisplayName} x{requiredAmount}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "비용 없음";
        }

        /// <summary>
        /// 전역 인벤토리 매니저를 반환합니다.
        /// </summary>
        private InventoryManager GetInventory()
        {
            return GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        }

        /// <summary>
        /// 전역 경제 매니저를 반환합니다.
        /// </summary>
        private EconomyManager GetEconomy()
        {
            return GameManager.Instance != null ? GameManager.Instance.Economy : null;
        }

        /// <summary>
        /// 전역 도구 매니저를 반환합니다.
        /// </summary>
        private ToolManager GetTools()
        {
            return GameManager.Instance != null ? GameManager.Instance.Tools : null;
        }

        /// <summary>
        /// 마지막 업그레이드 메시지를 저장하고 UI 에 알립니다.
        /// </summary>
        private void SetMessage(string message)
        {
            LastUpgradeMessage = message;
            RaiseChanged();
        }

        /// <summary>
        /// 업그레이드 상태 변경 이벤트를 전달합니다.
        /// </summary>
        private void RaiseChanged()
        {
            UpgradeStateChanged?.Invoke();
        }
    }

    [Serializable]
    public class InventoryUpgradeCost
    {
        [Min(0)] public int goldCost;
        public ResourceData requiredResource;
        [Min(0)] public int requiredAmount = 1;
        [TextArea] public string description;
    }

    [Serializable]
    public class ToolUnlockCost
    {
        public ToolType toolType = ToolType.None;
        [Min(0)] public int goldCost;
        public ResourceData requiredResource;
        [Min(0)] public int requiredAmount = 1;
        [TextArea] public string description;
    }

    public enum UpgradeWorkbenchAction
    {
        None,
        UnlockTool,
        UpgradeInventory
    }
}
