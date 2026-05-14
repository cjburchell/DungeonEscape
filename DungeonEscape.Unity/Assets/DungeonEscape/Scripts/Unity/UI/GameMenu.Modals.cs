using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private void ShowMenuModal(string title, string message, IEnumerable<string> choices, Action<int> selected)
        {
            ShowMenuModal(title, message, choices, null, selected);
        }

        private void ShowMenuModal(string title, string message, IEnumerable<string> choices, IEnumerable<Hero> choiceHeroes, Action<int> selected)
        {
            viewModel.ShowModal(
                title,
                message,
                choices,
                choiceHeroes,
                InputManager.GetCommand(InputCommand.Interact) || UnityEngine.Input.GetMouseButton(0));
            menuModalSelected = selected;
            ResetMenuNavigationRepeat();
        }

        private bool IsMenuModalVisible()
        {
            return viewModel.IsModalVisible();
        }

        private bool MenuModalHasChoices()
        {
            return viewModel.ModalHasChoices();
        }

        private void HideMenuModal()
        {
            viewModel.HideModal();
            menuModalSelected = null;
            ResetMenuNavigationRepeat();
        }

        private void UpdateMenuModal()
        {
            if (InputManager.GetCommandDown(InputCommand.Cancel))
            {
                HideMenuModal();
                return;
            }

            if (viewModel.ModalWaitingForConfirmRelease)
            {
                if (InputManager.GetCommand(InputCommand.Interact) || UnityEngine.Input.GetMouseButton(0))
                {
                    return;
                }

                viewModel.ReleaseModalConfirmWait();
            }

            if (!MenuModalHasChoices())
            {
                if (InputManager.GetCommandDown(InputCommand.Interact))
                {
                    HideMenuModal();
                }

                return;
            }

            var moveY = GetMenuMoveY();
            if (moveY < 0)
            {
                viewModel.MoveModalSelection(-1);
            }
            else if (moveY > 0)
            {
                viewModel.MoveModalSelection(1);
            }

            if (viewModel.ModalChoices.Count <= 2)
            {
                var moveX = InputManager.GetMoveXDown();
                if (moveX < 0)
                {
                    viewModel.MoveModalSelection(-1);
                }
                else if (moveX > 0)
                {
                    viewModel.MoveModalSelection(1);
                }
            }

            if (InputManager.GetCommandDown(InputCommand.Interact))
            {
                SelectMenuModalChoice(viewModel.ModalSelectedIndex);
            }
        }

        private void SelectMenuModalChoice(int index)
        {
            int selectedIndex;
            if (!viewModel.TrySelectModalChoice(index, out selectedIndex))
            {
                return;
            }

            var selected = menuModalSelected;
            menuModalSelected = null;
            ResetMenuNavigationRepeat();
            if (selected != null)
            {
                selected(selectedIndex);
            }
        }

        private void DrawMenuModalOverlay()
        {
            if (!IsMenuModalVisible() || rebindingInput != null)
            {
                return;
            }

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var hasChoices = MenuModalHasChoices();
            var choiceCount = hasChoices ? viewModel.ModalChoices.Count : 1;
            var compactDialog = !hasChoices || choiceCount <= 2;
            var height = compactDialog
                ? 170f * scale
                : Mathf.Min(Screen.height - 48f * scale, (150f + choiceCount * 42f) * scale);
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.enabled = true;
            DrawModalBackdrop();
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            if (compactDialog)
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.Label(viewModel.ModalTitle, titleStyle);
            GUILayout.Label(viewModel.ModalMessage, labelStyle);
            GUILayout.Space(10f * scale);

            if (hasChoices)
            {
                if (viewModel.ModalChoiceHeroes != null)
                {
                    for (var i = 0; i < viewModel.ModalChoices.Count; i++)
                    {
                        DrawMenuModalChoiceRow(i, 38f * scale);
                    }
                }
                else if (viewModel.ModalChoices.Count <= 2)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    for (var i = 0; i < viewModel.ModalChoices.Count; i++)
                    {
                        if (UiControls.Button(viewModel.ModalChoices[i], i == viewModel.ModalSelectedIndex, uiTheme, GUILayout.Width(120f * scale), GUILayout.Height(34f * scale)) &&
                            !viewModel.ModalWaitingForConfirmRelease)
                        {
                            SelectMenuModalChoice(i);
                            break;
                        }

                        if (i < viewModel.ModalChoices.Count - 1)
                        {
                            GUILayout.Space(10f * scale);
                        }
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    for (var i = 0; i < viewModel.ModalChoices.Count; i++)
                    {
                        if (UiControls.Button(viewModel.ModalChoices[i], i == viewModel.ModalSelectedIndex, uiTheme) &&
                            !viewModel.ModalWaitingForConfirmRelease)
                        {
                            SelectMenuModalChoice(i);
                            break;
                        }
                    }
                }
            }
            else if (UiControls.Button("OK", buttonStyle, GUILayout.Width(120f * scale)) &&
                     !viewModel.ModalWaitingForConfirmRelease)
            {
                HideMenuModal();
            }

            if (compactDialog)
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndArea();
        }

        private void DrawMenuModalChoiceRow(int index, float height)
        {
            var selected = index == viewModel.ModalSelectedIndex;
            GUILayout.BeginHorizontal(GetListRowStyle(selected), GUILayout.Height(height));
            var hero = viewModel.GetModalChoiceHero(index);
            if (hero != null)
            {
                Sprite sprite;
                DrawSpriteIconNoFrame(UiAssetResolver.TryGetHeroSprite(hero, out sprite) ? sprite : null, 32f * GetPixelScale());
            }
            else
            {
                GUILayout.Space(32f * GetPixelScale());
            }

            GUILayout.Label(viewModel.ModalChoices[index], GetMenuListLabelStyle(selected), GUILayout.Height(height));
            GUILayout.EndHorizontal();
            SelectMenuModalChoiceOnMouseClick(index);
        }

        private void SelectMenuModalChoiceOnMouseClick(int index)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            if (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
            {
                return;
            }

            viewModel.MoveModalSelection(index - viewModel.ModalSelectedIndex);
            if (!viewModel.ModalWaitingForConfirmRelease)
            {
                SelectMenuModalChoice(index);
            }

            currentEvent.Use();
        }

        private void DrawRebindingOverlay()
        {
            if (rebindingInput == null)
            {
                return;
            }

            var scale = GetPixelScale();
            var width = Mathf.Min(Screen.width - 32f * scale, 620f * scale);
            var height = 150f * scale;
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.enabled = true;
            DrawModalBackdrop();
            GUI.Box(rect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 18f * scale, rect.y + 16f * scale, rect.width - 36f * scale, rect.height - 32f * scale));
            GUILayout.Label("Input Bindings", titleStyle);
            GUILayout.Label(GetRebindingPrompt(), labelStyle);
            GUILayout.FlexibleSpace();
            if (UiControls.Button("Cancel", buttonStyle, GUILayout.Width(120f * scale)))
            {
                rebindingInput = null;
                rebindingSlot = null;
            }

            GUILayout.EndArea();
        }

        private static void DrawModalBackdrop()
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
