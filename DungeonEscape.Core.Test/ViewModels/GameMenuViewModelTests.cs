using Redpoint.DungeonEscape.ViewModels;
using Xunit;

namespace DungeonEscape.Core.Test.ViewModels
{
    public sealed class GameMenuViewModelTests
    {
        [Fact]
        public void ResetReturnsMenuStateToDefaults()
        {
            var viewModel = new GameMenuViewModel();
            viewModel.SetCurrentScreen(4);
            viewModel.SetCurrentFocus(2);
            viewModel.SetSelectedRowIndex(8);
            viewModel.SetSelectedDetailIndex(7);

            viewModel.Reset();

            Assert.Equal(0, viewModel.CurrentScreen);
            Assert.Equal(0, viewModel.CurrentFocus);
            Assert.Equal(0, viewModel.SelectedRowIndex);
            Assert.Equal(0, viewModel.SelectedDetailIndex);
        }

        [Fact]
        public void RowSelectionMovesWithinBounds()
        {
            var viewModel = new GameMenuViewModel();

            Assert.Equal(0, viewModel.MoveSelectedRowIndex(-1, 3));
            Assert.Equal(1, viewModel.MoveSelectedRowIndex(1, 3));
            Assert.Equal(2, viewModel.MoveSelectedRowIndex(5, 3));
            Assert.Equal(0, viewModel.MoveSelectedRowIndex(-5, 3));
            Assert.Equal(0, viewModel.MoveSelectedRowIndex(1, 0));
        }

        [Fact]
        public void DetailSelectionUpdatesPageIndex()
        {
            var viewModel = new GameMenuViewModel();

            Assert.Equal(12, viewModel.MoveSelectedDetailIndex(12, 20, 10));
            Assert.Equal(1, viewModel.DetailPageIndex);

            Assert.Equal(19, viewModel.MoveSelectedDetailIndex(20, 20, 10));
            Assert.Equal(1, viewModel.DetailPageIndex);
        }

        [Fact]
        public void ClampHelpersHandleEmptyLists()
        {
            var viewModel = new GameMenuViewModel();
            viewModel.SetSelectedRowIndex(10);
            viewModel.SetSelectedDetailIndex(10);
            viewModel.SetSelectedEquipmentItemIndex(10);

            Assert.Equal(0, viewModel.ClampSelectedRowIndex(0));
            Assert.Equal(0, viewModel.ClampSelectedDetailIndex(0));
            Assert.Equal(0, viewModel.ClampSelectedEquipmentItemIndex(0));
        }

        [Fact]
        public void SelectDetailPageChoosesFirstItemOnPage()
        {
            var viewModel = new GameMenuViewModel();

            Assert.Equal(10, viewModel.SelectDetailPage(1, 25, 10));
            Assert.Equal(1, viewModel.DetailPageIndex);

            Assert.Equal(24, viewModel.SelectDetailPage(3, 25, 10));
        }
    }
}
