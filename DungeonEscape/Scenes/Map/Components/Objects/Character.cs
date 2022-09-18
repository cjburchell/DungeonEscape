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
                    Dialogs = new List<DialogHead> { new() {Text = text, Choices = new List<Choice> { new() {Text = "OK"}}}}
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
            this.StartDialog(this.Dialog.Dialogs,() =>
            {
                this.GameState.IsPaused = false;
            });
            return true;

        }

        private void StartDialog(IReadOnlyCollection<DialogHead> dialogs, Action done)
        {
            var dialog = dialogs.FirstOrDefault(i => i.StartQuest && this.GameState.Party.ActiveQuests.All(q => i.Quest != q.Id));
            Quest quest = null;
            if (dialog != null)
            {
                quest = this.GameState.Quests.FirstOrDefault(i => i.Id == dialog.Quest);
                if (quest != null)
                {
                    var activeQuest = new ActiveQuest
                    {
                        Id = quest.Id, CurrentStage = 0,
                        Stages = quest.Stages.Select(i => new QuestStageState {Number = i.Number}).ToList()
                    };
                    this.GameState.Party.ActiveQuests.Add(activeQuest);
                }
            }
            else
            {
                foreach (var activeQuest in this.GameState.Party.ActiveQuests)
                {
                    dialog = dialogs.FirstOrDefault(
                        i => i.Quest == activeQuest.Id && i.QuestStage != null && i.QuestStage.Contains(activeQuest.CurrentStage));
                    if (dialog == null) continue;

                    quest = this.GameState.Quests.FirstOrDefault(i => i.Id == dialog.Quest);
                    if (quest != null)
                    {
                        break;
                    }
                }
            }
            
            dialog ??= dialogs.FirstOrDefault();
            if (dialog == null)
            {
                done();
                return;
            }
            
            ShowDialog(dialog, done, quest);
        }

        private void ShowDialog(DialogText dialog, Action done, Quest quest)
        {
            // Choose the dialog based on the correct quest state.
             new TalkWindow(this._ui).Show($"{this.SpriteState.Name}: {dialog.Text}", dialog.Choices?.Where(choice =>
            {
                if (choice.Actions.Contains(QuestAction.TakeItem) && choice.ItemId != null)
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
                    completedQuestMessage = this.GameState.AdvanceQuest(quest.Id, choice.NextQuestStage);
                }
                
                void DoneAction()
                {
                    if (choice.Dialog != null)
                    {
                        this.ShowDialog(choice.Dialog, done, quest);
                    }
                    else
                    {
                        done();
                    }
                }
                
                bool ProcessActions(ICollection<QuestAction> actions)
                {
                    if (!actions.Any())
                    {
                        return true;
                    }
                    
                    var action = actions.FirstOrDefault();
                    actions.Remove(action);
                    
                    void DoneProcessAction()
                    {
                        if(!ProcessActions(actions)) return;
                        DoneAction();
                    }
                    
                    switch (action)
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

                            if (string.IsNullOrEmpty(itemMessage)) break;

                            new TalkWindow(this._ui).Show($"{itemMessage}{questMessage}{completedQuestMessage}",
                                DoneProcessAction);
                            return false;
                        case QuestAction.TakeItem:
                        {
                            var (item, member) = this.GameState.Party.RemoveItem(choice.ItemId);
                            if (item == null) break;

                            new TalkWindow(this._ui).Show(
                                $"{member.Name} gave {item.Name} to {this.SpriteState.Name}\n{completedQuestMessage}",
                                DoneProcessAction);
                            return false;
                        }
                        case QuestAction.Fight:
                            var monster = this.GameState.Monsters.FirstOrDefault(m => m.Id == choice.MonsterId);
                            if (monster == null)
                            {
                                break;
                            }

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

                            return false;
                        case QuestAction.Warp:
                            if (!choice.MapId.HasValue)
                            {
                                break;
                            }

                            if (!string.IsNullOrEmpty(completedQuestMessage))
                            {
                                new TalkWindow(this._ui).Show(completedQuestMessage,
                                    () => { this.GameState.SetMap(choice.MapId, choice.SpawnId); });
                            }
                            else
                            {
                                this.GameState.SetMap(choice.MapId, choice.SpawnId);
                            }

                            return false;
                        case QuestAction.Join:
                            this.JoinParty();
                            new TalkWindow(this._ui).Show(
                                $"{this.SpriteState.Name} Joined the party\n{completedQuestMessage}",
                                DoneProcessAction);
                            return false;
                        case QuestAction.OpenDoor:
                        {
                            var mapId = choice.MapId ?? this.GameState.Party.CurrentMapId;
                            var mapState = this.GameState.MapStates.FirstOrDefault(item => item.Id == mapId);
                            var door = mapState?.Objects.FirstOrDefault(i => i.Id == choice.ObjectId);
                            if (door != null)
                            {
                                this.GameState.Sounds.PlaySoundEffect("door");
                                door.IsOpen = true;
                            }
                            break;
                        }
                        case QuestAction.None:
                        default:
                            break;
                    }

                    return true;
                }

                if (!ProcessActions(choice.Actions)) return;

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