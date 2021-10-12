using System.Collections.Generic;

namespace GameFile
{
    public class Map
    {
        public int Id;
        
        public int Width;
        public int Height;
        public Point DefaultStart = new Point();
        public int DefaultTileId;

        public List<Tile> WaterLayer = new List<Tile>();
        public List<Tile> WallLayer = new List<Tile>();
        public List<Tile> FloorLayer = new List<Tile>();
        
        public List<TileInfo> TileInfo = new List<TileInfo>();
        public List<Sprite> Sprites = new List<Sprite>();
        public List<Monster> RandomMonsters = new List<Monster>();
        public List<TileInfo> RandomMonstersTileInfo = new List<TileInfo>();
    }
}