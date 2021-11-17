namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.Sprites;
    using Nez.Tiled;
    using State;

    public class MapObject: Component, ICollidable
    {
        protected readonly TmxObject tmxObject;
        protected readonly ObjectState state;
        private readonly TmxTilesetTile mapTile;
        private SpriteAnimator animator;
        protected readonly IGame gameState;
        private readonly TmxTileset tileSet;

        public static MapObject Create(TmxObject tmxObject, ObjectState state, TmxMap map, UISystem ui, IGame gameState)
        {
            if (!Enum.TryParse(tmxObject.Type, out SpriteType spriteType))
            {
                return null;
            }

            return spriteType switch
            {
                SpriteType.Ship => new Ship(tmxObject, state, map, gameState),
                SpriteType.Warp => new Warp(tmxObject, state, map, gameState),
                SpriteType.Chest => new Chest(tmxObject, state, map, ui, gameState),
                SpriteType.Door => new Door(tmxObject, state, map, ui, gameState),
                _ => new MapObject(tmxObject, state, map, gameState)
            };
        }

        protected MapObject(TmxObject tmxObject, ObjectState state, TmxMap map, IGame gameState)
        {
            this.gameState = gameState;
            this.tmxObject = tmxObject;
            this.state = state;
            if (tmxObject.Tile == null)
            {
                return;
            }

            this.mapTile = map.GetTilesetTile(tmxObject.Tile.Gid);
            this.tileSet = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
        }

        public override void Initialize()
        {
            base.Initialize();
            
            if (this.mapTile != null)
            {
                var texture = this.mapTile.Image.Texture;
                var sprites =
                    Nez.Textures.Sprite.SpritesFromAtlas(this.mapTile.Image.Texture, texture.Width, texture.Height);
                this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
                this.animator.RenderLayer = 20;
            }
            else if (this.tileSet != null)
            {
                var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this.tileSet.Image.Texture,
                    (int) this.tmxObject.Width, (int) this.tmxObject.Height);
                this.animator =
                    this.Entity.AddComponent(
                        new SpriteAnimator(sprites[this.tmxObject.Tile.Gid - this.tileSet.FirstGid]));
                this.animator.RenderLayer = 20;
            }

            Rectangle box;
            Vector2 pos;
            if (this.animator == null)
            {
                pos = new Vector2
                {
                    X = this.tmxObject.X,
                    Y = this.tmxObject.Y
                };
                
                box = new Rectangle
                {
                    Y = 0,
                    X = 0,
                    Width = (int)this.tmxObject.Width,
                    Height = (int)this.tmxObject.Height
                };
            }
            else
            {
                pos = new Vector2
                {
                    X = this.tmxObject.X + (int) (this.tmxObject.Width / 2.0),
                    Y = this.tmxObject.Y - (int) (this.tmxObject.Height / 2.0)
                };
                
                box = new Rectangle
                {
                    X = (int) (-this.tmxObject.Width / 2.0f),
                    Y = (int) (-this.tmxObject.Height / 2.0f),
                    Width = (int) this.tmxObject.Width,
                    Height = (int) this.tmxObject.Height
                };
            }
            
            this.Entity.SetPosition(pos);
            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this, box));
            collider.IsTrigger = true;
        }

        protected void DisplayVisual(bool display = true)
        {
            this.animator?.SetEnabled(display);
        }

        public virtual void OnHit(Party party)
        {
        }

        public virtual bool OnAction(Party party)
        {
            return false;
        }
    }
}