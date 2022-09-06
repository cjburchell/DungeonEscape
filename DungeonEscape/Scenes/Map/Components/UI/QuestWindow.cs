



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
            new Point(20, 20), 1000, 500)
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
            var scrollPane = new ScrollPane(questTable, Skin);
            const int margin = 10;
            const int itemHeight = 30*3;
            var itemWidth = this.Width - margin* 6 ;
            var cellWidth = itemWidth - margin * 2;
            foreach (var activeQuest in this._activeQuests)
            {
                var quest = this._quests.FirstOrDefault(i => i.Id == activeQuest.Id);
                if (quest == null)
                {
                    continue;
                }

                var questItem = new Table().SetBackground(new BorderPrimitiveDrawable(Color.Black, Color.White, 1));
                var competed = activeQuest.Completed ? "(Finished)" : "";
                questItem.Add(new Label($"{quest.Name}{competed}", Skin).SetAlignment(Align.Left)).Width(cellWidth)
                    .SetPadBottom(5);
                questItem.Row();
                questItem.Add(new Label($"{quest.Description}", Skin).SetAlignment(Align.Left)).Width(cellWidth);
                questItem.Row();
                var currentStage = quest.Stages.FirstOrDefault(i => i.Number == activeQuest.CurrentStage);
                if (currentStage != null)
                {
                    questItem.Add(new Label($"{currentStage.Description}", Skin).SetAlignment(Align.Left))
                        .Width(cellWidth);
                }

                questTable.Add(questItem).Width(itemWidth).Height(itemHeight).SetPadTop(margin);
                questTable.Row();
            }

            var height = Math.Min((_activeQuests.Count() * (itemHeight + margin)) + ButtonHeight + 2 + margin * 2, 500);
            this.Window.SetHeight(height);
            
            table.SetFillParent(true);
            table.Row();
            table.Add(scrollPane).Width(this.Width - margin*4 ).Height(height-ButtonHeight-margin*2).SetPadBottom(margin);
            table.Row();
            table.Add(closeButton).Height(ButtonHeight).Width(ButtonWidth).SetColspan(4).Center().Bottom().SetPadBottom(2);
            scrollPane.Validate();
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