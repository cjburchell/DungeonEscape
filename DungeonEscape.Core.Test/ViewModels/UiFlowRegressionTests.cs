using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape;
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

            var potion = new ItemInstance
            {
                Name = "Potion",
                Type = ItemType.OneUse,
                Slot = Slot.PrimaryHand,
                Item = new Data.Item { Target = Target.Single }
            };
            hero.Items.Add(potion);

            var wings = new ItemInstance
            {
                Name = "Wings",
                Type = ItemType.OneUse,
                Slot = Slot.PrimaryHand,
                Item = new Data.Item { SkillType = SkillType.Outside, Target = Target.None }
            };

            Assert.Equal(GameMenuUseAction.Single, vm.GetUseItemAction(potion));
            Assert.Equal(GameMenuUseAction.Outside, vm.GetUseItemAction(wings));
            Assert.True(vm.CanCastSpellFromPartyMenu(true, SkillType.Heal, false, false));
            Assert.False(vm.CanCastSpellFromPartyMenu(false, SkillType.Heal, false, false));
        }

        [Fact]
        public void CombatFlow_ActionAndTargetSelectionStayInRange()
        {
            var vm = new CombatViewModel();
            vm.SetMenuIndex(0);
            vm.SetActionIndex(0);
            vm.MoveActionIndex(1, 3);
            Assert.Equal(1, vm.ActionIndex);

            vm.SetTargetIndex(0);
            vm.MoveTargetIndex(1, 2);
            Assert.Equal(1, vm.TargetIndex);
            vm.MoveTargetIndex(1, 2);
            Assert.Equal(1, vm.TargetIndex);
        }

        [Fact]
        public void StoreAndHealerFlows_ClampSelectionAndPreserveValidRows()
        {
            var storeVm = new StoreViewModel();
            storeVm.SetSelectedTab(StoreTab.Buy);
            storeVm.SetSelectedIndex(999);
            Assert.Equal(1, storeVm.ClampSelectedTab());
            Assert.True(storeVm.ClampSelectedIndex(4) <= 3);

            var healerVm = new HealerViewModel();
            healerVm.SetSelectedIndex(10);
            healerVm.SetSelectedServiceIndex(5);
            healerVm.ClampSelectedIndex(2);
            healerVm.ClampSelectedServiceIndex(2);
            Assert.Equal(1, healerVm.SelectedIndex);
            Assert.Equal(1, healerVm.SelectedServiceIndex);
        }
    }
}
