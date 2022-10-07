// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeProtected.Global

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Redpoint.DungeonEscape.Scenes.Map.Components.Objects;

namespace Redpoint.DungeonEscape.State
{
    using Nez;
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Skill
    {
        public string Name { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public Target Targets { get; set; }
        public int MaxTargets { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public SkillType Type { get; set; }
        public bool IsPiercing { get; set; }
        public int StatConst { get; set; }
        public int StatTimes { get; set; } = 1;
        public int StatRandom { get; set; }
        public int DurationConst { get; set; }
        public int DurationTimes { get; set; } = 1;
        public int DurationRandom { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public DurationType DurationType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public StatType StatType { get; set; } = StatType.None;
        public string EffectName { get; set; }
        
        public override string ToString()
        {
            return this.Name;
        }
        public bool DoAttack { get; set; }
        
        private static readonly List<SkillType> AttackSkill = new() {SkillType.Damage, SkillType.Dot, SkillType.Sleep, SkillType.Confusion, SkillType.StopSpell, SkillType.Decrease};
        
        [JsonIgnore]
        public bool IsAttackSkill => AttackSkill.Contains(this.Type);

        private static readonly List<SkillType> NonEncounterSkill = new() {SkillType.Heal, SkillType.Outside, SkillType.Return, SkillType.Revive, SkillType.Clear, SkillType.Repel, SkillType.StatIncrease, SkillType.Open};
        
        [JsonIgnore]
        public bool IsEncounterSkill => EncounterSkill.Contains(this.Type);
        
        private static readonly List<SkillType> EncounterSkill = new() {SkillType.Heal, SkillType.Damage, SkillType.Revive, SkillType.Dot, SkillType.Sleep, SkillType.Confusion, SkillType.StopSpell, SkillType.Buff, SkillType.Decrease, SkillType.Clear, SkillType.StatDecrease, SkillType.Steal};

        
        [JsonIgnore]
        public bool IsNonEncounterSkill => NonEncounterSkill.Contains(this.Type);

        public (string, bool) Do(IFighter target, IFighter source, BaseState targetObject, IGame game, int round, bool isMagic = false)
        {
            return this.Type switch
            {
                SkillType.Open => this.DoOpen(source, targetObject, game),
                SkillType.Heal => this.DoHeal(target),
                SkillType.Outside => DoOutside(game),
                SkillType.Damage => this.DoDamage(target, source, isMagic),
                SkillType.Repel => this.DoRepel(source, game, round),
                SkillType.Return => DoReturn(game),
                SkillType.Revive => this.DoHeal(target),
                SkillType.Buff => this.DoBuff(target, game, round,true),
                SkillType.Decrease => this.DoBuff(target, game, round,false),
                SkillType.StopSpell => this.DoStopSpell(target, game, round),
                SkillType.Sleep => this.DoSleep(target, game, round),
                SkillType.Confusion => this.DoConfusion(target, game, round),
                SkillType.Dot =>this.DoDot(target, game, round),
                SkillType.Steal => this.DoSteal(target, source, game),
                SkillType.Clear =>this.DoClearEffects(target),
                SkillType.StatDecrease => this.DoStat(target,false),
                SkillType.StatIncrease => this.DoStat(target,true),
                SkillType.None => ($"{source.Name} {this.EffectName}", false),
                _ => ("", false)
            };
        }

        private (string, bool) DoOpen(IFighter source, BaseState targetObject, IGame game)
        {
            if (targetObject.Type != SpriteType.Door)
            {
                return ("This is not a door", false);
            }

            var door = targetObject as ObjectState;

            if (door.IsOpen is true)
            {
                return ("The Door is already open", false);
            }

            game.Sounds.PlaySoundEffect("door");
            door.IsOpen = true;
            
            return ($"{source.Name} Is unable to Open Door\n", false);
        }

        private (string, bool) DoRepel(IFighter source, IGame game, int round)
        {
            if (game.Party.AliveMembers.Any(partyMember => partyMember.Status.Any(i => i.Type == EffectType.Repel)))
            {
                return ($"{source.Name} was not affected\n", false);
            }

            source.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.Repel,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : game.Party.StepCount
            });

            return ($"Enemies are {this.EffectName}\n", true);
        }

        private (string, bool) DoStopSpell(IFighter target, IGame game, int round)
        {
            if (target.Status.Any(i => i.Type == EffectType.StopSpell))
            {
                return ($"{target.Name} was not affected\n", false);
            }

            target.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.StopSpell,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : game.Party.StepCount
            });

            return ($"{target.Name} is {this.EffectName}\n", true);
        }

        private (string, bool) DoHeal(IFighter target)
        {
            var oldHeath = target.Health;
            if (this.StatRandom != 0)
            {
                target.Health += Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
            }
            else
            {
                target.Health = target.MaxHealth;
            }

            if (target.MaxHealth < target.Health)
            {
                target.Health = target.MaxHealth;
            }

            return (oldHeath == 0
                ? $"{target.Name} is revived and gains {target.Health} health\n"
                : $"{target.Name} gains {target.Health - oldHeath} health\n", true);
        }

