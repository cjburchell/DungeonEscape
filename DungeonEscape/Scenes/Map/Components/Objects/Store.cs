﻿namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;
    using UI;
    using Random = Nez.Random;

    public class Store : Sprite
    {
        private readonly UiSystem _ui;
        private readonly List<Item> _items;

        public Store(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UiSystem ui) : base(
            tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            var level = this.GameState.Party.Members.Max(item => item.Level);
            var availableItems = this.GameState.Items.Where(item => item.MinLevel < level).ToArray();
            var maxItems = Math.Min(5, availableItems.Length);
            this._items = new List<Item>();
            for (var i = 0; i < maxItems; i++)
            {
                var item = availableItems[Random.NextInt(availableItems.Length)];
                this._items.Add(item);
                var tempItems = availableItems.ToList();
                tempItems.Remove(item);
                availableItems = tempItems.ToArray();
            }
        }

        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            
            var goldWindow = new GoldWindow(party, this._ui.Canvas, new Point(MapScene.ScreenWidth - 155, MapScene.ScreenHeight / 3 * 2 - 55));
            goldWindow.ShowWindow();
            void Done()
            {
                goldWindow.CloseWindow();
                this.GameState.IsPaused = false;
            }

            var storeWindow = new StoreWindow(this._ui);
            storeWindow.Show(action =>
            {
                switch (action)
                {
                    case null:
                        Done();
                        return;
                    case StoreAction.Buy:
                    {
                        var sellItems = new BuyItemsWindow(this._ui);
                        sellItems.Show(this._items, item =>
                        {
                            if (item == null)
                            {
                                Done();
                                return;
                            }
                        
                            if (this.GameState.Party.Items.Count >= Party.MaxItems)
                            {
                                new TalkWindow(this._ui).Show($"You do not have enough space in your inventory for {item.Name}", Done);
                            }
                            else
                            {
                                if (this.GameState.Party.Gold >= item.Gold)
                                {
                                    new TalkWindow(this._ui).Show($"You got the {item.Name}", Done);
                                    this.GameState.Party.Items.Add(new ItemInstance(item));
                                    this.GameState.Party.Gold -= item.Gold;
                                    this._items.Remove(item);
                                }
                                else
                                {
                                    new TalkWindow(this._ui).Show($"You do not have enough gold for the {item.Name}", Done);
                                }
                            }
                        });
                        break;
                    }
                    case StoreAction.Sell when this.GameState.Party.Items.Count == 0:
                        new TalkWindow(this._ui).Show("You do not have any items that I would like to buy.", Done);
                        return;
                    case StoreAction.Sell:
                    {
                        var inventoryWindow = new SellPartyItemsWindow(this._ui);
                        inventoryWindow.Show(this.GameState.Party.Items, item =>
                        {
                            if (item == null)
                            {
                                Done();
                                return;
                            }

                            var questionWindow = new QuestionWindow(this._ui);
                            questionWindow.Show(
                                $"You can sell the {item.Name} to me for {item.Gold * 3 / 4} gold",
                                result =>
                                {
                                    if (!result)
                                    {
                                        this.GameState.IsPaused = false;
                                        return;
                                    }

                                    this.GameState.Party.Gold += item.Gold * 3 / 4;
                                    if (item.IsEquipped)
                                    {
                                        item.UnEquip(this.GameState.Party.Members);
                                    }

                                    this.GameState.Party.Items.Remove(item);
                                    Done();
                                });
                        });
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }
            });
            return true;
        }
    }
}