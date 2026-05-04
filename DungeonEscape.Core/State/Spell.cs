using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.State
{
    public class Spell
    {
        [JsonIgnore]
        public bool IsNonEncounterSpell { get { return Skill != null && Skill.IsNonEncounterSkill; } }

        [JsonIgnore]
        public bool IsEncounterSpell { get { return Skill != null && Skill.IsEncounterSkill; } }

        [JsonIgnore]
        public bool IsAttackSpell { get { return Skill != null && Skill.IsAttackSkill; } }

        [JsonIgnore]
        private Skill Skill { get; set; }

        [JsonIgnore]
        public Target Targets { get { return Skill == null ? Target.None : Skill.Targets; } }

        [JsonIgnore]
        public SkillType Type { get { return Skill == null ? SkillType.None : Skill.Type; } }

        [JsonIgnore]
        public int MaxTargets { get { return Skill == null ? 0 : Skill.MaxTargets; } }

        [JsonProperty("Skill")]
        public string SkillId { get; set; }

        public int ImageId { get; set; }
        public int Cost { get; set; }
        public int MinLevel { get; set; }

        [JsonProperty("Classes", ItemConverterType = typeof(StringEnumConverter))]
        public List<Class> Classes { get; set; }

        public string Name { get; set; }

        public void Setup(IEnumerable<Skill> skills)
        {
            Skill = skills == null ? null : skills.FirstOrDefault(i => i.Name == SkillId);
        }

        public string Cast(IEnumerable<IFighter> targets, IEnumerable<BaseState> targetObjects, IFighter caster, IGame game, int round = 0)
        {
            if (caster.Magic < Cost)
            {
                return caster.Name + ": I do not have enough magic to cast " + Name + ".";
            }

            caster.Magic -= Cost;

            if (game != null && game.Sounds != null)
            {
                game.Sounds.PlaySoundEffect("spell", true);
            }

            var message = caster.Name + " casts " + Name + "\n";
            if (Skill == null)
            {
                return message + "but it did not work\n";
            }

            var hit = false;
            switch (Targets)
            {
                case Target.Single:
                case Target.Group:
                    foreach (var target in targets.Where(i => (Skill.Type == SkillType.Revive || !i.IsDead) && !i.RanAway))
                    {
                        if (IsAttackSpell && !caster.CanHit(target))
                        {
                            message += target.Name + " dodges the spell\n";
                            continue;
                        }

                        var result = Skill.Do(target, caster, null, game, round, true);
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
                    break;
                case Target.Object:
                    foreach (var targetObject in targetObjects)
                    {
                        var result = Skill.Do(null, caster, targetObject, game, round, true);
                        if (string.IsNullOrEmpty(result.Item1))
                        {
                            result.Item1 = "but it did not work\n";
                        }

                        message += result.Item1;
                    }
                    break;
                case Target.None:
                    var noneResult = Skill.Do(null, caster, null, game, round, true);
                    if (string.IsNullOrEmpty(noneResult.Item1))
                    {
                        noneResult.Item1 = "but it did not work\n";
                    }

                    message += noneResult.Item1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (IsAttackSpell && game != null && game.Sounds != null)
            {
                game.Sounds.PlaySoundEffect(hit ? "receive-damage" : "miss");
            }

            return message;
        }
    }
}
