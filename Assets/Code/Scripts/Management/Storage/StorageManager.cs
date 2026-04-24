using System;
using System.Collections.Generic;
using System.Text;
using Code.Scripts.CoreLoop.Core;
using Code.Scripts.Shared.Data;
using Code.Scripts.Management.Inventory;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// Storage 네임스페이스
namespace Code.Scripts.Management.Storage
{
    /// <summary>
    /// 허브 창고의 보관 목록과 선택형 맡기기, 꺼내기 상태를 관리한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "Storage", sourceAssembly: "Assembly-CSharp", sourceClassName: "StorageManager")]
    public class StorageManager : MonoBehaviour
    {
        [SerializeField] private List<InventoryEntry> runtimeItems = new();
        [SerializeField] private int selectedInventoryIndex;
        [SerializeField] private int selectedStorageIndex;

        private readonly Dictionary<ResourceData, int> itemAmounts = new();
        private bool initialized;

        public event Action StorageChanged;

        public IReadOnlyList<InventoryEntry> RuntimeItems => runtimeItems;
        public int UsedSlotCount => itemAmounts.Count;
        public string LastOperationMessage { get; private set; } = "창고가 비어 있습니다.";

        /// <summary>
        /// 창고 런타임 목록을 한 번만 초기화합니다.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            RefreshRuntimeItems();
            RaiseChanged();
        }

        /// <summary>
        /// 창고에 꺼낼 수 있는 자원이 하나라도 있는지 확인합니다.
        /// </summary>
        public bool HasAnyStoredItems()
        {
            InitializeIfNeeded();
            return itemAmounts.Count > 0;
        }

        /// <summary>
        /// 현재 맡기기 대상으로 선택된 인벤토리 항목을 반환합니다.
        /// </summary>
        public InventoryEntry GetSelectedInventoryEntry(InventoryManager inventory)
        {
            List<InventoryEntry> snapshot = GetInventorySnapshot(inventory);
            int index = GetNormalizedIndex(snapshot.Count, selectedInventoryIndex);
            return index >= 0 ? snapshot[index] : null;
        }

        /// <summary>
        /// 현재 꺼내기 대상으로 선택된 창고 항목을 반환합니다.
        /// </summary>
        public InventoryEntry GetSelectedStoredEntry()
        {
            InitializeIfNeeded();

            List<InventoryEntry> snapshot = GetStorageSnapshot();
            int index = GetNormalizedIndex(snapshot.Count, selectedStorageIndex);
            return index >= 0 ? snapshot[index] : null;
        }

        /// <summary>
        /// 맡길 항목 선택 커서를 다음 인벤토리 항목으로 이동합니다.
        /// </summary>
        public bool CycleInventorySelection(InventoryManager inventory)
        {
            List<InventoryEntry> snapshot = GetInventorySnapshot(inventory);
            if (snapshot.Count == 0)
            {
                SetMessage("맡길 재료가 없습니다.");
                return false;
            }

            selectedInventoryIndex = GetNormalizedIndex(snapshot.Count, selectedInventoryIndex + 1);
            InventoryEntry selectedEntry = snapshot[selectedInventoryIndex];
            SetMessage($"맡길 품목을 {selectedEntry.resource.DisplayName}(으)로 바꿨습니다.");
            return true;
        }

        /// <summary>
        /// 꺼낼 항목 선택 커서를 다음 창고 항목으로 이동합니다.
        /// </summary>
        public bool CycleStoredSelection()
        {
            InitializeIfNeeded();

            List<InventoryEntry> snapshot = GetStorageSnapshot();
            if (snapshot.Count == 0)
            {
                SetMessage("꺼낼 재료가 없습니다.");
                return false;
            }

            selectedStorageIndex = GetNormalizedIndex(snapshot.Count, selectedStorageIndex + 1);
            InventoryEntry selectedEntry = snapshot[selectedStorageIndex];
            SetMessage($"꺼낼 품목을 {selectedEntry.resource.DisplayName}(으)로 바꿨습니다.");
            return true;
        }

