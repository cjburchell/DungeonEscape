namespace Redpoint.DungeonEscape.State
{
    using Nez;

    public static class Dice
    {
        public static int Roll(int randomFactor, int times = 1, int constValue = 0)
        {
            var value = constValue;
            if (randomFactor == 0)
            {
                return value;
            }

            for (var i = 0; i < times; i++)
            {
                value += Random.NextInt(randomFactor) + 1;
            }

            return value;
        }
        
        public static int RollD100()
        {
            return Random.NextInt(100) + 1;
        }
        
        public static int RollD20()
        {
            return Random.NextInt(20) + 1;
        }
    }
}