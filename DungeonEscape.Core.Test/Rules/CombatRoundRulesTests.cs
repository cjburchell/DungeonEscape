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
        public void ChooseMonsterActionKeepsFightingWhenMonsterIsNotIncapacitated()
        {
            var monster = CreateMonster("Slime");
            monster.Status.Add(new StatusEffect { Type = EffectType.Poisoned });

            var action = CombatRoundRules.ChooseMonsterAction(
                monster,
                new[] { CreateHero("Hero") },
                new[] { monster },
                null,
                max => 0,
                () => 1);

            Assert.Equal(CombatRoundActionState.Fight, action.State);
            Assert.Same(monster, action.Source);
        }

        [Fact]
        public void ChooseMonsterActionIgnoresLegacyMonsterSpells()
        {
            var monster = CreateMonster("Caster", new[] { "Heal" });
            monster.Health = 5;
            monster.MaxHealth = 100;
            var heal = CreateSpell("Heal", SkillType.Heal);

            var action = CombatRoundRules.ChooseMonsterAction(
                monster,
                new[] { CreateHero("Hero") },
                new[] { monster },
                new[] { heal },
                max => 0,
                () => 1);

            Assert.Equal(CombatRoundActionState.Fight, action.State);
            Assert.Null(action.Spell);
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
        public void SelectNextResolvableActionChoosesHighestInitiativeResolvableAction()
        {
            var slow = CreateHero("Slow", agility: 1);
            var fast = CreateHero("Fast", agility: 9);
            var target = CreateMonster("Target");
            var actions = new[]
            {
                new CombatRoundAction { Source = slow, State = CombatRoundActionState.Fight, InitiativeTotal = 20, Targets = new List<IFighter> { target } },
                new CombatRoundAction { Source = fast, State = CombatRoundActionState.Fight, InitiativeTotal = 10, Targets = new List<IFighter> { target } }
            };

            var selected = CombatRoundRules.SelectNextResolvableAction(actions, source => new List<IFighter> { target });

            Assert.Same(slow, selected.Source);
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
        public void SkillTypeIncludesDndMigrationNames()
        {
            var names = new HashSet<string>(System.Enum.GetNames(typeof(SkillType)));

            Assert.Contains(nameof(SkillType.Disengage), names);
            Assert.Contains(nameof(SkillType.RemoveCondition), names);
            Assert.Contains(nameof(SkillType.AbilityModifier), names);
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
                (selectedAction, target) => selectedAction.Source.Name + " hits " + target.Name + ".",
                null,
                null,
                null,
                null,
                source => new List<IFighter> { fallback },
                out endFight);

            Assert.False(endFight);
            Assert.Equal("Hero hits Fallback.", message);
        }

        [Fact]
        public void ChooseMonsterActionUsesDndActionsInsteadOfLegacySkills()
        {
            var bite = new MonsterAction
            {
                Name = "Bite",
                AttackBonus = 4,
                DamageDice = 1,
                DamageDie = 6,
                DamageBonus = 2
            };
            var monster = CreateMonster("Wolf", null, new[] { bite });

            var action = CombatRoundRules.ChooseMonsterAction(
                monster,
                new[] { CreateHero("Hero") },
                new[] { monster },
                null,
                max => 0,
                () => 100);

            Assert.Equal(CombatRoundActionState.MonsterAction, action.State);
            Assert.Same(bite, action.MonsterAction);
            Assert.Null(action.Skill);
        }

        [Fact]
        public void ChooseMonsterActionSeparatesNormalAndBonusActions()
        {
            var bite = new MonsterAction { Name = "Bite", AttackBonus = 4 };
            var pounce = new MonsterAction { Name = "Pounce", AttackBonus = 4, IsBonusAction = true };
            var monster = CreateMonster("Wolf", null, new[] { bite, pounce });

            var action = CombatRoundRules.ChooseMonsterAction(
                monster,
                new[] { CreateHero("Hero") },
                new[] { monster },
                null,
                max => 0,
                () => 100);
            var bonusAction = CombatRoundRules.ChooseMonsterBonusAction(
                monster,
                new[] { CreateHero("Hero") },
                new[] { monster },
                null,
                max => 0,
                () => 100);

            Assert.Same(bite, action.MonsterAction);
            Assert.Same(pounce, bonusAction.MonsterAction);
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

        private static IFighter CreateMonster(
            string name,
            IEnumerable<string> spells = null,
            IEnumerable<MonsterAction> actions = null)
        {
            return new MonsterInstance(
                new Monster
                {
                    Name = name,
                    HitPoints = 10,
                    MagicConst = 10,
                    MagicTimes = 1,
                    Dexterity = 13,
                    SpellList = spells == null ? new List<string>() : spells.ToList(),
                    Actions = actions == null ? new List<MonsterAction>() : actions.ToList()
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
