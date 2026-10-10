using Redpoint.DungeonEscape.Data;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Rules;

namespace Redpoint.DungeonEscape.State
{
    public class Party
    {
        public WorldPosition OverWorldPosition { get; set; }
        public WorldPosition? SavedPoint { get; set; }
        public string SavedMapId { get; set; }
        public List<VisitedLocation> VisitedLocations { get; set; }

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
        public Biome CurrentBiome { get; set; }
        public Direction CurrentDirection { get; set; }
        public int StepCount { get; set; }

        public Party()
        {
            OverWorldPosition = WorldPosition.Zero;
            CurrentBiome = Biome.None;
            CurrentDirection = Direction.Down;
            Members = new List<Hero>();
            ActiveQuests = new List<ActiveQuest>();
            VisitedLocations = new List<VisitedLocation>();
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
            var selectedMember = AliveMembers
                .OrderBy(DndStatRules.GetCarriedWeight)
                .FirstOrDefault();

            if (selectedMember != null)
            {
                selectedMember.Items.Add(item);
            }

            return selectedMember;
        }

        public bool CanOpenChest(int level)
        {
            return AliveMembers.Any(item => item.Level >= level);
        }

        public string ShortRest()
        {
            if (Members == null || Members.Count == 0)
            {
                return "There is no party to rest.";
            }

            var healed = 0;
            var restedMembers = 0;
            foreach (var member in AliveMembers)
            {
                if (member == null)
                {
                    continue;
                }

                var beforeHealth = member.Health;
                var recoverAmount = Math.Max(1, member.MaxHealth / 4);
                member.Health = Math.Min(member.MaxHealth, member.Health + recoverAmount);
                if (member.Status != null && member.Status.Count > 0)
                {
                    member.Status.Clear();
                }

                healed += Math.Max(0, member.Health - beforeHealth);
                restedMembers++;
            }

            if (restedMembers == 0)
            {
                return "Your party does not need to rest.";
            }

            return "Your party takes a short rest and recovers " + healed + " HP.";
        }

        public string LongRest(int cost)
        {
            if (Members == null || Members.Count == 0)
            {
                return "There is no party to rest.";
            }

            cost = Math.Max(0, cost);
            if (Gold < cost)
            {
                return "You do not have " + cost + " gold for the inn.";
            }

            Gold -= cost;
            foreach (var member in ActiveMembers)
            {
                if (member == null)
                {
                    continue;
                }

                member.Health = member.MaxHealth;
                if (member.Status != null)
                {
                    member.Status.Clear();
                }

                member.RestoreSpellSlots();
            }

            return cost == 0
                ? "Your party makes camp and is fully restored."
                : "Your party has rested at the inn and is fully restored.";
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

        public int AverageActiveLevel()
        {
            var members = AliveMembers.ToList();
            if (members.Count == 0)
            {
                return 1;
            }

            return Math.Max(1, (int)Math.Round(members.Average(member => member.Level), MidpointRounding.AwayFromZero));
        }

        public Hero GetOrderedHero(int order)
        {
            var memberArray = ActiveMembers.OrderBy(i => i.IsDead).ToArray();
            return memberArray.Length <= order ? null : memberArray[order];
        }
    }
}
