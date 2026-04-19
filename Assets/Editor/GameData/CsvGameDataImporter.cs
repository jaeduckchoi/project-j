#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Shared;
using Shared.Data;
using UnityEditor;
using UnityEngine;

namespace Editor.GameData
{
    /// <summary>
    /// 수작업 CSV 원본에서 generated ResourceData, RecipeData, manifest를 생성하는 에디터 전용 importer다.
    /// </summary>
    public static class CsvGameDataImporter
    {
        public const string SourceRoot = "Assets/DataSource/GameData";

        private const string IngredientsFileName = "ingredients.csv";
        private const string RecipesFileName = "recipes.csv";
        private const string RecipeIngredientsFileName = "recipe_ingredients.csv";
        private const string MenuItemPath = "Tools/Jonggu Restaurant/Game Data/Generate From CSV";

        private static readonly string[] IngredientColumns =
        {
            "ingredient_id",
            "ingredient_name",
            "description",
            "region_tag",
            "rarity",
            "difficulty",
            "supply_source",
            "acquisition_source",
            "acquisition_method",
            "acquisition_tool",
            "buy_price",
            "sell_price",
            "memo",
            "active"
        };

        private static readonly string[] RecipeColumns =
        {
            "recipe_id",
            "recipe_name",
            "description",
            "supply_source",
            "difficulty",
            "cooking_method",
            "sell_price",
            "reputation_delta",
            "memo",
            "active"
        };

        private static readonly string[] RecipeIngredientColumns =
        {
            "recipe_id",
            "ingredient_id",
            "quantity",
            "sort_order",
            "active"
        };

        private static readonly HashSet<string> AllowedIngredientIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "ingredient_001",
            "ingredient_002",
            "ingredient_003",
            "ingredient_004",
            "ingredient_005",
            "ingredient_021",
            "ingredient_022",
            "ingredient_023",
            "ingredient_024",
            "ingredient_025",
            "ingredient_026",
            "ingredient_027",
            "ingredient_028",
            "ingredient_041",
            "ingredient_042",
            "ingredient_043",
            "ingredient_044",
            "ingredient_045",
            "ingredient_046",
            "ingredient_047",
            "ingredient_048",
            "ingredient_049",
            "ingredient_061",
            "ingredient_062",
            "ingredient_063",
            "ingredient_064"
        };

