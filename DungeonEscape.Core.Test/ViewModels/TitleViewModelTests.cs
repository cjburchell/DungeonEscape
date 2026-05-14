using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.ViewModels;
using Xunit;

namespace DungeonEscape.Core.Test.ViewModels
{
    public sealed class TitleViewModelTests
    {
        [Fact]
        public void MainRowsReflectAvailableSaveActions()
        {
            var viewModel = new TitleViewModel();

            var noSaves = viewModel.GetMainRows(false, 0).Select(row => row.Action).ToArray();
            var allSaves = viewModel.GetMainRows(true, 2).Select(row => row.Action).ToArray();

            Assert.Equal(new[] { TitleMainAction.NewQuest, TitleMainAction.Quit }, noSaves);
            Assert.Equal(new[] { TitleMainAction.Continue, TitleMainAction.NewQuest, TitleMainAction.LoadQuest, TitleMainAction.Quit }, allSaves);
        }

        [Fact]
        public void CreateNavigationMatchesTwoColumnLayout()
        {
            var viewModel = new TitleViewModel();
            viewModel.ShowCreateMenu();

            viewModel.SetSelectedIndex(TitleViewModel.CreateNameIndex);
            Assert.Equal(TitleViewModel.CreateGenerateNameIndex, viewModel.GetCreateNavigationIndex(1, 0));
            Assert.Equal(TitleViewModel.CreateGenderIndex, viewModel.GetCreateNavigationIndex(0, 1));

            viewModel.SetSelectedIndex(TitleViewModel.CreateBackIndex);
            Assert.Equal(TitleViewModel.CreateStartIndex, viewModel.GetCreateNavigationIndex(-1, 0));
            Assert.Equal(TitleViewModel.CreateRerollIndex, viewModel.GetCreateNavigationIndex(0, -1));
        }

        [Fact]
        public void LoadNavigationMovesBetweenSaveLoadDeleteAndBackRows()
        {
            var viewModel = new TitleViewModel();
            viewModel.ShowLoadMenu();

            Assert.Equal(TitleViewModel.GetLoadSaveIndex(1), viewModel.GetLoadNavigationIndex(0, 1, 3));

            viewModel.SetSelectedIndex(TitleViewModel.GetLoadSaveIndex(1));
            Assert.Equal(TitleViewModel.GetLoadDeleteIndex(1), viewModel.GetLoadNavigationIndex(1, 0, 3));
            Assert.Equal(TitleViewModel.GetLoadSaveIndex(0), viewModel.GetLoadNavigationIndex(0, -1, 3));

            viewModel.SetSelectedIndex(TitleViewModel.GetLoadSaveIndex(2));
            Assert.Equal(TitleViewModel.GetLoadBackIndex(3), viewModel.GetLoadNavigationIndex(0, 1, 3));
        }

        [Fact]
        public void CreateCyclingWrapsGenderClassAndSprite()
        {
            var viewModel = new TitleViewModel();
            viewModel.SetCreatePlayerGender(Gender.Male);
            viewModel.SetCreatePlayerClass(Class.Hero);
            viewModel.SetCreatePlayerSpriteIndex(0);

            viewModel.CycleCreateGender(-1);
            viewModel.CycleCreateClass(-1);
            viewModel.CycleCreateImage(-1, 4);

            Assert.Equal(Gender.Female, viewModel.CreatePlayerGender);
            Assert.NotEqual(Class.Hero, viewModel.CreatePlayerClass);
            Assert.Equal(3, viewModel.CreatePlayerSpriteIndex);
        }

        [Fact]
        public void SpriteSelectionSkipsBlockedCharacterIndexes()
        {
            Assert.Equal(20, TitleViewModel.GetNextSelectableCreateImageIndex(17, 1, 24));
            Assert.Equal(17, TitleViewModel.GetNextSelectableCreateImageIndex(20, -1, 24));

            var viewModel = new TitleViewModel();
            viewModel.SetCreatePlayerSpriteIndex(TitleViewModel.FirstBlockedCreateSpriteIndex);
            viewModel.EnsureSelectableCreateImageIndex(24);

            Assert.NotEqual(TitleViewModel.FirstBlockedCreateSpriteIndex, viewModel.CreatePlayerSpriteIndex);
            Assert.NotEqual(TitleViewModel.SecondBlockedCreateSpriteIndex, viewModel.CreatePlayerSpriteIndex);
        }

        [Fact]
        public void DropdownIndexClampsToAvailableValues()
        {
            var viewModel = new TitleViewModel();
            viewModel.SetSelectedDropdownIndex(99);

            Assert.Equal(2, viewModel.ClampDropdownIndex(3));
        }
    }
}
