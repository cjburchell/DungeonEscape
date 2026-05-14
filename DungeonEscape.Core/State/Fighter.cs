using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Redpoint.DungeonEscape.State
{
    public abstract class Fighter : IFighter
    {
        public override string ToString()
        {
            return Name;
        }

        public string GetStats()
        {
            return Name + " (" + Level + ")\nH:" + Health + "/" + MaxHealth + " A:" + Attack + " D:" + Defence;
        }

        public string Name { get; set; }
        public int Health { get; set; }
        public int Magic { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int MagicDefence { get; set; }
        public int Agility { get; set; }

        [JsonIgnore]
        public IEnumerable<StatValue> Stats
        {
            get
            {
                return new List<StatValue>
                {
                    new StatValue { Value = MaxHealth, Type = StatType.Health },
                    new StatValue { Value = MaxMagic, Type = StatType.Magic },
                    new StatValue { Value = Attack, Type = StatType.Attack },
                    new StatValue { Value = Defence, Type = StatType.Defence },
                    new StatValue { Value = MagicDefence, Type = StatType.MagicDefence },
                    new StatValue { Value = Agility, Type = StatType.Agility }
                };
            }
        }

        public int MaxMagic { get; set; }
        public int MaxHealth { get; set; }
        public ulong Xp { get; set; }

        [JsonIgnore]
        public bool IsDead { get { return Health <= 0; } }

        public int Level { get; set; }

        [JsonIgnore]
        public bool RanAway { get; set; }

        public abstract IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells);
        public abstract IEnumerable<Skill> GetSkills(IEnumerable<Skill> skills);

        public List<StatusEffect> Status { get; set; }
        public List<ItemInstance> Items { get; set; }

        [JsonIgnore]
        public int CriticalAttack { get { return Attack + (20 * Level); } }

        protected Fighter()
        {
            Status = new List<StatusEffect>();
            Items = new List<ItemInstance>();
        }

        public void RemoveEffect(StatusEffect effect)
        {
            if (effect.Type == EffectType.Buff)
            {
                ApplyBuff(effect, -effect.StatValue);
            }

            Status.Remove(effect);
        }

        public void AddEffect(StatusEffect effect)
        {
            if (effect.Type == EffectType.Buff)
            {
                ApplyBuff(effect, effect.StatValue);
            }

            Status.Add(effect);
        }

        private void ApplyBuff(StatusEffect effect, int value)
        {
            switch (effect.StatType)
            {
                case StatType.Health:
                    MaxHealth += value;
                    if (Health > MaxHealth)
                    {
                        Health = MaxHealth;
                    }
                    break;
                case StatType.Attack:
                    Attack += value;
                    break;
                case StatType.Defence:
                    Defence += value;
                    break;
                case StatType.MagicDefence:
                    MagicDefence += value;
                    break;
                case StatType.Agility:
                    Agility += value;
                    break;
                case StatType.Magic:
                    MaxMagic += value;
                    if (Magic > MaxMagic)
                    {
                        Magic = MaxMagic;
                    }
                    break;
            }
        }

        public virtual void Equip(ItemInstance item)
        {
        }

        public virtual List<string> GetEquipmentId(IEnumerable<Slot> slots)
        {
            return new List<string>();
        }

        public string CheckForExpiredStates(int round, DurationType durationType)
        {
            var message = "";
            var expiredList = Status.FindAll(i => i.DurationType == durationType && i.Duration <= round - i.StartTime);
            foreach (var expired in expiredList)
            {
                message += expired.Type != EffectType.Repel
                    ? expired.Name + " on " + Name + " has worn off\n"
                    : expired.Name + " has worn off\n";

                RemoveEffect(expired);
            }

            return message;
        }

        public string UpdateStatusEffects(IGame game)
        {
            var message = "";
            if (Status.Any(i => i.Type == EffectType.Sleep))
            {
                message += Name + " is asleep\n";
            }

            if (Status.Any(i => i.Type == EffectType.Confusion))
            {
                message += Name + " is confused\n";
            }

            foreach (var effect in Status.FindAll(i => i.Type == EffectType.OverTime))
            {
                switch (effect.StatType)
                {
                    case StatType.Health:
                        if (effect.StatValue > 0)
                        {
                            message += Name + " gained " + effect.StatValue + " points of health\n";
                            Health += effect.StatValue;
                        }
                        else
                        {
                            var defence = (100 - Math.Min(MagicDefence, 99)) / 100f;
                            var damage = Math.Min((int)(effect.StatValue * defence), -1);
                            message += Name + " took " + (-damage) + " points of damage\n";
                            Health += damage;
                            PlayDamageAnimation();
                            if (game != null && game.Sounds != null)
                            {
                                game.Sounds.PlaySoundEffect("receive-damage");
                            }
                        }

                        if (IsDead)
                        {
                            message += "and has died!\n";
                            Health = 0;
                        }

                        if (Health > MaxHealth)
                        {
                            Health = MaxHealth;
                        }
                        break;
                    case StatType.Magic:
                        if (effect.StatValue > 0)
                        {
                            message += Name + " gained " + effect.StatValue + " points of magic\n";
                            Magic += effect.StatValue;
                        }
                        else
                        {
                            var defence = (100 - Math.Min(MagicDefence, 99)) / 100f;
                            var damage = Math.Min((int)(effect.StatValue * defence), -1);
                            message += Name + " lost " + (-damage) + " points of magic\n";
                            Magic += damage;
                            PlayDamageAnimation();
                            if (game != null && game.Sounds != null)
                            {
                                game.Sounds.PlaySoundEffect("receive-damage");
                            }
                        }

                        if (Magic <= 0)
                        {
                            Magic = 0;
                        }

                        if (Magic > MaxMagic)
                        {
                            Magic = MaxMagic;
                        }
                        break;
                }
            }

            return message;
        }

        public void PlayDamageAnimation()
        {
        }

        public bool CanHit(IFighter target)
        {
            var roll = Dice.RollD20();
            return roll == 20 || (Agility - target.Agility) / 100 * 10 + roll > 4;
        }

        public bool CanCriticalHit(IFighter target)
        {
            var roll = Dice.RollD100();
            return roll >= 95 || (Agility - target.Agility) / 100 * 50 + roll > 90;
        }

        public int CalculateDamage(int attack, bool isPiercing = false, bool isMagic = false)
        {
            if (isPiercing)
            {
                return attack;
            }

            var defence = (100 - Math.Min(isMagic ? MagicDefence : Defence, 99)) / 100f;
            return attack != 0 ? Math.Max((int)(attack * defence), 10) : 0;
        }

        public string HitCheck()
        {
            if (IsDead)
            {
                return "";
            }

            var message = "";
            var sleepEffect = Status.FirstOrDefault(i => i.Type == EffectType.Sleep);
            if (sleepEffect != null && Dice.RollD20() > 15)
            {
                message += sleepEffect.Name + " on " + Name + " has worn off\n";
                RemoveEffect(sleepEffect);
            }

            return message;
        }

        public void Update()
        {
        }
    }
}
