using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.ViewModels;
using Xunit;

namespace DungeonEscape.Core.Test.ViewModels
{
    public sealed class UiFlowRegressionTests
    {
        [Fact]
        public void TitleFlow_NewQuestAndLoadMenusExposeExpectedNavigation()
        {
            var vm = new TitleViewModel();

            var mainRows = vm.GetMainRows(true, 2).Select(x => x.Action).ToArray();
            Assert.Equal(new[] { TitleMainAction.Continue, TitleMainAction.NewQuest, TitleMainAction.LoadQuest, TitleMainAction.Quit }, mainRows);

            vm.ShowCreateMenu();
            vm.SetSelectedIndex(TitleViewModel.CreateNameIndex);
            Assert.Equal(TitleViewModel.CreateGenderIndex, vm.GetCreateNavigationIndex(0, 1));

            vm.ShowLoadMenu();
            vm.SetSelectedIndex(TitleViewModel.GetLoadSaveIndex(0));
            Assert.Equal(TitleViewModel.GetLoadDeleteIndex(0), vm.GetLoadNavigationIndex(1, 0, 2));
            Assert.Equal(TitleViewModel.GetLoadSaveIndex(1), vm.GetLoadNavigationIndex(0, 1, 2));
        }

        [Fact]
        public void GameMenuFlow_ItemsAndSpellsSelectExpectedActions()
        {
            var vm = new GameMenuViewModel();
            var hero = new Hero { Name = "Hero", IsActive = true, Health = 10, MaxHealth = 10, Magic = 10, MaxMagic = 10 };

            var potion = new ItemInstance(new Item
            {
                Name = "Potion",
                Type = ItemType.OneUse,
                Slots = new List<Slot> { Slot.PrimaryHand },
                Target = Target.Single
            });
            hero.Items.Add(potion);

            var wings = new ItemInstance(new Item
            {
                Name = "Wings",
                Type = ItemType.OneUse,
                Slots = new List<Slot> { Slot.PrimaryHand },
                Skill = new Skill { Type = SkillType.Outside },
                Target = Target.None
            });

            Assert.Equal(GameMenuUseAction.Single, vm.GetUseItemAction(potion));
            Assert.Equal(GameMenuUseAction.Outside, vm.GetUseItemAction(wings));
            Assert.True(vm.CanCastSpellFromPartyMenu(true, SkillType.Heal, false, false));
            Assert.False(vm.CanCastSpellFromPartyMenu(false, SkillType.Heal, false, false));
        }

        [Fact]
        public void CombatFlow_ActionAndTargetSelectionStayInRange()
        {
            var vm = new CombatViewModel();
            vm.SetSelectedMenuIndex(0);
            vm.MoveSelection(1, 3, false);
            Assert.Equal(1, vm.SelectedMenuIndex);

            vm.SetSelectedMenuIndex(0);
            vm.MoveSelection(1, 2, false);
            Assert.Equal(1, vm.SelectedMenuIndex);
            vm.MoveSelection(1, 2, false);
            Assert.Equal(1, vm.SelectedMenuIndex);
        }

        [Fact]
        public void StoreAndHealerFlows_ClampSelectionAndPreserveValidRows()
        {
            var storeVm = new StoreViewModel();
            storeVm.SetCurrentTab(StoreTab.Buy, true);
            storeVm.SetSelectedBuyIndex(999);
            storeVm.ClampBuySelection(new List<Item> { new Item(), new Item(), new Item(), new Item() });
            Assert.Equal(StoreTab.Buy, storeVm.CurrentTab);
            Assert.Equal(3, storeVm.SelectedBuyIndex);

            var healerVm = new HealerViewModel();
            healerVm.SetSelectedServiceIndex(5);
            healerVm.SetSelectedTargetIndex(5);
            healerVm.ClampServiceSelection(new List<HealerServiceRow> { new HealerServiceRow(), new HealerServiceRow() });
            healerVm.ClampTargetSelection(new HealerServiceRow { Targets = new List<Hero> { new Hero(), new Hero() } });
            Assert.Equal(1, healerVm.SelectedServiceIndex);
            Assert.Equal(1, healerVm.SelectedTargetIndex);
        }
    }
}
