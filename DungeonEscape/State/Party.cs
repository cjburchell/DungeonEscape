
using Redpoint.DungeonEscape.Scenes.Map;

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
        public string SavedMapId { get; set; }


        [JsonIgnore] public bool HasShip
        {
            get
            {
                return this.Members.Any(i => i.Items.Any(j => j.Name == "Deed to the ship"));
            }
        }

        public string PlayerName { get; set; }
        public List<Hero> Members { get; } = new();
        
        [JsonIgnore]
        public IEnumerable<Hero> ActiveMembers => this.Members.Where(member => member.IsActive).OrderBy(i => i.Order);
        
        [JsonIgnore]
        public IEnumerable<Hero> InactiveMembers => this.Members.Where(member => !member.IsActive);

        [JsonIgnore]
        public IEnumerable<Hero> AliveMembers => this.ActiveMembers.Where(member => !member.IsDead);
        
        [JsonIgnore]
        public IEnumerable<Hero> DeadMembers => this.ActiveMembers.Where(member => member.IsDead && member.IsActive);

        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public List<ActiveQuest> ActiveQuests { get; set; } = new();
        
        
        public int Gold { get; set; }

        public Vector2? CurrentPosition { get; set; }
        public string CurrentMapId { get; set; }
        public bool CurrentMapIsOverWorld { get; set; }

        public int StepCount { get; set; }
        


        public ItemInstance GetItem(string itemId)
        {
            return this.AliveMembers.Select(member =>  member.Items.FirstOrDefault(i => i.Item.Id == itemId)).FirstOrDefault(item => item != null);
        }

        public (Item, Hero) RemoveItem(string itemId)
        {
            foreach (var member in this.ActiveMembers)
            {
                var item = member.Items.FirstOrDefault(i => i.Item.Id == itemId);
                if (item == null) continue;
                member.Items.Remove(item);
                return (item.Item, member);
            }

            return (null, null);
        }

        public Hero AddItem(ItemInstance item)
        {
            var selectedMember = this.AliveMembers.FirstOrDefault(partyMember => partyMember.Items.Count < MaxItems);
            if (selectedMember == null)
            {
                if (item.Type != ItemType.Quest)
                {
                    return null;
                }

                selectedMember = this.AliveMembers.First();
            }
            
            selectedMember.Items.Add(item);
            return selectedMember;
        }

        public bool CanOpenChest(int level)
        {
            return this.AliveMembers.Any(item => item.Level >= level);
        }
        
        public string OpenDoor(ObjectState door, IGame game)
        {
            ItemInstance key = null;
            Hero itemMember = null;
            foreach (var member in AliveMembers)
            {
                key = member.Items.FirstOrDefault(item => item.Item.IsKey && item.MinLevel == door.Level);
                if (key != null)
                {
                    itemMember = member;
                    break;
                }
            }
            
            if (key == null)
            {
                return "You do not have a key for this door";
            }

            return MapInput.UseItem(game, itemMember, null, door, key).Item1;
        }

        public int MaxLevel()
        {
            return this.AliveMembers.Max(item => item.Level);
        }

        public Hero GetOrderedHero(int order)
        {
            var memberArray = this.ActiveMembers.OrderBy(i => i.IsDead).ToArray();
            return memberArray.Length <= order ? null : memberArray[order];
        }
    }
}