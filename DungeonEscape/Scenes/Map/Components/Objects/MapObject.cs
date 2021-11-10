using System;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Sprites;
using Nez.Tiled;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class MapObject: Component, ICollidable
    {
        protected readonly TmxObject tmxObject;
        protected readonly ObjectState state;
        private readonly int gridTileHeight;
        private readonly int gridTileWidth;
        private readonly TmxTilesetTile mapTile;
        private SpriteAnimator animator;
        protected IGame gameState;
        private TmxTileset tileSet;

        public static MapObject Create(TmxObject tmxObject, ObjectState state, int gridTileHeight, int gridTileWidth, TmxMap map, UISystem ui, IGame gameState)
        {
            if (!Enum.TryParse(tmxObject.Type, out SpriteType spriteType))
            {
                return null;
            }

            return spriteType switch
            {
                SpriteType.Ship => new Ship(tmxObject, state, gridTileHeight, gridTileWidth, map, gameState),
                SpriteType.Warp => new Warp(tmxObject, state, gridTileHeight, gridTileWidth, map, gameState),
                SpriteType.Chest => new Chest(tmxObject, state, gridTileHeight, gridTileWidth, map, ui, gameState),
                SpriteType.Door => new Door(tmxObject, state, gridTileHeight, gridTileWidth, map, ui, gameState),
                _ => new MapObject(tmxObject, state, gridTileHeight, gridTileWidth, map, gameState)
            };
        }

        protected MapObject(TmxObject tmxObject, ObjectState state, int gridTileHeight, int gridTileWidth, TmxMap map, IGame gameState)
        {
            this.gameState = gameState;
            this.tmxObject = tmxObject;
            this.state = state;
            this.gridTileHeight = gridTileHeight;
            this.gridTileWidth = gridTileWidth;
            if (tmxObject.Tile != null)
            {
                this.mapTile = map.GetTilesetTile(tmxObject.Tile.Gid);
                this.tileSet = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var pos = new Vector2
            {
                X = this.tmxObject.X + (int) (this.tmxObject.Width / 2.0),
                Y = this.tmxObject.Y - (int) (this.tmxObject.Height / 2.0)
            };

            this.Entity.SetPosition(pos);

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

            var offset = 5;
            var box = new Rectangle
            {
                X = (int) (-this.tmxObject.Width / 2.0f) + offset,
                Y = (int) (-this.tmxObject.Height / 2.0f) + offset,
                Width = (int) this.tmxObject.Width - offset,
                Height = (int) this.tmxObject.Height - offset
            };

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