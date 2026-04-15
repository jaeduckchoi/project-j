using System;
using System.Collections.Generic;
using Management.Inventory;
using Shared.Data;
using UnityEngine;

namespace Restaurant.Kitchen
{
    public sealed class InventoryReservationService
    {
        private readonly Dictionary<ResourceData, int> reservedAmounts = new();

        public event Action Changed;

        public int GetReservedAmount(ResourceData resource)
        {
            return resource != null && reservedAmounts.TryGetValue(resource, out int amount) ? amount : 0;
        }

        public int GetAvailableAmount(InventoryManager inventory, ResourceData resource)
        {
            if (inventory == null || resource == null)
            {
                return 0;
            }

            return Mathf.Max(0, inventory.GetAmount(resource) - GetReservedAmount(resource));
        }

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

        public void Release(KitchenCarryItem item)
        {
            if (item == null)
            {
                return;
            }

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
