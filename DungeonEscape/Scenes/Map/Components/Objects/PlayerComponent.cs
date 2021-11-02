using System.Collections.Generic;
using System.Linq;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Tiled;
using Nez.UI;
using Random = Nez.Random;

namespace DungeonEscape.Scenes.Map.Components.Objects
{
    public class PlayerComponent : Component, IUpdatable, ITriggerListener
    {
        private readonly TmxMap map;
        private readonly Label debugText;
        private readonly List<RandomMonster> randomMonsters;
        private readonly UISystem ui;
        private const float MoveSpeed = 150;
        private SpriteAnimator animator;
        private SpriteAnimator shipAnimator;
        private VirtualIntegerAxis xAxisInput;
        private VirtualIntegerAxis yAxisInput;
        private IGame GameState { get; }

        public PlayerComponent(IGame gameState, TmxMap map, Label debugText, List<RandomMonster> randomMonsters, UISystem ui)
        {
            this.map = map;
            this.debugText = debugText;
            this.randomMonsters = randomMonsters;
            this.ui = ui;
            this.GameState = gameState;
        }

        private Mover mover;
        private VirtualButton actionButton;

        public override void OnAddedToEntity()
        {
            this.mover = this.Entity.AddComponent(new Mover());
            {
                // ReSharper disable once StringLiteralTypo
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

            this.statusWindow =
                new PartyStatusWindow(this.GameState.Party, this.ui.Canvas);
            
            this.goldWindow =
                new GoldWindow(this.GameState.Party, this.ui.Canvas);

            var overWater = this.IsOverWater();
            this.shipAnimator.SetEnabled(overWater);
            this.animator.SetEnabled(!overWater);
        }

        public override void OnRemovedFromEntity()
        {
            this.xAxisInput.Deregister();
            this.yAxisInput.Deregister();
            this.actionButton.Deregister();
        }

        private bool IsOverWater()
        {
            var (x, y) = MapScene.ToMapGrid(this.Entity.Position, this.map);
            var tile = this.map.GetLayer<TmxLayer>("water").GetTile(x, y);
            return tile != null && tile.Gid != 0;
        }

        private bool UpdateMovement()
        {
            var overWater = this.IsOverWater();
            this.shipAnimator.SetEnabled(overWater);
            this.animator.SetEnabled(!overWater);
            // handle movement and animations    
            var moveDir = new Vector2(this.xAxisInput.Value, this.yAxisInput.Value);
            
            if (moveDir == Vector2.Zero)
            {
                this.shipAnimator.Pause();
                this.animator.Pause();
                return false;
            }
            
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
            
            var movement = moveDir * (overWater?MoveSpeed*1.5f:MoveSpeed) * Time.DeltaTime;
            var newPoint = movement + this.Entity.Position;

            var minX = this.map.TileWidth / 2.0f;
            var maxX = this.map.Width * this.map.TileWidth - this.map.TileWidth/2.0f;
                
            if (newPoint.X < minX)
            {
                newPoint.X = this.GameState.Party.CurrentMapId != 0 ? minX : maxX;
            }
            else if (newPoint.X > maxX)
            {
                newPoint.X = this.GameState.Party.CurrentMapId != 0 ? maxX : minX;
            }

            var minY = this.map.TileHeight / 2.0f;
            var maxY = this.map.Height * this.map.TileHeight - this.map.TileHeight/2.0f;
                
            if (newPoint.Y < minY)
            {
                newPoint.Y = this.GameState.Party.CurrentMapId != 0 ? minY : maxY;
            }
            else if (newPoint.Y > maxY)
            {
                newPoint.Y = this.GameState.Party.CurrentMapId != 0 ? maxY : minY;
            }

            movement = newPoint - this.Entity.Position;

            this.GameState.Party.CurrentPosition = MapScene.ToMapGrid(this.Entity.Position, this.map);
            if (this.GameState.Party.CurrentMapId == 0)
            {
                this.GameState.Party.OverWorldPosition = this.GameState.Party.CurrentPosition;
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

            if (this.mover.CalculateMovement(ref movement, out _))
            {
                return false;
            }

            this.mover.ApplyMovement(movement);
            return true;
        }

        void IUpdatable.Update()
        {
            if (this.GameState.IsPaused)
            {
                this.GameState.UpdatePauseState();
                this.statusWindow.CloseWindow(false);
                this.goldWindow.CloseWindow(false);
                return;
            }

            if (this.debugText.IsVisible())
            {
                var currentBiome = this.GetCurrentBiome();
                this.debugText.SetText(
                    $"B: {currentBiome}, G: {MapScene.ToMapGrid(this.Entity.Position, this.map)}, R: {this.Entity.Position}");
            }
            
            if (this.actionButton.IsReleased)
            {
                foreach (var overObject in this.currentlyOverObjects)
                {
                    if (overObject.OnAction(this.GameState.Party))
                    {
                        break;
                    }
                }
            }

            if (this.GameState.IsPaused)
            {
                this.statusWindow.CloseWindow(false);
                this.goldWindow.CloseWindow(false);
                return;
            }

            if (!this.UpdateMovement())
            {
                this.statusWindow.ShowWindow();
                this.goldWindow.ShowWindow();
                return;
            }

            this.statusWindow.CloseWindow(false);
            this.goldWindow.CloseWindow(false);

            if (this.GameState.IsPaused)
            {
                return;
            }

            if (this.CheckForMonsterEncounter())
            {
                this.DoMonsterEncounter();
            }
        }

        private void DoMonsterEncounter()
        {
            var currentBiome = this.GetCurrentBiome();
            var availableMonsters = new List<Monster>();
            foreach (var monster in this.randomMonsters.Where(item =>
                item.Biome == currentBiome || item.Biome == Biome.All && item.Data.MinLevel >= this.GameState.Party.Members.First().Level))
            {
                for (var i = 0; i < monster.Probability; i++)
                {
                    availableMonsters.Add(monster.Data);
                }
            }

            if (availableMonsters.Count == 0)
            {
                return;
            }
            
            const int MaxMonstersToFight = 10;
            var maxMonsters = this.GameState.Party.Members.First().Level / 4 + this.GameState.Party.Members.Count;
            if (maxMonsters > MaxMonstersToFight)
            {
                maxMonsters = MaxMonstersToFight;
            }
            
            var numberOfMonsters = Random.NextInt(maxMonsters) + 1;
            var monsters = new List<Monster>();
            for (var i = 0; i < numberOfMonsters; i++)
            {
                var monsterNub = Random.NextInt(availableMonsters.Count);
                monsters.Add(availableMonsters[monsterNub]);
            }
            
            this.GameState.StartFight(monsters);
        }

        private Biome GetCurrentBiome()
        {
            if (this.GameState.Party.CurrentMapId != 0)
            {
                return Biome.None;
            }

            var (x, y) = MapScene.ToMapGrid(this.Entity.Position, this.map);
            var tile = this.map.GetLayer<TmxLayer>("biomes")?.GetTile(x, y);
            if (tile != null)
            {
                return (Biome) (tile.Gid - 900);
            }

            return Biome.None;

        }

        private bool CheckForMonsterEncounter()
        {
            var currentBiome = this.GetCurrentBiome();
            return this.randomMonsters.Count(item => item.Biome == currentBiome || item.Biome == Biome.All) != 0 && Random.Chance(1.0f / 64.0f);
        }

        private readonly List<ICollidable> currentlyOverObjects = new List<ICollidable>();
        private PartyStatusWindow statusWindow;
        private GoldWindow goldWindow;


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
            
            this.currentlyOverObjects.Add(objCollider.Object);
            
            objCollider.Object.OnHit(this.GameState.Party);
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
            
            this.currentlyOverObjects.Remove(objCollider.Object);
        }
    }
}    