using DungeonEscape.State;
using Nez.AI.Pathfinding;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Nez;
    using UI;
    using Random = Nez.Random;

    public class Store : Sprite
    {
        private readonly UISystem ui;
        private readonly List<Item> items;

        public Store(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UISystem ui) : base(
            tmxObject, state, map, gameState, graph)
        {
            this.ui = ui;
            var level = this.gameState.Party.Members.Max(item => item.Level);
            var availableItems = this.gameState.Items.Where(item => item.MinLevel < level).ToArray();
            var maxItems = Math.Min(5, availableItems.Length);
            this.items = new List<Item>();
            for (var i = 0; i < maxItems; i++)
            {
                var item = availableItems[Random.NextInt(availableItems.Length)];
                this.items.Add(item);
                var tempItems = availableItems.ToList();
                tempItems.Remove(item);
                availableItems = tempItems.ToArray();
            }
        }

        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            
            var goldWindow = new GoldWindow(party, this.ui.Canvas);
            goldWindow.ShowWindow();
            void Done()
            {
                goldWindow.CloseWindow();
                this.gameState.IsPaused = false;
            }

            var storeWindow = new StoreWindow(this.ui);
            storeWindow.Show(action =>
            {
                if (!action.HasValue)
                {
                    Done();
                    return;
                }
                
                if (action is StoreAction.Buy)
                {
                    var sellitems = new BuyItemsWindow(this.ui);
                    sellitems.Show(this.items, item =>
                    {
                        if (item == null)
                        {
                            Done();
                            return;
                        }
                        
                        if (this.gameState.Party.Items.Count >= Party.MaxItems)
                        {
                            var talkWindow = new TalkWindow(this.ui, "No Space");
                            talkWindow.Show($"You do not have enough space in your inventory for {item.Name}", Done);
                        }
                        else
                        {
                            if (this.gameState.Party.Gold >= item.Gold)
                            {
                                var talkWindow = new TalkWindow(this.ui, "Got Item");
                                talkWindow.Show($"You got the {item.Name}", Done);
                                this.gameState.Party.Items.Add(new ItemInstance(item));
                                this.gameState.Party.Gold -= item.Gold;
                                this.items.Remove(item);
                            }
                            else
                            {
                                var talkWindow = new TalkWindow(this.ui, "No Gold");
                                talkWindow.Show($"You do not have enough gold for the {item.Name}", Done);
                            }
                        }
                    });
                }
                else if (action is StoreAction.Sell)
                {
                    if (this.gameState.Party.Items.Count == 0)
                    {
                        var talkWindow = new TalkWindow(this.ui, "No Items");
                        talkWindow.Show("You do not have any items that I would like to buy.", Done);
                        return;
                    }

                    var inventoryWindow = new SellPartyItemsWindow(this.ui);
                    inventoryWindow.Show(this.gameState.Party.Items, item =>
                    {
                        if (item == null)
                        {
                            Done();
                            return;
                        }

                        var questionWindow = new QuestionWindow(this.ui);
                        questionWindow.Show(
                            $"You can sell the {item.Name} to me for {(item.Gold * 3) / 4} gold",
                            result =>
                            {
                                if (!result)
                                {
                                    this.gameState.IsPaused = false;
                                    return;
                                }

                                this.gameState.Party.Gold += item.Gold * 3 / 4;
                                if (item.IsEquipped)
                                {
                                    item.Unequip(this.gameState.Party.Members);
                                }

                                this.gameState.Party.Items.Remove(item);
                                Done();
                            });
                    });
                }
            });
            return true;
        }
    }
}