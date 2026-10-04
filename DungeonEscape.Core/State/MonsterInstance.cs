using Redpoint.DungeonEscape.Data;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Rules;

namespace Redpoint.DungeonEscape.State
{
    public class MonsterInstance : Fighter
    {
        private readonly Monster _info;

        public MonsterInstance(Monster info, IGame gameState)
        {
            _info = info;
            Health = DndStatRules.GetMonsterHitPoints(info, null);
            MaxHealth = Health;
            Magic = Dice.Roll(info.MagicRandom, info.MagicTimes, info.MagicConst);
            MaxMagic = Magic;
            Attack = GetLegacyAttack(info);
            Defence = GetLegacyDefence(info);
            MagicDefence = GetLegacyMagicDefence(info);
            Agility = info.Dexterity;
            Strength = info.Strength;
            Dexterity = info.Dexterity;
            Constitution = info.Constitution;
            Intelligence = info.Intelligence;
            Wisdom = info.Wisdom;
            Charisma = info.Charisma;
            ArmorClass = info.ArmorClass;
            ProficiencyBonus = info.ProficiencyBonus;
            AttackBonus = info.AttackBonus;
            DamageDice = info.DamageDice;
            DamageDie = info.DamageDie;
            DamageBonus = info.DamageBonus;
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

        public Rarity Rarity { get { return _info.Rarity; } }
        public int Gold { get; set; }

        public override IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return _info.SpellList.Select(spellId => availableSpells.FirstOrDefault(item => item.Name == spellId))
                .Where(spell => spell != null).ToList();
        }

        public override IEnumerable<Skill> GetSkills(IEnumerable<Skill> availableSkills)
        {
            return Enumerable.Empty<Skill>();
        }

        public IEnumerable<MonsterAction> GetActions()
        {
            return _info.Actions ?? Enumerable.Empty<MonsterAction>();
        }

        private static int GetLegacyAttack(Monster info)
        {
            return info == null
                ? 1
                : System.Math.Max(1, info.DamageDice * System.Math.Max(1, info.DamageDie) + info.DamageBonus);
        }

        private static int GetLegacyDefence(Monster info)
        {
            return info == null ? 0 : System.Math.Max(0, (info.ArmorClass - 10) * 5);
        }

        private static int GetLegacyMagicDefence(Monster info)
        {
            if (info == null)
            {
                return 0;
            }

            var wisdom = DndStatRules.GetAbilityModifier(info.Wisdom);
            return System.Math.Max(0, (wisdom + info.ProficiencyBonus) * 10);
        }
    }
}
