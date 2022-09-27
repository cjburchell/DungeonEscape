// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace Redpoint.DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Nez.Sprites;
    using Nez.UI;

    public abstract class Fighter : IFighter
    {
        public override string ToString()
        {
            return $"{this.Name} L:{this.Level} H: {this.Health}/{this.MaxHealth} A:{this.Attack} D:{this.Defence}";
        }
        
        public string Name { get; set; }
        
        public int Health { get; set; }
        public int Magic { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int MagicDefence { get; set; }
        public int Agility { get; set; }

        [JsonIgnore]
        public IEnumerable<StatValue> Stats =>
            new List<StatValue>
            {
                new()
                {
                    Value = this.MaxHealth,
                    Type = StatType.Health
                },
                new()
                {
                    Value = this.MaxMagic,
                    Type = StatType.Magic
                },
                new()
                {
                    Value = this.Attack,
                    Type = StatType.Attack
                },
                new()
                {
                    Value = this.Defence,
                    Type = StatType.Defence
                },
                new()
                {
                    Value = this.MagicDefence,
                    Type = StatType.MagicDefence
                },
                new()
                {
                    Value = this.Agility,
                    Type = StatType.Agility
                }
            };

        public int MaxMagic { get; set; }
        public int MaxHealth { get; set; }
        
        public ulong Xp { get; set; }
        
        [JsonIgnore]
        public bool IsDead => this.Health <= 0;
        
        public int Level { get; set; }

        [JsonIgnore]
        public bool RanAway { get; set; }

        public abstract IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells);
        
        public abstract IEnumerable<Skill> GetSkills(IEnumerable<Skill> skills);

        public List<StatusEffect> Status { get; set; } = new();
        
        public List<ItemInstance> Items { get; set; } = new();

        public void RemoveEffect(StatusEffect effect)
        {
            if (effect.Type == EffectType.Buff)
            {
                switch (effect.StatType)
                {
                    case StatType.Health:
                        this.MaxHealth -= effect.StatValue;
                        if (this.Health > this.MaxHealth)
                        {
                            this.Health = this.MaxHealth;
                        }
                        break;
                    case StatType.Attack:
                        this.Attack -= effect.StatValue;
                        break;
                    case StatType.Defence:
                        this.Defence -= effect.StatValue;
                        break;
                    case StatType.MagicDefence:
                        this.MagicDefence -= effect.StatValue;
                        break;
                    case StatType.Agility:
                        this.Agility -= effect.StatValue;
                        break;
                    case StatType.Magic:
                        this.MaxMagic -= effect.StatValue;
                        if (this.Magic > this.MaxMagic)
                        {
                            this.Magic = this.MaxMagic;
                        }
                        break;
                }
            }
            
            this.Status.Remove(effect);
        }

        [JsonIgnore] public int CriticalAttack => this.Attack + (20 * this.Level);

        public void AddEffect(StatusEffect effect)
        {
            if (effect.Type == EffectType.Buff)
            {
                switch (effect.StatType)
                {
                    case StatType.Health:
                        this.MaxHealth += effect.StatValue;
                        if (this.Health > this.MaxHealth)
                        {
                            this.Health = this.MaxHealth;
                        }
                        break;
                    case StatType.Attack:
                        this.Attack += effect.StatValue;
                        break;
                    case StatType.Defence:
                        this.Defence += effect.StatValue;
                        break;
                    case StatType.MagicDefence:
                        this.MagicDefence += effect.StatValue;
                        break;
                    case StatType.Agility:
                        this.Agility += effect.StatValue;
                        break;
                    case StatType.Magic:
                        this.MaxMagic += effect.StatValue;
                        if (this.Magic > this.MaxMagic)
                        {
                            this.Magic = this.MaxMagic;
                        }
                        break;
                }
            }
            
            this.Status.Add(effect);
        }

        public virtual void Equip(ItemInstance item) { }
        
        public virtual List<string> GetEquipmentId(IEnumerable<Slot> slots)
        {
            return new List<string>();
        }

        public string CheckForExpiredStates(int round, DurationType durationType)
        {
            var message = "";
            var expiredList =
                this.Status.FindAll(i => i.DurationType == durationType && i.Duration <= round - i.StartTime);
            foreach (var expired in expiredList)
            {
                if (expired.Type != EffectType.Repel)
                {
                    message += $"{expired.Name} on {this.Name} has worn off\n";
                }
                else
                {
                    message += $"{expired.Name} has worn off\n";
                }
               
                this.RemoveEffect(expired);
            }

            return message;
        }

        public string UpdateStatusEffects(IGame game)
        {
            var message = "";
            if (Status.Any(i => i.Type == EffectType.Sleep))
            {
                message += $"{this.Name} is asleep\n";
            }
            
            if (Status.Any(i => i.Type == EffectType.Confusion))
            {
                message += $"{this.Name} is confused\n";
            }

            foreach (var effect in this.Status.FindAll(i => i.Type == EffectType.OverTime))
            {
                switch (effect.StatType)
                {
                    case StatType.Health:
                        if (effect.StatValue > 0)
                        {
                            message += $"{this.Name} gained {effect.StatValue} points of health\n";
                            this.Health += effect.StatValue;
                        }
                        else
                        {
                            var defence = (100 - Math.Min(this.MagicDefence, 99)) / 100f;
                            var damage = Math.Min((int)(effect.StatValue * defence), -1);
                            message += $"{this.Name} took {-damage} points of damage\n";
                            this.Health += damage;
                            this.PlayDamageAnimation();
                            game.Sounds.PlaySoundEffect("receive-damage");
                        }
                        
                        if (this.IsDead)
                        {
                            message += "and has died!\n";
                            this.Health = 0;
                        }
                        
                        if (this.Health > this.MaxHealth)
                        {
                            this.Health = this.MaxHealth;
                        }
                        
                        break;
                    case StatType.Magic:
                        if (effect.StatValue > 0)
                        {
                            message += $"{this.Name} gained {effect.StatValue} points of magic\n";
                            this.Magic += effect.StatValue;
                        }
                        else
                        {
                            
                            var defence = (100 - Math.Min(this.MagicDefence, 99)) / 100f;
                            var damage = Math.Min((int)(effect.StatValue * defence), -1);
                            message += $"{this.Name} lost {-damage} points of magic\n";
                            this.Magic += damage;
                            this.PlayDamageAnimation();
                            game.Sounds.PlaySoundEffect("receive-damage");
                        }
                        
                        if (this.Magic <= 0)
                        {
                            this.Magic = 0;
                        }
                        
                        if (this.Magic > this.MaxMagic)
                        {
                            this.Magic = this.MaxMagic;
                        }
                        break;
                }
            }

            return message;
        }
        
        [JsonIgnore] protected SpriteAnimator Animator { get; set; }
        
        [JsonIgnore] public Image Image { get; protected set; } = new Image();

        public void PlayDamageAnimation()
        {
            this.Animator.Play("Damage", SpriteAnimator.LoopMode.Once);
        }

        public bool CanHit(IFighter target)
        {
            var roll = Dice.RollD20();
            return roll == 20 || (this.Agility-target.Agility)/100 * 10 + roll > 4;
        }

        public bool CanCriticalHit(IFighter target)
        {
            var roll = Dice.RollD100();
            return roll >= 95 || (this.Agility-target.Agility)/100 * 50 + roll > 90;
        }

        public int CalculateDamage(int attack, bool isPiercing = false, bool isMagic = false)
        {
            if (isPiercing)
            {
                return attack;
            }
            
            var defence = (100 - Math.Min(isMagic?this.MagicDefence:this.Defence, 99)) / 100f;
            return attack != 0 ? Math.Max((int)(attack * defence), 10): 0;
        }

        public string HitCheck()
        {
            if (this.IsDead)
            {
                return "";
            }
            
            var message = "";
            var sleepEffect = this.Status.FirstOrDefault(i => i.Type == EffectType.Sleep);
            if (sleepEffect != null && Dice.RollD20() > 15)
            {
                message += $"{sleepEffect.Name} on {this.Name} has worn off\n";
                this.RemoveEffect(sleepEffect);
            }

            return message;
        }

        public void Update()
        {
            this.Animator?.Update();
            if (this.Animator != null)
            {
                this.Image?.SetSprite(this.Animator.Sprite);
            }
        }
    }
}