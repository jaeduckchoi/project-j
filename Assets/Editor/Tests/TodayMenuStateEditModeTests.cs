using NUnit.Framework;
using Restaurant;

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

            Assert.That(state.AssignRecipeToSelectedSlot("kimchi_fried_rice"), Is.True);
            Assert.That(state.SelectSlot(1), Is.True);
            Assert.That(state.AssignRecipeToSelectedSlot("kimchi_stew"), Is.True);
            Assert.That(state.SelectSlot(2), Is.True);
            Assert.That(state.AssignRecipeToSelectedSlot("kimchi_fried_rice"), Is.True);

            Assert.That(state.GetRecipeId(0), Is.Empty);
            Assert.That(state.GetRecipeId(1), Is.EqualTo("kimchi_stew"));
            Assert.That(state.GetRecipeId(2), Is.EqualTo("kimchi_fried_rice"));
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

            state.AssignRecipeToSelectedSlot("kimchi_fried_rice");
            state.SelectSlot(1);
            state.AssignRecipeToSelectedSlot("kimchi_stew");
            state.SelectSlot(2);

            Assert.That(state.IsComplete, Is.False);

            state.AssignRecipeToSelectedSlot("kimchi_pancake");

            Assert.That(state.IsComplete, Is.True);
        }
    }
}
