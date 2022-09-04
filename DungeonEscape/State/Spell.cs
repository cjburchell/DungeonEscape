// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Nez.Textures;
    using Nez.Tiled;

    public class Spell : SkillBase
    {
        private static readonly List<SkillType> AttackSpells = new() {SkillType.Damage, SkillType.Dot, SkillType.Sleep, SkillType.Confusion, SkillType.StopSpell, SkillType.Decrease};

        private static readonly List<SkillType> EncounterSpells = new() {SkillType.Heal, SkillType.Damage, SkillType.Revive, SkillType.Dot, SkillType.Sleep, SkillType.Confusion, SkillType.StopSpell, SkillType.Buff, SkillType.Decrease, SkillType.Clear};

        private static readonly List<SkillType> NonEncounterSpells = new() {SkillType.Heal, SkillType.Outside, SkillType.Return, SkillType.Revive, SkillType.Clear};
        
        public int ImageId { get; set; }

        public void Setup(TmxTileset tileset)
        {
            this.Image = tileset.Image != null ? new Sprite(tileset.Image.Texture, tileset.TileRegions[this.ImageId]) : new Sprite(tileset.Tiles[this.ImageId].Image.Texture);
        }

        [JsonIgnore]
        public bool IsNonEncounterSpell => NonEncounterSpells.Contains(this.Type);

        [JsonIgnore]
        public bool IsEncounterSpell => EncounterSpells.Contains(this.Type);

        [JsonIgnore]
        public bool IsAttackSpell => AttackSpells.Contains(this.Type);
        
        
        public int Cost { get; set; }

        public int MinLevel { get; set; }
        
        
        [JsonProperty("Classes", ItemConverterType=typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }
        
        [JsonIgnore]
        public Sprite Image { get; private set; }
        
        public string Cast(IEnumerable<IFighter> targets, IFighter caster, IGame game, int round = 0)
        {
            if (caster.Magic < this.Cost)
            {
                return $"{caster.Name}: I do not have enough magic to cast {this.Name}.";
            }

            caster.Magic -= this.Cost;

            return this.Type switch
            {
                SkillType.Heal => this.CastHeal(targets, caster, false, game),
                SkillType.Outside => this.CastOutside(caster as Hero, game),
                SkillType.Damage => this.CastDamage(targets, caster, game),
                SkillType.Return => this.CastReturn(caster as Hero, game),
                SkillType.Revive => this.CastHeal(targets, caster, true, game),
                SkillType.Buff => this.CastBuff(targets, caster, game, round,true),
                SkillType.Decrease => this.CastBuff(targets, caster, game, round,false),
                SkillType.StopSpell => this.CastStopSpell(targets, caster, game, round),
                SkillType.Sleep => this.CastSleep(targets, caster, game, round),
                SkillType.Confusion => this.CastConfusion(targets, caster, game, round),
                SkillType.Dot =>this.CastDot(targets, caster, game, round),
                SkillType.Clear =>this.CastClearEffects(targets, caster, game),
                _ => $"{caster.Name} casts {this.Name} but it did not work"
            };
        }
        
        private string CastClearEffects(IEnumerable<IFighter> targets, IFighter caster, IGame gameState)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                if (target.Status.Count == 0)
                {
                    message += $"{target.Name} is unaffected by {this.Name}\n";
                }
                else
                {
                    foreach (var effect in target.Status.ToList())
                    {
                        message += $"{target.Name} {effect.Name} has ended\n";
                        target.RemoveEffect(effect);
                    }
                }
            }

            return message;
        }

        private string CastDot(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i => !i.IsDead && !i.RanAway))
            {
                if (caster.CanHit(target))
                {
                    var buff = target.CalculateDamage(Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst), this.IsPiercing);
                    if (buff == 0)
                    {
                        message += $"{target.Name} was not affected\n";
                    }
                    else
                    {
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

                        message += $"{target.Name} is {this.EffectName}\n";
                    }
                }
                else
                {
                    message += $"{target.Name} dodges the spell\n";
                }
            }

            return message;
        }

        private string CastStopSpell(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                if (caster.CanHit(target))
                {
                    target.AddEffect(new StatusEffect
                    {
                        Name = this.EffectName,
                        Type = EffectType.StopSpell,
                        Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                        DurationType = this.DurationType,
                        StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
                    });

                    message += $"{target.Name} is {this.EffectName}\n";
                }
                else
                {
                    message += $"{target.Name} dodges the spell\n";
                }
            }

            return message;
        }

        private string CastConfusion(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                if (caster.CanHit(target))
                {
                    target.AddEffect(new StatusEffect
                    {
                        Name = this.EffectName,
                        Type = EffectType.Confusion,
                        Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                        DurationType = this.DurationType,
                        StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
                    });

                    message += $"{target.Name} is {this.EffectName}\n";
                }
                else
                {
                    message += $"{target.Name} dodges the spell\n";
                }
               
            }

            return message;
        }

        private string CastSleep(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                if (caster.CanHit(target))
                {
                    target.AddEffect(new StatusEffect
                    {
                        Name = this.EffectName,
                        Type = EffectType.Sleep,
                        Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                        DurationType = this.DurationType,
                        StartTime = this.DurationType == DurationType.Rounds ? round : gameState.Party.StepCount
                    });

                    message += $"{target.Name} is put to sleep\n";
                }
                else
                {
                    message += $"{target.Name} dodges the spell\n";
                }
                
            }

            return message;
        }

        private string CastBuff(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round, bool increase)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                if (caster.CanHit(target))
                {
                    var roll = Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
                    var buff = increase ?  roll : target.CalculateDamage(roll, this.IsPiercing);
                    if (buff == 0)
                    {
                        message += $"{target.Name} was not affected by {this.Name}\n";
                    }
                    else
                    {
                        target.AddEffect(new StatusEffect
                        {
                            Name = this.EffectName,
                            Type = EffectType.Buff,
                            StatType = this.StatType,
                            StatValue = increase? buff: -buff,
                            Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                            DurationType = this.DurationType,
                            StartTime = this.DurationType == DurationType.Rounds? round: gameState.Party.StepCount
                        });

                        var changed = increase ? "increased" : "decreased";
                        message += $"{target.Name} {changed} {this.StatType.ToString()} {buff} points\n";
                    }
                }
                else
                {
                    message += $"{target.Name} dodges the spell\n";
                }
            }

            return message;
        }
        
        private string CastDamage(IEnumerable<IFighter> targets, IFighter caster, IGame gameState)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            var hit = false;
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                var damage = 0;
                if (caster.CanHit(target))
                {
                    damage = target.CalculateDamage(Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst), this.IsPiercing);
                }
                else
                {
                    message += " dodges the spell and";
                }
                
                
                if (damage == 0)
                {
                    message += " was unharmed\n";
                }
                else
                {
                    hit = true;
                    target.PlayDamageAnimation();
                    if (this.StatType == StatType.Health)
                    {
                        target.Health -= damage;
                        message += $" took {damage} points of damage\n";
                    }
                    else if (this.StatType == StatType.Magic)
                    {
                        target.Magic -= damage;
                        message += $" lost {damage} points of magic\n";
                        if (target.Magic < 0)
                        {
                            target.Magic = 0;
                        }
                    }
                }

                if (target.Health <= 0)
                {
                    message += "and has died!\n";
                }
            }

            gameState.Sounds.PlaySoundEffect(hit? "receive-damage" : "miss" );

            return message;
        }

        private string CastHeal(IEnumerable<IFighter> targets, IFighter caster, bool everyone, IGame gameState)
        {
            gameState.Sounds.PlaySoundEffect("spell");
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(item => (everyone || !item.IsDead) && !item.RanAway))
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

                if (oldHeath == 0)
                {
                    message += $"{target.Name} is revived and gains {target.Health - oldHeath} health\n";
                }
                else
                {
                    message += $"{target.Name} gains {target.Health - oldHeath} health\n";
                }
            }

            return message;
        }

        private string CastOutside(IFighter caster, IGame gameState)
        {
            if (gameState.Party.CurrentMapIsOverWorld)
            {
                return $"{caster.Name} casts {this.Name} but you are already outside";
            }

            gameState.Sounds.PlaySoundEffect("spell");
            gameState.SetMap();
            return null;
        }

        private string CastReturn(IFighter caster, IGame gameState)
        {
            if (!gameState.Party.CurrentMapIsOverWorld)
            {
                return $"{caster.Name} casts {this.Name} but you are not outside";
            }

            if (!gameState.Party.SavedMapId.HasValue)
            {
                return $"{caster.Name} casts {this.Name} but you have never saved your game";
            }

            gameState.Sounds.PlaySoundEffect("spell");
            gameState.SetMap(gameState.Party.SavedMapId, null, gameState.Party.SavedPoint);
            return null;
        }
    }
}