using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine;
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
                ApplyButtonStyle(button, index == selectedIndex);
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
                ApplyButtonStyle(loadButton, row.LoadSelected);

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
                ApplyButtonStyle(deleteButton, row.DeleteSelected);
                deleteButton.style.marginLeft = 8;

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
            ApplyButtonStyle(backButton, backSelected);
            root.Add(backButton);
        }

        private static void ApplyButtonStyle(Button button, bool selected)
        {
            button.style.marginBottom = 8;
            button.style.minHeight = 34;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.color = selected ? new StyleColor(Color.black) : new StyleColor(Color.white);
            button.style.backgroundColor = selected
                ? new StyleColor(new Color(1f, 0.95f, 0.2f, 0.95f))
                : new StyleColor(new Color(0.08f, 0.08f, 0.08f, 0.92f));
        }
    }
}
