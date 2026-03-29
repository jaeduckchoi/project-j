using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Generated 게임 데이터 에셋을 이름으로 다시 찾는 폴백 로더다.
// 씬 참조가 비어 있어도 에디터 플레이와 빌드에서 기본 레시피, 자원 데이터를 복구한다.
namespace Data
{
public static class GeneratedGameDataLocator
{
    private const string GeneratedDataRoot = "Assets/Generated/GameData";
    private const string GeneratedDataManifestPath = "Generated/GeneratedGameDataManifest";

    private static readonly Dictionary<string, ResourceData> resourceCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, RecipeData> recipeCache = new(StringComparer.OrdinalIgnoreCase);
    private static bool manifestLoadAttempted;

    /*
     * generated 자원 에셋을 이름, id, 표시 이름 기준으로 찾아 반환합니다.
     */
    public static ResourceData FindGeneratedResource(string assetName, params string[] alternateKeys)
    {
        RefreshLoadedAssetCaches();

        ResourceData resource = FindInCache(resourceCache, assetName, alternateKeys);
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

    /*
     * generated 레시피 에셋을 이름, id, 표시 이름 기준으로 찾아 반환합니다.
     */
    public static RecipeData FindGeneratedRecipe(string assetName, params string[] alternateKeys)
    {
        RefreshLoadedAssetCaches();

        RecipeData recipe = FindInCache(recipeCache, assetName, alternateKeys);
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

    /*
     * 현재 로드된 에셋과 manifest 에서 찾은 에셋을 캐시에 다시 반영합니다.
     */
    private static void RefreshLoadedAssetCaches()
    {
        // 빌드에서는 manifest 가 기본 진입점 역할을 합니다.
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

    /*
     * Resources 에 둔 generated 데이터 manifest 를 한 번만 읽어 캐시에 반영합니다.
     */
    private static void CacheManifestAssets()
    {
        if (manifestLoadAttempted)
        {
            return;
        }

        manifestLoadAttempted = true;

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

    /*
     * 주 키와 대체 키를 순서대로 조회해 캐시에서 매칭 에셋을 찾습니다.
     */
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

    /*
     * 공백과 대소문자를 정규화한 뒤 실제 캐시 조회를 수행합니다.
     */
    private static bool TryGetFromCache<T>(Dictionary<string, T> cache, string key, out T value)
        where T : UnityEngine.Object
    {
        value = null;
        string normalizedKey = NormalizeKey(key);
        return !string.IsNullOrWhiteSpace(normalizedKey) && cache.TryGetValue(normalizedKey, out value) && value != null;
    }

    /*
     * 자원 에셋을 여러 키 형태로 캐시에 등록합니다.
     */
    private static void CacheResource(ResourceData resource)
    {
        if (resource == null)
        {
            return;
        }

        CacheValue(resourceCache, resource.name, resource);
        CacheValue(resourceCache, resource.ResourceId, resource);
        CacheValue(resourceCache, resource.DisplayName, resource);
    }

    /*
     * 레시피 에셋을 여러 키 형태로 캐시에 등록합니다.
     */
    private static void CacheRecipe(RecipeData recipe)
    {
        if (recipe == null)
        {
            return;
        }

        CacheValue(recipeCache, recipe.name, recipe);
        CacheValue(recipeCache, recipe.RecipeId, recipe);
        CacheValue(recipeCache, recipe.DisplayName, recipe);
    }

    /*
     * 단일 키 값을 정규화해 캐시에 넣습니다.
     */
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

    /*
     * 공백과 대소문자 차이를 제거해 비교용 키를 만듭니다.
     */
    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Trim().ToLowerInvariant();
    }

#if UNITY_EDITOR
    /*
     * 에디터 환경에서 generated 폴더의 에셋을 직접 읽어옵니다.
     */
    private static T LoadGeneratedAsset<T>(string assetName) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        string exactPath = $"{GeneratedDataRoot}/{assetName}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(exactPath);
        if (asset != null)
        {
            return asset;
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
#endif
}
}
