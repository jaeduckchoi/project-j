using NUnit.Framework;
using Code.Scripts.Restaurant;

namespace Editor.Tests
{
    public class TodayMenuStateEditModeTests
    {
        [Test]
        public void Constructor_InitializesThreeFixedSlotsAsIncomplete()
        {
            TodayMenuState state = new();

            Assert.That(state.RecipeIds.Count, Is.EqualTo(TodayMenuState.SlotCount));
            Assert.That(state.SelectedSlotIndex, Is.EqualTo(0));
            Assert.That(state.IsComplete, Is.False);
            Assert.That(state.GetRecipeId(0), Is.Empty);
            Assert.That(state.GetRecipeId(1), Is.Empty);
            Assert.That(state.GetRecipeId(2), Is.Empty);
        }

        [Test]
        public void AssignRecipeToSelectedSlot_MovesDuplicateRecipeToCurrentSlot()
        {
            TodayMenuState state = new();

            Assert.That(state.AssignRecipeToSelectedSlot("food_001"), Is.True);
            Assert.That(state.SelectSlot(1), Is.True);
            Assert.That(state.AssignRecipeToSelectedSlot("food_002"), Is.True);
            Assert.That(state.SelectSlot(2), Is.True);
            Assert.That(state.AssignRecipeToSelectedSlot("food_001"), Is.True);

            Assert.That(state.GetRecipeId(0), Is.Empty);
            Assert.That(state.GetRecipeId(1), Is.EqualTo("food_002"));
            Assert.That(state.GetRecipeId(2), Is.EqualTo("food_001"));
            Assert.That(state.IsComplete, Is.False);
        }

        [Test]
        public void SelectSlot_RejectsOutOfRangeIndexWithoutChangingSelection()
        {
            TodayMenuState state = new();

            Assert.That(state.SelectSlot(-1), Is.False);
            Assert.That(state.SelectSlot(TodayMenuState.SlotCount), Is.False);
            Assert.That(state.SelectedSlotIndex, Is.EqualTo(0));
        }

        [Test]
        public void IsComplete_BecomesTrueOnlyAfterThreeUniqueAssignments()
        {
            TodayMenuState state = new();

            state.AssignRecipeToSelectedSlot("food_001");
            state.SelectSlot(1);
            state.AssignRecipeToSelectedSlot("food_002");
            state.SelectSlot(2);

            Assert.That(state.IsComplete, Is.False);

            state.AssignRecipeToSelectedSlot("food_003");

            Assert.That(state.IsComplete, Is.True);
        }
    }
}
