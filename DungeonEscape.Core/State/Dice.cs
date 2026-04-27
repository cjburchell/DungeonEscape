using System;

namespace Redpoint.DungeonEscape.State
{
    public static class Dice
    {
        private static readonly Random Random = new Random();

        public static int Roll(int randomFactor, int times = 1, int constValue = 0)
        {
            var value = constValue;

            if (randomFactor == 0)
            {
                return value;
            }

            for (var i = 0; i < times; i++)
            {
                value += Random.Next(randomFactor) + 1;
            }

            return value;
        }

        public static int RollD100()
        {
            return Random.Next(100) + 1;
        }

        public static int RollD20()
        {
            return Random.Next(20) + 1;
        }
    }
}
