using System.Linq;
using System.Collections.Generic;
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

        [Fact]
        public void LoadSlotRowsContainDisplayTextAndLoadDeleteIndexes()
        {
            var viewModel = new TitleViewModel();
            var slots = new List<GameSave>
            {
                new GameSave
                {
                    Time = new System.DateTime(2026, 5, 14, 12, 0, 0),
                    Party = new Party
                    {
                        PlayerName = "Quest One",
                        CurrentMapId = "StartTown",
                        CurrentPosition = WorldPosition.Zero,
                        Gold = 42,
                        StepCount = 7
                    }
                }
            };
            slots[0].Party.Members.Add(new Hero { Name = "Able", IsActive = true, Level = 3, Health = 10, MaxHealth = 10 });

            var row = Assert.Single(viewModel.GetLoadSlotRows(slots));

            Assert.Equal(0, row.SlotIndex);
            Assert.Equal(TitleViewModel.GetLoadSaveIndex(0), row.LoadIndex);
            Assert.Equal(TitleViewModel.GetLoadDeleteIndex(0), row.DeleteIndex);
            Assert.Equal("Quest One", row.Title);
            Assert.Contains("Level 3", row.Summary);
            Assert.Equal(row.Title + "\n" + row.Summary, row.ButtonText);
            Assert.Equal("Delete", row.DeleteButtonText);
        }

        [Fact]
        public void LoadSlotRowsMarkSelectedLoadAndDeleteButtons()
        {
            var viewModel = new TitleViewModel();
            var slots = new List<GameSave>
            {
                new GameSave { Party = new Party { PlayerName = "Quest One" } },
                new GameSave { Party = new Party { PlayerName = "Quest Two" } }
            };

            var loadRows = viewModel.GetLoadSlotRows(slots, TitleViewModel.GetLoadSaveIndex(1));
            Assert.False(loadRows[0].LoadSelected);
            Assert.True(loadRows[1].LoadSelected);
            Assert.False(loadRows[1].DeleteSelected);

            var deleteRows = viewModel.GetLoadSlotRows(slots, TitleViewModel.GetLoadDeleteIndex(0));
            Assert.True(deleteRows[0].DeleteSelected);
            Assert.False(deleteRows[0].LoadSelected);
        }

        [Fact]
        public void ClampLoadSelectionUsesBackRowAsMaximum()
        {
            var viewModel = new TitleViewModel();
            viewModel.SetSelectedIndex(99);

            Assert.Equal(TitleViewModel.GetLoadBackIndex(2), viewModel.ClampLoadSelection(2));
            Assert.True(viewModel.IsLoadBackSelected(2));
        }
    }
}
