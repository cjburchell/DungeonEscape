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
        public void AvailableEncounterSpellsRespectHeroStateLevelClassAndMagic()
        {
            var viewModel = new CombatViewModel();
            var encounterSkill = new Skill { Name = "HealSkill", Type = SkillType.Heal };
            var fieldSkill = new Skill { Name = "OutsideSkill", Type = SkillType.Outside };
            var spells = new List<Spell>
            {
                CreateSpell("Heal", encounterSkill, 3, 1, Class.Hero),
                CreateSpell("Too Much", encounterSkill, 8, 1, Class.Hero),
                CreateSpell("Too High", encounterSkill, 3, 3, Class.Hero),
                CreateSpell("Wrong Class", encounterSkill, 3, 1, Class.Wizard),
                CreateSpell("Outside", fieldSkill, 3, 1, Class.Hero)
            };
            var hero = new Hero { Class = Class.Hero, Health = 10, Level = 2, Magic = 5 };

            var available = viewModel.GetAvailableEncounterSpells(hero, spells);

            Assert.Equal(new[] { "Heal" }, available.Select(spell => spell.Name).ToArray());
            Assert.Empty(viewModel.GetAvailableEncounterSpells(new Hero { Health = 0 }, spells));
            Assert.Empty(viewModel.GetAvailableEncounterSpells(hero, null));
        }

        [Fact]
        public void AvailableEncounterSkillsRespectHeroStateAndEncounterType()
        {
            var viewModel = new CombatViewModel();
            var heal = new Skill { Name = "Heal", Type = SkillType.Heal };
            var outside = new Skill { Name = "Outside", Type = SkillType.Outside };
            var damage = new Skill { Name = "Damage", Type = SkillType.Damage };
            var hero = new Hero { Health = 10, Skills = new List<string> { "Heal", "Outside", "Missing", "Damage" } };

            var available = viewModel.GetAvailableEncounterSkills(hero, new[] { heal, outside, damage });

            Assert.Equal(new[] { "Heal", "Damage" }, available.Select(skill => skill.Name).ToArray());
            Assert.Empty(viewModel.GetAvailableEncounterSkills(new Hero { Health = 0 }, new[] { heal }));
            Assert.Empty(viewModel.GetAvailableEncounterSkills(hero, null));
        }

        [Fact]
        public void AvailableEncounterItemsRespectSkillAndCharges()
        {
            var viewModel = new CombatViewModel();
            var encounterSkill = new Skill { Name = "BombSkill", Type = SkillType.Damage };
            var fieldSkill = new Skill { Name = "KeySkill", Type = SkillType.Open };
            var usable = new ItemInstance(new Item { Name = "Bomb", Skill = encounterSkill, Charges = 2 });
            var noCharges = new ItemInstance(new Item { Name = "Empty Wand", Skill = encounterSkill, Charges = 1 }) { Charges = 0 };
            var fieldOnly = new ItemInstance(new Item { Name = "Key", Skill = fieldSkill, Charges = 1 });
            var noSkill = new ItemInstance(new Item { Name = "Rock", Charges = 1 });
            var hero = new Hero { Health = 10, Items = new List<ItemInstance> { usable, noCharges, fieldOnly, noSkill, null } };

            var available = viewModel.GetAvailableEncounterItems(hero);

            Assert.Same(usable, Assert.Single(available));
            Assert.True(viewModel.IsAvailableEncounterItem(usable));
            Assert.False(viewModel.IsAvailableEncounterItem(noCharges));
            Assert.False(viewModel.IsAvailableEncounterItem(fieldOnly));
            Assert.False(viewModel.IsAvailableEncounterItem(noSkill));
            Assert.False(viewModel.IsAvailableEncounterItem(null));
            Assert.Empty(viewModel.GetAvailableEncounterItems(new Hero { Health = 0 }));
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

        private static Spell CreateSpell(string name, Skill skill, int cost, int minLevel, Class heroClass)
        {
            var spell = new Spell
            {
                Name = name,
                Cost = cost,
                MinLevel = minLevel,
                Classes = new List<Class> { heroClass },
                SkillId = skill.Name
            };
            spell.Setup(new[] { skill });
            return spell;
        }
    }
}
