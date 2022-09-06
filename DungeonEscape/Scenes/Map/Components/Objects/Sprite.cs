namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private readonly TmxObject _tmxObject;
        protected readonly TmxMap Map;
        protected readonly IGame GameState;
        protected SpriteAnimator Animator;
        private Mover _mover;
        private readonly bool _canMove;
        private MoveState _state = MoveState.Stopped;
        private readonly AstarGridGraph _graph;
        private List<Point> _path;
        private const float MoveSpeed = 75;
        private int _currentPathIndex;
        // ReSharper disable once NotAccessedField.Local
        protected readonly SpriteState SpriteState;
        private float _elapsedTime;
        private float _nextElapsedTime = Random.NextInt(5) + 1;
        private readonly TmxTileset _tilSet;
        protected readonly int BaseId;
        private readonly bool _collideable;

        public static Sprite Create(TmxObject tmxObject, SpriteState state, TmxMap map, UiSystem ui, IGame gameState, AstarGridGraph graph)
        {
            if (!Enum.TryParse(tmxObject.Type, out SpriteType spriteType))
            {
                return null;
            }

            return spriteType switch
            {
                SpriteType.NpcHeal => new Healer(tmxObject, state, map, gameState, graph, ui),
                SpriteType.NpcStore => new Store(tmxObject, state, map, gameState, graph, ui),
                SpriteType.NpcSave => new Saver(tmxObject, state, map, gameState, graph, ui),
                SpriteType.NpcKey => new Store(tmxObject, state, map, gameState, graph, ui, gameState.CustomItems.Where(i=> i.Type == ItemType.Key).ToList(), "Would you like to buy a key?", false),
                SpriteType.Npc => new Character(tmxObject, state, map, ui, gameState, graph),
                SpriteType.NpcPartyMember => new PartyMember(tmxObject, state, map, ui, gameState, graph),
                _ => new Sprite(tmxObject, state, map, gameState, graph)
            };
        }
        
        protected Sprite(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph)
        {
            this.SpriteState = state;
            this._graph = graph;
            this._tmxObject = tmxObject;
            this.Map = map;
            this.GameState = gameState;
            this._tilSet = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
            this.BaseId = tmxObject.Tile.Gid - this._tilSet.FirstGid;
            this._canMove = bool.Parse(this._tmxObject.Properties["CanMove"]);
            this._collideable = bool.Parse(this._tmxObject.Properties["Collideable"]);
            this.SpriteState.Name = tmxObject.Name;
        }

        protected virtual void SetupAnimation(List<Nez.Textures.Sprite> sprites)
        {
            this.Animator.AddAnimation("WalkDown", new[]
            {
                sprites[this.BaseId + 0],
                sprites[this.BaseId + 1]
            });

            this.Animator.AddAnimation("WalkUp", new[]
            {
                sprites[this.BaseId + 6],
                sprites[this.BaseId + 7]
            });

            this.Animator.AddAnimation("WalkRight", new[]
            {
                sprites[this.BaseId + 2],
                sprites[this.BaseId + 3]
            });

            this.Animator.AddAnimation("WalkLeft", new[]
            {
                sprites[this.BaseId + 4],
                sprites[this.BaseId + 5]
            });
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            
            var pos = new Vector2
            {
                X = this._tmxObject.X + (int) (this._tmxObject.Width / 2.0),
                Y = this._tmxObject.Y - (int) (this._tmxObject.Height / 2.0)
            };

            this.Entity.SetPosition(pos);
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(this._tilSet.Image.Texture, this._tilSet.TileWidth, this._tilSet.TileHeight,  this._tilSet.Spacing);
            this.Animator = this.Entity.AddComponent(new SpriteAnimator(sprites[this.BaseId]));
            this.Animator.Speed = 0.5f;
            this.Animator.RenderLayer = 10;

            this.SetupAnimation(sprites);
            
            this._mover = this.Entity.AddComponent(new Mover());
            this.Animator.RenderLayer = 15;

            var fullArea = new Rectangle
            {
                X = (int) (-this._tmxObject.Width / 2.0f),
                Y = (int) (-this._tmxObject.Height / 2.0f),
                Width = (int) this._tmxObject.Width,
                Height = (int) this._tmxObject.Height
            };

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this, fullArea));
            collider.IsTrigger = true;

            if (!this._collideable)
            {
                return;
            }

            var a = this._tmxObject.Height/2 - this._tmxObject.Width/2; // 16

            var box = new Rectangle
            {
                X = -((int)this._tmxObject.Width / 4),
                Y = (int) (a - this._tmxObject.Width / 4),
                Width = (int)this._tmxObject.Width / 2,
                Height = (int)this._tmxObject.Width / 2
            };
            
            this.Entity.AddComponent(new BoxCollider(box));
        }

        void IUpdatable.Update()
        {
            if (this.GameState.IsPaused)
            {
                return;
            }

            if (!this._canMove)
            {
                return;
            }

            switch (this._state)
            {
                case MoveState.Stopped:
                {
                    this._elapsedTime += Time.DeltaTime;
                    if (!(this._elapsedTime >= this._nextElapsedTime))
                    {
                        return;
                    }

                    this._elapsedTime = 0;
                    this._nextElapsedTime = Random.NextInt(5) + 1;

                    if (Random.Chance(0.05f))
                    {
                        return;
                    }

                    const int maxSpacesToMove = 2;
                    var pos = this.Entity.Position;
                    var mapGoTo = new Point(Random.NextInt(maxSpacesToMove * 2 + 1) - maxSpacesToMove,
                        Random.NextInt(maxSpacesToMove * 2 + 1) - maxSpacesToMove);
                    if (mapGoTo.X < 0)
                    {
                        mapGoTo.X = 0;
                    }

                    if (mapGoTo.Y < 0)
                    {
                        mapGoTo.Y = 0;
                    }

                    if (mapGoTo.X >= this.Map.Width)
                    {
                        mapGoTo.X = this.Map.Width - 1;
                    }

                    if (mapGoTo.Y >= this.Map.Height)
                    {
                        mapGoTo.X = this.Map.Height - 1;
                    }

                    var toPos = pos + MapScene.ToRealLocation(mapGoTo, this.Map);
                    this._path = this._graph.Search(
                        MapScene.ToMapGrid(pos, this.Map),
                        MapScene.ToMapGrid(toPos, this.Map));
                    

                    if (this._path == null)
                    {
                        this._state = MoveState.Stopped;
                    }
                    else
                    {
                        if (_path.Count > 6)
                        {
                            _path = _path.GetRange(0, 6);
                        }
                        
                        this._currentPathIndex = 0;
                        this._state = MoveState.Moving;
                    }

                    break;
                }
                case MoveState.Moving when this._path == null:
                    this._state = MoveState.Stopped;
                    this.Animator.Pause();
                    break;
                case MoveState.Moving:
                {
                    var p1 = this.Entity.Position;
                    if (Vector2.Distance(p1, MapScene.ToRealLocation(this._path[this._currentPathIndex], this.Map)) <=
                        1)
                    {
                        this._currentPathIndex++;
                        if (this._currentPathIndex >= this._path.Count)
                        {
                            this._state = MoveState.Stopped;
                            this.Animator.Pause();
                            return;
                        }
                    }

                    var (x, y) = MapScene.ToRealLocation(this._path[this._currentPathIndex], this.Map);
                    var angle = (float) Math.Atan2(y - p1.Y, x - p1.X);
                    var vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var animation = "WalkDown";
                    if (Math.Abs(vector.X) > Math.Abs(vector.Y))
                    {
                        if (vector.X < 0)
                        {
                            animation = "WalkLeft";
                        }
                        else if (vector.X > 0)
                        {
                            animation = "WalkRight";
                        }
                    }
                    else
                    {
                        if (vector.Y < 0)
                        {
                            animation = "WalkUp";
                        }
                        else if (vector.Y > 0)
                        {
                            animation = "WalkDown";
                        }
                    }

                    if (!this.Animator.IsAnimationActive(animation))
                    {
                        this.Animator.Play(animation);
                    }
                    else
                    {
                        this.Animator.UnPause();
                    }

                    var movement = vector * MoveSpeed * Time.DeltaTime;
                    if (this._mover.CalculateMovement(ref movement, out _))
                    {
                        this._state = MoveState.Stopped;
                        this.Animator.Pause();
                        return;
                    }

                    this._mover.ApplyMovement(movement);
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