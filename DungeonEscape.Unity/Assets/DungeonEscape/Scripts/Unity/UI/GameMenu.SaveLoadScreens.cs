using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
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

                var saves = Menu.gameState.GetManualSaveSlots();
                Menu.viewModel.ClampSelectedRowIndex(saves.Count + 1);

                GUILayout.BeginVertical();
                Menu.saveScrollPosition = Menu.BeginThemedScroll(
                    Menu.saveScrollPosition,
                    Mathf.Max(120f * Menu.GetPixelScale(), Menu.menuBodyHeight));
                for (var i = 0; i < saves.Count; i++)
                {
                    Menu.BeginSelectableRow();
                    DrawSaveRow(saves[i]);
                    EndSelectableRow();
                    Menu.SelectSaveRowOnMouseClick(i);
                }

                Menu.BeginSelectableRow();
                DrawNewSaveRow();
                EndSelectableRow();
                Menu.SelectSaveRowOnMouseClick(saves.Count);

                EndThemedScroll();
                GUILayout.EndVertical();
            }

            public override void ActivateSelectedRow()
            {
                Menu.ActivateSelectedSaveSlot();
            }

            private void DrawSaveRow(GameSave save)
            {
                GUILayout.BeginVertical();
                GUILayout.Label(GameSaveFormatter.GetTitle(save), Menu.labelStyle);
                GUILayout.Label(GameSaveFormatter.GetSummary(save), Menu.smallStyle);
                GUILayout.EndVertical();
            }

            private void DrawNewSaveRow()
            {
                GUILayout.BeginVertical();
                GUILayout.Label("New Save", Menu.labelStyle);
                GUILayout.Label("Save the current quest.", Menu.smallStyle);
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

                var saves = Menu.gameState.GetManualSaveSlots();
                Menu.viewModel.ClampSelectedRowIndex(saves.Count);
                if (saves.Count == 0)
                {
                    GUILayout.Label("No saves.", Menu.labelStyle);
                    return;
                }

                Menu.saveScrollPosition = Menu.BeginThemedScroll(
                    Menu.saveScrollPosition,
                    Mathf.Max(120f * Menu.GetPixelScale(), Menu.menuBodyHeight));
                for (var i = 0; i < saves.Count; i++)
                {
                    Menu.BeginSelectableRow();
                    DrawSaveRow(saves[i]);
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
                if (Menu.selectedRowIndex >= 0 && Menu.selectedRowIndex < saves.Count)
                {
                    Menu.ConfirmLoadManual(Menu.selectedRowIndex);
                }
            }

            private void DrawSaveRow(GameSave save)
            {
                GUILayout.BeginVertical();
                GUILayout.Label(GameSaveFormatter.GetTitle(save), Menu.labelStyle);
                GUILayout.Label(GameSaveFormatter.GetSummary(save), Menu.smallStyle);
                GUILayout.EndVertical();
            }
        }
    }
}
