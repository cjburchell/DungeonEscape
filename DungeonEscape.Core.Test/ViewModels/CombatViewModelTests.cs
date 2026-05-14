using Redpoint.DungeonEscape.ViewModels;
using Xunit;

namespace DungeonEscape.Core.Test.ViewModels
{
    public sealed class CombatViewModelTests
    {
        [Fact]
        public void MoveSelectionClampsForMenus()
        {
            var viewModel = new CombatViewModel();

            Assert.Equal(0, viewModel.MoveSelection(-1, 3, false));
            Assert.Equal(1, viewModel.MoveSelection(1, 3, false));
            viewModel.SetSelectedMenuIndex(2);
            Assert.Equal(2, viewModel.MoveSelection(1, 3, false));
        }

        [Fact]
        public void MoveSelectionWrapsForMonsterTargets()
        {
            var viewModel = new CombatViewModel();

            Assert.Equal(2, viewModel.MoveSelection(-1, 3, true));
            Assert.Equal(0, viewModel.MoveSelection(1, 3, true));
        }

        [Fact]
        public void MoveSelectionResetsWhenEmpty()
        {
            var viewModel = new CombatViewModel();
            viewModel.SetSelectedMenuIndex(5);

            Assert.Equal(0, viewModel.MoveSelection(1, 0, true));
        }
    }
}
