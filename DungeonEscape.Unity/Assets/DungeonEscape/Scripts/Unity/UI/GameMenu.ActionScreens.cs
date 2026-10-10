using System.Collections.Generic;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private sealed class MainActionMenuScreen : MenuScreenController
        {
            public MainActionMenuScreen(GameMenu menu)
                : base(menu)
            {
            }

            public override int GetSelectableRowCount()
            {
                return GetActions().Count;
            }

            public override void Draw()
            {
                var actions = GetActions();
                Menu.viewModel.ClampSelectedMainActionIndex(actions.Count);
                Menu.viewModel.ClampSelectedRowIndex(actions.Count);
                Menu.DrawActionList(actions, Menu.selectedRowIndex, true);
            }

            public override void ActivateSelectedRow()
            {
                var actions = GetActions();
                if (Menu.selectedRowIndex < 0 || Menu.selectedRowIndex >= actions.Count)
                {
                    return;
                }

                Menu.selectedMainActionIndex = Menu.selectedRowIndex;
                switch (actions[Menu.selectedRowIndex])
                {
                    case "Items":
                        Menu.OpenMenuScreen(MenuScreen.Items);
                        break;
                    case "Spells":
                        Menu.OpenMenuScreen(MenuScreen.Spells);
                        break;
                    case "Equipment":
                        Menu.OpenMenuScreen(MenuScreen.Equipment);
                        break;
                    case "Abilities":
                        Menu.OpenMenuScreen(MenuScreen.Abilities);
                        break;
                    case "Status":
                        Menu.OpenMenuScreen(MenuScreen.Status);
                        break;
                    case "Quests":
                        Menu.OpenMenuScreen(MenuScreen.Quests);
                        break;
                    case "Party":
                        Menu.OpenMenuScreen(MenuScreen.Party);
                        break;
                    case "Level Up":
                        Menu.ShowLevelUpPicker();
                        break;
                    case "Short Rest":
                        Menu.ShortRest();
                        break;
                    case "Make Camp":
                        Menu.MakeCamp();
                        break;
                    case "Misc.":
                        Menu.OpenMenuScreen(MenuScreen.Misc);
                        break;
                }
            }

            public List<string> GetActions()
            {
                return Menu.viewModel.GetMainActions(
                    Menu.AnyMemberHasUsableMapSpells(),
                    Menu.AnyMemberHasUsableMapAbilities(),
                    Menu.CanManagePartyMembers(),
                    Menu.CanMakeCamp(),
                    Menu.AnyMemberCanLevelUp());
            }
        }

        private sealed class MiscActionMenuScreen : MenuScreenController
        {
            public MiscActionMenuScreen(GameMenu menu)
                : base(menu)
            {
            }

            public override int GetSelectableRowCount()
            {
                return GetActions().Count;
            }

            public override void Draw()
            {
                Menu.DrawActionList(GetActions(), Menu.selectedRowIndex, true);
            }

            public override void ActivateSelectedRow()
            {
                var actions = GetActions();
                if (Menu.selectedRowIndex < 0 || Menu.selectedRowIndex >= actions.Count)
                {
                    return;
                }

                switch (actions[Menu.selectedRowIndex])
                {
                    case "Save":
                        Menu.OpenMenuScreen(MenuScreen.Save);
                        return;
                    case "Load":
                        Menu.OpenMenuScreen(MenuScreen.Load);
                        return;
                    case "Settings":
                        Menu.OpenMenuScreen(MenuScreen.Settings);
                        return;
                    case "Exit to Main":
                        Menu.ConfirmReturnToMainMenu();
                        return;
                    case "Quit":
                        Menu.ConfirmQuitGame();
                        return;
                }
            }

            public IList<string> GetActions()
            {
                var actions = new List<string>();
                actions.Add("Save");
                actions.Add("Load");
                actions.Add("Settings");
                actions.Add("Exit to Main");
                actions.Add("Quit");
                return actions;
            }
        }
    }
}
