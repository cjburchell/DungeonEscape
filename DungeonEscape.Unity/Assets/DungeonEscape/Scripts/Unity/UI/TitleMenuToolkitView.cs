using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
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
            root.RemoveFromClassList("title-load-menu");
            root.AddToClassList("title-menu");
            var rowList = new List<TitleRow>(rows ?? new List<TitleRow>());
            var menu = new VisualElement();
            menu.name = "TitleMenuToolkitMainItems";
            menu.AddToClassList("title-menu__items");
            ApplyMainMenuLayout(menu, rowList.Count);
            var index = 0;
            foreach (var row in rowList)
            {
                var button = new Button(() =>
                {
                    if (row.Enabled && onAction != null)
                    {
                        onAction(row.Action);
                    }
                })
                {
                    text = string.Empty
                };
                button.SetEnabled(row.Enabled);
                button.AddToClassList(index == selectedIndex ? "title-menu__button--selected" : "title-menu__button");
                ApplyButtonStyles(button, index == selectedIndex);
                button.Add(CreateButtonLabel(row.Label, index == selectedIndex));
                menu.Add(button);
                index++;
            }

            root.Add(menu);
            menu.BringToFront();
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
            root.RemoveFromClassList("title-menu");
            root.AddToClassList("title-load-menu");
            var rowList = new List<TitleLoadSlotRow>(rows ?? new List<TitleLoadSlotRow>());
            var panel = new VisualElement { name = "TitleMenuToolkitLoadPanel" };
            panel.AddToClassList("title-load-menu__panel");
            ApplyLoadMenuLayout(panel, rowList.Count, root.ClassListContains("title-menu-toolkit-load-active"));
            foreach (var row in rowList)
            {
                var rowElement = new VisualElement();
                rowElement.AddToClassList("title-load-menu__row");
                ApplyLoadRowLayout(rowElement);

                var loadButton = new Button(() =>
                {
                    if (onLoad != null)
                    {
                        onLoad(row.SlotIndex);
                    }
                })
                {
                    text = string.Empty
                };
                loadButton.AddToClassList(row.LoadSelected ? "title-load-menu__load--selected" : "title-load-menu__load");
                ApplyButtonStyles(loadButton, row.LoadSelected);
                loadButton.Add(CreateButtonLabel(row.ButtonText, row.LoadSelected));

                var deleteButton = new Button(() =>
                {
                    if (onDelete != null)
                    {
                        onDelete(row.SlotIndex);
                    }
                })
                {
                    text = string.Empty
                };
                deleteButton.AddToClassList(row.DeleteSelected ? "title-load-menu__delete--selected" : "title-load-menu__delete");
                ApplyButtonStyles(deleteButton, row.DeleteSelected);
                deleteButton.Add(CreateButtonLabel(row.DeleteButtonText, row.DeleteSelected));

                rowElement.Add(loadButton);
                rowElement.Add(deleteButton);
                panel.Add(rowElement);
            }

            var backButton = new Button(() =>
            {
                if (onBack != null)
                {
                    onBack();
                }
            })
            {
                text = string.Empty
            };
            backButton.AddToClassList(backSelected ? "title-load-menu__back--selected" : "title-load-menu__back");
            ApplyButtonStyles(backButton, backSelected);
            backButton.Add(CreateButtonLabel("Back", backSelected));
            panel.Add(backButton);
            root.Add(panel);
            panel.BringToFront();
        }

        public static void BuildCreateMenu(
            VisualElement root,
            string playerName,
            Gender gender,
            Class playerClass,
            int spriteIndex,
            int[] stats,
            int selectedIndex,
            Action<string> onNameChanged,
            Action onGenerateName,
            Action onCycleGender,
            Action onCycleClass,
            Action onPreviousImage,
            Action onNextImage,
            Action onReroll,
            Action onStart,
            Action onBack)
        {
            if (root == null)
            {
                return;
            }

            root.Clear();
            root.RemoveFromClassList("title-menu");
            root.RemoveFromClassList("title-load-menu");
            root.AddToClassList("title-create-menu");

            var panel = new VisualElement { name = "TitleMenuToolkitCreatePanel" };
            panel.AddToClassList("title-create-menu__panel");
            ApplyCreateMenuLayout(panel);

            var title = CreateTextLabel("New Quest", "title-create-menu__title", Color.white, 24);
            panel.Add(title);

            var body = new VisualElement();
            body.AddToClassList("title-create-menu__body");
            ApplyCreateBodyLayout(body);

            var controls = new VisualElement();
            controls.AddToClassList("title-create-menu__controls");
            ApplyCreateControlsLayout(controls);
            controls.Add(CreateNameRow(playerName, selectedIndex == TitleViewModel.CreateNameIndex, onNameChanged, onGenerateName));
            controls.Add(CreateValueButtonRow("Gender:", gender.ToString(), selectedIndex == TitleViewModel.CreateGenderIndex, onCycleGender));
            controls.Add(CreateValueButtonRow("Class:", playerClass.ToString(), selectedIndex == TitleViewModel.CreateClassIndex, onCycleClass));
            controls.Add(CreateImageRow(spriteIndex, selectedIndex == TitleViewModel.CreateImageIndex, onPreviousImage, onNextImage));

            var statsPanel = CreateStatsPanel(stats, selectedIndex == TitleViewModel.CreateRerollIndex, onReroll);
            body.Add(controls);
            body.Add(statsPanel);
            panel.Add(body);

            var actions = new VisualElement();
            actions.AddToClassList("title-create-menu__actions");
            ApplyCreateActionsLayout(actions);
            actions.Add(CreateActionButton("Start", selectedIndex == TitleViewModel.CreateStartIndex, onStart));
            actions.Add(CreateActionButton("Back", selectedIndex == TitleViewModel.CreateBackIndex, onBack));
            panel.Add(actions);

            root.Add(panel);
            panel.BringToFront();
        }

        private static void ApplyButtonStyles(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            button.style.minHeight = 34;
            button.style.marginBottom = 8;
            button.style.backgroundColor = selected
                ? new Color(1f, 0.95f, 0.2f, 0.95f)
                : new Color(0.08f, 0.08f, 0.08f, 0.92f);
            button.style.flexDirection = FlexDirection.Row;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;
            button.style.paddingLeft = 8;
            button.style.paddingRight = 8;
            button.style.width = Length.Percent(100);
        }

        private static void ApplyMainMenuLayout(VisualElement menu, int rowCount)
        {
            if (menu == null)
            {
                return;
            }

            const float menuWidth = 280f;
            const float menuPadding = 12f;
            const float buttonHeight = 34f;
            const float buttonGap = 8f;
            var screenWidth = Mathf.Max(Screen.width, 640);
            var screenHeight = Mathf.Max(Screen.height, 480);
            var menuHeight = menuPadding * 2f + rowCount * buttonHeight + Mathf.Max(0, rowCount - 1) * buttonGap;

            menu.style.position = Position.Absolute;
            menu.style.left = Mathf.Round((screenWidth - menuWidth) / 2f);
            menu.style.top = Mathf.Round(screenHeight * 0.58f);
            menu.style.width = menuWidth;
            menu.style.height = menuHeight;
            menu.style.marginLeft = 0;
            menu.style.paddingTop = 12;
            menu.style.paddingRight = 12;
            menu.style.paddingBottom = 12;
            menu.style.paddingLeft = 12;
            menu.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
            menu.style.visibility = Visibility.Visible;
            menu.style.display = DisplayStyle.Flex;
        }

        private static void ApplyLoadMenuLayout(VisualElement panel, int rowCount, bool activeRenderer)
        {
            if (panel == null)
            {
                return;
            }

            const float panelWidth = 640f;
            const float panelPadding = 16f;
            const float rowHeight = 48f;
            const float rowGap = 8f;
            const float backHeight = 34f;
            var panelHeight = panelPadding * 2f + rowCount * rowHeight + Mathf.Max(0, rowCount - 1) * rowGap + rowGap + backHeight;

            panel.style.position = activeRenderer ? Position.Absolute : Position.Relative;
            panel.style.width = panelWidth;
            panel.style.height = panelHeight;
            panel.style.paddingTop = panelPadding;
            panel.style.paddingRight = panelPadding;
            panel.style.paddingBottom = panelPadding;
            panel.style.paddingLeft = panelPadding;
            panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
            panel.style.visibility = Visibility.Visible;
            panel.style.display = DisplayStyle.Flex;

            if (!activeRenderer)
            {
                return;
            }

            var screenWidth = Mathf.Max(Screen.width, 640);
            var screenHeight = Mathf.Max(Screen.height, 480);
            panel.style.left = Mathf.Round((screenWidth - panelWidth) / 2f);
            panel.style.top = Mathf.Round((screenHeight - panelHeight) / 2f);
        }

        private static void ApplyLoadRowLayout(VisualElement row)
        {
            if (row == null)
            {
                return;
            }

            row.style.flexDirection = FlexDirection.Row;
            row.style.height = 48;
            row.style.marginBottom = 8;
            row.style.width = Length.Percent(100);
        }

        private static VisualElement CreateNameRow(string playerName, bool selected, Action<string> onNameChanged, Action onGenerateName)
        {
            var row = CreateFormRow("Name:");
            var field = new TextField
            {
                name = "TitleMenuToolkitCreateName",
                value = playerName ?? string.Empty
            };
            field.AddToClassList(selected ? "title-create-menu__name--selected" : "title-create-menu__name");
            field.style.width = 136;
            field.style.height = 34;
            field.RegisterValueChangedCallback(evt =>
            {
                if (onNameChanged != null)
                {
                    onNameChanged(evt.newValue);
                }
            });
            row.Add(field);
            row.Add(CreateActionButton("Generate Name", false, onGenerateName, 152));
            return row;
        }

        private static VisualElement CreateValueButtonRow(string labelText, string value, bool selected, Action onClick)
        {
            var row = CreateFormRow(labelText);
            row.Add(CreateActionButton(value, selected, onClick, 136));
            return row;
        }

        private static VisualElement CreateImageRow(int spriteIndex, bool selected, Action onPreviousImage, Action onNextImage)
        {
            var row = CreateFormRow("Image:");
            var imagePanel = new VisualElement();
            imagePanel.AddToClassList(selected ? "title-create-menu__image--selected" : "title-create-menu__image");
            imagePanel.style.flexDirection = FlexDirection.Row;
            imagePanel.style.alignItems = Align.Center;
            imagePanel.style.justifyContent = Justify.Center;
            imagePanel.style.width = 170;
            imagePanel.style.height = 104;
            imagePanel.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            imagePanel.Add(CreateActionButton("<", selected, onPreviousImage, 34));
            imagePanel.Add(CreateTextLabel("Hero " + (spriteIndex + 1), "title-create-menu__image-label", Color.white, 14));
            imagePanel.Add(CreateActionButton(">", selected, onNextImage, 34));
            row.Add(imagePanel);
            return row;
        }

        private static VisualElement CreateStatsPanel(int[] stats, bool rerollSelected, Action onReroll)
        {
            var panel = new VisualElement();
            panel.AddToClassList("title-create-menu__stats");
            panel.style.width = 268;
            panel.style.height = 190;
            panel.style.paddingTop = 12;
            panel.style.paddingRight = 12;
            panel.style.paddingBottom = 12;
            panel.style.paddingLeft = 12;
            panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            panel.Add(CreateTextLabel("Stats", "title-create-menu__stats-title", Color.white, 16));
            var labels = new[] { "Health:", "Magic:", "Attack:", "Defence:", "Magic Defence:", "Agility:" };
            for (var i = 0; i < labels.Length; i++)
            {
                var value = stats != null && i < stats.Length ? stats[i] : 0;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = 20;
                row.Add(CreateTextLabel(labels[i], "title-create-menu__stat-label", Color.white, 13));
                row.Add(CreateTextLabel(value.ToString(), "title-create-menu__stat-value", Color.white, 13));
                panel.Add(row);
            }

            panel.Add(CreateActionButton("Re-Roll", rerollSelected, onReroll, 96));
            return panel;
        }

        private static VisualElement CreateFormRow(string labelText)
        {
            var row = new VisualElement();
            row.AddToClassList("title-create-menu__row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 38;
            row.style.marginBottom = 4;
            row.Add(CreateTextLabel(labelText, "title-create-menu__label", Color.white, 14));
            return row;
        }

        private static Button CreateActionButton(string text, bool selected, Action onClick, float width = 82f)
        {
            var button = new Button(() =>
            {
                if (onClick != null)
                {
                    onClick();
                }
            })
            {
                text = string.Empty
            };
            button.AddToClassList(selected ? "title-create-menu__button--selected" : "title-create-menu__button");
            ApplyButtonStyles(button, selected);
            button.style.width = width;
            button.Add(CreateButtonLabel(text, selected));
            return button;
        }

        private static Label CreateTextLabel(string text, string className, Color color, int fontSize)
        {
            var label = new Label(text ?? string.Empty);
            label.AddToClassList(className);
            ToolkitTextStyles.Apply(label, color, fontSize);
            return label;
        }

        private static void ApplyCreateMenuLayout(VisualElement panel)
        {
            const float panelWidth = 692f;
            const float panelHeight = 350f;
            var screenWidth = Mathf.Max(Screen.width, 640);
            var screenHeight = Mathf.Max(Screen.height, 480);
            panel.style.position = Position.Absolute;
            panel.style.left = Mathf.Round((screenWidth - panelWidth) / 2f);
            panel.style.top = Mathf.Round((screenHeight - panelHeight) / 2f);
            panel.style.width = panelWidth;
            panel.style.height = panelHeight;
            panel.style.paddingTop = 16;
            panel.style.paddingRight = 16;
            panel.style.paddingBottom = 16;
            panel.style.paddingLeft = 16;
            panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
            panel.style.display = DisplayStyle.Flex;
            panel.style.visibility = Visibility.Visible;
        }

        private static void ApplyCreateBodyLayout(VisualElement body)
        {
            body.style.flexDirection = FlexDirection.Row;
            body.style.height = 226;
        }

        private static void ApplyCreateControlsLayout(VisualElement controls)
        {
            controls.style.width = 370;
            controls.style.marginRight = 14;
        }

        private static void ApplyCreateActionsLayout(VisualElement actions)
        {
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.justifyContent = Justify.Center;
            actions.style.marginTop = 12;
        }

        private static Label CreateButtonLabel(string text, bool selected)
        {
            var label = new Label(text);
            label.AddToClassList(selected ? "title-toolkit-button-label--selected" : "title-toolkit-button-label");
            ToolkitTextStyles.Apply(label, selected ? Color.black : Color.white, 16);
            label.style.flexGrow = 1;
            return label;
        }
    }
}
