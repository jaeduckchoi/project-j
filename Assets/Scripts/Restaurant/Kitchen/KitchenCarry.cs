using System;
using System.Collections.Generic;
using Shared.Data;
using UnityEngine;

namespace Restaurant.Kitchen
{
    [Serializable]
    public sealed class KitchenCarryItem
    {
        public KitchenCarryItem(
            KitchenItemData itemData,
            ResourceData resourceData,
            KitchenItemState state,
            int quantity,
            bool isInventoryReservation,
            bool isUnlimitedBasic,
            string recipeId,
            KitchenBundle bundle)
        {
            ItemData = itemData;
            ResourceData = resourceData;
            State = state;
            Quantity = Mathf.Max(1, quantity);
            IsInventoryReservation = isInventoryReservation;
            IsUnlimitedBasic = isUnlimitedBasic;
            RecipeId = string.IsNullOrWhiteSpace(recipeId) ? string.Empty : recipeId;
            Bundle = bundle;
        }

        public KitchenItemData ItemData { get; }
        public ResourceData ResourceData { get; }
        public KitchenItemState State { get; }
        public int Quantity { get; }
        public bool IsInventoryReservation { get; }
        public bool IsUnlimitedBasic { get; }
        public string RecipeId { get; }
        public KitchenBundle Bundle { get; }
        public bool IsBundle => Bundle != null;

        public string ItemId
        {
            get
            {
                if (ItemData != null && !string.IsNullOrWhiteSpace(ItemData.ItemId))
                {
                    return ItemData.ItemId;
                }

                if (ResourceData != null && !string.IsNullOrWhiteSpace(ResourceData.ResourceId))
                {
                    return ResourceData.ResourceId;
                }

                return IsBundle ? "bundle" : string.Empty;
            }
        }

        public string DisplayName
        {
            get
            {
                if (IsBundle)
                {
                    return Bundle.DisplayName;
                }

                if (ItemData != null && !string.IsNullOrWhiteSpace(ItemData.DisplayName))
                {
                    return ItemData.DisplayName;
                }

                if (ResourceData != null && !string.IsNullOrWhiteSpace(ResourceData.DisplayName))
                {
                    return ResourceData.DisplayName;
                }

                return string.IsNullOrWhiteSpace(ItemId) ? "Kitchen Item" : ItemId;
            }
        }

        public Sprite Icon
        {
            get
            {
                if (ItemData != null && ItemData.Icon != null)
                {
                    return ItemData.Icon;
                }

                return ResourceData != null ? ResourceData.Icon : null;
            }
        }

        public KitchenCarryItem Clone()
        {
            return new KitchenCarryItem(
                ItemData,
                ResourceData,
                State,
                Quantity,
                IsInventoryReservation,
                IsUnlimitedBasic,
                RecipeId,
                Bundle != null ? new KitchenBundle(Bundle.Items) : null);
        }

        public string BuildSignatureToken()
        {
            return KitchenSignatureUtility.BuildToken(ItemId, State, Quantity);
        }

        public static KitchenCarryItem FromReservedResource(ResourceData resource, int amount)
        {
            return new KitchenCarryItem(null, resource, KitchenItemState.Raw, amount, true, false, string.Empty, null);
        }

        public static KitchenCarryItem FromUnlimitedBasic(ResourceData resource, int amount)
        {
            return new KitchenCarryItem(null, resource, KitchenItemState.Raw, amount, false, true, string.Empty, null);
        }

        public static KitchenCarryItem FromKitchenItem(KitchenItemData itemData, int amount = 1)
        {
            if (itemData == null)
            {
                return null;
            }

            return new KitchenCarryItem(
                itemData,
                itemData.Resource,
                itemData.State,
                amount,
                false,
                false,
                itemData.RecipeId,
                null);
        }

        public static KitchenCarryItem FromBundle(KitchenBundle bundle)
        {
            return new KitchenCarryItem(null, null, KitchenItemState.Raw, 1, false, false, string.Empty, bundle);
        }
    }

    [Serializable]
    public sealed class KitchenBundle
    {
        private readonly List<KitchenCarryItem> items = new();

        public KitchenBundle(IEnumerable<KitchenCarryItem> sourceItems)
        {
            if (sourceItems == null)
            {
                return;
            }

            foreach (KitchenCarryItem item in sourceItems)
            {
                if (item != null && !item.IsBundle)
                {
                    items.Add(item.Clone());
                }
            }
        }

        public IReadOnlyList<KitchenCarryItem> Items => items;
        public bool IsEmpty => items.Count == 0;
        public string Signature => KitchenSignatureUtility.BuildSignature(items);

        public string DisplayName
        {
            get
            {
                if (items.Count == 0)
                {
                    return "Empty Bundle";
                }

                List<string> names = new();
                foreach (KitchenCarryItem item in items)
                {
                    names.Add(item.DisplayName);
                }

                return $"Bundle: {string.Join(", ", names)}";
            }
        }
    }

    public sealed class KitchenCarryController
    {
        public event Action Changed;

        public KitchenCarryItem HeldItem { get; private set; }
        public bool IsEmpty => HeldItem == null;

        public bool TryHold(KitchenCarryItem item)
        {
            if (item == null || HeldItem != null)
            {
                return false;
            }

            HeldItem = item.Clone();
            Changed?.Invoke();
            return true;
        }

        public bool TryTake(out KitchenCarryItem item)
        {
            item = HeldItem;
            if (item == null)
            {
                return false;
            }

            HeldItem = null;
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (HeldItem == null)
            {
                return;
            }

            HeldItem = null;
            Changed?.Invoke();
        }
    }
}
