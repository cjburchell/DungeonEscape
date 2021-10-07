namespace ConvertMaps
{
    public static class IdGenerator
    {
        private static int TileId = 1;

        public static int New()
        {
            return TileId++;
        }
    }
}