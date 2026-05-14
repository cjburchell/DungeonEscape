using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class CombatRoundRulesTests
    {
        [Fact]
        public void ChooseMonsterActionReturnsNothingWhenMonsterIsAsleep()
        {
            var monster = CreateMonster("Slime");
            monster.Status.Add(new StatusEffect { Type = EffectType.Sleep });

            var action = CombatRoundRules.ChooseMonsterAction(
                monster,
                new[] { CreateHero("Hero") },
                new[] { monster },
                null,
                null,
                max => 0,
                () => 1);

            Assert.Equal(CombatRoundActionState.Nothing, action.State);
            Assert.Same(monster, action.Source);
        }

        [Fact]
        public void ChooseMonsterActionUsesHealSpellWhenMonsterHealthIsLow()
        {
            var monster = CreateMonster("Caster", new[] { "Heal" });
            monster.Health = 5;
            monster.MaxHealth = 100;
            monster.Magic = 10;
            var heal = CreateSpell("Heal", SkillType.Heal);

            var action = CombatRoundRules.ChooseMonsterAction(
                monster,
                new[] { CreateHero("Hero") },
                new[] { monster },
                new[] { heal },
                null,
                max => 0,
                () => 1);

            Assert.Equal(CombatRoundActionState.Spell, action.State);
            Assert.Same(heal, action.Spell);
            Assert.Same(action.Source, action.Targets.Single());
        }

        [Fact]
        public void ResolveActionTargetsFallsBackForOffensiveActionWhenOriginalTargetIsDead()
        {
            var hero = CreateHero("Hero");
            var deadMonster = CreateMonster("Dead");
            deadMonster.Health = 0;
            var fallback = CreateMonster("Fallback");
            var action = new CombatRoundAction
            {
                Source = hero,
                State = CombatRoundActionState.Fight,
                Targets = new List<IFighter> { deadMonster }
            };

            var targets = CombatRoundRules.ResolveActionTargets(action, source => new List<IFighter> { fallback });

            Assert.Equal(new[] { fallback }, targets);
        }

        [Fact]
        public void SelectNextResolvableActionChoosesHighestAgilityResolvableAction()
        {
            var slow = CreateHero("Slow", agility: 1);
            var fast = CreateHero("Fast", agility: 9);
            var target = CreateMonster("Target");
            var actions = new[]
            {
                new CombatRoundAction { Source = slow, State = CombatRoundActionState.Fight, Targets = new List<IFighter> { target } },
                new CombatRoundAction { Source = fast, State = CombatRoundActionState.Fight, Targets = new List<IFighter> { target } }
            };

            var selected = CombatRoundRules.SelectNextResolvableAction(actions, source => new List<IFighter> { target });

            Assert.Same(fast, selected.Source);
        }

        [Fact]
        public void RunWithHeroAndNoTargetsEndsFight()
        {
            var hero = CreateHero("Hero");

            var result = CombatRoundRules.Run(hero, new List<IFighter>());

            Assert.True(result.Succeeded);
            Assert.True(result.EndFight);
            Assert.Equal("Hero tried to run.\nAnd got away.", result.Message);
        }

        [Fact]
        public void RunWithMonsterMarksMonsterAsRanAway()
        {
            var monster = CreateMonster("Slime");

            var result = CombatRoundRules.Run(monster, new List<IFighter>());

            Assert.True(result.Succeeded);
            Assert.False(result.EndFight);
            Assert.True(monster.RanAway);
        }

        [Fact]
        public void ReviveSpellTargetsDeadPartyMembers()
        {
            var alive = CreateHero("Alive");
            var dead = CreateHero("Dead");
            dead.Health = 0;
            var revive = CreateSpell("Revive", SkillType.Revive);

            var targets = CombatRoundRules.GetPartySpellTargets(revive, new[] { alive }, new[] { dead });

            Assert.Equal(new[] { dead }, targets);
        }

        [Fact]
        public void ExecuteRoundActionDispatchesFightDelegateWithResolvedFallbackTarget()
        {
            var hero = CreateHero("Hero");
            var deadTarget = CreateMonster("Dead");
            deadTarget.Health = 0;
            var fallback = CreateMonster("Fallback");
            var action = new CombatRoundAction
            {
                Source = hero,
                State = CombatRoundActionState.Fight,
                Targets = new List<IFighter> { deadTarget }
            };

            bool endFight;
            var message = CombatRoundRules.ExecuteRoundAction(
                action,
                null,
                1,
                null,
                (source, target) => source.Name + " hits " + target.Name + ".",
                null,
                null,
                null,
                source => new List<IFighter> { fallback },
                out endFight);

            Assert.False(endFight);
            Assert.Equal("Hero hits Fallback.", message);
        }

        private static Hero CreateHero(string name, int agility = 5)
        {
            return new Hero
            {
                Name = name,
                IsActive = true,
                Health = 10,
                MaxHealth = 10,
                Agility = agility,
                Items = new List<ItemInstance>()
            };
        }

        private static IFighter CreateMonster(string name, IEnumerable<string> spells = null)
        {
            return new MonsterInstance(
                new Monster
                {
                    Name = name,
                    HealthConst = 10,
                    HealthTimes = 1,
                    MagicConst = 10,
                    MagicTimes = 1,
                    Agility = 3,
                    SpellList = spells == null ? new List<string>() : spells.ToList()
                },
                null);
        }

        private static Spell CreateSpell(string name, SkillType type)
        {
            var skill = new Skill { Name = name, Type = type, Targets = Target.Single, MaxTargets = 1 };
            var spell = new Spell { Name = name, SkillId = name };
            spell.Setup(new[] { skill });
            return spell;
        }
    }
}
