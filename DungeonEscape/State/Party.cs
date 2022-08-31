
namespace Redpoint.DungeonEscape.State
{
    using Newtonsoft.Json;
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
        public List<Hero> Members { get; } = new();
        
        [JsonIgnore]
        public IEnumerable<Hero> AliveMembers => this.Members.Where(member => !member.IsDead);

        // ReSharper disable once UnusedMember.Global
        public List<ActiveQuest> ActiveQuests { get; } = new();
        
        public int Gold { get; set; }

        public Vector2? CurrentPosition { get; set; }
        public int CurrentMapId { get; set; }
        public bool CurrentMapIsOverWorld { get; set; }

        public int StepCount { get; set; }

        public ItemInstance GetItem(string itemId)
        {
            return this.AliveMembers.Select(member =>  member.Items.FirstOrDefault(i => i.Id == itemId)).FirstOrDefault(item => item != null);
        }

        public void RemoveItem(string itemId)
        {
            foreach (var member in this.Members)
            {
                var item = member.Items.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    member.Items.Remove(item);
                    break;
                }
            }
        }

        public Hero AddItem(ItemInstance item)
        {
            var selectedMember = this.AliveMembers.FirstOrDefault(partyMember => partyMember.Items.Count < MaxItems);
            if (selectedMember == null)
            {
                return null;
            }
            
            selectedMember.Items.Add(item);
            return selectedMember;
        }

        public bool CanOpenChest(int level)
        {
            return this.AliveMembers.Any(item => item.Level >= level);
        }
        
        public bool CanOpenDoor(int doorLevel)
        {
            ItemInstance key = null;
            Hero itemMember = null;
            foreach (var member in AliveMembers)
            {
                key = member.Items.FirstOrDefault(item => item.Type == ItemType.Key && item.MinLevel == doorLevel);
                if (key != null)
                {
                    itemMember = member;
                    break;
                }
            }

            if (key == null)
            {
                return false;
            }

            itemMember.Items.Remove(key);
            return  true;
        }
    }
}