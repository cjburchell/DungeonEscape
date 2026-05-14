using Redpoint.DungeonEscape.ViewModels;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void ActionRowsIncludeAvailableActionsAndSkills()
        {
            var viewModel = new CombatViewModel();
            var hero = new Hero { Name = "Able" };
            var skills = new List<Skill> { new Skill { Name = "Focus" } };

            var rows = viewModel.GetActionRows(hero, true, skills, true);

            Assert.Equal(new[] { "Fight", "Spell", "Focus", "Item", "Run" }, rows.Select(row => row.Label).ToArray());
            Assert.Equal(CombatActionKind.Skill, rows[2].Kind);
            Assert.Equal(0, rows[2].SkillIndex);
        }

        [Fact]
        public void ActionRowsHideSpellWhenHeroIsStopped()
        {
            var viewModel = new CombatViewModel();
            var hero = new Hero { Name = "Able" };
            hero.Status.Add(new StatusEffect { Type = EffectType.StopSpell });

            var rows = viewModel.GetActionRows(hero, true, new List<Skill>(), false);

            Assert.Equal(new[] { "Fight", "Run" }, rows.Select(row => row.Label).ToArray());
        }

        [Fact]
        public void SpellAndItemRowsBuildDisplayLabels()
        {
            var viewModel = new CombatViewModel();
            var spell = new Spell { Name = "Heal", Cost = 4 };
            var item = new ItemInstance(new Item { Name = "Potion", Type = ItemType.OneUse, Slots = new List<Slot>() });

            Assert.Equal("Heal  4 MP", Assert.Single(viewModel.GetSpellRows(new[] { spell })).Label);
            Assert.StartsWith("Potion", Assert.Single(viewModel.GetItemRows(new[] { item })).Label);
        }

        [Fact]
        public void TargetHelpersResolveSelectionAndCandidateTypes()
        {
            var viewModel = new CombatViewModel();
            var hero = new Hero { Name = "Able" };
            var monster = new MonsterInstance(new Monster { Name = "Slime", HealthConst = 1 }, null);
            var candidates = new List<IFighter> { hero, monster };

            viewModel.SetSelectedMenuIndex(1);

            Assert.Same(monster, viewModel.GetSelectedTarget(candidates));
            Assert.True(viewModel.IsTargetCandidate(candidates, hero));
            Assert.Equal(1, viewModel.GetTargetIndex(candidates, monster));
            Assert.True(viewModel.HasPartyTargets(candidates));
            Assert.True(viewModel.HasMonsterTargets(candidates));
            Assert.False(viewModel.IsTargetCandidate(candidates, new Hero { Name = "Other" }));
        }
    }
}
