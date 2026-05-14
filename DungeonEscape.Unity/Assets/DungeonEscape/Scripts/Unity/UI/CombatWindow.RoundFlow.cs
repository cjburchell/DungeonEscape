using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class CombatWindow
    {
        private void BeginRound()
        {
            round++;
            actingHero = null;
            roundActions.Clear();
            pendingHeroes.Clear();

            var party = gameState == null ? null : gameState.Party;
            if (party != null)
            {
                pendingHeroes.AddRange(party.AliveMembers.Where(CanBeAttacked));
            }

            foreach (var monster in AliveMonsters())
            {
                roundActions.Add(ChooseMonsterAction(monster.Instance));
            }

            ChooseNextHeroAction();
        }

        private void ChooseNextHeroAction()
        {
            if (!AliveHeroes().Any())
            {
                ShowDefeatMessage();
                return;
            }

            if (!AliveMonsters().Any())
            {
                ShowVictoryMessage();
                return;
            }

            while (pendingHeroes.Count > 0)
            {
                actingHero = pendingHeroes[0];
                pendingHeroes.RemoveAt(0);
                if (!CanBeAttacked(actingHero))
                {
                    continue;
                }

                if (actingHero.Status.Any(effect => effect.Type == EffectType.Sleep))
                {
                    QueueHeroAction(new RoundAction
                    {
                        Source = actingHero,
                        State = RoundActionState.Nothing
                    });
                    continue;
                }

                if (actingHero.Status.Any(effect => effect.Type == EffectType.Confusion))
                {
                    var confusedTargets = AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>()
                        .Concat(AliveHeroes().Cast<IFighter>())
                        .Where(target => target != actingHero)
                        .ToList();
                    if (confusedTargets.Count > 0)
                    {
                        QueueHeroAction(new RoundAction
                        {
                            Source = actingHero,
                            State = RoundActionState.Fight,
                            Targets = new List<IFighter> { confusedTargets[CombatRandom.Next(confusedTargets.Count)] }
                        });
                        continue;
                    }
                }

                state = CombatState.ChooseAction;
                selectedMenuIndex = GetRememberedActionIndex(BuildActionButtons().ToList());
                messageText = actingHero.Name + "'s action.";
                return;
            }

            ResolveNextRoundAction();
        }

        private void QueueHeroAction(RoundAction action)
        {
            if (action != null)
            {
                roundActions.Add(action);
            }

            actingHero = null;
            ChooseNextHeroAction();
        }

        private void ResolveNextRoundAction()
        {
            if (!AliveHeroes().Any())
            {
                ShowDefeatMessage();
                return;
            }

            if (!AliveMonsters().Any())
            {
                ShowVictoryMessage();
                return;
            }

            var action = roundActions
                .Where(IsActionResolvable)
                .OrderByDescending(item => item.Source.Agility)
                .FirstOrDefault();
            if (action == null)
            {
                EndRound();
                return;
            }

            roundActions.Remove(action);
            bool endFight;
            var message = ExecuteRoundAction(action, out endFight);
            ShowMessage(message, endFight ? (Action)null : ResolveNextRoundAction);
        }

        private void EndRound()
        {
            if (AliveHeroes().Any() && AliveMonsters().Any())
            {
                BeginRound();
                return;
            }

            ResolveNextRoundAction();
        }

        private void BeginTargetSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            BeginTargetSelection(
                "Choose a target for " + actingHero.Name + ".",
                AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList(),
                Target.Single,
                1,
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Fight,
                    Targets = targets
                }));
        }

        private void BeginSpellSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            var spells = GetAvailableEncounterSpells(actingHero).ToList();
            if (spells.Count == 0)
            {
                ShowMessage(actingHero.Name + " cannot cast any combat spells.", ChooseNextHeroAction);
                return;
            }

            state = CombatState.ChooseSpell;
            selectedMenuIndex = GetRememberedSpellIndex(spells);
            messageText = "Choose a spell for " + actingHero.Name + ".";
            menuInput.BlockInteractUntilRelease();
        }

        private void BeginItemSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            var items = GetAvailableEncounterItems(actingHero).ToList();
            if (items.Count == 0)
            {
                ShowMessage(actingHero.Name + " has no combat items.", ChooseNextHeroAction);
                return;
            }

            state = CombatState.ChooseItem;
            selectedMenuIndex = GetRememberedItemIndex(items);
            messageText = "Choose an item for " + actingHero.Name + ".";
            menuInput.BlockInteractUntilRelease();
        }

        private void BeginTargetSelection(
            string title,
            List<IFighter> candidates,
            Target targetMode,
            int maxTargets,
            Action<List<IFighter>> done,
            bool allowDead = false)
        {
            candidates = candidates == null
                ? new List<IFighter>()
                : candidates.Where(candidate => allowDead ? candidate != null : CanBeAttacked(candidate)).ToList();
            if (targetMode == Target.None || candidates.Count == 0)
            {
                done(new List<IFighter>());
                return;
            }

            if (targetMode == Target.Group)
            {
                done(maxTargets > 0 ? candidates.Take(maxTargets).ToList() : candidates);
                return;
            }

            if (candidates.Count == 1)
            {
                RememberTarget(candidates[0]);
                done(new List<IFighter> { candidates[0] });
                return;
            }

            targetSelectionTitle = title;
            targetSelectionCandidates = candidates;
            targetSelectionDone = done;
            state = CombatState.ChooseTarget;
            selectedMenuIndex = GetRememberedTargetIndex(candidates);
            messageText = title;
            menuInput.BlockInteractUntilRelease();
        }

        private void ResolveHeroSpell(Spell spell)
        {
            if (actingHero == null || actingHero.IsDead || spell == null)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction("Spell");
            RememberSpell(spell);
            spell.Setup(GameDataCache.Current == null ? null : GameDataCache.Current.Skills);
            var candidates = spell.IsAttackSpell
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySpellTargets(spell);
            BeginTargetSelection(
                "Choose a target for " + spell.Name + ".",
                candidates,
                spell.Targets,
                spell.MaxTargets,
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Spell,
                    Spell = spell,
                    Targets = targets
                }),
                spell.Type == SkillType.Revive);
        }

        private void ResolveHeroSkill(Skill skill)
        {
            if (actingHero == null || actingHero.IsDead || skill == null)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction(skill.Name);
            var candidates = skill.IsAttackSkill
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySkillTargets(skill);
            BeginTargetSelection(
                "Choose a target for " + skill.Name + ".",
                candidates,
                skill.Targets,
                skill.MaxTargets,
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Skill,
                    Skill = skill,
                    Targets = targets
                }),
                skill.Type == SkillType.Revive);
        }

        private void ResolveHeroItem(ItemInstance item)
        {
            if (actingHero == null || actingHero.IsDead || item == null || item.Item == null)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction("Item");
            RememberItem(item);
            EnsureItemLinked(item);
            var skill = item.Item.Skill;
            if (skill == null)
            {
                ShowMessage(item.Name + " cannot be used in combat.", ChooseNextHeroAction);
                return;
            }

            var candidates = skill.IsAttackSkill
                ? AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
                : GetPartySkillTargets(skill);
            BeginTargetSelection(
                "Choose a target for " + item.Name + ".",
                candidates,
                item.Target,
                skill.MaxTargets,
                targets => QueueHeroAction(new RoundAction
                {
                    Source = actingHero,
                    State = RoundActionState.Item,
                    Item = item,
                    Targets = targets
                }),
                skill.Type == SkillType.Revive);
        }

        private void ResolveHeroRun()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction("Run");
            QueueHeroAction(new RoundAction
            {
                Source = actingHero,
                State = RoundActionState.Run,
                Targets = AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
            });
        }

        private RoundAction ChooseMonsterAction(IFighter monster)
        {
            if (monster == null)
            {
                return null;
            }

            if (monster.Status.Any(effect => effect.Type == EffectType.Sleep))
            {
                return new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Nothing
                };
            }

            var availableTargets = AliveHeroes().Cast<IFighter>().Where(CanBeAttacked).ToList();
            if (monster.Status.Any(effect => effect.Type == EffectType.Confusion))
            {
                availableTargets.AddRange(AliveMonsters().Select(item => item.Instance).Where(CanBeAttacked));
                availableTargets.Remove(monster);
            }

            if (availableTargets.Count == 0)
            {
                return new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Nothing
                };
            }

            var availableSpells = GameDataCache.Current == null || GameDataCache.Current.Spells == null
                ? new List<Spell>()
                : monster.GetSpells(GameDataCache.Current.Spells)
                    .Where(spell => spell != null && spell.IsEncounterSpell && spell.Cost <= monster.Magic)
                    .ToList();
            if (!monster.Status.Any(effect => effect.Type == EffectType.StopSpell) && availableSpells.Count > 0)
            {
                var lowHealthHealSpells = availableSpells
                    .Where(spell => spell.Type == SkillType.Heal && monster.MaxHealth > 0 && (float)monster.Health / monster.MaxHealth < 0.1f)
                    .ToList();
                if (lowHealthHealSpells.Count > 0)
                {
                    return new RoundAction
                    {
                        Source = monster,
                        State = RoundActionState.Spell,
                        Spell = lowHealthHealSpells[CombatRandom.Next(lowHealthHealSpells.Count)],
                        Targets = new List<IFighter> { monster }
                    };
                }

                var attackSpells = availableSpells.Where(spell => spell.IsAttackSpell).ToList();
                if (attackSpells.Count > 0)
                {
                    var spell = attackSpells[CombatRandom.Next(attackSpells.Count)];
                    return new RoundAction
                    {
                        Source = monster,
                        State = RoundActionState.Spell,
                        Spell = spell,
                        Targets = GetTargets(spell.Targets, spell.MaxTargets, availableTargets)
                    };
                }
            }

            var availableSkills = GameDataCache.Current == null || GameDataCache.Current.Skills == null
                ? new List<Skill>()
                : monster.GetSkills(GameDataCache.Current.Skills).Where(skill => skill != null && skill.IsEncounterSkill).ToList();
            if (availableSkills.Count > 0 && Dice.RollD100() > 75)
            {
                var skill = availableSkills[CombatRandom.Next(availableSkills.Count)];
                if (skill.Type == SkillType.Flee)
                {
                    return new RoundAction
                    {
                        Source = monster,
                        State = RoundActionState.Run,
                        Targets = availableTargets
                    };
                }

                return new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Skill,
                    Skill = skill,
                    Targets = GetTargets(skill.Targets, skill.MaxTargets, availableTargets)
                };
            }

            return new RoundAction
            {
                Source = monster,
                State = RoundActionState.Fight,
                Targets = new List<IFighter> { ChooseFighter(availableTargets) }
            };
        }

        private bool IsActionResolvable(RoundAction action)
        {
            if (action == null || action.Source == null || !CanBeAttacked(action.Source))
            {
                return false;
            }

            if (action.State == RoundActionState.Nothing || action.Targets == null)
            {
                return true;
            }

            if (IsReviveAction(action))
            {
                return action.Targets.Any(target => target != null && target.IsDead);
            }

            if (IsOffensiveAction(action))
            {
                return ResolveActionTargets(action).Any();
            }

            return action.Targets.Any(CanBeAttacked);
        }

        private string ExecuteRoundAction(RoundAction action, out bool endFight)
        {
            endFight = false;
            if (action == null || action.Source == null)
            {
                return "";
            }

            var message = action.Source.CheckForExpiredStates(round, DurationType.Rounds);
            message += action.Source.UpdateStatusEffects(gameState);
            if (action.Source.IsDead)
            {
                return string.IsNullOrEmpty(message) ? action.Source.Name + " cannot act." : message.TrimEnd();
            }

            if (action.State != RoundActionState.Nothing &&
                action.Source.Status.Any(effect => effect.Type == EffectType.Sleep))
            {
                message += action.Source.Name + " is asleep.";
                return message.TrimEnd();
            }

            switch (action.State)
            {
                case RoundActionState.Run:
                    string runMessage;
                    bool didEndFight;
                    Run(action.Source, action.Targets, out runMessage, out didEndFight);
                    endFight = didEndFight;
                    message += runMessage;
                    break;
                case RoundActionState.Fight:
                    message += Fight(action.Source, ResolveActionTargets(action).FirstOrDefault());
                    break;
                case RoundActionState.Spell:
                    message += CastSpell(action.Spell, ResolveActionTargets(action), action.Source);
                    break;
                case RoundActionState.Item:
                    message += UseItem(action.Item, ResolveActionTargets(action), action.Source);
                    break;
                case RoundActionState.Nothing:
                    message += action.Source.Name + " doesn't do anything.";
                    break;
                case RoundActionState.Skill:
                    message += DoSkill(action.Skill, ResolveActionTargets(action), action.Source);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return string.IsNullOrEmpty(message) ? "" : message.TrimEnd();
        }

        private void Run(IFighter source, IEnumerable<IFighter> targets, out string message, out bool endFight)
        {
            message = source.Name + " tried to run.\n";
            endFight = false;
            var fastestTarget = targets == null ? null : targets.Where(CanBeAttacked).OrderByDescending(target => target.Agility).FirstOrDefault();
            if (fastestTarget != null && !source.CanHit(fastestTarget))
            {
                message += "But could not get away.";
                return;
            }

            Audio.GetOrCreate().PlaySoundEffect("stairs-up");
            message += "And got away.";
            if (source is Hero)
            {
                Audio.GetOrCreate().PlayMusic(EndFightSong);
                endFight = true;
                return;
            }

            var fighter = source as Fighter;
            if (fighter != null)
            {
                fighter.RanAway = true;
            }
        }

        private List<IFighter> ResolveActionTargets(RoundAction action)
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

            return GetFallbackTargets(action);
        }

        private List<IFighter> GetOpposingTargets(IFighter source)
        {
            if (source is Hero)
            {
                return AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().Where(CanBeAttacked).ToList();
            }

            return AliveHeroes().Cast<IFighter>().Where(CanBeAttacked).ToList();
        }

        private List<IFighter> GetFallbackTargets(RoundAction action)
        {
            var candidates = GetOpposingTargets(action.Source);
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

        private static Target GetActionTargetMode(RoundAction action)
        {
            switch (action.State)
            {
                case RoundActionState.Spell:
                    return action.Spell == null ? Target.Single : action.Spell.Targets;
                case RoundActionState.Skill:
                    return action.Skill == null ? Target.Single : action.Skill.Targets;
                case RoundActionState.Item:
                    return action.Item == null ? Target.Single : action.Item.Target;
                default:
                    return Target.Single;
            }
        }

        private static int GetActionMaxTargets(RoundAction action)
        {
            switch (action.State)
            {
                case RoundActionState.Spell:
                    return action.Spell == null ? 1 : action.Spell.MaxTargets;
                case RoundActionState.Skill:
                    return action.Skill == null ? 1 : action.Skill.MaxTargets;
                case RoundActionState.Item:
                    return action.Item == null || action.Item.Item == null || action.Item.Item.Skill == null
                        ? 1
                        : action.Item.Item.Skill.MaxTargets;
                default:
                    return 1;
            }
        }

        private static bool IsReviveAction(RoundAction action)
        {
            return action != null &&
                   ((action.State == RoundActionState.Spell &&
                     action.Spell != null &&
                     action.Spell.Type == SkillType.Revive) ||
                    (action.State == RoundActionState.Skill &&
                     action.Skill != null &&
                     action.Skill.Type == SkillType.Revive) ||
                    (action.State == RoundActionState.Item &&
                     action.Item != null &&
                     action.Item.Item != null &&
                     action.Item.Item.Skill != null &&
                     action.Item.Item.Skill.Type == SkillType.Revive));
        }

        private static bool IsOffensiveAction(RoundAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.State)
            {
                case RoundActionState.Fight:
                    return true;
                case RoundActionState.Spell:
                    return action.Spell != null && action.Spell.IsAttackSpell;
                case RoundActionState.Skill:
                    return action.Skill != null && (action.Skill.IsAttackSkill || action.Skill.DoAttack);
                case RoundActionState.Item:
                    return action.Item != null &&
                           action.Item.Item != null &&
                           action.Item.Item.Skill != null &&
                           (action.Item.Item.Skill.IsAttackSkill || action.Item.Item.Skill.DoAttack);
                default:
                    return false;
            }
        }

        private static IFighter ChooseFighter(IReadOnlyCollection<IFighter> availableTargets)
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                return null;
            }

            if (availableTargets.Count == 1)
            {
                return availableTargets.First();
            }

            var roll = Dice.RollD100();
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

        private static List<IFighter> GetTargets(Target targetType, int maxTargets, IReadOnlyCollection<IFighter> availableTargets)
        {
            if (availableTargets == null || availableTargets.Count == 0)
            {
                return new List<IFighter>();
            }

            if (targetType != Target.Group)
            {
                return new List<IFighter> { ChooseFighter(availableTargets) };
            }

            if (maxTargets == 0)
            {
                return availableTargets.ToList();
            }

            var targets = new List<IFighter>();
            for (var i = 0; i < maxTargets; i++)
            {
                var target = ChooseFighter(availableTargets);
                if (target != null)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        private List<IFighter> GetPartySpellTargets(Spell spell)
        {
            return spell != null && spell.Type == SkillType.Revive
                ? DeadHeroes().Cast<IFighter>().ToList()
                : AliveHeroes().Cast<IFighter>().ToList();
        }

        private List<IFighter> GetPartySkillTargets(Skill skill)
        {
            return skill != null && skill.Type == SkillType.Revive
                ? DeadHeroes().Cast<IFighter>().ToList()
                : AliveHeroes().Cast<IFighter>().ToList();
        }
    }
}