        private static readonly HashSet<string> AllowedRecipeIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "food_001",
            "food_002",
            "food_003",
            "food_021",
            "food_022",
            "food_023",
            "food_024",
            "food_025",
            "food_026",
            "food_027",
            "food_028",
            "food_041",
            "food_042",
            "food_043",
            "food_044",
            "food_045",
            "food_046",
            "food_047",
            "food_048",
            "food_049",
            "food_061",
            "food_062",
            "food_063",
            "food_064",
            "food_065",
            "food_066",
            "food_067",
            "food_068",
            "food_069"
        };

        [MenuItem(MenuItemPath)]
        public static void GenerateFromCsvMenu()
        {
            CsvGameDataImportResult result = GenerateFromCsv(SourceRoot);
            foreach (string warning in result.Warnings)
            {
                Debug.LogWarning(warning);
            }

            if (!result.Success)
            {
                throw new UnityException(result.BuildSummary());
            }

            Debug.Log(result.BuildSummary());
        }

        /// <summary>
        /// 기본 CSV 원본 폴더를 읽고 generated 게임 데이터 에셋을 갱신한다.
        /// </summary>
        public static CsvGameDataImportResult GenerateFromCsv(string sourceRoot)
        {
            ImportContext context = LoadAndValidateFromFiles(sourceRoot);
            if (!context.Result.Success)
            {
                return context.Result;
            }

            GenerateAssets(context);
            return context.Result;
        }

        /// <summary>
        /// 파일 생성 없이 CSV 문자열만 검증한다. EditMode 테스트에서 사용한다.
        /// </summary>
        public static CsvGameDataImportResult ValidateCsvText(
            string ingredientsCsv,
            string recipesCsv,
            string recipeIngredientsCsv)
        {
            return LoadAndValidate(ingredientsCsv, recipesCsv, recipeIngredientsCsv).Result;
        }

        private static ImportContext LoadAndValidateFromFiles(string sourceRoot)
        {
            string normalizedRoot = NormalizeAssetPath(sourceRoot, SourceRoot);
            string ingredientsPath = $"{normalizedRoot}/{IngredientsFileName}";
            string recipesPath = $"{normalizedRoot}/{RecipesFileName}";
            string recipeIngredientsPath = $"{normalizedRoot}/{RecipeIngredientsFileName}";

            CsvGameDataImportResult result = new();
            string ingredientsCsv = ReadRequiredTextAsset(ingredientsPath, result);
            string recipesCsv = ReadRequiredTextAsset(recipesPath, result);
            string recipeIngredientsCsv = ReadRequiredTextAsset(recipeIngredientsPath, result);

            if (!result.Success)
            {
                return new ImportContext(result);
            }

            return LoadAndValidate(ingredientsCsv, recipesCsv, recipeIngredientsCsv, result);
        }

        private static ImportContext LoadAndValidate(
            string ingredientsCsv,
            string recipesCsv,
            string recipeIngredientsCsv,
            CsvGameDataImportResult result = null)
        {
            result ??= new CsvGameDataImportResult();
            ImportContext context = new(result);

            List<CsvRow> ingredientRows = ParseCsv(ingredientsCsv, IngredientsFileName, IngredientColumns, result);
            List<CsvRow> recipeRows = ParseCsv(recipesCsv, RecipesFileName, RecipeColumns, result);
            List<CsvRow> recipeIngredientRows = ParseCsv(recipeIngredientsCsv, RecipeIngredientsFileName, RecipeIngredientColumns, result);

            if (!result.Success)
            {
                return context;
            }

            ParseIngredientRows(ingredientRows, context);
            ParseRecipeRows(recipeRows, context);
            ParseRecipeIngredientRows(recipeIngredientRows, context);
            ValidateRecipeIngredientCoverage(context);

            result.ActiveIngredientCount = context.ActiveIngredients.Count;
            result.ActiveRecipeCount = context.ActiveRecipes.Count;
            result.ActiveRecipeIngredientCount = context.ActiveRecipeIngredients.Count;
            return context;
        }

        private static void ParseIngredientRows(List<CsvRow> rows, ImportContext context)
        {
            HashSet<string> seenIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (CsvRow row in rows)
            {
                string id = Required(row, "ingredient_id", context.Result);
                string displayName = Required(row, "ingredient_name", context.Result);
                bool active = ParseActive(row, context.Result);
                int difficulty = ParseInt(row, "difficulty", 0, context.Result);
                int buyPrice = ParseInt(row, "buy_price", 0, context.Result);
                int sellPrice = ParseInt(row, "sell_price", 0, context.Result);
                ResourceRarity rarity = ParseRarity(row, context.Result);

                if (!string.IsNullOrWhiteSpace(id) && !AllowedIngredientIds.Contains(id))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 허용되지 않은 ingredient_id입니다: {id}");
                }

                if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 중복 ingredient_id입니다: {id}");
                }

                if (!active || !context.Result.Success)
                {
                    continue;
                }

                context.ActiveIngredients[id] = new IngredientRow(
                    id,
                    displayName,
                    Value(row, "description"),
                    Value(row, "region_tag"),
                    rarity,
                    difficulty,
                    Value(row, "supply_source"),
                    Value(row, "acquisition_source"),
                    Value(row, "acquisition_method"),
                    Value(row, "acquisition_tool"),
                    buyPrice,
                    sellPrice,
                    Value(row, "memo"));
            }
        }

        private static void ParseRecipeRows(List<CsvRow> rows, ImportContext context)
        {
            HashSet<string> seenIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (CsvRow row in rows)
            {
                string id = Required(row, "recipe_id", context.Result);
                string displayName = Required(row, "recipe_name", context.Result);
                bool active = ParseActive(row, context.Result);
                int difficulty = ParseInt(row, "difficulty", 0, context.Result);
                int sellPrice = ParseInt(row, "sell_price", 0, context.Result);
                int reputationDelta = ParseInt(row, "reputation_delta", 0, context.Result);

                if (!string.IsNullOrWhiteSpace(id) && !AllowedRecipeIds.Contains(id))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 허용되지 않은 recipe_id입니다: {id}");
                }

                if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 중복 recipe_id입니다: {id}");
                }

                if (!active || !context.Result.Success)
                {
                    continue;
                }

                context.ActiveRecipes[id] = new RecipeRow(
                    id,
                    displayName,
                    Value(row, "description"),
                    Value(row, "supply_source"),
                    difficulty,
                    Value(row, "cooking_method"),
                    sellPrice,
                    reputationDelta,
                    Value(row, "memo"));
            }
        }

        private static void ParseRecipeIngredientRows(List<CsvRow> rows, ImportContext context)
        {
            Dictionary<string, HashSet<int>> sortOrdersByRecipe = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> recipeIngredientPairs = new(StringComparer.OrdinalIgnoreCase);

            foreach (CsvRow row in rows)
            {
                string recipeId = Required(row, "recipe_id", context.Result);
                string ingredientId = Required(row, "ingredient_id", context.Result);
                bool active = ParseActive(row, context.Result);
                int quantity = ParseInt(row, "quantity", 1, context.Result);
                int sortOrder = ParseInt(row, "sort_order", 0, context.Result);

                if (!active)
                {
                    continue;
                }

                if (!context.ActiveRecipes.ContainsKey(recipeId))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 활성 recipes.csv에 없는 recipe_id 참조입니다: {recipeId}");
                }

                if (!context.ActiveIngredients.ContainsKey(ingredientId))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 활성 ingredients.csv에 없는 ingredient_id 참조입니다: {ingredientId}");
                }

                string pairKey = $"{recipeId}|{ingredientId}";
                if (!string.IsNullOrWhiteSpace(recipeId)
                    && !string.IsNullOrWhiteSpace(ingredientId)
                    && !recipeIngredientPairs.Add(pairKey))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 같은 recipe 안에서 ingredient_id가 중복되었습니다: {recipeId}/{ingredientId}");
                }

                if (!sortOrdersByRecipe.TryGetValue(recipeId, out HashSet<int> sortOrders))
                {
                    sortOrders = new HashSet<int>();
                    sortOrdersByRecipe[recipeId] = sortOrders;
                }

                if (!sortOrders.Add(sortOrder))
                {
                    context.Result.AddError($"{row.FileName}:{row.RowNumber} 같은 recipe 안에서 sort_order가 중복되었습니다: {recipeId}/{sortOrder}");
                }

                if (!context.Result.Success)
                {
                    continue;
                }

                RecipeIngredientRow ingredientRow = new(ingredientId, quantity, sortOrder);
                context.ActiveRecipeIngredients.Add(ingredientRow);
                if (!context.RecipeIngredientsByRecipe.TryGetValue(recipeId, out List<RecipeIngredientRow> recipeIngredients))
                {
                    recipeIngredients = new List<RecipeIngredientRow>();
                    context.RecipeIngredientsByRecipe[recipeId] = recipeIngredients;
                }

                recipeIngredients.Add(ingredientRow);
            }
        }

        private static void ValidateRecipeIngredientCoverage(ImportContext context)
        {
            foreach (string recipeId in context.ActiveRecipes.Keys)
            {
                if (!context.RecipeIngredientsByRecipe.ContainsKey(recipeId))
                {
                    context.Result.AddError($"{RecipeIngredientsFileName}: 활성 레시피에 재료 매핑이 없습니다: {recipeId}");
                }
            }
        }

        private static void GenerateAssets(ImportContext context)
        {
            PrototypeGeneratedAssetSettings settings = PrototypeGeneratedAssetSettings.GetCurrent();
            EnsureFolderRecursive(settings.GameDataRoot);
            EnsureFolderRecursive(settings.ResourceDataRoot);
            EnsureFolderRecursive(settings.RecipeDataRoot);

            Dictionary<string, ResourceData> generatedResources = new(StringComparer.OrdinalIgnoreCase);
            List<ResourceData> manifestResources = new();
            List<IngredientRow> ingredientRows = new(context.ActiveIngredients.Values);
            ingredientRows.Sort((left, right) => string.Compare(left.IngredientId, right.IngredientId, StringComparison.Ordinal));

            foreach (IngredientRow row in ingredientRows)
            {
                string assetPath = $"{settings.ResourceDataRoot}/{BuildAssetFileName("resource", row.IngredientId)}.asset";
                ResourceData resource = LoadOrCreateAsset<ResourceData>(assetPath);
                resource.name = Path.GetFileNameWithoutExtension(assetPath);

                Sprite icon = LoadResourceSprite(settings.IngredientSpriteResourceRoot, row.IngredientId);
                if (icon == null)
                {
                    context.Result.AddWarning($"재료 아이콘을 찾지 못했습니다: Generated/Sprites/Item/Ingredient/{row.IngredientId}");
                }

                resource.ConfigureRuntime(row.IngredientId, row.DisplayName, row.Description, row.RegionTag, row.SellPrice, row.Rarity, icon);
                EditorUtility.SetDirty(resource);
                generatedResources[row.IngredientId] = resource;
                manifestResources.Add(resource);
            }

            List<RecipeData> manifestRecipes = new();
            List<RecipeRow> recipeRows = new(context.ActiveRecipes.Values);
            recipeRows.Sort((left, right) => string.Compare(left.RecipeId, right.RecipeId, StringComparison.Ordinal));

            foreach (RecipeRow row in recipeRows)
            {
                string assetPath = $"{settings.RecipeDataRoot}/{BuildAssetFileName("recipe", row.RecipeId)}.asset";
                RecipeData recipe = LoadOrCreateAsset<RecipeData>(assetPath);
                recipe.name = Path.GetFileNameWithoutExtension(assetPath);

                List<RecipeIngredient> ingredients = BuildRecipeIngredients(row.RecipeId, context, generatedResources);
                Sprite icon = LoadResourceSprite(settings.FoodSpriteResourceRoot, row.RecipeId);
                if (icon == null)
                {
                    context.Result.AddWarning($"레시피 아이콘을 찾지 못했습니다: Generated/Sprites/Item/Food/{row.RecipeId}");
                }

                recipe.ConfigureRuntime(
                    row.RecipeId,
                    row.DisplayName,
                    row.Description,
                    row.SellPrice,
                    row.ReputationDelta,
                    row.SupplySource,
                    row.Difficulty,
                    row.CookingMethod,
                    row.Memo,
                    ingredients,
                    icon);
                EditorUtility.SetDirty(recipe);
                manifestRecipes.Add(recipe);
            }

            DeleteInactiveGeneratedAssets(settings, context);
            GeneratedGameDataManifest manifest = LoadOrCreateAsset<GeneratedGameDataManifest>(settings.GeneratedGameDataManifestPath);
            manifest.ConfigureGeneratedAssets(manifestResources, manifestRecipes);
            EditorUtility.SetDirty(manifest);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<RecipeIngredient> BuildRecipeIngredients(
            string recipeId,
            ImportContext context,
            IReadOnlyDictionary<string, ResourceData> generatedResources)
        {
            List<RecipeIngredient> ingredients = new();
            if (!context.RecipeIngredientsByRecipe.TryGetValue(recipeId, out List<RecipeIngredientRow> rows))
            {
                return ingredients;
            }

            rows.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
            foreach (RecipeIngredientRow row in rows)
            {
                IngredientRow ingredient = context.ActiveIngredients[row.IngredientId];
                generatedResources.TryGetValue(row.IngredientId, out ResourceData resource);

                RecipeIngredient recipeIngredient = RecipeIngredient.CreateRuntime(
                    ingredient.IngredientId,
                    ingredient.DisplayName,
                    row.Quantity,
                    resource);
                recipeIngredient.ConfigureCatalogMetadata(
                    ingredient.Difficulty,
                    ingredient.SupplySource,
                    ingredient.AcquisitionSource,
                    ingredient.AcquisitionMethod,
                    ingredient.AcquisitionTool,
                    ingredient.BuyPrice,
                    ingredient.SellPrice,
                    ingredient.Memo);
                ingredients.Add(recipeIngredient);
            }

            return ingredients;
        }

        private static void DeleteInactiveGeneratedAssets(PrototypeGeneratedAssetSettings settings, ImportContext context)
        {
            foreach (string ingredientId in AllowedIngredientIds)
            {
                if (!context.ActiveIngredients.ContainsKey(ingredientId))
                {
                    DeleteGeneratedAssetIfExists($"{settings.ResourceDataRoot}/{BuildAssetFileName("resource", ingredientId)}.asset");
                }
            }

            foreach (string recipeId in AllowedRecipeIds)
            {
                if (!context.ActiveRecipes.ContainsKey(recipeId))
                {
                    DeleteGeneratedAssetIfExists($"{settings.RecipeDataRoot}/{BuildAssetFileName("recipe", recipeId)}.asset");
                }
            }
        }

        private static void DeleteGeneratedAssetIfExists(string assetPath)
        {
            if (!string.IsNullOrWhiteSpace(assetPath) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static Sprite LoadResourceSprite(string resourceRoot, string id)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot) || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return Resources.Load<Sprite>($"{resourceRoot}/{id.Trim()}");
        }

        private static List<CsvRow> ParseCsv(
            string content,
            string fileName,
            IReadOnlyList<string> requiredColumns,
            CsvGameDataImportResult result)
        {
            List<List<string>> records = ParseRecords(content ?? string.Empty, fileName, result);
            List<CsvRow> rows = new();
            if (records.Count == 0)
            {
                result.AddError($"{fileName}: CSV가 비어 있습니다.");
                return rows;
            }

            List<string> header = records[0];
            Dictionary<string, int> headerIndexes = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < header.Count; index++)
            {
                string column = NormalizeHeader(header[index]);
                if (!string.IsNullOrWhiteSpace(column) && !headerIndexes.ContainsKey(column))
                {
                    headerIndexes[column] = index;
                }
            }

            foreach (string requiredColumn in requiredColumns)
            {
                if (!headerIndexes.ContainsKey(requiredColumn))
                {
                    result.AddError($"{fileName}: 필수 열이 없습니다: {requiredColumn}");
                }
            }

            if (!result.Success)
            {
                return rows;
            }

            for (int recordIndex = 1; recordIndex < records.Count; recordIndex++)
            {
                List<string> record = records[recordIndex];
                if (IsBlankRecord(record))
                {
                    continue;
                }

                Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, int> headerIndex in headerIndexes)
                {
                    values[headerIndex.Key] = headerIndex.Value < record.Count ? record[headerIndex.Value].Trim() : string.Empty;
                }

                rows.Add(new CsvRow(fileName, recordIndex + 1, values));
            }

            return rows;
        }

        private static List<List<string>> ParseRecords(string content, string fileName, CsvGameDataImportResult result)
        {
            List<List<string>> records = new();
            List<string> currentRecord = new();
            StringBuilder field = new();
            bool inQuotes = false;

            if (content.Length > 0 && content[0] == '\uFEFF')
            {
                content = content.Substring(1);
            }

            for (int index = 0; index < content.Length; index++)
            {
                char current = content[index];
                if (inQuotes)
                {
                    if (current == '"')
                    {
                        if (index + 1 < content.Length && content[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(current);
                    }

                    continue;
                }

                if (current == '"')
                {
                    inQuotes = true;
                    continue;
                }

                if (current == ',')
                {
                    currentRecord.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (current == '\r' || current == '\n')
                {
                    currentRecord.Add(field.ToString());
                    field.Clear();
                    records.Add(currentRecord);
                    currentRecord = new List<string>();

                    if (current == '\r' && index + 1 < content.Length && content[index + 1] == '\n')
                    {
                        index++;
                    }

                    continue;
                }

                field.Append(current);
            }

            if (inQuotes)
            {
                result.AddError($"{fileName}: 닫히지 않은 따옴표가 있습니다.");
            }

            if (field.Length > 0 || currentRecord.Count > 0)
            {
                currentRecord.Add(field.ToString());
                records.Add(currentRecord);
            }

            return records;
        }

        private static string Required(CsvRow row, string column, CsvGameDataImportResult result)
        {
            string value = Value(row, column);
            if (string.IsNullOrWhiteSpace(value))
            {
                result.AddError($"{row.FileName}:{row.RowNumber} 필수 값이 비어 있습니다: {column}");
            }

            return value;
        }

        private static string Value(CsvRow row, string column)
        {
            return row.Values.TryGetValue(column, out string value) ? value.Trim() : string.Empty;
        }

        private static int ParseInt(CsvRow row, string column, int minimum, CsvGameDataImportResult result)
        {
            string value = Required(row, column, result);
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                result.AddError($"{row.FileName}:{row.RowNumber} 숫자 열이 잘못되었습니다: {column}={value}");
                return minimum;
            }

            if (parsed < minimum)
            {
                result.AddError($"{row.FileName}:{row.RowNumber} {column} 값은 {minimum} 이상이어야 합니다: {value}");
                return minimum;
            }

            return parsed;
        }

        private static bool ParseActive(CsvRow row, CsvGameDataImportResult result)
        {
            string value = Value(row, "active");
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                    return true;
                case "false":
                case "0":
                case "no":
                case "n":
                    return false;
                default:
                    result.AddError($"{row.FileName}:{row.RowNumber} active 값이 잘못되었습니다: {value}");
                    return false;
            }
        }

        private static ResourceRarity ParseRarity(CsvRow row, CsvGameDataImportResult result)
        {
            string value = Required(row, "rarity", result);
            if (Enum.TryParse(value, true, out ResourceRarity rarity))
            {
                return rarity;
            }

            switch (value.Trim())
            {
                case "일반":
                    return ResourceRarity.Common;
                case "고급":
                    return ResourceRarity.Uncommon;
                case "희귀":
                    return ResourceRarity.Rare;
                case "특급":
                    return ResourceRarity.Epic;
                default:
                    result.AddError($"{row.FileName}:{row.RowNumber} rarity 값이 잘못되었습니다: {value}");
                    return ResourceRarity.Common;
            }
        }

        private static bool IsBlankRecord(List<string> record)
        {
            if (record == null || record.Count == 0)
            {
                return true;
            }

            foreach (string field in record)
            {
                if (!string.IsNullOrWhiteSpace(field))
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeHeader(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimStart('\uFEFF').ToLowerInvariant();
        }

        private static string ReadRequiredTextAsset(string assetPath, CsvGameDataImportResult result)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
            {
                result.AddError($"CSV 파일을 찾지 못했습니다: {assetPath}");
                return string.Empty;
            }

            return File.ReadAllText(assetPath, Encoding.UTF8);
        }

        private static void EnsureFolderRecursive(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] segments = NormalizeAssetPath(folderPath, string.Empty).Split('/');
            string currentPath = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = currentPath + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }

        private static string NormalizeAssetPath(string value, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().Replace('\\', '/');
            while (normalized.Contains("//"))
            {
                normalized = normalized.Replace("//", "/");
            }

            return normalized.TrimEnd('/');
        }

        private static string BuildAssetFileName(string prefix, string id)
        {
            string normalizedId = string.IsNullOrWhiteSpace(id)
                ? string.Empty
                : id.Trim().ToLowerInvariant().Replace('_', '-');
            return $"{prefix}-{normalizedId}";
        }

        private sealed class ImportContext
        {
            public ImportContext(CsvGameDataImportResult result)
            {
                Result = result;
            }

            public CsvGameDataImportResult Result { get; }
            public Dictionary<string, IngredientRow> ActiveIngredients { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, RecipeRow> ActiveRecipes { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<RecipeIngredientRow> ActiveRecipeIngredients { get; } = new();
            public Dictionary<string, List<RecipeIngredientRow>> RecipeIngredientsByRecipe { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class CsvRow
        {
            public CsvRow(string fileName, int rowNumber, IReadOnlyDictionary<string, string> values)
            {
                FileName = fileName;
                RowNumber = rowNumber;
                Values = values;
            }

            public string FileName { get; }
            public int RowNumber { get; }
            public IReadOnlyDictionary<string, string> Values { get; }
        }

        private sealed class IngredientRow
        {
            public IngredientRow(
                string ingredientId,
                string displayName,
                string description,
                string regionTag,
                ResourceRarity rarity,
                int difficulty,
                string supplySource,
                string acquisitionSource,
                string acquisitionMethod,
                string acquisitionTool,
                int buyPrice,
                int sellPrice,
                string memo)
            {
                IngredientId = ingredientId;
                DisplayName = displayName;
                Description = description;
                RegionTag = regionTag;
                Rarity = rarity;
                Difficulty = difficulty;
                SupplySource = supplySource;
                AcquisitionSource = acquisitionSource;
                AcquisitionMethod = acquisitionMethod;
                AcquisitionTool = acquisitionTool;
                BuyPrice = buyPrice;
                SellPrice = sellPrice;
                Memo = memo;
            }

            public string IngredientId { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public string RegionTag { get; }
            public ResourceRarity Rarity { get; }
            public int Difficulty { get; }
            public string SupplySource { get; }
            public string AcquisitionSource { get; }
            public string AcquisitionMethod { get; }
            public string AcquisitionTool { get; }
            public int BuyPrice { get; }
            public int SellPrice { get; }
            public string Memo { get; }
        }

        private sealed class RecipeRow
        {
            public RecipeRow(
                string recipeId,
                string displayName,
                string description,
                string supplySource,
                int difficulty,
                string cookingMethod,
                int sellPrice,
                int reputationDelta,
                string memo)
            {
                RecipeId = recipeId;
                DisplayName = displayName;
                Description = description;
                SupplySource = supplySource;
                Difficulty = difficulty;
                CookingMethod = cookingMethod;
                SellPrice = sellPrice;
                ReputationDelta = reputationDelta;
                Memo = memo;
            }

            public string RecipeId { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public string SupplySource { get; }
            public int Difficulty { get; }
            public string CookingMethod { get; }
            public int SellPrice { get; }
            public int ReputationDelta { get; }
            public string Memo { get; }
        }

        private readonly struct RecipeIngredientRow
        {
            public RecipeIngredientRow(string ingredientId, int quantity, int sortOrder)
            {
                IngredientId = ingredientId;
                Quantity = quantity;
                SortOrder = sortOrder;
            }

            public string IngredientId { get; }
            public int Quantity { get; }
            public int SortOrder { get; }
        }
    }

    /// <summary>
    /// CSV importer 검증과 생성 결과를 에디터 메뉴와 테스트 양쪽에서 공유한다.
    /// </summary>
    public sealed class CsvGameDataImportResult
    {
        private readonly List<string> errors = new();
        private readonly List<string> warnings = new();

        public bool Success => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public int ActiveIngredientCount { get; internal set; }
        public int ActiveRecipeCount { get; internal set; }
        public int ActiveRecipeIngredientCount { get; internal set; }

        internal void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                errors.Add(error);
            }
        }

        internal void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                warnings.Add(warning);
            }
        }

        public string BuildSummary()
        {
            if (!Success)
            {
                return "CSV 게임 데이터 생성 실패\n" + string.Join("\n", errors);
            }

            return $"CSV 게임 데이터 생성 완료: 재료 {ActiveIngredientCount}개, 레시피 {ActiveRecipeCount}개, 매핑 {ActiveRecipeIngredientCount}개";
        }
    }
}
#endif
