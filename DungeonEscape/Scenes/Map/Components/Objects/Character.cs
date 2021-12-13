namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class Character : Sprite
    {
        private readonly UiSystem _ui;
        private readonly Dialog _dialog;

        public Character(TmxObject tmxObject, SpriteState state, TmxMap map, UiSystem ui, IGame gameState, AstarGridGraph graph) : base(tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            var text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : null;
            if (!string.IsNullOrEmpty(text))
            {
                this._dialog = new Dialog
                {
                    Text =  text,
                    Choices = new List<Choice> { new Choice {Action =  QuestAction.None, Text = "OK"}}
                };
                
                return;
            }
            
            var dialogId = tmxObject.Properties.ContainsKey("Dialog") ? int.Parse(tmxObject.Properties["Dialog"]) : 0;
            this._dialog = gameState.Dialogs.FirstOrDefault(i => i.Id == dialogId);
        }

        public override bool OnAction(Party party)
        {
            if (this._dialog == null)
            {
                return false;
            }
            
            this.GameState.IsPaused = true;
            this.ShowDialog(this._dialog, () =>
            {
                this.GameState.IsPaused = false;
            });
            return true;

        }

        private void ShowDialog(Dialog dialog, Action done)
        {
            new TalkWindow(this._ui).Show(dialog,choice =>
            {
                if (choice == null)
                {
                    done();
                    return;
                }

                switch (choice.Action)
                {
                    case QuestAction.GetItem:
                        break;
                    case QuestAction.LookingForItem:
                        break;
                    case QuestAction.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (choice.QuestId.HasValue)
                {
                    var quest = this.GameState.Quests.FirstOrDefault(i => i.Id == choice.QuestId);
                    if (quest != null)
                    {
                        var activeQuest = this.GameState.Party.ActiveQuests.FirstOrDefault(i => i.Id == choice.QuestId);
                        if (activeQuest == null)
                        {
                            activeQuest = new ActiveQuest {Id = quest.Id, CurrentStage = 0, Stages = quest.Stages.Select(i => new QuestStageState {Number = i.Number}).ToList()};
                            this.GameState.Party.ActiveQuests.Add(activeQuest);
                        }

                        var stageNumber = choice.QuestStage ?? 0;
                        if (stageNumber > activeQuest.CurrentStage)
                        {
                            var activeStage = activeQuest.Stages.FirstOrDefault(i => i.Number == activeQuest.CurrentStage);
                            if (activeStage != null)
                            {
                                activeStage.Completed = true;
                            }
                            
                            activeQuest.CurrentStage = stageNumber;
                        }
                    }
                }

                if (choice.Dialog != null)
                {
                    this.ShowDialog(choice.Dialog, done);
                }
                else
                {
                    done();
                }
                
            });
        }
    }
}