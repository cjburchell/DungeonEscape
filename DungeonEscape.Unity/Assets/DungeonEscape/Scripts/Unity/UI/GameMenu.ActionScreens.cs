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
                Menu.selectedMainActionIndex = UnityEngine.Mathf.Clamp(
                    Menu.selectedMainActionIndex,
                    0,
                    System.Math.Max(actions.Count - 1, 0));
                Menu.selectedRowIndex = UnityEngine.Mathf.Clamp(
                    Menu.selectedRowIndex,
                    0,
                    System.Math.Max(actions.Count - 1, 0));
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
                var actions = new List<string>
                {
                    "Items"
                };

                if (Menu.AnyMemberHasUsableMapSpells())
                {
                    actions.Add("Spells");
                }

                actions.Add("Equipment");

                if (Menu.AnyMemberHasUsableMapAbilities())
                {
                    actions.Add("Abilities");
                }

                actions.Add("Status");
                actions.Add("Quests");
                if (Menu.CanManagePartyMembers())
                {
                    actions.Add("Party");
                }

                actions.Add("Misc.");
                return actions;
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
