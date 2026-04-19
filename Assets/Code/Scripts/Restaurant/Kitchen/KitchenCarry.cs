using System;
using System.Collections.Generic;
using Code.Scripts.Shared.Data;
using UnityEngine;

namespace Code.Scripts.Restaurant.Kitchen
{
    /// <summary>
    /// 플레이어가 손에 들고 있거나 PassCounter 슬롯에 올려 둔 단일 주방 항목을 나타냅니다.
    /// 원재료, 조리 결과물, 완성 요리, 묶음 항목을 같은 흐름에서 다루기 위한 런타임 값 객체입니다.
    /// </summary>
    [Serializable]
    public sealed class KitchenCarryItem
    {
        /// <summary>
        /// 주방 항목 런타임 값을 생성합니다.
        /// </summary>
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

        /// <summary>
        /// 슬롯과 손 상태 사이에서 참조 공유가 생기지 않도록 현재 항목을 복제합니다.
        /// </summary>
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

        /// <summary>
        /// 레시피 조합 판정에 사용할 정규화된 시그니처 토큰을 만듭니다.
        /// </summary>
        public string BuildSignatureToken()
        {
            return KitchenSignatureUtility.BuildToken(ItemId, State, Quantity);
        }

        /// <summary>
        /// 인벤토리에서 예약한 원재료를 손에 들 수 있는 주방 항목으로 변환합니다.
        /// </summary>
        public static KitchenCarryItem FromReservedResource(ResourceData resource, int amount)
        {
            return new KitchenCarryItem(null, resource, KitchenItemState.Raw, amount, true, false, string.Empty, null);
        }

        /// <summary>
        /// 냉장고 기본 재료처럼 수량 제한 없이 제공되는 원재료 항목을 만듭니다.
        /// </summary>
        public static KitchenCarryItem FromUnlimitedBasic(ResourceData resource, int amount)
        {
            return new KitchenCarryItem(null, resource, KitchenItemState.Raw, amount, false, true, string.Empty, null);
        }

        /// <summary>
        /// 조리 단계 데이터에서 나온 주방 아이템을 손에 들 수 있는 항목으로 변환합니다.
        /// </summary>
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

        /// <summary>
        /// 레거시 호환 경로에서 여러 항목을 조리용으로 묶은 운반 항목을 만듭니다.
        /// </summary>
        public static KitchenCarryItem FromBundle(KitchenBundle bundle)
        {
            return new KitchenCarryItem(null, null, KitchenItemState.Raw, 1, false, false, string.Empty, bundle);
        }
    }

    /// <summary>
    /// PassCounter의 여러 슬롯 항목을 순서와 무관한 조합 단위로 묶은 값입니다.
    /// 레거시 조리 호환 경로는 이 묶음의 시그니처를 기준으로 동작합니다.
    /// </summary>
    [Serializable]
    public sealed class KitchenBundle
    {
        private readonly List<KitchenCarryItem> items = new();

        /// <summary>
        /// 전달된 항목들 중 다른 묶음이 아닌 항목만 복제해 새 묶음을 만듭니다.
        /// </summary>
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
                    return "빈 묶음";
                }

                List<string> names = new();
                foreach (KitchenCarryItem item in items)
                {
                    names.Add(item.DisplayName);
                }

                return $"묶음: {string.Join(", ", names)}";
            }
        }
    }

    /// <summary>
    /// 플레이어가 현재 손에 들고 있는 주방 항목 상태를 관리합니다.
    /// </summary>
    public sealed class KitchenCarryController
    {
        public event Action Changed;

        public KitchenCarryItem HeldItem { get; private set; }
        public bool IsEmpty => HeldItem == null;

        /// <summary>
        /// 손이 비어 있을 때만 새 항목을 보관합니다.
        /// </summary>
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

        /// <summary>
        /// 들고 있던 항목을 꺼내고 손 상태를 비웁니다.
        /// </summary>
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

        /// <summary>
        /// 들고 있는 항목이 있으면 손 상태를 비우고 변경을 알립니다.
        /// </summary>
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
