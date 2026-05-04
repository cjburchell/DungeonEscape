namespace Redpoint.DungeonEscape.State
{
    public struct WorldPosition
    {
        public float X { get; set; }
        public float Y { get; set; }

        public WorldPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static WorldPosition Zero
        {
            get { return new WorldPosition(0, 0); }
        }
    }
}
