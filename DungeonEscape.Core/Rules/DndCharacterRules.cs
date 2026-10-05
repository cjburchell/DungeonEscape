using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DndCharacterRules
    {
        private static readonly Class[] PlayableClasses =
        {
            Class.Fighter,
            Class.Paladin,
            Class.Cleric,
            Class.Wizard,
            Class.Monk,
            Class.Warlock,
            Class.Bard,
            Class.Rogue,
            Class.Sorcerer
        };

        public static Class[] GetPlayableClasses()
        {
            return (Class[])PlayableClasses.Clone();
        }

        public static string GetClassLabel(Class heroClass)
        {
            return heroClass.ToString();
        }

        public static string GetRoleClassLabel(Class heroClass)
        {
            return GetClassLabel(heroClass);
        }

        public static List<Item> GetStartingEquipment(Class heroClass)
        {
            switch (heroClass)
            {
                case Class.Paladin:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Chain Mail", Slot.Chest, 3),
                        CreateStartingWeapon(heroClass, "Longsword", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingArmor(heroClass, "Shield", Slot.OffHand, 2)
                    };
                case Class.Fighter:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Chain Mail", Slot.Chest, 3),
                        CreateStartingWeapon(heroClass, "Longsword", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingArmor(heroClass, "Shield", Slot.OffHand, 2)
                    };
                case Class.Cleric:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Scale Mail", Slot.Chest, 2),
                        CreateStartingWeapon(heroClass, "Mace", Slot.PrimaryHand, 1, 1, 6, 1),
                        CreateStartingArmor(heroClass, "Shield", Slot.OffHand, 2)
                    };
                case Class.Wizard:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Robe", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Dagger", Slot.PrimaryHand, 1, 1, 4, 0)
                    };
                case Class.Monk:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Quarterstaff", Slot.PrimaryHand, 1, 1, 6, 0)
                    };
                case Class.Warlock:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Light Crossbow", Slot.PrimaryHand, 1, 1, 8, 0),
                        CreateStartingWeapon(heroClass, "Dagger", Slot.OffHand, 1, 1, 4, 0)
                    };
                case Class.Bard:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Rapier", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingWeapon(heroClass, "Dagger", Slot.OffHand, 1, 1, 4, 0)
                    };
                case Class.Rogue:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Rapier", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingWeapon(heroClass, "Dagger", Slot.OffHand, 1, 1, 4, 0)
                    };
                case Class.Sorcerer:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Robe", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Dagger", Slot.PrimaryHand, 1, 1, 4, 0)
                    };
                default:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Dagger", Slot.PrimaryHand, 1, 1, 4, 0)
                    };
            }
        }

        public static bool IsClassNameMatch(string className, Class heroClass)
        {
            return string.Equals(
                className?.Trim(),
                heroClass.ToString(),
                System.StringComparison.OrdinalIgnoreCase);
        }

        public static void ApplyStartingAbilityScores(Hero hero)
        {
            if (hero == null)
            {
                return;
            }

            SetRoleBaseScores(hero);
            ApplySpeciesModifiers(hero);
        }

        private static void SetRoleBaseScores(Hero hero)
        {
            switch (hero.Class)
            {
                case Class.Paladin:
                    SetScores(hero, 16, 10, 14, 10, 12, 14);
                    break;
                case Class.Fighter:
                    SetScores(hero, 16, 12, 15, 10, 11, 10);
                    break;
                case Class.Cleric:
                    SetScores(hero, 12, 10, 14, 10, 16, 12);
                    break;
                case Class.Wizard:
                    SetScores(hero, 8, 14, 12, 16, 12, 10);
                    break;
                case Class.Monk:
                    SetScores(hero, 12, 16, 13, 10, 14, 10);
                    break;
                case Class.Warlock:
                    SetScores(hero, 10, 12, 14, 12, 10, 16);
                    break;
                case Class.Bard:
                    SetScores(hero, 10, 14, 12, 12, 10, 16);
                    break;
                case Class.Rogue:
                    SetScores(hero, 10, 16, 12, 12, 12, 10);
                    break;
                case Class.Sorcerer:
                    SetScores(hero, 8, 14, 12, 12, 10, 16);
                    break;
                default:
                    SetScores(hero, 10, 10, 10, 10, 10, 10);
                    break;
            }
        }

        private static void ApplySpeciesModifiers(Hero hero)
        {
            switch (hero.Species)
            {
                case Species.Elf:
                    hero.Dexterity += 2;
                    hero.Intelligence += 1;
                    break;
                case Species.Dwarf:
                    hero.Constitution += 2;
                    hero.Wisdom += 1;
                    break;
                case Species.Halfling:
                    hero.Dexterity += 2;
                    hero.Charisma += 1;
                    break;
                default:
                    hero.Strength += 1;
                    hero.Dexterity += 1;
                    hero.Constitution += 1;
                    hero.Intelligence += 1;
                    hero.Wisdom += 1;
                    hero.Charisma += 1;
                    break;
            }
        }

        private static Item CreateStartingArmor(Class heroClass, string name, Slot slot, int defenceBonus)
        {
            return new Item
            {
                Name = name,
                Type = ItemType.Armor,
                Category = ItemCategory.Armor,
                Slots = new List<Slot> { slot },
                Classes = new List<string> { heroClass.ToString() },
                Rarity = Rarity.Common,
                Stats = new List<StatValue>
                {
                    new StatValue { Type = StatType.Defence, Value = defenceBonus }
                }
            };
        }

        private static Item CreateStartingWeapon(
            Class heroClass,
            string name,
            Slot slot,
            int attackBonus,
            int damageDice,
            int damageDie,
            int damageBonus)
        {
            return new Item
            {
                Name = name,
                Type = ItemType.Weapon,
                Category = ItemCategory.Weapon,
                Slots = new List<Slot> { slot },
                Classes = new List<string> { heroClass.ToString() },
                Rarity = Rarity.Common,
                DamageDice = damageDice,
                DamageDie = damageDie,
                DamageBonus = damageBonus,
                Stats = new List<StatValue>
                {
                    new StatValue { Type = StatType.Attack, Value = attackBonus }
                }
            };
        }

        private static void SetScores(
            Hero hero,
            int strength,
            int dexterity,
            int constitution,
            int intelligence,
            int wisdom,
            int charisma)
        {
            hero.Strength = strength;
            hero.Dexterity = dexterity;
            hero.Constitution = constitution;
            hero.Intelligence = intelligence;
            hero.Wisdom = wisdom;
            hero.Charisma = charisma;
        }
    }
}
