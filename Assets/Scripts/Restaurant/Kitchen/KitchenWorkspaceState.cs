using System;
using System.Collections.Generic;
using UnityEngine;

namespace Restaurant.Kitchen
{
    public sealed class KitchenWorkspaceState
    {
        public const int MaxSlotCount = 5;

        private readonly KitchenCarryItem[] slots = new KitchenCarryItem[MaxSlotCount];

        public event Action Changed;

        public IReadOnlyList<KitchenCarryItem> Slots => slots;

        public bool HasAnyItem
        {
            get
            {
                foreach (KitchenCarryItem slot in slots)
                {
                    if (slot != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int EmptySlotCount
        {
            get
            {
                int count = 0;
                foreach (KitchenCarryItem slot in slots)
                {
                    if (slot == null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public string CurrentSignature => KitchenSignatureUtility.BuildSignature(GetFilledSlots());

        public bool TryPlace(int slotIndex, KitchenCarryItem item)
        {
            if (item == null || item.IsBundle || slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] != null)
            {
                return false;
            }

            slots[slotIndex] = item.Clone();
            Changed?.Invoke();
            return true;
        }

        public bool TryPlaceFirst(KitchenCarryItem item)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    return TryPlace(i, item);
                }
            }

            return false;
        }

        public bool TryPick(int slotIndex, out KitchenCarryItem item)
        {
            item = null;
            if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] == null)
            {
                return false;
            }

            item = slots[slotIndex];
            slots[slotIndex] = null;
            Changed?.Invoke();
            return true;
        }

        public bool TryCreateBundleFromFilledSlots(out KitchenBundle bundle)
        {
            bundle = new KitchenBundle(GetFilledSlots());
            if (bundle.IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = null;
            }

            Changed?.Invoke();
            return true;
        }

        public bool TryUnpackBundle(KitchenBundle bundle)
        {
            if (bundle == null || bundle.IsEmpty || EmptySlotCount < bundle.Items.Count)
            {
                return false;
            }

            foreach (KitchenCarryItem item in bundle.Items)
            {
                TryPlaceFirst(item);
            }

            Changed?.Invoke();
            return true;
        }

        public bool TryFinalize(KitchenDishData dish, out KitchenCarryItem finalDish)
        {
            finalDish = null;
            if (dish == null || !string.Equals(CurrentSignature, dish.FinalSignature, StringComparison.Ordinal))
            {
                return false;
            }

            finalDish = KitchenCarryItem.FromKitchenItem(dish.FinalDishItem);
            if (finalDish == null)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = null;
            }

            Changed?.Invoke();
            return finalDish != null;
        }

        public List<KitchenCarryItem> ReturnRawInputs()
        {
            List<KitchenCarryItem> returned = new();
            for (int i = 0; i < slots.Length; i++)
            {
                KitchenCarryItem item = slots[i];
                if (item == null || item.State != KitchenItemState.Raw)
                {
                    continue;
                }

                returned.Add(item);
                slots[i] = null;
            }

            if (returned.Count > 0)
            {
                Changed?.Invoke();
            }

            return returned;
        }

        private IEnumerable<KitchenCarryItem> GetFilledSlots()
        {
            foreach (KitchenCarryItem item in slots)
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }
    }
}
