// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace Redpoint.DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Nez;
    using Nez.Textures;
    using Nez.Tiled;

    public class Item
    {
        public override string ToString()
        {
            return this.Name;
        }

        public void Setup(TmxTileset tileset)
        {
            this.Image = tileset.Image != null ? new Sprite(tileset.Image.Texture, tileset.TileRegions[this.ImageId]) : new Sprite(tileset.Tiles[this.ImageId].Image.Texture);
        }
        
        public string Id { get; set; }
        public int ImageId { get; set; }

        public static Item CreateGold(int value)
        {
            var item = new Item
            {
                Name = "Gold",
                Cost = value,
                MinLevel = 0,
                Type = ItemType.Gold,
                ImageId = 202,
                Id = null
            };
            
            // gold Image
            var tileSet = Game.LoadTileSet("Content/items2.tsx");
            item.Setup(tileSet);

            return item;
        }
        
        public static readonly List<ItemType> EquippableItems = new()
        {
            ItemType.Weapon,
            ItemType.Armor
        };


        public static Item CreateRandomItem(IEnumerable<ItemDefinition> itemDefinitions, IEnumerable<Item> staticItems, List<StatName> statNames, int maxLevel, int minLevel = 1, Rarity? rarity = null)
        {
            maxLevel = Math.Max(maxLevel, 1);
            if (Nez.Random.Chance(0.25f))
            {
                var staticItemsList = staticItems.ToList();
                return staticItemsList.Where(i => i.Type == ItemType.OneUse && i.MinLevel < maxLevel).ToArray()[Nez.Random.NextInt(staticItemsList.Count(i => i.Type == ItemType.OneUse && i.MinLevel < maxLevel))];
            }
            
            
            var itemRarity = Nez.Random.NextInt(100);
            rarity ??= itemRarity > 75
                ? itemRarity > 90 ? itemRarity > 98 ? Rarity.Epic : Rarity.Rare : Rarity.Uncommon
                : Rarity.Common;
            
            var type = EquippableItems.ToArray()[Nez.Random.NextInt(EquippableItems.Count)];

            var item = new Item
            {
                Rarity = rarity.Value,
                Type = type,
                ImageId = 202,
                Id = Guid.NewGuid().ToString()
            };
            

            List<StatType> availableStats;
            ItemDefinition itemDefinition;
            var definitions = itemDefinitions.ToList();
            switch (type)
            {
                case ItemType.Weapon:
                {
                    availableStats = new List<StatType>
                        {StatType.Agility, StatType.Attack, StatType.Health, StatType.Magic};
                    itemDefinition = definitions.Where(i => i.Type == ItemType.Weapon).ToArray()[Nez.Random.NextInt(definitions.Count(i => i.Type == ItemType.Weapon))];
                    item.MinLevel = Nez.Random.NextInt(maxLevel - minLevel) + minLevel;
                    item.Stats.Add(new StatValue
                    {
                        Type = StatType.Attack,
                        Value = Math.Max(item.MinLevel - 5 + Nez.Random.NextInt(6), 1)
                    });

                    break;
                }
                case ItemType.Armor:
                {
                    availableStats = new List<StatType>
                        {StatType.Agility, StatType.Defence, StatType.Health, StatType.Magic};
                    itemDefinition = definitions.Where(i => i.Type == ItemType.Armor).ToArray()[Nez.Random.NextInt(definitions.Count(i => i.Type == ItemType.Armor))];
                    item.MinLevel = Nez.Random.NextInt(maxLevel - minLevel) + minLevel;
                    item.Stats.Add(new StatValue
                    {
                        Type = StatType.Defence,
                        Value = Math.Max(item.MinLevel - 5 + Nez.Random.NextInt(6), 1)
                    });

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(type.ToString());
            }
            
            var baseStatLevel = Math.Min((int)(item.MinLevel / 25.0f * itemDefinition.Names.Count) , itemDefinition.Names.Count-1);
            var baseName = itemDefinition.Names[baseStatLevel];
            if (itemDefinition.Classes != null)
            {
                item.Classes = itemDefinition.Classes.ToList();
            }

            item.ImageId = baseName.ImageId;
            item.Slots = itemDefinition.Slots;
            
            var prefix = string.Empty;
            var suffix = string.Empty;

            var statCount = Math.Min((int) rarity.Value, availableStats.Count);
            var chosenStats = new List<StatType>();
            for (var i = 0; i < statCount; i++)
            {
                var index = Nez.Random.NextInt(availableStats.Count);
                var stat = availableStats.ToArray()[index];
                availableStats.Remove(stat);
                chosenStats.Add(stat);
            }

            foreach (var stat in chosenStats.OrderBy(i => (int)i))
            {
                var itemLevel = Nez.Random.NextInt(maxLevel-minLevel) + minLevel;
                item.MinLevel = Math.Max(itemLevel, item.MinLevel);
                var statValue = Math.Max(itemLevel - 5 + Nez.Random.NextInt(6), 1);
                item.Stats.Add(new StatValue()
                {
                    Type = stat,
                    Value = statValue
                });

                var statName = statNames.FirstOrDefault(i => i.Type == stat);
                if (statName != null)
                {
                    if (string.IsNullOrEmpty(suffix) && statName.Suffix != null)
                    {
                        var statLevel =  Math.Min((int)(itemLevel / 25.0f * statName.Suffix.Count) , statName.Suffix.Count-1);
                        suffix = statName.Suffix[statLevel];
                    }
                    else if(statName.Prefix != null)    
                    {
                        var statLevel =  Math.Min((int)(itemLevel / 25.0f * statName.Prefix.Count) , statName.Prefix.Count-1);
                        if (string.IsNullOrEmpty(prefix))
                        {
                            prefix = statName.Prefix[statLevel];
                        }
                        else
                        {
                            prefix += " " + statName.Prefix[statLevel];
                        }
                    }
                }
            }

            var name = baseName.Name;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                name = $@"{prefix} {name}";
            }
            
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                name = $@"{name} of {suffix}";
            }

            item.Name = name;
            item.Cost = item.MinLevel * ((int) rarity.Value + 1) * 100;
            item.Cost += Nez.Random.NextInt(item.Cost / 3);


            if (Core.GraphicsDevice == null)
            {
                return item;
            }

            var tileSet = Game.LoadTileSet("Content/items2.tsx");
            item.Setup(tileSet);
            return item;
        }

        [JsonProperty("Slots", ItemConverterType=typeof(StringEnumConverter))]
        public List<Slot> Slots { get; set; }

        [JsonIgnore]
        public Sprite Image { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; } = ItemType.Unknown;
        
        public string Name { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; set; }

        public List<StatValue> Stats { get; set; } = new();
        
        public int Cost { get; set; }
        public int MinLevel { get; set; }

        [JsonProperty("Classes", ItemConverterType=typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }

        [JsonIgnore]
        public bool CanBeSoldInStore => this.Type != ItemType.Gold &&
                                        this.Type != ItemType.Key &&
                                        this.Type != ItemType.Unknown;
    }
}