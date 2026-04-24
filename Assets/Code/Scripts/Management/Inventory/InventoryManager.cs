using System;
using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Shared.Data;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

// Inventory 네임스페이스
namespace Code.Scripts.Management.Inventory
{
    /// <summary>
    /// 자원 전용 슬롯 기반 인벤토리다. 시작 8칸에서 업그레이드로 12칸, 16칸까지 확장한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "Inventory", sourceAssembly: "Assembly-CSharp", sourceClassName: "InventoryManager")]
    public class InventoryManager : MonoBehaviour
    {
        // 슬롯 단계와 초기 소지품을 인스펙터에서 지정한다.
        [SerializeField] private List<int> slotCapacities = new() { 8, 12, 16 };
        [SerializeField, Min(0)] private int capacityLevel;

        [SerializeField] private List<InventoryEntry> startingItems = new();

        // 디버깅과 UI 확인을 위한 현재 인벤토리 직렬화 목록이다.
        [SerializeField] private List<InventoryEntry> runtimeItems = new();

        private readonly Dictionary<ResourceData, int> itemAmounts = new();
        private bool initialized;

        public event Action InventoryChanged;

        public IReadOnlyList<InventoryEntry> RuntimeItems => runtimeItems;
        public int TotalItemCount => itemAmounts.Values.Sum();
        public int UsedSlotCount => itemAmounts.Count;
        public int MaxSlotCount => GetSlotCapacityForLevel(capacityLevel);
        public int CapacityLevel => capacityLevel;

        /// <summary>
        /// 시작 아이템을 사전에 적재하고 런타임 목록을 만든다.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            itemAmounts.Clear();

            foreach (InventoryEntry entry in startingItems)
            {
                if (entry == null || entry.resource == null || entry.amount <= 0)
                {
                    continue;
                }

                AddAmountInternal(entry.resource, entry.amount);
            }

            RefreshRuntimeItems();
            RaiseChanged();
        }

        /// <summary>
        /// 새 자원을 넣을 슬롯이 남아 있는지 또는 같은 자원이 이미 있는지 검사한다.
        /// </summary>
        public bool CanAdd(ResourceData resource)
        {
            if (resource == null)
            {
                return false;
            }

            InitializeIfNeeded();
            return itemAmounts.ContainsKey(resource) || UsedSlotCount < MaxSlotCount;
        }

