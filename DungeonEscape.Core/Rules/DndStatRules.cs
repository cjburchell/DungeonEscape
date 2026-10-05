using System;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DndStatRules
    {
        private const int DefaultDamageDie = 6;

        public enum AbilityScore
        {
            Strength,
            Dexterity,
            Constitution,
            Intelligence,
            Wisdom,
            Charisma
        }

        public static int GetAbilityModifier(int score)
        {
            return (int)Math.Floor((score - 10) / 2.0);
        }

        public static int GetSavingThrowModifier(IFighter fighter, AbilityScore abilityScore, bool proficient)
        {
            if (fighter == null)
            {
                return 0;
            }

            var abilityModifier = GetAbilityModifier(GetAbilityScoreValue(fighter, abilityScore));
            return proficient ? abilityModifier + GetEffectiveProficiencyBonus(fighter) : abilityModifier;
        }

        public static int GetSkillCheckModifier(IFighter fighter, string skillName, bool proficient)
        {
            if (fighter == null || string.IsNullOrWhiteSpace(skillName))
            {
                return 0;
            }

            var ability = GetSkillAbility(skillName);
            var modifier = GetAbilityModifier(GetAbilityScoreValue(fighter, ability));
            return proficient ? modifier + GetEffectiveProficiencyBonus(fighter) : modifier;
        }

        public static int GetStrength(IFighter fighter)
        {
            return GetAbilityScore(fighter == null ? 0 : fighter.Strength, fighter == null ? 0 : fighter.Attack, 2);
        }

        public static int GetDexterity(IFighter fighter)
        {
            return GetAbilityScore(fighter == null ? 0 : fighter.Dexterity, fighter == null ? 0 : fighter.Agility, 2);
        }

        public static int GetConstitution(IFighter fighter)
        {
            return GetAbilityScore(fighter == null ? 0 : fighter.Constitution, fighter == null ? 0 : fighter.MaxHealth, 10);
        }

        public static int GetIntelligence(IFighter fighter)
        {
            return GetAbilityScore(fighter == null ? 0 : fighter.Intelligence, fighter == null ? 0 : fighter.Magic, 2);
        }

        public static int GetWisdom(IFighter fighter)
        {
            return GetAbilityScore(fighter == null ? 0 : fighter.Wisdom, fighter == null ? 0 : fighter.MagicDefence, 5);
        }

        public static int GetCharisma(IFighter fighter)
        {
            return GetAbilityScore(fighter == null ? 0 : fighter.Charisma, fighter == null ? 0 : fighter.Magic, 3);
        }

        public static int GetProficiencyBonus(IFighter fighter)
        {
            return fighter == null ? 2 : GetProficiencyBonus(fighter.Level);
        }

        public static int GetProficiencyBonus(int level)
        {
            return 2 + Math.Max(0, level - 1) / 4;
        }

        public static int GetArmorClass(IFighter fighter)
        {
            if (fighter == null)
            {
                return 10;
            }

            if (fighter.ArmorClass > 0)
            {
                return fighter.ArmorClass;
            }

            var armorFromLegacyDefence = Clamp(fighter.Defence / 5, 0, 6);
            return 10 + GetAbilityModifier(GetDexterity(fighter)) + armorFromLegacyDefence;
        }

        public static int GetAttackBonus(IFighter fighter)
        {
            if (fighter == null)
            {
                return 0;
            }

            if (fighter.AttackBonus != 0)
            {
                return fighter.AttackBonus;
            }

            return GetEffectiveProficiencyBonus(fighter) + GetWeaponAbilityModifier(fighter);
        }

        public static int GetDamageBonus(IFighter fighter)
        {
            if (fighter == null)
            {
                return 0;
            }

            if (!(fighter is Hero) && fighter.DamageBonus != 0)
            {
                return fighter.DamageBonus;
            }

            return GetWeaponAbilityModifier(fighter) + fighter.DamageBonus;
        }

        public static int GetDamageDie(IFighter fighter)
        {
            if (fighter == null)
            {
                return DefaultDamageDie;
            }

            return fighter.DamageDie > 0 ? fighter.DamageDie : InferLegacyDamageDie(fighter.Attack);
        }

        public static int GetDamageDice(IFighter fighter)
        {
            return Math.Max(1, fighter == null ? 0 : fighter.DamageDice);
        }

        public static int GetInitiativeBonus(IFighter fighter)
        {
            return GetAbilityModifier(GetDexterity(fighter));
        }

        public static int GetHeroHitPointsForLevel(Hero hero, ClassStats classStats, int level)
        {
            if (hero == null)
            {
                return 1;
            }

            var total = Math.Max(1, GetHitDie(classStats) + GetAbilityModifier(GetConstitution(hero)));
            for (var currentLevel = 2; currentLevel <= Math.Max(1, level); currentLevel++)
            {
                total += GetHeroHitPointGain(hero, classStats);
            }

            return total;
        }

        public static int GetHeroHitPointGain(Hero hero, ClassStats classStats)
        {
            var hitDie = GetHitDie(classStats);
            var averageRoll = hitDie / 2 + 1;
            return Math.Max(1, averageRoll + GetAbilityModifier(GetConstitution(hero)));
        }

        public static int GetMonsterHitPoints(Monster monster, Func<int, int> rollDie)
        {
            if (monster == null)
            {
                return 1;
            }

            if (monster.HitPoints > 0)
            {
                return monster.HitPoints;
            }

            return Math.Max(1, RollHitDice(monster.HitDice, rollDie));
        }

        public static int GetMonsterAverageHitPoints(Monster monster)
        {
            if (monster == null)
            {
                return 1;
            }

            if (monster.HitPoints > 0)
            {
                return monster.HitPoints;
            }

            return Math.Max(1, GetAverageHitDice(monster.HitDice));
        }

        public static int RollHitDice(string hitDice, Func<int, int> rollDie)
        {
            var parsed = ParseHitDice(hitDice);
            if (parsed.Dice <= 0 || parsed.Die <= 0)
            {
                return 0;
            }

            var total = parsed.Bonus;
            for (var i = 0; i < parsed.Dice; i++)
            {
                total += rollDie == null ? Dice.RollDie(parsed.Die) : rollDie(parsed.Die);
            }

            return total;
        }

        public static int GetAverageHitDice(string hitDice)
        {
            var parsed = ParseHitDice(hitDice);
            if (parsed.Dice <= 0 || parsed.Die <= 0)
            {
                return 0;
            }

            return parsed.Dice * (parsed.Die + 1) / 2 + parsed.Bonus;
        }

        public static void RefreshHeroDerivedStats(Hero hero)
        {
            if (hero == null)
            {
                return;
            }

            hero.ProficiencyBonus = GetProficiencyBonus(hero.Level);
            hero.AttackBonus = 0;
            hero.ArmorClass = 0;
        }

        private static int GetEffectiveProficiencyBonus(IFighter fighter)
        {
            return fighter.ProficiencyBonus > 0 ? fighter.ProficiencyBonus : GetProficiencyBonus(fighter);
        }

        private static int GetAbilityScoreValue(IFighter fighter, AbilityScore abilityScore)
        {
            switch (abilityScore)
            {
                case AbilityScore.Strength:
                    return GetStrength(fighter);
                case AbilityScore.Dexterity:
                    return GetDexterity(fighter);
                case AbilityScore.Constitution:
                    return GetConstitution(fighter);
                case AbilityScore.Intelligence:
                    return GetIntelligence(fighter);
                case AbilityScore.Wisdom:
                    return GetWisdom(fighter);
                case AbilityScore.Charisma:
                    return GetCharisma(fighter);
                default:
                    return 10;
            }
        }

        private static AbilityScore GetSkillAbility(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return AbilityScore.Strength;
            }

            switch (skillName.Trim())
            {
                case "Acrobatics":
                case "Sleight of Hand":
                case "Stealth":
                    return AbilityScore.Dexterity;
                case "Athletics":
                    return AbilityScore.Strength;
                case "Arcana":
                case "History":
                case "Investigation":
                case "Nature":
                case "Religion":
                    return AbilityScore.Intelligence;
                case "Animal Handling":
                case "Insight":
                case "Medicine":
                case "Perception":
                case "Survival":
                    return AbilityScore.Wisdom;
                case "Deception":
                case "Intimidation":
                case "Performance":
                case "Persuasion":
                    return AbilityScore.Charisma;
                default:
                    return AbilityScore.Strength;
            }
        }

        private static int GetWeaponAbilityModifier(IFighter fighter)
        {
            return Math.Max(GetAbilityModifier(GetStrength(fighter)), GetAbilityModifier(GetDexterity(fighter)));
        }

        private static int GetAbilityScore(int explicitScore, int legacyValue, int divisor)
        {
            if (explicitScore > 0)
            {
                return explicitScore;
            }

            return Clamp(10 + legacyValue / Math.Max(1, divisor), 3, 20);
        }

        private static int GetHitDie(ClassStats classStats)
        {
            return classStats != null && classStats.HitDie > 0 ? classStats.HitDie : 8;
        }

        private static HitDice ParseHitDice(string hitDice)
        {
            if (string.IsNullOrWhiteSpace(hitDice))
            {
                return new HitDice();
            }

            var value = hitDice.Trim().ToLowerInvariant().Replace(" ", string.Empty);
            var separatorIndex = value.IndexOf('d');
            if (separatorIndex < 0)
            {
                return new HitDice();
            }

            var diceText = value.Substring(0, separatorIndex);
            var remainder = value.Substring(separatorIndex + 1);
            var bonusIndex = remainder.IndexOfAny(new[] { '+', '-' });
            var dieText = bonusIndex < 0 ? remainder : remainder.Substring(0, bonusIndex);
            var bonusText = bonusIndex < 0 ? string.Empty : remainder.Substring(bonusIndex);

            int dice;
            int die;
            int bonus;
            dice = string.IsNullOrEmpty(diceText) ? 1 : int.TryParse(diceText, out dice) ? dice : 0;
            die = int.TryParse(dieText, out die) ? die : 0;
            bonus = string.IsNullOrEmpty(bonusText) ? 0 : int.TryParse(bonusText, out bonus) ? bonus : 0;

            return new HitDice
            {
                Dice = dice,
                Die = die,
                Bonus = bonus
            };
        }

        private static int InferLegacyDamageDie(int attack)
        {
            if (attack >= 32)
            {
                return 12;
            }

            if (attack >= 22)
            {
                return 10;
            }

            if (attack >= 14)
            {
                return 8;
            }

            return DefaultDamageDie;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        private struct HitDice
        {
            public int Dice { get; set; }
            public int Die { get; set; }
            public int Bonus { get; set; }
        }
    }
}
