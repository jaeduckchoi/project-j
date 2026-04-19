using System;
using System.Collections.Generic;

namespace Code.Scripts.Restaurant
{
    /// <summary>
    /// 오늘의 메뉴 3칸과 현재 활성 슬롯을 관리하는 런타임 상태다.
    /// ScriptableObject에 쓰지 않고 안정적인 RecipeId만 유지한다.
    /// </summary>
    [Serializable]
    public sealed class TodayMenuState
    {
        /// <summary>
        /// 오늘의 메뉴 슬롯 수 정본이다.
        /// </summary>
        public const int SlotCount = 3;

        private readonly string[] recipeIds = new string[SlotCount];
        private int selectedSlotIndex;

        /// <summary>
        /// 저장된 RecipeId 목록과 선택 슬롯으로 상태를 복원한다.
        /// </summary>
        public TodayMenuState(IEnumerable<string> initialRecipeIds = null, int initialSelectedSlotIndex = 0)
        {
            for (int index = 0; index < SlotCount; index++)
            {
                recipeIds[index] = string.Empty;
            }

            LoadInitialRecipeIds(initialRecipeIds);
            selectedSlotIndex = ClampSlotIndex(initialSelectedSlotIndex);
        }

        /// <summary>
        /// 현재 슬롯별 RecipeId를 읽기 전용으로 반환한다.
        /// </summary>
        public IReadOnlyList<string> RecipeIds => recipeIds;

        /// <summary>
        /// 현재 활성 슬롯 인덱스다.
        /// </summary>
        public int SelectedSlotIndex => selectedSlotIndex;

        /// <summary>
        /// 3칸이 모두 채워졌는지 반환한다.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                for (int index = 0; index < SlotCount; index++)
                {
                    if (string.IsNullOrWhiteSpace(recipeIds[index]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 활성 슬롯을 바꾼다.
        /// </summary>
        public bool SelectSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex) || selectedSlotIndex == slotIndex)
            {
                return false;
            }

            selectedSlotIndex = slotIndex;
            return true;
        }

        /// <summary>
        /// 활성 슬롯에 RecipeId를 배치한다.
        /// 이미 다른 슬롯에 있으면 기존 슬롯에서 제거하고 현재 슬롯으로 이동한다.
        /// </summary>
        public bool AssignRecipeToSelectedSlot(string recipeId)
        {
            string normalizedRecipeId = NormalizeRecipeId(recipeId);
            if (string.IsNullOrWhiteSpace(normalizedRecipeId))
            {
                return false;
            }

            int existingSlotIndex = FindSlotIndex(normalizedRecipeId);
            if (existingSlotIndex == selectedSlotIndex
                && string.Equals(recipeIds[selectedSlotIndex], normalizedRecipeId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (existingSlotIndex >= 0)
            {
                recipeIds[existingSlotIndex] = string.Empty;
            }

            bool changed = !string.Equals(recipeIds[selectedSlotIndex], normalizedRecipeId, StringComparison.OrdinalIgnoreCase)
                || existingSlotIndex >= 0;
            recipeIds[selectedSlotIndex] = normalizedRecipeId;
            return changed;
        }

        /// <summary>
        /// 지정한 슬롯의 RecipeId를 반환한다.
        /// </summary>
        public string GetRecipeId(int slotIndex)
        {
            return IsValidSlotIndex(slotIndex) ? recipeIds[slotIndex] : string.Empty;
        }

        /// <summary>
        /// 주어진 RecipeId가 들어 있는 슬롯 인덱스를 반환한다.
        /// 없으면 -1이다.
        /// </summary>
        public int FindSlotIndex(string recipeId)
        {
            string normalizedRecipeId = NormalizeRecipeId(recipeId);
            if (string.IsNullOrWhiteSpace(normalizedRecipeId))
            {
                return -1;
            }

            for (int index = 0; index < SlotCount; index++)
            {
                if (string.Equals(recipeIds[index], normalizedRecipeId, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private void LoadInitialRecipeIds(IEnumerable<string> initialRecipeIds)
        {
            if (initialRecipeIds == null)
            {
                return;
            }

            int slotIndex = 0;
            foreach (string recipeId in initialRecipeIds)
            {
                if (slotIndex >= SlotCount)
                {
                    break;
                }

                string normalizedRecipeId = NormalizeRecipeId(recipeId);
                if (!string.IsNullOrWhiteSpace(normalizedRecipeId) && FindSlotIndex(normalizedRecipeId) < 0)
                {
                    recipeIds[slotIndex] = normalizedRecipeId;
                }

                slotIndex++;
            }
        }

        private static bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SlotCount;
        }

        private static int ClampSlotIndex(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return 0;
            }

            if (slotIndex >= SlotCount)
            {
                return SlotCount - 1;
            }

            return slotIndex;
        }

        private static string NormalizeRecipeId(string recipeId)
        {
            return string.IsNullOrWhiteSpace(recipeId) ? string.Empty : recipeId.Trim();
        }
    }
}
