using System;
using System.Collections.Generic;
using System.Linq;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Tiled;
using Nez.UI;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class Player : Component, IUpdatable, ITriggerListener
    {
        private readonly TmxMap map;
        private readonly Label debugText;
        private const float MoveSpeed = 150;
        private SpriteAnimator animator;
        private SpriteAnimator shipAnimator;
        private VirtualIntegerAxis xAxisInput;
        private VirtualIntegerAxis yAxisInput;
        public IGame GameState { get; }

        public Player(IGame gameState, TmxMap map, Label debugText)
        {
            this.map = map;
            this.debugText = debugText;
            this.GameState = gameState;
        }

        private Mover mover;
        private VirtualButton actionButton;

        public override void OnAddedToEntity()
        {
            this.mover = this.Entity.AddComponent(new Mover());
            {
                var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/playeranimation.png");
                var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, 32, 32);
                this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
                this.animator.Speed = 0.5f;
                this.animator.RenderLayer = 10;
                this.animator.AddAnimation("WalkDown", new[]
                {
                    sprites[0],
                    sprites[1]
                });

                this.animator.AddAnimation("WalkUp", new[]
                {
                    sprites[2],
                    sprites[3]
                });

                this.animator.AddAnimation("WalkRight", new[]
                {
                    sprites[4],
                    sprites[5]
                });

                this.animator.AddAnimation("WalkLeft", new[]
                {
                    sprites[6],
                    sprites[7]
                });
            }

            {
                var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/ship.png");
                var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, 32, 32);
                this.shipAnimator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
                this.shipAnimator.Speed = 0.5f;
                this.shipAnimator.RenderLayer = 10;
                this.shipAnimator.AddAnimation("WalkDown", new[]
                {
                    sprites[0],
                    sprites[1]
                });

                this.shipAnimator.AddAnimation("WalkUp", new[]
                {
                    sprites[2],
                    sprites[3]
                });

                this.shipAnimator.AddAnimation("WalkRight", new[]
                {
                    sprites[4],
                    sprites[5]
                });

                this.shipAnimator.AddAnimation("WalkLeft", new[]
                {
                    sprites[6],
                    sprites[7]
                });
            }

            this.Entity.AddComponent(new BoxCollider(-8, -8, 16, 16));
            
            this.xAxisInput = new VirtualIntegerAxis();
            this.xAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadLeftRight());
            this.xAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickX());
            this.xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Left, Keys.Right));
            this.xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D));
            
            this.yAxisInput = new VirtualIntegerAxis();
            this.yAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadUpDown());
            this.yAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickY());
            this.yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Up, Keys.Down));
            this.yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.W, Keys.S));
            
            this.actionButton = new VirtualButton();
            this.actionButton.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));
            this.actionButton.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));
        }

        public override void OnRemovedFromEntity()
        {
            this.xAxisInput.Deregister();
            this.yAxisInput.Deregister();
            this.actionButton.Deregister();
        }

        private void UpdateMovement()
        {
            var mapPoint = MapScene.ToMapGrid(this.Entity.Position, this.map);
            var tile = this.map.GetLayer<TmxLayer>("water").GetTile(mapPoint.X, mapPoint.Y);
            if (tile != null && tile.Gid != 0)
            {
                this.shipAnimator.SetEnabled(true);
            }
            else
            {
                this.shipAnimator.SetEnabled(false);
            }
            
            if (tile != null && tile.Gid != 0)
            {
                this.animator.SetEnabled(false);
            }
            else
            {
                this.animator.SetEnabled(true);
            }

            // handle movement and animations
            var moveDir = new Vector2(this.xAxisInput.Value, this.yAxisInput.Value);
            var animation = "WalkDown";

            if (moveDir.X < 0)
            {
                animation = "WalkLeft";
            }
            else if (moveDir.X > 0)
            {
                animation = "WalkRight";
            }

            if (moveDir.Y < 0)
            {
                animation = "WalkUp";
            }
            else if (moveDir.Y > 0)
            {
                animation = "WalkDown";
            }

            if (moveDir != Vector2.Zero)
            {
                var movement = moveDir * MoveSpeed * Time.DeltaTime;
                var newPoint = movement + this.Entity.Position;

                var minX = this.map.TileWidth / 2.0f;
                var maxX = this.map.Width * this.map.TileWidth - this.map.TileWidth/2.0f;
                
                if (newPoint.X < minX)
                {
                    newPoint.X = this.GameState.CurrentMapId != 0 ? minX : maxX;
                }
                else if (newPoint.X > maxX)
                {
                    newPoint.X = this.GameState.CurrentMapId != 0 ? maxX : minX;
                }

                var minY = this.map.TileHeight / 2.0f;
                var maxY = this.map.Height * this.map.TileHeight - this.map.TileHeight/2.0f;
                
                if (newPoint.Y < minY)
                {
                    newPoint.Y = this.GameState.CurrentMapId != 0 ? minY : maxY;
                }
                else if (newPoint.Y > maxY)
                {
                    newPoint.Y = this.GameState.CurrentMapId != 0 ? maxY : minY;
                }

                movement = newPoint - this.Entity.Position;


                if (this.GameState.CurrentMapId == 0)
                {
                    this.GameState.Player.OverWorldPos = MapScene.ToMapGrid(this.Entity.Position, this.map);
                }

                if (!this.animator.IsAnimationActive(animation))
                {
                    this.animator.Play(animation);
                }
                else
                {
                    this.animator.UnPause();
                }

                if (!this.shipAnimator.IsAnimationActive(animation))
                {
                    this.shipAnimator.Play(animation);
                }
                else
                {
                    this.shipAnimator.UnPause();
                }

                this.mover.CalculateMovement(ref movement, out _);
                this.mover.ApplyMovement(movement);
            }
            else
            {
                this.shipAnimator.Pause();
                this.animator.Pause();
            }
        }

        void IUpdatable.Update()
        {
            if (this.GameState.IsPaused)
            {
                return;
            }

            this.debugText.SetText($"G: {MapScene.ToMapGrid(this.Entity.Position, this.map)}, R: {this.Entity.Position}");

            if (this.actionButton.IsPressed)
            {
                foreach (var overObject in this.currentlyOverObjects)
                {
                    if (overObject.OnAction(this))
                    {
                        break;
                    }
                }
            }

            this.UpdateMovement();
        }
        
        private readonly List<ICollidable> currentlyOverObjects = new List<ICollidable>();


        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (this.GameState.IsPaused)
            {
                return;
            }

            if (!(other is ObjectBoxCollider objCollider))
            {
                return;
            }
            
            Console.WriteLine("Over Object");
            this.currentlyOverObjects.Add(objCollider.Object);
            
            objCollider.Object.OnHit(this);
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            if (this.GameState.IsPaused)
            {
                return;
            }

            if (!(other is ObjectBoxCollider objCollider))
            {
                return;
            }

            Console.WriteLine("Removed Object");
            this.currentlyOverObjects.Remove(objCollider.Object);
        }

        public bool CanOpenDoor(int doorLevel)
        {
            var key = this.GameState.Player.Items.FirstOrDefault(item => item.Type == ItemType.Key);
            if (key == null)
            {
                return false;
            }

            this.GameState.Player.Items.Remove(key);
            return  true;
        }

        public bool CanOpenChest(int level)
        {
            return true;
        }
    }
}    