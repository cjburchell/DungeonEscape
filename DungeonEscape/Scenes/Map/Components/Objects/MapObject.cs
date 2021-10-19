using System;
using System.Collections.Generic;
using DungeonEscape.Scenes.Map.Components.UI;
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
        private readonly int gridTileHeight;
        private readonly int gridTileWidth;
        private readonly TmxTilesetTile mapTile;
        private SpriteAnimator animator;
        protected IGame gameState;

        public static MapObject Create(TmxObject tmxObject,int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, TalkWindow talkWindow, IGame gameState)
        {
            if (!Enum.TryParse(tmxObject.Type, out SpriteType spriteType))
            {
                return null;
            }

            return spriteType switch
            {
                SpriteType.Ship => new Ship(tmxObject, gridTileHeight, gridTileWidth, mapTile, gameState),
                SpriteType.Warp => new Warp(tmxObject, gridTileHeight, gridTileWidth, mapTile, gameState),
                SpriteType.Chest => new Chest(tmxObject, gridTileHeight, gridTileWidth, mapTile, talkWindow, gameState),
                SpriteType.Door => new Door(tmxObject, gridTileHeight, gridTileWidth, mapTile, talkWindow, gameState),
                _ => new MapObject(tmxObject, gridTileHeight, gridTileWidth, mapTile, gameState)
            };
        }

        protected MapObject(TmxObject tmxObject,int gridTileHeight, int gridTileWidth, TmxTilesetTile mapTile, IGame gameState)
        {
            this.gameState = gameState;
            this.tmxObject = tmxObject;
            this.gridTileHeight = gridTileHeight;
            this.gridTileWidth = gridTileWidth;
            this.mapTile = mapTile;
        }

        public override void Initialize()
        {
            base.Initialize();

            this.Entity.SetPosition(this.tmxObject.X + (int) (this.gridTileWidth / 2.0),
                this.tmxObject.Y - (int) (this.gridTileHeight / 2.0));
            
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(mapTile.Image.Texture, 32, 32);
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

        public virtual void OnHit(Player player)
        {
        }

        public virtual bool OnAction(Player player)
        {
            return false;
        }
    }
}