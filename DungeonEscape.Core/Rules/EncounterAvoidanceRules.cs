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
        Persuade,
        Deceive,
        Intimidate,
        LeavePeacefully
    }

    public sealed class EncounterAvoidanceContext
    {
        public bool PartyUnnoticed { get; set; }
        public bool CanTalk { get; set; }
        public bool NonAggressive { get; set; }
        public bool PartySpotted { get; set; }
        public bool ImmediateAttack { get; set; }
        public int PassivePerceptionDc { get; set; }
        public int SpottingRollTotal { get; set; }
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
            var spottingCheck = RollWorstPartySkillCheck(party, "Stealth", passivePerception, rollDie);
            var spotted = !spottingCheck.Success;
            return new EncounterAvoidanceContext
            {
                PartyUnnoticed = spottingCheck.Success,
                CanTalk = monsterList.Any(CanBeReasonedWith),
                NonAggressive = monsterList.All(IsNonAggressive),
                PartySpotted = spotted,
                ImmediateAttack = spotted && monsterList.Any(IsHostile) && RollImmediateAttack(rollDie),
                PassivePerceptionDc = passivePerception,
                SpottingRollTotal = spottingCheck.BestTotal,
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
                case EncounterAvoidanceMethod.Persuade:
                    return ResolveSocialCheck(party, monsterList, context, "Persuasion", rollDie);
                case EncounterAvoidanceMethod.Deceive:
                    return ResolveSocialCheck(party, monsterList, context, "Deception", rollDie);
                case EncounterAvoidanceMethod.Intimidate:
                    return ResolveSocialCheck(party, monsterList, context, "Intimidation", rollDie);
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

        public static int GetLeadSkillModifier(Party party, string skillName)
        {
            var lead = GetLeadHero(party);
            return lead == null
                ? 0
                : DndStatRules.GetSkillCheckModifier(lead, skillName, lead.HasSkillProficiency(skillName));
        }

        private static EncounterAvoidanceResult ResolveSneakAway(
            Party party,
            List<Monster> monsters,
            EncounterAvoidanceContext context,
            Func<int, int> rollDie)
        {
            var dc = context != null && context.PartyUnnoticed
                ? Math.Max(8, GetEncounterPassivePerception(monsters) - 2)
                : GetEncounterPassivePerception(monsters);
            var lead = GetLeadHero(party);
            var check = RollLeadSkillCheck(party, "Stealth", dc, rollDie);
            var leadName = GetLeadName(lead);
            return new EncounterAvoidanceResult
            {
                Success = check.Success,
                XpMultiplier = check.Success ? context != null && context.PartyUnnoticed ? 0.5d : 0.25d : 0d,
                SkillName = "Stealth",
                DifficultyClass = dc,
                RollTotal = check.BestTotal,
                Message = check.Success
                    ? leadName + " signals the party to keep low, and the party slips away before the encounter turns violent."
                    : leadName + " tries to guide the party away, but the movement draws attention."
            };
        }

        private static EncounterAvoidanceResult ResolveSocialCheck(
            Party party,
            List<Monster> monsters,
            EncounterAvoidanceContext context,
            string skillName,
            Func<int, int> rollDie)
        {
            if (context == null || !context.CanTalk)
            {
                return Failure("These creatures cannot be reasoned with.");
            }

            var dc = context.SocialDc <= 0 ? GetSocialDifficultyClass(monsters) : context.SocialDc;
            var lead = GetLeadHero(party);
            var check = RollLeadSkillCheck(party, skillName, dc, rollDie);
            var leadName = GetLeadName(lead);

            return new EncounterAvoidanceResult
            {
                Success = check.Success,
                XpMultiplier = check.Success ? 1d : 0d,
                SkillName = skillName,
                DifficultyClass = dc,
                RollTotal = check.BestTotal,
                Message = GetSocialOutcomeMessage(leadName, skillName, check.Success)
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
                Message = "The party gives the creatures space, and they let the party pass without a fight."
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

        private static SkillCheckResult RollLeadSkillCheck(Party party, string skillName, int dc, Func<int, int> rollDie)
        {
            var lead = GetLeadHero(party);
            if (lead == null)
            {
                return new SkillCheckResult();
            }

            var total = RollSkillTotal(lead, skillName, rollDie);
            return new SkillCheckResult
            {
                HasRoll = true,
                Success = total >= dc,
                BestTotal = total,
                SkillName = skillName
            };
        }

        private static SkillCheckResult RollWorstPartySkillCheck(Party party, string skillName, int dc, Func<int, int> rollDie)
        {
            var members = party == null ? new List<Hero>() : party.AliveMembers.Where(hero => hero != null && !hero.IsDead).ToList();
            if (members.Count == 0)
            {
                return new SkillCheckResult();
            }

            var worst = int.MaxValue;
            foreach (var hero in members)
            {
                var total = RollSkillTotal(hero, skillName, rollDie);
                worst = Math.Min(worst, total);
            }

            return new SkillCheckResult
            {
                HasRoll = true,
                Success = worst >= dc,
                BestTotal = worst,
                SkillName = skillName
            };
        }

        private static Hero GetLeadHero(Party party)
        {
            return party == null ? null : party.AliveMembers.FirstOrDefault(hero => hero != null && !hero.IsDead);
        }

        private static string GetLeadName(Hero hero)
        {
            return hero == null || string.IsNullOrWhiteSpace(hero.Name) ? "The lead adventurer" : hero.Name;
        }

        private static bool RollImmediateAttack(Func<int, int> rollDie)
        {
            var roll = rollDie == null ? Dice.RollDie(20) : rollDie(20);
            return roll <= 10;
        }

        private static string GetSocialOutcomeMessage(string leadName, string skillName, bool success)
        {
            switch (skillName)
            {
                case "Deception":
                    return success
                        ? leadName + " bluffs confidently, and the creatures hesitate long enough for the party to leave."
                        : leadName + "'s bluff falls apart, and the creatures turn hostile.";
                case "Intimidation":
                    return success
                        ? leadName + " makes a threat the creatures believe, and they back down."
                        : leadName + "'s threat only provokes them.";
                default:
                    return success
                        ? leadName + " lowers their weapon and talks the creatures down."
                        : leadName + " tries to reason with them, but the creatures are not convinced.";
            }
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
            public string SkillName { get; set; }
        }
    }
}
