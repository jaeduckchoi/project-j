using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Data 네임스페이스
namespace Data
{
    /// <summary>
    /// Generated 게임 데이터 에셋을 이름으로 다시 찾는 폴백 로더다.
    /// 씬 참조가 비어 있어도 에디터 플레이와 빌드에서 기본 레시피, 자원 데이터를 복구한다.
    /// </summary>
    public static class GeneratedGameDataLocator
    {
        private const string GeneratedDataRoot = "Assets/Generated/GameData";
        private const string GeneratedDataManifestPath = "Generated/generated-game-data-manifest";

        private static readonly Dictionary<string, ResourceData> ResourceCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, RecipeData> RecipeCache = new(StringComparer.OrdinalIgnoreCase);
        private static bool _manifestLoadAttempted;

        /// <summary>
        /// generated 자원 에셋을 이름, id, 표시 이름 기준으로 찾아 반환합니다.
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
        /// generated 레시피 에셋을 이름, id, 표시 이름 기준으로 찾아 반환합니다.
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
        /// 현재 로드된 에셋과 manifest 에서 찾은 에셋을 캐시에 다시 반영합니다.
        /// </summary>
        private static void RefreshLoadedAssetCaches()
        {
            /// <summary>
            /// 빌드에서는 manifest 가 기본 진입점 역할을 합니다.
            /// </summary>
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
        /// Resources 에 둔 generated 데이터 manifest 를 한 번만 읽어 캐시에 반영합니다.
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
        /// 주 키와 대체 키를 순서대로 조회해 캐시에서 매칭 에셋을 찾습니다.
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
        /// 공백과 대소문자를 정규화한 뒤 실제 캐시 조회를 수행합니다.
        /// </summary>
        private static bool TryGetFromCache<T>(Dictionary<string, T> cache, string key, out T value)
            where T : UnityEngine.Object
        {
            value = null;
            string normalizedKey = NormalizeKey(key);
            return !string.IsNullOrWhiteSpace(normalizedKey) && cache.TryGetValue(normalizedKey, out value) && value != null;
        }

        /// <summary>
        /// 자원 에셋을 여러 키 형태로 캐시에 등록합니다.
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
        /// 레시피 에셋을 여러 키 형태로 캐시에 등록합니다.
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
        /// 단일 키 값을 정규화해 캐시에 넣습니다.
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
        /// 공백과 대소문자 차이를 제거해 비교용 키를 만듭니다.
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

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 환경에서 generated 폴더의 에셋을 직접 읽어옵니다.
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
                yield return $"{GeneratedDataRoot}/{resourceFileName}.asset";
            }
            else if (typeof(T) == typeof(RecipeData))
            {
                string recipeFileName = kebabAssetName.StartsWith("recipe-", StringComparison.Ordinal)
                    ? kebabAssetName
                    : $"recipe-{kebabAssetName}";
                yield return $"{GeneratedDataRoot}/{recipeFileName}.asset";
            }

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
