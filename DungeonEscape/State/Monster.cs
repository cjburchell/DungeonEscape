using System;
using System.Collections.Generic;
using System.Linq;
using Nez.Tiled;

namespace DungeonEscape.State
{
    using Microsoft.Xna.Framework.Graphics;

    public class Monster
    {
        private readonly TmxTilesetTile tile;
        
        public Monster(TmxTilesetTile tile, IReadOnlyCollection<Spell> spells)
        {
            this.tile = tile;
            this.Name = StringUtils.AddSpacesToSentence(tile.Type);
            this.Health = int.Parse(tile.Properties["Health"]);
            this.Attack = int.Parse(tile.Properties["Attack"]);
            this.HealthConst = int.Parse(tile.Properties["HealthConst"]);
            this.Gold = int.Parse(tile.Properties["Gold"]);
            this.XP = int.Parse(tile.Properties["XP"]);
            this.Defence = int.Parse(tile.Properties["Defence"]);
            this.Agility = int.Parse(tile.Properties["Agility"]);
            this.MinLevel = int.Parse(tile.Properties["MinLevel"]);
            this.Magic = int.Parse(tile.Properties["Magic"]);

            for (int i = 0; i < 10; i++)
            {
                var key = $"Spell{i}";
                if (tile.Properties.ContainsKey(key))
                {
                    var spellId = int.Parse(tile.Properties[key]);
                    var spell = spells.FirstOrDefault(item => item.Id == spellId);
                    this.Spells.Add(spell);
                }
            }
        }

        public int Id => this.tile.Id;

        public int Magic { get;  }

        public int MinLevel { get; }

        public Texture2D Image => this.tile.Image.Texture;
        
        public List<Spell> Spells { get; } = new List<Spell>();

        public int Agility { get; }

        public int Defence { get; }

        public int XP { get; }

        public int HealthConst { get; }
        
        public int Gold { get;  }

        public int Health { get;  }

        public int Attack { get;  }

        public string Name { get;  }
    }
}