using GameFile;

namespace DungeonEscape.Components
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Nez;
    using Nez.Sprites;
    using Nez.Textures;
    using Scene;
    using Point = GameFile.Point;

    public class PlayerComponent : Component, IUpdatable, ITriggerListener
    {
        private const float MoveSpeed = 150;
        private SpriteAnimator animator;
        private VirtualIntegerAxis xAxisInput;
        private VirtualIntegerAxis yAxisInput;
        SubpixelVector2 subpixelV2;

        private Mover mover;

        public override void OnAddedToEntity()
        {
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/playeranimation.png");
            var sprites = Sprite.SpritesFromAtlas(texture, 32, 32);
            
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
            
            this.yAxisInput = new VirtualIntegerAxis();
            this.yAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadUpDown());
            this.yAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickY());
            this.yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Up, Keys.Down));
        }

        public override void OnRemovedFromEntity()
        {
            this.xAxisInput.Deregister();
            this.yAxisInput.Deregister();
        }

        void IUpdatable.Update()
        {
            // handle movement and animations
            var moveDir = new Vector2(this.xAxisInput.Value, this.yAxisInput.Value);
            var animation = "WalkDown";

            if (moveDir.X < 0)
                animation = "WalkLeft";
            else if (moveDir.X > 0)
                animation = "WalkRight";

            if (moveDir.Y < 0)
                animation = "WalkUp";
            else if (moveDir.Y > 0)
                animation = "WalkDown";
            
            if (moveDir != Vector2.Zero)
            {
                if (!this.animator.IsAnimationActive(animation))
                    this.animator.Play(animation);
                else
                    this.animator.UnPause();

                var movement = moveDir * MoveSpeed * Time.DeltaTime;

                this.mover.CalculateMovement(ref movement, out var res);
                this.subpixelV2.Update(ref movement);
                this.mover.ApplyMovement(movement);
            }
            else
            {
                this.animator.Pause();
            }
        }

        private bool isInTransition = false;

        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (other is ObjectBoxCollider objCollider)
            {
                
                if (objCollider.Object.Type == SpriteType.NPC.ToString())
                {
                    Console.WriteLine($"Npc: {objCollider.Object.Name}");
                }
                Console.WriteLine($"triggerEnter: {objCollider.Object.Name}");
                if (objCollider.Object.Type == SpriteType.Warp.ToString() && !this.isInTransition)
                {
                    this.isInTransition = true;
                    var mapId = int.Parse(objCollider.Object.Properties["WarpMap"]);
                    Point point = null;
                    if (objCollider.Object.Properties.ContainsKey("WarpMapX") &&
                        objCollider.Object.Properties.ContainsKey("WarpMapX"))
                    {
                        point = new Point()
                        {
                            X =  int.Parse(objCollider.Object.Properties["WarpMapX"]),
                            Y = int.Parse(objCollider.Object.Properties["WarpMapY"])
                        };
                    }

                    Core.StartSceneTransition(new FadeTransition(() =>
                    {
                        var map = new MapScene(mapId, point);
                        map.Initialize();
                        return map;
                    }));
                }
            }
            
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