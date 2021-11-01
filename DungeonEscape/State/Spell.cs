using System;
using System.Collections.Generic;
using Nez.Tiled;
using Random = Nez.Random;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonEscape.State
{
    public enum SpellType
    {
        Heal,
        Outside,
        Fireball,
        Lighting,
        Return,
        Revive
    }

    public class Spell
    {
        public string Cast(Fighter target, Fighter caster, IGame game)
        {
            if (caster.Magic < this.Cost)
            {
                return $"{caster.Name}: I do not have enough magic to cast {this.Name}.";
            }
            
            switch (this.Type)
            {
                case SpellType.Heal:
                    return this.CastHeal(target, caster);
                case SpellType.Outside:
                    return this.CastOutside(caster as Hero, game);
                case SpellType.Fireball:
                    return this.CastFireball(target, caster);
                case SpellType.Lighting:
                    return this.CastFireball(target, caster);
                case SpellType.Return:
                    return this.CastReturn(caster as Hero, game);
                case SpellType.Revive:
                    return this.CastRevive(target, caster);
                default:
                    return $"{caster.Name} casts {this.Name} but it did not work";
            }
        }

        private string CastFireball(Fighter target, Fighter caster)
        {
            // TODO: Cast Fireball/Lighting
            return $"{caster.Name} casts {this.Name} but it did not work";
        }

        private string CastHeal(Fighter target, Fighter caster)
        {
            caster.Magic -= this.Cost;
            var oldHeath = target.Health;
            if (this.Power >= 3)
            {
                target.Health = target.MaxHealth;
            }
            else
            {
                target.Health+= Random.NextInt(40)+5;
                if(target.MaxHealth < target.Health)
                {
                    target.Health = target.MaxHealth;
                }
            }

            return $"{caster.Name} casts {this.Name} on {caster.Name} who gains {target.Health - oldHeath} health";
        }
        
        public string CastRevive(Fighter target, Fighter caster)
        {
            caster.Magic -= this.Cost;
            target.Health = 1;
            return $"{caster.Name} casts {this.Name} on {caster.Name}";
        }

        public string CastOutside(Hero caster, IGame gameState)
        {
            if (gameState.Party.CurrentMapId == 0)
            {
                return $"{caster.Name} casts {this.Name} but you are already outside";
            }

            caster.Magic -= this.Cost;
            gameState.SetMap();
            return null;
        }
        
        public string CastReturn(Hero caster, IGame gameState)
        {
            if (gameState.Party.CurrentMapId != 0)
            {
                return $"{caster.Name} casts {this.Name} but you are not outside";
            }
            
            if (!gameState.Party.SavedMapId.HasValue)
            {
                return $"{caster.Name} casts {this.Name} but you have never saved your game";
            }

            caster.Magic -= this.Cost;
            gameState.SetMap(gameState.Party.SavedMapId, gameState.Party.SavedPoint);
            return null;
        }
        
        private static readonly List<SpellType> encounterSpells = new List<SpellType> {SpellType.Heal, SpellType.Fireball, SpellType.Lighting, SpellType.Revive };
        private static readonly List<SpellType> nonEncounterSpells = new List<SpellType> {SpellType.Heal, SpellType.Outside, SpellType.Return, SpellType.Revive };
        
        public override string ToString()
        {
            return this.Name;
        }
        

        private readonly TmxTilesetTile tile;
        public int Id => this.tile.Id;
        
        public Spell(TmxTilesetTile tile)
        {
            this.tile = tile;
            this.Name = StringUtils.AddSpacesToSentence(tile.Properties["Name"]);
            this.Type = Enum.Parse<SpellType>(this.tile.Type);
            this.MinLevel = int.Parse(tile.Properties["MinLevel"]);
            this.Cost = int.Parse(tile.Properties["Cost"]);
            this.Power = int.Parse(tile.Properties["Power"]);
        }
        
        public bool IsNonEncounterSpell => nonEncounterSpells.Contains(this.Type);
        
        public bool IsEncounterSpell => encounterSpells.Contains(this.Type);

        public int Power { get; }

        public int Cost { get; }

        public int MinLevel { get; }

        public SpellType Type { get; }

        public string Name { get; }
        public Texture2D Image => this.tile.Image.Texture;
    }
}