using System;
using System.Collections.Generic;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System.Linq;
    using Common.Components.UI;
    using Nez.Tiled;
    using State;

    public class HiddenItem : MapObject
    {
        private readonly UiSystem _ui;
        private readonly int _level;
        
        private bool IsOpen
        {
            get => this.ObjectState.IsOpen != null && this.ObjectState.IsOpen.Value;
            set => this.ObjectState.IsOpen = value;
        }

        public HiddenItem(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState) : base(
            tmxObject, state, map, gameState)
        {
            this._ui = ui;
            this._level = tmxObject.Properties.ContainsKey("Level") ? int.Parse(tmxObject.Properties["Level"]) : 0;
            if (this.State.Items != null)
            {
                var tileSet = Game.LoadTileSet("Content/items2.tsx");
                foreach (var item in this.State.Items)
                {
                    item.Setup(tileSet, gameState.Skills);
                }

                return;
            }

            if (this.TmxObject.Properties.ContainsKey("ItemId"))
            {
                this.State.Items = new List<Item> { this.GameState.GetCustomItem(tmxObject.Properties["ItemId"]) };
                return;
            }

            if (this.TmxObject.Properties.ContainsKey("Gold"))
            {
                this.State.Items = new List<Item> { GameState.CreateGold(int.Parse(tmxObject.Properties["Gold"])) };
                return;
            }

            this.State.Items = new List<Item>
                { GameState.CreateChestItem(this._level == 0 ? this.GameState.Party.MaxLevel() : this._level) };
        }

        public override bool CanDoAction()
        {
            return !this.IsOpen && this.GameState.Party.CanOpenChest(this._level);
        }

        public override void OnAction(Action done)
        {
            if (!CanDoAction())
            {
                done();
                return;
            }
            
            var message = "";
            var gotItem = false;
            foreach (var item in this.State.Items.ToList())
            {
                if (item.Type == ItemType.Quest && !item.StartQuest)
                {
                    if (!this.GameState.Party.ActiveQuests.Any(i => i.Id == item.QuestId && item.ForStage.Contains(i.CurrentStage)))
                    {
                        continue;
                    }
                }
            
                if (item.Type == ItemType.Gold)
                {
                    message += $"You found {item.Cost} Gold\n";
                    this.GameState.Party.Gold += item.Cost;
                    this.State.Items.Remove(item);
                    gotItem = true;
                }
                else
                {
                    var selectedMember = this.GameState.Party.AddItem(new ItemInstance(item));
                    if (selectedMember == null)
                    {
                        message += $"You found {item.Name} but your party did not have enough space in your inventory it\n";
                    }
                    else
                    {
                        var questMessage = GameState.CheckQuest(item);
                        message += $"{selectedMember.Name} found a {item.Name}\n{questMessage}\n";
                        this.State.Items.Remove(item);
                        gotItem = true;
                    }
                }
            }
            if (gotItem)
            {
                this.GameState.Sounds.PlaySoundEffect("treasure");
            }

            void Done()
            {
                this.IsOpen = !this.State.Items.Any();
                done();
            }
            
            if (!string.IsNullOrEmpty(message))
            {
                new TalkWindow(this._ui).Show(message, Done);
            }
            else
            {
                Done();
            }
        }
    }
}