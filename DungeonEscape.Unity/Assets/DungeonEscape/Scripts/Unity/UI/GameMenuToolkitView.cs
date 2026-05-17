using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine.UIElements;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public static class GameMenuToolkitView
    {
        public static void BuildSaveRows(
            VisualElement root,
            IEnumerable<GameMenuSaveSlotRow> rows,
            int selectedIndex,
            Action<int> onSelect)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();
            root.AddToClassList("game-menu-save-list");
            var rowIndex = 0;
            foreach (var row in rows ?? new List<GameMenuSaveSlotRow>())
            {
                var button = new Button(() =>
                {
                    if (onSelect != null)
                    {
                        onSelect(row.SlotIndex);
                    }
                })
                {
                    text = row.Title + "\n" + row.Summary
                };
                button.AddToClassList(rowIndex == selectedIndex ? "game-menu-save-list__row--selected" : "game-menu-save-list__row");
                root.Add(button);
                rowIndex++;
            }
        }

        public static void BuildModal(
            VisualElement root,
            string title,
            string message,
            IEnumerable<string> choices,
            int selectedIndex,
            Action<int> onSelect)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();
            root.AddToClassList("game-menu-modal");
            root.Add(new Label(title) { name = "GameMenuModalTitle" });
            root.Add(new Label(message) { name = "GameMenuModalMessage" });

            var choiceIndex = 0;
            foreach (var choice in choices ?? new List<string>())
            {
                var currentIndex = choiceIndex;
                var button = new Button(() =>
                {
                    if (onSelect != null)
                    {
                        onSelect(currentIndex);
                    }
                })
                {
                    text = choice
                };
                button.AddToClassList(choiceIndex == selectedIndex ? "game-menu-modal__choice--selected" : "game-menu-modal__choice");
                root.Add(button);
                choiceIndex++;
            }
        }
    }
}
