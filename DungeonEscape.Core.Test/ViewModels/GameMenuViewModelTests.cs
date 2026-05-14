using Redpoint.DungeonEscape.ViewModels;
using Redpoint.DungeonEscape;
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

        [Fact]
        public void PartyItemActionLabelsReflectAvailableActions()
        {
            var viewModel = new GameMenuViewModel();
            var item = CreateItemInstance("Potion", ItemType.OneUse, Slot.PrimaryHand);

            Assert.Equal(
                new[] { "Use", "Equip", "Transfer", "Drop", "Cancel" },
                viewModel.GetPartyItemActionLabels(true, item, true, true));

            item.IsEquipped = true;
            Assert.Equal(
                new[] { "Unequip", "Drop", "Cancel" },
                viewModel.GetPartyItemActionLabels(false, item, false, false));

            var quest = CreateItemInstance("Gem", ItemType.Quest, Slot.PrimaryHand);
            Assert.Equal(new[] { "Cancel" }, viewModel.GetPartyItemActionLabels(false, quest, false, false));
        }

        [Fact]
        public void ItemAndSpellUseActionsMapSpecialTypesAndTargets()
        {
            var viewModel = new GameMenuViewModel();
            var outsideItem = CreateSkillItem("Wings", SkillType.Outside, Target.None);
            var returnSpell = CreateSpell("Return", SkillType.Return, Target.None);
            var groupSpell = CreateSpell("Heal All", SkillType.Heal, Target.Group);
            var singleItem = CreateItemInstance("Potion", ItemType.OneUse, Slot.PrimaryHand);
            singleItem.Item.Target = Target.Single;

            Assert.Equal(GameMenuUseAction.Outside, viewModel.GetUseItemAction(outsideItem));
            Assert.Equal(GameMenuUseAction.Single, viewModel.GetUseItemAction(singleItem));
            Assert.Equal(GameMenuUseAction.Return, viewModel.GetCastSpellAction(returnSpell));
            Assert.Equal(GameMenuUseAction.Group, viewModel.GetCastSpellAction(groupSpell));
        }

        [Fact]
        public void CanCastSpellFromPartyMenuAppliesOutsideAndReturnRules()
        {
            var viewModel = new GameMenuViewModel();

            Assert.False(viewModel.CanCastSpellFromPartyMenu(false, SkillType.Heal, true, true));
            Assert.False(viewModel.CanCastSpellFromPartyMenu(true, SkillType.Outside, false, true));
            Assert.False(viewModel.CanCastSpellFromPartyMenu(true, SkillType.Return, true, false));
            Assert.True(viewModel.CanCastSpellFromPartyMenu(true, SkillType.Heal, false, false));
        }

        [Fact]
        public void AdjustSelectedSettingMutatesSettingsAndReturnsEffect()
        {
            var viewModel = new GameMenuViewModel();
            var settings = new Settings { UiScale = 1f, MusicVolume = 0.5f, UiBorderThickness = 3 };

            Assert.Equal(GameMenuSettingsEffect.ApplySettings, viewModel.AdjustSelectedSetting(settings, GameMenuViewModel.SettingsGeneral, 1, 1));
            Assert.Equal(1.05f, settings.UiScale, 2);

            Assert.Equal(GameMenuSettingsEffect.ApplyAudioSettings, viewModel.AdjustSelectedSetting(settings, GameMenuViewModel.SettingsGeneral, 4, -1));
            Assert.Equal(0.49f, settings.MusicVolume, 2);

            Assert.Equal(GameMenuSettingsEffect.ApplySettings, viewModel.AdjustSelectedSetting(settings, GameMenuViewModel.SettingsUi, 6, 3));
            Assert.Equal(6, settings.UiBorderThickness);
        }

        [Fact]
        public void ActivateSelectedSettingTogglesSettingsAndInputActions()
        {
            var viewModel = new GameMenuViewModel();
            var settings = new Settings();

            Assert.Equal(GameMenuSettingsEffect.ApplySettings, viewModel.ActivateSelectedSetting(settings, GameMenuViewModel.SettingsGeneral, 3, 2));
            Assert.True(settings.IsFullScreen);

            Assert.Equal(GameMenuSettingsEffect.ApplySettingsAndRefreshVisibility, viewModel.ActivateSelectedSetting(settings, GameMenuViewModel.SettingsDebug, 2, 2));
            Assert.True(settings.ShowHiddenObjects);

            Assert.Equal(GameMenuSettingsEffect.StartRebinding, viewModel.ActivateSelectedSetting(settings, GameMenuViewModel.SettingsInput, 1, 2));
            Assert.Equal(GameMenuSettingsEffect.ResetBindings, viewModel.ActivateSelectedSetting(settings, GameMenuViewModel.SettingsInput, 3, 2));
        }

        [Fact]
        public void ModalStateStoresDisplayDataAndWaitState()
        {
            var viewModel = new GameMenuViewModel();
            var hero = CreateHero("Able", true, 0);

            viewModel.ShowModal("Title", "Message", new[] { "Yes", "No" }, new[] { hero, null }, true);

            Assert.True(viewModel.IsModalVisible());
            Assert.True(viewModel.ModalHasChoices());
            Assert.Equal("Title", viewModel.ModalTitle);
            Assert.Equal("Message", viewModel.ModalMessage);
            Assert.Equal(new[] { "Yes", "No" }, viewModel.ModalChoices);
            Assert.Same(hero, viewModel.GetModalChoiceHero(0));
            Assert.True(viewModel.ModalWaitingForConfirmRelease);

            viewModel.ReleaseModalConfirmWait();

            Assert.False(viewModel.ModalWaitingForConfirmRelease);
        }

        [Fact]
        public void ModalSelectionMovesAndClamps()
        {
            var viewModel = new GameMenuViewModel();
            viewModel.ShowModal("Title", "Message", new[] { "One", "Two", "Three" }, null, false);

            Assert.Equal(1, viewModel.MoveModalSelection(1));
            Assert.Equal(2, viewModel.MoveModalSelection(5));
            Assert.Equal(0, viewModel.MoveModalSelection(-5));
        }

        [Fact]
        public void ModalSelectionReturnsChoiceAndHidesModal()
        {
            var viewModel = new GameMenuViewModel();
            viewModel.ShowModal("Title", "Message", new[] { "One", "Two" }, null, false);

            int selected;
            Assert.True(viewModel.TrySelectModalChoice(1, out selected));

            Assert.Equal(1, selected);
            Assert.False(viewModel.IsModalVisible());
            Assert.Null(viewModel.ModalChoices);
        }

        [Fact]
        public void GeneralSettingsChangeEffectDistinguishesAudioFromSavedSettings()
        {
            var viewModel = new GameMenuViewModel();
            var settings = new Settings { UiScale = 1f, MusicVolume = 0.5f, SoundEffectsVolume = 0.5f, DialogTextCharactersPerSecond = 60f, AutoSaveEnabled = true, AutoSaveIntervalSeconds = 5f };

            Assert.Equal(
                GameMenuSettingsEffect.ApplyAudioSettings,
                viewModel.GetGeneralSettingsChangeEffect(settings, 1f, 60f, false, 0.4f, 0.5f, true, 5f));
            Assert.Equal(
                GameMenuSettingsEffect.ApplySettings,
                viewModel.GetGeneralSettingsChangeEffect(settings, 1.1f, 60f, false, 0.5f, 0.5f, true, 5f));
        }

        [Fact]
        public void UiAndDebugSettingsChangeEffectsDetectChangedFields()
        {
            var viewModel = new GameMenuViewModel();
            var oldSettings = new Settings { UiBackgroundColor = "#000000", UiBackgroundAlpha = 1f, UiBorderThickness = 3, ShowHiddenObjects = false, NoMonsters = false, SprintBoost = 1.5f, TurnMoveDelaySeconds = 0.1f };
            var newUiSettings = new Settings { UiBackgroundColor = "#111111", UiBackgroundAlpha = 1f, UiBorderThickness = 3 };

            Assert.Equal(GameMenuSettingsEffect.ApplySettings, viewModel.GetUiSettingsChangeEffect(oldSettings, newUiSettings));
            Assert.Equal(
                GameMenuSettingsEffect.ApplySettingsAndRefreshVisibility,
                viewModel.GetDebugSettingsChangeEffect(oldSettings, false, true, false, 1.5f, 0.1f));
            Assert.Equal(
                GameMenuSettingsEffect.ApplySettings,
                viewModel.GetDebugSettingsChangeEffect(oldSettings, false, false, true, 1.5f, 0.1f));
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

        private static ItemInstance CreateSkillItem(string name, SkillType skillType, Target target)
        {
            return new ItemInstance(new Item
            {
                Id = name,
                Name = name,
                Type = ItemType.OneUse,
                Target = target,
                Slots = new List<Slot> { Slot.PrimaryHand },
                Classes = new List<Class> { Class.Hero },
                Skill = new Skill { Name = name, Type = skillType }
            });
        }

        private static Spell CreateSpell(string name, SkillType skillType, Target target)
        {
            var skill = new Skill { Name = name + "Skill", Type = skillType, Targets = target };
            var spell = new Spell { Name = name, SkillId = skill.Name };
            spell.Setup(new[] { skill });
            return spell;
        }
    }
}
