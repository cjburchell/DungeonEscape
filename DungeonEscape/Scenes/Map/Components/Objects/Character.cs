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
        protected  Dialog Dialog;

        public Character(TmxObject tmxObject, SpriteState state, TmxMap map, UiSystem ui, IGame gameState, AstarGridGraph graph) : base(tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            var text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : null;
            if (!string.IsNullOrEmpty(text))
            {
                this.Dialog = new Dialog
                {
                    Dialogs = new List<DialogText> { new DialogText {Text = text, Choices = new List<Choice> { new Choice {Text = "Ok"}}}}
                };
                
                return;
            }
            
            var dialogId = tmxObject.Properties.ContainsKey("Dialog") ? int.Parse(tmxObject.Properties["Dialog"]) : 0;
            this.Dialog = gameState.Dialogs.FirstOrDefault(i => i.Id == dialogId);
        }

        public override bool OnAction(Party party)
        {
            if (this.Dialog == null)
            {
                return false;
            }
            
            this.GameState.IsPaused = true;
            var quest = this.Dialog.Quest.HasValue ? this.GameState.Quests.FirstOrDefault(i => i.Id == this.Dialog.Quest) : null;
            this.ShowDialog(this.Dialog.Dialogs, quest,() =>
            {
                this.GameState.IsPaused = false;
            });
            return true;

        }

        private void ShowDialog(IReadOnlyCollection<DialogText> dialogs, Quest quest, Action done)
        {
            // Choose the dialog based on the correct quest state.
            DialogText dialog;
            ActiveQuest activeQuest = null;
            if (quest == null)
            {
                dialog = dialogs.First();
            }
            else
            {
                activeQuest =
                    this.GameState.Party.ActiveQuests.FirstOrDefault(i => i.Id == quest.Id);
                if (activeQuest == null)
                {
                    activeQuest = new ActiveQuest
                    {
                        Id = quest.Id, CurrentStage = 0,
                        Stages = quest.Stages.Select(i => new QuestStageState {Number = i.Number}).ToList()
                    };
                    this.GameState.Party.ActiveQuests.Add(activeQuest);
                }

                dialog = dialogs.FirstOrDefault(d =>
                    d.ForQuestStage == activeQuest.CurrentStage
                ) ?? dialogs.First(d => !d.ForQuestStage.HasValue);
            }

            if (dialog == null)
            {
                done();
                return;
            }
            
            new TalkWindow(this._ui).Show(dialog.Text, dialog.Choices.Where(choice =>
            {
                if (choice.Action == QuestAction.LookingForItem && choice.ItemId.HasValue)
                {
                    // if the action is looking for an item
                    return this.GameState.Party.Items.FirstOrDefault(i => i.ItemId == choice.ItemId) != null;
                }
                return true;

            }) ,choice =>
            {
                if (choice == null)
                {
                    done();
                    return;
                }
                
                // advance a quest
                if (quest != null && activeQuest != null)
                {
                    var stageNumber = choice.NextQuestStage ?? 0;
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
                
                switch (choice.Action)
                {
                    case QuestAction.GiveItem:
                        this.GameState.Party.Items.Add(new ItemInstance(this.GameState.Items.FirstOrDefault(i=> i.Id == choice.ItemId)));
                        break;
                    case QuestAction.LookingForItem:
                        var item = this.GameState.Party.Items.FirstOrDefault(i => i.ItemId == choice.ItemId);
                        if (item != null)
                        {
                            this.GameState.Party.Items.Remove(item);
                        }
                        break;
                    case QuestAction.Fight:
                        var monster = this.GameState.Monsters.FirstOrDefault(m => m.Id == choice.MonsterId);
                        if (monster != null)
                        {
                            this.GameState.StartFight(new[]{monster});
                            return;
                        }
                        break;
                    case QuestAction.Warp:
                        if (choice.MapId.HasValue)
                        {
                            this.GameState.SetMap(choice.MapId,  choice.SpawnId);
                            return;
                        }
                        break;
                    case QuestAction.Join:
                        this.JoinParty();
                        break;
                    case QuestAction.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (choice.Dialogs != null)
                {
                    this.ShowDialog(choice.Dialogs, quest, done);
                }
                else
                {
                    done();
                }
                
            });
        }

        protected virtual void JoinParty()
        {
        }
    }
}