        /// <summary>
        /// 자원을 인벤토리에 추가하고 실제 반영 수량을 반환한다.
        /// </summary>
        public bool TryAdd(ResourceData resource, int amount, out int addedAmount)
        {
            addedAmount = 0;

            if (resource == null || amount <= 0)
            {
                return false;
            }

            InitializeIfNeeded();

            if (!CanAdd(resource))
            {
                return false;
            }

            AddAmountInternal(resource, amount);
            addedAmount = amount;
            RefreshRuntimeItems();
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// 특정 자원을 요구 수량 이상 가지고 있는지 확인한다.
        /// </summary>
        public bool Has(ResourceData resource, int requiredAmount = 1)
        {
            if (resource == null || requiredAmount <= 0)
            {
                return false;
            }

            InitializeIfNeeded();
            return itemAmounts.TryGetValue(resource, out int amount) && amount >= requiredAmount;
        }

        /// <summary>
        /// 자원을 제거하고 수량이 0이 되면 슬롯을 정리한다.
        /// </summary>
        public bool TryRemove(ResourceData resource, int amount)
        {
            if (resource == null || amount <= 0)
            {
                return false;
            }

            InitializeIfNeeded();

            if (!itemAmounts.TryGetValue(resource, out int currentAmount) || currentAmount < amount)
            {
                return false;
            }

            int remainingAmount = currentAmount - amount;

            if (remainingAmount <= 0)
            {
                itemAmounts.Remove(resource);
            }
            else
            {
                itemAmounts[resource] = remainingAmount;
            }

            RefreshRuntimeItems();
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// 현재 보유한 자원 수량을 반환한다.
        /// </summary>
        public int GetAmount(ResourceData resource)
        {
            if (resource == null)
            {
                return 0;
            }

            InitializeIfNeeded();
            return itemAmounts.TryGetValue(resource, out int amount) ? amount : 0;
        }

        /// <summary>
        /// 다음 인벤토리 단계가 남아 있을 때 슬롯 수를 확장한다.
        /// </summary>
        public bool TryUpgradeCapacity()
        {
            InitializeIfNeeded();

            if (capacityLevel >= slotCapacities.Count - 1)
            {
                return false;
            }

            capacityLevel += 1;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// 단계별 최대 슬롯 수를 안전하게 계산한다.
        /// </summary>
        public int GetSlotCapacityForLevel(int level)
        {
            if (slotCapacities == null || slotCapacities.Count == 0)
            {
                return 8;
            }

            int clampedLevel = Mathf.Clamp(level, 0, slotCapacities.Count - 1);
            return Mathf.Max(1, slotCapacities[clampedLevel]);
        }

        /// <summary>
        /// 모든 자원을 비워 테스트 시작 상태로 되돌린다.
        /// </summary>
        public void ClearInventory()
        {
            InitializeIfNeeded();
            itemAmounts.Clear();
            RefreshRuntimeItems();
            RaiseChanged();
        }

        /// <summary>
        /// 사전 내부 수량만 갱신한다.
        /// </summary>
        private void AddAmountInternal(ResourceData resource, int amount)
        {
            if (itemAmounts.ContainsKey(resource))
            {
                itemAmounts[resource] += amount;
                return;
            }

            itemAmounts.Add(resource, amount);
        }

        /// <summary>
        /// UI와 인스펙터가 읽기 쉬운 정렬된 목록을 다시 만든다.
        /// </summary>
        private void RefreshRuntimeItems()
        {
            InventoryEntrySnapshotUtility.RefreshRuntimeEntries(runtimeItems, itemAmounts);
        }

        /// <summary>
        /// 인벤토리 변경 이벤트를 발행한다.
        /// </summary>
        private void RaiseChanged()
        {
            InventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// 자원과 수량 한 쌍을 인벤토리 직렬화용으로 담는 구조다.
    /// </summary>
    [Serializable]
    public class InventoryEntry
    {
        [FormerlySerializedAs("Resource")] public ResourceData resource;
        [FormerlySerializedAs("Amount")] public int amount;

        /// <summary>
        /// Unity 직렬화를 위한 기본 생성자다.
        /// </summary>
        public InventoryEntry()
        {
        }

        /// <summary>
        /// 코드에서 자원 엔트리를 빠르게 만들 때 사용한다.
        /// </summary>
        public InventoryEntry(ResourceData resource, int amount)
        {
            this.resource = resource;
            this.amount = amount;
        }
    }

    internal static class InventoryEntrySnapshotUtility
    {
        internal static List<InventoryEntry> BuildPositiveSnapshot(IEnumerable<InventoryEntry> entries)
        {
            return entries == null
                ? new List<InventoryEntry>()
                : entries
                    .Where(entry => entry != null && entry.resource != null && entry.amount > 0)
                    .Select(entry => new InventoryEntry(entry.resource, entry.amount))
                    .ToList();
        }

        internal static void RefreshRuntimeEntries(List<InventoryEntry> targetEntries, IReadOnlyDictionary<ResourceData, int> itemAmounts)
        {
            if (targetEntries == null)
            {
                return;
            }

            targetEntries.Clear();
            if (itemAmounts == null)
            {
                return;
            }

            foreach (KeyValuePair<ResourceData, int> pair in itemAmounts.OrderBy(entry => entry.Key.DisplayName))
            {
                targetEntries.Add(new InventoryEntry(pair.Key, pair.Value));
            }
        }

        internal static int NormalizeCyclicIndex(int count, int index)
        {
            if (count <= 0)
            {
                return -1;
            }

            int normalized = index % count;
            return normalized < 0 ? normalized + count : normalized;
        }
    }
}
