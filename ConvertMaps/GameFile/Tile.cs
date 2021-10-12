namespace GameFile
{
    public class Tile
    {
        public int Id { get; set; }
        public Point Position { get; set; } = new Point();
        
        public Biome Biome { get; set; }
    }
}