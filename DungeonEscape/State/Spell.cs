using System;
using System.Collections.Generic;
using DungeonEscape.Scenes;
using Nez.Tiled;
using Random = Nez.Random;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonEscape.State
{
    using System.Runtime.CompilerServices;


    public enum SpellType
    {
        Heal = 0,
        Outside = 1,
        Fireball = 2,
        Lighting =  3,
        Return = 4,
        Antidote = 5,
        Revive = 6,
        Open = 7
    }

    public class Spell
    {
        public static string CastHeal(Hero target, Hero caster, Spell spell)
        {
            caster.Magic -= spell.Cost;
            var oldHeath = target.Health;
            if (spell.Power >= 3)
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

            return $"{caster.Name} casts {spell.Name} on {caster.Name} who gains {target.Health - oldHeath} health";
        }
        
        public static string CastRevive(Hero target, Hero caster, Spell spell)
        {
            caster.Magic -= spell.Cost;
            target.Health = 1;
            return $"{caster.Name} casts {spell.Name} on {caster.Name}";
        }

        public static string CastOutside(Hero caster, Spell spell, IGame gameState)
        {
            if (gameState.Party.CurrentMapId == 0)
            {
                return $"{caster.Name} casts {spell.Name} but you are already outside";
            }

            caster.Magic -= spell.Cost;
            MapScene.SetMap(gameState);
            return null;
        }
        
        public static string CastReturn(Hero caster, Spell spell, IGame gameState)
        {
            if (gameState.Party.CurrentMapId != 0)
            {
                return $"{caster.Name} casts {spell.Name} but you are not outside";
            }
            
            if (!gameState.Party.SavedMapId.HasValue)
            {
                return $"{caster.Name} casts {spell.Name} but you have never saved your game";
            }

            caster.Magic -= spell.Cost;
            MapScene.SetMap(gameState, gameState.Party.SavedMapId, gameState.Party.SavedPoint);
            return null;
        }
        
        private static readonly List<SpellType> encounterSpells = new List<SpellType> {SpellType.Heal, SpellType.Fireball, SpellType.Lighting, SpellType.Antidote, SpellType.Revive };
        private static readonly List<SpellType> nonEncounterSpells = new List<SpellType> {SpellType.Heal, SpellType.Outside, SpellType.Return, SpellType.Antidote, SpellType.Revive, SpellType.Open};
        
        public override string ToString()
        {
            return this.Name;
        }
        

        private TmxTilesetTile tile;
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