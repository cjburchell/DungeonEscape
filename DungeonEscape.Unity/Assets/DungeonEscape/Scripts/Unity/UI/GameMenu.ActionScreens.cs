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
                    Menu.CanManagePartyMembers());
            }
        }

        private sealed class MiscActionMenuScreen : MenuScreenController
        {
            private static readonly List<string> Actions = new List<string>
            {
                "Save",
                "Load",
                "Settings",
                "Exit to Main",
                "Quit"
            };

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
                switch (Menu.selectedRowIndex)
                {
                    case 0:
                        Menu.OpenMenuScreen(MenuScreen.Save);
                        break;
                    case 1:
                        Menu.OpenMenuScreen(MenuScreen.Load);
                        break;
                    case 2:
                        Menu.OpenMenuScreen(MenuScreen.Settings);
                        break;
                    case 3:
                        Menu.ConfirmReturnToMainMenu();
                        break;
                    case 4:
                        Menu.ConfirmQuitGame();
                        break;
                }
            }

            public IList<string> GetActions()
            {
                return Actions;
            }
        }
    }
}
