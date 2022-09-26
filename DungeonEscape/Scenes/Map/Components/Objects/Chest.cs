using System;
using System.Collections.Generic;
using System.Linq;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Nez.Sprites;
    using Nez.Tiled;
    using State;

    public class Chest : MapObject
    {
        private readonly UiSystem _ui;
        private readonly int _level;
        private SpriteAnimator _openImage;
        private readonly int _openImageId;
        
        private bool IsOpen
        {
            get => this.ObjectState.IsOpen != null && this.ObjectState.IsOpen.Value;
            set => this.ObjectState.IsOpen = value;
        }

        public Chest(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState) : base(tmxObject, state, map, gameState)
        {
            this._ui = ui;
            this._level = tmxObject.Properties.ContainsKey("ChestLevel") ? int.Parse(tmxObject.Properties["ChestLevel"]) : 0;
            this._openImageId = tmxObject.Properties.ContainsKey("OpenImage") ? int.Parse(tmxObject.Properties["OpenImage"]) : 135;
            if(this.State.Items != null)
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
                this.State.Items = new List<Item> {GameState.CreateGold(int.Parse(tmxObject.Properties["Gold"]))};
                return;
            }
            
            
            this.State.Items = new List<Item> {GameState.CreateChestItem(this._level == 0 ? this.GameState.Party.MaxLevel() : this._level)};
        }
        
        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.IsOpen);
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this.TileSet.Image.Texture,
                (int) this.TmxObject.Width, (int) this.TmxObject.Height);
            this._openImage =
                this.Entity.AddComponent(
                    new SpriteAnimator(sprites[this._openImageId]));
            this._openImage.RenderLayer = this.RenderLevel;
            this._openImage.SetEnabled(this.IsOpen);
        }

        public override bool CanDoAction()
        {
            return !this.IsOpen && this.GameState.Party.CanOpenChest(this._level);
        }

        public override void OnAction(Action done)
        {
            if (this.IsOpen)
            {
                new TalkWindow(this._ui).Show("You found nothing", done);
                return;
            }

            if (!this.GameState.Party.CanOpenChest(this._level))
            {
                new TalkWindow(this._ui).Show("Unable to open chest", done);
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
                    gotItem = true;
                    this.State.Items.Remove(item);
                }
                else
                {
                    var selectedMember = this.GameState.Party.AddItem(new ItemInstance(item));
                    if (selectedMember == null)
                    {
                        message +=
                            $"You found {item.Name} but your party did not have enough space in your inventory it\n";
                    }
                    else
                    {
                        this.State.Items.Remove(item);
                        gotItem = true;
                        var questMessage = GameState.CheckQuest(item);
                        message += $"{selectedMember.Name} found a {item.Name}\n{questMessage}\n";
                    }
                }
            }

            if (string.IsNullOrEmpty(message))
            {
                done();
            }
            else
            {
                new TalkWindow(this._ui).Show(message, done);
            }
            
            if (gotItem)
            {
                this.GameState.Sounds.PlaySoundEffect("treasure");
            }

            this.IsOpen = !this.State.Items.Any();
            this.DisplayVisual(!this.IsOpen);
            this._openImage.SetEnabled(this.IsOpen);
        }
    }
}