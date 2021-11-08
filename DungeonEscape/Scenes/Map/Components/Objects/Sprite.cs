using System;
using System.Collections.Generic;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Nez;
using Nez.AI.Pathfinding;
using Nez.Sprites;
using Nez.Tiled;
using Random = Nez.Random;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Sprite : Component, IUpdatable, ICollidable
    {
        private readonly TmxObject tmxObject;
        private readonly TmxMap map;
        protected readonly IGame gameState;
        private SpriteAnimator animator;
        private Mover mover;
        private bool canMove;
        private MoveState state = MoveState.Stopped;
        private readonly AstarGridGraph graph;
        private List<Point> path;
        private const float MoveSpeed = 75;
        private int currentPathIndex;
        protected readonly SpriteState spriteState;
        private float elapsedTime;
        private float nextElapsedTime = Random.NextInt(5) + 1;
        private readonly TmxTileset tilset;
        private readonly int baseId;

        public static Sprite Create(TmxObject tmxObject, SpriteState state, TmxMap map, UISystem ui, IGame gameState, AstarGridGraph graph)
        {
            if (!Enum.TryParse(tmxObject.Type, out SpriteType spriteType))
            {
                return null;
            }

            return spriteType switch
            {
                SpriteType.NPC_Heal => new Healer(tmxObject, state, map, gameState, graph, ui),
                SpriteType.NPC_Store => new Store(tmxObject, state, map, gameState, graph, ui),
                SpriteType.NPC_Save => new Saver(tmxObject, state, map, gameState, graph, ui),
                SpriteType.NPC_Key => new KeyStore(tmxObject, state, map, gameState, graph, ui),
                SpriteType.NPC => new Character(tmxObject, state, map, ui, gameState, graph),
                _ => new Sprite(tmxObject, state, map, gameState, graph)
            };
        }
        
        protected Sprite(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph)
        {
            this.spriteState = state;
            this.graph = graph;
            this.tmxObject = tmxObject;
            this.map = map;
            this.gameState = gameState;
            this.tilset = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
            this.baseId = tmxObject.Tile.Gid - this.tilset.FirstGid;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.Entity.SetPosition(this.tmxObject.X + (int)(this.map.TileWidth/2.0), this.tmxObject.Y - (int)(this.map.TileHeight/2.0));
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this.tilset.Image.Texture, this.tilset.TileWidth, this.tilset.TileHeight,  this.tilset.Spacing);
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[this.baseId]));
            this.animator.Speed = 0.5f;
            this.animator.RenderLayer = 10;
            this.animator.AddAnimation("WalkDown", new[]
            {
                sprites[this.baseId + 0],
                sprites[this.baseId + 1]
            });

            this.animator.AddAnimation("WalkUp", new[]
            {
                sprites[this.baseId + 6],
                sprites[this.baseId + 7]
            });

            this.animator.AddAnimation("WalkRight", new[]
            {
                sprites[this.baseId + 2],
                sprites[this.baseId + 3]
            });

            this.animator.AddAnimation("WalkLeft", new[]
            {
                sprites[this.baseId + 4],
                sprites[this.baseId + 5]
            });
            
            
            this.mover = this.Entity.AddComponent(new Mover());
            this.canMove = bool.Parse(this.tmxObject.Properties["CanMove"]);
            this.animator.RenderLayer = 15;

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this,
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

        protected void DisplayVisual(bool display = true)
        {
            this.animator.SetEnabled(display);
        }

        void IUpdatable.Update()
        {
            if (this.gameState.IsPaused)
            {
                return;
            }
            
            if (!this.canMove)
            {
                return;
            }
            
            if (this.state == MoveState.Stopped)
            {
                this.elapsedTime += Time.DeltaTime;
                if (!(this.elapsedTime >= this.nextElapsedTime))
                {
                    return;
                }

                this.elapsedTime = 0;
                this.nextElapsedTime = Random.NextInt(5) + 1;
                    
                if (Random.Chance(0.05f))
                {
                    return;
                }
                    
                const int  MaxSpacesToMove = 2;
                var pos = this.Entity.Position;
                var mapGoTo = new Point(Random.NextInt(MaxSpacesToMove*2 + 1)-MaxSpacesToMove, Random.NextInt(MaxSpacesToMove*2 + 1)-MaxSpacesToMove);
                if (mapGoTo.X < 0)
                {
                    mapGoTo.X = 0;
                }
                if (mapGoTo.Y < 0)
                {
                    mapGoTo.Y = 0;
                }
                if (mapGoTo.X >= this.map.Width)
                {
                    mapGoTo.X = this.map.Width-1;
                }
                if (mapGoTo.Y >= this.map.Height)
                {
                    mapGoTo.X = this.map.Height-1;
                }
                    
                var toPos = pos + MapScene.ToRealLocation(mapGoTo, this.map);
                this.path = this.graph.Search(
                    MapScene.ToMapGrid(pos, this.map),
                    MapScene.ToMapGrid(toPos, this.map));

                if (this.path == null)
                {
                    this.state = MoveState.Stopped;
                }
                else
                {
                    this.currentPathIndex = 0;
                    this.state = MoveState.Moving;
                }
            }
            else if (this.state == MoveState.Moving)
            {
                if (this.path == null)
                {
                    this.state = MoveState.Stopped;
                    this.animator.Pause();
                }
                else
                {
                    var p1 = this.Entity.Position;
                    if (Vector2.Distance(p1,MapScene.ToRealLocation(this.path[this.currentPathIndex], this.map)) <= 1)
                    {
                        this.currentPathIndex++;
                        if (this.currentPathIndex >= this.path.Count)
                        {
                            this.state = MoveState.Stopped;
                            this.animator.Pause();
                            return;
                        }
                    }
                    
                    var p2 = MapScene.ToRealLocation(this.path[this.currentPathIndex], this.map);
                    var angle = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                    var vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var animation = "WalkDown";
                    if (vector.X < 0)
                    {
                        animation = "WalkLeft";
                    }
                    else if (vector.X > 0)
                    {
                        animation = "WalkRight";
                    }

                    if (vector.Y < 0)
                    {
                        animation = "WalkUp";
                    }
                    else if (vector.Y > 0)
                    {
                        animation = "WalkDown";
                    }
                    
                    if (! this.animator.IsAnimationActive(animation))
                    {
                        this.animator.Play(animation);
                    }
                    else
                    {
                        this.animator.UnPause();
                    }
                    
                    var movement = vector * MoveSpeed * Time.DeltaTime;
                    if (this.mover.CalculateMovement(ref movement, out _))
                    {
                        this.state = MoveState.Stopped;
                        this.animator.Pause();
                        return;
                    }
                    
                    this.mover.ApplyMovement(movement);
                }
            }
            
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