using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.ViewModels
{
    public sealed class CombatViewModel
    {
        public int State { get; private set; }
        public int SelectedMenuIndex { get; private set; }

        public void Reset()
        {
            State = 0;
            SelectedMenuIndex = 0;
        }

        public void SetState(int value)
        {
            State = value;
        }

        public void SetSelectedMenuIndex(int value)
        {
            SelectedMenuIndex = value;
        }

        public int MoveSelection(int moveY, int count, bool wrap)
        {
            if (count <= 0)
            {
                SelectedMenuIndex = 0;
                return SelectedMenuIndex;
            }

            var nextIndex = SelectedMenuIndex + (moveY > 0 ? 1 : -1);
            SelectedMenuIndex = wrap ? WrapIndex(nextIndex, count) : Clamp(nextIndex, 0, count - 1);
            return SelectedMenuIndex;
        }

        public List<CombatActionRow> GetActionRows(Hero hero, bool hasEncounterSpells, IList<Skill> encounterSkills, bool hasEncounterItems)
        {
            var rows = new List<CombatActionRow>
            {
                new CombatActionRow { Label = "Fight", Kind = CombatActionKind.Fight }
            };

            if (hero != null && !HasStopSpell(hero) && hasEncounterSpells)
            {
                rows.Add(new CombatActionRow { Label = "Spell", Kind = CombatActionKind.Spell });
            }

            if (hero != null && encounterSkills != null)
            {
                for (var i = 0; i < encounterSkills.Count; i++)
                {
                    var skill = encounterSkills[i];
                    if (skill != null)
                    {
                        rows.Add(new CombatActionRow { Label = skill.Name, Kind = CombatActionKind.Skill, SkillIndex = i });
                    }
                }
            }

            if (hero != null && hasEncounterItems)
            {
                rows.Add(new CombatActionRow { Label = "Item", Kind = CombatActionKind.Item });
            }

            rows.Add(new CombatActionRow { Label = "Run", Kind = CombatActionKind.Run });
            return rows;
        }

        public List<CombatMenuRow> GetSpellRows(IList<Spell> spells)
        {
            var rows = new List<CombatMenuRow>();
            if (spells == null)
            {
                return rows;
            }

            for (var i = 0; i < spells.Count; i++)
            {
                var spell = spells[i];
                if (spell != null)
                {
                    rows.Add(new CombatMenuRow { Index = i, Label = spell.Name + "  " + spell.Cost + " MP" });
                }
            }

            return rows;
        }

        public List<CombatMenuRow> GetItemRows(IList<ItemInstance> items)
        {
            var rows = new List<CombatMenuRow>();
            if (items == null)
            {
                return rows;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item != null)
                {
                    rows.Add(new CombatMenuRow { Index = i, Label = item.NameWithStats });
                }
            }

            return rows;
        }

        public IFighter GetSelectedTarget(IList<IFighter> candidates)
        {
            return candidates != null && SelectedMenuIndex >= 0 && SelectedMenuIndex < candidates.Count
                ? candidates[SelectedMenuIndex]
                : null;
        }

        public bool IsTargetCandidate(IList<IFighter> candidates, IFighter fighter)
        {
            if (candidates == null || fighter == null)
            {
                return false;
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                if (ReferenceEquals(candidates[i], fighter))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasPartyTargets(IList<IFighter> candidates)
        {
            return HasTargetType<Hero>(candidates);
        }

        public bool HasMonsterTargets(IList<IFighter> candidates)
        {
            return HasTargetType<MonsterInstance>(candidates);
        }

        public int GetTargetIndex(IList<IFighter> candidates, IFighter target)
        {
            if (candidates == null || target == null)
            {
                return -1;
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                if (ReferenceEquals(candidates[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (index < 0)
            {
                return count - 1;
            }

            return index >= count ? 0 : index;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static bool HasStopSpell(Hero hero)
        {
            if (hero == null || hero.Status == null)
            {
                return false;
            }

            for (var i = 0; i < hero.Status.Count; i++)
            {
                if (hero.Status[i] != null && hero.Status[i].Type == EffectType.StopSpell)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTargetType<T>(IList<IFighter> candidates)
        {
            if (candidates == null)
            {
                return false;
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] is T)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
