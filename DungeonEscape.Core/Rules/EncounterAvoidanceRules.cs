using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public enum EncounterAvoidanceMethod
    {
        SneakAway,
        TalkDown,
        LeavePeacefully
    }

    public sealed class EncounterAvoidanceContext
    {
        public bool PartyUnnoticed { get; set; }
        public bool CanTalk { get; set; }
        public bool NonAggressive { get; set; }
        public int PassivePerceptionDc { get; set; }
        public int SocialDc { get; set; }
    }

    public sealed class EncounterAvoidanceResult
    {
        public bool Success { get; set; }
        public double XpMultiplier { get; set; }
        public string SkillName { get; set; }
        public int DifficultyClass { get; set; }
        public int RollTotal { get; set; }
        public string Message { get; set; }
    }

    public static class EncounterAvoidanceRules
    {
        public static EncounterAvoidanceContext CreateContext(
            Party party,
            IEnumerable<Monster> monsters,
            Func<int, int> rollDie)
        {
            var monsterList = (monsters ?? new List<Monster>()).Where(monster => monster != null).ToList();
            var passivePerception = GetEncounterPassivePerception(monsterList);
            return new EncounterAvoidanceContext
            {
                PartyUnnoticed = RollGroupSkillCheck(party, "Stealth", passivePerception, rollDie).Success,
                CanTalk = monsterList.Any(CanBeReasonedWith),
                NonAggressive = monsterList.All(IsNonAggressive),
                PassivePerceptionDc = passivePerception,
                SocialDc = GetSocialDifficultyClass(monsterList)
            };
        }

        public static EncounterAvoidanceResult Resolve(
            Party party,
            IEnumerable<Monster> monsters,
            EncounterAvoidanceContext context,
            EncounterAvoidanceMethod method,
            Func<int, int> rollDie)
        {
            var monsterList = (monsters ?? new List<Monster>()).Where(monster => monster != null).ToList();
            switch (method)
            {
                case EncounterAvoidanceMethod.SneakAway:
                    return ResolveSneakAway(party, monsterList, context, rollDie);
                case EncounterAvoidanceMethod.TalkDown:
                    return ResolveTalkDown(party, monsterList, context, rollDie);
                case EncounterAvoidanceMethod.LeavePeacefully:
                    return ResolveLeavePeacefully(monsterList, context);
                default:
                    return Failure("The encounter cannot be avoided.");
            }
        }

        public static int GetEncounterXp(IEnumerable<Monster> monsters, double multiplier)
        {
            var total = (monsters ?? new List<Monster>())
                .Where(monster => monster != null)
                .Sum(monster => (double)monster.Xp);
            return Math.Max(0, (int)Math.Round(total * Math.Max(0d, multiplier), MidpointRounding.AwayFromZero));
        }

        private static EncounterAvoidanceResult ResolveSneakAway(
            Party party,
            List<Monster> monsters,
            EncounterAvoidanceContext context,
            Func<int, int> rollDie)
        {
            var dc = context != null && context.PartyUnnoticed
                ? Math.Max(8, GetEncounterPassivePerception(monsters) - 5)
                : GetEncounterPassivePerception(monsters);
            var check = RollGroupSkillCheck(party, "Stealth", dc, rollDie);
            return new EncounterAvoidanceResult
            {
                Success = check.Success,
                XpMultiplier = check.Success ? context != null && context.PartyUnnoticed ? 0.5d : 0.25d : 0d,
                SkillName = "Stealth",
                DifficultyClass = dc,
                RollTotal = check.BestTotal,
                Message = check.Success
                    ? "The party slips away before the encounter turns violent."
                    : "The party fails to slip away quietly."
            };
        }

        private static EncounterAvoidanceResult ResolveTalkDown(
            Party party,
            List<Monster> monsters,
            EncounterAvoidanceContext context,
            Func<int, int> rollDie)
        {
            if (context == null || !context.CanTalk)
            {
                return Failure("These creatures cannot be reasoned with.");
            }

            var dc = context.SocialDc <= 0 ? GetSocialDifficultyClass(monsters) : context.SocialDc;
            var bestCheck = new SkillCheckResult();
            var bestSkill = "Persuasion";
            foreach (var skill in new[] { "Persuasion", "Deception", "Intimidation" })
            {
                var check = RollBestSkillCheck(party, skill, dc, rollDie);
                if (!bestCheck.HasRoll || check.BestTotal > bestCheck.BestTotal)
                {
                    bestCheck = check;
                    bestSkill = skill;
                }
            }

            return new EncounterAvoidanceResult
            {
                Success = bestCheck.Success,
                XpMultiplier = bestCheck.Success ? 1d : 0d,
                SkillName = bestSkill,
                DifficultyClass = dc,
                RollTotal = bestCheck.BestTotal,
                Message = bestCheck.Success
                    ? "The party talks its way out of the fight."
                    : "The attempt to defuse the encounter fails."
            };
        }

        private static EncounterAvoidanceResult ResolveLeavePeacefully(
            List<Monster> monsters,
            EncounterAvoidanceContext context)
        {
            if (context == null || !context.NonAggressive)
            {
                return Failure("The creatures are not willing to let the party leave peacefully.");
            }

            return new EncounterAvoidanceResult
            {
                Success = true,
                XpMultiplier = 0d,
                Message = "The party leaves the creatures in peace."
            };
        }

        private static EncounterAvoidanceResult Failure(string message)
        {
            return new EncounterAvoidanceResult
            {
                Success = false,
                Message = message
            };
        }

        private static int GetEncounterPassivePerception(IEnumerable<Monster> monsters)
        {
            var values = (monsters ?? new List<Monster>())
                .Where(monster => monster != null)
                .Select(monster => 10 + DndStatRules.GetAbilityModifier(monster.Wisdom <= 0 ? 10 : monster.Wisdom) + Math.Max(0, monster.ProficiencyBonus))
                .ToList();
            return values.Count == 0 ? 10 : Math.Max(8, values.Max());
        }

        private static int GetSocialDifficultyClass(IEnumerable<Monster> monsters)
        {
            var monsterList = (monsters ?? new List<Monster>()).Where(monster => monster != null).ToList();
            if (monsterList.Count == 0)
            {
                return 10;
            }

            var dc = 10 + Math.Min(8, Math.Max(0, monsterList.Max(monster => monster.MinLevel) / 2));
            if (monsterList.Any(IsHostile))
            {
                dc += 3;
            }

            if (monsterList.All(IsNonAggressive))
            {
                dc -= 2;
            }

            return Math.Max(8, dc);
        }

        private static SkillCheckResult RollGroupSkillCheck(Party party, string skillName, int dc, Func<int, int> rollDie)
        {
            var members = party == null ? new List<Hero>() : party.AliveMembers.Where(hero => hero != null && !hero.IsDead).ToList();
            if (members.Count == 0)
            {
                return new SkillCheckResult();
            }

            var successes = 0;
            var best = int.MinValue;
            foreach (var hero in members)
            {
                var total = RollSkillTotal(hero, skillName, rollDie);
                best = Math.Max(best, total);
                if (total >= dc)
                {
                    successes++;
                }
            }

            return new SkillCheckResult
            {
                HasRoll = true,
                Success = successes >= Math.Max(1, (members.Count + 1) / 2),
                BestTotal = best
            };
        }

        private static SkillCheckResult RollBestSkillCheck(Party party, string skillName, int dc, Func<int, int> rollDie)
        {
            var members = party == null ? new List<Hero>() : party.AliveMembers.Where(hero => hero != null && !hero.IsDead).ToList();
            if (members.Count == 0)
            {
                return new SkillCheckResult();
            }

            var best = members.Max(hero => RollSkillTotal(hero, skillName, rollDie));
            return new SkillCheckResult
            {
                HasRoll = true,
                Success = best >= dc,
                BestTotal = best
            };
        }

        private static int RollSkillTotal(Hero hero, string skillName, Func<int, int> rollDie)
        {
            var roll = rollDie == null ? Dice.RollDie(20) : rollDie(20);
            return roll + DndStatRules.GetSkillCheckModifier(hero, skillName, hero != null && hero.HasSkillProficiency(skillName));
        }

        private static bool CanBeReasonedWith(Monster monster)
        {
            return monster != null && monster.CanBeReasonedWith;
        }

        private static bool IsNonAggressive(Monster monster)
        {
            if (monster == null)
            {
                return false;
            }

            if (IsHostile(monster))
            {
                return false;
            }

            return monster.NonAggressive;
        }

        private static bool IsHostile(Monster monster)
        {
            return monster != null && monster.Hostile;
        }

        private struct SkillCheckResult
        {
            public bool HasRoll { get; set; }
            public bool Success { get; set; }
            public int BestTotal { get; set; }
        }
    }
}
