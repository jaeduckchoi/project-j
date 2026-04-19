using Editor.GameData;
using NUnit.Framework;

namespace Editor.Tests
{
    public class CsvGameDataImporterEditModeTests
    {
        private const string ValidIngredientsCsv =
            "ingredient_id,ingredient_name,description,region_tag,rarity,difficulty,supply_source,acquisition_source,acquisition_method,acquisition_tool,buy_price,sell_price,memo,active\n" +
            "ingredient_001,김치,기본 재료,기본,Common,1,기본 냉장고,허브,기본 제공,냉장고,0,4,,true\n" +
            "ingredient_002,밥,기본 재료,기본,Common,1,기본 냉장고,허브,기본 제공,냉장고,0,3,,true\n" +
            "ingredient_003,밀가루,기본 재료,기본,Common,0,기본 냉장고,허브,기본 제공,냉장고,0,0,,true\n" +
            "ingredient_004,고춧가루,기본 재료,기본,Common,0,기본 냉장고,허브,기본 제공,냉장고,0,0,,true\n";

        private const string ValidRecipesCsv =
            "recipe_id,recipe_name,description,supply_source,difficulty,cooking_method,sell_price,reputation_delta,memo,active\n" +
            "food_001,김치볶음밥,기본 메뉴,기본 주방,1,후라이팬,28,1,,true\n" +
            "food_002,김치찌개,기본 메뉴,기본 주방,1,냄비,32,1,,true\n" +
            "food_003,김치전,기본 메뉴,기본 주방,1,후라이팬,24,1,,true\n";

        private const string ValidRecipeIngredientsCsv =
            "recipe_id,ingredient_id,quantity,sort_order,active\n" +
            "food_001,ingredient_001,1,0,true\n" +
            "food_001,ingredient_002,1,1,true\n" +
            "food_002,ingredient_001,1,0,true\n" +
            "food_002,ingredient_004,1,1,true\n" +
            "food_003,ingredient_001,1,0,true\n" +
            "food_003,ingredient_003,1,1,true\n";

        [Test]
        public void ValidateCsvText_AcceptsSeedRows()
        {
            CsvGameDataImportResult result = Validate();

            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.ActiveIngredientCount, Is.EqualTo(4));
            Assert.That(result.ActiveRecipeCount, Is.EqualTo(3));
            Assert.That(result.ActiveRecipeIngredientCount, Is.EqualTo(6));
        }

        [Test]
        public void ValidateCsvText_ExcludesInactiveRows()
        {
            string ingredients = ValidIngredientsCsv +
                "ingredient_005,비활성 재료,미사용,기본,Common,1,기본 냉장고,허브,기본 제공,냉장고,0,1,,false\n";
            string recipes = ValidRecipesCsv +
                "food_021,비활성 메뉴,미사용,기본 주방,1,후라이팬,10,0,,false\n";
            string recipeIngredients = ValidRecipeIngredientsCsv +
                "food_021,ingredient_005,1,0,false\n";

            CsvGameDataImportResult result = Validate(ingredients, recipes, recipeIngredients);

            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.ActiveIngredientCount, Is.EqualTo(4));
            Assert.That(result.ActiveRecipeCount, Is.EqualTo(3));
            Assert.That(result.ActiveRecipeIngredientCount, Is.EqualTo(6));
        }

        [Test]
        public void ValidateCsvText_RejectsAllowlistViolationsAndDuplicateIds()
        {
            string ingredients = ValidIngredientsCsv +
                "ingredient_999,잘못된 재료,미사용,기본,Common,1,기본 냉장고,허브,기본 제공,냉장고,0,1,,true\n" +
                "ingredient_001,중복 재료,미사용,기본,Common,1,기본 냉장고,허브,기본 제공,냉장고,0,1,,true\n";
            string recipes = ValidRecipesCsv +
                "food_999,잘못된 메뉴,미사용,기본 주방,1,후라이팬,10,0,,true\n" +
                "food_001,중복 메뉴,미사용,기본 주방,1,후라이팬,10,0,,true\n";

            CsvGameDataImportResult result = Validate(ingredients, recipes);

            Assert.That(result.Success, Is.False);
            Assert.That(ContainsError(result, "허용되지 않은 ingredient_id"), Is.True);
            Assert.That(ContainsError(result, "중복 ingredient_id"), Is.True);
            Assert.That(ContainsError(result, "허용되지 않은 recipe_id"), Is.True);
            Assert.That(ContainsError(result, "중복 recipe_id"), Is.True);
        }

        [Test]
        public void ValidateCsvText_RejectsMissingReferencesAndDuplicateSortOrder()
        {
            string recipeIngredients =
                "recipe_id,ingredient_id,quantity,sort_order,active\n" +
                "food_001,ingredient_001,1,0,true\n" +
                "food_001,ingredient_002,1,1,true\n" +
                "food_001,ingredient_004,1,1,true\n" +
                "food_002,ingredient_999,1,0,true\n" +
                "food_003,ingredient_001,1,0,true\n" +
                "food_003,ingredient_003,1,1,true\n";

            CsvGameDataImportResult result = Validate(ValidIngredientsCsv, ValidRecipesCsv, recipeIngredients);

            Assert.That(result.Success, Is.False);
            Assert.That(ContainsError(result, "없는 ingredient_id 참조"), Is.True);
            Assert.That(ContainsError(result, "sort_order가 중복"), Is.True);
        }

        [Test]
        public void ValidateCsvText_RejectsBadNumbersAndRarity()
        {
            string ingredients =
                "ingredient_id,ingredient_name,description,region_tag,rarity,difficulty,supply_source,acquisition_source,acquisition_method,acquisition_tool,buy_price,sell_price,memo,active\n" +
                "ingredient_001,김치,기본 재료,기본,Legendary,어려움,기본 냉장고,허브,기본 제공,냉장고,0,4,,true\n";
            string recipes =
                "recipe_id,recipe_name,description,supply_source,difficulty,cooking_method,sell_price,reputation_delta,memo,active\n" +
                "food_001,김치볶음밥,기본 메뉴,기본 주방,1,후라이팬,비쌈,1,,true\n";
            string recipeIngredients =
                "recipe_id,ingredient_id,quantity,sort_order,active\n" +
                "food_001,ingredient_001,0,0,true\n";

            CsvGameDataImportResult result = Validate(ingredients, recipes, recipeIngredients);

            Assert.That(result.Success, Is.False);
            Assert.That(ContainsError(result, "rarity 값이 잘못"), Is.True);
            Assert.That(ContainsError(result, "숫자 열이 잘못"), Is.True);
            Assert.That(ContainsError(result, "quantity 값은 1 이상"), Is.True);
        }

        private static CsvGameDataImportResult Validate(
            string ingredientsCsv = ValidIngredientsCsv,
            string recipesCsv = ValidRecipesCsv,
            string recipeIngredientsCsv = ValidRecipeIngredientsCsv)
        {
            return CsvGameDataImporter.ValidateCsvText(ingredientsCsv, recipesCsv, recipeIngredientsCsv);
        }

        private static bool ContainsError(CsvGameDataImportResult result, string fragment)
        {
            foreach (string error in result.Errors)
            {
                if (error.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
