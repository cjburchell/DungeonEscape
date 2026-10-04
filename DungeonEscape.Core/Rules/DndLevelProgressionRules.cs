using System;

namespace Redpoint.DungeonEscape.Rules
{
    public static class DndLevelProgressionRules
    {
        public const int MaxCharacterLevel = 20;

        private static readonly ulong[] LevelXpThresholds =
        {
            0,
            300,
            900,
            2700,
            6500,
            14000,
            23000,
            34000,
            48000,
            64000,
            85000,
            100000,
            120000,
            140000,
            165000,
            195000,
            225000,
            265000,
            305000,
            355000
        };

        public static ulong GetXpForLevel(int level)
        {
            level = Math.Max(1, Math.Min(MaxCharacterLevel, level));
            return LevelXpThresholds[level - 1];
        }

        public static ulong GetNextLevelXp(int currentLevel)
        {
            if (currentLevel >= MaxCharacterLevel)
            {
                return GetXpForLevel(MaxCharacterLevel);
            }

            return GetXpForLevel(currentLevel + 1);
        }

        public static bool CanLevelUp(int currentLevel, ulong xp)
        {
            return currentLevel < MaxCharacterLevel && xp >= GetNextLevelXp(currentLevel);
        }
    }
}
