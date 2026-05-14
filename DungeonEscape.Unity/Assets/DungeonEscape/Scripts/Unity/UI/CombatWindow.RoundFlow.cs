using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Rules;
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
                    QueueHeroAction(new CombatRoundAction
                    {
                        Source = actingHero,
                        State = CombatRoundActionState.Nothing
                    });
                    continue;
                }

                if (actingHero.Status.Any(effect => effect.Type == EffectType.Confusion))
                {
                    var confusedAction = CombatRoundRules.ChooseConfusedHeroAction(
                        actingHero,
                        AliveHeroes().Cast<IFighter>(),
                        AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>(),
                        maxValue => CombatRandom.Next(maxValue));
                    if (confusedAction != null)
                    {
                        QueueHeroAction(confusedAction);
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

        private void QueueHeroAction(CombatRoundAction action)
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

            var action = CombatRoundRules.SelectNextResolvableAction(roundActions, GetOpposingTargets);
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
                targets => QueueHeroAction(new CombatRoundAction
                {
                    Source = actingHero,
                    State = CombatRoundActionState.Fight,
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
                targets => QueueHeroAction(new CombatRoundAction
                {
                    Source = actingHero,
                    State = CombatRoundActionState.Spell,
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
                targets => QueueHeroAction(new CombatRoundAction
                {
                    Source = actingHero,
                    State = CombatRoundActionState.Skill,
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
                targets => QueueHeroAction(new CombatRoundAction
                {
                    Source = actingHero,
                    State = CombatRoundActionState.Item,
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
            QueueHeroAction(new CombatRoundAction
            {
                Source = actingHero,
                State = CombatRoundActionState.Run,
                Targets = AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().ToList()
            });
        }

        private CombatRoundAction ChooseMonsterAction(IFighter monster)
        {
            return CombatRoundRules.ChooseMonsterAction(
                monster,
                AliveHeroes().Cast<IFighter>(),
                AliveMonsters().Select(item => item.Instance).Cast<IFighter>(),
                GameDataCache.Current == null ? null : GameDataCache.Current.Spells,
                GameDataCache.Current == null ? null : GameDataCache.Current.Skills,
                maxValue => CombatRandom.Next(maxValue),
                () => Dice.RollD100());
        }

        private string ExecuteRoundAction(CombatRoundAction action, out bool endFight)
        {
            return CombatRoundRules.ExecuteRoundAction(
                action,
                gameState,
                round,
                Run,
                Fight,
                CastSpell,
                UseItem,
                DoSkill,
                GetOpposingTargets,
                out endFight);
        }

        private CombatRunResult Run(CombatRoundAction action)
        {
            var result = CombatRoundRules.Run(action == null ? null : action.Source, action == null ? null : action.Targets);
            if (result.Succeeded)
            {
                Audio.GetOrCreate().PlaySoundEffect("stairs-up");
                if (result.EndFight)
                {
                    Audio.GetOrCreate().PlayMusic(EndFightSong);
                }
            }

            return result;
        }

        private List<IFighter> GetOpposingTargets(IFighter source)
        {
            if (source is Hero)
            {
                return AliveMonsters().Select(monster => monster.Instance).Cast<IFighter>().Where(CanBeAttacked).ToList();
            }

            return AliveHeroes().Cast<IFighter>().Where(CanBeAttacked).ToList();
        }

        private List<IFighter> GetPartySpellTargets(Spell spell)
        {
            return CombatRoundRules.GetPartySpellTargets(spell, AliveHeroes(), DeadHeroes());
        }

        private List<IFighter> GetPartySkillTargets(Skill skill)
        {
            return CombatRoundRules.GetPartySkillTargets(skill, AliveHeroes(), DeadHeroes());
        }
    }
}
