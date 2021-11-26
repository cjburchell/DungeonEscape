namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.Sprites;
    using State;

    public class Follower : Component, IUpdatable
    {
        private readonly Hero _hero;
        private readonly Entity _toFollow;
        private readonly PlayerComponent _player;
        private readonly IGame _gameState;
        private SpriteAnimator _animation;
        private Mover _mover;
        private const float MoveSpeed = 150;

        public Follower(Hero hero, Entity toFollow, PlayerComponent player, IGame gameState)
        {
            this._hero = hero;
            this._toFollow = toFollow;
            this._player = player;
            this._gameState = gameState;
        }

        public override void OnAddedToEntity()
        {
            const int heroHeight = 48;
            const int heroWidth = MapScene.DefaultTileSize;
            
            base.OnAddedToEntity();
            
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/hero.png");
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, heroWidth, heroHeight);
                
            var animationBaseIndex = (int) this._hero.Class * 16 + (int) this._hero.Gender * 8;
            var animator = this.Entity.AddComponent(new SpriteAnimator(sprites[animationBaseIndex + 4]));
            animator.Speed = 0.5f;
            animator.RenderLayer = 10;

            animator.AddAnimation("WalkDown", new[]
            {
                sprites[animationBaseIndex + 4],
                sprites[animationBaseIndex + 5]
            });

            animator.AddAnimation("WalkUp", new[]
            {
                sprites[animationBaseIndex + 0],
                sprites[animationBaseIndex + 1]
            });

            animator.AddAnimation("WalkRight", new[]
            {
                sprites[animationBaseIndex + 2],
                sprites[animationBaseIndex + 3]
            });

            animator.AddAnimation("WalkLeft", new[]
            {
                sprites[animationBaseIndex + 6],
                sprites[animationBaseIndex + 7]
            });

            this._animation = animator;
            this._animation.SetEnabled(false);
            
            var overWater = this._player.IsOverWater();
            this._animation.SetEnabled(!overWater);
            this._mover = this.Entity.AddComponent(new Mover());
        }

        public void Update()
        {
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
        }
    }
}