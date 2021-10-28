using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DungeonEscape.State
{
    public class Party
    {
        public const int MaxItems = 30;
        
        public Party()
        {
            this.Members.Add(new Hero {Name = "Player 1"});
            this.Members.Add(new Hero {Name = "Player 2"});
            this.Members.Add(new Hero {Name = "Player 3"});
        }
        public Point OverWorldPos { get; set; } = Point.Zero;
        public bool HasShip { get; set; }
        public List<Hero> Members { get; } = new List<Hero>();
        public int Gold { get; set; }
        public List<ItemInstance> Items { get; } = new List<ItemInstance>();
        
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