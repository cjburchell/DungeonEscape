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
                    Dialogs = new List<DialogText> { new DialogText {Text = text, Choices = new List<Choice> { new Choice {Text = "OK"}}}}
                };
                
                return;
            }

            var dialogId = tmxObject.Properties.ContainsKey("Dialog") ? tmxObject.Properties["Dialog"] : null;
            this.Dialog = gameState.Dialogs.FirstOrDefault(i => i.Id == dialogId);
        }

        public override bool OnAction(Party party)
        {
            if (this.Dialog == null)
            {
                return false;
            }
            
            this.GameState.IsPaused = true;
            var quest = !string.IsNullOrEmpty(this.Dialog.Quest) ? this.GameState.Quests.FirstOrDefault(i => i.Id == this.Dialog.Quest) : null;
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
            ActiveQuest activeQuest;
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

                dialog = dialogs.FirstOrDefault(d => d.ForQuestStage != null &&
                    d.ForQuestStage.Contains(activeQuest.CurrentStage) 
                ) ?? dialogs.FirstOrDefault();
            }

            if (dialog == null)
            {
                done();
                return;
            }
            
            new TalkWindow(this._ui).Show(dialog.Text, dialog.Choices?.Where(choice =>
            {
                if (choice.Action == QuestAction.TakeItem && choice.ItemId != null)
                {
                    // if the action is looking for an item
                    return this.GameState.Party.GetItem(choice.ItemId) != null;
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
                var completedQuestMessage = "";
                if (quest != null)
                {
                    completedQuestMessage = this.GameState.AdvanceQuest(quest.Id, 0, choice.NextQuestStage);
                }

                void DoneAction()
                {
                    if (choice.Dialogs != null)
                    {
                        this.ShowDialog(choice.Dialogs, quest, done);
                    }
                    else
                    {
                        done();
                    }
                }

                switch (choice.Action)
                {
                    case QuestAction.GiveItem:
                        var questMessage = "";
                        var itemMessage = "";
                        foreach (var itemId in choice.Items)
                        {
                            var item = this.GameState.GetCustomItem(itemId);
                            if (item != null)
                            {
                                var member = this.GameState.Party.AddItem(new ItemInstance(item));
                                if (member != null)
                                {
                                    itemMessage += $"{member.Name} got {item.Name}\n";
                                    questMessage += this.GameState.CheckQuest(item, false);
                                }
                            }
                            
                            this.GameState.Party.AddItem(new ItemInstance(item));
                        }
                        
                        if (!string.IsNullOrEmpty(itemMessage))
                        {
                            new TalkWindow(this._ui).Show($"{itemMessage}{questMessage}{completedQuestMessage}",
                                DoneAction);
                            return;
                        }
                        
                        break;
                    case QuestAction.TakeItem:
                    {
                        var (item, member) = this.GameState.Party.RemoveItem(choice.ItemId);
                        if (item != null)
                        {
                            new TalkWindow(this._ui).Show($"{member.Name} gave {item.Name} to {this.SpriteState.Name}\n{completedQuestMessage}",
                                DoneAction);
                            return;
                        }

                        break;
                    }
                    case QuestAction.Fight:
                        var monster = this.GameState.Monsters.FirstOrDefault(m => m.Id == choice.MonsterId);
                        if (monster != null)
                        {
                            if (!string.IsNullOrEmpty(completedQuestMessage))
                            {
                                new TalkWindow(this._ui).Show(completedQuestMessage,
                                    () =>
                                    {
                                        this.GameState.StartFight(new[] { monster },
                                            MapScene.GetCurrentBiome(this.Map, this.Entity.Position));
                                    });
                            }
                            else
                            {
                                this.GameState.StartFight(new[] { monster },
                                    MapScene.GetCurrentBiome(this.Map, this.Entity.Position));
                            }
                            return;
                        }
                        break;
                    case QuestAction.Warp:
                        if (choice.MapId.HasValue)
                        {
                            if (!string.IsNullOrEmpty(completedQuestMessage))
                            {
                                new TalkWindow(this._ui).Show(completedQuestMessage,
                                    () =>
                                    {
                                        this.GameState.SetMap(choice.MapId,  choice.SpawnId);
                                    });
                            }
                            else
                            {
                                this.GameState.SetMap(choice.MapId,  choice.SpawnId);
                            }
                            return;
                        }
                        break;
                    case QuestAction.Join:
                        this.JoinParty();
                        new TalkWindow(this._ui).Show($"{this.SpriteState.Name} Joined the party\n{completedQuestMessage}",
                            DoneAction);
                        return;
                    case QuestAction.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!string.IsNullOrEmpty(completedQuestMessage))
                {
                    new TalkWindow(this._ui).Show(completedQuestMessage,
                        DoneAction);
                    return;
                }
                
                DoneAction();

            });
        }

        protected virtual void JoinParty()
        {
        }
    }
}