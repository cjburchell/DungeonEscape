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
    }
}