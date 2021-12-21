// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Nez;
    using Nez.Textures;
    using Nez.Tiled;

    public class Spell
    {
        public string Cast(IEnumerable<IFighter> targets, IFighter caster, IGame game)
        {
            if (caster.Magic < this.Cost)
            {
                return $"{caster.Name}: I do not have enough magic to cast {this.Name}.";
            }

            caster.Magic -= this.Cost;

            return this.Type switch
            {
                SpellType.Heal => this.CastHeal(targets, caster, false, game),
                SpellType.Outside => this.CastOutside(caster as Hero, game),
                SpellType.Damage => this.CastDamage(targets, caster, game),
                SpellType.Return => this.CastReturn(caster as Hero, game),
                SpellType.Revive => this.CastHeal(targets, caster, true, game),
                _ => $"{caster.Name} casts {this.Name} but it did not work"
            };
        }

        private string CastDamage(IEnumerable<IFighter> targets, IFighter caster, IGame gameState)
        {
            gameState.Sounds.PlaySoundEffect("spell", true);
            var message = $"{caster.Name} casts {this.Name}\n";
            var totalDamage = 0;
            foreach (var target in targets)
            {
                var damage = Random.NextInt(this.Health) + this.HealthConst;
                totalDamage += damage;
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

            gameState.Sounds.PlaySoundEffect(totalDamage == 0 ? "miss" : "receive-damage");

            return message;
        }

        private string CastHeal(IEnumerable<IFighter> targets, IFighter caster, bool everyone, IGame gameState)
        {
            gameState.Sounds.PlaySoundEffect("spell");
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

        private string CastOutside(IFighter caster, IGame gameState)
        {
            if (gameState.Party.CurrentMapId == 0)
            {
                return $"{caster.Name} casts {this.Name} but you are already outside";
            }

            gameState.Sounds.PlaySoundEffect("spell");
            gameState.SetMap();
            return null;
        }

        private string CastReturn(IFighter caster, IGame gameState)
        {
            if (gameState.Party.CurrentMapId != 0)
            {
                return $"{caster.Name} casts {this.Name} but you are not outside";
            }

            if (!gameState.Party.SavedMapId.HasValue)
            {
                return $"{caster.Name} casts {this.Name} but you have never saved your game";
            }

            gameState.Sounds.PlaySoundEffect("spell");
            gameState.SetMap(gameState.Party.SavedMapId, null, gameState.Party.SavedPoint);
            return null;
        }

        private static readonly List<SpellType> AttackSpells = new List<SpellType> {SpellType.Damage};

        private static readonly List<SpellType> EncounterSpells = new List<SpellType>
            {SpellType.Heal, SpellType.Damage, SpellType.Revive};

        private static readonly List<SpellType> NonEncounterSpells = new List<SpellType>
            {SpellType.Heal, SpellType.Outside, SpellType.Return, SpellType.Revive};

        public override string ToString()
        {
            return this.Name;
        }
        
        public int ImageId { get; set; }
        
        public int Id { get; set; }

        public void Setup(TmxTileset tileset)
        {
            this.Image = tileset.Image != null ? new Sprite(tileset.Image.Texture, tileset.TileRegions[this.ImageId]) : new Sprite(tileset.Tiles[this.ImageId].Image.Texture);
        }

        [JsonIgnore]
        public bool IsNonEncounterSpell => NonEncounterSpells.Contains(this.Type);

        [JsonIgnore]
        public bool IsEncounterSpell => EncounterSpells.Contains(this.Type);

        [JsonIgnore]
        public bool IsAttackSpell => AttackSpells.Contains(this.Type);

        [JsonConverter(typeof(StringEnumConverter))]
        public Target Targets { get; set; }

        public int Cost { get; set; }

        public int MinLevel { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SpellType Type { get; set; }
        
        [JsonProperty("Classes", ItemConverterType=typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }

        public int Health { get; set; }

        public int HealthConst { get; set; }

        public string Name { get; set; }
        
        [JsonIgnore]
        public Sprite Image { get; private set; }
    }
}