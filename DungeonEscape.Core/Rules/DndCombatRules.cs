using System;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DndCombatRules
    {
        private const int DefaultDamageDie = 6;

        public static CombatAttackResult ResolveWeaponAttack(
            IFighter source,
            IFighter target,
            Func<int> rollD20,
            Func<int, int> rollDie)
        {
            var result = new CombatAttackResult
            {
                TargetArmorClass = GetArmorClass(target)
            };

            if (source == null || target == null)
            {
                return result;
            }

            result.Roll = rollD20 == null ? Dice.RollD20() : rollD20();
            result.Total = result.Roll + GetAttackBonus(source);
            result.Critical = result.Roll == 20;
            result.Hit = result.Critical || result.Total >= result.TargetArmorClass;
            if (result.Hit)
            {
                result.Damage = RollWeaponDamage(source, result.Critical, rollDie);
            }

            return result;
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

            return GetProficiencyBonus(fighter) + Math.Max(GetAbilityModifier(GetStrength(fighter)), GetAbilityModifier(GetDexterity(fighter)));
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

        public static int RollWeaponDamage(IFighter fighter, bool critical, Func<int, int> rollDie)
        {
            if (fighter == null)
            {
                return 0;
            }

            var die = fighter.DamageDie > 0 ? fighter.DamageDie : InferLegacyDamageDie(fighter.Attack);
            var dice = Math.Max(1, fighter.DamageDice);
            var totalDice = critical ? dice * 2 : dice;
            var damage = 0;
            for (var i = 0; i < totalDice; i++)
            {
                damage += rollDie == null ? Dice.RollDie(die) : rollDie(die);
            }

            var bonus = fighter.DamageBonus != 0
                ? fighter.DamageBonus
                : Math.Max(GetAbilityModifier(GetStrength(fighter)), GetAbilityModifier(GetDexterity(fighter)));
            return Math.Max(1, damage + bonus);
        }

        public static int GetProficiencyBonus(IFighter fighter)
        {
            if (fighter == null)
            {
                return 2;
            }

            if (fighter.ProficiencyBonus > 0)
            {
                return fighter.ProficiencyBonus;
            }

            return 2 + Math.Max(0, fighter.Level - 1) / 4;
        }

        public static int GetAbilityModifier(int score)
        {
            return (int)Math.Floor((score - 10) / 2.0);
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

        private static int GetAbilityScore(int explicitScore, int legacyValue, int divisor)
        {
            if (explicitScore > 0)
            {
                return explicitScore;
            }

            return Clamp(10 + legacyValue / Math.Max(1, divisor), 3, 20);
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
    }
}