        private (string, bool) DoClearEffects(IFighter target)
        {
            if (target.Status.Count(i => i.IsNegativeEffect) == 0)
            {
                return ($"{target.Name} is unaffected by {this.Name}\n", false);
            }

            var message = "";
            foreach (var effect in target.Status.Where(i => i.IsNegativeEffect).ToList())
            {
                message += $"{target.Name} {effect.Name} has ended\n";
                target.RemoveEffect(effect);
            }

            return (message, true);
        }

        private (string, bool) DoSteal(IFighter target, IFighter source, IGame game)
        {
            var message = this.DoAttack ?"and " : "";
            if (!source.CanHit(target)) 
                return ($"{source.Name} tried to steal from {target.Name} but did not find anything\n", false);
            Item item = null;
            if (target is Hero)
            {
                if (target.Items.Any() && Random.Chance(0.25f))
                {
                    var instance = target.Items[Random.NextInt(target.Items.Count)];
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
            else if( target is MonsterInstance )
            {
                item = game.CreateChestItem(source.Level);
            }

            if (item == null)
            {
                return  ($"{source.Name} tried to steal from {target.Name} but did not find anything\n", false);
            }
            
            if (item.Type == ItemType.Gold)
            {
                if (source is Hero)
                {
                    game.Party.Gold += item.Cost;
                }
                else if (source is MonsterInstance monster)
                {
                    monster.Gold += item.Cost;
                }
                else
                {
                    return  ($"{source.Name} tried to steal from {target.Name} but did not find anything\n", false);
                }
                
                game.Sounds.PlaySoundEffect("treasure");
                return (message + $"{source.Name} stole {item.Cost} Gold from {target.Name}.\n", true);
            }

            if (source is Hero)
            {
                var hero = game.Party.AddItem(new ItemInstance(item));
                if (hero == null)
                {
                    return ( $"{source.Name} tried to steal from {target.Name} but did not find anything\n", false);
                }
            }
            else
            {
                source.Items.Add(new ItemInstance(item));
            }

            return ( message + $"{source.Name} stole {item.Name} from {target.Name}.\n", true);
        }

        private (string, bool) DoDamage(IFighter target, IFighter source, bool isMagic)
        {
            var damage = target.CalculateDamage(Random.NextInt(source.Attack), IsPiercing, isMagic);
            var message = $"{target.Name}";
            
            if (damage <= 0)
            {
                damage = 0;
            }
            
            if (damage == 0)
            {
                message += " was unharmed\n";
                return (message, false);
            }

            if (this.StatType == StatType.Health)
            {
                target.Health -= damage;
                message += $" took {damage} points of damage from {this.EffectName}\n";
                message += target.HitCheck();
                if (target.Health <= 0)
                {
                    message += "and has died!\n";
                    target.Health = 0;
                }
            }
            else if (this.StatType == StatType.Magic)
            {
                target.Magic -= damage;
                message += $" lost {damage} points of magic from {this.EffectName}\n";
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
            var roll = Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
            var buff = increase ?  roll : target.CalculateDamage(roll, this.IsPiercing);
            if (buff == 0 || target.Status.Any(i => i.Type == EffectType.Buff && i.Name == this.EffectName))
            {
                return ($"{target.Name} was not affected by {this.Name}\n", false);
            }

            target.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.Buff,
                StatType = this.StatType,
                StatValue = increase ? buff : -buff,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
            });

            var changed = increase ? "increased" : "decreased";
            return ($"{target.Name} {changed} {this.StatType.ToString()} {buff} points\n", true);
        }

        private (string, bool) DoStat(IFighter target, bool increase)
        {
            var roll = Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
            var buff = increase ?  roll : target.CalculateDamage(roll, this.IsPiercing);
            var statValue = increase ? buff : -buff;
            
            switch (this.StatType)
            {
                case StatType.Health:
                    target.MaxHealth += statValue;
                    if (target.Health > target.MaxHealth)
                    {
                        target.Health = target.MaxHealth;
                    }
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
                    if (target.Magic > target.MaxMagic)
                    {
                        target.Magic = target.MaxMagic;
                    }
                    break;
            }
            
            var changed = increase ? "increased" : "decreased";
            return ($"{target.Name} permanently {changed} {this.StatType.ToString()} {buff} points\n", true);
        }

        private (string, bool) DoSleep(IFighter target, IGame gameState, int round)
        {
            if (target.Status.Any(i => i.Type == EffectType.Sleep))
            {
                return ($"{target.Name} was not affected by {this.Name}\n", false);
            }
            
            target.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.Sleep,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
            });

            return ($"{target.Name} is put to sleep\n", true);
        }

        private (string, bool) DoConfusion(IFighter target, IGame gameState, int round)
        {
            if (target.Status.Any(i => i.Type == EffectType.Confusion))
            {
                return ($"{target.Name} was not affected by {this.Name}\n", false);
            }
            
            target.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.Confusion,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
            });

            return ($"{target.Name} is {this.EffectName}\n", true);
        }

        private (string, bool) DoDot(IFighter target, IGame gameState, int round)
        {
            var buff = target.CalculateDamage(Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst));
            if (buff == 0 || target.Status.Any(i => i.Type == EffectType.OverTime && i.Name == this.EffectName))
            {
                return ($"{target.Name} was not affected by {this.Name}\n", false);
            }

            target.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.OverTime,
                StatType = this.StatType,
                StatValue = -buff,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
            });
            
            return ($"{target.Name} is {this.EffectName}\n", true);
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
    }
}