using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Redpoint.DungeonEscape.Data
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
        public bool IsCantrip { get { return SpellLevel <= 0; } }

        [JsonIgnore]
        public bool IsBonusAction
        {
            get
            {
                if (Skill != null && Skill.IsBonusAction)
                {
                    return true;
                }

                var spellName = string.IsNullOrWhiteSpace(DndSpell) ? Name : DndSpell;
                return IsDndBonusActionSpell(spellName);
            }
        }

        [JsonIgnore]
        public bool RequiresConcentration
        {
            get
            {
                var spellName = string.IsNullOrWhiteSpace(DndSpell) ? Name : DndSpell;
                if (string.IsNullOrWhiteSpace(spellName))
                {
                    return false;
                }

                return new[]
                {
                    "Bane",
                    "Bestow Curse",
                    "Blur",
                    "Cloud of Daggers",
                    "Confusion",
                    "Conjure Barrage",
                    "Conjure Minor Elementals",
                    "Conjure Volley",
                    "Darkness",
                    "Faerie Fire",
                    "Flaming Sphere",
                    "Gaseous Form",
                    "Haste",
                    "Hold Person",
                    "Hypnotic Pattern",
                    "Invisibility",
                    "Keen",
                    "Lesser Restoration",
                    "Magic Mouth",
                    "Moonbeam",
                    "Protection from Energy",
                    "Sleet Storm",
                    "Slow",
                    "Silence",
                    "Spike Growth",
                    "Stinking Cloud",
                    "Wall of Fire",
                    "Web",
                    "Wind Wall"
                }.Any(candidate => string.Equals(candidate, spellName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool CanBeCastBy(IFighter caster)
        {
            return !RequiresConcentration || DndStatRules.CanConcentrate(caster);
        }

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
        public int SpellLevel { get; set; }
        public string School { get; set; }
        public string DndSpell { get; set; }
        public int MinLevel { get; set; }

        public List<string> Classes { get; set; }

        public string Name { get; set; }

        public Spell()
        {
            SpellLevel = 1;
        }

        private static bool IsDndBonusActionSpell(string spellName)
        {
            if (string.IsNullOrWhiteSpace(spellName))
            {
                return false;
            }

            return new[]
            {
                "Healing Word",
                "Mass Healing Word",
                "Misty Step",
                "Shield of Faith",
                "Spiritual Weapon"
            }.Any(candidate => string.Equals(candidate, spellName, StringComparison.OrdinalIgnoreCase));
        }

        public void Setup(IEnumerable<Skill> skills)
        {
            Skill = skills == null ? null : skills.FirstOrDefault(i => i.Name == SkillId);
        }

        public string Cast(IEnumerable<IFighter> targets, IEnumerable<BaseState> targetObjects, IFighter caster, IGame game, int round = 0)
        {
            var heroCaster = caster as Hero;
            if (heroCaster != null && !IsCantrip && !heroCaster.HasAvailableSpellSlot(SpellLevel))
            {
                return caster.Name + ": I do not have a spell slot for " + Name + ".";
            }

            if (RequiresConcentration && !CanBeCastBy(caster))
            {
                return caster == null ? "The spell cannot be cast while the caster cannot concentrate." : caster.Name + " cannot concentrate on " + Name + " right now.";
            }

            if (heroCaster != null && RequiresConcentration)
            {
                DndStatRules.StartConcentration(heroCaster, Name);
            }

            if (heroCaster != null && !IsCantrip)
            {
                heroCaster.UseSpellSlot(SpellLevel);
            }

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
