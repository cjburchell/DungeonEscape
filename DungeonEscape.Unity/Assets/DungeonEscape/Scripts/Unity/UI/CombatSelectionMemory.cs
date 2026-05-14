using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Unity.UI
{
    internal sealed class CombatSelectionMemory
    {
        private readonly Dictionary<string, HeroCombatSelection> selections =
            new Dictionary<string, HeroCombatSelection>(StringComparer.OrdinalIgnoreCase);

        public void Clear()
        {
            selections.Clear();
        }

        public void RememberAction(Hero hero, string label)
        {
            var selection = GetSelection(hero);
            if (selection != null && !string.IsNullOrEmpty(label))
            {
                selection.ActionLabel = label;
            }
        }

        public void RememberSpell(Hero hero, Spell spell)
        {
            var selection = GetSelection(hero);
            if (selection != null && spell != null)
            {
                selection.SpellName = spell.Name;
            }
        }

        public void RememberItem(Hero hero, ItemInstance item)
        {
            var selection = GetSelection(hero);
            if (selection != null && item != null)
            {
                selection.ItemId = item.Id;
                selection.ItemName = item.Name;
            }
        }

        public void RememberTarget(Hero hero, IFighter target)
        {
            var selection = GetSelection(hero);
            if (selection != null && target != null)
            {
                selection.TargetName = target.Name;
            }
        }

        public int GetRememberedActionIndex(Hero hero, IList<CombatButton> actions)
        {
            var selection = GetSelection(hero);
            return GetIndexOrDefault(actions, action => action.Label, selection == null ? null : selection.ActionLabel);
        }

        public int GetRememberedSpellIndex(Hero hero, IList<Spell> spells)
        {
            var selection = GetSelection(hero);
            return GetIndexOrDefault(spells, spell => spell.Name, selection == null ? null : selection.SpellName);
        }

        public int GetRememberedItemIndex(Hero hero, IList<ItemInstance> items)
        {
            var selection = GetSelection(hero);
            if (selection == null)
            {
                return 0;
            }

            var byId = GetIndexOrDefault(items, item => item.Id, selection.ItemId);
            return byId != 0 || string.IsNullOrEmpty(selection.ItemId)
                ? byId
                : GetIndexOrDefault(items, item => item.Name, selection.ItemName);
        }

        public int GetRememberedTargetIndex(Hero hero, IList<IFighter> targets)
        {
            var selection = GetSelection(hero);
            return GetIndexOrDefault(targets, target => target.Name, selection == null ? null : selection.TargetName);
        }

        private HeroCombatSelection GetSelection(Hero hero)
        {
            if (hero == null || string.IsNullOrEmpty(hero.Name))
            {
                return null;
            }

            HeroCombatSelection selection;
            if (!selections.TryGetValue(hero.Name, out selection))
            {
                selection = new HeroCombatSelection();
                selections[hero.Name] = selection;
            }

            return selection;
        }

        private static int GetIndexOrDefault<T>(IList<T> values, Func<T, string> getKey, string rememberedKey)
        {
            if (values == null || values.Count == 0 || string.IsNullOrEmpty(rememberedKey))
            {
                return 0;
            }

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value != null && string.Equals(getKey(value), rememberedKey, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return 0;
        }

        private sealed class HeroCombatSelection
        {
            public string ActionLabel { get; set; }
            public string SpellName { get; set; }
            public string ItemId { get; set; }
            public string ItemName { get; set; }
            public string TargetName { get; set; }
        }
    }
}
