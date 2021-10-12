using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using Nez.Sprites;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public enum MoveState
    {
        Moving,
        Stopped,
    }
    
    public class SpriteComponent : Component, IUpdatable
    {
        private readonly TmxObject tmxObject;
        private readonly TmxMap map;
        private readonly int gridTileHeight;
        private readonly int gridTileWidth;
        private readonly TmxTilesetTile mapTile;
        private SpriteAnimator animator;
        private Mover mover;
        private bool canMove;
        private MoveState state = MoveState.Stopped;
        private AstarGridGraph graph;
        private List<Point> path;
        private const float MoveSpeed = 75;


        public SpriteComponent(TmxObject tmxObject, TmxMap map)
        {
            this.tmxObject = tmxObject;
            this.map = map;
            this.gridTileHeight = map.TileHeight;
            this.gridTileWidth = map.TileWidth;
            this.mapTile = map.GetTilesetTile(tmxObject.Tile.Gid);

            var floor = map.GetLayer<TmxLayer>("wall");
            var water = map.GetLayer<TmxLayer>("water");
            this.graph = new AstarGridGraph(new[] {floor, water});
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.Entity.SetPosition(this.tmxObject.X + (int)(gridTileWidth/2.0), this.tmxObject.Y - (int)(gridTileHeight/2.0));
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(mapTile.Image.Texture, 32, 32);
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this.mover = this.Entity.AddComponent(new Mover());
            this.canMove = bool.Parse(this.tmxObject.Properties["CanMove"]);

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this.tmxObject,
                new Rectangle
                {
                    X = (int)(-this.tmxObject.Width/2.0f), 
                    Y = (int)(-this.tmxObject.Height/2.0f), 
                    Width = (int) this.tmxObject.Width,
                    Height = (int) this.tmxObject.Height
                }));
            collider.IsTrigger = true;

            if (!bool.Parse(this.tmxObject.Properties["Collideable"]))
            {
                return;
            }
            
            var offsetWidth = (int) (this.tmxObject.Width * 0.25F);
            var offsetHeight = (int) (this.tmxObject.Height * 0.25F);
            this.Entity.AddComponent(new BoxCollider(new Rectangle
            {
                X = (int)(-this.tmxObject.Width/2.0f) + offsetWidth/2, 
                Y = (int)(-this.tmxObject.Height/2.0f), 
                Width = (int) this.tmxObject.Width - offsetWidth,
                Height = (int) this.tmxObject.Height - offsetHeight / 2
            }));
        }

        void IUpdatable.Update()
        {
            if (!this.canMove)
            {
                return;
            }
                
            if (this.state == MoveState.Stopped)
            {
                if (Random.NextInt(20) == 0)
                {
                    return;
                }

                var pos = this.Entity.Position;
                var (x, y) = pos + new Vector2(Random.NextInt(10) * this.map.TileWidth, Random.NextInt(10) * this.map.TileHeight);
                this.path = this.graph.Search(
                    new Point((int) pos.X / this.map.TileWidth, (int) pos.Y / this.map.TileHeight),
                    new Point((int) (x / this.map.TileWidth), (int) (y / this.map.TileHeight)));
            }
            else if (this.state == MoveState.Moving)
            {
                if (Random.NextInt(20) == 0)
                {
                    this.state = MoveState.Stopped;
                    return;
                }

                var distance = (MoveSpeed * Time.DeltaTime);
                //MoveToGoTo(Distace, pMap);

                //this.mover.CalculateMovement(ref movement, out _);
                //this.mover.ApplyMovement(movement);
            }
            
        }
    }
}