        /// <summary>
        /// 선택된 인벤토리 항목 전체를 창고로 옮깁니다.
        /// </summary>
        public int StoreSelectedFromInventory(InventoryManager inventory)
        {
            if (inventory == null)
            {
                SetMessage("인벤토리를 찾지 못했습니다.");
                return 0;
            }

            InventoryEntry selectedEntry = GetSelectedInventoryEntry(inventory);
            if (selectedEntry == null)
            {
                SetMessage("맡길 재료가 없습니다.");
                return 0;
            }

            if (!inventory.TryRemove(selectedEntry.resource, selectedEntry.amount))
            {
                SetMessage($"{selectedEntry.resource.DisplayName}을(를) 맡기지 못했습니다.");
                return 0;
            }

            AddAmountInternal(selectedEntry.resource, selectedEntry.amount);
            RefreshRuntimeItems();
            SetMessage($"{selectedEntry.resource.DisplayName} x{selectedEntry.amount}을(를) 창고에 맡겼습니다.");
            return selectedEntry.amount;
        }

        /// <summary>
        /// 선택된 창고 항목 전체를 인벤토리로 옮깁니다.
        /// </summary>
        public int WithdrawSelectedToInventory(InventoryManager inventory)
        {
            if (inventory == null)
            {
                SetMessage("인벤토리를 찾지 못했습니다.");
                return 0;
            }

            InventoryEntry selectedEntry = GetSelectedStoredEntry();
            if (selectedEntry == null)
            {
                SetMessage("꺼낼 재료가 없습니다.");
                return 0;
            }

            if (!inventory.CanAdd(selectedEntry.resource))
            {
                SetMessage("인벤토리 칸이 부족합니다.");
                return 0;
            }

            if (!inventory.TryAdd(selectedEntry.resource, selectedEntry.amount, out int addedAmount) || addedAmount <= 0)
            {
                SetMessage($"{selectedEntry.resource.DisplayName}을(를) 꺼내지 못했습니다.");
                return 0;
            }

            RemoveAmountInternal(selectedEntry.resource, addedAmount);
            RefreshRuntimeItems();
            SetMessage($"{selectedEntry.resource.DisplayName} x{addedAmount}을(를) 창고에서 꺼냈습니다.");
            return addedAmount;
        }

        /// <summary>
        /// 인벤토리의 모든 자원을 창고로 옮깁니다.
        /// </summary>
        public int StoreAllFromInventory(InventoryManager inventory)
        {
            if (inventory == null)
            {
                SetMessage("인벤토리를 찾지 못했습니다.");
                return 0;
            }

            List<InventoryEntry> snapshot = GetInventorySnapshot(inventory);
            if (snapshot.Count == 0)
            {
                SetMessage("맡길 재료가 없습니다.");
                return 0;
            }

            int movedAmount = 0;

            foreach (InventoryEntry entry in snapshot)
            {
                if (!inventory.TryRemove(entry.resource, entry.amount))
                {
                    continue;
                }

                AddAmountInternal(entry.resource, entry.amount);
                movedAmount += entry.amount;
            }

            RefreshRuntimeItems();
            SetMessage(movedAmount > 0
                ? $"재료 {movedAmount}개를 창고에 맡겼습니다."
                : "맡길 재료가 없습니다.");
            return movedAmount;
        }

        /// <summary>
        /// 창고의 모든 자원을 인벤토리로 옮길 수 있는 만큼 옮깁니다.
        /// </summary>
        public int WithdrawAllToInventory(InventoryManager inventory)
        {
            if (inventory == null)
            {
                SetMessage("인벤토리를 찾지 못했습니다.");
                return 0;
            }

            InitializeIfNeeded();

            List<InventoryEntry> snapshot = GetStorageSnapshot();
            if (snapshot.Count == 0)
            {
                SetMessage("꺼낼 재료가 없습니다.");
                return 0;
            }

            int movedAmount = 0;

            foreach (InventoryEntry entry in snapshot)
            {
                if (!inventory.CanAdd(entry.resource))
                {
                    continue;
                }

                if (!inventory.TryAdd(entry.resource, entry.amount, out int addedAmount) || addedAmount <= 0)
                {
                    continue;
                }

                RemoveAmountInternal(entry.resource, addedAmount);
                movedAmount += addedAmount;
            }

            RefreshRuntimeItems();
            SetMessage(movedAmount > 0
                ? $"재료 {movedAmount}개를 창고에서 꺼냈습니다."
                : "인벤토리 칸이 부족해 꺼낼 수 없습니다.");
            return movedAmount;
        }

