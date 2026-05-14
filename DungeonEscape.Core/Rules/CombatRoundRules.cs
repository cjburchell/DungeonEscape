using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class CombatRoundRules
    {
        public static CombatRoundAction ChooseMonsterAction(
            IFighter monster,
            IEnumerable<IFighter> aliveHeroes,
            IEnumerable<IFighter> aliveMonsters,
            IEnumerable<Spell> spells,
            IEnumerable<Skill> skills,
            Func<int, int> nextInt,
            Func<int> rollD100)
        {
            if (monster == null)
            {
                return null;
            }

            if (HasStatus(monster, EffectType.Sleep))
            {
                return new CombatRoundAction
                {
                    Source = monster,
                    State = CombatRoundActionState.Nothing
                };
            }

            var availableTargets = (aliveHeroes ?? new List<IFighter>()).Where(CanBeAttacked).ToList();
            if (HasStatus(monster, EffectType.Confusion))
            {
                availableTargets.AddRange((aliveMonsters ?? new List<IFighter>()).Where(CanBeAttacked));
                availableTargets.Remove(monster);
            }

            if (availableTargets.Count == 0)
            {
                return new CombatRoundAction
                {
                    Source = monster,
                    State = CombatRoundActionState.Nothing
                };
            }

            var availableSpells = (spells == null
                    ? new List<Spell>()
                    : monster.GetSpells(spells)
                        .Where(spell => spell != null && spell.IsEncounterSpell && spell.Cost <= monster.Magic))
                .ToList();
            if (!HasStatus(monster, EffectType.StopSpell) && availableSpells.Count > 0)
            {
                var lowHealthHealSpells = availableSpells
                    .Where(spell => spell.Type == SkillType.Heal && monster.MaxHealth > 0 && (float)monster.Health / monster.MaxHealth < 0.1f)
                    .ToList();
                if (lowHealthHealSpells.Count > 0)
                {
                    return new CombatRoundAction
                    {
                        Source = monster,
                        State = CombatRoundActionState.Spell,
                        Spell = lowHealthHealSpells[Next(nextInt, lowHealthHealSpells.Count)],
                        Targets = new List<IFighter> { monster }
                    };
                }

                var attackSpells = availableSpells.Where(spell => spell.IsAttackSpell).ToList();
                if (attackSpells.Count > 0)
                {
                    var spell = attackSpells[Next(nextInt, attackSpells.Count)];
                    return new CombatRoundAction
                    {
                        Source = monster,
                        State = CombatRoundActionState.Spell,
                        Spell = spell,
                        Targets = GetTargets(spell.Targets, spell.MaxTargets, availableTargets, rollD100)
                    };
                }
            }

            var availableSkills = (skills == null
                    ? new List<Skill>()
                    : monster.GetSkills(skills).Where(skill => skill != null && skill.IsEncounterSkill))
                .ToList();
            if (availableSkills.Count > 0 && (rollD100 == null ? 100 : rollD100()) > 75)
            {
                var skill = availableSkills[Next(nextInt, availableSkills.Count)];
                if (skill.Type == SkillType.Flee)
                {
                    return new CombatRoundAction
                    {
                        Source = monster,
                        State = CombatRoundActionState.Run,
                        Targets = availableTargets
                    };
                }

                return new CombatRoundAction
                {
                    Source = monster,
                    State = CombatRoundActionState.Skill,
                    Skill = skill,
                    Targets = GetTargets(skill.Targets, skill.MaxTargets, availableTargets, rollD100)
                };
            }

            return new CombatRoundAction
            {
                Source = monster,
                State = CombatRoundActionState.Fight,
                Targets = new List<IFighter> { ChooseFighter(availableTargets, rollD100) }
            };
        }

        public static CombatRoundAction ChooseConfusedHeroAction(
            IFighter hero,
            IEnumerable<IFighter> aliveHeroes,
            IEnumerable<IFighter> aliveMonsters,
            Func<int, int> nextInt)
        {
            var confusedTargets = (aliveMonsters ?? new List<IFighter>())
                .Concat(aliveHeroes ?? new List<IFighter>())
                .Where(target => target != null && target != hero)
                .ToList();
            if (confusedTargets.Count == 0)
            {
                return null;
            }

            return new CombatRoundAction
            {
                Source = hero,
                State = CombatRoundActionState.Fight,
                Targets = new List<IFighter> { confusedTargets[Next(nextInt, confusedTargets.Count)] }
            };
        }

        public static CombatRoundAction SelectNextResolvableAction(
            IEnumerable<CombatRoundAction> actions,
            Func<IFighter, List<IFighter>> getOpposingTargets)
        {
            return (actions ?? new List<CombatRoundAction>())
                .Where(action => IsActionResolvable(action, getOpposingTargets))
                .OrderByDescending(item => item.Source.Agility)
                .FirstOrDefault();
        }

        public static bool IsActionResolvable(
            CombatRoundAction action,
            Func<IFighter, List<IFighter>> getOpposingTargets)
        {
            if (action == null || action.Source == null || !CanBeAttacked(action.Source))
            {
                return false;
            }

            if (action.State == CombatRoundActionState.Nothing || action.Targets == null)
            {
                return true;
            }

            if (IsReviveAction(action))
            {
                return action.Targets.Any(target => target != null && target.IsDead);
            }

            if (IsOffensiveAction(action))
            {
                return ResolveActionTargets(action, getOpposingTargets).Any();
            }

            return action.Targets.Any(CanBeAttacked);
        }

        public static string ExecuteRoundAction(
            CombatRoundAction action,
            IGame game,
            int round,
            Func<CombatRoundAction, CombatRunResult> run,
            Func<IFighter, IFighter, string> fight,
            Func<Spell, List<IFighter>, IFighter, string> castSpell,
            Func<ItemInstance, List<IFighter>, IFighter, string> useItem,
            Func<Skill, List<IFighter>, IFighter, string> doSkill,
            Func<IFighter, List<IFighter>> getOpposingTargets,
            out bool endFight)
        {
            endFight = false;
            if (action == null || action.Source == null)
            {
                return "";
            }

            var message = action.Source.CheckForExpiredStates(round, DurationType.Rounds);
            message += action.Source.UpdateStatusEffects(game);
            if (action.Source.IsDead)
            {
                return string.IsNullOrEmpty(message) ? action.Source.Name + " cannot act." : message.TrimEnd();
            }

            if (action.State != CombatRoundActionState.Nothing && HasStatus(action.Source, EffectType.Sleep))
            {
                message += action.Source.Name + " is asleep.";
                return message.TrimEnd();
            }

            switch (action.State)
            {
                case CombatRoundActionState.Run:
                    var runResult = run == null ? Run(action.Source, action.Targets) : run(action);
                    if (runResult != null)
                    {
                        endFight = runResult.EndFight;
                        message += runResult.Message;
                    }
                    break;
                case CombatRoundActionState.Fight:
                    message += fight == null ? "" : fight(action.Source, ResolveActionTargets(action, getOpposingTargets).FirstOrDefault());
                    break;
                case CombatRoundActionState.Spell:
                    message += castSpell == null ? "" : castSpell(action.Spell, ResolveActionTargets(action, getOpposingTargets), action.Source);
                    break;
                case CombatRoundActionState.Item:
                    message += useItem == null ? "" : useItem(action.Item, ResolveActionTargets(action, getOpposingTargets), action.Source);
                    break;
                case CombatRoundActionState.Nothing:
                    message += action.Source.Name + " doesn't do anything.";
                    break;
                case CombatRoundActionState.Skill:
                    message += doSkill == null ? "" : doSkill(action.Skill, ResolveActionTargets(action, getOpposingTargets), action.Source);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return string.IsNullOrEmpty(message) ? "" : message.TrimEnd();
        }

        public static CombatRunResult Run(IFighter source, IEnumerable<IFighter> targets)
        {
            var result = new CombatRunResult
            {
                Message = source == null ? "" : source.Name + " tried to run.\n"
            };
            if (source == null)
            {
                return result;
            }

            var fastestTarget = targets == null ? null : targets.Where(CanBeAttacked).OrderByDescending(target => target.Agility).FirstOrDefault();
            if (fastestTarget != null && !source.CanHit(fastestTarget))
            {
                result.Message += "But could not get away.";
                return result;
            }

            result.Succeeded = true;
            result.Message += "And got away.";
            if (source is Hero)
            {
                result.EndFight = true;
                return result;
            }

            var fighter = source as Fighter;
            if (fighter != null)
            {
                fighter.RanAway = true;
            }

            return result;
        }

        public static List<IFighter> ResolveActionTargets(
            CombatRoundAction action,
            Func<IFighter, List<IFighter>> getOpposingTargets)
        {
            if (action == null)
            {
                return new List<IFighter>();
            }

            if (IsReviveAction(action))
            {
                return action.Targets == null
                    ? new List<IFighter>()
                    : action.Targets.Where(target => target != null && target.IsDead).ToList();
            }

            var targets = action.Targets == null
                ? new List<IFighter>()
                : action.Targets.Where(CanBeAttacked).ToList();
            if (targets.Count > 0 || !IsOffensiveAction(action))
            {
                return targets;
            }

            return GetFallbackTargets(action, getOpposingTargets);
        }

        public static List<IFighter> GetFallbackTargets(
            CombatRoundAction action,
            Func<IFighter, List<IFighter>> getOpposingTargets)
        {
            var candidates = getOpposingTargets == null ? new List<IFighter>() : getOpposingTargets(action.Source);
            if (candidates.Count == 0)
            {
                return candidates;
            }

            var targetMode = GetActionTargetMode(action);
            var maxTargets = GetActionMaxTargets(action);
            if (targetMode == Target.Group)
            {
                return maxTargets > 0 ? candidates.Take(maxTargets).ToList() : candidates;
            }

            return new List<IFighter> { candidates[0] };
        }

        public static Target GetActionTargetMode(CombatRoundAction action)
        {
            switch (action.State)
            {
                case CombatRoundActionState.Spell:
                    return action.Spell == null ? Target.Single : action.Spell.Targets;
                case CombatRoundActionState.Skill:
                    return action.Skill == null ? Target.Single : action.Skill.Targets;
                case CombatRoundActionState.Item:
                    return action.Item == null ? Target.Single : action.Item.Target;
                default:
                    return Target.Single;
            }
        }

        public static int GetActionMaxTargets(CombatRoundAction action)
        {
            switch (action.State)
            {
                case CombatRoundActionState.Spell:
                    return action.Spell == null ? 1 : action.Spell.MaxTargets;
                case CombatRoundActionState.Skill:
                    return action.Skill == null ? 1 : action.Skill.MaxTargets;
                case CombatRoundActionState.Item:
                    return action.Item == null || action.Item.Item == null || action.Item.Item.Skill == null
                        ? 1
                        : action.Item.Item.Skill.MaxTargets;
                default:
                    return 1;
            }
        }

        public static bool IsReviveAction(CombatRoundAction action)
        {
            return action != null &&
                   ((action.State == CombatRoundActionState.Spell &&
                     action.Spell != null &&
                     action.Spell.Type == SkillType.Revive) ||
                    (action.State == CombatRoundActionState.Skill &&
                     action.Skill != null &&
                     action.Skill.Type == SkillType.Revive) ||
                    (action.State == CombatRoundActionState.Item &&
                     action.Item != null &&
                     action.Item.Item != null &&
                     action.Item.Item.Skill != null &&
                     action.Item.Item.Skill.Type == SkillType.Revive));
        }

        public static bool IsOffensiveAction(CombatRoundAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.State)
            {
                case CombatRoundActionState.Fight:
                    return true;
                case CombatRoundActionState.Spell:
                    return action.Spell != null && action.Spell.IsAttackSpell;
                case CombatRoundActionState.Skill:
                    return action.Skill != null && (action.Skill.IsAttackSkill || action.Skill.DoAttack);
                case CombatRoundActionState.Item:
                    return action.Item != null &&
                           action.Item.Item != null &&
                           action.Item.Item.Skill != null &&
                           (action.Item.Item.Skill.IsAttackSkill || action.Item.Item.Skill.DoAttack);
                default:
                    return false;
            }
        }

        public static IFighter ChooseFighter(IReadOnlyCollection<IFighter> availableTargets, Func<int> rollD100)
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                return null;
            }

            if (availableTargets.Count == 1)
            {
                return availableTargets.First();
            }

            var roll = rollD100 == null ? 1 : rollD100();
            var totalHealth = Math.Max(1, availableTargets.Sum(target => Math.Max(1, target.MaxHealth)));
            var maxRoll = 0f;
            foreach (var target in availableTargets.OrderByDescending(target => target.MaxHealth))
            {
                maxRoll += (float)Math.Max(1, target.MaxHealth) / totalHealth * 100f;
                if (roll <= maxRoll)
                {
                    return target;
                }
            }

            return availableTargets.First();
        }

        public static List<IFighter> GetTargets(
            Target targetType,
            int maxTargets,
            IReadOnlyCollection<IFighter> availableTargets,
            Func<int> rollD100)
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                return new List<IFighter>();
            }

            if (targetType != Target.Group)
            {
                return new List<IFighter> { ChooseFighter(availableTargets, rollD100) };
            }

            if (maxTargets == 0)
            {
                return availableTargets.ToList();
            }

            var targets = new List<IFighter>();
            for (var i = 0; i < maxTargets; i++)
            {
                var target = ChooseFighter(availableTargets, rollD100);
                if (target != null)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        public static List<IFighter> GetPartySpellTargets(Spell spell, IEnumerable<Hero> aliveHeroes, IEnumerable<Hero> deadHeroes)
        {
            return spell != null && spell.Type == SkillType.Revive
                ? (deadHeroes ?? new List<Hero>()).Cast<IFighter>().ToList()
                : (aliveHeroes ?? new List<Hero>()).Cast<IFighter>().ToList();
        }

        public static List<IFighter> GetPartySkillTargets(Skill skill, IEnumerable<Hero> aliveHeroes, IEnumerable<Hero> deadHeroes)
        {
            return skill != null && skill.Type == SkillType.Revive
                ? (deadHeroes ?? new List<Hero>()).Cast<IFighter>().ToList()
                : (aliveHeroes ?? new List<Hero>()).Cast<IFighter>().ToList();
        }

        public static bool CanBeAttacked(IFighter fighter)
        {
            return fighter != null && !fighter.IsDead && !fighter.RanAway;
        }

        private static bool HasStatus(IFighter fighter, EffectType effectType)
        {
            return fighter != null && fighter.Status != null && fighter.Status.Any(effect => effect.Type == effectType);
        }

        private static int Next(Func<int, int> nextInt, int maxValue)
        {
            return maxValue <= 1 || nextInt == null ? 0 : nextInt(maxValue);
        }
    }
}
