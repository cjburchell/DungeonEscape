using System.Collections.Generic;
using System.Linq;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.Sprites;
    using State;

    public class Follower : Component, IUpdatable
    {
        //private readonly Hero _hero;
        private readonly int _order;
        private readonly Entity _toFollow;
        private readonly PlayerComponent _player;
        private readonly IGame _gameState;
        private readonly int _renderOffset;
        private SpriteAnimator _animation;
        private Mover _mover;
        private Hero _lastHero;
        private List<Nez.Textures.Sprite> _sprites;
        private SpriteAnimator _animator;
        private const float MoveSpeed = 150;
        private const int DeadAnimationIndex = 144;

        public Follower(int order, Entity toFollow, PlayerComponent player, IGame gameState, int renderOffset)
        {
            _order = order;
            this._toFollow = toFollow;
            this._player = player;
            this._gameState = gameState;
            _renderOffset = renderOffset;
        }
        
        private void UpdateAnimation()
        {
            var hero = this._gameState.Party.ActiveMembers.First(i => i.Order == this._order);
            if (_lastHero == hero)
            {
                return;    
            }

            _lastHero = hero;
            
            var animationBaseIndex = (int) hero.Class * 16 + (int) hero.Gender * 8;
            _animator.AddAnimation("WalkDown", new[]
            {
                _sprites[animationBaseIndex + 4],
                _sprites[animationBaseIndex + 5]
            });

            _animator.AddAnimation("WalkUp", new[]
            {
                _sprites[animationBaseIndex + 0],
                _sprites[animationBaseIndex + 1]
            });

            _animator.AddAnimation("WalkRight", new[]
            {
                _sprites[animationBaseIndex + 2],
                _sprites[animationBaseIndex + 3]
            });

            _animator.AddAnimation("WalkLeft", new[]
            {
                _sprites[animationBaseIndex + 6],
                _sprites[animationBaseIndex + 7]
            });
            
            _animator.SetSprite(_sprites[(hero.IsDead ? DeadAnimationIndex : animationBaseIndex) + 4]);
        }

        public override void OnAddedToEntity()
        {
            const int heroHeight = 48;
            const int heroWidth = MapScene.DefaultTileSize;
            
            base.OnAddedToEntity();
            
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/hero.png");
            _sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, heroWidth, heroHeight);
            var hero = this._gameState.Party.ActiveMembers.First(i => i.Order == this._order);
            var animationBaseIndex = (int) hero.Class * 16 + (int) hero.Gender * 8;
            _animator = this.Entity.AddComponent(new SpriteAnimator(_sprites[(hero.IsDead ? DeadAnimationIndex : animationBaseIndex) + 4]));
            _animator.Speed = 0.5f;

            UpdateAnimation();
            
            _animator.AddAnimation("WalkDownDead", new[]
            {
                _sprites[DeadAnimationIndex + 4],
                _sprites[DeadAnimationIndex + 5]
            });

            _animator.AddAnimation("WalkUpDead", new[]
            {
                _sprites[DeadAnimationIndex + 0],
                _sprites[DeadAnimationIndex + 1]
            });

            _animator.AddAnimation("WalkRightDead", new[]
            {
                _sprites[DeadAnimationIndex + 2],
                _sprites[DeadAnimationIndex + 3]
            });

            _animator.AddAnimation("WalkLeftDead", new[]
            {
                _sprites[DeadAnimationIndex + 6],
                _sprites[DeadAnimationIndex + 7]
            });

            this._animation = _animator;
            this._animation.SetEnabled(false);
            
            var overWater = this._player.IsOverWater();
            this._animation.SetEnabled(!overWater);
            this._mover = this.Entity.AddComponent(new Mover());
            _animation.RenderLayer = (int)(_renderOffset - this.Entity.Position.Y + 12);
        }

        public void Update()
        {
            UpdateAnimation();
            if (this._gameState.IsPaused)
            {
                return;
            }
            
            var overWater = this._player.IsOverWater();
            this._animation.SetEnabled(!overWater);
            if (overWater)
            {
                this.Entity.SetPosition(this._player.Entity.Position);
                this._animation.Pause();
                return;
            }
            
            var followerVector = this._toFollow.Position - this.Entity.Position;
            if (followerVector.Length() < MapScene.DefaultTileSize)
            {
                this._animation.Pause();
                return;
            }
            
            var (x, y) = this.Entity.Position;
            var (f, f1) = this._toFollow.Position;
            var angle = (float) Math.Atan2(f1 - y, f - x);
            var vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var animation = "WalkDown";

            if (Math.Abs(vector.X) > Math.Abs(vector.Y))
            {
                animation = vector.X switch
                {
                    < 0 => "WalkLeft",
                    > 0 => "WalkRight",
                    _ => animation
                };
            }
            else
            {
                animation = vector.Y switch
                {
                    < 0 => "WalkUp",
                    > 0 => "WalkDown",
                    _ => animation
                };
            }

            var hero = this._gameState.Party.ActiveMembers.First(i => i.Order == this._order);
            if (hero.IsDead)
            {
                animation += "Dead";
            }

            if (!this._animation.IsAnimationActive(animation))
            {
                this._animation.Play(animation);
            }
            else
            {
                this._animation.UnPause();
            }

            var movement = vector * MoveSpeed * Time.DeltaTime;
            if (this._mover.CalculateMovement(ref movement, out _))
            {
                this._animation.Pause();
                return;
            }

            this._mover.ApplyMovement(movement);

            if (_animation != null)
            {
                _animation.RenderLayer = (int)(_renderOffset - this.Entity.Position.Y + 12);
            }
            
        }
    }
}