        /// <summary>
        /// UI 에 표시할 창고 목록과 현재 선택 항목 요약을 생성합니다.
        /// </summary>
        public string BuildSummaryText()
        {
            InitializeIfNeeded();

            StringBuilder builder = new();
            if (runtimeItems.Count == 0)
            {
                builder.AppendLine("- 보관 중인 재료 없음");
            }
            else
            {
                foreach (InventoryEntry entry in runtimeItems)
                {
                    if (entry == null || entry.resource == null)
                    {
                        continue;
                    }

                    builder.AppendLine($"- {entry.resource.DisplayName} x{entry.amount}");
                }
            }

            builder.AppendLine();

            InventoryEntry depositEntry = GetSelectedInventoryEntry(GameRuntimeAccess.Inventory);
            builder.AppendLine(depositEntry != null
                ? $"맡길 품목: {depositEntry.resource.DisplayName} x{depositEntry.amount}"
                : "맡길 품목: 없음");

            InventoryEntry withdrawEntry = GetSelectedStoredEntry();
            builder.AppendLine(withdrawEntry != null
                ? $"꺼낼 품목: {withdrawEntry.resource.DisplayName} x{withdrawEntry.amount}"
                : "꺼낼 품목: 없음");

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 현재 인벤토리 상태를 선택용 스냅샷 목록으로 만듭니다.
        /// </summary>
        private List<InventoryEntry> GetInventorySnapshot(InventoryManager inventory)
        {
            if (inventory == null)
            {
                return new List<InventoryEntry>();
            }

            inventory.InitializeIfNeeded();
            return InventoryEntrySnapshotUtility.BuildPositiveSnapshot(inventory.RuntimeItems);
        }

        /// <summary>
        /// 현재 창고 상태를 선택용 스냅샷 목록으로 만듭니다.
        /// </summary>
        private List<InventoryEntry> GetStorageSnapshot()
        {
            return InventoryEntrySnapshotUtility.BuildPositiveSnapshot(runtimeItems);
        }

        /// <summary>
        /// 목록 길이에 맞게 선택 인덱스를 순환 가능한 값으로 정규화합니다.
        /// </summary>
        private static int GetNormalizedIndex(int count, int index)
        {
            return InventoryEntrySnapshotUtility.NormalizeCyclicIndex(count, index);
        }

        /// <summary>
        /// 내부 딕셔너리에 자원 수량을 누적합니다.
        /// </summary>
        private void AddAmountInternal(ResourceData resource, int amount)
        {
            if (resource == null || amount <= 0)
            {
                return;
            }

            if (itemAmounts.ContainsKey(resource))
            {
                itemAmounts[resource] += amount;
                return;
            }

            itemAmounts.Add(resource, amount);
        }

        /// <summary>
        /// 내부 딕셔너리에서 자원 수량을 차감합니다.
        /// </summary>
        private void RemoveAmountInternal(ResourceData resource, int amount)
        {
            if (resource == null || amount <= 0)
            {
                return;
            }

            if (!itemAmounts.TryGetValue(resource, out int currentAmount))
            {
                return;
            }

            int remainingAmount = currentAmount - amount;
            if (remainingAmount <= 0)
            {
                itemAmounts.Remove(resource);
                return;
            }

            itemAmounts[resource] = remainingAmount;
        }

        /// <summary>
        /// 딕셔너리 상태를 Inspector 와 UI 에서 읽기 쉬운 리스트로 변환합니다.
        /// </summary>
        private void RefreshRuntimeItems()
        {
            InventoryEntrySnapshotUtility.RefreshRuntimeEntries(runtimeItems, itemAmounts);
        }

        /// <summary>
        /// 마지막 창고 작업 메시지를 저장하고 변경 이벤트를 발생시킵니다.
        /// </summary>
        private void SetMessage(string message)
        {
            LastOperationMessage = message;
            RaiseChanged();
        }

        /// <summary>
        /// 창고 상태 변경 이벤트를 전달합니다.
        /// </summary>
        private void RaiseChanged()
        {
            StorageChanged?.Invoke();
        }
    }
}
