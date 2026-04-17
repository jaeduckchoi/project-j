using System;
using System.Collections.Generic;
namespace Restaurant.Kitchen
{
    /// <summary>
    /// FrontCounter의 5칸 작업대 상태와 조합 판정을 관리합니다.
    /// 슬롯 순서가 아니라 현재 들어 있는 항목들의 시그니처가 조리 계약의 기준입니다.
    /// </summary>
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

        /// <summary>
        /// 지정한 슬롯이 비어 있을 때 단일 항목을 배치합니다.
        /// </summary>
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

        /// <summary>
        /// 가장 앞의 빈 슬롯에 단일 항목을 배치합니다.
        /// </summary>
        public bool TryPlaceFirst(KitchenCarryItem item)
        {
            return TryPlaceFirst(item, true);
        }

        /// <summary>
        /// 지정한 슬롯의 항목을 꺼내고 해당 슬롯을 비웁니다.
        /// </summary>
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

        /// <summary>
        /// 현재 채워진 슬롯들을 하나의 BackCounter 운반 묶음으로 변환합니다.
        /// </summary>
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

        /// <summary>
        /// BackCounter에서 돌아온 묶음을 빈 슬롯에 다시 펼칩니다.
        /// </summary>
        public bool TryUnpackBundle(KitchenBundle bundle)
        {
            if (bundle == null || bundle.IsEmpty || EmptySlotCount < bundle.Items.Count)
            {
                return false;
            }

            // 빈 슬롯 수를 먼저 검증했기 때문에 중간 실패 없이 한 번의 상태 변경으로 펼칩니다.
            foreach (KitchenCarryItem item in bundle.Items)
            {
                TryPlaceFirst(item, false);
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 현재 작업대 시그니처가 완성 요리 조건과 일치하면 슬롯을 비우고 완성 항목을 만듭니다.
        /// </summary>
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

        /// <summary>
        /// 작업대에 남은 원재료만 냉장고로 되돌릴 수 있도록 꺼냅니다.
        /// </summary>
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

        /// <summary>
        /// 조합 시그니처 계산에 사용할 채워진 슬롯만 열거합니다.
        /// </summary>
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

        /// <summary>
        /// 내부 배치 흐름에서 변경 이벤트 발생 여부를 선택해 중복 갱신을 막습니다.
        /// </summary>
        private bool TryPlaceFirst(KitchenCarryItem item, bool notifyChanged)
        {
            if (item == null || item.IsBundle)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    continue;
                }

                slots[i] = item.Clone();
                if (notifyChanged)
                {
                    Changed?.Invoke();
                }

                return true;
            }

            return false;
        }
    }
}
