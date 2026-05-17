using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine.UIElements;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public static class TitleMenuToolkitView
    {
        public static void BuildMainMenu(
            VisualElement root,
            IEnumerable<TitleRow> rows,
            int selectedIndex,
            Action<TitleMainAction> onAction)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();
            root.AddToClassList("title-menu");
            var index = 0;
            foreach (var row in rows ?? new List<TitleRow>())
            {
                var button = new Button(() =>
                {
                    if (row.Enabled && onAction != null)
                    {
                        onAction(row.Action);
                    }
                })
                {
                    text = row.Label
                };
                button.SetEnabled(row.Enabled);
                button.AddToClassList(index == selectedIndex ? "title-menu__button--selected" : "title-menu__button");
                root.Add(button);
                index++;
            }
        }

        public static void BuildLoadMenu(
            VisualElement root,
            IEnumerable<TitleLoadSlotRow> rows,
            bool backSelected,
            Action<int> onLoad,
            Action<int> onDelete,
            Action onBack)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();
            root.AddToClassList("title-load-menu");
            foreach (var row in rows ?? new List<TitleLoadSlotRow>())
            {
                var rowElement = new VisualElement();
                rowElement.AddToClassList("title-load-menu__row");

                var loadButton = new Button(() =>
                {
                    if (onLoad != null)
                    {
                        onLoad(row.SlotIndex);
                    }
                })
                {
                    text = row.ButtonText
                };
                loadButton.AddToClassList(row.LoadSelected ? "title-load-menu__load--selected" : "title-load-menu__load");

                var deleteButton = new Button(() =>
                {
                    if (onDelete != null)
                    {
                        onDelete(row.SlotIndex);
                    }
                })
                {
                    text = row.DeleteButtonText
                };
                deleteButton.AddToClassList(row.DeleteSelected ? "title-load-menu__delete--selected" : "title-load-menu__delete");

                rowElement.Add(loadButton);
                rowElement.Add(deleteButton);
                root.Add(rowElement);
            }

            var backButton = new Button(() =>
            {
                if (onBack != null)
                {
                    onBack();
                }
            })
            {
                text = "Back"
            };
            backButton.AddToClassList(backSelected ? "title-load-menu__back--selected" : "title-load-menu__back");
            root.Add(backButton);
        }
    }
}
