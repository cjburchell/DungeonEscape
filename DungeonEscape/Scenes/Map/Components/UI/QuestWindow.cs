



using System.Linq;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using System;
    using Microsoft.Xna.Framework;
    using System.Collections.Generic;
    using Nez.UI;
    using State;
    
    public class QuestWindow : BasicWindow
    {
        private Action _done;
        private IEnumerable<ActiveQuest> _activeQuests;
        private IEnumerable<Quest> _quests;

        public QuestWindow(UiSystem ui) : base(ui, null,
            new Point(20, 20), 1000, 300)
        {
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            var table = this.Window.AddElement(new Table());
            
            var closeButton = new TextButton("Close", Skin)
            {
                ShouldUseExplicitFocusableControl = true
            };
            closeButton.OnClicked += _ =>
            {
                this.Ui.Sounds.PlaySoundEffect("confirm");
                this.CloseWindow();
            };
            
            var questTable = new Table();
            questTable.Row().SetPadTop(5);
            foreach (var activeQuest in this._activeQuests)
            {
                var quest = this._quests.FirstOrDefault(i => i.Id == activeQuest.Id);
                if (quest == null)
                {
                    continue;
                }

                var competed = activeQuest.Completed ? "Finished" : "";
                questTable.Add(new Label($"{quest.Name}", Skin).SetAlignment(Align.Left)).Width(125);
                questTable.Add(new Label($"({competed}", Skin).SetAlignment(Align.Right)).Width(125);
                questTable.Row();
                questTable.Add(new Label($"{quest.Description}", Skin).SetAlignment(Align.Left)).Width(125);
                questTable.Row();
            }
            
            var scrollPane = new ScrollPane(questTable, Skin);
            
            table.SetFillParent(true);
            table.Row();
            table.Add(scrollPane).Width(this.Width-20).Height(this.Height - 50);
            table.Row();
            table.Add(closeButton).Height(ButtonHeight).Width(ButtonWidth).SetColspan(4).Center().Bottom().SetPadBottom(2);
        }

        public override void CloseWindow(bool remove = true)
        {
            base.CloseWindow(remove);
            this._done?.Invoke();
        }

        public override void DoAction()
        {
            this.CloseWindow();
        }
        
        public void Show(IEnumerable<ActiveQuest> activeQuests, IEnumerable<Quest> quests, Action doneAction)
        {
            this._done = doneAction;
            this._activeQuests = activeQuests;
            this._quests = quests;
            this.ShowWindow();
        }
    }
}