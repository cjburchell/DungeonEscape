// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeProtected.Global

using System.Linq;

namespace Redpoint.DungeonEscape.State
{
    using Nez;
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Skill : SkillBase
    {
        public bool DoAttack { get; set; }
        
        public string Do(IFighter target, IFighter source, IGame game, int round)
        {
            return this.Type switch
            {
                SkillType.Damage => this.DoDamage(target, source),
                SkillType.Decrease => this.DoBuff(target, game, round,false),
                SkillType.Sleep => this.DoSleep(target, game, round),
                SkillType.Confusion => this.DoConfusion(target, game, round),
                SkillType.Dot =>this.DoDot(target, game, round),
                SkillType.Steal => this.DoSteal(target, source, game),
                SkillType.None => $"{source.Name} {this.EffectName}",
                _ => ""
            };
        }

        private string DoSteal(IFighter target, IFighter source, IGame game)
        {
            var message = this.DoAttack ?"and " : "";
            if (!source.CanHit(target)) 
                return this.DoAttack ? "" : $"{source.Name} tried to steal from {target.Name} but did not find anything\n";
            Item item = null;
            if (target is Hero)
            {
                if (target.Items.Any() && Random.Chance(0.25f))
                {
                    var instance = target.Items[Random.NextInt(target.Items.Count)];
                    item = instance.Item;
                    if (instance.IsEquipped)
                    {
                        instance.UnEquip(game.Party.Members);
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
                return this.DoAttack ? "" : $"{source.Name} tried to steal from {target.Name} but did not find anything\n";
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
                    return this.DoAttack ? "" : $"{source.Name} tried to steal from {target.Name} but did not find anything\n";
                }
                
                game.Sounds.PlaySoundEffect("treasure");
                return message + $"{source.Name} stole {item.Cost} Gold from {target.Name}.\n";
            }

            if (source is Hero)
            {
                var hero = game.Party.AddItem(new ItemInstance(item));
                if (hero == null)
                {
                    return this.DoAttack ? "" : $"{source.Name} tried to steal from {target.Name} but did not find anything\n";
                }
            }
            else
            {
                source.Items.Add(new ItemInstance(item));
            }

            return message + $"{source.Name} stole {item.Name} from {target.Name}.\n";
        }

        private string DoDamage(IFighter target, IFighter source)
        {
            var message = this.DoAttack?"": $"{source.Name} attacks {target.Name} with {this.EffectName}.\n";
            var damage = 0;
            if (source.CanHit(target))
            {
                var attack = Random.NextInt(source.Attack);
                damage = target.CalculateDamage(attack);
                message += $"{target.Name}";
            }
            else
            {
                message += $"{target.Name} dodges the {this.EffectName} attack and";
            }

            if (damage <= 0)
            {
                damage = 0;
            }
            
            if (damage == 0)
            {
                message += " was unharmed\n";
                if (this.DoAttack)
                {
                    return "";
                }
            }
            else
            {
                if (this.StatType == StatType.Health)
                {
                    target.Health -= damage;
                    message += $" took {damage} points of damage from {this.EffectName}\n";
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
            }

            if (target.Health <= 0)
            {
                message += "and has died!\n";
                target.Health = 0;
            }

            return message;
        }

        private string DoBuff(IFighter target, IGame gameState, int round, bool increase)
        {
            var roll = Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
            var buff = increase ?  roll : target.CalculateDamage(roll, this.IsPiercing);
            if (buff == 0 || target.Status.Any(i => i.Type == EffectType.Buff && i.Name == this.EffectName))
            {
                return this.DoAttack?"":$"{target.Name} was not affected by {this.Name}\n";
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
            return $"{target.Name} {changed} {this.StatType.ToString()} {buff} points\n";
        }

        private string DoSleep(IFighter target, IGame gameState, int round)
        {
            if (target.Status.Any(i => i.Type == EffectType.Sleep))
            {
                return this.DoAttack?"":$"{target.Name} was not affected by {this.Name}\n";
            }
            
            target.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.Sleep,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
            });

            return $"{target.Name} is put to sleep\n";
        }

        private string DoConfusion(IFighter target, IGame gameState, int round)
        {
            if (target.Status.Any(i => i.Type == EffectType.Confusion))
            {
                return this.DoAttack?"":$"{target.Name} was not affected by {this.Name}\n";
            }
            
            target.AddEffect(new StatusEffect
            {
                Name = this.EffectName,
                Type = EffectType.Confusion,
                Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                DurationType = this.DurationType,
                StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
            });

            return $"{target.Name} is {this.EffectName}\n";
        }

        private string DoDot(IFighter target, IGame gameState, int round)
        {
            var buff = target.CalculateDamage(Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst));
            if (buff == 0 || target.Status.Any(i => i.Type == EffectType.OverTime && i.Name == this.EffectName))
            {
                return this.DoAttack?"":$"{target.Name} was not affected by {this.Name}\n";
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
            
            return $"{target.Name} is {this.EffectName}\n";
        }
    }
}