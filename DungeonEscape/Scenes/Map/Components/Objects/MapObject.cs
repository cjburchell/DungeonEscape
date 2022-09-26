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
        public ObjectState ObjectState { get; }
        private readonly TmxTilesetTile _mapTile;
        private SpriteAnimator _animator;
        protected readonly IGame GameState;
        protected readonly TmxTileset TileSet;
        private string _currentAnimation;
        protected readonly int RenderLevel;

        public static MapObject Create(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState)
        {
            if (!Enum.TryParse(tmxObject.Class, out SpriteType spriteType))
            {
                return null;
            }
            
            state.Type = spriteType;

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
            this.RenderLevel = map.Height * map.TileHeight + 15;
            this.GameState = gameState;
            this.TmxObject = tmxObject;
            this.ObjectState = state;
            if (tmxObject.Tile == null)
            {
                return;
            }

            this._mapTile = map.GetTilesetTile(tmxObject.Tile.Gid);
            this.TileSet = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
            this.State.Name = tmxObject.Name;
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
                }
                else if(this._mapTile.AnimationFrames != null)
                {
                    var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this.TileSet.Image.Texture,
                        (int) this.TmxObject.Width, (int) this.TmxObject.Height);
                    this._animator =
                        this.Entity.AddComponent(
                            new SpriteAnimator(sprites[this._mapTile.AnimationFrames[0].Gid]));

                    var animation = this._mapTile.AnimationFrames.Select(i => sprites[i.Gid]).ToArray();
                    _currentAnimation = "animate";
                    this._animator.AddAnimation(_currentAnimation, animation);
                    this._animator.Speed = this._mapTile.AnimationFrames[0].Duration*10f;
                }
               
            }
            else if (this.TileSet != null)
            {
                var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this.TileSet.Image.Texture,
                    (int) this.TmxObject.Width, (int) this.TmxObject.Height);
                this._animator =
                    this.Entity.AddComponent(
                        new SpriteAnimator(sprites[this.TmxObject.Tile.Gid - this.TileSet.FirstGid]));
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
                this._animator.RenderLayer = this.RenderLevel;
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

        public virtual void OnHit()
        {
        }

        public virtual void OnAction(Action done)
        {
            done();
        }

        public virtual bool CanDoAction()
        {
            return false;
        }

        public BaseState State => this.ObjectState;

        public virtual void Update()
        {
            if (this.GameState.IsPaused)
            {
                return;
            }

            if (string.IsNullOrEmpty(_currentAnimation))
            {
                return;
            }
            
            if (!this._animator.IsAnimationActive(_currentAnimation))
            {
                this._animator.Play(_currentAnimation);
            }
            else
            {
                this._animator.UnPause();
            }
        }
    }
}