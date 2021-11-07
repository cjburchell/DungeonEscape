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

        public static MapObject Create(TmxObject tmxObject, ObjectState state, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, UISystem ui, IGame gameState)
        {
            if (!Enum.TryParse(tmxObject.Type, out SpriteType spriteType))
            {
                return null;
            }

            return spriteType switch
            {
                SpriteType.Ship => new Ship(tmxObject, state, gridTileHeight, gridTileWidth, mapTile, gameState),
                SpriteType.Warp => new Warp(tmxObject, state, gridTileHeight, gridTileWidth, mapTile, gameState),
                SpriteType.Chest => new Chest(tmxObject, state, gridTileHeight, gridTileWidth, mapTile, ui, gameState),
                SpriteType.Door => new Door(tmxObject, state, gridTileHeight, gridTileWidth, mapTile, ui, gameState),
                _ => new MapObject(tmxObject, state, gridTileHeight, gridTileWidth, mapTile, gameState)
            };
        }

        protected MapObject(TmxObject tmxObject, ObjectState state, int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, IGame gameState)
        {
            this.gameState = gameState;
            this.tmxObject = tmxObject;
            this.state = state;
            this.gridTileHeight = gridTileHeight;
            this.gridTileWidth = gridTileWidth;
            this.mapTile = mapTile;
        }

        public override void Initialize()
        {
            base.Initialize();

            this.Entity.SetPosition(this.tmxObject.X + (int) (this.gridTileWidth / 2.0),
                this.tmxObject.Y - (int) (this.gridTileHeight / 2.0));
            
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(mapTile.Image.Texture, MapScene.DefaultTileSize, MapScene.DefaultTileSize);
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this.animator.RenderLayer = 20;

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this,
                new Rectangle
                {
                    X = (int)(-this.tmxObject.Width/2.0f), 
                    Y = (int)(-this.tmxObject.Height/2.0f), 
                    Width = (int) this.tmxObject.Width,
                    Height = (int) this.tmxObject.Height
                }));
            collider.IsTrigger = true;
        }
        
        protected void DisplayVisual(bool display = true)
        {
            this.animator.SetEnabled(display);
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