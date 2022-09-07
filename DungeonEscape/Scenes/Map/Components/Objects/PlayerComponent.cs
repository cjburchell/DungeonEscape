namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Nez;
    using Nez.Sprites;
    using Nez.Tiled;
    using Nez.UI;
    using State;
    using Random = Nez.Random;

    public class PlayerComponent : Component, IUpdatable, ITriggerListener
    {
        private readonly TmxMap _map;
        private readonly Label _debugText;
        private readonly List<RandomMonster> _randomMonsters;
        private readonly UiSystem _ui;
        private const float MoveSpeed = 150;
        private SpriteAnimator _playerAnimation;
        private SpriteAnimator _shipAnimator;
        private VirtualIntegerAxis _xAxisInput;
        private VirtualIntegerAxis _yAxisInput;
        private readonly IGame _gameState;
        private readonly List<ICollidable> _currentlyOverObjects = new List<ICollidable>();
        private PartyStatusWindow _statusWindow;
        private GoldWindow _goldWindow;
        private Mover _mover;
        private VirtualButton _actionButton;
        private float _distance;
        private readonly Hero _hero;

        public PlayerComponent(Hero hero, IGame gameState, TmxMap map, Label debugText, List<RandomMonster> randomMonsters, UiSystem ui)
        {
            this._map = map;
            this._debugText = debugText;
            this._randomMonsters = randomMonsters;
            this._ui = ui;
            this._gameState = gameState;
            this._hero = hero;
        }
        
        public override void OnAddedToEntity()
        {
            const int heroHeight = 48;
            const int heroWidth = MapScene.DefaultTileSize;
            this._mover = this.Entity.AddComponent(new Mover());
            {
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
                
                const int deadAnimationIndex = 144;
            
                animator.AddAnimation("WalkDownDead", new[]
                {
                    sprites[deadAnimationIndex + 4],
                    sprites[deadAnimationIndex + 5]
                });

                animator.AddAnimation("WalkUpDead", new[]
                {
                    sprites[deadAnimationIndex + 0],
                    sprites[deadAnimationIndex + 1]
                });

                animator.AddAnimation("WalkRightDead", new[]
                {
                    sprites[deadAnimationIndex + 2],
                    sprites[deadAnimationIndex + 3]
                });

                animator.AddAnimation("WalkLeftDead", new[]
                {
                    sprites[deadAnimationIndex + 6],
                    sprites[deadAnimationIndex + 7]
                });

                this._playerAnimation = animator;
                this._playerAnimation.SetEnabled(false);
            }

            {
                var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/ship2.png");
                var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, heroWidth, 56);
                this._shipAnimator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
                this._shipAnimator.Speed = 0.5f;
                this._shipAnimator.RenderLayer = 10;
                this._shipAnimator.AddAnimation("WalkDown", new[]
                {
                    sprites[0],
                    sprites[1]
                });

                this._shipAnimator.AddAnimation("WalkUp", new[]
                {
                    sprites[4],
                    sprites[5]
                });

                this._shipAnimator.AddAnimation("WalkRight", new[]
                {
                    sprites[2],
                    sprites[3]
                });

                this._shipAnimator.AddAnimation("WalkLeft", new[]
                {
                    sprites[6],
                    sprites[7]
                });
            }
            
            const int a = heroHeight/2 - heroWidth/2; // 16

            var box = new Rectangle
            {
                X = -(heroWidth / 4),
                Y = a-heroWidth / 4,
                Width = heroWidth / 2,
                Height = heroWidth / 2
            };

            this.Entity.AddComponent(new BoxCollider(box));

            this._xAxisInput = new VirtualIntegerAxis();
            this._xAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadLeftRight());
            this._xAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickX());
            this._xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Left,
                Keys.Right));
            this._xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A,
                Keys.D));

            this._yAxisInput = new VirtualIntegerAxis();
            this._yAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadUpDown());
            this._yAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickY());
            this._yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Up,
                Keys.Down));
            this._yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.W,
                Keys.S));

            this._actionButton = new VirtualButton();
            this._actionButton.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));
            this._actionButton.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));

            this._statusWindow =
                new PartyStatusWindow(this._gameState.Party, this._ui.Canvas, this._ui.Sounds);

            this._goldWindow =
                new GoldWindow(this._gameState.Party, this._ui.Canvas, this._ui.Sounds);

            var overWater = this.IsOverWater();
            this._shipAnimator.SetEnabled(overWater);
            this._playerAnimation.SetEnabled(!overWater);

            //this.Entity.AddComponent(new SpriteTrail());
        }

        public override void OnRemovedFromEntity()
        {
            this._xAxisInput.Deregister();
            this._yAxisInput.Deregister();
            this._actionButton.Deregister();
        }

        public bool IsOverWater()
        {
            var (x, y) = MapScene.ToMapGrid(this.Entity.Position, this._map);
            var tile = this._map.GetLayer<TmxLayer>("water").GetTile(x, y);
            return tile != null && tile.Gid != 0;
        }

        private bool UpdateMovement()
        {
            var overWater = this.IsOverWater();
            this._shipAnimator.SetEnabled(overWater);
            this._playerAnimation.SetEnabled(!overWater);
            // handle movement and animations    
            var moveDir = new Vector2(this._xAxisInput.Value, this._yAxisInput.Value);
            
            if (moveDir == Vector2.Zero)
            {
                this._shipAnimator.Pause();
                this._playerAnimation.Pause();
                return false;
            }

            if (Math.Abs(moveDir.X) > Math.Abs(moveDir.Y))
            {
                moveDir.Y = 0;
            }
            else
            {
                moveDir.X = 0;
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

            var waterAnimation = animation;
            if (this._hero.IsDead)
            {
                animation += "Dead";
            }
            
            var movement = moveDir * (overWater?MoveSpeed*1.5f:MoveSpeed) * Time.DeltaTime;
            var newPoint = movement + this.Entity.Position;

            var minX = this._map.TileWidth / 2.0f;
            var maxX = this._map.Width * this._map.TileWidth - this._map.TileWidth/2.0f;
                
            if (newPoint.X < minX)
            {
                newPoint.X = !this._gameState.Party.CurrentMapIsOverWorld? minX : maxX;
            }
            else if (newPoint.X > maxX)
            {
                newPoint.X = !this._gameState.Party.CurrentMapIsOverWorld ? maxX : minX;
            }

            var minY = this._map.TileHeight / 2.0f;
            var maxY = this._map.Height * this._map.TileHeight - this._map.TileHeight/2.0f;
                
            if (newPoint.Y < minY)
            {
                newPoint.Y = !this._gameState.Party.CurrentMapIsOverWorld ? minY : maxY;
            }
            else if (newPoint.Y > maxY)
            {
                newPoint.Y = !this._gameState.Party.CurrentMapIsOverWorld ? maxY : minY;
            }

            movement = newPoint - this.Entity.Position;

            this._gameState.Party.CurrentPosition = this.Entity.Position;
            if (this._gameState.Party.CurrentMapIsOverWorld)
            {
                this._gameState.Party.OverWorldPosition = this._gameState.Party.CurrentPosition.Value;
            }

            if (! this._playerAnimation.IsAnimationActive(animation))
            {
                this._playerAnimation.Play(animation);
            }
            else
            {
                this._playerAnimation.UnPause();
            }

            if (!this._shipAnimator.IsAnimationActive(waterAnimation))
            {
                this._shipAnimator.Play(waterAnimation);
            }
            else
            {
                this._shipAnimator.UnPause();
            }

            if (this._mover.CalculateMovement(ref movement, out _))
            {
                return false;
            }

            this._distance += movement.Length();
            this._mover.ApplyMovement(movement);
            return true;
        }

        void IUpdatable.Update()
        {
            if (this._gameState.IsPaused)
            {
                this._gameState.UpdatePauseState();
                this._statusWindow.CloseWindow(false);
                this._goldWindow.CloseWindow(false);
                return;
            }

            if (this._debugText.IsVisible())
            {
                var currentBiome = MapScene.GetCurrentBiome(this._map, this.Entity.Position);
                this._debugText.SetText(
                    $"B: {currentBiome}, G: {MapScene.ToMapGrid(this.Entity.Position, this._map)}, R: {this.Entity.Position} d: {this._distance}");
            }
            
            if (this._actionButton.IsReleased)
            {
                foreach (var unused in this._currentlyOverObjects.Where(overObject => overObject.OnAction(this._gameState.Party)))
                {
                    break;
                }
            }

            if (this._gameState.IsPaused)
            {
                this._statusWindow.CloseWindow(false);
                this._goldWindow.CloseWindow(false);
                return;
            }

            if (!this.UpdateMovement())
            {
                this._statusWindow.ShowWindow();
                this._goldWindow.ShowWindow();
                return;
            }

            this._statusWindow.CloseWindow(false);
            this._goldWindow.CloseWindow(false);

            if (this._gameState.IsPaused)
            {
                return;
            }

            if (!this.CheckForFullStep())
            {
                return;
            }

            if (this.CheckForMonsterEncounter())
            {
                this.DoMonsterEncounter();
                return;
            }
            
            var message = this.CheckStatusEffects();
            message += this.CheckDamageTile();
            

            if (!string.IsNullOrWhiteSpace(message))
            {
                this._gameState.IsPaused = true;
                new TalkWindow(this._ui).Show(message, () =>
                {
                    this._gameState.IsPaused = false;
                    if (!this._gameState.Party.AliveMembers.Any())
                    {
                        this._gameState.ShowMainMenu();
                    }
                });
                return;
            }

            if (!this._gameState.Party.AliveMembers.Any())
            {
                this._gameState.ShowMainMenu();
            }
        }

        private string CheckDamageTile()
        {
            var (x, y) = MapScene.ToMapGrid(this.Entity.Position, this._map);
            var tile = this._map.GetLayer<TmxLayer>("damage")?.GetTile(x, y);
            if (tile == null || tile.Gid == 0 || !tile.TilesetTile.Properties.ContainsKey("damage"))
            {
                return "";
            }

            var message = "";
            var damage = int.Parse(tile.TilesetTile.Properties["damage"]);
            foreach (var member in this._gameState.Party.AliveMembers)
            {
                member.Health -= damage;
                this._gameState.Sounds.PlaySoundEffect("receive-damage");
                if (!member.IsDead)
                {
                    continue;
                }

                message += $"{member.Name} has died!\n";
                member.Health = 0;
            }

            return message;
        }

        private string CheckStatusEffects()
        {
            var message = "";
            foreach (var member in this._gameState.Party.AliveMembers)
            {
                message += member.CheckForExpiredStates(this._gameState.Party.StepCount, DurationType.Distance);
                member.UpdateStatusEffects(this._gameState);
                if (member.IsDead)
                {
                    message += $"{member.Name} has died!\n"; 
                }
            }

            return message;
        }

        private void DoMonsterEncounter()
        {
            var currentBiome = MapScene.GetCurrentBiome(this._map, this.Entity.Position);
            var availableMonsters = new List<Monster>();
            
            var level = this._gameState.Party.MaxLevel();
            foreach (var monster in this._randomMonsters.Where(item =>
                (item.Biome == currentBiome || item.Biome == Biome.All) && item.Data.MinLevel <= level))
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
            
            const int maxMonstersToFight = 10;
            const int maxMonsterGroups = 3;
            var maxMonsters = level / 4 + this._gameState.Party.AliveMembers.Count();
            if (maxMonsters > maxMonstersToFight)
            {
                maxMonsters = maxMonstersToFight;
            }
            
            var numberOfMonsters = Random.NextInt(maxMonsters) + 1;
            var monsters = new List<Monster>();
            var totalMonsters = 0;
            var usedMonsters = new List<int>();
            for (var group = 0; group < maxMonsterGroups-1; group++)
            {
                var availableMonstersList = availableMonsters.Where(i => !usedMonsters.Contains(i.Id)).ToArray();
                var monsterNub = Random.NextInt(availableMonstersList.Length);
                var monster = availableMonstersList[monsterNub];
                usedMonsters.Add(monster.Id);
                var numberInGroup = Random.NextInt(Math.Min(numberOfMonsters-totalMonsters, monster.GroupSize))+1;
                for (var i = 0; i < numberInGroup; i++)
                {
                    monsters.Add(monster);
                }

                totalMonsters += numberInGroup;
                if (totalMonsters >= numberOfMonsters)
                {
                    break;
                }
            }

            if (totalMonsters < numberOfMonsters)
            {
                var availableMonstersList = availableMonsters.Where(i => !usedMonsters.Contains(i.Id)).ToArray();
                var monsterNub = Random.NextInt(availableMonstersList.Length);
                var monster = availableMonstersList[monsterNub];
                var numberInGroup = Math.Min(numberOfMonsters-totalMonsters, monster.GroupSize);
                for (var i = 0; i < numberInGroup; i++)
                {
                    monsters.Add(monster);
                }
            }

            var repelActive =
                this._gameState.Party.Members.Any(
                    partyMember => partyMember.Status.Any(i => i.Type == EffectType.Repel));
            if (repelActive)
            {
                var maxHealth = this._gameState.Party.Members.Max(i => i.MaxHealth);
                foreach (var monster in monsters.ToList())
                {
                    var monsterHealth = Dice.Roll(8, monster.Health, monster.HealthConst);
                    if (monsterHealth < maxHealth)
                    {
                        monsters.Remove(monster);
                    }
                }

                if (!monsters.Any())
                {
                    return;
                }
            }
            
            this._gameState.StartFight(monsters, currentBiome);
        }

        private bool CheckForFullStep()
        {
            if (this._distance <= this._map.TileWidth)
            {
                return false;
            }

            this._gameState.Party.StepCount++;
            this._distance = 0;
            return true;
        }

        private bool CheckForMonsterEncounter()
        {
            if (this._gameState.Settings.NoMonsters)
            {
                return false;
            }
            
            var currentBiome = MapScene.GetCurrentBiome(this._map, this.Entity.Position);
            var hasRandomMonsters = this._randomMonsters.Any(item => item.Biome == currentBiome || item.Biome == Biome.All);

            //  Todo: use parties agility to calculate encounters
            return  hasRandomMonsters && Random.Chance(0.1f);
        }
        
        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (this._gameState.IsPaused)
            {
                return;
            }

            if (!(other is ObjectBoxCollider objCollider))
            {
                return;
            }
            
            this._currentlyOverObjects.Add(objCollider.Object);
            
            objCollider.Object.OnHit(this._gameState.Party);
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            if (this._gameState.IsPaused)
            {
                return;
            }

            if (!(other is ObjectBoxCollider objCollider))
            {
                return;
            }
            
            this._currentlyOverObjects.Remove(objCollider.Object);
        }
    }
}    