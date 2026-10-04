using System;
using System.Linq;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DndSpellcastingRules
    {
        public const int MaxSpellLevel = 9;

        private static readonly int[][] FullCasterSlots =
        {
            new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 },
            new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 },
            new[] { 4, 3, 3, 3, 2, 1, 1, 1, 1 },
            new[] { 4, 3, 3, 3, 3, 1, 1, 1, 1 },
            new[] { 4, 3, 3, 3, 3, 2, 1, 1, 1 },
            new[] { 4, 3, 3, 3, 3, 2, 2, 1, 1 }
        };

        private static readonly int[][] PaladinSlots =
        {
            new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 },
            new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 }
        };

        public static int[] GetMaxSpellSlots(Class heroClass, int level)
        {
            level = Math.Max(1, Math.Min(20, level));
            if (IsFullCaster(heroClass))
            {
                return CopySlots(FullCasterSlots[level - 1]);
            }

            if (heroClass == Class.Paladin)
            {
                return CopySlots(PaladinSlots[level - 1]);
            }

            if (heroClass == Class.Warlock)
            {
                return GetWarlockPactSlots(level);
            }

            return new int[MaxSpellLevel];
        }

        public static bool IsSpellcaster(Class heroClass)
        {
            return IsFullCaster(heroClass) || heroClass == Class.Paladin || heroClass == Class.Warlock;
        }

        public static int GetPreparedSpellLimit(Hero hero)
        {
            if (hero == null || !IsSpellcaster(hero.Class))
            {
                return 0;
            }

            var slots = GetMaxSpellSlots(hero.Class, hero.Level);
            if (!slots.Any(slot => slot > 0))
            {
                return 0;
            }

            var abilityModifier = DndStatRules.GetAbilityModifier(GetSpellcastingAbilityScore(hero));
            if (hero.Class == Class.Paladin)
            {
                return Math.Max(1, Math.Max(1, hero.Level) / 2 + abilityModifier);
            }

            return Math.Max(1, Math.Max(1, hero.Level) + abilityModifier);
        }

        private static bool IsFullCaster(Class heroClass)
        {
            return heroClass == Class.Bard ||
                   heroClass == Class.Cleric ||
                   heroClass == Class.Sorcerer ||
                   heroClass == Class.Wizard;
        }

        private static int GetSpellcastingAbilityScore(Hero hero)
        {
            switch (hero.Class)
            {
                case Class.Wizard:
                    return DndStatRules.GetIntelligence(hero);
                case Class.Cleric:
                    return DndStatRules.GetWisdom(hero);
                case Class.Bard:
                case Class.Paladin:
                case Class.Sorcerer:
                case Class.Warlock:
                    return DndStatRules.GetCharisma(hero);
                default:
                    return 10;
            }
        }

        private static int[] GetWarlockPactSlots(int level)
        {
            var slots = new int[MaxSpellLevel];
            var slotLevel = level >= 9 ? 5 : level >= 7 ? 4 : level >= 5 ? 3 : level >= 3 ? 2 : 1;
            var slotCount = level >= 17 ? 4 : level >= 11 ? 3 : level >= 2 ? 2 : 1;
            slots[slotLevel - 1] = slotCount;
            return slots;
        }

        private static int[] CopySlots(int[] slots)
        {
            var copy = new int[MaxSpellLevel];
            Array.Copy(slots, copy, Math.Min(slots.Length, copy.Length));
            return copy;
        }
    }
}
