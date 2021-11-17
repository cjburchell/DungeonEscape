namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
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
        private readonly TmxObject _tmxObject;
        private readonly TmxMap _map;
        protected readonly IGame GameState;
        private SpriteAnimator _animator;
        private Mover _mover;
        private readonly bool _canMove;
        private MoveState _state = MoveState.Stopped;
        private readonly AstarGridGraph _graph;
        private List<Point> _path;
        private const float MoveSpeed = 75;
        private int _currentPathIndex;
        // ReSharper disable once NotAccessedField.Local
        private readonly SpriteState _spriteState;
        private float _elapsedTime;
        private float _nextElapsedTime = Random.NextInt(5) + 1;
        private readonly TmxTileset _tilSet;
        private readonly int _baseId;
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
                SpriteType.NpcKey => new KeyStore(tmxObject, state, map, gameState, graph, ui),
                SpriteType.Npc => new Character(tmxObject, state, map, ui, gameState, graph),
                SpriteType.NpcPartyMember => new PartyMember(tmxObject, state, map, ui, gameState, graph),
                _ => new Sprite(tmxObject, state, map, gameState, graph)
            };
        }
        
        protected Sprite(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph)
        {
            this._spriteState = state;
            this._graph = graph;
            this._tmxObject = tmxObject;
            this._map = map;
            this.GameState = gameState;
            this._tilSet = map.GetTilesetForTileGid(tmxObject.Tile.Gid);
            this._baseId = tmxObject.Tile.Gid - this._tilSet.FirstGid;
            this._canMove = bool.Parse(this._tmxObject.Properties["CanMove"]);
            this._collideable = bool.Parse(this._tmxObject.Properties["Collideable"]);
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
            this._animator = this.Entity.AddComponent(new SpriteAnimator(sprites[this._baseId]));
            this._animator.Speed = 0.5f;
            this._animator.RenderLayer = 10;
            this._animator.AddAnimation("WalkDown", new[]
            {
                sprites[this._baseId + 0],
                sprites[this._baseId + 1]
            });

            this._animator.AddAnimation("WalkUp", new[]
            {
                sprites[this._baseId + 6],
                sprites[this._baseId + 7]
            });

            this._animator.AddAnimation("WalkRight", new[]
            {
                sprites[this._baseId + 2],
                sprites[this._baseId + 3]
            });

            this._animator.AddAnimation("WalkLeft", new[]
            {
                sprites[this._baseId + 4],
                sprites[this._baseId + 5]
            });
            
            
            this._mover = this.Entity.AddComponent(new Mover());
            this._animator.RenderLayer = 15;

            var fullArea = new Rectangle
            {
                X = (int) (-this._tmxObject.Width / 2.0f),
                Y = (int) (-this._tmxObject.Height / 2.0f),
                Width = (int) this._tmxObject.Width,
                Height = (int) this._tmxObject.Height
            };

            var collider = this.Entity.AddComponent(new ObjectBoxCollider(this,fullArea));
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

                    if (mapGoTo.X >= this._map.Width)
                    {
                        mapGoTo.X = this._map.Width - 1;
                    }

                    if (mapGoTo.Y >= this._map.Height)
                    {
                        mapGoTo.X = this._map.Height - 1;
                    }

                    var toPos = pos + MapScene.ToRealLocation(mapGoTo, this._map);
                    this._path = this._graph.Search(
                        MapScene.ToMapGrid(pos, this._map),
                        MapScene.ToMapGrid(toPos, this._map));

                    if (this._path == null)
                    {
                        this._state = MoveState.Stopped;
                    }
                    else
                    {
                        this._currentPathIndex = 0;
                        this._state = MoveState.Moving;
                    }

                    break;
                }
                case MoveState.Moving when this._path == null:
                    this._state = MoveState.Stopped;
                    this._animator.Pause();
                    break;
                case MoveState.Moving:
                {
                    var p1 = this.Entity.Position;
                    if (Vector2.Distance(p1, MapScene.ToRealLocation(this._path[this._currentPathIndex], this._map)) <=
                        1)
                    {
                        this._currentPathIndex++;
                        if (this._currentPathIndex >= this._path.Count)
                        {
                            this._state = MoveState.Stopped;
                            this._animator.Pause();
                            return;
                        }
                    }

                    var (x, y) = MapScene.ToRealLocation(this._path[this._currentPathIndex], this._map);
                    var angle = (float) Math.Atan2(y - p1.Y, x - p1.X);
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

                    if (!this._animator.IsAnimationActive(animation))
                    {
                        this._animator.Play(animation);
                    }
                    else
                    {
                        this._animator.UnPause();
                    }

                    var movement = vector * MoveSpeed * Time.DeltaTime;
                    if (this._mover.CalculateMovement(ref movement, out _))
                    {
                        this._state = MoveState.Stopped;
                        this._animator.Pause();
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