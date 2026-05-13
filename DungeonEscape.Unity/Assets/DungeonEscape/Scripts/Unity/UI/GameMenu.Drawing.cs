using System;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private Vector2 BeginThemedScroll(Vector2 position, float height)
        {
            return GUILayout.BeginScrollView(
                position,
                false,
                false,
                GUIStyle.none,
                uiTheme == null ? GUI.skin.verticalScrollbar : uiTheme.VerticalScrollbarStyle,
                GUILayout.Height(height));
        }

        private static void EndThemedScroll()
        {
            GUILayout.EndScrollView();
        }

        private void BeginSelectableRow()
        {
            var rowIndex = drawingRowIndex++;
            UiControls.BeginSelectableRow(rowIndex, selectedRowIndex, uiTheme);
        }

        private static void EndSelectableRow()
        {
            UiControls.EndSelectableRow();
        }

        private void SelectRowOnMouseClick(int rowIndex)
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

            selectedRowIndex = rowIndex;
        }

        private void SelectMenuRowOnMouseClick(int rowIndex, Action doubleClickAction)
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

            selectedRowIndex = rowIndex;
            currentFocus = MenuFocus.Primary;
            if (currentEvent.clickCount >= 2 && doubleClickAction != null)
            {
                UiControls.PlayConfirmSound();
                doubleClickAction();
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private void HandleDetailRowMouseClick(int rowIndex, Action doubleClickAction)
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

            selectedDetailIndex = rowIndex;
            detailPageIndex = rowIndex / 10;
            currentFocus = MenuFocus.Detail;
            if (currentEvent.clickCount >= 2 && doubleClickAction != null)
            {
                UiControls.PlayConfirmSound();
                doubleClickAction();
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private void HandleEquipmentCandidateMouseClick(int rowIndex, Action doubleClickAction)
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

            selectedEquipmentItemIndex = rowIndex;
            currentFocus = MenuFocus.SubDetail;
            if (currentEvent.clickCount >= 2 && doubleClickAction != null)
            {
                UiControls.PlayConfirmSound();
                doubleClickAction();
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private void SelectSaveRowOnMouseClick(int rowIndex)
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

            selectedRowIndex = rowIndex;
            if (currentEvent.clickCount >= 2)
            {
                UiControls.PlayConfirmSound();
                ShowSaveActionModal(rowIndex);
            }
            else
            {
                UiControls.PlaySelectSound();
            }

            currentEvent.Use();
        }

        private bool DrawCheckboxRow(bool value, string label)
        {
            return UiControls.CheckboxRow(value, label, uiTheme, GetPixelScale());
        }

        private float DrawSliderRow(string label, float value, float leftValue, float rightValue)
        {
            return UiControls.SliderRow(label, value, leftValue, rightValue, uiTheme);
        }

        private string DrawTextFieldRow(string label, string value, string fallback)
        {
            return UiControls.TextFieldRow(label, value, fallback, uiTheme);
        }
    }
}
