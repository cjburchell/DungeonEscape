using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.ViewModels;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private sealed class SaveMenuScreen : MenuScreenController
        {
            public SaveMenuScreen(GameMenu menu)
                : base(menu)
            {
            }

            public override int GetSelectableRowCount()
            {
                return Menu.gameState == null ? 0 : Menu.viewModel.GetSaveSelectableRowCount(Menu.gameState.ManualSaveSlotCount);
            }

            public override void Draw()
            {
                Menu.EnsureReferences();
                if (Menu.gameState == null)
                {
                    GUILayout.Label("Game state is not loaded.", Menu.labelStyle);
                    return;
                }

                var rows = Menu.viewModel.GetSaveSlotRows(Menu.gameState.GetManualSaveSlots(), true);
                Menu.viewModel.ClampSelectedRowIndex(rows.Count);

                GUILayout.BeginVertical();
                Menu.saveScrollPosition = Menu.BeginThemedScroll(
                    Menu.saveScrollPosition,
                    Mathf.Max(120f * Menu.GetPixelScale(), Menu.menuBodyHeight));
                for (var i = 0; i < rows.Count; i++)
                {
                    Menu.BeginSelectableRow();
                    DrawSaveRow(rows[i]);
                    EndSelectableRow();
                    Menu.SelectSaveRowOnMouseClick(i);
                }

                EndThemedScroll();
                GUILayout.EndVertical();
            }

            public override void ActivateSelectedRow()
            {
                Menu.ActivateSelectedSaveSlot();
            }

            private void DrawSaveRow(GameMenuSaveSlotRow row)
            {
                GUILayout.BeginVertical();
                GUILayout.Label(row.Title, Menu.labelStyle);
                GUILayout.Label(row.Summary, Menu.smallStyle);
                GUILayout.EndVertical();
            }
        }

        private sealed class LoadMenuScreen : MenuScreenController
        {
            public LoadMenuScreen(GameMenu menu)
                : base(menu)
            {
            }

            public override int GetSelectableRowCount()
            {
                return Menu.gameState == null ? 0 : Menu.viewModel.GetLoadSelectableRowCount(Menu.gameState.GetManualSaveSlots().Count);
            }

            public override void Draw()
            {
                Menu.EnsureReferences();
                if (Menu.gameState == null)
                {
                    GUILayout.Label("Game state is not loaded.", Menu.labelStyle);
                    return;
                }

                var rows = Menu.viewModel.GetSaveSlotRows(Menu.gameState.GetManualSaveSlots(), false);
                Menu.viewModel.ClampSelectedRowIndex(rows.Count);
                if (rows.Count == 0)
                {
                    GUILayout.Label("No saves.", Menu.labelStyle);
                    return;
                }

                Menu.saveScrollPosition = Menu.BeginThemedScroll(
                    Menu.saveScrollPosition,
                    Mathf.Max(120f * Menu.GetPixelScale(), Menu.menuBodyHeight));
                for (var i = 0; i < rows.Count; i++)
                {
                    Menu.BeginSelectableRow();
                    DrawSaveRow(rows[i]);
                    EndSelectableRow();
                    Menu.SelectRowOnMouseClick(i);
                }

                EndThemedScroll();
            }

            public override void ActivateSelectedRow()
            {
                var saves = Menu.gameState == null
                    ? new List<GameSave>()
                    : Menu.gameState.GetManualSaveSlots().ToList();
                var rows = Menu.viewModel.GetSaveSlotRows(saves, false);
                if (Menu.selectedRowIndex >= 0 && Menu.selectedRowIndex < rows.Count)
                {
                    Menu.ConfirmLoadManual(rows[Menu.selectedRowIndex].SlotIndex);
                }
            }

            private void DrawSaveRow(GameMenuSaveSlotRow row)
            {
                GUILayout.BeginVertical();
                GUILayout.Label(row.Title, Menu.labelStyle);
                GUILayout.Label(row.Summary, Menu.smallStyle);
                GUILayout.EndVertical();
            }
        }
    }
}
