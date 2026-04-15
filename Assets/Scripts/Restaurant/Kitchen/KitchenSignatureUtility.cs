using System;
using System.Collections.Generic;
using UnityEngine;

namespace Restaurant.Kitchen
{
    public static class KitchenSignatureUtility
    {
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

        public static string BuildToken(string itemId, KitchenItemState state, int quantity)
        {
            return $"{Normalize(itemId)}|{state}|{Mathf.Max(1, quantity)}";
        }

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
