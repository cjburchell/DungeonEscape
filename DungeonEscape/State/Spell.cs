using System;
using System.Collections.Generic;
using Nez.Tiled;
using Random = Nez.Random;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonEscape.State
{
    using System.Linq;

    public enum SpellType
    {
        Heal,
        Outside,
        Damage,
        Return,
        Revive
    }

    public enum Target
    {
        Single,
        Group,
    }

    public class Spell
    {
        public string Cast(IEnumerable<Fighter> targets, Fighter caster, IGame game)
        {
            if (caster.Magic < this.Cost)
            {
                return $"{caster.Name}: I do not have enough magic to cast {this.Name}.";
            }

            caster.Magic -= this.Cost;

            switch (this.Type)
            {
                case SpellType.Heal:
                    return this.CastHeal(targets, caster, false);
                case SpellType.Outside:
                    return this.CastOutside(caster as Hero, game);
                case SpellType.Damage:
                    return this.CastDamage(targets, caster);
                case SpellType.Return:
                    return this.CastReturn(caster as Hero, game);
                case SpellType.Revive:
                    return this.CastHeal(targets, caster, true);
                default:
                    return $"{caster.Name} casts {this.Name} but it did not work";
            }
        }

        private string CastDamage(IEnumerable<Fighter> targets, Fighter caster)
        {
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets)
            {
                var damage = Random.NextInt(this.Health) + this.HealthConst;
                target.Health -= damage;
                if (damage == 0)
                {
                    message += $"{target.Name} was unharmed\n";
                }
                else
                {
                    message += $"{target.Name} took {damage} points of damage\n";
                }

                if (target.Health <= 0)
                {
                    message += "and has died!\n";
                }
            }

            return message;
        }

        private string CastHeal(IEnumerable<Fighter> targets, Fighter caster, bool everyone)
        {
            var message = $"{caster.Name} casts {this.Name}\n";
            foreach (var target in targets.Where(item => everyone || !item.IsDead))
            {
                var oldHeath = target.Health;
                if (this.Health != 0)
                {
                    target.Health += Random.NextInt(this.Health) + this.HealthConst;
                }
                else
                {
                    target.Health = target.MaxHealth;
                }

                if (target.MaxHealth < target.Health)
                {
                    target.Health = target.MaxHealth;
                }

                if (oldHeath == 0)
                {
                    message += $"{target.Name} is revived and gains {target.Health - oldHeath} health\n";
                }
                else
                {
                    message += $"{target.Name} gains {target.Health - oldHeath} health\n";
                }
            }

            return message;
        }

        private string CastOutside(Hero caster, IGame gameState)
        {
            if (gameState.Party.CurrentMapId == 0)
            {
                return $"{caster.Name} casts {this.Name} but you are already outside";
            }

            gameState.SetMap();
            return null;
        }

        private string CastReturn(Hero caster, IGame gameState)
        {
            if (gameState.Party.CurrentMapId != 0)
            {
                return $"{caster.Name} casts {this.Name} but you are not outside";
            }

            if (!gameState.Party.SavedMapId.HasValue)
            {
                return $"{caster.Name} casts {this.Name} but you have never saved your game";
            }

            gameState.SetMap(gameState.Party.SavedMapId, gameState.Party.SavedPoint);
            return null;
        }

        private static readonly List<SpellType> attackSpells = new List<SpellType> {SpellType.Damage};

        private static readonly List<SpellType> encounterSpells = new List<SpellType>
            {SpellType.Heal, SpellType.Damage, SpellType.Revive};

        private static readonly List<SpellType> nonEncounterSpells = new List<SpellType>
            {SpellType.Heal, SpellType.Outside, SpellType.Return, SpellType.Revive};

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
            this.Targets = Enum.Parse<Target>(tile.Properties["Targets"]);
            if (tile.Properties.ContainsKey("Health"))
            {
                this.Health = int.Parse(tile.Properties["Health"]);
            }
            if (tile.Properties.ContainsKey("HealthConst"))
            {
                this.HealthConst = int.Parse(tile.Properties["HealthConst"]);
            }
        }

        public bool IsNonEncounterSpell => nonEncounterSpells.Contains(this.Type);

        public bool IsEncounterSpell => encounterSpells.Contains(this.Type);

        public bool IsAttackSpell => attackSpells.Contains(this.Type);

        public Target Targets { get; }

        public int Cost { get; }

        public int MinLevel { get; }

        public SpellType Type { get; }

        public int Health { get; }

        public int HealthConst { get; }

        public string Name { get; }
        public Texture2D Image => this.tile.Image.Texture;
    }
}