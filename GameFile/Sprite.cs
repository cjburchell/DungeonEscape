namespace GameFile
{
    using System.Collections.Generic;
    using System.Data.SqlTypes;

    public class Sprite
    {
        public Point StartPosition { get; set; } = new Point();
        public string Id { get; set; }
        
        public int State { get; set; }
        
        public bool CanMove { get; set; }
        public string Text { get; set; }
        
        public bool Collideable = true;
    }
}