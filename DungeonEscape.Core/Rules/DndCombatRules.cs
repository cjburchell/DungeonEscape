using System;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DndCombatRules
    {
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
            return DndStatRules.GetAttackBonus(fighter);
        }

        public static int GetArmorClass(IFighter fighter)
        {
            return DndStatRules.GetArmorClass(fighter);
        }

        public static int RollWeaponDamage(IFighter fighter, bool critical, Func<int, int> rollDie)
        {
            if (fighter == null)
            {
                return 0;
            }

            var die = DndStatRules.GetDamageDie(fighter);
            var dice = DndStatRules.GetDamageDice(fighter);
            var totalDice = critical ? dice * 2 : dice;
            var damage = 0;
            for (var i = 0; i < totalDice; i++)
            {
                damage += rollDie == null ? Dice.RollDie(die) : rollDie(die);
            }

            var bonus = DndStatRules.GetDamageBonus(fighter);
            return Math.Max(1, damage + bonus);
        }

        public static int GetProficiencyBonus(IFighter fighter)
        {
            return DndStatRules.GetProficiencyBonus(fighter);
        }

        public static int GetAbilityModifier(int score)
        {
            return DndStatRules.GetAbilityModifier(score);
        }

        public static int GetStrength(IFighter fighter)
        {
            return DndStatRules.GetStrength(fighter);
        }

        public static int GetDexterity(IFighter fighter)
        {
            return DndStatRules.GetDexterity(fighter);
        }

        public static int GetConstitution(IFighter fighter)
        {
            return DndStatRules.GetConstitution(fighter);
        }
    }
}
