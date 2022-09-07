// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Nez.Textures;
    using Nez.Tiled;

    public class Spell
    {
        private static readonly List<SkillType> EncounterSpells = new() {SkillType.Heal, SkillType.Damage, SkillType.Revive, SkillType.Dot, SkillType.Sleep, SkillType.Confusion, SkillType.StopSpell, SkillType.Buff, SkillType.Decrease, SkillType.Clear, SkillType.StatDecrease, SkillType.Steal};

        private static readonly List<SkillType> NonEncounterSpells = new() {SkillType.Heal, SkillType.Outside, SkillType.Return, SkillType.Revive, SkillType.Clear, SkillType.Repel, SkillType.StatIncrease};
        
        [JsonIgnore]
        public bool IsNonEncounterSpell => NonEncounterSpells.Contains(this.Skill.Type);

        [JsonIgnore]
        public bool IsEncounterSpell => EncounterSpells.Contains(this.Skill.Type);
        
        [JsonIgnore]
        public bool IsAttackSpell => this.Skill.IsAttackSkill;
        
        [JsonIgnore] private Skill Skill { get; set; }
        
        [JsonIgnore]
        public Sprite Image { get; private set; }
        [JsonIgnore] public Target Targets => Skill?.Targets ?? Target.None;
        [JsonIgnore] public SkillType Type => Skill?.Type ?? SkillType.None;
        [JsonIgnore] public int MaxTargets => Skill?.MaxTargets ?? 0;
        
        [JsonProperty("Skill")]
        public string SkillId { get; set; }
        
        public int ImageId { get; set; }
        public int Cost { get; set; }
        public int MinLevel { get; set; }

        [JsonProperty("Classes", ItemConverterType=typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }
        
        public string Name { get; set; }
        
        
        public void Setup(TmxTileset tileset, IEnumerable<Skill> skills)
        {
            this.Image = tileset.Image != null ? new Sprite(tileset.Image.Texture, tileset.TileRegions[this.ImageId]) : new Sprite(tileset.Tiles[this.ImageId].Image.Texture);
            this.Skill = skills.FirstOrDefault(i => i.Name == this.SkillId);
        }


        public string Cast(IEnumerable<IFighter> targets, IFighter caster, IGame game, int round = 0)
        {
            if (caster.Magic < this.Cost)
            {
                return $"{caster.Name}: I do not have enough magic to cast {this.Name}.";
            }

            caster.Magic -= this.Cost;
            
            game.Sounds.PlaySoundEffect("spell", true);

            var message = $"{caster.Name} casts {this.Name}\n";
            if (this.Skill == null)
            {
                message += "but it did not work\n";
                return message;
            }

            var hit = false;
            foreach (var target in targets.Where(i => (this.Skill.Type == SkillType.Revive || !i.IsDead) && !i.RanAway))
            {
                if (IsAttackSpell && !caster.CanHit(target))
                {
                    message += $"{target.Name} dodges the spell\n";
                    continue;
                }
                
                var result = ("", false);
                if (this.Skill != null)
                {
                    result = this.Skill.Do(target, caster, game, round, true);
                }

                if (string.IsNullOrEmpty(result.Item1))
                {
                    result.Item1 = "but it did not work\n";
                }

                message += result.Item1;
                if (result.Item2)
                {
                    hit = true;
                }
            }

            if (IsAttackSpell)
            {
                game.Sounds.PlaySoundEffect(hit? "receive-damage" : "miss" );
            }
            
            return message;
        }
    }
}