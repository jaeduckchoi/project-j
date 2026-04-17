using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Shared.Data
{
    /// <summary>
    /// generated 게임 데이터 에셋을 이름, id, 표시 이름 기준으로 다시 찾는 로더다.
    /// 씬 참조가 비어 있어도 manifest 또는 런타임 fallback 데이터로 기본 상태를 복구한다.
    /// </summary>
    public static class GeneratedGameDataLocator
    {
        private const string GeneratedDataManifestPath = "Generated/generated-game-data-manifest";
        private static PrototypeGeneratedAssetSettings AssetSettings => PrototypeGeneratedAssetSettings.GetCurrent();
        private static string GeneratedDataRoot => AssetSettings.GameDataRoot;
        private static string GeneratedResourceDataRoot => AssetSettings.ResourceDataRoot;
        private static string GeneratedRecipeDataRoot => AssetSettings.RecipeDataRoot;
        private static string GeneratedInputDataRoot => AssetSettings.InputDataRoot;

        private static readonly Dictionary<string, ResourceData> ResourceCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, RecipeData> RecipeCache = new(StringComparer.OrdinalIgnoreCase);
        private static bool _manifestLoadAttempted;
        private static bool _fallbackAssetsCreated;

        /// <summary>
        /// generated 자원 에셋을 이름, id, 표시 이름 기준으로 찾아 반환한다.
        /// </summary>
        public static ResourceData FindGeneratedResource(string assetName, params string[] alternateKeys)
        {
            RefreshLoadedAssetCaches();

            ResourceData resource = FindInCache(ResourceCache, assetName, alternateKeys);
            if (resource != null)
            {
                return resource;
            }

#if UNITY_EDITOR
            resource = LoadGeneratedAsset<ResourceData>(assetName);
            if (resource != null)
            {
                CacheResource(resource);
            }

            return resource;
#else
        return null;
#endif
        }

        /// <summary>
        /// generated 레시피 에셋을 이름, id, 표시 이름 기준으로 찾아 반환한다.
        /// </summary>
        public static RecipeData FindGeneratedRecipe(string assetName, params string[] alternateKeys)
        {
            RefreshLoadedAssetCaches();

            RecipeData recipe = FindInCache(RecipeCache, assetName, alternateKeys);
            if (recipe != null)
            {
                return recipe;
            }

#if UNITY_EDITOR
            recipe = LoadGeneratedAsset<RecipeData>(assetName);
            if (recipe != null)
            {
                CacheRecipe(recipe);
            }

            return recipe;
#else
        return null;
#endif
        }

        /// <summary>
        /// 현재 로드된 에셋과 manifest 에셋을 캐시에 반영한다.
        /// </summary>
        private static void RefreshLoadedAssetCaches()
        {
            // 빌드에서 기록한 manifest를 먼저 반영한 뒤 현재 메모리에 올라온 에셋도 함께 캐시에 모읍니다.
            CacheManifestAssets();

            foreach (ResourceData resource in Resources.FindObjectsOfTypeAll<ResourceData>())
            {
                CacheResource(resource);
            }

            foreach (RecipeData recipe in Resources.FindObjectsOfTypeAll<RecipeData>())
            {
                CacheRecipe(recipe);
            }
        }

        /// <summary>
        /// Resources 아래 generated 데이터 manifest를 한 번만 읽어 캐시에 반영한다.
        /// </summary>
        private static void CacheManifestAssets()
        {
            if (_manifestLoadAttempted)
            {
                return;
            }

            _manifestLoadAttempted = true;

            GeneratedGameDataManifest manifest = Resources.Load<GeneratedGameDataManifest>(GeneratedDataManifestPath);
            if (manifest == null)
            {
                BootstrapFallbackAssets();
                return;
            }

            if (manifest.Resources != null)
            {
                foreach (ResourceData resource in manifest.Resources)
                {
                    CacheResource(resource);
                }
            }

            if (manifest.Recipes != null)
            {
                foreach (RecipeData recipe in manifest.Recipes)
                {
                    CacheRecipe(recipe);
                }
            }
        }

        /// <summary>
        /// 기본 키와 대체 키를 순서대로 조회해 캐시에서 일치하는 에셋을 찾는다.
        /// </summary>
        private static T FindInCache<T>(Dictionary<string, T> cache, string primaryKey, params string[] alternateKeys)
            where T : UnityEngine.Object
        {
            if (TryGetFromCache(cache, primaryKey, out T match))
            {
                return match;
            }

            if (alternateKeys == null)
            {
                return null;
            }

            foreach (string key in alternateKeys)
            {
                if (TryGetFromCache(cache, key, out match))
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// 공백과 대소문자를 정규화한 키로 실제 캐시 조회를 수행한다.
        /// </summary>
        private static bool TryGetFromCache<T>(Dictionary<string, T> cache, string key, out T value)
            where T : UnityEngine.Object
        {
            value = null;
            string normalizedKey = NormalizeKey(key);
            return !string.IsNullOrWhiteSpace(normalizedKey) && cache.TryGetValue(normalizedKey, out value) && value != null;
        }

        /// <summary>
        /// 자원 에셋을 여러 키 형태로 캐시에 등록한다.
        /// </summary>
        private static void CacheResource(ResourceData resource)
        {
            if (resource == null)
            {
                return;
            }

            CacheValue(ResourceCache, resource.name, resource);
            CacheValue(ResourceCache, resource.ResourceId, resource);
            CacheValue(ResourceCache, resource.DisplayName, resource);
        }

        /// <summary>
        /// 레시피 에셋을 여러 키 형태로 캐시에 등록한다.
        /// </summary>
        private static void CacheRecipe(RecipeData recipe)
        {
            if (recipe == null)
            {
                return;
            }

            CacheValue(RecipeCache, recipe.name, recipe);
            CacheValue(RecipeCache, recipe.RecipeId, recipe);
            CacheValue(RecipeCache, recipe.DisplayName, recipe);
        }

        /// <summary>
        /// 단일 키 값을 정규화해 캐시에 넣는다.
        /// </summary>
        private static void CacheValue<T>(Dictionary<string, T> cache, string key, T value)
            where T : UnityEngine.Object
        {
            string normalizedKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey) || value == null)
            {
                return;
            }

            cache[normalizedKey] = value;
        }

        /// <summary>
        /// 공백, 구분자, 접두어 차이를 제거해 비교 가능한 키를 만든다.
        /// </summary>
        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string normalized = key.Trim().ToLowerInvariant();

            if (normalized.StartsWith("resource-", StringComparison.Ordinal)
                || normalized.StartsWith("resource_", StringComparison.Ordinal)
                || normalized.StartsWith("resource ", StringComparison.Ordinal))
            {
                normalized = normalized["resource".Length..].TrimStart('-', '_', ' ');
            }
            else if (normalized.StartsWith("recipe-", StringComparison.Ordinal)
                     || normalized.StartsWith("recipe_", StringComparison.Ordinal)
                     || normalized.StartsWith("recipe ", StringComparison.Ordinal))
            {
                normalized = normalized["recipe".Length..].TrimStart('-', '_', ' ');
            }

            return normalized
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal);
        }

        private static void BootstrapFallbackAssets()
        {
            if (_fallbackAssetsCreated)
            {
                return;
            }

            _fallbackAssetsCreated = true;

            ResourceData fish = CreateRuntimeResource("Fish", "생선", "바닷가에서 쉽게 얻을 수 있는 기본 재료입니다.", "바닷가", 10, ResourceRarity.Common);
            ResourceData shell = CreateRuntimeResource("Shell", "조개", "국물 요리에 쓰기 좋은 바닷가 재료입니다.", "바닷가", 12, ResourceRarity.Common);
            ResourceData seaweed = CreateRuntimeResource("Seaweed", "해초", "향이 좋은 해산 재료입니다.", "바닷가", 8, ResourceRarity.Common);
            ResourceData herb = CreateRuntimeResource("Herb", "약초", "깊은 숲에서 얻는 향이 짙은 약초입니다.", "깊은 숲", 14, ResourceRarity.Uncommon);
            ResourceData mushroom = CreateRuntimeResource("Mushroom", "버섯", "숲의 그늘 아래에서 자라는 식재료입니다.", "깊은 숲", 16, ResourceRarity.Uncommon);
            ResourceData glowMoss = CreateRuntimeResource("GlowMoss", "발광 이끼", "폐광산 안쪽의 습한 벽면에서 자라는 희귀 식재료입니다.", "폐광산", 22, ResourceRarity.Rare);
            ResourceData windHerb = CreateRuntimeResource("WindHerb", "향초", "바람이 센 언덕에서만 자라는 고급 허브입니다.", "바람 언덕", 18, ResourceRarity.Rare);

            CreateRuntimeRecipe(
                "sushi_set",
                "생선 초밥 세트",
                "생선을 빠르게 준비할 수 있는 기본 메뉴입니다.",
                30,
                1,
                new[]
                {
                    RecipeIngredient.CreateRuntime("Fish", "생선", 1, fish)
                });

            CreateRuntimeRecipe(
                "seafood_soup",
                "해물탕",
                "생선, 조개, 해초를 모두 넣은 고급 메뉴입니다.",
                55,
                2,
                new[]
                {
                    RecipeIngredient.CreateRuntime("Fish", "생선", 1, fish),
                    RecipeIngredient.CreateRuntime("Shell", "조개", 1, shell),
                    RecipeIngredient.CreateRuntime("Seaweed", "해초", 1, seaweed)
                });

            CreateRuntimeRecipe(
                "herb_fish_soup",
                "약초 생선탕",
                "바닷가 생선과 숲의 약초를 넣어 끓인 메뉴입니다.",
                42,
                2,
                new[]
                {
                    RecipeIngredient.CreateRuntime("Fish", "생선", 1, fish),
                    RecipeIngredient.CreateRuntime("Herb", "약초", 1, herb)
                });

            CreateRuntimeRecipe(
                "forest_basket",
                "숲 버섯 모둠",
                "약초와 버섯을 넣어 만든 가벼운 숲 메뉴입니다.",
                38,
                1,
                new[]
                {
                    RecipeIngredient.CreateRuntime("Herb", "약초", 1, herb),
                    RecipeIngredient.CreateRuntime("Mushroom", "버섯", 1, mushroom)
                });

            CreateRuntimeRecipe(
                "glow_moss_stew",
                "광채 해물탕",
                "발광 이끼와 해초를 함께 넣어 진하게 끓인 메뉴입니다.",
                68,
                3,
                new[]
                {
                    RecipeIngredient.CreateRuntime("Fish", "생선", 1, fish),
                    RecipeIngredient.CreateRuntime("Seaweed", "해초", 1, seaweed),
                    RecipeIngredient.CreateRuntime("GlowMoss", "발광 이끼", 1, glowMoss)
                });

            CreateRuntimeRecipe(
                "wind_herb_salad",
                "향초 해초 무침",
                "바람 언덕 향초와 해초를 함께 버무린 메뉴입니다.",
                46,
                2,
                new[]
                {
                    RecipeIngredient.CreateRuntime("Seaweed", "해초", 1, seaweed),
                    RecipeIngredient.CreateRuntime("WindHerb", "향초", 1, windHerb)
                });
        }

        private static ResourceData CreateRuntimeResource(
            string id,
            string displayName,
            string description,
            string regionTag,
            int sellPrice,
            ResourceRarity rarity)
        {
            ResourceData resource = ScriptableObject.CreateInstance<ResourceData>();
            resource.name = $"resource-{id}";
            resource.hideFlags = HideFlags.HideAndDontSave;
            resource.ConfigureRuntime(id, displayName, description, regionTag, sellPrice, rarity);
            CacheResource(resource);
            return resource;
        }

        private static void CreateRuntimeRecipe(
            string id,
            string displayName,
            string description,
            int sellPrice,
            int reputationDelta,
            IEnumerable<RecipeIngredient> ingredients)
        {
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.name = $"recipe-{id}";
            recipe.hideFlags = HideFlags.HideAndDontSave;
            recipe.ConfigureRuntime(id, displayName, description, sellPrice, reputationDelta, string.Empty, 0, string.Empty, string.Empty, ingredients);
            CacheRecipe(recipe);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 환경에서 generated 폴더의 에셋을 직접 읽는다.
        /// </summary>
        private static T LoadGeneratedAsset<T>(string assetName) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return null;
            }

            T asset;
            foreach (string candidatePath in GetCandidateAssetPaths<T>(assetName))
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(candidatePath);
                if (asset != null)
                {
                    return asset;
                }
            }

            string[] guids = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}", new[] { GeneratedDataRoot });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetCandidateAssetPaths<T>(string assetName) where T : UnityEngine.Object
        {
            string kebabAssetName = ToKebabCase(assetName);

            if (typeof(T) == typeof(ResourceData))
            {
                string resourceFileName = kebabAssetName.StartsWith("resource-", StringComparison.Ordinal)
                    ? kebabAssetName
                    : $"resource-{kebabAssetName}";
                yield return $"{GeneratedResourceDataRoot}/{resourceFileName}.asset";
                yield return $"{GeneratedDataRoot}/{resourceFileName}.asset";
            }
            else if (typeof(T) == typeof(RecipeData))
            {
                string recipeFileName = kebabAssetName.StartsWith("recipe-", StringComparison.Ordinal)
                    ? kebabAssetName
                    : $"recipe-{kebabAssetName}";
                yield return $"{GeneratedRecipeDataRoot}/{recipeFileName}.asset";
                yield return $"{GeneratedDataRoot}/{recipeFileName}.asset";
            }

            yield return $"{GeneratedInputDataRoot}/{kebabAssetName}.asset";
            yield return $"{GeneratedDataRoot}/{kebabAssetName}.asset";
        }

        private static string ToKebabCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new(value.Length + 8);
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];

                if (current is '_' or ' ')
                {
                    if (builder.Length > 0 && builder[^1] != '-')
                    {
                        builder.Append('-');
                    }

                    continue;
                }

                bool shouldInsertDash =
                    index > 0
                    && char.IsUpper(current)
                    && (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1]));

                if (shouldInsertDash && builder.Length > 0 && builder[^1] != '-')
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(current));
            }

            return builder.ToString();
        }
#endif
    }
}
