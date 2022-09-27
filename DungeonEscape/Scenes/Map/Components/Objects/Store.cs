namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
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

    public class Store : Sprite
    {
        private readonly UiSystem _ui;
        private readonly string _text;
        private readonly bool _willBuyItems;
        private const int MaxItems = 15;
        private const int MinItems = 10;

        public Store(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph,
            UiSystem ui) : base(
            tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            this._willBuyItems = !tmxObject.Properties.ContainsKey("WillBuyItems") ||
                                 bool.Parse(tmxObject.Properties["WillBuyItems"]);
            this._text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : "Welcome to my store.\nI buy and sell items. What can I do for you?";

            var itemListString = tmxObject.Properties.ContainsKey("Items") ? tmxObject.Properties["Items"] : null;
            if (itemListString != null)
            {
                this.SpriteState.Items = itemListString.Split(",")
                    .Select(gameState.GetCustomItem)
                    .Where(item => item != null).OrderBy(i => i.Cost).ToList();
            }
            else
            {
                var level = this.GameState.Party.MaxLevel();
                this.SpriteState.Items ??= new List<Item>();
                var missing = MinItems - this.SpriteState.Items.Count;
                for (var i = 0; i < missing; i++)
                {
                    var item = gameState.CreateRandomItem(level);
                    this.SpriteState.Items.Add(item);
                }

                this.SpriteState.Items = this.SpriteState.Items.OrderBy(i => i.Cost).ToList();
            }
        }

        public Store(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph,
            UiSystem ui, IReadOnlyCollection<Item> items, string text, bool willBuyItems) : base(
            tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
            this._text = text;
            this._willBuyItems = willBuyItems;
            this.SpriteState.Items = items.OrderBy(i=> i.Cost).ToList();
        }
        
        public override bool CanDoAction()
        {
            return true;
        }


        public override void OnAction(Action done)
        {
            var goldWindow = new GoldWindow(this.GameState.Party, this._ui.Canvas, this._ui.Sounds, new Point(MapScene.ScreenWidth - 155, MapScene.ScreenHeight / 3 * 2 - 55));
            goldWindow.ShowWindow();
            void Done()
            {
                goldWindow.CloseWindow();
                done();
            }

            var storeWindow = new StoreWindow(this._ui, this._willBuyItems, $"{this.SpriteState.Name}: {this._text}");
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
                        sellItems.Show(this.SpriteState.Items, item =>
                        {
                            if (item == null)
                            {
                                Done();
                                return;
                            }
                        
                            var selectedMember = this.GameState.Party.AliveMembers.FirstOrDefault(partyMember => partyMember.Items.Count < Party.MaxItems);
                            if (selectedMember == null)
                            {
                                new TalkWindow(this._ui).Show($"{this.SpriteState.Name}: You do not have enough space in your inventory for {item.Name}", Done);
                            }
                            else
                            {
                                if (this.GameState.Party.Gold >= item.Cost)
                                {
                                    new TalkWindow(this._ui).Show($"{this.SpriteState.Name}: {selectedMember.Name} got the {item.Name}", Done);
                                    selectedMember.Items.Add(new ItemInstance(item));
                                    this.GameState.Party.Gold -= item.Cost;
                                    this.SpriteState.Items.Remove(item);
                                }
                                else
                                {
                                    new TalkWindow(this._ui).Show($"{this.SpriteState.Name}: You do not have enough gold for the {item.Name}", Done);
                                }
                            }
                        });
                        break;
                    }
                    case StoreAction.Sell when this.GameState.Party.AliveMembers.All(partyMember => partyMember.Items.Count == 0):
                        new TalkWindow(this._ui).Show("{this.SpriteState.Name}: You do not have any items that I would like to buy.", Done);
                        return;
                    case StoreAction.Sell:
                    {
                        var selectHero = new SelectHeroWindow(this._ui);
                        selectHero.Show(this.GameState.Party.AliveMembers,
                        hero =>
                        {
                            var inventoryWindow = new SellPartyItemsWindow(this._ui, this.GameState.Party.Members);
                            inventoryWindow.Show(hero.Items.Where(i => i.Gold != 0 && i.Type != ItemType.Quest), item =>
                            {
                                if (item == null)
                                {
                                    Done();
                                    return;
                                }

                                var questionWindow = new QuestionWindow(this._ui);
                                questionWindow.Show(
                                    $"{this.SpriteState.Name}: You can sell the {item.Name} to me for {item.Gold * 3 / 4} gold",
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

                                        hero.Items.Remove(item);
                                        if (this.SpriteState.Items.Count <= MaxItems)
                                        {
                                            this.SpriteState.Items.Add(item.Item);
                                        }

                                        Done();
                                    });
                            });
                        });
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }
            });
        }
    }
}