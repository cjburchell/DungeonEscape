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
        private string CastSpell(Spell spell, List<IFighter> targets, IFighter caster)
        {
            if (caster == null || spell == null)
            {
                return "";
            }

            Audio.GetOrCreate().PlaySoundEffect("spell", true);
            var startingHealth = CaptureTargetHealth(targets);
            var message = spell.Cast(targets ?? new List<IFighter>(), new BaseState[0], caster, gameState, round);
            FlashHealthChangedTargets(startingHealth, targets);
            return string.IsNullOrEmpty(message) ? caster.Name + " casts " + spell.Name + "." : message.TrimEnd();
        }

        private string DoSkill(Skill skill, List<IFighter> targets, IFighter source)
        {
            if (source == null || skill == null)
            {
                return "";
            }

            Audio.GetOrCreate().PlaySoundEffect(skill.IsAttackSkill || skill.DoAttack ? "prepare-attack" : "spell", true);
            var message = source.Name + " uses " + skill.Name + ".\n";
            var hit = false;
            var selectedTargets = targets ?? new List<IFighter>();
            if (selectedTargets.Count == 0)
            {
                var result = skill.Do(null, source, null, gameState, round);
                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1;
                }
            }

            foreach (var target in selectedTargets)
            {
                if (target == null)
                {
                    continue;
                }

                if (skill.DoAttack || skill.Type == SkillType.Attack)
                {
                    int damage;
                    message += Fight(source, target, out damage, false) + "\n";
                    if (damage != 0)
                    {
                        hit = true;
                    }
                }

                if (target.IsDead)
                {
                    continue;
                }

                if (skill.IsAttackSkill)
                {
                    if (skill.Type == SkillType.Attack && !skill.DoAttack)
                    {
                        message += source.Name + " attacks " + target.Name + " with " + skill.EffectName + ".\n";
                    }

                    if (!source.CanHit(target))
                    {
                        if (!skill.DoAttack)
                        {
                            message += target.Name + " dodges the " + skill.EffectName + "\n";
                        }

                        continue;
                    }
                }

                var startingHealth = target.Health;
                var result = skill.Do(target, source, null, gameState, round);
                if (target.Health < startingHealth)
                {
                    StartDamageFlash(target);
                }
                else if (target.Health > startingHealth)
                {
                    StartHealFlash(target);
                }

                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1;
                }

                if (skill.IsAttackSkill && result.Item2)
                {
                    hit = true;
                }
            }

            if (skill.IsAttackSkill || skill.DoAttack)
            {
                Audio.GetOrCreate().PlaySoundEffect(hit ? "receive-damage" : "miss");
            }

            return message.TrimEnd();
        }

        private string UseItem(ItemInstance item, List<IFighter> targets, IFighter source)
        {
            if (source == null || item == null)
            {
                return "";
            }

            Audio.GetOrCreate().PlaySoundEffect("confirm");
            var message = "";
            var worked = false;
            var selectedTargets = targets == null || targets.Count == 0
                ? new List<IFighter> { source }
                : targets;
            foreach (var target in selectedTargets)
            {
                var startingHealth = target == null ? 0 : target.Health;
                var result = item.Use(source, target, null, gameState, round);
                if (target != null && target.Health < startingHealth)
                {
                    StartDamageFlash(target);
                }
                else if (target != null && target.Health > startingHealth)
                {
                    StartHealFlash(target);
                }

                if (result.Item2)
                {
                    worked = true;
                }

                if (!string.IsNullOrEmpty(result.Item1))
                {
                    message += result.Item1 + "\n";
                }
            }

            if (worked && item.Type == ItemType.OneUse)
            {
                item.UnEquip(gameState.Party.Members);
                source.Items.Remove(item);
            }
            else if (!item.HasCharges)
            {
                item.UnEquip(gameState.Party.Members);
                source.Items.Remove(item);
                message += item.Name + " has been destroyed.\n";
            }

            return string.IsNullOrEmpty(message) ? source.Name + " used " + item.Name + "." : message.TrimEnd();
        }

        private static Dictionary<string, int> CaptureTargetHealth(IEnumerable<IFighter> targets)
        {
            var health = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (targets == null)
            {
                return health;
            }

            foreach (var target in targets)
            {
                if (target != null && !string.IsNullOrEmpty(target.Name))
                {
                    health[target.Name] = target.Health;
                }
            }

            return health;
        }

        private static void FlashHealthChangedTargets(IDictionary<string, int> startingHealth, IEnumerable<IFighter> targets)
        {
            if (startingHealth == null || targets == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                int previousHealth;
                if (target != null &&
                    !string.IsNullOrEmpty(target.Name) &&
                    startingHealth.TryGetValue(target.Name, out previousHealth) &&
                    target.Health < previousHealth)
                {
                    StartDamageFlash(target);
                }
                else if (target != null &&
                         !string.IsNullOrEmpty(target.Name) &&
                         startingHealth.TryGetValue(target.Name, out previousHealth) &&
                         target.Health > previousHealth)
                {
                    StartHealFlash(target);
                }
            }
        }

        private static void StartDamageFlash(IFighter target)
        {
            if (target == null || string.IsNullOrEmpty(target.Name))
            {
                return;
            }

            DamageFlashEndTimes[target.Name] = Time.unscaledTime + DamageFlashDuration;
            if (target.IsDead)
            {
                DefeatedFighterVisibleEndTimes[target] = Time.unscaledTime + DamageFlashDuration;
            }
        }

        private static bool IsDefeatedFighterVisible(IFighter target)
        {
            if (target == null)
            {
                return false;
            }

            float endTime;
            if (!DefeatedFighterVisibleEndTimes.TryGetValue(target, out endTime))
            {
                return false;
            }

            if (Time.unscaledTime >= endTime)
            {
                DefeatedFighterVisibleEndTimes.Remove(target);
                return false;
            }

            return true;
        }

        private static void StartHealFlash(IFighter target)
        {
            if (target == null || string.IsNullOrEmpty(target.Name))
            {
                return;
            }

            HealFlashEndTimes[target.Name] = Time.unscaledTime + DamageFlashDuration;
        }

        private string Fight(IFighter source, IFighter target)
        {
            int damage;
            return Fight(source, target, out damage, true);
        }

        private string Fight(IFighter source, IFighter target, out int damage, bool playSounds)
        {
            if (source == null || target == null)
            {
                damage = 0;
                return "";
            }

            if (playSounds)
            {
                Audio.GetOrCreate().PlaySoundEffect("prepare-attack", true);
            }

            var message = source.Name + " attacks " + target.Name + ".\n";
            damage = 0;
            if (source.CanCriticalHit(target))
            {
                damage = target.CalculateDamage(RandomAttack(source.CriticalAttack));
                message += "Heroic maneuver!\n";
                message += target.Name;
            }
            else if (source.CanHit(target))
            {
                damage = target.CalculateDamage(RandomAttack(source.Attack));
                message += target.Name;
            }
            else
            {
                message += target.Name + " dodges the attack and";
            }

            if (damage <= 0)
            {
                message += " was unharmed";
            }
            else
            {
                target.Health -= damage;
                target.PlayDamageAnimation();
                StartDamageFlash(target);
                message += " took " + damage + " points of damage";
                message += "\n" + target.HitCheck().TrimEnd();
            }

            if (target.IsDead)
            {
                target.Health = 0;
                message += "\nand has died!";
            }

            if (playSounds)
            {
                Audio.GetOrCreate().PlaySoundEffect(damage == 0 ? "miss" : "receive-damage");
            }

            return message.TrimEnd();
        }

        private static int RandomAttack(int attack)
        {
            return attack <= 0 ? 0 : CombatRandom.Next(attack);
        }

        private static void EnsureItemLinked(ItemInstance item)
        {
            if (item == null || item.Item == null || GameDataCache.Current == null)
            {
                return;
            }

            item.Item.Setup(GameDataCache.Current.Skills);
        }

        private static bool CanBeAttacked(IFighter fighter)
        {
            return fighter != null && !fighter.IsDead && !fighter.RanAway;
        }

        private IEnumerable<CombatMonster> AliveMonsters()
        {
            return monsters.Where(monster => monster != null && CanBeAttacked(monster.Instance));
        }

        private IEnumerable<Hero> AliveHeroes()
        {
            return gameState == null || gameState.Party == null
                ? Enumerable.Empty<Hero>()
                : gameState.Party.AliveMembers.Where(CanBeAttacked);
        }

        private IEnumerable<Hero> DeadHeroes()
        {
            return gameState == null || gameState.Party == null
                ? Enumerable.Empty<Hero>()
                : gameState.Party.DeadMembers.Where(hero => hero != null);
        }

        private string GetVictoryMessage()
        {
            return gameState == null
                ? "The enemies have been defeated."
                : gameState.ApplyCombatRewards(monsters.Select(monster => monster.Instance));
        }

        private void ShowVictoryMessage()
        {
            Audio.GetOrCreate().PlaySoundEffect("victory", true);
            Audio.GetOrCreate().PlayMusic(EndFightSong);
            ShowMessage(GetVictoryMessage(), null);
        }

        private void ShowDefeatMessage()
        {
            Audio.GetOrCreate().PlaySoundEffect("receive-damage");
            ShowMessage("Everyone has died!", ReturnToTitleAfterDefeat);
        }

        private void ReturnToTitleAfterDefeat()
        {
            Close(false);
            Audio.GetOrCreate().PlayMusic(TitleSong);
            TitleMenu.OpenMainMenu();
        }
    }
}
