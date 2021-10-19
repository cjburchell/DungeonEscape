using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DungeonEscape.State
{
    public class Player
    {
        public Point OverWorldPos { get; set; } = Point.Zero;
        public bool HasShip { get; set; } = false;
        public int Gold { get; set; } = 10;
        public List<Item> Items { get; } = new List<Item>();
    }
}