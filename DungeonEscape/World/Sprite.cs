namespace DungeonEscape.World
{
    using GameFile;
    using Microsoft.Xna.Framework;

    public class Sprite : GameObject
    {
        public GameFile.Sprite Instance { get; set; }
        public TileInfo Info { get; set; }
        
        public override Rectangle BoundingBox =>
            new Rectangle(
                (int)Location.X + Visual.Width/4,
                (int)Location.Y + Visual.Height/4,
                Visual.Width/2,
                Visual.Height/2);
    }
}