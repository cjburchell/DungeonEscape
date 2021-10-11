using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using DungeonEscape.Scenes;

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
    
    public class PlayerComponent : Component, IUpdatable, ITriggerListener
    {
        private const float MoveSpeed = 150;
        private SpriteAnimator animator;
        private VirtualIntegerAxis xAxisInput;
        private VirtualIntegerAxis yAxisInput;
        SubpixelVector2 subpixelV2;
        public bool IsInTransition { get; set; } = true;
        private readonly IGame gameState;

        public PlayerComponent(IGame gameState)
        {
            this.gameState = gameState;
        }

        private Mover mover;

        public override void OnAddedToEntity()
        {
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/playeranimation.png");
            var sprites =  Nez.Textures.Sprite.SpritesFromAtlas(texture, 32, 32);
            
            this.mover = this.Entity.AddComponent(new Mover());
            this.animator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
            
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
            
            this.Entity.AddComponent(new BoxCollider(-8, -8, 16, 16));
        }

        public override void OnRemovedFromEntity()
        {
            this.xAxisInput.Deregister();
            this.yAxisInput.Deregister();
        }

        void IUpdatable.Update()
        {
            if (this.IsInTransition)
            {
                return;
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
                if (this.gameState.CurrentMapId == 0)
                {
                    this.gameState.Player.OverWorldPos = this.Entity.Transform.Position;
                }

                //var girdPos = this.Entity.Transform.Position;
                //girdPos.X = (int)(girdPos.X / 32);
                //girdPos.Y = (int)(girdPos.Y / 32);
                //Console.WriteLine(girdPos.ToString());
                
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



        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (this.IsInTransition)
            {
                return;
            }

            if (!(other is ObjectBoxCollider objCollider))
            {
                return;
            }
            
            if (objCollider.Object.Type == SpriteType.NPC.ToString())
            {
                Console.WriteLine($"Npc: {objCollider.Object.Name}");
            }
            Console.WriteLine($"triggerEnter: {objCollider.Object.Name}");
            if (objCollider.Object.Type != SpriteType.Warp.ToString())
            {
                return;
            }
            
            var mapId = int.Parse(objCollider.Object.Properties["WarpMap"]);
            Vector2? point = null;
            if (objCollider.Object.Properties.ContainsKey("WarpMapX") &&
                objCollider.Object.Properties.ContainsKey("WarpMapX"))
            {
                var map = this.gameState.GetMap(mapId);
                point = new Vector2()
                {
                    X = int.Parse(objCollider.Object.Properties["WarpMapX"]) * map.TileHeight + map.TileHeight / 2.0f,
                    Y = int.Parse(objCollider.Object.Properties["WarpMapY"]) * map.TileWidth + map.TileWidth / 2.0f
                };
            }
            else
            {
                if (mapId == 0 && this.gameState.Player.OverWorldPos != Vector2.Zero)
                {
                    point = this.gameState.Player.OverWorldPos;
                }
            }

            this.IsInTransition = true;
            MapScene.SetMap(mapId, point);
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            if (other is ObjectBoxCollider objCollider)
            {
                Console.WriteLine($"triggerEnter: {objCollider.Object.Name}");
            }
        }
    }
}