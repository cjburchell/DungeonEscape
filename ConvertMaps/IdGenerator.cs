namespace ConvertMaps
{
    public class IdGenerator
    {
        private int tileId = 1;

        public int New()
        {
            return tileId++;
        }

        public int NextTileId => tileId;
    }
}