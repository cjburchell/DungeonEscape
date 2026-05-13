using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class GameMenu
    {
        private QuestMenuScreen questMenuScreen;
        private SaveMenuScreen saveMenuScreen;
        private LoadMenuScreen loadMenuScreen;
        private SettingsMenuScreen settingsMenuScreen;
        private MainActionMenuScreen mainActionMenuScreen;
        private MiscActionMenuScreen miscActionMenuScreen;
        private MemberDetailMenuScreen memberDetailMenuScreen;

        private MainActionMenuScreen MainActionScreen
        {
            get { return mainActionMenuScreen ?? (mainActionMenuScreen = new MainActionMenuScreen(this)); }
        }

        private MiscActionMenuScreen MiscActionScreen
        {
            get { return miscActionMenuScreen ?? (miscActionMenuScreen = new MiscActionMenuScreen(this)); }
        }

        private MemberDetailMenuScreen MemberDetailScreen
        {
            get { return memberDetailMenuScreen ?? (memberDetailMenuScreen = new MemberDetailMenuScreen(this)); }
        }

        private QuestMenuScreen QuestScreen
        {
            get { return questMenuScreen ?? (questMenuScreen = new QuestMenuScreen(this)); }
        }

        private SaveMenuScreen SaveScreen
        {
            get { return saveMenuScreen ?? (saveMenuScreen = new SaveMenuScreen(this)); }
        }

        private LoadMenuScreen LoadScreen
        {
            get { return loadMenuScreen ?? (loadMenuScreen = new LoadMenuScreen(this)); }
        }

        private SettingsMenuScreen SettingsScreen
        {
            get { return settingsMenuScreen ?? (settingsMenuScreen = new SettingsMenuScreen(this)); }
        }

        private abstract class MenuScreenController
        {
            protected readonly GameMenu Menu;

            protected MenuScreenController(GameMenu menu)
            {
                Menu = menu;
            }

            public virtual int GetSelectableRowCount()
            {
                return 0;
            }

            public virtual void Draw()
            {
            }

            public virtual void ActivateSelectedRow()
            {
            }

            public virtual void AdjustSelection(int delta)
            {
            }
        }

    }
}
