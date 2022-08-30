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

    public class Spell
    {
        public string Cast(IEnumerable<IFighter> targets, IFighter caster, IGame game, int round = 0)
        {
            if (caster.Magic < this.Cost)
            {
                return $"{caster.Name}: I do not have enough magic to cast {this.Name}.";
            }

            caster.Magic -= this.Cost;

            return this.Type switch
            {
                SpellType.Heal => this.CastHeal(targets, caster, false, game),
                SpellType.Outside => this.CastOutside(caster as Hero, game),
                SpellType.Damage => this.CastDamage(targets, caster, game),
                SpellType.Return => this.CastReturn(caster as Hero, game),
                SpellType.Revive => this.CastHeal(targets, caster, true, game),
                SpellType.Buff => this.CastBuff(targets, caster, game, round,true),
                SpellType.Decrease => this.CastBuff(targets, caster, game, round,false),
                SpellType.StopSpell => this.CastStopSpell(targets, caster, game, round),
                SpellType.Sleep => this.CastSleep(targets, caster, game, round),
                SpellType.Confusion => this.CastConfusion(targets, caster, game, round),
                SpellType.Dot =>this.CastDot(targets, caster, game, round),
                SpellType.Clear =>this.CastClearEffects(targets, caster, game),
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
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                var buff = Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
                if (buff == 0)
                {
                    message += $"{target.Name} was not effected\n";
                }
                else
                {
                    target.AddEffect(new StatusEffect
                    {
                        Name = this.EffectName,
                        Type = EffectType.OverTime,
                        StatType = this.StatType,
                        StatValue =  -buff,
                        Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                        DurationType = this.DurationType,
                        StartTime = this.DurationType == DurationType.Rounds? round:gameState.Party.StepCount 
                    });
                    
                    message += $"{target.Name} is {this.EffectName}\n";
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
                target.AddEffect(new StatusEffect
                {
                    Name = this.EffectName,
                    Type = EffectType.StopSpell,
                    Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                    DurationType = this.DurationType,
                    StartTime = this.DurationType == DurationType.Rounds? round: gameState.Party.StepCount
                });
                
                message += $"{target.Name} is {this.EffectName}\n";
            }

            return message;
        }

        private string CastConfusion(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                target.AddEffect(new StatusEffect
                {
                    Name = this.EffectName,
                    Type = EffectType.Confusion,
                    Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                    DurationType = this.DurationType,
                    StartTime = this.DurationType == DurationType.Rounds? round: gameState.Party.StepCount
                });
                
                message += $"{target.Name} is {this.EffectName}\n";
            }

            return message;
        }

        private string CastSleep(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                target.AddEffect(new StatusEffect
                {
                    Name = this.EffectName,
                    Type = EffectType.Sleep,
                    Duration = Dice.Roll(this.DurationRandom, this.DurationTimes, this.DurationConst),
                    DurationType = this.DurationType,
                    StartTime = this.DurationType == DurationType.Rounds? round: gameState.Party.StepCount
                });
                
                message += $"{target.Name} is put to sleep\n";
            }

            return message;
        }

        private string CastBuff(IEnumerable<IFighter> targets, IFighter caster, IGame gameState, int round, bool increase)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                var buff = Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
                if (buff == 0)
                {
                    message += $"{target.Name} was not effected by {this.Name}\n";
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

            return message;
        }
        
        private string CastDamage(IEnumerable<IFighter> targets, IFighter caster, IGame gameState)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            var totalDamage = 0;
            foreach (var target in targets.Where(i=> !i.IsDead && !i.RanAway ))
            {
                var damage = Dice.Roll(this.StatRandom, this.StatTimes, this.StatConst);
                totalDamage += damage;
                target.Health -= damage;
                if (damage == 0)
                {
                    message += $"{target.Name} was unharmed\n";
                }
                else
                {
                    target.PlayDamageAnimation();
                    message += $"{target.Name} took {damage} points of damage\n";
                }

                if (target.Health <= 0)
                {
                    message += "and has died!\n";
                }
            }

            gameState.Sounds.PlaySoundEffect(totalDamage == 0 ? "miss" : "receive-damage");

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

        private static readonly List<SpellType> AttackSpells = new List<SpellType> {SpellType.Damage, SpellType.Dot, SpellType.Sleep, SpellType.Confusion, SpellType.StopSpell, SpellType.Decrease};

        private static readonly List<SpellType> EncounterSpells = new List<SpellType>
            {SpellType.Heal, SpellType.Damage, SpellType.Revive, SpellType.Dot, SpellType.Sleep, SpellType.Confusion, SpellType.StopSpell, SpellType.Buff, SpellType.Decrease, SpellType.Clear};

        private static readonly List<SpellType> NonEncounterSpells = new List<SpellType>
            {SpellType.Heal, SpellType.Outside, SpellType.Return, SpellType.Revive, SpellType.Clear};

        public override string ToString()
        {
            return this.Name;
        }

        public int DurationTimes { get; set; } = 1;
        
        public int DurationRandom { get; set; }

        public int DurationConst { get; set; }
        
        public int ImageId { get; set; }
        
        public int Id { get; set; }

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

        [JsonConverter(typeof(StringEnumConverter))]
        public Target Targets { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public DurationType DurationType { get; set; }
        
        public string EffectName { get; set; }

        public int Cost { get; set; }

        public int MinLevel { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SpellType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public StatType StatType { get; set; } = StatType.None;
        
        [JsonProperty("Classes", ItemConverterType=typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }

        public int StatTimes { get; set; } = 1;
        
        public int StatRandom { get; set; }

        public int StatConst { get; set; }

        public string Name { get; set; }
        
        [JsonIgnore]
        public Sprite Image { get; private set; }
    }
}