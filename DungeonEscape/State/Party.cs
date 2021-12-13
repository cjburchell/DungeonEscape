namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;

    public class Party
    {
        public const int MaxItems = 30;
        public Vector2 OverWorldPosition { get; set; } = Vector2.Zero;

        public Vector2? SavedPoint { get; set; }
        public int? SavedMapId { get; set; }
        
        public bool HasShip { get; set; }
        public List<Hero> Members { get; } = new List<Hero>();
        
        // ReSharper disable once UnusedMember.Global
        public List<ActiveQuest> ActiveQuests { get; } = new List<ActiveQuest>();
        
        public int Gold { get; set; }
        public List<ItemInstance> Items { get; } = new List<ItemInstance>();
        public Vector2 CurrentPosition { get; set; }
        public int CurrentMapId { get; set; }

        public bool CanOpenChest(int level)
        {
            return this.Members.Any(item => item.Level >= level);
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