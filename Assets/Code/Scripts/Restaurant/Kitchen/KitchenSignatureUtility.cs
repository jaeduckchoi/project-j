using System;
using System.Collections.Generic;
using UnityEngine;

namespace Restaurant.Kitchen
{
    /// <summary>
    /// 슬롯 순서와 입력 순서에 흔들리지 않는 주방 조합 시그니처를 생성합니다.
    /// </summary>
    public static class KitchenSignatureUtility
    {
        /// <summary>
        /// 실제 런타임 항목 목록을 정렬된 조합 시그니처로 변환합니다.
        /// </summary>
        public static string BuildSignature(IEnumerable<KitchenCarryItem> items)
        {
            List<string> tokens = new();
            if (items != null)
            {
                foreach (KitchenCarryItem item in items)
                {
                    if (item != null && !item.IsBundle)
                    {
                        tokens.Add(item.BuildSignatureToken());
                    }
                }
            }

            tokens.Sort(StringComparer.Ordinal);
            return string.Join("+", tokens);
        }

        /// <summary>
        /// 레시피 요구 조건 목록을 정렬된 조합 시그니처로 변환합니다.
        /// </summary>
        public static string BuildSignature(IEnumerable<KitchenItemRequirement> requirements)
        {
            List<string> tokens = new();
            if (requirements != null)
            {
                foreach (KitchenItemRequirement requirement in requirements)
                {
                    if (requirement != null)
                    {
                        tokens.Add(BuildToken(requirement.RequirementId, requirement.State, requirement.Quantity));
                    }
                }
            }

            tokens.Sort(StringComparer.Ordinal);
            return string.Join("+", tokens);
        }

        /// <summary>
        /// 항목 id, 조리 상태, 수량을 단일 비교 토큰으로 정규화합니다.
        /// </summary>
        public static string BuildToken(string itemId, KitchenItemState state, int quantity)
        {
            return $"{Normalize(itemId)}|{state}|{Mathf.Max(1, quantity)}";
        }

        /// <summary>
        /// id 비교에서 공백과 대소문자 차이를 제거합니다.
        /// </summary>
        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
        }
    }
}
