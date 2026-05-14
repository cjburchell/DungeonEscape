using Redpoint.DungeonEscape.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class CombatWindow
    {
        private void MoveSelection(int moveY)
        {
            var count = GetCurrentSelectionCount();
            if (count <= 0)
            {
                selectedMenuIndex = 0;
                return;
            }

            var previousIndex = selectedMenuIndex;
            var nextIndex = selectedMenuIndex + (moveY > 0 ? 1 : -1);
            selectedMenuIndex = IsMonsterTargetSelection()
                ? WrapIndex(nextIndex, count)
                : Mathf.Clamp(nextIndex, 0, count - 1);
            if (selectedMenuIndex != previousIndex)
            {
                UiControls.PlaySelectSound();
            }
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

        private int GetCurrentSelectionCount()
        {
            switch (state)
            {
                case CombatState.ChooseAction:
                    return BuildActionButtons().Count();
                case CombatState.ChooseSpell:
                    return actingHero == null ? 0 : GetAvailableEncounterSpells(actingHero).Count();
                case CombatState.ChooseItem:
                    return actingHero == null ? 0 : GetAvailableEncounterItems(actingHero).Count();
                case CombatState.ChooseTarget:
                    return targetSelectionCandidates == null ? 0 : targetSelectionCandidates.Count;
                default:
                    return 0;
            }
        }

        private void ActivateSelection()
        {
            switch (state)
            {
                case CombatState.ChooseAction:
                    var actions = BuildActionButtons().ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < actions.Count)
                    {
                        UiControls.PlayConfirmSound();
                        RememberAction(actions[selectedMenuIndex].Label);
                        actions[selectedMenuIndex].Action();
                    }

                    return;
                case CombatState.ChooseSpell:
                    var spells = actingHero == null ? new List<Spell>() : GetAvailableEncounterSpells(actingHero).ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < spells.Count)
                    {
                        UiControls.PlayConfirmSound();
                        ResolveHeroSpell(spells[selectedMenuIndex]);
                    }

                    return;
                case CombatState.ChooseItem:
                    var items = actingHero == null ? new List<ItemInstance>() : GetAvailableEncounterItems(actingHero).ToList();
                    if (selectedMenuIndex >= 0 && selectedMenuIndex < items.Count)
                    {
                        UiControls.PlayConfirmSound();
                        ResolveHeroItem(items[selectedMenuIndex]);
                    }

                    return;
                case CombatState.ChooseTarget:
                    ActivateTargetSelection(selectedMenuIndex);
                    return;
            }
        }

        private void ReturnToActionMenu()
        {
            if (state == CombatState.ChooseAction)
            {
                return;
            }

            targetSelectionDone = null;
            targetSelectionCandidates.Clear();
            state = CombatState.ChooseAction;
            selectedMenuIndex = GetRememberedActionIndex(BuildActionButtons().ToList());
            messageText = actingHero == null ? "Choose an action." : actingHero.Name + "'s turn.";
        }
    }
}
