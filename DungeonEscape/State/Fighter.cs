// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Nez.Sprites;
    using Nez.UI;

    public abstract class Fighter : IFighter
    {
        public override string ToString()
        {
            return this.Name;
        }
        
        public string Name { get; set; }
        public int Health { get; set; }
        public int Magic { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int Agility { get; set; }
        public int MaxMagic { get; set; }
        
        public ulong Xp { get; set; }
        
        [JsonIgnore]
        public bool IsDead => this.Health <= 0;
        
        public int Level { get; set; }
        public int MaxHealth { get; set; }
        
        [JsonIgnore]
        public bool RanAway { get; set; }

        public abstract IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells);

        public List<StatusEffect> Status { get; set; } = new List<StatusEffect>();

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

        public virtual void Equip(ItemInstance item){}
        public virtual string GetEquipmentId(ItemType itemType)
        {
            return null;
        }

        public string UpdateStatusEffects(int round, DurationType durationType, IGame game)
        {
            var message = "";
            var expiredList =
                this.Status.FindAll(i => i.DurationType == durationType && i.Duration <= round - i.StartTime);
            foreach (var expired in expiredList)
            {
                message += $"{expired.Name} on {this.Name} has worn off\n";
                this.RemoveEffect(expired);
            }

            if (this.Status.Count(i => i.Type == EffectType.Sleep) != 0)
            {
                message += $"{this.Name} is asleep\n";
            }
            
            if (this.Status.Count(i => i.Type == EffectType.Confusion) != 0)
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
                        }
                        else
                        {
                            message += $"{this.Name} took {-effect.StatValue} points of damage\n";
                            this.PlayDamageAnimation();
                            game.Sounds.PlaySoundEffect("receive-damage");
                        }
                        
                        this.Health += effect.StatValue;
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
                        }
                        else
                        {
                            message += $"{this.Name} lost {-effect.StatValue} points of magic\n";
                            this.PlayDamageAnimation();
                            game.Sounds.PlaySoundEffect("receive-damage");
                        }
                        
                        this.Magic += effect.StatValue;
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

        public void Update()
        {
            this.Animator?.Update();
            if (this.Animator != null)
            {
                this.Image?.SetSprite(this.Animator.Sprite);
            }
        }

        public void Use(ItemInstance item)
        {
            this.Agility += item.Agility;
            this.Attack += item.Attack;
            this.Defence += item.Defence;
            this.Health += item.Health;
            if (this.Health > this.MaxHealth)
            {
                this.Health = this.MaxHealth;
            }
        }
    }
}