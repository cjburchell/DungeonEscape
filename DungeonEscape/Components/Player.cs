using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;

namespace DungeonEscape.Components
{
    public enum SpriteType
    {
        Ship,
        Door,
        Chest,
        NPC,
        Monster,
        Warp
    }
    
    public class Player : Component, IUpdatable, ITriggerListener
    {
        private const float MoveSpeed = 150;
        private SpriteAnimator animator;
        private VirtualIntegerAxis xAxisInput;
        private VirtualIntegerAxis yAxisInput;
        SubpixelVector2 subpixelV2;
        public bool IsControllable { get; set; }
        public IGame GameState { get; }

        public Player(IGame gameState)
        {
            this.GameState = gameState;
        }

        private Mover mover;
        private VirtualButton actionButton;

        public override void OnAddedToEntity()
        {
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/playeranimation.png");
            var sprites =  Nez.Textures.Sprite.SpritesFromAtlas(texture, 32, 32);
            
            this.mover = this.Entity.AddComponent(new Mover());
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            this.animator.LayerDepth = 13;
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
                if (this.GameState.CurrentMapId == 0)
                {
                    this.GameState.Player.OverWorldPos = this.Entity.Transform.Position;
                }

                if (!this.animator.IsAnimationActive(animation))
                {
                    this.animator.Play(animation);
                }
                else
                {
                    this.animator.UnPause();
                }

                var movement = moveDir * MoveSpeed * Time.DeltaTime;

                this.mover.CalculateMovement(ref movement, out _);
                this.subpixelV2.Update(ref movement);
                this.mover.ApplyMovement(movement);
            }
            else
            {
                this.animator.Pause();
            }
        }

        void IUpdatable.Update()
        {
            if (!this.IsControllable)
            {
                return;
            }

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
        
        private List<ICollidable> currentlyOverObjects = new List<ICollidable>();
       
        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (!this.IsControllable)
            {
                return;
            }

            if (!(other is ObjectBoxCollider objCollider))
            {
                return;
            }
            
            this.currentlyOverObjects.Add(objCollider.Object);
            
            objCollider.Object.OnHit(this);
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            if (!this.IsControllable)
            {
                return;
            }

            if (!(other is ObjectBoxCollider objCollider))
            {
                return;
            }

            this.currentlyOverObjects.Remove(objCollider.Object);
        }

        public bool CanOpenDoor(in int doorLevel)
        {
            return true;
        }

        public bool CanOpenChest(in int level)
        {
            return true;
        }
    }
}    