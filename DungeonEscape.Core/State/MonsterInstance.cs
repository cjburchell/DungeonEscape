using System.Collections.Generic;
using System.Linq;

namespace Redpoint.DungeonEscape.State
{
    public class MonsterInstance : Fighter
    {
        private readonly Monster info;

        public MonsterInstance(Monster info, IGame gameState)
        {
            this.info = info;
            Health = Dice.Roll(info.HealthRandom, info.HealthTimes, info.HealthConst);
            MaxHealth = Health;
            Magic = Dice.Roll(info.MagicRandom, info.MagicTimes, info.MagicConst);
            MaxMagic = Magic;
            Attack = info.Attack;
            Defence = info.Defence;
            MagicDefence = info.MagicDefence;
            Agility = info.Agility;
            Name = info.Name;
            Level = info.MinLevel;
            Xp = info.Xp;
            Gold = info.Gold;

            if (gameState != null)
            {
                foreach (var item in info.Items.Select(gameState.GetCustomItem).Where(item => item != null))
                {
                    Items.Add(new ItemInstance(item));
                }
            }
        }

        public Rarity Rarity { get { return info.Rarity; } }
        public int Gold { get; set; }

        public override IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return info.SpellList.Select(spellId => availableSpells.FirstOrDefault(item => item.Name == spellId))
                .Where(spell => spell != null).ToList();
        }

        public override IEnumerable<Skill> GetSkills(IEnumerable<Skill> availableSkills)
        {
            return info.SkillList.Select(id => availableSkills.FirstOrDefault(item => item.Name == id))
                .Where(skill => skill != null).ToList();
        }
    }
}
