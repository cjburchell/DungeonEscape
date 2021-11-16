namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.AI.Pathfinding;
    using Nez.Sprites;
    using Nez.Tiled;
    using State;
    using Random = Nez.Random;

    public class Sprite : Component, IUpdatable, ICollidable
    {
        private readonly TmxObject tmxObject;
        private readonly TmxMap map;
        protected readonly IGame gameState;
        private SpriteAnimator animator;
        private Mover mover;
        private readonly bool canMove;
        private MoveState state = MoveState.Stopped;
        private readonly AstarGridGraph graph;
        private List<Point> path;
        private const float MoveSpeed = 75;
        private int currentPathIndex;
        // ReSharper disable once NotAccessedField.Local
        private readonly SpriteState spriteState;
        private float elapsedTime;
        private float nextElapsedTime = Random.NextInt(5) + 1;
        private readonly TmxTileset tilSet;
        private readonly int baseId;
        private readonly bool collideable;

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
                SpriteType.NPC_PartyMember => new PartyMember(tmxObject, state, map, ui, gameState, graph),
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
            this.tilSet = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
            this.baseId = tmxObject.Tile.Gid - this.tilSet.FirstGid;
            this.canMove = bool.Parse(this.tmxObject.Properties["CanMove"]);
            this.collideable = bool.Parse(this.tmxObject.Properties["Collideable"]);
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            
            var pos = new Vector2
            {
                X = this.tmxObject.X + (int) (this.tmxObject.Width / 2.0),
                Y = this.tmxObject.Y - (int) (this.tmxObject.Height / 2.0)
            };

            this.Entity.SetPosition(pos);
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this.tilSet.Image.Texture, this.tilSet.TileWidth, this.tilSet.TileHeight,  this.tilSet.Spacing);
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
            this.animator.RenderLayer = 15;

            var fullArea = new Rectangle
            {
                X = (int) (-this.tmxObject.Width / 2.0f),
                Y = (int) (-this.tmxObject.Height / 2.0f),
                Width = (int) this.tmxObject.Width,
                Height = (int) this.tmxObject.Height
            };

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this,fullArea));
            collider.IsTrigger = true;

            if (!this.collideable)
            {
                return;
            }

            var a = this.tmxObject.Height/2 - this.tmxObject.Width/2; // 16

            var box = new Rectangle
            {
                X = -((int)this.tmxObject.Width / 4),
                Y = (int) (a - this.tmxObject.Width / 4),
                Width = (int)this.tmxObject.Width / 2,
                Height = (int)this.tmxObject.Width / 2
            };
            
            this.Entity.AddComponent(new BoxCollider(box));
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
            
            switch (this.state)
            {
                case MoveState.Stopped:
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

                    break;
                }
                case MoveState.Moving when this.path == null:
                    this.state = MoveState.Stopped;
                    this.animator.Pause();
                    break;
                case MoveState.Moving:
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
                    
                    var (x, y) = MapScene.ToRealLocation(this.path[this.currentPathIndex], this.map);
                    var angle = (float)Math.Atan2(y - p1.Y, x - p1.X);
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
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
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