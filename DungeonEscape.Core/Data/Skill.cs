using Redpoint.DungeonEscape.State;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.Data
{
    public class Skill
    {
        private static readonly Random Random = new Random();

        private static readonly List<SkillType> AttackSkill = new List<SkillType>
        {
            SkillType.Damage,
            SkillType.Dot,
            SkillType.Decrease
        };

        private static readonly List<SkillType> NonEncounterSkill = new List<SkillType>
        {
            SkillType.Heal,
            SkillType.Outside,
            SkillType.Return,
            SkillType.Revive,
            SkillType.RemoveCondition,
            SkillType.Disengage,
            SkillType.AbilityModifier,
            SkillType.Open
        };

        private static readonly List<SkillType> EncounterSkill = new List<SkillType>
        {
            SkillType.Heal,
            SkillType.Damage,
            SkillType.Revive,
            SkillType.Dot,
            SkillType.RemoveCondition,
            SkillType.AbilityModifier,
            SkillType.Steal
        };

        public string Name { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public Target Targets { get; set; }
        public int MaxTargets { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public SkillType Type { get; set; }
        public bool IsPiercing { get; set; }
        public int StatConst { get; set; }
        public int StatTimes { get; set; }
        public int StatRandom { get; set; }
        public int DurationConst { get; set; }
        public int DurationTimes { get; set; }
        public int DurationRandom { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public DurationType DurationType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public StatType StatType { get; set; }
        public string EffectName { get; set; }
        public bool DoAttack { get; set; }
        public bool IsPositive { get; set; } = true;
        public bool IsBonusAction { get; set; }

        [JsonIgnore] public bool IsAttackSkill { get { return AttackSkill.Contains(Type); } }
        [JsonIgnore] public bool IsEncounterSkill { get { return EncounterSkill.Contains(Type); } }
        [JsonIgnore] public bool IsNonEncounterSkill { get { return NonEncounterSkill.Contains(Type); } }

        public Skill()
        {
            StatTimes = 1;
            DurationTimes = 1;
            StatType = StatType.None;
        }

        public override string ToString()
        {
            return Name;
        }

        public (string, bool) Do(IFighter target, IFighter source, BaseState targetObject, IGame game, int round, bool isMagic = false)
        {
            switch (Type)
            {
                case SkillType.Open:
                    return DoOpen(source, targetObject, game);
                case SkillType.Heal:
                    return DoHeal(target);
                case SkillType.Outside:
                    return DoOutside(game);
                case SkillType.Damage:
                    return DoDamage(target, source, isMagic);
                case SkillType.Return:
                    return DoReturn(game);
                case SkillType.Revive:
                    return DoHeal(target);
                case SkillType.Decrease:
                    return DoBuff(target, game, round, false);
                case SkillType.Dot:
                    return DoDot(target, game, round);
                case SkillType.Steal:
                    return DoSteal(target, source, game);
                case SkillType.Disengage:
                    return DoDisengage(source, game, round);
                case SkillType.RemoveCondition:
                    return DoRemoveCondition(target);
                case SkillType.AbilityModifier:
                    return DoAbilityModifier(target, IsPositive);
                case SkillType.None:
                    return (source.Name + " " + EffectName, false);
                default:
                    return ("", false);
            }
        }

        private (string, bool) DoOpen(IFighter source, BaseState targetObject, IGame game)
        {
            if (targetObject == null || targetObject.Type != SpriteType.Door)
            {
                return ("This is not a door", false);
            }

            var door = targetObject as ObjectState;
            if (door != null && door.IsOpen == true)
            {
                return ("The Door is already open", false);
            }

            if (game != null && game.Sounds != null)
            {
                game.Sounds.PlaySoundEffect("door");
            }

            if (door != null)
            {
                door.IsOpen = true;
            }

            return (source.Name + " Is unable to Open Door\n", false);
        }

        private (string, bool) DoDisengage(IFighter source, IGame game, int round)
        {
            if (source == null)
            {
                return ("", false);
            }

            var fighter = source as Fighter;
            if (fighter != null)
            {
                fighter.RanAway = true;
            }

            if (game != null && source.Status.All(status => status.Type != EffectType.Disengaged))
            {
                source.AddEffect(CreateEffect(EffectType.Disengaged, round, game));
            }

            return (source.Name + " disengages and retreats.\n", true);
        }

        private (string, bool) DoHeal(IFighter target)
        {
            if (StatType == StatType.Magic)
            {
                var hero = target as Hero;
                if (hero == null)
                {
                    return (target.Name + " is unaffected by " + Name + "\n", false);
                }

                hero.RestoreSpellSlots();
                return (target.Name + " regains spell slots\n", true);
            }

            var oldHealth = target.Health;
            target.Health = StatRandom != 0 ? target.Health + Dice.Roll(StatRandom, StatTimes, StatConst) : target.MaxHealth;

            if (target.MaxHealth < target.Health)
            {
                target.Health = target.MaxHealth;
            }

            return (oldHealth == 0
                ? target.Name + " is revived and gains " + target.Health + " HP\n"
                : target.Name + " gains " + (target.Health - oldHealth) + " HP\n", true);
        }

        private (string, bool) DoRemoveCondition(IFighter target)
        {
            if (target.Status.Count(i => i.IsNegativeEffect) == 0)
            {
                return (target.Name + " is unaffected by " + Name + "\n", false);
            }

            var message = "";
            foreach (var effect in target.Status.Where(i => i.IsNegativeEffect).ToList())
            {
                message += target.Name + " " + effect.Name + " has ended\n";
                target.RemoveEffect(effect);
            }

            return (message, true);
        }

        private (string, bool) DoClearEffects(IFighter target)
        {
            return DoRemoveCondition(target);
        }

        private (string, bool) DoSteal(IFighter target, IFighter source, IGame game)
        {
            var message = DoAttack ? "and " : "";
            if (!source.CanHit(target))
            {
                return (source.Name + " tried to steal from " + target.Name + " but did not find anything\n", false);
            }

            Item item = null;
            if (target is Hero)
            {
                if (target.Items.Any() && Random.NextDouble() < 0.25)
                {
                    var instance = target.Items[Random.Next(target.Items.Count)];
                    item = instance.Item;
                    if (instance.IsEquipped)
                    {
                        instance.UnEquip(game.Party.ActiveMembers);
                    }

                    target.Items.Remove(instance);
                }
                else
                {
                    var goldToSteal = Dice.Roll(5, 20);
                    if (game.Party.Gold < goldToSteal)
                    {
                        goldToSteal = game.Party.Gold;
                        game.Party.Gold = 0;
                    }

                    if (goldToSteal != 0)
                    {
                        item = game.CreateGold(goldToSteal);
                    }
                }
            }
            else if (target is MonsterInstance)
            {
                item = game.CreateChestItem(source.Level);
            }

            if (item == null)
            {
                return (source.Name + " tried to steal from " + target.Name + " but did not find anything\n", false);
            }

            if (item.Type == ItemType.Gold)
            {
                var monster = source as MonsterInstance;
                if (source is Hero)
                {
                    game.Party.Gold += item.Cost;
                }
                else if (monster != null)
                {
                    monster.Gold += item.Cost;
                }
                else
                {
                    return (source.Name + " tried to steal from " + target.Name + " but did not find anything\n", false);
                }

                if (game.Sounds != null)
                {
                    game.Sounds.PlaySoundEffect("treasure");
                }

                return (message + source.Name + " stole " + item.Cost + " Gold from " + target.Name + ".\n", true);
            }

            if (source is Hero)
            {
                var hero = game.Party.AddItem(new ItemInstance(item));
                if (hero == null)
                {
                    return (source.Name + " tried to steal from " + target.Name + " but did not find anything\n", false);
                }
            }
            else
            {
                source.Items.Add(new ItemInstance(item));
            }

            return (message + source.Name + " stole " + item.Name + " from " + target.Name + ".\n", true);
        }

        private (string, bool) DoDamage(IFighter target, IFighter source, bool isMagic)
        {
            var damage = target.CalculateDamage(source.Attack > 0 ? Random.Next(source.Attack) : 0, IsPiercing, isMagic);
            var message = target.Name;

            if (damage <= 0)
            {
                return (message + " was unharmed\n", false);
            }

            if (StatType == StatType.HP)
            {
                target.Health -= damage;
                message += " took " + damage + " points of damage from " + EffectName + "\n";
                message += target.HitCheck();
                if (target.Health <= 0)
                {
                    message += "and has died!\n";
                    target.Health = 0;
                }
            }
            else if (StatType == StatType.Magic)
            {
                target.Magic -= damage;
                message += " lost " + damage + " points of magic from " + EffectName + "\n";
                if (target.Magic < 0)
                {
                    target.Magic = 0;
                }
            }

            target.PlayDamageAnimation();
            return (message, true);
        }

        private (string, bool) DoBuff(IFighter target, IGame gameState, int round, bool increase)
        {
            var roll = Dice.Roll(StatRandom, StatTimes, StatConst);
            var buff = increase ? roll : target.CalculateDamage(roll, IsPiercing);
            if (buff == 0)
            {
                return (target.Name + " was not affected by " + Name + "\n", false);
            }

            var effect = CreateEffect(EffectType.OverTime, round, gameState);
            effect.StatValue = increase ? buff : -buff;
            effect.StatType = StatType;
            target.AddEffect(effect);

            var changed = increase ? "increased" : "decreased";
            return (target.Name + " " + changed + " " + StatType + " " + buff + " points\n", true);
        }

        private (string, bool) DoAbilityModifier(IFighter target, bool increase)
        {
            return DoStat(target, increase);
        }

        private (string, bool) DoStat(IFighter target, bool increase)
        {
            var roll = Dice.Roll(StatRandom, StatTimes, StatConst);
            var buff = increase ? roll : target.CalculateDamage(roll, IsPiercing);
            var statValue = increase ? buff : -buff;

            switch (StatType)
            {
                case StatType.HP:
                    target.MaxHealth += statValue;
                    if (target.Health > target.MaxHealth) target.Health = target.MaxHealth;
                    break;
                case StatType.Attack:
                    target.Attack += statValue;
                    break;
                case StatType.Defence:
                    target.Defence += statValue;
                    break;
                case StatType.MagicDefence:
                    target.MagicDefence += statValue;
                    break;
                case StatType.Agility:
                    target.Agility += statValue;
                    break;
                case StatType.Magic:
                    target.MaxMagic += statValue;
                    if (target.Magic > target.MaxMagic) target.Magic = target.MaxMagic;
                    break;
            }

            var changed = increase ? "increased" : "decreased";
            return (target.Name + " permanently " + changed + " " + StatType + " " + buff + " points\n", true);
        }

        private (string, bool) DoDot(IFighter target, IGame gameState, int round)
        {
            var buff = target.CalculateDamage(Dice.Roll(StatRandom, StatTimes, StatConst));
            if (buff == 0 || target.Status.Any(i => i.Type == EffectType.OverTime && i.Name == EffectName))
            {
                return (target.Name + " was not affected by " + Name + "\n", false);
            }

            var effect = CreateEffect(EffectType.OverTime, round, gameState);
            effect.StatValue = -buff;
            target.AddEffect(effect);
            return (target.Name + " is " + EffectName + "\n", true);
        }

        private (string, bool) DoOutside(IGame gameState)
        {
            if (gameState.Party.CurrentMapIsOverWorld)
            {
                return ("but you are already outside\n", false);
            }

            gameState.SetMap();
            return (null, true);
        }

        private (string, bool) DoReturn(IGame gameState)
        {
            if (!gameState.Party.CurrentMapIsOverWorld)
            {
                return ("but you are not outside\n", false);
            }

            if (string.IsNullOrEmpty(gameState.Party.SavedMapId))
            {
                return ("but you have never saved your game\n", false);
            }

            gameState.SetMap(gameState.Party.SavedMapId, null, gameState.Party.SavedPoint);
            return (null, true);
        }

        private StatusEffect CreateEffect(EffectType type, int round, IGame gameState)
        {
            return new StatusEffect
            {
                Name = EffectName,
                Type = type,
                StatType = StatType,
                Duration = Dice.Roll(DurationRandom, DurationTimes, DurationConst),
                DurationType = DurationType,
                StartTime = DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
            };
        }
    }
}
