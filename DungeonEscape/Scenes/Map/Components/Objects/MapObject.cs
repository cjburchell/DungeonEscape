using System.Linq;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.Sprites;
    using Nez.Tiled;
    using State;

    public class MapObject: Component, ICollidable, IUpdatable
    {
        protected readonly TmxObject TmxObject;
        protected readonly ObjectState State;
        private readonly TmxTilesetTile _mapTile;
        private SpriteAnimator _animator;
        protected readonly IGame GameState;
        protected readonly TmxTileset _tileSet;
        private string currentAnimanion;

        public static MapObject Create(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState)
        {
            if (!Enum.TryParse(tmxObject.Class, out SpriteType spriteType))
            {
                return null;
            }

            return spriteType switch
            {
                SpriteType.Warp => new Warp(tmxObject, state, map, gameState),
                SpriteType.Chest => new Chest(tmxObject, state, map, ui, gameState),
                SpriteType.HiddenItem => new HiddenItem(tmxObject, state, map, ui, gameState),
                SpriteType.Door => new Door(tmxObject, state, map, ui, gameState),
                _ => new MapObject(tmxObject, state, map, gameState)
            };
        }

        protected MapObject(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState)
        {
            this.GameState = gameState;
            this.TmxObject = tmxObject;
            this.State = state;
            if (tmxObject.Tile == null)
            {
                return;
            }

            this._mapTile = map.GetTilesetTile(tmxObject.Tile.Gid);
            this._tileSet = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
        }

        public override void Initialize()
        {
            base.Initialize();
            
            if (this._mapTile != null)
            {
                if (this._mapTile.Image != null)
                {
                    var texture = this._mapTile.Image.Texture;
                    var sprites =
                        Nez.Textures.Sprite.SpritesFromAtlas(this._mapTile.Image.Texture, texture.Width, texture.Height);
                    this._animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
                    this._animator.RenderLayer = 20;
                }
                else if(this._mapTile.AnimationFrames != null)
                {
                    var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this._tileSet.Image.Texture,
                        (int) this.TmxObject.Width, (int) this.TmxObject.Height);
                    this._animator =
                        this.Entity.AddComponent(
                            new SpriteAnimator(sprites[this._mapTile.AnimationFrames[0].Gid]));

                    var animation = this._mapTile.AnimationFrames.Select(i => sprites[i.Gid]).ToArray();
                    currentAnimanion = "animate";
                    this._animator.AddAnimation(currentAnimanion, animation);
                    this._animator.Speed = this._mapTile.AnimationFrames[0].Duration*10f;
                    this._animator.RenderLayer = 20;
                }
               
            }
            else if (this._tileSet != null)
            {
                var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this._tileSet.Image.Texture,
                    (int) this.TmxObject.Width, (int) this.TmxObject.Height);
                this._animator =
                    this.Entity.AddComponent(
                        new SpriteAnimator(sprites[this.TmxObject.Tile.Gid - this._tileSet.FirstGid]));
                this._animator.RenderLayer = 20;
            }

            Rectangle box;
            Vector2 pos;
            if (this._animator == null)
            {
                pos = new Vector2
                {
                    X = this.TmxObject.X,
                    Y = this.TmxObject.Y
                };
                
                box = new Rectangle
                {
                    Y = 0,
                    X = 0,
                    Width = (int)this.TmxObject.Width,
                    Height = (int)this.TmxObject.Height
                };
            }
            else
            {
                pos = new Vector2
                {
                    X = this.TmxObject.X + (int) (this.TmxObject.Width / 2.0),
                    Y = this.TmxObject.Y - (int) (this.TmxObject.Height / 2.0)
                };
                
                box = new Rectangle
                {
                    X = (int) (-this.TmxObject.Width / 2.0f),
                    Y = (int) (-this.TmxObject.Height / 2.0f),
                    Width = (int) this.TmxObject.Width,
                    Height = (int) this.TmxObject.Height
                };
            }
            
            this.Entity.SetPosition(pos);
            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this, box));
            collider.IsTrigger = true;
        }

        protected void DisplayVisual(bool display = true)
        {
            this._animator?.SetEnabled(display);
        }

        public virtual void OnHit(Party party)
        {
        }

        public virtual bool OnAction(Party party)
        {
            return false;
        }

        public virtual void Update()
        {
            if (this.GameState.IsPaused)
            {
                return;
            }

            if (string.IsNullOrEmpty(currentAnimanion))
            {
                return;
            }
            
            if (!this._animator.IsAnimationActive(currentAnimanion))
            {
                this._animator.Play(currentAnimanion);
            }
            else
            {
                this._animator.UnPause();
            }
        }
    }
}