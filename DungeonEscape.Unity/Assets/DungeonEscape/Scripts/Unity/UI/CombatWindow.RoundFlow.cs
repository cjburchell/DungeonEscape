using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;

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
                var action = ChooseMonsterAction(monster.Instance);
                CombatRoundRules.RollInitiative(action, () => Dice.RollD20());
                roundActions.Add(action);
                var bonusAction = ChooseMonsterBonusAction(monster.Instance);
                if (bonusAction != null)
                {
                    CombatRoundRules.RollInitiative(bonusAction, () => Dice.RollD20());
                    roundActions.Add(bonusAction);
                }
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

                state = CombatState.ChooseAction;
                actingHeroActionQueued = false;
                actingHeroBonusActionQueued = false;
                selectedMenuIndex = GetRememberedActionIndex(BuildActionButtons().ToList());
                messageText = actingHero.Name + "'s action.";
                return;
            }

            ResolveNextRoundAction();
        }

        private void QueueHeroAction(CombatRoundAction action)
        {
            QueueHeroAction(action, false);
        }

        private void QueueHeroAction(CombatRoundAction action, bool bonusAction)
        {
            if (action != null)
            {
                CombatRoundRules.RollInitiative(action, () => Dice.RollD20());
                roundActions.Add(action);
            }

            if (bonusAction)
            {
                actingHeroBonusActionQueued = true;
            }
            else
            {
                actingHeroActionQueued = true;
            }

            if (actingHero != null &&
                (!actingHeroActionQueued || (!actingHeroBonusActionQueued && HasAvailableBonusActions(actingHero))))
            {
                state = CombatState.ChooseAction;
                selectedMenuIndex = GetRememberedActionIndex(BuildActionButtons().ToList());
                messageText = actingHero.Name + "'s action.";
                menuInput.BlockInteractUntilRelease();
                return;
            }

            EndHeroTurn();
        }

        private void EndHeroTurn()
        {
            actingHero = null;
            actingHeroActionQueued = false;
            actingHeroBonusActionQueued = false;
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
            ShowMessage(message, endFight ? null : ResolveNextRoundAction);
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

        private void BeginWeaponSelection()
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            var weapons = GetAvailableFightWeapons(actingHero).ToList();
            if (weapons.Count <= 1)
            {
                BeginFightTargetSelection(weapons.FirstOrDefault());
                return;
            }

            state = CombatState.ChooseWeapon;
            selectedMenuIndex = GetCurrentWeaponIndex(weapons);
            messageText = "Choose a weapon for " + actingHero.Name + ".";
            menuInput.BlockInteractUntilRelease();
        }

        private void BeginFightTargetSelection(ItemInstance weapon)
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
                    Weapon = weapon,
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

            var spells = GetAvailableActionSpells(actingHero).ToList();
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

            var items = GetAvailableActionItems(actingHero).ToList();
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

        private void BeginBonusActionSelection()
        {
            if (actingHero == null || actingHero.IsDead || actingHeroBonusActionQueued)
            {
                ReturnToActionMenu();
                return;
            }

            var bonusActions = GetAvailableBonusActionButtons();
            if (bonusActions.Count <= 1)
            {
                ShowMessage(actingHero.Name + " has no bonus actions.", ReturnToActionMenu);
                return;
            }

            state = CombatState.ChooseBonusAction;
            selectedMenuIndex = 0;
            messageText = "Choose a bonus action for " + actingHero.Name + ".";
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
            ResolveHeroSpell(spell, false);
        }

        private void ResolveHeroSpell(Spell spell, bool bonusAction)
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
                }, bonusAction),
                spell.Type == SkillType.Revive);
        }

        private void ResolveHeroWeapon(ItemInstance weapon)
        {
            if (actingHero == null || actingHero.IsDead)
            {
                ChooseNextHeroAction();
                return;
            }

            RememberAction("Fight");
            if (weapon != null && !weapon.IsEquipped)
            {
                if (gameState == null || !gameState.EquipHeroItem(actingHero, weapon))
                {
                    ShowMessage(actingHero.Name + " cannot ready " + weapon.Name + ".", ChooseNextHeroAction);
                    return;
                }
            }

            BeginFightTargetSelection(weapon);
        }

        private void ResolveHeroSkill(Skill skill)
        {
            ResolveHeroSkill(skill, false);
        }

        private void ResolveHeroSkill(Skill skill, bool bonusAction)
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
                }, bonusAction),
                skill.Type == SkillType.Revive);
        }

        private void ResolveHeroItem(ItemInstance item)
        {
            ResolveHeroItem(item, false);
        }

        private void ResolveHeroItem(ItemInstance item, bool bonusAction)
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
                }, bonusAction),
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
                maxValue => CombatRandom.Next(maxValue),
                () => Dice.RollD100());
        }

        private CombatRoundAction ChooseMonsterBonusAction(IFighter monster)
        {
            return CombatRoundRules.ChooseMonsterBonusAction(
                monster,
                AliveHeroes().Cast<IFighter>(),
                AliveMonsters().Select(item => item.Instance).Cast<IFighter>(),
                GameDataCache.Current == null ? null : GameDataCache.Current.Spells,
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
                (selectedAction, target) => Fight(selectedAction == null ? null : selectedAction.Source, target, selectedAction == null ? null : selectedAction.Weapon),
                CastSpell,
                UseItem,
                DoSkill,
                DoMonsterAction,
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

        private bool HasAvailableBonusActions(Hero hero)
        {
            return hero != null && !actingHeroBonusActionQueued &&
                   (GetAvailableBonusSpells(hero).Any() ||
                    GetAvailableBonusSkills(hero).Any() ||
                    GetAvailableBonusItems(hero).Any());
        }
    }
}
