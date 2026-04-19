using System;
using System.Collections.Generic;
using Code.Scripts.Management.Inventory;
using Code.Scripts.Shared.Data;
using UnityEngine;

namespace Code.Scripts.Restaurant.Kitchen
{
    /// <summary>
    /// 냉장고에서 꺼낸 인벤토리 재료를 BackCounter 조리 시작 전까지 예약 상태로 관리합니다.
    /// 예약 수량은 실제 인벤토리 차감과 분리해 잘못된 조합 시 재료를 되돌릴 수 있게 합니다.
    /// </summary>
    public sealed class InventoryReservationService
    {
        private readonly Dictionary<ResourceData, int> reservedAmounts = new();

        public event Action Changed;

        /// <summary>
        /// 지정한 자원에 대해 현재 조리 흐름에서 예약 중인 수량을 반환합니다.
        /// </summary>
        public int GetReservedAmount(ResourceData resource)
        {
            return resource != null && reservedAmounts.TryGetValue(resource, out int amount) ? amount : 0;
        }

        /// <summary>
        /// 인벤토리 보유량에서 예약량을 제외한 실제 선택 가능 수량을 계산합니다.
        /// </summary>
        public int GetAvailableAmount(InventoryManager inventory, ResourceData resource)
        {
            if (inventory == null || resource == null)
            {
                return 0;
            }

            return Mathf.Max(0, inventory.GetAmount(resource) - GetReservedAmount(resource));
        }

        /// <summary>
        /// 인벤토리 재료를 지정 수량만큼 예약하고 손에 들 수 있는 항목을 생성합니다.
        /// </summary>
        public bool TryReserve(InventoryManager inventory, ResourceData resource, int amount, out KitchenCarryItem item)
        {
            item = null;
            if (inventory == null || resource == null || amount <= 0)
            {
                return false;
            }

            inventory.InitializeIfNeeded();
            if (GetAvailableAmount(inventory, resource) < amount)
            {
                return false;
            }

            reservedAmounts.TryGetValue(resource, out int current);
            reservedAmounts[resource] = current + amount;
            item = KitchenCarryItem.FromReservedResource(resource, amount);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 예약된 항목을 소비하지 않고 되돌릴 때 예약 수량을 해제합니다.
        /// </summary>
        public void Release(KitchenCarryItem item)
        {
            if (item == null)
            {
                return;
            }

            // 묶음은 여러 예약 재료를 품을 수 있으므로 항목 단위로 재귀 해제합니다.
            if (item.IsBundle && item.Bundle != null)
            {
                foreach (KitchenCarryItem bundleItem in item.Bundle.Items)
                {
                    Release(bundleItem);
                }

                return;
            }

            if (!item.IsInventoryReservation || item.ResourceData == null)
            {
                return;
            }

            reservedAmounts.TryGetValue(item.ResourceData, out int current);
            int remaining = current - item.Quantity;
            if (remaining > 0)
            {
                reservedAmounts[item.ResourceData] = remaining;
            }
            else
            {
                reservedAmounts.Remove(item.ResourceData);
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// 조리 시작 시 묶음 안의 예약 재료를 실제 인벤토리에서 차감합니다.
        /// </summary>
        public bool ConsumeReservedInputs(InventoryManager inventory, KitchenBundle bundle)
        {
            if (bundle == null)
            {
                return false;
            }

            Dictionary<ResourceData, int> required = BuildReservedResourceAmounts(bundle);
            if (required.Count == 0)
            {
                return true;
            }

            // 실제 차감 전에 예약량과 인벤토리 수량을 모두 확인해 부분 차감을 피합니다.
            foreach (KeyValuePair<ResourceData, int> pair in required)
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                if (inventory == null || GetReservedAmount(pair.Key) < pair.Value || inventory.GetAmount(pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<ResourceData, int> pair in required)
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                if (!inventory.TryRemove(pair.Key, pair.Value))
                {
                    return false;
                }

                int remaining = GetReservedAmount(pair.Key) - pair.Value;
                if (remaining > 0)
                {
                    reservedAmounts[pair.Key] = remaining;
                }
                else
                {
                    reservedAmounts.Remove(pair.Key);
                }
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 묶음 안의 인벤토리 예약 항목만 자원별 필요 수량으로 합산합니다.
        /// </summary>
        private static Dictionary<ResourceData, int> BuildReservedResourceAmounts(KitchenBundle bundle)
        {
            Dictionary<ResourceData, int> required = new();
            foreach (KitchenCarryItem item in bundle.Items)
            {
                if (item == null || !item.IsInventoryReservation || item.ResourceData == null)
                {
                    continue;
                }

                required.TryGetValue(item.ResourceData, out int current);
                required[item.ResourceData] = current + item.Quantity;
            }

            return required;
        }
    }
}
