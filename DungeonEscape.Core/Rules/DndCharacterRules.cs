using System.Collections.Generic;
using System.Linq;
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

        private static readonly string[] SkillNames =
        {
            "Acrobatics",
            "Animal Handling",
            "Arcana",
            "Athletics",
            "Deception",
            "History",
            "Insight",
            "Intimidation",
            "Investigation",
            "Medicine",
            "Nature",
            "Perception",
            "Performance",
            "Persuasion",
            "Religion",
            "Sleight of Hand",
            "Stealth",
            "Survival"
        };

        public static Class[] GetPlayableClasses()
        {
            return (Class[])PlayableClasses.Clone();
        }

        public static string[] GetSkillNames()
        {
            return (string[])SkillNames.Clone();
        }

        public static int GetClassSkillChoiceCount(Class heroClass)
        {
            return GetClassSkillChoiceCount(null, heroClass);
        }

        public static int GetClassSkillChoiceCount(ClassStats classStats, Class heroClass)
        {
            if (classStats != null && classStats.SkillChoiceCount > 0)
            {
                return classStats.SkillChoiceCount;
            }

            switch (heroClass)
            {
                case Class.Bard:
                    return 3;
                case Class.Rogue:
                    return 4;
                default:
                    return 2;
            }
        }

        public static List<string> GetClassSkillOptions(Class heroClass)
        {
            return GetClassSkillOptions(null, heroClass);
        }

        public static List<string> GetClassSkillOptions(ClassStats classStats, Class heroClass)
        {
            if (classStats != null && classStats.SkillOptions != null && classStats.SkillOptions.Count > 0)
            {
                return classStats.SkillOptions
                    .Where(skill => !string.IsNullOrWhiteSpace(skill))
                    .Distinct(System.StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            switch (heroClass)
            {
                case Class.Bard:
                    return SkillNames.ToList();
                case Class.Cleric:
                    return new List<string> { "History", "Insight", "Medicine", "Persuasion", "Religion" };
                case Class.Fighter:
                    return new List<string> { "Acrobatics", "Animal Handling", "Athletics", "History", "Insight", "Intimidation", "Perception", "Survival" };
                case Class.Monk:
                    return new List<string> { "Acrobatics", "Athletics", "History", "Insight", "Religion", "Stealth" };
                case Class.Paladin:
                    return new List<string> { "Athletics", "Insight", "Intimidation", "Medicine", "Persuasion", "Religion" };
                case Class.Rogue:
                    return new List<string> { "Acrobatics", "Athletics", "Deception", "Insight", "Intimidation", "Investigation", "Perception", "Performance", "Persuasion", "Sleight of Hand", "Stealth" };
                case Class.Sorcerer:
                    return new List<string> { "Arcana", "Deception", "Insight", "Intimidation", "Persuasion", "Religion" };
                case Class.Warlock:
                    return new List<string> { "Arcana", "Deception", "History", "Intimidation", "Investigation", "Nature", "Religion" };
                case Class.Wizard:
                    return new List<string> { "Arcana", "History", "Insight", "Investigation", "Medicine", "Religion" };
                default:
                    return SkillNames.ToList();
            }
        }

        public static string GetClassLabel(Class heroClass)
        {
            return heroClass.ToString();
        }

        public static string GetRoleClassLabel(Class heroClass)
        {
            return GetClassLabel(heroClass);
        }

        public static List<BackgroundDefinition> GetDefaultBackgrounds()
        {
            return new List<BackgroundDefinition>
            {
                new BackgroundDefinition { Id = "background/base/acolyte", Name = "Acolyte", Wisdom = 2, Charisma = 1, SkillProficiencies = new List<string> { "Insight", "Religion" } },
                new BackgroundDefinition { Id = "background/base/artisan", Name = "Artisan", Intelligence = 1, Charisma = 2, SkillProficiencies = new List<string> { "Investigation", "Persuasion" } },
                new BackgroundDefinition { Id = "background/base/criminal", Name = "Criminal", Dexterity = 2, Intelligence = 1, SkillProficiencies = new List<string> { "Deception", "Stealth" } },
                new BackgroundDefinition { Id = "background/base/entertainer", Name = "Entertainer", Dexterity = 1, Charisma = 2, SkillProficiencies = new List<string> { "Acrobatics", "Performance" } },
                new BackgroundDefinition { Id = "background/base/guard", Name = "Guard", Strength = 2, Wisdom = 1, SkillProficiencies = new List<string> { "Athletics", "Perception" } },
                new BackgroundDefinition { Id = "background/base/guide", Name = "Guide", Dexterity = 1, Wisdom = 2, SkillProficiencies = new List<string> { "Nature", "Survival" } },
                new BackgroundDefinition { Id = "background/base/noble", Name = "Noble", Intelligence = 1, Charisma = 2, SkillProficiencies = new List<string> { "History", "Persuasion" } },
                new BackgroundDefinition { Id = "background/base/sage", Name = "Sage", Intelligence = 2, Wisdom = 1, SkillProficiencies = new List<string> { "Arcana", "History" } },
                new BackgroundDefinition { Id = "background/base/sailor", Name = "Sailor", Strength = 1, Dexterity = 2, SkillProficiencies = new List<string> { "Athletics", "Perception" } },
                new BackgroundDefinition { Id = "background/base/soldier", Name = "Soldier", Strength = 2, Constitution = 1, SkillProficiencies = new List<string> { "Athletics", "Intimidation" } }
            };
        }

        public static List<SpeciesDefinition> GetDefaultSpeciesDefinitions()
        {
            return new List<SpeciesDefinition>
            {
                new SpeciesDefinition { Species = Species.Human, Name = "Human", Strength = 1, Dexterity = 1, Constitution = 1, Intelligence = 1, Wisdom = 1, Charisma = 1 },
                new SpeciesDefinition { Species = Species.Elf, Name = "Elf", Dexterity = 2, Intelligence = 1 },
                new SpeciesDefinition { Species = Species.Dwarf, Name = "Dwarf", Constitution = 2, Wisdom = 1 },
                new SpeciesDefinition { Species = Species.Halfling, Name = "Halfling", Dexterity = 2, Charisma = 1 }
            };
        }

        public static SpeciesDefinition FindSpecies(IEnumerable<SpeciesDefinition> speciesDefinitions, Species species)
        {
            var speciesList = speciesDefinitions == null
                ? GetDefaultSpeciesDefinitions()
                : speciesDefinitions.Where(item => item != null).ToList();
            return speciesList.FirstOrDefault(item => item.Species == species) ??
                   GetDefaultSpeciesDefinitions().First(item => item.Species == species);
        }

        public static BackgroundDefinition FindBackground(IEnumerable<BackgroundDefinition> backgrounds, string backgroundId)
        {
            var backgroundList = backgrounds == null ? GetDefaultBackgrounds() : backgrounds.Where(item => item != null).ToList();
            if (backgroundList.Count == 0)
            {
                backgroundList = GetDefaultBackgrounds();
            }

            return backgroundList.FirstOrDefault(item =>
                       string.Equals(item.Id, backgroundId, System.StringComparison.Ordinal)) ??
                   backgroundList.FirstOrDefault() ??
                   GetDefaultBackgrounds().First();
        }

        public static string GetBackgroundLabel(BackgroundDefinition background)
        {
            if (background == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(background.Name) ? background.Id ?? string.Empty : background.Name;
        }

        public static void ApplyBackgroundAbilityBonuses(Hero hero, BackgroundDefinition background)
        {
            if (hero == null || background == null)
            {
                return;
            }

            hero.Strength += background.Strength;
            hero.Dexterity += background.Dexterity;
            hero.Constitution += background.Constitution;
            hero.Intelligence += background.Intelligence;
            hero.Wisdom += background.Wisdom;
            hero.Charisma += background.Charisma;
        }

        public static List<string> GetStartingSkillProficiencies(ClassStats classStats, BackgroundDefinition background)
        {
            var skills = classStats == null || classStats.SkillProficiencies == null
                ? new List<string>()
                : classStats.SkillProficiencies.ToList();
            if (background != null && background.SkillProficiencies != null)
            {
                skills.AddRange(background.SkillProficiencies);
            }

            return skills
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<Item> GetStartingEquipment(Class heroClass)
        {
            switch (heroClass)
            {
                case Class.Paladin:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Chain Mail", "item/base/chain-mail", Slot.Chest, 3),
                        CreateStartingWeapon(heroClass, "Longsword", "item/base/longsword", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingArmor(heroClass, "Shield", "item/base/shield", Slot.OffHand, 2)
                    };
                case Class.Fighter:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Chain Mail", "item/base/chain-mail", Slot.Chest, 3),
                        CreateStartingWeapon(heroClass, "Longsword", "item/base/longsword", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingArmor(heroClass, "Shield", "item/base/shield", Slot.OffHand, 2)
                    };
                case Class.Cleric:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Scale Mail", "item/base/scale-mail", Slot.Chest, 2),
                        CreateStartingWeapon(heroClass, "Mace", "item/base/mace", Slot.PrimaryHand, 1, 1, 6, 1),
                        CreateStartingArmor(heroClass, "Shield", "item/base/shield", Slot.OffHand, 2)
                    };
                case Class.Wizard:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Robe", "item/base/robe", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Dagger", "item/base/dagger", Slot.PrimaryHand, 1, 1, 4, 0)
                    };
                case Class.Monk:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", "item/base/leather-armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Quarterstaff", "item/base/quarterstaff", Slot.PrimaryHand, 1, 1, 6, 0)
                    };
                case Class.Warlock:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", "item/base/leather-armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Light Crossbow", "item/base/light-crossbow", Slot.PrimaryHand, 1, 1, 8, 0),
                        CreateStartingWeapon(heroClass, "Dagger", "item/base/dagger", Slot.OffHand, 1, 1, 4, 0)
                    };
                case Class.Bard:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", "item/base/leather-armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Rapier", "item/base/rapier", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingWeapon(heroClass, "Dagger", "item/base/dagger", Slot.OffHand, 1, 1, 4, 0)
                    };
                case Class.Rogue:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", "item/base/leather-armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Rapier", "item/base/rapier", Slot.PrimaryHand, 2, 1, 8, 2),
                        CreateStartingWeapon(heroClass, "Dagger", "item/base/dagger", Slot.OffHand, 1, 1, 4, 0)
                    };
                case Class.Sorcerer:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Robe", "item/base/robe", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Dagger", "item/base/dagger", Slot.PrimaryHand, 1, 1, 4, 0)
                    };
                default:
                    return new List<Item>
                    {
                        CreateStartingArmor(heroClass, "Leather Armor", "item/base/leather-armor", Slot.Chest, 1),
                        CreateStartingWeapon(heroClass, "Dagger", "item/base/dagger", Slot.PrimaryHand, 1, 1, 4, 0)
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
            ApplyStartingAbilityScores(hero, null);
        }

        public static void ApplyStartingAbilityScores(Hero hero, IEnumerable<SpeciesDefinition> speciesDefinitions)
        {
            if (hero == null)
            {
                return;
            }

            SetRoleBaseScores(hero);
            ApplySpeciesModifiers(hero, speciesDefinitions);
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

        private static void ApplySpeciesModifiers(Hero hero, IEnumerable<SpeciesDefinition> speciesDefinitions)
        {
            var definition = FindSpecies(speciesDefinitions, hero.Species);
            hero.Strength += definition.Strength;
            hero.Dexterity += definition.Dexterity;
            hero.Constitution += definition.Constitution;
            hero.Intelligence += definition.Intelligence;
            hero.Wisdom += definition.Wisdom;
            hero.Charisma += definition.Charisma;
        }

        private static Item CreateStartingArmor(Class heroClass, string name, string id, Slot slot, int defenceBonus)
        {
            return new Item
            {
                Id = id,
                ImageId = GetStartingItemImageId(name),
                Name = name,
                Type = ItemType.Armor,
                Category = ItemCategory.Armor,
                Slots = new List<Slot> { slot },
                Classes = new List<string> { heroClass.ToString() },
                Rarity = Rarity.Common,
                Weight = GetStartingItemWeight(name),
                Stats = new List<StatValue>
                {
                    new StatValue { Type = StatType.Defence, Value = defenceBonus }
                }
            };
        }

        private static Item CreateStartingWeapon(
            Class heroClass,
            string name,
            string id,
            Slot slot,
            int attackBonus,
            int damageDice,
            int damageDie,
            int damageBonus)
        {
            return new Item
            {
                Id = id,
                ImageId = GetStartingItemImageId(name),
                Name = name,
                Type = ItemType.Weapon,
                Category = ItemCategory.Weapon,
                Slots = new List<Slot> { slot },
                Classes = new List<string> { heroClass.ToString() },
                Rarity = Rarity.Common,
                Weight = GetStartingItemWeight(name),
                DamageDice = damageDice,
                DamageDie = damageDie,
                DamageBonus = damageBonus,
                Stats = new List<StatValue>
                {
                    new StatValue { Type = StatType.Attack, Value = attackBonus }
                }
            };
        }

        private static int GetStartingItemImageId(string name)
        {
            switch (name)
            {
                case "Chain Mail":
                    return 280;
                case "Dagger":
                    return 37;
                case "Leather Armor":
                    return 278;
                case "Light Crossbow":
                    return 162;
                case "Longsword":
                    return 1;
                case "Mace":
                    return 50;
                case "Quarterstaff":
                    return 88;
                case "Rapier":
                    return 3;
                case "Robe":
                    return 303;
                case "Scale Mail":
                    return 289;
                case "Shield":
                    return 176;
                default:
                    return 0;
            }
        }

        private static int GetStartingItemWeight(string name)
        {
            switch (name)
            {
                case "Chain Mail":
                    return 55;
                case "Dagger":
                    return 1;
                case "Leather Armor":
                    return 10;
                case "Light Crossbow":
                    return 6;
                case "Longsword":
                    return 3;
                case "Mace":
                    return 4;
                case "Quarterstaff":
                    return 4;
                case "Rapier":
                    return 2;
                case "Robe":
                    return 4;
                case "Scale Mail":
                    return 22;
                case "Shield":
                    return 6;
                default:
                    return 0;
            }
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
