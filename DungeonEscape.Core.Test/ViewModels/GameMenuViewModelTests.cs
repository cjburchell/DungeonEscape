using Redpoint.DungeonEscape.ViewModels;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void SettingsAndSaveLoadRowCountsMatchTabsAndSaveCounts()
        {
            var viewModel = new GameMenuViewModel();

            Assert.Equal(8, viewModel.GetSettingsSelectableRowCount(GameMenuViewModel.SettingsGeneral, 4));
            Assert.Equal(9, viewModel.GetSettingsSelectableRowCount(GameMenuViewModel.SettingsUi, 4));
            Assert.Equal(6, viewModel.GetSettingsSelectableRowCount(GameMenuViewModel.SettingsInput, 4));
            Assert.Equal(6, viewModel.GetSettingsSelectableRowCount(GameMenuViewModel.SettingsDebug, 4));
            Assert.Equal(3, viewModel.GetSaveSelectableRowCount(3));
            Assert.Equal(2, viewModel.GetLoadSelectableRowCount(2));
        }

        [Fact]
        public void InventoryMembersActiveThenReserveByOrder()
        {
            var viewModel = new GameMenuViewModel();
            var reserve = CreateHero("Reserve", false, 0);
            var activeTwo = CreateHero("Active Two", true, 2);
            var activeOne = CreateHero("Active One", true, 1);
            var party = new Party();
            party.Members.Add(reserve);
            party.Members.Add(activeTwo);
            party.Members.Add(activeOne);

            var members = viewModel.GetInventoryMembers(party);

            Assert.Equal(new[] { "Active One", "Active Two", "Reserve" }, members.Select(member => member.Name).ToArray());
        }

        [Fact]
        public void MenuMembersFilterSpellAndAbilityScreens()
        {
            var viewModel = new GameMenuViewModel();
            var caster = CreateHero("Caster", true, 0);
            var skilled = CreateHero("Skilled", true, 1);
            var party = new Party();
            party.Members.Add(caster);
            party.Members.Add(skilled);

            var spellMembers = viewModel.GetMenuMembers(party, GameMenuViewModel.ScreenSpells, hero => hero.Name == "Caster", hero => true);
            var abilityMembers = viewModel.GetMenuMembers(party, GameMenuViewModel.ScreenAbilities, hero => true, hero => hero.Name == "Skilled");

            Assert.Equal("Caster", Assert.Single(spellMembers).Name);
            Assert.Equal("Skilled", Assert.Single(abilityMembers).Name);
        }

        [Fact]
        public void MainActionsReflectAvailableCapabilities()
        {
            var viewModel = new GameMenuViewModel();

            Assert.Equal(
                new[] { "Items", "Equipment", "Status", "Quests", "Misc." },
                viewModel.GetMainActions(false, false, false));

            Assert.Equal(
                new[] { "Items", "Spells", "Equipment", "Abilities", "Status", "Quests", "Party", "Misc." },
                viewModel.GetMainActions(true, true, true));
        }

        [Fact]
        public void DetailCountsUseSelectedScreenData()
        {
            var viewModel = new GameMenuViewModel();
            var hero = CreateHero("Hero", true, 0);
            hero.Items.Add(CreateItemInstance("Potion", ItemType.OneUse, Slot.PrimaryHand));

            Assert.Equal(1, viewModel.GetCurrentDetailCount(GameMenuViewModel.ScreenItems, hero, 0, 0));
            Assert.Equal(2, viewModel.GetCurrentDetailCount(GameMenuViewModel.ScreenSpells, hero, 2, 0));
            Assert.Equal(3, viewModel.GetCurrentDetailCount(GameMenuViewModel.ScreenAbilities, hero, 0, 3));
            Assert.Equal(viewModel.GetEquipmentSlots().Count, viewModel.GetCurrentDetailCount(GameMenuViewModel.ScreenEquipment, hero, 0, 0));
        }

        [Fact]
        public void EquipmentCandidatesIncludeEquippedItemBeforeValidUnequippedItems()
        {
            var viewModel = new GameMenuViewModel();
            var hero = CreateHero("Hero", true, 0);
            hero.Class = Class.Hero;
            var equipped = CreateItemInstance("Old Sword", ItemType.Weapon, Slot.PrimaryHand);
            var candidate = CreateItemInstance("New Sword", ItemType.Weapon, Slot.PrimaryHand);
            var wrongSlot = CreateItemInstance("Armor", ItemType.Armor, Slot.Chest);
            equipped.IsEquipped = true;
            hero.Items.Add(equipped);
            hero.Items.Add(candidate);
            hero.Items.Add(wrongSlot);
            hero.Slots[Slot.PrimaryHand] = equipped.Id;

            var candidates = viewModel.GetEquipmentCandidates(hero, Slot.PrimaryHand);

            Assert.Equal(new[] { "Old Sword", "New Sword" }, candidates.Select(item => item.Name).ToArray());
            Assert.Same(equipped, viewModel.GetEquippedItem(hero, Slot.PrimaryHand));
        }

        private static Hero CreateHero(string name, bool isActive, int order)
        {
            return new Hero
            {
                Name = name,
                IsActive = isActive,
                Order = order,
                Class = Class.Hero,
                Health = 10,
                MaxHealth = 10,
                Items = new List<ItemInstance>()
            };
        }

        private static ItemInstance CreateItemInstance(string name, ItemType type, Slot slot)
        {
            return new ItemInstance(new Item
            {
                Id = name,
                Name = name,
                Type = type,
                Slots = new List<Slot> { slot },
                Classes = new List<Class> { Class.Hero }
            });
        }
    }
}
