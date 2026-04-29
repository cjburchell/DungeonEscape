using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace Redpoint.DungeonEscape.State
{
    public class Party
    {
        public const int MaxItems = 30;

        public WorldPosition OverWorldPosition { get; set; }
        public WorldPosition? SavedPoint { get; set; }
        public string SavedMapId { get; set; }

        [JsonIgnore]
        public bool HasShip
        {
            get { return Members.Any(i => i.Items.Any(j => j.Name == "Deed to the ship")); }
        }

        public string PlayerName { get; set; }
        public List<Hero> Members { get; private set; }

        [JsonIgnore]
        public IEnumerable<Hero> ActiveMembers
        {
            get { return Members.Where(member => member.IsActive).OrderBy(i => i.Order); }
        }

        [JsonIgnore]
        public IEnumerable<Hero> InactiveMembers
        {
            get { return Members.Where(member => !member.IsActive); }
        }

        [JsonIgnore]
        public IEnumerable<Hero> AliveMembers
        {
            get { return ActiveMembers.Where(member => !member.IsDead); }
        }

        [JsonIgnore]
        public IEnumerable<Hero> DeadMembers
        {
            get { return ActiveMembers.Where(member => member.IsDead && member.IsActive); }
        }

        public List<ActiveQuest> ActiveQuests { get; set; }
        public int Gold { get; set; }
        public WorldPosition? CurrentPosition { get; set; }
        public string CurrentMapId { get; set; }
        public bool CurrentMapIsOverWorld { get; set; }
        public Direction CurrentDirection { get; set; }
        public int StepCount { get; set; }

        public Party()
        {
            OverWorldPosition = WorldPosition.Zero;
            CurrentDirection = Direction.Down;
            Members = new List<Hero>();
            ActiveQuests = new List<ActiveQuest>();
        }

        public ItemInstance GetItem(string itemId)
        {
            return AliveMembers
                .Select(member => member.Items.FirstOrDefault(i => IsItemMatch(i, itemId)))
                .FirstOrDefault(item => item != null);
        }

        public (Item, Hero) RemoveItem(string itemId)
        {
            foreach (var member in ActiveMembers)
            {
                var item = member.Items.FirstOrDefault(i => IsItemMatch(i, itemId));
                if (item == null)
                {
                    continue;
                }

                member.Items.Remove(item);
                return (item.Item, member);
            }

            return (null, null);
        }

        private static bool IsItemMatch(ItemInstance instance, string itemId)
        {
            return instance != null &&
                   instance.Item != null &&
                   !string.IsNullOrEmpty(itemId) &&
                   (string.Equals(instance.Item.Id, itemId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(instance.Item.Name, itemId, StringComparison.OrdinalIgnoreCase));
        }

        public Hero AddItem(ItemInstance item)
        {
            var selectedMember = AliveMembers.FirstOrDefault(partyMember => partyMember.Items.Count < MaxItems);
            if (selectedMember == null)
            {
                if (item.Type != ItemType.Quest)
                {
                    return null;
                }

                selectedMember = AliveMembers.First();
            }

            selectedMember.Items.Add(item);
            return selectedMember;
        }

        public bool CanOpenChest(int level)
        {
            return AliveMembers.Any(item => item.Level >= level);
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

            return key.Use(itemMember, itemMember, door, game, 0).Item1;
        }

        public int MaxLevel()
        {
            return AliveMembers.Max(item => item.Level);
        }

        public Hero GetOrderedHero(int order)
        {
            var memberArray = ActiveMembers.OrderBy(i => i.IsDead).ToArray();
            return memberArray.Length <= order ? null : memberArray[order];
        }
    }
}
