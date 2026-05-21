using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine;
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
                    text = string.Empty
                };
                button.AddToClassList(rowIndex == selectedIndex ? "game-menu-save-list__row--selected" : "game-menu-save-list__row");
                button.Add(CreateButtonLabel(row.Title + "\n" + row.Summary, rowIndex == selectedIndex));
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
            var titleLabel = new Label(title) { name = "GameMenuModalTitle" };
            ToolkitTextStyles.Apply(titleLabel, Color.white, 18);
            root.Add(titleLabel);
            var messageLabel = new Label(message) { name = "GameMenuModalMessage" };
            ToolkitTextStyles.Apply(messageLabel, Color.white, 15);
            root.Add(messageLabel);

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
                    text = string.Empty
                };
                button.AddToClassList(choiceIndex == selectedIndex ? "game-menu-modal__choice--selected" : "game-menu-modal__choice");
                button.Add(CreateButtonLabel(choice, choiceIndex == selectedIndex));
                root.Add(button);
                choiceIndex++;
            }
        }

        private static Label CreateButtonLabel(string text, bool selected)
        {
            var label = new Label(text);
            label.AddToClassList(selected ? "game-menu-toolkit-button-label--selected" : "game-menu-toolkit-button-label");
            ToolkitTextStyles.Apply(label, selected ? Color.black : Color.white, 15);
            return label;
        }
    }
}
