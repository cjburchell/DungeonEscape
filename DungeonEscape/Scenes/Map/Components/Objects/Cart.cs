using System.Collections.Generic;
using System.Linq;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.Sprites;

    public class Cart : Component, IUpdatable
    {
        private readonly Entity _toFollow;
        private readonly PlayerComponent _player;
        private readonly IGame _gameState;
        private readonly int _renderOffset;
        private SpriteAnimator _animation;
        private Mover _mover;
        private List<Nez.Textures.Sprite> _sprites;
        private SpriteAnimator _animator;
        private const float MoveSpeed = 150;

        public Cart(Entity toFollow, PlayerComponent player, IGame gameState, int renderOffset)
        {
            this._toFollow = toFollow;
            this._player = player;
            this._gameState = gameState;
            _renderOffset = renderOffset;
        }
        
        private void UpdateAnimation()
        {
            _animator.AddAnimation("WalkDown", new[]
            {
                _sprites[6],
                _sprites[7],
                _sprites[8],
                _sprites[7]
            });

            _animator.AddAnimation("WalkUp", new[]
            {
                _sprites[0],
                _sprites[1],
                _sprites[2],
                _sprites[1]
            });

            _animator.AddAnimation("WalkRight", new[]
            {
                _sprites[3],
                _sprites[4],
                _sprites[5],
                _sprites[4]
            });

            _animator.AddAnimation("WalkLeft", new[]
            {
                _sprites[9],
                _sprites[10],
                _sprites[11],
                _sprites[10]
            });
            
            _animator.SetSprite(_sprites[0]);
        }

        public override void OnAddedToEntity()
        {
            const int heroHeight = 54;
            const int heroWidth = 48;
            
            base.OnAddedToEntity();
            
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/cart.png");
            _sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, heroWidth, heroHeight);
            _animator = this.Entity.AddComponent(new SpriteAnimator(_sprites[0]));
            _animator.Speed = 0.5f;

            UpdateAnimation();
            
            this._animation = _animator;
            this._animation.SetEnabled(false);
            
            var overWater = this._player.IsOverWater();
            this._animation.SetEnabled(!overWater);
            this._mover = this.Entity.AddComponent(new Mover());
            _animation.RenderLayer = (int)(_renderOffset - this.Entity.Position.Y + 12);
        }

        public void Update()
        {
            if (this._gameState.IsPaused)
            {
                return;
            }
            
            if (this._player.IsOverWater() || !this._gameState.Party.CurrentMapIsOverWorld || !this._gameState.Party.InactiveMembers.Any())
            {
                this._animation.SetEnabled(false);
                this.Entity.SetPosition(this._player.Entity.Position);
                this._animation.Pause();
                return;
            }

            this._animation.SetEnabled(true);

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