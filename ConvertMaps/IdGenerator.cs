namespace ConvertMaps
{
    public static class IdGenerator
    {
        private static int tileId = 1;

        public static int New()
        {
            return tileId++;
        }

        public static int NextTileId => tileId;
    }
}