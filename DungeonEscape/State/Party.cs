using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DungeonEscape.State
{
    public class Party
    {
        public const int MaxItems = 30;
        public Point OverWorldPosition { get; set; } = Point.Zero;

        public Point? SavedPoint { get; set; }
        public int? SavedMapId { get; set; }
        
        public bool HasShip { get; set; }
        public List<Hero> Members { get; } = new List<Hero>();
        public int Gold { get; set; }
        public List<ItemInstance> Items { get; } = new List<ItemInstance>();
        public Point CurrentPosition { get; set; }
        public int CurrentMapId { get; set; }

        public bool CanOpenChest(int level)
        {
            return true;
        }
        
        public bool CanOpenDoor(int doorLevel)
        {
            var key = this.Items.FirstOrDefault(item => item.Type == ItemType.Key && item.MinLevel == doorLevel);
            if (key == null)
            {
                return false;
            }

            this.Items.Remove(key);
            return  true;
        }
    }
}